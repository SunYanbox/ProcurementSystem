using ProcurementSystem.Models;
using Xunit;
using ProcurementSystem.Services;

namespace ProcurementSystem.Tests;

public class UserMapperTests
{
    [Fact]
    public void ToDto_MapsAllFields()
    {
        var dept = new Department { Id = 1, Name = "研发部" };
        var user = new User
        {
            Id = 10,
            Username = "alice",
            WorkId = "A001",
            Name = "Alice",
            Email = "alice@example.com",
            Phone = "13800000000",
            DepartmentId = 1,
            Department = dept,
            Role = Role.Employee,
            Working = true,
            AvatarUrl = "https://example.com/a.png",
            CreatedAt = new DateTime(2026, 8, 30, 18, 0, 0, DateTimeKind.Utc)
        };

        var dto = UserMapper.ToDto(user);

        Assert.Equal(10, dto.Id);
        Assert.Equal("alice", dto.Username);
        Assert.Equal("A001", dto.WorkId);
        Assert.Equal("Alice", dto.Name);
        Assert.Equal("alice@example.com", dto.Email);
        Assert.Equal("13800000000", dto.Phone);
        Assert.Equal(1, dto.DepartmentId);
        Assert.Equal("研发部", dto.DepartmentName);
        Assert.Equal("Employee", dto.Role);
        Assert.True(dto.Working);
        Assert.Equal("https://example.com/a.png", dto.AvatarUrl);
        Assert.Equal("2026-08-30T18:00:00.0000000Z", dto.CreatedAt);
    }

    [Fact]
    public void ToDto_KeepsNullableFieldsNull()
    {
        var dept = new Department { Id = 1, Name = "研发部" };
        var user = new User
        {
            Id = 11,
            Username = null,
            WorkId = "A002",
            Name = "Bob",
            DepartmentId = 1,
            Department = dept,
            Role = Role.Admin
        };

        var dto = UserMapper.ToDto(user);

        Assert.Null(dto.Username);
        Assert.Null(dto.Email);
        Assert.Null(dto.Phone);
        Assert.Null(dto.AvatarUrl);
        Assert.Equal("Admin", dto.Role);
    }
}
