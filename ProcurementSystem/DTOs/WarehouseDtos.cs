namespace ProcurementSystem.DTOs;

public class CreateItemTypeRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class UpdateItemTypeRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class CreateItemRequest
{
    public string Name { get; set; } = string.Empty;

    public long TypeId { get; set; }

    public string? Description { get; set; }

    public string Specification { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Price { get; set; }
}

public class UpdateItemRequest
{
    // Every field is optional because PUT performs a partial update;
    // null means "keep the existing value".
    public string? Name { get; set; }

    public long? TypeId { get; set; }

    public string? Description { get; set; }

    public string? Specification { get; set; }

    public string? Unit { get; set; }

    public decimal? Price { get; set; }

    public bool? IsActive { get; set; }
}

public class CreateTransactionRequest
{
    // Keep type as string: the HTTP contract must not leak the C# enum type,
    // and invalid values are rejected by WarehouseService rather than model binding.
    public string Type { get; set; } = string.Empty;

    // Must be non-zero; the sign must match the transaction type's intent.
    public int QuantityChange { get; set; }

    public string? Note { get; set; }
}

public class ItemTypeDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class ItemDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public long TypeId { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Specification { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Price { get; set; }

    // Only populated by stock queries; null for catalog listings.
    public int? StockQuantity { get; set; }

    public bool IsActive { get; set; }
}

public class StockItemDto
{
    public long Id { get; set; }

    public long ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public string ItemSpecification { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public int Quantity { get; set; }

    // ISO 8601 string keeps the JSON shape independent of DateTime serialization settings.
    public string UpdatedAt { get; set; } = string.Empty;
}

public class StockTransactionDto
{
    public long Id { get; set; }

    public long ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int QuantityChange { get; set; }

    // "ManualInbound" / "ManualOutbound" / "ProcurementInbound" / "Adjustment".
    public string Type { get; set; } = string.Empty;

    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }

    public string? Note { get; set; }

    public string OperatorName { get; set; } = string.Empty;

    // ISO 8601 string keeps the JSON shape independent of DateTime serialization settings.
    public string CreatedAt { get; set; } = string.Empty;
}
