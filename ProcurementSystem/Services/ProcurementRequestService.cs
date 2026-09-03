using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.DTOs;
using ProcurementSystem.Models;

namespace ProcurementSystem.Services;

public enum ProcurementRequestError
{
    RequestNotFound,
    NotOwner,
    InvalidItemOrCustom,
    QuantityNotPositive,
    ItemNotActive,
    ItemNotFound,
    NotDraft,
    NotPending,
    NotCancellable,
    NotPurchasable,
    InvalidDecision,
    RefusalReasonRequired,
    // 离职员工不能创建新的采购申请
    SourceInactive,
    ConcurrencyConflict
}

public record ProcurementRequestResult<T>(T? Value, ProcurementRequestError? Error)
{
    public static ProcurementRequestResult<T> Ok(T value) => new(value, null);
    public static ProcurementRequestResult<T> Fail(ProcurementRequestError error) => new(default, error);
}

public interface IProcurementRequestService
{
    Task<IReadOnlyList<string>> ListStatusesAsync();
    Task<ProcurementRequestResult<ProcurementRequestDto>> CreateDraftAsync(
        long userId, CreateProcurementRequestRequest dto);
    Task<ProcurementRequestResult<ProcurementRequestDto>> EditDraftAsync(
        long requestId, long userId, UpdateProcurementRequestRequest dto);
    Task<ProcurementRequestResult<ProcurementRequestDto>> SubmitAsync(long requestId, long userId);
    Task<ProcurementRequestResult<ProcurementRequestDto>> CancelAsync(long requestId, long userId);
    Task<IReadOnlyList<ProcurementRequestDto>> ListMineAsync(
        long userId, string? status, string? from, string? to, string? search);
    Task<ProcurementRequestResult<ProcurementRequestDto>> GetByIdAsync(long requestId, long userId, bool isAdmin);
    Task<IReadOnlyList<ProcurementRequestDto>> ListAllAsync(
        string? status, long? sourceId, string? from, string? to, string? search);
    Task<ProcurementRequestResult<ProcurementRequestDto>> AuditAsync(
        long requestId, long adminId, AuditProcurementRequestRequest dto);
    Task<ProcurementRequestResult<ProcurementRequestDto>> PurchaseAsync(long requestId, long adminId);
}

public class ProcurementRequestService(ProcurementDbContext db) : IProcurementRequestService
{
    public Task<IReadOnlyList<string>> ListStatusesAsync()
    {
        IReadOnlyList<string> statuses = Enum.GetNames<RequestStatus>();
        return Task.FromResult(statuses);
    }

    public async Task<ProcurementRequestResult<ProcurementRequestDto>> CreateDraftAsync(
        long userId, CreateProcurementRequestRequest dto)
    {
        // 离职员工不能创建新的采购申请，即便其登录令牌仍在有效期内
        var source = await db.Users.FindAsync(userId);
        if (source is null || !source.Working)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.SourceInactive);

        var validation = await ValidateItemOrCustomAsync(dto.ItemId, dto.CustomItemName, dto.CustomSpecification, dto.Quantity);
        if (validation is not null)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(validation.Value);

        var request = new ProcurementRequest
        {
            SourceId = userId,
            ItemId = dto.ItemId,
            CustomItemName = dto.ItemId is null ? dto.CustomItemName : null,
            CustomSpecification = dto.ItemId is null ? dto.CustomSpecification : null,
            Quantity = dto.Quantity,
            Purpose = dto.Purpose,
            Status = RequestStatus.Draft
        };

        db.ProcurementRequests.Add(request);
        await db.SaveChangesAsync();

