using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.DTOs;

namespace ProcurementSystem.Services;

public interface IUserService
{
    Task<UserDto?> GetMeAsync(long userId);
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
}
