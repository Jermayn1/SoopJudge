using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using SoopWorkshop.Backend.API.Configuration;
using SoopWorkshop.Shared.DTOs.Auth;
using SoopWorkshop.Shared.DTOs.Auth.Requests;

namespace SoopWorkshop.Tests.Integration.Controllers
{
    /// <summary>
    /// Der Zugangsschutz vor allen api/admin/*-Endpunkten: Anmeldung, Cookie und
    /// die Abweisung ohne Anmeldung.
    /// </summary>
    public class AdminAuthenticationTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
    {
        // Die Endpunkte, an denen das HTTP-Verhalten geprüft wird. Bewusst eine
        // Stichprobe und NICHT die Zusicherung, dass überall [Authorize] steht -
        // die trägt JederAdminEndpunkt_... weiter unten.
        public static TheoryData<string, string> GeschuetzteEndpunkte => new()
        {
            { "GET", "/api/admin/auth/session" },
            { "GET", "/api/admin/categories" },
            { "GET", "/api/admin/tasks" },
            { "GET", "/api/admin/transfer/export" }
        };

        // Die einzigen Admin-Aktionen, die ohne Anmeldung erreichbar sein dürfen.
        // Kommt eine dazu, muss sie hier eingetragen werden - und genau dieser
        // Zwang ist der Zweck: eine bewusste Ausnahme wird aufgeschrieben, eine
        // vergessene fällt auf.
        private static readonly string[] AbsichtlichOhneAnmeldung =
        [
            "api/admin/auth/login",
            "api/admin/auth/logout"
        ];

