using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.Models;

// Usage (from the solution root):
//   dotnet run --project DevTools [command] [workId] [dbPath]
//
// Commands:
//   reset [workId] [dbPath]        Unbind login data for every seeded account
//                                  (A001, ADMIN001), or for the given workId only.
//   seed-employee [dbPath]         Create the A001 employee record for testing.
//   seed-admin [dbPath]            Create the ADMIN001 admin employee record for testing.
//   promote <workId> [dbPath]      Set the employee's role to Admin.
//   demote <workId> [dbPath]       Set the employee's role back to Employee.
//   del-user [workId] [dbPath]  Delete test users created by the Postman collection
//                                  (B001 by default), or the given workId.
//   help                           Print this help text.
//
// With no command, defaults to `reset` so a plain `dotnet run --project DevTools`
// restores every seeded account to its unbound state.
var command = args.Length > 0 ? args[0] : "reset";
var dbPathArg = command is "seed-employee" or "seed-admin"
    ? (args.Length > 1 ? args[1] : null)
    : args.Length > 2 ? args[2] : null;
var dbPath = dbPathArg ?? Path.Combine("ProcurementSystem", "procurement.db");

// `reset` and `del-user` accept an optional workId; other commands handle their own arguments.
var workId = (command is "reset" or "del-user") && args.Length > 1 ? args[1] : null;

var options = new DbContextOptionsBuilder<ProcurementDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

using var db = new ProcurementDbContext(options);

// Migrate keeps the tool self-sufficient: the schema is created or upgraded
// without requiring the web app to have run first.
db.Database.Migrate();

return command switch
{
    "reset" => await ResetAsync(db, workId),
    "seed-employee" => await SeedUserAsync(db, "A001", "测试员工", Role.Employee),
    "seed-admin" => await SeedUserAsync(db, "ADMIN001", "系统管理员", Role.Admin),
    "promote" => await SetRoleAsync(db, GetRequiredWorkId(command, args), Role.Admin),
    "demote" => await SetRoleAsync(db, GetRequiredWorkId(command, args), Role.Employee),
    "del-user" => await DeleteUserAsync(db, workId),
    "help" => PrintHelp(),
    _ => UnknownCommand(command)
};

static async Task<int> ResetAsync(ProcurementDbContext db, string? workId)
{
    // No workId means "reset every seeded account", so the whole dev fixture
    // returns to a clean state in one step.
    var targets = workId is null ? new[] { "A001", "ADMIN001" } : new[] { workId };

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

static async Task<int> SeedUserAsync(
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

static async Task<int> SetRoleAsync(ProcurementDbContext db, string workId, Role role)
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

static async Task<int> DeleteUserAsync(ProcurementDbContext db, string? workId)
{
    // The Postman collection only creates B001, so that is the default cleanup
    // target. Passing a workId deletes exactly that user instead.
    var targets = workId is null ? new[] { "B001" } : new[] { workId };

    var deletedAny = false;
    foreach (var target in targets)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.WorkId == target);

        if (user is null)
        {
            Console.WriteLine($"Skip '{target}': no employee with that work ID.");
            continue;
        }

        // Refresh tokens are removed explicitly so no stale credentials survive
        // even though the FK cascade would normally handle it.
        var tokens = await db.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        db.RefreshTokens.RemoveRange(tokens);
        db.Users.Remove(user);

        Console.WriteLine($"Deleted user '{target}' ({user.Name}).");
        deletedAny = true;
    }

    await db.SaveChangesAsync();
    return deletedAny ? 0 : 1;
}

static string GetRequiredWorkId(string command, string[] args)
{
    if (args.Length > 1)
        return args[1];

    Console.Error.WriteLine($"Command '{command}' requires a workId argument.");
    return string.Empty;
}

static int PrintHelp()
{
    Console.WriteLine(
        """
        DevTools commands:
          reset [workId] [dbPath]         Reset login binding for seeded accounts
                                          (A001, ADMIN001), or for the given workId only.
          seed-employee [dbPath]          Create the A001 employee record.
          seed-admin [dbPath]             Create the ADMIN001 admin record.
          promote <workId> [dbPath]       Set the employee's role to Admin.
          demote <workId> [dbPath]        Set the employee's role back to Employee.
          del-user [workId] [dbPath]   Delete test users (B001 by default),
                                          or the given workId.
          help                            Show this help.
        """);
    return 0;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'. Run `dotnet run --project DevTools -- help` to list commands.");
    return 1;
}
