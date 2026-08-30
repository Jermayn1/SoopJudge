namespace SoopWorkshop.Backend.Domain.Entities
{
    public class TaskCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisible { get; set; }

        // Name des Symbols in der Seitenleiste, z. B. "Layers".
        //
        // Früher haben die Seitenleisten dafür den Kategorienamen ausgewertet
        // ("oop" -> Layers, sonst BookOpen). Damit wechselte das Symbol beim
        // Umbenennen, und eine neue Kategorie bekam nie ein eigenes. Welche
        // Namen es gibt, weiß das Frontend (src/admin/icons.ts) - hier steht
        // nur der Name; ein unbekannter fällt dort auf das Standardsymbol
        // zurück.
        //
        // Leer heißt "kein eigenes Symbol".
        public string IconName { get; set; } = string.Empty;

        public ICollection<TaskItem> Tasks { get; set; } = [];
    }
}