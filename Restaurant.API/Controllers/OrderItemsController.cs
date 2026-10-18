using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

[Authorize(Policy = "Permission.Orders")]
[ApiController]
[Route("api/[controller]")]
public class OrderItemsController : ControllerBase
{
    private readonly IOrderItemService _orderItemService;

    public OrderItemsController(IOrderItemService orderItemService)
    {
        _orderItemService = orderItemService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderItemDto>>> GetOrderItems()
    {
        var items = await _orderItemService.GetAllOrderItemsAsync();
        if (!Restaurant.Models.AppPermissions.Has(User, Restaurant.Models.AppPermissions.Reports))
            foreach (var item in items) item.UnitCostPrice = 0;
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderItemDto>> GetOrderItem(int id)
    {
        var item = await _orderItemService.GetOrderItemByIdAsync(id);
        if (item == null)
        {
            return NotFound(new { message = $"Order item with ID {id} was not found." });
        }
        if (!Restaurant.Models.AppPermissions.Has(User, Restaurant.Models.AppPermissions.Reports)) item.UnitCostPrice = 0;
        return Ok(item);
    }

    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<IEnumerable<OrderItemDto>>> GetOrderItemsByOrderId(int orderId)
    {
        var items = await _orderItemService.GetOrderItemsByOrderIdAsync(orderId);
        if (!Restaurant.Models.AppPermissions.Has(User, Restaurant.Models.AppPermissions.Reports))
            foreach (var item in items) item.UnitCostPrice = 0;
        return Ok(items);
    }

    [HttpPost("order/{orderId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderItemDto>> CreateOrderItem(int orderId, [FromBody] OrderItemCreateDto orderItemCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        OrderItemDto? createdItem;
        try
        {
            createdItem = await _orderItemService.CreateOrderItemAsync(orderId, orderItemCreateDto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        if (createdItem == null)
        {
            return NotFound(new { message = $"Order with ID {orderId} was not found." });
        }

        return CreatedAtAction(nameof(GetOrderItem), new { id = createdItem.Id }, createdItem);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderItem(int id, [FromBody] OrderItemUpdateDto orderItemUpdateDto)
    {
        if (id != orderItemUpdateDto.Id)
        {
            return BadRequest(new { message = "ID in URL does not match ID in body." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        bool success;
        try
        {
            success = await _orderItemService.UpdateOrderItemAsync(orderItemUpdateDto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        if (!success)
        {
            return NotFound(new { message = "Order item or associated order was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        bool success;
        try
        {
            success = await _orderItemService.DeleteOrderItemAsync(id);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        if (!success)
        {
            return NotFound(new { message = $"Order item with ID {id} was not found." });
        }

        return NoContent();
    }
}
