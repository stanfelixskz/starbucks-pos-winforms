using POSInventorySystem.Models;

namespace POSInventorySystem.Data;

public static class DataStore
{
    // ==========================================
    // PRODUCTS
    // ==========================================

    public static List<Product> Products { get; } =
        new List<Product>
        {
            // ==================================
            // BEVERAGES
            // ==================================

            new BeverageProduct(
                "P001",
                "Caffe Latte",
                150.00m,
                10,
                "Beverage"
            ),

            new BeverageProduct(
                "P002",
                "Caramel Macchiato",
                170.00m,
                8,
                "Beverage"
            ),

            new BeverageProduct(
                "P003",
                "Java Chip Frappuccino",
                180.00m,
                7,
                "Beverage"
            ),

            new BeverageProduct(
                "P004",
                "Caffe Americano",
                135.00m,
                10,
                "Beverage"
            ),

            new BeverageProduct(
                "P005",
                "Mocha Frappuccino",
                175.00m,
                8,
                "Beverage"
            ),

            new BeverageProduct(
                "P006",
                "Iced Caffe Latte",
                155.00m,
                10,
                "Beverage"
            ),

            // ==================================
            // FOOD
            // ==================================

            new FoodProduct(
                "P007",
                "Chocolate Chip Cookie",
                95.00m,
                12,
                "Food"
            ),

            new FoodProduct(
                "P008",
                "Chocolate Croissant",
                120.00m,
                6,
                "Food"
            ),

            new FoodProduct(
                "P009",
                "Chicken Pesto Sandwich",
                165.00m,
                5,
                "Food"
            ),

            new FoodProduct(
                "P010",
                "Sausage Roll",
                110.00m,
                6,
                "Food"
            )
        };


    // ==========================================
    // INGREDIENTS
    // ==========================================

    public static List<Ingredient> Ingredients { get; } =
        new List<Ingredient>
        {
            // ==================================
            // BEVERAGE INGREDIENTS
            // ==================================

            new Ingredient(
                "I001",
                "Coffee Beans",
                5000,
                "g",
                1000
            ),

            new Ingredient(
                "I002",
                "Whole Milk",
                10000,
                "ml",
                2000
            ),

            new Ingredient(
                "I003",
                "Oat Milk",
                5000,
                "ml",
                1000
            ),

            new Ingredient(
                "I004",
                "Espresso",
                2000,
                "ml",
                500
            ),

            new Ingredient(
                "I005",
                "Caramel Sauce",
                2000,
                "ml",
                500
            ),

            new Ingredient(
                "I006",
                "Vanilla Syrup",
                2000,
                "ml",
                500
            ),

            new Ingredient(
                "I007",
                "Frappuccino Base",
                3000,
                "ml",
                750
            ),

            new Ingredient(
                "I008",
                "Ice",
                10000,
                "g",
                2000
            ),

            new Ingredient(
                "I009",
                "Chocolate Chips",
                2000,
                "g",
                500
            ),

            new Ingredient(
                "I010",
                "Whipped Cream",
                2000,
                "ml",
                500
            ),

            new Ingredient(
                "I011",
                "Mocha Sauce",
                2000,
                "ml",
                500
            ),

            new Ingredient(
                "I012",
                "Water",
                10000,
                "ml",
                2000
            ),

            // ==================================
            // FOOD INGREDIENTS
            // ==================================

            new Ingredient(
                "I013",
                "Bread",
                50,
                "pcs",
                10
            ),

            new Ingredient(
                "I014",
                "Chicken",
                5000,
                "g",
                1000
            ),

            new Ingredient(
                "I015",
                "Pesto",
                2000,
                "g",
                500
            ),

            new Ingredient(
                "I016",
                "Cheese",
                2000,
                "g",
                500
            ),

            new Ingredient(
                "I017",
                "Sausage",
                3000,
                "g",
                750
            ),

            new Ingredient(
                "I018",
                "Pastry",
                50,
                "pcs",
                10
            ),

            // ==================================
            // ADDITIONAL CUSTOMIZATION INGREDIENTS
            // ==================================

            new Ingredient(
                "I019",
                "Almond Milk",
                5000,
                "ml",
                1000
            ),

            new Ingredient(
                "I020",
                "Hazelnut Syrup",
                2000,
                "ml",
                500
            )
        };


    // ==========================================
    // GET INGREDIENT
    // ==========================================

    public static Ingredient? GetIngredient(
        string ingredientId)
    {
        return Ingredients.FirstOrDefault(
            ingredient =>
                ingredient.IngredientId.Equals(
                    ingredientId,
                    StringComparison.OrdinalIgnoreCase
                )
        );
    }


    // ==========================================
    // GET PRODUCT
    // ==========================================

    public static Product? GetProduct(
        string productId)
    {
        return Products.FirstOrDefault(
            product =>
                product.ProductId.Equals(
                    productId,
                    StringComparison.OrdinalIgnoreCase
                )
        );
    }


    // ==========================================
    // TRANSACTIONS
    // ==========================================

    public static List<Transaction> Transactions { get; } =
        new List<Transaction>();


    // ==========================================
    // USERS
    // ==========================================

    public static List<User> Users { get; } =
        new List<User>
        {
            new User(
                "admin",
                "admin123",
                "Administrator",
                "Admin"
            ),

            new User(
                "cashier",
                "cashier123",
                "Cashier",
                "Cashier"
            )
        };
}