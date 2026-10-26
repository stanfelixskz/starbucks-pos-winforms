namespace POSInventorySystem.Models;

public class Product
{
    public string ProductId { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public string Category { get; private set; }

    // At or below this many pieces a food item counts as low
    public int LowStockLevel { get; private set; } = 5;

    // Staff can stop selling an item by hand (for example a broken
    // machine), even when the ingredients are there
    public bool Unavailable { get; private set; }

    public Product(
        string productId,
        string name,
        decimal price,
        int stock,
        string category)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        Stock = stock;
        Category = category;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        Stock += quantity;
    }

    // Used by a stocktake: replaces the count with the counted number
    public void SetStock(int quantity)
    {
        Stock = Math.Max(0, quantity);
    }

    public void SetUnavailable(bool value)
    {
        Unavailable = value;
    }

    public void SetLowStockLevel(int level)
    {
        LowStockLevel = Math.Max(0, level);
    }

    public bool IsLowStock()
    {
        return Stock <= LowStockLevel;
    }
    public void UpdateProductInfo(
    string name,
    decimal price,
    string category)
{
    Name = name;
    Price = price;
    Category = category;
}
    public bool RemoveStock(int quantity)
{
    if (quantity <= 0 || quantity > Stock)
    {
        return false;
    }

    Stock -= quantity;
    return true;
}
}
