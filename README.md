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

## Project layout

| Folder | Contents |
|---|---|
| `Models/` | Product, Ingredient, Transaction, CartItem, StockRecord, payments |
| `Data/` | `Database.cs` (all SQL, table creation, loading and saving), seed data, recipes |
| `Services/` | Login, admin actions, inventory rules, password hashing |
| `UI/` | Windows Forms screens and dialogs (built in code) |
| `Assets/Fonts/` | Source Sans 3 (SIL Open Font License) |

## Database tables

`Users`, `Products`, `Ingredients`, `Transactions`, `TransactionItems`, `StockRecords`.
Recipes are defined in code (`Data/RecipeData.cs`).

## Notes

- `Assets/logo.png` (or `logo.jpg`) is optional. If it is missing, the screens show a text header.
  The logo is not included in this repository.
