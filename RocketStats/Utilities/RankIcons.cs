using System.Drawing.Drawing2D;

namespace RocketStats;

public static class RankIcons
{
    private static Dictionary<string, Image> _rankImages = new();

    public static Image GetRankImage(string rankName)
    {
        if (_rankImages.Count == 0)
        {
            LoadDefaultRankImages();
        }

        if (_rankImages.TryGetValue(rankName.ToLower(), out var image))
        {
            return image;
        }

        return GetDefaultRankImage(rankName);
    }

    private static void LoadDefaultRankImages()
    {
        var ranks = new[] { "unranked", "bronze", "silver", "gold", "platinum", "diamond", "champion", "grand champion", "supersonic legend" };
        var colors = new[] {
            Color.FromArgb(100, 100, 100),
            Color.FromArgb(170, 100, 50),
            Color.FromArgb(180, 180, 180),
            Color.FromArgb(255, 215, 0),
            Color.FromArgb(100, 200, 255),
            Color.FromArgb(0, 191, 255),
            Color.FromArgb(255, 100, 255),
            Color.FromArgb(255, 200, 100),
            Color.FromArgb(255, 50, 50)
        };

        for (int i = 0; i < ranks.Length; i++)
        {
            _rankImages[ranks[i]] = CreateRankIcon(ranks[i], colors[i]);
        }
    }

    private static Image CreateRankIcon(string rankName, Color color)
    {
        var bmp = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var outerPen = new Pen(color, 4);
        g.DrawEllipse(outerPen, 4, 4, 56, 56);

        using var innerBrush = new SolidBrush(color);
        g.FillEllipse(innerBrush, 12, 12, 40, 40);

        string initial = rankName.Substring(0, 1).ToUpper();
        using var font = new Font("Segoe UI", 16, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        var textSize = g.MeasureString(initial, font);
        g.DrawString(initial, font, textBrush, 32 - textSize.Width / 2, 32 - textSize.Height / 2);

        return bmp;
    }

    private static Image GetDefaultRankImage(string rankName)
    {
        return CreateRankIcon(rankName.ToLower(), Color.FromArgb(100, 100, 100));
    }
}
