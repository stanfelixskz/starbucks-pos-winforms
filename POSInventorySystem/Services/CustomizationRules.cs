using POSInventorySystem.Data;

namespace POSInventorySystem.Services;

// What a drink's options use up. Everything that depends on an ingredient
// ID or an amount lives here, in one place, so changing it is one edit and
// a wrong ID is reported at startup instead of failing silently later.
public static class CustomizationRules
{
    // Packs used per serving
    public const decimal MilkPacksPerServing = 0.2m;
    public const decimal EspressoPacksPerShot = 0.03m;
    public const decimal SyrupPacksPerServing = 0.015m;

    // The recipes already include whole milk; it is skipped when the
    // customer picks a different milk
    public const string WholeMilkId = "I002";
    public const string EspressoId = "I004";

    // Option name -> ingredient ID
    public static readonly IReadOnlyDictionary<string, string> Milks =
        new Dictionary<string, string>
        {
            ["Whole Milk"] = "I002",
            ["Oat Milk"] = "I003",
            ["Almond Milk"] = "I019",
            ["Soy Milk"] = "I021"
        };

    public static readonly IReadOnlyDictionary<string, string> Syrups =
        new Dictionary<string, string>
        {
            ["Caramel"] = "I005",
            ["Vanilla"] = "I006",
            ["Hazelnut"] = "I020"
        };

    // Every ingredient ID above that is not in the ingredient list.
    // Empty when everything lines up.
    public static List<string> FindMissingIngredients()
    {
        return Milks.Values
            .Concat(Syrups.Values)
            .Append(EspressoId)
            .Append(WholeMilkId)
            .Distinct()
            .Where(id => DataStore.GetIngredient(id) == null)
            .ToList();
    }
}
