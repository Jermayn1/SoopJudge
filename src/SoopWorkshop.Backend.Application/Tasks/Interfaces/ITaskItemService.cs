using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Interfaces
{
    public interface ITaskItemService
    {
        Task<Result<List<TaskItemDto>>> GetAllAsync();
        Task<Result<TaskItemDto>> GetByIdAsync(Guid id);
        Task<Result<TaskItemDto>> CreateAsync(CreateTaskItemDto dto);
        Task<Result<TaskItemDto>> UpdateAsync(UpdateTaskItemDto dto);
        Task<Result<bool>> DeleteAsync(Guid id);
        Task<Result<bool>> ToggleVisibilityAsync(Guid id);
    }
}