using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace POSInventorySystem.UI;

// Starbucks-style look: greens, cream and a touch of gold.
public static class Theme
{
    public static readonly Color Green = ColorTranslator.FromHtml("#00704A");
    public static readonly Color GreenLight = ColorTranslator.FromHtml("#1E9E6A");
    public static readonly Color GreenDark = ColorTranslator.FromHtml("#1E3932");
    public static readonly Color Cream = ColorTranslator.FromHtml("#F2F0EB");
    public static readonly Color Gold = ColorTranslator.FromHtml("#CBA258");
    public static readonly Color Card = Color.White;
    public static readonly Color Text = ColorTranslator.FromHtml("#1E3932");
    public static readonly Color Muted = ColorTranslator.FromHtml("#6B7B76");
    public static readonly Color Line = ColorTranslator.FromHtml("#E1DDD3");
    public static readonly Color Danger = ColorTranslator.FromHtml("#C0392B");

    // Source Sans 3 (SIL Open Font License), bundled in Assets\Fonts.
    // If the file is missing, Segoe UI is used instead.
    private static readonly FontFamily family = LoadFamily();

    private static FontFamily LoadFamily()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory, "Assets", "Fonts", "SourceSans3-Regular.ttf");

        if (File.Exists(path))
        {
            try
            {
                PrivateFontCollection fonts = new PrivateFontCollection();
                fonts.AddFontFile(path);
                return fonts.Families[0];
            }
            catch (Exception)
            {
                // fall through to the fallback font
            }
        }

        return new FontFamily("Segoe UI");
    }

    public static Font Regular(float size = 10f) =>
        new Font(family, size, FontStyle.Regular);

    public static Font Bold(float size = 10f) =>
        new Font(family, size, FontStyle.Bold);

    // ------------------------------
    // Controls
    // ------------------------------

    public static RoundedButton PrimaryButton(string text) =>
        new RoundedButton
        {
            Text = text,
            FillColor = Green,
            HoverColor = GreenLight,
            ForeColor = Color.White
        };

    public static RoundedButton SecondaryButton(string text) =>
        new RoundedButton
        {
            Text = text,
            FillColor = Color.White,
            HoverColor = Cream,
            ForeColor = Green,
            BorderColor = Green
        };

    public static RoundedButton DangerButton(string text) =>
        new RoundedButton
        {
            Text = text,
            FillColor = Color.White,
            HoverColor = ColorTranslator.FromHtml("#FBEAE8"),
            ForeColor = Danger,
            BorderColor = Danger
        };

    public static Label Label(
        string text,
        float size = 10f,
        bool bold = false,
        Color? color = null)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = bold ? Bold(size) : Regular(size),
            ForeColor = color ?? Text,
            BackColor = Color.Transparent
        };
    }

    public static TextBox TextBox(int width = 260) =>
        new TextBox
        {
            Width = width,
            Font = Regular(11f),
            BorderStyle = BorderStyle.FixedSingle
        };

    public static Panel CardPanel() =>
        new CardPanel
        {
            BackColor = Card,
            Padding = new Padding(16)
        };

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = GreenDark;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GreenDark;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = Bold(9.5f);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = 38;
        grid.DefaultCellStyle.Font = Regular(10f);
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor =
            ColorTranslator.FromHtml("#D4E9E2");
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
        grid.AlternatingRowsDefaultCellStyle.BackColor =
            ColorTranslator.FromHtml("#F8F7F3");
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Line;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 34;
    }

    public static GraphicsPath RoundRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        GraphicsPath path = new GraphicsPath();

        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        return path;
    }
}


// A flat, rounded button with hover and disabled states.
public class RoundedButton : Button
{
    private bool hover;

    public Color FillColor { get; set; } = Theme.Green;
    public Color HoverColor { get; set; } = Theme.GreenLight;
    public Color? BorderColor { get; set; }
    public int Radius { get; set; } = 10;

    // Sidebar items: a gold bar on the left edge marks the active page
    public bool AccentBar { get; set; }

    // Small red pill on the right (for example a low-stock count)
    public string BadgeText { get; set; } = "";

    // Left-aligned text with padding, instead of centered
    public bool AlignLeft { get; set; }
    public int LeftPadding { get; set; } = 22;

    public RoundedButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = Theme.Bold(10.5f);
        Cursor = Cursors.Hand;
        Height = 44;
        UseVisualStyleBackColor = false;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? BackColor);

        Rectangle rect = new Rectangle(
            0, 0, Width - 1, Height - 1);

        Color fill = FillColor;
        Color fore = ForeColor;
        Color? border = BorderColor;

        if (!Enabled)
        {
            fill = Color.FromArgb(225, 225, 220);
            fore = Color.FromArgb(150, 150, 145);
            border = null;
        }
        else if (hover)
        {
            fill = HoverColor;
        }

        using GraphicsPath path = Theme.RoundRect(rect, Radius);
        using SolidBrush brush = new SolidBrush(fill);

        g.FillPath(brush, path);

        if (border.HasValue)
        {
            using Pen pen = new Pen(border.Value, 1.5f);
            g.DrawPath(pen, path);
        }

        if (AccentBar && Enabled)
        {
            using GraphicsPath bar = Theme.RoundRect(
                new Rectangle(0, 10, 5, Height - 21), 2);
            using SolidBrush gold = new SolidBrush(Theme.Gold);

            g.FillPath(gold, bar);
        }

        int badgeSpace = 0;

        if (BadgeText.Length > 0)
        {
            using Font badgeFont = Theme.Bold(8.5f);

            int textWidth = TextRenderer.MeasureText(
                BadgeText, badgeFont).Width;

            int w = Math.Max(24, textWidth + 8);
            Rectangle pill = new Rectangle(
                Width - w - 14, (Height - 22) / 2, w, 22);

            using GraphicsPath pillPath = Theme.RoundRect(pill, 11);
            using SolidBrush red = new SolidBrush(Theme.Danger);

            g.FillPath(red, pillPath);

            TextRenderer.DrawText(
                g, BadgeText, badgeFont, pill, Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding);

            badgeSpace = w + 20;
        }

        Rectangle textArea = AlignLeft
            ? new Rectangle(
                LeftPadding, 0,
                Math.Max(10, Width - LeftPadding - badgeSpace - 6), Height)
            : ClientRectangle;

        TextRenderer.DrawText(
            g,
            Text,
            Font,
            textArea,
            fore,
            (AlignLeft
                ? TextFormatFlags.Left
                : TextFormatFlags.HorizontalCenter) |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis);
    }
}


// A white panel with softly rounded corners.
public class CardPanel : Panel
{
    public int Radius { get; set; } = 14;

    public CardPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Theme.Cream);

        Rectangle rect = new Rectangle(
            0, 0, Width - 1, Height - 1);

        using GraphicsPath path = Theme.RoundRect(rect, Radius);
        using SolidBrush brush = new SolidBrush(BackColor);
        using Pen pen = new Pen(Theme.Line);

        g.FillPath(brush, path);
        g.DrawPath(pen, path);
    }
}
