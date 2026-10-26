using Microsoft.Data.SqlClient;
using POSInventorySystem.Models;
using POSInventorySystem.Services;

namespace POSInventorySystem.Data;

// SQL Server LocalDB storage. The DataStore lists stay the working copy;
// this class creates the database, loads it at startup and saves changes.
public static class Database
{
    // The database name can be changed (the tests use their own copy)
    public static string DatabaseName { get; set; } = "StarbucksPOS";

    private const string Server = @"(localdb)\MSSQLLocalDB";

    private static string Connect(string database) =>
        $"Server={Server};Database={database};" +
        "Integrated Security=true;TrustServerCertificate=true;" +
        "Connect Timeout=60";

    private static SqlConnection Open(string database)
    {
        SqlConnection connection = new SqlConnection(Connect(database));
        connection.Open();
        return connection;
    }

    // How many stock records are already saved (the list only grows)
    private static int savedStockRecords;

    // ==========================================
    // STARTUP
    // ==========================================

    public static void Initialize()
    {
        CreateDatabase();

        using SqlConnection connection = Open(DatabaseName);

        CreateTables(connection);
        SeedIfEmpty(connection);
        LoadAll(connection);
    }

    private static void CreateDatabase()
    {
        // The first connection also starts LocalDB, which can take a moment
        using SqlConnection connection = Open("master");

        Run(connection, null,
            $"IF DB_ID(N'{DatabaseName}') IS NULL " +
            $"CREATE DATABASE [{DatabaseName}];");
    }

