using System.Xml.Linq;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Junit
{
    // Liest die XML-Reports des JUnit-Console-Launchers.
    //
    // Bewusst der XML-Report und nicht die Konsolenausgabe: die ist auf Menschen
    // ausgelegt, ändert sich zwischen Versionen und lässt sich nicht zuverlässig
    // zerlegen. Der Launcher schreibt je Engine eine Datei (TEST-junit-jupiter.xml,
    // TEST-junit-vintage.xml, ...), von denen die meisten leer sind - deshalb
    // werden alle gelesen und zusammengefasst.
    public static class JUnitReportReader
    {
        public static List<JUnitTestCase> Read(string reportsDirectory)
        {
            if (!Directory.Exists(reportsDirectory))
                return [];

            var testCases = new List<JUnitTestCase>();

            foreach (var file in Directory.GetFiles(reportsDirectory, "TEST-*.xml").OrderBy(path => path))
            {
                testCases.AddRange(ReadFile(file));
            }

            return testCases;
        }

        private static IEnumerable<JUnitTestCase> ReadFile(string path)
        {
            XDocument document;

            try
            {
                document = XDocument.Load(path);
            }
            catch (System.Xml.XmlException)
            {
                // Abgeschnittener Report, etwa weil die JVM mitten im Lauf beendet
                // wurde. Der Aufrufer erkennt das an der fehlenden Testmethode und
                // erklärt es dem Teilnehmer - hier still überspringen ist richtig,
                // weil eine kaputte Datei keine Aussage über die Abgabe enthält.
                yield break;
            }

            foreach (var element in document.Descendants("testcase"))
            {
                var className = element.Attribute("classname")?.Value ?? string.Empty;
                var methodName = element.Attribute("name")?.Value ?? string.Empty;

                var failure = element.Element("failure") ?? element.Element("error");
                var skipped = element.Element("skipped");

                // Übersprungene Tests gelten als nicht bestanden: der Teilnehmer
                // hat die geforderte Leistung nicht gezeigt.
                var passed = failure is null && skipped is null;
                var message = ReadMessage(failure, skipped);
                var comparison = AssertionMessage.Split(message);

                yield return new JUnitTestCase(
                    ReadDisplayName(element) ?? BuildFallbackName(className, methodName),
                    className,
                    methodName,
                    passed,
                    comparison.Message,
                    comparison.Expected,
                    comparison.Actual);
            }
        }

        // Der Launcher legt den @DisplayName in system-out ab, als Zeile
        // "display-name: JUnit Jupiter > MainTest > main gibt Hallo Soop aus".
        // Der letzte Abschnitt ist der Text, den der Admin geschrieben hat - und
        // damit weit verständlicher als ein Methodenname.
        private static string? ReadDisplayName(XElement testCase)
        {
            var systemOut = testCase.Element("system-out")?.Value;
            if (string.IsNullOrWhiteSpace(systemOut))
                return null;

            var line = systemOut
                .Split('\n')
                .Select(entry => entry.Trim())
                .FirstOrDefault(entry => entry.StartsWith("display-name:", StringComparison.Ordinal));

            if (line is null)
                return null;

            var value = line["display-name:".Length..].Trim();
            var lastSegment = value.Split(" > ").LastOrDefault()?.Trim();

            return string.IsNullOrWhiteSpace(lastSegment) ? null : lastSegment;
        }

        private static string BuildFallbackName(string className, string methodName) =>
            string.IsNullOrWhiteSpace(className) ? methodName : $"{className}.{methodName}";

        private static string ReadMessage(XElement? failure, XElement? skipped)
        {
            if (skipped is not null)
                return skipped.Attribute("message")?.Value ?? "Der Test wurde übersprungen.";

            if (failure is null)
                return string.Empty;

            var message = failure.Attribute("message")?.Value;
            if (!string.IsNullOrWhiteSpace(message))
                return message;

            // Ohne message-Attribut bleibt der Stacktrace. Nur die erste Zeile,
            // der Rest sind Innereien von JUnit und hilft niemandem weiter.
            var text = failure.Value.Trim();
            return text.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
        }
    }
}
