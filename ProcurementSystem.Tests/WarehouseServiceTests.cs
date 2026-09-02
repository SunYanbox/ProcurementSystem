using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Models;
using ProcurementSystem.Data;
using ProcurementSystem.Services;
using Xunit;

namespace ProcurementSystem.Tests;

public class WarehouseServiceTests : TestBase
{
    // Helpers

    private static async Task<ItemType> SeedItemTypeAsync(
        ProcurementDbContext db, string name = "办公耗材", string? description = "日常打印耗材")
    {
        var itemType = new ItemType { Name = name, Description = description };
        db.ItemTypes.Add(itemType);
        await db.SaveChangesAsync();
        return itemType;
    }

    private static async Task<Item> SeedItemAsync(
        ProcurementDbContext db,
        long typeId,
        string name = "A4复印纸",
        string specification = "A4 80g",
        string unit = "包",
        decimal price = 25.5m,
        bool isActive = true,
        string? description = null)
    {
        var item = new Item
        {
            Name = name,
            TypeId = typeId,
            Specification = specification,
            Unit = unit,
            Price = price,
            IsActive = isActive,
            Description = description
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static async Task<StockItem> SeedStockAsync(
        ProcurementDbContext db, long itemId, int quantity)
    {
        var stock = new StockItem { ItemId = itemId, Quantity = quantity };
        db.StockItems.Add(stock);
        await db.SaveChangesAsync();
        return stock;
    }

    // ListItemTypesAsync

    [Fact]
    public async Task ListItemTypesAsync_ReturnsAllOrderedById()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var first = await SeedItemTypeAsync(db, "办公耗材");
        var second = await SeedItemTypeAsync(db, "办公设备");

        var types = await service.ListItemTypesAsync();

        Assert.Equal(2, types.Count);
        Assert.Equal(first.Id, types[0].Id);
        Assert.Equal(second.Id, types[1].Id);
    }

    [Fact]
    public async Task ListItemTypesAsync_ReturnsEmpty_WhenNoTypes()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);

        var types = await service.ListItemTypesAsync();

