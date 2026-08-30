using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;

namespace SoopWorkshop.Tests.Integration.Controllers
{
    /// <summary>
    /// Was der Aufrufer sieht, wenn etwas durchrutscht, das niemand vorhergesehen
    /// hat. 33 Zeilen Produktivcode, bis hierher ohne einen einzigen Test.
    /// </summary>
    public class ExceptionMiddlewareTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        [Fact]
        public async Task UnbehandelteAusnahme_Liefert500MitFehlerobjektStattStacktrace()
        {
            var kaputt = Substitute.For<ITaskCategoryService>();
            kaputt.GetAllVisibleAsync().Throws(new InvalidOperationException(
                "Geheimer Innenausbau, den niemand von aussen sehen darf."));

            using var factory = Fixture.Factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ITaskCategoryService>();
                    services.AddScoped(_ => kaputt);
                }));

            using var client = factory.CreateClient(new()
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });

            var response = await client.GetAsync("/api/categories");

            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");

            var koerper = await response.Content.ReadFromJsonAsync<Fehlerobjekt>();
            koerper.ShouldNotBeNull();
            koerper.Error.ShouldBe("Ein unerwarteter Fehler ist aufgetreten.");

            // Nichts aus dem Inneren nach außen: weder die Meldung der Ausnahme
            // noch ihr Stacktrace. Der Grund steht im Protokoll des Servers.
            var roh = await response.Content.ReadAsStringAsync();
            roh.ShouldNotContain("Geheimer Innenausbau");
            roh.ShouldNotContain("InvalidOperationException");
        }

        private sealed record Fehlerobjekt(string Error);
    }
}
