using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.DTOs;

public class CreateUserRequest
{
    [Required]
    public string WorkId { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long DepartmentId { get; set; }

    // Keep role as string here: the HTTP contract must not leak the C# enum type,
    // and invalid values are rejected by UserService rather than by model binding.
    public string? Role { get; set; }
}

public class UpdateUserRequest
{
    // Every field is optional because PUT /api/users/{id} performs a partial update;
    // null means "keep the existing value".
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public long? DepartmentId { get; set; }

    public string? Role { get; set; }

    public bool? Working { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string OldPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class BindPhoneRequest
{
    [Required]
    public string Phone { get; set; } = string.Empty;
}

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
