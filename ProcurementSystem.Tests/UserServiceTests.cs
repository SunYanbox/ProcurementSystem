using ProcurementSystem.Services;
using Xunit;

namespace ProcurementSystem.Tests;

public class UserServiceTests : TestBase
{
    [Fact]
    public async Task GetMeAsync_ReturnsUserDto_WhenUserExists()
    {
        using var db = CreateDbContext();
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        employee.Username = "alice";
        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        await db.SaveChangesAsync();

        var service = CreateUserService(db);
        var dto = await service.GetMeAsync(employee.Id);

        Assert.NotNull(dto);
        Assert.Equal(employee.Id, dto.Id);
        Assert.Equal("alice", dto.Username);
        Assert.Equal("研发部", dto.DepartmentName);
    }

    [Fact]
    public async Task GetMeAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        using var db = CreateDbContext();
        var service = CreateUserService(db);

        var dto = await service.GetMeAsync(999);

        Assert.Null(dto);
    }
}
