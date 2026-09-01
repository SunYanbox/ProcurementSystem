namespace ProcurementSystem.Models;

public class StockTransaction
{
    public long Id { get; set; }

    public long ItemId { get; set; }

    public Item Item { get; set; } = null!;

    // Positive = inbound, negative = outbound. Zero is rejected by the service.
    public int QuantityChange { get; set; }

    public TransactionType Type { get; set; }

    // Optional link to the business document that caused this transaction,
    // e.g. "ProcurementRequest" when procurement inbound records one.
    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }

    public string? Note { get; set; }

    public long OperatorId { get; set; }

    public User Operator { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
