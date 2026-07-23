using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class StatCard : RoundedPanel
{
    private Label _titleLabel = new();
    private Label _valueLabel = new();
    private Label _subValueLabel = new();
    private PictureBox _iconPicture = new();

    public string Title
    {
        get => _titleLabel.Text;
        set { _titleLabel.Text = value; }
    }

    public string Value
    {
        get => _valueLabel.Text;
        set { _valueLabel.Text = value; }
    }

    public string SubValue
    {
        get => _subValueLabel.Text;
        set { _subValueLabel.Text = value; }
    }

    public Image? Icon
    {
        get => _iconPicture.Image;
        set { _iconPicture.Image = value; }
    }

    public Color ValueColor
    {
        get => _valueLabel.ForeColor;
        set { _valueLabel.ForeColor = value; }
    }

    public StatCard()
    {
        CornerRadius = 12;
        BackColor = Color.FromArgb(25, 25, 25);
        BorderColor = Color.FromArgb(40, 40, 40);
        BorderWidth = 1;
        Padding = new Padding(16);

        _iconPicture.SizeMode = PictureBoxSizeMode.StretchImage;
        _iconPicture.Size = new Size(32, 32);
        _iconPicture.Location = new Point(16, 16);

        _titleLabel.AutoSize = true;
        _titleLabel.Location = new Point(56, 16);
        _titleLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _titleLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);

        _valueLabel.AutoSize = true;
        _valueLabel.Location = new Point(56, 32);
        _valueLabel.ForeColor = Color.White;
        _valueLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);

        _subValueLabel.AutoSize = true;
        _subValueLabel.Location = new Point(56, 52);
        _subValueLabel.ForeColor = Color.FromArgb(120, 120, 120);
        _subValueLabel.Font = new Font("Segoe UI", 8, FontStyle.Regular);

        Controls.Add(_iconPicture);
        Controls.Add(_titleLabel);
        Controls.Add(_valueLabel);
        Controls.Add(_subValueLabel);

        Height = 80;
    }
}
