namespace SoopWorkshop.Backend.Application.Tasks
{
    // Zerlegt eine Methodensignatur, wie der Admin sie aufschreibt.
    public static class JavaSignature
    {
        // Liefert den reinen Methodennamen aus einer Signatur:
        //   "public static int addiere(int a, int b)"  ->  "addiere"
        //   "addiere(int, int)"                        ->  "addiere"
        //   "addiere"                                  ->  "addiere"
        //
        // Bewusst so simpel: alles vor der Klammer nehmen und davon das letzte
        // Wort. Ein echter Parser wäre hier Aufwand ohne Gegenwert, denn die
        // genauen Parametertypen prüft ohnehin erst der Java-Compiler.
        public static string ExtractMethodName(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
                return string.Empty;

            var beforeParameters = signature.Split('(')[0].Trim();

            var lastWord = beforeParameters
                .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();

            return lastWord ?? string.Empty;
        }
    }
}
