namespace Game.Server.Models.Admin;

public record class LevelStatistics(
    string LevelName,
    int RecordCount,
    LevelStatisticsRecord[] TopTen
);
