using SoopWorkshop.Shared.Constants;
using SoopWorkshop.Shared.DTOs.Transfer;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Transfer
{
    // Prüft eine Transferdatei, bevor irgendetwas geschrieben wird.
    //
    // Reine Funktion ohne Datenbank und ohne Prozesse - wie der EvaluationScorer
    // und aus demselben Grund: so lässt sich die Prüfung vollständig
    // durchtesten, ohne eine Datenbank hochzufahren.
    //
    // Gesammelt werden ALLE Verstöße, nicht nur der erste. Result<T> trägt
    // nur eine Meldung; bei vierzig Aufgaben will aber niemand vierzigmal
    // hochladen, um vierzig Fehler nacheinander zu erfahren.
    public static class TaskBundleValidator
    {
        public static List<string> Validate(TaskBundleDto bundle)
        {
            var errors = new List<string>();

            if (bundle.FormatVersion != TaskBundleFormat.CurrentVersion)
            {
                // Ohne diese Prüfung liest eine spätere Fassung die Datei still
                // falsch, statt zu sagen, dass sie sie nicht kennt.
                errors.Add(
                    $"Die Datei hat das Format {bundle.FormatVersion}, dieses Programm liest " +
                    $"Format {TaskBundleFormat.CurrentVersion}.");

                // Weiter zu prüfen wäre Raten - die Struktur ist ja womöglich
                // eine andere.
                return errors;
            }

            CheckDuplicateIds(bundle, errors);

            foreach (var category in bundle.Categories)
            {
                CheckCategory(category, errors);

                foreach (var task in category.Tasks)
                    CheckTask(category, task, errors);
            }

            return errors;
        }

        private static void CheckDuplicateIds(TaskBundleDto bundle, List<string> errors)
        {
            var doppelteKategorie = bundle.Categories
                .GroupBy(category => category.Id)
                .FirstOrDefault(group => group.Count() > 1);

            if (doppelteKategorie is not null)
                errors.Add($"Die Kategorie-Id {doppelteKategorie.Key} kommt mehrfach vor.");

            var doppelteAufgabe = bundle.Categories
                .SelectMany(category => category.Tasks)
                .GroupBy(task => task.Id)
                .FirstOrDefault(group => group.Count() > 1);

            if (doppelteAufgabe is not null)
                errors.Add($"Die Aufgaben-Id {doppelteAufgabe.Key} kommt mehrfach vor.");
        }

        private static void CheckCategory(TaskBundleCategoryDto category, List<string> errors)
        {
            if (category.Id == Guid.Empty)
                errors.Add($"Die Kategorie „{Describe(category.Name)}“ hat keine Id.");

            if (string.IsNullOrWhiteSpace(category.Name))
                errors.Add($"Eine Kategorie ({category.Id}) hat keinen Namen.");
            else if (category.Name.Length > TaskFieldLimits.CategoryName)
                errors.Add(
                    $"Der Name der Kategorie „{Describe(category.Name)}“ ist zu lang " +
                    $"({category.Name.Length} statt höchstens {TaskFieldLimits.CategoryName}).");

            if (category.IconName.Length > TaskFieldLimits.CategoryIconName)
                errors.Add($"Der Symbolname der Kategorie „{Describe(category.Name)}“ ist zu lang.");

            if (category.Order < 0)
                errors.Add($"Die Kategorie „{Describe(category.Name)}“ hat eine negative Reihenfolge.");
        }

        private static void CheckTask(
            TaskBundleCategoryDto category,
            TaskBundleTaskDto task,
            List<string> errors)
        {
            var wo = $"„{Describe(category.Name)}“ / „{Describe(task.Title)}“";

            if (task.Id == Guid.Empty)
                errors.Add($"Die Aufgabe {wo} hat keine Id.");

            if (string.IsNullOrWhiteSpace(task.Title))
                errors.Add($"Eine Aufgabe in „{Describe(category.Name)}“ hat keinen Titel.");
            else if (task.Title.Length > TaskFieldLimits.TaskTitle)
                errors.Add($"Der Titel von {wo} ist zu lang.");

            if (string.IsNullOrWhiteSpace(task.Description))
                errors.Add($"Die Aufgabe {wo} hat keine Beschreibung.");

            if (task.Order < 0)
                errors.Add($"Die Aufgabe {wo} hat eine negative Reihenfolge.");

            CheckExpectedTypes(wo, task, errors);
            CheckTests(wo, task, errors);
            CheckUnitTestFiles(wo, task, errors);
            CheckWeights(wo, task, errors);
            CheckVisibility(wo, task, errors);
        }

        private static void CheckExpectedTypes(string wo, TaskBundleTaskDto task, List<string> errors)
        {
            foreach (var type in task.ExpectedTypes)
            {
                if (string.IsNullOrWhiteSpace(type.Name))
                {
                    // Ohne Klassennamen gäbe es keinen Rumpf, in dem der
                    // ContractChecker nach der Methode suchen könnte.
                    errors.Add($"In {wo} steht eine geforderte Klasse ohne Namen.");
                    continue;
                }

                if (type.Name.Length > TaskFieldLimits.ExpectedTypeName)
                    errors.Add($"Der Klassenname „{type.Name}“ in {wo} ist zu lang.");

                foreach (var signature in type.Methods)
                {
                    if (string.IsNullOrWhiteSpace(signature))
                        errors.Add($"In {wo} steht bei „{type.Name}“ eine leere Signatur.");
                    else if (signature.Length > TaskFieldLimits.ExpectedMethodSignature)
                        errors.Add($"Eine Signatur bei „{type.Name}“ in {wo} ist zu lang.");
                }
            }

            var doppelt = task.ExpectedTypes
                .GroupBy(type => type.Name, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);

            if (doppelt is not null)
                errors.Add($"Die Klasse „{doppelt.Key}“ wird in {wo} mehrfach gefordert.");
        }

        private static void CheckTests(string wo, TaskBundleTaskDto task, List<string> errors)
        {
            foreach (var test in task.Tests)
            {
                if (string.IsNullOrWhiteSpace(test.Description))
                    errors.Add($"Ein Testfall in {wo} hat keine Beschreibung.");
                else if (test.Description.Length > TaskFieldLimits.TestDescription)
                    errors.Add(
                        $"Die Beschreibung eines Testfalls in {wo} ist zu lang " +
                        $"({test.Description.Length} statt höchstens {TaskFieldLimits.TestDescription}).");

                if (string.IsNullOrEmpty(test.ExpectedOutput))
                    errors.Add($"Ein Testfall in {wo} hat keine erwartete Ausgabe.");
            }
        }

        private static void CheckUnitTestFiles(string wo, TaskBundleTaskDto task, List<string> errors)
        {
            foreach (var file in task.UnitTestFiles)
            {
                // Dieselben Regeln wie in TaskUnitTestFileService.ValidateFileName:
                // der Name wird später zu einem Dateinamen im Arbeitsverzeichnis.
                if (string.IsNullOrWhiteSpace(file.FileName))
                {
                    errors.Add($"Eine JUnit-Datei in {wo} hat keinen Namen.");
                    continue;
                }

                if (!file.FileName.EndsWith(".java", StringComparison.OrdinalIgnoreCase))
                    errors.Add($"„{file.FileName}“ in {wo} muss auf .java enden.");

                if (file.FileName.Contains('/') || file.FileName.Contains('\\') || file.FileName.Contains(".."))
                    errors.Add($"„{file.FileName}“ in {wo} darf keinen Pfadanteil enthalten.");

                if (file.FileName.Length > TaskFieldLimits.UnitTestFileName)
                    errors.Add($"Der Dateiname „{file.FileName}“ in {wo} ist zu lang.");

                if (string.IsNullOrWhiteSpace(file.Content))
                    errors.Add($"Die JUnit-Datei „{file.FileName}“ in {wo} ist leer.");
            }

            var doppelt = task.UnitTestFiles
                .GroupBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);

            if (doppelt is not null)
                errors.Add(
                    $"Der Dateiname „{doppelt.Key}“ kommt in {wo} mehrfach vor. Im selben " +
                    "Arbeitsverzeichnis würde die eine Datei die andere überschreiben.");
        }

        private static void CheckWeights(string wo, TaskBundleTaskDto task, List<string> errors)
        {
            foreach (var weight in task.Weights)
            {
                if (!EvaluationCategoryOrder.IsActive(weight.Category))
                    errors.Add(
                        $"Die Kategorie {weight.Category} in {wo} wird nicht mehr bewertet. " +
                        $"Möglich sind: {string.Join(", ", EvaluationCategoryOrder.Active)}.");

                if (weight.Weight <= 0)
                    errors.Add($"Das Gewicht für {weight.Category} in {wo} muss größer als 0 sein.");
            }

            var doppelt = task.Weights
                .GroupBy(weight => weight.Category)
                .FirstOrDefault(group => group.Count() > 1);

            if (doppelt is not null)
                errors.Add($"Für die Kategorie {doppelt.Key} sind in {wo} mehrere Gewichte angegeben.");
        }

        // Spiegelt TaskItemService.DescribeMissingTestData.
        //
        // Wichtig, weil IsVisible beim Anlegen und Ändern an dieser Prüfung
        // vorbeikommt - sie greift dort nur über PATCH .../visibility. Ohne
        // diese Stelle könnte eine Datei genau die Lage herstellen, gegen die
        // die Prüfung gebaut wurde: eine sichtbare Aufgabe, deren Modus
        // Testdaten verlangt, die es nicht gibt. Die würde still milder
        // bewertet, weil ihre Kategorie aus der Wertung fällt.
        private static void CheckVisibility(string wo, TaskBundleTaskDto task, List<string> errors)
        {
            if (!task.IsVisible)
                return;

            var brauchtKonsole = task.EvaluationMode is EvaluationMode.ConsoleOnly or EvaluationMode.Both;
            var brauchtUnit = task.EvaluationMode is EvaluationMode.UnitTestOnly or EvaluationMode.Both;

            if (brauchtKonsole && task.Tests.Count == 0)
                errors.Add(
                    $"Die Aufgabe {wo} ist sichtbar und auf „{task.EvaluationMode}“ gestellt, " +
                    "hat aber keinen Konsolen-Testfall.");

            if (brauchtUnit && task.UnitTestFiles.Count == 0)
                errors.Add(
                    $"Die Aufgabe {wo} ist sichtbar und auf „{task.EvaluationMode}“ gestellt, " +
                    "hat aber keine JUnit-Datei.");
        }

        private static string Describe(string name) =>
            string.IsNullOrWhiteSpace(name) ? "ohne Namen" : name;
    }
}
