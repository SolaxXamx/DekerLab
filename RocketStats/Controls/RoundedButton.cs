using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class RoundedButton : Button
{
    private int _cornerRadius = 8;
    private Color _hoverColor = Color.FromArgb(40, 40, 40);
    private Color _pressedColor = Color.FromArgb(60, 60, 60);
    private bool _isHovered = false;
    private bool _isPressed = false;

    [Category("Appearance")]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = value; Invalidate(); }
    }

    [Category("Appearance")]
    public Color HoverColor
    {
        get => _hoverColor;
        set { _hoverColor = value; Invalidate(); }
    }

    [Category("Appearance")]
    public Color PressedColor
    {
        get => _pressedColor;
        set { _pressedColor = value; Invalidate(); }
    }

    public RoundedButton()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var rect = ClientRectangle;
        var path = GetRoundedRect(rect, _cornerRadius);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color fillColor = _isPressed ? _pressedColor : (_isHovered ? _hoverColor : BackColor);
        using var brush = new SolidBrush(fillColor);
        e.Graphics.FillPath(brush, path);

        TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
        TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor, flags);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _isPressed = true;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isPressed = false;
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
