using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.Models;
using DevTools.Seeds;
using DevTools.Users;
using DevTools.Warehouse;
using DevTools.Purchases;
using DevTools.Permissions;

// Usage (from the solution root):
//   dotnet run --project DevTools [command] [workId] [dbPath]
//
// Commands:
//   seed [dbPath]                  Create all seed accounts used by the tests:
//                                  A001, ADMIN001, W001, and WADMIN001.
//   seed-employee [dbPath]         Create the A001 employee record.
//   seed-admin [dbPath]            Create the ADMIN001 admin record.
//   seed-warehouse [dbPath]        Create the W001 employee record used by the
//                                  warehouse tests.
//   seed-warehouse-admin [dbPath]  Create the WADMIN001 admin record used by the
//                                  warehouse tests.
//   seed-purchase [dbPath]         Create the P001 employee record used by the
//                                  procurement tests.
//   seed-purchase-admin [dbPath]   Create the PADMIN001 admin record used by the
//                                  procurement tests.
//   reset [workId] [dbPath]        Unbind login data for every seed account
//                                  (A001, ADMIN001, W001, WADMIN001), or for the
//                                  given workId only.
//   del-user [workId] [dbPath]     Delete test users (B001 plus all seed accounts
//                                  by default), or the given workId. Prints every
//                                  deleted row.
//   cleanup-warehouse [dbPath]     Delete the item type, items, stock snapshots,
//                                  and transactions created by the warehouse
//                                  tests, printing every deleted row.
//   promote <workId> [dbPath]      Set the employee's role to Admin.
//   demote <workId> [dbPath]       Set the employee's role back to Employee.
//   help                           Print this help text.
//
// With no command, runs cleanup-warehouse, then del-user, then reset, then seed
// so a plain `dotnet run --project DevTools` removes the data produced by the
// Postman tests and restores a fixture that can be tested again immediately.
var command = args.Length > 0 ? args[0] : null;
var dbPathArg = command is "seed" or "seed-employee" or "seed-admin" or "seed-warehouse" or "seed-warehouse-admin" or "seed-purchase" or "seed-purchase-admin" or "cleanup-warehouse" or "cleanup-purchases"
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
    // Cleanup order matters: procurement requests reference catalog items and
    // users with Restrict, so they must go before warehouse rows and users.
    // Seeding comes last so a plain run leaves a ready-to-test fixture.
    var purchaseCleaned = await PurchaseCleanup.CleanupAsync(db);
    var cleaned = await WarehouseCleanup.CleanupAsync(db);
    var deleted = await UserCommands.DeleteUserAsync(db, null);
    var reset = await SeedCommands.ResetAsync(db, null);
    var seeded = await SeedCommands.SeedAllAsync(db);
    return purchaseCleaned == 0 && cleaned == 0 && deleted == 0 && reset == 0 && seeded == 0 ? 0 : 1;
}

return command switch
{
    "seed" => await SeedCommands.SeedAllAsync(db),
    "seed-employee" => await SeedCommands.SeedUserAsync(db, "A001", "测试员工", Role.Employee),
    "seed-admin" => await SeedCommands.SeedUserAsync(db, "ADMIN001", "系统管理员", Role.Admin),
    "seed-warehouse" => await SeedCommands.SeedUserAsync(db, "W001", "仓库测试员工", Role.Employee),
    "seed-warehouse-admin" => await SeedCommands.SeedUserAsync(db, "WADMIN001", "仓库管理员", Role.Admin),
    "seed-purchase" => await SeedCommands.SeedUserAsync(db, "P001", "采购测试员工", Role.Employee),
    "seed-purchase-admin" => await SeedCommands.SeedUserAsync(db, "PADMIN001", "采购管理员", Role.Admin),
    "reset" => await SeedCommands.ResetAsync(db, workId),
    "del-user" => await UserCommands.DeleteUserAsync(db, workId),
    "cleanup-warehouse" => await WarehouseCleanup.CleanupAsync(db),
    "promote" => await RoleCommands.SetRoleAsync(db, GetRequiredWorkId(command, args), Role.Admin),
    "demote" => await RoleCommands.SetRoleAsync(db, GetRequiredWorkId(command, args), Role.Employee),
    "help" => PrintHelp(),
    _ => UnknownCommand(command)
};

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
          (no command)                    Clean up data produced by the tests and
                                          restore a ready-to-test fixture.
          seed [dbPath]                   Create all seed accounts at once.
          seed-employee [dbPath]          Create the A001 employee record.
          seed-admin [dbPath]             Create the ADMIN001 admin record.
          seed-warehouse [dbPath]         Create the W001 employee record used by the
                                          warehouse tests.
          seed-warehouse-admin [dbPath]   Create the WADMIN001 admin record used by the
                                          warehouse tests.
          seed-purchase [dbPath]          Create the P001 employee record used by the
                                          procurement tests.
          seed-purchase-admin [dbPath]    Create the PADMIN001 admin record used by the
                                          procurement tests.
          reset [workId] [dbPath]         Reset login binding for seed accounts, or the
                                          given workId only.
          del-user [workId] [dbPath]      Delete test users (B001 plus seed accounts by
                                          default), or the given workId.
          cleanup-warehouse [dbPath]      Delete warehouse test data.
          cleanup-purchases [dbPath]       Delete procurement request test data.
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
