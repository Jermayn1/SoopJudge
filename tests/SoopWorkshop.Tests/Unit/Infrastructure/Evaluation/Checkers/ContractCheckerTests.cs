using SoopWorkshop.Backend.Application.Evaluation.Models;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class ContractCheckerTests
    {
        private readonly ContractChecker _checker = new();

        private static TaskExpectedType Type(string name, params string[] methodNames) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Order = 1,
            Methods = [.. methodNames.Select((methodName, index) => new TaskExpectedMethod
            {
                Id = Guid.NewGuid(),
                Signature = $"public static int {methodName}(int a, int b)",
                Name = methodName,
                Order = index + 1
            })]
        };

        private static TaskItem Task(params TaskExpectedType[] types)
        {
            var item = new TaskItem { Id = Guid.NewGuid() };

            var order = 1;
            foreach (var type in types)
            {
                type.Order = order++;
                item.ExpectedTypes.Add(type);
            }

            return item;
        }

        private Task<CheckerOutcome> CheckAsync(TaskItem task, string code) =>
            _checker.CheckAsync(
                EvaluationContextFactory.For(task: task, files: [SubmissionFileFactory.Create(code)]),
                CancellationToken.None);

        [Fact]
        public void IsApplicable_OhneVorgaben_NichtAnwendbar()
        {
            _checker.IsApplicable(EvaluationContextFactory.For(task: Task())).ShouldBeFalse();
        }

        [Fact]
        public void IsApplicable_MitGeforderterKlasse_IstAnwendbar()
        {
            _checker.IsApplicable(EvaluationContextFactory.For(task: Task(Type("Main")))).ShouldBeTrue();
        }

        // Der Fall, wegen dem es den Checker gibt: Java erzwingt nur, dass
        // Dateiname und Klassenname zusammenpassen - nicht, dass sie heißen wie
        // die Aufgabe verlangt.
        [Fact]
        public async Task CheckAsync_FalscherKlassenname_FaelltDurchUndNenntBeideNamen()
        {
            var outcome = await CheckAsync(Task(Type("Main")), "public class Rechner { }");

            var result = outcome.Results.ShouldHaveSingleItem();
            result.Passed.ShouldBeFalse();
            result.Description.ShouldBe("Die Klasse „Main“ ist vorhanden");
            result.ExpectedOutput.ShouldBe("Main");
            result.ActualOutput.ShouldBe("Rechner");
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task CheckAsync_RichtigerKlassenname_Besteht()
        {
            var outcome = await CheckAsync(Task(Type("Main")), "public class Main { }");

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
            outcome.ErrorTip.ShouldBeNull();
        }

        [Theory]
        [InlineData("public interface Zaehler { }")]
        [InlineData("public enum Zaehler { EINS }")]
        [InlineData("public record Zaehler(int wert) { }")]
        public async Task CheckAsync_AndereBauform_ZaehltEbenfalls(string code)
        {
            var outcome = await CheckAsync(Task(Type("Zaehler")), code);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
        }

        // Groß-/Kleinschreibung ist in Java bedeutsam - 'main' ist nicht 'Main'.
        [Fact]
        public async Task CheckAsync_KlassennameNurAndersGeschrieben_FaelltDurch()
        {
            var outcome = await CheckAsync(Task(Type("Main")), "public class main { }");

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
        }

        [Fact]
        public async Task CheckAsync_FehlendeMethode_NenntDieErwarteteSignatur()
        {
            var outcome = await CheckAsync(
                Task(Type("Main", "addiere")),
                "public class Main { public static int addieren(int a, int b) { return a + b; } }");

            var method = outcome.Results.Single(result => result.Description.Contains("Methode"));
            method.Passed.ShouldBeFalse();

            // Der Name in der Überschrift, die vollständige Signatur daneben -
            // sonst sprengt sie die Zeile und lässt sich nicht vergleichen.
            method.Description.ShouldBe("Die Methode „addiere“ steht in „Main“");
            method.ExpectedOutput.ShouldBe("public static int addiere(int a, int b)");
            method.ActualOutput.ShouldBe("in dieser Klasse nicht gefunden");

            outcome.ErrorTip.ShouldNotBeNull().ShouldContain("public static int addiere(int a, int b)");
        }

        [Fact]
        public async Task CheckAsync_VorhandeneMethode_Besteht()
        {
            var outcome = await CheckAsync(
                Task(Type("Main", "addiere")),
                "public class Main { public static int addiere(int a, int b) { return a + b; } }");

            outcome.Results.ShouldAllBe(result => result.Passed);
            outcome.ErrorTip.ShouldBeNull();
        }

        [Fact]
        public async Task CheckAsync_MehrereMethoden_LiefertJeEineTeilpruefung()
        {
            var outcome = await CheckAsync(
                Task(Type("Main", "addiere", "subtrahiere")),
                "public class Main { public static int addiere(int a, int b) { return a + b; } }");

            // Klasse plus zwei Methoden.
            outcome.Results.Count.ShouldBe(3);
            outcome.Results.Count(result => result.Passed).ShouldBe(2);
        }

        // ── Mehrere Klassen ────────────────────────────────────────────────

        [Fact]
        public async Task CheckAsync_MehrereGeforderteKlassenAlleDa_Besteht()
        {
            var outcome = await CheckAsync(
                Task(Type("Konto", "einzahlen"), Type("Kunde", "getName")),
                """
                public class Konto {
                    public void einzahlen(double betrag) { }
                }
                public class Kunde {
                    public String getName() { return ""; }
                }
                """);

            outcome.Results.Count.ShouldBe(4);
            outcome.Results.ShouldAllBe(result => result.Passed);
            outcome.ErrorTip.ShouldBeNull();
        }

        // Der eigentliche Grund für den Umbau: vorher wurde im gesamten
        // Quelltext gesucht, 'einzahlen' zählte also auch dann als vorhanden,
        // wenn es in der falschen Klasse stand.
        [Fact]
        public async Task CheckAsync_MethodeInDerFalschenKlasse_FaelltDurch()
        {
            var outcome = await CheckAsync(
                Task(Type("Konto", "einzahlen"), Type("Kunde")),
                """
                public class Konto {
                }
                public class Kunde {
                    public void einzahlen(double betrag) { }
                }
                """);

            var method = outcome.Results.Single(result => result.Description.Contains("Methode"));
            method.Passed.ShouldBeFalse();
            method.Description.ShouldBe("Die Methode „einzahlen“ steht in „Konto“");
            method.ActualOutput.ShouldBe("in dieser Klasse nicht gefunden");

            // Beide Klassen selbst sind da.
            outcome.Results.Where(result => result.Description.Contains("Klasse"))
                .ShouldAllBe(result => result.Passed);
        }

        // Fehlt die Klasse, wird ihre Methode trotzdem als eigene Teilprüfung
        // gezeigt - eine verschwiegene Prüfung wäre eine stillschweigend
        // mildere Bewertung.
        [Fact]
        public async Task CheckAsync_KlasseFehlt_MeldetAuchIhreMethodenAlsNichtBestanden()
        {
            var outcome = await CheckAsync(
                Task(Type("Konto", "einzahlen")),
                "public class Kunde { public void einzahlen(double betrag) { } }");

            outcome.Results.Count.ShouldBe(2);
            outcome.Results.ShouldAllBe(result => !result.Passed);

            var method = outcome.Results.Single(result => result.Description.Contains("Methode"));
            method.ActualOutput.ShouldBe("Klasse „Konto“ fehlt");
        }

        // Ist-Verhalten, bewusst hingenommen: der Rumpf wird über Klammern
        // abgegrenzt, eine innere Klasse liegt damit im Rumpf der äußeren und
        // ihre Methoden zählen auch für diese. Innere Klassen kommen im
        // Workshop nicht vor, und die genaue Zugehörigkeit prüft die
        // JUnit-Kompilierung ohnehin exakt.
        [Fact]
        public async Task CheckAsync_MethodeInInnererKlasse_ZaehltAuchFuerDieAeussere()
        {
            var outcome = await CheckAsync(
                Task(Type("Konto", "einzahlen")),
                """
                public class Konto {
                    class Buchung {
                        void einzahlen(double betrag) { }
                    }
                }
                """);

            outcome.Results.ShouldAllBe(result => result.Passed);
        }

        // Kommentare und Zeichenketten sind kein Code - eine dort erwähnte
        // Klasse ist nicht deklariert.
        [Fact]
        public async Task CheckAsync_KlassennameNurImKommentar_FaelltDurch()
        {
            var outcome = await CheckAsync(
                Task(Type("Main")),
                """
                // class Main wäre hier richtig gewesen
                public class Rechner {
                    String hinweis = "class Main";
                }
                """);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
        }

        // Ist-Verhalten, bewusst hingenommen: geprüft wird die Anwesenheit des
        // Namens vor einer Klammer, nicht die vollständige Deklaration. Ein
        // bloßer Aufruf zählt deshalb bereits als Treffer. Die exakte Signatur
        // prüft ohnehin der Compiler beim Übersetzen der JUnit-Datei.
        [Fact]
        public async Task CheckAsync_MethodeNurAufgerufen_GiltBereitsAlsVorhanden()
        {
            var outcome = await CheckAsync(
                Task(Type("Main", "addiere")),
                "public class Main { void run() { addiere(1, 2); } }");

            outcome.Results.ShouldAllBe(result => result.Passed);
        }

        // Ein Aufruf auf einem Objekt zählt dagegen nicht - sonst würde jede
        // Nutzung einer fremden Bibliothek als eigene Methode durchgehen.
        [Fact]
        public async Task CheckAsync_MethodenaufrufAufFremdemObjekt_ZaehltNicht()
        {
            var outcome = await CheckAsync(
                Task(Type("Main", "addiere")),
                "public class Main { void run() { rechner.addiere(1, 2); } }");

            outcome.Results.Single(result => result.Description.Contains("Methode"))
                .Passed.ShouldBeFalse();
        }

        [Fact]
        public void Category_IstKompilierbarkeit()
        {
            _checker.Category.ShouldBe(EvaluationCategory.Compilability);
        }
    }
}
