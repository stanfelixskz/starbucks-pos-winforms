namespace POSInventorySystem.Models;

public class Recipe
{
    public Product Product { get; set; }
    public List<RecipeIngredient> Ingredients { get; set; }

    public Recipe(Product product)
    {
        Product = product;
        Ingredients = new List<RecipeIngredient>();
    }

    public void AddIngredient(
        Ingredient ingredient,
        decimal quantityRequired)
    {
        Ingredients.Add(
            new RecipeIngredient(
                ingredient,
                quantityRequired
            )
        );
    }
}