using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RocketStats;

public class LoadingSpinner : Control
{
    private int _angle = 0;
    private int _lineWidth = 4;
    private int _radius = 20;
    private Color _color = Color.FromArgb(0, 120, 215);
    private System.Windows.Forms.Timer _timer = new();

    public Color SpinnerColor
    {
        get => _color;
        set { _color = value; Invalidate(); }
    }

    public int LineWidth
    {
        get => _lineWidth;
        set { _lineWidth = value; Invalidate(); }
    }

    public LoadingSpinner()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Size = new Size(50, 50);

        _timer.Interval = 50;
        _timer.Tick += (s, e) => { _angle = (_angle + 10) % 360; Invalidate(); };
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var center = new Point(Width / 2, Height / 2);
        var rect = new Rectangle(center.X - _radius, center.Y - _radius, _radius * 2, _radius * 2);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var pen = new Pen(_color, _lineWidth);
        pen.EndCap = LineCap.Round;
        pen.StartCap = LineCap.Round;

        e.Graphics.DrawArc(pen, rect, _angle, 90);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        _timer.Enabled = Visible;
    }
}
