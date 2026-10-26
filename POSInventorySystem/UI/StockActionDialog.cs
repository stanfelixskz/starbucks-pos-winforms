namespace POSInventorySystem.UI;

public enum StockAction
{
    Receive,
    Waste,
    Adjust
}

// One popup for Receive (delivery in), Waste (thrown away) and
// Adjust (stocktake count). Only the fields that action needs are shown.
public class StockActionDialog : Form
{
    private static readonly string[] WasteReasons =
    {
        "Spoiled", "Expired", "Broken or dropped", "Other"
    };

    private static readonly string[] AdjustReasons =
    {
        "Stocktake count", "Miscount corrected", "Other"
    };

    private readonly StockAction action;
    private readonly bool isIngredient;
    private readonly decimal current;
    private readonly string unitName;

    private readonly RadioButton packRadio;
    private readonly RadioButton boxRadio;
    private readonly NumericUpDown packsPerBox;
    private readonly NumericUpDown countBox;
    private readonly ComboBox reasonBox;
    private readonly TextBox noteBox;
    private readonly Label preview;
    private readonly RoundedButton confirm;

    public StockAction Action => action;

    // "pack", "box" or "pc"
    public string Unit =>
        !isIngredient ? "pc" : boxRadio.Checked ? "box" : "pack";

    // Quantity for receive / waste, or the counted total for adjust
    public int Count => (int)countBox.Value;

    public int PacksPerBox => (int)packsPerBox.Value;

    public string Reason => reasonBox.Visible
        ? reasonBox.SelectedItem?.ToString() ?? ""
        : "";

    // Supplier for a delivery, or extra detail for waste / adjust
    public string Note => noteBox.Text.Trim();

    public StockActionDialog(
        StockAction action,
        string itemId,
        string itemName,
        decimal currentStock,
        bool isIngredient,
        string? stockText = null)
    {
        this.action = action;
        this.isIngredient = isIngredient;
        current = currentStock;
        unitName = isIngredient ? "packs" : "pcs";

        Text = action switch
        {
            StockAction.Receive => "Receive stock",
            StockAction.Waste => "Record waste",
            _ => "Stocktake adjustment"
        };

        ClientSize = new Size(440, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        // ---- Header ----
        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Theme.GreenDark
        };

        Label title = Theme.Label(itemName, 15f, true, Color.White);
        title.Location = new Point(22, 12);

        Label sub = Theme.Label(
            $"{itemId}   ·   in stock: " +
            (stockText ?? $"{currentStock:0.##} {unitName}"),
            9.5f, false, Theme.Gold);
        sub.Location = new Point(24, 46);

        header.Controls.AddRange(new Control[] { title, sub });
        Controls.Add(header);

        int y = 96;

        // ---- Pack / box (ingredients receive and waste in packs or boxes) ----
        packRadio = new RadioButton
        {
            Text = "Pack",
            Checked = true,
            Font = Theme.Regular(11f),
            AutoSize = true,
            Location = new Point(24, y + 22)
        };

        boxRadio = new RadioButton
        {
            Text = "Box",
            Font = Theme.Regular(11f),
            AutoSize = true,
            Location = new Point(120, y + 22)
        };

        packsPerBox = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 1000,
            Value = 10,
            Width = 120,
            Font = Theme.Regular(11f),
            Location = new Point(24, y + 90),
            Enabled = false
        };

        bool usesBoxes = isIngredient && action != StockAction.Adjust;

        if (usesBoxes)
        {
            Controls.AddRange(new Control[]
            {
                Caption("COUNTED IN", 24, y),
                packRadio,
                boxRadio,
                Caption("PACKS IN ONE BOX", 24, y + 68),
                packsPerBox
            });

            y += 144;
        }

        // ---- Quantity ----
        string quantityLabel = action switch
        {
            StockAction.Receive => isIngredient
                ? "NUMBER RECEIVED"
                : "PIECES RECEIVED",
            StockAction.Waste => isIngredient
                ? "AMOUNT WASTED"
                : "PIECES WASTED",
            _ => isIngredient
                ? "COUNTED NOW (packs)"
                : "COUNTED NOW (pcs)"
        };

        Controls.Add(Caption(quantityLabel, 24, y));

        countBox = new NumericUpDown
        {
            Minimum = action == StockAction.Adjust ? 0 : 1,
            Maximum = 100000,
            Value = action == StockAction.Adjust
                ? Math.Max(0, Math.Min(100000, Math.Ceiling(currentStock)))
                : 1,
            DecimalPlaces = 0,
            Width = 160,
            Font = Theme.Bold(13f),
            Location = new Point(24, y + 22)
        };

