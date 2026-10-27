namespace POSInventorySystem.Models;

// One dated entry in the stock log: a restock (per pack / box / piece)
// or an automatic deduction caused by a completed sale.
public class StockRecord
{
    public DateTime Timestamp { get; private set; }
    public string ItemId { get; private set; }
    public string ItemName { get; private set; }

    // "RESTOCK", "SALE", "WASTE" or "ADJUST"
    public string Type { get; private set; }

    // How the stock was counted: "pack", "box" or "pc"
    public string Unit { get; private set; }

    // Number of packs / boxes / pieces entered
    public decimal Count { get; private set; }

    // Net change in the item's stock (in packs for ingredients,
    // in pieces for products). Negative for sales.
    public decimal StockChange { get; private set; }

    // Transaction ID for sales, or who/what for restocks
    public string Reference { get; private set; }

    // Reason for waste / adjustment, or supplier for a delivery
    public string Note { get; private set; }

    public void SetReference(string reference)
    {
        Reference = reference;
    }

    public StockRecord(
        DateTime timestamp,
        string itemId,
        string itemName,
        string type,
        string unit,
        decimal count,
        decimal stockChange,
        string reference,
        string note = "")
    {
        Timestamp = timestamp;
        ItemId = itemId;
        ItemName = itemName;
        Type = type;
        Unit = unit;
        Count = count;
        StockChange = stockChange;
        Reference = reference;
        Note = note;
    }
}
