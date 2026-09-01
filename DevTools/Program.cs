using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.Models;

// Usage (from the solution root):
//   dotnet run --project DevTools [command] [workId] [dbPath]
//
// Commands:
//   seed [dbPath]                  Create all seed accounts used by the tests:
//                                  A001, ADMIN001, and W001.
//   seed-employee [dbPath]         Create the A001 employee record.
//   seed-admin [dbPath]            Create the ADMIN001 admin record.
//   seed-warehouse [dbPath]        Create the W001 employee record used by the
//                                  warehouse tests.
//   seed-warehouse-admin [dbPath]  Create the WADMIN001 admin record used by the
//                                  warehouse tests.
//   reset [workId] [dbPath]        Unbind login data for every seed account
//                                  (A001, ADMIN001, W001), or for the given workId only.
//   del-user [workId] [dbPath]     Delete seed accounts (A001, ADMIN001, W001 by
//                                  default), or the given workId. Prints every
//                                  deleted row.
//   cleanup-warehouse [dbPath]     Delete the item type, items, stock snapshots,
//                                  and transactions created by the warehouse
//                                  tests, printing every deleted row.
//   promote <workId> [dbPath]      Set the employee's role to Admin.
//   demote <workId> [dbPath]       Set the employee's role back to Employee.
//   help                           Print this help text.
//
// With no command, runs cleanup-warehouse, then del-user, then reset so a plain
// `dotnet run --project DevTools` removes all test leftovers and leaves the
// database with no seed accounts.
var command = args.Length > 0 ? args[0] : null;
var dbPathArg = command is "seed" or "seed-employee" or "seed-admin" or "seed-warehouse" or "cleanup-warehouse"
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

if (command is null)
{
    // Cleanup order matters: warehouse catalog rows must go before users
    // because StockTransaction.OperatorId references the operator user.
    var cleaned = await CleanupWarehouseAsync(db);
    var deleted = await DeleteUserAsync(db, null);
    var reset = await ResetAsync(db, null);
    return cleaned == 0 && deleted == 0 && reset == 0 ? 0 : 1;
}

return command switch
{
    "seed" => await SeedAllAsync(db),
    "seed-employee" => await SeedUserAsync(db, "A001", "测试员工", Role.Employee),
    "seed-admin" => await SeedUserAsync(db, "ADMIN001", "系统管理员", Role.Admin),
    "seed-warehouse" => await SeedUserAsync(db, "W001", "仓库测试员工", Role.Employee),
    "seed-warehouse-admin" => await SeedUserAsync(db, "WADMIN001", "仓库管理员", Role.Admin),
    "reset" => await ResetAsync(db, workId),
    "del-user" => await DeleteUserAsync(db, workId),
    "cleanup-warehouse" => await CleanupWarehouseAsync(db),
    "promote" => await SetRoleAsync(db, GetRequiredWorkId(command, args), Role.Admin),
    "demote" => await SetRoleAsync(db, GetRequiredWorkId(command, args), Role.Employee),
    "help" => PrintHelp(),
    _ => UnknownCommand(command)
};

static async Task<int> SeedAllAsync(ProcurementDbContext db)
{
    var results = new[]
    {
        await SeedUserAsync(db, "A001", "测试员工", Role.Employee),
        await SeedUserAsync(db, "ADMIN001", "系统管理员", Role.Admin),
        await SeedUserAsync(db, "W001", "仓库测试员工", Role.Employee),
        await SeedUserAsync(db, "WADMIN001", "仓库管理员", Role.Admin)
    };

    return results.Any(r => r != 0) ? 1 : 0;
}

static async Task<int> ResetAsync(ProcurementDbContext db, string? workId)
{
    // No workId means "reset every seed account", so the whole dev fixture
    // returns to a clean state in one step.
    var targets = workId is null ? new[] { "A001", "ADMIN001", "W001", "WADMIN001" } : new[] { workId };

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
    ProcurementDbContext db, string workId, string name, Role role,
    string? username = null, string? password = null)
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

    // Some tests log in directly, so those seed accounts need a pre-bound
    // login account instead of going through /api/auth/register.
    if (username is not null && password is not null)
    {
        user.Username = username;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
    }

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var roleLabel = role == Role.Admin ? "admin" : "employee";
    Console.WriteLine($"Created {roleLabel} '{workId}' ({name}) in department '{department.Name}'.");
    if (username is not null)
    {
        Console.WriteLine($"Bound login account '{username}' with the given password.");
    }
    else
    {
        Console.WriteLine("Register it through the API, then log in to obtain a token.");
    }

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

