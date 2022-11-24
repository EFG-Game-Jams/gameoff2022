using System;

[Serializable]
public class LeaderboardListEntry
{
    public int rank;
    public int playerId;
    public string playerName;
    public int levelId;
    public string levelName;
    public int gameRevision;
    public int timeInMilliseconds;
    public int replayId;
}