using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.Services;

// Admin operations. These return results instead of printing,
// so any screen (or a database layer later) can use them.
public class AdminService
{
    // ==========================================
    // PRODUCTS
    // ==========================================

    public string NextProductId()
    {
        int highest = 0;

        foreach (Product product in DataStore.Products)
        {
            if (product.ProductId.Length > 1 &&
                int.TryParse(
                    product.ProductId.Substring(1),
                    out int number) &&
                number > highest)
            {
                highest = number;
            }
        }

        return $"P{highest + 1:D2}";
    }

    public bool AddProduct(
        string name,
        decimal price,
        int stock,
        bool isBeverage,
        int lowStockLevel,
        out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Product name is required.";
            return false;
        }

        if (price <= 0)
        {
            error = "Price must be greater than zero.";
            return false;
        }

        if (stock < 0)
        {
            error = "Stock cannot be negative.";
            return false;
        }

        string id = NextProductId();
        Product? product = null;

        // If another terminal took this number first, the database tells us
        // the next free one and we try again
        for (int attempt = 0; attempt < 5 && product == null; attempt++)
        {
            Product candidate = isBeverage
                ? new BeverageProduct(
                    id, name.Trim(), price, stock, "Beverage")
                : new FoodProduct(
                    id, name.Trim(), price, stock, "Food");

            candidate.SetLowStockLevel(lowStockLevel);

            try
            {
                Database.AddProduct(candidate);
                product = candidate;
            }
            catch (Exception ex) when (Database.IsDuplicateKey(ex))
            {
                try
                {
                    id = Database.NextProductIdFromDatabase();
                }
                catch (Exception inner)
                {
                    error = $"Could not save the product: {inner.Message}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = $"Could not save the product: {ex.Message}";
                return false;
            }
        }

        if (product == null)
        {
            error = "Could not find a free product number.";
            return false;
        }

        DataStore.Products.Add(product);

        return true;
    }

    public bool UpdateProduct(
        Product product,
        string name,
        decimal price,
        int lowStockLevel,
        out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Product name is required.";
            return false;
        }

        if (price <= 0)
        {
            error = "Price must be greater than zero.";
            return false;
        }

        string oldName = product.Name;
        decimal oldPrice = product.Price;
        int oldLevel = product.LowStockLevel;

        product.UpdateProductInfo(
            name.Trim(),
            price,
            product.Category
        );

        product.SetLowStockLevel(lowStockLevel);

        try
        {
            Database.UpdateProduct(product);
        }
        catch (Exception ex)
        {
            // Put the old values back so memory matches the database
            product.UpdateProductInfo(oldName, oldPrice, product.Category);
            product.SetLowStockLevel(oldLevel);
            error = $"Could not save the change: {ex.Message}";
            return false;
        }

