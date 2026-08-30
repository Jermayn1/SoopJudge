using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Führt das kompilierte Programm für jeden Konsolen-Testfall aus und
    // vergleicht die Ausgabe mit der erwarteten.
    public class TestCaseChecker : IEvaluationChecker
    {
        private readonly IProcessRunner _processRunner;
        private readonly EvaluationOptions _options;

        public TestCaseChecker(IProcessRunner processRunner, IOptions<EvaluationOptions> options)
        {
            _processRunner = processRunner;
            _options = options.Value;
        }

        // Konsolen-Testfälle und JUnit-Tests beantworten dieselbe Frage - tut das
        // Programm, was die Aufgabe verlangt. Sie zahlen deshalb auf dieselbe
        // Kategorie ein und unterscheiden sich nur im Aufwand für den Admin.
        public EvaluationCategory Category => EvaluationCategory.Functionality;

        public int Order => EvaluationCheckerOrder.TestCases;

        // Ohne hinterlegte Testfälle gibt es nichts zu prüfen. Nutzt die Aufgabe
        // zusätzlich JUnit, trägt der JUnitChecker die Kategorie; sonst fällt sie
        // aus der Wertung und ihr Gewicht verteilt sich auf die übrigen. Früher
        // gab es hier stattdessen die volle Punktzahl geschenkt.
        public bool IsApplicable(EvaluationContext context) =>
            context.Task.EvaluationMode is EvaluationMode.ConsoleOnly or EvaluationMode.Both
            && context.Task.Tests.Count > 0;

        public async Task<CheckerOutcome> CheckAsync(EvaluationContext context, CancellationToken cancellationToken)
        {
            var tests = context.Task.Tests.OrderBy(test => test.Order).ToList();
            var compilation = context.Compilation;

            // Kompiliert die Abgabe nicht, gelten alle Testfälle als nicht
            // bestanden. Die Kategorie fällt bewusst nicht weg - sonst würde
            // ihr Gewicht umverteilt und kaputter Code besser bewertet.
            if (compilation is null || !compilation.Success || compilation.MainClassName is null)
            {
                var failed = tests.Select(test => new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = test.Description,
                    Input = test.Input,
                    ExpectedOutput = test.ExpectedOutput,
                    ActualOutput = string.Empty,
                    Passed = false
                }).ToArray();

                return CheckerOutcome.WithTip(
                    "Da der Code nicht kompiliert, konnten keine Testfälle ausgeführt werden.",
                    failed);
            }

            var results = new List<TestCaseResult>();

            foreach (var test in tests)
            {
                var actualOutput = await RunProgramAsync(compilation, test.Input, cancellationToken);
                var passed = NormalizeOutput(actualOutput) == NormalizeOutput(test.ExpectedOutput);

                results.Add(new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = test.Description,
                    Input = test.Input,
                    ExpectedOutput = test.ExpectedOutput,

                    // Erst vergleichen, dann beschriften: gibt das Programm gar
                    // nichts aus, stünde in der Anzeige sonst nur "Erwartet" und
                    // darunter nichts - der Teilnehmer sieht dann nicht, ob die
                    // Ausgabe fehlte oder die Anzeige kaputt ist.
                    ActualOutput = string.IsNullOrWhiteSpace(actualOutput)
                        ? "(keine Ausgabe)"
                        : actualOutput,

                    Passed = passed
                });
            }

            return results.All(result => result.Passed)
                ? CheckerOutcome.Of([.. results])
                : CheckerOutcome.WithTip(EvaluationMessages.ComparisonHint, [.. results]);
        }

        // Entfernt führende und abschließende Leerzeichen und vereinheitlicht Zeilenumbrüche,
        // damit kleine Formatierungsunterschiede nicht zu Fehlern führen.
        private static string NormalizeOutput(string output) =>
            output.Replace("\r\n", "\n").Trim();

        private async Task<string> RunProgramAsync(
            CompilationResult compilation,
            string input,
            CancellationToken cancellationToken)
        {
            var process = await _processRunner.RunAsync(
                new ProcessRequest(
                    "java",
                    // Ohne diese beiden Angaben schreibt die JVM unter Windows in der
                    // Codepage des Systems (Cp1252) — Umlaute in der Programmausgabe
                    // kämen dann zerlegt beim Teilnehmer an.
                    ["-Dstdout.encoding=UTF-8", "-Dstderr.encoding=UTF-8", compilation.MainClassName!],
                    compilation.WorkingDirectory,
                    input,
                    TimeSpan.FromSeconds(_options.RunTimeoutSeconds)),
                cancellationToken);

            if (process.ExecutableNotFound)
                return "„java“ wurde nicht gefunden. Ist das JDK installiert und im PATH?";

            if (process.TimedOut)
                return $"Zeitüberschreitung: Das Programm hat länger als {_options.RunTimeoutSeconds} Sekunden gebraucht. " +
                       "Prüfe, ob eine Schleife nie endet oder auf eine Eingabe gewartet wird, die es nicht gibt.";

            // Bricht das Programm ab, ohne etwas auszugeben, ist der Stacktrace die
            // einzige Information, die dem Teilnehmer weiterhilft.
            if (string.IsNullOrWhiteSpace(process.StandardOutput) && !string.IsNullOrWhiteSpace(process.StandardError))
                return process.StandardError;

            return process.StandardOutput;
        }
    }
}
