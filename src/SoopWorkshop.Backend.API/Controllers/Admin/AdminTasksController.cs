using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Common;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    [ApiController]
    [Authorize]
    [Route("api/admin/tasks")]
    public class AdminTasksController : ControllerBase
    {
        private readonly ITaskItemService _taskItemService;

        public AdminTasksController(ITaskItemService taskItemService)
        {
            _taskItemService = taskItemService;
        }

        // Gibt alle Aufgaben zurück
        [HttpGet]
        [ProducesResponseType<List<TaskItemDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TaskItemDto>>> GetAll()
        {
            var result = await _taskItemService.GetAllAsync();

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<TaskItemDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskItemDto>> GetById(Guid id)
        {
            var result = await _taskItemService.GetByIdAsync(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpPost]
        [ProducesResponseType<TaskItemDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskItemDto>> Create([FromBody] CreateTaskItemDto dto)
        {
            var result = await _taskItemService.CreateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType<TaskItemDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskItemDto>> Update(Guid id, [FromBody] UpdateTaskItemDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Die ID in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _taskItemService.UpdateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _taskItemService.DeleteAsync(id);

            return result.IsSuccess
                ? NoContent()
                : NotFound(result.ErrorMessage);
        }

        // Toggelt die Sichtbarkeit einer Aufgabe
        [HttpPatch("{id:guid}/visibility")]
        [ProducesResponseType<VisibilityStateDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VisibilityStateDto>> ToggleVisibility(Guid id)
        {
            var result = await _taskItemService.ToggleVisibilityAsync(id);

            if (result.IsSuccess)
                return Ok(new VisibilityStateDto { IsVisible = result.Value });

            // Zwei unterscheidbare Fehlschläge, zwei Statuscodes. "Aufgabe nicht
            // gefunden" ist 404, "der Aufgabe fehlen die Testdaten ihres Modus"
            // ist 400 — im zweiten Fall gibt es die Aufgabe ja, sie ist nur noch
            // nicht vollständig.
            return result.Failure == ResultFailure.NotFound
                ? NotFound(result.ErrorMessage)
                : BadRequest(result.ErrorMessage);
        }
    }
}
