using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.Services;

public class InventoryService
{
    // ==========================================
    // PRODUCT INVENTORY
    // ==========================================

    public void DisplayAllProducts()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("          PRODUCT INVENTORY");
        Console.WriteLine("========================================");

        // ==========================================
        // BEVERAGES
        // ==========================================

        Console.WriteLine();
        Console.WriteLine("BEVERAGES");
        Console.WriteLine("----------------------------------------");

        Console.WriteLine(
            $"{"ID",-6} {"PRODUCT",-25} {"STATUS",-15}"
        );

        Console.WriteLine("----------------------------------------");

        foreach (Product product in DataStore.Products)
        {
            if (product is BeverageProduct)
            {
                bool available =
                    IsProductAvailable(
                        product,
                        1
                    );

                string status =
                    available
                        ? "AVAILABLE"
                        : "UNAVAILABLE";

                Console.WriteLine(
                    $"{product.ProductId,-6} " +
                    $"{product.Name,-25} " +
                    $"{status,-15}"
                );
            }
        }


        // ==========================================
        // FOOD
        // ==========================================

        Console.WriteLine();
        Console.WriteLine("FOOD");
        Console.WriteLine("----------------------------------------");

        Console.WriteLine(
            $"{"ID",-6} {"PRODUCT",-25} {"STOCK",-10}"
        );

        Console.WriteLine("----------------------------------------");

        foreach (Product product in DataStore.Products)
        {
            if (product is FoodProduct)
            {
                Console.WriteLine(
                    $"{product.ProductId,-6} " +
                    $"{product.Name,-25} " +
                    $"{product.Stock,-10}"
                );
            }
        }

        Console.WriteLine("========================================");
    }


    // ==========================================
    // FIND PRODUCT
    // ==========================================

    public Product? FindProduct(string productId)
    {
        return DataStore.Products
            .FirstOrDefault(product =>
                product.ProductId.Equals(
                    productId,
                    StringComparison.OrdinalIgnoreCase
                ));
    }


    // ==========================================
    // RESTOCK PRODUCT
    // ==========================================

    public bool RestockProduct(
        string productId,
        int quantity)
    {
        Product? product =
            FindProduct(productId);

        if (product == null ||
            quantity <= 0)
        {
            return false;
        }

        product.AddStock(quantity);

        return true;
    }


    // ==========================================
    // REMOVE PRODUCT STOCK
    // ==========================================

    public bool RemoveStock(
        string productId,
        int quantity)
    {
        Product? product =
            FindProduct(productId);

        if (product == null ||
            quantity <= 0 ||
            product.Stock < quantity)
        {
            return false;
        }

        product.RemoveStock(quantity);

        return true;
    }


    // ==========================================
    // GET LOW STOCK PRODUCTS
    // ==========================================

    public List<Product> GetLowStockProducts()
    {
        return DataStore.Products
            .Where(product => product.IsLowStock())
            .ToList();
    }


    // ==========================================
    // CHECK PRODUCT AVAILABILITY
    // ==========================================

