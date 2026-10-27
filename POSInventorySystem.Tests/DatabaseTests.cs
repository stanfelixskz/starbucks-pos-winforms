using Microsoft.Data.SqlClient;
using POSInventorySystem.Data;
using POSInventorySystem.Models;
using POSInventorySystem.Services;

namespace POSInventorySystem.Tests;

// These need SQL Server LocalDB. Each test makes its own throw-away
// database (see TestDatabase), and the tests are skipped on a machine that
// does not have LocalDB.
public class DatabaseTests
{
    private static TestDatabase Start()
    {
        Skip.IfNot(TestSupport.LocalDbAvailable(), "SQL Server LocalDB is not available.");
        return new TestDatabase();
    }

    // A finished sale, as the cashier screen produces it
    private static Transaction MakeSale(string cashier, string productId, int quantity)
    {
        var sale = new Transaction { CashierName = cashier };
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

    private static decimal Beans(TestDatabase db) =>
        (decimal)TestSupport.Scalar(db.Name,
            "SELECT Quantity FROM dbo.Ingredients WHERE IngredientId = 'I001'")!;

    [SkippableFact]
    public void A_new_database_starts_from_the_starting_data_whatever_ran_before()
    {
        // Leave the in-memory data well away from its starting values,
        // as other tests (or an earlier run) can
        TestSupport.ResetData();
        DataStore.Products.Add(new FoodProduct("P99", "Leftover", 1m, 1, "Food"));

        using TestDatabase db = Start();

        Assert.Equal(5m, Beans(db));
        Assert.Equal(10, (int)TestSupport.Scalar(db.Name, "SELECT COUNT(*) FROM dbo.Products")!);
        Assert.Equal(21, (int)TestSupport.Scalar(db.Name, "SELECT COUNT(*) FROM dbo.Ingredients")!);
    }

    [SkippableFact]
    public void A_sale_that_could_not_be_saved_is_kept_and_saved_by_the_next_save()
    {
        using TestDatabase db = Start();

        Transaction sale = MakeSale("Maria", "P01", 2);
        string working = Database.DatabaseName;

        // The database can not be reached
        Database.DatabaseName = "StarbucksPOS_does_not_exist";

        Assert.ThrowsAny<Exception>(() => Database.SaveSale(sale));
        Assert.Equal(1, Database.PendingSaleCount);

        // The database is back; any later save sends the waiting sale
        Database.DatabaseName = working;
        Database.SaveInventory();

        Assert.Equal(0, Database.PendingSaleCount);

        // Start the app again: the sale and its stock change are both there
        db.Restart();

        Transaction saved = Assert.Single(
            DataStore.Transactions, t => t.TransactionId == sale.TransactionId);

        Assert.Equal(sale.Total, saved.Total);
        Assert.Equal("Maria", saved.CashierName);
        Assert.True(DataStore.GetIngredient("I001")!.Quantity < 5m);
    }

    [SkippableFact]
    public void Stock_is_saved_as_a_change_so_another_terminal_is_not_overwritten()
    {
        using TestDatabase db = Start();

        decimal beans = DataStore.GetIngredient("I001")!.Quantity;   // 5 packs

        // Another terminal receives stock straight into the database
        TestSupport.Sql(db.Name,
            "UPDATE dbo.Ingredients SET Quantity = Quantity + 10 WHERE IngredientId = 'I001';" +
            "UPDATE dbo.Ingredients SET Quantity = Quantity + 7 WHERE IngredientId = 'I003';");

        // This terminal sells two lattes (0.036 packs of beans) from its stale copy
        Database.SaveSale(MakeSale("Maria", "P01", 2));

        Assert.Equal(beans + 10m - 0.036m, Beans(db));

        // An ingredient this terminal never touched keeps the other terminal's change
        decimal oat = (decimal)TestSupport.Scalar(db.Name,
            "SELECT Quantity FROM dbo.Ingredients WHERE IngredientId = 'I003'")!;

        Assert.Equal(5m + 7m, oat);
    }

    [SkippableFact]
    public void A_stock_change_that_failed_to_save_is_sent_again_next_time()
    {
        using TestDatabase db = Start();

        Ingredient beans = DataStore.GetIngredient("I001")!;
        decimal start = beans.Quantity;
        string working = Database.DatabaseName;

        beans.Quantity -= 1m;
        Database.DatabaseName = "StarbucksPOS_does_not_exist";
        Assert.ThrowsAny<Exception>(() => Database.SaveInventory());

        Database.DatabaseName = working;
        Database.SaveInventory();

        Assert.Equal(start - 1m, Beans(db));

        // and it is not applied twice
        Database.SaveInventory();
        Assert.Equal(start - 1m, Beans(db));
    }

    [SkippableFact]
    public void A_duplicate_order_number_gets_the_next_free_one()
    {
        using TestDatabase db = Start();

        Transaction sale = MakeSale("Maria", "P07", 1);
        string mine = sale.TransactionId;

        // Another terminal saved an order with the same number first
        TestSupport.Sql(db.Name,
            $"INSERT INTO dbo.Transactions VALUES ('{mine}', SYSDATETIME(), " +
            "SYSDATETIME(), 'Juan', 50, 100)");

        Database.SaveSale(sale);

        Assert.NotEqual(mine, sale.TransactionId);
        Assert.Equal(2, (int)TestSupport.Scalar(db.Name,
            "SELECT COUNT(*) FROM dbo.Transactions")!);

        // The stock records of the sale follow its new number
        Assert.Contains(DataStore.StockRecords,
            r => r.Type == "SALE" && r.Reference == sale.TransactionId);
        Assert.DoesNotContain(DataStore.StockRecords, r => r.Reference == mine);

        // The next order carries on after it
        Assert.NotEqual(sale.TransactionId, new Transaction().TransactionId);
    }

    [SkippableFact]
    public void A_new_product_whose_number_was_taken_gets_the_next_free_one()
    {
        using TestDatabase db = Start();

        var admin = new AdminService();
        string taken = admin.NextProductId();

        // Another terminal added a product with that number first
        TestSupport.Sql(db.Name,
            "INSERT INTO dbo.Products (ProductId, Name, Price, Stock, Category) " +
            $"VALUES ('{taken}', 'Other terminal item', 10, 1, 'Food')");

        Assert.True(admin.AddProduct("Blueberry Muffin", 105m, 8, false, 5, out string error), error);

        Product added = Assert.Single(DataStore.Products, p => p.Name == "Blueberry Muffin");
        Assert.NotEqual(taken, added.ProductId);
        Assert.Equal(2, (int)TestSupport.Scalar(db.Name,
            "SELECT COUNT(*) FROM dbo.Products WHERE ProductId NOT LIKE 'P0%' AND ProductId <> 'P10'")!);
    }

    [SkippableFact]
    public void An_older_database_is_brought_up_to_date_and_nothing_is_repeated()
    {
        using TestDatabase db = Start();

        // Pretend this database was made before the last two schema steps
        SqlConnection.ClearAllPools();
        TestSupport.Sql(db.Name, @"
DELETE FROM dbo.SchemaVersion WHERE Version > 3;
DECLARE @n NVARCHAR(200) = (
    SELECT dc.name FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Products') AND c.name = N'Unavailable');
EXEC('ALTER TABLE dbo.Products DROP CONSTRAINT ' + @n);
ALTER TABLE dbo.Products DROP COLUMN Unavailable;
UPDATE dbo.Ingredients SET LowStockThreshold = 0.5, ParLevel = 2.4 WHERE IngredientId = 'I004';");

        db.Restart();

        Assert.Equal(5, (int)TestSupport.Scalar(db.Name, "SELECT MAX(Version) FROM dbo.SchemaVersion")!);
        Assert.Equal(1, (int)TestSupport.Scalar(db.Name,
            "SELECT COUNT(*) FROM sys.columns " +
            "WHERE object_id = OBJECT_ID(N'dbo.Products') AND name = N'Unavailable'")!);

        // The data steps ran too: 0.5 / 2.4 became whole packs
        Ingredient espresso = DataStore.GetIngredient("I004")!;
        Assert.Equal(1m, espresso.LowStockThreshold);
        Assert.Equal(3m, espresso.ParLevel);

        // Starting again changes nothing
        db.Restart();
        Assert.Equal(5, (int)TestSupport.Scalar(db.Name, "SELECT COUNT(*) FROM dbo.SchemaVersion")!);
    }

    [SkippableFact]
    public void Saved_accounts_keep_only_a_hash_of_the_password()
    {
        using TestDatabase db = Start();

        var login = new LoginService();
        Assert.True(login.Register("juan", "secret1", "Juan Dela Cruz", out string error), error);

        string stored = (string)TestSupport.Scalar(db.Name,
            "SELECT PasswordHash FROM dbo.Users WHERE Username = 'juan'")!;

        Assert.DoesNotContain("secret1", stored);

        db.Restart();

        Assert.True(login.Login("JUAN", "secret1", out string role, out _));
        Assert.Equal("Cashier", role);
    }
}
