using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace MonLauncherJeux;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ApplicationConfiguration.Initialize();
        
        // Show splash screen
        var splashLogoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "logo_blue.png");
        using (var splash = new SplashForm(splashLogoPath))
        {
            splash.Show();
            Application.DoEvents();
            System.Threading.Thread.Sleep(2000);
        }
        
        using var form = new LauncherForm(args);
        Application.Run(form);
    }


internal sealed record GameEntry(string Name, string Path, string IconPath);

internal sealed class GameStat
{
    public int Launches { get; set; }
    public double Minutes { get; set; }
    public DateTime? LastPlayed { get; set; }
}

internal sealed class LauncherStats
{
    public int TotalLaunches { get; set; }
    public double TotalMinutes { get; set; }
    public Dictionary<string, GameStat> PerGame { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> DailyMinutes { get; set; } = new();
    public List<string> Achievements { get; set; } = new();

    public GameStat For(string path)
    {
        if (!PerGame.TryGetValue(path, out var stat))
        {
            stat = new GameStat();
            PerGame[path] = stat;
        }
        return stat;
    }
}

internal sealed class Profile
{
    public string UserName { get; set; } = string.Empty;
    public string AvatarPath { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string SortMode { get; set; } = string.Empty;
    public List<string> Favorites { get; set; } = new();
}

internal sealed record Palette(Color Background, Color Panel, Color Card, Color Text, Color Muted, Color Accent);

internal sealed class RunningSession
{
    public required Process Process { get; init; }
    public required string Path { get; init; }
    public DateTime StartUtc { get; init; } = DateTime.UtcNow;
}

internal sealed record Achievement(string Id, string Icon, string Title, string Description);

internal static class Achievements
{
    public static readonly IReadOnlyList<Achievement> Catalog = new[]
    {
        new Achievement("first", "\U0001F3AE", "Première partie", "Lancer un jeu pour la première fois"),
        new Achievement("launch10", "\U0001F525", "Habitué", "Lancer des jeux 10 fois au total"),
        new Achievement("launch50", "\u26A1", "Accro", "Lancer des jeux 50 fois au total"),
        new Achievement("time1h", "\u23F1", "Une heure au compteur", "Cumuler 1 heure de jeu"),
        new Achievement("time10h", "\U0001F3C5", "Marathonien", "Cumuler 10 heures de jeu"),
        new Achievement("games5", "\U0001F4DA", "Collectionneur", "Avoir 5 jeux dans la bibliothèque"),
        new Achievement("games10", "\U0001F5C4", "Ludothèque", "Avoir 10 jeux dans la bibliothèque"),
        new Achievement("level5", "\u2B50", "Niveau 5", "Atteindre le niveau 5"),
        new Achievement("level10", "\U0001F31F", "Niveau 10", "Atteindre le niveau 10")
    };

    public static bool IsUnlocked(string id, LauncherStats stats, int gamesCount, int level) => id switch
    {
        "first" => stats.TotalLaunches >= 1,
        "launch10" => stats.TotalLaunches >= 10,
        "launch50" => stats.TotalLaunches >= 50,
        "time1h" => stats.TotalMinutes >= 60,
        "time10h" => stats.TotalMinutes >= 600,
        "games5" => gamesCount >= 5,
        "games10" => gamesCount >= 10,
        "level5" => level >= 5,
        "level10" => level >= 10,
        _ => false
    };
}

internal static class LevelSystem
{
    private static double NeededFor(int level) => 100d + (level - 1) * 50d;

    public static (int Level, double IntoLevel, double NeededForLevel) ComputeFromXp(double xp)
    {
        var level = 1;
        var accumulated = 0d;
        var needed = NeededFor(level);
        while (xp >= accumulated + needed)
        {
            accumulated += needed;
            level++;
            needed = NeededFor(level);
        }
        return (level, xp - accumulated, needed);
    }
}

internal sealed class LauncherForm : Form
{
    private static readonly string AppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonLauncherJeux");
    private static readonly string IconFolder = Path.Combine(AppFolder, "icons");
    private static readonly string DataFile = Path.Combine(AppFolder, "games.json");
    private static readonly string StatsFile = Path.Combine(AppFolder, "stats.json");
    private static readonly string ProfileFile = Path.Combine(AppFolder, "profile.json");
    private static readonly string AppDir = AppContext.BaseDirectory;

    private static readonly string[] SortModes = { "Nom", "Temps de jeu", "Lancements", "Récent" };

    private static readonly Color[] TilePalette =
    {
        ColorTranslator.FromHtml("#E23B3B"),
        ColorTranslator.FromHtml("#3B5BE2"),
        ColorTranslator.FromHtml("#22B04B"),
        ColorTranslator.FromHtml("#E2823B"),
        ColorTranslator.FromHtml("#8B5CF6"),
        ColorTranslator.FromHtml("#14B8A6"),
        ColorTranslator.FromHtml("#EC4899"),
        ColorTranslator.FromHtml("#F59E0B")
    };

    private readonly List<GameEntry> _games = new();
    private readonly LauncherStats _stats = new();
    private readonly Profile _profile = new();
    private Image? _avatarImage;
    private readonly List<RunningSession> _running = new();
    private readonly List<GameTile> _tiles = new();
    private int _selectedIndex = -1;
    private string _search = string.Empty;
    private string _sortMode = "Nom";

    // Top navigation bar.
    private readonly Panel _navBar = new() { Height = 96 };
    private readonly Panel _profilePanel = new() { Dock = DockStyle.Left, Width = 280, Cursor = Cursors.Hand };
    private readonly AvatarCircle _avatar = new() { Width = 56, Height = 56, Left = 22, Top = 20, Cursor = Cursors.Hand };
    private readonly Label _userName = new() { AutoSize = true, Left = 92, Top = 22, Font = new Font("Segoe UI", 14, FontStyle.Bold), Cursor = Cursors.Hand };
    private readonly Label _levelLabel = new() { AutoSize = true, Left = 92, Top = 50, Font = new Font("Segoe UI", 10), Cursor = Cursors.Hand };
    private readonly FlowLayoutPanel _navCenter = new() { AutoSize = true, WrapContents = false };
    private readonly NavButton _homeButton = new() { Text = "Accueil", Width = 120, Height = 44 };
    private HomePage _homePage = null!;
    private Control _currentView = null!;
    private readonly NavButton _libraryButton = new() { Text = "Bibliothèque", Width = 140, Height = 44 };
    private readonly NavButton _profileButton = new() { Text = "Profil", Width = 96, Height = 44 };
    private readonly NavButton _settingsButton = new() { Text = "Paramètre", Width = 120, Height = 44 };
    private readonly Label _clock = new() { AutoSize = true, Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 15, FontStyle.Bold), Padding = new Padding(0, 0, 26, 0) };
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };

    // Library view = grid + right sidebar.
    private readonly Panel _content = new();
    private readonly FlowLayoutPanel _grid = new() { Dock = DockStyle.Fill, WrapContents = true, AutoScroll = true, Padding = new Padding(26, 22, 12, 22) };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Right, Width = 330 };
    private readonly FlowLayoutPanel _sidebarFlow = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(18, 20, 18, 20) };
    private readonly RoundedPanel _searchBox = new() { Width = 288, Height = 46, CornerRadius = 14, Margin = new Padding(0, 0, 0, 12) };
    private readonly TextBox _searchInput = new() { BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 12) };
    private readonly Label _searchIcon = new() { Text = "\U0001F50D", AutoSize = true, Font = new Font("Segoe UI Emoji", 11) };
    private readonly Label _sortLabel = new() { Text = "TRIER PAR", AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Margin = new Padding(2, 0, 0, 4) };
    private readonly ComboBox _sortCombo = new() { Width = 288, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Margin = new Padding(0, 0, 0, 12) };
    private readonly Label _resultLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Margin = new Padding(2, 0, 0, 12) };
    private readonly RoundedButton _randomButton = new() { Text = "\U0001F3B2  Jeu au hasard", Width = 288, Height = 46, CornerRadius = 14, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 0, 0, 18) };

    private RoundedProgressBar _levelProgress = null!;
    private Label _levelCardValue = null!;
    private Label _levelCardXp = null!;
    private Label _timeCardValue = null!;
    private Label _launchCardValue = null!;
    private Label _libraryCardValue = null!;
    private Label _favCardValue = null!;

    // Full-screen pages.
    private readonly GameDetailPage _detailPage = new() { Visible = false };
    private readonly SettingsPage _settingsPage = new() { Visible = false };
    private readonly ProfilePage _profilePage = new() { Visible = false };
    private GameEntry? _detailGame;

    private Control _currentView = null!;

    // Achievement toast.
    private readonly Toast _toast = new() { Visible = false };
    private readonly Queue<Achievement> _toastQueue = new();
    private readonly System.Windows.Forms.Timer _toastTimer = new() { Interval = 3200 };

    private readonly Dictionary<string, Palette> _themeList = new()
    {
        ["Sombre violet"] = MakePalette("#11101A", "#1D1B2E", "#292641", "#F5F3FF", "#B8B3D9", "#8B5CF6"),
        ["Steam bleu"] = MakePalette("#0B1623", "#12263A", "#1B3A57", "#E6F1FF", "#9FBAD1", "#66C0F4"),
        ["Cyber vert"] = MakePalette("#07130D", "#0D2418", "#163B28", "#EAFFF2", "#A0D9B8", "#22C55E"),
        ["Rouge néon"] = MakePalette("#18070B", "#2A0D14", "#42131F", "#FFF1F3", "#E6A8B5", "#FB3059"),
        ["Glacier clair"] = MakePalette("#DCE6F1", "#C9D8EA", "#FFFFFF", "#1B2733", "#5A6B7B", "#3E8FD8"),
        ["Océan"] = MakePalette("#061826", "#0B2A40", "#123B58", "#E4F4FF", "#93B8CF", "#1FA6C7"),
        ["Ambre"] = MakePalette("#160F04", "#271B08", "#3B2A0E", "#FFF6E6", "#D6BE95", "#F5A623"),
        ["Rose bonbon"] = MakePalette("#1A0812", "#2C0E20", "#431631", "#FFEDF6", "#E0A9C6", "#F53D8C"),
        ["Émeraude"] = MakePalette("#04140F", "#082720", "#0E3B31", "#E6FFF6", "#93CFBD", "#10C79A"),
        ["Ardoise"] = MakePalette("#0F1216", "#191E25", "#252C35", "#EEF2F6", "#9BA6B2", "#5B8DEF")
    };
    private string _themeName = "Sombre violet";
    private Palette _theme;

    private readonly System.Windows.Forms.Timer _fadeTimer = new() { Interval = 15 };

    // Page cross-fade.
    private readonly FadeOverlay _fadeOverlay = new() { Visible = false };
    private readonly System.Windows.Forms.Timer _pageFadeTimer = new() { Interval = 15 };
    private float _pageFade;

    // Fullscreen toggle (F11).
    private bool _fullscreen;
    private FormWindowState _prevWindowState = FormWindowState.Normal;
    private FormBorderStyle _prevBorder = FormBorderStyle.Sizable;

    private static Palette MakePalette(string bg, string panel, string card, string text, string muted, string accent) =>
        new(ColorTranslator.FromHtml(bg), ColorTranslator.FromHtml(panel), ColorTranslator.FromHtml(card),
            ColorTranslator.FromHtml(text), ColorTranslator.FromHtml(muted), ColorTranslator.FromHtml(accent));

    public LauncherForm(IEnumerable<string> startupFiles)
    {
        Directory.CreateDirectory(IconFolder);

        Text = "DekerLab";
        TryLoadAppIcon();
        MinimumSize = new Size(1040, 700);
        Size = new Size(1240, 800);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;
        DoubleBuffered = true;
        KeyPreview = true;
        Opacity = 0d;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;

        LoadGames();
        LoadStats();
        LoadProfile();

        _theme = _themeList.TryGetValue(_themeName, out var pal) ? pal : _themeList.First().Value;

        BuildInterface();
        AddFiles(startupFiles, showErrors: false);
        RenderLibrary();
        RefreshStatsUi();
        CheckAchievements();

        Resize += (_, _) => LayoutViews();
        Shown += (_, _) => { LayoutViews(); StartFadeIn(); };
    }

        private void ShowHome()
        {
            _homePage.UpdateStats(_games.Count, _stats.TotalMinutes / 60, GetCurrentLevel());
            SwitchView(_homePage);
        }

        private void ShowLibrary() => SwitchView(_content);

        private void ShowProfile() => SwitchView(_profilePage);

        private void ShowSettings() => SwitchView(_settingsPage);

        private void SwitchView(Control newView)
        {
            _currentView.Visible = false;
            _currentView = newView;
            _currentView.Visible = true;
            _currentView.BringToFront();
        }

        private int GetCurrentLevel()
        {
            var (level, _, _) = LevelSystem.ComputeFromXp(_stats.TotalMinutes);
            return level;
        }
   }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        TryRoundWindowCorners();
    }

    private void StartFadeIn()
    {
        _fadeTimer.Tick += (_, _) =>
        {
            Opacity = Math.Min(1d, Opacity + 0.1d);
            if (Opacity >= 1d)
            {
                Opacity = 1d;
                _fadeTimer.Stop();
            }
        };
        _fadeTimer.Start();
    }

    private void BuildInterface()
    {
        BuildNavBar();
        BuildContent();
        BuildPages();

        Controls.Add(_content);
        Controls.Add(_detailPage);
        Controls.Add(_settingsPage);
        Controls.Add(_profilePage);
        Controls.Add(_navBar);
        Controls.Add(_toast);
        Controls.Add(_fadeOverlay);

        _currentView = _content;
        _content.Visible = true;

        _toastTimer.Tick += (_, _) => AdvanceToast();

        _pageFadeTimer.Tick += (_, _) =>
        {
            _pageFade += 0.14f;
            if (_pageFade >= 1f)
            {
                _pageFade = 1f;
                _pageFadeTimer.Stop();
                _fadeOverlay.Visible = false;
            }
            _fadeOverlay.Alpha = _pageFade;
        };

        ApplyTheme();
    }

    private void StartPageFade()
    {
        if (!IsHandleCreated || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }
        try
        {
            var bmp = new Bitmap(ClientSize.Width, ClientSize.Height);
            DrawToBitmap(bmp, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
            _fadeOverlay.Backdrop = _theme.Background;
            _fadeOverlay.SetFrame(bmp);
            _fadeOverlay.Bounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            _pageFade = 0f;
            _fadeOverlay.Alpha = 0f;
            _fadeOverlay.Visible = true;
            _fadeOverlay.BringToFront();
            _pageFadeTimer.Stop();
            _pageFadeTimer.Start();
        }
        catch
        {
            _fadeOverlay.Visible = false;
        }
    }

    private void BuildNavBar()
    {
        _navBar.Dock = DockStyle.Top;
        _avatar.Initial = InitialOf(_profile.UserName);
        _avatar.Avatar = _avatarImage;
        _userName.Text = _profile.UserName;

        _profilePanel.Controls.Add(_avatar);
        _profilePanel.Controls.Add(_userName);
        _profilePanel.Controls.Add(_levelLabel);

        _profilePanel.Click += (_, _) => ShowProfile();
        _avatar.Click += (_, _) => ShowProfile();
        _userName.Click += (_, _) => ShowProfile();
        _levelLabel.Click += (_, _) => ShowProfile();

        _libraryButton.Active = true;
        _libraryButton.Click += (_, _) => ShowLibrary();
        _profileButton.Click += (_, _) => ShowProfile();
        _settingsButton.Click += (_, _) => ShowSettings();

        _navCenter.Controls.Add(_libraryButton);
        _navCenter.Controls.Add(_profileButton);
        _navCenter.Controls.Add(_settingsButton);

        _navBar.Controls.Add(_navCenter);
        _navBar.Controls.Add(_clock);
        _navBar.Controls.Add(_profilePanel);
        _navBar.SizeChanged += (_, _) => CenterNav();
        _navCenter.SizeChanged += (_, _) => CenterNav();

        _clock.Text = DateTime.Now.ToString("HH:mm");
        _clockTimer.Tick += (_, _) =>
        {
            _clock.Text = DateTime.Now.ToString("HH:mm");
            if (_running.Count > 0)
            {
                RefreshStatsUi();
            }
        };
        _clockTimer.Start();

        CenterNav();
    }

    private void CenterNav()
    {
        _navCenter.Left = Math.Max(_profilePanel.Right + 12, (_navBar.Width - _navCenter.Width) / 2);
        _navCenter.Top = (_navBar.Height - _navCenter.Height) / 2;
    }

    private void BuildContent()
    {
        _searchIcon.Location = new Point(14, 13);
        _searchInput.Location = new Point(40, 13);
        _searchInput.Width = _searchBox.Width - 54;
        _searchInput.TextChanged += (_, _) =>
        {
            _search = _searchInput.Text.Trim();
            ApplySearch();
        };
        _searchBox.Controls.Add(_searchIcon);
        _searchBox.Controls.Add(_searchInput);

        _sortCombo.Items.AddRange(SortModes);
        _sortCombo.SelectedItem = SortModes.Contains(_sortMode) ? _sortMode : SortModes[0];
        _sortCombo.SelectedIndexChanged += (_, _) =>
        {
            _sortMode = _sortCombo.SelectedItem?.ToString() ?? "Nom";
            _profile.SortMode = _sortMode;
            SaveProfile();
            RenderLibrary();
        };

        _randomButton.Click += (_, _) => LaunchRandomGame();

        _sidebarFlow.Controls.Add(_searchBox);
        _sidebarFlow.Controls.Add(_resultLabel);
        _sidebarFlow.Controls.Add(_sortLabel);
        _sidebarFlow.Controls.Add(_sortCombo);
        _sidebarFlow.Controls.Add(_randomButton);

        var levelCard = CreateSideCard("NIVEAU", out _levelCardValue, height: 150, big: true);
        _levelProgress = new RoundedProgressBar { Left = 18, Top = 100, Width = 254, Height = 12 };
        _levelCardXp = new Label { AutoSize = true, Left = 18, Top = 118, Font = new Font("Segoe UI", 8.5f) };
        levelCard.Controls.Add(_levelProgress);
        levelCard.Controls.Add(_levelCardXp);

        var timeCard = CreateSideCard("TEMPS DE JEU", out _timeCardValue);
        var launchCard = CreateSideCard("PARTIES LANCÉES", out _launchCardValue);
        var libraryCard = CreateSideCard("JEUX", out _libraryCardValue);
        var favCard = CreateSideCard("PLUS JOUÉ", out _favCardValue);
        favCard.Height = 96;

        _sidebarFlow.Controls.Add(levelCard);
        _sidebarFlow.Controls.Add(timeCard);
        _sidebarFlow.Controls.Add(launchCard);
        _sidebarFlow.Controls.Add(libraryCard);
        _sidebarFlow.Controls.Add(favCard);
        _sidebar.Controls.Add(_sidebarFlow);

        _grid.MouseClick += (_, _) => _grid.Focus();

        _content.Controls.Add(_grid);
        _content.Controls.Add(_sidebar);
    }

    private RoundedPanel CreateSideCard(string caption, out Label value, int height = 92, bool big = false)
    {
        var card = new RoundedPanel { Width = 290, Height = height, Margin = new Padding(0, 0, 0, 14), CornerRadius = 16 };
        var head = new Label { Text = caption, AutoSize = true, Left = 18, Top = 14, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
        value = new Label { Text = "-", AutoSize = true, Left = 16, Top = 36, Font = new Font("Segoe UI", big ? 32 : 22, FontStyle.Bold) };
        card.Controls.Add(head);
        card.Controls.Add(value);
        card.Tag = head;
        return card;
    }

    private void BuildPages()
    {
        _detailPage.Back += ShowLibrary;
        _detailPage.Play += () => { if (_detailGame is not null) LaunchGame(_detailGame); };
        _detailPage.OpenLocation += () => { if (_detailGame is not null) OpenFileLocation(_detailGame); };
        _detailPage.Rename += () => { if (_detailGame is not null) RenameGame(_detailGame); };
        _detailPage.RemoveGame += () =>
        {
            if (_detailGame is not null)
            {
                var game = _detailGame;
                if (ConfirmRemove(game))
                {
                    ShowLibrary();
                    RemoveGame(game);
                }
            }
        };

        _settingsPage.Back += ShowLibrary;
        _settingsPage.PseudoChanged += name =>
        {
            _profile.UserName = name;
            _userName.Text = name;
            _avatar.Initial = InitialOf(name);
            _avatar.Invalidate();
            SaveProfile();
        };
        _settingsPage.AvatarChosen += ChangeAvatar;
        _settingsPage.AvatarRemoved += RemoveAvatar;
        _settingsPage.ThemeChosen += name =>
        {
            _themeName = name;
            _theme = _themeList[name];
            _profile.Theme = name;
            SaveProfile();
            ApplyTheme();
            RenderLibrary();
            RefreshStatsUi();
            _settingsPage.HighlightTheme(name);
        };
        _settingsPage.DataFolderRequested += OpenDataFolder;
        _settingsPage.StatsResetRequested += ResetStats;

        _profilePage.Back += ShowLibrary;
        _profilePage.PlayGame += path =>
        {
            var game = _games.FirstOrDefault(g => string.Equals(g.Path, path, StringComparison.OrdinalIgnoreCase));
            if (game is not null)
            {
                LaunchGame(game);
            }
        };
    }

    private void LayoutViews()
    {
        var area = ViewArea(_currentView != _content);
        _currentView.Bounds = area;
        PositionToast();
    }

    private Rectangle ViewArea(bool fullScreen)
    {
        var top = fullScreen ? 0 : _navBar.Height;
        return new Rectangle(0, top, ClientSize.Width, ClientSize.Height - top);
    }

    private void SwitchView(Control incoming, bool showNav)
    {
        _navBar.Visible = showNav;
        _libraryButton.Active = ReferenceEquals(incoming, _content);
        _libraryButton.Invalidate();
        _profileButton.Active = ReferenceEquals(incoming, _profilePage);
        _profileButton.Invalidate();
        _settingsButton.Active = ReferenceEquals(incoming, _settingsPage);
        _settingsButton.Invalidate();

        incoming.Bounds = ViewArea(!showNav);
        incoming.Visible = true;
        incoming.BringToFront();
        if (showNav)
        {
            _navBar.BringToFront();
        }
        _toast.BringToFront();

        foreach (var view in new Control[] { _content, _detailPage, _settingsPage, _profilePage })
        {
            if (!ReferenceEquals(view, incoming))
            {
                view.Visible = false;
            }
        }

        _currentView = incoming;
        StartPageFade();
    }

    private void ShowLibrary()
    {
        SwitchView(_content, showNav: true);
        _grid.Focus();
    }

    private void ShowSettings()
    {
        var themes = _themeList.Select(pair => (pair.Key, pair.Value.Accent)).ToList();
        _settingsPage.Bind(_profile.UserName, _avatarImage, themes, _themeName);
        SwitchView(_settingsPage, showNav: true);
    }

    private void ShowProfile()
    {
        BindProfile();
        SwitchView(_profilePage, showNav: true);
    }

    private void BindProfile()
    {
        var xp = _stats.TotalMinutes + LiveMinutesTotal() + _stats.TotalLaunches * 5d;
        var (level, into, needed) = LevelSystem.ComputeFromXp(xp);

        var top3 = _games
            .Select(g => (g.Name, g.IconPath, g.Path, Minutes: _stats.For(g.Path).Minutes + LiveMinutes(g.Path)))
            .Where(x => x.Minutes > 0)
            .OrderByDescending(x => x.Minutes)
            .Take(3)
            .Select(x => (x.Name, x.IconPath, x.Path, x.Minutes))
            .ToList();

        var achievements = Achievements.Catalog
            .Select(a => (a, Unlocked: _stats.Achievements.Contains(a.Id)))
            .ToList();

        _profilePage.Bind(_profile.UserName, _avatarImage, level, into, needed, top3, achievements, WeeklyData());
    }

    private List<(string Label, double Minutes)> WeeklyData()
    {
        var result = new List<(string, double)>();
        var today = DateTime.Now.Date;
        string[] days = { "Dim", "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam" };
        for (var i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var key = day.ToString("yyyy-MM-dd");
            var minutes = _stats.DailyMinutes.GetValueOrDefault(key);
            if (day == today)
            {
                minutes += LiveMinutesTotal();
            }
            result.Add((days[(int)day.DayOfWeek], minutes));
        }
        return result;
    }

    private void OpenDetail(GameEntry game)
    {
        _detailGame = game;
        var color = TilePalette[Math.Abs(StableHash(game.Name)) % TilePalette.Length];
        _detailPage.Bind(game, EffectiveStat(game.Path), color, LoadGameIcon(game));
        SwitchView(_detailPage, showNav: false);
    }

    private void ApplyTheme()
    {
        MenuTheme.Current = _theme;
        BackColor = _theme.Background;

        _navBar.BackColor = _theme.Panel;
        _profilePanel.BackColor = _theme.Panel;
        _avatar.BackColor = _theme.Panel;
        _avatar.RingColor = _theme.Accent;
        _avatar.ForeColor = Color.White;
        _userName.BackColor = _theme.Panel;
        _userName.ForeColor = _theme.Text;
        _levelLabel.BackColor = _theme.Panel;
        _levelLabel.ForeColor = _theme.Muted;
        _clock.BackColor = _theme.Panel;
        _clock.ForeColor = _theme.Text;
        _navCenter.BackColor = _theme.Panel;

        foreach (var nav in new[] { _libraryButton, _profileButton, _settingsButton })
        {
            nav.SurroundColor = _theme.Panel;
            nav.AccentColor = _theme.Accent;
            nav.TextColor = _theme.Text;
            nav.MutedColor = _theme.Muted;
            nav.Invalidate();
        }

        _content.BackColor = _theme.Background;
        _grid.BackColor = _theme.Background;

        _sidebar.BackColor = _theme.Panel;
        _sidebarFlow.BackColor = _theme.Panel;
        _sortLabel.BackColor = _theme.Panel;
        _sortLabel.ForeColor = _theme.Muted;
        _resultLabel.BackColor = _theme.Panel;
        _resultLabel.ForeColor = _theme.Muted;
        _sortCombo.BackColor = _theme.Card;
        _sortCombo.ForeColor = _theme.Text;

        _randomButton.BaseColor = _theme.Card;
        _randomButton.HoverColor = UiHelpers.Blend(_theme.Card, _theme.Accent, 0.45f);
        _randomButton.SurroundColor = _theme.Panel;
        _randomButton.ForeColor = _theme.Text;
        _randomButton.Invalidate();

        _searchBox.FillColor = _theme.Card;
        _searchBox.SurroundColor = _theme.Panel;
        _searchBox.Invalidate();
        _searchIcon.BackColor = _theme.Card;
        _searchIcon.ForeColor = _theme.Muted;
        _searchInput.BackColor = _theme.Card;
        _searchInput.ForeColor = _theme.Text;

        foreach (Control child in _sidebarFlow.Controls)
        {
            if (child is RoundedPanel rounded && !ReferenceEquals(rounded, _searchBox))
            {
                rounded.FillColor = _theme.Card;
                rounded.SurroundColor = _theme.Panel;
                foreach (Control inner in rounded.Controls)
                {
                    if (inner is RoundedProgressBar)
                    {
                        continue;
                    }
                    inner.BackColor = _theme.Card;
                    inner.ForeColor = ReferenceEquals(inner, rounded.Tag) ? _theme.Muted : _theme.Text;
                }
                rounded.Invalidate();
            }
        }

        _levelProgress.TrackColor = UiHelpers.Blend(_theme.Card, _theme.Background, 0.5f);
        _levelProgress.FillColor = _theme.Accent;
        _levelProgress.Invalidate();
        if (_levelCardXp is not null)
        {
            _levelCardXp.ForeColor = _theme.Muted;
        }

        _detailPage.SetTheme(_theme);
        _settingsPage.SetTheme(_theme);
        _profilePage.SetTheme(_theme);
        _toast.SetTheme(_theme);

        _avatar.Invalidate();
        Invalidate(true);
    }

    private void ChangeAvatar(string sourcePath)
    {
        try
        {
            Directory.CreateDirectory(AppFolder);
            var destination = Path.Combine(AppFolder, "avatar.png");
            using (var source = Image.FromFile(sourcePath))
            using (var copy = new Bitmap(source))
            {
                _avatarImage?.Dispose();
                _avatarImage = new Bitmap(copy);
                copy.Save(destination, ImageFormat.Png);
            }
            _profile.AvatarPath = destination;
            SaveProfile();
            _avatar.Avatar = _avatarImage;
            _avatar.Invalidate();
            _settingsPage.SetAvatar(_avatarImage);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Image invalide : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RemoveAvatar()
    {
        _avatarImage?.Dispose();
        _avatarImage = null;
        _profile.AvatarPath = string.Empty;
        SaveProfile();
        _avatar.Avatar = null;
        _avatar.Invalidate();
        _settingsPage.SetAvatar(null);
    }

    private void PickGame()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choisir un jeu ou un raccourci",
            Filter = "Jeux et raccourcis (*.exe;*.lnk)|*.exe;*.lnk|Tous les fichiers (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddFiles(dialog.FileNames, showErrors: true);
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
        {
            AddFiles(files, showErrors: true);
        }
    }

    private void AddFiles(IEnumerable<string> files, bool showErrors)
    {
        var added = false;
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                continue;
            }

            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension is not ".exe" and not ".lnk")
            {
                if (showErrors)
                {
                    MessageBox.Show(this, "Ajoute uniquement un fichier .exe ou un raccourci .lnk.", "Type de fichier non supporté", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                continue;
            }

            var launchPath = file;
            var iconSource = ShortcutResolver.TryResolveTarget(file) ?? file;
            if (_games.Any(game => string.Equals(game.Path, launchPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(iconSource);
            var iconPath = IconImporter.ImportIcon(iconSource, IconFolder, name);
            _games.Add(new GameEntry(name, launchPath, iconPath));
            added = true;
        }

        if (added)
        {
            SaveGames();
            RenderLibrary();
            RefreshStatsUi();
            CheckAchievements();
        }
    }

    private IEnumerable<GameEntry> SortedGames()
    {
        var sorted = _sortMode switch
        {
            "Temps de jeu" => _games.OrderByDescending(g => _stats.For(g.Path).Minutes + LiveMinutes(g.Path)),
            "Lancements" => _games.OrderByDescending(g => _stats.For(g.Path).Launches),
            "Récent" => _games.OrderByDescending(g => _stats.For(g.Path).LastPlayed ?? DateTime.MinValue),
            _ => _games.OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
        };
        return sorted.OrderByDescending(g => IsFavorite(g.Path) ? 1 : 0);
    }

    private void RenderLibrary()
    {
        _grid.SuspendLayout();
        _grid.Controls.Clear();
        _tiles.Clear();

        var delay = 0;
        foreach (var game in SortedGames())
        {
            var tile = CreateTile(game);
            _tiles.Add(tile);
            _grid.Controls.Add(tile);
            tile.StartAppear(delay);
            delay += 25;
        }

        var addTile = new GameTile(null, TilePalette[0], _theme.Accent, _theme.Background, _theme.Text) { IsAddTile = true };
        addTile.Launch += _ => PickGame();
        _grid.Controls.Add(addTile);
        addTile.StartAppear(delay);

        _grid.ResumeLayout();

        ApplySearch();

        if (_tiles.Count > 0)
        {
            SelectIndex(Math.Clamp(_selectedIndex < 0 ? 0 : _selectedIndex, 0, _tiles.Count - 1));
        }
        else
        {
            _selectedIndex = -1;
        }
    }

    private void ApplySearch()
    {
        var visible = 0;
        if (string.IsNullOrEmpty(_search))
        {
            foreach (var tile in _tiles)
            {
                tile.Visible = true;
                visible++;
            }
        }
        else
        {
            foreach (var tile in _tiles)
            {
                tile.Visible = tile.Game is not null &&
                    tile.Game.Name.Contains(_search, StringComparison.OrdinalIgnoreCase);
                if (tile.Visible)
                {
                    visible++;
                }
            }
        }
        UpdateResultLabel(visible);
    }

    private void UpdateResultLabel(int visible)
    {
        if (string.IsNullOrEmpty(_search))
        {
            _resultLabel.Text = _games.Count <= 1 ? $"{_games.Count} jeu" : $"{_games.Count} jeux";
        }
        else
        {
            _resultLabel.Text = visible <= 1 ? $"{visible} résultat" : $"{visible} résultats";
        }
    }

    private GameTile CreateTile(GameEntry game)
    {
        var color = TilePalette[Math.Abs(StableHash(game.Name)) % TilePalette.Length];
        var tile = new GameTile(game, color, _theme.Accent, _theme.Background, _theme.Text)
        {
            IsFavorite = IsFavorite(game.Path)
        };
        tile.Open += t => { SelectIndex(_tiles.IndexOf(t)); OpenDetail(t.Game!); };
        tile.Launch += t => LaunchGame(t.Game!);
        tile.Rename += t => RenameGame(t.Game!);
        tile.OpenLocation += t => OpenFileLocation(t.Game!);
        tile.ToggleFavorite += t => ToggleFavorite(t.Game!);
        tile.Remove += t => { if (ConfirmRemove(t.Game!)) RemoveGame(t.Game!); };
        return tile;
    }

    private void SelectIndex(int index)
    {
        if (index < 0 || index >= _tiles.Count)
        {
            return;
        }
        _selectedIndex = index;
        for (var i = 0; i < _tiles.Count; i++)
        {
            _tiles[i].SetSelected(i == index);
        }
        _grid.ScrollControlIntoView(_tiles[index]);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.F)
        {
            if (!ReferenceEquals(_currentView, _content))
            {
                ShowLibrary();
            }
            _searchInput.Focus();
            _searchInput.SelectAll();
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }

        if (!ReferenceEquals(_currentView, _content))
        {
            if (e.KeyCode == Keys.Escape)
            {
                ShowLibrary();
                e.Handled = true;
            }
            return;
        }

        if (_searchInput.Focused)
        {
            if (e.KeyCode == Keys.Escape)
            {
                _searchInput.Clear();
                _grid.Focus();
                e.Handled = true;
            }
            return;
        }

        if (_tiles.Count == 0)
        {
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Right:
                SelectIndex(Math.Min(_selectedIndex + 1, _tiles.Count - 1));
                e.Handled = true;
                break;
            case Keys.Left:
                SelectIndex(Math.Max(_selectedIndex - 1, 0));
                e.Handled = true;
                break;
            case Keys.Enter:
                if (_selectedIndex >= 0)
                {
                    OpenDetail(_tiles[_selectedIndex].Game!);
                }
                e.Handled = true;
                break;
            case Keys.Delete:
                if (_selectedIndex >= 0 && ConfirmRemove(_tiles[_selectedIndex].Game!))
                {
                    RemoveGame(_tiles[_selectedIndex].Game!);
                }
                e.Handled = true;
                break;
        }
    }

    private void LaunchGame(GameEntry game)
    {
        if (!File.Exists(game.Path))
        {
            MessageBox.Show(this, "Ce jeu ou raccourci n'existe plus.", "Introuvable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Process? process;
        try
        {
            process = Process.Start(new ProcessStartInfo(game.Path)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(game.Path) ?? Environment.CurrentDirectory
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossible de lancer le jeu : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _stats.TotalLaunches++;
        var stat = _stats.For(game.Path);
        stat.Launches++;
        stat.LastPlayed = DateTime.Now;
        SaveStats();

        if (process is not null)
        {
            var session = new RunningSession { Process = process, Path = game.Path };
            _running.Add(session);
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) =>
            {
                try
                {
                    BeginInvoke(() => OnGameClosed(session));
                }
                catch (InvalidOperationException)
                {
                    // Form handle already gone; nothing to update.
                }
            };
        }

        RefreshStatsUi();
        RefreshDetailIfOpen(game);
        CheckAchievements();
    }

    private void OnGameClosed(RunningSession session)
    {
        _running.Remove(session);
        var minutes = (DateTime.UtcNow - session.StartUtc).TotalMinutes;
        session.Process.Dispose();
        if (minutes < 0)
        {
            minutes = 0;
        }
        _stats.TotalMinutes += minutes;
        _stats.For(session.Path).Minutes += minutes;

        var key = DateTime.Now.ToString("yyyy-MM-dd");
        _stats.DailyMinutes[key] = _stats.DailyMinutes.GetValueOrDefault(key) + minutes;

        SaveStats();
        RefreshStatsUi();
        if (_detailGame is not null && string.Equals(_detailGame.Path, session.Path, StringComparison.OrdinalIgnoreCase))
        {
            RefreshDetailIfOpen(_detailGame);
        }
        CheckAchievements();
    }

    private void CheckAchievements()
    {
        var xp = _stats.TotalMinutes + _stats.TotalLaunches * 5d;
        var (level, _, _) = LevelSystem.ComputeFromXp(xp);

        foreach (var achievement in Achievements.Catalog)
        {
            if (!_stats.Achievements.Contains(achievement.Id) &&
                Achievements.IsUnlocked(achievement.Id, _stats, _games.Count, level))
            {
                _stats.Achievements.Add(achievement.Id);
                _toastQueue.Enqueue(achievement);
            }
        }

        if (_toastQueue.Count > 0)
        {
            SaveStats();
            if (!_toast.Visible)
            {
                AdvanceToast();
            }
        }
    }

    private void AdvanceToast()
    {
        _toastTimer.Stop();
        if (_toastQueue.Count == 0)
        {
            _toast.Visible = false;
            return;
        }

        var achievement = _toastQueue.Dequeue();
        _toast.Show(achievement.Icon, achievement.Title);
        PositionToast();
        _toast.Visible = true;
        _toast.BringToFront();
        _toastTimer.Start();
    }

    private void PositionToast()
    {
        _toast.Location = new Point(ClientSize.Width - _toast.Width - 24, ClientSize.Height - _toast.Height - 24);
    }

    private double LiveMinutes(string path) =>
        _running.Where(s => string.Equals(s.Path, path, StringComparison.OrdinalIgnoreCase))
                .Sum(s => (DateTime.UtcNow - s.StartUtc).TotalMinutes);

    private double LiveMinutesTotal() =>
        _running.Sum(s => (DateTime.UtcNow - s.StartUtc).TotalMinutes);

    private GameStat EffectiveStat(string path)
    {
        var stored = _stats.For(path);
        return new GameStat { Launches = stored.Launches, Minutes = stored.Minutes + LiveMinutes(path), LastPlayed = stored.LastPlayed };
    }

    private void RefreshDetailIfOpen(GameEntry game)
    {
        if (ReferenceEquals(_currentView, _detailPage) && _detailGame is not null && ReferenceEquals(_detailGame, game))
        {
            _detailPage.UpdateStats(EffectiveStat(game.Path));
        }
    }

    private bool ConfirmRemove(GameEntry game)
    {
        return MessageBox.Show(this, $"Retirer « {game.Name} » de la bibliothèque ?\n(Le fichier du jeu n'est pas supprimé.)",
            "Confirmer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    private void RemoveGame(GameEntry game)
    {
        _games.Remove(game);
        SaveGames();
        RenderLibrary();
        RefreshStatsUi();
    }

    private void RenameGame(GameEntry game)
    {
        var newName = InputDialog.Ask(this, "Renommer le jeu", "Nouveau nom :", game.Name, _theme);
        if (string.IsNullOrWhiteSpace(newName) || newName == game.Name)
        {
            return;
        }

        var index = _games.IndexOf(game);
        if (index < 0)
        {
            return;
        }
        _games[index] = game with { Name = newName.Trim() };
        SaveGames();
        RenderLibrary();
        if (_detailGame is not null && ReferenceEquals(_detailGame, game))
        {
            _detailGame = _games[index];
            OpenDetail(_detailGame);
        }
    }

    private void OpenFileLocation(GameEntry game)
    {
        if (!File.Exists(game.Path))
        {
            MessageBox.Show(this, "Le fichier n'existe plus.", "Introuvable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{game.Path}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossible d'ouvrir l'emplacement : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private bool IsFavorite(string path) =>
        _profile.Favorites.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));

    private void ToggleFavorite(GameEntry game)
    {
        if (IsFavorite(game.Path))
        {
            _profile.Favorites.RemoveAll(p => string.Equals(p, game.Path, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            _profile.Favorites.Add(game.Path);
        }
        SaveProfile();
        RenderLibrary();
    }

    private void LaunchRandomGame()
    {
        var available = _games.Where(g => File.Exists(g.Path)).ToList();
        if (available.Count == 0)
        {
            available = _games.ToList();
        }
        if (available.Count == 0)
        {
            MessageBox.Show(this, "Ajoute d'abord un jeu à ta bibliothèque.", "Bibliothèque vide", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var pick = available[Random.Shared.Next(available.Count)];
        var index = _tiles.FindIndex(t => ReferenceEquals(t.Game, pick));
        if (index >= 0)
        {
            SelectIndex(index);
        }
        OpenDetail(pick);
    }

    private void ResetStats()
    {
        if (MessageBox.Show(this, "Remettre à zéro toutes les statistiques ?\n(Temps de jeu, niveau, trophées et activité seront effacés.)",
            "Confirmer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }
        _stats.TotalLaunches = 0;
        _stats.TotalMinutes = 0;
        _stats.PerGame.Clear();
        _stats.DailyMinutes.Clear();
        _stats.Achievements.Clear();
        SaveStats();
        RefreshStatsUi();
        RenderLibrary();
    }

    private void OpenDataFolder()
    {
        try
        {
            Directory.CreateDirectory(AppFolder);
            Process.Start(new ProcessStartInfo(AppFolder) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Impossible d'ouvrir le dossier : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            _prevBorder = FormBorderStyle;
            _prevWindowState = WindowState;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Normal;
            WindowState = FormWindowState.Maximized;
        }
        else
        {
            FormBorderStyle = _prevBorder;
            WindowState = _prevWindowState;
        }
        _fullscreen = !_fullscreen;
    }

    private void RefreshStatsUi()
    {
        var totalMinutes = _stats.TotalMinutes + LiveMinutesTotal();
        var xp = totalMinutes + _stats.TotalLaunches * 5d;
        var (level, into, needed) = LevelSystem.ComputeFromXp(xp);

        _levelLabel.Text = $"niveau {level}";
        _avatar.Level = level;

        if (_levelCardValue is not null)
        {
            _levelCardValue.Text = level.ToString();
            _levelProgress.Value = needed <= 0 ? 0f : (float)(into / needed);
            _levelProgress.Invalidate();
            _levelCardXp.Text = $"{(int)into} / {(int)needed} XP";

            _timeCardValue.Text = FormatTime(totalMinutes);
            _launchCardValue.Text = _stats.TotalLaunches.ToString();
            _libraryCardValue.Text = _games.Count.ToString();
            _favCardValue.Text = FindFavorite();
        }

        if (ReferenceEquals(_currentView, _detailPage) && _detailGame is not null)
        {
            _detailPage.UpdateStats(EffectiveStat(_detailGame.Path));
        }
    }

    private static string FormatTime(double totalMinutes)
    {
        if (totalMinutes <= 0)
        {
            return "0 min";
        }
        if (totalMinutes < 1)
        {
            return "< 1 min";
        }
        var hours = (int)(totalMinutes / 60);
        var mins = (int)(totalMinutes % 60);
        return hours > 0 ? $"{hours}h {mins:00}" : $"{mins} min";
    }

    private string FindFavorite()
    {
        string? bestPath = null;
        var best = -1d;
        foreach (var pair in _stats.PerGame)
        {
            var minutes = pair.Value.Minutes + LiveMinutes(pair.Key);
            if (minutes > best)
            {
                best = minutes;
                bestPath = pair.Key;
            }
        }
        if (bestPath is null)
        {
            return "—";
        }
        var match = _games.FirstOrDefault(g => string.Equals(g.Path, bestPath, StringComparison.OrdinalIgnoreCase));
        return match?.Name ?? "—";
    }

    private static Image? LoadGameIcon(GameEntry game)
    {
        if (!File.Exists(game.IconPath))
        {
            return null;
        }
        try
        {
            using var source = Image.FromFile(game.IconPath);
            return new Bitmap(source);
        }
        catch
        {
            return null;
        }
    }

    private static string InitialOf(string name) =>
        string.IsNullOrWhiteSpace(name) ? "?" : name.Trim().Substring(0, 1).ToUpperInvariant();

    private void LoadGames()
    {
        if (!File.Exists(DataFile))
        {
            return;
        }

        var loaded = JsonSerializer.Deserialize<List<GameEntry>>(File.ReadAllText(DataFile));
        if (loaded is not null)
        {
            _games.AddRange(loaded);
        }
    }

    private void SaveGames()
    {
        Directory.CreateDirectory(AppFolder);
        File.WriteAllText(DataFile, JsonSerializer.Serialize(_games, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void LoadStats()
    {
        if (!File.Exists(StatsFile))
        {
            return;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<LauncherStats>(File.ReadAllText(StatsFile));
            if (loaded is not null)
            {
                _stats.TotalLaunches = loaded.TotalLaunches;
                _stats.TotalMinutes = loaded.TotalMinutes;
                _stats.PerGame.Clear();
                foreach (var pair in loaded.PerGame)
                {
                    _stats.PerGame[pair.Key] = pair.Value;
                }
                _stats.DailyMinutes = loaded.DailyMinutes ?? new Dictionary<string, double>();
                _stats.Achievements = loaded.Achievements ?? new List<string>();
            }
        }
        catch (JsonException)
        {
            // Corrupt stats file; start fresh.
        }
    }

    private void SaveStats()
    {
        Directory.CreateDirectory(AppFolder);
        File.WriteAllText(StatsFile, JsonSerializer.Serialize(_stats, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void LoadProfile()
    {
        _profile.UserName = Environment.UserName;
        if (File.Exists(ProfileFile))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<Profile>(File.ReadAllText(ProfileFile));
                if (loaded is not null)
                {
                    if (!string.IsNullOrWhiteSpace(loaded.UserName))
                    {
                        _profile.UserName = loaded.UserName;
                    }
                    _profile.AvatarPath = loaded.AvatarPath;
                    _profile.Theme = loaded.Theme;
                    _profile.SortMode = loaded.SortMode;
                    _profile.Favorites = loaded.Favorites ?? new List<string>();
                    if (!string.IsNullOrWhiteSpace(loaded.Theme) && _themeList.ContainsKey(loaded.Theme))
                    {
                        _themeName = loaded.Theme;
                    }
                    if (SortModes.Contains(loaded.SortMode))
                    {
                        _sortMode = loaded.SortMode;
                    }
                }
            }
            catch (JsonException)
            {
                // Corrupt profile; keep defaults.
            }
        }

        if (!string.IsNullOrEmpty(_profile.AvatarPath) && File.Exists(_profile.AvatarPath))
        {
            try
            {
                using var source = Image.FromFile(_profile.AvatarPath);
                _avatarImage = new Bitmap(source);
            }
            catch
            {
                _avatarImage = null;
            }
        }
    }

    private void SaveProfile()
    {
        Directory.CreateDirectory(AppFolder);
        _profile.Theme = _themeName;
        _profile.SortMode = _sortMode;
        File.WriteAllText(ProfileFile, JsonSerializer.Serialize(_profile, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void TryLoadAppIcon()
    {
        try
        {
            var icoPath = Path.Combine(AppDir, "app.ico");
            if (File.Exists(icoPath))
            {
                Icon = new Icon(icoPath);
            }
        }
        catch
        {
            // Icon is optional; ignore if it cannot be loaded.
        }
    }

    private void TryRoundWindowCorners()
    {
        try
        {
            var preference = 2; // DWMWCP_ROUND
            _ = NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }
        catch
        {
            // Rounded window corners are only available on Windows 11; ignore elsewhere.
        }
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var c in value)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }
}

internal static class UiHelpers
{
    public static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0f || rect.Width <= 0f || rect.Height <= 0f)
        {
            path.AddRectangle(rect);
            path.CloseFigure();
            return path;
        }

        var diameter = Math.Min(radius * 2f, Math.Min(rect.Width, rect.Height));
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Color Blend(Color from, Color to, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(from.A + (to.A - from.A) * t),
            (int)(from.R + (to.R - from.R) * t),
            (int)(from.G + (to.G - from.G) * t),
            (int)(from.B + (to.B - from.B) * t));
    }

    public static Color ContrastText(Color background)
    {
        var luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255d;
        return luminance > 0.6 ? Color.FromArgb(20, 24, 30) : Color.White;
    }

    public static Rectangle FitInside(Size image, Rectangle bounds)
    {
        if (image.Width <= 0 || image.Height <= 0)
        {
            return bounds;
        }
        var scale = Math.Min((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
        var width = (int)(image.Width * scale);
        var height = (int)(image.Height * scale);
        return new Rectangle(bounds.X + (bounds.Width - width) / 2, bounds.Y + (bounds.Height - height) / 2, width, height);
    }

    public static Rectangle CoverInside(Size image, Rectangle bounds)
    {
        if (image.Width <= 0 || image.Height <= 0)
        {
            return bounds;
        }
        var scale = Math.Max((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
        var width = (int)Math.Ceiling(image.Width * scale);
        var height = (int)Math.Ceiling(image.Height * scale);
        return new Rectangle(bounds.X + (bounds.Width - width) / 2, bounds.Y + (bounds.Height - height) / 2, width, height);
    }
}

internal class RoundedPanel : Panel
{
    private int _cornerRadius = 16;

    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = value; Invalidate(); }
    }

    public Color FillColor { get; set; } = Color.White;
    public Color BorderColor { get; set; } = Color.Empty;
    public float BorderWidth { get; set; }
    public Color SurroundColor { get; set; } = Color.Empty;
    public bool Glass { get; set; } = true;

    public RoundedPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var backdrop = SurroundColor.IsEmpty ? (Parent?.BackColor ?? BackColor) : SurroundColor;
        g.Clear(backdrop);

        var inset = Math.Max(BorderWidth, 0.5f);
        var rect = new RectangleF(inset, inset, Width - inset * 2f, Height - inset * 2f);
        using var path = UiHelpers.RoundedRect(rect, CornerRadius);
        using (var brush = new SolidBrush(FillColor))
        {
            g.FillPath(brush, path);
        }

        if (Glass && rect.Height > 6f)
        {
            var lum = (0.299 * FillColor.R + 0.587 * FillColor.G + 0.114 * FillColor.B) / 255d;
            var sheenAlpha = lum <= 0.55 ? 30 : 18;
            g.SetClip(path);
            var sheenRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height * 0.45f);
            using (var sheen = new LinearGradientBrush(
                       new RectangleF(sheenRect.X, sheenRect.Y - 1, sheenRect.Width, sheenRect.Height + 1),
                       Color.FromArgb(sheenAlpha, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
            {
                g.FillRectangle(sheen, sheenRect);
            }
            g.ResetClip();
            using var edge = new Pen(Color.FromArgb(lum <= 0.55 ? 26 : 40, 255, 255, 255), 1f);
            g.DrawPath(edge, path);
        }

        if (BorderWidth > 0f && !BorderColor.IsEmpty)
        {
            using var pen = new Pen(BorderColor, BorderWidth);
            g.DrawPath(pen, path);
        }
    }
}

internal sealed class RoundedProgressBar : Control
{
    private float _value;

    public float Value
    {
        get => _value;
        set { _value = Math.Clamp(value, 0f, 1f); Invalidate(); }
    }

    public Color TrackColor { get; set; } = Color.Gray;
    public Color FillColor { get; set; } = Color.MediumSlateBlue;

    public RoundedProgressBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Parent?.BackColor ?? BackColor);

        var radius = Height / 2f;
        var track = new RectangleF(0, 0, Width, Height);
        using (var trackPath = UiHelpers.RoundedRect(track, radius))
        using (var brush = new SolidBrush(TrackColor))
        {
            g.FillPath(brush, trackPath);
        }

        if (_value > 0f)
        {
            var fill = new RectangleF(0, 0, Math.Max(Height, Width * _value), Height);
            using var fillPath = UiHelpers.RoundedRect(fill, radius);
            using var brush = new SolidBrush(FillColor);
            g.FillPath(brush, fillPath);
        }
    }
}

internal sealed class RoundedButton : Button
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private float _hover;
    private bool _pointerInside;

    public int CornerRadius { get; set; } = 12;
    public Color BaseColor { get; set; } = Color.MediumSlateBlue;
    public Color HoverColor { get; set; } = Color.SlateBlue;
    public Color SurroundColor { get; set; } = Color.Empty;

    public RoundedButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        Cursor = Cursors.Hand;

        _timer.Tick += (_, _) =>
        {
            var target = _pointerInside ? 1f : 0f;
            _hover += (target - _hover) * 0.22f;
            if (Math.Abs(target - _hover) < 0.01f)
            {
                _hover = target;
                _timer.Stop();
            }
            Invalidate();
        };
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _pointerInside = true;
        _timer.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _pointerInside = false;
        _timer.Start();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var backdrop = SurroundColor.IsEmpty ? (Parent?.BackColor ?? BackColor) : SurroundColor;
        g.Clear(backdrop);

        var color = UiHelpers.Blend(BaseColor, HoverColor, _hover);
        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using var path = UiHelpers.RoundedRect(rect, CornerRadius);
        using (var brush = new SolidBrush(color))
        {
            g.FillPath(brush, path);
        }

        // Liquid-glass sheen over the top half.
        g.SetClip(path);
        var sheenRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height * 0.5f);
        using (var sheen = new LinearGradientBrush(
                   new RectangleF(sheenRect.X, sheenRect.Y - 1, sheenRect.Width, sheenRect.Height + 1),
                   Color.FromArgb(70 + (int)(45 * _hover), 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
        {
            g.FillRectangle(sheen, sheenRect);
        }
        g.ResetClip();

        using (var pen = new Pen(Color.FromArgb(70, 255, 255, 255), 1.2f))
        {
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class NavButton : Button
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private float _hover;
    private bool _pointerInside;

    public bool Active { get; set; }
    public Color AccentColor { get; set; } = Color.MediumSlateBlue;
    public Color TextColor { get; set; } = Color.White;
    public Color MutedColor { get; set; } = Color.Gray;
    public Color SurroundColor { get; set; } = Color.Empty;

    public NavButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 11, FontStyle.Regular);
        Margin = new Padding(4, 0, 4, 0);

        _timer.Tick += (_, _) =>
        {
            var target = _pointerInside ? 1f : 0f;
            _hover += (target - _hover) * 0.2f;
            if (Math.Abs(target - _hover) < 0.01f)
            {
                _hover = target;
                _timer.Stop();
            }
            Invalidate();
        };
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _pointerInside = true;
        _timer.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _pointerInside = false;
        _timer.Start();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var backdrop = SurroundColor.IsEmpty ? (Parent?.BackColor ?? BackColor) : SurroundColor;
        g.Clear(backdrop);

        var textColor = Active
            ? TextColor
            : UiHelpers.Blend(MutedColor, TextColor, _hover);
        var font = Active ? new Font(Font, FontStyle.Bold) : Font;
        TextRenderer.DrawText(g, Text, font, new Rectangle(0, 0, Width, Height - 6), textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Active)
        {
            font.Dispose();
        }

        var underlineAlpha = Active ? 1f : _hover * 0.4f;
        if (underlineAlpha > 0.01f)
        {
            var barWidth = Math.Min(Width - 24, 46);
            var barRect = new RectangleF((Width - barWidth) / 2f, Height - 5f, barWidth, 3f);
            using var barPath = UiHelpers.RoundedRect(barRect, 1.5f);
            using var brush = new SolidBrush(Color.FromArgb((int)(underlineAlpha * 255), AccentColor));
            g.FillPath(brush, barPath);
        }
    }
}

internal sealed class GlassButton : Button
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private float _hover;
    private bool _pointerInside;

    public int CornerRadius { get; set; } = 0; // 0 => full pill
    public Color BaseBackground { get; set; } = Color.Black;
    public Color Glow { get; set; } = Color.MediumSlateBlue;

    public GlassButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        Cursor = Cursors.Hand;

        _timer.Tick += (_, _) =>
        {
            var target = _pointerInside ? 1f : 0f;
            _hover += (target - _hover) * 0.2f;
            if (Math.Abs(target - _hover) < 0.01f)
            {
                _hover = target;
                _timer.Stop();
            }
            Invalidate();
        };
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _pointerInside = true;
        _timer.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _pointerInside = false;
        _timer.Start();
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(BaseBackground);

        var radius = CornerRadius <= 0 ? Height / 2f : CornerRadius;
        var rect = new RectangleF(1f, 1f, Width - 2f, Height - 2f);
        using var path = UiHelpers.RoundedRect(rect, radius);

        var lum = (0.299 * BaseBackground.R + 0.587 * BaseBackground.G + 0.114 * BaseBackground.B) / 255d;
        var dark = lum <= 0.55;

        var top = (dark ? 0.14f : 0.60f) + _hover * (dark ? 0.12f : 0.18f);
        var bottom = (dark ? 0.04f : 0.28f) + _hover * 0.05f;
        var c1 = UiHelpers.Blend(BaseBackground, Color.White, top);
        var c2 = UiHelpers.Blend(BaseBackground, Color.White, bottom);
        using (var grad = new LinearGradientBrush(new RectangleF(rect.X, rect.Y - 1, rect.Width, rect.Height + 2), c1, c2, LinearGradientMode.Vertical))
        {
            g.FillPath(grad, path);
        }

        if (_hover > 0.01f)
        {
            using var glow = new SolidBrush(Color.FromArgb((int)(40 * _hover), Glow));
            g.FillPath(glow, path);
        }

        // Glossy sheen across the top half.
        g.SetClip(path);
        var sheenRect = new RectangleF(rect.X, rect.Y, rect.Width, rect.Height * 0.55f);
        using (var sheen = new LinearGradientBrush(
                   new RectangleF(sheenRect.X, sheenRect.Y - 1, sheenRect.Width, sheenRect.Height + 1),
                   Color.FromArgb(dark ? 70 : 150, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Vertical))
        {
            g.FillRectangle(sheen, sheenRect);
        }
        g.ResetClip();

        using (var pen = new Pen(Color.FromArgb(dark ? 95 : 160, 255, 255, 255), 1.2f))
        {
            g.DrawPath(pen, path);
        }

        var textColor = dark ? Color.White : Color.FromArgb(28, 32, 40);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class AvatarCircle : Control
{
    public int Level { get; set; } = 1;
    public Color RingColor { get; set; } = Color.MediumSlateBlue;
    public string Initial { get; set; } = "?";
    public Image? Avatar { get; set; }

    public AvatarCircle()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Parent?.BackColor ?? BackColor);

        var rect = new Rectangle(2, 2, Width - 5, Height - 5);
        using var circle = new GraphicsPath();
        circle.AddEllipse(rect);

        if (Avatar is not null)
        {
            g.SetClip(circle);
            var cover = UiHelpers.CoverInside(Avatar.Size, rect);
            g.DrawImage(Avatar, cover);
            g.ResetClip();
        }
        else
        {
            using (var fill = new SolidBrush(UiHelpers.Blend(RingColor, Color.Black, 0.35f)))
            {
                g.FillEllipse(fill, rect);
            }
            TextRenderer.DrawText(g, Initial, new Font("Segoe UI", Height / 3, FontStyle.Bold), new Rectangle(0, 0, Width, Height), ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        using var pen = new Pen(RingColor, 3f);
        g.DrawEllipse(pen, rect);
    }
}

internal sealed class GameTile : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private float _highlight;
    private float _appear = 1f;
    private int _appearAt;
    private bool _pointerInside;
    private bool _selected;
    private readonly Image? _icon;
    private readonly bool _missing;

    private readonly Color _accent;
    private readonly Color _surround;
    private readonly Color _textColor;
    private static readonly Color IconBackdrop = Color.FromArgb(26, 27, 33);

    public GameEntry? Game { get; }
    public bool IsAddTile { get; set; }
    public bool IsFavorite { get; set; }

    public event Action<GameTile>? Open;
    public event Action<GameTile>? Launch;
    public event Action<GameTile>? Remove;
    public event Action<GameTile>? Rename;
    public event Action<GameTile>? OpenLocation;
    public event Action<GameTile>? ToggleFavorite;

    public GameTile(GameEntry? game, Color tileColor, Color accent, Color surround, Color textColor)
    {
        Game = game;
        _accent = accent;
        _surround = surround;
        _textColor = textColor;
        _missing = game is not null && !File.Exists(game.Path);

        Width = 188;
        Height = 250;
        Margin = new Padding(12, 0, 12, 24);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        if (game is not null && File.Exists(game.IconPath))
        {
            try
            {
                using var source = Image.FromFile(game.IconPath);
                _icon = new Bitmap(source);
            }
            catch
            {
                _icon = null;
            }
        }

        _timer.Tick += (_, _) =>
        {
            var target = (_selected || _pointerInside) ? 1f : 0f;
            _highlight += (target - _highlight) * 0.22f;
            var highlightDone = Math.Abs(target - _highlight) < 0.01f;
            if (highlightDone)
            {
                _highlight = target;
            }

            var appearDone = true;
            if (_appear < 1f)
            {
                if (Environment.TickCount >= _appearAt)
                {
                    _appear += 0.12f;
                    if (_appear >= 1f)
                    {
                        _appear = 1f;
                    }
                    else
                    {
                        appearDone = false;
                    }
                }
                else
                {
                    appearDone = false;
                }
            }

            if (highlightDone && appearDone)
            {
                _timer.Stop();
            }
            Invalidate();
        };
    }

    public void StartAppear(int delayMs)
    {
        _appear = 0f;
        _appearAt = Environment.TickCount + delayMs;
        _timer.Start();
    }

    public void SetSelected(bool value)
    {
        if (_selected == value)
        {
            return;
        }
        _selected = value;
        _timer.Start();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _pointerInside = true;
        _timer.Start();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _pointerInside = false;
        _timer.Start();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            if (IsAddTile)
            {
                Launch?.Invoke(this);
            }
            else
            {
                Open?.Invoke(this);
            }
        }
        else if (e.Button == MouseButtons.Right && !IsAddTile)
        {
            ShowMenu();
        }
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button == MouseButtons.Left && !IsAddTile)
        {
            Launch?.Invoke(this);
        }
    }

    private void ShowMenu()
    {
        var menu = MenuTheme.Create();
        var open = new ToolStripMenuItem("Ouvrir la fiche");
        open.Click += (_, _) => Open?.Invoke(this);
        var play = new ToolStripMenuItem("Lancer");
        play.Click += (_, _) => Launch?.Invoke(this);
        var rename = new ToolStripMenuItem("Renommer");
        rename.Click += (_, _) => Rename?.Invoke(this);
        var location = new ToolStripMenuItem("Ouvrir l'emplacement du fichier");
        location.Click += (_, _) => OpenLocation?.Invoke(this);
        var favorite = new ToolStripMenuItem(IsFavorite ? "Retirer des favoris" : "Ajouter aux favoris");
        favorite.Click += (_, _) => ToggleFavorite?.Invoke(this);
        var remove = new ToolStripMenuItem("Retirer");
        remove.Click += (_, _) => Remove?.Invoke(this);
        menu.Items.Add(open);
        menu.Items.Add(play);
        menu.Items.Add(favorite);
        menu.Items.Add(rename);
        menu.Items.Add(location);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(remove);
        menu.Show(this, new Point(Width / 2, Height / 2));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(_surround);

        var appear = Math.Clamp(_appear, 0f, 1f);
        var appearShift = (int)Math.Round((1f - appear) * 18f);
        if (appearShift != 0)
        {
            g.TranslateTransform(0, appearShift);
        }

        var lift = (int)Math.Round(8 * _highlight);
        var border = 2f + 3f * _highlight;
        var body = new RectangleF(border, border - lift, Width - border * 2f, Height - border * 2f);
        var bodyRect = Rectangle.Round(body);

        using var path = UiHelpers.RoundedRect(body, 18);

        if (IsAddTile)
        {
            using (var pen = new Pen(UiHelpers.Blend(_textColor, _accent, _highlight), 2f) { DashStyle = DashStyle.Dash })
            {
                g.DrawPath(pen, path);
            }
            TextRenderer.DrawText(g, "\uFF0B", new Font("Segoe UI", 44, FontStyle.Bold), bodyRect, UiHelpers.Blend(_textColor, _accent, _highlight),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, "Ajouter un jeu", new Font("Segoe UI", 10, FontStyle.Bold),
                new Rectangle(bodyRect.X, bodyRect.Bottom - 46, bodyRect.Width, 34), _textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        using (var backdrop = new SolidBrush(IconBackdrop))
        {
            g.FillPath(backdrop, path);
        }

        g.SetClip(path);

        if (_icon is not null)
        {
            g.DrawImage(_icon, UiHelpers.CoverInside(_icon.Size, bodyRect));
        }
        else
        {
            TextRenderer.DrawText(g, "\U0001F3AE", new Font("Segoe UI Emoji", 52), bodyRect, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        var stripHeight = 46f;
        var stripRect = new RectangleF(body.X, body.Bottom - stripHeight, body.Width, stripHeight);
        using (var strip = new LinearGradientBrush(
                   new RectangleF(stripRect.X, stripRect.Y - 1, stripRect.Width, stripRect.Height + 2),
                   Color.FromArgb(0, 0, 0, 0), Color.FromArgb(220, 0, 0, 0), LinearGradientMode.Vertical))
        {
            g.FillRectangle(strip, stripRect);
        }
        g.ResetClip();

        TextRenderer.DrawText(g, Game?.Name ?? string.Empty, new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Rectangle.Round(new RectangleF(stripRect.X + 6, stripRect.Y, stripRect.Width - 12, stripRect.Height)), Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (IsFavorite)
        {
            var star = new Rectangle(bodyRect.Right - 40, bodyRect.Top + 8, 32, 32);
            using (var shadow = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillEllipse(shadow, star);
            }
            TextRenderer.DrawText(g, "\u2605", new Font("Segoe UI", 13, FontStyle.Bold), star,
                Color.Gold, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        if (_missing)
        {
            var badge = new Rectangle(bodyRect.Left + 8, bodyRect.Top + 8, 30, 30);
            using (var shadow = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
            {
                g.FillEllipse(shadow, badge);
            }
            TextRenderer.DrawText(g, "\u26A0", new Font("Segoe UI Emoji", 12, FontStyle.Bold), badge,
                Color.FromArgb(255, 120, 120), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        if (_highlight > 0.01f)
        {
            using var pen = new Pen(_accent, border);
            g.DrawPath(pen, path);
        }

        if (appear < 1f)
        {
            var alpha = (int)Math.Round((1f - appear) * 255f);
            if (alpha > 0)
            {
                using var fade = new SolidBrush(Color.FromArgb(alpha, _surround));
                g.FillRectangle(fade, new Rectangle(0, -appearShift, Width, Height));
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _icon?.Dispose();
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class FadeOverlay : Control
{
    private Image? _frame;
    private float _alpha = 1f;

    public Color Backdrop { get; set; } = Color.Black;

    public FadeOverlay()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
    }

    public float Alpha
    {
        get => _alpha;
        set
        {
            _alpha = Math.Clamp(value, 0f, 1f);
            Invalidate();
        }
    }

    public void SetFrame(Bitmap frame)
    {
        _frame?.Dispose();
        _frame = frame;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        using (var back = new SolidBrush(Backdrop))
        {
            g.FillRectangle(back, ClientRectangle);
        }

        if (_frame is null)
        {
            return;
        }

        var opacity = 1f - Math.Clamp(_alpha, 0f, 1f);
        if (opacity <= 0f)
        {
            return;
        }

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        g.DrawImage(_frame, new Rectangle(0, 0, Width, Height), 0, 0, _frame.Width, _frame.Height, GraphicsUnit.Pixel, attributes);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _frame?.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class LogoBox : Control
{
    private Image? _image;
    public Color TileColor { get; set; } = Color.SlateGray;
    public Color Accent { get; set; } = Color.MediumSlateBlue;

    public LogoBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    public void SetImage(Image? image)
    {
        _image?.Dispose();
        _image = image;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Parent?.BackColor ?? BackColor);

        var rect = new RectangleF(1, 1, Width - 2, Height - 2);
        using var path = UiHelpers.RoundedRect(rect, 22);
        using (var brush = new SolidBrush(TileColor))
        {
            g.FillPath(brush, path);
        }

        g.SetClip(path);
        if (_image is not null)
        {
            var area = new Rectangle(18, 18, Width - 36, Height - 36);
            g.DrawImage(_image, UiHelpers.FitInside(_image.Size, area));
        }
        else
        {
            TextRenderer.DrawText(g, "\U0001F3AE", new Font("Segoe UI Emoji", 72), new Rectangle(0, 0, Width, Height), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        g.ResetClip();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image?.Dispose();
        }
        base.Dispose(disposing);
    }
}

internal sealed class WeeklyChart : Control
{
    private (string Label, double Minutes)[] _data = Array.Empty<(string, double)>();
    public Color BarColor { get; set; } = Color.MediumSlateBlue;
    public Color TextColor { get; set; } = Color.White;
    public Color MutedColor { get; set; } = Color.Gray;
    public Color TrackColor { get; set; } = Color.FromArgb(60, 60, 70);

    public WeeklyChart()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    public void SetData(IReadOnlyList<(string Label, double Minutes)> data)
    {
        _data = data.ToArray();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? BackColor);

        if (_data.Length == 0)
        {
            return;
        }

        var max = Math.Max(1d, _data.Max(d => d.Minutes));
        var labelHeight = 22;
        var valueHeight = 16;
        var chartTop = valueHeight + 4;
        var chartBottom = Height - labelHeight;
        var chartHeight = Math.Max(10, chartBottom - chartTop);
        var slot = (float)Width / _data.Length;
        var barWidth = Math.Min(38f, slot * 0.55f);

        using var barBrush = new SolidBrush(BarColor);
        using var trackBrush = new SolidBrush(TrackColor);

        for (var i = 0; i < _data.Length; i++)
        {
            var centerX = slot * i + slot / 2f;
            var left = centerX - barWidth / 2f;

            var trackRect = new RectangleF(left, chartTop, barWidth, chartHeight);
            using (var trackPath = UiHelpers.RoundedRect(trackRect, barWidth / 2.4f))
            {
                g.FillPath(trackBrush, trackPath);
            }

            var ratio = (float)(_data[i].Minutes / max);
            var barHeight = Math.Max(_data[i].Minutes > 0 ? barWidth : 0f, chartHeight * ratio);
            if (barHeight > 0f)
            {
                var barRect = new RectangleF(left, chartBottom - barHeight, barWidth, barHeight);
                using var barPath = UiHelpers.RoundedRect(barRect, barWidth / 2.4f);
                g.FillPath(barBrush, barPath);
            }

            if (_data[i].Minutes > 0)
            {
                var valueText = _data[i].Minutes >= 60 ? $"{_data[i].Minutes / 60d:0.#}h" : $"{(int)_data[i].Minutes}m";
                TextRenderer.DrawText(g, valueText, new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    new Rectangle((int)(centerX - slot / 2f), 0, (int)slot, valueHeight), TextColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            TextRenderer.DrawText(g, _data[i].Label, new Font("Segoe UI", 8f),
                new Rectangle((int)(centerX - slot / 2f), chartBottom + 2, (int)slot, labelHeight), MutedColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}

internal sealed class Toast : RoundedPanel
{
    private readonly Label _icon = new() { AutoSize = false, Width = 40, Height = 46, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Emoji", 18) };
    private readonly Label _title = new() { AutoSize = false, Left = 58, Top = 8, Width = 230, Height = 18, Font = new Font("Segoe UI", 8f, FontStyle.Bold) };
    private readonly Label _name = new() { AutoSize = false, Left = 58, Top = 26, Width = 230, Height = 24, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

    public Toast()
    {
        Width = 320;
        Height = 66;
        CornerRadius = 16;
        _icon.Location = new Point(12, 10);
        Controls.Add(_icon);
        Controls.Add(_title);
        Controls.Add(_name);
    }

    public void SetTheme(Palette palette)
    {
        FillColor = palette.Card;
        SurroundColor = palette.Background;
        BorderColor = palette.Accent;
        BorderWidth = 1.5f;
        _icon.BackColor = palette.Card;
        _icon.ForeColor = palette.Text;
        _title.BackColor = palette.Card;
        _title.ForeColor = palette.Accent;
        _name.BackColor = palette.Card;
        _name.ForeColor = palette.Text;
        Invalidate();
    }

    public void Show(string icon, string title)
    {
        _icon.Text = icon;
        _title.Text = "TROPHÉE DÉBLOQUÉ";
        _name.Text = title;
        Invalidate();
    }
}

internal sealed class GameDetailPage : Panel
{
    private readonly GlassButton _back = new() { Text = "\u2190  Retour", Width = 130, Height = 44, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly LogoBox _logo = new();
    private readonly Label _name = new() { AutoSize = true, Font = new Font("Segoe UI", 30, FontStyle.Bold) };
    private readonly Label _path = new() { AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
    private readonly Label _lastPlayed = new() { AutoSize = true, Font = new Font("Segoe UI", 9.5f) };
    private readonly RoundedPanel _timeCard = new() { CornerRadius = 16, Width = 230, Height = 108 };
    private readonly Label _timeHead = new() { Text = "TEMPS DE JEU", AutoSize = true, Left = 18, Top = 16, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
    private readonly Label _timeValue = new() { Text = "-", AutoSize = true, Left = 16, Top = 42, Font = new Font("Segoe UI", 22, FontStyle.Bold) };
    private readonly RoundedPanel _launchCard = new() { CornerRadius = 16, Width = 230, Height = 108 };
    private readonly Label _launchHead = new() { Text = "LANCÉ", AutoSize = true, Left = 18, Top = 16, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
    private readonly Label _launchValue = new() { Text = "-", AutoSize = true, Left = 16, Top = 42, Font = new Font("Segoe UI", 22, FontStyle.Bold) };
    private readonly RoundedButton _play = new() { Text = "\u25B6  Jouer", Width = 170, Height = 56, CornerRadius = 16, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White };
    private readonly GlassButton _location = new() { Text = "\U0001F4C1  Emplacement", Width = 170, Height = 56, CornerRadius = 16, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly GlassButton _rename = new() { Text = "Renommer", Width = 140, Height = 56, CornerRadius = 16, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly GlassButton _remove = new() { Text = "Retirer", Width = 140, Height = 56, CornerRadius = 16, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

    public event Action? Back;
    public event Action? Play;
    public event Action? RemoveGame;
    public event Action? Rename;
    public event Action? OpenLocation;

    public GameDetailPage()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        Controls.Add(_back);
        Controls.Add(_logo);
        Controls.Add(_name);
        Controls.Add(_path);
        Controls.Add(_lastPlayed);
        _timeCard.Controls.Add(_timeHead);
        _timeCard.Controls.Add(_timeValue);
        _launchCard.Controls.Add(_launchHead);
        _launchCard.Controls.Add(_launchValue);
        Controls.Add(_timeCard);
        Controls.Add(_launchCard);
        Controls.Add(_play);
        Controls.Add(_location);
        Controls.Add(_rename);
        Controls.Add(_remove);

        _back.Click += (_, _) => Back?.Invoke();
        _play.Click += (_, _) => Play?.Invoke();
        _remove.Click += (_, _) => RemoveGame?.Invoke();
        _rename.Click += (_, _) => Rename?.Invoke();
        _location.Click += (_, _) => OpenLocation?.Invoke();

        SizeChanged += (_, _) => DoLayout();
    }

    public void SetTheme(Palette palette)
    {
        BackColor = palette.Background;
        _back.BaseBackground = palette.Background;
        _back.Glow = palette.Accent;
        _logo.Accent = palette.Accent;
        _name.BackColor = palette.Background;
        _name.ForeColor = palette.Text;
        _path.BackColor = palette.Background;
        _path.ForeColor = palette.Muted;
        _lastPlayed.BackColor = palette.Background;
        _lastPlayed.ForeColor = palette.Muted;

        foreach (var (card, head, value) in new[] { (_timeCard, _timeHead, _timeValue), (_launchCard, _launchHead, _launchValue) })
        {
            card.FillColor = palette.Card;
            card.SurroundColor = palette.Background;
            head.BackColor = palette.Card;
            head.ForeColor = palette.Muted;
            value.BackColor = palette.Card;
            value.ForeColor = palette.Text;
            card.Invalidate();
        }

        _play.BaseColor = palette.Accent;
        _play.HoverColor = UiHelpers.Blend(palette.Accent, Color.White, 0.18f);
        _play.SurroundColor = palette.Background;
        _play.ForeColor = UiHelpers.ContrastText(palette.Accent);

        foreach (var button in new[] { _location, _rename })
        {
            button.BaseBackground = palette.Background;
            button.Glow = palette.Accent;
            button.Invalidate();
        }

        _remove.BaseBackground = palette.Background;
        _remove.Glow = Color.FromArgb(220, 60, 80);
        _remove.Invalidate();
        Invalidate(true);
    }

    public void Bind(GameEntry game, GameStat stat, Color tileColor, Image? image)
    {
        _name.Text = game.Name;
        _path.Text = game.Path;
        _logo.TileColor = tileColor;
        _logo.SetImage(image);
        UpdateStats(stat);
        DoLayout();
    }

    public void UpdateStats(GameStat stat)
    {
        var minutes = stat.Minutes;
        string time;
        if (minutes <= 0)
        {
            time = "0 min";
        }
        else if (minutes < 1)
        {
            time = "< 1 min";
        }
        else
        {
            var hours = (int)(minutes / 60);
            var mins = (int)(minutes % 60);
            time = hours > 0 ? $"{hours}h {mins:00}" : $"{mins} min";
        }
        _timeValue.Text = time;
        _launchValue.Text = stat.Launches == 1 ? "1 fois" : $"{stat.Launches} fois";
        _lastPlayed.Text = stat.LastPlayed is null ? "Jamais lancé" : "Dernière session : " + stat.LastPlayed.Value.ToString("dd/MM/yyyy HH:mm");
    }

    private void DoLayout()
    {
        _back.Left = 28;
        _back.Top = 26;

        var logoSize = Math.Clamp(Height - 320, 200, 380);
        _logo.SetBounds(44, 108, logoSize, logoSize);

        var rightX = _logo.Right + 48;
        _name.Location = new Point(rightX, 130);
        _path.Location = new Point(rightX, 130 + _name.Height + 10);
        _lastPlayed.Location = new Point(rightX, _path.Bottom + 8);

        _timeCard.Location = new Point(44, Height - 190);
        _launchCard.Location = new Point(44 + _timeCard.Width + 16, Height - 190);

        _remove.Location = new Point(Width - _remove.Width - 44, Height - 176);
        _rename.Location = new Point(_remove.Left - _rename.Width - 12, Height - 176);
        _location.Location = new Point(_rename.Left - _location.Width - 12, Height - 176);
        _play.Location = new Point(_location.Left - _play.Width - 12, Height - 176);
    }
}

internal sealed class ProfilePage : Panel
{
    private readonly GlassButton _back = new() { Text = "\u2190  Retour", Width = 130, Height = 44, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly AvatarCircle _avatar = new() { Width = 150, Height = 150, Left = 44, Top = 96 };
    private readonly Label _name = new() { AutoSize = true, Font = new Font("Segoe UI", 30, FontStyle.Bold) };
    private readonly Label _levelLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 13, FontStyle.Bold) };
    private readonly RoundedProgressBar _levelProgress = new() { Height = 16, Width = 360 };
    private readonly Label _xpLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 9.5f) };

    private readonly Label _weekTitle = new() { Text = "ACTIVITÉ DE LA SEMAINE", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly RoundedPanel _weekCard = new() { CornerRadius = 18, Height = 190 };
    private readonly WeeklyChart _weekChart = new() { Dock = DockStyle.Fill };

    private readonly Label _topTitle = new() { Text = "TOP 3 DES JEUX", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly Panel _topContainer = new() { Height = 3 * 74 };

    private readonly Label _achievementsTitle = new() { Text = "TROPHÉES", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly FlowLayoutPanel _achievementsFlow = new() { AutoSize = false, WrapContents = true, AutoScroll = false };

    private Palette _palette = new(Color.Black, Color.Black, Color.Black, Color.White, Color.Gray, Color.MediumSlateBlue);

    public event Action? Back;
    public event Action<string>? PlayGame;

    public ProfilePage()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        AutoScroll = true;

        _weekCard.Controls.Add(_weekChart);
        _weekChart.Padding = new Padding(10);

        Controls.Add(_avatar);
        Controls.Add(_name);
        Controls.Add(_levelLabel);
        Controls.Add(_levelProgress);
        Controls.Add(_xpLabel);
        Controls.Add(_weekTitle);
        Controls.Add(_weekCard);
        Controls.Add(_topTitle);
        Controls.Add(_topContainer);
        Controls.Add(_achievementsTitle);
        Controls.Add(_achievementsFlow);
        Controls.Add(_back);

        _back.Left = 28;
        _back.Top = 26;
        _back.Click += (_, _) => Back?.Invoke();

        SizeChanged += (_, _) => DoLayout();
    }

    public void SetTheme(Palette palette)
    {
        _palette = palette;
        BackColor = palette.Background;

        _back.BaseBackground = palette.Background;
        _back.Glow = palette.Accent;
        _back.Invalidate();

        _avatar.BackColor = palette.Background;
        _avatar.RingColor = palette.Accent;
        _avatar.ForeColor = Color.White;
        _avatar.Invalidate();

        _name.BackColor = palette.Background;
        _name.ForeColor = palette.Text;
        _levelLabel.BackColor = palette.Background;
        _levelLabel.ForeColor = palette.Accent;
        _xpLabel.BackColor = palette.Background;
        _xpLabel.ForeColor = palette.Muted;
        _levelProgress.TrackColor = UiHelpers.Blend(palette.Card, palette.Background, 0.4f);
        _levelProgress.FillColor = palette.Accent;
        _levelProgress.Invalidate();

        foreach (var label in new[] { _weekTitle, _topTitle, _achievementsTitle })
        {
            label.BackColor = palette.Background;
            label.ForeColor = palette.Muted;
        }

        _weekCard.FillColor = palette.Card;
        _weekCard.SurroundColor = palette.Background;
        _weekCard.Invalidate();
        _weekChart.BackColor = palette.Card;
        _weekChart.BarColor = palette.Accent;
        _weekChart.TextColor = palette.Text;
        _weekChart.MutedColor = palette.Muted;
        _weekChart.TrackColor = UiHelpers.Blend(palette.Card, palette.Background, 0.5f);
        _weekChart.Invalidate();

        _topContainer.BackColor = palette.Background;
        _achievementsFlow.BackColor = palette.Background;

        Invalidate(true);
    }

    public void Bind(string name, Image? avatar, int level, double intoLevel, double neededLevel,
        IReadOnlyList<(string Name, string IconPath, string Path, double Minutes)> top3,
        IReadOnlyList<(Achievement Ach, bool Unlocked)> achievements,
        IReadOnlyList<(string Label, double Minutes)> weekly)
    {
        _avatar.Avatar = avatar;
        _avatar.Initial = string.IsNullOrWhiteSpace(name) ? "?" : name.Trim().Substring(0, 1).ToUpperInvariant();
        _avatar.Invalidate();

        _name.Text = name;
        _levelLabel.Text = $"Niveau {level}";
        _levelProgress.Value = neededLevel <= 0 ? 0f : (float)(intoLevel / neededLevel);
        _xpLabel.Text = $"{(int)intoLevel} / {(int)neededLevel} XP jusqu'au niveau {level + 1}";

        _weekChart.SetData(weekly);
        BuildTop(top3);
        BuildAchievements(achievements);
        DoLayout();
    }

    private void BuildTop(IReadOnlyList<(string Name, string IconPath, string Path, double Minutes)> top3)
    {
        foreach (Control control in _topContainer.Controls)
        {
            DisposeRowImages(control);
        }
        _topContainer.Controls.Clear();

        if (top3.Count == 0)
        {
            var empty = new Label
            {
                Text = "Lance un jeu pour voir apparaître ton top 3 ici.",
                AutoSize = false,
                Height = 60,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10),
                ForeColor = _palette.Muted,
                BackColor = _palette.Background
            };
            _topContainer.Controls.Add(empty);
            return;
        }

        for (var i = 0; i < top3.Count; i++)
        {
            var row = BuildTopRow(i + 1, top3[i].Name, top3[i].IconPath, top3[i].Path, top3[i].Minutes);
            row.Top = i * 74;
            _topContainer.Controls.Add(row);
        }
    }

    private RoundedPanel BuildTopRow(int rank, string name, string iconPath, string gamePath, double minutes)
    {
        var row = new RoundedPanel
        {
            Height = 62,
            CornerRadius = 14,
            FillColor = _palette.Card,
            SurroundColor = _palette.Background
        };

        var rankLabel = new Label
        {
            Text = "#" + rank,
            AutoSize = false,
            Width = 46,
            Height = 62,
            Left = 6,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            BackColor = _palette.Card,
            ForeColor = _palette.Accent
        };

        var picture = new PictureBox
        {
            Left = 56,
            Top = 9,
            Width = 44,
            Height = 44,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = _palette.Card
        };
        if (File.Exists(iconPath))
        {
            try
            {
                using var source = Image.FromFile(iconPath);
                picture.Image = new Bitmap(source);
            }
            catch
            {
                // Ignore unreadable icons.
            }
        }

        var nameLabel = new Label
        {
            Text = name,
            AutoSize = false,
            Left = 114,
            Top = 10,
            Width = 300,
            Height = 24,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = _palette.Card,
            ForeColor = _palette.Text,
            AutoEllipsis = true
        };

        var hours = (int)(minutes / 60);
        var mins = (int)(minutes % 60);
        var timeText = hours > 0 ? $"{hours}h {mins:00} de jeu" : $"{mins} min de jeu";
        var timeLabel = new Label
        {
            Text = timeText,
            AutoSize = false,
            Left = 114,
            Top = 34,
            Width = 300,
            Height = 20,
            Font = new Font("Segoe UI", 9),
            BackColor = _palette.Card,
            ForeColor = _palette.Muted
        };

        row.Controls.Add(rankLabel);
        row.Controls.Add(picture);
        row.Controls.Add(nameLabel);
        row.Controls.Add(timeLabel);
        row.Cursor = Cursors.Hand;
        row.Click += (_, _) => PlayGame?.Invoke(gamePath);
        return row;
    }

    private void BuildAchievements(IReadOnlyList<(Achievement Ach, bool Unlocked)> achievements)
    {
        _achievementsFlow.Controls.Clear();
        var unlocked = achievements.Count(a => a.Unlocked);
        _achievementsTitle.Text = $"TROPHÉES  ({unlocked}/{achievements.Count})";

        foreach (var (ach, isUnlocked) in achievements)
        {
            var badge = new RoundedPanel
            {
                Width = 168,
                Height = 96,
                CornerRadius = 16,
                Margin = new Padding(0, 0, 12, 12),
                FillColor = isUnlocked ? _palette.Card : UiHelpers.Blend(_palette.Card, _palette.Background, 0.55f),
                SurroundColor = _palette.Background
            };

            var icon = new Label
            {
                Text = ach.Icon,
                AutoSize = false,
                Left = 12,
                Top = 12,
                Width = 40,
                Height = 40,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Emoji", 17),
                BackColor = badge.FillColor,
                ForeColor = isUnlocked ? _palette.Text : _palette.Muted
            };

            var title = new Label
            {
                Text = ach.Title,
                AutoSize = false,
                Left = 58,
                Top = 14,
                Width = 100,
                Height = 36,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = badge.FillColor,
                ForeColor = isUnlocked ? _palette.Text : _palette.Muted
            };

            var status = new Label
            {
                Text = isUnlocked ? "Débloqué" : "À débloquer",
                AutoSize = false,
                Left = 12,
                Top = 66,
                Width = 144,
                Height = 20,
                Font = new Font("Segoe UI", 8f),
                BackColor = badge.FillColor,
                ForeColor = isUnlocked ? _palette.Accent : _palette.Muted
            };

            badge.Controls.Add(icon);
            badge.Controls.Add(title);
            badge.Controls.Add(status);

            var tip = new ToolTip();
            tip.SetToolTip(badge, ach.Description);
            tip.SetToolTip(icon, ach.Description);
            tip.SetToolTip(title, ach.Description);

            _achievementsFlow.Controls.Add(badge);
        }
    }

    private static void DisposeRowImages(Control container)
    {
        foreach (Control child in container.Controls)
        {
            if (child is PictureBox { Image: { } image })
            {
                child.Dispose();
                image.Dispose();
            }
        }
    }

    private void DoLayout()
    {
        var contentWidth = Math.Max(600, ClientSize.Width - 88);

        _back.Left = 28;
        _back.Top = 26;

        _avatar.Location = new Point(44, 96);

        var rightX = _avatar.Right + 36;
        _name.Location = new Point(rightX, 108);
        _levelLabel.Location = new Point(rightX, 108 + _name.Height + 12);
        _levelProgress.Location = new Point(rightX, _levelLabel.Bottom + 10);
        _levelProgress.Width = Math.Min(420, contentWidth - (rightX - 44));
        _xpLabel.Location = new Point(rightX, _levelProgress.Bottom + 8);

        var y = Math.Max(_avatar.Bottom, _xpLabel.Bottom) + 30;

        _weekTitle.Location = new Point(44, y);
        y = _weekTitle.Bottom + 10;
        _weekCard.SetBounds(44, y, contentWidth, 190);
        y = _weekCard.Bottom + 26;

        _topTitle.Location = new Point(44, y);
        y = _topTitle.Bottom + 10;
        _topContainer.SetBounds(44, y, contentWidth, _topContainer.Controls.Count == 0 ? 60 : Math.Max(60, _topContainer.Controls.Count * 74));
        foreach (Control row in _topContainer.Controls)
        {
            row.Width = contentWidth;
            if (row is RoundedPanel)
            {
                foreach (Control child in row.Controls)
                {
                    if (child is Label { Font.Bold: true } label && label.Top == 10)
                    {
                        label.Width = contentWidth - 130;
                    }
                }
            }
        }
        y = _topContainer.Bottom + 26;

        _achievementsTitle.Location = new Point(44, y);
        y = _achievementsTitle.Bottom + 10;
        var perRow = Math.Max(1, contentWidth / 180);
        var rows = (int)Math.Ceiling(Achievements.Catalog.Count / (double)perRow);
        _achievementsFlow.SetBounds(44, y, contentWidth, rows * 108 + 10);
    }
}

internal sealed class SettingsPage : Panel
{
    private readonly GlassButton _back = new() { Text = "\u2190  Retour", Width = 130, Height = 44, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly Label _title = new() { Text = "Paramètres", AutoSize = true, Font = new Font("Segoe UI", 26, FontStyle.Bold), Left = 44, Top = 90 };

    private readonly Label _pseudoLabel = new() { Text = "Pseudo", AutoSize = true, Left = 44, Top = 168, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly RoundedPanel _pseudoBox = new() { Left = 44, Top = 198, Width = 360, Height = 48, CornerRadius = 12 };
    private readonly TextBox _pseudoInput = new() { BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 13) };

    private readonly Label _photoLabel = new() { Text = "Photo de profil", AutoSize = true, Left = 460, Top = 168, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly AvatarCircle _photoPreview = new() { Left = 460, Top = 198, Width = 96, Height = 96 };
    private readonly RoundedButton _changePhoto = new() { Text = "Changer la photo", Left = 572, Top = 202, Width = 200, Height = 44, CornerRadius = 12, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White };
    private readonly RoundedButton _removePhoto = new() { Text = "Retirer la photo", Left = 572, Top = 254, Width = 200, Height = 40, CornerRadius = 12, Font = new Font("Segoe UI", 9.5f) };

    private readonly Label _themeLabel = new() { Text = "Thème", AutoSize = true, Left = 44, Top = 330, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly FlowLayoutPanel _themeRow = new() { Left = 44, Top = 362, Width = 900, AutoSize = true, WrapContents = true, MaximumSize = new Size(900, 0) };
    private readonly List<RoundedButton> _themeButtons = new();

    private readonly Label _dataLabel = new() { Text = "Données", AutoSize = true, Left = 44, Top = 470, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
    private readonly GlassButton _openFolder = new() { Text = "\U0001F4C1  Dossier des données", Left = 44, Top = 502, Width = 260, Height = 46, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly GlassButton _resetStats = new() { Text = "\u267B  Réinitialiser les stats", Left = 316, Top = 502, Width = 260, Height = 46, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

    public event Action? Back;
    public event Action<string>? PseudoChanged;
    public event Action<string>? AvatarChosen;
    public event Action? AvatarRemoved;
    public event Action<string>? ThemeChosen;
    public event Action? DataFolderRequested;
    public event Action? StatsResetRequested;

    public SettingsPage()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        _pseudoBox.Controls.Add(_pseudoInput);
        _pseudoInput.Location = new Point(14, 12);
        _pseudoInput.Width = _pseudoBox.Width - 28;
        _pseudoInput.TextChanged += (_, _) =>
        {
            PseudoChanged?.Invoke(_pseudoInput.Text);
            _photoPreview.Initial = string.IsNullOrWhiteSpace(_pseudoInput.Text) ? "?" : _pseudoInput.Text.Trim().Substring(0, 1).ToUpperInvariant();
            _photoPreview.Invalidate();
        };

        _back.Click += (_, _) => Back?.Invoke();
        _changePhoto.Click += (_, _) => ChoosePhoto();
        _removePhoto.Click += (_, _) => AvatarRemoved?.Invoke();
        _openFolder.Click += (_, _) => DataFolderRequested?.Invoke();
        _resetStats.Click += (_, _) => StatsResetRequested?.Invoke();

        Controls.Add(_back);
        Controls.Add(_title);
        Controls.Add(_pseudoLabel);
        Controls.Add(_pseudoBox);
        Controls.Add(_photoLabel);
        Controls.Add(_photoPreview);
        Controls.Add(_changePhoto);
        Controls.Add(_removePhoto);
        Controls.Add(_themeLabel);
        Controls.Add(_themeRow);
        Controls.Add(_dataLabel);
        Controls.Add(_openFolder);
        Controls.Add(_resetStats);

        _back.Left = 28;
        _back.Top = 26;
    }

    private void ChoosePhoto()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Choisir une photo de profil",
            Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tous les fichiers (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AvatarChosen?.Invoke(dialog.FileName);
        }
    }

    public void Bind(string pseudo, Image? avatar, IReadOnlyList<(string Name, Color Accent)> themes, string currentTheme)
    {
        _pseudoInput.Text = pseudo;
        _photoPreview.Initial = string.IsNullOrWhiteSpace(pseudo) ? "?" : pseudo.Trim().Substring(0, 1).ToUpperInvariant();
        _photoPreview.Avatar = avatar;
        _photoPreview.Invalidate();

        if (_themeButtons.Count == 0)
        {
            foreach (var (name, accent) in themes)
            {
                var button = new RoundedButton
                {
                    Text = name,
                    Width = 156,
                    Height = 46,
                    CornerRadius = 12,
                    Margin = new Padding(0, 0, 12, 12),
                    BaseColor = accent,
                    HoverColor = UiHelpers.Blend(accent, Color.White, 0.18f),
                    ForeColor = UiHelpers.ContrastText(accent),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
                };
                var captured = name;
                button.Click += (_, _) => ThemeChosen?.Invoke(captured);
                _themeButtons.Add(button);
                _themeRow.Controls.Add(button);
            }
        }
        HighlightTheme(currentTheme);
    }

    public void SetAvatar(Image? avatar)
    {
        _photoPreview.Avatar = avatar;
        _photoPreview.Invalidate();
    }

    public void HighlightTheme(string currentTheme)
    {
        foreach (var button in _themeButtons)
        {
            var isCurrent = string.Equals(button.Text, currentTheme, StringComparison.Ordinal);
            button.Font = new Font("Segoe UI", 9.5f, isCurrent ? FontStyle.Bold | FontStyle.Underline : FontStyle.Bold);
            button.Invalidate();
        }
    }

    public void SetTheme(Palette palette)
    {
        BackColor = palette.Background;

        _back.BaseBackground = palette.Background;
        _back.Glow = palette.Accent;
        _back.Invalidate();

        foreach (var label in new[] { _title, _pseudoLabel, _photoLabel, _themeLabel, _dataLabel })
        {
            label.BackColor = palette.Background;
            label.ForeColor = palette.Text;
        }

        foreach (var glass in new[] { _openFolder, _resetStats })
        {
            glass.BaseBackground = palette.Background;
            glass.Glow = palette.Accent;
            glass.ForeColor = palette.Text;
            glass.Invalidate();
        }

        _pseudoBox.FillColor = palette.Card;
        _pseudoBox.SurroundColor = palette.Background;
        _pseudoBox.Invalidate();
        _pseudoInput.BackColor = palette.Card;
        _pseudoInput.ForeColor = palette.Text;

        _photoPreview.BackColor = palette.Background;
        _photoPreview.RingColor = palette.Accent;
        _photoPreview.ForeColor = Color.White;
        _photoPreview.Invalidate();

        _changePhoto.BaseColor = palette.Accent;
        _changePhoto.HoverColor = UiHelpers.Blend(palette.Accent, Color.White, 0.18f);
        _changePhoto.SurroundColor = palette.Background;
        _changePhoto.ForeColor = UiHelpers.ContrastText(palette.Accent);

        _removePhoto.BaseColor = palette.Card;
        _removePhoto.HoverColor = UiHelpers.Blend(palette.Card, Color.FromArgb(220, 60, 80), 0.5f);
        _removePhoto.SurroundColor = palette.Background;
        _removePhoto.ForeColor = palette.Text;

        _themeRow.BackColor = palette.Background;

        Invalidate(true);
    }
}

internal sealed class InputDialog : Form
{
    private readonly TextBox _input = new() { BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12) };

    private InputDialog(string title, string prompt, string initial, Palette palette)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(420, 160);
        BackColor = palette.Panel;
        ForeColor = palette.Text;

        var label = new Label { Text = prompt, AutoSize = true, Left = 20, Top = 20, Font = new Font("Segoe UI", 10), ForeColor = palette.Text, BackColor = palette.Panel };
        _input.SetBounds(20, 52, 380, 30);
        _input.Text = initial;
        _input.BackColor = palette.Card;
        _input.ForeColor = palette.Text;

        var ok = new RoundedButton { Text = "OK", Width = 120, Height = 40, CornerRadius = 10, BaseColor = palette.Accent, HoverColor = UiHelpers.Blend(palette.Accent, Color.White, 0.18f), SurroundColor = palette.Panel, ForeColor = UiHelpers.ContrastText(palette.Accent), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        ok.SetBounds(160, 104, 120, 40);
        ok.DialogResult = DialogResult.OK;

        var cancel = new RoundedButton { Text = "Annuler", Width = 120, Height = 40, CornerRadius = 10, BaseColor = palette.Card, HoverColor = UiHelpers.Blend(palette.Card, Color.White, 0.15f), SurroundColor = palette.Panel, ForeColor = palette.Text, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        cancel.SetBounds(288, 104, 120, 40);
        cancel.DialogResult = DialogResult.Cancel;

        Controls.Add(label);
        Controls.Add(_input);
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public static string? Ask(IWin32Window owner, string title, string prompt, string initial, Palette palette)
    {
        using var dialog = new InputDialog(title, prompt, initial, palette);
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog._input.Text : null;
    }
}

internal static class MenuTheme
{
    public static Palette Current { get; set; } =
        new(Color.FromArgb(29, 27, 46), Color.FromArgb(29, 27, 46), Color.FromArgb(41, 38, 65),
            Color.White, Color.Gray, Color.MediumSlateBlue);

    public static ContextMenuStrip Create()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new MinimalMenuRenderer(Current),
            ShowImageMargin = false,
            Font = new Font("Segoe UI", 10f),
            BackColor = Current.Panel,
            ForeColor = Current.Text,
            Padding = new Padding(6)
        };
        menu.Opening += (_, _) =>
        {
            foreach (ToolStripItem item in menu.Items)
            {
                item.ForeColor = Current.Text;
                if (item is ToolStripMenuItem)
                {
                    item.Padding = new Padding(10, 5, 12, 5);
                }
            }
        };
        return menu;
    }
}

internal sealed class MinimalMenuRenderer : ToolStripProfessionalRenderer
{
    private readonly Palette _palette;

    public MinimalMenuRenderer(Palette palette) : base(new MinimalColorTable(palette))
    {
        _palette = palette;
        RoundedEdges = true;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(0.5f, 0.5f, e.AffectedBounds.Width - 1f, e.AffectedBounds.Height - 1f);
        using var path = UiHelpers.RoundedRect(rect, 14);

        var lum = (0.299 * _palette.Panel.R + 0.587 * _palette.Panel.G + 0.114 * _palette.Panel.B) / 255d;
        var top = UiHelpers.Blend(_palette.Panel, Color.White, lum <= 0.55 ? 0.10f : 0.04f);
        using (var grad = new LinearGradientBrush(
                   new RectangleF(rect.X, rect.Y - 1, rect.Width, rect.Height + 2), top, _palette.Panel, LinearGradientMode.Vertical))
        {
            g.FillPath(grad, path);
        }
        using var edge = new Pen(Color.FromArgb(lum <= 0.55 ? 40 : 60, 255, 255, 255), 1f);
        g.DrawPath(edge, path);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        // No hard border for a minimalist look.
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected)
        {
            return;
        }
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(6, 2, e.Item.Bounds.Width - 12, e.Item.Bounds.Height - 4);
        using var path = UiHelpers.RoundedRect(rect, 9);
        var c1 = UiHelpers.Blend(_palette.Card, _palette.Accent, 0.45f);
        var c2 = UiHelpers.Blend(_palette.Card, _palette.Accent, 0.22f);
        using (var brush = new LinearGradientBrush(
                   new RectangleF(rect.X, rect.Y - 1, rect.Width, rect.Height + 2), c1, c2, LinearGradientMode.Vertical))
        {
            g.FillPath(brush, path);
        }

        // Accent bar on the left of the highlighted item.
        var bar = new RectangleF(rect.X + 3, rect.Y + 5, 3f, rect.Height - 10);
        using var barPath = UiHelpers.RoundedRect(bar, 1.5f);
        using var barBrush = new SolidBrush(_palette.Accent);
        g.FillPath(barBrush, barPath);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Bounds.Height / 2;
        using var pen = new Pen(UiHelpers.Blend(_palette.Panel, _palette.Text, 0.15f));
        e.Graphics.DrawLine(pen, 12, y, e.Item.Bounds.Width - 12, y);
    }
}

internal sealed class MinimalColorTable : ProfessionalColorTable
{
    private readonly Palette _palette;

    public MinimalColorTable(Palette palette)
    {
        _palette = palette;
        UseSystemColors = false;
    }

    public override Color ToolStripDropDownBackground => _palette.Panel;
    public override Color ImageMarginGradientBegin => _palette.Panel;
    public override Color ImageMarginGradientMiddle => _palette.Panel;
    public override Color ImageMarginGradientEnd => _palette.Panel;
    public override Color MenuBorder => _palette.Panel;
    public override Color MenuItemBorder => Color.Transparent;
    public override Color MenuItemSelected => Color.Transparent;
    public override Color MenuItemSelectedGradientBegin => Color.Transparent;
    public override Color MenuItemSelectedGradientEnd => Color.Transparent;
    public override Color SeparatorDark => UiHelpers.Blend(_palette.Panel, _palette.Text, 0.15f);
    public override Color SeparatorLight => _palette.Panel;
}

internal static class NativeMethods
{
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int PrivateExtractIcons(string lpszFile, int nIconIndex, int cxIcon, int cyIcon, IntPtr[] phicon, int[] piconid, int nIcons, int flags);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr handle);
}

internal static class IconImporter
{
    public static string ImportIcon(string sourcePath, string iconFolder, string gameName)
    {
        Directory.CreateDirectory(iconFolder);
        var cleanName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var output = Path.Combine(iconFolder, $"{cleanName}_{Math.Abs(sourcePath.ToLowerInvariant().GetHashCode())}.png");

        var bitmap = ExtractLargeIcon(sourcePath, 256) ?? ExtractLargeIcon(sourcePath, 128) ?? ExtractLargeIcon(sourcePath, 64);
        if (bitmap is not null)
        {
            using (bitmap)
            {
                bitmap.Save(output, ImageFormat.Png);
            }
            return output;
        }

        using var icon = Icon.ExtractAssociatedIcon(sourcePath);
        if (icon is null)
        {
            return string.Empty;
        }

        using var fallback = icon.ToBitmap();
        fallback.Save(output, ImageFormat.Png);
        return output;
    }

    private static Bitmap? ExtractLargeIcon(string path, int size)
    {
        var handles = new IntPtr[1];
        var ids = new int[1];
        try
        {
            var count = NativeMethods.PrivateExtractIcons(path, 0, size, size, handles, ids, 1, 0);
            if (count <= 0 || handles[0] == IntPtr.Zero)
            {
                return null;
            }

            using var icon = Icon.FromHandle(handles[0]);
            return new Bitmap(icon.ToBitmap());
        }
        catch
        {
            return null;
        }
        finally
        {
            if (handles[0] != IntPtr.Zero)
            {
                NativeMethods.DestroyIcon(handles[0]);
            }
        }
    }
}

internal static class ShortcutResolver
{
    public static string? TryResolveTarget(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        try
        {
            var shellLinkType = Type.GetTypeFromCLSID(ShellLinkClsid);
            if (shellLinkType is null)
            {
                return null;
            }

            var shellLinkObject = Activator.CreateInstance(shellLinkType);
            if (shellLinkObject is not IShellLinkW shellLink)
            {
                return null;
            }

            ((IPersistFile)shellLink).Load(path, 0);
            var buffer = new StringBuilder(1024);
            shellLink.GetPath(buffer, buffer.Capacity, IntPtr.Zero, 0);
            var target = buffer.ToString();
            return File.Exists(target) ? target : null;
        }
        catch
        {
            return null;
        }
    }

    private static readonly Guid ShellLinkClsid = new("00021401-0000-0000-C000-000000000046");

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}

// ========== HOME PAGE ==========

internal sealed class HomePage : UserControl
{
    private readonly LauncherForm _parent;
    private readonly string _logoPath;
    private readonly Palette _theme;
    private PictureBox _logoPicture = null!;
    private Label _welcomeLabel = null!;
    private Label _statsLabel = null!;
    private RoundedButton _libraryButton = null!;
    private RoundedButton _profileButton = null!;
    private RoundedButton _settingsButton = null!;

    public HomePage(LauncherForm parent, string logoPath, Palette theme)
    {
        _parent = parent;
        _logoPath = logoPath;
        _theme = theme;
        InitializeComponent();
        Dock = DockStyle.Fill;
        BackColor = _theme.Background;
    }

    private void InitializeComponent()
    {
        // Logo
        _logoPicture = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(200, 80),
            Location = new Point((Width - 200) / 2, 40),
            BackColor = Color.Transparent
        };
        if (File.Exists(_logoPath))
        {
            try { _logoPicture.Image = Image.FromFile(_logoPath); }
            catch { _logoPicture.Image = new Bitmap(200, 80); }
        }
        Controls.Add(_logoPicture);

        // Welcome label
        _welcomeLabel = new Label
        {
            Text = "Bienvenue sur DekerLab",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = _theme.Text,
            AutoSize = true,
            Location = new Point((Width - 300) / 2, 140),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(_welcomeLabel);

        // Stats label
        _statsLabel = new Label
        {
            Text = "0 jeux • 0h de jeu • Niveau 1",
            Font = new Font("Segoe UI", 14),
            ForeColor = _theme.Muted,
            AutoSize = true,
            Location = new Point((Width - 300) / 2, 180),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(_statsLabel);

        // Buttons
        int buttonWidth = 200;
        int buttonHeight = 50;
        int buttonY = 240;
        int buttonSpacing = 20;

        _libraryButton = new RoundedButton
        {
            Text = "Bibliothèque",
            Size = new Size(buttonWidth, buttonHeight),
            Location = new Point((Width - buttonWidth) / 2, buttonY),
            CornerRadius = 12,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = _theme.Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _libraryButton.Click += (s, e) => _parent.ShowLibrary();
        Controls.Add(_libraryButton);

        _profileButton = new RoundedButton
        {
            Text = "Profil",
            Size = new Size(buttonWidth, buttonHeight),
            Location = new Point((Width - buttonWidth) / 2, buttonY + buttonHeight + buttonSpacing),
            CornerRadius = 12,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = _theme.Card,
            ForeColor = _theme.Text,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _profileButton.Click += (s, e) => _parent.ShowProfile();
        Controls.Add(_profileButton);

        _settingsButton = new RoundedButton
        {
            Text = "Paramètres",
            Size = new Size(buttonWidth, buttonHeight),
            Location = new Point((Width - buttonWidth) / 2, buttonY + 2 * (buttonHeight + buttonSpacing)),
            CornerRadius = 12,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = _theme.Card,
            ForeColor = _theme.Text,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _settingsButton.Click += (s, e) => _parent.ShowSettings();
        Controls.Add(_settingsButton);

        // Resize handling
        Resize += OnResize;
    }

    private void OnResize(object sender, EventArgs e)
    {
        if (_logoPicture != null)
            _logoPicture.Location = new Point((Width - _logoPicture.Width) / 2, 40);
        if (_welcomeLabel != null)
            _welcomeLabel.Location = new Point((Width - 300) / 2, 140);
        if (_statsLabel != null)
            _statsLabel.Location = new Point((Width - 300) / 2, 180);
        if (_libraryButton != null)
            _libraryButton.Location = new Point((Width - _libraryButton.Width) / 2, 240);
        if (_profileButton != null)
            _profileButton.Location = new Point((Width - _profileButton.Width) / 2, 300);
        if (_settingsButton != null)
            _settingsButton.Location = new Point((Width - _settingsButton.Width) / 2, 360);
    }

    public void UpdateStats(int gameCount, double totalHours, int level)
    {
        _statsLabel.Text = $"{gameCount} jeux • {totalHours:F1}h de jeu • Niveau {level}";
    }
}

// ========== ROUNDED BUTTON ==========

internal sealed class RoundedButton : Button
{
    private int _cornerRadius = 12;
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = value; Invalidate(); }
    }

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Size = new Size(150, 40);
    }

    private GraphicsPath GetFigurePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
        path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        base.OnPaint(pevent);
        var path = GetFigurePath(ClientRectangle, CornerRadius);
        pevent.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        pevent.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        pevent.Graphics.FillPath(new SolidBrush(BackColor), path);
        pevent.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor),
            ClientRectangle, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    }
}

// ========== NEW CLASSES FOR SPLASH SCREEN ==========

internal sealed class SplashForm : Form
{
    private readonly Timer _animationTimer = new() { Interval = 50 };
    private readonly PictureBox _logoPicture = new();
    private readonly Label _loadingLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly Label _statusLabel = new();
    private float _opacity = 0f;
    private int _progress = 0;
    private readonly string _logoPath;

    public SplashForm(string logoPath)
    {
        _logoPath = logoPath;
        InitializeComponent();
        StartAnimation();
    }

    private void InitializeComponent()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(16, 16, 24);
        Size = new Size(600, 400);
        Opacity = 0;
        DoubleBuffered = true;

        _logoPicture.SizeMode = PictureBoxSizeMode.StretchImage;
        _logoPicture.Size = new Size(300, 120);
        _logoPicture.Location = new Point((Width - _logoPicture.Width) / 2, 80);
        if (File.Exists(_logoPath))
        {
            try { _logoPicture.Image = Image.FromFile(_logoPath); }
            catch { _logoPicture.Image = new Bitmap(300, 120); }
        }
        Controls.Add(_logoPicture);

        _loadingLabel.Text = "DekerLab";
        _loadingLabel.Font = new Font("Segoe UI", 24, FontStyle.Bold);
        _loadingLabel.ForeColor = Color.White;
        _loadingLabel.Size = new Size(300, 40);
        _loadingLabel.Location = new Point((Width - _loadingLabel.Width) / 2, 220);
        _loadingLabel.TextAlign = ContentAlignment.MiddleCenter;
        Controls.Add(_loadingLabel);

        _statusLabel.Text = "Chargement...";
        _statusLabel.Font = new Font("Segoe UI", 12);
        _statusLabel.ForeColor = Color.FromArgb(180, 180, 180);
        _statusLabel.Size = new Size(300, 20);
        _statusLabel.Location = new Point((Width - _statusLabel.Width) / 2, 260);
        _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
        Controls.Add(_statusLabel);

        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Value = 0;
        _progressBar.Size = new Size(300, 6);
        _progressBar.Location = new Point((Width - _progressBar.Width) / 2, 290);
        _progressBar.BackColor = Color.FromArgb(40, 40, 40);
        _progressBar.ForeColor = Color.FromArgb(0, 120, 215);
        _progressBar.Style = ProgressBarStyle.Continuous;
        Controls.Add(_progressBar);

        _animationTimer.Tick += OnAnimationTick;
    }

    private void StartAnimation() => _animationTimer.Start();

    private void OnAnimationTick(object sender, EventArgs e)
    {
        if (_opacity < 1.0f) { _opacity += 0.05f; Opacity = _opacity; }
        if (_progress < 100) { _progress += 2; _progressBar.Value = _progress; }
        _statusLabel.Text = _progress < 30 ? "Initialisation..." : 
                           _progress < 60 ? "Chargement des jeux..." : 
                           _progress < 90 ? "Préparation de l'interface..." : "Prêt à lancer !";
        if (_progress >= 100 && Opacity >= 1.0f) { _animationTimer.Stop(); Close(); }
    }
}
