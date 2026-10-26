using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// Sales page: filter by date, cashier and search; look at the orders
// (paged and sortable, with a receipt panel), the days, or the items.
public partial class AdminForm
{
    private const int SalesTabOrders = 0;
    private const int SalesTabDays = 1;
    private const int SalesTabItems = 2;
    private const int SalesPageSize = 25;

    private int salesTab = SalesTabOrders;
    private int salesPage;

    // Orders are shown newest first until a column header is clicked
    private int salesSortColumn = 1;
    private bool salesSortAscending;

    private ComboBox salesPeriod = null!;
    private DateTimePicker salesFrom = null!;
    private DateTimePicker salesTo = null!;
    private Label salesToLabel = null!;
    private ComboBox salesCashier = null!;
    private TextBox salesSearch = null!;
    private Panel salesTabs = null!;

    private readonly Label[] salesCardValues = new Label[4];
    private readonly Label[] salesCardSubs = new Label[4];

    private Label salesChip = null!;
    private RoundedButton salesChipClear = null!;

    private DataGridView salesOrdersGrid = null!;
    private DataGridView salesDaysGrid = null!;
    private DataGridView salesItemsGrid = null!;

    private Panel salesOrdersView = null!;
    private Panel salesDaysCard = null!;
    private Panel salesItemsCard = null!;

    private TextBox receiptBox = null!;
    private RoundedButton reprintButton = null!;
    private RoundedButton salesPrev = null!;
    private RoundedButton salesNext = null!;
    private Label salesPageLabel = null!;

    // Stops the filter events from re-running while the page is being filled
    private bool salesBusy;

    // ==============================
    // BUILD
    // ==============================

    private Control BuildSalesPage()
    {
        Panel root = new Panel();

        // ---- Filters (top) ----
        Panel filters = new Panel
        {
            Dock = DockStyle.Top,
            Height = 92
        };

        salesPeriod = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 150,
            Font = Theme.Regular(10.5f),
            Location = new Point(0, 4)
        };

        salesPeriod.Items.AddRange(new object[]
        {
            "Today", "Yesterday", "Last 7 days", "Last 30 days",
            "All time", "Custom range"
        });
        salesPeriod.SelectedIndex = 2;
        salesPeriod.SelectedIndexChanged += (_, _) => SalesFilterChanged();

        salesFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 110,
            Font = Theme.Regular(10f),
            Location = new Point(160, 4),
            Visible = false
        };

        salesToLabel = Theme.Label("to", 10f, false, Theme.Muted);
        salesToLabel.Location = new Point(278, 8);
        salesToLabel.Visible = false;

        salesTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 110,
            Font = Theme.Regular(10f),
            Location = new Point(300, 4),
            Visible = false
        };

        salesFrom.ValueChanged += (_, _) => SalesFilterChanged();
        salesTo.ValueChanged += (_, _) => SalesFilterChanged();

        salesCashier = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 170,
            Font = Theme.Regular(10.5f),
            Location = new Point(430, 4)
        };

        salesCashier.Items.Add("All cashiers");
        salesCashier.SelectedIndex = 0;
        salesCashier.SelectedIndexChanged += (_, _) => SalesFilterChanged();

        salesSearch = Theme.TextBox(240);
        salesSearch.Location = new Point(616, 4);
        salesSearch.PlaceholderText = "Search order # or item";
        salesSearch.TextChanged += (_, _) => SalesFilterChanged();

        salesTabs = MakeToggle(
            new[] { "Orders", "By day", "By item" },
            SalesTabOrders,
            index =>
            {
                salesTab = index;
                ApplySales();
            },
            buttonWidth: 100);

        salesTabs.Location = new Point(0, 48);

        // Shown when the dashboard sent us here for one item
        salesChip = Theme.Label("", 10f, true, Theme.Green);
        salesChip.Location = new Point(340, 56);

        salesChipClear = Theme.SecondaryButton("Show all");
        salesChipClear.Size = new Size(96, 30);
        salesChipClear.Font = Theme.Bold(9f);
        salesChipClear.Location = new Point(340, 52);
        salesChipClear.Visible = false;
        salesChipClear.Click += (_, _) =>
        {
            salesFilterProductId = null;
            SalesFilterChanged();
        };

        filters.Controls.AddRange(new Control[]
        {
            salesPeriod, salesFrom, salesToLabel, salesTo, salesCashier,
            salesSearch, salesTabs, salesChip, salesChipClear
        });

        // ---- Summary cards (react to the filters) ----
        TableLayoutPanel cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 104,
            ColumnCount = 4,
            RowCount = 1
        };

        for (int i = 0; i < 4; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        string[] captions = { "ORDERS", "SALES", "ITEMS SOLD", "AVERAGE ORDER" };

        for (int i = 0; i < 4; i++)
        {
            Panel card = Theme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(i == 0 ? 0 : 8, 0, i == 3 ? 0 : 8, 12);

            Label caption = Theme.Label(captions[i], 9f, true, Theme.Muted);
            caption.Location = new Point(18, 12);

            salesCardValues[i] = Theme.Label("", 20f, true, Theme.Green);
            salesCardValues[i].Location = new Point(16, 32);

            salesCardSubs[i] = Theme.Label("", 9f, false, Theme.Muted);
            salesCardSubs[i].Location = new Point(18, 72);

            card.Controls.AddRange(new Control[]
            {
                caption, salesCardValues[i], salesCardSubs[i]
            });

            cards.Controls.Add(card, i, 0);
        }

        // ---- Orders view: list with paging, and the receipt on the right ----
        salesOrdersGrid = MakeGrid(
            new[] { "Order", "Ordered", "Cashier", "Items", "Total" },
            55, 135, 85, 230, 90);

        // Sorting is done by the page itself (so money sorts as numbers)
        foreach (DataGridViewColumn column in salesOrdersGrid.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.Programmatic;
        }

        salesOrdersGrid.ColumnHeaderMouseClick += (_, e) =>
        {
            if (e.ColumnIndex == salesSortColumn)
            {
                salesSortAscending = !salesSortAscending;
            }
            else
            {
                salesSortColumn = e.ColumnIndex;
                salesSortAscending = true;
            }

            salesPage = 0;
            ApplySales();
        };

        salesOrdersGrid.SelectionChanged += (_, _) => ShowOrderReceipt();

        salesPrev = Theme.SecondaryButton("‹  Previous");
        salesPrev.Size = new Size(120, 34);
        salesPrev.Font = Theme.Bold(9.5f);
        salesPrev.Click += (_, _) =>
        {
            salesPage--;
            ApplySales();
        };

        salesNext = Theme.SecondaryButton("Next  ›");
        salesNext.Size = new Size(120, 34);
        salesNext.Font = Theme.Bold(9.5f);
        salesNext.Click += (_, _) =>
        {
            salesPage++;
            ApplySales();
        };

        salesPageLabel = Theme.Label("", 10f, false, Theme.Muted);

        Panel pager = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48
        };

        salesPrev.Location = new Point(0, 8);
        salesNext.Location = new Point(130, 8);
        salesPageLabel.Location = new Point(270, 16);

        pager.Controls.AddRange(new Control[] { salesPrev, salesNext, salesPageLabel });

        Panel ordersBody = new Panel();
        ordersBody.Controls.Add(salesOrdersGrid);
        ordersBody.Controls.Add(pager);

        Panel ordersCard = TitledCard("Orders  ·  click a heading to sort, click a row for its receipt", ordersBody);

        // Receipt of the selected order
        receiptBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            ForeColor = Theme.Text,
            Font = new Font("Consolas", 9.5f),
            TabStop = false
        };

        reprintButton = Theme.PrimaryButton("Reprint receipt");
        reprintButton.Dock = DockStyle.Bottom;
        reprintButton.Height = 44;
        reprintButton.Enabled = false;
        reprintButton.Click += (_, _) => ReprintSelectedReceipt();

        Panel receiptBody = new Panel();
        receiptBody.Controls.Add(receiptBox);
        receiptBody.Controls.Add(reprintButton);

        Panel receiptCard = TitledCard("Receipt", receiptBody);
        receiptCard.Dock = DockStyle.Fill;

        Panel receiptHost = new Panel
        {
            Dock = DockStyle.Right,
            Width = 392,
            Padding = new Padding(12, 0, 0, 0)
        };

        receiptHost.Controls.Add(receiptCard);

        salesOrdersView = new Panel { Dock = DockStyle.Fill };
        salesOrdersView.Controls.Add(ordersCard);
        salesOrdersView.Controls.Add(receiptHost);

        // ---- By day ----
        salesDaysGrid = MakeGrid(
            new[] { "Date", "Orders", "Items", "Sales", "Average order" },
            160, 70, 70, 100, 100);

        salesDaysCard = TitledCard("Sales by day", salesDaysGrid);

        // ---- By item ----
        salesItemsGrid = MakeGrid(
            new[] { "Item", "Sold", "Revenue", "Share of sales" },
            200, 70, 100, 90);

        salesItemsCard = TitledCard("Items sold  ·  best seller first", salesItemsGrid);

        root.Controls.Add(salesOrdersView);
        root.Controls.Add(salesDaysCard);
        root.Controls.Add(salesItemsCard);
        root.Controls.Add(cards);
        root.Controls.Add(filters);

        return root;
    }

    // ==============================
    // FILTERING
    // ==============================

    private void SalesFilterChanged()
    {
        if (salesBusy)
        {
            return;
        }

        salesPage = 0;
        ApplySales();
    }

    // The date range the period box stands for
    private (DateTime? From, DateTime? To) SalesRange()
    {
        DateTime today = DateTime.Today;

        switch (salesPeriod.SelectedIndex)
        {
            case 0: return (today, today);
            case 1: return (today.AddDays(-1), today.AddDays(-1));
            case 2: return (today.AddDays(-6), today);
            case 3: return (today.AddDays(-29), today);
            case 5:
                DateTime a = salesFrom.Value.Date;
                DateTime b = salesTo.Value.Date;
                return a <= b ? (a, b) : (b, a);
            default: return (null, null);
        }
    }

    private string SalesPeriodText() =>
        salesPeriod.SelectedItem?.ToString() ?? "";

    private List<Transaction> FilteredOrders()
    {
        var (from, to) = SalesRange();
        string cashier = salesCashier.SelectedIndex > 0
            ? salesCashier.SelectedItem?.ToString() ?? ""
            : "";
        string search = salesSearch.Text.Trim();

        return DataStore.Transactions
            .Where(t =>
            {
                DateTime day = (t.CompletedAt ?? t.Date).Date;

                if ((from.HasValue && day < from.Value) ||
                    (to.HasValue && day > to.Value))
                {
                    return false;
                }

                if (cashier.Length > 0 && t.CashierName != cashier)
                {
                    return false;
                }

                if (salesFilterProductId != null &&
                    !t.Items.Any(i => i.Product.ProductId == salesFilterProductId))
                {
                    return false;
                }

                return search.Length == 0 ||
                    t.TransactionId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    t.Items.Any(i => i.Product.Name.Contains(
                        search, StringComparison.OrdinalIgnoreCase));
            })
            .ToList();
    }

    // ==============================
    // REFRESH
    // ==============================

    // Runs every time the page is opened
    private void RefreshSales()
    {
        salesBusy = true;

        // Keep the chosen cashier if they are still in the list
        string chosen = salesCashier.SelectedItem?.ToString() ?? "All cashiers";

        salesCashier.Items.Clear();
        salesCashier.Items.Add("All cashiers");

        foreach (string name in DataStore.Transactions
                     .Select(t => t.CashierName)
                     .Where(n => n.Length > 0)
                     .Distinct()
                     .OrderBy(n => n))
        {
            salesCashier.Items.Add(name);
        }

        int index = salesCashier.Items.IndexOf(chosen);
        salesCashier.SelectedIndex = index >= 0 ? index : 0;

        salesBusy = false;

        ApplySales();
    }

    private void ApplySales()
    {
        salesBusy = true;

        bool custom = salesPeriod.SelectedIndex == 5;
        salesFrom.Visible = custom;
        salesToLabel.Visible = custom;
        salesTo.Visible = custom;

        // Where the "only this item" chip goes depends on the date row
        if (salesFilterProductId != null)
        {
            salesChip.Text =
                "Only orders with " +
                DataStore.GetProduct(salesFilterProductId)?.Name;
            salesChip.Left = 340;
            salesChipClear.Left = salesChip.Right + 12;
        }

        salesChip.Visible = salesFilterProductId != null;
        salesChipClear.Visible = salesFilterProductId != null;

        List<Transaction> orders = FilteredOrders();

        // ---- Cards ----
        decimal sales = orders.Sum(t => t.Total);
        int items = orders.Sum(t => t.Items.Sum(i => i.Quantity));

        salesCardValues[0].Text = orders.Count.ToString();
        salesCardValues[1].Text = $"₱{sales:N2}";
        salesCardValues[2].Text = items.ToString();
        salesCardValues[3].Text = orders.Count == 0
            ? "-"
            : $"₱{sales / orders.Count:N2}";

        foreach (Label sub in salesCardSubs)
        {
            sub.Text = SalesPeriodText();
        }

        // ---- Which view ----
        salesOrdersView.Visible = salesTab == SalesTabOrders;
        salesDaysCard.Visible = salesTab == SalesTabDays;
        salesItemsCard.Visible = salesTab == SalesTabItems;

        FillOrders(orders);
        FillDays(orders);
        FillItems(orders);

        salesBusy = false;
    }

    // The value a column sorts by
    private static IComparable SalesKey(Transaction t, int column) =>
        column switch
        {
            0 => int.TryParse(t.TransactionId.Substring(1), out int n) ? n : 0,
            1 => t.CompletedAt ?? t.Date,
            2 => t.CashierName,
            3 => t.Items.Sum(i => i.Quantity),
            _ => t.Total
        };

    private void FillOrders(List<Transaction> orders)
    {
        List<Transaction> sorted = (salesSortAscending
            ? orders.OrderBy(t => SalesKey(t, salesSortColumn))
            : orders.OrderByDescending(t => SalesKey(t, salesSortColumn)))
            .ToList();

        int pages = Math.Max(1, (int)Math.Ceiling(sorted.Count / (double)SalesPageSize));
        salesPage = Math.Clamp(salesPage, 0, pages - 1);

        shownOrders = sorted
            .Skip(salesPage * SalesPageSize)
            .Take(SalesPageSize)
            .ToList();

        salesOrdersGrid.Rows.Clear();

        foreach (Transaction t in shownOrders)
        {
            int row = salesOrdersGrid.Rows.Add(
                t.TransactionId,
                (t.CompletedAt ?? t.Date).ToString("MMM dd, hh:mm tt"),
                t.CashierName.Length == 0 ? "-" : t.CashierName,
                string.Join(", ", t.Items.Select(
                    i => $"{i.Product.Name} x{i.Quantity}")),
                $"₱{t.Total:N2}");

            salesOrdersGrid.Rows[row].Tag = t;
        }

        // Arrow on the column the list is sorted by
        foreach (DataGridViewColumn column in salesOrdersGrid.Columns)
        {
            column.HeaderCell.SortGlyphDirection =
                column.Index == salesSortColumn
                    ? (salesSortAscending ? SortOrder.Ascending : SortOrder.Descending)
                    : SortOrder.None;
        }

        salesPrev.Enabled = salesPage > 0;
        salesNext.Enabled = salesPage < pages - 1;

        salesPageLabel.Text = sorted.Count == 0
            ? "No orders match these filters"
            : $"Page {salesPage + 1} of {pages}   ·   {sorted.Count} order(s)";

        ShowOrderReceipt();
    }

    private void FillDays(List<Transaction> orders)
    {
        salesDaysGrid.Rows.Clear();

        foreach (var day in orders
                     .GroupBy(t => (t.CompletedAt ?? t.Date).Date)
                     .OrderByDescending(g => g.Key))
        {
            decimal total = day.Sum(t => t.Total);

            salesDaysGrid.Rows.Add(
                day.Key.ToString("ddd, MMM d, yyyy"),
                day.Count(),
                day.Sum(t => t.Items.Sum(i => i.Quantity)),
                $"₱{total:N2}",
                $"₱{total / day.Count():N2}");
        }
    }

    private void FillItems(List<Transaction> orders)
    {
        salesItemsGrid.Rows.Clear();

        decimal all = orders.Sum(t => t.Total);

        var grouped = orders
            .SelectMany(t => t.Items)
            .GroupBy(i => i.Product.ProductId)
            .Select(g => (
                Name: g.First().Product.Name,
                Qty: g.Sum(i => i.Quantity),
                Revenue: g.Sum(i => i.Subtotal)))
            .OrderByDescending(g => g.Qty)
            .ThenByDescending(g => g.Revenue);

        foreach (var item in grouped)
        {
            salesItemsGrid.Rows.Add(
                item.Name,
                item.Qty,
                $"₱{item.Revenue:N2}",
                all == 0 ? "-" : $"{item.Revenue / all:P0}");
        }
    }

    // ==============================
    // RECEIPT PANEL
    // ==============================

    private List<Transaction> shownOrders = new();

    private Transaction? SelectedOrder() =>
        salesOrdersGrid.CurrentRow?.Tag as Transaction;

    private void ShowOrderReceipt()
    {
        Transaction? order = SelectedOrder();

        receiptBox.Text = order == null
            ? "Select an order to see its receipt."
            : ReceiptBuilder.Build(order)
                .Replace("\r\n", "\n")
                .Replace("\n", "\r\n");

        receiptBox.Select(0, 0);
        reprintButton.Enabled = order != null;
    }

    private void ReprintSelectedReceipt()
    {
        Transaction? order = SelectedOrder();

        if (order == null)
        {
            return;
        }

        using ReceiptDialog dialog = new ReceiptDialog(
            $"Receipt {order.TransactionId}",
            "Receipt",
            ReceiptBuilder.Build(order),
            "Close");

        dialog.ShowDialog(this);
    }
}
