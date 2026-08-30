using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using SoopWorkshop.Backend.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SoopWorkshop.Tests.Integration
{
    /// <summary>
    /// Ein PostgreSQL-Container für den gesamten Testlauf.
    /// </summary>
    /// <remarks>
    /// Warum überhaupt eine echte Datenbank: EF InMemory kennt weder
    /// Transaktionen noch Cascade-Delete noch Fremdschlüsselbedingungen. Genau
    /// darauf steht aber der Bestands-Transfer - ein Rollback-Test gegen
    /// InMemory wäre grün, ohne irgendetwas zu belegen.
    ///
    /// Kosten: einmal je Testlauf Container starten und migrieren (rund 10 bis
    /// 20 Sekunden), danach nichts mehr. Zwischen den Tests räumt Respawn auf,
    /// das dauert Millisekunden. Erneut zu migrieren wäre um ein Vielfaches
    /// teurer.
    /// </remarks>
    public sealed class PostgresFixture : IAsyncLifetime
    {
        /// <summary>
        /// Dieselbe Hauptversion wie in docker-compose.yml. Wandert die eine,
        /// muss die andere mit: sonst prüft jeder Testlauf die Migrationen gegen
        /// eine andere PostgreSQL-Version als die, auf der sie im Betrieb laufen -
        /// und genau diese Zusicherung ist der Grund für den Container.
        /// </summary>
        private const string Image = "postgres:16-alpine";

        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(Image)
            .WithDatabase("soopworkshop_test")
            .WithUsername("soop")
            .WithPassword("soop")
            .Build();

        private Respawner _respawner = null!;
        private NpgsqlConnection _respawnConnection = null!;

        public string ConnectionString { get; private set; } = string.Empty;

        /// <summary>
        /// Die API im Speicher. Liegt hier und nicht in der Testklasse, weil
        /// xUnit je Testmethode eine neue Klasseninstanz baut - ein eigener Host
        /// pro Test hieße, die halbe Anwendung hundertfach hochzufahren.
        /// </summary>
        public SoopWorkshopFactory Factory { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();

            // Über die echten Migrationen, nicht über EnsureCreated. Damit prüft
            // jeder Testlauf nebenbei, dass der Migrationsstand durchläuft. Eine
            // Migration, die von Hand nachgebessert werden musste, fällt sonst
            // erst beim Aufsetzen des Servers auf.
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            await using (var context = new AppDbContext(options))
            {
                await context.Database.MigrateAsync();
            }

            // Respawn hält eine eigene Verbindung offen: es liest das Schema
            // einmal ein und kennt danach die Löschreihenfolge selbst.
            _respawnConnection = new NpgsqlConnection(ConnectionString);
            await _respawnConnection.OpenAsync();

            _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                // Die Migrationshistorie muss stehen bleiben, sonst hält EF die
                // Datenbank beim nächsten Zugriff für nicht migriert.
                TablesToIgnore = ["__EFMigrationsHistory"]
            });

            Factory = new SoopWorkshopFactory(ConnectionString);
        }

        /// <summary>Setzt alle Tabellen zurück, ohne erneut zu migrieren.</summary>
        public Task ResetAsync() => _respawner.ResetAsync(_respawnConnection);

        public async Task DisposeAsync()
        {
            // Erst die Anwendung, dann die Datenbank unter ihr.
            await Factory.DisposeAsync();
            await _respawnConnection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Alle Integrationstests teilen sich denselben Container. Ohne diese
    /// Sammlung startet xUnit je Testklasse einen eigenen - bei einem Dutzend
    /// Klassen wären das Minuten statt Sekunden.
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
    {
        public const string Name = "Postgres";
    }
}