        Controls.Add(countBox);

        y += 70;

        // ---- Reason (waste and adjust) ----
        reasonBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 392,
            Font = Theme.Regular(11f),
            Location = new Point(24, y + 22)
        };

        if (action != StockAction.Receive)
        {
            reasonBox.Items.AddRange(
                action == StockAction.Waste ? WasteReasons : AdjustReasons);
            reasonBox.SelectedIndex = 0;

            Controls.AddRange(new Control[]
            {
                Caption("REASON (required)", 24, y),
                reasonBox
            });

            y += 70;
        }
        else
        {
            reasonBox.Visible = false;
        }

        // ---- Note (supplier for receive, detail otherwise) ----
        noteBox = Theme.TextBox(392);
        noteBox.Location = new Point(24, y + 22);
        noteBox.PlaceholderText = action switch
        {
            StockAction.Receive => "e.g. Supplier name or delivery note no.",
            StockAction.Waste => "Optional detail",
            _ => "Optional detail"
        };

        Controls.AddRange(new Control[]
        {
            Caption(
                action == StockAction.Receive ? "SUPPLIER / DELIVERY NOTE" : "NOTE",
                24, y),
            noteBox
        });

        // ---- Preview line ----
        preview = Theme.Label("", 11f, true, Theme.Green);
        preview.Location = new Point(24, ClientSize.Height - 150);
        Controls.Add(preview);

        // ---- Buttons ----
        confirm = Theme.PrimaryButton(action switch
        {
            StockAction.Receive => "Receive",
            StockAction.Waste => "Record waste",
            _ => "Save count"
        });
        confirm.Size = new Size(200, 46);
        confirm.Location = new Point(24, ClientSize.Height - 70);
        confirm.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        RoundedButton cancel = Theme.SecondaryButton("Cancel");
        cancel.Size = new Size(110, 46);
        cancel.Location = new Point(240, ClientSize.Height - 70);
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.AddRange(new Control[] { confirm, cancel });

        AcceptButton = confirm;
        CancelButton = cancel;

        packRadio.CheckedChanged += (_, _) => RefreshPreview();
        boxRadio.CheckedChanged += (_, _) => RefreshPreview();
        packsPerBox.ValueChanged += (_, _) => RefreshPreview();
        countBox.ValueChanged += (_, _) => RefreshPreview();
        reasonBox.SelectedIndexChanged += (_, _) => RefreshPreview();

        RefreshPreview();

        Shown += (_, _) => countBox.Focus();
    }

    private static Label Caption(string text, int x, int y)
    {
        Label label = Theme.Label(text, 9f, true, Theme.Muted);
        label.Location = new Point(x, y);
        return label;
    }

    private void RefreshPreview()
    {
        bool boxes = isIngredient && action != StockAction.Adjust && boxRadio.Checked;
        packsPerBox.Enabled = boxes;

        // Waste and adjust can not take more than there is
        decimal entered = boxes
            ? countBox.Value * packsPerBox.Value
            : countBox.Value;

        // Ingredient stock is shown in whole packs
        string F(decimal v) =>
            isIngredient ? Math.Ceiling(v).ToString("0") : v.ToString("0.##");

        string line;
        bool valid = true;

        switch (action)
        {
            case StockAction.Receive:
                line = $"Adds {F(entered)} {unitName}   →   " +
                       $"{F(current + entered)} {unitName}";
                break;

            case StockAction.Waste:
                valid = entered <= current;
                line = valid
                    ? $"Removes {F(entered)} {unitName}   →   " +
                      $"{F(current - entered)} {unitName}"
                    : $"Only {F(current)} {unitName} in stock";
                valid = valid && reasonBox.SelectedIndex >= 0;
                break;

            default:
                decimal change = countBox.Value - (isIngredient ? Math.Ceiling(current) : current);
                line = change == 0
                    ? "No change from the system count"
                    : $"Changes by {(change > 0 ? "+" : "")}{change:0.##} {unitName}";
                valid = reasonBox.SelectedIndex >= 0;
                break;
        }

        preview.Text = line;
        preview.ForeColor = valid ? Theme.Green : Theme.Danger;
        confirm.Enabled = valid && (action == StockAction.Adjust || countBox.Value > 0);
    }
}
