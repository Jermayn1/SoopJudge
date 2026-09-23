using System.Text;
using System.Text.RegularExpressions;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Junit
{
    // Leitet die Prüfaufrufe einer JUnit-Datei auf soopjudge.Werte um, damit der
    // Testlauf auch bei bestandenen Vergleichen festhält, was verglichen wurde.
    //
    // Umgeschrieben wird nur der Name vor der Klammer: aus assertEquals( wird
    // soopjudge.Werte.assertEquals(, aus Assertions.assertEquals( ebenso. Die
    // Zeilen bleiben dabei, wo sie sind, und genau daran hängt die Zuordnung:
    // Werte meldet die Zeile des Aufrufs, und hier steht, welcher Ausdruck in
    // dieser Zeile geprüft wurde.
    //
    // Zeichenketten, Zeichen und Kommentare werden übersprungen - ein
    // "assertEquals(" in einer Ausgabe ist kein Aufruf. Deklariert die Datei
    // selbst eine Methode mit einem der Namen, bleibt dieser Name unangetastet,
    // sonst riefe die Datei plötzlich nicht mehr ihre eigene Methode auf.
    public static class AssertionRewriter
    {
        public const string Target = "soopjudge.Werte";

        // Welches Argument das Geprüfte ist. Bei den Vergleichen steht die
        // Erwartung vorn, bei den übrigen gibt es nur das Geprüfte.
        private static readonly Dictionary<string, int> CheckedArgument = new()
        {
            ["assertEquals"] = 1,
            ["assertArrayEquals"] = 1,
            ["assertThrows"] = 1,
            ["assertTrue"] = 0,
            ["assertFalse"] = 0,
            ["assertNull"] = 0,
        };

        // Nach diesen Wörtern beginnt ein Ausdruck, auch wenn davor ein
        // Buchstabe steht: "return assertThrows(...)" ist ein Aufruf,
        // "void assertEquals(" eine Deklaration.
        private static readonly HashSet<string> ExpressionKeywords = ["return", "else", "do", "yield", "case"];

        private const string AssertionsQualifier = "Assertions.";
        private const string FullQualifier = "org.junit.jupiter.api.Assertions.";

        // Länger wird ein Aufruf in der Anzeige nicht. Was darüber hinausgeht,
        // ist kein Aufruf mehr, den man auf einen Blick liest.
        private const int MaxCallLength = 200;

        public sealed record CallSite(int Line, string Method, string? Call);

        public sealed record Result(string Source, IReadOnlyList<CallSite> CallSites);

        public static Result Rewrite(string source)
        {
            var tokens = Scan(source);
            var declared = DeclaredNames(source, tokens);

            var builder = new StringBuilder(source.Length + 256);
            var callSites = new List<CallSite>();
            var copied = 0;

            foreach (var token in tokens)
            {
                if (!CheckedArgument.TryGetValue(token.Name, out var argumentIndex) || declared.Contains(token.Name))
                    continue;

                var open = NextSignificant(source, token.End);
                if (open < 0 || source[open] != '(')
                    continue;

                var qualifierStart = QualifierStart(source, token.Start);
                if (qualifierStart < 0)
                    continue;

                builder.Append(source, copied, qualifierStart - copied);
                builder.Append(Target).Append('.').Append(token.Name);
                copied = token.End;

                var arguments = SplitArguments(source, open);
                var call = arguments is not null && arguments.Count > argumentIndex
                    ? Describe(token.Name, arguments[argumentIndex])
                    : null;

                callSites.Add(new CallSite(LineOf(source, token.Start), token.Name, call));
            }

            builder.Append(source, copied, source.Length - copied);
            return new Result(builder.ToString(), callSites);
        }

        private readonly record struct Token(string Name, int Start, int End);

        // Alle Bezeichner außerhalb von Zeichenketten, Zeichen und Kommentaren.
        private static List<Token> Scan(string source)
        {
            var tokens = new List<Token>();
            var i = 0;

            while (i < source.Length)
            {
                var skipped = SkipLiteralOrComment(source, i);
                if (skipped > i)
                {
                    i = skipped;
                    continue;
                }

                if (char.IsLetter(source[i]) || source[i] is '_' or '$')
                {
                    var start = i;
                    while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] is '_' or '$'))
                        i++;
                    tokens.Add(new Token(source[start..i], start, i));
                    continue;
                }

                i++;
            }

            return tokens;
        }

        // Liefert die Position hinter einem Literal oder Kommentar, der bei i
        // beginnt, sonst i selbst.
        private static int SkipLiteralOrComment(string source, int i)
        {
            if (Starts(source, i, "//"))
            {
                var end = source.IndexOf('\n', i);
                return end < 0 ? source.Length : end;
            }

            if (Starts(source, i, "/*"))
            {
                var end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                return end < 0 ? source.Length : end + 2;
            }

            if (Starts(source, i, "\"\"\""))
            {
                var end = source.IndexOf("\"\"\"", i + 3, StringComparison.Ordinal);
                return end < 0 ? source.Length : end + 3;
            }

            if (source[i] is '"' or '\'')
            {
                var quote = source[i];
                var j = i + 1;
                while (j < source.Length && source[j] != quote && source[j] != '\n')
                    j += source[j] == '\\' ? 2 : 1;
                return Math.Min(j + 1, source.Length);
            }

            return i;
        }

        private static bool Starts(string source, int i, string value) =>
            string.CompareOrdinal(source, i, value, 0, value.Length) == 0;

        // Namen, die die Datei selbst als Methode deklariert: ein Bezeichner
        // davor, der kein Schlüsselwort für einen Ausdruck ist, und dahinter
        // eine Klammer.
        private static HashSet<string> DeclaredNames(string source, List<Token> tokens)
        {
            var declared = new HashSet<string>();

            for (var t = 1; t < tokens.Count; t++)
            {
                var token = tokens[t];
                if (!CheckedArgument.ContainsKey(token.Name))
                    continue;

                var before = PreviousSignificant(source, token.Start);
                var previous = tokens[t - 1];
                var directlyAfterWord = before >= 0 && before == previous.End - 1;

                if (directlyAfterWord && !ExpressionKeywords.Contains(previous.Name)
                    && NextSignificant(source, token.End) is var open && open >= 0 && source[open] == '(')
                {
                    declared.Add(token.Name);
                }
            }

            return declared;
        }

        // Wo die Umschreibung beginnt: beim Namen selbst, oder beim
        // "Assertions." bzw. vollen Paketnamen davor. -1 heißt: kein
        // Prüfaufruf, etwa kasse.assertEquals( oder eine Deklaration.
        private static int QualifierStart(string source, int nameStart)
        {
            foreach (var qualifier in new[] { FullQualifier, AssertionsQualifier })
            {
                if (!EndsWithBefore(source, nameStart, qualifier))
                    continue;

                // "MeineAssertions.assertEquals" oder "x.Assertions.assertEquals"
                // ist nicht JUnit.
                var start = nameStart - qualifier.Length;
                return start > 0 && (source[start - 1] == '.' || IsIdentifierPart(source[start - 1])) ? -1 : start;
            }

            var before = PreviousSignificant(source, nameStart);
            if (before < 0)
                return nameStart;

            if (source[before] == '.')
                return -1;

            if (IsIdentifierPart(source[before]))
            {
                var wordEnd = before + 1;
                var wordStart = wordEnd;
                while (wordStart > 0 && IsIdentifierPart(source[wordStart - 1]))
                    wordStart--;
                return ExpressionKeywords.Contains(source[wordStart..wordEnd]) ? nameStart : -1;
            }

            return nameStart;
        }

        private static bool EndsWithBefore(string source, int position, string value) =>
            position >= value.Length && string.CompareOrdinal(source, position - value.Length, value, 0, value.Length) == 0;

        private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c is '_' or '$';

        private static int PreviousSignificant(string source, int position)
        {
            var i = position - 1;
            while (i >= 0 && char.IsWhiteSpace(source[i]))
                i--;
            return i;
        }

        private static int NextSignificant(string source, int position)
        {
            var i = position;
            while (i < source.Length)
            {
                if (char.IsWhiteSpace(source[i]))
                {
                    i++;
                    continue;
                }

                var skipped = SkipLiteralOrComment(source, i);
                if (skipped > i && (Starts(source, i, "//") || Starts(source, i, "/*")))
                {
                    i = skipped;
                    continue;
                }

                return i;
            }

            return -1;
        }

        // Zerlegt die Argumentliste ab der öffnenden Klammer an den Kommas der
        // obersten Ebene. null, wenn die Klammer nicht geschlossen wird.
        private static List<string>? SplitArguments(string source, int open)
        {
            var arguments = new List<string>();
            var depth = 0;
            var start = open + 1;
            var i = open;

            while (i < source.Length)
            {
                var skipped = SkipLiteralOrComment(source, i);
                if (skipped > i)
                {
                    i = skipped;
                    continue;
                }

                var c = source[i];
                if (c is '(' or '[' or '{')
                {
                    depth++;
                }
                else if (c is ')' or ']' or '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        arguments.Add(source[start..i]);
                        return arguments;
                    }
                }
                else if (c == ',' && depth == 1)
                {
                    arguments.Add(source[start..i]);
                    start = i + 1;
                }

                i++;
            }

            return null;
        }

        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex OpeningSpace = new(@"\(\s+", RegexOptions.Compiled);
        private static readonly Regex ClosingSpace = new(@"\s+\)", RegexOptions.Compiled);
        private static readonly Regex LambdaPrefix = new(@"^\(\s*\)\s*->\s*", RegexOptions.Compiled);

        // Der Ausdruck, den der Teilnehmer in der Anzeige liest. Nur, wenn er
        // etwas aufruft: bei "ergebnis" oder "true" gäbe es nichts zu erfahren.
        private static string? Describe(string method, string argument)
        {
            // Ein Aufruf über mehrere Zeilen soll aussehen, wie man ihn in eine
            // Zeile schreiben würde, ohne Leerraum hinter der Klammer.
            var text = Whitespace.Replace(argument, " ").Trim();
            text = OpeningSpace.Replace(text, "(");
            text = ClosingSpace.Replace(text, ")");

            if (method == "assertThrows")
            {
                text = LambdaPrefix.Replace(text, string.Empty);
                if (text.StartsWith('{') && text.EndsWith('}'))
                    text = text[1..^1].Trim().TrimEnd(';').Trim();
            }

            if (!text.Contains('(') || text.Length > MaxCallLength)
                return null;

            return text;
        }

        private static int LineOf(string source, int position)
        {
            var line = 1;
            for (var i = 0; i < position; i++)
            {
                if (source[i] == '\n')
                    line++;
            }
            return line;
        }
    }
}
