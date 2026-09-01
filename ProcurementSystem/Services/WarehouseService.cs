using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.DTOs;
using ProcurementSystem.Models;

namespace ProcurementSystem.Services;

public enum WarehouseError
{
    ItemTypeNameTaken,
    ItemTypeNotFound,
    TypeNotFound,
    ItemNotFound,
    TransactionTypeInvalid,
    QuantityChangeZero,
    TypeQuantityMismatch,
    InsufficientStock
}

public record WarehouseResult<T>(T? Value, WarehouseError? Error)
{
    public static WarehouseResult<T> Ok(T value) => new(value, null);
    public static WarehouseResult<T> Fail(WarehouseError error) => new(default, error);
}

public interface IWarehouseService
{
    Task<IReadOnlyList<ItemTypeDto>> ListItemTypesAsync();
    Task<WarehouseResult<ItemTypeDto>> CreateItemTypeAsync(CreateItemTypeRequest dto);
    Task<WarehouseResult<ItemTypeDto>> UpdateItemTypeAsync(long id, UpdateItemTypeRequest dto);
    Task<IReadOnlyList<ItemDto>> ListItemsAsync(string? search, long? typeId, bool? isActive, string? ordering);
    Task<WarehouseResult<ItemDto>> CreateItemAsync(CreateItemRequest dto);
    Task<WarehouseResult<ItemDto>> UpdateItemAsync(long id, UpdateItemRequest dto);
    Task<IReadOnlyList<StockItemDto>> ListStocksAsync(string? search, long? typeId, bool? lowStock);
    Task<WarehouseResult<StockItemDto>> GetStockAsync(long itemId);
    Task<WarehouseResult<StockTransactionDto>> CreateTransactionAsync(long itemId, CreateTransactionRequest dto, long operatorId);
    Task<IReadOnlyList<StockTransactionDto>> ListTransactionsAsync(long? itemId, string? type, string? from, string? to);
}

public class WarehouseService(ProcurementDbContext db) : IWarehouseService
{
    public async Task<IReadOnlyList<ItemTypeDto>> ListItemTypesAsync()
    {
        var types = await db.ItemTypes
            .OrderBy(t => t.Id)
            .ToListAsync();

        return types.Select(ToItemTypeDto).ToList();
    }

    public async Task<WarehouseResult<ItemTypeDto>> CreateItemTypeAsync(CreateItemTypeRequest dto)
    {
        if (await db.ItemTypes.AnyAsync(t => t.Name == dto.Name))
            return WarehouseResult<ItemTypeDto>.Fail(WarehouseError.ItemTypeNameTaken);

        var itemType = new ItemType
        {
            Name = dto.Name,
            Description = dto.Description
        };

        db.ItemTypes.Add(itemType);
        await db.SaveChangesAsync();

        return WarehouseResult<ItemTypeDto>.Ok(ToItemTypeDto(itemType));
    }

    public async Task<WarehouseResult<ItemTypeDto>> UpdateItemTypeAsync(long id, UpdateItemTypeRequest dto)
    {
        var itemType = await db.ItemTypes.FindAsync(id);
        if (itemType is null)
            return WarehouseResult<ItemTypeDto>.Fail(WarehouseError.ItemTypeNotFound);

        if (await db.ItemTypes.AnyAsync(t => t.Id != id && t.Name == dto.Name))
            return WarehouseResult<ItemTypeDto>.Fail(WarehouseError.ItemTypeNameTaken);

        itemType.Name = dto.Name;
        itemType.Description = dto.Description;
        await db.SaveChangesAsync();

        return WarehouseResult<ItemTypeDto>.Ok(ToItemTypeDto(itemType));
    }

    public async Task<IReadOnlyList<ItemDto>> ListItemsAsync(string? search, long? typeId, bool? isActive, string? ordering)
    {
        IQueryable<Item> query = db.Items.Include(i => i.Type);

        var activeFilter = isActive ?? true;
        query = query.Where(i => i.IsActive == activeFilter);

        if (typeId.HasValue)
            query = query.Where(i => i.TypeId == typeId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                i.Name.Contains(search) ||
                i.Type.Name.Contains(search) ||
                (i.Description != null && i.Description.Contains(search)) ||
                i.Specification.Contains(search));
        }

