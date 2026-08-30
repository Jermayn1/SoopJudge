using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Tests.Integration.Repositories
{
    public class TaskCategoryRepositoryTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private static ITaskCategoryRepository Repo(IServiceProvider services) =>
            services.GetRequiredService<ITaskCategoryRepository>();

        [Fact]
        public async Task GetByIdAsync_LaedtDieAufgabenMit()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(async services =>
            {
                var geladen = await Repo(services).GetByIdAsync(category.Id);

                geladen.ShouldNotBeNull();
                geladen.Tasks.ShouldHaveSingleItem().Title.ShouldBe("Bankkonto");
            });
        }

        // Die Filterung greift auf ZWEI Ebenen, und das ist der Kern: eine
        // sichtbare Kategorie darf keine verborgene Aufgabe durchlassen. Ohne
        // das Where im Include stünden unfertige Aufgaben in der
        // Teilnehmersicht, obwohl sie ausdrücklich verborgen sind.
        [Fact]
        public async Task GetAllVisibleAsync_FiltertKategorieUndAufgabe()
        {
            var sichtbar = PersistedDataFactory.VollstaendigeKategorie("Sichtbar");
            var verborgeneAufgabe = PersistedDataFactory.VollstaendigeAufgabe(sichtbar.Id, "Noch nicht fertig");
            verborgeneAufgabe.IsVisible = false;
            verborgeneAufgabe.Order = 2;
            sichtbar.Tasks.Add(verborgeneAufgabe);

            var verborgen = PersistedDataFactory.VollstaendigeKategorie("Verborgen");
            verborgen.IsVisible = false;

            await WithDbAsync(async db =>
            {
                db.TaskCategories.AddRange(sichtbar, verborgen);
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(async services =>
            {
                var kategorien = await Repo(services).GetAllVisibleAsync();

                var einzige = kategorien.ShouldHaveSingleItem();
                einzige.Name.ShouldBe("Sichtbar");
                einzige.Tasks.ShouldHaveSingleItem().Title.ShouldBe("Bankkonto");
            });
        }

        [Fact]
        public async Task GetAllAsync_LiefertAuchVerborgeneUndSortiertNachOrder()
        {
            var zuletzt = PersistedDataFactory.VollstaendigeKategorie("Zuletzt");
            zuletzt.Order = 9;
            zuletzt.IsVisible = false;

            var zuerst = PersistedDataFactory.VollstaendigeKategorie("Zuerst");
            zuerst.Order = 0;

            await WithDbAsync(async db =>
            {
                db.TaskCategories.AddRange(zuletzt, zuerst);
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(async services =>
            {
                var kategorien = await Repo(services).GetAllAsync();

                kategorien.Select(c => c.Name).ShouldBe(["Zuerst", "Zuletzt"]);
            });
        }

        // Darauf steht der Replace-Import: seine Vorschau nennt die Zahl der
        // Abgaben, die mitgehen. Trägt die Kaskade nicht bis dorthin, bricht der
        // Import an einer Fremdschlüsselbedingung ab - oder, schlimmer, die
        // Vorschau sagt etwas anderes als das, was danach passiert.
        [Fact]
        public async Task DeleteAsync_LoeschtAllesDarunterBisZurAbgabe()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            var task = category.Tasks.Single();
            var submission = PersistedDataFactory.Abgabe(task.Id);

            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();

                db.Submissions.Add(submission);
                await db.SaveChangesAsync();

                db.EvaluationResults.Add(PersistedDataFactory.Ergebnis(submission.Id));
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(services => Repo(services).DeleteAsync(category.Id));

            await WithDbAsync(async db =>
            {
                (await db.TaskCategories.CountAsync()).ShouldBe(0);
                (await db.TaskItems.CountAsync()).ShouldBe(0);
                (await db.TaskTests.CountAsync()).ShouldBe(0);
                (await db.TaskUnitTestFiles.CountAsync()).ShouldBe(0);
                (await db.TaskExpectedTypes.CountAsync()).ShouldBe(0);
                (await db.TaskExpectedMethods.CountAsync()).ShouldBe(0);
                (await db.TaskCategoryWeights.CountAsync()).ShouldBe(0);
                (await db.TaskHints.CountAsync()).ShouldBe(0);
                (await db.Submissions.CountAsync()).ShouldBe(0);
                (await db.SubmissionFiles.CountAsync()).ShouldBe(0);
                (await db.EvaluationResults.CountAsync()).ShouldBe(0);
                (await db.CategoryResults.CountAsync()).ShouldBe(0);
                (await db.TestCaseResults.CountAsync()).ShouldBe(0);
            });
        }

        [Fact]
        public async Task DeleteAsync_UnbekannteId_TutNichtsUndWirftNicht()
        {
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(new TaskCategory { Id = Guid.NewGuid(), Name = "Bleibt", Order = 1 });
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(services => Repo(services).DeleteAsync(Guid.NewGuid()));

            await WithDbAsync(async db => (await db.TaskCategories.CountAsync()).ShouldBe(1));
        }

        [Fact]
        public async Task ExistsAsync_UnterscheidetVorhandenVonNicht()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(async services =>
            {
                var repository = Repo(services);
                (await repository.ExistsAsync(category.Id, CancellationToken.None)).ShouldBeTrue();
                (await repository.ExistsAsync(Guid.NewGuid(), CancellationToken.None)).ShouldBeFalse();
            });
        }
    }
}
