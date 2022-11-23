using Game.Server.Entities;
using Game.Server.Models.Leaderboard;
using Game.Server.Models.Replay;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Services;

public class GameService
{
    private readonly FileService fileService;
    private readonly ReplayDatabase replayDatabase;
    private readonly ILogger<GameService> logger;

    public GameService(
        FileService fileService,
        ReplayDatabase replayDatabase,
        ILogger<GameService> logger)
    {
        this.fileService = fileService;
        this.replayDatabase = replayDatabase;
        this.logger = logger;
    }

    /// <summary>
    /// Returns the secret for the client to store locally to identify itself
    /// with in API calls
    /// </summary>
    public async Task<Guid> CreateSession(string name, int itchIdentifier)
    {
        var player = await replayDatabase.Players
            .SingleOrDefaultAsync(p => p.ItchIdentifier == itchIdentifier);

        if (player == null)
        {
            logger.LogInformation("Creating new player for {Name}", name);
            player = new()
            {
                ItchIdentifier = itchIdentifier,
                Name = name,
            };
        }

        logger.LogInformation("Creating new session for player with name {Name}", name);
        SessionEntity session = new()
        {
            CreatedUtc = DateTime.UtcNow,
            Player = player,
            Secret = Guid.NewGuid()
        };
        replayDatabase.Add(session);
        await replayDatabase.SaveChangesAsync();

        return session.Secret;
    }

    public async Task<(int Id, string Name)> GetPlayerFor(Guid secret)
    {
        var player = await replayDatabase.Sessions
            .AsNoTracking()
            .Select(s => new { s.Secret, s.PlayerId, s.Player.Name })
            .SingleAsync(s => s.Secret == secret);
        return (player.PlayerId, player.Name);
    }

    public async Task<int?> TryGetPlayerIdFor(Guid secret)
    {
        return (await replayDatabase.Sessions
            .AsNoTracking()
            .Select(s => new { s.Secret, s.Player.Id })
            .SingleOrDefaultAsync(s => s.Secret == secret))
            ?.Id;
    }

    public async Task<int> SaveReplay(
        Guid secret,
        uint gameRevision,
        uint timeInMilliseconds,
        string levelName,
        string data)
    {
        var player = await GetSessionPlayer(secret);
        var level = await GetOrCreateLevel(levelName);

        logger.LogInformation(
            "Storing replay for {PlayerName} {PlayerId} {SessionSecret} with level {LevelName}",
            player.Name,
            player.Id,
            secret,
            levelName);

        var (fileId, fileSize) = await fileService.StoreReplayData(data);
        try
        {
            var replay = await replayDatabase.Replays
                .FirstOrDefaultAsync(r =>
                    r.GameRevision == gameRevision &&
                    r.LevelId == level.Id &&
                    r.PlayerId == player.Id);
            Guid? previousReplayFileIdToDelete = null;
            if (replay == null)
            {
                replay = new ReplayEntity
                {
                    FileName = fileId,
                    FileSize = fileSize,
                    GameRevision = gameRevision,
                    Level = level,
                    Player = player,
                    TimeInMilliseconds = timeInMilliseconds
                };
                replayDatabase.Add(replay);
            }
            else if (replay.TimeInMilliseconds < timeInMilliseconds)
            {
                logger.LogInformation(
                    "New replay of {PlayerName} {PlayerId} {SessionSecret} with level {LevelName} is slower {NewTimeInMilliseconds} than historic replay {ExistingTimeInMilliseconds}",
                    player.Name,
                    player.Id,
                    secret,
                    levelName,
                    timeInMilliseconds,
                    replay.TimeInMilliseconds);
                return replay.Id;
            }
            else
            {
                previousReplayFileIdToDelete = replay.FileName;
                replay.FileName = fileId;
                replay.FileSize = fileSize;
                replay.TimeInMilliseconds = timeInMilliseconds;
            }
            await replayDatabase.SaveChangesAsync();

            if (previousReplayFileIdToDelete.HasValue)
            {
                fileService.TryDeleteReplay(previousReplayFileIdToDelete.Value);
            }

            return replay.Id;
        }
        catch (Exception)
        {
            fileService.TryDeleteReplay(fileId);
            throw;
        }
    }

    public async Task<ReplayResponse> DownloadReplay(Guid secret, int replayId)
    {
        var session = await replayDatabase.Sessions
            .AsNoTracking()
            .Select(s => new { s.Secret, s.PlayerId, PlayerName = s.Player.Name })
            .SingleOrDefaultAsync(s => s.Secret == secret);
        if (session == null)
        {
            logger.LogWarning(
                "Session with unknown secret {SessionSecret} attempted to download replay with ID {ReplayId}",
                secret,
                replayId);
            throw new InvalidOperationException("Nonexisting sessions are not allowed to download replays");
        }

        var replay = await replayDatabase.Replays
            .AsNoTracking()
            .Select(r => new
            {
                r.Id,
                r.FileName,
                r.LevelId,
                LevelName = r.Level.Name,
                r.GameRevision,
                r.TimeInMilliseconds
            })
            .SingleAsync(r => r.Id == replayId);

        return new ReplayResponse(
            session.PlayerId,
            session.PlayerName,
            replay.LevelId,
            replay.LevelName,
            replay.GameRevision,
            replay.TimeInMilliseconds,
            await fileService.ReadReplay(replay.FileName));
    }

    public async Task<bool> SessionExists(Guid secret) => await replayDatabase.Sessions
        .AnyAsync(s => s.Secret == secret);

    public async Task<IQueryable<ReplayEntity>> GetGlobalLeaderboardQuery(uint gameRevision, string levelName)
    {
        var level = await replayDatabase.Levels
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Name == levelName);

        if (level == null)
        {
            logger.LogInformation(
                "Requesting leaderboard query for non-existing level {LevelName}",
                levelName);
            // This will always yield an empty set whilst supporting async LINQ
            // EF extension method calls
            return replayDatabase.Replays.Where(r => false);
        }

        return replayDatabase.Replays
            .AsNoTracking()
            .Include(r => r.Player)
            .Include(r => r.Level)
            .Where(r => r.GameRevision == gameRevision && r.LevelId == level.Id);
    }

    public IQueryable<ReplayEntity> GetPersonalLeaderboardQuery(
        uint gameRevision,
        int playerId)
    {
        return replayDatabase.Replays
            .AsNoTracking()
            .Include(r => r.Player)
            .Include(r => r.Level)
            .Where(r => r.GameRevision == gameRevision && r.PlayerId == playerId);
    }

    private async Task<PlayerEntity> GetSessionPlayer(Guid secret)
    {
        return await replayDatabase.Players
            .SingleAsync(p => p.Sessions.Any(s => s.Secret == secret));
    }

    private async Task<LevelEntity> GetOrCreateLevel(string levelName)
    {
        var level = await replayDatabase.Levels
            .FirstOrDefaultAsync(l => l.Name == levelName);
        if (level == null)
        {
            logger.LogInformation("Creating level with name {LevelName}", levelName);
            level = new LevelEntity
            {
                Name = levelName,
            };
            replayDatabase.Add(level);
        }

        return level;
    }
}