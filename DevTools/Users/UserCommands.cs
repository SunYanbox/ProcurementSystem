using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;
using ProcurementSystem.Models;

namespace DevTools.Users;

// Printing each deleted row makes the cleanup scope visible, which matters when
// a failed Postman run leaves unexpected accounts behind.
internal static class UserCommands
{
    public static readonly string[] DefaultDeleteWorkIds =
        { "B001", "A001", "ADMIN001", "W001", "WADMIN001" };

    public static async Task<int> DeleteUserAsync(ProcurementDbContext db, string? workId)
    {
        var targets = workId is null ? DefaultDeleteWorkIds : new[] { workId };

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
                Console.WriteLine($"Deleted token '{TruncateMiddle(token.Token)}'.");
            }
            db.RefreshTokens.RemoveRange(tokens);

            db.Users.Remove(user);
            Console.WriteLine($"Deleted user workId='{user.WorkId}' name='{user.Name}' id={user.Id}.");
            deletedAny = true;
        }

        await db.SaveChangesAsync();
        return deletedAny ? 0 : 1;
    }

    public static string TruncateMiddle(string value)
    {
        // Long opaque strings (tokens, hashes) are truncated in the middle so the
        // prefix and suffix that matter for debugging stay readable.
        const int maxLength = 24;
        if (value.Length <= maxLength)
            return value;

        var keep = (maxLength - 3) / 2;
        return $"{value[..keep]}...{value[^keep..]}";
    }
}
