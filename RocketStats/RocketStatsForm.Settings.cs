using System.Drawing.Drawing2D;
using System.Text.Json;

namespace RocketStats;

public partial class RocketStatsForm
{
    private RoundedPanel _settingsContainer = new();
    private Label _settingsTitle = new();
    private Label _themeLabel = new();
    private ComboBox _themeComboBox = new();
    private Label _primaryColorLabel = new();
    private Panel _primaryColorPreview = new();
    private Label _autoRefreshLabel = new();
    private NumericUpDown _autoRefreshNumeric = new();
    private Label _autoRefreshUnit = new();
    private CheckBox _autoLoadCheckBox = new();
    private CheckBox _animationsCheckBox = new();
    private RoundedButton _saveSettingsButton = new();
    private RoundedButton _resetSettingsButton = new();

    private void InitializeSettingsPanel()
    {
        _settingsPanel.Dock = DockStyle.Fill;
        _settingsPanel.BackColor = Color.Transparent;
        _settingsPanel.AutoScroll = true;
        _settingsPanel.Padding = new Padding(20);

        _settingsContainer.Dock = DockStyle.Fill;
        _settingsContainer.CornerRadius = 12;
        _settingsContainer.BackColor = Color.FromArgb(25, 25, 25);
        _settingsContainer.BorderColor = Color.FromArgb(40, 40, 40);
        _settingsContainer.BorderWidth = 1;
        _settingsContainer.Padding = new Padding(20);

        _settingsTitle.Text = "PARAMETRES";
        _settingsTitle.ForeColor = Color.White;
        _settingsTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        _settingsTitle.Dock = DockStyle.Top;
        _settingsTitle.Height = 40;

        _themeLabel.Text = "Theme:";
        _themeLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _themeLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _themeLabel.Location = new Point(20, 60);
        _themeLabel.AutoSize = true;

        _themeComboBox.Items.AddRange(new[] { "Sombre", "Clair" });
        _themeComboBox.SelectedIndex = _settings.Theme == "Light" ? 1 : 0;
        _themeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _themeComboBox.FlatStyle = FlatStyle.Flat;
        _themeComboBox.Location = new Point(150, 56);
        _themeComboBox.Width = 150;
        _themeComboBox.Height = 30;
        _themeComboBox.SelectedIndexChanged += ThemeComboBox_SelectedIndexChanged;

        _primaryColorLabel.Text = "Couleur principale:";
        _primaryColorLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _primaryColorLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _primaryColorLabel.Location = new Point(20, 100);
        _primaryColorLabel.AutoSize = true;

        _primaryColorPreview.BackColor = Color.FromArgb(0, 120, 215);
        _primaryColorPreview.Size = new Size(30, 30);
        _primaryColorPreview.Location = new Point(150, 96);
        _primaryColorPreview.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, _primaryColorPreview.Width, _primaryColorPreview.Height);
            e.Graphics.FillPath(new SolidBrush(_primaryColorPreview.BackColor), path);
        };
        _primaryColorPreview.Click += PrimaryColorPreview_Click;

        _autoRefreshLabel.Text = "Actualisation automatique (minutes):";
        _autoRefreshLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _autoRefreshLabel.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _autoRefreshLabel.Location = new Point(20, 140);
        _autoRefreshLabel.AutoSize = true;

        _autoRefreshNumeric.Minimum = 1;
        _autoRefreshNumeric.Maximum = 1440;
        _autoRefreshNumeric.Value = _settings.AutoRefreshInterval;
        _autoRefreshNumeric.Location = new Point(150, 136);
        _autoRefreshNumeric.Width = 60;
        _autoRefreshNumeric.Height = 30;

        _autoRefreshUnit.Text = "min";
        _autoRefreshUnit.ForeColor = Color.FromArgb(150, 150, 150);
        _autoRefreshUnit.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _autoRefreshUnit.Location = new Point(216, 140);
        _autoRefreshUnit.AutoSize = true;

        _autoLoadCheckBox.Text = "Charger automatiquement le dernier joueur";
        _autoLoadCheckBox.ForeColor = Color.FromArgb(150, 150, 150);
        _autoLoadCheckBox.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _autoLoadCheckBox.Checked = _settings.AutoLoadLastPlayer;
        _autoLoadCheckBox.Location = new Point(20, 180);
        _autoLoadCheckBox.AutoSize = true;

        _animationsCheckBox.Text = "Activer les animations";
        _animationsCheckBox.ForeColor = Color.FromArgb(150, 150, 150);
        _animationsCheckBox.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        _animationsCheckBox.Checked = _settings.ShowAnimations;
        _animationsCheckBox.Location = new Point(20, 210);
        _animationsCheckBox.AutoSize = true;

        _saveSettingsButton.Text = "Sauvegarder";
        _saveSettingsButton.CornerRadius = 8;
        _saveSettingsButton.Width = 120;
        _saveSettingsButton.Location = new Point(20, 250);
        _saveSettingsButton.Click += SaveSettingsButton_Click;

        _resetSettingsButton.Text = "Reinitialiser";
        _resetSettingsButton.CornerRadius = 8;
        _resetSettingsButton.Width = 120;
        _resetSettingsButton.Location = new Point(150, 250);
        _resetSettingsButton.Click += ResetSettingsButton_Click;

        _settingsContainer.Controls.Add(_settingsTitle);
        _settingsContainer.Controls.Add(_themeLabel);
        _settingsContainer.Controls.Add(_themeComboBox);
        _settingsContainer.Controls.Add(_primaryColorLabel);
        _settingsContainer.Controls.Add(_primaryColorPreview);
        _settingsContainer.Controls.Add(_autoRefreshLabel);
        _settingsContainer.Controls.Add(_autoRefreshNumeric);
        _settingsContainer.Controls.Add(_autoRefreshUnit);
        _settingsContainer.Controls.Add(_autoLoadCheckBox);
        _settingsContainer.Controls.Add(_animationsCheckBox);
        _settingsContainer.Controls.Add(_saveSettingsButton);
        _settingsContainer.Controls.Add(_resetSettingsButton);

        _settingsPanel.Controls.Add(_settingsContainer);
    }

    private void ThemeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _settings.Theme = _themeComboBox.SelectedIndex == 0 ? "Dark" : "Light";
        ApplyTheme();
    }

    private void PrimaryColorPreview_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Fonctionnalite de selection de couleur a implementer", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SaveSettingsButton_Click(object? sender, EventArgs e)
    {
        SaveSettings();
    }

    private void ResetSettingsButton_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Voulez-vous vraiment reinitialiser tous les parametres?", "Confirmation",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _settings = new AppSettings();
            _themeComboBox.SelectedIndex = 0;
            _autoRefreshNumeric.Value = 30;
            _autoLoadCheckBox.Checked = true;
            _animationsCheckBox.Checked = true;
            ApplyTheme();
            SaveSettings();
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erreur lors du chargement des parametres: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settings.Theme = _themeComboBox.SelectedIndex == 0 ? "Dark" : "Light";
            _settings.AutoRefreshInterval = (int)_autoRefreshNumeric.Value;
            _settings.AutoLoadLastPlayer = _autoLoadCheckBox.Checked;
            _settings.ShowAnimations = _animationsCheckBox.Checked;
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
            _settings.WindowMaximized = WindowState == FormWindowState.Maximized;

            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
            ApplyTheme();
            MessageBox.Show("Parametres sauvegardes avec succes!", "RocketStats", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erreur lors de la sauvegarde: " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
