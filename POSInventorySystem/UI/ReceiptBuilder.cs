using System.Text;
using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// Builds a printed-style sales receipt with ITEM / QTY / PRICE / AMOUNT columns.
public static class ReceiptBuilder
{
    // Column widths (they add up to the receipt width)
    private const int Width = 44;
    private const int ItemCol = 24;
    private const int QtyCol = 4;
    private const int PriceCol = 8;
    private const int AmountCol = 8;

    public static string Build(Transaction transaction)
    {
        StringBuilder sb = new StringBuilder();
        string dash = new string('-', Width);
        string equal = new string('=', Width);

        // ---- Header ----
        sb.AppendLine(equal);
        sb.AppendLine(Center("STARBUCKS"));
        sb.AppendLine(Center("Sales Receipt"));
        sb.AppendLine(equal);

        sb.AppendLine(Pair("Receipt #", transaction.TransactionId));
        sb.AppendLine(Pair("Date", (transaction.CompletedAt ?? transaction.Date)
            .ToString("MMM dd, yyyy")));
        sb.AppendLine(Pair("Time", (transaction.CompletedAt ?? transaction.Date)
            .ToString("hh:mm:ss tt")));

        if (transaction.CashierName.Length > 0)
        {
            sb.AppendLine(Pair("Cashier", transaction.CashierName));
        }

        sb.AppendLine(dash);

        // ---- Column headings ----
        sb.AppendLine(
            Cell("ITEM", ItemCol, left: true) +
            Cell("QTY", QtyCol) +
            Cell("PRICE", PriceCol) +
            Cell("AMOUNT", AmountCol));

        sb.AppendLine(dash);

        // ---- Items ----
        foreach (CartItem item in transaction.Items)
        {
            // Name, then the customization on the lines below it
            sb.AppendLine(item.Product.Name);

            if (item.Customization != null)
            {
                foreach (string detail in CustomizationLines(item))
                {
                    sb.AppendLine("  " + detail);
                }
            }

            // Quantity, unit price and line amount on one row
            sb.AppendLine(
                Cell("", ItemCol) +
                Cell(item.Quantity.ToString(), QtyCol) +
                Cell(Money(item.UnitPrice), PriceCol) +
                Cell(Money(item.Subtotal), AmountCol));
        }

        sb.AppendLine(dash);

        // ---- Totals ----
        int count = transaction.Items.Sum(i => i.Quantity);

        sb.AppendLine($"Items ({count})");
        sb.AppendLine(Pair("TOTAL", Money(transaction.Total), bold: true));

        if (transaction.Payment is CashPayment cash)
        {
            sb.AppendLine(dash);
            sb.AppendLine(Pair("Cash", Money(cash.CashReceived)));
            sb.AppendLine(Pair("Change", Money(cash.Change)));
        }

        // ---- Footer ----
        sb.AppendLine(equal);
        sb.AppendLine(Center("Thank you for your purchase!"));
        sb.AppendLine(Center("Please come again."));
        sb.AppendLine(equal);

        return sb.ToString();
    }

    // Lines describing a drink's options, e.g. "Tall / Hot / Whole Milk"
    private static IEnumerable<string> CustomizationLines(CartItem item)
    {
        BeverageCustomization c = item.Customization!;

        yield return $"{c.Size} / {c.Serving} / {c.Milk}";

        if (c.ExtraShots > 0)
        {
            yield return $"+{c.ExtraShots} espresso shot(s)";
        }

        if (c.Syrup != "None")
        {
            yield return $"{c.Syrup} syrup";
        }
    }

    private static string Money(decimal amount) => $"{amount:N2}";

    // Right-aligned money or quantity inside a fixed-width cell
    private static string Cell(string text, int width, bool left = false)
    {
        text = Trim(text, width);

        return left
            ? text.PadRight(width)
            : text.PadLeft(width);
    }

    private static string Trim(string text, int max) =>
        text.Length <= max ? text : text.Substring(0, max);

    // "Label ........ value" across the full width
    private static string Pair(string label, string value, bool bold = false)
    {
        int gap = Math.Max(1, Width - label.Length - value.Length);

        string line = label + new string(' ', gap) + value;

        return bold ? line.ToUpperInvariant() : line;
    }

    private static string Center(string text)
    {
        int pad = Math.Max(0, (Width - text.Length) / 2);
        return new string(' ', pad) + text;
    }
}
