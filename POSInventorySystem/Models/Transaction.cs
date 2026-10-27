using POSInventorySystem.Data;
using POSInventorySystem.Services;

namespace POSInventorySystem.Models;

public class Transaction
{
    private readonly List<CartItem> items =
        new List<CartItem>();

    private static int transactionCounter = 1;

    public string TransactionId { get; private set; }

    // When the order was started
    public DateTime Date { get; private set; }

    // When payment was taken and stock was deducted
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyList<CartItem> Items => items;

    public Payment? Payment { get; private set; }

    // Who rang up the order
    public string CashierName { get; set; } = "";

    public decimal Total
    {
        get
        {
            return items.Sum(item => item.Subtotal);
        }
    }

    // Used when loading a saved order from the database
    private Transaction(string transactionId, DateTime date)
    {
        TransactionId = transactionId;
        Date = date;
    }

    // Rebuilds a completed, saved order without using up an ID number
    public static Transaction Restore(
        string transactionId,
        DateTime orderedAt,
        DateTime? completedAt,
        string cashierName,
        IEnumerable<CartItem> savedItems,
        decimal? cashReceived)
    {
        Transaction transaction = new Transaction(transactionId, orderedAt);

        transaction.CompletedAt = completedAt;
        transaction.CashierName = cashierName;
        transaction.items.AddRange(savedItems);

        if (cashReceived.HasValue)
        {
            transaction.Payment = new CashPayment(
                transaction.Total, cashReceived.Value);
        }

        return transaction;
    }

    // Used when two terminals picked the same number: the order is
    // renumbered before it is saved
    public void AssignId(string transactionId)
    {
        TransactionId = transactionId;
    }

    // Continue numbering after the orders already saved
    public static void SetNextNumber(int number)
    {
        transactionCounter = Math.Max(1, number);
    }

    public Transaction()
    {
        TransactionId = $"T{transactionCounter:D2}";
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

        // Same product and customization: just raise the quantity
        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
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
    // QUANTITY ALREADY IN CART
    // ==========================================

    public int GetQuantityInCart(
        Product product,
        BeverageCustomization? customization = null)
    {
        CartItem? existingItem =
            items.FirstOrDefault(
                item =>
                    item.Product.ProductId ==
                    product.ProductId &&
                    AreCustomizationsEqual(
                        item.Customization,
                        customization)
            );

        return existingItem?.Quantity ?? 0;
    }

    // ==========================================
    // CAN THE WHOLE CART BE MADE?
    // ==========================================
    // Optionally includes one extra item that is
    // about to be added.
    // ==========================================

    public bool CanFulfill(
        Product? extraProduct = null,
        int extraQuantity = 0,
        BeverageCustomization? extraCustomization = null)
    {
        List<(Product, int, BeverageCustomization?)> order =
            items
                .Select(item => (
                    item.Product,
                    item.Quantity,
                    item.Customization))
                .ToList();

        if (extraProduct != null && extraQuantity > 0)
        {
            order.Add((
                extraProduct,
                extraQuantity,
                extraCustomization));
        }

        return new InventoryService().CanFulfillOrder(order);
    }

    // ==========================================
    // REMOVE ITEM
    // ==========================================

    public bool RemoveItem(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            return false;
        }

        items.RemoveAt(index);

        return true;
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
            return false;
        }

        // ==========================================
        // STEP 1: CHECK EVERYTHING FIRST
        // ==========================================

        // The whole cart is checked together because items
        // can share ingredients.
        if (!CanFulfill())
        {
            return false;
        }

        // ==========================================
        // STEP 2: CONSUME INVENTORY
        // ==========================================

        // Snapshot stock so the deductions can be logged afterwards
        Dictionary<string, decimal> ingredientsBefore =
            DataStore.Ingredients.ToDictionary(
                ingredient => ingredient.IngredientId,
                ingredient => ingredient.Quantity
            );

        Dictionary<string, int> productsBefore =
            DataStore.Products.ToDictionary(
                product => product.ProductId,
                product => product.Stock
            );

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
                return false;
            }
        }

        // ==========================================
        // STEP 3: RECORD TIME AND DEDUCTIONS
        // ==========================================

        CompletedAt = DateTime.Now;

        // Lock in the prices that were charged
        foreach (CartItem item in items)
        {
            item.FreezePrice();
        }

        foreach (Ingredient ingredient in DataStore.Ingredients)
        {
            decimal used =
                ingredientsBefore[ingredient.IngredientId] -
                ingredient.Quantity;

            if (used > 0)
            {
                DataStore.StockRecords.Add(
                    new StockRecord(
                        CompletedAt.Value,
                        ingredient.IngredientId,
                        ingredient.Name,
                        "SALE",
                        "pack",
                        used,
                        -used,
                        TransactionId
                    )
                );
            }
        }

        foreach (Product product in DataStore.Products)
        {
            int sold =
                productsBefore[product.ProductId] -
                product.Stock;

            if (sold > 0)
            {
                DataStore.StockRecords.Add(
                    new StockRecord(
                        CompletedAt.Value,
                        product.ProductId,
                        product.Name,
                        "SALE",
                        "pc",
                        sold,
                        -sold,
                        TransactionId
                    )
                );
            }
        }

        return true;
    }
}