using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class JUnitCheckerTests : IDisposable
    {
        private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
        private readonly EvaluationOptions _options;
        private readonly string _workingDirectory;
        private readonly string _jarPath;

        public JUnitCheckerTests()
        {
            _workingDirectory = Path.Combine(Path.GetTempPath(), "soopworkshop-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_workingDirectory);

            // Der Checker prüft nur, ob die Datei existiert - der Inhalt ist ihm
            // egal, weil der Prozessaufruf gemockt ist.
            _jarPath = Path.Combine(_workingDirectory, "junit.jar");
            File.WriteAllText(_jarPath, "kein echtes JAR");

            _options = new EvaluationOptions
            {
                CompileTimeoutSeconds = 30,
                JUnitRunTimeoutSeconds = 30,
                JUnitJarPath = _jarPath
            };
        }

        public void Dispose()
        {
            if (Directory.Exists(_workingDirectory))
                Directory.Delete(_workingDirectory, recursive: true);

            GC.SuppressFinalize(this);
        }

        private JUnitChecker CreateChecker() =>
            new(_processRunner, Options.Create(_options), NullLogger<JUnitChecker>.Instance);

        private static TaskUnitTestFile TestFile(string fileName = "MainTest.java", string content = "class MainTest { }") =>
            new() { Id = Guid.NewGuid(), FileName = fileName, Content = content };

        private EvaluationContext Context(
            EvaluationMode mode = EvaluationMode.UnitTestOnly,
            bool compiled = true,
            params TaskUnitTestFile[] testFiles)
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                EvaluationMode = mode,
                UnitTestFiles = testFiles.Length == 0 ? [TestFile()] : testFiles
            };

            return EvaluationContextFactory.For(
                task: task,
                workingDirectory: _workingDirectory,
                compilation: new CompilationResult
                {
                    Success = compiled,
                    WorkingDirectory = _workingDirectory,
                    MainClassName = compiled ? "Main" : null
                });
        }

        private void JavacReturns(ProcessResult result) =>
            _processRunner.RunAsync(Arg.Is<ProcessRequest>(r => r.FileName == "javac"), Arg.Any<CancellationToken>())
                .Returns(result);

        // Der echte Launcher schreibt den Report als Datei - deshalb legt die
        // Attrappe ihn genauso ab, sonst prüft der Test am Verfahren vorbei.
        private void JavaWritesReport(string reportXml, ProcessResult? result = null) =>
            _processRunner.RunAsync(Arg.Is<ProcessRequest>(r => r.FileName == "java"), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var reportsDirectory = Path.Combine(_workingDirectory, "junit-reports");
                    Directory.CreateDirectory(reportsDirectory);
                    File.WriteAllText(Path.Combine(reportsDirectory, "TEST-junit-jupiter.xml"), reportXml);

                    return Task.FromResult(result ?? ProcessResultFactory.Success());
                });

        private void JavaReturns(ProcessResult result) =>
            _processRunner.RunAsync(Arg.Is<ProcessRequest>(r => r.FileName == "java"), Arg.Any<CancellationToken>())
                .Returns(result);

        private static string Report(params (string Name, bool Passed)[] testCases)
        {
            var entries = testCases.Select(testCase => testCase.Passed
                ? $"""
                   <testcase name="{testCase.Name}()" classname="MainTest" time="0.01">
                     <system-out><![CDATA[
                   display-name: JUnit Jupiter > MainTest > {testCase.Name}
                   ]]></system-out>
                   </testcase>
                   """
                : $"""
                   <testcase name="{testCase.Name}()" classname="MainTest" time="0.01">
                     <failure message="expected: &lt;5&gt; but was: &lt;-1&gt;" type="org.opentest4j.AssertionFailedError" />
                     <system-out><![CDATA[
                   display-name: JUnit Jupiter > MainTest > {testCase.Name}
                   ]]></system-out>
                   </testcase>
                   """);

            return $"""
                   <?xml version="1.0" encoding="UTF-8"?>
                   <testsuite name="JUnit Jupiter" tests="{testCases.Length}">
                   {string.Join("\n", entries)}
                   </testsuite>
                   """;
        }

        [Theory]
        [InlineData(EvaluationMode.ConsoleOnly, false)]
        [InlineData(EvaluationMode.UnitTestOnly, true)]
        [InlineData(EvaluationMode.Both, true)]
        public void IsApplicable_RichtetSichNachDemModus(EvaluationMode mode, bool expected)
        {
            CreateChecker().IsApplicable(Context(mode)).ShouldBe(expected);
        }

        // Ein falsch gesetzter Modus darf nicht still zu einer milderen Bewertung
        // führen - hier ist lautes Scheitern richtig.
        [Fact]
        public async Task CheckAsync_ModusVerlangtUnitTestsAberKeineDatei_Wirft()
        {
            var task = new TaskItem { Id = Guid.NewGuid(), EvaluationMode = EvaluationMode.UnitTestOnly };
            var context = EvaluationContextFactory.For(task: task, workingDirectory: _workingDirectory);

            var exception = await Should.ThrowAsync<InvalidOperationException>(
                () => CreateChecker().CheckAsync(context, CancellationToken.None));

            exception.Message.ShouldContain("keine JUnit-Datei");
        }

        [Fact]
        public async Task CheckAsync_JarFehlt_WirftMitPfadangabe()
        {
            _options.JUnitJarPath = Path.Combine(_workingDirectory, "gibtesnicht.jar");

            var exception = await Should.ThrowAsync<InvalidOperationException>(
                () => CreateChecker().CheckAsync(Context(), CancellationToken.None));

            exception.Message.ShouldContain("gibtesnicht.jar");
        }

        // Die Kategorie fällt bewusst nicht weg, sonst würde ihr Gewicht
        // umverteilt und kaputter Code besser bewertet.
        [Fact]
        public async Task CheckAsync_AbgabeKompiliertNicht_MeldetNichtBestandenOhneProzess()
        {
            var outcome = await CreateChecker().CheckAsync(
                Context(compiled: false),
                CancellationToken.None);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();

            await _processRunner.DidNotReceive().RunAsync(Arg.Any<ProcessRequest>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CheckAsync_AlleTestsBestanden_LiefertDieAnzeigenamen()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaWritesReport(Report(("addiere ergibt 5", true), ("main gibt die Summe aus", true)));

            var outcome = await CreateChecker().CheckAsync(Context(), CancellationToken.None);

            outcome.Results.Count.ShouldBe(2);
            outcome.Results.ShouldAllBe(result => result.Passed);
            outcome.Results.Select(result => result.Description)
                .ShouldBe(["addiere ergibt 5", "main gibt die Summe aus"]);
            outcome.ErrorTip.ShouldBeNull();
        }

        // Ein fehlgeschlagener Unit-Test muss dieselben Felder füllen wie ein
        // fehlgeschlagener Konsolen-Testfall - sonst zeigt die Ergebnisseite bei
        // ihm gar keinen Grund an.
        [Fact]
        public async Task CheckAsync_EinTestFehlgeschlagen_FuelltErwartetUndErhalten()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaWritesReport(Report(("addiere ergibt 5", false), ("main gibt die Summe aus", true)));

            var outcome = await CreateChecker().CheckAsync(Context(), CancellationToken.None);

            var failed = outcome.Results.Single(result => !result.Passed);
            failed.ExpectedOutput.ShouldBe("5");
            failed.ActualOutput.ShouldBe("-1");
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();
        }

        // Ohne Vergleich gehört die ganze Meldung unter "Erhalten" - im
        // Anzeigenamen wäre ein Stacktrace-Fetzen unlesbar.
        [Fact]
        public async Task CheckAsync_FehlerOhneVergleich_ZeigtDieMeldungAlsErhalten()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaWritesReport("""
                <testsuite name="JUnit Jupiter" tests="1">
                  <testcase name="wirft()" classname="MainTest" time="0">
                    <failure message="java.lang.NullPointerException" type="java.lang.NullPointerException" />
                    <system-out><![CDATA[
                display-name: JUnit Jupiter > MainTest > Das Programm laeuft ohne Absturz
                ]]></system-out>
                  </testcase>
                </testsuite>
                """);

            var failed = (await CreateChecker().CheckAsync(Context(), CancellationToken.None))
                .Results.ShouldHaveSingleItem();

            failed.Description.ShouldBe("Das Programm laeuft ohne Absturz");
            failed.ActualOutput.ShouldBe("java.lang.NullPointerException");
            failed.ExpectedOutput.ShouldBeEmpty();
        }

        // Eine eigene Meldung des Admins ergänzt den Anzeigenamen, statt die
        // Werte zu verdrängen.
        [Fact]
        public async Task CheckAsync_EigeneMeldung_ErgaenztDenAnzeigenamen()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaWritesReport("""
                <testsuite name="JUnit Jupiter" tests="1">
                  <testcase name="rechnet()" classname="MainTest" time="0">
                    <failure message="Bei negativen Zahlen ==> expected: &lt;-1&gt; but was: &lt;5&gt;" type="org.opentest4j.AssertionFailedError" />
                    <system-out><![CDATA[
                display-name: JUnit Jupiter > MainTest > addiere rechnet richtig
                ]]></system-out>
                  </testcase>
                </testsuite>
                """);

            var failed = (await CreateChecker().CheckAsync(Context(), CancellationToken.None))
                .Results.ShouldHaveSingleItem();

            failed.Description.ShouldBe("addiere rechnet richtig (Bei negativen Zahlen)");
            failed.ExpectedOutput.ShouldBe("-1");
            failed.ActualOutput.ShouldBe("5");
        }

        // Der Kern des Teilnehmer-Feedbacks: "cannot find symbol" beantwortet die
        // Frage nicht, wie die Methode heißen soll.
        [Fact]
        public async Task CheckAsync_TestdateiKompiliertNicht_ErklaertDieErwarteteSignatur()
        {
            JavacReturns(ProcessResultFactory.Failure("""
                MainTest.java:47: error: cannot find symbol
                        assertEquals(5, Main.addiere(2, 3));
                  symbol:   method addiere(int,int)
                  location: class Main
                """));

            var outcome = await CreateChecker().CheckAsync(Context(), CancellationToken.None);

            outcome.ErrorTip.ShouldNotBeNull();
            outcome.ErrorTip.ShouldContain("addiere(int,int)");

            // Rohausgabe bleibt erhalten - die Zeilennummer ist oft der schnellste Weg.
            outcome.Results.ShouldHaveSingleItem().ActualOutput.ShouldContain("MainTest.java:47");
        }

        [Fact]
        public async Task CheckAsync_KeinReport_ErklaertDenAbbruchDurchSystemExit()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaReturns(ProcessResultFactory.Success());

            var outcome = await CreateChecker().CheckAsync(Context(), CancellationToken.None);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
            outcome.ErrorTip.ShouldNotBeNull().ShouldContain("System.exit");
        }

        [Fact]
        public async Task CheckAsync_Zeitueberschreitung_NenntDieZeitgrenze()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaReturns(ProcessResultFactory.TimedOut());

            var outcome = await CreateChecker().CheckAsync(Context(), CancellationToken.None);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
            outcome.ErrorTip.ShouldNotBeNull().ShouldContain("30 Sekunden");
        }

        // Ohne diese Angaben schreibt die JVM unter Windows in Cp1252, während
        // der ProcessRunner UTF-8 erwartet.
        [Fact]
        public async Task CheckAsync_StartetDenLauncherMitUtf8UndExpliziterKlassenauswahl()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaWritesReport(Report(("egal", true)));

            await CreateChecker().CheckAsync(
                Context(EvaluationMode.UnitTestOnly, true, TestFile("RechnerTest.java")),
                CancellationToken.None);

            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRequest>(r => r.FileName == "java"
                                            && r.Arguments.Contains("-Dstdout.encoding=UTF-8")
                                            && r.Arguments.Contains("-Dstderr.encoding=UTF-8")
                                            && r.Arguments.Contains("--select-class")
                                            && r.Arguments.Contains("RechnerTest")),
                Arg.Any<CancellationToken>());
        }

        // Path.PathSeparator statt ';' - unter Linux trennt ':', und im Betrieb
        // läuft die Auswertung in einem Linux-Container.
        [Fact]
        public async Task CheckAsync_KompiliertMitJarImClasspathUndPlattformTrenner()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaWritesReport(Report(("egal", true)));

            await CreateChecker().CheckAsync(Context(), CancellationToken.None);

            var expectedClassPath = $"{_jarPath}{Path.PathSeparator}.";

            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRequest>(r => r.FileName == "javac"
                                            && r.Arguments.Contains(expectedClassPath)
                                            && r.Arguments.Contains("MainTest.java")),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CheckAsync_SchreibtDieTestdateiInsArbeitsverzeichnis()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaWritesReport(Report(("egal", true)));

            await CreateChecker().CheckAsync(
                Context(EvaluationMode.Both, true, TestFile("RechnerTest.java", "class RechnerTest { }")),
                CancellationToken.None);

            File.Exists(Path.Combine(_workingDirectory, "RechnerTest.java")).ShouldBeTrue();
        }

        // Zweite Verteidigungslinie wie beim Upload: ein Dateiname mit
        // Pfadanteilen darf das Arbeitsverzeichnis nicht verlassen.
        [Fact]
        public async Task CheckAsync_DateinameEnthaeltPfad_SchreibtNurInsArbeitsverzeichnis()
        {
            JavacReturns(ProcessResultFactory.Success());
            JavaWritesReport(Report(("egal", true)));

            await CreateChecker().CheckAsync(
                Context(EvaluationMode.UnitTestOnly, true, TestFile("../MainTest.java")),
                CancellationToken.None);

            File.Exists(Path.Combine(_workingDirectory, "MainTest.java")).ShouldBeTrue();
        }
    }
}
