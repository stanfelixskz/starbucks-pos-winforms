using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.Tests;

// Sales that could not be saved are kept in a file, so a crash or closing
// the app does not lose them, and stock is deducted exactly once.
public class PendingSaleTests : IDisposable
{
    // An instance that does not exist, so every connection fails
    private const string Unreachable = @"(localdb)\NoSuchInstance";

    private readonly string workingServer = Database.ServerName;

    public void Dispose() => Database.ServerName = workingServer;

    private static TestDatabase Start()
    {
        Skip.IfNot(TestSupport.LocalDbAvailable(), "SQL Server LocalDB is not available.");
        return new TestDatabase();
    }

    // A finished sale, as the cashier screen produces it
    private static Transaction MakeSale(string productId, int quantity)
    {
        var sale = new Transaction { CashierName = "Maria" };
        Product product = DataStore.GetProduct(productId)!;

        sale.AddItem(
            product,
            quantity,
            product is BeverageProduct ? TestSupport.Plain() : null);

        sale.ProcessPayment(new CashPayment(sale.Total, sale.Total + 100m));

        Assert.True(sale.CompleteTransaction());
        DataStore.Transactions.Add(sale);

        return sale;
    }

    // The sale is finished at the till but the database can not be reached
    private static Transaction SaleThatCouldNotBeSaved()
    {
        Transaction sale = MakeSale("P01", 2);

        Database.ServerName = Unreachable;
        Assert.ThrowsAny<Exception>(() => Database.SaveSale(sale));

        return sale;
    }

    private static decimal DatabaseBeans(TestDatabase db) =>
        (decimal)TestSupport.Scalar(db.Name,
            "SELECT Quantity FROM dbo.Ingredients WHERE IngredientId = 'I001'")!;

    private static int Count(TestDatabase db, string sql) =>
        (int)TestSupport.Scalar(db.Name, sql)!;

    [SkippableFact]
    public void A_sale_that_could_not_be_saved_is_written_to_the_file_straight_away()
    {
        using TestDatabase db = Start();

        Transaction sale = SaleThatCouldNotBeSaved();

        Assert.True(File.Exists(Database.PendingFilePath));
        Assert.Contains(sale.TransactionId, File.ReadAllText(Database.PendingFilePath));
    }

    [SkippableFact]
    public void A_waiting_sale_survives_a_restart_and_its_stock_is_deducted_once()
    {
        using TestDatabase db = Start();

        decimal beansBefore = DatabaseBeans(db);
        Transaction sale = SaleThatCouldNotBeSaved();
        decimal used = beansBefore - DataStore.GetIngredient("I001")!.Quantity;

        Assert.True(used > 0);

        // The app is closed and started again, and the database is back
        Database.ServerName = workingServer;
        db.Restart();

        Assert.Equal(1, Database.PendingSaleCount);
        Assert.Contains(DataStore.Transactions, t => t.TransactionId == sale.TransactionId);
        Assert.Null(Database.PendingFileProblem);

        // Its stock change is back in memory, but not in the database yet
        Assert.Equal(beansBefore - used, DataStore.GetIngredient("I001")!.Quantity);
        Assert.Equal(beansBefore, DatabaseBeans(db));

        // The next save writes the sale and the stock together
        Database.SaveInventory();

        Assert.Equal(0, Database.PendingSaleCount);
        Assert.False(File.Exists(Database.PendingFilePath));
        Assert.Equal(1, Count(db, "SELECT COUNT(*) FROM dbo.Transactions"));
        Assert.Equal(beansBefore - used, DatabaseBeans(db));

        // Starting again does not deduct or save anything a second time
        db.Restart();

        Assert.Equal(0, Database.PendingSaleCount);
        Assert.Single(DataStore.Transactions);
        Assert.Equal(beansBefore - used, DataStore.GetIngredient("I001")!.Quantity);
        Assert.Equal(beansBefore - used, DatabaseBeans(db));
    }

    [SkippableFact]
    public void The_sale_and_its_stock_records_come_back_with_the_same_details()
    {
        using TestDatabase db = Start();

        Transaction sale = SaleThatCouldNotBeSaved();
        decimal total = sale.Total;
        int records = DataStore.StockRecords.Count(r => r.Reference == sale.TransactionId);

        Database.ServerName = workingServer;
        db.Restart();

        Transaction back = Assert.Single(DataStore.Transactions);
        Assert.Equal(sale.TransactionId, back.TransactionId);
        Assert.Equal(sale.Date, back.Date);
        Assert.Equal(sale.CompletedAt, back.CompletedAt);
        Assert.Equal("Maria", back.CashierName);
        Assert.Equal(total, back.Total);
        Assert.Equal(sale.Items.Count, back.Items.Count);
        Assert.Equal("Tall", back.Items[0].Customization!.Size);
        Assert.Equal(sale.Payment is CashPayment, back.Payment is CashPayment);

        Assert.Equal(records,
            DataStore.StockRecords.Count(r => r.Reference == sale.TransactionId));
        Assert.True(records > 0);

        // A new order does not reuse the waiting order's number
        Assert.NotEqual(sale.TransactionId, new Transaction().TransactionId);
    }

