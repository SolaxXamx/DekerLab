using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class RoundedPictureBox : PictureBox
{
    private int _cornerRadius = 50;
    private bool _circular = true;

    [Category("Appearance")]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = value; Invalidate(); }
    }

    [Category("Appearance")]
    public bool Circular
    {
        get => _circular;
        set { _circular = value; Invalidate(); }
    }

    public RoundedPictureBox()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        SizeMode = PictureBoxSizeMode.StretchImage;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Image == null)
        {
            base.OnPaint(e);
            return;
        }

        var rect = ClientRectangle;
        var path = GetRoundedRect(rect, _circular ? Math.Min(rect.Width, rect.Height) / 2 : _cornerRadius);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        e.Graphics.SetClip(path);
        e.Graphics.DrawImage(Image, rect);
        e.Graphics.ResetClip();

        if (BorderStyle != BorderStyle.None)
        {
            using var pen = new Pen(ForeColor, 1);
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
