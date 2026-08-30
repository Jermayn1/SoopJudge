using SoopWorkshop.Shared.DTOs.Transfer;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Application.Transfer
{
    // Was der Import mit dem vorhandenen Bestand machen wird.
    //
    // Reine Funktion wie der Validator. Die Vorschau und die Ausführung rufen
    // beide genau diese Rechnung auf - eine Vorschau, die etwas anderes anzeigt
    // als danach passiert, wäre schlimmer als gar keine.
    public static class ImportPlanner
    {
        // Was der Planer vom Bestand wissen muss. Bewusst nicht die Entitäten
        // selbst: so bleibt die Application-Schicht frei von EF und die Rechnung
        // ohne Datenbank testbar.
        public readonly record struct ExistingCategory(Guid Id, IReadOnlyList<ExistingTask> Tasks);

        public readonly record struct ExistingTask(Guid Id, int SubmissionCount);

        public static ImportReportDto Plan(
            IReadOnlyList<ExistingCategory> existing,
            TaskBundleDto bundle,
            ImportMode mode)
        {
            var report = new ImportReportDto();

            var vorhandeneKategorien = existing.Select(category => category.Id).ToHashSet();
            var vorhandeneAufgaben = existing
                .SelectMany(category => category.Tasks)
                .ToDictionary(task => task.Id, task => task.SubmissionCount);

            if (mode == ImportMode.Replace)
            {
                // Der gesamte Bestand geht weg, danach kommt die Datei komplett
                // neu herein. Auch die Aufgaben, die in der Datei stehen - sie
                // werden gelöscht und wieder angelegt, nicht aktualisiert.
                report.CategoriesDeleted = existing.Count;
                report.TasksDeleted = vorhandeneAufgaben.Count;

                // Das Löschen einer Kategorie nimmt per Cascade alles mit, was
                // darunter hängt. Die Abgaben der Teilnehmer sind der Teil, den
                // niemand erwartet - deshalb steht die Zahl im Bericht.
                report.SubmissionsDeleted = vorhandeneAufgaben.Values.Sum();

                report.CategoriesCreated = bundle.Categories.Count;
                report.TasksCreated = bundle.Categories.Sum(category => category.Tasks.Count);

                if (report.SubmissionsDeleted > 0)
                    report.Warnings.Add(
                        $"Beim Ersetzen gehen {report.SubmissionsDeleted} bereits abgegebene " +
                        "Lösung(en) samt Auswertung verloren.");

                return report;
            }

            foreach (var category in bundle.Categories)
            {
                if (vorhandeneKategorien.Contains(category.Id))
                    report.CategoriesUpdated++;
                else
                    report.CategoriesCreated++;

                foreach (var task in category.Tasks)
                {
                    if (vorhandeneAufgaben.ContainsKey(task.Id))
                        report.TasksUpdated++;
                    else
                        report.TasksCreated++;
                }
            }

            // Beim Zusammenführen wird nichts gelöscht. Was auf dem Server
            // steht und nicht in der Datei, bleibt - das ist der Preis dafür,
            // dass ein Zusammenführen nie etwas kaputt macht.
            var uebrig = vorhandeneAufgaben.Count
                - bundle.Categories.SelectMany(c => c.Tasks).Count(t => vorhandeneAufgaben.ContainsKey(t.Id));

            if (uebrig > 0)
                report.Warnings.Add(
                    $"{uebrig} Aufgabe(n) im Bestand stehen nicht in der Datei. Beim " +
                    "Zusammenführen bleiben sie unangetastet.");

            return report;
        }
    }
}
