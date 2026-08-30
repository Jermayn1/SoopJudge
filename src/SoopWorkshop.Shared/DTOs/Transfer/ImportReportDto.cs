namespace SoopWorkshop.Shared.DTOs.Transfer
{
    // Was ein Import tun wird beziehungsweise getan hat.
    //
    // Dieselbe Form für Vorschau und Ausführung, aus demselben Rechenweg -
    // eine Vorschau, die etwas anderes anzeigt als danach passiert, wäre
    // schlimmer als keine.
    public class ImportReportDto
    {
        // Solange hier etwas steht, wurde NICHTS geschrieben. Alle Fehler auf
        // einmal, nicht nur der erste: bei vierzig Aufgaben will niemand
        // vierzigmal probieren.
        public List<string> Errors { get; set; } = [];

        // Auffälligkeiten, die den Import nicht verhindern.
        public List<string> Warnings { get; set; } = [];

        public int CategoriesCreated { get; set; }
        public int CategoriesUpdated { get; set; }
        public int CategoriesDeleted { get; set; }

        public int TasksCreated { get; set; }
        public int TasksUpdated { get; set; }
        public int TasksDeleted { get; set; }

        // Abgaben werden nie importiert. Beim Ersetzen gehen sie aber per
        // Cascade mit den gelöschten Aufgaben verloren - deshalb steht die Zahl
        // hier, damit der Dialog sie nennen kann.
        public int SubmissionsDeleted { get; set; }

        public bool IsValid => Errors.Count == 0;
    }
}
