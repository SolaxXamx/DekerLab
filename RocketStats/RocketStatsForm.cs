using System.Drawing.Drawing2D;
using System.Text.Json;

namespace RocketStats;

public partial class RocketStatsForm : Form
{
    private AppSettings _settings = new();
    private PlayerStats? _currentPlayerStats;
    private string _currentUsername = string.Empty;
    private bool _isLoading = false;
    private bool _isDragging = false;
    private Point _dragStartPoint;

    private static readonly string AppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RocketStats");
    private static readonly string SettingsFile = Path.Combine(AppFolder, "settings.json");
    private static readonly string CacheFolder = Path.Combine(AppFolder, "Cache");

    public RocketStatsForm()
    {
        InitializeComponent();
        LoadSettings();
        InitializeCustomComponents();
        ApplyTheme();
    }
}
