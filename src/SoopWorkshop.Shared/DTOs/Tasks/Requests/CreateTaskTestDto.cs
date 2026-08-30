using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    public class CreateTaskTestDto
    {
        [Required]
        public Guid TaskItemId { get; set; }
        
        public string Input { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Die erwartete Ausgabe ist erforderlich.")]
        public string ExpectedOutput { get; set; } = string.Empty;
        
        // 500 und nicht mehr: die Spalte in der Datenbank ist 500 Zeichen lang
        // (TaskTestConfiguration). Eine höhere Grenze hier ließe eine längere
        // Beschreibung die Validierung passieren und erst in der Datenbank
        // scheitern - mit einer Meldung, die niemandem sagt, was zu tun ist.
        // Fachlich ist die Beschreibung ohnehin ein Satz, kein Absatz.
        [Required(ErrorMessage = "Die Beschreibung ist erforderlich.")]
        [MaxLength(500, ErrorMessage = "Die Beschreibung darf maximal 500 Zeichen lang sein.")]
        public string Description { get; set; } = string.Empty;
        
        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        public int Order { get; set; }
    }
}