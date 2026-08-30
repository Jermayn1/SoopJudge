namespace SoopWorkshop.Backend.Domain.Entities
{
    public class SubmissionFile
    {
        public Guid Id { get; set; }
        public Guid SubmissionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public Submission Submission { get; set; } = null!;
    }
}