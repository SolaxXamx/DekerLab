using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class ProgressCircle : Control
{
    private int _value = 0;
    private int _maximum = 100;
    private int _lineWidth = 8;
    private Color _progressColor = Color.FromArgb(0, 120, 215);
    private Color _backColor = Color.FromArgb(50, 50, 50);
    private Color _textColor = Color.White;

    [Category("Appearance")]
    public int Value
    {
        get => _value;
        set { _value = Math.Max(0, Math.Min(value, _maximum)); Invalidate(); }
    }

    [Category("Appearance")]
    public int Maximum
    {
        get => _maximum;
        set { _maximum = Math.Max(1, value); Invalidate(); }
    }

    [Category("Appearance")]
    public int LineWidth
    {
        get => _lineWidth;
        set { _lineWidth = value; Invalidate(); }
    }

    [Category("Appearance")]
    public Color ProgressColor
    {
        get => _progressColor;
        set { _progressColor = value; Invalidate(); }
    }

    [Category("Appearance")]
    public Color BackColor2
    {
        get => _backColor;
        set { _backColor = value; Invalidate(); }
    }

    [Category("Appearance")]
    public Color TextColor
    {
        get => _textColor;
        set { _textColor = value; Invalidate(); }
    }

    public ProgressCircle()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Size = new Size(100, 100);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var rect = ClientRectangle;
        var center = new Point(rect.Width / 2, rect.Height / 2);
        var radius = Math.Min(rect.Width, rect.Height) / 2 - _lineWidth / 2;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var backPen = new Pen(_backColor, _lineWidth);
        e.Graphics.DrawEllipse(backPen, center.X - radius, center.Y - radius, radius * 2, radius * 2);

        if (_value > 0 && _maximum > 0)
        {
            float angle = (_value * 360f) / _maximum;
            using var progressPen = new Pen(_progressColor, _lineWidth);
            progressPen.EndCap = LineCap.Round;
            progressPen.StartCap = LineCap.Round;
            e.Graphics.DrawArc(progressPen, center.X - radius, center.Y - radius, radius * 2, radius * 2, -90, angle);
        }

        float percentage = _maximum > 0 ? (_value * 100f) / _maximum : 0;
        string text = $"{percentage:F0}%";

        using var font = new Font("Segoe UI", 14, FontStyle.Bold);
        var textSize = e.Graphics.MeasureString(text, font);
        var textRect = new RectangleF(center.X - textSize.Width / 2, center.Y - textSize.Height / 2, textSize.Width, textSize.Height);

        using var textBrush = new SolidBrush(_textColor);
        e.Graphics.DrawString(text, font, textBrush, textRect);
    }
}
