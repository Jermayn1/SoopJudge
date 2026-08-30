using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;

namespace SoopWorkshop.Backend.Infrastructure.Processes
{
    // Führt externe Prozesse aus und liefert Exitcode, beide Ausgabeströme und
    // die Sonderfälle Zeitüberschreitung und "Programm nicht gefunden" zurück.
    public class ProcessRunner : IProcessRunner
    {
        private readonly ILogger<ProcessRunner> _logger;

        public ProcessRunner(ILogger<ProcessRunner> logger)
        {
            _logger = logger;
        }

        public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = request.FileName,
                WorkingDirectory = request.WorkingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                // Ab JDK 18 ist UTF-8 der Standardzeichensatz der JVM. Ohne diese
                // Angabe würde .NET unter Windows mit der Konsolen-Codepage dekodieren
                // und Umlaute in der Programmausgabe zerlegen.
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            foreach (var argument in request.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();
            }
            catch (Win32Exception exception)
            {
                _logger.LogWarning(exception, "'{FileName}' konnte nicht gestartet werden.", request.FileName);
                return new ProcessResult(-1, string.Empty, string.Empty, TimedOut: false, ExecutableNotFound: true);
            }

            // Beide Ströme sofort und gleichzeitig leeren. Wird nur einer gelesen,
            // blockiert der Kindprozess, sobald der Puffer des anderen voll ist.
            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(request.Timeout);

            await WriteStandardInputAsync(process, request.StandardInput, timeoutSource.Token);

            try
            {
                await process.WaitForExitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException)
            {
                KillProcessTree(process, request.FileName);

                var partialOutput = await ReadRemainingAsync(standardOutputTask);
                var partialError = await ReadRemainingAsync(standardErrorTask);

                // Bricht der Aufrufer ab (z. B. beim Herunterfahren), ist das keine
                // Zeitüberschreitung der Abgabe und wird nach oben durchgereicht.
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogWarning(
                    "'{FileName}' hat die Zeitgrenze von {Seconds} s ueberschritten und wurde beendet.",
                    request.FileName,
                    request.Timeout.TotalSeconds);

                return new ProcessResult(-1, partialOutput, partialError, TimedOut: true, ExecutableNotFound: false);
            }

            return new ProcessResult(
                process.ExitCode,
                await standardOutputTask,
                await standardErrorTask,
                TimedOut: false,
                ExecutableNotFound: false);
        }

        // Schreibt die Eingabe und schließt stdin, damit ein wartendes Programm weiterläuft.
        private async Task WriteStandardInputAsync(Process process, string? input, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrEmpty(input))
                {
                    await process.StandardInput.WriteAsync(input.AsMemory(), cancellationToken);
                }

                process.StandardInput.Close();
            }
            catch (IOException exception)
            {
                // Ein Programm, das seine Eingabe nie liest und vorher endet, ist ein
                // gültiger Fall — die Auswertung darf daran nicht scheitern.
                _logger.LogDebug(exception, "Eingabe konnte nicht vollstaendig geschrieben werden.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Schreiben der Eingabe wurde abgebrochen.");
            }
        }

        // Nach dem Beenden schließen sich die Pipes, die Lesevorgänge laufen dann aus.
        private async Task<string> ReadRemainingAsync(Task<string> readTask)
        {
            try
            {
                return await readTask;
            }
            catch (Exception exception) when (exception is IOException or OperationCanceledException)
            {
                _logger.LogDebug(exception, "Ausgabe eines beendeten Prozesses konnte nicht gelesen werden.");
                return string.Empty;
            }
        }

        private void KillProcessTree(Process process, string fileName)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
            {
                // Der Prozess hat sich zwischen Prüfung und Kill selbst beendet.
                _logger.LogDebug(exception, "'{FileName}' liess sich nicht beenden.", fileName);
            }
        }
    }
}
