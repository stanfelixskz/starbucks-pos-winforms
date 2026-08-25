namespace POSInventorySystem.Models;

public class Ingredient
{
    public string IngredientId { get; set; }
    public string Name { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; }

    // Amount at which the ingredient is considered low stock
    public decimal LowStockThreshold { get; set; }

    public Ingredient(
        string ingredientId,
        string name,
        decimal quantity,
        string unit,
        decimal lowStockThreshold)
    {
        IngredientId = ingredientId;
        Name = name;
        Quantity = quantity;
        Unit = unit;
        LowStockThreshold = lowStockThreshold;
    }

    public bool IsLowStock()
    {
        return Quantity <= LowStockThreshold;
    }
}