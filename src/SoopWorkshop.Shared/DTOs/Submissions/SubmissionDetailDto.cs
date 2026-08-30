namespace SoopWorkshop.Shared.DTOs.Submissions
{
    // Eine einzelne Abgabe mit ihren Dateien, für die Ansicht im Panel.
    //
    // Anders als SubmissionListItemDto trägt dieser Typ die vollen Inhalte: er
    // beantwortet eine Abgabe auf einmal, nicht hunderte Zeilen. Für die Liste
    // bleibt es dabei, dass die Dateien draußen bleiben.
    //
    // Bewusst OHNE Status, Fehlermeldung und Punktzahl. Die Seite bezieht den
    // Auswertungsstand aus /status und /result — es gibt genau eine Quelle
    // dafür. Zwei Quellen widersprechen sich früher oder später, und dann steht
    // "fertig" neben einem laufenden Balken.
    public class SubmissionDetailDto
    {
        public Guid Id { get; set; }

        public Guid TaskItemId { get; set; }

        public string TaskTitle { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        // Nach Dateiname sortiert. SubmissionFile hat keine Order-Spalte, also
        // entschiede sonst die Datenbank je Abfrage neu.
        public List<SubmissionFileDto> Files { get; set; } = [];
    }
}
