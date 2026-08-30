using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks;

namespace SoopWorkshop.Tests.Integration
{
    /// <summary>
    /// Belegt, dass das Fundament der Integrationstests trägt: Container,
    /// Migrationen, Anwendung im Speicher und das Aufräumen zwischen den Tests.
    /// Fällt hier etwas, ist jeder andere Integrationstest wertlos - deshalb
    /// steht es getrennt und nicht als Vorbedingung irgendwo mit drin.
    /// </summary>
    public class FoundationTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        [Fact]
        public async Task Anwendung_AntwortetAufDenOeffentlichenEndpunkt()
        {
            var response = await CreateClient().GetAsync("/api/categories");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var categories = await response.Content.ReadFromJsonAsync<List<TaskCategoryDto>>();
            categories.ShouldNotBeNull();
        }

        [Fact]
        public async Task Datenbank_IstMigriertUndSchreibbar()
        {
            var id = Guid.NewGuid();

            await WithDbAsync(async db =>
            {
                db.TaskCategories.Add(new TaskCategory { Id = id, Name = "Fundament", Order = 1 });
                await db.SaveChangesAsync();
            });

            // Bewusst ein zweiter Kontext: der erste hätte die Kategorie noch in
            // der Änderungsverfolgung und würde sie auch dann liefern, wenn
            // nichts in der Datenbank gelandet wäre.
            await WithDbAsync(async db =>
            {
                var gespeichert = await db.TaskCategories.SingleOrDefaultAsync(c => c.Id == id);
                gespeichert.ShouldNotBeNull();
                gespeichert.Name.ShouldBe("Fundament");
            });
        }

        // Diese beiden Tests gehören zusammen: sie legen dieselbe Kategorie an
        // und würden sich am eindeutigen Schlüssel stoßen, wenn Respawn
        // zwischen ihnen nicht aufräumte. Fällt einer davon, ist jeder spätere
        // Test von seiner Ausführungsreihenfolge abhängig.
        [Fact]
        public Task Aufraeumen_LaeuftVorJedemTest_ErsterDurchgang() => LegeStandardkategorieAn();

        [Fact]
        public Task Aufraeumen_LaeuftVorJedemTest_ZweiterDurchgang() => LegeStandardkategorieAn();

        private async Task LegeStandardkategorieAn()
        {
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

            await WithDbAsync(async db =>
            {
                (await db.TaskCategories.CountAsync()).ShouldBe(0);

                db.TaskCategories.Add(new TaskCategory { Id = id, Name = "Immer dieselbe", Order = 1 });
                await db.SaveChangesAsync();
            });
        }
    }
}
