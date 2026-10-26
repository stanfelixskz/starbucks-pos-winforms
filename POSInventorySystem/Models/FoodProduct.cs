namespace POSInventorySystem.Models;

public class FoodProduct : Product
{
    public FoodProduct(
        string productId,
        string name,
        decimal price,
        int stock,
        string category)
        : base(productId, name, price, stock, category)
    {
    }
}