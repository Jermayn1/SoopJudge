using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class TaskCategoryRepository : ITaskCategoryRepository
    {
        private readonly AppDbContext _context;

        public TaskCategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskCategory>> GetAllAsync()
        {
            return await _context.TaskCategories
                .Include(c => c.Tasks)
                .OrderBy(c => c.Order)
                .ToListAsync();
        }

        public async Task<List<TaskCategory>> GetAllVisibleAsync()
        {
            return await _context.TaskCategories
                .Include(c => c.Tasks.Where(t => t.IsVisible))
                .Where(c => c.IsVisible)
                .OrderBy(c => c.Order)
                .ToListAsync();
        }

        public async Task<TaskCategory?> GetByIdAsync(Guid id)
        {
            return await _context.TaskCategories
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.TaskCategories.AnyAsync(c => c.Id == id, cancellationToken);
        }

        public async Task AddAsync(TaskCategory category)
        {
            _context.TaskCategories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TaskCategory category)
        {
            _context.TaskCategories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var category = await _context.TaskCategories.FindAsync(id);
            if (category is not null)
            {
                _context.TaskCategories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}