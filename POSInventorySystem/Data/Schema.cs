using Microsoft.Data.SqlClient;

namespace POSInventorySystem.Data;

// The database layout as numbered steps. Each step runs once and is recorded
// in dbo.SchemaVersion, so a schema change is one more entry here instead of
// another inline check. Every step is safe to run on a database that already
// has the change, which is how databases created by older versions of the
// app are brought up to date.
internal static class Schema
{
    private static readonly (int Version, string Description, string Sql)[] Steps =
    {
        (1, "Base tables", @"
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
    Category   NVARCHAR(20)  NOT NULL
);

IF OBJECT_ID(N'dbo.Ingredients', N'U') IS NULL
CREATE TABLE dbo.Ingredients (
    IngredientId       NVARCHAR(10)  NOT NULL PRIMARY KEY,
    Name               NVARCHAR(100) NOT NULL,
    Quantity           DECIMAL(12,4) NOT NULL,
    LowStockThreshold  DECIMAL(12,4) NOT NULL
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
    Reference    NVARCHAR(50)   NOT NULL
);"),

        (2, "Note on stock records", @"
IF COL_LENGTH(N'dbo.StockRecords', N'Note') IS NULL
ALTER TABLE dbo.StockRecords ADD Note NVARCHAR(200) NOT NULL DEFAULT N'';"),

        (3, "Ingredient units, pack sizes and par levels; product low-stock level", @"
IF COL_LENGTH(N'dbo.Ingredients', N'Unit') IS NULL
ALTER TABLE dbo.Ingredients ADD
    Unit NVARCHAR(10) NOT NULL DEFAULT N'',
    PackSize DECIMAL(12,4) NOT NULL DEFAULT 1,
    ParLevel DECIMAL(12,4) NOT NULL DEFAULT 0;

IF COL_LENGTH(N'dbo.Products', N'LowStockLevel') IS NULL
ALTER TABLE dbo.Products ADD LowStockLevel INT NOT NULL DEFAULT 5;"),

        (4, "Products can be switched off by hand", @"
IF COL_LENGTH(N'dbo.Products', N'Unavailable') IS NULL
ALTER TABLE dbo.Products ADD Unavailable BIT NOT NULL DEFAULT 0;"),

        (5, "Whole-number reorder and par levels", @"
UPDATE dbo.Ingredients SET
    LowStockThreshold = CEILING(LowStockThreshold),
    ParLevel = CEILING(ParLevel)
WHERE LowStockThreshold <> CEILING(LowStockThreshold)
   OR ParLevel <> CEILING(ParLevel);

UPDATE dbo.Ingredients SET ParLevel = LowStockThreshold
WHERE ParLevel < LowStockThreshold;"),

        (6, "Drink recipes, with an amount for each size", @"
IF OBJECT_ID(N'dbo.RecipeIngredients', N'U') IS NULL
CREATE TABLE dbo.RecipeIngredients (
    ProductId     NVARCHAR(10)  NOT NULL
        REFERENCES dbo.Products(ProductId),
    IngredientId  NVARCHAR(10)  NOT NULL
        REFERENCES dbo.Ingredients(IngredientId),
    Size          NVARCHAR(10)  NOT NULL,
    Amount        DECIMAL(12,4) NOT NULL,
    PRIMARY KEY (ProductId, IngredientId, Size)
);"),
    };

    // Runs every step that has not been recorded yet, each in its own
    // database transaction.
    public static void Apply(SqlConnection connection)
    {
        Database.Run(connection, null, @"
IF OBJECT_ID(N'dbo.SchemaVersion', N'U') IS NULL
CREATE TABLE dbo.SchemaVersion (
    Version      INT           NOT NULL PRIMARY KEY,
    Description  NVARCHAR(200) NOT NULL,
    AppliedAt    DATETIME2     NOT NULL
);");

        int current;

        using (SqlCommand command = new SqlCommand(
            "SELECT ISNULL(MAX(Version), 0) FROM dbo.SchemaVersion",
            connection))
        {
            current = (int)command.ExecuteScalar()!;
        }

        foreach (var (version, description, sql) in Steps)
        {
            if (version <= current)
            {
                continue;
            }

            using SqlTransaction transaction = connection.BeginTransaction();

            Database.Run(connection, transaction, sql);

            Database.Run(connection, transaction,
                "INSERT INTO dbo.SchemaVersion (Version, Description, AppliedAt) " +
                "VALUES (@version, @description, SYSDATETIME())",
                ("@version", version),
                ("@description", description));

            transaction.Commit();
        }
    }
}
