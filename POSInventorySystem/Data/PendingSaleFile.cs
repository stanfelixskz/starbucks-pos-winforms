using System.Text.Json;
using POSInventorySystem.Models;

namespace POSInventorySystem.Data;

// The waiting sales (finished at the till, not in the database yet) kept in
// a JSON file, so they survive a crash or closing the app. A sale is written
// together with the stock it used, because that stock change is not in the
// database either.
//
// The file is replaced by writing a temporary file and renaming it over the
// old one, so a crash while writing leaves the previous file intact.
internal static class PendingSaleFile
{
    private const int FormatVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    private sealed record Document(int Version, List<SaleEntry> Sales);

    private sealed record SaleEntry(
        string Id,
        DateTime OrderedAt,
        DateTime? CompletedAt,
        string Cashier,
        decimal? CashReceived,
        List<ItemEntry> Items,
        List<StockEntry> Stock);

    private sealed record ItemEntry(
        string ProductId,
        int Quantity,
        decimal UnitPrice,
        string? Size,
        string? Serving,
        string? Milk,
        int? ExtraShots,
        string? Syrup);

    private sealed record StockEntry(
        DateTime Timestamp,
        string ItemId,
        string ItemName,
        string Type,
        string Unit,
        decimal Count,
        decimal StockChange,
        string Reference,
        string Note);

    // A sale read back from the file
    public sealed record Loaded(Transaction Sale, List<StockRecord> Stock);

    // What a read found: the sales that could be rebuilt, and how many
    // entries could not
    public sealed record ReadResult(List<Loaded> Sales, int Unreadable);

    // ------------------------------------------------------------
    // WRITE
    // ------------------------------------------------------------

    // An empty list removes the file
    public static void Write(
        string path,
        IReadOnlyList<(Transaction Sale, List<StockRecord> Stock)> sales)
    {
        if (sales.Count == 0)
        {
            System.IO.File.Delete(path);
            return;
        }

        Document file = new Document(FormatVersion, sales.Select(s => ToEntry(s.Sale, s.Stock)).ToList());

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        string temp = path + ".tmp";

        System.IO.File.WriteAllText(temp, JsonSerializer.Serialize(file, Options));
        System.IO.File.Move(temp, path, overwrite: true);
    }

    private static SaleEntry ToEntry(Transaction sale, List<StockRecord> stock) =>
        new(
            sale.TransactionId,
            sale.Date,
            sale.CompletedAt,
            sale.CashierName,
            (sale.Payment as CashPayment)?.CashReceived,
            sale.Items.Select(i => new ItemEntry(
                i.Product.ProductId,
                i.Quantity,
                i.UnitPrice,
                i.Customization?.Size,
                i.Customization?.Serving,
                i.Customization?.Milk,
                i.Customization?.ExtraShots,
                i.Customization?.Syrup)).ToList(),
            stock.Select(r => new StockEntry(
                r.Timestamp, r.ItemId, r.ItemName, r.Type, r.Unit,
                r.Count, r.StockChange, r.Reference, r.Note)).ToList());

    // ------------------------------------------------------------
    // READ
    // ------------------------------------------------------------

    // Returns null when there is no file. Throws when the file as a whole
    // can not be read (not JSON, wrong shape); the caller keeps a copy.
    public static ReadResult? Read(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            return null;
        }

        Document? file = JsonSerializer.Deserialize<Document>(
            System.IO.File.ReadAllText(path), Options);

        if (file?.Sales == null)
        {
            throw new InvalidDataException("The file has no list of sales.");
        }

        List<Loaded> sales = new();
        int unreadable = 0;

        foreach (SaleEntry entry in file.Sales)
        {
            try
            {
                sales.Add(FromEntry(entry));
            }
            catch (Exception)
            {
                unreadable++;
            }
        }

        return new ReadResult(sales, unreadable);
    }

    private static Loaded FromEntry(SaleEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Id) || entry.Items.Count == 0)
        {
            throw new InvalidDataException("Incomplete sale.");
        }

        List<CartItem> items = new();

        foreach (ItemEntry i in entry.Items)
        {
            Product product = DataStore.GetProduct(i.ProductId)
                ?? throw new InvalidDataException($"Unknown product {i.ProductId}.");

            if (i.Quantity <= 0)
            {
                throw new InvalidDataException("Bad quantity.");
            }

            BeverageCustomization? customization = i.Size == null
                ? null
                : new BeverageCustomization(
                    i.Size, i.Serving ?? "", i.Milk ?? "None",
                    i.ExtraShots ?? 0, i.Syrup ?? "None");

            CartItem item = new CartItem(product, i.Quantity, customization);
            item.FreezePrice(i.UnitPrice);
            items.Add(item);
        }

        Transaction sale = Transaction.Restore(
            entry.Id,
            entry.OrderedAt,
            entry.CompletedAt,
            entry.Cashier ?? "",
            items,
            entry.CashReceived);

        List<StockRecord> stock = entry.Stock
            .Select(r => new StockRecord(
                r.Timestamp, r.ItemId, r.ItemName, r.Type, r.Unit,
                r.Count, r.StockChange, r.Reference, r.Note ?? ""))
            .ToList();

        return new Loaded(sale, stock);
    }
}
