using System.Drawing.Drawing2D;

namespace RocketStats;

public partial class RocketStatsForm
{
    private void InitializeRanksPanel()
    {
        _ranksPanel.Dock = DockStyle.Top;
        _ranksPanel.Height = 140;
        _ranksPanel.BackColor = Color.Transparent;
        _ranksPanel.Padding = new Padding(0, 10, 0, 10);

        var ranksTitle = new Label
        {
            Text = "RANGS",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(0, 0),
            AutoSize = true
        };

        _ranksPanel.Controls.Add(ranksTitle);

        _rank1v1Card.RankName = "1v1";
        _rank1v1Card.Division = "-";
        _rank1v1Card.MMR = "MMR: 0";
        _rank1v1Card.Matches = "0 matchs";
        _rank1v1Card.RankImage = RankIcons.GetRankImage("unranked");
        _rank1v1Card.Location = new Point(0, 25);
        _rank1v1Card.Width = _mainContent.Width - 40;

        _rank2v2Card.RankName = "2v2";
        _rank2v2Card.Division = "-";
        _rank2v2Card.MMR = "MMR: 0";
        _rank2v2Card.Matches = "0 matchs";
        _rank2v2Card.RankImage = RankIcons.GetRankImage("unranked");
        _rank2v2Card.Location = new Point(0, 25);
        _rank2v2Card.Width = _mainContent.Width - 40;
        _rank2v2Card.Visible = false;

        _rank3v3Card.RankName = "3v3";
        _rank3v3Card.Division = "-";
        _rank3v3Card.MMR = "MMR: 0";
        _rank3v3Card.Matches = "0 matchs";
        _rank3v3Card.RankImage = RankIcons.GetRankImage("unranked");
        _rank3v3Card.Location = new Point(0, 25);
        _rank3v3Card.Width = _mainContent.Width - 40;
        _rank3v3Card.Visible = false;

        _ranksPanel.Controls.Add(_rank1v1Card);
        _ranksPanel.Controls.Add(_rank2v2Card);
        _ranksPanel.Controls.Add(_rank3v3Card);
    }

    private void InitializeStatsCardsPanel()
    {
        _statsCardsPanel.Dock = DockStyle.Top;
        _statsCardsPanel.Height = 200;
        _statsCardsPanel.BackColor = Color.Transparent;
        _statsCardsPanel.Padding = new Padding(0, 10, 0, 10);

        var statsTitle = new Label
        {
            Text = "STATISTIQUES PRINCIPALES",
            ForeColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(0, 0),
            AutoSize = true
        };

        _statsCardsPanel.Controls.Add(statsTitle);

        _totalMatchesCard.Title = "Matchs jou\u0015s";
        _totalMatchesCard.Value = "0";
        _totalMatchesCard.SubValue = "Total";
        _totalMatchesCard.Icon = CreateStatIcon("\uD83C\uDFAE");
        _totalMatchesCard.Location = new Point(0, 25);

        _winRateCard.Title = "Taux de victoire";
        _winRateCard.Value = "0%";
        _winRateCard.SubValue = "Win Rate";
        _winRateCard.Icon = CreateStatIcon("\uD83C\uDFC6");
        _winRateCard.Location = new Point(0, 25);

        _goalsCard.Title = "Buts";
        _goalsCard.Value = "0";
        _goalsCard.SubValue = "Total";
        _goalsCard.Icon = CreateStatIcon("\u26BD");
        _goalsCard.Location = new Point(0, 25);

        _assistsCard.Title = "Passes d\u0019cisives";
        _assistsCard.Value = "0";
        _assistsCard.SubValue = "Total";
        _assistsCard.Icon = CreateStatIcon("\uD83C\uDFAF");
        _assistsCard.Location = new Point(0, 25);

        _savesCard.Title = "Arr\u001ats";
        _savesCard.Value = "0";
        _savesCard.SubValue = "Total";
        _savesCard.Icon = CreateStatIcon("\uD83D\uDEE1\u200D\uD83D\uDCA8");
        _savesCard.Location = new Point(0, 25);

        _mvpsCard.Title = "MVP";
        _mvpsCard.Value = "0";
        _mvpsCard.SubValue = "Total";
        _mvpsCard.Icon = CreateStatIcon("\u2B50");
        _mvpsCard.Location = new Point(0, 25);

        _playtimeCard.Title = "Temps de jeu";
        _playtimeCard.Value = "0h";
        _playtimeCard.SubValue = "Total";
        _playtimeCard.Icon = CreateStatIcon("\u23F1");
        _playtimeCard.Location = new Point(0, 25);

        _statsCardsPanel.Controls.Add(_totalMatchesCard);
        _statsCardsPanel.Controls.Add(_winRateCard);
        _statsCardsPanel.Controls.Add(_goalsCard);
        _statsCardsPanel.Controls.Add(_assistsCard);
        _statsCardsPanel.Controls.Add(_savesCard);
        _statsCardsPanel.Controls.Add(_mvpsCard);
        _statsCardsPanel.Controls.Add(_playtimeCard);
    }

    private Image CreateStatIcon(string emoji)
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var font = new Font("Segoe UI Emoji", 16);
        using var brush = new SolidBrush(Color.White);
        g.DrawString(emoji, font, brush, 0, 4);
        return bmp;
    }

    private void InitializeProfilePanel()
    {
        _profilePanel.Dock = DockStyle.Fill;
        _profilePanel.BackColor = Color.Transparent;
        _profilePanel.AutoScroll = true;
        _profilePanel.Padding = new Padding(10);

        var title = new Label
        {
            Text = "PROFIL DU JOUEUR",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 40
        };

        _profilePanel.Controls.Add(title);
    }

    private void InitializeStatsPanel()
    {
        _statsPanel.Dock = DockStyle.Fill;
        _statsPanel.BackColor = Color.Transparent;
        _statsPanel.AutoScroll = true;
        _statsPanel.Padding = new Padding(10);

        var title = new Label
        {
            Text = "STATISTIQUES D\u0014TAILL\u0015ES",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 40
        };

        _statsPanel.Controls.Add(title);
    }

    private void InitializeGraphsPanel()
    {
        _graphsPanel.Dock = DockStyle.Fill;
        _graphsPanel.BackColor = Color.Transparent;
        _graphsPanel.AutoScroll = true;
        _graphsPanel.Padding = new Padding(10);

        var title = new Label
        {
            Text = "GRAPHIQUES",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 40
        };

        _mmrGraph.Dock = DockStyle.Top;
        _mmrGraph.Height = 250;
        _mmrGraph.Margin = new Padding(0, 10, 0, 10);

        var mmrTitle = new Label
        {
            Text = "\u0012volution du MMR (3v3)",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(0, 0, 0, 5)
        };

        _winsGraph.Dock = DockStyle.Top;
        _winsGraph.Height = 250;
        _winsGraph.Margin = new Padding(0, 10, 0, 10);

        var winsTitle = new Label
        {
            Text = "\u0012volution des victoires",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(0, 0, 0, 5)
        };

        _graphsPanel.Controls.Add(title);
        _graphsPanel.Controls.Add(mmrTitle);
        _graphsPanel.Controls.Add(_mmrGraph);
        _graphsPanel.Controls.Add(winsTitle);
        _graphsPanel.Controls.Add(_winsGraph);
    }
}
