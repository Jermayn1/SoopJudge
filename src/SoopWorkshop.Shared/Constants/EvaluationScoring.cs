namespace SoopWorkshop.Shared.Constants
{
    // Rahmen der Punkteberechnung. Die Verteilung auf die Kategorien steckt seit der
    // Bewertungs-Engine v2 nicht mehr in Konstanten, sondern in Gewichten: global in
    // der Konfiguration (Evaluation:CategoryWeights), pro Aufgabe überschreibbar.
    public static class EvaluationScoring
    {
        // Erreichbare Gesamtpunktzahl. Die Kategoriepunkte werden so gerundet,
        // dass ihre Summe exakt diesen Wert ergibt.
        public const int TotalPoints = 100;
    }
}
