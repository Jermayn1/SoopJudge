using System.Text.RegularExpressions;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Junit
{
    // Übersetzt javac-Meldungen in Sätze, die einem Teilnehmer sagen, was
    // erwartet wurde.
    //
    // Der Anlass: kompiliert die hinterlegte JUnit-Datei nicht gegen die Abgabe,
    // ist das ein legitimes Nichtbestehen - aber "cannot find symbol" beantwortet
    // nicht die Frage, wie die Methode denn heißen soll. Die Rohausgabe wird
    // deshalb ergänzt, nicht ersetzt.
    public static class JavaCompilerMessages
    {
        // javac schreibt den fehlenden Bezeichner in zwei Folgezeilen:
        //   symbol:   method berechneSumme(int,int)
        //   location: class Main
        private static readonly Regex Symbol = new(@"symbol:\s+(?<kind>\w+)\s+(?<name>.+)", RegexOptions.Compiled);
        private static readonly Regex Location = new(@"location:\s+(?:class|interface|variable)\s+(?<name>\S+)", RegexOptions.Compiled);

        private static readonly Regex IncompatibleTypes =
            new(@"incompatible types:\s+(?<actual>.+?)\s+cannot be converted to\s+(?<expected>.+)", RegexOptions.Compiled);

        private static readonly Regex CannotBeApplied =
            new(@"(?<kind>method|constructor)\s+(?<name>\w+)\s+in\s+(?:class|interface)\s+(?<owner>\S+)\s+cannot be applied to given types",
                RegexOptions.Compiled);

        private static readonly Regex NotPublic =
            new(@"(?<name>\S+)\s+is not public in\s+(?<owner>\S+)", RegexOptions.Compiled);

        // Liefert eine verständliche Erklärung oder null, wenn die Ausgabe kein
        // bekanntes Muster enthält. Dann bleibt es bei der Rohausgabe - lieber
        // roh als falsch geraten.
        public static string? Translate(string compilerOutput)
        {
            if (string.IsNullOrWhiteSpace(compilerOutput))
                return null;

            var explanations = new List<string>();

            AddMissingSymbol(compilerOutput, explanations);
            AddFirstMatch(IncompatibleTypes, compilerOutput, explanations, match =>
                $"Ein Typ passt nicht: erwartet wurde „{match.Groups["expected"].Value.Trim()}“, " +
                $"geliefert wurde „{match.Groups["actual"].Value.Trim()}“.");

            AddFirstMatch(CannotBeApplied, compilerOutput, explanations, match =>
                match.Groups["kind"].Value == "constructor"
                    ? $"Der Konstruktor von „{match.Groups["owner"].Value}“ passt nicht zu den übergebenen Werten. " +
                      "Prüfe Anzahl und Reihenfolge der Parameter."
                    : $"Die Methode „{match.Groups["name"].Value}“ in „{match.Groups["owner"].Value}“ passt nicht zu den " +
                      "übergebenen Werten. Prüfe Anzahl, Reihenfolge und Typen der Parameter.");

            AddFirstMatch(NotPublic, compilerOutput, explanations, match =>
                $"„{match.Groups["name"].Value}“ in „{match.Groups["owner"].Value}“ ist nicht öffentlich. " +
                "Damit der Test darauf zugreifen kann, muss es „public“ sein.");

            return explanations.Count == 0 ? null : string.Join(" ", explanations);
        }

        private static void AddMissingSymbol(string compilerOutput, List<string> explanations)
        {
            var symbolMatch = Symbol.Match(compilerOutput);
            if (!symbolMatch.Success)
                return;

            var kind = symbolMatch.Groups["kind"].Value switch
            {
                "method" => "die Methode",
                "variable" => "die Variable",
                "class" => "die Klasse",
                "constructor" => "der Konstruktor",
                _ => "das Element"
            };

            var name = symbolMatch.Groups["name"].Value.Trim();
            var locationMatch = Location.Match(compilerOutput);

            var where = locationMatch.Success
                ? $" in der Klasse „{locationMatch.Groups["name"].Value}“"
                : string.Empty;

            explanations.Add(
                $"Der Test erwartet {kind} „{name}“{where} — in deiner Abgabe gibt es sie so nicht. " +
                "Achte auf exakte Schreibweise, Groß-/Kleinschreibung und die Parameter.");
        }

        private static void AddFirstMatch(
            Regex pattern,
            string compilerOutput,
            List<string> explanations,
            Func<Match, string> build)
        {
            var match = pattern.Match(compilerOutput);
            if (match.Success)
                explanations.Add(build(match));
        }
    }
}
