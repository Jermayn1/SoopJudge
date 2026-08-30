using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.Infrastructure.Persistence;
using SoopWorkshop.Shared.DTOs.Auth.Requests;

namespace SoopWorkshop.Tests.Integration
{
    /// <summary>
    /// Basis für alle Tests gegen echte Datenbank und echte HTTP-Pipeline.
    /// Vor jedem Test ist die Datenbank leer.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "Integration")]
    public abstract class IntegrationTestBase(PostgresFixture fixture) : IAsyncLifetime
    {
        protected PostgresFixture Fixture { get; } = fixture;

        public Task InitializeAsync() => Fixture.ResetAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// Ein HTTP-Client gegen die Anwendung im Speicher.
        /// </summary>
        /// <remarks>
        /// Zwei Einstellungen, beide aus einem konkreten Grund:
        ///
        /// Weiterleitungen werden NICHT verfolgt. Ein [Authorize] ohne Anmeldung
        /// muss 401 liefern und nicht 302 auf eine Anmeldeseite, die es in einer
        /// API nicht gibt - folgte der Client der Weiterleitung, sähe der Test am
        /// Ende einen 404 und die eigentliche Aussage wäre weg.
        ///
        /// Die Basisadresse ist https, obwohl der TestServer gar kein TLS spricht.
        /// Das Anmelde-Cookie ist Secure, und der CookieContainer von .NET schickt
        /// ein Secure-Cookie über http grundsätzlich nicht zurück - auch nicht an
        /// localhost. Über http liefe deshalb jeder angemeldete Aufruf in einen
        /// 401, der wie ein kaputter Zugangsschutz aussieht und keiner ist. Über
        /// https verhält sich der Client wie ein Browser auf einem
        /// vertrauenswürdigen Ursprung.
        /// </remarks>
        protected HttpClient CreateClient() =>
            Fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });

        /// <summary>Ein Client, der sich vorher am Admin-Bereich angemeldet hat.</summary>
        protected async Task<HttpClient> CreateAdminClientAsync()
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/admin/auth/login",
                new AdminLoginDto { Password = SoopWorkshopFactory.AdminPassword });

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
            return client;
        }

        /// <summary>
        /// Führt etwas in einem eigenen DI-Scope aus - mit denselben
        /// Registrierungen, die die Anwendung benutzt.
        /// </summary>
        protected async Task WithScopeAsync(Func<IServiceProvider, Task> action)
        {
            using var scope = Fixture.Factory.Services.CreateScope();
            await action(scope.ServiceProvider);
        }

        /// <summary>
        /// Wie <see cref="WithScopeAsync(Func{IServiceProvider, Task})"/>, nur
        /// direkt mit dem DbContext.
        /// </summary>
        /// <remarks>
        /// Je Aufruf ein frischer Kontext, und das ist Absicht: wer zum Prüfen
        /// denselben Kontext benutzt, mit dem er geschrieben hat, bekommt die
        /// Antwort aus der Änderungsverfolgung statt aus der Datenbank. Genau so
        /// entgeht einem, dass gar nichts gespeichert wurde.
        /// </remarks>
        protected async Task WithDbAsync(Func<AppDbContext, Task> action)
        {
            using var scope = Fixture.Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await action(context);
        }
    }
}