        query = ordering switch
        {
            "name" => query.OrderBy(i => i.Name),
            "-name" => query.OrderByDescending(i => i.Name),
            "price" => query.OrderBy(i => i.Price),
            "-price" => query.OrderByDescending(i => i.Price),
            _ => query.OrderBy(i => i.Id)
        };

        var items = await query.ToListAsync();
        return items.Select(ToItemDto).ToList();
    }

    public async Task<WarehouseResult<ItemDto>> CreateItemAsync(CreateItemRequest dto)
    {
        if (!await db.ItemTypes.AnyAsync(t => t.Id == dto.TypeId))
            return WarehouseResult<ItemDto>.Fail(WarehouseError.TypeNotFound);

        var item = new Item
        {
            Name = dto.Name,
            TypeId = dto.TypeId,
            Description = dto.Description,
            Specification = dto.Specification,
            Unit = dto.Unit,
            Price = dto.Price
        };

        db.Items.Add(item);
        await db.SaveChangesAsync();

        await db.Entry(item).Reference(i => i.Type).LoadAsync();
        return WarehouseResult<ItemDto>.Ok(ToItemDto(item));
    }

    public async Task<WarehouseResult<ItemDto>> UpdateItemAsync(long id, UpdateItemRequest dto)
    {
        var item = await db.Items
            .Include(i => i.Type)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item is null)
            return WarehouseResult<ItemDto>.Fail(WarehouseError.ItemNotFound);

        if (dto.TypeId.HasValue && !await db.ItemTypes.AnyAsync(t => t.Id == dto.TypeId.Value))
            return WarehouseResult<ItemDto>.Fail(WarehouseError.TypeNotFound);

        if (dto.Name is not null) item.Name = dto.Name;
        if (dto.TypeId.HasValue) item.TypeId = dto.TypeId.Value;
        if (dto.Description is not null) item.Description = dto.Description;
        if (dto.Specification is not null) item.Specification = dto.Specification;
        if (dto.Unit is not null) item.Unit = dto.Unit;
        if (dto.Price.HasValue) item.Price = dto.Price.Value;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;

        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return WarehouseResult<ItemDto>.Ok(ToItemDto(item));
    }

    public async Task<IReadOnlyList<StockItemDto>> ListStocksAsync(string? search, long? typeId, bool? lowStock)
    {
        IQueryable<StockItem> query = db.StockItems
            .Include(s => s.Item)
            .ThenInclude(i => i.Type);

        if (typeId.HasValue)
            query = query.Where(s => s.Item.TypeId == typeId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Item.Name.Contains(search) ||
                s.Item.Type.Name.Contains(search) ||
                s.Item.Specification.Contains(search));
        }

        if (lowStock == true)
            query = query.Where(s => s.Quantity == 0);

        var stocks = await query.OrderBy(s => s.ItemId).ToListAsync();
        return stocks.Select(ToStockItemDto).ToList();
    }

    public async Task<WarehouseResult<StockItemDto>> GetStockAsync(long itemId)
    {
        var stock = await db.StockItems
            .Include(s => s.Item)
            .ThenInclude(i => i.Type)
            .FirstOrDefaultAsync(s => s.ItemId == itemId);

        return stock is null
            ? WarehouseResult<StockItemDto>.Fail(WarehouseError.ItemNotFound)
            : WarehouseResult<StockItemDto>.Ok(ToStockItemDto(stock));
    }

    public async Task<WarehouseResult<StockTransactionDto>> CreateTransactionAsync(
        long itemId, CreateTransactionRequest dto, long operatorId)
    {
        if (!TryParseTransactionType(dto.Type, out var type))
            return WarehouseResult<StockTransactionDto>.Fail(WarehouseError.TransactionTypeInvalid);

        if (dto.QuantityChange == 0)
            return WarehouseResult<StockTransactionDto>.Fail(WarehouseError.QuantityChangeZero);

        if ((type == TransactionType.ManualInbound && dto.QuantityChange < 0) ||
            (type == TransactionType.ManualOutbound && dto.QuantityChange > 0))
        {
            return WarehouseResult<StockTransactionDto>.Fail(WarehouseError.TypeQuantityMismatch);
        }

        var item = await db.Items
            .Include(i => i.Type)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item is null)
            return WarehouseResult<StockTransactionDto>.Fail(WarehouseError.ItemNotFound);

        var stock = await db.StockItems.FirstOrDefaultAsync(s => s.ItemId == itemId);

        await using var tx = await db.Database.BeginTransactionAsync();

        var transaction = new StockTransaction
        {
            ItemId = itemId,
            QuantityChange = dto.QuantityChange,
            Type = type,
            Note = dto.Note,
            OperatorId = operatorId
        };

        if (stock is null)
        {
            if (dto.QuantityChange < 0)
                return WarehouseResult<StockTransactionDto>.Fail(WarehouseError.InsufficientStock);

            stock = new StockItem { ItemId = itemId, Quantity = dto.QuantityChange };
            db.StockItems.Add(stock);
        }
        else
        {
            if (dto.QuantityChange < 0 && stock.Quantity + dto.QuantityChange < 0)
                return WarehouseResult<StockTransactionDto>.Fail(WarehouseError.InsufficientStock);

            stock.Quantity += dto.QuantityChange;
        }

        stock.UpdatedAt = DateTime.UtcNow;
        stock.Version = Guid.NewGuid().ToByteArray();

        db.StockTransactions.Add(transaction);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        transaction.Item = item;
        transaction.Operator = await db.Users.FindAsync(operatorId) ?? throw new InvalidOperationException("Operator not found");

        return WarehouseResult<StockTransactionDto>.Ok(ToStockTransactionDto(transaction));
    }

    public async Task<IReadOnlyList<StockTransactionDto>> ListTransactionsAsync(
        long? itemId, string? type, string? from, string? to)
    {
        IQueryable<StockTransaction> query = db.StockTransactions
            .Include(t => t.Item)
            .Include(t => t.Operator);

        if (itemId.HasValue)
            query = query.Where(t => t.ItemId == itemId.Value);

        if (!string.IsNullOrWhiteSpace(type) && TryParseTransactionType(type, out var parsedType))
            query = query.Where(t => t.Type == parsedType);

        if (DateTime.TryParse(from, out var fromDate))
            query = query.Where(t => t.CreatedAt >= fromDate);

        if (DateTime.TryParse(to, out var toDate))
            query = query.Where(t => t.CreatedAt <= toDate);

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return transactions.Select(ToStockTransactionDto).ToList();
    }

    private static bool TryParseTransactionType(string? value, out TransactionType type)
    {
        return Enum.TryParse(value, ignoreCase: true, out type) &&
               Enum.IsDefined(type);
    }

    private static ItemTypeDto ToItemTypeDto(ItemType itemType) => new()
    {
        Id = itemType.Id,
        Name = itemType.Name,
        Description = itemType.Description
    };

    private static ItemDto ToItemDto(Item item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        TypeId = item.TypeId,
        TypeName = item.Type.Name,
        Description = item.Description,
        Specification = item.Specification,
        Unit = item.Unit,
        Price = item.Price,
        StockQuantity = null,
        IsActive = item.IsActive
    };

    private static StockItemDto ToStockItemDto(StockItem stockItem) => new()
    {
        Id = stockItem.Id,
        ItemId = stockItem.ItemId,
        ItemName = stockItem.Item.Name,
        ItemSpecification = stockItem.Item.Specification,
        Unit = stockItem.Item.Unit,
        Quantity = stockItem.Quantity,
        UpdatedAt = stockItem.UpdatedAt.ToString("o")
    };

    private static StockTransactionDto ToStockTransactionDto(StockTransaction transaction) => new()
    {
        Id = transaction.Id,
        ItemId = transaction.ItemId,
        ItemName = transaction.Item.Name,
        QuantityChange = transaction.QuantityChange,
        Type = transaction.Type.ToString(),
        ReferenceType = transaction.ReferenceType,
        ReferenceId = transaction.ReferenceId,
        Note = transaction.Note,
        OperatorName = transaction.Operator.Name,
        CreatedAt = transaction.CreatedAt.ToString("o")
    };
}
