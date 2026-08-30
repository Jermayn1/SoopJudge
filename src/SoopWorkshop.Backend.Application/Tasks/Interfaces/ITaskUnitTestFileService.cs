using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Interfaces
{
    public interface ITaskUnitTestFileService
    {
        Task<Result<List<TaskUnitTestFileDto>>> GetByTaskItemIdAsync(Guid taskItemId);
        Task<Result<TaskUnitTestFileDto>> CreateAsync(CreateTaskUnitTestFileDto dto);
        Task<Result<TaskUnitTestFileDto>> UpdateAsync(UpdateTaskUnitTestFileDto dto);
        Task<Result<bool>> DeleteAsync(Guid id);

        // Setzt alle Dateien einer Aufgabe auf einmal.
        Task<Result<List<TaskUnitTestFileDto>>> SaveAllAsync(SaveTaskUnitTestFilesDto dto);
    }
}
