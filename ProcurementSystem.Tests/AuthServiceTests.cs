using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProcurementSystem.DTOs;
using ProcurementSystem.Models;
using ProcurementSystem.Services;
using Xunit;

namespace ProcurementSystem.Tests;

public class AuthServiceTests : TestBase
{
    private const string Username = "testuser";
    private const string Password = "password123";

    [Fact]
    public async Task LoginAsync_Succeeds_WithUsername()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);

        var result = await auth.LoginAsync(new LoginRequest { Username = Username, Password = Password });

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrEmpty(result.Value.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));
        Assert.Equal(Username, result.Value.User.Username);
    }

    [Fact]
    public async Task LoginAsync_Succeeds_WithWorkId()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);

        var result = await auth.LoginAsync(new LoginRequest { Username = "A001", Password = Password });

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task LoginAsync_Fails_WhenUserNotFound()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);

        var result = await auth.LoginAsync(new LoginRequest { Username = "ghost", Password = Password });

        Assert.Equal(AuthError.InvalidCredentials, result.Error);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task LoginAsync_Fails_WhenPasswordWrong()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);

        var result = await auth.LoginAsync(new LoginRequest { Username = Username, Password = "wrongpass" });

        Assert.Equal(AuthError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task RegisterAsync_Succeeds_ForUnboundEmployee()
    {
        using var db = CreateDbContext();
        await SeedUnboundEmployeeAsync(db);
        var auth = CreateAuthService(db);

        var result = await auth.RegisterAsync(new RegisterRequest
        {
            WorkId = "A001",
            Username = Username,
            Password = Password,
            PasswordAgain = Password
        });

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal(Username, result.Value.Username);
        var user = await db.Users.SingleAsync(u => u.WorkId == "A001");
        Assert.NotNull(user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_Fails_WhenPasswordsMismatch()
    {
        using var db = CreateDbContext();
        await SeedUnboundEmployeeAsync(db);
        var auth = CreateAuthService(db);

        var result = await auth.RegisterAsync(new RegisterRequest
        {
            WorkId = "A001",
            Username = Username,
            Password = Password,
            PasswordAgain = "different"
        });

        Assert.Equal(AuthError.PasswordMismatch, result.Error);
    }

    [Fact]
    public async Task RegisterAsync_Fails_WhenWorkIdNotFound()
    {
        using var db = CreateDbContext();
        var auth = CreateAuthService(db);

        var result = await auth.RegisterAsync(new RegisterRequest
        {
            WorkId = "NOBODY",
            Username = Username,
            Password = Password,
            PasswordAgain = Password
        });

        Assert.Equal(AuthError.WorkIdNotFound, result.Error);
    }

    [Fact]
    public async Task RegisterAsync_Fails_WhenWorkIdAlreadyBound()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);

        var result = await auth.RegisterAsync(new RegisterRequest
        {
            WorkId = "A001",
            Username = "another",
            Password = Password,
            PasswordAgain = Password
        });

        Assert.Equal(AuthError.WorkIdAlreadyBound, result.Error);
    }

    [Fact]
    public async Task RegisterAsync_Fails_WhenUsernameTaken()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        await SeedUnboundEmployeeAsync(db, workId: "A002");
        var auth = CreateAuthService(db);

        var result = await auth.RegisterAsync(new RegisterRequest
        {
            WorkId = "A002",
            Username = Username,
            Password = Password,
            PasswordAgain = Password
        });

        Assert.Equal(AuthError.UsernameTaken, result.Error);
    }

    [Fact]
    public async Task RefreshAsync_RotatesTokens_WhenTokenValid()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);
        var login = await auth.LoginAsync(new LoginRequest { Username = Username, Password = Password });
        var oldRefresh = login.Value!.RefreshToken;

        var result = await auth.RefreshAsync(oldRefresh);

        Assert.Null(result.Error);
        Assert.NotNull(result.Value);
        Assert.NotEqual(oldRefresh, result.Value.RefreshToken);
        var stored = await db.RefreshTokens.SingleAsync(t => t.Token == oldRefresh);
        Assert.NotNull(stored.RevokedAt);
    }

    [Fact]
    public async Task RefreshAsync_Fails_WhenTokenAlreadyRevoked()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);
        var login = await auth.LoginAsync(new LoginRequest { Username = Username, Password = Password });
        var refresh = login.Value!.RefreshToken;
        var stored = await db.RefreshTokens.SingleAsync(t => t.Token == refresh);
        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var result = await auth.RefreshAsync(refresh);

        Assert.Equal(AuthError.TokenInvalid, result.Error);
    }

    [Fact]
    public async Task RefreshAsync_Fails_WhenTokenExpired()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);
        var login = await auth.LoginAsync(new LoginRequest { Username = Username, Password = Password });
        var refresh = login.Value!.RefreshToken;
        var stored = await db.RefreshTokens.SingleAsync(t => t.Token == refresh);
        stored.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var result = await auth.RefreshAsync(refresh);

        Assert.Equal(AuthError.TokenInvalid, result.Error);
    }

    [Fact]
    public async Task RefreshAsync_Fails_WhenTokenUnknown()
    {
        using var db = CreateDbContext();
        var auth = CreateAuthService(db);

        var result = await auth.RefreshAsync("nonexistent-token");

        Assert.Equal(AuthError.TokenInvalid, result.Error);
    }

    [Fact]
    public async Task VerifyAsync_ReturnsTrue_ForValidToken()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);
        var login = await auth.LoginAsync(new LoginRequest { Username = Username, Password = Password });

        var valid = await auth.VerifyAsync(login.Value!.AccessToken);

        Assert.True(valid);
    }

    [Fact]
    public async Task VerifyAsync_ReturnsFalse_ForExpiredToken()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);
        var expiredToken = CreateExpiredJwt();

        var valid = await auth.VerifyAsync(expiredToken);

        Assert.False(valid);
    }

    [Fact]
    public async Task VerifyAsync_ReturnsFalse_ForTamperedToken()
    {
        using var db = CreateDbContext();
        await SeedBoundUserAsync(db);
        var auth = CreateAuthService(db);
        var login = await auth.LoginAsync(new LoginRequest { Username = Username, Password = Password });
        var tampered = login.Value!.AccessToken[..^2] + "xx";

        var valid = await auth.VerifyAsync(tampered);

        Assert.False(valid);
    }

    [Fact]
    public async Task LoginAsync_Fails_WhenUserInactive()
    {
        using var db = CreateDbContext();
        var (_, employee) = await SeedBoundUserAsync(db);
        employee.Working = false;
        await db.SaveChangesAsync();
        var auth = CreateAuthService(db);

        var result = await auth.LoginAsync(new LoginRequest { Username = Username, Password = Password });

        // 即使密码正确，离职员工也禁止登录，不签发任何令牌
        Assert.Equal(AuthError.UserInactive, result.Error);
        Assert.Null(result.Value);
    }

    // Signs a JWT whose exp is already in the past, to prove VerifyAsync checks lifetime.
    private static string CreateExpiredJwt()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-only-key-that-is-at-least-32-bytes-long-1234567890"));
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "1") },
            expires: DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
