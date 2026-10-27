using POSInventorySystem.Data;
using POSInventorySystem.Models;
using POSInventorySystem.Services;

namespace POSInventorySystem.Tests;

public class RecipeTests : IDisposable
{
    private readonly InventoryService inventory = new InventoryService();
    private readonly AdminService admin = new AdminService();

    // Anything that saves goes to a throw-away database, never the real one
    private static TestDatabase Start()
    {
        Skip.IfNot(TestSupport.LocalDbAvailable(), "SQL Server LocalDB is not available.");
        return new TestDatabase();
    }

    public RecipeTests() => TestSupport.ResetData();

    public void Dispose() => TestSupport.ResetData();

    // Beans and milk, with an amount for each size
    private static List<RecipeRow> SampleRecipe() => new()
    {
        new("I001", "Tall", 10m), new("I001", "Grande", 12.5m), new("I001", "Venti", 15m),
        new("I002", "Tall", 100m), new("I002", "Grande", 125m), new("I002", "Venti", 150m),
    };

    [Fact]
    public void A_recipe_with_no_ingredients_is_refused()
    {
        Assert.NotEqual("", RecipeRules.Validate(new List<RecipeRow>()));
        Assert.NotEqual("", RecipeRules.Validate(null));
    }

    [Fact]
    public void A_good_recipe_passes()
    {
        Assert.Equal("", RecipeRules.Validate(SampleRecipe()));
    }

    [Fact]
    public void Zero_amounts_and_missing_sizes_are_refused()
    {
        var zero = SampleRecipe();
        zero[1] = new RecipeRow("I001", "Grande", 0m);
        Assert.NotEqual("", RecipeRules.Validate(zero));

        var missingSize = SampleRecipe().Where(r => r.Size != "Venti").ToList();
        Assert.NotEqual("", RecipeRules.Validate(missingSize));
    }

    [SkippableFact]
    public void A_drink_cannot_be_added_without_a_recipe()
    {
        using TestDatabase db = Start();

        int before = DataStore.Products.Count;

        Assert.False(admin.AddProduct("Mocha", 150m, 0, true, 5, null, out string error));
        Assert.Contains("recipe", error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(before, DataStore.Products.Count);
    }

    [SkippableFact]
    public void A_drink_is_added_with_its_recipe_and_can_be_sold()
    {
        using TestDatabase db = Start();

        Assert.True(admin.AddProduct("Mocha", 150m, 0, true, 5, SampleRecipe(), out string error), error);

        Product mocha = DataStore.Products.Last();
        Assert.Equal("Mocha", mocha.Name);
        Assert.True(inventory.HasRecipe(mocha));
        Assert.True(inventory.IsProductAvailable(mocha, 1, TestSupport.Plain()));
    }

    [SkippableFact]
    public void Food_needs_no_recipe()
    {
        using TestDatabase db = Start();

        Assert.True(admin.AddProduct("Cookie", 60m, 10, false, 5, out string error), error);
        Assert.True(inventory.HasRecipe(DataStore.Products.Last()));
    }

    [SkippableFact]
    public void A_bigger_cup_uses_more_of_the_ingredients()
    {
        using TestDatabase db = Start();

        Assert.True(admin.AddProduct("Mocha", 150m, 0, true, 5, SampleRecipe(), out string error), error);
        Product mocha = DataStore.Products.Last();

        decimal Beans(string size) => inventory
            .RequiredIngredients(mocha, 1, new BeverageCustomization(size, "Hot", "None", 0, "None"))!
            .Single(n => n.Ingredient.IngredientId == "I001").Amount;

        Assert.True(Beans("Grande") > Beans("Tall"));
        Assert.True(Beans("Venti") > Beans("Grande"));
    }

    [SkippableFact]
    public void Editing_a_recipe_changes_what_a_drink_uses()
    {
        using TestDatabase db = Start();

        Product latte = DataStore.GetProduct("P01")!;

        Assert.True(admin.SaveRecipe(latte, SampleRecipe(), out string error), error);

        Recipe recipe = RecipeRules.Find("P01")!;
        Assert.Equal(2, recipe.For(null).Count());
    }

    [SkippableFact]
    public void Only_drinks_have_recipes()
    {
        using TestDatabase db = Start();

        Product food = DataStore.Products.First(p => p is not BeverageProduct);

        Assert.False(admin.SaveRecipe(food, SampleRecipe(), out _));
    }

    [SkippableFact]
    public void A_saved_recipe_is_still_there_after_restarting()
    {
        Skip.IfNot(TestSupport.LocalDbAvailable(), "SQL Server LocalDB is not available.");

        using TestDatabase db = new TestDatabase();

        Assert.True(admin.AddProduct("Mocha", 150m, 0, true, 5, SampleRecipe(), out string error), error);
        string id = DataStore.Products.Last().ProductId;

        db.Restart();

        Recipe recipe = RecipeRules.Find(id)!;
        Assert.NotNull(recipe);
        Assert.Equal(6, recipe.Ingredients.Count);
    }

    [SkippableFact]
    public void A_drink_and_its_recipe_are_saved_together_or_not_at_all()
    {
        Skip.IfNot(TestSupport.LocalDbAvailable(), "SQL Server LocalDB is not available.");

        using TestDatabase db = new TestDatabase();

        // An ingredient that does not exist makes the recipe insert fail
        var bad = new List<RecipeRow> { new("I999", "Tall", 1m), new("I999", "Grande", 1m), new("I999", "Venti", 1m) };
        Product ghost = new BeverageProduct("P90", "Ghost", 100m, 0, "Beverage");

        Assert.ThrowsAny<Exception>(() => Database.AddProduct(ghost, bad));

        db.Restart();

        Assert.Null(DataStore.GetProduct("P90"));
    }
}
