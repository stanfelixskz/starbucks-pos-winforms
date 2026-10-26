namespace POSInventorySystem.Models;

public class CartItem
{
    public Product Product { get; private set; }

    public int Quantity { get; private set; }

    public BeverageCustomization? Customization { get; private set; }


    // ==========================================
    // UNIT PRICE
    // ==========================================

    // Set once the sale is completed (or loaded from the database)
    // so later price edits never change old orders.
    private decimal? fixedUnitPrice;

    public decimal UnitPrice
    {
        get
        {
            if (fixedUnitPrice.HasValue)
            {
                return fixedUnitPrice.Value;
            }

            decimal price = Product.Price;

            if (Customization != null)
            {
                price +=
                    Customization.GetAdditionalPrice();
            }

            return price;
        }
    }


    // ==========================================
    // SUBTOTAL
    // ==========================================

    public decimal Subtotal
    {
        get
        {
            return UnitPrice * Quantity;
        }
    }


    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public CartItem(
        Product product,
        int quantity,
        BeverageCustomization? customization = null)
    {
        Product = product;
        Quantity = quantity;
        Customization = customization;
    }


    // ==========================================
    // FREEZE PRICE
    // ==========================================

    public void FreezePrice()
    {
        fixedUnitPrice = UnitPrice;
    }

    public void FreezePrice(decimal unitPrice)
    {
        fixedUnitPrice = unitPrice;
    }


    // ==========================================
    // ADD QUANTITY
    // ==========================================

    public void AddQuantity(int amount)
    {
        if (Quantity + amount >= 1)
        {
            Quantity += amount;
        }
    }
}