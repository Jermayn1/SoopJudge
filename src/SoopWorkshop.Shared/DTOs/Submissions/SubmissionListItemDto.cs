using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Submissions
{
    // Eine Zeile in der Abgaben-Übersicht des Panels.
    //
    // Bewusst OHNE die Dateien und ohne die Teilprüfungen: die Liste zeigt
    // hunderte Zeilen, und beides braucht sie nicht. Wer die Auswertung sehen
    // will, folgt dem Link auf die Ergebnisseite - dieselbe, die auch der
    // Teilnehmer sieht.
    public class SubmissionListItemDto
    {
        public Guid Id { get; set; }

        public Guid TaskItemId { get; set; }

        public string TaskTitle { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public SubmissionStatus Status { get; set; }

        // Nur bei Status Failed gefüllt.
        public string ErrorMessage { get; set; } = string.Empty;

        // Null, solange keine Auswertung vorliegt - und das ist etwas anderes
        // als 0 Punkte. Eine 0 wäre eine Aussage über die Lösung, null sagt
        // nur, dass noch nichts bewertet wurde.
        public int? TotalScore { get; set; }

        public int? MaxScore { get; set; }

        // Kein FileCount: dafür müsste die Abfrage die Dateien mitladen
        // (deren Inhalt bis zu 1 MB je Stück beträgt) oder eine zweite
        // Abfrage stellen. Für eine Liste, die ohnehin auf die Ergebnisseite
        // verlinkt, ist beides den Preis nicht wert - dort stehen die
        // Dateinamen vollständig.
    }
}
