namespace SoopWorkshop.Shared.DTOs.Evaluation
{
    public class TestCaseComparisonDto
    {
        // Der geprüfte Ausdruck, etwa "Ggt.ggt(9, 28)". Leer, wenn es keinen
        // gibt, der sich zu zeigen lohnt.
        public string Call { get; set; } = string.Empty;

        public string Expected { get; set; } = string.Empty;
        public string Actual { get; set; } = string.Empty;
        public bool Passed { get; set; }
    }
}
