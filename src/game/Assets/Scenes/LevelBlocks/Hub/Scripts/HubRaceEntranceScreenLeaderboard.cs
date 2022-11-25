using System.Collections;
using UnityEngine;
using Util.EnumeratorExtensions;

public abstract class HubRaceEntranceScreenLeaderboard : HubRaceEntranceScreen
{
    [SerializeField] protected TMPro.TextMeshProUGUI textTitle;
    [SerializeField] protected TMPro.TextMeshProUGUI textStatus;
    [SerializeField] protected LeaderboardRecord[] records;

    protected string levelName;
    protected bool refreshing;
    protected bool downloadingReplay;

    public UnityEngine.Events.UnityEvent onRefreshComplete;

    protected abstract IEnumerator GetRefreshRequest(LeaderboardClient client);

    protected override void Start()
    {
        base.Start();

        foreach (var record in records)
            record.GetComponent<RocketButtonBase>().onTrigger.AddListener(_ => RequestReplay(record));

        SetStatus("");
        ClearLeaderboard();
    }

    public virtual void Refresh(string levelName, bool force = false)
    {
        if (this.levelName == levelName && !force)
            return; // already refreshed

        this.levelName = levelName;

        var client = LeaderboardClient.GetClient();
        /*if (client.IsOffline)
        {
            DisplayOffline();
        }
        else*/ if (!refreshing)
        {
            refreshing = true;
            DisplayRefreshing();
            StartCoroutine(CoRefresh(client));
        }
    }
    protected IEnumerator CoRefresh(LeaderboardClient client)
    {
        yield return GetRefreshRequest(client)            
            .OnException(e =>
            {
                Debug.LogWarning($"Leaderboard refresh error: {e.Message}");
                DisplayRefreshFailed();
            });

        onRefreshComplete?.Invoke();
        refreshing = false;
    }

    protected void SetStatus(string statusOrNull)
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

    protected void ClearLeaderboard()
    {
        foreach (var record in records)
            record.gameObject.SetActive(false);
    }
    protected void DisplayLeaderboard(LeaderboardList data)
    {
        SetStatus("");

        int localPlayerId = Application.isPlaying ? LeaderboardClient.GetClient().PlayerId : 2;

        for (int i = 0; i < records.Length; ++i)
        {
            //int rank = i + 1;
            if (i < data.items.Length)
            {
                //records[i].SetRecord(data.items[i], rank, rank == 3);
                records[i].SetRecord(data.items[i], data.items[i].rank, data.items[i].playerId == localPlayerId);
                records[i].gameObject.SetActive(true);
            }
            else
            {
                records[i].gameObject.SetActive(false);
            }
        }
    }

    protected void RequestReplay(LeaderboardRecord record)
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

    protected IEnumerator CoRequestReplay(LeaderboardClient client, LeaderboardRecord record)
    {
        yield return client
            .DownloadReplay(BeginReplay, record.Record.replayId)
            .OnException(e =>
            {
                Debug.LogWarning($"Replay download error: {e.Message}");
                SetStatus("Replay download failed");
            });

        downloadingReplay = false;
    }

    protected SatriProtoPlayer.TransformSnapshot GetPlayerReturnSnapshot()
    {
        SatriProtoPlayer player = FindObjectOfType<SatriProtoPlayer>();
        var snapshot = player.GetTransform();
        snapshot.cameraPitch = 0;
        snapshot.position += Vector3.up * 5;
        return snapshot;
    }

    protected void BeginReplay(ReplayDownload replay)
    {
        GamemodeHub.BeginReplay(replay, GetPlayerReturnSnapshot());
    }
}
