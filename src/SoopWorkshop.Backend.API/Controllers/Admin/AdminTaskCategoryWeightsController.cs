using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    // Aufgabenspezifische Gewichte der Bewertungskategorien. Ohne Eintrag gelten
    // die Standardgewichte aus Evaluation:CategoryWeights.
    [ApiController]
    [Authorize]
    [Route("api/admin/tasks/{taskItemId:guid}/weights")]
    public class AdminTaskCategoryWeightsController : ControllerBase
    {
        private readonly ITaskCategoryWeightService _service;

        public AdminTaskCategoryWeightsController(ITaskCategoryWeightService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType<List<TaskCategoryWeightDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TaskCategoryWeightDto>>> GetByTaskItem(Guid taskItemId)
        {
            var result = await _service.GetByTaskItemIdAsync(taskItemId);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Ersetzt alle Gewichte der Aufgabe. Eine leere Liste stellt die
        // Standardgewichte wieder her.
        [HttpPut]
        [ProducesResponseType<List<TaskCategoryWeightDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TaskCategoryWeightDto>>> SaveAll(Guid taskItemId, [FromBody] SaveTaskCategoryWeightsDto dto)
        {
            if (taskItemId != dto.TaskItemId)
                return BadRequest("Die TaskItemId in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _service.SaveAllAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }
    }
}
