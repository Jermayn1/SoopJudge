using SoopWorkshop.Backend.Application.Transfer;
using SoopWorkshop.Shared.DTOs.Transfer;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Application.Transfer
{
    public class TaskBundleValidatorTests
    {
        [Fact]
        public void Validate_GueltigeDatei_KeineFehler()
        {
            var bundle = TaskBundleFactory.Bundle(
                TaskBundleFactory.Category(tasks: TaskBundleFactory.Task()));

            TaskBundleValidator.Validate(bundle).ShouldBeEmpty();
        }

        // Ohne diese Prüfung liest eine spätere Fassung die Datei still falsch.
        [Fact]
        public void Validate_FremdesFormat_LehntAbUndPrueftNichtWeiter()
        {
            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category());
            bundle.FormatVersion = 99;

            var errors = TaskBundleValidator.Validate(bundle);

            errors.ShouldHaveSingleItem().ShouldContain("Format 99");
        }

        [Fact]
        public void Validate_DoppelteKategorieId_LehntAb()
        {
            var id = Guid.NewGuid();
            var bundle = TaskBundleFactory.Bundle(
                TaskBundleFactory.Category(id, "Eins"),
                TaskBundleFactory.Category(id, "Zwei"));

            TaskBundleValidator.Validate(bundle)
                .ShouldContain(error => error.Contains("Kategorie-Id") && error.Contains("mehrfach"));
        }

        [Fact]
        public void Validate_DoppelteAufgabenIdUeberKategorienHinweg_LehntAb()
        {
            var id = Guid.NewGuid();
            var bundle = TaskBundleFactory.Bundle(
                TaskBundleFactory.Category(name: "Eins", tasks: TaskBundleFactory.Task(id)),
                TaskBundleFactory.Category(name: "Zwei", tasks: TaskBundleFactory.Task(id)));

            TaskBundleValidator.Validate(bundle)
                .ShouldContain(error => error.Contains("Aufgaben-Id") && error.Contains("mehrfach"));
        }

        [Theory]
        [InlineData("MainTest.txt", "muss auf .java enden")]
        [InlineData("../MainTest.java", "Pfadanteil")]
        [InlineData("unter/MainTest.java", "Pfadanteil")]
        public void Validate_UnzulaessigerDateiname_LehntAb(string fileName, string erwartet)
        {
            var task = TaskBundleFactory.Task(mode: EvaluationMode.UnitTestOnly);
            task.UnitTestFiles = [TaskBundleFactory.JUnitFile(fileName)];

            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category(tasks: task));

            TaskBundleValidator.Validate(bundle).ShouldContain(error => error.Contains(erwartet));
        }

        [Fact]
        public void Validate_ZweiDateienMitDemselbenNamen_LehntAb()
        {
            var task = TaskBundleFactory.Task(mode: EvaluationMode.UnitTestOnly);
            task.UnitTestFiles = [TaskBundleFactory.JUnitFile(), TaskBundleFactory.JUnitFile("maintest.java")];

            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category(tasks: task));

            TaskBundleValidator.Validate(bundle)
                .ShouldContain(error => error.Contains("mehrfach") && error.Contains("überschreiben"));
        }

        [Theory]
        [InlineData(EvaluationCategory.TestCases)]
        [InlineData(EvaluationCategory.CharacterSet)]
        public void Validate_AbgeschaffteKategorieAlsGewicht_LehntAb(EvaluationCategory retired)
        {
            var task = TaskBundleFactory.Task();
            task.Weights = [new TaskBundleWeightDto { Category = retired, Weight = 10 }];

            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category(tasks: task));

            TaskBundleValidator.Validate(bundle)
                .ShouldContain(error => error.Contains("nicht mehr bewertet"));
        }

        [Fact]
        public void Validate_GewichtNichtPositiv_LehntAb()
        {
            var task = TaskBundleFactory.Task();
            task.Weights = [new TaskBundleWeightDto { Category = EvaluationCategory.CleanCode, Weight = 0 }];

            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category(tasks: task));

            TaskBundleValidator.Validate(bundle)
                .ShouldContain(error => error.Contains("größer als 0"));
        }

        // Die Lücke, gegen die diese Prüfung gebaut ist: IsVisible kommt beim
        // Anlegen und Ändern an DescribeMissingTestData vorbei, die greift nur
        // über PATCH .../visibility. Eine Datei könnte also genau die Lage
        // herstellen, gegen die die Prüfung existiert.
        [Fact]
        public void Validate_SichtbareAufgabeOhnePassendeTestdaten_LehntAb()
        {
            var task = TaskBundleFactory.Task(mode: EvaluationMode.UnitTestOnly, isVisible: true);
            task.UnitTestFiles = [];

            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category(tasks: task));

            TaskBundleValidator.Validate(bundle)
                .ShouldContain(error => error.Contains("sichtbar") && error.Contains("keine JUnit-Datei"));
        }

        // Verborgen darf sie das: die Testdaten kommen ja erst noch.
        [Fact]
        public void Validate_VerborgeneAufgabeOhneTestdaten_IstInOrdnung()
        {
            var task = TaskBundleFactory.Task(mode: EvaluationMode.UnitTestOnly, isVisible: false);
            task.UnitTestFiles = [];

            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category(tasks: task));

            TaskBundleValidator.Validate(bundle).ShouldBeEmpty();
        }

        [Fact]
        public void Validate_GeforderteKlasseOhneNamen_LehntAb()
        {
            var task = TaskBundleFactory.Task();
            task.ExpectedTypes = [new TaskBundleExpectedTypeDto { Name = "  ", Methods = ["public void tu()"] }];

            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category(tasks: task));

            TaskBundleValidator.Validate(bundle)
                .ShouldContain(error => error.Contains("geforderte Klasse ohne Namen"));
        }

        [Fact]
        public void Validate_ZuLangeTestbeschreibung_LehntAb()
        {
            var task = TaskBundleFactory.Task();
            task.Tests = [TaskBundleFactory.ConsoleTest(new string('x', 501))];

            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category(tasks: task));

            TaskBundleValidator.Validate(bundle)
                .ShouldContain(error => error.Contains("zu lang") && error.Contains("501"));
        }

        // Der eigentliche Sinn des Sammelns: bei vierzig Aufgaben will niemand
        // vierzigmal hochladen, um vierzig Fehler nacheinander zu erfahren.
        [Fact]
        public void Validate_MehrereVerstoesse_NenntSieAlle()
        {
            var task = TaskBundleFactory.Task();
            task.Title = string.Empty;
            task.Description = string.Empty;
            task.Weights = [new TaskBundleWeightDto { Category = EvaluationCategory.CleanCode, Weight = -1 }];

            var kategorie = TaskBundleFactory.Category(name: string.Empty, tasks: task);
            var bundle = TaskBundleFactory.Bundle(kategorie);

            TaskBundleValidator.Validate(bundle).Count.ShouldBeGreaterThanOrEqualTo(4);
        }
    }
}
