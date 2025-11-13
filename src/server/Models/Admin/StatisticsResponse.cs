namespace Game.Server.Models.Admin;

public record class StatisticsResponse(
    int PlayerCount,
    int SessionCount,
    LevelStatistics[] LevelStatistics
);
