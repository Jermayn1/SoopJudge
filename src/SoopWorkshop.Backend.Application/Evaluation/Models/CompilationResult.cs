namespace SoopWorkshop.Backend.Application.Evaluation.Models
{
    // Ergebnis des Kompiliervorgangs. Der CompilabilityChecker legt es im
    // EvaluationContext ab, alle späteren Checker lesen es von dort.
    public class CompilationResult
    {
        public bool Success { get; set; }
        public string WorkingDirectory { get; set; } = string.Empty;
        public string ErrorOutput { get; set; } = string.Empty;
        public string? MainClassName { get; set; }
    }
}
