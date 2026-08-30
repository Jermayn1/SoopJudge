namespace SoopWorkshop.Shared.DTOs.Evaluation
{
    public class EvaluationResultDto
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public List<CategoryResultDto> CategoryResults { get; set; } = [];
    }
}