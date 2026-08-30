using System.Net;
using System.Net.Http.Json;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Integration.Controllers
{
    /// <summary>
    /// Die Abgaben-Übersicht: Seitenweise Ausgabe, Filter und die Sortierung,
    /// auf die sich das Blättern verlässt.
    /// </summary>
    public class AdminSubmissionsControllerTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private async Task<Guid> GivenAufgabe(string kategorie = "OOP")
        {
            var category = PersistedDataFactory.VollstaendigeKategorie(kategorie);

            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();
            });

            return category.Tasks.Single().Id;
        }

        private async Task GivenAbgaben(Guid taskItemId, int anzahl, SubmissionStatus status)
        {
            await WithDbAsync(async db =>
            {
                for (var i = 0; i < anzahl; i++)
                {
                    var abgabe = PersistedDataFactory.Abgabe(taskItemId, status);

                    // Auseinandergezogene Zeitpunkte, damit die Sortierung
                    // überhaupt etwas zu sortieren hat. Mit identischen
                    // Zeitstempeln entschiede die Datenbank, und der Test
                    // prüfte nur noch Zufall.
                    abgabe.SubmittedAt = DateTime.UtcNow.AddMinutes(-i);
                    db.Submissions.Add(abgabe);
                }

                await db.SaveChangesAsync();
            });
        }

        [Fact]
        public async Task OhneAnmeldung_Liefert401()
        {
            var response = await CreateClient().GetAsync("/api/admin/submissions");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Eine leere Übersicht ist kein Fehlschlag. Würde sie einer sein,
        // zeigte das Panel am ersten Workshop-Tag eine Fehlermeldung.
        [Fact]
        public async Task OhneAbgaben_LiefertEineLeereSeite()
        {
            var client = await CreateAdminClientAsync();

            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            seite.ShouldNotBeNull();
            seite.Items.ShouldBeEmpty();
            seite.Total.ShouldBe(0);
        }

        [Fact]
        public async Task LiefertAufgabeUndKategorieZurZeile()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 1, SubmissionStatus.Pending);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            var zeile = seite!.Items.ShouldHaveSingleItem();

            // Genau das, was ohne die Includes im Repository still leer bliebe -
            // die Übersicht sähe funktionsfähig aus und nennte nur nichts.
            zeile.TaskTitle.ShouldBe("Bankkonto");
            zeile.CategoryName.ShouldBe("OOP");
            zeile.TaskItemId.ShouldBe(taskId);
            zeile.Status.ShouldBe(SubmissionStatus.Pending);
        }

        // Null und 0 sind nicht dasselbe: 0 wäre eine Aussage über die
        // Lösung, null sagt nur "noch nicht bewertet".
        [Fact]
        public async Task OhneAuswertung_BleibtDiePunktzahlLeer()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 1, SubmissionStatus.Running);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            var zeile = seite!.Items.ShouldHaveSingleItem();
            zeile.TotalScore.ShouldBeNull();
            zeile.MaxScore.ShouldBeNull();
        }

        [Fact]
        public async Task MitAuswertung_NenntDiePunktzahl()
        {
            var taskId = await GivenAufgabe();

            await WithDbAsync(async db =>
            {
                var abgabe = PersistedDataFactory.Abgabe(taskId);
                db.Submissions.Add(abgabe);
                await db.SaveChangesAsync();

                db.EvaluationResults.Add(PersistedDataFactory.Ergebnis(abgabe.Id));
                await db.SaveChangesAsync();
            });

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            var zeile = seite!.Items.ShouldHaveSingleItem();
            zeile.TotalScore.ShouldNotBeNull();
            zeile.MaxScore.ShouldNotBeNull();
        }

        [Fact]
        public async Task NeuesteZuerst()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 5, SubmissionStatus.Done);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>("/api/admin/submissions");

            seite!.Items
                .Select(i => i.SubmittedAt)
                .ShouldBeInOrder(SortDirection.Descending);
        }

        // Der Test, der die Gesamtzahl von der Seitengröße trennt. Würde
        // Total die Seite zählen statt die Menge, stünde im Panel dauerhaft
        // "1 von 1" und niemand käme je auf Seite 2.
        [Fact]
        public async Task Blaettert_UndTotalZaehltDieGanzeMenge()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 7, SubmissionStatus.Done);

            var client = await CreateAdminClientAsync();

            var ersteSeite = await client.GetFromJsonAsync<SubmissionPageDto>(
                "/api/admin/submissions?skip=0&take=3");
            var zweiteSeite = await client.GetFromJsonAsync<SubmissionPageDto>(
                "/api/admin/submissions?skip=3&take=3");

            ersteSeite!.Items.Count.ShouldBe(3);
            ersteSeite.Total.ShouldBe(7);

            zweiteSeite!.Items.Count.ShouldBe(3);
            zweiteSeite.Total.ShouldBe(7);

            // Keine Zeile doppelt: sonst wäre die Sortierung nicht stabil.
            ersteSeite.Items.Select(i => i.Id)
                .Intersect(zweiteSeite.Items.Select(i => i.Id))
                .ShouldBeEmpty();
        }

        [Fact]
        public async Task FiltertNachStatus()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 2, SubmissionStatus.Done);
            await GivenAbgaben(taskId, 3, SubmissionStatus.Failed);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>(
                "/api/admin/submissions?status=Failed");

            seite!.Total.ShouldBe(3);
            seite.Items.ShouldAllBe(i => i.Status == SubmissionStatus.Failed);
        }

        [Fact]
        public async Task FiltertNachAufgabe()
        {
            var ersteAufgabe = await GivenAufgabe("OOP");
            var zweiteAufgabe = await GivenAufgabe("Schleifen");

            await GivenAbgaben(ersteAufgabe, 2, SubmissionStatus.Done);
            await GivenAbgaben(zweiteAufgabe, 4, SubmissionStatus.Done);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>(
                $"/api/admin/submissions?taskItemId={zweiteAufgabe}");

            seite!.Total.ShouldBe(4);
            seite.Items.ShouldAllBe(i => i.TaskItemId == zweiteAufgabe);
        }

        // Eine Seitengrenze, die der Aufrufer selbst bestimmt, ist keine.
        [Fact]
        public async Task UeberzogenesTake_WirdGedeckelt()
        {
            var taskId = await GivenAufgabe();
            await GivenAbgaben(taskId, 3, SubmissionStatus.Done);

            var client = await CreateAdminClientAsync();
            var seite = await client.GetFromJsonAsync<SubmissionPageDto>(
                "/api/admin/submissions?take=100000");

            seite!.Take.ShouldBe(200);
        }

        // Die Ansicht einer einzelnen Abgabe: das, was die Liste bewusst
        // weglässt — die Dateien mit ihrem Inhalt.

        private async Task<Guid> GivenAbgabeMitDateien(Guid taskItemId, params string[] dateinamen)
        {
            var abgabe = PersistedDataFactory.Abgabe(taskItemId);

            abgabe.Files = dateinamen
                .Select(name => new SubmissionFile
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = abgabe.Id,
                    FileName = name,
                    Content = $"public class {Path.GetFileNameWithoutExtension(name)} {{}}"
                })
                .ToList();

            await WithDbAsync(async db =>
            {
                db.Submissions.Add(abgabe);
                await db.SaveChangesAsync();
            });

            return abgabe.Id;
        }

        [Fact]
        public async Task Detail_OhneAnmeldung_Liefert401()
        {
            var response = await CreateClient().GetAsync($"/api/admin/submissions/{Guid.NewGuid()}");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // 404 und nicht 200 mit leerem Rumpf: "gibt es nicht" ist eine eigene
        // Auskunft, und das Frontend unterscheidet sie von "nicht erreichbar".
        [Fact]
        public async Task Detail_UnbekannteId_Liefert404()
        {
            var client = await CreateAdminClientAsync();

            var response = await client.GetAsync($"/api/admin/submissions/{Guid.NewGuid()}");

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Detail_LiefertDenQuelltextMit()
        {
            var taskId = await GivenAufgabe();
            var abgabeId = await GivenAbgabeMitDateien(taskId, "Konto.java");

            var client = await CreateAdminClientAsync();
            var detail = await client.GetFromJsonAsync<SubmissionDetailDto>(
                $"/api/admin/submissions/{abgabeId}");

            var datei = detail!.Files.ShouldHaveSingleItem();
            datei.FileName.ShouldBe("Konto.java");

            // Der Inhalt ist der ganze Zweck des Endpunkts. Käme hier nur der
            // Name an, sähe die Ansicht funktionsfähig aus und zeigte leere
            // Fenster.
            datei.Content.ShouldBe("public class Konto {}");
        }

        [Fact]
        public async Task Detail_MehrereDateien_KommenNachNamenSortiert()
        {
            var taskId = await GivenAufgabe();
            var abgabeId = await GivenAbgabeMitDateien(taskId, "Kunde.java", "Bank.java", "Konto.java");

            var client = await CreateAdminClientAsync();
            var detail = await client.GetFromJsonAsync<SubmissionDetailDto>(
                $"/api/admin/submissions/{abgabeId}");

            detail!.Files.Select(f => f.FileName)
                .ShouldBe(["Bank.java", "Konto.java", "Kunde.java"]);
        }

        // Genau das, was ohne die Includes im Repository still leer bliebe.
        [Fact]
        public async Task Detail_NenntAufgabeUndKategorie()
        {
            var taskId = await GivenAufgabe("Schleifen");
            var abgabeId = await GivenAbgabeMitDateien(taskId, "Main.java");

            var client = await CreateAdminClientAsync();
            var detail = await client.GetFromJsonAsync<SubmissionDetailDto>(
                $"/api/admin/submissions/{abgabeId}");

            detail!.TaskTitle.ShouldBe("Bankkonto");
            detail.CategoryName.ShouldBe("Schleifen");
            detail.TaskItemId.ShouldBe(taskId);
        }
    }
}
