using Game.Server.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Game.Server.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class AuthenticationController : Controller
{
    private readonly IItchService itchService;

    public AuthenticationController(IItchService itchService)
    {
        this.itchService = itchService;
    }

    [HttpGet]
    [Route("/login")]
    public IActionResult Login() => Redirect(itchService.GetLoginUrl());

    [HttpGet]
    [Route("/login-callback")]
    public IActionResult LoginCallback() => View();

    // TODO Remove this controller method
    // Test API token: 7jN0mLVnsxLIDqQn0FyUGZFFyyLZRQNpd3xa0AMc
    [Route("/login-debug")]
    public async Task<IActionResult> LoginDebug([FromQuery] string accessToken)
    {
        return View("LoginDebug", await itchService.CheckCredentials(accessToken));
    }
}