using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SoopWorkshop.Backend.Application.Evaluation.Interfaces;
using SoopWorkshop.Backend.Infrastructure.Evaluation;

namespace SoopWorkshop.Tests.Integration
{
    /// <summary>
    /// Startet die echte API im Speicher - dieselbe Program.cs, dieselbe
    /// Pipeline, dieselben Controller.
    /// </summary>
    public sealed class SoopWorkshopFactory(string connectionString) : WebApplicationFactory<Program>
    {
        /// <summary>Das Passwort, mit dem sich Tests anmelden.</summary>
        public const string AdminPassword = "test-passwort";

        /// <summary>Der Ursprung, den die CORS-Regel erlauben soll.</summary>
        public const string AllowedOrigin = "http://localhost:5173";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Pflicht, und zwar aus einem konkreten Grund: in Development lädt
            // Program.cs die .env des Repositorys als LETZTE Konfigurationsquelle
            // und schlägt damit absichtlich auch Umgebungsvariablen. Sie würde
            // die Werte hier überschreiben, und die Tests liefen gegen die
            // Entwicklungsdatenbank.
            builder.UseEnvironment("Testing");

            // UseSetting statt ConfigureAppConfiguration: beide Werte werden
            // gebraucht, bevor irgendein Test-Code laufen kann. AddInfrastructure
            // wirft bei leerem Connection-String, AddAdminAuthentication bei
            // fehlendem Passwort - beides absichtlich, damit nichts still ohne
            // Datenbank oder ohne Zugangsschutz startet.
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.UseSetting("Admin:Password", AdminPassword);
            builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

            builder.ConfigureTestServices(services =>
            {
                // Der EvaluationWorker setzt beim Start verwaiste Pending- und
                // Running-Abgaben auf Failed. In einem Test, der genau so eine
                // Abgabe angelegt hat, ändert er damit die Datenlage unter der
                // Hand. Außerdem bräuchte er ein JDK.
                var worker = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType == typeof(EvaluationWorker));

                if (worker is not null)
                    services.Remove(worker);

                // Mit dem Worker fällt auch der einzige Leser der Warteschlange
                // weg. Die echte EvaluationQueue ist ein begrenzter Channel
                // (QueueCapacity, Standard 100) mit FullMode.Wait und hängt als
                // Singleton an dieser Factory, die einmal je Testlauf gebaut wird -
                // Respawn setzt die Datenbank zurück, diesen Channel nicht.
                // Legten die Tests insgesamt mehr als 100 Abgaben an, wartete
                // CreateAsync bei der nächsten unbegrenzt auf einen Leser, den es
                // nicht gibt: der Testlauf hängt ohne Fehlermeldung.
                services.RemoveAll<IEvaluationQueue>();
                services.AddSingleton<IEvaluationQueue>(new MitschreibendeWarteschlange());
            });
        }

        /// <summary>
        /// Nimmt jede Abgabe an und merkt sie sich, statt sie zu stapeln.
        /// </summary>
        /// <remarks>
        /// Nebenbei wird das Einreihen damit überhaupt erst prüfbar: vorher
        /// belegte nur der Status Pending in der Datenbank, dass die Abgabe
        /// angekommen ist - ob sie je in der Warteschlange landete, sah niemand.
        /// </remarks>
        public sealed class MitschreibendeWarteschlange : IEvaluationQueue
        {
            private readonly System.Collections.Concurrent.ConcurrentQueue<Guid> _eingereiht = new();

            public IReadOnlyCollection<Guid> Eingereiht => _eingereiht;

            public ValueTask EnqueueAsync(Guid submissionId, CancellationToken cancellationToken)
            {
                _eingereiht.Enqueue(submissionId);
                return ValueTask.CompletedTask;
            }

            public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
                AsyncEnumerable.Empty<Guid>();
        }
    }
}
