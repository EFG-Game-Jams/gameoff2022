using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HubRaceEntranceScreenLeaderboardWorld : HubRaceEntranceScreenLeaderboard
{
    [SerializeField] TMPro.TextMeshProUGUI textTitle;
    [SerializeField] TMPro.TextMeshProUGUI textStatus;

    private string levelName;
    private bool refreshing;
    private bool downloadingReplay;

    private void OnValidate()
    {
        textTitle.text = "Global Leaderboard";

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
        DisplayLeaderboard(demoList);
    }

    protected override void Start()
    {
        base.Start();

        foreach (var record in records)
            record.GetComponent<RocketButtonBase>().onTrigger.AddListener(_ => RequestReplay(record));

        SetStatus("");
        ClearLeaderboard();
    }

    public override void Refresh(string levelName, bool force)
    {
        if (this.levelName == levelName && !force)
            return; // already refreshed

        this.levelName = levelName;

        var client = LeaderboardClient.GetClient();
        if (client.IsOffline)
        {
            DisplayOffline();
        }
        else if (!refreshing)
        {
            refreshing = true;
            DisplayRefreshing();
            StartCoroutine(CoRefresh(client));
        }
    }

    private void RequestReplay(LeaderboardRecord record)
    {
        if (downloadingReplay)
        {
            return;
        }
        else
        {
            SetStatus("Downloading replay...");
            StartCoroutine(CoRequestReplay(LeaderboardClient.GetClient(), record));
        }
    }

    private void SetStatus(string statusOrNull)
    {
        textStatus.gameObject.SetActive(statusOrNull != null);
        if (statusOrNull != null)
            textStatus.text = statusOrNull;
    }

    private void DisplayOffline()
    {
        SetStatus("Offline - enable at hub panel");
        ClearLeaderboard();
    }
    private void DisplayRefreshing()
    {
        SetStatus("Fetching leaderboard...");
    }
    private void DisplayRefreshFailed()
    {
        SetStatus("Unable to fetch leaderboard");
        ClearLeaderboard();
    }
    private void ClearLeaderboard()
    {
        foreach (var record in records)
            record.gameObject.SetActive(false);
    }
    private void DisplayLeaderboard(LeaderboardList data)
    {
        SetStatus("");
        for (int i = 0; i < records.Length; ++i)
        {
            int rank = i + 1;
            if (i < data.items.Length)
            {
                records[i].SetRecord(data.items[i], rank, rank == 3);
                records[i].gameObject.SetActive(true);
            }
            else
            {
                records[i].gameObject.SetActive(false);
            }

        }
    }

    private IEnumerator CoRefresh(LeaderboardClient client)
    {
        IEnumerator request = client.GetGlobalLeaderboard(DisplayLeaderboard, levelName, take: 10);

        IEnumerator safeRequest = RunThrowingIterator(request, e =>
        {
            if (e != null)
            {
                Debug.LogWarning($"Leaderboard refresh error: {e.Message}");
                DisplayRefreshFailed();
            }
        });

        yield return safeRequest;
        refreshing = false;
    }


    private IEnumerator CoRequestReplay(LeaderboardClient client, LeaderboardRecord record)
    {
        IEnumerator request = client.DownloadReplay(BeginReplay, record.Record.replayId);

        IEnumerator safeRequest = RunThrowingIterator(request, e =>
        {
            if (e != null)
            {
                Debug.LogWarning($"Replay download error: {e.Message}");
                SetStatus("Replay download failed");
            }
        });

        yield return safeRequest;
        downloadingReplay = false;
    }

    private SatriProtoPlayer.TransformSnapshot GetPlayerReturnSnapshot()
    {
        SatriProtoPlayer player = FindObjectOfType<SatriProtoPlayer>();
        var snapshot = player.GetTransform();
        snapshot.cameraPitch = 0;
        snapshot.position += Vector3.up * 5;
        return snapshot;
    }

    private void BeginReplay(ReplayDownload replay)
    {
        GamemodeHub.BeginReplay(replay, GetPlayerReturnSnapshot());
    }
}
