using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Domain.Entities
{
    public class Submission
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

        // Nur bei Status Failed gefüllt: der Grund in einer Form, die dem Teilnehmer
        // weiterhilft. Technische Details bleiben im Log.
        public string ErrorMessage { get; set; } = string.Empty;

        public TaskItem Task { get; set; } = null!;
        public ICollection<SubmissionFile> Files { get; set; } = [];
        public EvaluationResult? EvaluationResult { get; set; }
    }
}