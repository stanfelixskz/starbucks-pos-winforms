namespace POSInventorySystem.Models;

public class Ingredient
{
    public string IngredientId { get; set; }
    public string Name { get; set; }

    // Stock on hand, counted in packs.
    // Recipes use a fraction of a pack per serving.
    public decimal Quantity { get; set; }

    // Reorder point: at or below this many packs the item is low
    public decimal LowStockThreshold { get; set; }

    // What one pack holds, e.g. 1000 "g" or 1000 "ml" or 10 "pcs"
    public string Unit { get; set; }
    public decimal PackSize { get; set; }

    // Par level: the number of packs to order back up to
    public decimal ParLevel { get; set; }

    public Ingredient(
        string ingredientId,
        string name,
        decimal quantity,
        decimal lowStockThreshold,
        string unit = "",
        decimal packSize = 1,
        decimal parLevel = 0)
    {
        IngredientId = ingredientId;
        Name = name;
        Quantity = quantity;
        LowStockThreshold = lowStockThreshold;
        Unit = unit;
        PackSize = packSize;
        ParLevel = parLevel;
    }

    public bool IsLowStock()
    {
        return Quantity <= LowStockThreshold;
    }

    // Stock is shown in whole packs. A pack that is in use counts as one,
    // so it reads 0 only when nothing is left.
    public static string Packs(decimal packs)
    {
        decimal whole = Math.Ceiling(packs);

        return whole == 1 ? "1 pack" : $"{whole:0} packs";
    }

    // Packs to order to get back up to the par level (whole packs)
    public decimal SuggestedOrder =>
        ParLevel > Quantity ? Math.Ceiling(ParLevel - Quantity) : 0;
}
