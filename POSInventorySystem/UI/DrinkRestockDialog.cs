using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// A drink has no stock of its own, so restocking it means restocking the
// ingredients that are holding it back. This lists those ingredients with a
// box for how many packs arrived.
public class DrinkRestockDialog : Form
{
    private readonly List<(Ingredient Ingredient, NumericUpDown Packs)> rows = new();

    // Ingredients with at least one pack entered
    public IEnumerable<(Ingredient Ingredient, int Packs)> Received =>
        rows.Select(r => (r.Ingredient, (int)r.Packs.Value))
            .Where(r => r.Item2 > 0);

    public DrinkRestockDialog(
        string drinkName,
        IReadOnlyList<Ingredient> holdingBack)
    {
        Text = "Restock ingredients";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        int height = 200 + holdingBack.Count * 62;
        ClientSize = new Size(480, Math.Min(height, 620));

        // ---- Header ----
        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 84,
            BackColor = Theme.GreenDark
        };

        Label title = Theme.Label(drinkName, 15f, true, Color.White);
        title.Location = new Point(22, 12);

        Label sub = Theme.Label(
            "Not available  ·  restock the ingredients holding it back",
            9.5f, false, Theme.Gold);
        sub.Location = new Point(24, 48);

        header.Controls.AddRange(new Control[] { title, sub });

        // ---- One row per ingredient ----
        Panel list = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(24, 14, 24, 0)
        };

        Label caption = Theme.Label(
            "PACKS RECEIVED", 9f, true, Theme.Muted);
        caption.Location = new Point(344, 10);
        list.Controls.Add(caption);

        int y = 36;

        foreach (Ingredient ingredient in holdingBack)
        {
            Label name = Theme.Label(
                ingredient.Name, 11.5f, true);
            name.Location = new Point(24, y);

            Label stock = Theme.Label(
                $"In stock: {Ingredient.Packs(ingredient.Quantity)}   ·   " +
                $"par {ingredient.ParLevel:0}",
                9.5f, false, Theme.Muted);
            stock.Location = new Point(24, y + 24);

            NumericUpDown packs = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100000,
                DecimalPlaces = 0,
                Value = Math.Max(1, Math.Min(100000, ingredient.SuggestedOrder)),
                Width = 90,
                Font = Theme.Bold(12f),
                Location = new Point(344, y + 4)
            };

            list.Controls.AddRange(new Control[] { name, stock, packs });
            rows.Add((ingredient, packs));

            y += 62;
        }

        Controls.Add(list);

        // ---- Buttons ----
        Panel footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80
        };

        RoundedButton receive = Theme.PrimaryButton("Receive");
        receive.Size = new Size(200, 46);
        receive.Location = new Point(24, 16);
        receive.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        RoundedButton cancel = Theme.SecondaryButton("Cancel");
        cancel.Size = new Size(110, 46);
        cancel.Location = new Point(240, 16);
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.AddRange(new Control[] { receive, cancel });

        // The filling list goes in first so the header and footer frame it
        Controls.Add(footer);
        Controls.Add(header);

        AcceptButton = receive;
        CancelButton = cancel;
    }
}
