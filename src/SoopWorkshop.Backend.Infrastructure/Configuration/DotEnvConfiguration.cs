using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SoopWorkshop.Backend.Infrastructure.Configuration
{
    // Liest die .env aus dem Repository-Wurzelverzeichnis und stellt sie der
    // Anwendung als Konfiguration bereit.
    //
    // Absicht: in der Entwicklung gibt es genau eine Datei, in der die Zugangsdaten
    // und die Stellschrauben der Auswertung stehen — dieselbe, aus der auch
    // docker-compose liest. Deshalb wird die Quelle bewusst ganz oben auf den
    // Stapel gelegt und schlägt damit auch Umgebungsvariablen. Eine vergessene
    // Variable in der Shell kann so nicht mehr still etwas anderes bewirken,
    // als in der Datei steht.
    //
    // Außerhalb von Development wird sie nicht geladen: im Betrieb kommen die
    // Werte aus echten Umgebungsvariablen.
    public static class DotEnvConfiguration
    {
        // Wie viele Ebenen aufwärts nach der .env gesucht wird. Die API startet
        // in src/SoopWorkshop.Backend.API, die Datei liegt zwei Ebenen darüber.
        private const int MaxSearchDepth = 6;

        public static IConfigurationBuilder AddDotEnv(this IConfigurationBuilder builder, string startDirectory)
        {
            var path = FindDotEnv(startDirectory);
            if (path is null)
                return builder;

            var values = Parse(File.ReadAllLines(path));
            AddDerivedConnectionString(values);

            return builder.AddInMemoryCollection(values);
        }

        public static string? FindDotEnv(string startDirectory)
        {
            var directory = new DirectoryInfo(startDirectory);

            for (var depth = 0; depth < MaxSearchDepth && directory is not null; depth++)
            {
                var candidate = Path.Combine(directory.FullName, ".env");
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }

            return null;
        }

        // Erwartet KEY=VALUE je Zeile. Leerzeilen und Zeilen mit # werden übergangen.
        // Doppelte Unterstriche werden wie bei Umgebungsvariablen zum Doppelpunkt,
        // damit Evaluation__MaxConcurrency auf Evaluation:MaxConcurrency zeigt.
        public static Dictionary<string, string?> Parse(IEnumerable<string> lines)
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                    continue;

                var separator = trimmed.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = trimmed[..separator].Trim();
                var value = Unquote(trimmed[(separator + 1)..].Trim());

                values[key.Replace("__", ":")] = value;
            }

            return values;
        }

        // Baut den Connection-String aus den POSTGRES_-Werten, damit das Passwort
        // nur an einer Stelle steht — derselben, aus der docker-compose die
        // Datenbank aufsetzt. Ein ausdrücklich gesetzter Connection-String
        // gewinnt weiterhin.
        private static void AddDerivedConnectionString(Dictionary<string, string?> values)
        {
            const string key = "ConnectionStrings:DefaultConnection";

            if (!string.IsNullOrWhiteSpace(GetValue(values, key)))
                return;

            var password = GetValue(values, "POSTGRES_PASSWORD");
            if (string.IsNullOrWhiteSpace(password))
                return;

            // 127.0.0.1 und nicht localhost: unter Windows löst localhost zuerst
            // auf ::1 auf, wo Dockers WSL-Relay horcht, ohne zum Container
            // durchzureichen. Der Fehler kommt als "28P01 password authentication
            // failed" zurück und sieht damit wie ein falsches Passwort aus.
            // Über den Builder statt per Zeichenkette zusammengesetzt: ein Passwort
            // mit Semikolon oder Anführungszeichen würde den Connection-String
            // sonst zerlegen, und der Fehler sähe wie ein falsches Passwort aus.
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = GetValue(values, "POSTGRES_HOST") ?? "127.0.0.1",
                Port = int.TryParse(GetValue(values, "POSTGRES_PORT"), out var port) ? port : 5432,
                Database = GetValue(values, "POSTGRES_DB") ?? "soopworkshop",
                Username = GetValue(values, "POSTGRES_USER") ?? "postgres",
                Password = password
            };

            values[key] = builder.ConnectionString;
        }

        private static string? GetValue(Dictionary<string, string?> values, string key) =>
            values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

        private static string Unquote(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                return value[1..^1];
            }

            return value;
        }
    }
}
