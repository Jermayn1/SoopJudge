using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Backend.Infrastructure.Persistence;

namespace SoopWorkshop.Tests.Integration.Repositories
{
    public class TaskItemRepositoryTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        private async Task<TaskItem> GivenVollstaendigeAufgabe()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);
                await db.SaveChangesAsync();
            });

            return category.Tasks.Single();
        }

        private static ITaskItemRepository Repo(IServiceProvider services) =>
            services.GetRequiredService<ITaskItemRepository>();

        // Der Kern dieser Etappe. Was GetByIdAsync nicht mitlädt, sieht die
        // Auswertung als "nicht vorhanden" und bewertet entsprechend - beim
        // Ergänzen von CategoryWeights war das die stillste denkbare
        // Fehlerquelle. Deshalb wird JEDE Navigation einzeln geprüft: fällt
        // eine weg, fällt genau eine Zusicherung.
        [Fact]
        public async Task GetByIdAsync_LaedtJedeNavigationMit()
        {
            var task = await GivenVollstaendigeAufgabe();

            await WithScopeAsync(async services =>
            {
                var geladen = await Repo(services).GetByIdAsync(task.Id);

                geladen.ShouldNotBeNull();
                geladen.Hints.ShouldNotBeEmpty();
                geladen.Tests.ShouldNotBeEmpty();
                geladen.UnitTestFiles.ShouldNotBeEmpty();
                geladen.CategoryWeights.ShouldNotBeEmpty();

                var typ = geladen.ExpectedTypes.ShouldHaveSingleItem();
                typ.Name.ShouldBe("Konto");

                // Zwei Ebenen tief: ohne ThenInclude käme der Typ ohne seine
                // Methoden, und der ContractChecker fünde die Methode nie.
                typ.Methods.ShouldHaveSingleItem().Name.ShouldBe("einzahlen");
            });
        }

        [Fact]
        public async Task GetByIdAsync_UnbekannteId_LiefertNull()
        {
            await WithScopeAsync(async services =>
                (await Repo(services).GetByIdAsync(Guid.NewGuid())).ShouldBeNull());
        }

        // Gefragt wird die Datenbank, nicht die Änderungsverfolgung: nach
        // SaveChanges steht dort ohnehin wieder alles auf Unchanged, eine
        // Zustandsprüfung danach wäre still wirkungslos. PostgreSQL führt zu
        // jeder Zeile ein xmin - die Nummer der Transaktion, die sie zuletzt
        // geschrieben hat. Ändert sie sich, wurde die Zeile angefasst.
        //
        // Das ist der Weg, den der Aufgaben-Editor geht: TaskItemService lädt
        // über GetByIdAsync und reicht dieselbe Entität weiter.
        [Fact]
        public async Task UpdateAsync_AufVerfolgterAufgabe_SchreibtDieKindzeilenNichtNeu()
        {
            var task = await GivenVollstaendigeAufgabe();

            var vorher = await ZeilenstempelAsync();

            await WithScopeAsync(async services =>
            {
                var repository = Repo(services);

                var geladen = await repository.GetByIdAsync(task.Id);
                geladen!.Title = "Neuer Titel";

                await repository.UpdateAsync(geladen);
            });

            var nachher = await ZeilenstempelAsync();

            // Gegenprobe im Test selbst: hätte sich gar nichts geschrieben,
            // wäre die Zusicherung unten wertlos.
            nachher.Aufgabe.ShouldNotBe(vorher.Aufgabe);

            nachher.Testfaelle.ShouldBe(vorher.Testfaelle);
            nachher.JUnitDateien.ShouldBe(vorher.JUnitDateien);
            nachher.Gewichte.ShouldBe(vorher.Gewichte);

            await WithDbAsync(async db =>
                (await db.TaskItems.SingleAsync(t => t.Id == task.Id)).Title.ShouldBe("Neuer Titel"));
        }

        private async Task<(List<string> Aufgabe, List<string> Testfaelle,
            List<string> JUnitDateien, List<string> Gewichte)> ZeilenstempelAsync()
        {
            List<string> aufgabe = [], testfaelle = [], junit = [], gewichte = [];

            // Die vier Abfragen stehen ausgeschrieben da, statt den Tabellennamen
            // hereinzureichen: ein Tabellenname ist ein Bezeichner und lässt sich
            // nicht als Parameter binden. Zusammengesetztes SQL wäre hier zwar
            // harmlos, aber EF beanstandet es zu Recht pauschal (EF1002), und eine
            // unterdrückte Warnung ist teurer als vier Zeilen.
            await WithDbAsync(async db =>
            {
                aufgabe = await db.Database
                    .SqlQuery<string>($"""SELECT xmin::text AS "Value" FROM "TaskItems" ORDER BY "Id" """)
                    .ToListAsync();
                testfaelle = await db.Database
                    .SqlQuery<string>($"""SELECT xmin::text AS "Value" FROM "TaskTests" ORDER BY "Id" """)
                    .ToListAsync();
                junit = await db.Database
                    .SqlQuery<string>($"""SELECT xmin::text AS "Value" FROM "TaskUnitTestFiles" ORDER BY "Id" """)
                    .ToListAsync();
                gewichte = await db.Database
                    .SqlQuery<string>($"""SELECT xmin::text AS "Value" FROM "TaskCategoryWeights" ORDER BY "Id" """)
                    .ToListAsync();
            });

            return (aufgabe, testfaelle, junit, gewichte);
        }

        // Der gefährliche der beiden Fälle: bei einer LOSGELÖSTEN Aufgabe färbt
        // Update() den ganzen mitgegebenen Graphen auf Modified, jede Kindzeile
        // wird also neu geschrieben. Bei einer bereits verfolgten Aufgabe hält
        // EFs Graph-Traversierung dagegen an den verfolgten Knoten an und es
        // bleibt bei einer Zeile - der Gegentest steht daneben.
        //
        // Aktuell trifft das niemanden: der Editor reicht immer die verfolgte
        // Entität weiter. Wer das ändert, sieht die Folge hier.
        [Fact]
        public async Task UpdateAsync_AufLosgeloesterAufgabeMitKindern_SchreibtDieKindzeilenNeu()
        {
            var task = await GivenVollstaendigeAufgabe();
            var vorher = await ZeilenstempelAsync();

            await WithScopeAsync(async services =>
            {
                TaskItem losgeloest;

                using (var ladeScope = Fixture.Factory.Services.CreateScope())
                {
                    var ladeDb = ladeScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    losgeloest = await ladeDb.TaskItems
                        .Include(t => t.Tests)
                        .Include(t => t.UnitTestFiles)
                        .Include(t => t.CategoryWeights)
                        .AsNoTracking()
                        .SingleAsync(t => t.Id == task.Id);
                }

                losgeloest.Title = "Von aussen mitsamt Kindern";
                await Repo(services).UpdateAsync(losgeloest);
            });

            var nachher = await ZeilenstempelAsync();

            nachher.Aufgabe.ShouldNotBe(vorher.Aufgabe);
            nachher.Testfaelle.ShouldNotBe(vorher.Testfaelle);
            nachher.JUnitDateien.ShouldNotBe(vorher.JUnitDateien);
            nachher.Gewichte.ShouldNotBe(vorher.Gewichte);
        }

        // Eine losgelöste Entität kennt die Änderungsverfolgung nicht - hier
        // ist Update() nötig, sonst geht das Speichern still ins Leere.
        [Fact]
        public async Task UpdateAsync_AufLosgeloesterAufgabe_SpeichertTrotzdem()
        {
            var task = await GivenVollstaendigeAufgabe();

            await WithScopeAsync(async services =>
            {
                var losgeloest = new TaskItem
                {
                    Id = task.Id,
                    TaskCategoryId = task.TaskCategoryId,
                    Title = "Von aussen geaendert",
                    Description = task.Description,
                    Difficulty = task.Difficulty,
                    Order = task.Order,
                    IsVisible = task.IsVisible,
                    EvaluationMode = task.EvaluationMode
                };

                await Repo(services).UpdateAsync(losgeloest);
            });

            await WithDbAsync(async db =>
                (await db.TaskItems.SingleAsync(t => t.Id == task.Id))
                    .Title.ShouldBe("Von aussen geaendert"));
        }

        [Fact]
        public async Task DeleteAsync_LoeschtAufgabeUndAlleKinder()
        {
            var task = await GivenVollstaendigeAufgabe();

            await WithScopeAsync(services => Repo(services).DeleteAsync(task.Id));

            await WithDbAsync(async db =>
            {
                (await db.TaskItems.CountAsync()).ShouldBe(0);
                (await db.TaskTests.CountAsync()).ShouldBe(0);
                (await db.TaskUnitTestFiles.CountAsync()).ShouldBe(0);
                (await db.TaskCategoryWeights.CountAsync()).ShouldBe(0);
                (await db.TaskExpectedTypes.CountAsync()).ShouldBe(0);
                (await db.TaskExpectedMethods.CountAsync()).ShouldBe(0);

                // Die Kategorie bleibt - die Kaskade läuft nur nach unten.
                (await db.TaskCategories.CountAsync()).ShouldBe(1);
            });
        }

        [Fact]
        public async Task ExistsAsync_UnterscheidetVorhandenVonNicht()
        {
            var task = await GivenVollstaendigeAufgabe();

            await WithScopeAsync(async services =>
            {
                var repository = Repo(services);
                (await repository.ExistsAsync(task.Id, CancellationToken.None)).ShouldBeTrue();
                (await repository.ExistsAsync(Guid.NewGuid(), CancellationToken.None)).ShouldBeFalse();
            });
        }

        [Fact]
        public async Task GetAllAsync_LiefertNachOrderSortiert()
        {
            var category = PersistedDataFactory.VollstaendigeKategorie();
            var categoryId = category.Id;

            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(category);

                var zweite = PersistedDataFactory.VollstaendigeAufgabe(categoryId, "Zuletzt");
                zweite.Order = 9;
                var erste = PersistedDataFactory.VollstaendigeAufgabe(categoryId, "Zuerst");
                erste.Order = 0;

                db.TaskItems.AddRange(zweite, erste);
                await db.SaveChangesAsync();
            });

            await WithScopeAsync(async services =>
            {
                var alle = await Repo(services).GetAllAsync();
                alle.Select(t => t.Order).ShouldBeInOrder();
                alle.First().Title.ShouldBe("Zuerst");
                alle.Last().Title.ShouldBe("Zuletzt");
            });
        }
    }
}
