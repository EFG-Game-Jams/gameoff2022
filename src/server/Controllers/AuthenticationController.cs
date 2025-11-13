using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Game.Server.Controllers;

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(Policies.HostOnly)]
public class AuthenticationController : Controller
{
    [HttpGet]
    [Route("/login")]
    public IActionResult Login() => View();

    // TODO redirect to itch page / local unity hosting
    [HttpGet]
    [Route("/login-callback")]
    public IActionResult LoginCallback() => View();
}
