using Microsoft.EntityFrameworkCore;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Infrastructure.Persistence.Repositories
{
    public class TaskUnitTestFileRepository : ITaskUnitTestFileRepository
    {
        private readonly AppDbContext _context;

        public TaskUnitTestFileRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskUnitTestFile>> GetByTaskItemIdAsync(Guid taskItemId)
        {
            return await _context.TaskUnitTestFiles
                .Where(file => file.TaskItemId == taskItemId)
                .OrderBy(file => file.Order)
                .ToListAsync();
        }

        public async Task<TaskUnitTestFile?> GetByIdAsync(Guid id)
        {
            return await _context.TaskUnitTestFiles.FirstOrDefaultAsync(file => file.Id == id);
        }

        public async Task AddAsync(TaskUnitTestFile file)
        {
            _context.TaskUnitTestFiles.Add(file);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TaskUnitTestFile file)
        {
            _context.TaskUnitTestFiles.Update(file);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            await _context.TaskUnitTestFiles
                .Where(file => file.Id == id)
                .ExecuteDeleteAsync();
        }

        // Alte Dateien raus, neue rein, ein SaveChanges - damit die Aufgabe
        // zwischendurch nie ohne ihre Tests dasteht.
        public async Task ReplaceForTaskItemAsync(Guid taskItemId, List<TaskUnitTestFile> files)
        {
            var existing = await _context.TaskUnitTestFiles
                .Where(file => file.TaskItemId == taskItemId)
                .ToListAsync();

            _context.TaskUnitTestFiles.RemoveRange(existing);
            _context.TaskUnitTestFiles.AddRange(files);

            await _context.SaveChangesAsync();
        }
    }
}
