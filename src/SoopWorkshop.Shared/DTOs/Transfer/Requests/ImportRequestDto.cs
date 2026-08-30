using System.ComponentModel.DataAnnotations;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Transfer.Requests
{
    // Dieselbe Form für die Vorschau und für das Ausführen. Die Vorschau ist
    // genau derselbe Aufruf ohne Schreiben - sonst könnten die beiden
    // auseinanderlaufen.
    public class ImportRequestDto
    {
        [Required]
        public TaskBundleDto Bundle { get; set; } = new();

        public ImportMode Mode { get; set; } = ImportMode.Merge;
    }
}
