using System.ComponentModel.DataAnnotations;

namespace ProcurementSystem.DTOs;

public class LoginRequest
{
    // Username, work ID, phone, or email all accepted here.
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    // Must match an employee record created by an admin.
    [Required]
    public string WorkId { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string PasswordAgain { get; set; } = string.Empty;
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class VerifyRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public UserDto User { get; set; } = null!;
}
