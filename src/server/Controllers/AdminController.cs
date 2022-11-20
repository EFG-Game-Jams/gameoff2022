using Microsoft.AspNetCore.Mvc;

namespace Game.Server.Controllers;

// TODO GitHub authentication
[ApiExplorerSettings(IgnoreApi = true)]
public class AdminController : Controller
{
    [Route("/")]
    public IActionResult Index() => View();
}