    private static void CreateTables(SqlConnection connection)
    {
        Run(connection, null, @"
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    Username      NVARCHAR(50)  NOT NULL PRIMARY KEY,
    PasswordHash  NVARCHAR(200) NOT NULL,
    FullName      NVARCHAR(100) NOT NULL,
    Role          NVARCHAR(20)  NOT NULL
);

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products (
    ProductId  NVARCHAR(10)  NOT NULL PRIMARY KEY,
    Name       NVARCHAR(100) NOT NULL,
    Price      DECIMAL(10,2) NOT NULL,
    Stock      INT           NOT NULL,
    Category   NVARCHAR(20)  NOT NULL,
    LowStockLevel INT        NOT NULL DEFAULT 5,
    Unavailable BIT          NOT NULL DEFAULT 0
);

IF OBJECT_ID(N'dbo.Ingredients', N'U') IS NULL
CREATE TABLE dbo.Ingredients (
    IngredientId       NVARCHAR(10)  NOT NULL PRIMARY KEY,
    Name               NVARCHAR(100) NOT NULL,
    Quantity           DECIMAL(12,4) NOT NULL,
    LowStockThreshold  DECIMAL(12,4) NOT NULL,
    Unit               NVARCHAR(10)  NOT NULL DEFAULT N'',
    PackSize           DECIMAL(12,4) NOT NULL DEFAULT 1,
    ParLevel           DECIMAL(12,4) NOT NULL DEFAULT 0
);

IF OBJECT_ID(N'dbo.Transactions', N'U') IS NULL
CREATE TABLE dbo.Transactions (
    TransactionId  NVARCHAR(10)  NOT NULL PRIMARY KEY,
    OrderedAt      DATETIME2     NOT NULL,
    CompletedAt    DATETIME2     NULL,
    CashierName    NVARCHAR(100) NOT NULL,
    Total          DECIMAL(10,2) NOT NULL,
    CashReceived   DECIMAL(10,2) NULL
);

IF OBJECT_ID(N'dbo.TransactionItems', N'U') IS NULL
CREATE TABLE dbo.TransactionItems (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    TransactionId  NVARCHAR(10)  NOT NULL
        REFERENCES dbo.Transactions(TransactionId),
    ProductId      NVARCHAR(10)  NOT NULL
        REFERENCES dbo.Products(ProductId),
    Quantity       INT           NOT NULL,
    UnitPrice      DECIMAL(10,2) NOT NULL,
    Size           NVARCHAR(20)  NULL,
    Serving        NVARCHAR(20)  NULL,
    Milk           NVARCHAR(30)  NULL,
    ExtraShots     INT           NULL,
    Syrup          NVARCHAR(30)  NULL
);

IF OBJECT_ID(N'dbo.StockRecords', N'U') IS NULL
CREATE TABLE dbo.StockRecords (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    [Timestamp]  DATETIME2      NOT NULL,
    ItemId       NVARCHAR(10)   NOT NULL,
    ItemName     NVARCHAR(100)  NOT NULL,
    Type         NVARCHAR(20)   NOT NULL,
    Unit         NVARCHAR(10)   NOT NULL,
    [Count]      DECIMAL(12,4)  NOT NULL,
    StockChange  DECIMAL(12,4)  NOT NULL,
    Reference    NVARCHAR(50)   NOT NULL,
    Note         NVARCHAR(200)  NOT NULL DEFAULT N''
);

-- Existing databases from before the Note column
IF COL_LENGTH(N'dbo.StockRecords', N'Note') IS NULL
ALTER TABLE dbo.StockRecords ADD Note NVARCHAR(200) NOT NULL DEFAULT N'';

-- Databases from before units, pack sizes and low-stock levels
IF COL_LENGTH(N'dbo.Ingredients', N'Unit') IS NULL
ALTER TABLE dbo.Ingredients ADD
    Unit NVARCHAR(10) NOT NULL DEFAULT N'',
    PackSize DECIMAL(12,4) NOT NULL DEFAULT 1,
    ParLevel DECIMAL(12,4) NOT NULL DEFAULT 0;

IF COL_LENGTH(N'dbo.Products', N'LowStockLevel') IS NULL
ALTER TABLE dbo.Products ADD LowStockLevel INT NOT NULL DEFAULT 5;

IF COL_LENGTH(N'dbo.Products', N'Unavailable') IS NULL
ALTER TABLE dbo.Products ADD Unavailable BIT NOT NULL DEFAULT 0;");

        // Reorder and par levels are whole packs (par can not sit below the
        // reorder point)
        Run(connection, null,
            "UPDATE dbo.Ingredients SET " +
            "LowStockThreshold = CEILING(LowStockThreshold), " +
            "ParLevel = CEILING(ParLevel) " +
            "WHERE LowStockThreshold <> CEILING(LowStockThreshold) " +
            "OR ParLevel <> CEILING(ParLevel)");

        Run(connection, null,
            "UPDATE dbo.Ingredients SET ParLevel = LowStockThreshold " +
            "WHERE ParLevel < LowStockThreshold");

        // Older ingredients get their unit, pack size and par level from the
        // starting data (only rows that don't have a unit yet)
        foreach (Ingredient i in DataStore.Ingredients)
        {
            Run(connection, null,
                "UPDATE dbo.Ingredients SET Unit = @unit, PackSize = @size, " +
                "ParLevel = @par WHERE IngredientId = @id AND Unit = N''",
                ("@unit", i.Unit),
                ("@size", i.PackSize),
                ("@par", i.ParLevel),
                ("@id", i.IngredientId));
        }
    }

    // First run only: store the starting products, ingredients and users
    private static void SeedIfEmpty(SqlConnection connection)
    {
        using SqlTransaction transaction = connection.BeginTransaction();

        if (Count(connection, transaction, "Products") == 0)
        {
            foreach (Product p in DataStore.Products)
            {
                InsertProduct(connection, transaction, p);
            }
        }

        if (Count(connection, transaction, "Ingredients") == 0)
        {
            foreach (Ingredient i in DataStore.Ingredients)
            {
                InsertIngredient(connection, transaction, i);
            }
        }

        if (Count(connection, transaction, "Users") == 0)
        {
            foreach (User u in DataStore.Users)
            {
                // The starting accounts hold plain passwords in code;
                // only the hash is ever stored.
                Run(connection, transaction,
                    "INSERT INTO dbo.Users VALUES (@user, @hash, @name, @role)",
                    ("@user", u.Username),
                    ("@hash", PasswordHasher.Hash(u.Password)),
                    ("@name", u.FullName),
                    ("@role", u.Role));
            }
        }

        transaction.Commit();
    }

