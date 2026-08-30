using SoopWorkshop.Backend.Domain.Entities;

namespace SoopWorkshop.Backend.Application.Repositories
{
    public interface ITaskCategoryWeightRepository
    {
        Task<List<TaskCategoryWeight>> GetByTaskItemIdAsync(Guid taskItemId);

        // Ersetzt alle Gewichte einer Aufgabe. Eine leere Liste bedeutet:
        // wieder die Standardgewichte aus der Konfiguration verwenden.
        Task ReplaceForTaskItemAsync(Guid taskItemId, List<TaskCategoryWeight> weights);
    }
}
