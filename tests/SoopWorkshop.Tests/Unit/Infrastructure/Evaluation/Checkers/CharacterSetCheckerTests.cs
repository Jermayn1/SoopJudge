using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Infrastructure.Evaluation.Checkers
{
    public class CharacterSetCheckerTests
    {
        private readonly CharacterSetChecker _checker = new();

        private Task<Backend.Application.Evaluation.Models.CheckerOutcome> CheckAsync(IReadOnlyList<SubmissionFile> files) =>
            _checker.CheckAsync(EvaluationContextFactory.For(files: files), CancellationToken.None);

        // Ohne Dateien gibt es nichts zu beanstanden. Anders als beim TestCaseChecker
        // sind das keine Gratispunkte, denn ohne Code gibt es auch keinen Verstoß.
        [Fact]
        public async Task CheckAsync_OhneDateien_GiltAlsBestanden()
        {
            var outcome = await CheckAsync([]);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
        }

        [Fact]
        public async Task CheckAsync_CodeOhneUmlaute_GiltAlsBestanden()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Main {
                    public static void main(String[] args) {
                        System.out.println("Hallo Soop");
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
            outcome.ErrorTip.ShouldBeNull();
        }

        [Theory]
        [InlineData("ä")]
        [InlineData("ö")]
        [InlineData("ü")]
        [InlineData("Ä")]
        [InlineData("Ö")]
        [InlineData("Ü")]
        [InlineData("ß")]
        public async Task CheckAsync_VerbotenesZeichenImBezeichner_GiltAlsNichtBestanden(string character)
        {
            var files = SubmissionFileFactory.CreateMany(
                $$"""
                public class Main {
                    public static void main(String[] args) {
                        int wert{{character}} = 0;
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();
        }

        // Der Kern der Regel: verboten ist der Umlaut nur im Namen.
        //
        // Randfall des Bereinigens, bewusst ohne Test: ein nicht geschlossenes
        // Literal verschluckt den Rest der Datei. Das trifft
        // NamingConventionChecker und ContractChecker genauso, und eine solche
        // Abgabe kompiliert ohnehin nicht.
        [Fact]
        public async Task CheckAsync_UmlautNurInDerAusgabe_GiltAlsBestanden()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Main {
                    public static void main(String[] args) {
                        System.out.println("Größe in Metern: 5");
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
            outcome.ErrorTip.ShouldBeNull();
        }

        // Ein Textblock ist ein Literal wie jedes andere. Erkannt wird er nur,
        // weil StripCommentsAndLiterals die drei Anführungszeichen vor dem
        // einfachen String prüft.
        [Fact]
        public async Task CheckAsync_UmlautNurImTextblock_GiltAlsBestanden()
        {
            var files = SubmissionFileFactory.CreateMany(
                """"
                public class Main {
                    public static void main(String[] args) {
                        System.out.println("""
                            Größe in Metern
                            """);
                    }
                }
                """");

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
        }

        // Ein Kommentar sagt über die Benennung im Programm so wenig aus wie
        // eine Ausgabe. Der NamingConventionChecker nimmt ihn aus demselben
        // Grund seit jeher aus.
        [Fact]
        public async Task CheckAsync_UmlautNurImKommentar_GiltAlsBestanden()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Main {
                    // Größe berechnen - hier steht ein Umlaut: ä
                    public static void main(String[] args) { }
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
        }

        // Gegentest zu den beiden darüber: eine zu weit gefasste Bereinigung
        // würde den Namen mitnehmen und hier grün melden.
        [Fact]
        public async Task CheckAsync_UmlautImBezeichnerNebenAusgabe_GiltAlsNichtBestanden()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Main {
                    public static void main(String[] args) {
                        int größe = 5;
                        System.out.println("Größe in Metern: " + größe);
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();
        }

        // Nicht nur Variablen: Klassen- und Methodennamen zählen genauso.
        [Fact]
        public async Task CheckAsync_UmlautImKlassennamen_GiltAlsNichtBestanden()
        {
            var files = SubmissionFileFactory.CreateMany("public class Grüße { }");

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
        }

        // Ist-Verhalten: geprüft wird nur der Dateiinhalt, nicht der Dateiname.
        [Fact]
        public async Task CheckAsync_UmlautNurImDateinamen_GiltAlsBestanden()
        {
            var files = new List<SubmissionFile>
            {
                SubmissionFileFactory.Create("public class Gruesse { }", "Grüße.java")
            };

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeTrue();
        }

        [Fact]
        public async Task CheckAsync_UmlautInZweiterDatei_GiltAlsNichtBestanden()
        {
            var files = SubmissionFileFactory.CreateMany(
                "public class Main { }",
                "public class Helper { String grüße = \"Hallo\"; }");

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
        }

        // Zeichensatz ist seit der Bewertungs-Engine v2 eine Teilprüfung unter
        // Clean Code und keine eigene Kategorie mehr.
        [Fact]
        public void Category_IstCleanCode()
        {
            _checker.Category.ShouldBe(EvaluationCategory.CleanCode);
        }

        [Fact]
        public void IsApplicable_ImmerAnwendbar()
        {
            _checker.IsApplicable(EvaluationContextFactory.For()).ShouldBeTrue();
        }
    }
}
