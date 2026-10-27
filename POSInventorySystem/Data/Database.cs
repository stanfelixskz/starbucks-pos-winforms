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

        Schema.Apply(connection);
        BackfillIngredientUnits(connection);
        SeedIfEmpty(connection);
        LoadAll(connection);
        SeedRecipesIfEmpty(connection);
        LoadRecipes(connection);
    }

    private static void CreateDatabase()
    {
        // The first connection also starts LocalDB, which can take a moment
        using SqlConnection connection = Open("master");

        Run(connection, null,
            $"IF DB_ID(N'{DatabaseName}') IS NULL " +
            $"CREATE DATABASE [{DatabaseName}];");
    }

    // Older ingredients get their unit, pack size and par level from the
    // starting data (only rows that don't have a unit yet)
    private static void BackfillIngredientUnits(SqlConnection connection)
    {
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

    // First run (or an older database): store the starting drink recipes,
    // built from the ingredients that were just loaded
    private static void SeedRecipesIfEmpty(SqlConnection connection)
    {
        using (SqlCommand count = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.RecipeIngredients", connection))
        {
            if ((int)count.ExecuteScalar()! > 0)
            {
                return;
            }
        }

        RecipeData.Recipes.Clear();
        RecipeData.Initialize();

        using SqlTransaction transaction = connection.BeginTransaction();

        foreach (Recipe recipe in RecipeData.Recipes)
        {
            if (DataStore.GetProduct(recipe.Product.ProductId) != null)
            {
                InsertRecipeRows(
                    connection, transaction,
                    recipe.Product.ProductId,
                    RecipeRules.ToRows(recipe));
            }
        }

        transaction.Commit();
    }

    // The recipes the app uses always come from the database
    private static void LoadRecipes(SqlConnection connection)
    {
        RecipeData.Recipes.Clear();

        Dictionary<string, List<RecipeRow>> rows = new();

        using (SqlDataReader r = Query(connection,
            "SELECT ProductId, IngredientId, Size, Amount " +
            "FROM dbo.RecipeIngredients ORDER BY ProductId, IngredientId, Size"))
        {
            while (r.Read())
            {
                string productId = r.GetString(0);

                if (!rows.ContainsKey(productId))
                {
                    rows[productId] = new List<RecipeRow>();
                }

                rows[productId].Add(new RecipeRow(
                    r.GetString(1), r.GetString(2), r.GetDecimal(3)));
            }
        }

        foreach (Product product in DataStore.Products)
        {
            if (rows.TryGetValue(product.ProductId, out List<RecipeRow>? list))
            {
                RecipeData.Recipes.Add(RecipeRules.Build(product, list));
            }
        }
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
        RememberSavedStock();
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

    // Sales that finished at the till but are not in the database yet
    // (for example the database could not be reached). They are retried on
    // the next save and when the cashier window closes, and never dropped.
    private static readonly List<Transaction> pendingSales = new();

    public static int PendingSaleCount => pendingSales.Count;

    // The stock last saved for each item. Only items that differ are
    // written, and as an addition, so a change made by another terminal is
    // not overwritten.
    private static readonly Dictionary<string, decimal> savedIngredientStock = new();
    private static readonly Dictionary<string, int> savedProductStock = new();

    private static void RememberSavedStock()
    {
        savedIngredientStock.Clear();

        foreach (Ingredient i in DataStore.Ingredients)
        {
            savedIngredientStock[i.IngredientId] = i.Quantity;
        }

        savedProductStock.Clear();

        foreach (Product p in DataStore.Products)
        {
            savedProductStock[p.ProductId] = p.Stock;
        }
    }

    // Saves a completed sale together with the stock it used. If the
    // database can not be reached this throws, but the sale stays queued
    // and the next successful save writes it.
    public static void SaveSale(Transaction sale)
    {
        if (!pendingSales.Contains(sale))
        {
            pendingSales.Add(sale);
        }

        SaveInventory();
    }

    // Saves every queued sale. A sale leaves the queue only after its
    // database transaction has committed.
    public static void FlushPendingSales()
    {
        foreach (Transaction sale in pendingSales.ToList())
        {
            InsertSale(sale);
            pendingSales.Remove(sale);
        }
    }

    // Saves queued sales, then the stock changes and any new stock records.
    // Used after a sale or a restock. If a save fails, the same changes are
    // sent again by the next one.
    public static void SaveInventory()
    {
        FlushPendingSales();

        using SqlConnection connection = Open(DatabaseName);
        using SqlTransaction transaction = connection.BeginTransaction();

        int records = SyncInventory(connection, transaction);

        transaction.Commit();
        savedStockRecords = records;
        RememberSavedStock();
    }

    // Saves one sale. If another terminal already used its order number,
    // the sale is given the next free number first.
    private static void InsertSale(Transaction sale)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                InsertSaleOnce(sale);
                return;
            }
            catch (SqlException ex) when (IsDuplicateKey(ex))
            {
                using SqlConnection connection = Open(DatabaseName);

                // The very same sale is already there (the answer to an
                // earlier attempt was lost): nothing more to do
                if (SaleAlreadySaved(connection, sale))
                {
                    return;
                }

                // Someone else owns this number: take the next free one
                string oldId = sale.TransactionId;
                int next = NextTransactionNumber(connection);

                sale.AssignId($"T{next:D2}");
                Transaction.SetNextNumber(next + 1);

                // Its stock records are not saved yet; keep them pointing
                // at the new number
                foreach (StockRecord record in DataStore.StockRecords
                             .Skip(savedStockRecords))
                {
                    if (record.Type == "SALE" && record.Reference == oldId)
                    {
                        record.SetReference(sale.TransactionId);
                    }
                }
            }
        }

        throw new InvalidOperationException(
            "Could not find a free order number for the sale.");
    }

    private static void InsertSaleOnce(Transaction sale)
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

        transaction.Commit();
    }

    private static bool SaleAlreadySaved(SqlConnection connection, Transaction sale)
    {
        using SqlCommand command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Transactions " +
            "WHERE TransactionId = @id AND OrderedAt = @ordered " +
            "AND CashierName = @cashier AND Total = @total",
            connection);

        command.Parameters.AddWithValue("@id", sale.TransactionId);
        command.Parameters.AddWithValue("@ordered", sale.Date);
        command.Parameters.AddWithValue("@cashier", sale.CashierName);
        command.Parameters.AddWithValue("@total", sale.Total);

        return (int)command.ExecuteScalar()! > 0;
    }

    // The next free order number according to the database
    private static int NextTransactionNumber(SqlConnection connection)
    {
        using SqlCommand command = new SqlCommand(
            "SELECT ISNULL(MAX(CAST(SUBSTRING(TransactionId, 2, 9) AS INT)), 0) + 1 " +
            "FROM dbo.Transactions " +
            "WHERE ISNUMERIC(SUBSTRING(TransactionId, 2, 9)) = 1",
            connection);

        return (int)command.ExecuteScalar()!;
    }

    // The next free product number according to the database
    public static string NextProductIdFromDatabase()
    {
        using SqlConnection connection = Open(DatabaseName);

        using SqlCommand command = new SqlCommand(
            "SELECT ISNULL(MAX(CAST(SUBSTRING(ProductId, 2, 9) AS INT)), 0) + 1 " +
            "FROM dbo.Products WHERE ISNUMERIC(SUBSTRING(ProductId, 2, 9)) = 1",
            connection);

        return $"P{(int)command.ExecuteScalar()!:D2}";
    }

    // The next free ingredient number according to the database
    public static string NextIngredientIdFromDatabase()
    {
        using SqlConnection connection = Open(DatabaseName);

        using SqlCommand command = new SqlCommand(
            "SELECT ISNULL(MAX(CAST(SUBSTRING(IngredientId, 2, 9) AS INT)), 0) + 1 " +
            "FROM dbo.Ingredients WHERE ISNUMERIC(SUBSTRING(IngredientId, 2, 9)) = 1",
            connection);

        return $"I{(int)command.ExecuteScalar()!:D3}";
    }

    // 2627 and 2601: a row with that key already exists
    public static bool IsDuplicateKey(Exception ex) =>
        ex is SqlException { Number: 2627 or 2601 };

    // Returns how many stock records exist once this save commits.
    // Only items whose stock changed since the last save are written, and
    // each is written as an addition to what the database holds.
    private static int SyncInventory(
        SqlConnection connection,
        SqlTransaction transaction)
    {
        foreach (Ingredient i in DataStore.Ingredients)
        {
            decimal change = i.Quantity -
                savedIngredientStock.GetValueOrDefault(
                    i.IngredientId, i.Quantity);

            if (change == 0)
            {
                continue;
            }

            Run(connection, transaction,
                "UPDATE dbo.Ingredients SET Quantity = Quantity + @change " +
                "WHERE IngredientId = @id",
                ("@change", change),
                ("@id", i.IngredientId));
        }

        foreach (Product p in DataStore.Products)
        {
            int change = p.Stock -
                savedProductStock.GetValueOrDefault(p.ProductId, p.Stock);

            if (change == 0)
            {
                continue;
            }

            Run(connection, transaction,
                "UPDATE dbo.Products SET Stock = Stock + @change " +
                "WHERE ProductId = @id",
                ("@change", change),
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

    // Saves a new product. A drink's recipe is saved in the same database
    // transaction, so a drink never exists without its recipe.
    public static void AddProduct(
        Product product,
        IReadOnlyList<RecipeRow>? recipe = null)
    {
        using SqlConnection connection = Open(DatabaseName);
        using SqlTransaction transaction = connection.BeginTransaction();

        InsertProduct(connection, transaction, product);

        if (recipe != null)
        {
            InsertRecipeRows(connection, transaction, product.ProductId, recipe);
        }

        transaction.Commit();
    }

    // Replaces a drink's whole recipe
    public static void SaveRecipe(string productId, IReadOnlyList<RecipeRow> recipe)
    {
        using SqlConnection connection = Open(DatabaseName);
        using SqlTransaction transaction = connection.BeginTransaction();

        Run(connection, transaction,
            "DELETE FROM dbo.RecipeIngredients WHERE ProductId = @id",
            ("@id", productId));

        InsertRecipeRows(connection, transaction, productId, recipe);

        transaction.Commit();
    }

    private static void InsertRecipeRows(
        SqlConnection connection,
        SqlTransaction transaction,
        string productId,
        IEnumerable<RecipeRow> rows)
    {
        foreach (RecipeRow row in rows)
        {
            Run(connection, transaction,
                "INSERT INTO dbo.RecipeIngredients " +
                "(ProductId, IngredientId, Size, Amount) " +
                "VALUES (@product, @ingredient, @size, @amount)",
                ("@product", productId),
                ("@ingredient", row.IngredientId),
                ("@size", row.Size),
                ("@amount", row.Amount));
        }
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

        savedProductStock[p.ProductId] = p.Stock;
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

        savedIngredientStock[i.IngredientId] = i.Quantity;
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

    internal static void Run(
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
