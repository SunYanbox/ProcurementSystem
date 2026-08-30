namespace ProcurementSystem.DTOs;

public class LoginRequest
{
    // Username, work ID, phone, or email all accepted here.
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    // Must match an employee record created by an admin.
    public string WorkId { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string PasswordAgain { get; set; } = string.Empty;
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class VerifyRequest
{
    public string Token { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public UserDto User { get; set; } = null!;
}
