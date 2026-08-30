using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Tasks;

namespace SoopWorkshop.Backend.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskCategoryService _categoryService;
        private readonly ITaskItemService _taskItemService;

        public TasksController(ITaskCategoryService categoryService, ITaskItemService taskItemService)
        {
            _categoryService = categoryService;
            _taskItemService = taskItemService;
        }

        // Gibt alle sichtbaren Kategorien und ihre sichtbaren Aufgaben zurück
        [HttpGet("categories")]
        [ProducesResponseType<List<TaskCategoryDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TaskCategoryDto>>> GetVisibleCategories()
        {
            var result = await _categoryService.GetAllVisibleAsync();

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        // Gibt die Details einer Aufgabe zurück (Aufgabenstellung, Tipps, etc.)
        [HttpGet("tasks/{id:guid}")]
        [ProducesResponseType<TaskItemDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskItemDto>> GetTaskById(Guid id)
        {
            var result = await _taskItemService.GetByIdAsync(id);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }
    }
}
