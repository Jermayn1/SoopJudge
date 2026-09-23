using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Application.Evaluation;
using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Backend.Infrastructure.Processes;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    // Der ganze Weg mit echtem javac, echtem Launcher und dem JAR aus lib/.
    //
    // Die übrigen Tests des Checkers ersetzen den Prozess durch eine Attrappe.
    // Ob soopjudge.Werte zu JUnit passt, ob die Umleitung übersetzt und ob der
    // Launcher System.err wirklich in den Report legt, zeigt nur ein echter
    // Lauf. Braucht ein JDK im PATH, wie das Backend selbst.
    public class JUnitCheckerJdkTests : IDisposable
    {
        private readonly string _workingDirectory;
        private readonly ProcessRunner _processRunner = new(NullLogger<ProcessRunner>.Instance);

        public JUnitCheckerJdkTests()
        {
            _workingDirectory = Path.Combine(Path.GetTempPath(), "soopworkshop-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_workingDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_workingDirectory))
                Directory.Delete(_workingDirectory, recursive: true);

            GC.SuppressFinalize(this);
        }

        private const string Kasse = """
            public class Kasse {
                private double umsatz;

                public double bezahlen(double gegeben, double betrag) {
                    // Schreibt auch Zeichen, die XML nicht erlaubt - der Report
                    // muss trotzdem lesbar bleiben.
                    System.err.println("Kasse\u0001 zahlt");
                    umsatz += betrag;
                    return gegeben - betrag;
                }

                public long anzahl() { return 3L; }
                public boolean leer() { return umsatz == 0; }
                public void storno() { throw new IllegalStateException("leer"); }
            }
            """;

        private const string KasseTest = """
            import static org.junit.jupiter.api.Assertions.*;

            import org.junit.jupiter.api.DisplayName;
            import org.junit.jupiter.api.Test;

            class KasseTest {
                private final Kasse kasse = new Kasse();

                @Test
                @DisplayName("bezahlen liefert das Wechselgeld")
                void bezahlen() {
                    assertEquals(7.5, kasse.bezahlen(20, 12.5), 0.0001);
                    assertEquals(
                        3,
                        kasse.anzahl());
                    assertFalse(kasse.leer());
                }

                @Test
                @DisplayName("storno ohne Rechnung wirft")
                void storno() {
                    assertThrows(IllegalStateException.class, () -> kasse.storno());
                }

                @Test
                @DisplayName("bezahlen rechnet falsch")
                void falsch() {
                    assertEquals(1.0, kasse.bezahlen(10, 5), 0.0001, "Wechselgeld");
                }
            }
            """;

        private async Task<CheckerOutcome> RunAsync()
        {
            await File.WriteAllTextAsync(Path.Combine(_workingDirectory, "Kasse.java"), Kasse);
            var compiled = await _processRunner.RunAsync(
                new ProcessRequest("javac", ["-encoding", "UTF-8", "Kasse.java"], _workingDirectory, null, TimeSpan.FromSeconds(60)),
                CancellationToken.None);
            compiled.Success.ShouldBeTrue(compiled.StandardError);

            var options = new EvaluationOptions
            {
                CompileTimeoutSeconds = 60,
                JUnitRunTimeoutSeconds = 60,
                JUnitJarPath = Path.Combine("lib", "junit-platform-console-standalone-6.1.3.jar")
            };

            var checker = new JUnitChecker(_processRunner, Options.Create(options), NullLogger<JUnitChecker>.Instance);

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                EvaluationMode = EvaluationMode.UnitTestOnly,
                UnitTestFiles = [new TaskUnitTestFile { Id = Guid.NewGuid(), FileName = "KasseTest.java", Content = KasseTest }]
            };

            var context = EvaluationContextFactory.For(
                task: task,
                workingDirectory: _workingDirectory,
                compilation: new CompilationResult { Success = true, WorkingDirectory = _workingDirectory });

            return await checker.CheckAsync(context, CancellationToken.None);
        }

        [Fact]
        public async Task CheckAsync_BestandenePruefung_KenntAufrufErwartetUndErhalten()
        {
            var outcome = await RunAsync();

            var bezahlen = outcome.Results.Single(r => r.Description == "bezahlen liefert das Wechselgeld");
            bezahlen.Passed.ShouldBeTrue();

            var comparisons = bezahlen.Comparisons.OrderBy(c => c.Order).ToList();
            comparisons.Select(c => (c.Call, c.Expected, c.Actual, c.Passed)).ShouldBe(
            [
                ("kasse.bezahlen(20, 12.5)", "7.5", "7.5", true),
                ("kasse.anzahl()", "3", "3", true),
                ("kasse.leer()", "false", "false", true),
            ]);
        }

        [Fact]
        public async Task CheckAsync_AssertThrows_NenntDieAusnahme()
        {
            var outcome = await RunAsync();

            var storno = outcome.Results.Single(r => r.Description == "storno ohne Rechnung wirft");
            var comparison = storno.Comparisons.ShouldHaveSingleItem();
            comparison.Call.ShouldBe("kasse.storno()");
            comparison.Expected.ShouldBe("IllegalStateException");
            comparison.Actual.ShouldBe("IllegalStateException");
        }

        // Ein gescheiterter Vergleich steht ebenfalls da, als nicht bestanden -
        // und die Note bleibt dieselbe wie ohne Mitschreiben.
        [Fact]
        public async Task CheckAsync_GescheiterterVergleich_BleibtDurchgefallenUndKenntSeinenAufruf()
        {
            var outcome = await RunAsync();

            var falsch = outcome.Results.Single(r => r.Description.StartsWith("bezahlen rechnet falsch"));
            falsch.Passed.ShouldBeFalse();
            falsch.ExpectedOutput.ShouldBe("1.0");
            falsch.ActualOutput.ShouldBe("5.0");

            var comparison = falsch.Comparisons.ShouldHaveSingleItem();
            comparison.Call.ShouldBe("kasse.bezahlen(10, 5)");
            comparison.Passed.ShouldBeFalse();
        }
    }
}
