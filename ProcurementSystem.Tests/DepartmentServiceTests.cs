using ProcurementSystem.Services;
using Xunit;

namespace ProcurementSystem.Tests;

public class DepartmentServiceTests : TestBase
{
    [Fact]
    public async Task ListAsync_ReturnsAllDepartments_OrderedById()
    {
        using var db = CreateDbContext();
        // SeedUnboundEmployeeAsync creates 研发部 first; add a second department
        // with a smaller Id-ordered name to prove ordering is by Id, not Name.
        await SeedUnboundEmployeeAsync(db);
        db.Departments.Add(new ProcurementSystem.Models.Department { Name = "财务部" });
        await db.SaveChangesAsync();
        var service = new DepartmentService(db);

        var departments = await service.ListAsync();

        Assert.Equal(2, departments.Count);
        Assert.Equal("研发部", departments[0].Name);
        Assert.Equal("财务部", departments[1].Name);
        Assert.True(departments[0].Id < departments[1].Id);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoDepartments()
    {
        using var db = CreateDbContext();
        var service = new DepartmentService(db);

        var departments = await service.ListAsync();

        Assert.Empty(departments);
    }
}
