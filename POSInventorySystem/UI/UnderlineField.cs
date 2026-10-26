using System.Diagnostics.CodeAnalysis;

namespace POSInventorySystem.UI;

// A clean text input: no box, just a line underneath that turns
// green while the field has focus.
public class UnderlineField : Control
{
    private readonly TextBox box;
    private bool focused;

    public UnderlineField(int width, string placeholder = "")
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Theme.Regular(12f),
            BackColor = Color.White,
            ForeColor = Theme.Text,
            PlaceholderText = placeholder,
            Location = new Point(0, 8),
            Width = width
        };

        box.GotFocus += (_, _) => SetFocused(true);
        box.LostFocus += (_, _) => SetFocused(false);

        Controls.Add(box);

        BackColor = Color.White;
        Size = new Size(width, 40);
    }

    [AllowNull]
    public override string Text
    {
        get => box.Text;
        set => box.Text = value ?? "";
    }

    public bool UseSystemPasswordChar
    {
        get => box.UseSystemPasswordChar;
        set => box.UseSystemPasswordChar = value;
    }

    public void Clear() => box.Clear();

    public new bool Focus() => box.Focus();

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        box.Width = Width;
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        box.BackColor = BackColor;
    }

    private void SetFocused(bool value)
    {
        focused = value;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);

        using Pen pen = new Pen(
            focused ? Theme.Green : Theme.Line,
            focused ? 2f : 1f);

        e.Graphics.DrawLine(pen, 0, Height - 2, Width, Height - 2);
    }
}
