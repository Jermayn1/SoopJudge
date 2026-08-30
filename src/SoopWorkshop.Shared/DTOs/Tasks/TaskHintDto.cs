namespace SoopWorkshop.Shared.DTOs.Tasks
{
    public class TaskHintDto
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}