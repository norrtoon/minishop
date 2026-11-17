using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniShop.DTOs;
using MiniShop.Models;
using MiniShop.Security;
using MiniShop.Services;

namespace MiniShop.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("my")]
    public async Task<ActionResult<List<OrderDto>>> GetMy() =>
        Ok(await orderService.GetForUserAsync(User.GetUserId()));

    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    [ProducesResponseType<List<OrderDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<OrderDto>>> GetAll([FromQuery] OrderStatus? status) =>
        Ok(await orderService.GetAllAsync(status));

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(int id) =>
        Ok(await orderService.GetByIdAsync(id, User.GetUserId(), User.IsAdmin()));

    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderDto dto)
    {
        var order = await orderService.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, UpdateOrderStatusDto dto) =>
        Ok(await orderService.UpdateStatusAsync(id, dto.Status!.Value));

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderDto>> Cancel(int id) =>
        Ok(await orderService.CancelAsync(id, User.GetUserId(), User.IsAdmin()));

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        await orderService.DeleteAsync(id);
        return NoContent();
    }
}
