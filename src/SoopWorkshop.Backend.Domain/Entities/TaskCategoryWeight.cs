using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Domain.Entities
{
    // Überschreibt für eine einzelne Aufgabe das Standardgewicht einer
    // Bewertungskategorie. Fehlt der Eintrag, gilt der Wert aus der Konfiguration.
    public class TaskCategoryWeight
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public EvaluationCategory Category { get; set; }

        // Relativ zu den Gewichten der anderen anwendbaren Kategorien - nicht
        // in Punkten. Erst die Normierung macht daraus die erreichbaren Punkte.
        public double Weight { get; set; }

        public TaskItem Task { get; set; } = null!;
    }
}
