using System.Text.RegularExpressions;

namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Junit
{
    // Zerlegt die Fehlermeldung eines fehlgeschlagenen JUnit-Tests.
    //
    // JUnit meldet über opentest4j in einer festen Form:
    //   "expected: <5> but was: <-1>"
    // und mit eigener Meldung des Admins davor:
    //   "Die Summe stimmt nicht ==> expected: <5> but was: <-1>"
    //
    // Herausgelöst werden die beiden Werte, damit ein fehlgeschlagener
    // Unit-Test in der Anzeige genauso aussieht wie ein fehlgeschlagener
    // Konsolen-Testfall - "Erwartet" und "Erhalten" untereinander statt einer
    // englischen Zeile, die der Teilnehmer erst entziffern muss.
    public static class AssertionMessage
    {
        private static readonly Regex Comparison = new(
            @"^(?<message>.*?)(?:\s*==>\s*)?expected: <(?<expected>.*?)> but was: <(?<actual>.*?)>\s*$",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public sealed record Parts(string Message, string Expected, string Actual);

        public static Parts Split(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return new Parts(string.Empty, string.Empty, string.Empty);

            var match = Comparison.Match(message.Trim());

            // Passt die Form nicht - etwa bei einer NullPointerException oder
            // einem assertTrue ohne Vergleich - bleibt die Meldung unverändert.
            // Lieber roh als falsch zerlegt.
            if (!match.Success)
                return new Parts(message.Trim(), string.Empty, string.Empty);

            return new Parts(
                match.Groups["message"].Value.Trim(),
                match.Groups["expected"].Value,
                match.Groups["actual"].Value);
        }
    }
}
