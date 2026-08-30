using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Repositories
{
    public interface IEvaluationResultRepository
    {
        Task<EvaluationResult?> GetBySubmissionIdAsync(Guid submissionId);
        Task AddAsync(EvaluationResult result);
    }
}