using Microsoft.Extensions.Options;
using NSubstitute;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class TestCaseCheckerTests
    {
        private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
        private readonly EvaluationOptions _options = new() { RunTimeoutSeconds = 10 };

        private TestCaseChecker CreateChecker() => new(_processRunner, Options.Create(_options));

        private static CompilationResult Compiled(string mainClassName = "Main") =>
            new() { Success = true, MainClassName = mainClassName, WorkingDirectory = "/tmp/egal" };

        private static TaskTest Test(string expectedOutput, string input = "", string description = "Testfall", int order = 0) =>
            new()
            {
                Id = Guid.NewGuid(),
                Input = input,
                ExpectedOutput = expectedOutput,
                Description = description,
                Order = order
            };

        // Kontext mit kompilierter Abgabe und den übergebenen Testfällen.
        private static EvaluationContext Context(CompilationResult compilation, params TaskTest[] tests) =>
            EvaluationContextFactory.For(
                task: EvaluationContextFactory.TaskWithTests(tests),
                compilation: compilation);

        private void ProgramReturns(ProcessResult result) =>
            _processRunner.RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>()).Returns(result);

        [Fact]
        public async Task CheckAsync_AusgabeStimmtUeberein_MeldetTeilpruefungAlsBestanden()
        {
            ProgramReturns(ProcessResultFactory.Success("Hallo Soop"));

            var outcome = await CreateChecker().CheckAsync(Context(Compiled(), Test("Hallo Soop")), CancellationToken.None);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
            outcome.ErrorTip.ShouldBeNull();
        }

        // Zeilenumbrüche und umgebende Leerzeichen sollen keinen Unterschied machen.
        [Theory]
        [InlineData("Hallo\r\nSoop", "Hallo\nSoop")]
        [InlineData("  Hallo Soop  ", "Hallo Soop")]
        [InlineData("Hallo Soop\n", "Hallo Soop")]
        public async Task CheckAsync_AusgabeWeichtNurInFormatierungAb_GiltAlsBestanden(string actual, string expected)
        {
            ProgramReturns(ProcessResultFactory.Success(actual));

            var outcome = await CreateChecker().CheckAsync(Context(Compiled(), Test(expected)), CancellationToken.None);

            outcome.Results.ShouldAllBe(result => result.Passed);
        }

        // Früher lieferte eine Zeitüberschreitung eine leere Ausgabe — der Teilnehmer
        // sah nur einen roten Testfall ohne Erklärung.
        [Fact]
        public async Task CheckAsync_ProgrammLaeuftZuLange_ErklaertDieZeitueberschreitung()
        {
            ProgramReturns(ProcessResultFactory.TimedOut());

            var outcome = await CreateChecker().CheckAsync(Context(Compiled(), Test("Hallo Soop")), CancellationToken.None);

            var testCase = outcome.Results.ShouldHaveSingleItem();
            testCase.Passed.ShouldBeFalse();
            testCase.ActualOutput.ShouldContain("Zeitüberschreitung");
            testCase.ActualOutput.ShouldContain("10 Sekunden");
        }

        [Fact]
        public async Task CheckAsync_JavaFehlt_WeistAufDasFehlendeJdkHin()
        {
            ProgramReturns(ProcessResultFactory.NotFound());

            var outcome = await CreateChecker().CheckAsync(Context(Compiled(), Test("Hallo Soop")), CancellationToken.None);

            outcome.Results.ShouldHaveSingleItem().ActualOutput.ShouldContain("JDK");
        }

        // Ein Laufzeitfehler ohne vorherige Ausgabe war bisher unsichtbar.
        [Fact]
        public async Task CheckAsync_ProgrammBrichtOhneAusgabeAb_ZeigtDieFehlerausgabe()
        {
            ProgramReturns(ProcessResultFactory.Failure(
                standardError: "Exception in thread \"main\" java.lang.NullPointerException"));

            var outcome = await CreateChecker().CheckAsync(Context(Compiled(), Test("Hallo Soop")), CancellationToken.None);

            outcome.Results.ShouldHaveSingleItem().ActualOutput.ShouldContain("NullPointerException");
        }

        // Gibt das Programm etwas aus, zählt allein die Standardausgabe — sonst
        // würde eine Warnung auf stderr jeden Vergleich zerstören.
        [Fact]
        public async Task CheckAsync_ProgrammSchreibtAufBeideStroeme_VergleichtNurDieStandardausgabe()
        {
            ProgramReturns(ProcessResultFactory.Success("Hallo Soop", standardError: "Note: irgendeine Warnung"));

            var outcome = await CreateChecker().CheckAsync(Context(Compiled(), Test("Hallo Soop")), CancellationToken.None);

            outcome.Results.ShouldAllBe(result => result.Passed);
        }

        // Die Kategorie fällt bei einem Kompilierfehler bewusst nicht weg: sonst
        // würde ihr Gewicht umverteilt und kaputter Code besser bewertet.
        [Fact]
        public async Task CheckAsync_KompilierungFehlgeschlagen_MarkiertAlleTestfaelleAlsNichtBestanden()
        {
            var compilation = new CompilationResult { Success = false, MainClassName = null };

            var outcome = await CreateChecker().CheckAsync(
                Context(compilation, Test("A"), Test("B")),
                CancellationToken.None);

            outcome.Results.Count.ShouldBe(2);
            outcome.Results.ShouldAllBe(result => !result.Passed);
            outcome.ErrorTip.ShouldNotBeNull();

            await _processRunner.DidNotReceive().RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>());
        }

        // Ohne die Eingabe im Ergebnis steht in der Anzeige "erwartet 7,
        // erhalten 5", ohne dass jemand sieht, womit gerechnet wurde.
        [Fact]
        public async Task CheckAsync_MitEingabe_UebernimmtSieInsErgebnis()
        {
            ProgramReturns(ProcessResultFactory.Success("falsch"));

            var outcome = await CreateChecker().CheckAsync(
                Context(Compiled(), Test("7", input: "3\n4\n")),
                CancellationToken.None);

            outcome.Results.ShouldHaveSingleItem().Input.ShouldBe("3\n4\n");
        }

        [Fact]
        public async Task CheckAsync_MitEingabe_ReichtDieEingabeAnDenProzessDurch()
        {
            ProgramReturns(ProcessResultFactory.Success("7"));

            await CreateChecker().CheckAsync(Context(Compiled(), Test("7", input: "3\n4\n")), CancellationToken.None);

            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRequest>(r => r.FileName == "java"
                                            && r.Arguments.Contains("Main")
                                            && r.StandardInput == "3\n4\n"),
                Arg.Any<CancellationToken>());
        }

        // Ohne diese Angaben schreibt die JVM unter Windows in Cp1252, während der
        // ProcessRunner UTF-8 erwartet — Umlaute kämen zerlegt beim Teilnehmer an.
        [Fact]
        public async Task CheckAsync_StartetJavaMitUtf8Ausgabe()
        {
            ProgramReturns(ProcessResultFactory.Success("Hallo Soop"));

            await CreateChecker().CheckAsync(Context(Compiled(), Test("Hallo Soop")), CancellationToken.None);

            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRequest>(r => r.Arguments.Contains("-Dstdout.encoding=UTF-8")
                                            && r.Arguments.Contains("-Dstderr.encoding=UTF-8")),
                Arg.Any<CancellationToken>());
        }

        // Ohne Testfälle ist der Checker nicht anwendbar. Die Kategorie fällt
        // damit aus der Wertung und ihr Gewicht verteilt sich auf die übrigen -
        // statt die volle Punktzahl zu verschenken. Nachgewiesen in
        // EvaluationScorerTests.
        [Fact]
        public void IsApplicable_KeineTestfaelle_IstNichtAnwendbar()
        {
            var context = EvaluationContextFactory.For(task: EvaluationContextFactory.TaskWithTests());

            CreateChecker().IsApplicable(context).ShouldBeFalse();
        }

        [Fact]
        public void IsApplicable_MitTestfaellen_IstAnwendbar()
        {
            var context = Context(Compiled(), Test("A"));

            CreateChecker().IsApplicable(context).ShouldBeTrue();
        }

        // Ersetzt den früheren Test zur Ganzzahl-Division: der Checker zählt nur
        // noch bestandene Teilprüfungen, die Punkte rechnet der EvaluationScorer.
        [Fact]
        public async Task CheckAsync_ZweiVonDreiBestanden_MeldetZweiBestandeneTeilpruefungen()
        {
            _processRunner.RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>())
                .Returns(
                    ProcessResultFactory.Success("A"),
                    ProcessResultFactory.Success("B"),
                    ProcessResultFactory.Success("falsch"));

            var outcome = await CreateChecker().CheckAsync(
                Context(Compiled(), Test("A"), Test("B"), Test("C")),
                CancellationToken.None);

            outcome.Results.Count(result => result.Passed).ShouldBe(2);
            outcome.ErrorTip.ShouldNotBeNull();
        }

        [Fact]
        public async Task CheckAsync_TestfaelleLaufenInDerReihenfolgeVonOrder()
        {
            ProgramReturns(ProcessResultFactory.Success("egal"));

            var outcome = await CreateChecker().CheckAsync(
                Context(
                    Compiled(),
                    Test("A", description: "zweiter", order: 2),
                    Test("B", description: "erster", order: 1)),
                CancellationToken.None);

            outcome.Results.Select(result => result.Description).ShouldBe(["erster", "zweiter"]);
        }
    }
}
