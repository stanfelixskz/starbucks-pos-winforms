using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.Services;

public class AdminService
{
    // ==========================================
    // VIEW INVENTORY
    // ==========================================

    public void ViewInventory()
    {
        Console.WriteLine();
        Console.WriteLine("==============================================");
        Console.WriteLine("              PRODUCT INVENTORY");
        Console.WriteLine("==============================================");

        Console.WriteLine(
            $"{"ID",-6} {"PRODUCT",-25} {"PRICE",10} {"STOCK",8}"
        );

        Console.WriteLine("----------------------------------------------");

        foreach (Product product in DataStore.Products)
        {
            Console.WriteLine(
                $"{product.ProductId,-6} " +
                $"{product.Name,-25} " +
                $"₱{product.Price,9:F2} " +
                $"{product.Stock,8}"
            );
        }

        Console.WriteLine("==============================================");

        Console.WriteLine();
        Console.WriteLine("==============================================");
        Console.WriteLine("             INGREDIENT INVENTORY");
        Console.WriteLine("==============================================");

        Console.WriteLine(
            $"{"ID",-6} {"INGREDIENT",-25} {"QUANTITY",12} {"UNIT",-8}"
        );

        Console.WriteLine("----------------------------------------------");

        foreach (Ingredient item in DataStore.Ingredients)
        {
            Console.WriteLine(
                $"{item.IngredientId,-6} " +
                $"{item.Name,-25} " +
                $"{item.Quantity,12:F2} " +
                $"{item.Unit,-8}"
            );
        }

        Console.WriteLine("==============================================");
    }


    // ==========================================
    // ADD PRODUCT
    // ==========================================

    public void AddProduct()
    {
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("          ADD PRODUCT");
        Console.WriteLine("=================================");

        Console.Write("Enter Product ID: ");
        string? productId = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(productId))
        {
            Console.WriteLine("Invalid Product ID.");
            return;
        }

        bool idExists = DataStore.Products.Any(
            product => product.ProductId.Equals(
                productId,
                StringComparison.OrdinalIgnoreCase)
        );

        if (idExists)
        {
            Console.WriteLine("Product ID already exists.");
            return;
        }

