using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.DTOs;
using MiniShop.Models;
using MiniShop.Security;
using MiniShop.Services;

namespace MiniShop.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpGet("products/{productId:int}/reviews")]
    [ProducesResponseType<ProductReviewsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReviewsDto>> GetForProduct(int productId) =>
        Ok(await reviewService.GetForProductAsync(productId, User.FindUserId()));

    [Authorize]
    [HttpPost("products/{productId:int}/reviews")]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Create(int productId, ReviewUpsertDto dto)
    {
        var review = await reviewService.CreateAsync(productId, User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetForProduct), new { productId }, review);
    }

    [Authorize]
    [HttpPut("reviews/{id:int}")]
    [ProducesResponseType<ReviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> Update(int id, ReviewUpsertDto dto) =>
        Ok(await reviewService.UpdateAsync(id, User.GetUserId(), dto));

    [Authorize]
    [HttpDelete("reviews/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await reviewService.DeleteAsync(id, User.GetUserId(), User.IsAdmin());
        return NoContent();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("reviews")]
    public async Task<ActionResult<List<ReviewDto>>> GetLatest([FromQuery] int count = 100) =>
        Ok(await reviewService.GetLatestAsync(Math.Clamp(count, 1, 500)));
}
