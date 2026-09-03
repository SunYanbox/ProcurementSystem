using ProcurementSystem.DTOs;
using ProcurementSystem.Models;

namespace ProcurementSystem.Services;

// Shared User -> UserDto mapping so AuthService and UserService never drift apart.
public static class UserMapper
{
    // Callers must load user.Department beforehand.
    public static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        WorkId = user.WorkId,
        Name = user.Name,
        Email = user.Email,
        Phone = user.Phone,
        DepartmentId = user.DepartmentId,
        DepartmentName = user.Department.Name,
        Role = user.Role.ToString(),
        Working = user.Working,
        AvatarUrl = user.AvatarUrl,
        CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc).ToString("o")
    };
}
