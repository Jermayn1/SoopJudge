using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Backend.Domain.Entities
{
    public class CategoryResult
    {
        public Guid Id { get; set; }
        public Guid EvaluationResultId { get; set; }
        public EvaluationCategory Category { get; set; }
        public bool Passed { get; set; }
        public int Points { get; set; }
        public int MaxPoints { get; set; }
        public string ErrorTip { get; set; } = string.Empty;

        public EvaluationResult EvaluationResult { get; set; } = null!;
        public ICollection<TestCaseResult> TestCaseResults { get; set; } = [];
    }
}