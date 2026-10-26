using System.Drawing.Drawing2D;

namespace POSInventorySystem.UI;

public class ChartBar
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
    public string Tooltip { get; set; } = "";
    public bool Highlight { get; set; }
    public object? Tag { get; set; }
}


// Shared behaviour for the dashboard charts: hover, click,
// grow-in animation, tooltip and an empty message.
public abstract class ChartBase : Control
{
    private readonly System.Windows.Forms.Timer timer;

    protected List<ChartBar> Bars = new();
    protected int Hover = -1;
    protected float Progress = 1f;
    protected Point Mouse;

    public string EmptyText { get; set; } = "No data yet.";

    public Func<decimal, string> FormatValue { get; set; } =
        v => $"₱{v:N0}";

    public IReadOnlyList<ChartBar> Items => Bars;

    // Raised with the clicked index (BarChart also raises -1
    // when the selected bar is clicked again).
    public event EventHandler<int>? ItemClicked;

    protected ChartBase()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.White;

        timer = new System.Windows.Forms.Timer { Interval = 15 };
        timer.Tick += (_, _) =>
        {
            Progress += 0.07f;

            if (Progress >= 1f)
            {
                Progress = 1f;
                timer.Stop();
            }

            Invalidate();
        };
    }

    // Eased 0..1 growth used by the bars
    protected float Eased =>
        1f - (float)Math.Pow(1f - Progress, 3);

    protected bool AllZero => Bars.All(b => b.Value <= 0);

    public void SetItems(IEnumerable<ChartBar> items, bool animate = true)
    {
        Bars = items.ToList();
        Hover = -1;

        if (animate)
        {
            Progress = 0f;
            timer.Start();
        }
        else
        {
            Progress = 1f;
        }

        Invalidate();
    }

    public void SkipAnimation()
    {
        timer.Stop();
        Progress = 1f;
        Invalidate();
    }

    protected abstract int IndexAt(Point point);

    protected virtual void HandleClick(int index) =>
        ItemClicked?.Invoke(this, index);

    protected void RaiseItemClicked(int index) =>
        ItemClicked?.Invoke(this, index);

    protected override void OnMouseMove(MouseEventArgs e)
    {
        Mouse = e.Location;

        int index = IndexAt(e.Location);

        Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;

        if (index != Hover || index >= 0)
        {
            Hover = index;
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        Hover = -1;
        Cursor = Cursors.Default;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        int index = IndexAt(e.Location);

        if (index >= 0)
        {
            HandleClick(index);
        }

        base.OnMouseClick(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
        }

        base.Dispose(disposing);
    }

    protected void DrawEmpty(Graphics g, Rectangle area)
    {
        TextRenderer.DrawText(
            g,
            EmptyText,
            Theme.Regular(10.5f),
            area,
            Theme.Muted,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.WordBreak);
    }

    // Dark rounded tooltip, kept inside the control.
    protected void DrawTooltip(
        Graphics g,
        string text,
        Point anchor,
        bool above)
    {
        using Font font = Theme.Regular(9f);

        Size size = TextRenderer.MeasureText(
            g, text, font, new Size(420, 0),
            TextFormatFlags.NoPadding);

        int w = size.Width + 22;
        int h = size.Height + 16;

        int x = Math.Clamp(anchor.X - w / 2, 2, Math.Max(2, Width - w - 2));
        int y = above ? anchor.Y - h - 8 : anchor.Y + 18;

        if (y < 2)
        {
            y = anchor.Y + 18;
        }

        y = Math.Clamp(y, 2, Math.Max(2, Height - h - 2));

        Rectangle box = new Rectangle(x, y, w, h);

        using GraphicsPath path = Theme.RoundRect(box, 8);
        using SolidBrush fill = new SolidBrush(Theme.GreenDark);

        g.FillPath(fill, path);

        TextRenderer.DrawText(
            g, text, font,
            new Rectangle(x + 11, y + 8, size.Width + 2, size.Height + 2),
            Color.White,
            TextFormatFlags.NoPadding);
    }
}


// Vertical bars (for example sales per day).
public class BarChart : ChartBase
{
    private int selected = -1;

    private const int LeftMargin = 74;
    private const int TopMargin = 30;
    private const int RightMargin = 14;
    private const int BottomMargin = 40;

