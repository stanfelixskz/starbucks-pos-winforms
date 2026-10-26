using POSInventorySystem.Data;
using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// Dashboard page of the admin window.
public partial class AdminForm
{
    private BarChart salesChart = null!;
    private HBarChart topChart = null!;
    private DataGridView dayGrid = null!;
    private DataGridView dashLowGrid = null!;
    private DataGridView activityGrid = null!;
    private Label dayTitle = null!;
    private readonly Label[] cardValues = new Label[4];
    private readonly Label[] cardSubs = new Label[4];

    private int salesDays = 7;
    private bool topByQuantity = true;
    private DateTime selectedDay = DateTime.Today;

    // Orders are dated by when they were completed
    private static DateTime OrderDate(Transaction t) =>
        (t.CompletedAt ?? t.Date).Date;

    private static IEnumerable<Transaction> OrdersOn(DateTime day) =>
        DataStore.Transactions.Where(t => OrderDate(t) == day.Date);

    // ==============================
    // BUILD
    // ==============================

    private Control BuildDashboardPage()
    {
        TableLayoutPanel root = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 3
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        // -------- Summary cards --------

        TableLayoutPanel cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };

        for (int i = 0; i < 4; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        string[] captions =
        {
            "TODAY'S SALES", "TODAY'S ORDERS",
            "ITEMS SOLD TODAY", "LOW STOCK ALERTS"
        };

        for (int i = 0; i < 4; i++)
        {
            Panel card = Theme.CardPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(i == 0 ? 0 : 7, 0, i == 3 ? 0 : 7, 12);

            Label caption = Theme.Label(captions[i], 9f, true, Theme.Muted);
            caption.Location = new Point(18, 12);

            cardValues[i] = Theme.Label("", 20f, true, Theme.Green);
            cardValues[i].Location = new Point(16, 32);

            cardSubs[i] = Theme.Label("", 9f, false, Theme.Muted);
            cardSubs[i].Location = new Point(18, 70);

            card.Controls.AddRange(new Control[]
            {
                caption, cardValues[i], cardSubs[i]
            });

            cards.Controls.Add(card, i, 0);
        }

        // -------- Charts --------

        TableLayoutPanel charts = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        salesChart = new BarChart
        {
            Dock = DockStyle.Fill,
            EmptyText = "No sales in this period yet.\nCompleted orders will show up here."
        };

        salesChart.ItemClicked += (_, index) =>
        {
            selectedDay = index >= 0 && salesChart.Items[index].Tag is DateTime day
                ? day
                : DateTime.Today;

            RefreshDayOrders();
        };

        Panel salesToggle = MakeToggle(
            new[] { "7 days", "30 days" },
            0,
            index =>
            {
                salesDays = index == 0 ? 7 : 30;
                selectedDay = DateTime.Today;
                RefreshDashboardCharts();
            });

        Panel salesCard = ChartCard(
            "Sales  ·  click a bar to see that day's orders",
            salesToggle,
            salesChart);

        salesCard.Margin = new Padding(0, 0, 7, 12);

        topChart = new HBarChart
        {
            Dock = DockStyle.Fill,
            EmptyText = "No sales yet today.\nTop sellers will show up here."
        };

        topChart.ItemClicked += (_, index) =>
        {
            if (topChart.Items[index].Tag is string productId)
            {
                GoToSalesFilteredBy(productId);
            }
        };

        Panel topToggle = MakeToggle(
            new[] { "Qty", "Revenue" },
            0,
            index =>
            {
                topByQuantity = index == 0;
                RefreshDashboardCharts();
            });

        Panel topCard = ChartCard(
            "Top sellers today",
            topToggle,
            topChart);

        topCard.Margin = new Padding(7, 0, 0, 12);

        charts.Controls.Add(salesCard, 0, 0);
        charts.Controls.Add(topCard, 1, 0);

        // -------- Lists --------

        TableLayoutPanel lists = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1
        };

