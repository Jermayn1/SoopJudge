using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Repositories
{
    public interface ITaskCategoryRepository
    {
        Task<List<TaskCategory>> GetAllAsync();
        Task<List<TaskCategory>> GetAllVisibleAsync();
        Task<TaskCategory?> GetByIdAsync(Guid id);

        // Reine Existenzprüfung, wie bei ITaskItemRepository - GetByIdAsync lädt
        // die Aufgaben der Kategorie mit, die dafür niemand braucht.
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(TaskCategory category);
        Task UpdateAsync(TaskCategory category);
        Task DeleteAsync(Guid id);
    }
}