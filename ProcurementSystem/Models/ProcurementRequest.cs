namespace ProcurementSystem.Models;

public class ProcurementRequest
{
    public long Id { get; set; }

    // The employee who created the request.
    public long SourceId { get; set; }

    public User Source { get; set; } = null!;

    // Catalog item when the request references an existing item; null for custom items.
    public long? ItemId { get; set; }

    public Item? Item { get; set; }

    // Custom item fields are used only when ItemId is null.
    public string? CustomItemName { get; set; }

    public string? CustomSpecification { get; set; }

    public int Quantity { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    // Populated when an admin approves or rejects the request.
    public long? AuditedById { get; set; }

    public User? AuditedBy { get; set; }

    public DateTime? AuditedAt { get; set; }

    // Required only when the decision was reject.
    public string? RefusalReason { get; set; }

    // Populated when an admin completes the procurement inbound.
    public long? PurchasedById { get; set; }

    public User? PurchasedBy { get; set; }

    public DateTime? PurchasedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    // Submission time; for drafts this stays at creation time until submitted.
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
