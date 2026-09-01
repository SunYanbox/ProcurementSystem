using ProcurementSystem.DTOs;
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

    [Fact]
    public async Task CreateAsync_ReturnsUserDto_WhenInputValid()
    {
        using var db = CreateDbContext();
        var (dept, _) = await SeedUnboundEmployeeAsync(db);
        var service = CreateUserService(db);

        var result = await service.CreateAsync(new CreateUserRequest
        {
            WorkId = "B001",
            Name = "测试员工B",
            Email = "b001@example.com",
            Phone = "13800000002",
            DepartmentId = dept.Id,
            Role = "Employee"
        });

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal("B001", result.Value.WorkId);
        Assert.Equal("Employee", result.Value.Role);
        Assert.Equal(dept.Name, result.Value.DepartmentName);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenWorkIdTaken()
    {
        using var db = CreateDbContext();
        var (dept, employee) = await SeedUnboundEmployeeAsync(db, "B001");
        var service = CreateUserService(db);

        var result = await service.CreateAsync(new CreateUserRequest
        {
            WorkId = employee.WorkId,
            Name = "重复工号",
            DepartmentId = dept.Id
        });

        Assert.Equal(UserError.WorkIdTaken, result.Error);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenEmailTaken()
    {
        using var db = CreateDbContext();
        var (dept, employee) = await SeedUnboundEmployeeAsync(db);
        employee.Email = "taken@example.com";
        await db.SaveChangesAsync();
        var service = CreateUserService(db);

        var result = await service.CreateAsync(new CreateUserRequest
        {
            WorkId = "B001",
            Name = "邮箱冲突",
            Email = "taken@example.com",
            DepartmentId = dept.Id
        });

        Assert.Equal(UserError.EmailTaken, result.Error);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenPhoneTaken()
    {
        using var db = CreateDbContext();
        var (dept, employee) = await SeedUnboundEmployeeAsync(db);
        employee.Phone = "13800000001";
        await db.SaveChangesAsync();
        var service = CreateUserService(db);

        var result = await service.CreateAsync(new CreateUserRequest
        {
            WorkId = "B001",
            Name = "手机号冲突",
            Phone = "13800000001",
            DepartmentId = dept.Id
        });

        Assert.Equal(UserError.PhoneTaken, result.Error);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenDepartmentNotFound()
    {
        using var db = CreateDbContext();
        var service = CreateUserService(db);

        var result = await service.CreateAsync(new CreateUserRequest
        {
            WorkId = "B001",
            Name = "部门不存在",
            DepartmentId = 999
        });

        Assert.Equal(UserError.DepartmentNotFound, result.Error);
    }

    [Fact]
    public async Task CreateAsync_Fails_WhenRoleInvalid()
    {
        using var db = CreateDbContext();
        var (dept, _) = await SeedUnboundEmployeeAsync(db);
        var service = CreateUserService(db);

        var result = await service.CreateAsync(new CreateUserRequest
        {
            WorkId = "B001",
            Name = "非法角色",
            DepartmentId = dept.Id,
            Role = "SuperAdmin"
        });

        Assert.Equal(UserError.RoleInvalid, result.Error);
    }

    [Fact]
    public async Task CreateAsync_TreatsBlankEmailAndPhoneAsNull()
    {
        using var db = CreateDbContext();
        var (dept, _) = await SeedUnboundEmployeeAsync(db);
        var service = CreateUserService(db);

        var result = await service.CreateAsync(new CreateUserRequest
        {
            WorkId = "B001",
            Name = "空白归一化",
            Email = "",
            Phone = "  ",
            DepartmentId = dept.Id
        });

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Null(result.Value.Email);
        Assert.Null(result.Value.Phone);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllUsers_WhenNoFilters()
    {
        using var db = CreateDbContext();
        await SeedUnboundEmployeeAsync(db, "A001");
        await SeedUnboundEmployeeAsync(db, "B001");
        var service = CreateUserService(db);

        var users = await service.ListAsync(null, null, null, null);

        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task ListAsync_FiltersByDepartment()
    {
        using var db = CreateDbContext();
        var (dept, _) = await SeedUnboundEmployeeAsync(db, "A001");
        await SeedUnboundEmployeeAsync(db, "B001");
        var service = CreateUserService(db);

        var users = await service.ListAsync(dept.Id, null, null, null);

        Assert.Equal(2, users.Count);
        Assert.All(users, u => Assert.Equal(dept.Id, u.DepartmentId));
    }

    [Fact]
    public async Task ListAsync_FiltersByRole()
    {
        using var db = CreateDbContext();
        var (_, employee) = await SeedUnboundEmployeeAsync(db, "A001");
        employee.Role = ProcurementSystem.Models.Role.Admin;
        await db.SaveChangesAsync();
        await SeedUnboundEmployeeAsync(db, "B001");
        var service = CreateUserService(db);

        var users = await service.ListAsync(null, "Admin", null, null);

        Assert.Single(users);
        Assert.Equal("Admin", users[0].Role);
    }

    [Fact]
    public async Task ListAsync_FiltersByWorking()
    {
        using var db = CreateDbContext();
        var (_, employee) = await SeedUnboundEmployeeAsync(db, "A001");
        employee.Working = false;
        await db.SaveChangesAsync();
        await SeedUnboundEmployeeAsync(db, "B001");
        var service = CreateUserService(db);

        var users = await service.ListAsync(null, null, false, null);

        Assert.Single(users);
        Assert.False(users[0].Working);
    }

    [Fact]
    public async Task ListAsync_SearchesAcrossFields()
    {
        using var db = CreateDbContext();
        var (_, employee) = await SeedUnboundEmployeeAsync(db, "A001");
        employee.Name = "张三";
        employee.Phone = "13900001111";
        await db.SaveChangesAsync();
        await SeedUnboundEmployeeAsync(db, "B001");
        var service = CreateUserService(db);

        var byName = await service.ListAsync(null, null, null, "张三");
        var byPhone = await service.ListAsync(null, null, null, "13900001111");

        Assert.Single(byName);
        Assert.Equal("张三", byName[0].Name);
        Assert.Single(byPhone);
        Assert.Equal("张三", byPhone[0].Name);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenRoleInvalid()
    {
        using var db = CreateDbContext();
        await SeedUnboundEmployeeAsync(db, "A001");
        var service = CreateUserService(db);

        var users = await service.ListAsync(null, "Hacker", null, null);

        Assert.Empty(users);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUserDto_WhenUserExists()
    {
        using var db = CreateDbContext();
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var service = CreateUserService(db);

        var result = await service.GetByIdAsync(employee.Id);

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal(employee.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Fails_WhenUserNotFound()
    {
        using var db = CreateDbContext();
        var service = CreateUserService(db);

        var result = await service.GetByIdAsync(999);

        Assert.Equal(UserError.UserNotFound, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyProvidedFields()
    {
        using var db = CreateDbContext();
        var (dept, employee) = await SeedUnboundEmployeeAsync(db);
        var service = CreateUserService(db);

        var result = await service.UpdateAsync(employee.Id, new UpdateUserRequest
        {
            Name = "新姓名",
            Phone = "13800009999"
        });

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal("新姓名", result.Value.Name);
        Assert.Equal("13800009999", result.Value.Phone);
        Assert.Equal(dept.Id, result.Value.DepartmentId);
        Assert.Equal("Employee", result.Value.Role);
        Assert.True(result.Value.Working);
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenUserNotFound()
    {
        using var db = CreateDbContext();
        var service = CreateUserService(db);

        var result = await service.UpdateAsync(999, new UpdateUserRequest { Name = "x" });

        Assert.Equal(UserError.UserNotFound, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenEmailTaken_ByOtherUser()
    {
        using var db = CreateDbContext();
        var (_, employeeA) = await SeedUnboundEmployeeAsync(db, "A001");
        employeeA.Email = "shared@example.com";
        await db.SaveChangesAsync();
        var (_, employeeB) = await SeedUnboundEmployeeAsync(db, "B001");
        var service = CreateUserService(db);

        var result = await service.UpdateAsync(employeeB.Id, new UpdateUserRequest
        {
            Email = "shared@example.com"
        });

        Assert.Equal(UserError.EmailTaken, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_AllowsKeepingOwnEmail()
    {
        using var db = CreateDbContext();
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        employee.Email = "own@example.com";
        await db.SaveChangesAsync();
        var service = CreateUserService(db);

        var result = await service.UpdateAsync(employee.Id, new UpdateUserRequest
        {
            Email = "own@example.com"
        });

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal("own@example.com", result.Value.Email);
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenPhoneTaken_ByOtherUser()
    {
        using var db = CreateDbContext();
        var (_, employeeA) = await SeedUnboundEmployeeAsync(db, "A001");
        employeeA.Phone = "13800000001";
        await db.SaveChangesAsync();
        var (_, employeeB) = await SeedUnboundEmployeeAsync(db, "B001");
        var service = CreateUserService(db);

        var result = await service.UpdateAsync(employeeB.Id, new UpdateUserRequest
        {
            Phone = "13800000001"
        });

        Assert.Equal(UserError.PhoneTaken, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenRoleInvalid()
    {
        using var db = CreateDbContext();
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var service = CreateUserService(db);

        var result = await service.UpdateAsync(employee.Id, new UpdateUserRequest
        {
            Role = "SuperAdmin"
        });

        Assert.Equal(UserError.RoleInvalid, result.Error);
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenDepartmentNotFound()
    {
        using var db = CreateDbContext();
        var (_, employee) = await SeedUnboundEmployeeAsync(db);
        var service = CreateUserService(db);

        var result = await service.UpdateAsync(employee.Id, new UpdateUserRequest
        {
            DepartmentId = 999
        });

        Assert.Equal(UserError.DepartmentNotFound, result.Error);
    }
}