        private List<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor> AdminAktionen() =>
            Fixture.Factory.Services
                .GetRequiredService<IActionDescriptorCollectionProvider>()
                .ActionDescriptors.Items
                .Where(a => a.AttributeRouteInfo?.Template?
                    .StartsWith("api/admin", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

        /// <summary>
        /// Prüft JEDE Aktion unter api/admin, nicht eine Auswahl davon.
        /// </summary>
        /// <remarks>
        /// Der Vorgänger dieses Tests war eine handgepflegte Liste geschützter
        /// Pfade. Sie deckte vier von sieben Admin-Controllern ab - die Endpunkte
        /// für Testfälle, JUnit-Dateien und Gewichte fehlten. Hätte jemand dort
        /// [Authorize] entfernt, wären alle Tests grün geblieben, während die
        /// JUnit-Dateien aller Aufgaben für jeden les- und überschreibbar
        /// gewesen wären, der den Port erreicht.
        ///
        /// Eine Liste geschützter Pfade fällt bei einem neuen Controller nach
        /// OFFEN aus. Diese Richtung fällt nach ZU.
        /// </remarks>
        [Fact]
        public void JederAdminEndpunkt_IstEntwederGeschuetztOderAusdruecklichOffen()
        {
            var adminAktionen = AdminAktionen();

            // Ohne diese Zusicherung könnte der Test grün sein, weil er gar
            // nichts gefunden hat - etwa nach einer Umbenennung der Route.
            adminAktionen.Count.ShouldBeGreaterThan(10);

            var ungeschuetzt = adminAktionen
                .Where(a => !a.EndpointMetadata.OfType<IAuthorizeData>().Any())
                .Select(a => a.AttributeRouteInfo!.Template!)
                .Where(t => !AbsichtlichOhneAnmeldung.Contains(t, StringComparer.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            ungeschuetzt.ShouldBeEmpty(
                "Diese Admin-Endpunkte tragen kein [Authorize] und stehen nicht in " +
                "AbsichtlichOhneAnmeldung: " + string.Join(", ", ungeschuetzt));
        }

        // Die Gegenrichtung: eine Ausnahme, die niemand mehr braucht, soll
        // auffallen statt liegenzubleiben.
        [Fact]
        public void KeineUnnoetigenAusnahmen_VomZugangsschutz()
        {
            var offen = AdminAktionen()
                .Where(a => a.EndpointMetadata.OfType<IAllowAnonymous>().Any())
                .Select(a => a.AttributeRouteInfo!.Template!)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            offen.ShouldBe(AbsichtlichOhneAnmeldung.OrderBy(t => t).ToList());
        }

        [Theory]
        [MemberData(nameof(GeschuetzteEndpunkte))]
        public async Task OhneAnmeldung_Liefert401(string methode, string pfad)
        {
            var response = await CreateClient().SendAsync(
                new HttpRequestMessage(new HttpMethod(methode), pfad));

            // 401, nicht 302. Die Cookie-Authentifizierung leitet standardmäßig
            // auf eine Anmeldeseite um, die es in einer API nicht gibt - der fetch
            // im Frontend würde ihr folgen und am Ende einen 404 auf
            // /Account/Login melden. OnRedirectToLogin ist deshalb umgebogen.
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            response.Headers.Location.ShouldBeNull();
        }

        [Fact]
        public async Task Anmelden_MitRichtigemPasswort_SetztDasCookie()
        {
            var client = CreateClient();

            var response = await client.PostAsJsonAsync(
                "/api/admin/auth/login",
                new AdminLoginDto { Password = SoopWorkshopFactory.AdminPassword });

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            var cookie = response.Headers
                .GetValues("Set-Cookie")
                .ShouldHaveSingleItem();

            cookie.ShouldContain(AdminAuthenticationExtensions.CookieName);

            // Der Grund, warum es überhaupt ein Cookie ist und kein Token im
            // Speicher der Seite: kein Zugriff aus JavaScript. Ein eingeschleustes
            // Skript kann den Zugang so nicht auslesen.
            cookie.ShouldContain("httponly", Case.Insensitive);

            // Die Tests laufen unter "Testing", also im BETRIEBSZWEIG der
            // Cookie-Politik - und damit gegen die Einstellung, die ausgeliefert
            // wird. Hinter dem Reverse Proxy liefern Frontend und API denselben
            // Ursprung aus; es gibt keine Origin-Grenze mehr zu überqueren, Lax
            // ist die engere und richtige Angabe.
            cookie.ShouldContain("samesite=lax", Case.Insensitive);

            // Secure, weil die Basisadresse https ist. Das ist keine feste
            // Zusicherung mehr, sondern SameAsRequest - siehe den Test darunter.
            cookie.ShouldContain("secure", Case.Insensitive);
        }

        /// <summary>
        /// Über http trägt dasselbe Cookie kein Secure - und kommt damit zurück.
        /// </summary>
        /// <remarks>
        /// Das ist der Fall, auf dem der ganze Betriebsaufbau steht: der Betreuer
        /// verwaltet von einem anderen Rechner im Netz, nicht vom Server selbst.
        /// Mit dem früheren CookieSecurePolicy.Always hätte der Browser das
        /// Cookie über http nie zurückgeschickt (localhost ist die Ausnahme, ein
        /// Rechner im LAN nicht) - die Anmeldung hätte kommentarlos nie
        /// funktioniert, und der Fehler hätte wie ein kaputter Server ausgesehen.
        ///
        /// SameAsRequest ist dabei kein Ausschalter: der Test darüber belegt,
        /// dass über https weiterhin Secure gesetzt wird. Sobald ein Zertifikat
        /// auf dem Server liegt, zieht die Absicherung ohne Codeänderung nach.
        /// </remarks>
        [Fact]
        public async Task Anmelden_UeberHttp_SetztKeinSecureUndDasCookieKommtZurueck()
        {
            using var client = Fixture.Factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    BaseAddress = new Uri("http://localhost")
                });

            var anmeldung = await client.PostAsJsonAsync(
                "/api/admin/auth/login",
                new AdminLoginDto { Password = SoopWorkshopFactory.AdminPassword });

            anmeldung.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            var cookie = anmeldung.Headers.GetValues("Set-Cookie").ShouldHaveSingleItem();
            cookie.ShouldNotContain("secure", Case.Insensitive);

            // Der eigentliche Beleg: nicht der Kopf, sondern dass der Zugang
            // damit auch wirklich trägt. Der CookieContainer von .NET verhält
            // sich hier wie ein Browser - ein Secure-Cookie wäre über http gar
            // nicht erst zurückgeschickt worden.
            var geschuetzt = await client.GetAsync("/api/admin/auth/session");
            geschuetzt.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Anmelden_MitFalschemPasswort_Liefert401UndSetztKeinCookie()
        {
            var response = await CreateClient().PostAsJsonAsync(
                "/api/admin/auth/login",
                new AdminLoginDto { Password = "falsch" });

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            response.Headers.Contains("Set-Cookie").ShouldBeFalse();

            (await response.Content.ReadAsStringAsync()).ShouldBe("Das Passwort stimmt nicht.");
        }

        [Theory]
        [MemberData(nameof(GeschuetzteEndpunkte))]
        public async Task NachAnmeldung_SindDieEndpunkteErreichbar(string methode, string pfad)
        {
            var client = await CreateAdminClientAsync();

            var response = await client.SendAsync(
                new HttpRequestMessage(new HttpMethod(methode), pfad));

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Session_NachAnmeldung_MeldetAngemeldet()
        {
            var client = await CreateAdminClientAsync();

            var session = await client.GetFromJsonAsync<AdminSessionDto>("/api/admin/auth/session");

            session.ShouldNotBeNull();
            session.IsAuthenticated.ShouldBeTrue();
        }

        [Fact]
        public async Task Abmelden_SchliesstDenZugangWieder()
        {
            var client = await CreateAdminClientAsync();

            (await client.PostAsync("/api/admin/auth/logout", null))
                .StatusCode.ShouldBe(HttpStatusCode.NoContent);

            (await client.GetAsync("/api/admin/auth/session"))
                .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Die öffentliche Seite darf von all dem nichts merken.
        [Fact]
        public async Task OeffentlicheEndpunkte_BrauchenKeineAnmeldung()
        {
            (await CreateClient().GetAsync("/api/categories"))
                .StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}
