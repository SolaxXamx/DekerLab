using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class SideBarButton : RoundedButton
{
    private Label _notificationLabel = new();
    private bool _isSelected = false;
    private Color _selectedColor = Color.FromArgb(0, 120, 215);

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; Invalidate(); }
    }

    public Color SelectedColor
    {
        get => _selectedColor;
        set { _selectedColor = value; Invalidate(); }
    }

    public string NotificationText
    {
        get => _notificationLabel.Text;
        set { _notificationLabel.Text = value; _notificationLabel.Visible = !string.IsNullOrEmpty(value); }
    }

    public SideBarButton()
    {
        CornerRadius = 8;
        Height = 48;
        Dock = DockStyle.Top;
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new Padding(12, 0, 0, 0);
        FlatAppearance.BorderSize = 0;

        _notificationLabel.AutoSize = true;
        _notificationLabel.BackColor = Color.FromArgb(255, 80, 80);
        _notificationLabel.ForeColor = Color.White;
        _notificationLabel.Font = new Font("Segoe UI", 7, FontStyle.Bold);
        _notificationLabel.Size = new Size(18, 18);
        _notificationLabel.TextAlign = ContentAlignment.MiddleCenter;
        _notificationLabel.Location = new Point(Width - 25, 15);
        _notificationLabel.Visible = false;

        Controls.Add(_notificationLabel);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_isSelected)
        {
            BackColor = _selectedColor;
            ForeColor = Color.White;
        }
        else
        {
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(150, 150, 150);
        }

        base.OnPaint(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!_isSelected)
        {
            BackColor = Color.FromArgb(40, 40, 40);
            ForeColor = Color.White;
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!_isSelected)
        {
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(150, 150, 150);
        }
    }
}
