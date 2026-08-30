using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoopWorkshop.Backend.Application.Transfer.Interfaces;
using SoopWorkshop.Backend.Infrastructure.Persistence;
using SoopWorkshop.Shared.DTOs.Transfer;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Integration
{
    /// <summary>
    /// Der Bestands-Transfer: Rundlauf, idempotentes Merge, kaskadierendes
    /// Replace und der Rollback. Er ist die einzige Stelle im Projekt mit einer
    /// Transaktion und braucht deshalb eine echte Datenbank.
    /// </summary>
    public class TaskTransferServiceTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private static ITaskTransferService Service(IServiceProvider services) =>
            services.GetRequiredService<ITaskTransferService>();

        private async Task GivenBestand()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();
            });
        }

        private async Task<TaskBundleDto> ExportiereAsync()
        {
            TaskBundleDto bundle = null!;

            await WithScopeAsync(async services =>
            {
                var result = await Service(services).ExportAsync(CancellationToken.None);
                result.IsSuccess.ShouldBeTrue();
                bundle = result.Value!;
            });

            return bundle;
        }

        private async Task<ImportReportDto> ImportiereAsync(TaskBundleDto bundle, ImportMode mode)
        {
            ImportReportDto report = null!;

            await WithScopeAsync(async services =>
            {
                var result = await Service(services).ImportAsync(bundle, mode, CancellationToken.None);
                result.IsSuccess.ShouldBeTrue(result.ErrorMessage);
                report = result.Value!;
            });

            return report;
        }

        // --- Rundlauf ----------------------------------------------------------

        [Fact]
        public async Task Export_NimmtDenGanzenBaumMit()
        {
            await GivenBestand();

            var bundle = await ExportiereAsync();

            bundle.FormatVersion.ShouldNotBe(0);

            var kategorie = bundle.Categories.ShouldHaveSingleItem();
            kategorie.Name.ShouldBe("OOP");
            kategorie.IconName.ShouldBe("Layers");

            var aufgabe = kategorie.Tasks.ShouldHaveSingleItem();
            aufgabe.Title.ShouldBe("Bankkonto");
            aufgabe.Hints.ShouldHaveSingleItem();
            aufgabe.Tests.ShouldHaveSingleItem();
            aufgabe.UnitTestFiles.ShouldHaveSingleItem().FileName.ShouldBe("KontoTest.java");
            aufgabe.Weights.ShouldHaveSingleItem();
            aufgabe.ExpectedTypes.ShouldHaveSingleItem()
                .Methods.ShouldHaveSingleItem().ShouldBe("void einzahlen(int betrag)");
        }

        // Abgaben sind Workshop-Daten, keine Konfiguration - sie gehören nicht in
        // die Datei. Stünden sie drin, trüge man mit dem Aufgabenbestand die
        // Lösungen der Teilnehmer durch die Gegend.
        [Fact]
        public async Task Export_NimmtKeineAbgabenMit()
        {
            await GivenBestand();

            await WithDbAsync(async db =>
            {
                var taskId = (await db.TaskItems.SingleAsync()).Id;
                db.Submissions.Add(PersistedDataFactory.Abgabe(taskId));
                await db.SaveChangesAsync();
            });

            var bundle = await ExportiereAsync();

            // Im Bundle-Format gibt es gar kein Feld dafür - der Beleg ist, dass
            // die Abgabe im Bestand steht und der Export sie unbeeindruckt lässt.
            await WithDbAsync(async db => (await db.Submissions.CountAsync()).ShouldBe(1));
            bundle.Categories.ShouldHaveSingleItem().Tasks.ShouldHaveSingleItem();
        }

        [Fact]
        public async Task ExportUndImport_InLeereDatenbank_StelltDenBestandWiederHer()
        {
            await GivenBestand();
            var bundle = await ExportiereAsync();

            await WithDbAsync(async db =>
            {
                db.TaskCategories.RemoveRange(await db.TaskCategories.ToListAsync());
                await db.SaveChangesAsync();
            });

            var report = await ImportiereAsync(bundle, ImportMode.Merge);

            report.IsValid.ShouldBeTrue();
            report.CategoriesCreated.ShouldBe(1);
            report.TasksCreated.ShouldBe(1);

            // Der Beleg ist nicht die Anzahl, sondern die Gleichheit: ein zweiter
            // Export muss dasselbe liefern wie der erste.
            var nachher = await ExportiereAsync();
            Vergleiche(nachher, bundle);
        }

        // --- Merge -------------------------------------------------------------

        // Die GUIDs wandern mit. Ohne das legte jeder erneute Import denselben
        // Bestand ein zweites Mal an, statt ihn zu aktualisieren.
        [Fact]
        public async Task Merge_ZweimalImportiert_VerdoppeltNichts()
        {
            await GivenBestand();
            var bundle = await ExportiereAsync();

            await ImportiereAsync(bundle, ImportMode.Merge);
            var zweiter = await ImportiereAsync(bundle, ImportMode.Merge);

            zweiter.CategoriesCreated.ShouldBe(0);
            zweiter.CategoriesUpdated.ShouldBe(1);
            zweiter.TasksCreated.ShouldBe(0);
            zweiter.TasksUpdated.ShouldBe(1);

            await WithDbAsync(async db =>
            {
                (await db.TaskCategories.CountAsync()).ShouldBe(1);
                (await db.TaskItems.CountAsync()).ShouldBe(1);

                // Auch die Kinder dürfen sich nicht häufen: sie werden ersetzt,
                // nicht ergänzt.
                (await db.TaskTests.CountAsync()).ShouldBe(1);
                (await db.TaskUnitTestFiles.CountAsync()).ShouldBe(1);
                (await db.TaskHints.CountAsync()).ShouldBe(1);
                (await db.TaskCategoryWeights.CountAsync()).ShouldBe(1);
                (await db.TaskExpectedTypes.CountAsync()).ShouldBe(1);
                (await db.TaskExpectedMethods.CountAsync()).ShouldBe(1);
            });
        }

        [Fact]
        public async Task Merge_GeaenderteDatei_UebernimmtDieAenderung()
        {
            await GivenBestand();
            var bundle = await ExportiereAsync();

            bundle.Categories[0].Tasks[0].Title = "Bankkonto (ueberarbeitet)";
            bundle.Categories[0].Tasks[0].Hints = ["Erster Tipp", "Zweiter Tipp"];

            await ImportiereAsync(bundle, ImportMode.Merge);

            await WithDbAsync(async db =>
            {
                (await db.TaskItems.SingleAsync()).Title.ShouldBe("Bankkonto (ueberarbeitet)");
                (await db.TaskHints.CountAsync()).ShouldBe(2);
            });
        }

        // Merge fasst nur an, was in der Datei steht. Ein Bestand, den die Datei
        // nicht kennt, bleibt stehen - genau das unterscheidet Merge von Replace.
        [Fact]
        public async Task Merge_LaesstUnbekanntenBestandStehen()
        {
            await GivenBestand();
            var bundle = await ExportiereAsync();

            var fremd = PersistedDataFactory.VollstaendigeKategorie("Bleibt stehen");
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(fremd);
                await db.SaveChangesAsync();
            });

            await ImportiereAsync(bundle, ImportMode.Merge);

            await WithDbAsync(async db =>
                (await db.TaskCategories.CountAsync()).ShouldBe(2));
        }

        // --- Replace -----------------------------------------------------------

        [Fact]
        public async Task Replace_LoeschtWasDieDateiNichtKennt_UndDieAbgabenDazu()
        {
            await GivenBestand();
            var bundle = await ExportiereAsync();

            var fremd = PersistedDataFactory.VollstaendigeKategorie("Faellt weg");
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(fremd);
                await db.SaveChangesAsync();

                db.Submissions.Add(PersistedDataFactory.Abgabe(fremd.Tasks.Single().Id));
                await db.SaveChangesAsync();
            });

            var report = await ImportiereAsync(bundle, ImportMode.Replace);

            // Beim Ersetzen geht der GESAMTE Bestand weg und die Datei kommt
            // komplett neu herein - auch die Kategorie, die in der Datei steht,
            // wird gelöscht und wieder angelegt statt aktualisiert. Der Bericht
            // sagt das ehrlich: 2 gelöscht, 1 angelegt, nicht "1 gelöscht,
            // 1 aktualisiert".
            report.CategoriesDeleted.ShouldBe(2);
            report.TasksDeleted.ShouldBe(2);
            report.CategoriesCreated.ShouldBe(1);
            report.TasksCreated.ShouldBe(1);

            // Die Zahl, auf die es im Bestätigungsdialog ankommt.
            report.SubmissionsDeleted.ShouldBe(1);
            report.Warnings.ShouldContain(warnung => warnung.Contains("abgegebene"));

            await WithDbAsync(async db =>
            {
                (await db.TaskCategories.CountAsync()).ShouldBe(1);
                (await db.TaskCategories.SingleAsync()).Name.ShouldBe("OOP");
                (await db.Submissions.CountAsync()).ShouldBe(0);
                (await db.SubmissionFiles.CountAsync()).ShouldBe(0);
            });
        }

        // Der Dialog nennt vor dem Ausführen die Zahl der Abgaben, die mitgehen.
        // Sähe die Vorschau etwas anderes als der Import, wäre die Bestätigung
        // wertlos - man klickt sie im Vertrauen auf diese Zahl.
        [Fact]
        public async Task Vorschau_SagtDasselbeVoraus_WasDerImportDannTut()
        {
            await GivenBestand();
            var bundle = await ExportiereAsync();

            var fremd = PersistedDataFactory.VollstaendigeKategorie("Faellt weg");
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(fremd);
                await db.SaveChangesAsync();

                db.Submissions.Add(PersistedDataFactory.Abgabe(fremd.Tasks.Single().Id));
                await db.SaveChangesAsync();
            });

            ImportReportDto vorschau = null!;
            await WithScopeAsync(async services =>
            {
                var result = await Service(services).PreviewAsync(bundle, ImportMode.Replace, CancellationToken.None);
                vorschau = result.Value!;
            });

            var tatsaechlich = await ImportiereAsync(bundle, ImportMode.Replace);

            vorschau.CategoriesDeleted.ShouldBe(tatsaechlich.CategoriesDeleted);
            vorschau.TasksDeleted.ShouldBe(tatsaechlich.TasksDeleted);
            vorschau.SubmissionsDeleted.ShouldBe(tatsaechlich.SubmissionsDeleted);
            vorschau.CategoriesUpdated.ShouldBe(tatsaechlich.CategoriesUpdated);
            vorschau.TasksUpdated.ShouldBe(tatsaechlich.TasksUpdated);
        }

        [Fact]
        public async Task Vorschau_SchreibtNichts()
        {
            await GivenBestand();
            var bundle = await ExportiereAsync();

            bundle.Categories[0].Tasks[0].Title = "Wird nicht uebernommen";

            await WithScopeAsync(services =>
                Service(services).PreviewAsync(bundle, ImportMode.Replace, CancellationToken.None));

            await WithDbAsync(async db =>
            {
                (await db.TaskCategories.CountAsync()).ShouldBe(1);
                (await db.TaskItems.SingleAsync()).Title.ShouldBe("Bankkonto");
            });
        }

        // --- Ungültige Datei --------------------------------------------------

        // Der Validator ist als reine Funktion schon abgedeckt. Hier geht es nur
        // um eines: dass eine abgelehnte Datei die Datenbank nicht anfasst.
        [Fact]
        public async Task Import_UngueltigeDatei_MeldetFehlerUndSchreibtNichts()
        {
            await GivenBestand();

            var kaputt = new TaskBundleDto
            {
                FormatVersion = 999,
                Categories = []
            };

            var report = await ImportiereAsync(kaputt, ImportMode.Replace);

            report.IsValid.ShouldBeFalse();
            report.Errors.ShouldNotBeEmpty();

            // Entscheidend: Replace hätte ohne die Prüfung alles gelöscht.
            await WithDbAsync(async db =>
            {
                (await db.TaskCategories.CountAsync()).ShouldBe(1);
                (await db.TaskItems.CountAsync()).ShouldBe(1);
            });
        }

        // --- Rollback ----------------------------------------------------------

        // Der einzige Grund, warum die Transaktion existiert - und der Test, der
        // mit EF InMemory grün gewesen wäre, ohne irgendetwas zu belegen.
        //
        // Der Fehler wird von außen hineingeschoben: der Validator deckt sich mit
        // den Spaltengrenzen der Datenbank, es gibt also keine gültige Datei, die
        // erst beim Schreiben scheitert. Genau deshalb ein Interceptor - er trifft
        // den Moment, auf den es ankommt: Replace hat bereits ALLES gelöscht und
        // schreibt gerade den neuen Bestand. Ohne Transaktion stünde danach eine
        // leere Datenbank da.
        [Fact]
        public async Task Import_ScheitertNachDemLoeschen_LaesstDenBestandUnveraendert()
        {
            await GivenBestand();
            var bundle = await ExportiereAsync();

            var vorher = await BestandAsync();

            // Den Kontext ausdrücklich neu registrieren, statt nur einen
            // IInterceptor in die DI zu legen: so hängt der Test nicht daran, ob
            // EF Interceptoren aus dem Anwendungscontainer einsammelt.
            using var factory = Fixture.Factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<DbContextOptions>();

                    services.AddDbContext<AppDbContext>(options => options
                        .UseNpgsql(Fixture.ConnectionString)
                        .AddInterceptors(new FehlerBeimZweitenSpeichern()));
                }));

            using (var scope = factory.Services.CreateScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<ITaskTransferService>();
                var result = await service.ImportAsync(bundle, ImportMode.Replace, CancellationToken.None);

                // Kein stiller Fehlschlag: der Aufrufer erfährt, dass nichts
                // geschrieben wurde.
                result.IsSuccess.ShouldBeFalse();
                result.ErrorMessage.ShouldContain("nichts geändert");
            }

            (await BestandAsync()).ShouldBe(vorher);
        }

        private async Task<(int Kategorien, int Aufgaben, int Testfaelle, int JUnitDateien)> BestandAsync()
        {
            (int, int, int, int) zahlen = default;

            await WithDbAsync(async db => zahlen = (
                await db.TaskCategories.CountAsync(),
                await db.TaskItems.CountAsync(),
                await db.TaskTests.CountAsync(),
                await db.TaskUnitTestFiles.CountAsync()));

            return zahlen;
        }

        /// <summary>
        /// Lässt das erste SaveChanges durch (die Löschungen) und wirft beim
        /// zweiten (den Einfügungen).
        /// </summary>
        private sealed class FehlerBeimZweitenSpeichern : SaveChangesInterceptor
        {
            private int _aufrufe;

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                if (++_aufrufe == 2)
                    throw new InvalidOperationException("Absichtlicher Fehler mitten im Import.");

                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }

        private static void Vergleiche(TaskBundleDto ist, TaskBundleDto soll)
        {
            ist.Categories.Count.ShouldBe(soll.Categories.Count);

            foreach (var (a, b) in ist.Categories.Zip(soll.Categories))
            {
                a.Id.ShouldBe(b.Id);
                a.Name.ShouldBe(b.Name);
                a.Order.ShouldBe(b.Order);
                a.IsVisible.ShouldBe(b.IsVisible);
                a.IconName.ShouldBe(b.IconName);
                a.Tasks.Count.ShouldBe(b.Tasks.Count);

                foreach (var (x, y) in a.Tasks.Zip(b.Tasks))
                {
                    x.Id.ShouldBe(y.Id);
                    x.Title.ShouldBe(y.Title);
                    x.Description.ShouldBe(y.Description);
                    x.Difficulty.ShouldBe(y.Difficulty);
                    x.Order.ShouldBe(y.Order);
                    x.IsVisible.ShouldBe(y.IsVisible);
                    x.EvaluationMode.ShouldBe(y.EvaluationMode);
                    x.Hints.ShouldBe(y.Hints);
                    x.Tests.Select(t => t.Description).ShouldBe(y.Tests.Select(t => t.Description));
                    x.UnitTestFiles.Select(f => f.FileName).ShouldBe(y.UnitTestFiles.Select(f => f.FileName));
                    x.UnitTestFiles.Select(f => f.Content).ShouldBe(y.UnitTestFiles.Select(f => f.Content));
                    x.Weights.Select(w => w.Category).ShouldBe(y.Weights.Select(w => w.Category));
                    x.Weights.Select(w => w.Weight).ShouldBe(y.Weights.Select(w => w.Weight));
                    x.ExpectedTypes.Select(t => t.Name).ShouldBe(y.ExpectedTypes.Select(t => t.Name));
                    x.ExpectedTypes.SelectMany(t => t.Methods)
                        .ShouldBe(y.ExpectedTypes.SelectMany(t => t.Methods));
                }
            }
        }
    }
}
