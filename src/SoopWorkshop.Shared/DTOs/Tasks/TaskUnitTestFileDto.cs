namespace SoopWorkshop.Shared.DTOs.Tasks
{
    // Lese-DTO für eine hinterlegte JUnit-Quelldatei.
    // Zum Anlegen und Ändern dienen die DTOs unter DTOs/Tasks/Requests.
    public class TaskUnitTestFileDto
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsVisibleToParticipant { get; set; }
    }
}