        return ProcurementRequestResult<ProcurementRequestDto>.Ok(await ToDtoAsync(request));
    }

    public async Task<ProcurementRequestResult<ProcurementRequestDto>> EditDraftAsync(
        long requestId, long userId, UpdateProcurementRequestRequest dto)
    {
        // 离职员工不能编辑既有草稿，即便它创建于在职期间
        var source = await db.Users.FindAsync(userId);
        if (source is null || !source.Working)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.SourceInactive);

        var request = await db.ProcurementRequests.FindAsync(requestId);
        if (request is null)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.RequestNotFound);

        if (request.SourceId != userId)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.NotOwner);

        if (request.Status != RequestStatus.Draft)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.NotDraft);

        if (dto.ItemId is not null || dto.CustomItemName is not null || dto.CustomSpecification is not null)
        {
            var itemId = dto.ItemId ?? request.ItemId;
            var customName = dto.CustomItemName ?? request.CustomItemName;
            var customSpec = dto.CustomSpecification ?? request.CustomSpecification;
            var quantity = dto.Quantity ?? request.Quantity;

            var validation = await ValidateItemOrCustomAsync(itemId, customName, customSpec, quantity);
            if (validation is not null)
                return ProcurementRequestResult<ProcurementRequestDto>.Fail(validation.Value);

            request.ItemId = itemId;
            request.CustomItemName = itemId is null ? customName : null;
            request.CustomSpecification = itemId is null ? customSpec : null;
        }

        if (dto.Quantity.HasValue)
        {
            if (dto.Quantity.Value <= 0)
                return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.QuantityNotPositive);
            request.Quantity = dto.Quantity.Value;
        }

        if (dto.Purpose is not null)
            request.Purpose = dto.Purpose;

        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ProcurementRequestResult<ProcurementRequestDto>.Ok(await ToDtoAsync(request));
    }

    public async Task<ProcurementRequestResult<ProcurementRequestDto>> SubmitAsync(long requestId, long userId)
    {
        // 草稿可能创建于在职期间，但提交时若已离职则必须拦截
        var source = await db.Users.FindAsync(userId);
        if (source is null || !source.Working)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.SourceInactive);

        var request = await db.ProcurementRequests.FindAsync(requestId);
        if (request is null)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.RequestNotFound);

        if (request.SourceId != userId)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.NotOwner);

        if (request.Status != RequestStatus.Draft)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.NotDraft);

        request.Status = RequestStatus.Pending;
        request.RequestedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ProcurementRequestResult<ProcurementRequestDto>.Ok(await ToDtoAsync(request));
    }

    public async Task<ProcurementRequestResult<ProcurementRequestDto>> CancelAsync(long requestId, long userId)
    {
        var request = await db.ProcurementRequests.FindAsync(requestId);
        if (request is null)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.RequestNotFound);

        if (request.SourceId != userId)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.NotOwner);

        if (request.Status is not (RequestStatus.Draft or RequestStatus.Pending or RequestStatus.Approved))
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.NotCancellable);

        request.Status = RequestStatus.Cancelled;
        request.CancelledAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ProcurementRequestResult<ProcurementRequestDto>.Ok(await ToDtoAsync(request));
    }

    public async Task<IReadOnlyList<ProcurementRequestDto>> ListMineAsync(
        long userId, string? status, string? from, string? to, string? search)
    {
        IQueryable<ProcurementRequest> query = db.ProcurementRequests
            .Include(r => r.Source)
            .Include(r => r.Item)
            .Include(r => r.AuditedBy)
            .Include(r => r.PurchasedBy)
            .Where(r => r.SourceId == userId);

        query = ApplyFilters(query, status, from, to, search);

        var requests = await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

        return requests.Select(ToDto).ToList();
    }

    public async Task<ProcurementRequestResult<ProcurementRequestDto>> GetByIdAsync(
        long requestId, long userId, bool isAdmin)
    {
        var request = await db.ProcurementRequests
            .Include(r => r.Source)
            .Include(r => r.Item)
            .Include(r => r.AuditedBy)
            .Include(r => r.PurchasedBy)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.RequestNotFound);

        if (!isAdmin && request.SourceId != userId)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.NotOwner);

        return ProcurementRequestResult<ProcurementRequestDto>.Ok(ToDto(request));
    }

    public async Task<IReadOnlyList<ProcurementRequestDto>> ListAllAsync(
        string? status, long? sourceId, string? from, string? to, string? search)
    {
        IQueryable<ProcurementRequest> query = db.ProcurementRequests
            .Include(r => r.Source)
            .Include(r => r.Item)
            .Include(r => r.AuditedBy)
            .Include(r => r.PurchasedBy);

        if (sourceId.HasValue)
            query = query.Where(r => r.SourceId == sourceId.Value);

        query = ApplyFilters(query, status, from, to, search);

        var requests = await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

        return requests.Select(ToDto).ToList();
    }

    public async Task<ProcurementRequestResult<ProcurementRequestDto>> AuditAsync(
        long requestId, long adminId, AuditProcurementRequestRequest dto)
    {
        var request = await db.ProcurementRequests.FindAsync(requestId);
        if (request is null)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.RequestNotFound);

        if (request.Status != RequestStatus.Pending)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.NotPending);

        var decision = dto.Decision?.Trim().ToLowerInvariant();
        if (decision == "approve")
        {
            request.Status = RequestStatus.Approved;
            request.AuditedById = adminId;
            request.AuditedAt = DateTime.UtcNow;
            request.RefusalReason = null;
        }
        else if (decision == "reject")
        {
            if (string.IsNullOrWhiteSpace(dto.RefusalReason))
                return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.RefusalReasonRequired);

            request.Status = RequestStatus.Rejected;
            request.AuditedById = adminId;
            request.AuditedAt = DateTime.UtcNow;
            request.RefusalReason = dto.RefusalReason;
        }
        else
        {
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.InvalidDecision);
        }

        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return ProcurementRequestResult<ProcurementRequestDto>.Ok(await ToDtoAsync(request));
    }

    public async Task<ProcurementRequestResult<ProcurementRequestDto>> PurchaseAsync(long requestId, long adminId)
    {
        var request = await db.ProcurementRequests
            .Include(r => r.Item)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.RequestNotFound);

        if (request.Status != RequestStatus.Approved)
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.NotPurchasable);

        await using var tx = await db.Database.BeginTransactionAsync();

        request.Status = RequestStatus.Purchased;
        request.PurchasedById = adminId;
        request.PurchasedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        if (request.ItemId.HasValue)
        {
            var stock = await db.StockItems.FirstOrDefaultAsync(s => s.ItemId == request.ItemId.Value);
            if (stock is null)
            {
                stock = new StockItem
                {
                    ItemId = request.ItemId.Value,
                    Quantity = request.Quantity,
                    Version = Guid.NewGuid().ToByteArray(),
                    UpdatedAt = DateTime.UtcNow
                };
                db.StockItems.Add(stock);
            }
            else
            {
                stock.Quantity += request.Quantity;
                stock.Version = Guid.NewGuid().ToByteArray();
                stock.UpdatedAt = DateTime.UtcNow;
            }

            db.StockTransactions.Add(new StockTransaction
            {
                ItemId = request.ItemId.Value,
                QuantityChange = request.Quantity,
                Type = TransactionType.ProcurementInbound,
                ReferenceType = "ProcurementRequest",
                ReferenceId = request.Id,
                OperatorId = adminId
            });
        }

        try
        {
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProcurementRequestResult<ProcurementRequestDto>.Fail(ProcurementRequestError.ConcurrencyConflict);
        }

        return ProcurementRequestResult<ProcurementRequestDto>.Ok(await ToDtoAsync(request));
    }

    private async Task<ProcurementRequestError?> ValidateItemOrCustomAsync(
        long? itemId, string? customName, string? customSpec, int quantity)
    {
        var hasItem = itemId.HasValue;
        var hasCustom = !string.IsNullOrWhiteSpace(customName) || !string.IsNullOrWhiteSpace(customSpec);

        // Catalog and custom item are mutually exclusive.
        if (hasItem == hasCustom)
            return ProcurementRequestError.InvalidItemOrCustom;

        if (quantity <= 0)
            return ProcurementRequestError.QuantityNotPositive;

        if (hasItem)
        {
            var item = await db.Items.FindAsync(itemId!.Value);
            if (item is null)
                return ProcurementRequestError.ItemNotFound;
            if (!item.IsActive)
                return ProcurementRequestError.ItemNotActive;
        }

        return null;
    }

    private static IQueryable<ProcurementRequest> ApplyFilters(
        IQueryable<ProcurementRequest> query,
        string? status, string? from, string? to, string? search)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (TryParseStatus(status, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);
            else
                return query.Where(_ => false); // Invalid status yields an empty result.
        }

        if (DateTime.TryParse(from, out var fromDate))
            query = query.Where(r => r.RequestedAt >= fromDate);

        if (DateTime.TryParse(to, out var toDate))
            query = query.Where(r => r.RequestedAt <= toDate);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.Purpose.Contains(search) ||
                (r.Item != null && r.Item.Name.Contains(search)) ||
                r.Source.Name.Contains(search));
        }

        return query;
    }

    private static bool TryParseStatus(string? value, out RequestStatus status)
        => Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    private async Task<ProcurementRequestDto> ToDtoAsync(ProcurementRequest request)
    {
        await db.Entry(request).Reference(r => r.Source).LoadAsync();
        await db.Entry(request).Reference(r => r.Item).LoadAsync();
        await db.Entry(request).Reference(r => r.AuditedBy).LoadAsync();
        await db.Entry(request).Reference(r => r.PurchasedBy).LoadAsync();
        return ToDto(request);
    }

    private static ProcurementRequestDto ToDto(ProcurementRequest request) => new()
    {
        Id = request.Id,
        SourceId = request.SourceId,
        SourceName = request.Source.Name,
        ItemId = request.ItemId,
        ItemName = request.Item?.Name,
        CustomItemName = request.CustomItemName,
        CustomSpecification = request.CustomSpecification,
        Quantity = request.Quantity,
        Purpose = request.Purpose,
        Status = request.Status.ToString(),
        AuditedByName = request.AuditedBy?.Name,
        AuditedAt = request.AuditedAt is null ? null : DateTime.SpecifyKind(request.AuditedAt.Value, DateTimeKind.Utc).ToString("o"),
        RefusalReason = request.RefusalReason,
        PurchasedByName = request.PurchasedBy?.Name,
        PurchasedAt = request.PurchasedAt is null ? null : DateTime.SpecifyKind(request.PurchasedAt.Value, DateTimeKind.Utc).ToString("o"),
        // SQLite 读取的 DateTime Kind 为 Unspecified，ToString("o") 不含 Z 后缀，
        // 前端会把 UTC 值误当作本地时间；此处显式标为 Utc 以输出带 Z 的 ISO 字符串。
        RequestedAt = DateTime.SpecifyKind(request.RequestedAt, DateTimeKind.Utc).ToString("o"),
        CancelledAt = request.CancelledAt is null ? null : DateTime.SpecifyKind(request.CancelledAt.Value, DateTimeKind.Utc).ToString("o")
    };
}
