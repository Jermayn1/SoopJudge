using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class TaskTestRepository : ITaskTestRepository
    {
        private readonly AppDbContext _context;

        public TaskTestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskTest>> GetByTaskItemIdAsync(Guid taskItemId)
        {
            return await _context.TaskTests
                .Where(t => t.TaskItemId == taskItemId)
                .OrderBy(t => t.Order)
                .ToListAsync();
        }

        public async Task<TaskTest?> GetByIdAsync(Guid id)
        {
            return await _context.TaskTests.FindAsync(id);
        }

        public async Task AddAsync(TaskTest test)
        {
            _context.TaskTests.Add(test);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TaskTest test)
        {
            _context.TaskTests.Update(test);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var test = await _context.TaskTests.FindAsync(id);
            if (test is not null)
            {
                _context.TaskTests.Remove(test);
                await _context.SaveChangesAsync();
            }
        }

        // Alte Testfälle raus, neue rein, ein SaveChanges - damit die Aufgabe
        // zwischendurch nie ohne ihre Prüfungen dasteht.
        public async Task ReplaceForTaskItemAsync(Guid taskItemId, List<TaskTest> tests)
        {
            var existing = await _context.TaskTests
                .Where(test => test.TaskItemId == taskItemId)
                .ToListAsync();

            _context.TaskTests.RemoveRange(existing);
            _context.TaskTests.AddRange(tests);

            await _context.SaveChangesAsync();
        }
    }
}