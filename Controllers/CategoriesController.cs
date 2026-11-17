using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.DTOs;
using MiniShop.Models;
using MiniShop.Services;

namespace MiniShop.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll() =>
        Ok(await categoryService.GetAllAsync());

    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetById(int id) =>
        Ok(await categoryService.GetByIdAsync(id));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(CategoryUpsertDto dto)
    {
        var created = await categoryService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryUpsertDto dto) =>
        Ok(await categoryService.UpdateAsync(id, dto));

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        await categoryService.DeleteAsync(id);
        return NoContent();
    }
}
