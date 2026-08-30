namespace SoopWorkshop.Shared.DTOs.Tasks
{
    // Eine geforderte Klasse samt den Methoden, die in ihr stehen müssen.
    public class TaskExpectedTypeDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }

        // Vollständige Signaturen zur Anzeige, z. B.
        // "public void einzahlen(double betrag)".
        public List<string> Methods { get; set; } = [];
    }
}