    // Replaces the in-memory lists with what is saved
    private static void LoadAll(SqlConnection connection)
    {
        DataStore.Ingredients.Clear();
        DataStore.Products.Clear();
        DataStore.Users.Clear();
        DataStore.Transactions.Clear();
        DataStore.StockRecords.Clear();

        using (SqlDataReader r = Query(connection,
            "SELECT IngredientId, Name, Quantity, LowStockThreshold, " +
            "Unit, PackSize, ParLevel " +
            "FROM dbo.Ingredients ORDER BY LEN(IngredientId), IngredientId"))
        {
            while (r.Read())
            {
                DataStore.Ingredients.Add(new Ingredient(
                    r.GetString(0), r.GetString(1),
                    r.GetDecimal(2), r.GetDecimal(3),
                    r.GetString(4), r.GetDecimal(5), r.GetDecimal(6)));
            }
        }

        using (SqlDataReader r = Query(connection,
            "SELECT ProductId, Name, Price, Stock, Category, LowStockLevel, " +
            "Unavailable " +
            "FROM dbo.Products ORDER BY LEN(ProductId), ProductId"))
        {
            while (r.Read())
            {
                string id = r.GetString(0);
                string name = r.GetString(1);
                decimal price = r.GetDecimal(2);
                int stock = r.GetInt32(3);
                string category = r.GetString(4);

                Product product = category == "Beverage"
                    ? new BeverageProduct(id, name, price, stock, category)
                    : new FoodProduct(id, name, price, stock, category);

                product.SetLowStockLevel(r.GetInt32(5));
                product.SetUnavailable(r.GetBoolean(6));

                DataStore.Products.Add(product);
            }
        }

        using (SqlDataReader r = Query(connection,
            "SELECT Username, PasswordHash, FullName, Role FROM dbo.Users"))
        {
            while (r.Read())
            {
                DataStore.Users.Add(new User(
                    r.GetString(0), r.GetString(1),
                    r.GetString(2), r.GetString(3)));
            }
        }

        LoadTransactions(connection);

        using (SqlDataReader r = Query(connection,
            "SELECT [Timestamp], ItemId, ItemName, Type, Unit, [Count], " +
            "StockChange, Reference, Note FROM dbo.StockRecords ORDER BY Id"))
        {
            while (r.Read())
            {
                DataStore.StockRecords.Add(new StockRecord(
                    r.GetDateTime(0), r.GetString(1), r.GetString(2),
                    r.GetString(3), r.GetString(4), r.GetDecimal(5),
                    r.GetDecimal(6), r.GetString(7), r.GetString(8)));
            }
        }

        savedStockRecords = DataStore.StockRecords.Count;
    }

    private static void LoadTransactions(SqlConnection connection)
    {
        // Items grouped by order
        Dictionary<string, List<CartItem>> items = new();

        using (SqlDataReader r = Query(connection,
            "SELECT TransactionId, ProductId, Quantity, UnitPrice, " +
            "Size, Serving, Milk, ExtraShots, Syrup " +
            "FROM dbo.TransactionItems ORDER BY Id"))
        {
            while (r.Read())
            {
                Product? product = DataStore.GetProduct(r.GetString(1));

                if (product == null)
                {
                    continue;
                }

                BeverageCustomization? customization = r.IsDBNull(4)
                    ? null
                    : new BeverageCustomization(
                        r.GetString(4), r.GetString(5), r.GetString(6),
                        r.GetInt32(7), r.GetString(8));

                CartItem item = new CartItem(
                    product, r.GetInt32(2), customization);

                item.FreezePrice(r.GetDecimal(3));

                string id = r.GetString(0);

                if (!items.ContainsKey(id))
                {
                    items[id] = new List<CartItem>();
                }

                items[id].Add(item);
            }
        }

        int highest = 0;

        using (SqlDataReader r = Query(connection,
            "SELECT TransactionId, OrderedAt, CompletedAt, CashierName, " +
            "CashReceived FROM dbo.Transactions ORDER BY OrderedAt"))
        {
            while (r.Read())
            {
                string id = r.GetString(0);

                DataStore.Transactions.Add(Transaction.Restore(
                    id,
                    r.GetDateTime(1),
                    r.IsDBNull(2) ? null : r.GetDateTime(2),
                    r.GetString(3),
                    items.GetValueOrDefault(id) ?? new List<CartItem>(),
                    r.IsDBNull(4) ? null : r.GetDecimal(4)));

                if (id.Length > 1 &&
                    int.TryParse(id.Substring(1), out int number))
                {
                    highest = Math.Max(highest, number);
                }
            }
        }

        // The next new order continues the numbering
        Transaction.SetNextNumber(highest + 1);
    }

