namespace Game.Server.Models.Replay;

public record class ReplayResponse(
    int PlayerId,
    string PlayerName,
    int LevelId,
    string LevelName,
    uint GameRevision,
    uint TimeInMilliseconds,
    string Data);