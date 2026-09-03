using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProcurementSystem.Data;
using ProcurementSystem.DTOs;
using ProcurementSystem.Models;

namespace ProcurementSystem.Services;

public enum AuthError
{
    InvalidCredentials,
    PasswordMismatch,
    WorkIdNotFound,
    UsernameTaken,
    WorkIdAlreadyBound,
    TokenInvalid,
    // 账号存在但员工已标记离职，禁止继续登录
    UserInactive
}

// Distinguishes business failures so the controller can map each to the
// correct HTTP status without leaking EF or exception details.
public record AuthResult<T>(T? Value, AuthError? Error)
{
    public static AuthResult<T> Ok(T value) => new(value, null);
    public static AuthResult<T> Fail(AuthError error) => new(default, error);
}

public interface IAuthService
{
    Task<AuthResult<AuthResponse>> LoginAsync(LoginRequest dto);
    Task<AuthResult<UserDto>> RegisterAsync(RegisterRequest dto);
    Task<AuthResult<AuthResponse>> RefreshAsync(string refreshToken);
    Task<bool> VerifyAsync(string token);
}

public class AuthService(ProcurementDbContext db, IConfiguration config) : IAuthService
{
    private string JwtKey =>
        config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");

    private string JwtIssuer => config["Jwt:Issuer"] ?? "ProcurementSystem";

    private string JwtAudience => config["Jwt:Audience"] ?? "ProcurementSystem";

    private int AccessTokenMinutes => int.Parse(config["Jwt:AccessTokenMinutes"] ?? "30");

    private int RefreshTokenDays => int.Parse(config["Jwt:RefreshTokenDays"] ?? "7");

    public async Task<AuthResult<AuthResponse>> LoginAsync(LoginRequest dto)
    {
        // One field accepts username, work ID, phone, or email.
        var user = await db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u =>
                u.Username == dto.Username ||
                u.WorkId == dto.Username ||
                u.Phone == dto.Username ||
                u.Email == dto.Username);

        if (user is null)
            return AuthResult<AuthResponse>.Fail(AuthError.InvalidCredentials);

        if (user.PasswordHash is null ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return AuthResult<AuthResponse>.Fail(AuthError.InvalidCredentials);
        }

        // 离职员工禁止登录，即使密码正确也不签发令牌
        if (!user.Working)
            return AuthResult<AuthResponse>.Fail(AuthError.UserInactive);

        var (accessToken, refreshToken) = CreateTokens(user);
        await db.SaveChangesAsync();

        return AuthResult<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = UserMapper.ToDto(user)
        });
    }

    public async Task<AuthResult<UserDto>> RegisterAsync(RegisterRequest dto)
    {
        if (dto.Password != dto.PasswordAgain)
            return AuthResult<UserDto>.Fail(AuthError.PasswordMismatch);

        var user = await db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.WorkId == dto.WorkId);

        if (user is null)
            return AuthResult<UserDto>.Fail(AuthError.WorkIdNotFound);

        // 离职员工同样禁止自助注册，即使档案仍存在
        if (!user.Working)
            return AuthResult<UserDto>.Fail(AuthError.UserInactive);

        // Non-null Username means this employee record already has a login account.
        if (user.Username is not null)
            return AuthResult<UserDto>.Fail(AuthError.WorkIdAlreadyBound);

        if (await db.Users.AnyAsync(u => u.Username == dto.Username))
            return AuthResult<UserDto>.Fail(AuthError.UsernameTaken);

        user.Username = dto.Username;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return AuthResult<UserDto>.Ok(UserMapper.ToDto(user));
    }

    public async Task<AuthResult<AuthResponse>> RefreshAsync(string refreshToken)
    {
        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt <= DateTime.UtcNow)
            return AuthResult<AuthResponse>.Fail(AuthError.TokenInvalid);

        var user = await db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == stored.UserId);

        if (user is null)
            return AuthResult<AuthResponse>.Fail(AuthError.TokenInvalid);

        // 离职员工禁止通过 refresh token 续期，且立即撤销当前 token 防止重放
        if (!user.Working)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return AuthResult<AuthResponse>.Fail(AuthError.UserInactive);
        }

        // Rotate: revoke the old token so a stolen one can only be used once.
        stored.RevokedAt = DateTime.UtcNow;
        var (accessToken, newRefreshToken) = CreateTokens(user);
        await db.SaveChangesAsync();

        return AuthResult<AuthResponse>.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            User = UserMapper.ToDto(user)
        });
    }

    public Task<bool> VerifyAsync(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        try
        {
            handler.ValidateToken(token, CreateValidationParameters(), out _);
            return Task.FromResult(true);
        }
        catch (SecurityTokenException)
        {
            return Task.FromResult(false);
        }
        catch (ArgumentException)
        {
            return Task.FromResult(false);
        }
    }

    private (string AccessToken, string RefreshToken) CreateTokens(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays)
        };

        db.RefreshTokens.Add(refreshToken);
        return (accessToken, refreshToken.Token);
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            // JWT "sub" is the canonical subject identifier; the role claim
            // drives [Authorize(Roles = "Admin")] checks.
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = JwtIssuer,
        ValidateAudience = true,
        ValidAudience = JwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
        ClockSkew = TimeSpan.Zero
    };

}
