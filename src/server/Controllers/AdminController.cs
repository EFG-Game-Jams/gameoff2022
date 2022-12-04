using Game.Server.Models.Admin;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(Policies.HostOnly)]
public class AdminController : Controller
{
    private readonly ReplayDatabase replayDatabase;

    public AdminController(ReplayDatabase replayDatabase)
    {
        this.replayDatabase = replayDatabase;
    }

    [Route("/")]
    public IActionResult Index() => View();

    [Route("/statistics")]
    public async Task<IActionResult> Statistics()
    {
        var playerCount = await replayDatabase.Players.CountAsync();
        var sessionCount = await replayDatabase.Sessions.CountAsync();
        var levels = await replayDatabase.Levels
            .Select(l => new { l.Name, l.Id, RecordCount = l.Replays.Count() })
            .OrderBy(l => l.Name)
            .ToArrayAsync();

        var levelRecords = new List<LevelStatistics>();
        foreach (var level in levels)
        {
            var records = await replayDatabase.Replays
                .Where(r => r.LevelId == level.Id)
                .OrderBy(r => r.TimeInMilliseconds)
                .Select(r => new { r.Player.Name, r.TimeInMilliseconds })
                .Take(10)
                .ToArrayAsync();

            levelRecords.Add(new LevelStatistics(
                level.Name,
                level.RecordCount,
                records
                    .Select(r => new LevelStatisticsRecord(r.Name, (int)r.TimeInMilliseconds))
                    .ToArray()));
        }

        return View(new StatisticsResponse(playerCount, sessionCount, levelRecords.ToArray()));
    }
}