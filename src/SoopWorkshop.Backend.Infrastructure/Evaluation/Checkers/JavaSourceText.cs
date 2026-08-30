using System.Text;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Checkers
{
    // Hilfsmittel für Prüfungen, die per Regex über Java-Quelltext laufen.
    public static class JavaSourceText
    {
        // Entfernt Kommentare sowie String-, Textblock- und Char-Literale.
        //
        // Ohne das meldete die Namensprüfung einen Verstoß, sobald irgendwo
        // "mein_wert" in einem Kommentar oder in einer Ausgabe stand - der Code
        // selbst war dabei einwandfrei. Entfernte Stellen hinterlassen ein
        // Leerzeichen, damit keine zwei Bezeichner zusammenwachsen.
        public static string StripCommentsAndLiterals(string source)
        {
            var result = new StringBuilder(source.Length);
            var index = 0;

            while (index < source.Length)
            {
                var current = source[index];
                var next = index + 1 < source.Length ? source[index + 1] : '\0';

                // Zeilenkommentar: bis zum Zeilenende überspringen. Der Umbruch
                // selbst bleibt stehen und wird im nächsten Durchlauf übernommen.
                if (current == '/' && next == '/')
                {
                    while (index < source.Length && source[index] != '\n')
                        index++;

                    continue;
                }

                if (current == '/' && next == '*')
                {
                    index += 2;
                    while (index + 1 < source.Length && !(source[index] == '*' && source[index + 1] == '/'))
                        index++;

                    index = Math.Min(index + 2, source.Length);
                    result.Append(' ');
                    continue;
                }

                // Textblock (\"\"\" ... \"\"\") vor dem einfachen String prüfen,
                // sonst endet er sofort beim zweiten Anführungszeichen.
                if (current == '"' && next == '"' && index + 2 < source.Length && source[index + 2] == '"')
                {
                    index += 3;
                    while (index + 2 < source.Length &&
                           !(source[index] == '"' && source[index + 1] == '"' && source[index + 2] == '"'))
                    {
                        if (source[index] == '\\')
                            index++;

                        index++;
                    }

                    index = Math.Min(index + 3, source.Length);
                    result.Append(' ');
                    continue;
                }

                if (current is '"' or '\'')
                {
                    var quote = current;
                    index++;

                    while (index < source.Length && source[index] != quote)
                    {
                        // Maskiertes Zeichen mitsamt Backslash überspringen,
                        // damit \" das Literal nicht vorzeitig beendet.
                        if (source[index] == '\\')
                            index++;

                        index++;
                    }

                    index++;
                    result.Append(' ');
                    continue;
                }

                result.Append(current);
                index++;
            }

            return result.ToString();
        }
    }
}