    public int SelectedIndex
    {
        get => selected;
        set
        {
            selected = value;
            Invalidate();
        }
    }

    private Rectangle Plot =>
        new Rectangle(
            LeftMargin,
            TopMargin,
            Math.Max(10, Width - LeftMargin - RightMargin),
            Math.Max(10, Height - TopMargin - BottomMargin));

    protected override int IndexAt(Point point)
    {
        if (Bars.Count == 0)
        {
            return -1;
        }

        Rectangle plot = Plot;

        if (point.X < plot.Left ||
            point.X >= plot.Right ||
            point.Y < plot.Top - 10 ||
            point.Y > Height)
        {
            return -1;
        }

        float slot = plot.Width / (float)Bars.Count;

        return Math.Clamp((int)((point.X - plot.Left) / slot),
            0, Bars.Count - 1);
    }

    protected override void HandleClick(int index)
    {
        selected = index == selected ? -1 : index;
        Invalidate();
        RaiseItemClicked(selected);
    }

    // 1, 2, 4, 5 or 10 times a power of ten, so four steps divide evenly
    private static decimal NiceMax(decimal max)
    {
        if (max <= 0)
        {
            return 100m;
        }

        double exponent =
            Math.Pow(10, Math.Floor(Math.Log10((double)max)));

        double mantissa = (double)max / exponent;

        double nice =
            mantissa <= 1 ? 1 :
            mantissa <= 2 ? 2 :
            mantissa <= 4 ? 4 :
            mantissa <= 5 ? 5 : 10;

        return (decimal)(nice * exponent);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        Rectangle plot = Plot;

        decimal max = NiceMax(
            Bars.Count == 0 ? 0 : Bars.Max(b => b.Value));

        // ---- Grid and value axis ----
        using Pen gridPen = new Pen(Theme.Line);

        for (int i = 0; i <= 4; i++)
        {
            int y = plot.Bottom - (int)(plot.Height * i / 4f);

            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);

            TextRenderer.DrawText(
                g,
                FormatValue(max * i / 4),
                Theme.Regular(8.5f),
                new Rectangle(0, y - 9, LeftMargin - 8, 18),
                Theme.Muted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }

        if (Bars.Count == 0 || AllZero)
        {
            DrawEmpty(g, plot);

            if (Bars.Count == 0)
            {
                return;
            }
        }

        float slot = plot.Width / (float)Bars.Count;
        float barWidth = Math.Min(slot * 0.62f, 64f);

        // Label every n-th bar so labels never overlap
        int step = Bars.Count <= 10
            ? 1
            : (int)Math.Ceiling(52f / slot);

        for (int i = 0; i < Bars.Count; i++)
        {
            ChartBar bar = Bars[i];

            float centerX = plot.Left + slot * (i + 0.5f);
            float height = (float)(bar.Value / max) * plot.Height * Eased;

            bool isHover = i == Hover;
            bool isSelected = i == selected;

            RectangleF rect = new RectangleF(
                centerX - barWidth / 2,
                plot.Bottom - height,
                barWidth,
                height);

            if (bar.Value > 0)
            {
                Color fill = bar.Highlight
                    ? (isHover ? Color.FromArgb(218, 184, 110) : Theme.Gold)
                    : (isHover ? Theme.GreenLight : Theme.Green);

                using GraphicsPath path = Theme.RoundRect(
                    Rectangle.Round(rect),
                    (int)Math.Min(5, Math.Max(1, rect.Height / 2)));

                using SolidBrush brush = new SolidBrush(fill);
                g.FillPath(brush, path);

                if (isSelected)
                {
                    using Pen ring = new Pen(Theme.GreenDark, 2.5f);
                    g.DrawPath(ring, path);
                }

                // Value above the bar (always when there are few bars)
                if ((Bars.Count <= 10 || isHover || isSelected) &&
                    Progress >= 1f)
                {
                    TextRenderer.DrawText(
                        g,
                        FormatValue(bar.Value),
                        Theme.Bold(8.5f),
                        new Rectangle(
                            (int)(centerX - slot / 2 - 10),
                            (int)rect.Top - 20,
                            (int)slot + 20,
                            18),
                        Theme.Text,
                        TextFormatFlags.HorizontalCenter |
                        TextFormatFlags.VerticalCenter);
                }
            }
            else if (!AllZero)
            {
                // Thin stub so empty days are still visible
                using SolidBrush stub = new SolidBrush(Theme.Line);

                g.FillRectangle(
                    stub,
                    centerX - barWidth / 2,
                    plot.Bottom - 3,
                    barWidth,
                    3);
            }

            if ((Bars.Count - 1 - i) % step == 0)
            {
                TextRenderer.DrawText(
                    g,
                    bar.Label,
                    isSelected ? Theme.Bold(8.5f) : Theme.Regular(8.5f),
                    new Rectangle(
                        (int)(centerX - slot * step / 2),
                        plot.Bottom + 6,
                        (int)(slot * step),
                        20),
                    isSelected ? Theme.Text : Theme.Muted,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.Top);
            }
        }

        // ---- Baseline ----
        using Pen baseline = new Pen(Theme.Muted);
        g.DrawLine(baseline, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

        // ---- Tooltip ----
        if (Hover >= 0 && Hover < Bars.Count && Progress >= 1f)
        {
            ChartBar bar = Bars[Hover];

            float centerX = plot.Left + slot * (Hover + 0.5f);
            float top = plot.Bottom -
                (float)(bar.Value / max) * plot.Height;

            DrawTooltip(
                g,
                bar.Tooltip,
                new Point((int)centerX, (int)top - 22),
                above: true);
        }
    }
}


// Horizontal ranking bars (for example top sellers).
public class HBarChart : ChartBase
{
    private int RowHeight =>
        Bars.Count == 0
            ? 40
            : Math.Clamp((Height - 8) / Bars.Count, 28, 46);

