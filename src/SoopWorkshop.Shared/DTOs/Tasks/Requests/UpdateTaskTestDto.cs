using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    public class UpdateTaskTestDto
    {
        [Required]
        public Guid Id { get; set; }
        
        public string Input { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Die erwartete Ausgabe ist erforderlich.")]
        public string ExpectedOutput { get; set; } = string.Empty;
        
        // 500, nicht 2000 - siehe CreateTaskTestDto: die Datenbankspalte gibt die
        // Grenze vor, nicht dieses Attribut.
        [Required(ErrorMessage = "Eine Beschreibung ist erforderlich.")]
        [MaxLength(500, ErrorMessage = "Die Beschreibung darf maximal 500 Zeichen lang sein.")]
        public string Description { get; set; } = string.Empty;
        
        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        public int Order { get; set; }
    }
}