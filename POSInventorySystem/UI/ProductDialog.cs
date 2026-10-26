using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// Add a new product, or edit the name, price and low-stock level of one.
public class ProductDialog : Form
{
    private readonly TextBox nameBox;
    private readonly TextBox priceBox;
    private readonly NumericUpDown stockBox;
    private readonly NumericUpDown lowBox;
    private readonly ComboBox typeBox;
    private readonly Label lowCaption;
    private readonly Label errorLabel;
    private readonly bool isNew;

    public string ItemName => nameBox.Text.Trim();
    public decimal Price { get; private set; }
    public int Stock => (int)stockBox.Value;
    public int LowStockLevel => (int)lowBox.Value;
    public bool IsBeverage =>
        isNew ? typeBox.SelectedIndex == 1 : existingIsBeverage;

    private readonly bool existingIsBeverage;

    public ProductDialog(string productId, Product? existing)
    {
        isNew = existing == null;
        existingIsBeverage = existing is BeverageProduct;

        Text = isNew ? "Add product" : "Edit product";
        ClientSize = new Size(440, isNew ? 560 : 440);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Theme.GreenDark
        };

        Label title = Theme.Label(
            isNew ? $"New product  {productId}" : $"Edit  {productId}",
            14f, true, Color.White);
        title.Location = new Point(22, 16);
        header.Controls.Add(title);
        Controls.Add(header);

        int y = 78;

        Controls.Add(Caption("NAME", 24, y));
        nameBox = Theme.TextBox(392);
        nameBox.Location = new Point(24, y + 22);
        nameBox.Text = existing?.Name ?? "";
        Controls.Add(nameBox);

        y += 70;

        Controls.Add(Caption("PRICE (₱)", 24, y));
        priceBox = Theme.TextBox(392);
        priceBox.Location = new Point(24, y + 22);
        priceBox.Text = existing?.Price.ToString("0.##") ?? "";
        Controls.Add(priceBox);

        y += 70;

        stockBox = new NumericUpDown
        {
            Maximum = 100000,
            Width = 170,
            Font = Theme.Regular(11f)
        };

        typeBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 170,
            Font = Theme.Regular(11f)
        };

        typeBox.Items.AddRange(new object[] { "Food", "Beverage" });
        typeBox.SelectedIndex = 0;

        if (isNew)
        {
            Controls.Add(Caption("TYPE", 24, y));
            typeBox.Location = new Point(24, y + 22);
            Controls.Add(typeBox);

            Controls.Add(Caption("STARTING STOCK (food, pieces)", 224, y));
            stockBox.Location = new Point(224, y + 22);
            Controls.Add(stockBox);

            y += 70;
        }

        // The low-stock level only matters for food (drinks are made to
        // order from ingredients)
        lowCaption = Caption("LOW STOCK AT (pieces, food only)", 24, y);

        lowBox = new NumericUpDown
        {
            Maximum = 100000,
            Value = existing is { } p ? Math.Min(100000, p.LowStockLevel) : 5,
            Width = 170,
            Font = Theme.Regular(11f),
            Location = new Point(24, y + 22)
        };

        Controls.AddRange(new Control[] { lowCaption, lowBox });

        typeBox.SelectedIndexChanged += (_, _) => SyncLowBox();
        SyncLowBox();

        errorLabel = Theme.Label("", 9.5f, false, Theme.Danger);
        errorLabel.AutoSize = false;
        errorLabel.Size = new Size(392, 30);
        errorLabel.Location = new Point(24, ClientSize.Height - 112);
        Controls.Add(errorLabel);

        RoundedButton save = Theme.PrimaryButton(
            isNew ? "Add product" : "Save changes");
        save.Size = new Size(200, 46);
        save.Location = new Point(24, ClientSize.Height - 70);
        save.Click += (_, _) => Save();

        RoundedButton cancel = Theme.SecondaryButton("Cancel");
        cancel.Size = new Size(110, 46);
        cancel.Location = new Point(240, ClientSize.Height - 70);
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.AddRange(new Control[] { save, cancel });

        AcceptButton = save;
        CancelButton = cancel;
    }

    private static Label Caption(string text, int x, int y)
    {
        Label label = Theme.Label(text, 9f, true, Theme.Muted);
        label.Location = new Point(x, y);
        return label;
    }

    private void SyncLowBox()
    {
        bool food = !IsBeverage;

        lowBox.Enabled = food;
        lowCaption.ForeColor = food ? Theme.Muted : Color.FromArgb(170, 170, 160);
    }

    private void Save()
    {
        if (ItemName.Length == 0)
        {
            errorLabel.Text = "Product name is required.";
            return;
        }

        if (!decimal.TryParse(priceBox.Text, out decimal price) ||
            price <= 0)
        {
            errorLabel.Text = "Enter a valid price.";
            return;
        }

        Price = price;

        DialogResult = DialogResult.OK;
        Close();
    }
}
