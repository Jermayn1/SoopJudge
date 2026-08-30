namespace SoopWorkshop.Shared.DTOs.Tasks
{
    public class TaskCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisible { get; set; }

        // Name des Symbols in der Seitenleiste. Leer heißt "kein eigenes";
        // welche Namen es gibt, weiß das Frontend.
        public string IconName { get; set; } = string.Empty;

        public List<TaskItemDto> Tasks { get; set; } = [];
    }
}