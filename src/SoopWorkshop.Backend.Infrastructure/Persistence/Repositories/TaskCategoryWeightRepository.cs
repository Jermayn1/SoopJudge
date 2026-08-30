using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class TaskCategoryWeightRepository : ITaskCategoryWeightRepository
    {
        private readonly AppDbContext _context;

        public TaskCategoryWeightRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskCategoryWeight>> GetByTaskItemIdAsync(Guid taskItemId)
        {
            return await _context.TaskCategoryWeights
                .Where(weight => weight.TaskItemId == taskItemId)
                .OrderBy(weight => weight.Category)
                .ToListAsync();
        }

        public async Task ReplaceForTaskItemAsync(Guid taskItemId, List<TaskCategoryWeight> weights)
        {
            var existing = await _context.TaskCategoryWeights
                .Where(weight => weight.TaskItemId == taskItemId)
                .ToListAsync();

            _context.TaskCategoryWeights.RemoveRange(existing);
            _context.TaskCategoryWeights.AddRange(weights);

            await _context.SaveChangesAsync();
        }
    }
}
