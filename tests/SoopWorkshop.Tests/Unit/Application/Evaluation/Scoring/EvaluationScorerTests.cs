using SoopWorkshop.Backend.Application.Evaluation.Scoring;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Unit.Application.Evaluation.Scoring
{
    public class EvaluationScorerTests
    {
        // Kategorie mit "passed von total" bestandenen Teilprüfungen.
        private static CategoryScoreInput Category(
            EvaluationCategory category,
            double weight,
            int passed,
            int total,
            string? errorTip = null)
        {
            var results = Enumerable.Range(0, total)
                .Select(index => new TestCaseResult
                {
                    Id = Guid.NewGuid(),
                    Description = $"Teilpruefung {index}",
                    Passed = index < passed
                })
                .ToList();

            return new CategoryScoreInput(category, weight, results, errorTip);
        }

        // Die Standardgewichte einer reinen Konsolenaufgabe.
        private static CategoryScoreInput CleanCode(int passed, int total = 3) =>
            Category(EvaluationCategory.CleanCode, 15, passed, total);

        private static CategoryScoreInput Compilability(bool passed) =>
            Category(EvaluationCategory.Compilability, 20, passed ? 1 : 0, 1);

        // Konsolen-Testfälle und JUnit-Tests zahlen beide hierauf ein.
        private static CategoryScoreInput Functionality(int passed, int total) =>
            Category(EvaluationCategory.Functionality, 65, passed, total);

        [Fact]
        public void Score_DreiKategorien_ErreichbarePunkteSindExaktDieGesamtpunktzahl()
        {
            var result = EvaluationScorer.Score([CleanCode(3), Compilability(true), Functionality(1, 2)]);

            result.Sum(category => category.MaxPoints).ShouldBe(EvaluationScoring.TotalPoints);
        }

        // Gewichte, die sich nicht glatt auf 100 aufteilen lassen - hier muss die
        // Restverteilung greifen.
        [Fact]
        public void Score_KrummeGewichte_ErreichbarePunkteSindExaktDieGesamtpunktzahl()
        {
            var result = EvaluationScorer.Score(
            [
                Category(EvaluationCategory.CleanCode, 7, 3, 3),
                Category(EvaluationCategory.Compilability, 11, 1, 1),
                Category(EvaluationCategory.Functionality, 13, 1, 2)
            ]);

            result.Sum(category => category.MaxPoints).ShouldBe(EvaluationScoring.TotalPoints);
        }

        // Krumme Gewichte sind der eigentliche Test der Restverteilung: drei
        // gleiche Gewichte ergeben je 33,33 Punkte und müssen trotzdem 100 werden.
        [Fact]
        public void Score_GleicheGewichte_VerteiltDenRestOhneVerlust()
        {
            var result = EvaluationScorer.Score(
            [
                Category(EvaluationCategory.CleanCode, 1, 1, 1),
                Category(EvaluationCategory.Compilability, 1, 1, 1),
                Category(EvaluationCategory.Functionality, 1, 1, 1)
            ]);

            result.Sum(category => category.MaxPoints).ShouldBe(100);
            result.Sum(category => category.Points).ShouldBe(100);
        }

        // Gerechnet wird in double und danach nach größtem Rest gerundet. Eine
        // Ganzzahl-Division käme auf 65 / 3 = 21 und vergäbe für zwei bestandene
        // Testfälle nur 42 statt 43 Punkte.
        [Fact]
        public void Score_ZweiVonDreiTestfaellen_VerliertKeinePunkteMehrDurchGanzzahlDivision()
        {
            var result = EvaluationScorer.Score([CleanCode(3), Compilability(true), Functionality(2, 3)]);

            var testCases = result.Single(category => category.Category == EvaluationCategory.Functionality);
            testCases.Points.ShouldBe(43);
            testCases.MaxPoints.ShouldBe(65);
        }

        // Keine Gratispunkte: eine Aufgabe ohne Prüfungen der Funktionalität
        // liefert die Kategorie gar nicht erst, ihr Gewicht verteilt sich auf die
        // übrigen. Würde die Kategorie stattdessen als "bestanden" gewertet,
        // brächte sie 65 Punkte ein, ohne dass irgendetwas geprüft wurde.
        [Fact]
        public void Score_AufgabeOhneTestfaelle_GibtKeineGratispunkte()
        {
            var result = EvaluationScorer.Score([CleanCode(0, 3), Compilability(true)]);

            result.ShouldNotContain(category => category.Category == EvaluationCategory.Functionality);
            result.Sum(category => category.MaxPoints).ShouldBe(EvaluationScoring.TotalPoints);

            // Clean Code komplett durchgefallen, nur die Kompilierbarkeit zählt.
            result.Sum(category => category.Points).ShouldBeLessThan(EvaluationScoring.TotalPoints);
            result.Single(category => category.Category == EvaluationCategory.CleanCode).Points.ShouldBe(0);
        }

        [Fact]
        public void Score_NichtAnwendbareKategorie_VerteiltIhrGewichtAufDieUebrigen()
        {
            var mitTestfaellen = EvaluationScorer.Score([CleanCode(3), Compilability(true), Functionality(2, 2)]);
            var ohneTestfaelle = EvaluationScorer.Score([CleanCode(3), Compilability(true)]);

            var cleanCodeMit = mitTestfaellen.Single(c => c.Category == EvaluationCategory.CleanCode).MaxPoints;
            var cleanCodeOhne = ohneTestfaelle.Single(c => c.Category == EvaluationCategory.CleanCode).MaxPoints;

            cleanCodeOhne.ShouldBeGreaterThan(cleanCodeMit);
            ohneTestfaelle.Sum(category => category.MaxPoints).ShouldBe(EvaluationScoring.TotalPoints);
        }

        [Fact]
        public void Score_AllesBestanden_LiefertDieVollePunktzahl()
        {
            var result = EvaluationScorer.Score([CleanCode(3), Compilability(true), Functionality(4, 4)]);

            result.Sum(category => category.Points).ShouldBe(EvaluationScoring.TotalPoints);
            result.ShouldAllBe(category => category.Passed);
        }

        [Fact]
        public void Score_NichtsBestanden_LiefertNullPunkte()
        {
            var result = EvaluationScorer.Score([CleanCode(0), Compilability(false), Functionality(0, 4)]);

            result.Sum(category => category.Points).ShouldBe(0);
            result.ShouldAllBe(category => !category.Passed);
        }

        // Aufrunden darf aus "fast alles richtig" nie die volle Punktzahl machen -
        // sonst sieht der Teilnehmer 65/65 neben einem roten Testfall.
        [Fact]
        public void Score_FastAllesBestanden_ErreichtNichtDieVollePunktzahlDerKategorie()
        {
            var result = EvaluationScorer.Score([Category(EvaluationCategory.Functionality, 100, 199, 200)]);

            var testCases = result.ShouldHaveSingleItem();
            testCases.Points.ShouldBeLessThan(testCases.MaxPoints);
            testCases.Passed.ShouldBeFalse();
        }

        [Fact]
        public void Score_TeilweiseBestanden_MeldetDieKategorieAlsNichtBestanden()
        {
            var result = EvaluationScorer.Score([CleanCode(2, 3), Compilability(true)]);

            result.Single(category => category.Category == EvaluationCategory.CleanCode).Passed.ShouldBeFalse();
        }

        [Fact]
        public void Score_Kategorien_KommenInAnzeigereihenfolge()
        {
            // Bewusst in falscher Reihenfolge übergeben.
            var result = EvaluationScorer.Score(
                [Functionality(1, 1), Compilability(true), CleanCode(3)]);

            result.Select(category => category.Category).ShouldBe(
            [
                EvaluationCategory.CleanCode,
                EvaluationCategory.Compilability,
                EvaluationCategory.Functionality
            ]);
        }

        [Fact]
        public void Score_Teilpruefungen_BekommenFortlaufendeReihenfolge()
        {
            var result = EvaluationScorer.Score([CleanCode(1, 3), Compilability(true)]);

            var cleanCode = result.Single(category => category.Category == EvaluationCategory.CleanCode);
            cleanCode.TestCaseResults.Select(testCase => testCase.Order).ShouldBe([0, 1, 2]);
            cleanCode.TestCaseResults.ShouldAllBe(testCase => testCase.CategoryResultId == cleanCode.Id);
        }

        [Fact]
        public void Score_MitFehlerhinweis_UebernimmtIhnInDieKategorie()
        {
            var result = EvaluationScorer.Score(
                [Category(EvaluationCategory.CleanCode, 15, 0, 1, "Vermeide Umlaute."), Compilability(true)]);

            result.Single(category => category.Category == EvaluationCategory.CleanCode)
                .ErrorTip.ShouldBe("Vermeide Umlaute.");
        }

        [Fact]
        public void Score_OhneKategorien_LiefertLeeresErgebnis()
        {
            EvaluationScorer.Score([]).ShouldBeEmpty();
        }

        // Eine falsch konfigurierte Aufgabe darf nicht still zu einer anderen
        // Note führen - lieber laut scheitern.
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Score_GewichtNichtPositiv_Wirft(double weight)
        {
            var inputs = new[] { Category(EvaluationCategory.CleanCode, weight, 1, 1) };

            Should.Throw<InvalidOperationException>(() => EvaluationScorer.Score(inputs))
                .Message.ShouldContain("Gewicht");
        }

        [Fact]
        public void Score_AnwendbareKategorieOhneTeilpruefung_Wirft()
        {
            var inputs = new[] { Category(EvaluationCategory.CleanCode, 15, 0, 0) };

            Should.Throw<InvalidOperationException>(() => EvaluationScorer.Score(inputs))
                .Message.ShouldContain("keine Teilpruefung");
        }

        // Egal wie die Aufgabe geschnitten ist: die Kategoriepunkte müssen genau
        // die angezeigte Gesamtpunktzahl ergeben und dürfen sie nie überschreiten.
        [Theory]
        [InlineData(1, 3, 1, 4)]
        [InlineData(2, 3, 3, 7)]
        [InlineData(3, 3, 6, 7)]
        [InlineData(0, 3, 0, 11)]
        [InlineData(1, 2, 5, 6)]
        public void Score_BeliebigeAufteilung_SummeBleibtInnerhalbDerGesamtpunktzahl(
            int cleanCodePassed,
            int cleanCodeTotal,
            int testsPassed,
            int testsTotal)
        {
            var result = EvaluationScorer.Score(
            [
                Category(EvaluationCategory.CleanCode, 15, cleanCodePassed, cleanCodeTotal),
                Compilability(true),
                Functionality(testsPassed, testsTotal)
            ]);

            result.Sum(category => category.MaxPoints).ShouldBe(EvaluationScoring.TotalPoints);
            result.Sum(category => category.Points).ShouldBeLessThanOrEqualTo(EvaluationScoring.TotalPoints);
            result.ShouldAllBe(category => category.Points <= category.MaxPoints);
        }
    }
}
