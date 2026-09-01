using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.DTOs;
using ProcurementSystem.Models;

namespace ProcurementSystem.Services;

public enum UserError
{
    WorkIdTaken,
    EmailTaken,
    PhoneTaken,
    PhoneInvalid,
    DepartmentNotFound,
    RoleInvalid,
    UserNotFound,
    WrongPassword,
    PasswordTooWeak
}

// Mirrors AuthResult but stays in the user domain so admin operations
// do not depend on authentication error semantics.
public record UserResult<T>(T? Value, UserError? Error)
{
    public static UserResult<T> Ok(T value) => new(value, null);
    public static UserResult<T> Fail(UserError error) => new(default, error);
}

public interface IUserService
{
    Task<UserDto?> GetMeAsync(long userId);
    Task<UserResult<UserDto>> CreateAsync(CreateUserRequest dto);
    Task<IReadOnlyList<UserDto>> ListAsync(long? departmentId, string? role, bool? working, string? search);
    Task<UserResult<UserDto>> GetByIdAsync(long id);
    Task<UserResult<UserDto>> UpdateAsync(long id, UpdateUserRequest dto);
    Task<UserResult<bool>> ChangePasswordAsync(long userId, ChangePasswordRequest dto);
    Task<UserResult<UserDto>> BindPhoneAsync(long userId, BindPhoneRequest dto);
    Task<UserResult<UserDto>> UnbindPhoneAsync(long userId);
}

public class UserService(ProcurementDbContext db) : IUserService
{
    public async Task<UserDto?> GetMeAsync(long userId)
    {
        var user = await db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user is null ? null : UserMapper.ToDto(user);
    }

    public async Task<UserResult<UserDto>> CreateAsync(CreateUserRequest dto)
    {
        if (!TryParseRole(dto.Role, out var role))
            return UserResult<UserDto>.Fail(UserError.RoleInvalid);

        if (!await db.Departments.AnyAsync(d => d.Id == dto.DepartmentId))
            return UserResult<UserDto>.Fail(UserError.DepartmentNotFound);

        if (await db.Users.AnyAsync(u => u.WorkId == dto.WorkId))
            return UserResult<UserDto>.Fail(UserError.WorkIdTaken);

        // Blank strings are normalized to null so "" and null are treated the same.
        var email = NormalizeEmpty(dto.Email);
        var phone = NormalizeEmpty(dto.Phone);

        if (email is not null && await db.Users.AnyAsync(u => u.Email == email))
            return UserResult<UserDto>.Fail(UserError.EmailTaken);

        if (phone is not null && await db.Users.AnyAsync(u => u.Phone == phone))
            return UserResult<UserDto>.Fail(UserError.PhoneTaken);

        var user = new User
        {
            WorkId = dto.WorkId,
            Name = dto.Name,
            Email = email,
            Phone = phone,
            DepartmentId = dto.DepartmentId,
            Role = role
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Reload the navigation after SaveChanges so ToDto has the department name.
        await db.Entry(user).Reference(u => u.Department).LoadAsync();
        return UserResult<UserDto>.Ok(UserMapper.ToDto(user));
    }

    public async Task<IReadOnlyList<UserDto>> ListAsync(
        long? departmentId, string? role, bool? working, string? search)
    {
        IQueryable<User> query = db.Users.Include(u => u.Department);

        if (departmentId.HasValue)
            query = query.Where(u => u.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(role))
        {
            // An invalid role string yields an empty list rather than a 400,
            // because query filters are best-effort, not business-rule enforcement.
            if (TryParseRole(role, out var parsedRole))
                query = query.Where(u => u.Role == parsedRole);
            else
                return Array.Empty<UserDto>();
        }

        if (working.HasValue)
            query = query.Where(u => u.Working == working.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.Name.Contains(search) ||
                u.WorkId.Contains(search) ||
                (u.Username != null && u.Username.Contains(search)) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.Phone != null && u.Phone.Contains(search)));
        }

        var users = await query.ToListAsync();
        return users.Select(UserMapper.ToDto).ToList();
    }

    public async Task<UserResult<UserDto>> GetByIdAsync(long id)
    {
        var user = await db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user is null
            ? UserResult<UserDto>.Fail(UserError.UserNotFound)
            : UserResult<UserDto>.Ok(UserMapper.ToDto(user));
    }

    public async Task<UserResult<UserDto>> UpdateAsync(long id, UpdateUserRequest dto)
    {
        var user = await db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            return UserResult<UserDto>.Fail(UserError.UserNotFound);

        if (dto.Role is not null)
        {
            if (!TryParseRole(dto.Role, out var role))
                return UserResult<UserDto>.Fail(UserError.RoleInvalid);
            user.Role = role;
        }

        if (dto.DepartmentId.HasValue)
        {
            if (!await db.Departments.AnyAsync(d => d.Id == dto.DepartmentId.Value))
                return UserResult<UserDto>.Fail(UserError.DepartmentNotFound);
            user.DepartmentId = dto.DepartmentId.Value;
        }

        if (dto.Name is not null)
            user.Name = dto.Name;

        if (dto.Email is not null)
        {
            var email = NormalizeEmpty(dto.Email);
            // Exclude self so keeping the same email does not trigger a conflict.
            if (email is not null && await db.Users.AnyAsync(u => u.Id != id && u.Email == email))
                return UserResult<UserDto>.Fail(UserError.EmailTaken);
            user.Email = email;
        }

        if (dto.Phone is not null)
        {
            var phone = NormalizeEmpty(dto.Phone);
            if (phone is not null && await db.Users.AnyAsync(u => u.Id != id && u.Phone == phone))
                return UserResult<UserDto>.Fail(UserError.PhoneTaken);
            user.Phone = phone;
        }

        if (dto.Working.HasValue)
            user.Working = dto.Working.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return UserResult<UserDto>.Ok(UserMapper.ToDto(user));
    }

    public async Task<UserResult<bool>> ChangePasswordAsync(long userId, ChangePasswordRequest dto)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null)
            return UserResult<bool>.Fail(UserError.UserNotFound);

        // A user without a hash has not self-registered yet, so there is no
        // "old password" to verify — treat it as a wrong password.
        if (user.PasswordHash is null ||
            !BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
        {
            return UserResult<bool>.Fail(UserError.WrongPassword);
        }

        if (dto.NewPassword.Length < 6)
            return UserResult<bool>.Fail(UserError.PasswordTooWeak);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return UserResult<bool>.Ok(true);
    }

    public async Task<UserResult<UserDto>> BindPhoneAsync(long userId, BindPhoneRequest dto)
    {
        var user = await db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return UserResult<UserDto>.Fail(UserError.UserNotFound);

        var phone = NormalizeEmpty(dto.Phone);
        if (phone is null)
            return UserResult<UserDto>.Fail(UserError.PhoneInvalid);

        // Exclude self so re-submitting the same phone is a no-op rather than a conflict.
        if (await db.Users.AnyAsync(u => u.Id != userId && u.Phone == phone))
            return UserResult<UserDto>.Fail(UserError.PhoneTaken);

        user.Phone = phone;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return UserResult<UserDto>.Ok(UserMapper.ToDto(user));
    }

    public async Task<UserResult<UserDto>> UnbindPhoneAsync(long userId)
    {
        var user = await db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return UserResult<UserDto>.Fail(UserError.UserNotFound);

        user.Phone = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return UserResult<UserDto>.Ok(UserMapper.ToDto(user));
    }

    private static bool TryParseRole(string? value, out Role role)
    {
        if (value is null)
        {
            role = Role.Employee;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out role) &&
               Enum.IsDefined(role);
    }

    private static string? NormalizeEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
