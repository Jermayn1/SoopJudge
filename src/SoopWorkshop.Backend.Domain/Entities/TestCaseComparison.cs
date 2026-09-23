namespace SoopWorkshop.Backend.Domain.Entities
{
    // Ein einzelner Vergleich innerhalb einer Teilprüfung - bei JUnit ein
    // assertEquals, assertTrue und Co. Eine Testmethode vergleicht oft mehrere
    // Werte, und jeder davon soll in der Anzeige für sich stehen.
    public class TestCaseComparison
    {
        public Guid Id { get; set; }
        public Guid TestCaseResultId { get; set; }

        // Der geprüfte Ausdruck aus der Testdatei, etwa "Ggt.ggt(9, 28)". Leer,
        // wenn er nichts aufruft, der sich zu zeigen lohnt.
        public string Call { get; set; } = string.Empty;

        public string Expected { get; set; } = string.Empty;
        public string Actual { get; set; } = string.Empty;
        public bool Passed { get; set; }

        // Reihenfolge der Aufrufe im Testlauf.
        public int Order { get; set; }

        public TestCaseResult TestCaseResult { get; set; } = null!;
    }
}
