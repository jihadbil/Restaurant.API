using Microsoft.AspNetCore.Mvc;
using Restaurant.Models.DTOs;
using Restaurant.Services.IServices;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.API.Controllers;

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
        return Ok(item);
    }

    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<IEnumerable<OrderItemDto>>> GetOrderItemsByOrderId(int orderId)
    {
        var items = await _orderItemService.GetOrderItemsByOrderIdAsync(orderId);
        return Ok(items);
    }

    [HttpPost("order/{orderId}")]
    public async Task<ActionResult<OrderItemDto>> CreateOrderItem(int orderId, [FromBody] OrderItemCreateDto orderItemCreateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var createdItem = await _orderItemService.CreateOrderItemAsync(orderId, orderItemCreateDto);
        if (createdItem == null)
        {
            return NotFound(new { message = $"Order with ID {orderId} was not found." });
        }

        return CreatedAtAction(nameof(GetOrderItem), new { id = createdItem.Id }, createdItem);
    }

    [HttpPut("{id}")]
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

        var success = await _orderItemService.UpdateOrderItemAsync(orderItemUpdateDto);
        if (!success)
        {
            return NotFound(new { message = "Order item or associated order was not found." });
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        var success = await _orderItemService.DeleteOrderItemAsync(id);
        if (!success)
        {
            return NotFound(new { message = $"Order item with ID {id} was not found." });
        }

        return NoContent();
    }
}
