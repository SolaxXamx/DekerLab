using System.Drawing.Drawing2D;

namespace RocketStats;

public partial class RocketStatsForm
{
    private RoundedPanel _searchPanel = new();
    private RoundedTextBox _searchTextBox = new();
    private RoundedButton _searchButton = new();
    private RoundedButton _refreshButton = new();
    private LoadingSpinner _loadingSpinner = new();
    private Panel _profilePreviewPanel = new();
    private RoundedPictureBox _profileAvatar = new();
    private Label _profileNameLabel = new();
    private Label _profileLevelLabel = new();
    private ProgressCircle _levelProgress = new();
    private Panel _ranksPanel = new();
    private RankCard _rank1v1Card = new();
    private RankCard _rank2v2Card = new();
    private RankCard _rank3v3Card = new();
    private Panel _statsCardsPanel = new();
    private StatCard _totalMatchesCard = new();
    private StatCard _winRateCard = new();
    private StatCard _goalsCard = new();
    private StatCard _assistsCard = new();
    private StatCard _savesCard = new();
    private StatCard _mvpsCard = new();
    private StatCard _playtimeCard = new();
    private GraphPanel _mmrGraph = new();
    private GraphPanel _winsGraph = new();

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

        _settingsButton.Text = "  Param\u0019tres";
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

    private void InitializeDashboardPanel()
    {
        _dashboardPanel.Dock = DockStyle.Fill;
        _dashboardPanel.BackColor = Color.Transparent;
        _dashboardPanel.AutoScroll = true;

        InitializeSearchPanel();
        InitializeProfilePreviewPanel();
        InitializeRanksPanel();
        InitializeStatsCardsPanel();

        _dashboardPanel.Controls.Add(_searchPanel);
        _dashboardPanel.Controls.Add(_profilePreviewPanel);
        _dashboardPanel.Controls.Add(_ranksPanel);
        _dashboardPanel.Controls.Add(_statsCardsPanel);
    }

    private void InitializeSearchPanel()
    {
        _searchPanel.Dock = DockStyle.Top;
        _searchPanel.Height = 60;
        _searchPanel.CornerRadius = 12;
        _searchPanel.BackColor = Color.FromArgb(25, 25, 25);
        _searchPanel.BorderColor = Color.FromArgb(40, 40, 40);
        _searchPanel.BorderWidth = 1;
        _searchPanel.Padding = new Padding(16);

        _searchTextBox.SetPlaceholderText("Entrez le pseudo Epic...");
        _searchTextBox.CornerRadius = 8;
        _searchTextBox.Width = 300;
        _searchTextBox.Location = new Point(16, 16);
        _searchTextBox.KeyPress += SearchTextBox_KeyPress;

        _searchButton.Text = "Rechercher";
        _searchButton.CornerRadius = 8;
        _searchButton.Width = 100;
        _searchButton.Location = new Point(326, 16);
        _searchButton.Click += SearchButton_Click;

        _refreshButton.Text = "Actualiser";
        _refreshButton.CornerRadius = 8;
        _refreshButton.Width = 100;
        _refreshButton.Location = new Point(436, 16);
        _refreshButton.Click += RefreshButton_Click;

        _loadingSpinner.Location = new Point(546, 20);
        _loadingSpinner.Visible = false;

        _searchPanel.Controls.Add(_searchTextBox);
        _searchPanel.Controls.Add(_searchButton);
        _searchPanel.Controls.Add(_refreshButton);
        _searchPanel.Controls.Add(_loadingSpinner);
    }

    private void InitializeProfilePreviewPanel()
    {
        _profilePreviewPanel.Dock = DockStyle.Top;
        _profilePreviewPanel.Height = 120;
        _profilePreviewPanel.BackColor = Color.Transparent;
        _profilePreviewPanel.Padding = new Padding(0, 10, 0, 10);

        var profileContainer = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 12,
            BackColor = Color.FromArgb(25, 25, 25),
            BorderColor = Color.FromArgb(40, 40, 40),
            BorderWidth = 1
        };

        _profileAvatar.Circular = true;
        _profileAvatar.Size = new Size(80, 80);
        _profileAvatar.Location = new Point(20, 20);
        _profileAvatar.Image = ImageHelper.CreateAvatar("", 80);

        _profileNameLabel.Text = "Aucun joueur s\u0019lectionn\u0019";
        _profileNameLabel.ForeColor = Color.White;
        _profileNameLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        _profileNameLabel.Location = new Point(110, 20);
        _profileNameLabel.AutoSize = true;

        _profileLevelLabel.Text = "Niveau: 1";
        _profileLevelLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _profileLevelLabel.Font = new Font("Segoe UI", 11, FontStyle.Regular);
        _profileLevelLabel.Location = new Point(110, 45);
        _profileLevelLabel.AutoSize = true;

        _levelProgress.Size = new Size(60, 60);
        _levelProgress.Location = new Point(110, 65);
        _levelProgress.Value = 0;
        _levelProgress.ProgressColor = ThemeColors.SecondaryAccent;
        _levelProgress.BackColor2 = Color.FromArgb(40, 40, 40);

        profileContainer.Controls.Add(_profileAvatar);
        profileContainer.Controls.Add(_profileNameLabel);
        profileContainer.Controls.Add(_profileLevelLabel);
        profileContainer.Controls.Add(_levelProgress);

        _profilePreviewPanel.Controls.Add(profileContainer);
    }
}
