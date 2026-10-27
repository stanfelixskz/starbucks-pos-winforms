using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.Services;

public class InventoryService
{
    // ==========================================
    // WHAT AN ORDER LINE USES UP
    // ==========================================
    // The one place that works out which ingredients a drink needs:
    // the recipe, plus milk, extra shots and syrup. The availability
    // check and the deduction both use it, so they can never disagree.
    // Returns null when an option points at an ingredient that does not
    // exist (the drink can not be made).

    public List<(Ingredient Ingredient, decimal Amount)>? RequiredIngredients(
        Product product,
        int quantity,
        BeverageCustomization? customization)
    {
        Dictionary<string, (Ingredient Ingredient, decimal Amount)> needs = new();

        void Need(Ingredient ingredient, decimal amount)
        {
            needs[ingredient.IngredientId] = needs.TryGetValue(
                ingredient.IngredientId, out var existing)
                ? (ingredient, existing.Amount + amount)
                : (ingredient, amount);
        }

        Recipe? recipe = RecipeData.Recipes.FirstOrDefault(
            r => r.Product.ProductId == product.ProductId);

        if (recipe != null)
        {
            foreach (RecipeIngredient line in recipe.Ingredients)
            {
                // Whole milk is part of the recipe; another milk replaces it
                if (line.Ingredient.IngredientId == CustomizationRules.WholeMilkId &&
                    customization != null &&
                    customization.Milk != "Whole Milk")
                {
                    continue;
                }

                Need(line.Ingredient, line.QuantityRequired * quantity);
            }
        }

        if (customization == null)
        {
            return needs.Values.ToList();
        }

        if (customization.Milk != "None")
        {
            Ingredient? milk = Find(CustomizationRules.Milks, customization.Milk);

            if (milk == null)
            {
                return null;
            }

            Need(milk, CustomizationRules.MilkPacksPerServing * quantity);
        }

        if (customization.ExtraShots > 0)
        {
            Ingredient? espresso =
                DataStore.GetIngredient(CustomizationRules.EspressoId);

            if (espresso == null)
            {
                return null;
            }

            Need(espresso,
                CustomizationRules.EspressoPacksPerShot *
                customization.ExtraShots *
                quantity);
        }

        if (customization.Syrup != "None")
        {
            Ingredient? syrup = Find(CustomizationRules.Syrups, customization.Syrup);

            if (syrup == null)
            {
                return null;
            }

            Need(syrup, CustomizationRules.SyrupPacksPerServing * quantity);
        }

        return needs.Values.ToList();
    }

    private static Ingredient? Find(
        IReadOnlyDictionary<string, string> options,
        string option) =>
        options.TryGetValue(option, out string? id)
            ? DataStore.GetIngredient(id)
            : null;


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

        // Drinks are made to order: they are available when every
        // ingredient they need is in stock
        if (product is BeverageProduct)
        {
            var needs = RequiredIngredients(product, quantity, customization);

            return needs != null &&
                   needs.All(n => n.Ingredient.Quantity >= n.Amount);
        }

        // Food (and anything else) is counted in pieces
        return product.Stock >= quantity;
    }


    // ==========================================
    // CONSUME PRODUCT / INGREDIENTS
    // ==========================================

    public bool ConsumeProduct(
        Product product,
        int quantity,
        BeverageCustomization? customization = null)
    {
        if (!IsProductAvailable(product, quantity, customization))
        {
            return false;
        }

        // Drinks use up ingredients, never Product.Stock
        if (product is BeverageProduct)
        {
            foreach (var (ingredient, amount) in
                     RequiredIngredients(product, quantity, customization)!)
            {
                ingredient.Quantity -= amount;
            }

            return true;
        }

        return product.RemoveStock(quantity);
    }


    // ==========================================
    // CHECK A WHOLE ORDER
    // ==========================================
    // Items can share ingredients, so checking each item on
    // its own is not enough. This deducts everything in turn
    // and then restores the stock, so nothing is changed.

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
}
