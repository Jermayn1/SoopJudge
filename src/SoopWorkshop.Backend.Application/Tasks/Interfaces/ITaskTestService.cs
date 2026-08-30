using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Interfaces
{
    public interface ITaskTestService
    {
        Task<Result<List<TaskTestDto>>> GetByTaskItemIdAsync(Guid taskItemId);
        Task<Result<TaskTestDto>> CreateAsync(CreateTaskTestDto dto);
        Task<Result<TaskTestDto>> UpdateAsync(UpdateTaskTestDto dto);
        Task<Result<bool>> DeleteAsync(Guid id);

        // Ersetzt alle Testfälle einer Aufgabe in einem Aufruf.
        Task<Result<List<TaskTestDto>>> SaveAllAsync(SaveTaskTestsDto dto);
    }
}
