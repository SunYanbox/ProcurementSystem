using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.Models;
using ProcurementSystem.Services;
using Xunit;

namespace ProcurementSystem.Tests;

public class ProcurementRequestServiceTests : TestBase
{
    // Helpers

    private async Task<User> SeedEmployeeAsync(ProcurementDbContext db, string workId = "E001")
    {
        var (_, employee) = await SeedUnboundEmployeeAsync(db, workId);
        return employee;
    }

    private async Task<User> SeedAdminAsync(ProcurementDbContext db, string workId = "ADMIN01")
    {
        var (_, admin) = await SeedUnboundEmployeeAsync(db, workId);
        admin.Role = Role.Admin;
        await db.SaveChangesAsync();
        return admin;
    }

    private static async Task<Item> SeedCatalogItemAsync(ProcurementDbContext db)
    {
        var type = new ItemType { Name = "办公耗材" };
        db.ItemTypes.Add(type);
        await db.SaveChangesAsync();

        var item = new Item
        {
            Name = "A4复印纸",
            TypeId = type.Id,
            Specification = "A4 80g",
            Unit = "包",
            Price = 25.5m,
            IsActive = true
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    // ListStatusesAsync

    [Fact]
    public async Task ListStatusesAsync_ReturnsAllStatuses()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);

        var statuses = await service.ListStatusesAsync();

        Assert.Equal(6, statuses.Count);
        Assert.Contains("Draft", statuses);
        Assert.Contains("Pending", statuses);
        Assert.Contains("Approved", statuses);
        Assert.Contains("Rejected", statuses);
        Assert.Contains("Purchased", statuses);
        Assert.Contains("Cancelled", statuses);
    }

    // CreateDraftAsync

    [Fact]
    public async Task CreateDraftAsync_CreatesDraftForCatalogItem()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var item = await SeedCatalogItemAsync(db);

        var result = await service.CreateDraftAsync(employee.Id, new DTOs.CreateProcurementRequestRequest
        {
            ItemId = item.Id,
            Quantity = 5,
            Purpose = "打印"
        });