    private int LabelWidth => Math.Min(190, (int)(Width * 0.38f));

    protected override int IndexAt(Point point)
    {
        if (Bars.Count == 0)
        {
            return -1;
        }

        int index = (point.Y - 4) / RowHeight;

        return point.Y >= 4 && index >= 0 && index < Bars.Count
            ? index
            : -1;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        if (Bars.Count == 0 || AllZero)
        {
            DrawEmpty(g, ClientRectangle);
            return;
        }

        decimal max = Bars.Max(b => b.Value);
        int rowHeight = RowHeight;
        int labelWidth = LabelWidth;
        int valueWidth = 96;
        int barArea = Math.Max(20, Width - labelWidth - valueWidth - 12);

        for (int i = 0; i < Bars.Count; i++)
        {
            ChartBar bar = Bars[i];
            int y = 4 + i * rowHeight;
            bool isHover = i == Hover;

            if (isHover)
            {
                using GraphicsPath rowPath = Theme.RoundRect(
                    new Rectangle(0, y, Width - 1, rowHeight - 2), 8);
                using SolidBrush rowBrush = new SolidBrush(Theme.Cream);
                g.FillPath(rowBrush, rowPath);
            }

            TextRenderer.DrawText(
                g,
                $"{i + 1}.  {bar.Label}",
                isHover ? Theme.Bold(9.5f) : Theme.Regular(9.5f),
                new Rectangle(10, y, labelWidth - 12, rowHeight - 2),
                Theme.Text,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);

            float width =
                (float)(bar.Value / max) * barArea * Eased;

            Rectangle rect = new Rectangle(
                labelWidth,
                y + rowHeight / 2 - 9,
                Math.Max(4, (int)width),
                18);

            Color fill = bar.Highlight
                ? (isHover ? Color.FromArgb(218, 184, 110) : Theme.Gold)
                : (isHover ? Theme.GreenLight : Theme.Green);

            using (GraphicsPath path = Theme.RoundRect(rect, 6))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
            }

            if (Progress >= 1f)
            {
                TextRenderer.DrawText(
                    g,
                    FormatValue(bar.Value),
                    Theme.Bold(9f),
                    new Rectangle(
                        rect.Right + 8, y, valueWidth, rowHeight - 2),
                    Theme.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        if (Hover >= 0 && Hover < Bars.Count && Progress >= 1f)
        {
            DrawTooltip(g, Bars[Hover].Tooltip, Mouse, above: false);
        }
    }
}
