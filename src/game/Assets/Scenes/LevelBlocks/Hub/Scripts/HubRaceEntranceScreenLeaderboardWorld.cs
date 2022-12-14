using System.Collections;
using UnityEngine;
using Util.EnumeratorExtensions;

public class HubRaceEntranceScreenLeaderboardWorld : HubRaceEntranceScreenLeaderboard
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
            demoList.items[i].rank = i + 1;
            demoList.items[i].playerId = i;
        }
        DisplayLeaderboard(demoList);
    }

    public override void Refresh(string levelName, bool force = false)
    {
        if (LeaderboardClient.GetClient().IsOffline)
            textTitle.text = "Offline Leaderboard";
        else
            textTitle.text = "Global Leaderboard";

        base.Refresh(levelName, force);
    }
    protected override IEnumerator GetRefreshRequest(LeaderboardClient client)
    {
        return client.GetGlobalLeaderboard(DisplayLeaderboard, levelName, take: 10);
    }
}
