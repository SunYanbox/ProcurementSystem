namespace ProcurementSystem.Models;

public class Item
{
    public long Id { get; set; }

    // Item names are not unique by design; the catalog may contain similar names
    // distinguished by specification.
    public string Name { get; set; } = string.Empty;

    public long TypeId { get; set; }

    public ItemType Type { get; set; } = null!;

    public string? Description { get; set; }

    public string Specification { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    // Use decimal to avoid floating-point rounding errors on prices.
    public decimal Price { get; set; }

    // Inactive items are hidden from catalog listings but kept for history.
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // One-to-one with the stock snapshot; null if no stock record exists yet.
    public StockItem? StockItem { get; set; }

    public ICollection<StockTransaction> Transactions { get; set; } = new List<StockTransaction>();
}
