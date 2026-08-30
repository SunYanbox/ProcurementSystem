namespace ProcurementSystem.Models;

public class RefreshToken
{
    public long Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Null means the token is still valid; set when explicitly revoked.
    public DateTime? RevokedAt { get; set; }
}