    // ==========================================
    // SAVING
    // ==========================================

    // Saves a completed sale together with the stock it used.
    public static void SaveSale(Transaction sale)
    {
        using SqlConnection connection = Open(DatabaseName);
        using SqlTransaction transaction = connection.BeginTransaction();

        decimal? cash = (sale.Payment as CashPayment)?.CashReceived;

        Run(connection, transaction,
            "INSERT INTO dbo.Transactions VALUES " +
            "(@id, @ordered, @completed, @cashier, @total, @cash)",
            ("@id", sale.TransactionId),
            ("@ordered", sale.Date),
            ("@completed", sale.CompletedAt),
            ("@cashier", sale.CashierName),
            ("@total", sale.Total),
            ("@cash", cash));

        foreach (CartItem item in sale.Items)
        {
            BeverageCustomization? c = item.Customization;

            Run(connection, transaction,
                "INSERT INTO dbo.TransactionItems " +
                "(TransactionId, ProductId, Quantity, UnitPrice, " +
                "Size, Serving, Milk, ExtraShots, Syrup) VALUES " +
                "(@id, @product, @qty, @price, @size, @serving, " +
                "@milk, @shots, @syrup)",
                ("@id", sale.TransactionId),
                ("@product", item.Product.ProductId),
                ("@qty", item.Quantity),
                ("@price", item.UnitPrice),
                ("@size", c?.Size),
                ("@serving", c?.Serving),
                ("@milk", c?.Milk),
                ("@shots", c?.ExtraShots),
                ("@syrup", c?.Syrup));
        }

        int records = SyncInventory(connection, transaction);

        transaction.Commit();
        savedStockRecords = records;
    }

    // Saves current ingredient / product stock and any new stock records.
    // Used after a sale or a restock. Writes every row, so a failed
    // save is repaired by the next successful one.
    public static void SaveInventory()
    {
        using SqlConnection connection = Open(DatabaseName);
        using SqlTransaction transaction = connection.BeginTransaction();

        int records = SyncInventory(connection, transaction);

        transaction.Commit();
        savedStockRecords = records;
    }

    // Returns how many stock records exist once this save commits
    private static int SyncInventory(
        SqlConnection connection,
        SqlTransaction transaction)
    {
        foreach (Ingredient i in DataStore.Ingredients)
        {
            Run(connection, transaction,
                "UPDATE dbo.Ingredients SET Quantity = @qty " +
                "WHERE IngredientId = @id",
                ("@qty", i.Quantity),
                ("@id", i.IngredientId));
        }

        foreach (Product p in DataStore.Products)
        {
            Run(connection, transaction,
                "UPDATE dbo.Products SET Stock = @stock " +
                "WHERE ProductId = @id",
                ("@stock", p.Stock),
                ("@id", p.ProductId));
        }

        int total = DataStore.StockRecords.Count;

        foreach (StockRecord r in DataStore.StockRecords
                     .Skip(savedStockRecords))
        {
            Run(connection, transaction,
                "INSERT INTO dbo.StockRecords " +
                "([Timestamp], ItemId, ItemName, Type, Unit, [Count], " +
                "StockChange, Reference, Note) VALUES " +
                "(@time, @id, @name, @type, @unit, @count, @change, @ref, @note)",
                ("@time", r.Timestamp),
                ("@id", r.ItemId),
                ("@name", r.ItemName),
                ("@type", r.Type),
                ("@unit", r.Unit),
                ("@count", r.Count),
                ("@change", r.StockChange),
                ("@ref", r.Reference),
                ("@note", r.Note));
        }

        return total;
    }

