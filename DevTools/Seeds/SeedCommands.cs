using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.Models;

namespace DevTools.Seeds;

// Creation and reset share the same fixed work IDs, so keeping them together
// prevents the two lists from drifting apart.
internal static class SeedCommands
{
    public static readonly string[] SeedWorkIds =
        { "A001", "ADMIN001", "W001", "WADMIN001", "P001", "PADMIN001" };

    public static async Task<int> SeedAllAsync(ProcurementDbContext db)
    {
        var results = new[]
        {
            await SeedUserAsync(db, "A001", "测试员工", Role.Employee),
            await SeedUserAsync(db, "ADMIN001", "系统管理员", Role.Admin),
            await SeedUserAsync(db, "W001", "仓库测试员工", Role.Employee),
            await SeedUserAsync(db, "WADMIN001", "仓库管理员", Role.Admin),
            await SeedUserAsync(db, "P001", "采购测试员工", Role.Employee),
            await SeedUserAsync(db, "PADMIN001", "采购管理员", Role.Admin)
        };

        return results.Any(r => r != 0) ? 1 : 0;
    }

    public static async Task<int> SeedUserAsync(
        ProcurementDbContext db, string workId, string name, Role role)
    {
        if (await db.Users.AnyAsync(u => u.WorkId == workId))
        {
            Console.WriteLine($"Employee '{workId}' already exists, skipping.");
            return 0;
        }

        // Reuse the seeded department when available; create one otherwise so the
        // tool works even on a fresh database where the web app has not run yet.
        var department = await db.Departments.FirstOrDefaultAsync();
        if (department is null)
        {
            department = new Department { Name = "研发部" };
            db.Departments.Add(department);
            await db.SaveChangesAsync();
        }

        var user = new User
        {
            WorkId = workId,
            Name = name,
            Role = role,
            DepartmentId = department.Id
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var roleLabel = role == Role.Admin ? "admin" : "employee";
        Console.WriteLine($"Created {roleLabel} '{workId}' ({name}) in department '{department.Name}'.");
        Console.WriteLine("Register it through the API, then log in to obtain a token.");

        return 0;
    }

    public static async Task<int> ResetAsync(ProcurementDbContext db, string? workId)
    {
        // No workId means "reset every seed account", so the whole dev fixture
        // returns to a clean state in one step.
        var targets = workId is null ? SeedWorkIds : new[] { workId };

        var resetAny = false;
        foreach (var target in targets)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.WorkId == target);

            if (user is null)
            {
                Console.WriteLine($"Skip '{target}': no employee with that work ID.");
                continue;
            }

            // Unbind the login account so register can accept this employee again.
            user.Username = null;
            user.PasswordHash = null;
            user.UpdatedAt = DateTime.UtcNow;

            // Refresh tokens are worthless once the account is unbound, so remove them
            // to avoid stale credentials surviving the reset.
            var tokens = await db.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
            db.RefreshTokens.RemoveRange(tokens);

            Console.WriteLine($"Reset login binding for '{target}' ({user.Name}).");
            resetAny = true;
        }

        await db.SaveChangesAsync();
        return resetAny ? 0 : 1;
    }
}
