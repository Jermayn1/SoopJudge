using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Integration.Repositories
{
    /// <summary>
    /// Die drei Repositories mit Blockspeicherung. Sie sind bewusst zusammen
    /// getestet: ihr Vertrag ist derselbe - alter Bestand raus, neuer rein, ein
    /// SaveChanges, damit die Aufgabe zwischendurch nie halb dasteht.
    /// </summary>
    public class BlockRepositoryTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private async Task<Guid> GivenAufgabe()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();
            });

            return category.Tasks.Single().Id;
        }

        // --- TaskTestRepository ------------------------------------------------

        [Fact]
        public async Task TaskTestRepository_ReplaceForTaskItemAsync_ErsetztStattZuErgaenzen()
        {
            var taskId = await GivenAufgabe();

            await WithScopeAsync(services =>
                services.GetRequiredService<ITaskTestRepository>().ReplaceForTaskItemAsync(taskId,
                [
                    new TaskTest
                    {
                        TaskItemId = taskId,
                        Description = "Der neue Fall",
                        ExpectedOutput = "42",
                        Order = 1
                    }
                ]));

            await WithDbAsync(async db =>
            {
                var tests = await db.TaskTests.Where(t => t.TaskItemId == taskId).ToListAsync();
                tests.ShouldHaveSingleItem().Description.ShouldBe("Der neue Fall");
            });
        }

        [Fact]
        public async Task TaskTestRepository_ReplaceForTaskItemAsync_LeereListeLoeschtAlles()
        {
            var taskId = await GivenAufgabe();

            await WithScopeAsync(services =>
                services.GetRequiredService<ITaskTestRepository>().ReplaceForTaskItemAsync(taskId, []));

            await WithDbAsync(async db =>
                (await db.TaskTests.CountAsync(t => t.TaskItemId == taskId)).ShouldBe(0));
        }

        // Die Blockspeicherung darf nur die eigene Aufgabe anfassen. Ohne den
        // Where-Filter beim Löschen räumte ein Speichern in Aufgabe A die
        // Testfälle von Aufgabe B mit ab.
        [Fact]
        public async Task TaskTestRepository_ReplaceForTaskItemAsync_LaesstFremdeAufgabenInRuhe()
        {
            var eigene = await GivenAufgabe();

            var fremdeKategorie = PersistedDataFactory.VollstaendigeKategorie("Fremd");
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(fremdeKategorie);
                await db.SaveChangesAsync();
            });
            var fremde = fremdeKategorie.Tasks.Single().Id;

            await WithScopeAsync(services =>
                services.GetRequiredService<ITaskTestRepository>().ReplaceForTaskItemAsync(eigene, []));

            await WithDbAsync(async db =>
                (await db.TaskTests.CountAsync(t => t.TaskItemId == fremde)).ShouldBe(1));
        }

        [Fact]
        public async Task TaskTestRepository_GetByTaskItemIdAsync_SortiertNachOrder()
        {
            var taskId = await GivenAufgabe();

            await WithScopeAsync(services =>
                services.GetRequiredService<ITaskTestRepository>().ReplaceForTaskItemAsync(taskId,
                [
                    new TaskTest { TaskItemId = taskId, Description = "Zweiter", Order = 2 },
                    new TaskTest { TaskItemId = taskId, Description = "Erster", Order = 1 }
                ]));

            await WithScopeAsync(async services =>
            {
                var tests = await services.GetRequiredService<ITaskTestRepository>()
                    .GetByTaskItemIdAsync(taskId);

                tests.Select(t => t.Description).ShouldBe(["Erster", "Zweiter"]);
            });
        }

        // --- TaskUnitTestFileRepository ---------------------------------------

        [Fact]
        public async Task TaskUnitTestFileRepository_ReplaceForTaskItemAsync_ErsetztStattZuErgaenzen()
        {
            var taskId = await GivenAufgabe();

            await WithScopeAsync(services =>
                services.GetRequiredService<ITaskUnitTestFileRepository>().ReplaceForTaskItemAsync(taskId,
                [
                    new TaskUnitTestFile
                    {
                        TaskItemId = taskId,
                        FileName = "NeuTest.java",
                        Content = "class NeuTest {}",
                        Order = 1
                    }
                ]));

            await WithDbAsync(async db =>
            {
                var dateien = await db.TaskUnitTestFiles.Where(f => f.TaskItemId == taskId).ToListAsync();
                dateien.ShouldHaveSingleItem().FileName.ShouldBe("NeuTest.java");
            });
        }

        // Läuft über ExecuteDelete, also am Kontext vorbei. Die Prüfung muss
        // deshalb aus einem frischen Kontext kommen.
        [Fact]
        public async Task TaskUnitTestFileRepository_DeleteAsync_LoeschtGenauEine()
        {
            var taskId = await GivenAufgabe();

            Guid zuLoeschen = Guid.Empty;
            await WithDbAsync(async db =>
            {
                zuLoeschen = (await db.TaskUnitTestFiles.SingleAsync(f => f.TaskItemId == taskId)).Id;
            });

            await WithScopeAsync(services =>
                services.GetRequiredService<ITaskUnitTestFileRepository>().DeleteAsync(zuLoeschen));

            await WithDbAsync(async db =>
                (await db.TaskUnitTestFiles.CountAsync(f => f.Id == zuLoeschen)).ShouldBe(0));
        }

        // --- TaskCategoryWeightRepository -------------------------------------

        [Fact]
        public async Task TaskCategoryWeightRepository_ReplaceForTaskItemAsync_ErsetztDenGanzenSatz()
        {
            var taskId = await GivenAufgabe();

            await WithScopeAsync(services =>
                services.GetRequiredService<ITaskCategoryWeightRepository>().ReplaceForTaskItemAsync(taskId,
                [
                    new TaskCategoryWeight
                    {
                        TaskItemId = taskId,
                        Category = EvaluationCategory.CleanCode,
                        Weight = 20
                    },
                    new TaskCategoryWeight
                    {
                        TaskItemId = taskId,
                        Category = EvaluationCategory.Compilability,
                        Weight = 30
                    }
                ]));

            await WithScopeAsync(async services =>
            {
                var gewichte = await services.GetRequiredService<ITaskCategoryWeightRepository>()
                    .GetByTaskItemIdAsync(taskId);

                gewichte.Count.ShouldBe(2);

                // Festgehaltenes Ist-Verhalten: OrderBy(Category) sortiert nach dem
                // Zahlenwert des Enums. Weil dort die nicht mehr vergebenen Werte
                // CharacterSet und NamingConventions vorn stehen, liegt
                // Compilability (2) vor CleanCode (3). Die Anzeigereihenfolge ist
                // eine andere und kommt aus EvaluationCategoryOrder (CleanCode
                // zuerst). Beides nebeneinander ist unauffällig, solange niemand
                // die Reihenfolge aus dem Repository für die Anzeige hält.
                gewichte.Select(w => w.Category)
                    .ShouldBe([EvaluationCategory.Compilability, EvaluationCategory.CleanCode]);

                gewichte.Single(w => w.Category == EvaluationCategory.CleanCode)
                    .Weight.ShouldBe(20);
            });
        }

        [Fact]
        public async Task TaskCategoryWeightRepository_HaeltNachkommastellen()
        {
            var taskId = await GivenAufgabe();

            await WithScopeAsync(services =>
                services.GetRequiredService<ITaskCategoryWeightRepository>().ReplaceForTaskItemAsync(taskId,
                [
                    new TaskCategoryWeight
                    {
                        TaskItemId = taskId,
                        Category = EvaluationCategory.Functionality,
                        Weight = 33.75
                    }
                ]));

            await WithDbAsync(async db =>
                (await db.TaskCategoryWeights.SingleAsync(w => w.TaskItemId == taskId))
                    .Weight.ShouldBe(33.75));
        }

        // --- EvaluationResultRepository ---------------------------------------

        [Fact]
        public async Task EvaluationResultRepository_GetBySubmissionIdAsync_LaedtZweiEbenenTief()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            var submission = PersistedDataFactory.Abgabe(category.Tasks.Single().Id);

            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();

                db.Submissions.Add(submission);
                await db.SaveChangesAsync();

                db.EvaluationResults.Add(PersistedDataFactory.Ergebnis(submission.Id));
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(async services =>
            {
                var ergebnis = await services.GetRequiredService<IEvaluationResultRepository>()
                    .GetBySubmissionIdAsync(submission.Id);

                ergebnis.ShouldNotBeNull();
                ergebnis.TotalScore.ShouldBe(80);

                var kategorie = ergebnis.CategoryResults.ShouldHaveSingleItem();

                // Ohne ThenInclude käme die Kategorie ohne ihre Teilprüfungen,
                // und die Ergebnisseite zeigte eine leere Karte statt der Gründe.
                var teilpruefung = kategorie.TestCaseResults.ShouldHaveSingleItem();
                teilpruefung.ExpectedOutput.ShouldBe("Stand: 100");
                teilpruefung.ActualOutput.ShouldBe("Stand: 0");
            });
        }

        [Fact]
        public async Task EvaluationResultRepository_GetBySubmissionIdAsync_OhneErgebnis_LiefertNull()
        {
            await WithScopeAsync(async services =>
                (await services.GetRequiredService<IEvaluationResultRepository>()
                    .GetBySubmissionIdAsync(Guid.NewGuid())).ShouldBeNull());
        }
    }
}
