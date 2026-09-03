using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.DTOs;
using ProcurementSystem.Services;

namespace ProcurementSystem.Controllers;

[ApiController]
[Route("api/warehouse")]
public class WarehouseController(IWarehouseService warehouseService) : ControllerBase
{
    [HttpGet("item-types")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ItemTypeDto>>> ListItemTypes()
    {
        var types = await warehouseService.ListItemTypesAsync();
        return Ok(types);
    }

    [HttpPost("item-types")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ItemTypeDto>> CreateItemType(CreateItemTypeRequest dto)
    {
        var result = await warehouseService.CreateItemTypeAsync(dto);
        return result.Error switch
        {
            WarehouseError.ItemTypeNameTaken => Conflict(),
            null => StatusCode(StatusCodes.Status201Created, result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpPut("item-types/{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ItemTypeDto>> UpdateItemType(long id, UpdateItemTypeRequest dto)
    {
        var result = await warehouseService.UpdateItemTypeAsync(id, dto);
        return result.Error switch
        {
            WarehouseError.ItemTypeNotFound => NotFound(),
            WarehouseError.ItemTypeNameTaken => Conflict(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpGet("items")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ItemDto>>> ListItems(
        [FromQuery] string? search,
        [FromQuery] long? typeId,
        [FromQuery] bool? isActive,
        [FromQuery] string? ordering)
    {
        var items = await warehouseService.ListItemsAsync(search, typeId, isActive, ordering);
        return Ok(items);
    }

    [HttpPost("items")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ItemDto>> CreateItem(CreateItemRequest dto)
    {
        var result = await warehouseService.CreateItemAsync(dto);
        return result.Error switch
        {
            WarehouseError.TypeNotFound => BadRequest(),
            null => StatusCode(StatusCodes.Status201Created, result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpPut("items/{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ItemDto>> UpdateItem(long id, UpdateItemRequest dto)
    {
        var result = await warehouseService.UpdateItemAsync(id, dto);
        return result.Error switch
        {
            WarehouseError.ItemNotFound => NotFound(),
            WarehouseError.TypeNotFound => BadRequest(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpGet("stocks")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<StockItemDto>>> ListStocks(
        [FromQuery] string? search,
        [FromQuery] long? typeId,
        [FromQuery] bool? lowStock)
    {
        var stocks = await warehouseService.ListStocksAsync(search, typeId, lowStock);
        return Ok(stocks);
    }

    [HttpGet("stocks/{itemId:long}")]
    [Authorize]
    public async Task<ActionResult<StockItemDto>> GetStock(long itemId)
    {
        var result = await warehouseService.GetStockAsync(itemId);
        return result.Error switch
        {
            WarehouseError.ItemNotFound => NotFound(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpPost("stocks/{itemId:long}/transactions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StockTransactionDto>> CreateTransaction(
        long itemId, CreateTransactionRequest dto)
    {
        var operatorId = GetCurrentUserId();
        if (operatorId is null)
            return Unauthorized();

        var result = await warehouseService.CreateTransactionAsync(itemId, dto, operatorId.Value);
        return result.Error switch
        {
            WarehouseError.ItemNotFound => NotFound(),
            WarehouseError.TransactionTypeInvalid or
            WarehouseError.QuantityChangeZero or
            WarehouseError.TypeQuantityMismatch or
            WarehouseError.InsufficientStock => BadRequest(),
            WarehouseError.ConcurrencyConflict => StatusCode(StatusCodes.Status409Conflict),
            null => StatusCode(StatusCodes.Status201Created, result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpGet("transactions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<StockTransactionDto>>> ListTransactions(
        [FromQuery] long? itemId,
        [FromQuery] string? type,
        [FromQuery] string? from,
        [FromQuery] string? to)
    {
        var transactions = await warehouseService.ListTransactionsAsync(itemId, type, from, to);
        return Ok(transactions);
    }

    private long? GetCurrentUserId()
    {
        // JwtBearer maps the JWT "sub" claim to ClaimTypes.NameIdentifier by default.
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return sub is not null && long.TryParse(sub, out var userId) ? userId : null;
    }
}