    public static void AddProduct(Product product)
    {
        using SqlConnection connection = Open(DatabaseName);
        InsertProduct(connection, null, product);
    }

    public static void UpdateProduct(Product product)
    {
        using SqlConnection connection = Open(DatabaseName);

        Run(connection, null,
            "UPDATE dbo.Products SET Name = @name, Price = @price, " +
            "LowStockLevel = @low WHERE ProductId = @id",
            ("@name", product.Name),
            ("@price", product.Price),
            ("@low", product.LowStockLevel),
            ("@id", product.ProductId));
    }

    public static void SetProductUnavailable(Product product)
    {
        using SqlConnection connection = Open(DatabaseName);

        Run(connection, null,
            "UPDATE dbo.Products SET Unavailable = @off WHERE ProductId = @id",
            ("@off", product.Unavailable),
            ("@id", product.ProductId));
    }

    public static void AddIngredient(Ingredient ingredient)
    {
        using SqlConnection connection = Open(DatabaseName);
        InsertIngredient(connection, null, ingredient);
    }

    public static void UpdateIngredient(Ingredient i)
    {
        using SqlConnection connection = Open(DatabaseName);

        Run(connection, null,
            "UPDATE dbo.Ingredients SET Name = @name, Unit = @unit, " +
            "PackSize = @size, LowStockThreshold = @low, ParLevel = @par " +
            "WHERE IngredientId = @id",
            ("@name", i.Name),
            ("@unit", i.Unit),
            ("@size", i.PackSize),
            ("@low", i.LowStockThreshold),
            ("@par", i.ParLevel),
            ("@id", i.IngredientId));
    }

    public static void AddUser(User user)
    {
        using SqlConnection connection = Open(DatabaseName);

        Run(connection, null,
            "INSERT INTO dbo.Users VALUES (@user, @hash, @name, @role)",
            ("@user", user.Username),
            ("@hash", user.Password),
            ("@name", user.FullName),
            ("@role", user.Role));
    }

    private static void InsertProduct(
        SqlConnection connection,
        SqlTransaction? transaction,
        Product p)
    {
        Run(connection, transaction,
            "INSERT INTO dbo.Products " +
            "(ProductId, Name, Price, Stock, Category, LowStockLevel) VALUES " +
            "(@id, @name, @price, @stock, @cat, @low)",
            ("@id", p.ProductId),
            ("@name", p.Name),
            ("@price", p.Price),
            ("@stock", p.Stock),
            ("@cat", p.Category),
            ("@low", p.LowStockLevel));
    }

    private static void InsertIngredient(
        SqlConnection connection,
        SqlTransaction? transaction,
        Ingredient i)
    {
        Run(connection, transaction,
            "INSERT INTO dbo.Ingredients " +
            "(IngredientId, Name, Quantity, LowStockThreshold, Unit, " +
            "PackSize, ParLevel) VALUES " +
            "(@id, @name, @qty, @low, @unit, @size, @par)",
            ("@id", i.IngredientId),
            ("@name", i.Name),
            ("@qty", i.Quantity),
            ("@low", i.LowStockThreshold),
            ("@unit", i.Unit),
            ("@size", i.PackSize),
            ("@par", i.ParLevel));
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private static int Count(
        SqlConnection connection,
        SqlTransaction transaction,
        string table)
    {
        using SqlCommand command = new SqlCommand(
            $"SELECT COUNT(*) FROM dbo.{table}", connection, transaction);

        return (int)command.ExecuteScalar()!;
    }

    private static SqlDataReader Query(SqlConnection connection, string sql)
    {
        SqlCommand command = new SqlCommand(sql, connection);
        return command.ExecuteReader();
    }

    private static void Run(
        SqlConnection connection,
        SqlTransaction? transaction,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        using SqlCommand command =
            new SqlCommand(sql, connection, transaction);

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }
}
