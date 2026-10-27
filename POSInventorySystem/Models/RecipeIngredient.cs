namespace POSInventorySystem.Models;

// One ingredient of a drink for one size.
public class RecipeIngredient
{
    public Ingredient Ingredient { get; set; }

    // Amount per serving, in the ingredient's own unit (g, ml or pcs)
    public decimal Amount { get; set; }

    public string Size { get; set; }

    // The same amount as a share of one pack. It follows the ingredient's
    // current pack size, so changing a pack size never changes how much a
    // drink really uses.
    public decimal QuantityRequired =>
        Ingredient.PackSize > 0 ? Amount / Ingredient.PackSize : 0m;

    public RecipeIngredient(
        Ingredient ingredient,
        decimal amount,
        string size = Recipe.DefaultSize)
    {
        Ingredient = ingredient;
        Amount = amount;
        Size = size;
    }
}


// A recipe line as it is stored and passed around before it is attached to
// the ingredient objects.
public record RecipeRow(string IngredientId, string Size, decimal Amount);
