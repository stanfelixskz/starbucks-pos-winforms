using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.Services;

// The rules a drink's recipe has to follow, and conversions between the
// stored rows and a Recipe.
public static class RecipeRules
{
    // Checks a recipe before it is saved. Returns "" when it is fine,
    // otherwise a message that says what to fix.
    public static string Validate(IReadOnlyList<RecipeRow>? rows)
    {
        if (rows == null || rows.Count == 0)
        {
            return "A drink needs a recipe: add at least one ingredient.";
        }

        foreach (RecipeRow row in rows)
        {
            Ingredient? ingredient = DataStore.GetIngredient(row.IngredientId);

            if (ingredient == null)
            {
                return $"Unknown ingredient {row.IngredientId}.";
            }

            if (!Recipe.Sizes.Contains(row.Size))
            {
                return $"Unknown size \"{row.Size}\".";
            }

            if (row.Amount <= 0)
            {
                return $"{ingredient.Name}: the {row.Size} amount must be " +
                       "greater than zero.";
            }
        }

        foreach (var group in rows.GroupBy(r => r.IngredientId))
        {
            string name = DataStore.GetIngredient(group.Key)!.Name;

            if (group.Count() != group.Select(r => r.Size).Distinct().Count())
            {
                return $"{name} is listed twice.";
            }

            if (group.Count() != Recipe.Sizes.Length)
            {
                return $"{name} needs an amount for every size.";
            }
        }

        return "";
    }

    // The recipe for a product from its stored rows
    public static Recipe Build(Product product, IEnumerable<RecipeRow> rows)
    {
        Recipe recipe = new Recipe(product);

        foreach (RecipeRow row in rows)
        {
            Ingredient? ingredient = DataStore.GetIngredient(row.IngredientId);

            if (ingredient != null)
            {
                recipe.Ingredients.Add(
                    new RecipeIngredient(ingredient, row.Amount, row.Size));
            }
        }

        return recipe;
    }

    public static List<RecipeRow> ToRows(Recipe recipe) =>
        recipe.Ingredients
            .Select(line => new RecipeRow(
                line.Ingredient.IngredientId, line.Size, line.Amount))
            .ToList();

    public static Recipe? Find(string productId) =>
        RecipeData.Recipes.FirstOrDefault(
            r => r.Product.ProductId == productId);
}
