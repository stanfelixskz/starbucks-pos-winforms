using System.Drawing.Drawing2D;
using POSInventorySystem.Models;

namespace POSInventorySystem.UI;

// A clickable menu tile for the cashier screen.
public class ProductCard : Control
{
    private bool hover;

    public Product Product { get; }
    public bool IsAvailable { get; }

    public ProductCard(Product product, bool isAvailable)
    {
        Product = product;
        IsAvailable = isAvailable;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        Size = new Size(210, 128);
        Margin = new Padding(0, 0, 14, 14);
        Cursor = isAvailable ? Cursors.Hand : Cursors.Default;
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

    protected override void OnClick(EventArgs e)
    {
        if (IsAvailable)
        {
            base.OnClick(e);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Theme.Cream);

        Rectangle rect = new Rectangle(
            0, 0, Width - 1, Height - 1);

        bool beverage = Product is BeverageProduct;

        using (GraphicsPath path = Theme.RoundRect(rect, 12))
        using (SolidBrush fill = new SolidBrush(
            IsAvailable ? Color.White : Color.FromArgb(236, 234, 228)))
        using (Pen pen = new Pen(
            hover && IsAvailable ? Theme.Green : Theme.Line,
            hover && IsAvailable ? 2f : 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        Color ink = IsAvailable ? Theme.Text : Theme.Muted;

        // Category pill
        string category = beverage ? "BEVERAGE" : "FOOD";

        using (Font pillFont = Theme.Bold(7.5f))
        using (SolidBrush pill = new SolidBrush(
            IsAvailable
                ? (beverage ? Theme.Green : Theme.Gold)
                : Color.FromArgb(190, 190, 185)))
        {
            Size size = TextRenderer.MeasureText(
                category, pillFont);

            Rectangle pillRect = new Rectangle(
                14, 12, size.Width + 4, 18);

            using GraphicsPath pillPath =
                Theme.RoundRect(pillRect, 8);

            g.FillPath(pill, pillPath);

            TextRenderer.DrawText(
                g, category, pillFont, pillRect, Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter);
        }

        // Name
        TextRenderer.DrawText(
            g,
            Product.Name,
            Theme.Bold(11f),
            new Rectangle(12, 36, Width - 24, 44),
            ink,
            TextFormatFlags.WordBreak |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.Left);

        // Price
        TextRenderer.DrawText(
            g,
            $"₱{Product.Price:F2}",
            Theme.Bold(13f),
            new Rectangle(12, Height - 38, 110, 28),
            IsAvailable ? Theme.Green : Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

        // Status
        string status = IsAvailable
            ? "Available"
            : (beverage ? "Unavailable" : "Out of stock");

        TextRenderer.DrawText(
            g,
            status,
            Theme.Regular(8.5f),
            new Rectangle(Width - 108, Height - 38, 96, 28),
            IsAvailable ? Theme.Muted : Theme.Danger,
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
    }
}
