namespace Game.Server.Entities;

#nullable disable
public class SessionEntity
{
    public int Id { get; set; }

    public int PlayerId { get; set; }
    public PlayerEntity Player { get; set; }

    public DateTime CreatedUtc { get; set; }

    public Guid Secret { get; set; }
}