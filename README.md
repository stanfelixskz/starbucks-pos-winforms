# Starbucks POS & Inventory

A Windows Forms point-of-sale and inventory system for a coffee shop, written in C# (.NET 10).
Data is stored in a SQL Server LocalDB database (`StarbucksPOS`) through `Microsoft.Data.SqlClient`.

## Features

**Cashier**
- Guided ordering: choose an item, customize a drink, set the quantity, review the cart.
- Cash payment only, with live change and a printed-style receipt (item, qty, price, amount).
- Beverages and food shown in separate sections; unavailable items are greyed out.

**Admin**
- Dashboard: today's sales, orders, low-stock alerts, sales chart (7 / 30 days), top sellers.
- Inventory: ingredients counted in whole packs with reorder and par levels, food counted in pieces,
  Receive / Waste / Adjust (stocktake) with reasons, order list, recipes, stock records.
- Drinks have no stock of their own: they show whether they can still be sold, and can be switched off by hand.
- Sales: filter by date, cashier or search; orders (paged, sortable, receipt panel), by day, by item.
- Accounts: register cashier accounts (passwords are stored as salted hashes).

## Requirements

- Windows 10 or 11
- .NET 10 SDK
- SQL Server LocalDB (installed with Visual Studio, or as part of SQL Server Express)

## Run

```
cd POSInventorySystem
dotnet run
```

The first start creates the `StarbucksPOS` database and its tables, and loads the starting data.
This can take 15-20 seconds while LocalDB starts. Connection string (see `Data/Database.cs`):

```
Server=(localdb)\MSSQLLocalDB;Database=StarbucksPOS;Integrated Security=true;TrustServerCertificate=true
```

Demo accounts created on first run:

| Role | Username | Password |
|---|---|---|
| Admin | `admin` | `admin123` |
| Cashier | `cashier` | `cashier123` |

**Before real use:** these demo accounts are created with well-known passwords. Change them (or
delete the accounts and register new ones) before the system holds real data.

## Tests

```
dotnet test StarbucksPOS.slnx
```

- Unit tests cover the stock rules (availability, deduction, shared ingredients), login, password
  hashing, receipts and whole-pack stock text.
- Database tests cover what happens when saving goes wrong: a sale that could not be saved is kept
  and saved later, stock is saved as changes so two terminals do not overwrite each other, duplicate
  order and product numbers are renumbered, and older databases are upgraded.
- The database tests create their own throw-away database (never `StarbucksPOS`) and are skipped
  automatically on a machine without SQL Server LocalDB.
- A GitHub Actions workflow (`.github/workflows/build.yml`) builds and tests on every push.

## Project layout

| Folder | Contents |
|---|---|
| `Models/` | Product, Ingredient, Transaction, CartItem, StockRecord, payments |
| `Data/` | `Database.cs` (loading and saving), `Schema.cs` (numbered migrations), seed data, recipes |
| `Services/` | Login, admin actions, inventory rules, drink option rules, password hashing |
| `UI/` | Windows Forms screens and dialogs (built in code) |
| `Assets/Fonts/` | Source Sans 3 (SIL Open Font License) |

## Database tables

`Users`, `Products`, `Ingredients`, `Transactions`, `TransactionItems`, `StockRecords`.
Recipes are stored in the database (`RecipeIngredients`): a drink can only be added together with its recipe, with an amount per ingredient for each size (Tall, Grande, Venti) in the ingredient's own unit. A drink without a recipe is not sold. The six starting drinks are seeded from `Data/RecipeData.cs`. Food is counted in pieces and has no recipe.

## Notes

- `Assets/logo.png` (or `logo.jpg`) is optional. If it is missing, the screens show a text header.
  The logo is not included in this repository.

## Database connection

The app uses SQL Server LocalDB, instance `(localdb)\MSSQLLocalDB`, by default. To use another instance or server, set the `STARBUCKSPOS_SERVER` environment variable and start the app again:

    setx STARBUCKSPOS_SERVER "(localdb)\OtherName"

If the database can not be opened at startup, the error window shows the exact `sqllocaldb` commands to fix it. Startup waits up to 60 seconds (LocalDB can be slow to start the first time); saves while working give up after 10 seconds. Saves run in the background behind a small "Saving..." box, and a sale that can not be saved is kept and saved with the next one.

## Sales that could not be saved

A finished sale that can not be saved to the database is kept in `%LocalAppData%\StarbucksPOS\pending-sales-StarbucksPOS.json` (written to a temporary file and renamed, so a crash never leaves half a file). The next start loads it back, applies its stock change again and saves the sale and its stock together in one database transaction, then removes it from the file. A sale that is already in the database is not saved or deducted twice. A file that can not be read is kept next to the original with `.unreadable-<time>` added to its name, and the app warns about it at startup.

