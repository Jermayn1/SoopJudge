using Microsoft.AspNetCore.Authentication.Cookies;

namespace SoopWorkshop.Backend.API.Configuration
{
    public static class AdminAuthenticationExtensions
    {
        // Eigener Name statt des Standards. Der Standardname nennt die
        // eingesetzte Technik, und diesen hier findet man beim Nachsehen
        // in den Entwicklerwerkzeugen sofort wieder.
        public const string CookieName = "soop.admin";

        public static IServiceCollection AddAdminAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var section = configuration.GetSection(AdminOptions.SectionName);

            // Gleiche Haltung wie beim Connection-String: lieber ein Start, der
            // mit einem Satz abbricht, als einer, der still ohne Schutz läuft.
            if (string.IsNullOrWhiteSpace(section["Password"]))
            {
                throw new InvalidOperationException(
                    "Es ist kein Admin-Passwort gesetzt." + Environment.NewLine +
                    "Lokal: Admin__Password in der .env im Repository-Wurzelverzeichnis setzen" + Environment.NewLine +
                    "(Vorlage: .env.example). Im Betrieb ueber die Umgebungsvariable Admin__Password." + Environment.NewLine +
                    "Ohne Passwort waere api/admin/* fuer jeden offen, der den Port erreicht.");
            }

            services.Configure<AdminOptions>(section);

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = CookieName;

                    // Kein Zugriff aus JavaScript. Genau deshalb ein Cookie und
                    // kein Token im Speicher der Seite: ein eingeschleustes
                    // Skript kann den Zugang so nicht auslesen.
                    options.Cookie.HttpOnly = true;

                    // Die beiden Betriebsformen beschreiben verschiedene Lagen,
                    // und das Cookie soll die tatsächliche beschreiben statt
                    // eine davon zu behaupten.
                    //
                    // ENTWICKLUNG: Frontend (5173) und API (5120) sind
                    // verschiedene Origins, das Cookie überquert eine
                    // Origin-Grenze. Das verlangt SameSite=None, und None
                    // verlangt Secure. Browser behandeln localhost als
                    // vertrauenswürdigen Ursprung und nehmen Secure-Cookies
                    // dort auch über http an.
                    //
                    // BETRIEB: hinter dem Reverse Proxy liefern Frontend und API
                    // denselben Ursprung aus, es gibt also keine Grenze mehr zu
                    // überqueren - Lax genügt und ist die engere Angabe.
                    //
                    // SameAsRequest ist KEIN Ausschalter: über https ist das
                    // Cookie Secure, über http nicht. Es sagt die Wahrheit über
                    // die Verbindung, statt eine Zusicherung zu behaupten, die
                    // sie nicht hergibt. Voraussetzung dafür ist
                    // UseForwardedHeaders in Program.cs - ohne das sähe das
                    // Backend hinter dem Proxy immer http.
                    //
                    // Warum überhaupt: der Betreuer verwaltet von einem anderen
                    // Rechner im Netz. Mit Always wäre das Cookie dort über
                    // http nie zurückgekommen, und die Anmeldung hätte
                    // kommentarlos nie funktioniert.
                    if (environment.IsDevelopment())
                    {
                        options.Cookie.SameSite = SameSiteMode.None;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    }
                    else
                    {
                        options.Cookie.SameSite = SameSiteMode.Lax;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    }

                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;

                    // Ohne diese beiden antwortet [Authorize] mit 302 auf eine
                    // Anmeldeseite, die es in einer API nicht gibt. Für einen
                    // Aufruf aus JavaScript ist die Weiterleitung eine falsche
                    // Auskunft: der Endpunkt ist nicht umgezogen, er ist nicht
                    // erlaubt. Der fetch würde ihr folgen und am Ende einen
                    // 404 auf /Account/Login melden.
                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    };

                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}
