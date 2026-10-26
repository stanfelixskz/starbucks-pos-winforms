namespace POSInventorySystem.UI;

// Cash payment: type the cash received, see the change live.
public class PaymentDialog : Form
{
    private readonly decimal total;
    private readonly TextBox cashBox;
    private readonly Label changeLabel;
    private readonly Label messageLabel;
    private readonly RoundedButton confirm;

    public decimal CashReceived { get; private set; }

    public PaymentDialog(decimal total)
    {
        this.total = total;

        Text = "Cash payment";
        ClientSize = new Size(440, 470);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Theme.GreenDark
        };

        Label due = Theme.Label(
            "AMOUNT DUE", 9.5f, true, Theme.Gold);
        due.Location = new Point(24, 18);

        Label amount = Theme.Label(
            $"₱{total:F2}", 30f, true, Color.White);
        amount.Location = new Point(22, 40);

        header.Controls.AddRange(new Control[] { due, amount });

        Label cashLabel = Theme.Label(
            "CASH RECEIVED", 9f, true, Theme.Muted);
        cashLabel.Location = new Point(24, 130);

        cashBox = Theme.TextBox(390);
        cashBox.Font = Theme.Bold(18f);
        cashBox.Location = new Point(24, 154);
        cashBox.TextChanged += (_, _) => Recalculate();

        // Quick amounts
        FlowLayoutPanel quick = new FlowLayoutPanel
        {
            Location = new Point(24, 208),
            Size = new Size(396, 48),
            WrapContents = false
        };

        AddQuick(quick, "Exact", total);

        foreach (decimal bill in new[] { 100m, 200m, 500m, 1000m })
        {
            AddQuick(quick, $"₱{bill:0}", bill);
        }

        Label changeCaption = Theme.Label(
            "CHANGE", 9f, true, Theme.Muted);
        changeCaption.Location = new Point(24, 276);

        changeLabel = Theme.Label("₱0.00", 22f, true, Theme.Green);
        changeLabel.Location = new Point(22, 296);

        messageLabel = Theme.Label("", 9.5f, false, Theme.Danger);
        messageLabel.Location = new Point(24, 346);

        confirm = Theme.PrimaryButton("Confirm payment");
        confirm.Size = new Size(260, 48);
        confirm.Location = new Point(24, 392);
        confirm.Enabled = false;
        confirm.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        RoundedButton cancel = Theme.SecondaryButton("Back");
        cancel.Size = new Size(116, 48);
        cancel.Location = new Point(300, 392);
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.AddRange(new Control[]
        {
            header, cashLabel, cashBox, quick, changeCaption,
            changeLabel, messageLabel, confirm, cancel
        });

        AcceptButton = confirm;
        CancelButton = cancel;

        Shown += (_, _) => cashBox.Focus();
    }

    private void AddQuick(
        FlowLayoutPanel parent,
        string text,
        decimal amount)
    {
        RoundedButton button = Theme.SecondaryButton(text);
        button.Size = new Size(70, 40);
        button.Font = Theme.Bold(9.5f);
        button.Margin = new Padding(0, 0, 6, 0);
        button.Click += (_, _) =>
        {
            cashBox.Text = amount.ToString("0.##");
            cashBox.SelectionStart = cashBox.Text.Length;
        };

        parent.Controls.Add(button);
    }

    private void Recalculate()
    {
        string text = cashBox.Text.Trim();

        if (text.Length == 0)
        {
            changeLabel.Text = "₱0.00";
            messageLabel.Text = "";
            confirm.Enabled = false;
            return;
        }

        if (!decimal.TryParse(text, out decimal cash) || cash <= 0)
        {
            changeLabel.Text = "₱0.00";
            messageLabel.Text = "Enter a valid amount.";
            confirm.Enabled = false;
            return;
        }

        if (cash < total)
        {
            changeLabel.Text = "₱0.00";
            messageLabel.Text =
                $"Insufficient cash. Short by ₱{total - cash:F2}.";
            confirm.Enabled = false;
            return;
        }

        CashReceived = cash;
        changeLabel.Text = $"₱{cash - total:F2}";
        messageLabel.Text = "";
        confirm.Enabled = true;
    }
}
