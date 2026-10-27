using POSInventorySystem.Data;
using POSInventorySystem.Models;
using POSInventorySystem.Services;

namespace POSInventorySystem.UI;

public class CashierForm : Form, ILogoutForm
{
    private readonly InventoryService inventoryService =
        new InventoryService();

    private readonly string cashierName;

    private Transaction transaction = new Transaction();
    private readonly FlowLayoutPanel beverageFlow;
    private readonly FlowLayoutPanel foodFlow;
    private readonly Panel menuScroll;
    private readonly DataGridView cartGrid;
    private readonly Label orderLabel;
    private readonly Label totalLabel;
    private readonly Label clockLabel;
    private readonly Label userLabel;
    private readonly RoundedButton logoutButton;
    private readonly Panel topBar;
    private readonly RoundedButton payButton;
    private readonly RoundedButton plusButton;
    private readonly RoundedButton minusButton;
    private readonly RoundedButton removeButton;
    private readonly RoundedButton clearButton;

    public bool LoggedOut { get; private set; }

    public CashierForm(string cashierName)
    {
        this.cashierName = cashierName;

        Text = "Starbucks POS";
        MinimumSize = new Size(1100, 700);
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        // ==============================
        // TOP BAR
        // ==============================

        Panel top = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Theme.GreenDark
        };

        Label brand = Theme.Label("STARBUCKS", 16f, true, Color.White);
        brand.Location = new Point(24, 16);

        Label brandSub = Theme.Label("POS", 16f, true, Theme.Gold);
        brandSub.Location = new Point(158, 16);

        PictureBox? logo = BrandLogo.Create(120, 52);

        if (logo != null)
        {
            logo.Location = new Point(20, 6);
            brand.Visible = false;
            brandSub.Visible = false;
            top.Controls.Add(logo);
        }

        logoutButton = Theme.SecondaryButton("Log out");
        logoutButton.Size = new Size(100, 38);
        logoutButton.Font = Theme.Bold(9.5f);
        logoutButton.Click += (_, _) => Logout();

        userLabel = Theme.Label(
            cashierName, 10.5f, true, Color.White);

        clockLabel = Theme.Label("", 9.5f, false, Theme.Gold);

        top.Controls.AddRange(new Control[]
        {
            brand, brandSub, logoutButton, userLabel, clockLabel
        });

        topBar = top;
        top.Resize += (_, _) => LayoutTopBar();
        // ==============================
        // CART (RIGHT)
        // ==============================

        Panel cartHost = new Panel
        {
            Dock = DockStyle.Right,
            Width = 440,
            Padding = new Padding(8, 16, 20, 20),
            BackColor = Theme.Cream
        };

        Panel cart = Theme.CardPanel();
        cart.Dock = DockStyle.Fill;
        cart.Padding = new Padding(16);

        cartGrid = new DataGridView { Dock = DockStyle.Fill };
        Theme.StyleGrid(cartGrid);
        cartGrid.ColumnHeadersVisible = false;
        cartGrid.AutoSizeRowsMode =
            DataGridViewAutoSizeRowsMode.AllCells;
        cartGrid.Columns.Add("item", "Item");
        cartGrid.Columns.Add("qty", "Qty");
        cartGrid.Columns.Add("sub", "Subtotal");
        cartGrid.Columns[0].DefaultCellStyle.WrapMode =
            DataGridViewTriState.True;
        cartGrid.Columns[1].FillWeight = 22;
        cartGrid.Columns[1].DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.TopCenter;
        cartGrid.Columns[2].FillWeight = 40;
        cartGrid.Columns[2].DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.TopRight;
        cartGrid.Columns[0].FillWeight = 100;
        cartGrid.SelectionChanged += (_, _) => UpdateCartButtons();

