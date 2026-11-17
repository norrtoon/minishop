using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.DTOs;
using MiniShop.Models;
using MiniShop.Services;

namespace MiniShop.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll([FromQuery] ProductQueryDto query) =>
        Ok(await productService.GetPagedAsync(query));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id) =>
        Ok(await productService.GetByIdAsync(id));

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDto>> Create(ProductUpsertDto dto)
    {
        var created = await productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductUpsertDto dto) =>
        Ok(await productService.UpdateAsync(id, dto));

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await productService.DeleteAsync(id);
        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:int}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UploadImage(int id, IFormFile? file) =>
        Ok(await productService.SetImageAsync(id, file));

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}/image")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> RemoveImage(int id) =>
        Ok(await productService.RemoveImageAsync(id));
}
