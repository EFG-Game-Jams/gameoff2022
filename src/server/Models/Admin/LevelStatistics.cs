namespace Game.Server.Models.Admin;

public record class LevelStatistics(string LevelName, LevelStatisticsRecord[] TopTen);