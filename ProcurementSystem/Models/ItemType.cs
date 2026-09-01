namespace ProcurementSystem.Models;

public class ItemType
{
    public long Id { get; set; }

    // Uniqueness is enforced by a unique index in ProcurementDbContext.OnModelCreating.
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
