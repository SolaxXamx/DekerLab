using System.Drawing.Drawing2D;

namespace RocketStats;

public static class ImageHelper
{
    public static Image CreateAvatar(string username, int size = 64)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        var colors = new[] {
            Color.FromArgb(0, 120, 215), Color.FromArgb(0, 180, 255),
            Color.FromArgb(255, 100, 0), Color.FromArgb(255, 200, 0),
            Color.FromArgb(0, 200, 80), Color.FromArgb(255, 80, 80),
            Color.FromArgb(180, 0, 255), Color.FromArgb(255, 0, 180)
        };

        int colorIndex = Math.Abs(username.GetHashCode()) % colors.Length;

        using var bgBrush = new SolidBrush(colors[colorIndex]);
        g.FillEllipse(bgBrush, 0, 0, size, size);

        string initial = string.IsNullOrEmpty(username) ? "?" : username.Substring(0, 1).ToUpper();
        using var font = new Font("Segoe UI", size / 2, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        var textSize = g.MeasureString(initial, font);
        g.DrawString(initial, font, textBrush, size / 2 - textSize.Width / 2, size / 2 - textSize.Height / 2);

        return bmp;
    }

    public static Image CreateGradientBackground(int width, int height, Color color1, Color color2)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);

        using var brush = new LinearGradientBrush(new Point(0, 0), new Point(width, height), color1, color2);
        g.FillRectangle(brush, 0, 0, width, height);

        return bmp;
    }
}