        Panel cartHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60
        };

        Label cartTitle = Theme.Label("Current order", 15f, true);
        cartTitle.Location = new Point(0, 0);

        orderLabel = Theme.Label("", 9.5f, false, Theme.Muted);
        orderLabel.Location = new Point(2, 32);

        cartHeader.Controls.AddRange(
            new Control[] { cartTitle, orderLabel });

        Panel cartFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 232
        };

        minusButton = SmallButton("−  1");
        minusButton.Location = new Point(0, 8);
        minusButton.Click += (_, _) => ChangeQuantity(-1);

        plusButton = SmallButton("+  1");
        plusButton.Location = new Point(82, 8);
        plusButton.Click += (_, _) => ChangeQuantity(1);

        removeButton = Theme.DangerButton("Remove");
        removeButton.Size = new Size(96, 38);
        removeButton.Font = Theme.Bold(9.5f);
        removeButton.Location = new Point(164, 8);
        removeButton.Click += (_, _) => RemoveSelected();

        Label totalCaption = Theme.Label(
            "TOTAL", 9.5f, true, Theme.Muted);
        totalCaption.Location = new Point(0, 68);

        totalLabel = Theme.Label("₱0.00", 28f, true, Theme.Green);
        totalLabel.Location = new Point(-2, 88);

        payButton = Theme.PrimaryButton("Pay with cash");
        payButton.Font = Theme.Bold(13f);
        payButton.Height = 54;
        payButton.Anchor =
            AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
        payButton.Location = new Point(0, 144);
        payButton.Click += (_, _) => Pay();

        clearButton = Theme.SecondaryButton("Clear order");
        clearButton.Font = Theme.Bold(9.5f);
        clearButton.Height = 30;
        clearButton.Location = new Point(0, 202);
        clearButton.Click += (_, _) => ClearOrder();

        cartFooter.Controls.AddRange(new Control[]
        {
            minusButton, plusButton, removeButton,
            totalCaption, totalLabel, payButton, clearButton
        });

        cartFooter.Resize += (_, _) =>
        {
            payButton.Width = cartFooter.Width;
            clearButton.Width = cartFooter.Width;
        };

        cart.Controls.Add(cartGrid);
        cart.Controls.Add(cartFooter);
        cart.Controls.Add(cartHeader);
        cartHost.Controls.Add(cart);

        // ==============================
        // MENU (LEFT)
        // Beverages on top, food below, both in one scrolling area
        // ==============================

        Panel menuHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 8, 0),
            BackColor = Theme.Cream
        };

        Label step = Theme.Label(
            "Choose an item", 9.5f, true, Theme.Muted);
        step.Dock = DockStyle.Top;
        step.Height = 26;

        menuScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Cream
        };

        beverageFlow = MenuSection("BEVERAGES", menuScroll);
        foodFlow = MenuSection("FOOD", menuScroll);

        menuScroll.Resize += (_, _) => SizeMenuSections();

        menuHost.Controls.Add(menuScroll);
        menuHost.Controls.Add(step);

        // ==============================
        // ASSEMBLE
        // ==============================

        Controls.Add(menuHost);
        Controls.Add(cartHost);
        Controls.Add(top);

        System.Windows.Forms.Timer clock =
            new System.Windows.Forms.Timer { Interval = 1000 };

        clock.Tick += (_, _) => UpdateClock();
        clock.Start();

        Disposed += (_, _) => clock.Dispose();

        RefreshProducts();
        RefreshCart();
        UpdateClock();
    }

    private static RoundedButton SmallButton(string text)
    {
        RoundedButton button = Theme.SecondaryButton(text);
        button.Size = new Size(74, 38);
        button.Font = Theme.Bold(9.5f);
        return button;
    }

    private void UpdateClock()
    {
        clockLabel.Text = DateTime.Now.ToString("ddd, MMM dd  hh:mm:ss tt");
        LayoutTopBar();
    }

    // Keeps the name, clock and log out button on the right edge
    private void LayoutTopBar()
    {
        logoutButton.Location = new Point(
            topBar.Width - logoutButton.Width - 24, 13);

        int right = logoutButton.Left - 24;

        userLabel.Location = new Point(right - userLabel.Width, 10);
        clockLabel.Location = new Point(right - clockLabel.Width, 34);
    }
    // ==============================
    // MENU
    // ==============================

    // A heading with a row of product cards under it
    private FlowLayoutPanel MenuSection(string title, Panel host)
    {
        Label heading = Theme.Label(title, 10.5f, true, Theme.Green);
        heading.Tag = "heading";
        heading.Margin = new Padding(0, 10, 0, 6);
        heading.Padding = new Padding(0);
        heading.AutoSize = false;
        heading.Height = 26;
        heading.Width = 400;

        FlowLayoutPanel flow = new FlowLayoutPanel
        {
            WrapContents = true,
            AutoSize = false,
            BackColor = Theme.Cream,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        flow.Tag = "cards";
        host.Controls.Add(flow);
        host.Controls.Add(heading);

        return flow;
    }

    // Lays the sections out one under the other, filling the width
    private void SizeMenuSections()
    {
        int width = Math.Max(300, menuScroll.ClientSize.Width - 8);

        // Beverages block first, then food
        int y = PlaceSection(beverageFlow, width, 0);
        PlaceSection(foodFlow, width, y);
    }

    private int PlaceSection(FlowLayoutPanel flow, int width, int top)
    {
        Label heading = menuScroll.Controls.OfType<Label>()
            .First(l => l.Text == (flow == beverageFlow ? "BEVERAGES" : "FOOD"));

        heading.Location = new Point(0, top);

        // Fit as many cards per row as the width allows, then set the
        // height to the number of rows so nothing spills sideways.
        const int cardWidth = 210 + 14;
        const int cardHeight = 128 + 14;

        int columns = Math.Max(1, width / cardWidth);
        int rows = (int)Math.Ceiling(flow.Controls.Count / (double)columns);

        flow.Location = new Point(0, top + heading.Height + 4);
        flow.Size = new Size(columns * cardWidth, Math.Max(1, rows) * cardHeight);

        return flow.Bottom + 18;
    }

    private void RefreshProducts()
    {
        beverageFlow.SuspendLayout();
        foodFlow.SuspendLayout();

        foreach (FlowLayoutPanel flow in new[] { beverageFlow, foodFlow })
        {
            foreach (Control old in flow.Controls.Cast<Control>().ToList())
            {
                flow.Controls.Remove(old);
                old.Dispose();
            }
        }

        foreach (Product product in DataStore.Products)
        {
            FlowLayoutPanel target = product is BeverageProduct
                ? beverageFlow
                : foodFlow;

            bool available =
                inventoryService.IsProductAvailable(product, 1);

            ProductCard card = new ProductCard(product, available);
            card.Click += (_, _) => AddProduct(product);

            target.Controls.Add(card);
        }

        beverageFlow.ResumeLayout();
        foodFlow.ResumeLayout();

        SizeMenuSections();
    }

    // Steps 2 + 3: customize and choose quantity, then add to the cart.
    private void AddProduct(Product product)
    {
        using OrderItemDialog dialog = new OrderItemDialog(product);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        // Checks the whole cart together, since items share ingredients
        if (!transaction.CanFulfill(
            product,
            dialog.Quantity,
            dialog.Customization))
        {
            MessageBox.Show(
                product is BeverageProduct
                    ? "Sorry, there are not enough ingredients to prepare this order."
                    : "Sorry, there is not enough stock for this order.",
                "Not enough stock",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        transaction.AddItem(
            product,
            dialog.Quantity,
            dialog.Customization);

        RefreshCart();
    }

    // ==============================
    // CART
    // ==============================

    private void RefreshCart()
    {
        int selected =
            cartGrid.CurrentRow?.Index ?? -1;

        cartGrid.Rows.Clear();

        foreach (CartItem item in transaction.Items)
        {
            string text = item.Product.Name;

            if (item.Customization != null)
            {
                BeverageCustomization c = item.Customization;

                text += $"\n{c.Size}, {c.Serving}, {c.Milk}";

                if (c.ExtraShots > 0)
                {
                    text += $", +{c.ExtraShots} shot(s)";
                }

                if (c.Syrup != "None")
                {
                    text += $", {c.Syrup} syrup";
                }
            }

            text += $"\n₱{item.UnitPrice:F2} each";

            cartGrid.Rows.Add(
                text,
                item.Quantity,
                $"₱{item.Subtotal:F2}");
        }

        if (cartGrid.Rows.Count > 0)
        {
            int index = Math.Clamp(
                selected, 0, cartGrid.Rows.Count - 1);

            cartGrid.CurrentCell = cartGrid.Rows[index].Cells[0];
        }

        orderLabel.Text =
            $"{transaction.TransactionId}   " +
            $"{transaction.Date:MMM dd, hh:mm tt}";

        totalLabel.Text = $"₱{transaction.Total:F2}";

        payButton.Enabled = transaction.Items.Count > 0;
        clearButton.Enabled = transaction.Items.Count > 0;
        UpdateCartButtons();
    }

    private void UpdateCartButtons()
    {
        bool hasSelection =
            cartGrid.CurrentRow != null &&
            cartGrid.CurrentRow.Index < transaction.Items.Count;

        plusButton.Enabled = hasSelection;
        minusButton.Enabled = hasSelection;
        removeButton.Enabled = hasSelection;
    }

    private CartItem? SelectedItem()
    {
        int index = cartGrid.CurrentRow?.Index ?? -1;

        return index >= 0 && index < transaction.Items.Count
            ? transaction.Items[index]
            : null;
    }

    private void ChangeQuantity(int delta)
    {
        CartItem? item = SelectedItem();

        if (item == null)
        {
            return;
        }

        if (delta > 0 &&
            !transaction.CanFulfill(
                item.Product,
                1,
                item.Customization))
        {
            MessageBox.Show(
                "Not enough stock to add another one.",
                "Not enough stock",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        item.AddQuantity(delta);
        RefreshCart();
    }

    private void RemoveSelected()
    {
        int index = cartGrid.CurrentRow?.Index ?? -1;

        if (transaction.RemoveItem(index))
        {
            RefreshCart();
        }
    }

    private void ClearOrder()
    {
        if (MessageBox.Show(
            "Remove every item from this order?",
            "Clear order",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        while (transaction.Items.Count > 0)
        {
            transaction.RemoveItem(0);
        }

        RefreshCart();
    }

    // ==============================
    // PAYMENT (CASH ONLY)
    // ==============================

    private void Pay()
    {
        if (transaction.Items.Count == 0)
        {
            return;
        }

        if (!transaction.CanFulfill())
        {
            MessageBox.Show(
                "There is not enough stock for this order.\n" +
                "Please remove an item and try again.",
                "Not enough stock",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        using PaymentDialog payment = new PaymentDialog(transaction.Total);

        if (payment.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!transaction.ProcessPayment(
            new CashPayment(transaction.Total, payment.CashReceived)))
        {
            MessageBox.Show(
                "Payment failed.",
                "Payment",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        transaction.CashierName = cashierName;

        // Deducts ingredients / stock and records the time
        if (!transaction.CompleteTransaction())
        {
            MessageBox.Show(
                "The transaction could not be completed.",
                "Transaction",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        DataStore.Transactions.Add(transaction);

        try
        {
            Busy.Run(this, () => Database.SaveSale(transaction));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The sale is complete but could not be saved to the " +
                "database:" + Environment.NewLine +
                ex.Message + Environment.NewLine +
                Environment.NewLine +
                "It is kept and will be saved automatically with the next " +
                "sale or restock.",
                "Database",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        using (ReceiptDialog receipt = new ReceiptDialog(transaction))
        {
            receipt.ShowDialog(this);
        }

        // Clear everything for the next customer
        transaction = new Transaction();

        RefreshProducts();
        RefreshCart();
    }

    // ==============================
    // CLOSING
    // ==============================

    // Sales that could not be saved are tried once more when the window
    // closes. If they still fail, the cashier decides whether to leave.
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (Database.PendingSaleCount > 0)
        {
            try
            {
                Busy.Run(this, () => Database.SaveInventory());
            }
            catch (Exception)
            {
                // still not reachable; ask below
            }

            if (Database.PendingSaleCount > 0 &&
                MessageBox.Show(
                    $"{Database.PendingSaleCount} sale(s) are not saved to the " +
                    "database yet. If you close now they will be lost.\n\n" +
                    "Close anyway?",
                    "Unsaved sales",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                e.Cancel = true;
                LoggedOut = false;
                return;
            }
        }

        base.OnFormClosing(e);
    }

    // ==============================
    // LOGOUT
    // ==============================

    private void Logout()
    {
        if (transaction.Items.Count > 0 &&
            MessageBox.Show(
                "There is an unfinished order. Log out and discard it?",
                "Log out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        LoggedOut = true;
        Close();
    }
}
