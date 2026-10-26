namespace POSInventorySystem.Models;

public class RecipeIngredient
{
    public Ingredient Ingredient { get; set; }

    // Fraction of one pack used per serving (e.g. 0.018 = 18/1000 of a pack)
    public decimal QuantityRequired { get; set; }

    public RecipeIngredient(
        Ingredient ingredient,
        decimal quantityRequired)
    {
        Ingredient = ingredient;
        QuantityRequired = quantityRequired;
    }
}
