using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.Models;

namespace DevTools.Permissions;

// Role changes are temporary test operations, not part of the seed fixture,
// so they live separately from account creation and cleanup.
internal static class RoleCommands
{
    public static async Task<int> SetRoleAsync(ProcurementDbContext db, string workId, Role role)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.WorkId == workId);

        if (user is null)
        {
            Console.Error.WriteLine($"No employee with work ID '{workId}' found.");
            return 1;
        }

        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Console.WriteLine($"Set '{workId}' ({user.Name}) role to {role}.");
        return 0;
    }
}