        Assert.Empty(types);
    }

    // CreateItemTypeAsync

    [Fact]
    public async Task CreateItemTypeAsync_CreatesType()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);

        var result = await service.CreateItemTypeAsync(new DTOs.CreateItemTypeRequest
        {
            Name = "办公耗材",
            Description = "打印耗材"
        });

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal("办公耗材", result.Value.Name);
        Assert.Equal("打印耗材", result.Value.Description);
        Assert.True(await db.ItemTypes.AnyAsync(t => t.Id == result.Value.Id));
    }

    [Fact]
    public async Task CreateItemTypeAsync_Fails_WhenNameTaken()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        await SeedItemTypeAsync(db, "办公耗材");

        var result = await service.CreateItemTypeAsync(new DTOs.CreateItemTypeRequest
        {
            Name = "办公耗材"
        });

        Assert.Equal(WarehouseError.ItemTypeNameTaken, result.Error);
        Assert.Null(result.Value);
    }

    // UpdateItemTypeAsync

    [Fact]
    public async Task UpdateItemTypeAsync_UpdatesType()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db, "办公耗材", "旧描述");

        var result = await service.UpdateItemTypeAsync(type.Id, new DTOs.UpdateItemTypeRequest
        {
            Name = "打印耗材",
            Description = "新描述"
        });

        Assert.Null(result.Error);
        Assert.Equal("打印耗材", result.Value!.Name);
        Assert.Equal("新描述", result.Value.Description);
        var saved = await db.ItemTypes.FindAsync(type.Id);
        Assert.Equal("打印耗材", saved!.Name);
    }

    [Fact]
    public async Task UpdateItemTypeAsync_Fails_WhenNotFound()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);

        var result = await service.UpdateItemTypeAsync(999, new DTOs.UpdateItemTypeRequest
        {
            Name = "任意类型"
        });

        Assert.Equal(WarehouseError.ItemTypeNotFound, result.Error);
    }

    [Fact]
    public async Task UpdateItemTypeAsync_Fails_WhenNameConflictsWithAnother()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db, "办公耗材");
        await SeedItemTypeAsync(db, "办公设备");

        var result = await service.UpdateItemTypeAsync(type.Id, new DTOs.UpdateItemTypeRequest
        {
            Name = "办公设备"
        });

        Assert.Equal(WarehouseError.ItemTypeNameTaken, result.Error);
    }

    // ListItemsAsync

    [Fact]
    public async Task ListItemsAsync_ReturnsActiveOnly_ByDefault()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db);
        await SeedItemAsync(db, type.Id, "A4复印纸", isActive: true);
        await SeedItemAsync(db, type.Id, "停用打印纸", isActive: false);

        var items = await service.ListItemsAsync(null, null, null, null);

        var item = Assert.Single(items);
        Assert.Equal("A4复印纸", item.Name);
    }

    [Fact]
    public async Task ListItemsAsync_ReturnsInactive_WhenRequested()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db);
        await SeedItemAsync(db, type.Id, "A4复印纸", isActive: true);
        await SeedItemAsync(db, type.Id, "停用打印纸", isActive: false);

        var items = await service.ListItemsAsync(null, null, false, null);

        var item = Assert.Single(items);
        Assert.Equal("停用打印纸", item.Name);
    }

    [Fact]
    public async Task ListItemsAsync_FiltersByTypeId()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var typeA = await SeedItemTypeAsync(db, "办公耗材");
        var typeB = await SeedItemTypeAsync(db, "办公设备");
        await SeedItemAsync(db, typeA.Id, "A4复印纸");
        await SeedItemAsync(db, typeB.Id, "打印机");

        var items = await service.ListItemsAsync(null, typeA.Id, null, null);

        var item = Assert.Single(items);
        Assert.Equal(typeA.Id, item.TypeId);
    }

    [Fact]
    public async Task ListItemsAsync_SearchesByNameSpecificationDescriptionAndType()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db, "办公耗材");
        await SeedItemAsync(db, type.Id, "A4复印纸", specification: "80g", description: "打印用纸");
        await SeedItemAsync(db, type.Id, "铅笔", specification: "HB");

        var byName = await service.ListItemsAsync("复印", null, null, null);
        Assert.Single(byName);

        var bySpec = await service.ListItemsAsync("80g", null, null, null);
        Assert.Single(bySpec);

        var byTypeName = await service.ListItemsAsync("耗材", null, null, null);
        Assert.Equal(2, byTypeName.Count);
    }

    [Fact]
    public async Task ListItemsAsync_OrdersByPriceDescending()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db);
        await SeedItemAsync(db, type.Id, "铅笔", price: 2m);
        await SeedItemAsync(db, type.Id, "A4复印纸", price: 25.5m);

        var items = await service.ListItemsAsync(null, null, null, "-price");

        Assert.Equal(2, items.Count);
        Assert.Equal("A4复印纸", items[0].Name);
        Assert.Equal("铅笔", items[1].Name);
    }

    // CreateItemAsync

    [Fact]
    public async Task CreateItemAsync_CreatesItem()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db, "办公耗材");

        var result = await service.CreateItemAsync(new DTOs.CreateItemRequest
        {
            Name = "A4复印纸",
            TypeId = type.Id,
            Specification = "A4 80g",
            Unit = "包",
            Price = 25.5m
        });

        Assert.Null(result.Error);
        Assert.Equal("A4复印纸", result.Value!.Name);
        Assert.Equal("办公耗材", result.Value.TypeName);
        Assert.True(await db.Items.AnyAsync(i => i.Id == result.Value.Id));
    }

    [Fact]
    public async Task CreateItemAsync_Fails_WhenTypeNotFound()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);

        var result = await service.CreateItemAsync(new DTOs.CreateItemRequest
        {
            Name = "A4复印纸",
            TypeId = 999,
            Specification = "A4 80g",
            Unit = "包",
            Price = 25.5m
        });

        Assert.Equal(WarehouseError.TypeNotFound, result.Error);
        Assert.Null(result.Value);
    }

    // UpdateItemAsync

    [Fact]
    public async Task UpdateItemAsync_PartialUpdateChangesOnlyProvidedFields()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸", price: 25.5m);

        var result = await service.UpdateItemAsync(item.Id, new DTOs.UpdateItemRequest
        {
            Price = 30m
        });

        Assert.Null(result.Error);
        Assert.Equal(30m, result.Value!.Price);
        Assert.Equal("A4复印纸", result.Value.Name);
        Assert.Equal(type.Id, result.Value.TypeId);
    }

    [Fact]
    public async Task UpdateItemAsync_Fails_WhenItemNotFound()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);

        var result = await service.UpdateItemAsync(999, new DTOs.UpdateItemRequest { Price = 10m });

        Assert.Equal(WarehouseError.ItemNotFound, result.Error);
    }

    [Fact]
    public async Task UpdateItemAsync_Fails_WhenNewTypeNotFound()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");

        var result = await service.UpdateItemAsync(item.Id, new DTOs.UpdateItemRequest
        {
            TypeId = 999
        });

        Assert.Equal(WarehouseError.TypeNotFound, result.Error);
    }

    // ListStocksAsync

    [Fact]
    public async Task ListStocksAsync_ReturnsAllStocks()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db);
        var itemA = await SeedItemAsync(db, type.Id, "A4复印纸");
        var itemB = await SeedItemAsync(db, type.Id, "铅笔");
        await SeedStockAsync(db, itemA.Id, 10);
        await SeedStockAsync(db, itemB.Id, 5);

        var stocks = await service.ListStocksAsync(null, null, null);

        Assert.Equal(2, stocks.Count);
        Assert.Contains(stocks, s => s.ItemName == "A4复印纸" && s.Quantity == 10);
        Assert.Contains(stocks, s => s.ItemName == "铅笔" && s.Quantity == 5);
    }

    [Fact]
    public async Task ListStocksAsync_FiltersLowStock()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db);
        var itemA = await SeedItemAsync(db, type.Id, "A4复印纸");
        var itemB = await SeedItemAsync(db, type.Id, "铅笔");
        await SeedStockAsync(db, itemA.Id, 0);
        await SeedStockAsync(db, itemB.Id, 5);

        var stocks = await service.ListStocksAsync(null, null, true);

        var stock = Assert.Single(stocks);
        Assert.Equal(0, stock.Quantity);
    }

    [Fact]
    public async Task ListStocksAsync_FiltersByTypeId()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var typeA = await SeedItemTypeAsync(db, "办公耗材");
        var typeB = await SeedItemTypeAsync(db, "办公设备");
        var itemA = await SeedItemAsync(db, typeA.Id, "A4复印纸");
        var itemB = await SeedItemAsync(db, typeB.Id, "打印机");
        await SeedStockAsync(db, itemA.Id, 10);
        await SeedStockAsync(db, itemB.Id, 2);

        var stocks = await service.ListStocksAsync(null, typeA.Id, null);

        var stock = Assert.Single(stocks);
        Assert.Equal(itemA.Id, stock.ItemId);
    }

    // GetStockAsync

    [Fact]
    public async Task GetStockAsync_ReturnsStock()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var type = await SeedItemTypeAsync(db, "办公耗材");
        var item = await SeedItemAsync(db, type.Id, "A4复印纸", specification: "A4 80g", unit: "包");
        await SeedStockAsync(db, item.Id, 10);

        var result = await service.GetStockAsync(item.Id);

        Assert.Null(result.Error);
        Assert.Equal("A4复印纸", result.Value!.ItemName);
        Assert.Equal("A4 80g", result.Value.ItemSpecification);
        Assert.Equal("包", result.Value.Unit);
        Assert.Equal(10, result.Value.Quantity);
    }

    [Fact]
    public async Task GetStockAsync_Fails_WhenNotFound()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);

        var result = await service.GetStockAsync(999);

        Assert.Equal(WarehouseError.ItemNotFound, result.Error);
    }

    // CreateTransactionAsync

    [Fact]
    public async Task CreateTransactionAsync_Fails_WhenTypeInvalid()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "NotAType", QuantityChange = 5 },
            employee.Id);

        Assert.Equal(WarehouseError.TransactionTypeInvalid, result.Error);
    }

    [Fact]
    public async Task CreateTransactionAsync_Fails_WhenQuantityZero()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "ManualInbound", QuantityChange = 0 },
            employee.Id);

        Assert.Equal(WarehouseError.QuantityChangeZero, result.Error);
    }

    [Fact]
    public async Task CreateTransactionAsync_Fails_WhenManualInboundHasNegativeChange()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "ManualInbound", QuantityChange = -5 },
            employee.Id);

        Assert.Equal(WarehouseError.TypeQuantityMismatch, result.Error);
    }

    [Fact]
    public async Task CreateTransactionAsync_Fails_WhenManualOutboundHasPositiveChange()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "ManualOutbound", QuantityChange = 5 },
            employee.Id);

        Assert.Equal(WarehouseError.TypeQuantityMismatch, result.Error);
    }

    [Fact]
    public async Task CreateTransactionAsync_Fails_WhenItemNotFound()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);

        var result = await service.CreateTransactionAsync(
            999,
            new DTOs.CreateTransactionRequest { Type = "ManualInbound", QuantityChange = 5 },
            employee.Id);

        Assert.Equal(WarehouseError.ItemNotFound, result.Error);
    }

    [Fact]
    public async Task CreateTransactionAsync_FirstInboundCreatesSnapshot()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "ManualInbound", QuantityChange = 10 },
            employee.Id);

        Assert.Null(result.Error);
        Assert.Equal(10, result.Value!.QuantityChange);
        var stock = await db.StockItems.SingleAsync(s => s.ItemId == item.Id);
        Assert.Equal(10, stock.Quantity);
    }

    [Fact]
    public async Task CreateTransactionAsync_SecondInboundAccumulates()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");
        await SeedStockAsync(db, item.Id, 10);

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "ManualInbound", QuantityChange = 5 },
            employee.Id);

        Assert.Null(result.Error);
        var stock = await db.StockItems.SingleAsync(s => s.ItemId == item.Id);
        Assert.Equal(15, stock.Quantity);
    }

    [Fact]
    public async Task CreateTransactionAsync_Fails_WhenOutboundWithoutSnapshot()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "ManualOutbound", QuantityChange = -5 },
            employee.Id);

        Assert.Equal(WarehouseError.InsufficientStock, result.Error);
        Assert.False(await db.StockItems.AnyAsync(s => s.ItemId == item.Id));
    }

    [Fact]
    public async Task CreateTransactionAsync_Fails_WhenOverdraw()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");
        await SeedStockAsync(db, item.Id, 3);

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "ManualOutbound", QuantityChange = -5 },
            employee.Id);

        Assert.Equal(WarehouseError.InsufficientStock, result.Error);
        var stock = await db.StockItems.SingleAsync(s => s.ItemId == item.Id);
        Assert.Equal(3, stock.Quantity);
    }

    [Fact]
    public async Task CreateTransactionAsync_OutboundReducesStock()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");
        await SeedStockAsync(db, item.Id, 10);

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "ManualOutbound", QuantityChange = -3 },
            employee.Id);

        Assert.Null(result.Error);
        Assert.Equal(-3, result.Value!.QuantityChange);
        var stock = await db.StockItems.SingleAsync(s => s.ItemId == item.Id);
        Assert.Equal(7, stock.Quantity);
    }

    [Fact]
    public async Task CreateTransactionAsync_AdjustmentAllowsNegativeChange()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");
        await SeedStockAsync(db, item.Id, 10);

        var result = await service.CreateTransactionAsync(
            item.Id,
            new DTOs.CreateTransactionRequest { Type = "Adjustment", QuantityChange = -5 },
            employee.Id);

        Assert.Null(result.Error);
        Assert.Equal(-5, result.Value!.QuantityChange);
        var stock = await db.StockItems.SingleAsync(s => s.ItemId == item.Id);
        Assert.Equal(5, stock.Quantity);
    }

    // ListTransactionsAsync

    [Fact]
    public async Task ListTransactionsAsync_ReturnsAllOrderedByCreatedAtDesc()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db, "办公耗材");
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");
        await SeedTransactionAsync(db, item.Id, employee.Id, TransactionType.ManualInbound, 10, DateTime.UtcNow.AddMinutes(-10));
        await SeedTransactionAsync(db, item.Id, employee.Id, TransactionType.ManualOutbound, -2, DateTime.UtcNow);

        var transactions = await service.ListTransactionsAsync(null, null, null, null);

        Assert.Equal(2, transactions.Count);
        Assert.True(DateTime.Parse(transactions[0].CreatedAt) >= DateTime.Parse(transactions[1].CreatedAt));
        Assert.Equal("ManualOutbound", transactions[0].Type);
        Assert.Equal("ManualInbound", transactions[1].Type);
    }

    [Fact]
    public async Task ListTransactionsAsync_FiltersByItemId()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var itemA = await SeedItemAsync(db, type.Id, "A4复印纸");
        var itemB = await SeedItemAsync(db, type.Id, "铅笔");
        await SeedTransactionAsync(db, itemA.Id, employee.Id, TransactionType.ManualInbound, 10);
        await SeedTransactionAsync(db, itemB.Id, employee.Id, TransactionType.ManualInbound, 5);

        var transactions = await service.ListTransactionsAsync(itemA.Id, null, null, null);

        var transaction = Assert.Single(transactions);
        Assert.Equal(itemA.Id, transaction.ItemId);
    }

    [Fact]
    public async Task ListTransactionsAsync_FiltersByType()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");
        await SeedTransactionAsync(db, item.Id, employee.Id, TransactionType.ManualInbound, 10);
        await SeedTransactionAsync(db, item.Id, employee.Id, TransactionType.ManualOutbound, -2);

        var transactions = await service.ListTransactionsAsync(null, "ManualOutbound", null, null);

        var transaction = Assert.Single(transactions);
        Assert.Equal("ManualOutbound", transaction.Type);
    }

    [Fact]
    public async Task ListTransactionsAsync_FiltersByDateRange()
    {
        using var db = CreateDbContext();
        var service = new WarehouseService(db);
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var type = await SeedItemTypeAsync(db);
        var item = await SeedItemAsync(db, type.Id, "A4复印纸");
        await SeedTransactionAsync(db, item.Id, employee.Id, TransactionType.ManualInbound, 10, FixedUtc(2026, 1, 1));
        await SeedTransactionAsync(db, item.Id, employee.Id, TransactionType.ManualInbound, 5, FixedUtc(2026, 1, 3));
        await SeedTransactionAsync(db, item.Id, employee.Id, TransactionType.ManualInbound, 3, FixedUtc(2026, 1, 5));

        var from = "2026-01-02T00:00:00Z";
        var transactions = await service.ListTransactionsAsync(null, null, from, null);

        Assert.Equal(2, transactions.Count);
        Assert.All(transactions, t => Assert.True(DateTime.Parse(t.CreatedAt) >= DateTime.Parse(from)));
    }

    private static async Task SeedTransactionAsync(
        ProcurementDbContext db,
        long itemId,
        long operatorId,
        TransactionType type,
        int quantityChange,
        DateTime? createdAt = null)
    {
        db.StockTransactions.Add(new StockTransaction
        {
            ItemId = itemId,
            OperatorId = operatorId,
            Type = type,
            QuantityChange = quantityChange,
            CreatedAt = createdAt ?? DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static DateTime FixedUtc(int year, int month, int day) =>
        DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);
}
