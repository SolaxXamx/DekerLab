using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RocketStats;

public class RankCard : RoundedPanel
{
    private RoundedPictureBox _rankImage = new();
    private Label _rankNameLabel = new();
    private Label _divisionLabel = new();
    private Label _mmrLabel = new();
    private Label _matchesLabel = new();
    private ProgressCircle _progressCircle = new();

    public string RankName
    {
        get => _rankNameLabel.Text;
        set { _rankNameLabel.Text = value; }
    }

    public string Division
    {
        get => _divisionLabel.Text;
        set { _divisionLabel.Text = value; }
    }

    public string MMR
    {
        get => _mmrLabel.Text;
        set { _mmrLabel.Text = value; }
    }

    public string Matches
    {
        get => _matchesLabel.Text;
        set { _matchesLabel.Text = value; }
    }

    public Image? RankImage
    {
        get => _rankImage.Image;
        set { _rankImage.Image = value; }
    }

    public int ProgressValue
    {
        get => _progressCircle.Value;
        set { _progressCircle.Value = value; }
    }

    public RankCard()
    {
        CornerRadius = 12;
        BackColor = Color.FromArgb(25, 25, 25);
        BorderColor = Color.FromArgb(40, 40, 40);
        BorderWidth = 1;
        Padding = new Padding(16);

        _rankImage.Circular = true;
        _rankImage.Size = new Size(64, 64);
        _rankImage.Location = new Point(16, 16);
        _rankImage.BackColor = Color.Transparent;

        _rankNameLabel.AutoSize = true;
        _rankNameLabel.Location = new Point(90, 16);
        _rankNameLabel.ForeColor = Color.White;
        _rankNameLabel.Font = new Font("Segoe UI", 14, FontStyle.Bold);

        _divisionLabel.AutoSize = true;
        _divisionLabel.Location = new Point(90, 36);
        _divisionLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _divisionLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);

        _mmrLabel.AutoSize = true;
        _mmrLabel.Location = new Point(90, 56);
        _mmrLabel.ForeColor = Color.FromArgb(0, 180, 255);
        _mmrLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);

        _matchesLabel.AutoSize = true;
        _matchesLabel.Location = new Point(90, 76);
        _matchesLabel.ForeColor = Color.FromArgb(120, 120, 120);
        _matchesLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);

        _progressCircle.Size = new Size(60, 60);
        _progressCircle.Location = new Point(Width - 80, 20);
        _progressCircle.ProgressColor = Color.FromArgb(0, 180, 255);
        _progressCircle.BackColor2 = Color.FromArgb(40, 40, 40);
        _progressCircle.LineWidth = 6;

        Controls.Add(_rankImage);
        Controls.Add(_rankNameLabel);
        Controls.Add(_divisionLabel);
        Controls.Add(_mmrLabel);
        Controls.Add(_matchesLabel);
        Controls.Add(_progressCircle);

        Height = 120;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_progressCircle != null)
        {
            _progressCircle.Location = new Point(Width - 80, 20);
        }
    }
}
