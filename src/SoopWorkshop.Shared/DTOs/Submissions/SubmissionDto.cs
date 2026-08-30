using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Shared.DTOs.Submissions
{
    public class SubmissionDto
    {
        public Guid Id { get; set; }
        public Guid TaskItemId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public SubmissionStatus Status { get; set; }
    }
}