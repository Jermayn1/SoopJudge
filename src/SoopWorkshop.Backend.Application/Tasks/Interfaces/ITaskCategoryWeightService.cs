using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.Application.Tasks.Interfaces
{
    public interface ITaskCategoryWeightService
    {
        Task<Result<List<TaskCategoryWeightDto>>> GetByTaskItemIdAsync(Guid taskItemId);
        Task<Result<List<TaskCategoryWeightDto>>> SaveAllAsync(SaveTaskCategoryWeightsDto dto);
    }
}
