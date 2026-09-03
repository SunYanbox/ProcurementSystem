using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcurementSystem.DTOs;
using ProcurementSystem.Services;

namespace ProcurementSystem.Controllers;

[ApiController]
[Route("api/procurement-requests")]
public class ProcurementRequestsController(IProcurementRequestService procurementRequestService) : ControllerBase
{
    [HttpGet("statuses")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<string>>> ListStatuses()
    {
        var statuses = await procurementRequestService.ListStatusesAsync();
        return Ok(statuses);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ProcurementRequestDto>> CreateDraft(CreateProcurementRequestRequest dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await procurementRequestService.CreateDraftAsync(userId.Value, dto);
        return result.Error switch
        {
            ProcurementRequestError.SourceInactive => StatusCode(StatusCodes.Status403Forbidden),
            ProcurementRequestError.InvalidItemOrCustom or
            ProcurementRequestError.QuantityNotPositive or
            ProcurementRequestError.ItemNotFound or
            ProcurementRequestError.ItemNotActive => BadRequest(),
            null => StatusCode(StatusCodes.Status201Created, result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpPut("{id:long}")]
    [Authorize]
    public async Task<ActionResult<ProcurementRequestDto>> EditDraft(long id, UpdateProcurementRequestRequest dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await procurementRequestService.EditDraftAsync(id, userId.Value, dto);
        return MapResult(result);
    }

    [HttpPost("{id:long}/submit")]
    [Authorize]
    public async Task<ActionResult<ProcurementRequestDto>> Submit(long id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await procurementRequestService.SubmitAsync(id, userId.Value);
        return MapResult(result);
    }

    [HttpPost("{id:long}/cancel")]
    [Authorize]
    public async Task<ActionResult<ProcurementRequestDto>> Cancel(long id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await procurementRequestService.CancelAsync(id, userId.Value);
        return MapResult(result);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ProcurementRequestDto>>> ListMine(
        [FromQuery] string? status,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? search)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var requests = await procurementRequestService.ListMineAsync(userId.Value, status, from, to, search);
        return Ok(requests);
    }

    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<ActionResult<ProcurementRequestDto>> GetById(long id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await procurementRequestService.GetByIdAsync(id, userId.Value, User.IsInRole("Admin"));
        return result.Error switch
        {
            ProcurementRequestError.RequestNotFound => NotFound(),
            ProcurementRequestError.NotOwner => Forbid(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ProcurementRequestDto>>> ListAll(
        [FromQuery] string? status,
        [FromQuery] long? sourceId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? search)
    {
        var requests = await procurementRequestService.ListAllAsync(status, sourceId, from, to, search);
        return Ok(requests);
    }

    [HttpPost("{id:long}/audit")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProcurementRequestDto>> Audit(long id, AuditProcurementRequestRequest dto)
    {
        var adminId = GetCurrentUserId();
        if (adminId is null)
            return Unauthorized();

        var result = await procurementRequestService.AuditAsync(id, adminId.Value, dto);
        return result.Error switch
        {
            ProcurementRequestError.RequestNotFound => NotFound(),
            ProcurementRequestError.NotPending or
            ProcurementRequestError.InvalidDecision or
            ProcurementRequestError.RefusalReasonRequired => BadRequest(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    [HttpPost("{id:long}/purchase")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProcurementRequestDto>> Purchase(long id)
    {
        var adminId = GetCurrentUserId();
        if (adminId is null)
            return Unauthorized();

        var result = await procurementRequestService.PurchaseAsync(id, adminId.Value);
        return result.Error switch
        {
            ProcurementRequestError.RequestNotFound => NotFound(),
            ProcurementRequestError.NotPurchasable => BadRequest(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    private ActionResult<ProcurementRequestDto> MapResult(ProcurementRequestResult<ProcurementRequestDto> result)
    {
        return result.Error switch
        {
            ProcurementRequestError.RequestNotFound => NotFound(),
            ProcurementRequestError.NotOwner => Forbid(),
            ProcurementRequestError.SourceInactive => StatusCode(StatusCodes.Status403Forbidden),
            ProcurementRequestError.NotDraft or
            ProcurementRequestError.NotCancellable or
            ProcurementRequestError.NotPurchasable or
            ProcurementRequestError.NotPending or
            ProcurementRequestError.InvalidDecision or
            ProcurementRequestError.RefusalReasonRequired or
            ProcurementRequestError.InvalidItemOrCustom or
            ProcurementRequestError.QuantityNotPositive or
            ProcurementRequestError.ItemNotFound or
            ProcurementRequestError.ItemNotActive => BadRequest(),
            null => Ok(result.Value),
            _ => throw new InvalidOperationException($"Unhandled error: {result.Error}")
        };
    }

    private long? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return sub is not null && long.TryParse(sub, out var userId) ? userId : null;
    }
}
