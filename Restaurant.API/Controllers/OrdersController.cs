using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Models;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [Authorize(Policy = AppPermissions.Orders)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders([FromQuery] OrderQueryDto query)
    {
        var result = await _orderService.GetOrdersPageAsync(query);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        return Ok(result.Items.Select(order => PermissionAuthorization.ForViewer(order, User)));
    }

    [HttpGet("paged"), Authorize(Policy = AppPermissions.Orders)]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetOrdersPage([FromQuery] OrderQueryDto query)
    {
        var page = await _orderService.GetOrdersPageAsync(query);
        return Ok(page with { Items = page.Items.Select(order => PermissionAuthorization.ForViewer(order, User)).ToList() });
    }

    [HttpGet("byrequest/{requestId}"), Authorize(Policy = AppPermissions.NewOrder)]
    public async Task<ActionResult<OrderDto>> ByRequest(string requestId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();
        var order = await _orderService.GetByRequestAsync(requestId, userId);
        return order == null ? NotFound() : Ok(PermissionAuthorization.ForViewer(order, User));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        if (!AppPermissions.Has(User, AppPermissions.Orders) && !AppPermissions.Has(User, AppPermissions.NewOrder)) return Forbid();
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound(new { message = $"Order with ID {id} was not found." });
        }
        if (!AppPermissions.Has(User, AppPermissions.Orders) && order.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
        return Ok(PermissionAuthorization.ForViewer(order, User));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] OrderCreateDto orderCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var createdOrder = await _orderService.CreateOrderAsync(
                orderCreateDto,
                userId,
                CanApplyDiscount());
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, PermissionAuthorization.ForViewer(createdOrder, User));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("checkout")]
    [Authorize(Policy = AppPermissions.NewOrder)]
    public async Task<ActionResult<OrderDto>> Checkout([FromBody] OrderCheckoutCreateDto checkoutDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        try
        {
            var createdOrder = await _orderService.CheckoutAsync(
                checkoutDto,
                userId,
                CanApplyDiscount());
            return CreatedAtAction(nameof(GetOrder), new { id = createdOrder.Id }, PermissionAuthorization.ForViewer(createdOrder, User));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AppPermissions.Orders)]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] OrderUpdateDto orderUpdateDto)
    {
        if (id != orderUpdateDto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        bool success;
        try
        {
            success = await _orderService.UpdateOrderAsync(
                orderUpdateDto,
                userId,
                CanApplyDiscount(),
                CanVoidOrder());
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        if (!success)
        {
            return NotFound(new { message = $"Order with ID {id} was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        bool success;
        try
        {
            success = await _orderService.DeleteOrderAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        if (!success)
        {
            return NotFound(new { message = $"Order with ID {id} was not found." });
        }

        return NoContent();
    }

    private bool CanApplyDiscount()
    {
        return User.IsInRole(AppRoles.Admin)
            || User.HasClaim("Permission", "Permission.POS.ApplyDiscount");
    }

    private bool CanVoidOrder()
    {
        return User.IsInRole(AppRoles.Admin)
            || User.HasClaim("Permission", "Permission.Orders.VoidOrder");
    }
}
