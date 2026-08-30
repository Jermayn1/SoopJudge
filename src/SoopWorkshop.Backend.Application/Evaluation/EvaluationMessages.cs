namespace SoopWorkshop.Backend.Application.Evaluation
{
    // Hinweistexte, die sich mehrere Checker teilen.
    //
    // Warum geteilt: Konsolen-Testfälle und Unit-Tests zahlen auf dieselbe
    // Kategorie ein. Hätte jeder seinen eigenen Wortlaut, stünden bei einer
    // Aufgabe mit beiden Prüfarten zwei Hinweise hintereinander, die dasselbe
    // sagen - der Teilnehmer liest dann einen Absatz statt eines Satzes.
    public static class EvaluationMessages
    {
        public const string ComparisonHint =
            "Vergleiche „Erwartet“ und „Erhalten“ genau — Groß-/Kleinschreibung, " +
            "Leerzeichen und Zeilenumbrüche zählen mit.";
    }
}