        Console.Write("Enter Product Name: ");
        string? name = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.WriteLine("Invalid product name.");
            return;
        }

        Console.Write("Enter Price: ");

        if (!decimal.TryParse(
            Console.ReadLine(),
            out decimal price) ||
            price <= 0)
        {
            Console.WriteLine("Invalid price.");
            return;
        }

        Console.Write("Enter Initial Stock: ");

        if (!int.TryParse(
            Console.ReadLine(),
            out int stock) ||
            stock < 0)
        {
            Console.WriteLine("Invalid stock quantity.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Select Product Type:");
        Console.WriteLine("1. Food");
        Console.WriteLine("2. Beverage");
        Console.Write("Choose product type: ");

        string? typeChoice = Console.ReadLine();

        Product product;

        switch (typeChoice)
        {
            case "1":
                product = new FoodProduct(
                    productId,
                    name,
                    price,
                    stock,
                    "Food"
                );
                break;

            case "2":
                product = new BeverageProduct(
                    productId,
                    name,
                    price,
                    stock,
                    "Beverage"
                );
                break;

            default:
                Console.WriteLine("Invalid product type.");
                return;
        }

        DataStore.Products.Add(product);

        Console.WriteLine();
        Console.WriteLine("Product added successfully.");
    }


    // ==========================================
    // UPDATE PRODUCT
    // ==========================================

    public void UpdateProduct()
    {
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("         UPDATE PRODUCT");
        Console.WriteLine("=================================");

        Console.Write("Enter Product ID: ");
        string? productId = Console.ReadLine();

        Product? product = DataStore.Products.FirstOrDefault(
            item => item.ProductId.Equals(
                productId,
                StringComparison.OrdinalIgnoreCase)
        );

        if (product == null)
        {
            Console.WriteLine("Product not found.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Current Name: {product.Name}");
        Console.Write("Enter New Name: ");

        string? newName = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(newName))
        {
            Console.WriteLine("Invalid product name.");
            return;
        }

        Console.WriteLine(
            $"Current Price: ₱{product.Price:F2}"
        );

        Console.Write("Enter New Price: ");

        if (!decimal.TryParse(
            Console.ReadLine(),
            out decimal newPrice) ||
            newPrice <= 0)
        {
            Console.WriteLine("Invalid price.");
            return;
        }

        Console.WriteLine(
            $"Current Category: {product.Category}"
        );

        Console.Write("Enter New Category: ");

        string? newCategory = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(newCategory))
        {
            Console.WriteLine("Invalid category.");
            return;
        }

        product.UpdateProductInfo(
            newName,
            newPrice,
            newCategory
        );

        Console.WriteLine();
        Console.WriteLine("Product updated successfully.");
    }


    // ==========================================
    // RESTOCK PRODUCT
    // ==========================================

    public void RestockProduct()
    {
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("         RESTOCK PRODUCT");
        Console.WriteLine("=================================");

        Console.Write("Enter Product ID: ");
        string? productId = Console.ReadLine();

        Product? product = DataStore.Products.FirstOrDefault(
            item => item.ProductId.Equals(
                productId,
                StringComparison.OrdinalIgnoreCase)
        );

        if (product == null)
        {
            Console.WriteLine("Product not found.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Product: {product.Name}");
        Console.WriteLine($"Current Stock: {product.Stock}");

        Console.Write("Enter quantity to add: ");

        if (!int.TryParse(
            Console.ReadLine(),
            out int quantity) ||
            quantity <= 0)
        {
            Console.WriteLine("Invalid stock quantity.");
            return;
        }

        product.AddStock(quantity);

        Console.WriteLine();
        Console.WriteLine("Product restocked successfully.");
        Console.WriteLine($"New Stock: {product.Stock}");
    }


    // ==========================================
    // RESTOCK INGREDIENT
    // ==========================================

    public void RestockIngredient()
    {
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("       RESTOCK INGREDIENT");
        Console.WriteLine("=================================");

        Console.Write("Enter Ingredient ID: ");
        string? ingredientId = Console.ReadLine();

        Ingredient? ingredient = DataStore.Ingredients.FirstOrDefault(
            item => item.IngredientId.Equals(
                ingredientId,
                StringComparison.OrdinalIgnoreCase)
        );

        if (ingredient == null)
        {
            Console.WriteLine("Ingredient not found.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Ingredient: {ingredient.Name}");

        Console.WriteLine(
            $"Current Quantity: " +
            $"{ingredient.Quantity:F2} {ingredient.Unit}"
        );

        Console.Write("Enter quantity to add: ");

        if (!decimal.TryParse(
            Console.ReadLine(),
            out decimal quantity) ||
            quantity <= 0)
        {
            Console.WriteLine("Invalid quantity.");
            return;
        }

        ingredient.Quantity += quantity;

        Console.WriteLine();
        Console.WriteLine(
            "Ingredient restocked successfully."
        );

        Console.WriteLine(
            $"New Quantity: " +
            $"{ingredient.Quantity:F2} {ingredient.Unit}"
        );
    }


    // ==========================================
    // VIEW LOW STOCK
    // ==========================================

    public void ViewLowStock()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("           LOW STOCK INVENTORY");
        Console.WriteLine("========================================");

        // ==========================================
        // LOW STOCK PRODUCTS
        // ==========================================

        Console.WriteLine();
        Console.WriteLine("PRODUCTS");
        Console.WriteLine("----------------------------------------");

        bool hasLowStockProducts = false;

        foreach (Product product in DataStore.Products)
        {
            if (product.IsLowStock())
            {
                Console.WriteLine(
                    $"{product.ProductId,-6} " +
                    $"{product.Name,-25} " +
                    $"Stock: {product.Stock}"
                );

                hasLowStockProducts = true;
            }
        }

        if (!hasLowStockProducts)
        {
            Console.WriteLine(
                "No products are currently low on stock."
            );
        }


        // ==========================================
        // LOW STOCK INGREDIENTS
        // ==========================================

        Console.WriteLine();
        Console.WriteLine("INGREDIENTS");
        Console.WriteLine("----------------------------------------");

        bool hasLowStockIngredients = false;

        foreach (Ingredient item in DataStore.Ingredients)
        {
            if (item.IsLowStock())
            {
                Console.WriteLine(
                    $"{item.IngredientId,-6} " +
                    $"{item.Name,-25} " +
                    $"Quantity: {item.Quantity:F2} {item.Unit}"
                );

                hasLowStockIngredients = true;
            }
        }

        if (!hasLowStockIngredients)
        {
            Console.WriteLine(
                "No ingredients are currently low on stock."
            );
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
    }


    // ==========================================
    // VIEW SALES RECORDS
    // ==========================================

    public void ViewSalesRecords()
    {
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("          SALES RECORDS");
        Console.WriteLine("=================================");

        if (DataStore.Transactions.Count == 0)
        {
            Console.WriteLine(
                "No sales transactions recorded."
            );

            Console.WriteLine("=================================");
            return;
        }

        foreach (Transaction transaction
                 in DataStore.Transactions)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Transaction ID: " +
                $"{transaction.TransactionId}"
            );

            Console.WriteLine(
                $"Date: {transaction.Date}"
            );

            Console.WriteLine("Items:");

            foreach (CartItem item in transaction.Items)
            {
                Console.WriteLine(
                    $"  {item.Product.Name} " +
                    $"x{item.Quantity} = " +
                    $"₱{item.Subtotal:F2}"
                );
            }

            Console.WriteLine(
                $"Total: ₱{transaction.Total:F2}"
            );

            Console.WriteLine(
                $"Payment: " +
                $"{transaction.Payment?.GetType().Name ?? "N/A"}"
            );
        }

        Console.WriteLine();
        Console.WriteLine("=================================");
    }


    // ==========================================
    // VIEW SALES SUMMARY
    // ==========================================

    public void ViewSalesSummary()
    {
        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine("       TODAY'S SALES SUMMARY");
        Console.WriteLine("=================================");

        DateTime today = DateTime.Today;

        List<Transaction> todayTransactions =
            DataStore.Transactions
                .Where(transaction =>
                    transaction.Date.Date == today)
                .ToList();

        Console.WriteLine(
            $"Date: {today:MMMM dd, yyyy}"
        );

        Console.WriteLine();

        if (todayTransactions.Count == 0)
        {
            Console.WriteLine(
                "No sales transactions recorded today."
            );

            Console.WriteLine("=================================");
            return;
        }

        int totalTransactions =
            todayTransactions.Count;

        decimal totalSales =
            todayTransactions.Sum(
                transaction => transaction.Total
            );

        decimal cashSales =
            todayTransactions
                .Where(transaction =>
                    transaction.Payment is CashPayment)
                .Sum(transaction => transaction.Total);

        decimal gcashSales =
            todayTransactions
                .Where(transaction =>
                    transaction.Payment is GCashPayment)
                .Sum(transaction => transaction.Total);

        decimal cardSales =
            todayTransactions
                .Where(transaction =>
                    transaction.Payment is CardPayment)
                .Sum(transaction => transaction.Total);

        Console.WriteLine(
            $"Total Transactions: {totalTransactions}"
        );

        Console.WriteLine(
            $"Total Sales: ₱{totalSales:F2}"
        );

        Console.WriteLine();

        Console.WriteLine("---------------------------------");
        Console.WriteLine("       PAYMENT BREAKDOWN");
        Console.WriteLine("---------------------------------");

        Console.WriteLine(
            $"Cash Sales:  ₱{cashSales:F2}"
        );

        Console.WriteLine(
            $"GCash Sales: ₱{gcashSales:F2}"
        );

        Console.WriteLine(
            $"Card Sales:  ₱{cardSales:F2}"
        );

        Console.WriteLine("=================================");
    }
}