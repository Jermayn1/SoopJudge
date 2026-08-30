using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    [ApiController]
    [Authorize]
    [Route("api/admin/tasks/{taskItemId:guid}/tests")]
    public class AdminTaskTestsController : ControllerBase
    {
        private readonly ITaskTestService _taskTestService;

        public AdminTaskTestsController(ITaskTestService taskTestService)
        {
            _taskTestService = taskTestService;
        }

        [HttpGet]
        [ProducesResponseType<List<TaskTestDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TaskTestDto>>> GetByTaskItem(Guid taskItemId)
        {
            var result = await _taskTestService.GetByTaskItemIdAsync(taskItemId);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPost]
        [ProducesResponseType<TaskTestDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskTestDto>> Create(Guid taskItemId, [FromBody] CreateTaskTestDto dto)
        {
            if (taskItemId != dto.TaskItemId)
                return BadRequest("Die TaskItemId in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _taskTestService.CreateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Setzt alle Testfälle der Aufgabe auf einmal. Was nicht im Body steht,
        // wird gelöscht - gedacht für einen Editor, der die Testfälle einer
        // Aufgabe als Block bearbeitet und zusammen speichert.
        [HttpPut]
        [ProducesResponseType<List<TaskTestDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TaskTestDto>>> SaveAll(
            Guid taskItemId,
            [FromBody] SaveTaskTestsDto dto)
        {
            if (taskItemId != dto.TaskItemId)
                return BadRequest("Die TaskItemId in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _taskTestService.SaveAllAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType<TaskTestDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskTestDto>> Update(Guid taskItemId, Guid id, [FromBody] UpdateTaskTestDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Die ID in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _taskTestService.UpdateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid taskItemId, Guid id)
        {
            var result = await _taskTestService.DeleteAsync(id);

            return result.IsSuccess
                ? NoContent()
                : NotFound(result.ErrorMessage);
        }
    }
}
