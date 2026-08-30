using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Repositories
{
    public interface ITaskUnitTestFileRepository
    {
        Task<List<TaskUnitTestFile>> GetByTaskItemIdAsync(Guid taskItemId);
        Task<TaskUnitTestFile?> GetByIdAsync(Guid id);
        Task AddAsync(TaskUnitTestFile file);
        Task UpdateAsync(TaskUnitTestFile file);
        Task DeleteAsync(Guid id);

        // Ersetzt alle Dateien einer Aufgabe in einem Zug. Ein Editor mit mehreren
        // Dateien speichert damit in einem Aufruf, statt Anlegen, Ändern und
        // Löschen einzeln gegeneinander abgleichen zu müssen.
        Task ReplaceForTaskItemAsync(Guid taskItemId, List<TaskUnitTestFile> files);
    }
}
