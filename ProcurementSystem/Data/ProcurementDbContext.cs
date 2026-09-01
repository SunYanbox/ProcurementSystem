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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ItemType> ItemTypes => Set<ItemType>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

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

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(t => t.Token).IsUnique();

            // Refresh tokens are worthless without their user, so cascade on delete.
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemType>(entity =>
        {
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasOne(i => i.Type)
                .WithMany(t => t.Items)
                .HasForeignKey(i => i.TypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockItem>(entity =>
        {
            // One stock snapshot per item.
            entity.HasIndex(s => s.ItemId).IsUnique();

            // GUID byte array simulates rowversion on SQLite.
            entity.Property(s => s.Version).IsConcurrencyToken();

            entity.HasOne(s => s.Item)
                .WithOne(i => i.StockItem)
                .HasForeignKey<StockItem>(s => s.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasOne(t => t.Item)
                .WithMany(i => i.Transactions)
                .HasForeignKey(t => t.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Operator)
                .WithMany()
                .HasForeignKey(t => t.OperatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
