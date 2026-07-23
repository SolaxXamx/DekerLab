using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class GradientPanel : Panel
{
    private Color _color1 = Color.FromArgb(30, 30, 30);
    private Color _color2 = Color.FromArgb(20, 20, 20);
    private LinearGradientMode _gradientMode = LinearGradientMode.Vertical;

    [Category("Appearance")]
    public Color Color1
    {
        get => _color1;
        set { _color1 = value; Invalidate(); }
    }

    [Category("Appearance")]
    public Color Color2
    {
        get => _color2;
        set { _color2 = value; Invalidate(); }
    }

    [Category("Appearance")]
    public LinearGradientMode GradientMode
    {
        get => _gradientMode;
        set { _gradientMode = value; Invalidate(); }
    }

    public GradientPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var rect = ClientRectangle;
        using var brush = new LinearGradientBrush(rect, _color1, _color2, _gradientMode);
        e.Graphics.FillRectangle(brush, rect);
    }
}
