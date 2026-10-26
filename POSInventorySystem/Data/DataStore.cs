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
                "P01",
                "Caffe Latte",
                150.00m,
                10,
                "Beverage"
            ),

            new BeverageProduct(
                "P02",
                "Caramel Macchiato",
                170.00m,
                8,
                "Beverage"
            ),

            new BeverageProduct(
                "P03",
                "Java Chip Frappuccino",
                180.00m,
                7,
                "Beverage"
            ),

            new BeverageProduct(
                "P04",
                "Caffe Americano",
                135.00m,
                10,
                "Beverage"
            ),

            new BeverageProduct(
                "P05",
                "Mocha Frappuccino",
                175.00m,
                8,
                "Beverage"
            ),

            new BeverageProduct(
                "P06",
                "Iced Caffe Latte",
                155.00m,
                10,
                "Beverage"
            ),

            // ==================================
            // FOOD
            // ==================================

            new FoodProduct(
                "P07",
                "Chocolate Chip Cookie",
                95.00m,
                12,
                "Food"
            ),

            new FoodProduct(
                "P08",
                "Chocolate Croissant",
                120.00m,
                6,
                "Food"
            ),

            new FoodProduct(
                "P09",
                "Chicken Pesto Sandwich",
                165.00m,
                5,
                "Food"
            ),

            new FoodProduct(
                "P10",
                "Sausage Roll",
                110.00m,
                6,
                "Food"
            )
        };


    // ==========================================
    // INGREDIENTS
    // ==========================================

    // Quantities are counted in packs. Pack size is noted beside each item.
    public static List<Ingredient> Ingredients { get; } =
        new List<Ingredient>
        {
            // ==================================
            // BEVERAGE INGREDIENTS
            // ==================================

            // 1 pack = 1000 g
            new Ingredient(
                "I001",
                "Coffee Beans",
                5,
                1m,
                "g",
                1000m,
                5m
            ),

            // 1 pack = 1000 ml
            new Ingredient(
                "I002",
                "Whole Milk",
                10,
                2m,
                "ml",
                1000m,
                10m
            ),

            // 1 pack = 1000 ml
            new Ingredient(
                "I003",
                "Oat Milk",
                5,
                1m,
                "ml",
                1000m,
                5m
            ),

            // 1 pack = 1000 ml
            new Ingredient(
                "I004",
                "Espresso",
                2,
                1m,
                "ml",
                1000m,
                2m
            ),

            // 1 pack = 1000 ml
            new Ingredient(
                "I005",
                "Caramel Sauce",
                2,
                1m,
                "ml",
                1000m,
                2m
            ),

            // 1 pack = 1000 ml
            new Ingredient(
                "I006",
                "Vanilla Syrup",
                2,
                1m,
                "ml",
                1000m,
                2m
            ),

            // 1 pack = 1000 ml
            new Ingredient(
                "I007",
                "Frappuccino Base",
                3,
                1m,
                "ml",
                1000m,
                3m
            ),

            // 1 pack = 5000 g
            new Ingredient(
                "I008",
                "Ice",
                2,
                1m,
                "g",
                5000m,
                2m
            ),

            // 1 pack = 1000 g
            new Ingredient(
                "I009",
                "Chocolate Chips",
                2,
                1m,
                "g",
                1000m,
                2m
            ),

            // 1 pack = 500 ml
            new Ingredient(
                "I010",
                "Whipped Cream",
                4,
                1m,
                "ml",
                500m,
                4m
            ),

            // 1 pack = 1000 ml
            new Ingredient(
                "I011",
                "Mocha Sauce",
                2,
                1m,
                "ml",
                1000m,
                2m
            ),

            // 1 pack = 5000 ml
            new Ingredient(
                "I012",
                "Water",
                2,
                1m,
                "ml",
                5000m,
                2m
            ),


            // ==================================
            // FOOD INGREDIENTS
            // ==================================

            // 1 pack = 10 pcs
            new Ingredient(
                "I013",
                "Bread",
                5,
                1m,
                "pcs",
                10m,
                5m
            ),

            // 1 pack = 1000 g
            new Ingredient(
                "I014",
                "Chicken",
                5,
                1m,
                "g",
                1000m,
                5m
            ),

            // 1 pack = 500 g
            new Ingredient(
                "I015",
                "Pesto",
                4,
                1m,
                "g",
                500m,
                4m
            ),

            // 1 pack = 500 g
            new Ingredient(
                "I016",
                "Cheese",
                4,
                1m,
                "g",
                500m,
                4m
            ),

            // 1 pack = 1000 g
            new Ingredient(
                "I017",
                "Sausage",
                3,
                1m,
                "g",
                1000m,
                3m
            ),

            // 1 pack = 10 pcs
            new Ingredient(
                "I018",
                "Pastry",
                5,
                1m,
                "pcs",
                10m,
                5m
            ),


            // ==================================
            // ADDITIONAL CUSTOMIZATION INGREDIENTS
            // ==================================

            // 1 pack = 1000 ml
            new Ingredient(
                "I019",
                "Almond Milk",
                5,
                1m,
                "ml",
                1000m,
                5m
            ),

            // 1 pack = 1000 ml
            new Ingredient(
                "I020",
                "Hazelnut Syrup",
                2,
                1m,
                "ml",
                1000m,
                2m
            ),

            // 1 pack = 1000 ml
            new Ingredient(
                "I021",
                "Soy Milk",
                5,
                1m,
                "ml",
                1000m,
                5m
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
    // STOCK RECORDS (restocks and sale deductions)
    // ==========================================

    public static List<StockRecord> StockRecords { get; } =
        new List<StockRecord>();


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
