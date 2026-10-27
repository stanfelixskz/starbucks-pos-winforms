using POSInventorySystem.Data;
using POSInventorySystem.Models;
using POSInventorySystem.Services;

namespace POSInventorySystem.Tests;

public class InventoryServiceTests : IDisposable
{
    private readonly InventoryService inventory = new InventoryService();

    private static Product Latte => DataStore.GetProduct("P01")!;
    private static Ingredient Ing(string id) => DataStore.GetIngredient(id)!;

    public InventoryServiceTests() => TestSupport.ResetData();

    public void Dispose() => TestSupport.ResetData();

    [Fact]
    public void A_drink_is_available_when_every_ingredient_is_in_stock()
    {
        Assert.True(inventory.IsProductAvailable(Latte, 1, TestSupport.Plain()));
    }

    [Fact]
    public void A_drink_is_not_available_when_an_ingredient_is_out()
    {
        Ing("I001").Quantity = 0m;   // coffee beans

        Assert.False(inventory.IsProductAvailable(Latte, 1, TestSupport.Plain()));
        Assert.Equal("Coffee Beans", Assert.Single(inventory.MissingIngredients(Latte)).Name);
    }

    [Fact]
    public void Switching_a_product_off_by_hand_stops_it_selling()
    {
        Latte.SetUnavailable(true);

        Assert.False(inventory.IsProductAvailable(Latte, 1, TestSupport.Plain()));
    }

    [Fact]
    public void Food_is_counted_in_pieces()
    {
        Product cookie = DataStore.GetProduct("P07")!;
        cookie.SetStock(1);

        Assert.True(inventory.IsProductAvailable(cookie, 1));
        Assert.False(inventory.IsProductAvailable(cookie, 2));
        Assert.True(inventory.ConsumeProduct(cookie, 1));
        Assert.Equal(0, cookie.Stock);
    }

    [Fact]
    public void Selling_uses_the_recipe_plus_milk_shots_and_syrup()
    {
        var custom = new BeverageCustomization("Tall", "Hot", "Soy Milk", 1, "Hazelnut");

        Assert.True(inventory.ConsumeProduct(Latte, 2, custom));

        Assert.Equal(100m - 0.036m, Ing("I001").Quantity);   // beans: 0.018 x 2
        Assert.Equal(100m - 0.4m, Ing("I021").Quantity);     // soy milk: 0.2 x 2
        Assert.Equal(100m - 0.06m, Ing("I004").Quantity);    // espresso: 0.03 x 1 shot x 2
        Assert.Equal(100m - 0.03m, Ing("I020").Quantity);    // hazelnut: 0.015 x 2
        Assert.Equal(100m, Ing("I002").Quantity);            // whole milk untouched
    }

    [Fact]
    public void Whole_milk_is_deducted_when_it_is_chosen()
    {
        var custom = new BeverageCustomization("Tall", "Hot", "Whole Milk", 0, "None");

        inventory.ConsumeProduct(Latte, 1, custom);

        Assert.Equal(100m - CustomizationRules.MilkPacksPerServing, Ing("I002").Quantity);
    }

    [Fact]
    public void The_check_and_the_deduction_use_the_same_amounts()
    {
        var custom = new BeverageCustomization("Venti", "Iced", "Oat Milk", 2, "Caramel");
        const int quantity = 3;

        var needs = inventory.RequiredIngredients(Latte, quantity, custom)!;
        var before = needs.ToDictionary(n => n.Ingredient.IngredientId, n => n.Ingredient.Quantity);

        Assert.True(inventory.ConsumeProduct(Latte, quantity, custom));

        foreach (var (ingredient, amount) in needs)
        {
            Assert.Equal(before[ingredient.IngredientId] - amount, ingredient.Quantity);
        }
    }

    [Fact]
    public void An_option_whose_ingredient_does_not_exist_is_not_available()
    {
        Ingredient soy = Ing("I021");
        DataStore.Ingredients.Remove(soy);

        try
        {
            var custom = new BeverageCustomization("Tall", "Hot", "Soy Milk", 0, "None");

            Assert.Null(inventory.RequiredIngredients(Latte, 1, custom));
            Assert.False(inventory.IsProductAvailable(Latte, 1, custom));
        }
        finally
        {
            DataStore.Ingredients.Add(soy);
        }
    }

    [Fact]
    public void An_order_is_checked_as_a_whole_because_items_share_ingredients()
    {
        Ing("I001").Quantity = 0.03m;   // enough beans for one latte (0.018), not two

        var custom = TestSupport.Plain();

        Assert.True(inventory.CanFulfillOrder(new() { (Latte, 1, custom) }));
        Assert.False(inventory.CanFulfillOrder(new() { (Latte, 1, custom), (Latte, 1, custom) }));
    }

    [Fact]
    public void Checking_an_order_does_not_change_the_stock()
    {
        var custom = new BeverageCustomization("Tall", "Hot", "Soy Milk", 1, "Hazelnut");

        inventory.CanFulfillOrder(new() { (Latte, 5, custom), (DataStore.GetProduct("P07")!, 3, null) });

        Assert.All(DataStore.Ingredients, i => Assert.Equal(100m, i.Quantity));
        Assert.Equal(50, DataStore.GetProduct("P07")!.Stock);
    }

    [Fact]
    public void Every_option_ingredient_exists_in_the_starting_data()
    {
        Assert.Empty(CustomizationRules.FindMissingIngredients());
    }

    [Fact]
    public void A_missing_option_ingredient_is_reported()
    {
        Ingredient hazelnut = Ing("I020");
        DataStore.Ingredients.Remove(hazelnut);

        try
        {
            Assert.Contains("I020", CustomizationRules.FindMissingIngredients());
        }
        finally
        {
            DataStore.Ingredients.Add(hazelnut);
        }
    }
}
