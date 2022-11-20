using System.ComponentModel.DataAnnotations;

namespace Game.Server.Entities;

#nullable disable
public class PlayerEntity
{
    public int Id { get; set; }

    public int ItchIdentifier { get; set; }

    [Required]
    [MaxLength(64)]
    public string Name { get; set; }

    public List<ReplayEntity> Replays { get; set; }
    public List<SessionEntity> Sessions { get; set; }
}