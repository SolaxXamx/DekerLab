using System.Text.Json;

namespace RocketStats;

public partial class RocketStatsForm
{
    private void InitializeCustomComponents()
    {
        _searchTextBox.SetPlaceholderText("Entrez le pseudo Epic...");
        ConfigureLayout();
    }

    private void ApplyTheme()
    {
        if (_settings.Theme == "Dark")
        {
            ApplyDarkTheme();
        }
        else
        {
            ApplyLightTheme();
        }
    }

    private void ApplyDarkTheme()
    {
        BackColor = Color.FromArgb(15, 15, 15);
        _titleBar.Color1 = Color.FromArgb(20, 20, 20);
        _titleBar.Color2 = Color.FromArgb(15, 15, 15);
        _sideBar.BackColor = Color.FromArgb(20, 20, 20);
        _mainContent.BackColor = Color.FromArgb(15, 15, 15);

        _appTitle.ForeColor = Color.White;
        _minimizeButton.ForeColor = Color.White;
        _maximizeButton.ForeColor = Color.White;
        _closeButton.ForeColor = Color.White;
        _userNameLabel.ForeColor = Color.FromArgb(150, 150, 150);

        _dashboardButton.ForeColor = Color.FromArgb(150, 150, 150);
        _profileButton.ForeColor = Color.FromArgb(150, 150, 150);
        _statsButton.ForeColor = Color.FromArgb(150, 150, 150);
        _graphsButton.ForeColor = Color.FromArgb(150, 150, 150);
        _settingsButton.ForeColor = Color.FromArgb(150, 150, 150);

        _searchPanel.BackColor = Color.FromArgb(25, 25, 25);
        _searchPanel.BorderColor = Color.FromArgb(40, 40, 40);
        _profilePreviewPanel.BackColor = Color.Transparent;
        _ranksPanel.BackColor = Color.Transparent;
        _statsCardsPanel.BackColor = Color.Transparent;

        _settingsContainer.BackColor = Color.FromArgb(25, 25, 25);
        _settingsContainer.BorderColor = Color.FromArgb(40, 40, 40);
        _settingsTitle.ForeColor = Color.White;
        _themeLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _primaryColorLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _autoRefreshLabel.ForeColor = Color.FromArgb(150, 150, 150);
        _autoRefreshUnit.ForeColor = Color.FromArgb(150, 150, 150);
        _autoLoadCheckBox.ForeColor = Color.FromArgb(150, 150, 150);
        _animationsCheckBox.ForeColor = Color.FromArgb(150, 150, 150);
    }

    private void ApplyLightTheme()
    {
        BackColor = Color.FromArgb(240, 240, 240);
        _titleBar.Color1 = Color.FromArgb(230, 230, 230);
        _titleBar.Color2 = Color.FromArgb(220, 220, 220);
        _sideBar.BackColor = Color.FromArgb(230, 230, 230);
        _mainContent.BackColor = Color.FromArgb(240, 240, 240);

        _appTitle.ForeColor = Color.Black;
        _minimizeButton.ForeColor = Color.Black;
        _maximizeButton.ForeColor = Color.Black;
        _closeButton.ForeColor = Color.Black;
        _userNameLabel.ForeColor = Color.FromArgb(80, 80, 80);

        _dashboardButton.ForeColor = Color.FromArgb(80, 80, 80);
        _profileButton.ForeColor = Color.FromArgb(80, 80, 80);
        _statsButton.ForeColor = Color.FromArgb(80, 80, 80);
        _graphsButton.ForeColor = Color.FromArgb(80, 80, 80);
        _settingsButton.ForeColor = Color.FromArgb(80, 80, 80);

        _searchPanel.BackColor = Color.White;
        _searchPanel.BorderColor = Color.FromArgb(200, 200, 200);
        _profilePreviewPanel.BackColor = Color.Transparent;
        _ranksPanel.BackColor = Color.Transparent;
        _statsCardsPanel.BackColor = Color.Transparent;

        _settingsContainer.BackColor = Color.White;
        _settingsContainer.BorderColor = Color.FromArgb(200, 200, 200);
        _settingsTitle.ForeColor = Color.Black;
        _themeLabel.ForeColor = Color.FromArgb(80, 80, 80);
        _primaryColorLabel.ForeColor = Color.FromArgb(80, 80, 80);
        _autoRefreshLabel.ForeColor = Color.FromArgb(80, 80, 80);
        _autoRefreshUnit.ForeColor = Color.FromArgb(80, 80, 80);
        _autoLoadCheckBox.ForeColor = Color.FromArgb(80, 80, 80);
        _animationsCheckBox.ForeColor = Color.FromArgb(80, 80, 80);
    }

