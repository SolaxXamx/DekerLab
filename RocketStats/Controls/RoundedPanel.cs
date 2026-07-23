using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class RoundedPanel : Panel
{
    private int _cornerRadius = 10;
    private Color _borderColor = Color.FromArgb(50, 50, 50);
    private int _borderWidth = 1;

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

    public RoundedPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var rect = ClientRectangle;
        var path = GetRoundedRect(rect, _cornerRadius);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillPath(new SolidBrush(BackColor), path);

        if (_borderWidth > 0)
        {
            using var pen = new Pen(_borderColor, _borderWidth);
            pen.Alignment = PenAlignment.Inset;
            e.Graphics.DrawPath(pen, path);
        }
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
