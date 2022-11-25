using System.Collections;
using UnityEngine;
using Util.EnumeratorExtensions;

public class HubRaceEntranceScreenLeaderboardNear : HubRaceEntranceScreenLeaderboard
{
    private void OnValidate()
    {
        textTitle.text = "Editor Leaderboard";
        LeaderboardList demoList = new LeaderboardList
        {
            items = new LeaderboardListEntry[]
            {
                new LeaderboardListEntry { playerName = "Rocket Chad", timeInMilliseconds = 1 },
                new LeaderboardListEntry { playerName = "Rocket Chad's little brother", timeInMilliseconds = 73468 },
                new LeaderboardListEntry { playerName = "The local player", timeInMilliseconds = 98654 },
                new LeaderboardListEntry { playerName = "Somegirl864", timeInMilliseconds = 150123 },
                new LeaderboardListEntry { playerName = "XxSomeguy73xX", timeInMilliseconds = 290456 },
                new LeaderboardListEntry { playerName = "Rocket Sloth", timeInMilliseconds = 790915 }
            }
        };
        for (int i = 0; i < demoList.items.Length; ++i)
        {
            demoList.items[i].rank = i * i * 9345 + 1;
            demoList.items[i].playerId = i;
        }
        DisplayLeaderboard(demoList);
    }

    public override void Refresh(string levelName, bool force = false)
    {
        if (LeaderboardClient.GetClient().IsOffline)
            textTitle.text = "Offline Ranking";
        else
            textTitle.text = "Global Ranking";

        base.Refresh(levelName, force);
    }
    protected override IEnumerator GetRefreshRequest(LeaderboardClient client)
    {
        return client.GetLeaderboardNeighbours(DisplayLeaderboard, levelName, take: 10);
    }

    public string GetLocalPlayerTime()
    {
        if (LeaderboardClient.GetClient().IsOffline)
            return "offline";

        int playerId = LeaderboardClient.GetClient().PlayerId;
        foreach (var record in records)
        {
            if (!record.gameObject.activeSelf)
                break;
            if (record.Record.playerId == playerId)
                return GamemodeHub.FormatTime(record.Record.timeInMilliseconds / 1000.0);
        }

        return "N/A";
    }
}