    private void ShowView(Control view)
    {
        _dashboardButton.IsSelected = view == _dashboardPanel;
        _profileButton.IsSelected = view == _profilePanel;
        _statsButton.IsSelected = view == _statsPanel;
        _graphsButton.IsSelected = view == _graphsPanel;
        _settingsButton.IsSelected = view == _settingsPanel;

        if (_mainContent.Controls.Contains(_currentView))
        {
            _mainContent.Controls.Remove(_currentView);
        }

        _currentView = view;
        _mainContent.Controls.Add(_currentView);

        if (view == _dashboardPanel || view == _profilePanel || view == _statsPanel || view == _graphsPanel)
        {
            UpdateDisplay();
        }
    }

    private async void SearchButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_searchTextBox.Text)) return;
        _currentUsername = _searchTextBox.Text.Trim();
        await LoadPlayerData(_currentUsername);
    }

    private async void RefreshButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentUsername)) return;
        await LoadPlayerData(_currentUsername);
    }

    private void SearchTextBox_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Enter)
        {
            SearchButton_Click(null, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private async Task LoadPlayerData(string username)
    {
        if (_isLoading) return;

        _isLoading = true;
        _loadingSpinner.Visible = true;
        _searchButton.Enabled = false;
        _refreshButton.Enabled = false;
        _searchTextBox.Enabled = false;

        try
        {
            _currentPlayerStats = await DataScraper.ScrapePlayerData(username);
            _currentUsername = username;
            _settings.LastUsername = username;
            SaveSettings();
            UpdateDisplay();
            _userNameLabel.Text = username;
            _userAvatar.Image = ImageHelper.CreateAvatar(username, 48);
            ShowNotification("Donn\u001ees charg\u001ees avec succ\u001as!");
        }
        catch (Exception ex)
        {
            ShowError("Erreur: " + ex.Message);
        }
        finally
        {
            _isLoading = false;
            _loadingSpinner.Visible = false;
            _searchButton.Enabled = true;
            _refreshButton.Enabled = true;
            _searchTextBox.Enabled = true;
        }
    }

    private void UpdateDisplay()
    {
        if (_currentPlayerStats == null) return;

        _profileAvatar.Image = ImageHelper.CreateAvatar(_currentPlayerStats.EpicUsername, 80);
        _profileNameLabel.Text = _currentPlayerStats.EpicUsername;

        var levelInfo = CalculateLevelProgress(_currentPlayerStats.Level, _currentPlayerStats.Xp);
        _levelProgress.Value = levelInfo.ProgressPercentage;
        _profileLevelLabel.Text = "Niveau: " + _currentPlayerStats.Level;

        UpdateRankCard(_rank1v1Card, _currentPlayerStats.Rank1v1, "1v1");
        UpdateRankCard(_rank2v2Card, _currentPlayerStats.Rank2v2, "2v2");
        UpdateRankCard(_rank3v3Card, _currentPlayerStats.Rank3v3, "3v3");

        _totalMatchesCard.Value = _currentPlayerStats.Stats.TotalMatches.ToString("N0");
        _winRateCard.Value = _currentPlayerStats.Stats.WinRate.ToString("F1") + "%";
        _goalsCard.Value = _currentPlayerStats.Stats.TotalGoals.ToString("N0");
        _assistsCard.Value = _currentPlayerStats.Stats.TotalAssists.ToString("N0");
        _savesCard.Value = _currentPlayerStats.Stats.TotalSaves.ToString("N0");
        _mvpsCard.Value = _currentPlayerStats.Stats.TotalMVPs.ToString("N0");

        int hours = (int)(_currentPlayerStats.Stats.TotalPlaytimeMinutes / 60);
        int minutes = (int)(_currentPlayerStats.Stats.TotalPlaytimeMinutes % 60);
        _playtimeCard.Value = hours + "h " + minutes + "m";

        UpdateGraphs();
        ConfigureLayout();
    }

    private (int Level, int ProgressPercentage) CalculateLevelProgress(int level, long xp)
    {
        int xpForCurrentLevel = level * 1000;
        int xpForNextLevel = (level + 1) * 1000;
        int xpInCurrentLevel = (int)(xp % 1000);
        int progress = (int)((xpInCurrentLevel * 100f) / 1000);
        return (level, progress);
    }

    private void UpdateRankCard(RankCard card, RankInfo rank, string playlist)
    {
        card.RankName = playlist + " - " + rank.Tier;
        card.Division = "Division " + rank.Division;
        card.MMR = "MMR: " + rank.MMR;
        card.Matches = rank.MatchesPlayed + " matchs (" + rank.Wins + "W-" + rank.Losses + "L)";
        card.RankImage = RankIcons.GetRankImage(rank.Tier.ToLower());
        int divisionProgress = rank.Division * 25;
        card.ProgressValue = divisionProgress;
    }

    private void UpdateGraphs()
    {
        if (_currentPlayerStats == null || _currentPlayerStats.MMRHistory.Count == 0) return;

        var mmrData = _currentPlayerStats.MMRHistory.Select(h => (float)h.MMR3v3).ToList();
        _mmrGraph.DataPoints = mmrData;

        var winsData = new List<float>();
        var random = new Random();
        int currentWins = 0;
        foreach (var entry in _currentPlayerStats.MMRHistory)
        {
            currentWins += random.Next(0, 5);
            winsData.Add(currentWins);
        }
        _winsGraph.DataPoints = winsData;
    }

    private void ConfigureLayout()
    {
        int availableWidth = _mainContent.Width - 40;
        int cardWidth = availableWidth / 3;

        _rank1v1Card.Width = cardWidth;
        _rank1v1Card.Location = new Point(0, 25);
        _rank2v2Card.Width = cardWidth;
        _rank2v2Card.Location = new Point(cardWidth + 10, 25);
        _rank2v2Card.Visible = true;
        _rank3v3Card.Width = cardWidth;
        _rank3v3Card.Location = new Point((cardWidth + 10) * 2, 25);
        _rank3v3Card.Visible = true;

        int statCardWidth = availableWidth / 4;
        _totalMatchesCard.Width = statCardWidth;
        _totalMatchesCard.Location = new Point(0, 25);
        _winRateCard.Width = statCardWidth;
        _winRateCard.Location = new Point(statCardWidth + 10, 25);
        _goalsCard.Width = statCardWidth;
        _goalsCard.Location = new Point((statCardWidth + 10) * 2, 25);
        _assistsCard.Width = statCardWidth;
        _assistsCard.Location = new Point((statCardWidth + 10) * 3, 25);
        _savesCard.Width = statCardWidth;
        _savesCard.Location = new Point(0, 115);
        _mvpsCard.Width = statCardWidth;
        _mvpsCard.Location = new Point(statCardWidth + 10, 115);
        _playtimeCard.Width = statCardWidth;
        _playtimeCard.Location = new Point((statCardWidth + 10) * 2, 115);
    }

    private void ShowNotification(string message)
    {
        MessageBox.Show(message, "RocketStats", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStartPoint = new Point(e.X, e.Y);
        }
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Point p = PointToScreen(e.Location);
            Location = new Point(p.X - _dragStartPoint.X, p.Y - _dragStartPoint.Y);
        }
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void RocketStatsForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settings.WindowMaximized = WindowState == FormWindowState.Maximized;
        try
        {
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }

    private void RocketStatsForm_Resize(object? sender, EventArgs e)
    {
        if (_currentView == _dashboardPanel)
        {
            ConfigureLayout();
        }
    }
}
