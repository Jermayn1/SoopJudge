using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    // JUnit-Dateien einer Aufgabe. Bewusst bequemer geschnitten als die älteren
    // Admin-Endpunkte: die Sichtbarkeit kommt beim Anlegen mit, und PUT auf die
    // Sammlung speichert alle Dateien in einem Aufruf.
    [ApiController]
    [Authorize]
    [Route("api/admin/tasks/{taskItemId:guid}/unittests")]
    public class AdminTaskUnitTestFilesController : ControllerBase
    {
        private readonly ITaskUnitTestFileService _service;

        public AdminTaskUnitTestFilesController(ITaskUnitTestFileService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType<List<TaskUnitTestFileDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TaskUnitTestFileDto>>> GetByTaskItem(Guid taskItemId)
        {
            var result = await _service.GetByTaskItemIdAsync(taskItemId);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPost]
        [ProducesResponseType<TaskUnitTestFileDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskUnitTestFileDto>> Create(Guid taskItemId, [FromBody] CreateTaskUnitTestFileDto dto)
        {
            if (taskItemId != dto.TaskItemId)
                return BadRequest("Die TaskItemId in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _service.CreateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Ersetzt alle Dateien der Aufgabe. Was nicht im Body steht, wird gelöscht.
        [HttpPut]
        [ProducesResponseType<List<TaskUnitTestFileDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TaskUnitTestFileDto>>> SaveAll(Guid taskItemId, [FromBody] SaveTaskUnitTestFilesDto dto)
        {
            if (taskItemId != dto.TaskItemId)
                return BadRequest("Die TaskItemId in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _service.SaveAllAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType<TaskUnitTestFileDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskUnitTestFileDto>> Update(Guid taskItemId, Guid id, [FromBody] UpdateTaskUnitTestFileDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Die ID in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _service.UpdateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid taskItemId, Guid id)
        {
            var result = await _service.DeleteAsync(id);

            return result.IsSuccess
                ? NoContent()
                : NotFound(result.ErrorMessage);
        }
    }
}
