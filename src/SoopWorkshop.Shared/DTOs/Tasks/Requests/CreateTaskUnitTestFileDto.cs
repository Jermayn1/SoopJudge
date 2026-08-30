using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    public class CreateTaskUnitTestFileDto
    {
        [Required]
        public Guid TaskItemId { get; set; }

        [Required(ErrorMessage = "Der Dateiname ist erforderlich.")]
        [MaxLength(255, ErrorMessage = "Der Dateiname darf maximal 255 Zeichen lang sein.")]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Der Inhalt der Testdatei ist erforderlich.")]
        public string Content { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        public int Order { get; set; }

        // Direkt beim Anlegen setzbar - kein zusätzlicher Aufruf nötig, anders
        // als bei der Sichtbarkeit von Kategorie und Aufgabe.
        public bool IsVisibleToParticipant { get; set; }
    }
}
