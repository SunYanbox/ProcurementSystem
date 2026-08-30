using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProcurementSystem.Data;
using ProcurementSystem.Models;
using ProcurementSystem.Services;

namespace ProcurementSystem.Tests;

// Provides each test with a real SQLite in-memory database behind the same
// open connection, so EF Core queries run against an actual schema without
// touching the developer's procurement.db file.
public abstract class TestBase : IDisposable
{
    private readonly SqliteConnection _connection;

    protected TestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using (var db = CreateDbContext())
        {
            db.Database.Migrate();
        }
    }

    protected ProcurementDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProcurementDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ProcurementDbContext(options);
    }

    protected AuthService CreateAuthService(ProcurementDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-only-key-that-is-at-least-32-bytes-long-1234567890",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:AccessTokenMinutes"] = "30",
                ["Jwt:RefreshTokenDays"] = "7"
            })
            .Build();

        return new AuthService(db, config);
    }

    protected UserService CreateUserService(ProcurementDbContext db) => new(db);

    // Creates a department and an unbound employee (Username/PasswordHash null).
    protected async Task<(Department Dept, User Employee)> SeedUnboundEmployeeAsync(
        ProcurementDbContext db,
        string workId = "A001")
    {
        // Reuse the seeded department when present so multiple seed calls in one test do not violate the unique Department.Name index.
        var dept = await db.Departments.FirstOrDefaultAsync(d => d.Name == "研发部") ?? new Department { Name = "研发部" };
        var employee = new User
        {
            WorkId = workId,
            Name = "测试员工",
            Department = dept
        };

        db.Users.Add(employee);
        await db.SaveChangesAsync();
        return (dept, employee);
    }

    // Creates the same employee but with a login account already bound.
    protected async Task<(Department Dept, User Employee)> SeedBoundUserAsync(
        ProcurementDbContext db,
        string username = "testuser",
        string password = "password123",
        string workId = "A001")
    {
        var (dept, employee) = await SeedUnboundEmployeeAsync(db, workId);
        employee.Username = username;
        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        await db.SaveChangesAsync();
        return (dept, employee);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
