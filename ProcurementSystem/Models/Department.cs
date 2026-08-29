namespace ProcurementSystem.Models;

public class Department
{
    public long Id { get; set; }

    // Uniqueness is enforced by a unique index in ProcurementDbContext.OnModelCreating.
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}
