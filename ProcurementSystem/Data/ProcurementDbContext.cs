using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Models;

namespace ProcurementSystem.Data;

// Persistence only: maps entities to tables and configures constraints.
public class ProcurementDbContext : DbContext
{
    public ProcurementDbContext(DbContextOptions<ProcurementDbContext> options)
        : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(d => d.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            // Username and WorkId are business unique keys.
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.WorkId).IsUnique();

            // SQLite treats NULLs as distinct in unique indexes, so multiple users
            // may have no email or phone without violating uniqueness.
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Phone).IsUnique();

            entity.HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
