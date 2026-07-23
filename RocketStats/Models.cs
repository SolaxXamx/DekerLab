using System.Text.Json.Serialization;

namespace RocketStats;

// ============================================
// DATA MODELS
// ============================================

public class PlayerStats
{
    [JsonPropertyName("epicUsername")]
    public string EpicUsername { get; set; } = string.Empty;
    
    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;
    
    [JsonPropertyName("xp")]
    public long Xp { get; set; } = 0;
    
    [JsonPropertyName("rank1v1")]
    public RankInfo Rank1v1 { get; set; } = new();
    
    [JsonPropertyName("rank2v2")]
    public RankInfo Rank2v2 { get; set; } = new();
    
    [JsonPropertyName("rank3v3")]
    public RankInfo Rank3v3 { get; set; } = new();
    
    [JsonPropertyName("stats")]
    public PlayerStatistics Stats { get; set; } = new();
    
    [JsonPropertyName("mmrHistory")]
    public List<MMRHistoryEntry> MMRHistory { get; set; } = new();
    
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

public class RankInfo
{
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "Unranked";
    
    [JsonPropertyName("division")]
    public int Division { get; set; } = 0;
    
    [JsonPropertyName("mmr")]
    public int MMR { get; set; } = 0;
    
    [JsonPropertyName("matchesPlayed")]
    public int MatchesPlayed { get; set; } = 0;
    
    [JsonPropertyName("wins")]
    public int Wins { get; set; } = 0;
    
    [JsonPropertyName("losses")]
    public int Losses { get; set; } = 0;
    
    [JsonPropertyName("winRate")]
    public double WinRate { get; set; } = 0;
}

public class PlayerStatistics
{
    [JsonPropertyName("totalMatches")]
    public int TotalMatches { get; set; } = 0;
    
    [JsonPropertyName("totalWins")]
    public int TotalWins { get; set; } = 0;
    
    [JsonPropertyName("totalLosses")]
    public int TotalLosses { get; set; } = 0;
    
    [JsonPropertyName("totalGoals")]
    public int TotalGoals { get; set; } = 0;
    
    [JsonPropertyName("totalAssists")]
    public int TotalAssists { get; set; } = 0;
    
    [JsonPropertyName("totalSaves")]
    public int TotalSaves { get; set; } = 0;
    
    [JsonPropertyName("totalMVPs")]
    public int TotalMVPs { get; set; } = 0;
    
    [JsonPropertyName("totalPlaytimeMinutes")]
    public double TotalPlaytimeMinutes { get; set; } = 0;
    
    [JsonPropertyName("winRate")]
    public double WinRate { get; set; } = 0;
}

public class MMRHistoryEntry
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; } = DateTime.Now;
    
    [JsonPropertyName("mmr1v1")]
    public int MMR1v1 { get; set; } = 0;
    
    [JsonPropertyName("mmr2v2")]
    public int MMR2v2 { get; set; } = 0;
    
    [JsonPropertyName("mmr3v3")]
    public int MMR3v3 { get; set; } = 0;
}

public class AppSettings
{
    [JsonPropertyName("lastUsername")]
    public string LastUsername { get; set; } = string.Empty;
    
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Dark";
    
    [JsonPropertyName("primaryColor")]
    public string PrimaryColor { get; set; } = "#0078D7";
    
    [JsonPropertyName("secondaryColor")]
    public string SecondaryColor { get; set; } = "#00BFFF";
    
    [JsonPropertyName("autoRefreshInterval")]
    public int AutoRefreshInterval { get; set; } = 30;
    
    [JsonPropertyName("autoLoadLastPlayer")]
    public bool AutoLoadLastPlayer { get; set; } = true;
    
    [JsonPropertyName("showAnimations")]
    public bool ShowAnimations { get; set; } = true;
    
    [JsonPropertyName("windowWidth")]
    public int WindowWidth { get; set; } = 1200;
    
    [JsonPropertyName("windowHeight")]
    public int WindowHeight { get; set; } = 800;
    
    [JsonPropertyName("windowMaximized")]
    public bool WindowMaximized { get; set; } = false;
}
