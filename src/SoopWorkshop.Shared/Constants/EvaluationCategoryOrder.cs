using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.Constants
{
    // Feste Anzeigereihenfolge der Kategorien. Bewusst getrennt von der
    // Ausführungsreihenfolge der Checker: kompiliert wird zuerst, angezeigt wird
    // aber mit Clean Code begonnen. Ohne diese Liste hängt die Reihenfolge im
    // Frontend davon ab, wie die Datenbank die Zeilen zurückgibt.
    public static class EvaluationCategoryOrder
    {
        private static readonly EvaluationCategory[] DisplayOrder =
        [
            EvaluationCategory.CleanCode,
            EvaluationCategory.Compilability,
            EvaluationCategory.Functionality
        ];

        // Dieselbe Liste, aber als Aussage darüber, welche Kategorien überhaupt
        // noch vergeben werden.
        //
        // Die übrigen Werte von EvaluationCategory - CharacterSet,
        // NamingConventions, TestCases, UnitTests - werden nicht mehr vergeben.
        // Sie sind zu Teilprüfungen von CleanCode und Functionality geworden und
        // stehen nur noch im Enum, weil sie als int in der Datenbank liegen:
        // ihre Zahlenwerte dürfen deshalb nie neu belegt werden.
        //
        // Wer aufgabenspezifische Gewichte setzt, darf nur die aktiven treffen:
        // ein Gewicht auf TestCases würde nie gelesen und wäre damit stille
        // Konfiguration, die nichts tut.
        public static IReadOnlyList<EvaluationCategory> Active => DisplayOrder;

        public static bool IsActive(EvaluationCategory category) =>
            Array.IndexOf(DisplayOrder, category) >= 0;

        // Kategorien, die nicht mehr vergeben werden (Zeichensatz, Namenskonventionen,
        // Testfälle und Unit-Tests aus früheren Auswertungen), landen hinten statt
        // zu verschwinden.
        public static int Of(EvaluationCategory category)
        {
            var index = Array.IndexOf(DisplayOrder, category);
            return index < 0 ? DisplayOrder.Length : index;
        }
    }
}
