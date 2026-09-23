using System.Text;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Junit
{
    // Ein Vergleich, den soopjudge.Werte während des Testlaufs mitgeschrieben
    // hat. Die Zeile im Report sieht so aus:
    //
    //   [soop-vergleich] assertEquals 1 KasseTest 12 Ny41 Ny41
    //
    // Methode, bestanden (1/0), Klasse und Zeile des Aufrufs, dann Erwartet und
    // Erhalten in Base64 - die Werte dürfen Leerzeichen und Zeilenumbrüche
    // enthalten, ohne die Zeile zu zerlegen.
    public sealed record RecordedComparison(
        string Method,
        bool Passed,
        string ClassName,
        int Line,
        string Expected,
        string Actual)
    {
        public const string Marker = "[soop-vergleich]";

        // Alles, was nicht genau so aussieht, gehört nicht dazu - etwa eine
        // Zeile, die die Abgabe selbst nach System.err schreibt.
        public static RecordedComparison? TryParse(string line)
        {
            var parts = line.Trim().Split(' ');
            if (parts.Length != 7 || parts[0] != Marker)
                return null;

            if (parts[2] is not ("0" or "1") || !int.TryParse(parts[4], out var number))
                return null;

            var expected = Decode(parts[5]);
            var actual = Decode(parts[6]);
            if (expected is null || actual is null)
                return null;

            return new RecordedComparison(parts[1], parts[2] == "1", parts[3], number, expected, actual);
        }

        private static string? Decode(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch (FormatException)
            {
                // Kein gültiges Base64: dann ist die Zeile keine von Werte,
                // sondern sieht nur so aus.
                return null;
            }
        }
    }
}