        lists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        lists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        lists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        dayGrid = MakeGrid(
            new[] { "Txn", "Time", "Items", "Total" },
            62, 100, 130, 100);

        dayTitle = Theme.Label("", 9.5f, false, Theme.Muted);
        dayTitle.Dock = DockStyle.Top;
        dayTitle.AutoSize = false;
        dayTitle.Height = 24;

        Panel dayBody = new Panel();
        dayBody.Controls.Add(dayGrid);
        dayBody.Controls.Add(dayTitle);

        Panel dayCard = TitledCard("Orders", dayBody);
        dayCard.Margin = new Padding(0, 0, 7, 0);

        dashLowGrid = MakeGrid(
            new[] { "Item", "Left", "Status" },
            120, 80, 72);

        dashLowGrid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                GoRestock(dashLowGrid.Rows[e.RowIndex].Tag as string);
            }
        };

        new ToolTip().SetToolTip(
            dashLowGrid, "Double-click an item to restock it");

        Panel lowCard = TitledCard("Low stock", dashLowGrid);
        lowCard.Margin = new Padding(7, 0, 7, 0);

        activityGrid = MakeGrid(
            new[] { "When", "Activity" },
            110, 170);

        activityGrid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                ShowInventoryTab(TabRecords);
            }
        };

        new ToolTip().SetToolTip(
            activityGrid, "Double-click to open the stock records");

        Panel activityCard = TitledCard("Recent activity", activityGrid);
        activityCard.Margin = new Padding(7, 0, 0, 0);

        lists.Controls.Add(dayCard, 0, 0);
        lists.Controls.Add(lowCard, 1, 0);
        lists.Controls.Add(activityCard, 2, 0);

        root.Controls.Add(cards, 0, 0);
        root.Controls.Add(charts, 0, 1);
        root.Controls.Add(lists, 0, 2);

        // Scrolls on small screens instead of squashing the charts
        Panel scroll = new Panel
        {
            AutoScroll = true,
            AutoScrollMinSize = new Size(0, 700)
        };

        root.Dock = DockStyle.Fill;
        scroll.Controls.Add(root);

        return scroll;
    }

    // Card with a title, a toggle on the right and a chart below
    private static Panel ChartCard(string title, Control toggle, Control chart)
    {
        Panel card = Theme.CardPanel();
        card.Padding = new Padding(16, 12, 16, 12);
        card.Dock = DockStyle.Fill;

        chart.Dock = DockStyle.Fill;

        Panel header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 42
        };

        Label label = Theme.Label(title, 11f, true);
        label.Location = new Point(0, 6);

        toggle.Dock = DockStyle.Right;

        header.Controls.Add(toggle);
        header.Controls.Add(label);

        card.Controls.Add(chart);
        card.Controls.Add(header);

        return card;
    }

    // Small segmented control: one option is always selected
    private static Panel MakeToggle(
        string[] names,
        int selected,
        Action<int> changed,
        int buttonWidth = 82)
    {
        Panel panel = new Panel
        {
            Width = names.Length * (buttonWidth + 6),
            Height = 36
        };

        List<RoundedButton> buttons = new();

        void Style(int active)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                bool on = i == active;

                buttons[i].FillColor = on ? Theme.Green : Color.White;
                buttons[i].HoverColor = on ? Theme.GreenLight : Theme.Cream;
                buttons[i].ForeColor = on ? Color.White : Theme.Green;
                buttons[i].Invalidate();
            }
        }

        for (int i = 0; i < names.Length; i++)
        {
            int index = i;

            RoundedButton button = Theme.SecondaryButton(names[i]);
            button.Size = new Size(buttonWidth, 30);
            button.Font = Theme.Bold(9f);
            button.Radius = 15;
            button.Location = new Point(i * (buttonWidth + 6), 3);
            button.Click += (_, _) =>
            {
                Style(index);
                changed(index);
            };

            buttons.Add(button);
            panel.Controls.Add(button);
        }

        Style(selected);

        return panel;
    }

    // ==============================
    // REFRESH
    // ==============================

    private void RefreshDashboard()
    {
        List<Transaction> today = OrdersOn(DateTime.Today).ToList();
        List<Transaction> yesterday =
            OrdersOn(DateTime.Today.AddDays(-1)).ToList();

        decimal sales = today.Sum(t => t.Total);
        decimal salesBefore = yesterday.Sum(t => t.Total);

        cardValues[0].Text = $"₱{sales:N2}";
        SetTrend(cardSubs[0], sales, salesBefore);

        cardValues[1].Text = today.Count.ToString();
        SetTrend(cardSubs[1], today.Count, yesterday.Count);

        cardValues[2].Text =
            today.Sum(t => t.Items.Sum(i => i.Quantity)).ToString();
        cardSubs[2].Text = "pieces and drinks";
        cardSubs[2].ForeColor = Theme.Muted;

        int alerts = LowStockRows().Count;

        cardValues[3].Text = alerts.ToString();
        cardValues[3].ForeColor = alerts > 0 ? Theme.Danger : Theme.Green;
        cardSubs[3].Text = alerts > 0
            ? "needs restocking"
            : "everything is stocked";
        cardSubs[3].ForeColor = alerts > 0 ? Theme.Danger : Theme.Muted;

        RefreshDashboardCharts();
        RefreshDashboardLists();
    }

    private static void SetTrend(Label label, decimal now, decimal before)
    {
        if (before == 0)
        {
            label.Text = now > 0 ? "no sales yesterday" : "no sales yet";
            label.ForeColor = Theme.Muted;
            return;
        }

        decimal change = (now - before) / before * 100m;

        label.Text = change >= 0
            ? $"▲ {change:0}% vs yesterday"
            : $"▼ {Math.Abs(change):0}% vs yesterday";

        label.ForeColor = change >= 0 ? Theme.Green : Theme.Danger;
    }

    private void RefreshDashboardCharts()
    {
        // ---- Sales per day ----
        List<ChartBar> bars = new();

        for (int back = salesDays - 1; back >= 0; back--)
        {
            DateTime day = DateTime.Today.AddDays(-back);
            List<Transaction> orders = OrdersOn(day).ToList();
            decimal total = orders.Sum(t => t.Total);

            bars.Add(new ChartBar
            {
                Label = salesDays <= 7 ? day.ToString("ddd d") : day.ToString("MMM d"),
                Value = total,
                Highlight = back == 0,
                Tag = day,
                Tooltip =
                    $"{day:ddd, MMM d, yyyy}\n" +
                    $"₱{total:N2}\n" +
                    $"{orders.Count} order{(orders.Count == 1 ? "" : "s")}"
            });
        }

        salesChart.SetItems(bars);
        salesChart.SelectedIndex = bars.FindIndex(
            b => b.Tag is DateTime d && d == selectedDay.Date);

        // ---- Top sellers today ----
        var totals = new Dictionary<string, (string Name, int Qty, decimal Revenue)>();

        foreach (Transaction t in OrdersOn(DateTime.Today))
        {
            foreach (CartItem item in t.Items)
            {
                string id = item.Product.ProductId;

                (string Name, int Qty, decimal Revenue) current =
                    totals.GetValueOrDefault(
                        id, (item.Product.Name, 0, 0m));

                totals[id] = (
                    current.Name,
                    current.Qty + item.Quantity,
                    current.Revenue + item.Subtotal);
            }
        }

        topChart.FormatValue = topByQuantity
            ? v => $"{v:N0} sold"
            : v => $"₱{v:N0}";

        var ranked = topByQuantity
            ? totals.OrderByDescending(p => p.Value.Qty)
            : totals.OrderByDescending(p => p.Value.Revenue);

        List<ChartBar> top = ranked
            .Take(5)
            .Select((p, index) => new ChartBar
            {
                Label = p.Value.Name,
                Value = topByQuantity ? p.Value.Qty : p.Value.Revenue,
                Highlight = index == 0,
                Tag = p.Key,
                Tooltip =
                    $"{p.Value.Name}\n" +
                    $"{p.Value.Qty} sold  ·  ₱{p.Value.Revenue:N2}\n" +
                    "Click to see these orders"
            })
            .ToList();

        topChart.SetItems(top);

        RefreshDayOrders();
    }

    // Orders for the bar that is selected in the sales chart
    private void RefreshDayOrders()
    {
        List<Transaction> orders = OrdersOn(selectedDay)
            .OrderByDescending(t => t.CompletedAt ?? t.Date)
            .ToList();

        string when = selectedDay.Date == DateTime.Today
            ? "today"
            : selectedDay.ToString("ddd, MMM d");

        dayTitle.Text =
            $"{orders.Count} order{(orders.Count == 1 ? "" : "s")} {when}" +
            $"  ·  ₱{orders.Sum(t => t.Total):N2}";

        dayGrid.Rows.Clear();

        foreach (Transaction t in orders)
        {
            dayGrid.Rows.Add(
                t.TransactionId,
                (t.CompletedAt ?? t.Date).ToString("hh:mm tt"),
                string.Join(", ", t.Items.Select(
                    i => $"{i.Product.Name} x{i.Quantity}")),
                $"₱{t.Total:N2}");
        }
    }

    // Food and ingredients that need restocking
    private static List<(string Type, string Id, string Name, string Left, string Status)>
        LowStockRows()
    {
        var rows = new List<(string, string, string, string, string)>();

        foreach (Product p in DataStore.Products
                     .Where(p => p is FoodProduct && p.IsLowStock()))
        {
            rows.Add(("Product", p.ProductId, p.Name,
                $"{p.Stock} pcs", p.Stock <= 0 ? "OUT" : "LOW"));
        }

        foreach (Ingredient i in DataStore.Ingredients
                     .Where(i => i.IsLowStock()))
        {
            rows.Add(("Ingredient", i.IngredientId, i.Name,
                Ingredient.Packs(i.Quantity), "LOW"));
        }

        return rows;
    }

    private void RefreshDashboardLists()
    {
        dashLowGrid.Rows.Clear();

        foreach (var row in LowStockRows().Take(12))
        {
            int index = dashLowGrid.Rows.Add(row.Name, row.Left, row.Status);
            dashLowGrid.Rows[index].Tag = $"{row.Type}:{row.Id}";
        }

        // Sales and restocks together, newest first
        var activity = new List<(DateTime When, string Text)>();

        foreach (Transaction t in DataStore.Transactions)
        {
            activity.Add((t.CompletedAt ?? t.Date,
                $"Sale {t.TransactionId}  ·  ₱{t.Total:N2}"));
        }

        foreach (StockRecord r in DataStore.StockRecords
                     .Where(r => r.Type == "RESTOCK"))
        {
            activity.Add((r.Timestamp,
                $"Restock {r.ItemName}  ·  +{r.Count:0.##} {r.Unit}"));
        }

        activityGrid.Rows.Clear();

        foreach (var entry in activity
                     .OrderByDescending(a => a.When)
                     .Take(12))
        {
            activityGrid.Rows.Add(
                entry.When.Date == DateTime.Today
                    ? entry.When.ToString("hh:mm tt")
                    : entry.When.ToString("MMM d"),
                entry.Text);
        }
    }

    // ==============================
    // NAVIGATION SHORTCUTS
    // ==============================

    // Opens the Sales page showing only orders that contain a product
    private void GoToSalesFilteredBy(string productId)
    {
        salesFilterProductId = productId;
        salesPeriod.SelectedIndex = 0;   // Today
        salesTab = SalesTabOrders;
        ((RoundedButton)salesTabs.Controls[SalesTabOrders]).PerformClick();
        ShowPage("sales");
    }
}
