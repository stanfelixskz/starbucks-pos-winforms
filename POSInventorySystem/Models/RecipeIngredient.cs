namespace POSInventorySystem.Models;

public class RecipeIngredient
{
    public Ingredient Ingredient { get; set; }
    public decimal QuantityRequired { get; set; }

    public RecipeIngredient(
        Ingredient ingredient,
        decimal quantityRequired)
    {
        Ingredient = ingredient;
        QuantityRequired = quantityRequired;
    }
}