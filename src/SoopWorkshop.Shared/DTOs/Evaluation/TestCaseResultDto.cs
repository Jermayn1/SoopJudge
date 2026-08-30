namespace SoopWorkshop.Shared.DTOs.Evaluation
{
    public class TestCaseResultDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;

        // Womit geprüft wurde. Leer, wenn die Prüfung keine Eingabe hatte.
        public string Input { get; set; } = string.Empty;

        public string ExpectedOutput { get; set; } = string.Empty;
        public string ActualOutput { get; set; } = string.Empty;
        public bool Passed { get; set; }

        // Anzeigereihenfolge innerhalb der Kategorie. Das Frontend sortiert
        // danach, statt sich auf die Reihenfolge der Datenbank zu verlassen.
        public int Order { get; set; }
    }
}