namespace SoopWorkshop.Shared.DTOs.Tasks
{
    // Lese-DTO für einen Konsolen-Testfall.
    // Zum Anlegen und Ändern dienen CreateTaskTestDto und UpdateTaskTestDto
    // unter DTOs/Tasks/Requests.
    public class TaskTestDto
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
