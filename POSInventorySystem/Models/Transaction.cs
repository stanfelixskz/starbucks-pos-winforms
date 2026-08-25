using POSInventorySystem.Services;

namespace POSInventorySystem.Models;

public class Transaction
{
    private readonly List<CartItem> items =
        new List<CartItem>();

    private static int transactionCounter = 1;

    public string TransactionId { get; private set; }

    public DateTime Date { get; private set; }

    public IReadOnlyList<CartItem> Items => items;

    public Payment? Payment { get; private set; }

    public decimal Total
    {
        get
        {
            return items.Sum(item => item.Subtotal);
        }
    }

    public Transaction()
    {
        TransactionId = $"T{transactionCounter:D3}";
        transactionCounter++;

        Date = DateTime.Now;
    }

    // ==========================================
    // ADD ITEM
    // ==========================================

    public void AddItem(
        Product product,
        int quantity,
        BeverageCustomization? customization = null)
    {
        if (quantity <= 0)
        {
            Console.WriteLine("Invalid quantity.");
            return;
        }

        CartItem? existingItem =
            items.FirstOrDefault(
                item =>
                    item.Product.ProductId ==
                    product.ProductId &&
                    AreCustomizationsEqual(
                        item.Customization,
                        customization)
            );

        if (existingItem != null)
        {
            Console.WriteLine(
                "This exact product and customization " +
                "is already in the cart."
            );

            return;
        }

        items.Add(
            new CartItem(
                product,
                quantity,
                customization
            )
        );
    }

    // ==========================================
    // COMPARE CUSTOMIZATIONS
    // ==========================================

    private bool AreCustomizationsEqual(
        BeverageCustomization? first,
        BeverageCustomization? second)
    {
        if (first == null && second == null)
        {
            return true;
        }

        if (first == null || second == null)
        {
            return false;
        }

        return first.Size == second.Size &&
               first.Serving == second.Serving &&
               first.Milk == second.Milk &&
               first.ExtraShots == second.ExtraShots &&
               first.Syrup == second.Syrup;
    }

    // ==========================================
    // PROCESS PAYMENT
    // ==========================================

    public bool ProcessPayment(Payment payment)
    {
        if (!payment.ProcessPayment())
        {
            return false;
        }

        Payment = payment;

        return true;
    }

    // ==========================================
    // COMPLETE TRANSACTION
    // ==========================================

