using System.ComponentModel.DataAnnotations;

namespace Game.Server.Entities;

#nullable disable
public class LevelEntity
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string Name { get; set; }

    public List<ReplayEntity> Replays { get; set; }
}
