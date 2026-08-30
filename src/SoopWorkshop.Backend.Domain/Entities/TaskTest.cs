namespace SoopWorkshop.Backend.Domain.Entities
{
    public class TaskTest
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }

        public TaskItem Task { get; set; } = null!;
    }
}