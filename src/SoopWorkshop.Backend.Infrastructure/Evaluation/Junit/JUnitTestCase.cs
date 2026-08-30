namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Junit
{
    // Eine Testmethode aus dem XML-Report des JUnit-Launchers.
    public sealed record JUnitTestCase(
        string DisplayName,
        string ClassName,
        string MethodName,
        bool Passed,

        // Was übrig bleibt, wenn Erwartet und Erhalten herausgelöst sind:
        // entweder die eigene Meldung des Admins oder - wenn sich nichts
        // zerlegen ließ - die vollständige Fehlermeldung.
        string Message,

        // Aus "expected: <5> but was: <-1>" herausgelöst, damit ein
        // fehlgeschlagener Unit-Test genauso dargestellt wird wie ein
        // fehlgeschlagener Konsolen-Testfall.
        string Expected,
        string Actual);
}
