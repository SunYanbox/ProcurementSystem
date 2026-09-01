namespace ProcurementSystem.Models;

public class StockItem
{
    public long Id { get; set; }

    // One-to-one with Item; enforced by a unique index in ProcurementDbContext.
    public long ItemId { get; set; }

    public Item Item { get; set; } = null!;

    // Non-negative invariant is enforced by WarehouseService, not the database,
    // because the check requires business context (transaction type).
    public int Quantity { get; set; }

    // SQLite has no native rowversion type, so a GUID byte array is used as a
    // concurrency token and replaced on every snapshot update.
    public byte[] Version { get; set; } = Guid.NewGuid().ToByteArray();

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
