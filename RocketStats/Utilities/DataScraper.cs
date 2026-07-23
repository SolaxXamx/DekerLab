namespace RocketStats;

public static class DataScraper
{
    public static async Task<PlayerStats> ScrapePlayerData(string username)
    {
        await Task.Delay(1500);

        var random = new Random();
        var ranks = new[] { "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Champion", "Grand Champion" };
        var rankIndex = random.Next(0, ranks.Length);

        var stats = new PlayerStats
        {
            EpicUsername = username,
            AvatarUrl = "",
            Level = random.Next(50, 200),
            Xp = random.Next(10000, 100000),

            Rank1v1 = new RankInfo
            {
                Tier = ranks[Math.Max(0, rankIndex - 1)],
                Division = random.Next(1, 4),
                MMR = random.Next(800, 1500),
                MatchesPlayed = random.Next(50, 500),
                Wins = random.Next(20, 300),
                Losses = random.Next(20, 300),
                WinRate = Math.Round(random.NextDouble() * 100, 1)
            },

            Rank2v2 = new RankInfo
            {
                Tier = ranks[rankIndex],
                Division = random.Next(1, 4),
                MMR = random.Next(1000, 1800),
                MatchesPlayed = random.Next(100, 800),
                Wins = random.Next(50, 500),
                Losses = random.Next(50, 500),
                WinRate = Math.Round(random.NextDouble() * 100, 1)
            },

            Rank3v3 = new RankInfo
            {
                Tier = ranks[Math.Min(ranks.Length - 1, rankIndex + 1)],
                Division = random.Next(1, 4),
                MMR = random.Next(1200, 2000),
                MatchesPlayed = random.Next(150, 1000),
                Wins = random.Next(80, 600),
                Losses = random.Next(80, 600),
                WinRate = Math.Round(random.NextDouble() * 100, 1)
            },

            Stats = new PlayerStatistics
            {
                TotalMatches = random.Next(500, 2000),
                TotalWins = random.Next(300, 1500),
                TotalLosses = random.Next(200, 1000),
                TotalGoals = random.Next(1000, 5000),
                TotalAssists = random.Next(500, 3000),
                TotalSaves = random.Next(800, 4000),
                TotalMVPs = random.Next(50, 300),
                TotalPlaytimeMinutes = random.Next(1000, 10000),
                WinRate = Math.Round(random.NextDouble() * 100, 1)
            },

            MMRHistory = GenerateMMRHistory(10),
            LastUpdated = DateTime.Now
        };

        return stats;
    }

    private static List<MMRHistoryEntry> GenerateMMRHistory(int count)
    {
        var history = new List<MMRHistoryEntry>();
        var random = new Random();
        var currentMMR = 1200;

        for (int i = 0; i < count; i++)
        {
            currentMMR += random.Next(-50, 50);
            currentMMR = Math.Max(800, Math.Min(2000, currentMMR));

            history.Add(new MMRHistoryEntry
            {
                Date = DateTime.Now.AddDays(-count + i),
                MMR1v1 = currentMMR - random.Next(0, 200),
                MMR2v2 = currentMMR,
                MMR3v3 = currentMMR + random.Next(0, 200)
            });
        }

        return history;
    }
}
