using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class GraphPanel : RoundedPanel
{
    private List<float> _dataPoints = new();
    private Color _lineColor = Color.FromArgb(0, 120, 215);
    private Color _fillColor = Color.FromArgb(40, 0, 120, 215);
    private int _pointSize = 4;

    public List<float> DataPoints
    {
        get => _dataPoints;
        set { _dataPoints = value; Invalidate(); }
    }

    public Color LineColor
    {
        get => _lineColor;
        set { _lineColor = value; Invalidate(); }
    }

    public Color FillColor
    {
        get => _fillColor;
        set { _fillColor = value; Invalidate(); }
    }

    public GraphPanel()
    {
        CornerRadius = 12;
        BackColor = Color.FromArgb(20, 20, 20);
        BorderColor = Color.FromArgb(40, 40, 40);
        BorderWidth = 1;
        Height = 200;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_dataPoints.Count < 2) return;

        var rect = ClientRectangle;
        var graphRect = new Rectangle(rect.X + 20, rect.Y + 20, rect.Width - 40, rect.Height - 40);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var points = new PointF[_dataPoints.Count];
        float maxValue = _dataPoints.Max();
        float minValue = _dataPoints.Min();
        float valueRange = maxValue - minValue;
        if (valueRange == 0) valueRange = 1;

        for (int i = 0; i < _dataPoints.Count; i++)
        {
            float x = graphRect.X + (i * (graphRect.Width - 1f) / (_dataPoints.Count - 1));
            float y = graphRect.Bottom - ((_dataPoints[i] - minValue) / valueRange * graphRect.Height);
            points[i] = new PointF(x, y);
        }

        using var fillPath = new GraphicsPath();
        fillPath.AddLine(points);
        fillPath.AddLine(points[points.Length - 1].X, points[points.Length - 1].Y, 
                        points[points.Length - 1].X, graphRect.Bottom);
        fillPath.AddLine(graphRect.Bottom, points[points.Length - 1].X, 
                        graphRect.Bottom, points[0].X);
        fillPath.CloseFigure();

        using var fillBrush = new SolidBrush(_fillColor);
        e.Graphics.FillPath(fillBrush, fillPath);

        using var linePen = new Pen(_lineColor, 2);
        e.Graphics.DrawLines(linePen, points);

        using var pointBrush = new SolidBrush(_lineColor);
        foreach (var point in points)
        {
            e.Graphics.FillEllipse(pointBrush, point.X - _pointSize / 2, point.Y - _pointSize / 2, _pointSize, _pointSize);
        }

        using var gridPen = new Pen(Color.FromArgb(30, 30, 30), 1);
        gridPen.DashStyle = DashStyle.Dot;

        for (int i = 0; i <= 4; i++)
        {
            float y = graphRect.Bottom - (i * graphRect.Height / 4);
            e.Graphics.DrawLine(gridPen, graphRect.X, y, graphRect.Right, y);
        }
    }
}
