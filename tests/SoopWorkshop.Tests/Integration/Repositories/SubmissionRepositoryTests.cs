using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Integration.Repositories
{
    public class SubmissionRepositoryTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private static ISubmissionRepository Repo(IServiceProvider services) =>
            services.GetRequiredService<ISubmissionRepository>();

        private async Task<(Guid TaskId, Submission Submission)> GivenAbgabe(
            SubmissionStatus status = SubmissionStatus.Pending)
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            var task = category.Tasks.Single();
            var submission = PersistedDataFactory.Abgabe(task.Id, status);

            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();

                db.Submissions.Add(submission);
                await db.SaveChangesAsync();
            });

            return (task.Id, submission);
        }

        // Die Auswertung liest alles über submission.Task. Was hier fehlt, sieht
        // der JavaAnalyzer als "nicht vorhanden" - eine Aufgabe ohne mitgeladene
        // UnitTestFiles würde im Modus UnitTestOnly nicht etwa scheitern,
        // sondern anders bewertet. Deshalb jede Navigation einzeln.
        [Fact]
        public async Task GetByIdAsync_LaedtAbgabeUndDenGanzenAufgabenkontext()
        {
            var (_, submission) = await GivenAbgabe();

            await WithScopeAsync(async services =>
            {
                var geladen = await Repo(services).GetByIdAsync(submission.Id);

                geladen.ShouldNotBeNull();
                geladen.Files.ShouldHaveSingleItem().FileName.ShouldBe("Konto.java");

                geladen.Task.ShouldNotBeNull();
                geladen.Task.Tests.ShouldNotBeEmpty();
                geladen.Task.CategoryWeights.ShouldNotBeEmpty();
                geladen.Task.UnitTestFiles.ShouldNotBeEmpty();

                var typ = geladen.Task.ExpectedTypes.ShouldHaveSingleItem();
                typ.Methods.ShouldHaveSingleItem().Name.ShouldBe("einzahlen");
            });
        }

        [Fact]
        public async Task GetByIdAsync_UnbekannteId_LiefertNull()
        {
            await WithScopeAsync(async services =>
                (await Repo(services).GetByIdAsync(Guid.NewGuid())).ShouldBeNull());
        }

        // Der Status-Endpunkt fragt nur nach dem Stand und braucht weder Dateien
        // noch Aufgabe. Wichtiger ist AsNoTracking: der Endpunkt wird beim Pollen
        // alle zwei Sekunden aufgerufen.
        [Fact]
        public async Task GetSummaryByIdAsync_LiefertDenStandOhneVerfolgung()
        {
            var (taskId, submission) = await GivenAbgabe(SubmissionStatus.Running);

            await WithScopeAsync(async services =>
            {
                var db = services.GetRequiredService<Backend.Infrastructure.Persistence.AppDbContext>();

                var stand = await Repo(services).GetSummaryByIdAsync(submission.Id, CancellationToken.None);

                stand.ShouldNotBeNull();
                stand.Status.ShouldBe(SubmissionStatus.Running);
                stand.TaskItemId.ShouldBe(taskId);

                db.ChangeTracker.Entries<Submission>().ShouldBeEmpty();
            });
        }

        // Der EvaluationWorker räumt damit beim Start auf. Fängt die Abfrage
        // die falschen Zustände ein, setzt er entweder laufende Abgaben auf
        // Failed oder lässt verwaiste stehen.
        [Fact]
        public async Task GetIdsByStatusAsync_LiefertNurDieGesuchtenZustaendeAeltesteZuerst()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            var task = category.Tasks.Single();

            var alt = PersistedDataFactory.Abgabe(task.Id, SubmissionStatus.Pending);
            alt.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
            var neu = PersistedDataFactory.Abgabe(task.Id, SubmissionStatus.Running);
            neu.SubmittedAt = DateTime.UtcNow;
            var fertig = PersistedDataFactory.Abgabe(task.Id, SubmissionStatus.Done);

            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();

                db.Submissions.AddRange(alt, neu, fertig);
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(async services =>
            {
                var ids = await Repo(services).GetIdsByStatusAsync(
                    [SubmissionStatus.Pending, SubmissionStatus.Running],
                    CancellationToken.None);

                ids.ShouldBe([alt.Id, neu.Id]);
            });
        }

        // Läuft über ExecuteUpdate, also am Kontext vorbei direkt in die
        // Datenbank. Genau deshalb muss die Prüfung aus einem frischen Kontext
        // kommen - der schreibende würde den alten Stand aus seiner
        // Änderungsverfolgung liefern.
        [Fact]
        public async Task UpdateStatusAsync_SchreibtStandUndMeldung()
        {
            var (_, submission) = await GivenAbgabe(SubmissionStatus.Running);

            await WithScopeAsync(services => Repo(services).UpdateStatusAsync(
                submission.Id,
                SubmissionStatus.Failed,
                "Der Server wurde waehrend der Auswertung beendet.",
                CancellationToken.None));

            await WithDbAsync(async db =>
            {
                var gespeichert = await db.Submissions.SingleAsync(s => s.Id == submission.Id);
                gespeichert.Status.ShouldBe(SubmissionStatus.Failed);
                gespeichert.ErrorMessage.ShouldBe("Der Server wurde waehrend der Auswertung beendet.");
            });
        }

        [Fact]
        public async Task GetByTaskIdAsync_LiefertNurDieEigenenNeuesteZuerst()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            var task = category.Tasks.Single();
            var andereAufgabe = PersistedDataFactory.VollstaendigeAufgabe(category.Id, "Andere");

            var alt = PersistedDataFactory.Abgabe(task.Id);
            alt.SubmittedAt = DateTime.UtcNow.AddMinutes(-10);
            var neu = PersistedDataFactory.Abgabe(task.Id);
            neu.SubmittedAt = DateTime.UtcNow;
            var fremd = PersistedDataFactory.Abgabe(andereAufgabe.Id);

            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                db.TaskItems.Add(andereAufgabe);
                await db.SaveChangesAsync();

                db.Submissions.AddRange(alt, neu, fremd);
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(async services =>
            {
                var abgaben = await Repo(services).GetByTaskIdAsync(task.Id);

                abgaben.Select(s => s.Id).ShouldBe([neu.Id, alt.Id]);
            });
        }
    }
}
