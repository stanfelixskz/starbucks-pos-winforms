using POSInventorySystem.Data;
using POSInventorySystem.Models;
using POSInventorySystem.Services;

namespace POSInventorySystem.UI;

public partial class AdminForm : Form, ILogoutForm
{
    private readonly AdminService adminService = new AdminService();
    private readonly LoginService loginService = new LoginService();

    private readonly Panel content;
    private readonly Dictionary<string, Panel> pages = new();
    private readonly Dictionary<string, Action> refreshers = new();
    private readonly Dictionary<string, RoundedButton> navButtons = new();

    // Grids / controls that are refreshed from data
    private DataGridView productGrid = null!;
    private DataGridView ingredientGrid = null!;
    private DataGridView stockGrid = null!;
    private DataGridView usersGrid = null!;

    // Sales page filter (set by clicking a top seller)
    private string? salesFilterProductId;

    public bool LoggedOut { get; private set; }

    public AdminForm(string adminName)
    {
        Text = "Starbucks POS - Administrator";
        MinimumSize = new Size(1150, 720);
        Size = new Size(1320, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.Cream;
        Font = Theme.Regular();

        // ==============================
        // SIDEBAR
        // ==============================

        Panel side = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = Theme.GreenDark
        };

        // ---- Header: centered logo, role and name, then a thin divider ----
        int top = 30;

        PictureBox? logo = BrandLogo.Create(150, 84);

        if (logo != null)
        {
            logo.Location = new Point((240 - logo.Width) / 2, top);
            side.Controls.Add(logo);
            top += logo.Height + 16;
        }
        else
        {
            Label brand = SidebarLabel("STARBUCKS", 17f, true, Color.White, 36);
            brand.Top = top + 4;
            side.Controls.Add(brand);
            top += 52;
        }

        Label roleLabel = SidebarLabel(
            "ADMINISTRATOR", 9f, true, Theme.Gold, 20);
        roleLabel.Top = top;
        top += 24;

        Label who = SidebarLabel(
            adminName, 10.5f, false, Color.FromArgb(190, 215, 205), 24);
        who.Top = top;
        top += 40;

        Panel divider = new Panel
        {
            BackColor = Color.FromArgb(120, 100, 60),
            Size = new Size(192, 1),
            Location = new Point(24, top)
        };

        top += 26;

        side.Controls.AddRange(new Control[] { roleLabel, who, divider });

        content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Cream,
            Padding = new Padding(28, 24, 28, 24)
        };

        Controls.Add(content);
        Controls.Add(side);

        // ==============================
        // PAGES
        // ==============================

        AddPage("dashboard", "Dashboard", BuildDashboardPage(), RefreshDashboard);
        AddPage("inventory", "Inventory", BuildInventoryPage(), RefreshInventory);
        AddPage("sales", "Sales", BuildSalesPage(), RefreshSales);
        AddPage("accounts", "Accounts", BuildAccountsPage(), RefreshAccounts);

        int y = top;

        foreach (var (key, text) in new[]
        {
            ("dashboard", "Dashboard"),
            ("inventory", "Inventory"),
            ("sales", "Sales"),
            ("accounts", "Accounts")
        })
        {
            RoundedButton nav = new RoundedButton
            {
                Text = text,
                FillColor = Theme.GreenDark,
                HoverColor = Color.FromArgb(40, 80, 68),
                ForeColor = Color.White,
                Size = new Size(204, 46),
                Location = new Point(18, y),
                Radius = 8,
                Font = Theme.Regular(11f),
                AlignLeft = true,
                LeftPadding = 22
            };

            nav.Click += (_, _) =>
            {
                if (key == "sales")
                {
                    salesFilterProductId = null;
                }

                ShowPage(key);
            };

            navButtons[key] = nav;
            side.Controls.Add(nav);
            y += 54;
        }

        RoundedButton logout = Theme.SecondaryButton("Log out");
        logout.Size = new Size(204, 44);
        logout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        logout.Location = new Point(18, ClientSize.Height - 70);
        logout.Click += (_, _) =>
        {
            LoggedOut = true;
            Close();
        };

        Panel bottomLine = new Panel
        {
            BackColor = Color.FromArgb(52, 89, 77),
            Size = new Size(192, 1)
        };

        side.Controls.Add(logout);
        side.Controls.Add(bottomLine);

        void PlaceBottom()
        {
            logout.Location = new Point(18, side.Height - 70);
            bottomLine.Location = new Point(24, side.Height - 88);
        }

        PlaceBottom();
        side.Resize += (_, _) => PlaceBottom();

