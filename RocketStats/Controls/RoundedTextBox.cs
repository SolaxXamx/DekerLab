using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class RoundedTextBox : TextBox
{
    private int _cornerRadius = 8;
    private Color _borderColor = Color.FromArgb(80, 80, 80);
    private int _borderWidth = 1;
    private Color _focusedBorderColor = Color.FromArgb(0, 120, 215);
    private bool _isFocused = false;

    [Category("Appearance")]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = value; Invalidate(); }
    }

    [Category("Appearance")]
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    [Category("Appearance")]
    public int BorderWidth
    {
        get => _borderWidth;
        set { _borderWidth = value; Invalidate(); }
    }

    [Category("Appearance")]
    public Color FocusedBorderColor
    {
        get => _focusedBorderColor;
        set { _focusedBorderColor = value; Invalidate(); }
    }

    public RoundedTextBox()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BorderStyle = BorderStyle.None;
        BackColor = Color.FromArgb(20, 20, 20);
        ForeColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var rect = ClientRectangle;
        var path = GetRoundedRect(rect, _cornerRadius);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var bgBrush = new SolidBrush(BackColor);
        e.Graphics.FillPath(bgBrush, path);

        Color borderColor = _isFocused ? _focusedBorderColor : _borderColor;
        if (_borderWidth > 0)
        {
            using var pen = new Pen(borderColor, _borderWidth);
            e.Graphics.DrawPath(pen, path);
        }

        if (!string.IsNullOrEmpty(Text))
        {
            var textRect = new Rectangle(rect.X + 10, rect.Y, rect.Width - 20, rect.Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, 
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        _isFocused = true;
        Invalidate();
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        _isFocused = false;
        Invalidate();
    }

    private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
