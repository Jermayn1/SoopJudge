namespace SoopWorkshop.Backend.Infrastructure.Configuration
{
    // Fasst einen Connection-String so zusammen, dass er beim Start protokolliert
    // werden darf: Server, Port und Datenbank, niemals das Passwort.
    //
    // Grund: ein falscher Wert aus einer vergessenen Umgebungsvariable war bisher
    // erst im Stacktrace einer fehlgeschlagenen Abfrage zu sehen.
    public static class ConnectionStringSummary
    {
        public static string Describe(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return "nicht gesetzt";

            var parts = connectionString
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split('=', 2))
                .Where(pair => pair.Length == 2)
                .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim(), StringComparer.OrdinalIgnoreCase);

            var host = Get(parts, "Host") ?? Get(parts, "Server") ?? "?";
            var port = Get(parts, "Port") ?? "5432";
            var database = Get(parts, "Database") ?? "?";

            return $"{host}:{port}/{database}";
        }

        private static string? Get(Dictionary<string, string> parts, string key) =>
            parts.TryGetValue(key, out var value) && value.Length > 0 ? value : null;
    }
}
