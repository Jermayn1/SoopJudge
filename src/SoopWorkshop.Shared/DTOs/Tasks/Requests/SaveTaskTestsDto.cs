using System.ComponentModel.DataAnnotations;

namespace SoopWorkshop.Shared.DTOs.Tasks.Requests
{
    // Setzt alle Konsolen-Testfälle einer Aufgabe in einem Aufruf, wie es
    // SaveTaskUnitTestFilesDto für die JUnit-Dateien tut - was hier nicht
    // drinsteht, wird gelöscht.
    //
    // Der Editor bearbeitet die Testfälle einer Aufgabe als Block. Ohne diesen
    // Endpunkt müsste er pro Zeile einzeln anlegen, ändern und löschen und
    // könnte mitten in der Folge scheitern - die Aufgabe stünde dann mit einer
    // halb gespeicherten Prüfung da.
    public class SaveTaskTestsDto
    {
        [Required]
        public Guid TaskItemId { get; set; }

        public List<SaveTaskTestEntryDto> Tests { get; set; } = [];
    }

    public class SaveTaskTestEntryDto
    {
        // Darf leer sein: eine Aufgabe ohne Eingabe hat keine.
        public string Input { get; set; } = string.Empty;

        [Required(ErrorMessage = "Die erwartete Ausgabe ist erforderlich.")]
        public string ExpectedOutput { get; set; } = string.Empty;

        // Der Satz, den der Teilnehmer als Beschreibung der Teilprüfung liest.
        // Er ist eine Aussage über die Abgabe, nicht über das Ergebnis des Laufs
        // ("Das Programm addiert zwei positive Zahlen") - ob sie zutrifft, sagt
        // das Häkchen daneben.
        [Required(ErrorMessage = "Die Beschreibung ist erforderlich.")]
        [MaxLength(500, ErrorMessage = "Die Beschreibung darf maximal 500 Zeichen lang sein.")]
        public string Description { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")]
        public int Order { get; set; }
    }
}
