using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

public class ReceiptDialog : Form
{
    public ReceiptDialog(Transaction transaction)
        : this(
            $"Receipt {transaction.TransactionId}",
            "Order complete",
            ReceiptBuilder.Build(transaction),
            "Start new order")
    {
    }

    public ReceiptDialog(
        string windowTitle,
        string heading,
        string body,
        string buttonText)
    {
        Text = windowTitle;
        ClientSize = new Size(440, 640);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Cream;

        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Theme.Green
        };

        Label done = Theme.Label(
            heading, 15f, true, Color.White);
        done.Location = new Point(22, 16);
        header.Controls.Add(done);

        Panel paper = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Cream,
            Padding = new Padding(20)
        };

        TextBox receipt = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = Theme.Text,
            Font = new Font("Consolas", 10f),
            Text = body
                .Replace("\r\n", "\n")
                .Replace("\n", "\r\n"),
            TabStop = false
        };

        receipt.Select(0, 0);
        paper.Controls.Add(receipt);

        Panel footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            BackColor = Theme.Cream
        };

        RoundedButton next = Theme.PrimaryButton(buttonText);
        next.Size = new Size(396, 48);
        next.Location = new Point(22, 12);
        next.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        footer.Controls.Add(next);

        Controls.Add(paper);
        Controls.Add(footer);
        Controls.Add(header);

        AcceptButton = next;
    }
}
