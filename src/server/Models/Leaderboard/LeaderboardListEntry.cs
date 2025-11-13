namespace Game.Server.Models.Leaderboard;

public record class LeaderboardListEntry(
    int Rank,
    int PlayerId,
    string PlayerName,
    int LevelId,
    string LevelName,
    uint GameRevision,
    uint TimeInMilliseconds,
    int ReplayId
);
