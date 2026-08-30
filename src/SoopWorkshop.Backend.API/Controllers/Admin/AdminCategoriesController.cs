using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoopWorkshop.Backend.Application.Tasks.Interfaces;
using SoopWorkshop.Shared.DTOs.Tasks;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;

namespace SoopWorkshop.Backend.API.Controllers.Admin
{
    [ApiController]
    [Authorize]
    [Route("api/admin/categories")]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly ITaskCategoryService _categoryService;

        public AdminCategoriesController(ITaskCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // Gibt alle Kategorien zurück
        [HttpGet]
        [ProducesResponseType<List<TaskCategoryDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TaskCategoryDto>>> GetAll()
        {
            var result = await _categoryService.GetAllAsync();

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPost]
        [ProducesResponseType<TaskCategoryDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaskCategoryDto>> Create([FromBody] CreateTaskCategoryDto dto)
        {
            var result = await _categoryService.CreateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : BadRequest(result.ErrorMessage);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType<TaskCategoryDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TaskCategoryDto>> Update(Guid id, [FromBody] UpdateTaskCategoryDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Die ID in der URL stimmt nicht mit der ID im Body überein.");

            var result = await _categoryService.UpdateAsync(dto);

            return result.IsSuccess
                ? Ok(result.Value)
                : NotFound(result.ErrorMessage);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(Guid id)
        {
            var result = await _categoryService.DeleteAsync(id);

            return result.IsSuccess
                ? NoContent()
                : NotFound(result.ErrorMessage);
        }

        // Toggelt die Sichtbarkeit einer Kategorie
        [HttpPatch("{id:guid}/visibility")]
        [ProducesResponseType<VisibilityStateDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<string>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VisibilityStateDto>> ToggleVisibility(Guid id)
        {
            var result = await _categoryService.ToggleVisibilityAsync(id);

            return result.IsSuccess
                ? Ok(new VisibilityStateDto { IsVisible = result.Value })
                : NotFound(result.ErrorMessage);
        }
    }
}
