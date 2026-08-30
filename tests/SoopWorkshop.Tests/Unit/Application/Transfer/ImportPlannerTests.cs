using SoopWorkshop.Backend.Application.Transfer;
using SoopWorkshop.Shared.Enums;
using SoopWorkshop.Tests.Helpers;

namespace SoopWorkshop.Tests.Unit.Application.Transfer
{
    public class ImportPlannerTests
    {
        private static ImportPlanner.ExistingCategory Vorhanden(
            Guid categoryId,
            params (Guid TaskId, int Submissions)[] tasks) =>
            new(categoryId, [.. tasks.Select(t => new ImportPlanner.ExistingTask(t.TaskId, t.Submissions))]);

        [Fact]
        public void Plan_ZusammenfuehrenMitNeuerKategorie_ZaehltAlsAngelegt()
        {
            var bundle = TaskBundleFactory.Bundle(
                TaskBundleFactory.Category(tasks: TaskBundleFactory.Task()));

            var report = ImportPlanner.Plan([], bundle, ImportMode.Merge);

            report.CategoriesCreated.ShouldBe(1);
            report.TasksCreated.ShouldBe(1);
            report.CategoriesUpdated.ShouldBe(0);
            report.TasksDeleted.ShouldBe(0);
            report.SubmissionsDeleted.ShouldBe(0);
        }

        // Der Grund, warum die Ids mitwandern: ein erneuter Import derselben
        // Datei darf nichts verdoppeln.
        [Fact]
        public void Plan_ZusammenfuehrenMitBekanntenIds_ZaehltAlsAktualisiert()
        {
            var kategorieId = Guid.NewGuid();
            var aufgabeId = Guid.NewGuid();

            var bundle = TaskBundleFactory.Bundle(
                TaskBundleFactory.Category(kategorieId, tasks: TaskBundleFactory.Task(aufgabeId)));

            var report = ImportPlanner.Plan(
                [Vorhanden(kategorieId, (aufgabeId, 0))],
                bundle,
                ImportMode.Merge);

            report.CategoriesUpdated.ShouldBe(1);
            report.TasksUpdated.ShouldBe(1);
            report.CategoriesCreated.ShouldBe(0);
            report.TasksCreated.ShouldBe(0);
        }

        [Fact]
        public void Plan_ZusammenfuehrenLaesstUnbekanntesStehen_UndSagtEs()
        {
            var fremdeAufgabe = Guid.NewGuid();

            var bundle = TaskBundleFactory.Bundle(
                TaskBundleFactory.Category(tasks: TaskBundleFactory.Task()));

            var report = ImportPlanner.Plan(
                [Vorhanden(Guid.NewGuid(), (fremdeAufgabe, 0))],
                bundle,
                ImportMode.Merge);

            report.TasksDeleted.ShouldBe(0);
            report.SubmissionsDeleted.ShouldBe(0);
            report.Warnings.ShouldContain(warning => warning.Contains("unangetastet"));
        }

        // Das Löschen einer Kategorie nimmt per Cascade die Abgaben mit. Die
        // Zahl muss vorher auf dem Tisch liegen.
        [Fact]
        public void Plan_ErsetzenZaehltAbgabenMit()
        {
            var bundle = TaskBundleFactory.Bundle(
                TaskBundleFactory.Category(tasks: TaskBundleFactory.Task()));

            var report = ImportPlanner.Plan(
                [
                    Vorhanden(Guid.NewGuid(), (Guid.NewGuid(), 12), (Guid.NewGuid(), 5)),
                    Vorhanden(Guid.NewGuid(), (Guid.NewGuid(), 3))
                ],
                bundle,
                ImportMode.Replace);

            report.CategoriesDeleted.ShouldBe(2);
            report.TasksDeleted.ShouldBe(3);
            report.SubmissionsDeleted.ShouldBe(20);
            report.Warnings.ShouldContain(warning => warning.Contains("20"));
        }

        // Auch was in der Datei steht, wird beim Ersetzen gelöscht und neu
        // angelegt - nicht aktualisiert.
        [Fact]
        public void Plan_ErsetzenMitBekanntenIds_ZaehltTrotzdemAlsAngelegt()
        {
            var kategorieId = Guid.NewGuid();
            var aufgabeId = Guid.NewGuid();

            var bundle = TaskBundleFactory.Bundle(
                TaskBundleFactory.Category(kategorieId, tasks: TaskBundleFactory.Task(aufgabeId)));

            var report = ImportPlanner.Plan(
                [Vorhanden(kategorieId, (aufgabeId, 0))],
                bundle,
                ImportMode.Replace);

            report.CategoriesCreated.ShouldBe(1);
            report.TasksCreated.ShouldBe(1);
            report.CategoriesUpdated.ShouldBe(0);
            report.TasksUpdated.ShouldBe(0);
        }

        [Fact]
        public void Plan_ErsetzenOhneAbgaben_WarntNicht()
        {
            var bundle = TaskBundleFactory.Bundle(TaskBundleFactory.Category());

            var report = ImportPlanner.Plan(
                [Vorhanden(Guid.NewGuid(), (Guid.NewGuid(), 0))],
                bundle,
                ImportMode.Replace);

            report.SubmissionsDeleted.ShouldBe(0);
            report.Warnings.ShouldBeEmpty();
        }
    }
}