        return true;
    }


    // ==========================================
    // AVAILABILITY (stop / resume selling)
    // ==========================================

    public bool SetUnavailable(Product product, bool value, out string error)
    {
        error = "";

        bool old = product.Unavailable;
        product.SetUnavailable(value);

        try
        {
            Database.SetProductUnavailable(product);
        }
        catch (Exception ex)
        {
            product.SetUnavailable(old);
            error = $"Could not save the change: {ex.Message}";
            return false;
        }

        return true;
    }

    // ==========================================
    // INGREDIENTS (add / edit)
    // ==========================================

    public string NextIngredientId()
    {
        int highest = 0;

        foreach (Ingredient ingredient in DataStore.Ingredients)
        {
            if (ingredient.IngredientId.Length > 1 &&
                int.TryParse(
                    ingredient.IngredientId.Substring(1),
                    out int number) &&
                number > highest)
            {
                highest = number;
            }
        }

        return $"I{highest + 1:D3}";
    }

    private static string CheckIngredient(
        string name,
        string unit,
        decimal packSize,
        decimal lowAt,
        decimal par)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Ingredient name is required.";
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            return "Choose a unit (g, ml or pcs).";
        }

        if (packSize <= 0)
        {
            return "Pack size must be greater than zero.";
        }

        if (lowAt < 0 || par < 0)
        {
            return "Reorder and par levels can not be negative.";
        }

        if (par < lowAt)
        {
            return "The par level can not be lower than the reorder level.";
        }

        return "";
    }

    public bool AddIngredient(
        string name,
        string unit,
        decimal packSize,
        decimal lowAt,
        decimal par,
        decimal startingPacks,
        out string error)
    {
        error = CheckIngredient(name, unit, packSize, lowAt, par);

        if (error.Length > 0)
        {
            return false;
        }

        string id = NextIngredientId();
        Ingredient? ingredient = null;

        for (int attempt = 0; attempt < 5 && ingredient == null; attempt++)
        {
            Ingredient candidate = new Ingredient(
                id,
                name.Trim(),
                Math.Max(0, startingPacks),
                lowAt,
                unit,
                packSize,
                par);

            try
            {
                Database.AddIngredient(candidate);
                ingredient = candidate;
            }
            catch (Exception ex) when (Database.IsDuplicateKey(ex))
            {
                try
                {
                    id = Database.NextIngredientIdFromDatabase();
                }
                catch (Exception inner)
                {
                    error = $"Could not save the ingredient: {inner.Message}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = $"Could not save the ingredient: {ex.Message}";
                return false;
            }
        }

        if (ingredient == null)
        {
            error = "Could not find a free ingredient number.";
            return false;
        }

        DataStore.Ingredients.Add(ingredient);

        // The opening stock is a dated record like any other delivery
        if (ingredient.Quantity > 0)
        {
            DataStore.StockRecords.Add(
                new StockRecord(
                    DateTime.Now,
                    ingredient.IngredientId,
                    ingredient.Name,
                    "RESTOCK",
                    "pack",
                    ingredient.Quantity,
                    ingredient.Quantity,
                    "Admin",
                    "Opening stock"));

            try
            {
                Database.SaveInventory();
            }
            catch (Exception ex)
            {
                error = $"Saved, but the opening stock record failed: {ex.Message}";
            }
        }

        return true;
    }

    public bool UpdateIngredient(
        Ingredient ingredient,
        string name,
        string unit,
        decimal packSize,
        decimal lowAt,
        decimal par,
        out string error)
    {
        error = CheckIngredient(name, unit, packSize, lowAt, par);

        if (error.Length > 0)
        {
            return false;
        }

        string oldName = ingredient.Name;
        string oldUnit = ingredient.Unit;
        decimal oldSize = ingredient.PackSize;
        decimal oldLow = ingredient.LowStockThreshold;
        decimal oldPar = ingredient.ParLevel;

        ingredient.Name = name.Trim();
        ingredient.Unit = unit;
        ingredient.PackSize = packSize;
        ingredient.LowStockThreshold = lowAt;
        ingredient.ParLevel = par;

        try
        {
            Database.UpdateIngredient(ingredient);
        }
        catch (Exception ex)
        {
            ingredient.Name = oldName;
            ingredient.Unit = oldUnit;
            ingredient.PackSize = oldSize;
            ingredient.LowStockThreshold = oldLow;
            ingredient.ParLevel = oldPar;

            error = $"Could not save the change: {ex.Message}";
            return false;
        }

        return true;
    }

    // ==========================================
    // RECEIVE (delivery in)
    // ==========================================

    // Adds packs (or boxes of packs) to an ingredient and logs it.
    // The note holds the supplier or delivery reference.
    public decimal RestockIngredient(
        Ingredient ingredient,
        string unit,
        int count,
        int packsPerBox,
        string note = "")
    {
        decimal packsAdded = PacksFor(unit, count, packsPerBox);

        ingredient.Quantity += packsAdded;

        DataStore.StockRecords.Add(
            new StockRecord(
                DateTime.Now,
                ingredient.IngredientId,
                ingredient.Name,
                "RESTOCK",
                unit,
                count,
                packsAdded,
                "Admin",
                note
            )
        );

        Database.SaveInventory();

        return packsAdded;
    }

    public void RestockProduct(Product product, int quantity, string note = "")
    {
        product.AddStock(quantity);

        DataStore.StockRecords.Add(
            new StockRecord(
                DateTime.Now,
                product.ProductId,
                product.Name,
                "RESTOCK",
                "pc",
                quantity,
                quantity,
                "Admin",
                note
            )
        );

        Database.SaveInventory();
    }

    // ==========================================
    // WASTE (thrown away, broken, expired)
    // ==========================================

    public void WasteIngredient(
        Ingredient ingredient,
        string unit,
        int count,
        int packsPerBox,
        string reason,
        string note)
    {
        decimal packs = PacksFor(unit, count, packsPerBox);

        ingredient.Quantity -= packs;

        DataStore.StockRecords.Add(
            new StockRecord(
                DateTime.Now,
                ingredient.IngredientId,
                ingredient.Name,
                "WASTE",
                unit,
                count,
                -packs,
                "Admin",
                Describe(reason, note)
            )
        );

        Database.SaveInventory();
    }

    public void WasteProduct(Product product, int quantity, string reason, string note)
    {
        product.RemoveStock(quantity);

        DataStore.StockRecords.Add(
            new StockRecord(
                DateTime.Now,
                product.ProductId,
                product.Name,
                "WASTE",
                "pc",
                quantity,
                -quantity,
                "Admin",
                Describe(reason, note)
            )
        );

        Database.SaveInventory();
    }

    // ==========================================
    // ADJUST (stocktake: set the counted amount)
    // ==========================================

    public void AdjustIngredient(
        Ingredient ingredient,
        decimal counted,
        string reason,
        string note)
    {
        decimal change = counted - ingredient.Quantity;

        ingredient.Quantity = counted;

        DataStore.StockRecords.Add(
            new StockRecord(
                DateTime.Now,
                ingredient.IngredientId,
                ingredient.Name,
                "ADJUST",
                "pack",
                Math.Abs(change),
                change,
                "Admin",
                Describe(reason, note)
            )
        );

        Database.SaveInventory();
    }

    public void AdjustProduct(Product product, int counted, string reason, string note)
    {
        int change = counted - product.Stock;

        product.SetStock(counted);

        DataStore.StockRecords.Add(
            new StockRecord(
                DateTime.Now,
                product.ProductId,
                product.Name,
                "ADJUST",
                "pc",
                Math.Abs(change),
                change,
                "Admin",
                Describe(reason, note)
            )
        );

        Database.SaveInventory();
    }

    // ==========================================
    // HELPERS
    // ==========================================

    // Packs for a count entered as packs or boxes
    public static decimal PacksFor(string unit, int count, int packsPerBox) =>
        unit == "box" ? count * packsPerBox : count;

    // "Spoiled: left out overnight" (note is optional)
    private static string Describe(string reason, string note) =>
        note.Length == 0 ? reason : $"{reason}: {note}";
}
