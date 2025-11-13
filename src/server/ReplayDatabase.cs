using Game.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace Game.Server;

#nullable disable
public class ReplayDatabase : DbContext
{
    public DbSet<LevelEntity> Levels { get; set; }
    public DbSet<PlayerEntity> Players { get; set; }
    public DbSet<ReplayEntity> Replays { get; set; }
    public DbSet<SessionEntity> Sessions { get; set; }

    public string DbPath { get; }

    public ReplayDatabase(IHostEnvironment environment)
    {
        DbPath = Path.Join(environment.ContentRootPath, "taintrocket.db");
    }

    // The following configures EF to create a Sqlite database file
    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LevelEntity>().HasIndex(e => e.Name).IsUnique();

        modelBuilder.Entity<PlayerEntity>().HasIndex(e => e.ItchIdentifier).IsUnique();
        modelBuilder.Entity<PlayerEntity>().HasIndex(e => e.Name);

        modelBuilder.Entity<ReplayEntity>().HasIndex(e => e.FileName).IsUnique();
        modelBuilder.Entity<ReplayEntity>().HasIndex(e => e.TimeInMilliseconds);
        modelBuilder.Entity<ReplayEntity>().HasIndex(e => e.GameRevision);
        modelBuilder
            .Entity<ReplayEntity>()
            .HasIndex(e => new
            {
                e.PlayerId,
                e.LevelId,
                e.GameRevision,
            })
            .IsUnique();

        modelBuilder.Entity<SessionEntity>().HasIndex(e => e.Secret).IsUnique();
    }
}
