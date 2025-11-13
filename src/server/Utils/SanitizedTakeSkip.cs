namespace Game.Server.Utils;

public class SanitizedTakeSkip
{
    public SanitizedTakeSkip(int? take, int? skip)
    {
        Take = Math.Clamp(take ?? 0, 10, 50);
        Skip = Math.Max(skip ?? 0, 0);
    }

    public int Take { get; }
    public int Skip { get; }
}
