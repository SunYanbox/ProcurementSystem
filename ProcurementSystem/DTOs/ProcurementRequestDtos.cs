namespace ProcurementSystem.DTOs;

public class CreateProcurementRequestRequest
{
    // Catalog item id; null when requesting a custom item.
    public long? ItemId { get; set; }

    // Custom item fields; must be null when ItemId is provided.
    public string? CustomItemName { get; set; }

    public string? CustomSpecification { get; set; }

    public int Quantity { get; set; }

    public string Purpose { get; set; } = string.Empty;
}

public class UpdateProcurementRequestRequest
{
    // Every field is optional because only Draft requests can be edited;
    // null means keep the existing value.
    public long? ItemId { get; set; }

    public string? CustomItemName { get; set; }

    public string? CustomSpecification { get; set; }

    public int? Quantity { get; set; }

    public string? Purpose { get; set; }
}

public class AuditProcurementRequestRequest
{
    // "approve" / "reject"; kept as string to keep the HTTP contract stable.
    public string Decision { get; set; } = string.Empty;

    // Required when Decision == "reject".
    public string? RefusalReason { get; set; }
}

public class ProcurementRequestDto
{
    public long Id { get; set; }

    public long SourceId { get; set; }

    public string SourceName { get; set; } = string.Empty;

    public long? ItemId { get; set; }

    public string? ItemName { get; set; }

    public string? CustomItemName { get; set; }

    public string? CustomSpecification { get; set; }

    public int Quantity { get; set; }

    public string Purpose { get; set; } = string.Empty;

    // "Draft" / "Pending" / "Approved" / "Rejected" / "Purchased" / "Cancelled".
    public string Status { get; set; } = string.Empty;

    public string? AuditedByName { get; set; }

    public string? AuditedAt { get; set; }

    public string? RefusalReason { get; set; }

    public string? PurchasedByName { get; set; }

    public string? PurchasedAt { get; set; }

    public string RequestedAt { get; set; } = string.Empty;

    public string? CancelledAt { get; set; }
}
