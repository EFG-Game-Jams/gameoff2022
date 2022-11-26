using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summmary>Refer to the static GetClient() method use this singleton</summary>
public class LeaderboardClient
{
    private static LeaderboardClient leaderboardClient = null;

    private readonly string baseApiUrl;
    private ClientState state = ClientState.None;
    private string playerName;
    private int? playerId;
    private string sessionSecret;
    private bool isServerHealthy;

    private int offlineId = 1;
    private readonly List<OfflineLevel> offlineLevels = new List<OfflineLevel>();
    private readonly List<LeaderboardListEntry> offlineLeaderboard = new List<LeaderboardListEntry>();
    private readonly List<ReplayDownload> offlineReplays = new List<ReplayDownload>();

    private LeaderboardClient()
    {
        baseApiUrl = $"{LeaderboardConfig.LeaderboardUrl}/api/game/{LeaderboardConfig.GameRevision}";
    }

    public static LeaderboardClient GetClient()
    {
        if (leaderboardClient == null)
        {
            leaderboardClient = new LeaderboardClient();
            if (Application.isEditor || WebFunctions.GetLeaderboardsDisabled())
            {
                leaderboardClient.state = ClientState.Offline;
            }
        }

        return leaderboardClient;
    }

    public void DisableOnlineLeaderboard()
    {
        state = ClientState.Offline;
        playerId = null;
        sessionSecret = null;

        if (!Application.isEditor)
        {
            WebFunctions.PersistLeaderboardsEnabled(false);
        }
    }

    public void EnableOnlineLeaderboard()
    {
        if (!Application.isEditor)
        {
            WebFunctions.PersistLeaderboardsEnabled(true);
        }
    }

    public bool IsOffline => state == ClientState.Offline;

    public string PlayerName => playerName ?? "AnonymousRocket";
    public int PlayerId => playerId ?? 0;

    public IEnumerator ConnectAsEditor(string secret)
    {
        yield return CheckServerHealth();

        if (!isServerHealthy)
        {
            DisableOnlineLeaderboard();
            yield break;
        }

        if (secret != null)
        {
            yield return TryRecoverExistingSession(secret);
            if (state == ClientState.Online)
            {
                yield break; // OK
            }
        }

        DisableOnlineLeaderboard();
    }

    // TODO Support offline mode
    // This should be called whenever the player has agreed to use leaderboards
    // This may cause a redirect event, i.e. nuke your current state
    public IEnumerator Connect()
    {
        if (state != ClientState.None)
        {
            yield break;
        }

        yield return CheckServerHealth();

        if (!isServerHealthy)
        {
            DisableOnlineLeaderboard();
            yield break;
        }

        var sessionSecret = WebFunctions.ExtractSessionGuidFromFragment();
        if (sessionSecret != null)
        {
            yield return TryRecoverExistingSession(sessionSecret);
            if (state == ClientState.Online)
            {
                yield break; // OK
            }
            else
            {
                // TODO Notify the player of this failure?
                DisableOnlineLeaderboard();
                yield break;
            }
        }

        sessionSecret = WebFunctions.GetLeaderboardSessionGuid();
        if (sessionSecret != null)
        {
            yield return TryRecoverExistingSession(sessionSecret);
            if (state == ClientState.Online)
            {
                yield break; // OK
            }
        }

        // New player or session expired
        WebFunctions.RedirectToItchAuthorizationPage(LeaderboardConfig.LeaderboardUrl);
    }

    private IEnumerator TryRecoverExistingSession(string sessionSecret)
    {
        yield return GetSessionDetails((details) =>
        {
            this.sessionSecret = sessionSecret;
            playerName = details.playerName;
            playerId = details.playerId;
            state = ClientState.Online;

            if (!Application.isEditor)
            {
                WebFunctions.PersistLeaderboardSessionGuid(sessionSecret);
                WebFunctions.PersistLeaderboardsEnabled(true);
            }

            Debug.Log($"Succesfully connected to the leaderboard server as {playerName}");
        }, sessionSecret);
    }

    #region Leaderboards
    /// <returns>A paged list of global leaderboard entries.</returns>
    public IEnumerator GetGlobalLeaderboard(Action<LeaderboardList> callback, string levelName, int take = 10, int skip = 0)
    {
        AssertValidLevelName(levelName);

        if (IsOffline)
        {
            var level = offlineLevels.Find(l => l.name == levelName);
            if (level == null)
            {
                callback.Invoke(new LeaderboardList
                {
                    items = Array.Empty<LeaderboardListEntry>(),
                    totalCount = 0
                });
            }
            else
            {
                var items = offlineLeaderboard
                    .Where(ol => ol.levelId == level.id)
                    .ToArray();
                callback.Invoke(new LeaderboardList
                {
                    items = items,
                    totalCount = items.Length
                });
            }
            yield break;
        }

        AssertSession();

        yield return Get(callback, $"session/{sessionSecret}/leaderboard?take={take}&skip={skip}&levelName={levelName}");
    }

    /// <returns>A paged list of leaderboard entries around the player leaderboard entry. If the player has no record for the specified level an empty list is returned.</returns>
    public IEnumerator GetLeaderboardNeighbours(Action<LeaderboardList> callback, string levelName, int take = 10, int skip = 0)
    {
        AssertValidLevelName(levelName);

        if (IsOffline)
        {
            var level = offlineLevels.Find(l => l.name == levelName);
            if (level == null)
            {
                callback.Invoke(new LeaderboardList
                {
                    items = Array.Empty<LeaderboardListEntry>(),
                    totalCount = 0
                });
            }
            else
            {
                var items = offlineLeaderboard
                    .Where(ol => ol.levelId == level.id)
                    .ToArray();
                callback.Invoke(new LeaderboardList
                {
                    items = items,
                    totalCount = items.Length
                });
            }
            yield break;
        }

        AssertSession();

        yield return Get(callback, $"session/{sessionSecret}/leaderboard/neighbours?take={take}&skip={skip}&levelName={levelName}");
    }

