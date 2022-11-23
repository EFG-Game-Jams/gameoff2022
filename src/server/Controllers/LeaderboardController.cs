using Game.Server.Entities;
using Game.Server.Enums;
using Game.Server.Models.Leaderboard;
using Game.Server.Services;
using Game.Server.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Controllers;

// https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
[AllowAnonymous]
[ApiController]
[EnableCors]
public class LeaderboardController : ControllerBase
{
    private readonly GameService gameService;

    public LeaderboardController(GameService gameService)
    {
        this.gameService = gameService;
    }

    [HttpGet]
    [Route("/api/game/{revision}/session/{sessionSecret}/leaderboard")]
    public async Task<ActionResult<LeaderboardListResponse>> GetGlobalLeaderboard(
        [FromRoute] uint revision,
        [FromRoute] Guid sessionSecret,
        [FromQuery] string levelName,
        [FromQuery] int? take,
        [FromQuery] int? skip,
        [FromQuery] LeaderboardSortOrder? sortOrder)
    {
        if (!await gameService.SessionExists(sessionSecret))
        {
            return Unauthorized();
        }
        if (string.IsNullOrWhiteSpace(levelName))
        {
            return BadRequest();
        }

        var query = await gameService.GetGlobalLeaderboardQuery(revision, levelName);
        query = ApplySort(query, sortOrder);

        return new LeaderboardListResponse(
            (await ApplyTakeSkip(query, take, skip).ToArrayAsync())
                .Select(ToListItem)
                .ToArray(),
            await query.CountAsync());
    }

    // Always returns an ascending list (by time)
    [HttpGet]
    [Route("/api/game/{revision}/session/{sessionSecret}/leaderboard/neighbours")]
    public async Task<ActionResult<LeaderboardListResponse>> GetLeaderboardNeighbours(
        [FromRoute] uint revision,
        [FromRoute] Guid sessionSecret,
        [FromQuery] string levelName,
        [FromQuery] int? take,
        [FromQuery] int? skip)
    {
        var playerId = await gameService.TryGetPlayerIdFor(sessionSecret);
        if (playerId == null)
        {
            return Unauthorized();
        }

        var query = await gameService.GetGlobalLeaderboardQuery(revision, levelName);

        var playerPerfomance = await query.FirstOrDefaultAsync(e => e.PlayerId == playerId);
        if (playerPerfomance == null)
        {
            return new LeaderboardListResponse(
                Array.Empty<LeaderboardListEntry>(),
                0);
        }

        var takeSkip = new SanitizedTakeSkip(take, skip);

        var fasterRecords = await query
            .Where(e => e.TimeInMilliseconds < playerPerfomance.TimeInMilliseconds)
            .OrderByDescending(e => e.TimeInMilliseconds)
            .Take(takeSkip.Take - 1)
            .ToArrayAsync();

        var slowerRecords = await query
            .Where(e => e.TimeInMilliseconds > playerPerfomance.TimeInMilliseconds)
            .OrderBy(e => e.TimeInMilliseconds)
            .Take(takeSkip.Take - 1)
            .ToArrayAsync();

        var targetTakeFaster = takeSkip.Take / 2;
        var targetTakeSlower = takeSkip.Take / 2;
        if (takeSkip.Take % 2 == 0)
        {
            // Decrement one spot for the player performance record to insert
            // into
            --targetTakeSlower;
        }

        bool hasEnoughFaster = targetTakeFaster <= fasterRecords.Length;
        bool hasEnoughSlower = targetTakeSlower <= slowerRecords.Length;

        var items = new List<ReplayEntity>();
        if (hasEnoughFaster && hasEnoughSlower)
        {
            items.AddRange(fasterRecords.Take(targetTakeFaster).Reverse());
            items.Add(playerPerfomance);
            items.AddRange(slowerRecords.Take(targetTakeSlower));
        }
        else if (!hasEnoughFaster && hasEnoughSlower)
        {
            items.AddRange(fasterRecords.Reverse());
            items.Add(playerPerfomance);
            items.AddRange(slowerRecords
                .Take(Math.Min(slowerRecords.Length, targetTakeSlower + (targetTakeFaster - fasterRecords.Length))));
        }
        else if (hasEnoughFaster && !hasEnoughSlower)
        {
            items.AddRange(fasterRecords
                .Take(Math.Min(fasterRecords.Length, targetTakeFaster + (targetTakeSlower - slowerRecords.Length)))
                .Reverse());
            items.Add(playerPerfomance);
            items.AddRange(slowerRecords);
        }
        else // not enough from either
        {
            items.AddRange(fasterRecords.Reverse());
            items.Add(playerPerfomance);
            items.AddRange(slowerRecords);
        }

        return new LeaderboardListResponse(
            items.Select(ToListItem).ToArray(),
            takeSkip.Take); // The count doesn't really make sense in this context
    }

    [HttpGet]
    [Route("/api/game/{revision}/session/{sessionSecret}/leaderboard/personal")]
    public async Task<ActionResult<LeaderboardListResponse>> GetPersonalLeaderboard(
        [FromRoute] uint revision,
        [FromRoute] Guid sessionSecret,
        [FromQuery] LeaderboardSortOrder? sortOrder)
    {
        var playerId = await gameService.TryGetPlayerIdFor(sessionSecret);
        if (playerId == null)
        {
            return Unauthorized();
        }

        var query = ApplySort(
            gameService.GetPersonalLeaderboardQuery(revision, playerId.Value),
            sortOrder);

        var items = (await query.ToArrayAsync())
            .Select(ToListItem)
            .ToArray();
        return new LeaderboardListResponse(items, items.Length);
    }

    private static IQueryable<ReplayEntity> ApplySort(
        IQueryable<ReplayEntity> query,
        LeaderboardSortOrder? order)
    {
        return order switch
        {
            LeaderboardSortOrder.TimeAscending or null => query.OrderBy(e => e.TimeInMilliseconds),
            LeaderboardSortOrder.TimeDescending => query.OrderByDescending(e => e.TimeInMilliseconds),
            _ => throw new ArgumentOutOfRangeException(nameof(order), $"Sort order value {order} is not supported")
        };
    }

    private static IQueryable<ReplayEntity> ApplyTakeSkip(
        IQueryable<ReplayEntity> query,
        int? take,
        int? skip)
    {
        var takeSkip = new SanitizedTakeSkip(take, skip);
        return query.Skip(takeSkip.Skip).Take(takeSkip.Take);
    }

    private static LeaderboardListEntry ToListItem(ReplayEntity entity) => new(
        entity.PlayerId,
        entity.Player.Name,
        entity.LevelId,
        entity.Level.Name,
        entity.GameRevision,
        entity.TimeInMilliseconds,
        entity.Id);
}