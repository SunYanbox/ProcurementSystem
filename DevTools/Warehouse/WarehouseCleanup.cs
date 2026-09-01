using Microsoft.EntityFrameworkCore;
using ProcurementSystem.Data;

namespace DevTools.Warehouse;

// Deletes exactly the catalog and stock rows that warehouse.postman_collection.json
// creates, so repeated Postman runs do not accumulate leftovers.
internal static class WarehouseCleanup
{
    public static async Task<int> CleanupAsync(ProcurementDbContext db)
    {
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
                Console.WriteLine(
                    $"Deleted stock transaction id={transaction.Id} itemId={transaction.ItemId} " +
                    $"change={transaction.QuantityChange} type={transaction.Type}.");
            }
            db.StockTransactions.RemoveRange(transactions);

            var stocks = await db.StockItems
                .Where(s => targetItemIds.Contains(s.ItemId))
                .ToListAsync();
            foreach (var stock in stocks)
            {
                Console.WriteLine($"Deleted stock snapshot id={stock.Id} itemId={stock.ItemId} quantity={stock.Quantity}.");
            }
            db.StockItems.RemoveRange(stocks);

            foreach (var item in targetItems)
            {
                Console.WriteLine($"Deleted item id={item.Id} name='{item.Name}'.");
            }
            db.Items.RemoveRange(targetItems);
            cleanedAny = true;
        }

        if (targetType is not null)
        {
            Console.WriteLine($"Deleted item type id={targetType.Id} name='{targetType.Name}'.");
            db.ItemTypes.Remove(targetType);
            cleanedAny = true;
        }

        await db.SaveChangesAsync();
        return cleanedAny ? 0 : 1;
    }
}
