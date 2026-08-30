using System.Text.RegularExpressions;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Schneidet aus Java-Quelltext den Rumpf eines benannten Typs heraus.
    //
    // Gebraucht, seit der Vertrag mehrere Klassen kennt: "einzahlen" muss in
    // 'Konto' stehen und nicht irgendwo in der Abgabe. Ohne den Rumpf sucht der
    // ContractChecker im gesamten Quelltext und meldet Treffer, die fachlich
    // keine sind.
    //
    // Klammernzählen ist hier zulässig, weil der Aufrufer den Text vorher
    // durch JavaSourceText.StripCommentsAndLiterals schickt: Kommentare sowie
    // String-, Textblock- und Char-Literale sind dann weg, jede verbliebene
    // Klammer ist echter Code. Auf rohem Quelltext wäre das nicht verlässlich -
    // eine geschweifte Klammer in einem String würde mitzählen.
    public static class JavaTypeBodies
    {
        // Findet den Rumpf zwischen der öffnenden und der zugehörigen
        // schließenden Klammer. null, wenn der Typ nicht deklariert ist.
        //
        // Bekanntes Ist-Verhalten: eine innere Klasse liegt im Rumpf der äußeren
        // und ihre Methoden zählen damit auch für die äußere. Für den
        // Workshop hingenommen - innere Klassen kommen dort nicht vor, und die
        // genaue Zugehörigkeit prüft die JUnit-Kompilierung ohnehin exakt.
        public static string? BodyOf(string strippedCode, string typeName)
        {
            var declaration = Regex.Match(
                strippedCode,
                $@"\b(?:class|interface|enum|record)\s+{Regex.Escape(typeName)}\b");

            if (!declaration.Success)
                return null;

            var open = strippedCode.IndexOf('{', declaration.Index + declaration.Length);
            if (open < 0)
                return null;

            var depth = 0;

            for (var index = open; index < strippedCode.Length; index++)
            {
                if (strippedCode[index] == '{')
                    depth++;
                else if (strippedCode[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return strippedCode[(open + 1)..index];
                }
            }

            // Unbalancierte Klammern: der Code kompiliert ohnehin nicht. Lieber
            // den Rest zurückgeben als so zu tun, als gäbe es den Typ nicht.
            return strippedCode[(open + 1)..];
        }

        // Alle deklarierten Typnamen - für die Meldung "erwartet Konto, gefunden
        // Rechner, Kunde".
        public static List<string> DeclaredNames(string strippedCode) =>
            [.. Regex.Matches(strippedCode, @"\b(?:class|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)")
                .Select(match => match.Groups[1].Value)
                .Distinct()];
    }
}