        ShowPage("dashboard");
    }

    // A centered, fixed-width label for the sidebar header
    private static Label SidebarLabel(
        string text,
        float size,
        bool bold,
        Color color,
        int height)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Size = new Size(240, height),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = bold ? Theme.Bold(size) : Theme.Regular(size),
            ForeColor = color,
            BackColor = Color.Transparent
        };
    }

    // ==============================
    // NAVIGATION
    // ==============================

    private void AddPage(
        string key,
        string title,
        Control body,
        Action refresh)
    {
        Panel page = new Panel
        {
            Dock = DockStyle.Fill,
            Visible = false,
            BackColor = Theme.Cream
        };

        Label heading = Theme.Label(title, 20f, true);
        heading.Dock = DockStyle.Top;
        heading.AutoSize = false;
        heading.Height = 48;

        body.Dock = DockStyle.Fill;

        page.Controls.Add(body);
        page.Controls.Add(heading);

        content.Controls.Add(page);
        pages[key] = page;
        refreshers[key] = refresh;
    }

    private void ShowPage(string key)
    {
        foreach (var (name, page) in pages)
        {
            page.Visible = name == key;
        }

        foreach (var (name, button) in navButtons)
        {
            bool active = name == key;

            button.FillColor =
                active ? Theme.Green : Theme.GreenDark;
            button.HoverColor =
                active ? Theme.GreenLight : Color.FromArgb(40, 80, 68);
            button.AccentBar = active;
            button.Invalidate();
        }

        refreshers[key]();
        UpdateInventoryBadge();
    }

    // ==============================
    // SMALL HELPERS
    // ==============================

    private static DataGridView MakeGrid(string[] columns, params int[] weights)
    {
        DataGridView grid = new DataGridView { Dock = DockStyle.Fill };
        Theme.StyleGrid(grid);

        for (int i = 0; i < columns.Length; i++)
        {
            grid.Columns.Add(columns[i], columns[i]);

            if (i < weights.Length)
            {
                grid.Columns[i].FillWeight = weights[i];
            }
        }

        grid.Columns[0].MinimumWidth = 64;

        // Colour status text
        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 ||
                grid.Columns[e.ColumnIndex].Name != "Status")
            {
                return;
            }

            string value = e.Value?.ToString() ?? "";

            if (value == "LOW" || value == "OUT")
            {
                e.CellStyle.ForeColor = Theme.Danger;
                e.CellStyle.Font = Theme.Bold(10f);
            }
            else if (value == "OK")
            {
                e.CellStyle.ForeColor = Theme.Green;
                e.CellStyle.Font = Theme.Bold(10f);
            }
        };

        return grid;
    }

    private static Panel TitledCard(string title, Control body)
    {
        Panel card = Theme.CardPanel();
        card.Padding = new Padding(16, 14, 16, 16);
        card.Dock = DockStyle.Fill;

        Label label = Theme.Label(title, 12f, true);
        label.Dock = DockStyle.Top;
        label.AutoSize = false;
        label.Height = 34;

        body.Dock = DockStyle.Fill;

        card.Controls.Add(body);
        card.Controls.Add(label);

        return card;
    }

    private static Label FieldLabel(string text, int x, int y)
    {
        Label label = Theme.Label(text, 9f, true, Theme.Muted);
        label.Location = new Point(x, y);
        return label;
    }

    // ==============================
    // CASHIER ACCOUNTS
    // ==============================

    private Control BuildAccountsPage()
    {
        TableLayoutPanel split = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1
        };

        split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 440));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // -------- Register form --------

        Panel form = new Panel();

        TextBox fullName = Theme.TextBox(380);
        fullName.Location = new Point(0, 24);

        TextBox username = Theme.TextBox(380);
        username.Location = new Point(0, 90);

        TextBox password = Theme.TextBox(380);
        password.Location = new Point(0, 156);
        password.UseSystemPasswordChar = true;

        TextBox confirm = Theme.TextBox(380);
        confirm.Location = new Point(0, 222);
        confirm.UseSystemPasswordChar = true;

        Label message = Theme.Label("", 10f, true, Theme.Danger);
        message.Location = new Point(0, 262);
        message.MaximumSize = new Size(380, 0);

        RoundedButton create = Theme.PrimaryButton("Create cashier account");
        create.Size = new Size(380, 48);
        create.Location = new Point(0, 310);
        create.Click += (_, _) =>
        {
            if (password.Text != confirm.Text)
            {
                message.ForeColor = Theme.Danger;
                message.Text = "Passwords do not match.";
                return;
            }

            string error = "";

            if (!Busy.Run(this, () => loginService.Register(
                username.Text.Trim(),
                password.Text,
                fullName.Text,
                out error)))
            {
                message.ForeColor = Theme.Danger;
                message.Text = error;
                return;
            }

            message.ForeColor = Theme.Green;
            message.Text =
                $"Account '{username.Text.Trim()}' created.";

            fullName.Clear();
            username.Clear();
            password.Clear();
            confirm.Clear();

            RefreshAccounts();
        };

        form.Controls.AddRange(new Control[]
        {
            FieldLabel("FULL NAME", 0, 2), fullName,
            FieldLabel("USERNAME", 0, 68), username,
            FieldLabel("PASSWORD (6+ characters)", 0, 134), password,
            FieldLabel("CONFIRM PASSWORD", 0, 200), confirm,
            message, create
        });

        Panel formCard = TitledCard("Register a cashier", form);
        formCard.Margin = new Padding(0, 0, 12, 0);
        formCard.Dock = DockStyle.Fill;

        // -------- Users list --------

        usersGrid = MakeGrid(new[] { "Username", "Full name", "Role" });

        Panel listCard = TitledCard("Accounts", usersGrid);
        listCard.Margin = new Padding(12, 0, 0, 0);
        listCard.Dock = DockStyle.Fill;

        split.Controls.Add(formCard, 0, 0);
        split.Controls.Add(listCard, 1, 0);

        return split;
    }

    private void RefreshAccounts()
    {
        usersGrid.Rows.Clear();

        foreach (User user in DataStore.Users)
        {
            usersGrid.Rows.Add(user.Username, user.FullName, user.Role);
        }
    }
}
