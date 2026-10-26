using System.Text;
using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// What to order from the supplier: every ingredient at or below its
// reorder point, with enough packs to get back up to its par level.
public class OrderListDialog : Form
{
    private readonly List<Ingredient> items;

    public OrderListDialog()
    {
        items = DataStore.Ingredients
            .Where(i => i.IsLowStock())
            .OrderBy(i => i.Name)
            .ToList();

        Text = "Order list";
        ClientSize = new Size(760, 520);
        MinimumSize = new Size(680, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Theme.GreenDark
        };

        Label title = Theme.Label("Order list", 15f, true, Color.White);
        title.Location = new Point(22, 10);

        Label sub = Theme.Label(
            items.Count == 0
                ? "Nothing needs reordering right now."
                : $"{items.Count} item(s) at or below their reorder point, " +
                  "with packs to order up to par",
            9.5f, false, Theme.Gold);
        sub.Location = new Point(24, 40);

        header.Controls.AddRange(new Control[] { title, sub });

        DataGridView grid = new DataGridView { Dock = DockStyle.Fill };
        Theme.StyleGrid(grid);
        grid.Columns.Add("Item", "Item");
        grid.Columns.Add("Stock", "In stock");
        grid.Columns.Add("Low", "Reorder at");
        grid.Columns.Add("Par", "Par");
        grid.Columns.Add("Order", "Order");
        grid.Columns[0].FillWeight = 140;
        grid.Columns[1].FillWeight = 170;
        grid.Columns[2].FillWeight = 70;
        grid.Columns[3].FillWeight = 50;
        grid.Columns[4].FillWeight = 80;

        foreach (Ingredient i in items)
        {
            int row = grid.Rows.Add(
                i.Name,
                Ingredient.Packs(i.Quantity),
                $"{i.LowStockThreshold:0}",
                $"{i.ParLevel:0}",
                i.SuggestedOrder > 0
                    ? Ingredient.Packs(i.SuggestedOrder)
                    : "-");

            grid.Rows[row].DefaultCellStyle.BackColor =
                ColorTranslator.FromHtml("#FDEDEB");
        }

        Panel host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 0)
        };

        host.Controls.Add(grid);

        Panel footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64
        };

        Label copied = Theme.Label("", 9.5f, true, Theme.Green);
        copied.Location = new Point(300, 22);

        RoundedButton copy = Theme.PrimaryButton("Copy to clipboard");
        copy.Size = new Size(200, 42);
        copy.Location = new Point(20, 10);
        copy.Enabled = items.Count > 0;
        copy.Click += (_, _) =>
        {
            Clipboard.SetText(BuildText());
            copied.Text = "Copied. Paste it into a message or document.";
        };

        RoundedButton close = Theme.SecondaryButton("Close");
        close.Size = new Size(120, 42);
        close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        close.Click += (_, _) => Close();

        footer.Controls.AddRange(new Control[] { copy, copied, close });
        footer.Resize += (_, _) =>
            close.Location = new Point(footer.Width - close.Width - 20, 10);

        close.Location = new Point(footer.Width - close.Width - 20, 10);

        Controls.Add(host);
        Controls.Add(footer);
        Controls.Add(header);

        CancelButton = close;
    }

    // Plain text for the supplier
    private string BuildText()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"ORDER LIST - {DateTime.Now:MMM dd, yyyy}");
        sb.AppendLine(new string('-', 40));

        foreach (Ingredient i in items.Where(i => i.SuggestedOrder > 0))
        {
            sb.AppendLine($"{i.Name}: {Ingredient.Packs(i.SuggestedOrder)}");
        }

        return sb.ToString();
    }
}
