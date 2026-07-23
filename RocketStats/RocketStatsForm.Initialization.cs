using System.Drawing.Drawing2D;

namespace RocketStats;

public partial class RocketStatsForm
{
    private GradientPanel _titleBar = new();
    private Panel _sideBar = new();
    private Panel _mainContent = new();
    private Label _appTitle = new();
    private RoundedButton _minimizeButton = new();
    private RoundedButton _maximizeButton = new();
    private RoundedButton _closeButton = new();
    private PictureBox _appIcon = new();
    private RoundedPictureBox _userAvatar = new();
    private Label _userNameLabel = new();
    private SideBarButton _dashboardButton = new();
    private SideBarButton _profileButton = new();
    private SideBarButton _statsButton = new();
    private SideBarButton _graphsButton = new();
    private SideBarButton _settingsButton = new();
    private Panel _dashboardPanel = new();
    private Panel _profilePanel = new();
    private Panel _statsPanel = new();
    private Panel _graphsPanel = new();
    private Panel _settingsPanel = new();
    private Control _currentView = new Panel();

    private void InitializeComponent()
    {
        Text = "RocketStats";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(15, 15, 15);
        Size = new Size(_settings.WindowWidth, _settings.WindowHeight);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        Directory.CreateDirectory(AppFolder);
        Directory.CreateDirectory(CacheFolder);

        InitializeTitleBar();
        InitializeSideBar();
        InitializeMainContent();
        InitializeDashboardPanel();
        InitializeProfilePanel();
        InitializeStatsPanel();
        InitializeGraphsPanel();
        InitializeSettingsPanel();

        _currentView = _dashboardPanel;
        _mainContent.Controls.Add(_currentView);

        Controls.Add(_titleBar);
        Controls.Add(_sideBar);
        Controls.Add(_mainContent);

        FormClosing += RocketStatsForm_FormClosing;
        Resize += RocketStatsForm_Resize;

        _titleBar.MouseDown += TitleBar_MouseDown;
        _titleBar.MouseMove += TitleBar_MouseMove;
        _titleBar.MouseUp += TitleBar_MouseUp;

        if (_settings.AutoLoadLastPlayer && !string.IsNullOrEmpty(_settings.LastUsername))
        {
            _currentUsername = _settings.LastUsername;
            LoadPlayerData(_settings.LastUsername);
        }
    }

    private void InitializeTitleBar()
    {
        _titleBar.Dock = DockStyle.Top;
        _titleBar.Height = 40;
        _titleBar.Color1 = Color.FromArgb(20, 20, 20);
        _titleBar.Color2 = Color.FromArgb(15, 15, 15);
        _titleBar.GradientMode = LinearGradientMode.Vertical;

        _appIcon.SizeMode = PictureBoxSizeMode.StretchImage;
        _appIcon.Size = new Size(24, 24);
        _appIcon.Location = new Point(12, 8);
        _appIcon.Image = CreateAppIcon();

        _appTitle.Text = "RocketStats";
        _appTitle.ForeColor = Color.White;
        _appTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _appTitle.Location = new Point(44, 10);
        _appTitle.AutoSize = true;

        ConfigureWindowButton(_minimizeButton, "_", (s, e) => WindowState = FormWindowState.Minimized);
        ConfigureWindowButton(_maximizeButton, "[]", (s, e) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized);
        ConfigureWindowButton(_closeButton, "X", (s, e) => Close());

        _titleBar.Controls.Add(_appIcon);
        _titleBar.Controls.Add(_appTitle);
        _titleBar.Controls.Add(_minimizeButton);
        _titleBar.Controls.Add(_maximizeButton);
        _titleBar.Controls.Add(_closeButton);
    }

    private void ConfigureWindowButton(RoundedButton button, string text, EventHandler clickHandler)
    {
        button.Text = text;
        button.CornerRadius = 0;
        button.Size = new Size(40, 40);
        button.Dock = DockStyle.Right;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Color.Transparent;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.Click += clickHandler;
    }

    private Image CreateAppIcon()
    {
        var bmp = new Bitmap(24, 24);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var pen = new Pen(Color.FromArgb(0, 120, 215), 2);
        g.DrawLine(pen, 12, 4, 12, 20);
        g.DrawLine(pen, 12, 4, 8, 12);
        g.DrawLine(pen, 12, 4, 16, 12);
        g.DrawLine(pen, 12, 20, 8, 20);
        g.DrawLine(pen, 12, 20, 16, 20);

        using var flameBrush = new SolidBrush(Color.FromArgb(255, 100, 0));
        g.FillRectangle(flameBrush, 10, 18, 4, 4);

        return bmp;
    }

    private void InitializeSideBar()
    {
        _sideBar.Dock = DockStyle.Left;
        _sideBar.Width = 250;
        _sideBar.BackColor = Color.FromArgb(20, 20, 20);

        _userAvatar.Circular = true;
        _userAvatar.Size = new Size(48, 48);
        _userAvatar.Location = new Point(16, 16);
        _userAvatar.Image = ImageHelper.CreateAvatar("", 48);

        _userNameLabel.Text = "Rechercher un joueur";
        _userNameLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _userNameLabel.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        _userNameLabel.Location = new Point(72, 16);
        _userNameLabel.AutoSize = true;

        _dashboardButton.Text = "  Tableau de bord";
        _dashboardButton.IsSelected = true;
        _dashboardButton.Click += (s, e) => ShowView(_dashboardPanel);

        _profileButton.Text = "  Profil";
        _profileButton.Click += (s, e) => ShowView(_profilePanel);

        _statsButton.Text = "  Statistiques";
        _statsButton.Click += (s, e) => ShowView(_statsPanel);

        _graphsButton.Text = "  Graphiques";
        _graphsButton.Click += (s, e) => ShowView(_graphsPanel);

        _settingsButton.Text = "  Parametres";
        _settingsButton.Click += (s, e) => ShowView(_settingsPanel);

        _sideBar.Controls.Add(_userAvatar);
        _sideBar.Controls.Add(_userNameLabel);
        _sideBar.Controls.Add(_dashboardButton);
        _sideBar.Controls.Add(_profileButton);
        _sideBar.Controls.Add(_statsButton);
        _sideBar.Controls.Add(_graphsButton);
        _sideBar.Controls.Add(_settingsButton);
    }

    private void InitializeMainContent()
    {
        _mainContent.Dock = DockStyle.Fill;
        _mainContent.BackColor = Color.FromArgb(15, 15, 15);
        _mainContent.Padding = new Padding(20);
    }
}
