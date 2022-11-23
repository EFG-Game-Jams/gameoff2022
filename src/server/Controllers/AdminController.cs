using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Game.Server.Controllers;

// TODO GitHub authentication
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(Policies.HostOnly)]
public class AdminController : Controller
{
    [Route("/")]
    public IActionResult Index() => View();
}