namespace SoopWorkshop.Backend.Domain.Entities
{
    public class TaskHint
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Order { get; set; }

        public TaskItem Task { get; set; } = null!;
    }
}