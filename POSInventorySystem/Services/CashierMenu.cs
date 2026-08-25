using POSInventorySystem.Data;
using POSInventorySystem.Models;
using POSInventorySystem.Services;

namespace POSInventorySystem;

public class CashierMenu
{
    public void Show()
    {
        InventoryService inventoryService =
            new InventoryService();

        while (true)
        {
            Transaction transaction =
                new Transaction();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("=================================");
                Console.WriteLine("      STARBUCKS POS SYSTEM");
                Console.WriteLine("=================================");
                Console.WriteLine("1. View Products");
                Console.WriteLine("2. Add Product to Cart");
                Console.WriteLine("3. View Cart");
                Console.WriteLine("4. Checkout");
                Console.WriteLine("5. Exit");
                Console.WriteLine("=================================");
                Console.WriteLine();

                Console.Write("Choose an option: ");

                string? choice =
                    Console.ReadLine();

                Console.WriteLine();


                // =================================
                // VIEW PRODUCTS
                // =================================

                if (choice == "1")
                {
                    Console.WriteLine(
                        "========================================"
                    );

                    Console.WriteLine(
                        "              PRODUCTS"
                    );

                    Console.WriteLine(
                        "========================================"
                    );


                    // =================================
                    // BEVERAGES
                    // =================================

                    Console.WriteLine();
                    Console.WriteLine(
                        "              BEVERAGES"
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    Console.WriteLine(
                        $"{"ID",-6} {"PRODUCT",-25} {"PRICE",10} {"STATUS",-15}"
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    foreach (Product product
                             in DataStore.Products)
                    {
                        if (product is BeverageProduct)
                        {
                            bool productAvailable =
                                inventoryService.IsProductAvailable(
                                    product,
                                    1
                                );

                            string productStatus =
                                productAvailable
                                    ? "AVAILABLE"
                                    : "UNAVAILABLE";

                            Console.WriteLine(
                                $"{product.ProductId,-6} " +
                                $"{product.Name,-25} " +
                                $"₱{product.Price,8:F2} " +
                                $"{productStatus,-15}"
                            );
                        }
                    }


                    // =================================
                    // FOOD
                    // =================================

                    Console.WriteLine();
                    Console.WriteLine(
                        "                 FOOD"
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    Console.WriteLine(
                        $"{"ID",-6} {"PRODUCT",-25} {"PRICE",10} {"STATUS",-15}"
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    foreach (Product product
                             in DataStore.Products)
                    {
                        if (product is FoodProduct)
                        {
                            bool productAvailable =
                                inventoryService.IsProductAvailable(
                                    product,
                                    1
                                );

                            string productStatus =
                                productAvailable
                                    ? "AVAILABLE"
                                    : "OUT OF STOCK";

                            Console.WriteLine(
                                $"{product.ProductId,-6} " +
                                $"{product.Name,-25} " +
                                $"₱{product.Price,8:F2} " +
                                $"{productStatus,-15}"
                            );
                        }
                    }

                    Console.WriteLine(
                        "========================================"
                    );
                }


                // =================================
                // ADD PRODUCT TO CART
                // =================================

                else if (choice == "2")
                {
                    Console.WriteLine(
                        "========================================"
                    );

                    Console.WriteLine(
                        "              ORDER MENU"
                    );

                    Console.WriteLine(
                        "========================================"
                    );


                    // =================================
                    // BEVERAGES
                    // =================================

                    Console.WriteLine();
                    Console.WriteLine(
                        "              BEVERAGES"
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    Console.WriteLine(
                        $"{"ID",-6} {"PRODUCT",-25} {"PRICE",10}"
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    foreach (Product menuProduct
                             in DataStore.Products)
                    {
                        if (menuProduct is BeverageProduct)
                        {
                            Console.WriteLine(
                                $"{menuProduct.ProductId,-6} " +
                                $"{menuProduct.Name,-25} " +
                                $"₱{menuProduct.Price,8:F2}"
                            );
                        }
                    }


                    // =================================
                    // FOOD
                    // =================================

                    Console.WriteLine();
                    Console.WriteLine(
                        "                 FOOD"
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    Console.WriteLine(
                        $"{"ID",-6} {"PRODUCT",-25} {"PRICE",10}"
                    );

                    Console.WriteLine(
                        "----------------------------------------"
                    );

                    foreach (Product menuProduct
                             in DataStore.Products)
                    {
                        if (menuProduct is FoodProduct)
                        {
                            Console.WriteLine(
                                $"{menuProduct.ProductId,-6} " +
                                $"{menuProduct.Name,-25} " +
                                $"₱{menuProduct.Price,8:F2}"
                            );
                        }
                    }

                    Console.WriteLine(
                        "========================================"
                    );


                    // =================================
                    // SELECT PRODUCT
                    // =================================

                    Console.WriteLine();

                    Console.Write(
                        "Enter Product ID: "
                    );

                    string? productId =
                        Console.ReadLine();

                    Product? product =
                        DataStore.Products.FirstOrDefault(
                            p =>
                                p.ProductId.Equals(
                                    productId,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        );

                    if (product == null)
                    {
                        Console.WriteLine(
                            "Product not found."
                        );

                        continue;
                    }


                    // =================================
                    // CHECK BASIC AVAILABILITY
                    // =================================

                    if (product is FoodProduct)
                    {
                        if (product.Stock <= 0)
                        {
                            Console.WriteLine();
                            Console.WriteLine(
                                "Sorry, this food item is out of stock."
                            );

                            continue;
                        }
                    }


                    // =================================
                    // CUSTOMIZATION
                    // =================================

                    BeverageCustomization? customization =
                        null;

                    if (product is BeverageProduct)
                    {
                        customization =
                            CreateBeverageCustomization();
                    }


                    // =================================
                    // QUANTITY
                    // =================================

                    Console.WriteLine();

                    Console.Write(
                        "Enter quantity: "
                    );

                    if (!int.TryParse(
                        Console.ReadLine(),
                        out int quantity))
                    {
                        Console.WriteLine(
                            "Invalid quantity."
                        );

                        continue;
                    }

                    if (quantity <= 0)
                    {
                        Console.WriteLine(
                            "Quantity must be greater than zero."
                        );

                        continue;
                    }


                    // =================================
                    // CHECK AVAILABILITY
                    // =================================

                    bool orderAvailable =
                        inventoryService.IsProductAvailable(
                            product,
                            quantity,
                            customization
                        );

                    if (!orderAvailable)
                    {
                        Console.WriteLine();

                        if (product is BeverageProduct)
                        {
                            Console.WriteLine(
                                "Sorry, there are not enough " +
                                "ingredients to prepare this beverage."
                            );
                        }
                        else
                        {
                            Console.WriteLine(
                                "Sorry, there is not enough " +
                                "stock for this product."
                            );
                        }

                        continue;
                    }


                    // =================================
                    // ADD TO CART
                    // =================================

                    transaction.AddItem(
                        product,
                        quantity,
                        customization
                    );

                    Console.WriteLine();

                    Console.WriteLine(
                        "Product added to cart."
                    );
                }


                // =================================
                // VIEW CART
                // =================================

                else if (choice == "3")
                {
                    Console.WriteLine(
                        "================================="
                    );

                    Console.WriteLine(
                        "          CURRENT CART"
                    );

                    Console.WriteLine(
                        "================================="
                    );

                    if (transaction.Items.Count == 0)
                    {
                        Console.WriteLine(
                            "Cart is empty."
                        );
                    }
                    else
                    {
                        foreach (CartItem item
                                 in transaction.Items)
                        {
                            item.DisplayCartItem();

                            Console.WriteLine();
                        }

                        Console.WriteLine(
                            $"TOTAL: ₱{transaction.Total:F2}"
                        );
                    }
                }


                // =================================
                // CHECKOUT
                // =================================

                else if (choice == "4")
                {
                    if (transaction.Items.Count == 0)
                    {
                        Console.WriteLine(
                            "Cart is empty."
                        );

                        continue;
                    }

                    Console.WriteLine(
                        "CHECKOUT"
                    );

                    Console.WriteLine(
                        "================================="
                    );

                    Console.WriteLine(
                        $"TRANSACTION ID: " +
                        $"{transaction.TransactionId}"
                    );

                    Console.WriteLine(
                        $"TOTAL: ₱{transaction.Total:F2}"
                    );

                    Console.WriteLine();


                    // =================================
                    // PAYMENT
                    // =================================

                    Console.WriteLine(
                        "Select Payment Method:"
                    );

                    Console.WriteLine(
                        "1. Cash"
                    );

                    Console.WriteLine(
                        "2. GCash"
                    );

                    Console.WriteLine(
                        "3. Card"
                    );

                    Console.WriteLine();

                    Console.Write(
                        "Choose payment method: "
                    );

                    string? paymentChoice =
                        Console.ReadLine();

                    Payment? payment =
                        null;

                    if (paymentChoice == "1")
                    {
                        Console.Write(
                            "Enter cash received: "
                        );

                        if (!decimal.TryParse(
                            Console.ReadLine(),
                            out decimal cashReceived))
                        {
                            Console.WriteLine(
                                "Invalid amount."
                            );

                            continue;
                        }

                        payment =
                            new CashPayment(
                                transaction.Total,
                                cashReceived
                            );
                    }
                    else if (paymentChoice == "2")
                    {
                        payment =
                            new GCashPayment(
                                transaction.Total
                            );
                    }
                    else if (paymentChoice == "3")
                    {
                        payment =
                            new CardPayment(
                                transaction.Total
                            );
                    }
                    else
                    {
                        Console.WriteLine(
                            "Invalid payment method."
                        );

                        continue;
                    }


                    // =================================
                    // PROCESS PAYMENT
                    // =================================

                    if (!transaction.ProcessPayment(
                        payment))
                    {
                        Console.WriteLine(
                            "Payment failed."
                        );

                        continue;
                    }


                    // =================================
                    // COMPLETE TRANSACTION
                    // =================================

                    if (!transaction.CompleteTransaction())
                    {
                        Console.WriteLine(
                            "Transaction failed."
                        );

                        continue;
                    }


                    // =================================
                    // SAVE TRANSACTION
                    // =================================

                    DataStore.Transactions.Add(
                        transaction
                    );

                    Console.WriteLine();

                    Console.WriteLine(
                        "Transaction completed successfully!"
                    );

                    Console.WriteLine();

                    transaction.DisplayReceipt();

                    Console.WriteLine();

                    Console.WriteLine(
                        "Updated Inventory:"
                    );


                    // =================================
                    // DISPLAY UPDATED INVENTORY
                    // =================================

                    foreach (CartItem item
                             in transaction.Items)
                    {
                        Recipe? recipe =
                            RecipeData.Recipes.FirstOrDefault(
                                r =>
                                    r.Product.ProductId ==
                                    item.Product.ProductId
                            );

                        if (recipe != null)
                        {
                            Console.WriteLine(
                                $"{item.Product.Name}: " +
                                "Made to order"
                            );

                            foreach (
                                RecipeIngredient recipeIngredient
                                in recipe.Ingredients)
                            {
                                Console.WriteLine(
                                    $"   " +
                                    $"{recipeIngredient.Ingredient.Name}: " +
                                    $"{recipeIngredient.Ingredient.Quantity:F2} " +
                                    $"{recipeIngredient.Ingredient.Unit}"
                                );
                            }
                        }
                        else
                        {
                            Console.WriteLine(
                                $"{item.Product.Name}: " +
                                $"{item.Product.Stock} remaining"
                            );
                        }
                    }

                    Console.WriteLine();

                    Console.WriteLine(
                        "Starting new transaction..."
                    );

                    break;
                }


                // =================================
                // EXIT
                // =================================

                else if (choice == "5")
                {
                    Console.WriteLine(
                        "Thank you for using the POS."
                    );

                    return;
                }

                else
                {
                    Console.WriteLine(
                        "Invalid option."
                    );
                }
            }
        }
    }


    // =================================
    // BEVERAGE CUSTOMIZATION
    // =================================

    private BeverageCustomization CreateBeverageCustomization()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("        BEVERAGE CUSTOMIZATION");
        Console.WriteLine("========================================");

        string size;

        // =================================
        // SIZE
        // =================================

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("SIZE");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("1. Tall");
            Console.WriteLine("2. Grande       (+₱10)");
            Console.WriteLine("3. Venti        (+₱20)");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine();
            Console.Write("Choose size: ");

            string? choice =
                Console.ReadLine();

            if (choice == "1")
            {
                size = "Tall";
                break;
            }

            if (choice == "2")
            {
                size = "Grande";
                break;
            }

            if (choice == "3")
            {
                size = "Venti";
                break;
            }

            Console.WriteLine(
                "Invalid size. Please try again."
            );
        }


        // =================================
        // SERVING
        // =================================

        string serving;

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("SERVING");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("1. Hot");
            Console.WriteLine("2. Iced");
            Console.WriteLine("3. Blended");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine();
            Console.Write("Choose serving: ");

            string? choice =
                Console.ReadLine();

            if (choice == "1")
            {
                serving = "Hot";
                break;
            }

            if (choice == "2")
            {
                serving = "Iced";
                break;
            }

            if (choice == "3")
            {
                serving = "Blended";
                break;
            }

            Console.WriteLine(
                "Invalid serving selection. Please try again."
            );
        }


        // =================================
        // MILK
        // =================================

        string milk;

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("MILK");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("1. Whole Milk");
            Console.WriteLine("2. Soy Milk       (+₱20)");
            Console.WriteLine("3. Oat Milk       (+₱20)");
            Console.WriteLine("4. Almond Milk    (+₱20)");
            Console.WriteLine("5. None");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine();
            Console.Write("Choose milk: ");

            string? choice =
                Console.ReadLine();

            if (choice == "1")
            {
                milk = "Whole Milk";
                break;
            }

            if (choice == "2")
            {
                milk = "Soy Milk";
                break;
            }

            if (choice == "3")
            {
                milk = "Oat Milk";
                break;
            }

            if (choice == "4")
            {
                milk = "Almond Milk";
                break;
            }

            if (choice == "5")
            {
                milk = "None";
                break;
            }

            Console.WriteLine(
                "Invalid milk selection. Please try again."
            );
        }


        // =================================
        // EXTRA ESPRESSO SHOTS
        // =================================

        int extraShots;

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("EXTRA ESPRESSO SHOTS");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("1. None");
            Console.WriteLine("2. +1 Shot        (+₱30)");
            Console.WriteLine("3. +2 Shots       (+₱60)");
            Console.WriteLine("4. +3 Shots       (+₱90)");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine();
            Console.Write("Choose extra shots: ");

            string? choice =
                Console.ReadLine();

            if (choice == "1")
            {
                extraShots = 0;
                break;
            }

            if (choice == "2")
            {
                extraShots = 1;
                break;
            }

            if (choice == "3")
            {
                extraShots = 2;
                break;
            }

            if (choice == "4")
            {
                extraShots = 3;
                break;
            }

            Console.WriteLine(
                "Invalid extra shot selection. Please try again."
            );
        }


        // =================================
        // SYRUP
        // =================================

        string syrup;

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("SYRUP");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("1. None");
            Console.WriteLine("2. Vanilla        (+₱15)");
            Console.WriteLine("3. Caramel        (+₱15)");
            Console.WriteLine("4. Hazelnut       (+₱15)");
            Console.WriteLine("----------------------------------------");

            Console.WriteLine();
            Console.Write("Choose syrup: ");

            string? choice =
                Console.ReadLine();

            if (choice == "1")
            {
                syrup = "None";
                break;
            }

            if (choice == "2")
            {
                syrup = "Vanilla";
                break;
            }

            if (choice == "3")
            {
                syrup = "Caramel";
                break;
            }

            if (choice == "4")
            {
                syrup = "Hazelnut";
                break;
            }

            Console.WriteLine(
                "Invalid syrup selection. Please try again."
            );
        }


        // =================================
        // COMPLETED
        // =================================

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("       CUSTOMIZATION COMPLETED");
        Console.WriteLine("========================================");

        return new BeverageCustomization(
            size,
            serving,
            milk,
            extraShots,
            syrup
        );
    }
}