using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;
using POSInventorySystem.Data;
using POSInventorySystem.Models;

// The app keeps its data in static lists and one static database name, so
// the tests must not run at the same time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace POSInventorySystem.Tests;

internal static class TestSupport
{
    // Follows the same server setting as the app
    public static string MasterConnection =>
        $"Server={Database.ServerName};Database=master;Integrated Security=true;" +
        "TrustServerCertificate=true;Connect Timeout=8";

    // ------------------------------------------------------------------
    // The starting data, captured before any test can change it. Every test
    // begins from a copy of this, so no test depends on which test ran
    // before it.
    // ------------------------------------------------------------------

    private static readonly List<(string Id, string Name, decimal Quantity,
        decimal Low, string Unit, decimal PackSize, decimal Par)> seedIngredients = new();

    private static readonly List<(bool Beverage, string Id, string Name,
        decimal Price, int Stock, string Category, int Low)> seedProducts = new();

    private static readonly List<(string Username, string Password,
        string FullName, string Role)> seedUsers = new();

    [ModuleInitializer]
    internal static void CaptureSeed()
    {
        // Waiting sales are written to a file; tests must never touch the
        // real one in %LocalAppData%
        Database.PendingFolder = Path.Combine(
            Path.GetTempPath(), "StarbucksPOS_tests_" + Environment.ProcessId);

        foreach (Ingredient i in DataStore.Ingredients)
        {
            seedIngredients.Add((i.IngredientId, i.Name, i.Quantity,
                i.LowStockThreshold, i.Unit, i.PackSize, i.ParLevel));
        }

        foreach (Product p in DataStore.Products)
        {
            seedProducts.Add((p is BeverageProduct, p.ProductId, p.Name,
                p.Price, p.Stock, p.Category, p.LowStockLevel));
        }

        foreach (User u in DataStore.Users)
        {
            seedUsers.Add((u.Username, u.Password, u.FullName, u.Role));
        }
    }

    // Puts every in-memory list back to the starting data
    public static void RestoreSeed()
    {
        DataStore.Ingredients.Clear();

        foreach (var s in seedIngredients)
        {
            DataStore.Ingredients.Add(new Ingredient(
                s.Id, s.Name, s.Quantity, s.Low, s.Unit, s.PackSize, s.Par));
        }

        DataStore.Products.Clear();

        foreach (var s in seedProducts)
        {
            Product product = s.Beverage
                ? new BeverageProduct(s.Id, s.Name, s.Price, s.Stock, s.Category)
                : new FoodProduct(s.Id, s.Name, s.Price, s.Stock, s.Category);

            product.SetLowStockLevel(s.Low);
            DataStore.Products.Add(product);
        }

        DataStore.Users.Clear();

        foreach (var s in seedUsers)
        {
            DataStore.Users.Add(new User(s.Username, s.Password, s.FullName, s.Role));
        }

        DataStore.Transactions.Clear();
        DataStore.StockRecords.Clear();
        Transaction.SetNextNumber(1);

        ReloadRecipes();
    }

    // The starting data, then a well-stocked shop (100 of everything)
    public static void ResetData()
    {
        RestoreSeed();

        foreach (Ingredient i in DataStore.Ingredients)
        {
            i.Quantity = 100m;
        }

        foreach (Product p in DataStore.Products)
        {
            p.SetStock(50);
        }
    }

    // Recipes hold the ingredient objects, so they must be rebuilt whenever
    // the ingredient list is replaced (for example by Database.Initialize)
    public static void ReloadRecipes()
    {
        RecipeData.Recipes.Clear();
        RecipeData.Initialize();
    }

    public static BeverageCustomization Plain() =>
        new BeverageCustomization("Tall", "Hot", "None", 0, "None");

    // True when SQL Server LocalDB can be reached (it can not on every
    // machine, for example a hosted build agent)
    private static bool? localDb;

    public static bool LocalDbAvailable()
    {
        if (localDb == null)
        {
            try
            {
                using SqlConnection connection = new SqlConnection(MasterConnection);
                connection.Open();
                localDb = true;
            }
            catch (Exception)
            {
                localDb = false;
            }
        }

        return localDb.Value;
    }

    public static void Sql(string database, string sql)
    {
        string connectionString = database == "master"
            ? MasterConnection
            : MasterConnection.Replace("Database=master", $"Database={database}");

        using SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();
        new SqlCommand(sql, connection).ExecuteNonQuery();
    }

    public static object? Scalar(string database, string sql)
    {
        using SqlConnection connection = new SqlConnection(
            MasterConnection.Replace("Database=master", $"Database={database}"));
        connection.Open();
        return new SqlCommand(sql, connection).ExecuteScalar();
    }
}


// A throw-away database for one test. It has its own name, so the real
// StarbucksPOS database is never touched, and it always starts from the
// app's starting data, whatever ran before.
public sealed class TestDatabase : IDisposable
{
    public string Name { get; } =
        "StarbucksPOS_Test_" + Guid.NewGuid().ToString("N")[..10];

    private readonly string previousName;

    public TestDatabase()
    {
        previousName = Database.DatabaseName;
        Database.DatabaseName = Name;

        TestSupport.RestoreSeed();
        Database.Initialize();
    }

    // Loads the database again, like starting the app a second time
    public void Restart()
    {
        Database.Initialize();
    }

    public void Dispose()
    {
        // The waiting-sales file belongs to this throw-away database
        try
        {
            foreach (string file in Directory.GetFiles(
                         Database.PendingFolder, $"pending-sales-{Name}.json*"))
            {
                File.Delete(file);
            }
        }
        catch (Exception)
        {
            // best effort
        }

        Database.DatabaseName = previousName;
        SqlConnection.ClearAllPools();

        try
        {
            TestSupport.Sql("master",
                $"ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{Name}];");
        }
        catch (Exception)
        {
            // best effort
        }

        // leave the app's data as it started for whatever runs next
        TestSupport.RestoreSeed();
    }
}
