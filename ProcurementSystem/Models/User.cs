namespace ProcurementSystem.Models;

public class User
{
    public long Id { get; set; }

    // Null means the employee record exists but no login account is bound yet.
    public string? Username { get; set; }

    // Internal field only. Never expose in any DTO.
    // Null until the employee completes self-registration.
    public string? PasswordHash { get; set; }

    // Work IDs may contain leading zeros or letters.
    public string WorkId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public long DepartmentId { get; set; }

    // Navigation property; the relationship uses DeleteBehavior.Restrict to prevent
    // deleting a department that still has users.
    public Department Department { get; set; } = null!;

    // Defaults to Employee; administrators are assigned explicitly.
    public Role Role { get; set; } = Role.Employee;

    public bool Working { get; set; } = true;

    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
