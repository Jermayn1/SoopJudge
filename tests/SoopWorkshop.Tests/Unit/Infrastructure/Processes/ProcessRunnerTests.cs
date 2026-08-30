using Microsoft.Extensions.Logging.Abstractions;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Infrastructure.Processes;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Processes
{
    public class ProcessRunnerTests : IDisposable
    {
        private readonly ScriptFactory _scripts = new();
        private readonly ProcessRunner _runner = new(NullLogger<ProcessRunner>.Instance);

        public void Dispose() => _scripts.Dispose();

        private ProcessRequest Request(
            IReadOnlyList<string> arguments,
            string? input = null,
            int timeoutSeconds = 30) =>
            new(_scripts.Interpreter, arguments, _scripts.WorkingDirectory, input, TimeSpan.FromSeconds(timeoutSeconds));

        [Fact]
        public async Task RunAsync_ProgrammEndetMitFehlercode_LiefertExitcodeUndAusgabe()
        {
            var arguments = _scripts.Create(
                windowsScript: "@echo off\r\necho hallo\r\nexit /b 3\r\n",
                unixScript: "echo hallo\nexit 3\n");

            var result = await _runner.RunAsync(Request(arguments), CancellationToken.None);

            result.ExitCode.ShouldBe(3);
            result.StandardOutput.ShouldContain("hallo");
            result.Success.ShouldBeFalse();
            result.TimedOut.ShouldBeFalse();
            result.ExecutableNotFound.ShouldBeFalse();
        }

        // Gegenprobe zum behobenen Deadlock: früher wurde nur einer der beiden Ströme
        // gelesen, wodurch der Kindprozess beim Volllaufen des anderen Puffers stehenblieb.
        [Fact]
        public async Task RunAsync_ProgrammSchreibtVielAufBeideStroeme_LiefertBeideVollstaendig()
        {
            var arguments = _scripts.Create(
                windowsScript:
                    "@echo off\r\n" +
                    "for /L %%i in (1,1,500) do (\r\n" +
                    "  echo AUSGABEZEILE\r\n" +
                    "  echo FEHLERZEILE 1>&2\r\n" +
                    ")\r\n",
                unixScript:
                    "i=0\n" +
                    "while [ $i -lt 500 ]; do\n" +
                    "  echo AUSGABEZEILE\n" +
                    "  echo FEHLERZEILE 1>&2\n" +
                    "  i=$((i+1))\n" +
                    "done\n");

            var result = await _runner.RunAsync(Request(arguments), CancellationToken.None);

            result.ExitCode.ShouldBe(0);
            CountLines(result.StandardOutput, "AUSGABEZEILE").ShouldBe(500);
            CountLines(result.StandardError, "FEHLERZEILE").ShouldBe(500);
        }

        [Fact]
        public async Task RunAsync_ProgrammLaeuftZuLange_MeldetZeitueberschreitung()
        {
            var arguments = _scripts.Create(
                windowsScript: "@echo off\r\nping -n 30 127.0.0.1 >nul\r\n",
                unixScript: "sleep 30\n");

            var result = await _runner.RunAsync(Request(arguments, timeoutSeconds: 2), CancellationToken.None);

            result.TimedOut.ShouldBeTrue();
            result.Success.ShouldBeFalse();
            result.ExecutableNotFound.ShouldBeFalse();
        }

        [Fact]
        public async Task RunAsync_UnbekanntesProgramm_MeldetNichtGefunden()
        {
            var request = new ProcessRequest(
                "gibtesganzsichernicht",
                [],
                _scripts.WorkingDirectory,
                StandardInput: null,
                TimeSpan.FromSeconds(5));

            var result = await _runner.RunAsync(request, CancellationToken.None);

            result.ExecutableNotFound.ShouldBeTrue();
            result.TimedOut.ShouldBeFalse();
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public async Task RunAsync_MitEingabe_ReichtEingabeAnDasProgrammDurch()
        {
            var arguments = _scripts.Create(
                windowsScript: "@echo off\r\nset /p eingabe=\r\necho gelesen:%eingabe%\r\n",
                unixScript: "read eingabe\necho \"gelesen:$eingabe\"\n");

            var result = await _runner.RunAsync(Request(arguments, input: "SoopWorkshop\n"), CancellationToken.None);

            result.StandardOutput.ShouldContain("gelesen:SoopWorkshop");
        }

        // Ein Abbruch von außen (Herunterfahren) ist keine Zeitüberschreitung der
        // Abgabe und muss als solcher nach oben durchschlagen.
        [Fact]
        public async Task RunAsync_AufruferBrichtAb_WirftOperationCanceledException()
        {
            var arguments = _scripts.Create(
                windowsScript: "@echo off\r\nping -n 30 127.0.0.1 >nul\r\n",
                unixScript: "sleep 30\n");

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            await Should.ThrowAsync<OperationCanceledException>(
                () => _runner.RunAsync(Request(arguments), cancellation.Token));
        }

        private static int CountLines(string output, string marker) =>
            output.Split('\n').Count(line => line.Contains(marker));
    }
}
