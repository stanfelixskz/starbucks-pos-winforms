using POSInventorySystem.Models;

namespace POSInventorySystem.Data;

public static class RecipeData
{
    public static List<Recipe> Recipes { get; } =
        new List<Recipe>();

    public static void Initialize()
    {
        // Prevent duplicate recipes
        if (Recipes.Count > 0)
        {
            return;
        }

        // ==========================================
        // COMMON INGREDIENTS
        // ==========================================

        Ingredient? coffeeBeans =
            DataStore.GetIngredient("I001");

        Ingredient? frappuccinoBase =
            DataStore.GetIngredient("I007");

        Ingredient? ice =
            DataStore.GetIngredient("I008");

        Ingredient? chocolateChips =
            DataStore.GetIngredient("I009");

        Ingredient? whippedCream =
            DataStore.GetIngredient("I010");

        Ingredient? mochaSauce =
            DataStore.GetIngredient("I011");

        Ingredient? water =
            DataStore.GetIngredient("I012");

        Ingredient? bread =
            DataStore.GetIngredient("I013");

        Ingredient? chicken =
            DataStore.GetIngredient("I014");

        Ingredient? pesto =
            DataStore.GetIngredient("I015");

        Ingredient? cheese =
            DataStore.GetIngredient("I016");

        Ingredient? sausage =
            DataStore.GetIngredient("I017");

        Ingredient? pastry =
            DataStore.GetIngredient("I018");


        // ==========================================
        // P001 - CAFFE LATTE
        // ==========================================

        Product? caffeLatte =
            DataStore.Products.FirstOrDefault(
                p => p.ProductId == "P01"
            );

        if (caffeLatte != null &&
            coffeeBeans != null)
        {
            Recipe recipe =
                new Recipe(caffeLatte);

            // Base ingredients only.
            // Milk is handled by customization.
            recipe.AddIngredient(
                coffeeBeans,
                0.018m
            );

            Recipes.Add(recipe);
        }


        // ==========================================
        // P002 - CARAMEL MACCHIATO
        // ==========================================

        Product? caramelMacchiato =
            DataStore.Products.FirstOrDefault(
                p => p.ProductId == "P02"
            );

        Ingredient? caramelSauce =
            DataStore.GetIngredient("I005");

        if (caramelMacchiato != null &&
            coffeeBeans != null &&
            caramelSauce != null)
        {
            Recipe recipe =
                new Recipe(caramelMacchiato);

            recipe.AddIngredient(
                coffeeBeans,
                0.018m
            );

            recipe.AddIngredient(
                caramelSauce,
                0.02m
            );

            Recipes.Add(recipe);
        }


        // ==========================================
        // P003 - JAVA CHIP FRAPPUCCINO
        // ==========================================

        Product? javaChip =
            DataStore.Products.FirstOrDefault(
                p => p.ProductId == "P03"
            );

        if (javaChip != null &&
            coffeeBeans != null &&
            frappuccinoBase != null &&
            ice != null &&
            chocolateChips != null &&
            whippedCream != null)
        {
            Recipe recipe =
                new Recipe(javaChip);

            recipe.AddIngredient(
                coffeeBeans,
                0.018m
            );

            recipe.AddIngredient(
                frappuccinoBase,
                0.15m
            );

            recipe.AddIngredient(
                ice,
                0.04m
            );

            recipe.AddIngredient(
                chocolateChips,
                0.03m
            );

            recipe.AddIngredient(
                whippedCream,
                0.04m
            );

            Recipes.Add(recipe);
        }


        // ==========================================
        // P004 - CAFFE AMERICANO
        // ==========================================

        Product? americano =
            DataStore.Products.FirstOrDefault(
                p => p.ProductId == "P04"
            );

        if (americano != null &&
            coffeeBeans != null &&
            water != null)
        {
            Recipe recipe =
                new Recipe(americano);

            recipe.AddIngredient(
                coffeeBeans,
                0.018m
            );

            recipe.AddIngredient(
                water,
                0.05m
            );

            Recipes.Add(recipe);
        }


        // ==========================================
        // P005 - MOCHA FRAPPUCCINO
        // ==========================================

        Product? mochaFrappuccino =
            DataStore.Products.FirstOrDefault(
                p => p.ProductId == "P05"
            );

        if (mochaFrappuccino != null &&
            coffeeBeans != null &&
            frappuccinoBase != null &&
            ice != null &&
            mochaSauce != null &&
            whippedCream != null)
        {
            Recipe recipe =
                new Recipe(mochaFrappuccino);

            recipe.AddIngredient(
                coffeeBeans,
                0.018m
            );

            recipe.AddIngredient(
                frappuccinoBase,
                0.15m
            );

            recipe.AddIngredient(
                ice,
                0.04m
            );

            recipe.AddIngredient(
                mochaSauce,
                0.025m
            );

            recipe.AddIngredient(
                whippedCream,
                0.04m
            );

            Recipes.Add(recipe);
        }


        // ==========================================
        // P006 - ICED CAFFE LATTE
        // ==========================================

        Product? icedLatte =
            DataStore.Products.FirstOrDefault(
                p => p.ProductId == "P06"
            );

        if (icedLatte != null &&
            coffeeBeans != null &&
            ice != null)
        {
            Recipe recipe =
                new Recipe(icedLatte);

            // Milk is handled by customization.
            recipe.AddIngredient(
                coffeeBeans,
                0.018m
            );

            recipe.AddIngredient(
                ice,
                0.03m
            );

            Recipes.Add(recipe);
        }


        // ==========================================
        // P007 - CHOCOLATE CHIP COOKIE
        // ==========================================

        // No recipe yet.
        // Uses finished-product stock.


        // ==========================================
        // P008 - CHOCOLATE CROISSANT
        // ==========================================

        // No recipe yet.
        // Uses finished-product stock.


        // ==========================================
        // P009 - CHICKEN PESTO SANDWICH
        // ==========================================

        Product? chickenPesto =
            DataStore.Products.FirstOrDefault(
                p => p.ProductId == "P09"
            );

        if (chickenPesto != null &&
            bread != null &&
            chicken != null &&
            pesto != null &&
            cheese != null)
        {
            Recipe recipe =
                new Recipe(chickenPesto);

            recipe.AddIngredient(
                bread,
                0.2m
            );

            recipe.AddIngredient(
                chicken,
                0.1m
            );

            recipe.AddIngredient(
                pesto,
                0.04m
            );

            recipe.AddIngredient(
                cheese,
                0.06m
            );

            Recipes.Add(recipe);
        }


        // ==========================================
        // P010 - SAUSAGE ROLL
        // ==========================================

        Product? sausageRoll =
            DataStore.Products.FirstOrDefault(
                p => p.ProductId == "P10"
            );

        if (sausageRoll != null &&
            pastry != null &&
            sausage != null)
        {
            Recipe recipe =
                new Recipe(sausageRoll);

            recipe.AddIngredient(
                pastry,
                0.1m
            );

            recipe.AddIngredient(
                sausage,
                0.1m
            );

            Recipes.Add(recipe);
        }
    }
}