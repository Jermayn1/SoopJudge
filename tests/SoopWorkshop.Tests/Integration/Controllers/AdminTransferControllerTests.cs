using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Shared.DTOs.Transfer;
using SoopWorkshop.Shared.DTOs.Transfer.Requests;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Integration.Controllers
{
    /// <summary>
    /// Der Weg, den das Panel wirklich geht: Export als Datei herunterladen,
    /// Inhalt als JSON wieder hochschicken.
    /// </summary>
    public class AdminTransferControllerTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private async Task GivenBestand()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();
            });
        }

        [Fact]
        public async Task ExportUndImport_UeberHttp_StellenDenBestandWiederHer()
        {
            await GivenBestand();

            var client = await CreateAdminClientAsync();

            var bundle = await client.GetFromJsonAsync<TaskBundleDto>("/api/admin/transfer/export");
            bundle.ShouldNotBeNull();
            bundle.Categories.ShouldHaveSingleItem();

            await WithDbAsync(async db =>
            {
                db.TaskCategories.RemoveRange(await db.TaskCategories.ToListAsync());
                await db.SaveChangesAsync();
            });

            var response = await client.PostAsJsonAsync(
                "/api/admin/transfer/import",
                new ImportRequestDto { Bundle = bundle, Mode = ImportMode.Merge });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var report = await response.Content.ReadFromJsonAsync<ImportReportDto>();
            report.ShouldNotBeNull();
            report.CategoriesCreated.ShouldBe(1);
            report.TasksCreated.ShouldBe(1);

            await WithDbAsync(async db =>
            {
                (await db.TaskCategories.CountAsync()).ShouldBe(1);
                (await db.TaskItems.SingleAsync()).Title.ShouldBe("Bankkonto");
            });
        }

        // Fehler in der Datei kommen bewusst NICHT als 400, sondern als Bericht
        // mit Status 200: es ist kein kaputter Aufruf, sondern ein Befund über
        // den Inhalt - und davon will der Aufrufer alle sehen, nicht den ersten
        // als Fehlermeldung.
        [Fact]
        public async Task Import_UngueltigeDatei_Liefert200MitBerichtVollerFehler()
        {
            await GivenBestand();

            var client = await CreateAdminClientAsync();

            var response = await client.PostAsJsonAsync(
                "/api/admin/transfer/import",
                new ImportRequestDto
                {
                    Bundle = new TaskBundleDto { FormatVersion = 999 },
                    Mode = ImportMode.Replace
                });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var report = await response.Content.ReadFromJsonAsync<ImportReportDto>();
            report.ShouldNotBeNull();
            report.IsValid.ShouldBeFalse();
            report.Errors.ShouldNotBeEmpty();

            // Entscheidend: Replace hätte ohne die Prüfung alles gelöscht.
            await WithDbAsync(async db => (await db.TaskCategories.CountAsync()).ShouldBe(1));
        }

        [Fact]
        public async Task Preview_SchreibtNichtsUndNenntDieAbgaben()
        {
            await GivenBestand();

            var client = await CreateAdminClientAsync();
            var bundle = await client.GetFromJsonAsync<TaskBundleDto>("/api/admin/transfer/export");

            await WithDbAsync(async db =>
            {
                var taskId = (await db.TaskItems.SingleAsync()).Id;
                db.Submissions.Add(PersistedDataFactory.Abgabe(taskId));
                await db.SaveChangesAsync();
            });

            var response = await client.PostAsJsonAsync(
                "/api/admin/transfer/import/preview",
                new ImportRequestDto { Bundle = bundle!, Mode = ImportMode.Replace });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var report = await response.Content.ReadFromJsonAsync<ImportReportDto>();
            report.ShouldNotBeNull();

            // Die Zahl, auf die sich der Bestätigungsdialog stützt.
            report.SubmissionsDeleted.ShouldBe(1);
            report.Warnings.ShouldNotBeEmpty();

            await WithDbAsync(async db => (await db.Submissions.CountAsync()).ShouldBe(1));
        }

        [Fact]
        public async Task Export_OhneAnmeldung_Liefert401()
        {
            await GivenBestand();

            (await CreateClient().GetAsync("/api/admin/transfer/export"))
                .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