    public bool CompleteTransaction()
    {
        InventoryService inventoryService =
            new InventoryService();

        if (items.Count == 0)
        {
            Console.WriteLine(
                "Cannot complete an empty transaction."
            );

            return false;
        }

        // ==========================================
        // STEP 1: CHECK EVERYTHING FIRST
        // ==========================================

        foreach (CartItem item in items)
        {
            bool itemAvailable =
                inventoryService.IsProductAvailable(
                    item.Product,
                    item.Quantity,
                    item.Customization
                );

            if (!itemAvailable)
            {
                Console.WriteLine();
Console.WriteLine(
    $"Not enough stock for " +
    $"{item.Product.Name}."
);

                return false;
            }
        }

        // ==========================================
        // STEP 2: CONSUME INVENTORY
        // ==========================================

        foreach (CartItem item in items)
        {
            bool consumed =
                inventoryService.ConsumeProduct(
                    item.Product,
                    item.Quantity,
                    item.Customization
                );

            if (!consumed)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"Unable to process inventory for " +
                    $"{item.Product.Name}."
                );

                return false;
            }
        }

        return true;
    }

    // ==========================================
    // DISPLAY RECEIPT
    // ==========================================

    public void DisplayReceipt()
    {
        Console.WriteLine();

        Console.WriteLine(
            "========================================"
        );

        Console.WriteLine(
            "              STARBUCKS"
        );

        Console.WriteLine(
            "           SALES RECEIPT"
        );

        Console.WriteLine(
            "========================================"
        );

        Console.WriteLine(
            $"Transaction: {TransactionId}"
        );

        Console.WriteLine(
            $"Date: {Date:MMM dd, yyyy hh:mm tt}"
        );

        Console.WriteLine(
            "----------------------------------------"
        );

        foreach (CartItem item in items)
        {
            Console.WriteLine(
                $"{item.Product.ProductId}  " +
                $"{item.Product.Name}"
            );

            // ======================================
            // BEVERAGE CUSTOMIZATION
            // ======================================

            if (item.Customization != null)
            {
                BeverageCustomization customization =
                    item.Customization;

                Console.WriteLine(
                    $"      {customization.Size} / " +
                    $"{customization.Serving}"
                );

                Console.WriteLine(
                    $"      {customization.Milk}"
                );

                if (customization.ExtraShots > 0)
                {
                    Console.WriteLine(
                        $"      +{customization.ExtraShots} " +
                        $"Espresso Shot(s)"
                    );
                }
                else
                {
                    Console.WriteLine(
                        "      No Extra Shots"
                    );
                }

                if (customization.Syrup != "None")
                {
                    Console.WriteLine(
                        $"      {customization.Syrup} Syrup"
                    );
                }
                else
                {
                    Console.WriteLine(
                        "      No Syrup"
                    );
                }

                Console.WriteLine();

                // ==================================
                // BASE PRICE
                // ==================================

                Console.WriteLine(
                    $"      {"Base Price:",-20}" +
                    $"₱{item.Product.Price,8:F2}"
                );

                // ==================================
                // SIZE PRICE
                // ==================================

                decimal sizePrice = 0m;

                if (customization.Size == "Grande")
                {
                    sizePrice = 10m;
                }
                else if (customization.Size == "Venti")
                {
                    sizePrice = 20m;
                }

                Console.WriteLine(
                    $"      {customization.Size + ":",-20}" +
                    $"+₱{sizePrice,7:F2}"
                );

                // ==================================
                // MILK PRICE
                // ==================================

                decimal milkPrice = 0m;

                if (customization.Milk == "Soy Milk" ||
                    customization.Milk == "Oat Milk" ||
                    customization.Milk == "Almond Milk")
                {
                    milkPrice = 20m;
                }

                Console.WriteLine(
                    $"      {customization.Milk + ":",-20}" +
                    $"+₱{milkPrice,7:F2}"
                );

                // ==================================
                // ESPRESSO SHOT PRICE
                // ==================================

                decimal shotPrice =
                    customization.ExtraShots * 30m;

                Console.WriteLine(
                    $"      {"Espresso Shots:",-20}" +
                    $"+₱{shotPrice,7:F2}"
                );

                // ==================================
                // SYRUP PRICE
                // ==================================

                decimal syrupPrice = 0m;

                if (customization.Syrup != "None")
                {
                    syrupPrice = 15m;
                }

                Console.WriteLine(
                    $"      {customization.Syrup + " Syrup:",-20}" +
                    $"+₱{syrupPrice,7:F2}"
                );
            }
            else
            {
                // ==================================
                // FOOD ITEM
                // ==================================

                Console.WriteLine();

                Console.WriteLine(
                    $"      {"Base Price:",-20}" +
                    $"₱{item.Product.Price,8:F2}"
                );
            }

            Console.WriteLine();

            // ======================================
            // UNIT PRICE
            // ======================================

            Console.WriteLine(
                $"      {"Unit Price:",-20}" +
                $"₱{item.UnitPrice,8:F2}"
            );

            // ======================================
            // QUANTITY
            // ======================================

            Console.WriteLine(
                $"      {"Quantity:",-20}" +
                $"{item.Quantity,8}"
            );

            // ======================================
            // SUBTOTAL
            // ======================================

            Console.WriteLine(
                $"      {"Subtotal:",-20}" +
                $"₱{item.Subtotal,8:F2}"
            );

            Console.WriteLine();

            Console.WriteLine(
                "----------------------------------------"
            );
        }

        // ==========================================
        // TOTAL
        // ==========================================

        Console.WriteLine(
            $"TOTAL:                    ₱{Total:F2}"
        );

        // ==========================================
        // PAYMENT
        // ==========================================

        if (Payment != null)
        {
            Console.WriteLine(
                "----------------------------------------"
            );

            if (Payment is CashPayment cashPayment)
            {
                Console.WriteLine(
                    "Payment: Cash"
                );

                Console.WriteLine(
                    $"Amount Due:               " +
                    $"₱{cashPayment.Amount:F2}"
                );

                Console.WriteLine(
                    $"Cash Received:            " +
                    $"₱{cashPayment.CashReceived:F2}"
                );

                Console.WriteLine(
                    $"Change:                   " +
                    $"₱{cashPayment.Change:F2}"
                );
            }
            else if (Payment is GCashPayment)
            {
                Console.WriteLine(
                    "Payment: GCash"
                );

                Console.WriteLine(
                    $"Amount Paid:              " +
                    $"₱{Payment.Amount:F2}"
                );
            }
            else if (Payment is CardPayment)
            {
                Console.WriteLine(
                    "Payment: Card"
                );

                Console.WriteLine(
                    $"Amount Paid:              " +
                    $"₱{Payment.Amount:F2}"
                );
            }
        }

        Console.WriteLine(
            "----------------------------------------"
        );

        Console.WriteLine(
            "          Thank you for your"
        );

        Console.WriteLine(
            "             purchase!"
        );

        Console.WriteLine(
            "========================================"
        );
    }
}