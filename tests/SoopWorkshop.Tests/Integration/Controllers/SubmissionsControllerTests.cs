using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Shared.DTOs.Evaluation;
using SoopWorkshop.Shared.DTOs.Submissions;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Integration.Controllers
{
    public class SubmissionsControllerTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
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

        private static MultipartFormDataContent Formular(Guid taskItemId, params (string Name, string Inhalt)[] dateien)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent(taskItemId.ToString()), "taskItemId" }
            };

            foreach (var (name, inhalt) in dateien)
            {
                var datei = new ByteArrayContent(Encoding.UTF8.GetBytes(inhalt));
                datei.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                content.Add(datei, "files", name);
            }

            return content;
        }

        [Fact]
        public async Task Create_GueltigeAbgabe_LiefertDieAbgabeUndReihtSieEin()
        {
            var taskItemId = await GivenAufgabe();

            var response = await CreateClient().PostAsync(
                "/api/submissions",
                Formular(taskItemId, ("Konto.java", "public class Konto {}")));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var abgabe = await response.Content.ReadFromJsonAsync<SubmissionDto>();
            abgabe.ShouldNotBeNull();
            abgabe.TaskItemId.ShouldBe(taskItemId);

            await WithDbAsync(async db =>
            {
                var gespeichert = await db.Submissions
                    .Include(s => s.Files)
                    .SingleAsync(s => s.Id == abgabe.Id);

                gespeichert.Status.ShouldBe(SubmissionStatus.Pending);
                gespeichert.Files.ShouldHaveSingleItem().FileName.ShouldBe("Konto.java");
            });

            // Der zweite Teil des Testnamens, bis hierher unbelegt: Pending in der
            // Datenbank sagt nur, dass die Abgabe angekommen ist - nicht, dass sie
            // je in der Warteschlange landete. Ohne das Einreihen bliebe sie
            // liegen, und niemand sähe es.
            var warteschlange = (SoopWorkshopFactory.MitschreibendeWarteschlange)
                Fixture.Factory.Services.GetRequiredService<IEvaluationQueue>();

            warteschlange.Eingereiht.ShouldContain(abgabe.Id);
        }

        // Der Kern dieses Tests ist nicht der Statuscode, sondern der WORTLAUT
        // und der Inhaltstyp. Die API antwortet mit BadRequest("..."), also einem
        // nackten String, und ASP.NET wählt den Formatter nach der REIHENFOLGE im
        // Accept-Kopf. Steht application/json vorn, wird daraus
        // »"'notiz.txt' ist keine .java-Datei."« - JSON-kodiert samt
        // Anführungszeichen, und genau so läse es der Teilnehmer. Der Client
        // schickt deshalb text/plain zuerst.
        [Fact]
        public async Task Create_FalscheEndung_LiefertDenSatzAlsKlartext()
        {
            var taskItemId = await GivenAufgabe();

            var client = CreateClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("text/plain, application/json");

            var response = await client.PostAsync(
                "/api/submissions",
                Formular(taskItemId, ("notiz.txt", "kein Java")));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.ShouldBe("text/plain");

            var meldung = await response.Content.ReadAsStringAsync();
            meldung.ShouldBe("„notiz.txt“ ist keine .java-Datei.");
            meldung.ShouldNotStartWith("\"");
        }

        // Die Gegenprobe zum Test darüber, und der Beleg, dass die
        // Kopfreihenfolge im Client kein Zierrat ist: kehrt man sie um, wählt
        // ASP.NET den JSON-Formatter für denselben nackten String - und der
        // Teilnehmer läse Anführungszeichen mit.
        [Fact]
        public async Task Create_FalscheEndung_MitJsonZuerst_KommtDerSatzJsonKodiertAn()
        {
            var taskItemId = await GivenAufgabe();

            var client = CreateClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain");

            var response = await client.PostAsync(
                "/api/submissions",
                Formular(taskItemId, ("notiz.txt", "kein Java")));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");

            var roh = await response.Content.ReadAsStringAsync();
            roh.ShouldStartWith("\"");
            roh.ShouldEndWith("\"");
            roh.ShouldContain("ist keine .java-Datei.");
        }

        [Fact]
        public async Task Create_LeereDatei_WirdMitNamenAbgelehnt()
        {
            var taskItemId = await GivenAufgabe();

            var response = await CreateClient().PostAsync(
                "/api/submissions",
                Formular(taskItemId, ("Leer.java", string.Empty)));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).ShouldContain("„Leer.java“ ist leer.");
        }

        // Über den Browser nicht auslösbar - das Frontend blockt vorher. Genau
        // deshalb gehört der Fall hierher und nicht in die Klickanleitung.
        [Fact]
        public async Task Create_DateinameMitPfadanteil_WirdAbgelehnt()
        {
            var taskItemId = await GivenAufgabe();

            var response = await CreateClient().PostAsync(
                "/api/submissions",
                Formular(taskItemId, ("../Konto.java", "public class Konto {}")));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).ShouldContain("gültiger Dateiname");
        }

        [Fact]
        public async Task Create_DoppelterDateiname_WirdAbgelehnt()
        {
            var taskItemId = await GivenAufgabe();

            var response = await CreateClient().PostAsync(
                "/api/submissions",
                Formular(taskItemId,
                    ("Konto.java", "public class Konto {}"),
                    ("Konto.java", "public class Konto {}")));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).ShouldContain("mehrfach hochgeladen");
        }

        // Führte früher in die Fremdschlüsselbedingung und kam als 500 zurück.
        [Fact]
        public async Task Create_UnbekannteAufgabe_Liefert400StattServerfehler()
        {
            var response = await CreateClient().PostAsync(
                "/api/submissions",
                Formular(Guid.NewGuid(), ("Konto.java", "public class Konto {}")));

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        // --- Status und Ergebnis -----------------------------------------------

        [Fact]
        public async Task GetStatus_LiefertStandUndAufgabenId()
        {
            var taskItemId = await GivenAufgabe();
            var submission = PersistedDataFactory.Abgabe(taskItemId, SubmissionStatus.Running);

            await WithDbAsync(async db =>
            {
                db.Submissions.Add(submission);
                await db.SaveChangesAsync();
            });

            var stand = await CreateClient()
                .GetFromJsonAsync<SubmissionStatusDto>($"/api/submissions/{submission.Id}/status");

            stand.ShouldNotBeNull();
            stand.Status.ShouldBe(SubmissionStatus.Running);

            // Ohne dieses Feld führt der Zurück-Link der Ergebnisseite ins Leere.
            stand.TaskItemId.ShouldBe(taskItemId);
        }

        // "Gibt es nicht" und "Server nicht erreichbar" müssen im Frontend
        // unterscheidbar bleiben - dafür braucht es hier einen echten 404.
        [Fact]
        public async Task GetStatus_UnbekannteAbgabe_Liefert404()
        {
            (await CreateClient().GetAsync($"/api/submissions/{Guid.NewGuid()}/status"))
                .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetResult_OhneErgebnis_Liefert404()
        {
            var taskItemId = await GivenAufgabe();
            var submission = PersistedDataFactory.Abgabe(taskItemId, SubmissionStatus.Pending);

            await WithDbAsync(async db =>
            {
                db.Submissions.Add(submission);
                await db.SaveChangesAsync();
            });

            (await CreateClient().GetAsync($"/api/submissions/{submission.Id}/result"))
                .StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetResult_MitErgebnis_LiefertKategorienUndTeilpruefungen()
        {
            var taskItemId = await GivenAufgabe();
            var submission = PersistedDataFactory.Abgabe(taskItemId);

            await WithDbAsync(async db =>
            {
                db.Submissions.Add(submission);
                await db.SaveChangesAsync();

                db.EvaluationResults.Add(PersistedDataFactory.Ergebnis(submission.Id));
                await db.SaveChangesAsync();
            });

            var ergebnis = await CreateClient()
                .GetFromJsonAsync<EvaluationResultDto>($"/api/submissions/{submission.Id}/result");

            ergebnis.ShouldNotBeNull();
            ergebnis.TotalScore.ShouldBe(80);

            var kategorie = ergebnis.CategoryResults.ShouldHaveSingleItem();
            var teilpruefung = kategorie.TestCaseResults.ShouldHaveSingleItem();

            // Die Anzeige braucht Erwartet UND Erhalten gemeinsam (Paragraph 5.7).
            teilpruefung.ExpectedOutput.ShouldBe("Stand: 100");
            teilpruefung.ActualOutput.ShouldBe("Stand: 0");
        }
    }
}
