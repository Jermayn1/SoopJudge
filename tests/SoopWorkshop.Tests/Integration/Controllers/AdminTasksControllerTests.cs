using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Integration.Controllers
{
    public class AdminTasksControllerTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private async Task<(Guid CategoryId, Guid TaskId)> GivenBestand()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();
            });

            return (category.Id, category.Tasks.Single().Id);
        }

        // Die Kernregel aus Paragraph 5.1: eine Aufgabe, deren Modus Daten
        // verlangt, die es nicht gibt, lässt sich nicht sichtbar schalten.
        // Ohne sie würde die fehlende Kategorie aus der Wertung fallen und die
        // Aufgabe still MILDER bewertet - der Teilnehmer bekäme Punkte für
        // etwas, das nie geprüft wurde.
        [Fact]
        public async Task ToggleVisibility_ModusVerlangtTestdatenDieFehlen_WirdAbgelehnt()
        {
            var (categoryId, _) = await GivenBestand();

            var ohneTestdaten = new TaskItem
            {
                Id = Guid.NewGuid(),
                TaskCategoryId = categoryId,
                Title = "Noch ohne Tests",
                Description = "Beschreibung",
                EvaluationMode = EvaluationMode.UnitTestOnly,
                IsVisible = false
            };

            await WithDbAsync(async db =>
            {
                db.TaskItems.Add(ohneTestdaten);
                await db.SaveChangesAsync();
            });

            var client = await CreateAdminClientAsync();
            var response = await client.PatchAsync($"/api/admin/tasks/{ohneTestdaten.Id}/visibility", null);

            // Worauf es fachlich ankommt: die Aufgabe bleibt verborgen, und die
            // Meldung sagt, was fehlt.
            (await response.Content.ReadAsStringAsync()).ShouldContain("JUnit-Datei");

            // 400, nicht 404: die Aufgabe GIBT es, ihr fehlen nur die Testdaten
            // ihres Modus. Käme hier 404, läse das Frontend "gibt es nicht" für
            // etwas, das offen im Editor liegt - genau die Zusammenlegung zweier
            // Ausgänge, gegen die ApiResult gebaut ist.
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            await WithDbAsync(async db =>
                (await db.TaskItems.SingleAsync(t => t.Id == ohneTestdaten.Id))
                    .IsVisible.ShouldBeFalse());
        }

        // Die Gegenprobe zum Test darüber. Ohne sie belegte nichts mehr, dass
        // die beiden Fälle überhaupt noch unterscheidbar sind - ein Controller,
        // der pauschal 400 liefert, bestünde oben genauso.
        [Fact]
        public async Task ToggleVisibility_AufgabeGibtEsNicht_Liefert404()
        {
            var client = await CreateAdminClientAsync();

            var response = await client.PatchAsync(
                $"/api/admin/tasks/{Guid.NewGuid()}/visibility", null);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ToggleVisibility_MitVollstaendigenTestdaten_SchaltetUm()
        {
            var (_, taskId) = await GivenBestand();

            var client = await CreateAdminClientAsync();

            // Die Aufgabe aus der Factory ist bereits sichtbar - erst aus, dann an.
            (await client.PatchAsync($"/api/admin/tasks/{taskId}/visibility", null))
                .StatusCode.ShouldBe(HttpStatusCode.OK);

            var response = await client.PatchAsync($"/api/admin/tasks/{taskId}/visibility", null);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var stand = await response.Content.ReadFromJsonAsync<VisibilityStateDto>();
            stand.ShouldNotBeNull();
            stand.IsVisible.ShouldBeTrue();
        }

        // Der Endpunkt, auf dem die Vorschau im Verwaltungsbereich steht. Er
        // liefert denselben DTO wie die öffentliche Seite - samt derselben
        // Filterung. Genau deshalb ist die Vorschau ehrlich durch Konstruktion.
        [Fact]
        public async Task GetById_LiefertNurFreigeschalteteJUnitDateien()
        {
            var (_, taskId) = await GivenBestand();

            await WithDbAsync(async db =>
            {
                db.TaskUnitTestFiles.Add(new TaskUnitTestFile
                {
                    Id = Guid.NewGuid(),
                    TaskItemId = taskId,
                    FileName = "GeheimTest.java",
                    Content = "class GeheimTest {}",
                    Order = 2,
                    IsVisibleToParticipant = false
                });
                await db.SaveChangesAsync();
            });

            var client = await CreateAdminClientAsync();
            var aufgabe = await client.GetFromJsonAsync<TaskItemDto>($"/api/admin/tasks/{taskId}");

            aufgabe.ShouldNotBeNull();
            aufgabe.VisibleUnitTestFiles
                .Select(file => file.FileName)
                .ShouldBe(["KontoTest.java"]);
        }

        [Fact]
        public async Task Create_UnbekannteKategorie_Liefert400StattServerfehler()
        {
            var client = await CreateAdminClientAsync();

            var response = await client.PostAsJsonAsync("/api/admin/tasks", new CreateTaskItemDto
            {
                TaskCategoryId = Guid.NewGuid(),
                Title = "Ins Leere",
                Description = "Beschreibung"
            });

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        // Regression: neue Kindzeilen müssen OHNE Id in den Graphen, wenn die
        // Aufgabe bereits von EF verfolgt wird. An einem gesetzten Schlüssel
        // erkennt die Änderungsverfolgung eine bestehende Zeile, schickt ein
        // UPDATE ins Leere und der Aufruf endet in einem 500er.
        [Fact]
        public async Task Update_MitTippsUndErwartetenTypen_Gelingt()
        {
            var (categoryId, taskId) = await GivenBestand();

            var client = await CreateAdminClientAsync();

            var response = await client.PutAsJsonAsync($"/api/admin/tasks/{taskId}", new UpdateTaskItemDto
            {
                Id = taskId,
                TaskCategoryId = categoryId,
                Title = "Bankkonto, ueberarbeitet",
                Description = "Neue Beschreibung",
                Difficulty = Difficulty.Hard,
                Order = 2,
                EvaluationMode = EvaluationMode.Both,
                Hints = ["Erster Tipp", "Zweiter Tipp"],
                ExpectedTypes =
                [
                    new ExpectedTypeInputDto
                    {
                        Name = "Konto",
                        Methods = ["void einzahlen(int betrag)", "int getStand()"]
                    },
                    new ExpectedTypeInputDto { Name = "Kunde", Methods = [] }
                ]
            });

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var aufgabe = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            aufgabe.ShouldNotBeNull();
            aufgabe.Title.ShouldBe("Bankkonto, ueberarbeitet");
            aufgabe.Hints.Count.ShouldBe(2);
            aufgabe.ExpectedTypes.Select(type => type.Name).ShouldBe(["Konto", "Kunde"]);

            // Ein zweites Mal speichern: derselbe Fund trat erst beim Ändern
            // einer Aufgabe auf, die die Kinder schon hatte.
            var zweitesMal = await client.PutAsJsonAsync($"/api/admin/tasks/{taskId}", new UpdateTaskItemDto
            {
                Id = taskId,
                TaskCategoryId = categoryId,
                Title = "Und noch einmal",
                Description = "Neue Beschreibung",
                EvaluationMode = EvaluationMode.Both,
                Hints = ["Nur noch ein Tipp"],
                ExpectedTypes = [new ExpectedTypeInputDto { Name = "Konto", Methods = [] }]
            });

            zweitesMal.StatusCode.ShouldBe(HttpStatusCode.OK);

            await WithDbAsync(async db =>
            {
                (await db.TaskHints.CountAsync(h => h.TaskItemId == taskId)).ShouldBe(1);
                (await db.TaskExpectedTypes.CountAsync(t => t.TaskItemId == taskId)).ShouldBe(1);
                (await db.TaskExpectedMethods.CountAsync()).ShouldBe(0);
            });
        }

        [Fact]
        public async Task GetAll_LiefertAuchVerborgeneAufgaben()
        {
            var (categoryId, _) = await GivenBestand();

            await WithDbAsync(async db =>
            {
                var verborgen = PersistedDataFactory.VollstaendigeAufgabe(categoryId, "Verborgen");
                verborgen.IsVisible = false;
                verborgen.Order = 2;
                db.TaskItems.Add(verborgen);
                await db.SaveChangesAsync();
            });

            var client = await CreateAdminClientAsync();
            var aufgaben = await client.GetFromJsonAsync<List<TaskItemDto>>("/api/admin/tasks");

            aufgaben.ShouldNotBeNull();
            aufgaben.Count.ShouldBe(2);
        }

        [Fact]
        public async Task Delete_UnbekannteAufgabe_Liefert404()
        {
            var client = await CreateAdminClientAsync();

            (await client.DeleteAsync($"/api/admin/tasks/{Guid.NewGuid()}"))
                .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }
}
