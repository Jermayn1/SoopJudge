using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Schreibt die Abgabe ins Arbeitsverzeichnis und ruft javac auf. Läuft als
    // erster Checker, weil alle späteren das Kompilierergebnis aus dem Kontext
    // brauchen.
    public class CompilabilityChecker : IEvaluationChecker
    {
        private readonly IProcessRunner _processRunner;
        private readonly EvaluationOptions _options;

        public CompilabilityChecker(IProcessRunner processRunner, IOptions<EvaluationOptions> options)
        {
            _processRunner = processRunner;
            _options = options.Value;
        }

        public EvaluationCategory Category => EvaluationCategory.Compilability;

        public int Order => EvaluationCheckerOrder.Compilability;

        // Ohne Kompilieren geht keine Aufgabe - immer anwendbar.
        public bool IsApplicable(EvaluationContext context) => true;

        public async Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            var files = context.Files;
            var fileNames = new List<string>();

            foreach (var file in files)
            {
                // Zweite Verteidigungslinie hinter der Upload-Prüfung: nur der reine
                // Dateiname darf ins Arbeitsverzeichnis, niemals ein Pfad.
                var fileName = Path.GetFileName(file.FileName);
                await File.WriteAllTextAsync(Path.Combine(context.WorkingDirectory, fileName), file.Content, cancellationToken);
                fileNames.Add(fileName);
            }

            var arguments = new List<string>
            {
                // Die Dateien werden als UTF-8 geschrieben; ohne diese Angabe würde javac
                // unter Windows mit der Plattform-Codepage lesen und Umlaute zerlegen.
                "-encoding", "UTF-8",

                // Auch die Fehlermeldungen des Compilers als UTF-8 ausgeben — sonst
                // hängt ihre Lesbarkeit von der Codepage des Servers ab.
                "-J-Dstdout.encoding=UTF-8",
                "-J-Dstderr.encoding=UTF-8"
            };

            // Bewusst nur die Dateinamen, nicht die vollen Pfade: javac stellt sie den
            // Fehlermeldungen voran, und der Teilnehmer soll "Main.java:3: error" lesen
            // und nicht das Temp-Verzeichnis des Servers.
            arguments.AddRange(fileNames);

            var process = await _processRunner.RunAsync(
                new ProcessRequest(
                    "javac",
                    arguments,
                    context.WorkingDirectory,
                    StandardInput: null,
                    TimeSpan.FromSeconds(_options.CompileTimeoutSeconds)),
                cancellationToken);

            var success = process.Success;
            var errorOutput = success ? string.Empty : DescribeFailure(process);

            // Ergebnis für die nachfolgenden Checker hinterlegen.
            context.Compilation = new CompilationResult
            {
                Success = success,
                WorkingDirectory = context.WorkingDirectory,
                ErrorOutput = errorOutput,
                MainClassName = success ? FindMainClassName(files) : null
            };

            var result = new TestCaseResult
            {
                Id = Guid.NewGuid(),
                Description = "Der Code kompiliert",
                Passed = success,
                ActualOutput = errorOutput
            };

            return success
                ? CheckerOutcome.Of(result)
                : CheckerOutcome.WithTip(
                    "Der Code kompiliert nicht fehlerfrei. Prüfe die Fehlermeldung des Compilers auf Tippfehler oder fehlende Importe.",
                    result);
        }

        // Übersetzt das Prozessergebnis in eine Meldung, die dem Teilnehmer sagt,
        // was schiefgelaufen ist — ein leerer Text wäre hier wertlos.
        private string DescribeFailure(ProcessResult process)
        {
            if (process.ExecutableNotFound)
                return "„javac“ wurde nicht gefunden. Ist das JDK installiert und im PATH?";

            if (process.TimedOut)
                return $"Zeitüberschreitung beim Kompilieren (Grenze: {_options.CompileTimeoutSeconds} Sekunden).";

            return string.IsNullOrWhiteSpace(process.StandardError)
                ? process.StandardOutput
                : process.StandardError;
        }

        // Sucht die Datei mit "public static void main" und gibt den dazugehörigen
        // Klassennamen zurück. In Java muss der Dateiname mit dem Klassennamen übereinstimmen.
        private static string? FindMainClassName(IReadOnlyList<SubmissionFile> files)
        {
            var mainFile = files.FirstOrDefault(f => f.Content.Contains("public static void main"));
            return mainFile is null ? null : Path.GetFileNameWithoutExtension(mainFile.FileName);
        }
    }
}
