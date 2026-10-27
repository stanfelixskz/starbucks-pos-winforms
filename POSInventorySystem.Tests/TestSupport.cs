using Microsoft.Data.SqlClient;
using POSInventorySystem.Data;
using POSInventorySystem.Models;

// The app keeps its data in static lists and one static database name, so
// the tests must not run at the same time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace POSInventorySystem.Tests;

internal static class TestSupport
{
    public const string MasterConnection =
        @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;" +
        "TrustServerCertificate=true;Connect Timeout=8";

    // Puts the in-memory data into a known, well-stocked state
    public static void ResetData()
    {
        foreach (Ingredient i in DataStore.Ingredients)
        {
            i.Quantity = 100m;
        }

        foreach (Product p in DataStore.Products)
        {
            p.SetStock(50);
            p.SetUnavailable(false);
        }

        DataStore.Transactions.Clear();
        DataStore.StockRecords.Clear();

        ReloadRecipes();
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


// A throw-away database for one test class. It has its own name, so the
// real StarbucksPOS database is never touched.
public sealed class TestDatabase : IDisposable
{
    public string Name { get; } =
        "StarbucksPOS_Test_" + Guid.NewGuid().ToString("N")[..10];

    private readonly string previousName;

    public TestDatabase()
    {
        previousName = Database.DatabaseName;
        Database.DatabaseName = Name;

        Database.Initialize();
        TestSupport.ReloadRecipes();
    }

    // Loads the database again, like starting the app a second time
    public void Restart()
    {
        Database.Initialize();
        TestSupport.ReloadRecipes();
    }

    public void Dispose()
    {
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
    }
}
