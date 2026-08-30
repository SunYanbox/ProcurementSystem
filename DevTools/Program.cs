using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;

// Usage (from the solution root):
//   dotnet run --project DevTools [workId] [dbPath]
// Defaults target the seeded employee and the web app's SQLite file,
// so a plain `dotnet run --project DevTools` resets the register flow.
var workId = args.Length > 0 ? args[0] : "A001";
var dbPath = args.Length > 1 ? args[1] : Path.Combine("ProcurementSystem", "procurement.db");

var options = new DbContextOptionsBuilder<ProcurementDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

using var db = new ProcurementDbContext(options);

// Migrate keeps the tool self-sufficient: the schema is created or upgraded
// without requiring the web app to have run first.
db.Database.Migrate();

var user = await db.Users.FirstOrDefaultAsync(u => u.WorkId == workId);

if (user is null)
{
    Console.Error.WriteLine($"No employee with work ID '{workId}' found.");
    return 1;
}

// Unbind the login account so register can accept this employee again.
user.Username = null;
user.PasswordHash = null;
user.UpdatedAt = DateTime.UtcNow;

// Refresh tokens are worthless once the account is unbound, so remove them
// to avoid stale credentials surviving the reset.
var tokens = await db.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
db.RefreshTokens.RemoveRange(tokens);

await db.SaveChangesAsync();

Console.WriteLine($"Reset login binding for '{workId}' ({user.Name}).");
return 0;
