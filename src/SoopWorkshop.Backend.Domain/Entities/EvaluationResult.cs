namespace SoopWorkshop.Backend.Domain.Entities
{
    public class EvaluationResult
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }

        public Submission Submission { get; set; } = null!;
        public ICollection<CategoryResult> CategoryResults { get; set; } = [];
    }
}