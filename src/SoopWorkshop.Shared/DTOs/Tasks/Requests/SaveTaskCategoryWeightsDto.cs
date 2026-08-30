using System.ComponentModel.DataAnnotations;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    // Setzt die Gewichte einer Aufgabe in einem Aufruf. Eine leere Liste stellt
    // die Standardgewichte aus der Konfiguration wieder her.
    public class SaveTaskCategoryWeightsDto
    {
        [Required]
        public Guid TaskItemId { get; set; }

        public List<SaveTaskCategoryWeightEntryDto> Weights { get; set; } = [];
    }

    public class SaveTaskCategoryWeightEntryDto
    {
        public EvaluationCategory Category { get; set; }

        // Relativ zu den anderen Kategorien, nicht in Punkten. Null oder negativ
        // ergibt keine sinnvolle Verteilung und wird abgelehnt.
        [Range(0.0001, double.MaxValue, ErrorMessage = "Das Gewicht muss größer als 0 sein.")]
        public double Weight { get; set; }
    }
}
