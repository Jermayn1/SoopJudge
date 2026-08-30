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
        public async Task CheckAsync_VerbotenesZeichen_GiltAlsNichtBestanden(string character)
        {
            var files = SubmissionFileFactory.CreateMany(
                $$"""
                public class Main {
                    public static void main(String[] args) {
                        System.out.println("{{character}}");
                    }
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();
        }

        // Ausdrücklich gewollt: geprüft wird die ROHE Datei. Anders als
        // ContractChecker und NamingConventionChecker schickt der
        // CharacterSetChecker den Quelltext NICHT durch StripCommentsAndLiterals -
        // ein Umlaut im Kommentar kostet denselben Punkt wie einer im Bezeichner.
        //
        // Der Grund: die Regel existiert wegen der Kodierungsfallen der
        // Java-Konsole, und die treffen gerade Kommentare und Ausgabetexte. Der
        // Fall im String-Literal steht in der Theory oben.
        [Fact]
        public async Task CheckAsync_UmlautNurImKommentar_GiltAlsNichtBestanden()
        {
            var files = SubmissionFileFactory.CreateMany(
                """
                public class Main {
                    // Größe berechnen - hier steht ein Umlaut: ä
                    public static void main(String[] args) { }
                }
                """);

            var outcome = await CheckAsync(files);

            outcome.Results.ShouldHaveSingleItem().Passed.ShouldBeFalse();
            outcome.ErrorTip.ShouldNotBeNullOrEmpty();
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
                "public class Helper { String text = \"Grüße\"; }");

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