public bool IsProductAvailable(
    Product product,
    int quantity,
    BeverageCustomization? customization = null)
{
    if (quantity <= 0)
    {
        return false;
    }

    // ==========================================
    // BEVERAGES
    // ==========================================
    // Beverages are made to order.
    // Their availability depends on their
    // recipe ingredients and customizations.
    // ==========================================

    if (product is BeverageProduct)
    {
        Recipe? recipe =
            RecipeData.Recipes.FirstOrDefault(
                r => r.Product.ProductId ==
                     product.ProductId
            );

        if (recipe == null)
        {
            return true;
        }

        // Check base recipe ingredients
        foreach (RecipeIngredient recipeIngredient
                 in recipe.Ingredients)
        {
            // Skip Whole Milk when another milk
            // has been selected.
            if (recipeIngredient.Ingredient.IngredientId == "I002" &&
                customization != null &&
                customization.Milk != "Whole Milk")
            {
                continue;
            }

            decimal requiredAmount =
                recipeIngredient.QuantityRequired *
                quantity;

            if (recipeIngredient.Ingredient.Quantity <
                requiredAmount)
            {
                return false;
            }
        }

        // Check customized ingredients
        if (customization != null)
        {
            // ------------------------------
            // Alternative Milk
            // ------------------------------

            if (customization.Milk != "None" &&
                customization.Milk != "Whole Milk")
            {
                Ingredient? milk =
                    GetMilkIngredient(
                        customization.Milk
                    );

                if (milk == null)
                {
                    return false;
                }

                decimal requiredMilk =
                    200m * quantity;

                if (milk.Quantity < requiredMilk)
                {
                    return false;
                }
            }

            // ------------------------------
            // Extra Espresso Shots
            // ------------------------------

            if (customization.ExtraShots > 0)
            {
                Ingredient? espresso =
                    DataStore.GetIngredient("I004");

                if (espresso == null)
                {
                    return false;
                }

                decimal requiredEspresso =
                    30m *
                    customization.ExtraShots *
                    quantity;

                if (espresso.Quantity <
                    requiredEspresso)
                {
                    return false;
                }
            }

            // ------------------------------
            // Syrup
            // ------------------------------

            if (customization.Syrup != "None")
            {
                Ingredient? syrup =
                    GetSyrupIngredient(
                        customization.Syrup
                    );

                if (syrup == null)
                {
                    return false;
                }

                decimal requiredSyrup =
                    15m * quantity;

                if (syrup.Quantity <
                    requiredSyrup)
                {
                    return false;
                }
            }
        }

        return true;
    }


    // ==========================================
    // FOOD
    // ==========================================

    if (product is FoodProduct)
    {
        return product.Stock >= quantity;
    }


    // ==========================================
    // OTHER PRODUCTS
    // ==========================================

    return product.Stock >= quantity;
}

    // ==========================================
    // GET MILK INGREDIENT
    // ==========================================

    private Ingredient? GetMilkIngredient(
        string milkName)
    {
        if (milkName == "Soy Milk")
        {
            return DataStore.GetIngredient("I019");
        }

        if (milkName == "Almond Milk")
        {
            return DataStore.GetIngredient("I020");
        }

        if (milkName == "Oat Milk")
        {
            return DataStore.GetIngredient("I003");
        }

        if (milkName == "Whole Milk")
        {
            return DataStore.GetIngredient("I002");
        }

        return null;
    }


    // ==========================================
    // GET SYRUP INGREDIENT
    // ==========================================

    private Ingredient? GetSyrupIngredient(
        string syrupName)
    {
        if (syrupName == "Caramel")
        {
            return DataStore.GetIngredient("I005");
        }

        if (syrupName == "Vanilla")
        {
            return DataStore.GetIngredient("I006");
        }

        if (syrupName == "Hazelnut")
        {
            return DataStore.GetIngredient("I021");
        }

        return null;
    }


    // ==========================================
    // CONSUME PRODUCT / INGREDIENTS
    // ==========================================

public bool ConsumeProduct(
    Product product,
    int quantity,
    BeverageCustomization? customization = null)
{
    if (!IsProductAvailable(
        product,
        quantity,
        customization))
    {
        return false;
    }


    // ==========================================
    // BEVERAGE
    // ==========================================
    // Deduct ingredients, NOT Product.Stock.
    // ==========================================

    if (product is BeverageProduct)
    {
        Recipe? recipe =
            RecipeData.Recipes.FirstOrDefault(
                r => r.Product.ProductId ==
                     product.ProductId
            );

        if (recipe == null)
        {
            return true;
        }

        // --------------------------------------
        // BASE RECIPE INGREDIENTS
        // --------------------------------------

        foreach (RecipeIngredient recipeIngredient
                 in recipe.Ingredients)
        {
            // Skip Whole Milk when another
            // milk was selected.
            if (recipeIngredient.Ingredient.IngredientId == "I002" &&
                customization != null &&
                customization.Milk != "Whole Milk")
            {
                continue;
            }

            decimal requiredAmount =
                recipeIngredient.QuantityRequired *
                quantity;

            recipeIngredient.Ingredient.Quantity -=
                requiredAmount;
        }


        // --------------------------------------
        // CUSTOMIZATION INGREDIENTS
        // --------------------------------------

        if (customization != null)
        {
            // Alternative milk
            if (customization.Milk != "None" &&
                customization.Milk != "Whole Milk")
            {
                Ingredient? milk =
                    GetMilkIngredient(
                        customization.Milk
                    );

                if (milk != null)
                {
                    milk.Quantity -=
                        200m * quantity;
                }
            }


            // Extra espresso shots
            if (customization.ExtraShots > 0)
            {
                Ingredient? espresso =
                    DataStore.GetIngredient("I004");

                if (espresso != null)
                {
                    espresso.Quantity -=
                        30m *
                        customization.ExtraShots *
                        quantity;
                }
            }


            // Syrup
            if (customization.Syrup != "None")
            {
                Ingredient? syrup =
                    GetSyrupIngredient(
                        customization.Syrup
                    );

                if (syrup != null)
                {
                    syrup.Quantity -=
                        15m * quantity;
                }
            }
        }

        return true;
    }


    // ==========================================
    // FOOD
    // ==========================================

    if (product is FoodProduct)
    {
        return product.RemoveStock(quantity);
    }


    // ==========================================
    // OTHER PRODUCTS
    // ==========================================

    return product.RemoveStock(quantity);
}
}