namespace Game.Server.Entities;

#nullable disable
public class ReplayEntity
{
    public int Id { get; set; }

    public uint TimeInMilliseconds { get; set; }

    public Guid FileName { get; set; }

    public long FileSize { get; set; }

    public uint GameRevision { get; set; }

    // TODO Add JSON column for the flexible options

    public int PlayerId { get; set; }
    public PlayerEntity Player { get; set; }

    public int LevelId { get; set; }
    public LevelEntity Level { get; set; }
}