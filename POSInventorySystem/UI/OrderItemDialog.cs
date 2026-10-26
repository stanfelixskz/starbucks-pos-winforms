using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// Step 2 + 3 of ordering: customize a drink (if it is one)
// and choose the quantity.
public class OrderItemDialog : Form
{
    private readonly Product product;
    private readonly bool isBeverage;

    private readonly List<Func<string>> getters = new();
    private readonly NumericUpDown quantityBox;
    private readonly Label priceLabel;

    private string size = "Tall";
    private string serving = "Hot";
    private string milk = "Whole Milk";
    private string shots = "0";
    private string syrup = "None";

    public int Quantity => (int)quantityBox.Value;

    public BeverageCustomization? Customization =>
        isBeverage ? BuildCustomization() : null;

    public OrderItemDialog(Product product)
    {
        this.product = product;
        isBeverage = product is BeverageProduct;

        Text = product.Name;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        // ------------------------------
        // Header
        // ------------------------------

        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = Theme.GreenDark
        };

        Label name = Theme.Label(
            product.Name, 16f, true, Color.White);
        name.Location = new Point(24, 14);

        Label price = Theme.Label(
            $"Base price ₱{product.Price:F2}", 10f, false, Theme.Gold);
        price.Location = new Point(26, 46);

        header.Controls.AddRange(new Control[] { name, price });

        // ------------------------------
        // Options
        // ------------------------------

        FlowLayoutPanel options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(20, 12, 20, 0)
        };

        if (isBeverage)
        {
            AddGroup(options, "SIZE",
                new[] { "Tall", "Grande  +₱10", "Venti  +₱20" },
                new[] { "Tall", "Grande", "Venti" },
                v => size = v);

            AddGroup(options, "SERVING",
                new[] { "Hot", "Iced", "Blended" },
                new[] { "Hot", "Iced", "Blended" },
                v => serving = v);

            AddGroup(options, "MILK",
                new[]
                {
                    "Whole Milk", "Soy Milk  +₱20", "Oat Milk  +₱20",
                    "Almond Milk  +₱20", "None"
                },
                new[]
                {
                    "Whole Milk", "Soy Milk", "Oat Milk",
                    "Almond Milk", "None"
                },
                v => milk = v);

            AddGroup(options, "EXTRA ESPRESSO SHOTS",
                new[] { "None", "+1  +₱30", "+2  +₱60", "+3  +₱90" },
                new[] { "0", "1", "2", "3" },
                v => shots = v);

            AddGroup(options, "SYRUP",
                new[]
                {
                    "None", "Vanilla  +₱15",
                    "Caramel  +₱15", "Hazelnut  +₱15"
                },
                new[] { "None", "Vanilla", "Caramel", "Hazelnut" },
                v => syrup = v);
        }

        // ------------------------------
        // Footer: quantity, price, buttons
        // ------------------------------

        Panel footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 130,
            BackColor = Color.White
        };

        Label qtyLabel = Theme.Label(
            "QUANTITY", 9f, true, Theme.Muted);
        qtyLabel.Location = new Point(24, 14);

        quantityBox = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 99,
            Value = 1,
            Width = 90,
            Font = Theme.Bold(14f),
            Location = new Point(24, 36)
        };
        quantityBox.ValueChanged += (_, _) => UpdatePrice();

        priceLabel = Theme.Label("", 12f, true, Theme.Green);
        priceLabel.Location = new Point(150, 40);

        RoundedButton add = Theme.PrimaryButton("Add to order");
        add.Size = new Size(200, 44);
        add.Location = new Point(24, 78);
        add.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        RoundedButton cancel = Theme.SecondaryButton("Cancel");
        cancel.Size = new Size(110, 44);
        cancel.Location = new Point(240, 78);
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.AddRange(new Control[]
        {
            qtyLabel, quantityBox, priceLabel, add, cancel
        });

        ClientSize = new Size(
            isBeverage ? 560 : 420,
            isBeverage ? 720 : 300);

        Controls.Add(options);
        Controls.Add(footer);
        Controls.Add(header);

        AcceptButton = add;
        CancelButton = cancel;

        UpdatePrice();
    }

    // Adds a titled row of radio buttons; the first one is selected.
    private void AddGroup(
        FlowLayoutPanel parent,
        string title,
        string[] labels,
        string[] values,
        Action<string> onChange)
    {
        Label heading = Theme.Label(title, 9f, true, Theme.Muted);
        heading.Margin = new Padding(0, 8, 0, 3);
        parent.Controls.Add(heading);

        FlowLayoutPanel row = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            Width = 560,
            Margin = new Padding(0)
        };

        for (int i = 0; i < labels.Length; i++)
        {
            string value = values[i];

            RadioButton radio = new RadioButton
            {
                Text = labels[i],
                Appearance = Appearance.Button,
                FlatStyle = FlatStyle.Flat,
                AutoSize = false,
                Size = new Size(104, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.Regular(9.5f),
                BackColor = Color.White,
                ForeColor = Theme.Text,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 4),
                Checked = i == 0
            };

            radio.FlatAppearance.BorderColor = Theme.Line;
            radio.FlatAppearance.CheckedBackColor = Theme.Green;
            radio.FlatAppearance.MouseOverBackColor =
                ColorTranslator.FromHtml("#D4E9E2");

            radio.CheckedChanged += (_, _) =>
            {
                radio.ForeColor =
                    radio.Checked ? Color.White : Theme.Text;

                if (radio.Checked)
                {
                    onChange(value);
                    UpdatePrice();
                }
            };

            if (radio.Checked)
            {
                radio.ForeColor = Color.White;
            }

            row.Controls.Add(radio);
        }

        parent.Controls.Add(row);
    }

    private BeverageCustomization BuildCustomization() =>
        new BeverageCustomization(
            size,
            serving,
            milk,
            int.Parse(shots),
            syrup);

    private void UpdatePrice()
    {
        if (priceLabel == null || quantityBox == null)
        {
            return;
        }

        decimal unit = product.Price;

        if (isBeverage)
        {
            unit += BuildCustomization().GetAdditionalPrice();
        }

        priceLabel.Text =
            $"₱{unit:F2} each   =   ₱{unit * Quantity:F2}";
    }
}
