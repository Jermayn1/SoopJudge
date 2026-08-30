namespace SoopWorkshop.Backend.API.Configuration
{
    // Zugang zum Admin-Bereich. Gebunden an den Konfigurationsabschnitt "Admin";
    // lokal steht der Wert in der .env als Admin__Password.
    //
    // Kein Standardwert. Ein voreingestelltes Passwort wäre schlimmer als
    // keines: es sieht nach Schutz aus und ist öffentlich bekannt.
    public class AdminOptions
    {
        public const string SectionName = "Admin";

        public string Password { get; set; } = string.Empty;
    }
}
