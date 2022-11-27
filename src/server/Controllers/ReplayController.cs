using Game.Server.Models.Replay;
using Game.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Game.Server.Controllers;

// https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html
[AllowAnonymous]
[ApiController]
[EnableCors]
public class ReplayController : ControllerBase
{
    private readonly GameService gameService;

    public ReplayController(GameService gameService)
    {
        this.gameService = gameService;
    }

    [HttpPost]
    [Route("/api/game/{revision}/session/{sessionSecret}/replay")]
    public async Task<ActionResult<ReplayCreatedResponse>> CreateReplay(
        [FromRoute] uint revision,
        [FromRoute] Guid sessionSecret,
        [FromBody] CreateReplayRequest request)
    {
        var player = await gameService.TryGetPlayerIdFor(sessionSecret);
        if (!player.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            return new ReplayCreatedResponse(await gameService.SaveReplay(
                sessionSecret,
                revision,
                (uint)request.TimeInMilliseconds.Value,
                request.LevelName,
                request.Data));
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }
    }

    [HttpGet]
    [Route("/api/game/{revision}/session/{sessionSecret}/replay/{replayId}")]
    public async Task<ActionResult<ReplayResponse>> DownloadReplay(
        [FromRoute] uint revision,
        [FromRoute] Guid sessionSecret,
        [FromRoute] int replayId)
    {
        var player = await gameService.TryGetPlayerIdFor(sessionSecret);
        if (!player.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            return await gameService.DownloadReplay(replayId);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}