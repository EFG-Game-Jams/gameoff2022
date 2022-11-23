using Game.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Game.Server.Controllers;

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(Policies.HostOnly)]
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

    // TODO redirect to itch page / local unity hosting 
    [HttpGet]
    [Route("/login-callback")]
    public IActionResult LoginCallback() => View();
}