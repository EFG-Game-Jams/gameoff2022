using Game.Server.Models.Session;
using Game.Server.Services;
using Game.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Game.Server.Controllers;

[AllowAnonymous]
[ApiController]
[EnableCors]
public class SessionController : ControllerBase
{
    private readonly IItchService itchService;
    private readonly GameService gameService;

    public SessionController(IItchService itchService, GameService gameService)
    {
        this.itchService = itchService;
        this.gameService = gameService;
    }

    [HttpGet]
    [Route("/api/game/{revision}/session/guid")]
    public ActionResult<SessionSecretResponse> GenerateSecret([FromRoute] uint revision) => new SessionSecretResponse(Guid.NewGuid().ToString());

    [HttpPost]
    [Route("/api/game/{revision}/session/{sessionSecret}/create/{accessToken}")]
    public async Task<ActionResult<CreateSessionResponse>> Register(
        [FromRoute] uint revision,
        [FromRoute] Guid sessionSecret,
        [FromRoute] string accessToken)
    {
        try
        {
            var itchProfile = await itchService.FetchProfile(accessToken);
            await gameService.CreateSession(sessionSecret, itchProfile.User.Username, itchProfile.User.Id);
            return new CreateSessionResponse(sessionSecret, itchProfile.User.Username);
        }
        catch (HttpRequestException)
        {
            return BadRequest();
        }
    }

    [HttpGet]
    [Route("/api/game/{revision}/session/{sessionSecret}/details")]
    public async Task<ActionResult<SessionDetailsResponse>> GetSessionDetails(
        [FromRoute] Guid sessionSecret)
    {
        try
        {
            var (Id, Name) = await gameService.GetPlayerFor(sessionSecret);
            return new SessionDetailsResponse(Id, Name);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}