namespace POSInventorySystem.Models;

public class CartItem
{
    public Product Product { get; private set; }

    public int Quantity { get; private set; }

    public BeverageCustomization? Customization { get; private set; }


    // ==========================================
    // UNIT PRICE
    // ==========================================

    public decimal UnitPrice
    {
        get
        {
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
    // DISPLAY CART ITEM
    // ==========================================

    public void DisplayCartItem()
    {
        Console.WriteLine(
            $"{Product.Name} x{Quantity} = ₱{Subtotal:F2}"
        );


        // ==========================================
        // FOOD ITEM
        // ==========================================

        if (Customization == null)
        {
            Console.WriteLine(
                $"  Unit Price: ₱{UnitPrice:F2}"
            );

            Console.WriteLine(
                $"  Quantity:   {Quantity}"
            );

            Console.WriteLine(
                $"  Subtotal:   ₱{Subtotal:F2}"
            );

            return;
        }


        // ==========================================
        // BEVERAGE CUSTOMIZATION
        // ==========================================

        Customization.DisplayCustomization();

        Console.WriteLine();

        Console.WriteLine(
            "Price Breakdown:"
        );


        // ==========================================
        // BASE PRICE
        // ==========================================

        Console.WriteLine(
            $"  {"Base Price:",-20}" +
            $"₱{Product.Price:F2}"
        );


        // ==========================================
        // SIZE PRICE
        // ==========================================

        decimal sizePrice = 0m;

        if (Customization.Size == "Grande")
        {
            sizePrice = 10m;
        }
        else if (Customization.Size == "Venti")
        {
            sizePrice = 20m;
        }

        Console.WriteLine(
            $"  {Customization.Size,-20}" +
            $"+₱{sizePrice:F2}"
        );


        // ==========================================
        // MILK PRICE
        // ==========================================

        decimal milkPrice = 0m;

        if (Customization.Milk == "Soy Milk" ||
            Customization.Milk == "Oat Milk" ||
            Customization.Milk == "Almond Milk")
        {
            milkPrice = 20m;
        }

        Console.WriteLine(
            $"  {Customization.Milk,-20}" +
            $"+₱{milkPrice:F2}"
        );


        // ==========================================
        // EXTRA SHOTS PRICE
        // ==========================================

        decimal shotsPrice =
            Customization.ExtraShots * 30m;

        Console.WriteLine(
            $"  {"Extra Shots:",-20}" +
            $"+₱{shotsPrice:F2}"
        );


        // ==========================================
        // SYRUP PRICE
        // ==========================================

        decimal syrupPrice = 0m;

        if (Customization.Syrup != "None")
        {
            syrupPrice = 15m;
        }

        Console.WriteLine(
            $"  {Customization.Syrup + " Syrup",-20}" +
            $"+₱{syrupPrice:F2}"
        );


        // ==========================================
        // TOTAL PRICE
        // ==========================================

        Console.WriteLine(
            "  --------------------"
        );

        Console.WriteLine(
            $"  {"Unit Price:",-20}" +
            $"₱{UnitPrice:F2}"
        );

        Console.WriteLine(
            $"  {"Quantity:",-20}" +
            $"{Quantity}"
        );

        Console.WriteLine(
            $"  {"Subtotal:",-20}" +
            $"₱{Subtotal:F2}"
        );
    }
}