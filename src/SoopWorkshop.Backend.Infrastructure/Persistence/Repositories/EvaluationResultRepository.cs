using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class EvaluationResultRepository : IEvaluationResultRepository
    {
        private readonly AppDbContext _context;

        public EvaluationResultRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EvaluationResult?> GetBySubmissionIdAsync(Guid submissionId)
        {
            return await _context.EvaluationResults
                .Include(e => e.CategoryResults)
                    .ThenInclude(c => c.TestCaseResults)
                .FirstOrDefaultAsync(e => e.SubmissionId == submissionId);
        }

        public async Task AddAsync(EvaluationResult result)
        {
            _context.EvaluationResults.Add(result);
            await _context.SaveChangesAsync();
        }
    }
}