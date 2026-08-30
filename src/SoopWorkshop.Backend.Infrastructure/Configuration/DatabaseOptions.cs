namespace SoopWorkshop.Backend.Infrastructure.Configuration
{
    // Verhalten der Datenbank beim Start. Gebunden an den Abschnitt "Database";
    // im Betrieb über Database__MigrateOnStartup zu setzen.
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        // Standard true, damit "docker compose up" ohne Zwischenschritt ein
        // benutzbares System ergibt. Abschaltbar für den Fall, dass die
        // Anwendung im Betrieb keine Rechte am Schema haben soll - dann wandern
        // die Migrationen in einen eigenen Schritt.
        public bool MigrateOnStartup { get; set; } = true;

        // Wie lange auf eine erreichbare Datenbank gewartet wird, bevor der Start
        // abgebrochen wird. Im Container-Verbund kann die Datenbank den Zuschlag
        // des Healthchecks bekommen und trotzdem den ersten Verbindungsversuch
        // noch ablehnen; ein einzelner Versuch wäre deshalb zu streng.
        public int MigrationRetries { get; set; } = 10;

        public int MigrationRetryDelaySeconds { get; set; } = 3;
    }
}
