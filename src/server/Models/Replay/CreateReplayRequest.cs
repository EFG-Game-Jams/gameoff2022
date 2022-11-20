namespace Game.Server.Models.Replay;

public class CreateReplayRequest
{
    public int? TimeInMilliseconds { get; set; }
    public string? LevelName { get; set; }
    //public Dictionary<string, string>? GameSettings { get; set; }
    public string? Data { get; set; }
}