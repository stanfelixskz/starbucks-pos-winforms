using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.Services;

public class InventoryService
{


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

    // Switched off by hand
    if (product.Unavailable)
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

            if (customization.Milk != "None")
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
                    0.2m * quantity;

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
                    0.03m *
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
                    0.015m * quantity;

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
    // CHECK A WHOLE ORDER
    // ==========================================
    // Items can share ingredients, so checking each item on
    // its own is not enough. This deducts everything in turn
    // and then restores the stock, so nothing is changed.
    // ==========================================

    public bool CanFulfillOrder(
        List<(Product Product,
              int Quantity,
              BeverageCustomization? Customization)> order)
    {
        Dictionary<string, decimal> ingredientsBefore =
            DataStore.Ingredients.ToDictionary(
                ingredient => ingredient.IngredientId,
                ingredient => ingredient.Quantity
            );

        Dictionary<string, int> productsBefore =
            DataStore.Products.ToDictionary(
                product => product.ProductId,
                product => product.Stock
            );

        bool canFulfill = true;

        foreach (var line in order)
        {
            if (!ConsumeProduct(
                line.Product,
                line.Quantity,
                line.Customization))
            {
                canFulfill = false;
                break;
            }
        }

        foreach (Ingredient ingredient in DataStore.Ingredients)
        {
            ingredient.Quantity =
                ingredientsBefore[ingredient.IngredientId];
        }

        foreach (Product product in DataStore.Products)
        {
            int used =
                productsBefore[product.ProductId] -
                product.Stock;

            if (used > 0)
            {
                product.AddStock(used);
            }
        }

        return canFulfill;
    }

    // ==========================================
    // WHAT IS MISSING FOR A DRINK
    // ==========================================
    // The recipe ingredients that are too low to make even one serving.
    // Empty when the drink can be made (or has no recipe).

    public List<Ingredient> MissingIngredients(Product product)
    {
        Recipe? recipe = RecipeData.Recipes.FirstOrDefault(
            r => r.Product.ProductId == product.ProductId);

        if (recipe == null)
        {
            return new List<Ingredient>();
        }

        return recipe.Ingredients
            .Where(line => line.Ingredient.Quantity < line.QuantityRequired)
            .Select(line => line.Ingredient)
            .DistinctBy(i => i.IngredientId)
            .ToList();
    }

    // ==========================================
    // GET MILK INGREDIENT
    // ==========================================

    private Ingredient? GetMilkIngredient(
        string milkName)
    {
        if (milkName == "Soy Milk")
        {
            return DataStore.GetIngredient("I021");
        }

        if (milkName == "Almond Milk")
        {
            return DataStore.GetIngredient("I019");
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
            return DataStore.GetIngredient("I020");
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
            if (customization.Milk != "None")
            {
                Ingredient? milk =
                    GetMilkIngredient(
                        customization.Milk
                    );

                if (milk != null)
                {
                    milk.Quantity -=
                        0.2m * quantity;
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
                        0.03m *
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
                        0.015m * quantity;
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