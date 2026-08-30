using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    // Setzt alle JUnit-Dateien einer Aufgabe in einem Aufruf. Gedacht für einen
    // Editor, in dem mehrere Dateien nebeneinander bearbeitet und zusammen
    // gespeichert werden - was hier nicht drinsteht, wird gelöscht.
    public class SaveTaskUnitTestFilesDto
    {
        [Required]
        public Guid TaskItemId { get; set; }

        public List<SaveTaskUnitTestFileEntryDto> Files { get; set; } = [];
    }

    public class SaveTaskUnitTestFileEntryDto
    {
        [Required(ErrorMessage = "Der Dateiname ist erforderlich.")]
        [MaxLength(255, ErrorMessage = "Der Dateiname darf maximal 255 Zeichen lang sein.")]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Der Inhalt der Testdatei ist erforderlich.")]
        public string Content { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        public int Order { get; set; }

        public bool IsVisibleToParticipant { get; set; }
    }
}
