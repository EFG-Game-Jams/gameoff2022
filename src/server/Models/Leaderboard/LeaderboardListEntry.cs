namespace Game.Server.Models.Leaderboard;

public record class LeaderboardListEntry(
    int PlayerId,
    string PlayerName,
    int LevelId,
    string LevelName,
    uint GameRevision,
    uint TimeInMilliseconds,
    int ReplayId);