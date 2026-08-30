using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Tasks
{
    // Aufgabenspezifisches Gewicht einer Bewertungskategorie. Fehlt der Eintrag,
    // gilt der Standard aus der Konfiguration (Evaluation:CategoryWeights).
    public class TaskCategoryWeightDto
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public EvaluationCategory Category { get; set; }
        public double Weight { get; set; }
    }
}
