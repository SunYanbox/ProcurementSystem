namespace ProcurementSystem.DTOs;

public class UserDto
{
    public long Id { get; set; }

    // Null until the employee binds a login account.
    public string? Username { get; set; }

    public string WorkId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public long DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    // "Admin" or "Employee"; string keeps the API contract stable if the enum is renamed.
    public string Role { get; set; } = string.Empty;

    public bool Working { get; set; }

    public string? AvatarUrl { get; set; }

    // ISO 8601 string keeps the JSON shape independent of DateTime serialization settings.
    public string CreatedAt { get; set; } = string.Empty;
}
