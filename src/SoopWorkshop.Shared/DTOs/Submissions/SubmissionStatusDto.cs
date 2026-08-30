using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Submissions
{
    // Antwort von GET /api/submissions/{id}/status.
    // Bewusst getrennt vom Ergebnis: das Frontend muss "läuft noch",
    // "fehlgeschlagen" und "nicht gefunden" unterscheiden können.
    public class SubmissionStatusDto
    {
        public Guid Id { get; set; }

        // Zu welcher Aufgabe die Abgabe gehört. Das Frontend braucht das für den
        // Zurück-Link von der Ergebnisseite — ohne dieses Feld müsste es die
        // Aufgabe erst über einen zweiten Aufruf ermitteln oder sie sich merken,
        // und ein direkt aufgerufener Ergebnis-Link hätte gar keinen Weg zurück.
        public Guid TaskItemId { get; set; }

        public SubmissionStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }

        // Nur bei Status Failed gefüllt.
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