    [SkippableFact]
    public void A_crash_after_the_database_save_but_before_the_file_is_cleaned_saves_nothing_twice()
    {
        using TestDatabase db = Start();

        decimal beansBefore = DatabaseBeans(db);
        Transaction sale = SaleThatCouldNotBeSaved();
        decimal used = beansBefore - DataStore.GetIngredient("I001")!.Quantity;

        // Keep the file as it was while the sale was waiting
        string copy = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        File.Copy(Database.PendingFilePath, copy);

        try
        {
            // The database comes back and the sale is saved; the file is cleaned
            Database.ServerName = workingServer;
            Database.SaveInventory();

            Assert.False(File.Exists(Database.PendingFilePath));
            Assert.Equal(beansBefore - used, DatabaseBeans(db));

            // The crash: the app stopped before the file was cleaned, so the
            // old file is still there when it starts again
            File.Copy(copy, Database.PendingFilePath);
            db.Restart();

            Assert.Equal(0, Database.PendingSaleCount);
            Assert.Equal(1, Count(db, "SELECT COUNT(*) FROM dbo.Transactions"));
            Assert.Single(DataStore.Transactions);
            Assert.Equal(beansBefore - used, DataStore.GetIngredient("I001")!.Quantity);
            Assert.Equal(beansBefore - used, DatabaseBeans(db));
            Assert.Equal(1, Count(db,
                $"SELECT COUNT(*) FROM dbo.StockRecords WHERE Reference = '{sale.TransactionId}' " +
                "AND ItemId = 'I001'"));

            // ...and the leftover file is gone
            Assert.False(File.Exists(Database.PendingFilePath));
        }
        finally
        {
            File.Delete(copy);
        }
    }

    [SkippableFact]
    public void A_save_whose_answer_was_lost_is_not_saved_or_deducted_twice()
    {
        using TestDatabase db = Start();

        decimal beansBefore = DatabaseBeans(db);
        Transaction sale = SaleThatCouldNotBeSaved();
        decimal used = beansBefore - DataStore.GetIngredient("I001")!.Quantity;

        // In truth the database had received it (sale and stock), but the
        // answer never reached the app
        Database.ServerName = workingServer;

        TestSupport.Sql(db.Name,
            "INSERT INTO dbo.Transactions VALUES (" +
            $"'{sale.TransactionId}', '{sale.Date:yyyy-MM-ddTHH:mm:ss.fffffff}', " +
            $"'{sale.CompletedAt:yyyy-MM-ddTHH:mm:ss.fffffff}', 'Maria', " +
            $"{sale.Total.ToString(CultureInfo.InvariantCulture)}, NULL); " +
            $"UPDATE dbo.Ingredients SET Quantity = Quantity - {used.ToString(CultureInfo.InvariantCulture)} " +
            "WHERE IngredientId = 'I001'");

        Database.SaveInventory();

        Assert.Equal(0, Database.PendingSaleCount);
        Assert.Equal(1, Count(db, "SELECT COUNT(*) FROM dbo.Transactions"));
        Assert.Equal(beansBefore - used, DatabaseBeans(db));
        Assert.Equal(beansBefore - used, DataStore.GetIngredient("I001")!.Quantity);
    }

    [SkippableFact]
    public void A_corrupt_file_is_set_aside_and_the_app_still_starts()
    {
        using TestDatabase db = Start();

        Directory.CreateDirectory(Database.PendingFolder);
        File.WriteAllText(Database.PendingFilePath, "{ this is not json");

        db.Restart();

        Assert.Equal(0, Database.PendingSaleCount);
        Assert.NotNull(Database.PendingFileProblem);
        Assert.False(File.Exists(Database.PendingFilePath));

        // The bad file is kept for someone to look at
        string kept = Assert.Single(Directory.GetFiles(
            Database.PendingFolder, $"pending-sales-{db.Name}.json.unreadable-*"));
        Assert.Equal("{ this is not json", File.ReadAllText(kept));

        // And the till carries on as normal
        MakeSale("P01", 1);
        Database.SaveSale(DataStore.Transactions.Last());

        Assert.Equal(1, Count(db, "SELECT COUNT(*) FROM dbo.Transactions"));
    }

    [SkippableFact]
    public void A_sale_that_cannot_be_rebuilt_is_skipped_and_the_rest_are_kept()
    {
        using TestDatabase db = Start();

        Transaction good = SaleThatCouldNotBeSaved();
        Database.ServerName = workingServer;

        // Add a second entry that points at a product that does not exist
        JsonNode root = JsonNode.Parse(File.ReadAllText(Database.PendingFilePath))!;
        JsonArray sales = root["Sales"]!.AsArray();
        JsonNode broken = sales[0]!.DeepClone();

        broken["Id"] = "T98";
        broken["Items"]![0]!["ProductId"] = "NOPE";
        sales.Add(broken);

        File.WriteAllText(Database.PendingFilePath, root.ToJsonString());

        db.Restart();

        Assert.Equal(1, Database.PendingSaleCount);
        Assert.NotNull(Database.PendingFileProblem);
        Assert.Contains(DataStore.Transactions, t => t.TransactionId == good.TransactionId);
        Assert.DoesNotContain(DataStore.Transactions, t => t.TransactionId == "T98");
        Assert.Single(Directory.GetFiles(
            Database.PendingFolder, $"pending-sales-{db.Name}.json.unreadable-*"));
    }

    [SkippableFact]
    public void Without_a_file_nothing_is_waiting()
    {
        using TestDatabase db = Start();

        db.Restart();

        Assert.Equal(0, Database.PendingSaleCount);
        Assert.Null(Database.PendingFileProblem);
        Assert.False(File.Exists(Database.PendingFilePath));
    }
}
