namespace POSInventorySystem.Models;

// What one serving of a drink uses, for each cup size.
public class Recipe
{
    public static readonly string[] Sizes = { "Tall", "Grande", "Venti" };

    // The size used when none is chosen, and the one amounts are first
    // entered for
    public const string DefaultSize = "Tall";

    // When a recipe is first filled in, a bigger size starts from Tall's
    // amount times this much (the amounts can then be edited)
    public static decimal ScaleFor(string size) => size switch
    {
        "Grande" => 1.25m,
        "Venti" => 1.5m,
        _ => 1m
    };

    public Product Product { get; set; }

    // The lines for every size
    public List<RecipeIngredient> Ingredients { get; set; }

    public Recipe(Product product)
    {
        Product = product;
        Ingredients = new List<RecipeIngredient>();
    }

    // The lines for one size. A size with no lines of its own uses
    // Tall's, so a drink is never left without ingredients.
    public IEnumerable<RecipeIngredient> For(string? size)
    {
        string wanted = size ?? DefaultSize;

        List<RecipeIngredient> lines =
            Ingredients.Where(l => l.Size == wanted).ToList();

        return lines.Count > 0
            ? lines
            : Ingredients.Where(l => l.Size == DefaultSize);
    }

    // Adds an ingredient to every size. The amount is given as a share of
    // one pack for Tall (how the starting recipes were first written); it is
    // stored in the ingredient's own unit, and bigger sizes are scaled up.
    public void AddIngredient(
        Ingredient ingredient,
        decimal quantityRequired)
    {
        decimal tallAmount = quantityRequired * ingredient.PackSize;

        foreach (string size in Sizes)
        {
            Ingredients.Add(
                new RecipeIngredient(
                    ingredient,
                    Math.Round(tallAmount * ScaleFor(size), 1),
                    size));
        }
    }
}