static async Task<int> CleanupWarehouseAsync(ProcurementDbContext db)
{
    // Scope is deliberately minimal: only the item type and items that the
    // warehouse tests create.
    var targetType = await db.ItemTypes.FirstOrDefaultAsync(t => t.Name == "办公耗材");
    var targetItems = await db.Items
        .Where(i => i.Name.StartsWith("A4复印纸"))
        .ToListAsync();
    var targetItemIds = targetItems.Select(i => i.Id).ToArray();

    var cleanedAny = false;

    if (targetItemIds.Length > 0)
    {
        // Transactions must go first because StockItem and Item both Restrict
        // deletes when transactions still reference them.
        var transactions = await db.StockTransactions
            .Where(t => targetItemIds.Contains(t.ItemId))
            .ToListAsync();
        foreach (var transaction in transactions)
        {
            Console.WriteLine($"Deleted stock transaction #<Id {transaction.Id}> item=<Id {transaction.ItemId}> change={transaction.QuantityChange} type={transaction.Type}.");
        }
        db.StockTransactions.RemoveRange(transactions);

        var stocks = await db.StockItems
            .Where(s => targetItemIds.Contains(s.ItemId))
            .ToListAsync();
        foreach (var stock in stocks)
        {
            Console.WriteLine($"Deleted stock snapshot #<Id {stock.Id}> item=<Id {stock.ItemId}> quantity={stock.Quantity}.");
        }
        db.StockItems.RemoveRange(stocks);

        foreach (var item in targetItems)
        {
            Console.WriteLine($"Deleted item #<Id {item.Id}> '<Name {item.Name}>'.");
        }
        db.Items.RemoveRange(targetItems);
        cleanedAny = true;
    }

    if (targetType is not null)
    {
        Console.WriteLine($"Deleted item type #<Id {targetType.Id}> '<Name {targetType.Name}>'.");
        db.ItemTypes.Remove(targetType);
        cleanedAny = true;
    }

    await db.SaveChangesAsync();
    return cleanedAny ? 0 : 1;
}

static async Task<int> DeleteUserAsync(ProcurementDbContext db, string? workId)
{
    // The default targets are the auth collection leftover (B001) plus all
    // seed accounts: A001, ADMIN001, W001, and WADMIN001. Keeping B001 here
    // makes repeated runs of both Postman collections deterministic.
    var targets = workId is null
        ? new[] { "B001", "A001", "ADMIN001", "W001", "WADMIN001" }
        : new[] { workId };

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
        foreach (var token in tokens)
        {
            Console.WriteLine($"Deleted token <'{TruncateMiddle(token.Token)}'>.");
        }
        db.RefreshTokens.RemoveRange(tokens);

        db.Users.Remove(user);
        Console.WriteLine($"Deleted user '<WorkId {user.WorkId}>' ('<Name {user.Name}>') id=<Id {user.Id}>.");
        deletedAny = true;
    }

    await db.SaveChangesAsync();
    return deletedAny ? 0 : 1;
}

static string TruncateMiddle(string value)
{
    // Long opaque strings (tokens, hashes) are truncated in the middle so the
    // prefix and suffix that matter for debugging stay readable.
    const int maxLength = 24;
    if (value.Length <= maxLength)
        return value;

    var keep = (maxLength - 3) / 2;
    return $"{value[..keep]}...{value[^keep..]}";
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
          (no command)                    Clean warehouse test data, delete all seed
                                          accounts (A001, ADMIN001, W001), then reset.
          seed [dbPath]                   Create all seed accounts at once.
          seed-employee [dbPath]          Create the A001 employee record.
          seed-admin [dbPath]             Create the ADMIN001 admin record.
          seed-warehouse [dbPath]         Create the W001 employee record used by
                                          the warehouse tests.
          seed-warehouse-admin [dbPath]   Create the WADMIN001 admin record used by
                                          the warehouse tests.
          reset [workId] [dbPath]         Reset login binding for seed accounts
                                          (A001, ADMIN001, W001), or the given workId only.
          del-user [workId] [dbPath]      Delete seed accounts (A001, ADMIN001, W001
                                          by default), or the given workId.
          cleanup-warehouse [dbPath]      Delete warehouse test data (item type, items,
                                          stock snapshots, transactions).
          promote <workId> [dbPath]       Set the employee's role to Admin.
          demote <workId> [dbPath]        Set the employee's role back to Employee.
          help                            Show this help.
        """);
    return 0;
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'. Run `dotnet run --project DevTools -- help` to list commands.");
    return 1;
}