        Assert.Null(result.Error);
        Assert.Equal("Draft", result.Value!.Status);
        Assert.Equal(item.Id, result.Value.ItemId);
        Assert.Equal(item.Name, result.Value.ItemName);
    }

    [Fact]
    public async Task CreateDraftAsync_CreatesDraftForCustomItem()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);

        var result = await service.CreateDraftAsync(employee.Id, new DTOs.CreateProcurementRequestRequest
        {
            CustomItemName = "定制笔记本",
            CustomSpecification = "A5 100页",
            Quantity = 2,
            Purpose = "会议"
        });

        Assert.Null(result.Error);
        Assert.Null(result.Value!.ItemId);
        Assert.Equal("定制笔记本", result.Value.CustomItemName);
    }

    [Fact]
    public async Task CreateDraftAsync_Fails_WhenItemAndCustomBothMissing()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);

        var result = await service.CreateDraftAsync(employee.Id, new DTOs.CreateProcurementRequestRequest
        {
            Quantity = 1,
            Purpose = "测试"
        });

        Assert.Equal(ProcurementRequestError.InvalidItemOrCustom, result.Error);
    }

    [Fact]
    public async Task CreateDraftAsync_Fails_WhenItemAndCustomBothProvided()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var item = await SeedCatalogItemAsync(db);

        var result = await service.CreateDraftAsync(employee.Id, new DTOs.CreateProcurementRequestRequest
        {
            ItemId = item.Id,
            CustomItemName = "多余",
            Quantity = 1,
            Purpose = "测试"
        });

        Assert.Equal(ProcurementRequestError.InvalidItemOrCustom, result.Error);
    }

    [Fact]
    public async Task CreateDraftAsync_Fails_WhenQuantityNotPositive()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);

        var result = await service.CreateDraftAsync(employee.Id, new DTOs.CreateProcurementRequestRequest
        {
            CustomItemName = "定制笔记本",
            Quantity = 0,
            Purpose = "测试"
        });

        Assert.Equal(ProcurementRequestError.QuantityNotPositive, result.Error);
    }

    [Fact]
    public async Task CreateDraftAsync_Fails_WhenItemNotFound()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);

        var result = await service.CreateDraftAsync(employee.Id, new DTOs.CreateProcurementRequestRequest
        {
            ItemId = 999,
            Quantity = 1,
            Purpose = "测试"
        });

        Assert.Equal(ProcurementRequestError.ItemNotFound, result.Error);
    }

    [Fact]
    public async Task CreateDraftAsync_Fails_WhenItemInactive()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var item = await SeedCatalogItemAsync(db);
        item.IsActive = false;
        await db.SaveChangesAsync();

        var result = await service.CreateDraftAsync(employee.Id, new DTOs.CreateProcurementRequestRequest
        {
            ItemId = item.Id,
            Quantity = 1,
            Purpose = "测试"
        });

        Assert.Equal(ProcurementRequestError.ItemNotActive, result.Error);
    }

    // EditDraftAsync

    [Fact]
    public async Task EditDraftAsync_UpdatesDraft()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);

        var result = await service.EditDraftAsync(request.Id, employee.Id, new DTOs.UpdateProcurementRequestRequest
        {
            Quantity = 8,
            Purpose = "更新用途"
        });

        Assert.Null(result.Error);
        Assert.Equal(8, result.Value!.Quantity);
        Assert.Equal("更新用途", result.Value.Purpose);
    }

    [Fact]
    public async Task EditDraftAsync_Fails_WhenNotOwner()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employeeA = await SeedEmployeeAsync(db, "E001");
        var employeeB = await SeedEmployeeAsync(db, "E002");
        var request = await CreateDraftAsync(db, service, employeeA.Id);

        var result = await service.EditDraftAsync(request.Id, employeeB.Id, new DTOs.UpdateProcurementRequestRequest
        {
            Quantity = 8
        });

        Assert.Equal(ProcurementRequestError.NotOwner, result.Error);
    }

    [Fact]
    public async Task EditDraftAsync_Fails_WhenNotDraft()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);
        await service.SubmitAsync(request.Id, employee.Id);

        var result = await service.EditDraftAsync(request.Id, employee.Id, new DTOs.UpdateProcurementRequestRequest
        {
            Quantity = 8
        });

        Assert.Equal(ProcurementRequestError.NotDraft, result.Error);
    }

    // SubmitAsync

    [Fact]
    public async Task SubmitAsync_MovesDraftToPending()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);

        var result = await service.SubmitAsync(request.Id, employee.Id);

        Assert.Null(result.Error);
        Assert.Equal("Pending", result.Value!.Status);
        var saved = await db.ProcurementRequests.FindAsync(request.Id);
        Assert.Equal(RequestStatus.Pending, saved!.Status);
    }

    [Fact]
    public async Task SubmitAsync_Fails_WhenNotOwner()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employeeA = await SeedEmployeeAsync(db, "E001");
        var employeeB = await SeedEmployeeAsync(db, "E002");
        var request = await CreateDraftAsync(db, service, employeeA.Id);

        var result = await service.SubmitAsync(request.Id, employeeB.Id);

        Assert.Equal(ProcurementRequestError.NotOwner, result.Error);
    }

    [Fact]
    public async Task SubmitAsync_Fails_WhenNotDraft()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);
        await service.SubmitAsync(request.Id, employee.Id);

        var result = await service.SubmitAsync(request.Id, employee.Id);

        Assert.Equal(ProcurementRequestError.NotDraft, result.Error);
    }

    // CancelAsync

    [Fact]
    public async Task CancelAsync_CancelsDraft()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);

        var result = await service.CancelAsync(request.Id, employee.Id);

        Assert.Null(result.Error);
        Assert.Equal("Cancelled", result.Value!.Status);
        Assert.NotNull(result.Value.CancelledAt);
    }

    [Fact]
    public async Task CancelAsync_CancelsPending()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);
        await service.SubmitAsync(request.Id, employee.Id);

        var result = await service.CancelAsync(request.Id, employee.Id);

        Assert.Null(result.Error);
        Assert.Equal("Cancelled", result.Value!.Status);
    }

    [Fact]
    public async Task CancelAsync_CancelsApproved()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);
        await service.SubmitAsync(request.Id, employee.Id);
        await service.AuditAsync(request.Id, admin.Id, new DTOs.AuditProcurementRequestRequest { Decision = "approve" });

        var result = await service.CancelAsync(request.Id, employee.Id);

        Assert.Null(result.Error);
        Assert.Equal("Cancelled", result.Value!.Status);
    }

    [Fact]
    public async Task CancelAsync_Fails_WhenPurchased()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);
        await service.SubmitAsync(request.Id, employee.Id);
        await service.AuditAsync(request.Id, admin.Id, new DTOs.AuditProcurementRequestRequest { Decision = "approve" });
        await service.PurchaseAsync(request.Id, admin.Id);

        var result = await service.CancelAsync(request.Id, employee.Id);

        Assert.Equal(ProcurementRequestError.NotCancellable, result.Error);
    }

    // GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ReturnsRequestForOwner()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);

        var result = await service.GetByIdAsync(request.Id, employee.Id, isAdmin: false);

        Assert.Null(result.Error);
        Assert.Equal(request.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRequestForAdmin()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);

        var result = await service.GetByIdAsync(request.Id, admin.Id, isAdmin: true);

        Assert.Null(result.Error);
        Assert.Equal(request.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Fails_WhenNotOwnerAndNotAdmin()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employeeA = await SeedEmployeeAsync(db, "E001");
        var employeeB = await SeedEmployeeAsync(db, "E002");
        var request = await CreateDraftAsync(db, service, employeeA.Id);

        var result = await service.GetByIdAsync(request.Id, employeeB.Id, isAdmin: false);

        Assert.Equal(ProcurementRequestError.NotOwner, result.Error);
    }

    // AuditAsync

    [Fact]
    public async Task AuditAsync_ApprovesPendingRequest()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);
        await service.SubmitAsync(request.Id, employee.Id);

        var result = await service.AuditAsync(request.Id, admin.Id, new DTOs.AuditProcurementRequestRequest
        {
            Decision = "approve"
        });

        Assert.Null(result.Error);
        Assert.Equal("Approved", result.Value!.Status);
        Assert.Equal(admin.Name, result.Value.AuditedByName);
        Assert.NotNull(result.Value.AuditedAt);
    }

    [Fact]
    public async Task AuditAsync_RejectsPendingRequest_WithReason()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);
        await service.SubmitAsync(request.Id, employee.Id);

        var result = await service.AuditAsync(request.Id, admin.Id, new DTOs.AuditProcurementRequestRequest
        {
            Decision = "reject",
            RefusalReason = "预算不足"
        });

        Assert.Null(result.Error);
        Assert.Equal("Rejected", result.Value!.Status);
        Assert.Equal("预算不足", result.Value.RefusalReason);
    }

    [Fact]
    public async Task AuditAsync_Fails_WhenRejectWithoutReason()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);
        await service.SubmitAsync(request.Id, employee.Id);

        var result = await service.AuditAsync(request.Id, admin.Id, new DTOs.AuditProcurementRequestRequest
        {
            Decision = "reject"
        });

        Assert.Equal(ProcurementRequestError.RefusalReasonRequired, result.Error);
    }

    [Fact]
    public async Task AuditAsync_Fails_WhenInvalidDecision()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);
        await service.SubmitAsync(request.Id, employee.Id);

        var result = await service.AuditAsync(request.Id, admin.Id, new DTOs.AuditProcurementRequestRequest
        {
            Decision = "maybe"
        });

        Assert.Equal(ProcurementRequestError.InvalidDecision, result.Error);
    }

    [Fact]
    public async Task AuditAsync_Fails_WhenNotPending()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);

        var result = await service.AuditAsync(request.Id, admin.Id, new DTOs.AuditProcurementRequestRequest
        {
            Decision = "approve"
        });

        Assert.Equal(ProcurementRequestError.NotPending, result.Error);
    }

    // PurchaseAsync

    [Fact]
    public async Task PurchaseAsync_PurchasesCatalogItem_AndWritesInboundTransaction()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var item = await SeedCatalogItemAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id, item.Id);
        await service.SubmitAsync(request.Id, employee.Id);
        await service.AuditAsync(request.Id, admin.Id, new DTOs.AuditProcurementRequestRequest { Decision = "approve" });

        var result = await service.PurchaseAsync(request.Id, admin.Id);

        Assert.Null(result.Error);
        Assert.Equal("Purchased", result.Value!.Status);
        Assert.Equal(admin.Name, result.Value.PurchasedByName);
        Assert.NotNull(result.Value.PurchasedAt);

        var stock = await db.StockItems.SingleAsync(s => s.ItemId == item.Id);
        Assert.Equal(request.Quantity, stock.Quantity);

        var transaction = await db.StockTransactions.SingleAsync();
        Assert.Equal(TransactionType.ProcurementInbound, transaction.Type);
        Assert.Equal("ProcurementRequest", transaction.ReferenceType);
        Assert.Equal(request.Id, transaction.ReferenceId);
        Assert.Equal(admin.Id, transaction.OperatorId);
        Assert.Equal(request.Quantity, transaction.QuantityChange);
    }

    [Fact]
    public async Task PurchaseAsync_AccumulatesExistingStock()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var item = await SeedCatalogItemAsync(db);
        db.StockItems.Add(new StockItem { ItemId = item.Id, Quantity = 3 });
        await db.SaveChangesAsync();
        var request = await CreateDraftAsync(db, service, employee.Id, item.Id, 5);
        await service.SubmitAsync(request.Id, employee.Id);
        await service.AuditAsync(request.Id, admin.Id, new DTOs.AuditProcurementRequestRequest { Decision = "approve" });

        var result = await service.PurchaseAsync(request.Id, admin.Id);

        Assert.Null(result.Error);
        var stock = await db.StockItems.SingleAsync(s => s.ItemId == item.Id);
        Assert.Equal(8, stock.Quantity);
    }

    [Fact]
    public async Task PurchaseAsync_Fails_WhenNotApproved()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var admin = await SeedAdminAsync(db);
        var request = await CreateDraftAsync(db, service, employee.Id);

        var result = await service.PurchaseAsync(request.Id, admin.Id);

        Assert.Equal(ProcurementRequestError.NotPurchasable, result.Error);
    }

    // ListMineAsync

    [Fact]
    public async Task ListMineAsync_ReturnsOwnRequestsFilteredByStatus()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employee = await SeedEmployeeAsync(db);
        var other = await SeedEmployeeAsync(db, "E002");
        await CreateDraftAsync(db, service, employee.Id, customName: "自己的草稿");
        await CreateDraftAsync(db, service, other.Id, customName: "别人的草稿");

        var mine = await service.ListMineAsync(employee.Id, "Draft", null, null, null);

        var request = Assert.Single(mine);
        Assert.Equal(employee.Id, request.SourceId);
        Assert.Equal("自己的草稿", request.CustomItemName);
    }

    // ListAllAsync

    [Fact]
    public async Task ListAllAsync_ReturnsAllRequests()
    {
        using var db = CreateDbContext();
        var service = new ProcurementRequestService(db);
        var employeeA = await SeedEmployeeAsync(db, "E001");
        var employeeB = await SeedEmployeeAsync(db, "E002");
        await CreateDraftAsync(db, service, employeeA.Id, customName: "A的申请");
        await CreateDraftAsync(db, service, employeeB.Id, customName: "B的申请");

        var all = await service.ListAllAsync(null, null, null, null, null);

        Assert.Equal(2, all.Count);
    }

    private static async Task<ProcurementRequest> CreateDraftAsync(
        ProcurementDbContext db,
        ProcurementRequestService service,
        long userId,
        long? itemId = null,
        int quantity = 3,
        string customName = "定制笔记本")
    {
        var result = await service.CreateDraftAsync(userId, new DTOs.CreateProcurementRequestRequest
        {
            ItemId = itemId,
            CustomItemName = itemId.HasValue ? null : customName,
            CustomSpecification = itemId.HasValue ? null : "A5",
            Quantity = quantity,
            Purpose = "测试用途"
        });

        Assert.Null(result.Error);
        return (await db.ProcurementRequests.FindAsync(result.Value!.Id))!;
    }
}
