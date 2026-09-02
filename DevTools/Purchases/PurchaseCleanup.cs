using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;

namespace DevTools.Purchases;

// Deletes exactly the procurement requests that
// procurement-requests.postman_collection.json creates (Source WorkId P001),
// so repeated Postman runs do not accumulate leftovers.
internal static class PurchaseCleanup
{
    public static async Task<int> CleanupAsync(ProcurementDbContext db)
    {
        var requests = await db.ProcurementRequests
            .Include(r => r.Source)
            .Where(r => r.Source.WorkId == "P001")
            .ToListAsync();

        foreach (var request in requests)
        {
            Console.WriteLine(
                $"Deleted procurement request id={request.Id} sourceWorkId={request.Source.WorkId} " +
                $"status={request.Status} itemId={request.ItemId?.ToString() ?? "null"} " +
                $"customItemName={request.CustomItemName ?? "null"} quantity={request.Quantity}.");
        }

        db.ProcurementRequests.RemoveRange(requests);
        await db.SaveChangesAsync();

        return requests.Count > 0 ? 0 : 1;
    }
}
