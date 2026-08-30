using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Evaluation
{
    public class CategoryResultDto
    {
        public Guid Id { get; set; }
        public EvaluationCategory Category { get; set; }
        public bool Passed { get; set; }
        public int Points { get; set; }
        public int MaxPoints { get; set; }
        public string ErrorTip { get; set; } = string.Empty;
        public List<TestCaseResultDto> TestCaseResults { get; set; } = [];
    }
}