using POSInventorySystem.Data;
using POSInventorySystem.Models;
using POSInventorySystem.Services;
using POSInventorySystem.UI;

namespace POSInventorySystem.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void A_correct_password_verifies()
    {
        string stored = PasswordHasher.Hash("secret1");

        Assert.True(PasswordHasher.Verify("secret1", stored));
    }

    [Fact]
    public void A_wrong_password_does_not_verify()
    {
        Assert.False(PasswordHasher.Verify("nope", PasswordHasher.Hash("secret1")));
    }

    [Fact]
    public void The_same_password_gives_a_different_hash_each_time()
    {
        Assert.NotEqual(PasswordHasher.Hash("secret1"), PasswordHasher.Hash("secret1"));
    }

    [Fact]
    public void The_password_is_not_stored_in_the_hash()
    {
        Assert.DoesNotContain("secret1", PasswordHasher.Hash("secret1"));
    }
}

public class LoginServiceTests : IDisposable
{
    private readonly LoginService login = new LoginService();
    private readonly User account =
        new User("Tester", PasswordHasher.Hash("secret1"), "Test User", "Admin");

    public LoginServiceTests() => DataStore.Users.Add(account);

    public void Dispose() => DataStore.Users.Remove(account);

    [Theory]
    [InlineData("Tester")]
    [InlineData("tester")]
    [InlineData("TESTER")]
    public void The_user_name_is_not_case_sensitive(string typed)
    {
        Assert.True(login.UserExists(typed));
        Assert.True(login.Login(typed, "secret1", out string role, out string fullName));
        Assert.Equal("Admin", role);
        Assert.Equal("Test User", fullName);
    }

    [Fact]
    public void The_password_is_still_case_sensitive()
    {
        Assert.False(login.Login("tester", "SECRET1", out _, out _));
    }

    [Fact]
    public void Registration_and_login_agree_that_names_differing_by_case_are_the_same()
    {
        // The duplicate check happens before anything is saved
        Assert.False(login.Register("TESTER", "secret2", "Someone Else", out string error));
        Assert.Contains("taken", error);
    }
}

public class IngredientTests
{
    [Theory]
    [InlineData(0, "0 packs")]
    [InlineData(0.05, "1 pack")]
    [InlineData(1, "1 pack")]
    [InlineData(4.96, "5 packs")]
    [InlineData(10, "10 packs")]
    public void Stock_is_shown_in_whole_packs(double packs, string expected)
    {
        Assert.Equal(expected, Ingredient.Packs((decimal)packs));
    }

    [Fact]
    public void The_order_suggestion_gets_back_up_to_par_in_whole_packs()
    {
        var ice = new Ingredient("I008", "Ice", 0.2m, 1m, "g", 5000m, 2m);

        Assert.True(ice.IsLowStock());
        Assert.Equal(2m, ice.SuggestedOrder);   // ceil(2 - 0.2)
    }

    [Fact]
    public void Nothing_is_suggested_when_stock_is_at_par()
    {
        var milk = new Ingredient("I002", "Whole Milk", 10m, 2m, "ml", 1000m, 10m);

        Assert.False(milk.IsLowStock());
        Assert.Equal(0m, milk.SuggestedOrder);
    }
}

public class ReceiptBuilderTests
{
    [Fact]
    public void The_receipt_has_item_qty_price_and_amount_columns()
    {
        TestSupport.ResetData();

        var sale = new Transaction { CashierName = "Maria" };
        sale.AddItem(DataStore.GetProduct("P07")!, 2);   // cookie, 95.00 each
        sale.ProcessPayment(new CashPayment(sale.Total, 500m));

        string receipt = ReceiptBuilder.Build(sale);

        Assert.Contains("ITEM", receipt);
        Assert.Contains("QTY", receipt);
        Assert.Contains("PRICE", receipt);
        Assert.Contains("AMOUNT", receipt);
        Assert.Contains("Chocolate Chip Cookie", receipt);
        Assert.Contains("95.00", receipt);
        Assert.Contains("190.00", receipt);
        Assert.Contains("Cashier", receipt);
        Assert.Contains("Maria", receipt);
        Assert.Contains("310.00", receipt);   // change: 500 - 190
    }
}
