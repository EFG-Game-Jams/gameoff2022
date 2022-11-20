namespace Game.Server.Models.Leaderboard;

public record class LeaderboardListResponse(
    LeaderboardListEntry[] Items,
    int TotalCount);