    /// <returns>All known leaderboard entries for the player (non paged)</returns>
    public IEnumerator GetPersonalRecords(Action<LeaderboardList> callback)
    {
        if (IsOffline)
        {
            callback.Invoke(new LeaderboardList
            {
                items = offlineLeaderboard.ToArray(),
                totalCount = offlineLeaderboard.Count
            });
            yield break;
        }

        AssertSession();

        yield return Get(callback, $"session/{sessionSecret}/leaderboard/personal");
    }
    #endregion

    #region Replays
    /// <returns>The ID of the created replay</returns>
    public IEnumerator CreateReplay(Action<CreatedReplay> callback, int timeInMilliseconds, string levelName, string data)
    {
        AssertValidLevelName(levelName);

        if (IsOffline)
        {
            var level = offlineLevels.Find(l => l.name == levelName);
            if (level == null)
            {
                level = new OfflineLevel
                {
                    id = offlineId++,
                    name = levelName
                };
                offlineLevels.Add(level);
            }

            var replay = offlineReplays.Find(r => r.levelId == level.id);
            if (replay == null)
            {
                replay = new ReplayDownload
                {
                    data = data,
                    levelId = level.id,
                    levelName = level.name,
                    playerId = PlayerId,
                    playerName = PlayerName,
                    timeInMilliseconds = timeInMilliseconds
                };
                offlineReplays.Add(replay);
            }
            else
            {
                replay.timeInMilliseconds = timeInMilliseconds;
                replay.data = data;
            }

            var record = offlineLeaderboard.Find(l => l.levelId == level.id);
            if (record == null)
            {
                record = new LeaderboardListEntry
                {
                    gameRevision = 0,
                    levelId = level.id,
                    levelName = level.name,
                    playerId = PlayerId,
                    playerName = PlayerName,
                    rank = 1,
                    replayId = offlineId++
                };
                offlineLeaderboard.Add(record);
            }

            callback.Invoke(new CreatedReplay
            {
                id = record.replayId
            });
            yield break;
        }

        AssertSession();

        yield return Post(
            callback,
            $"session/{sessionSecret}/replay",
            new CreateReplay
            {
                timeInMilliseconds = timeInMilliseconds,
                levelName = levelName,
                data = data
            });
    }

    public IEnumerator DownloadReplay(Action<ReplayDownload> callback, int replayId)
    {
        if (IsOffline)
        {
            var record = offlineLeaderboard.Find(r => r.replayId == replayId);
            if (record == null)
            {
                throw new InvalidOperationException("Requested replay has not been stored in the offline storage");
            }
            var replay = offlineReplays.Find(r => r.levelId == record.levelId);
            if (record == null)
            {
                throw new InvalidOperationException("Requested replay has not been stored in the offline storage");
            }

            callback.Invoke(replay);
            yield break;
        }

        AssertSession();

        yield return Get(callback, $"session/{sessionSecret}/replay/{replayId}");
    }
    #endregion

    #region Session
    private IEnumerator GetSessionDetails(Action<SessionDetails> callback, string sessionSecret)
    {
        yield return Get(callback, $"session/{sessionSecret}/details");
    }
    #endregion

    #region Health
    private IEnumerator CheckServerHealth()
    {
        var request = UnityWebRequest.Get($"{LeaderboardConfig.LeaderboardUrl}/health");
        request.timeout = 1;
        yield return request.SendWebRequest();

        isServerHealthy = request.responseCode == 200;
    }
    #endregion

    private static void AssertValidLevelName(string levelName)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(levelName, "^[a-z0-9-]+$"))
        {
            throw new InvalidOperationException($"Level names may only use the lower case ASCII alphabet and dashes (-). The level name {levelName} does not adhere to this.");
        }
    }

    private void AssertSession()
    {
        if (string.IsNullOrWhiteSpace(sessionSecret))
        {
            throw new InvalidOperationException($"Session secret cannot be null for this operation");
        }
    }

    private IEnumerator Get<T>(Action<T> callback, string path)
    {
        var request = UnityWebRequest.Get($"{baseApiUrl}/{path}");
        yield return request.SendWebRequest();

        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError($"{path} HTTP Error: {request.error}");
                break;
            case UnityWebRequest.Result.Success:
                var model = JsonUtility.FromJson<T>(request.downloadHandler.text);
                if (model == null)
                {
                    throw new InvalidOperationException($"Response model was null for {request.downloadHandler.text}");
                }
                callback.Invoke(model);
                break;
            default:
                throw new InvalidOperationException("Unknown unity web request result type");
        }
    }

    private IEnumerator Post<T>(Action<T> callback, string path, object model)
    {
        var request = UnityWebRequest.Post($"{baseApiUrl}/{path}", JsonUtility.ToJson(model));
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        switch (request.result)
        {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError($"{path} HTTP Error: {request.error}");
                break;
            case UnityWebRequest.Result.Success:
                var responseModel = JsonUtility.FromJson<T>(request.downloadHandler.text);
                if (responseModel == null)
                {
                    throw new InvalidOperationException($"Response model was null for {request.downloadHandler.text}");
                }
                callback.Invoke(responseModel);
                break;
            default:
                throw new InvalidOperationException("Unknown unity web request result type");
        }
    }

    enum ClientState
    {
        None,
        Offline,
        Online
    };
}
