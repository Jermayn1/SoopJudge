using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SoopWorkshop.Backend.Infrastructure.Configuration;

namespace SoopWorkshop.Backend.Infrastructure.Persistence
{
    /// <summary>
    /// Wendet ausstehende Migrationen beim Start an.
    /// </summary>
    /// <remarks>
    /// Ein IHostedService und bewusst kein BackgroundService: die Arbeit steht in
    /// StartAsync und hält damit den Start auf, bis das Schema steht. Ein
    /// BackgroundService gäbe die Ausführung beim ersten await zurück - der
    /// EvaluationWorker griffe dann auf eine womöglich noch nicht migrierte
    /// Datenbank zu. Aus demselben Grund wird dieser Dienst VOR ihm registriert.
    ///
    /// Ein Fehlschlag bricht den Start ab, und das ist Absicht: ohne Schema
    /// beantwortet die API jede Anfrage mit einem 500er, ein weiterlaufender
    /// Server täuschte also Betriebsbereitschaft nur vor. Zusammen mit
    /// "restart: unless-stopped" im Compose-Aufbau versucht der Container es von
    /// selbst erneut.
    /// </remarks>
    public class DatabaseMigrationService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DatabaseOptions _options;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(
            IServiceScopeFactory scopeFactory,
            IOptions<DatabaseOptions> options,
            ILogger<DatabaseMigrationService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.MigrateOnStartup)
            {
                _logger.LogInformation(
                    "Migrationen beim Start sind abgeschaltet (Database:MigrateOnStartup). " +
                    "Das Schema muss von Hand auf dem Stand gehalten werden.");
                return;
            }

            var versuche = Math.Max(1, _options.MigrationRetries);
            var pause = TimeSpan.FromSeconds(Math.Max(1, _options.MigrationRetryDelaySeconds));

            // Die Schleife endet auf genau zwei Wegen: mit return nach einem
            // erfolgreichen Lauf, oder mit der Ausnahme des LETZTEN Versuchs,
            // die der Filter unten nicht mehr fängt. Hinter der Schleife steht
            // deshalb bewusst nichts mehr - eine Zeile dort wäre unerreichbar.
            for (var versuch = 1; versuch <= versuche; versuch++)
            {
                try
                {
                    await MigrateAsync(cancellationToken);
                    return;
                }
                // Der Filter ist der Kern: beim letzten Versuch greift er nicht,
                // die Ausnahme verlässt StartAsync und bricht den Start ab.
                // Ohne Schema beantwortet die API jede Anfrage mit einem 500er -
                // ein weiterlaufender Server täuschte Betriebsbereitschaft vor.
                catch (Exception ex) when (versuch < versuche)
                {
                    // Nicht still: der erste Versuch scheitert im Container-Verbund
                    // regelmäßig, und wer die Meldung nie sieht, sucht beim
                    // nächsten echten Fehler an der falschen Stelle.
                    _logger.LogWarning(
                        ex,
                        "Die Datenbank war beim Versuch {Versuch} von {Versuche} nicht bereit. " +
                        "Erneuter Versuch in {Pause} Sekunden.",
                        versuch,
                        versuche,
                        pause.TotalSeconds);

                    await Task.Delay(pause, cancellationToken);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task MigrateAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var ausstehend = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (ausstehend.Count == 0)
            {
                _logger.LogInformation("Das Datenbankschema ist auf dem Stand.");
                return;
            }

            _logger.LogInformation(
                "{Anzahl} ausstehende Migration(en) werden angewendet: {Migrationen}",
                ausstehend.Count,
                string.Join(", ", ausstehend));

            await context.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Migrationen angewendet.");
        }
    }
}
