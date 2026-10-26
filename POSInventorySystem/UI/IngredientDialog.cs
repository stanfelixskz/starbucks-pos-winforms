using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// Add a new ingredient, or edit one: what a pack holds, the reorder
// point and the par level (how many packs to order back up to).
public class IngredientDialog : Form
{
    private readonly TextBox nameBox;
    private readonly ComboBox unitBox;
    private readonly NumericUpDown sizeBox;
    private readonly NumericUpDown lowBox;
    private readonly NumericUpDown parBox;
    private readonly NumericUpDown startBox;
    private readonly Label errorLabel;
    private readonly Label packLine;

    public string ItemName => nameBox.Text.Trim();
    public string Unit => unitBox.SelectedItem?.ToString() ?? "";
    public decimal PackSize => sizeBox.Value;
    public decimal LowAt => lowBox.Value;
    public decimal Par => parBox.Value;
    public decimal StartingPacks => startBox.Value;

    public IngredientDialog(string ingredientId, Ingredient? existing)
    {
        bool isNew = existing == null;

        Text = isNew ? "Add ingredient" : "Edit ingredient";
        ClientSize = new Size(440, isNew ? 610 : 530);
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
            isNew ? $"New ingredient  {ingredientId}" : $"Edit  {ingredientId}",
            14f, true, Color.White);
        title.Location = new Point(22, 16);
        header.Controls.Add(title);
        Controls.Add(header);

        int y = 78;

        // ---- Name ----
        Controls.Add(Caption("NAME", 24, y));

        nameBox = Theme.TextBox(392);
        nameBox.Location = new Point(24, y + 22);
        nameBox.Text = existing?.Name ?? "";
        Controls.Add(nameBox);

        y += 70;

        // ---- Unit and pack size ----
        Controls.Add(Caption("UNIT", 24, y));
        Controls.Add(Caption("ONE PACK HOLDS", 224, y));

        unitBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 170,
            Font = Theme.Regular(11f),
            Location = new Point(24, y + 22)
        };

        unitBox.Items.AddRange(new object[] { "g", "ml", "pcs" });
        unitBox.SelectedItem = existing?.Unit is { Length: > 0 }
            ? existing.Unit
            : "g";

        sizeBox = Number(existing?.PackSize ?? 1000, 224, y + 22, 0, 1000000);

        Controls.AddRange(new Control[] { unitBox, sizeBox });

        y += 70;

        packLine = Theme.Label("", 9.5f, false, Theme.Muted);
        packLine.Location = new Point(24, y - 14);
        Controls.Add(packLine);

        // ---- Reorder point and par level (in packs) ----
        Controls.Add(Caption("REORDER AT (packs)", 24, y + 8));
        Controls.Add(Caption("PAR LEVEL (packs)", 224, y + 8));

        lowBox = Number(Math.Ceiling(existing?.LowStockThreshold ?? 1), 24, y + 30, 0, 100000);
        parBox = Number(Math.Ceiling(existing?.ParLevel ?? 5), 224, y + 30, 0, 100000);

        Controls.AddRange(new Control[] { lowBox, parBox });

        Label help = Theme.Label(
            "Reorder at: when stock reaches this, it shows as low.\n" +
            "Par level: the order list suggests enough packs to get back to this.",
            9f, false, Theme.Muted);
        help.Location = new Point(24, y + 66);
        Controls.Add(help);

        y += 120;

        // ---- Starting stock (new only) ----
        startBox = Number(0, 24, y + 22, 0, 100000);

        if (isNew)
        {
            Controls.Add(Caption("STARTING STOCK (packs)", 24, y));
            Controls.Add(startBox);
            y += 70;
        }

        // ---- Error + buttons ----
        errorLabel = Theme.Label("", 9.5f, false, Theme.Danger);
        errorLabel.AutoSize = false;
        errorLabel.Size = new Size(392, 36);
        errorLabel.Location = new Point(24, ClientSize.Height - 120);
        Controls.Add(errorLabel);

        RoundedButton save = Theme.PrimaryButton(
            isNew ? "Add ingredient" : "Save changes");
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

        unitBox.SelectedIndexChanged += (_, _) => ShowPackLine();
        sizeBox.ValueChanged += (_, _) => ShowPackLine();
        ShowPackLine();
    }

    private static Label Caption(string text, int x, int y)
    {
        Label label = Theme.Label(text, 9f, true, Theme.Muted);
        label.Location = new Point(x, y);
        return label;
    }

    private static NumericUpDown Number(
        decimal value, int x, int y, int decimals, decimal max)
    {
        return new NumericUpDown
        {
            Minimum = 0,
            Maximum = max,
            DecimalPlaces = decimals,
            Value = Math.Min(max, value),
            Width = 170,
            Font = Theme.Regular(11f),
            Location = new Point(x, y)
        };
    }

    private void ShowPackLine()
    {
        packLine.Text = $"1 pack = {sizeBox.Value:#,0.##} {Unit}";
    }

    private void Save()
    {
        if (ItemName.Length == 0)
        {
            errorLabel.Text = "Ingredient name is required.";
            return;
        }

        if (PackSize <= 0)
        {
            errorLabel.Text = "Pack size must be greater than zero.";
            return;
        }

        if (Par < LowAt)
        {
            errorLabel.Text = "The par level can not be lower than the reorder level.";
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
