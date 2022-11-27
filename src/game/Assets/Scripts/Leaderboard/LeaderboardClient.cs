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
    private string playerName = null;
    private int? playerId = null;
    private string sessionSecret = null;
    private bool? leaderboardEnabledByUser = null;
    private bool isServerHealthy = false;

    private int offlineId = 1;
    private readonly List<OfflineReplay> offlineReplays = new List<OfflineReplay>();

    private LeaderboardClient()
    {
        baseApiUrl = $"{LeaderboardConfig.LeaderboardUrl}/api/game/{LeaderboardConfig.GameRevision}";
    }

    public static LeaderboardClient GetClient()
    {
        if (leaderboardClient == null)
        {
            leaderboardClient = new LeaderboardClient();
            if (!Application.isEditor)
            {
                var leaderboardDisabled = WebFunctions.GetLeaderboardsDisabled();
                var leaderboardEnabled = WebFunctions.GetLeaderboardsEnabled();

                if (leaderboardDisabled)
                {
                    leaderboardClient.leaderboardEnabledByUser = false;
                }
                else if (leaderboardEnabled)
                {
                    leaderboardClient.leaderboardEnabledByUser = true;
                }
            }
        }

        return leaderboardClient;
    }

    public static void DiscardExistingClient()
    {
        leaderboardClient = null;
    }

    public bool IsLeaderboardEnabledByUser => leaderboardEnabledByUser == true;
    public bool IsLeaderboardDisabledByUser => leaderboardEnabledByUser == false;
    public bool IsServerHealthy => isServerHealthy;

    public void ResetLeaderboardEnabledUserChoice()
    {
        if (!Application.isEditor)
        {
            leaderboardEnabledByUser = null;
            WebFunctions.UnsetPersistedLeaderboardsEnabled();
        }
        else
        {
            Debug.LogWarning("Cannot reset offline choice for Unity Editor");
        }
    }

    public bool IsOffline => state == ClientState.Offline;

    public string PlayerName => playerName ?? "AnonymousRocket";
    public int PlayerId => playerId ?? 0;

    public IEnumerator ConnectAsEditor(string secret, Action<LeaderboardClient> callback)
    {
        Debug.Log("Initializing leaderboard client for Unity editor");

        if (!isServerHealthy)
        {
            Debug.Log("Server health is bad or has not been checked yet, going offline.");
            DisableOnlineLeaderboard();
            callback?.Invoke(this);
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(secret))
        {
            yield return TryRecoverExistingSession(secret);
            if (state == ClientState.Online)
            {
                callback?.Invoke(this);
                yield break; // OK
            }
        }

        DisableOnlineLeaderboard();
        callback?.Invoke(this);
    }

    // TODO Support offline mode
    // This should be called whenever the player has agreed to use leaderboards
    // This may cause a redirect event, i.e. nuke your current state
    public IEnumerator Connect(Action<LeaderboardClient> callback)
    {
        Debug.Log("Initializing leaderboard client");

        if (state != ClientState.None)
        {
            Debug.Log("Client is already initialized, aborting.");
            callback?.Invoke(this);
            yield break;
        }
        if (isServerHealthy == false)
        {
            Debug.Log("Server health is bad or has not been checked yet, going offline.");
            DisableOnlineLeaderboard();
            callback?.Invoke(this);
            yield break;
        }

        WebFunctions.PersistLeaderboardsEnabled(true);

        var sessionSecret = WebFunctions.ExtractSessionGuidFromFragment();
        if (sessionSecret != null)
        {
            yield return TryRecoverExistingSession(sessionSecret);
            if (state == ClientState.Online)
            {
                callback?.Invoke(this);
                yield break; // OK
            }
            else
            {
                Debug.Log("Received session UID from fragment is invalid, going offline.");
                // TODO Notify the player of this failure?
                DisableOnlineLeaderboard();
                callback?.Invoke(this);
                yield break;
            }
        }

        sessionSecret = WebFunctions.GetLeaderboardSessionGuid();
        if (sessionSecret != null)
        {
            yield return TryRecoverExistingSession(sessionSecret);
            if (state == ClientState.Online)
            {
                callback?.Invoke(this);
                yield break; // OK
            }
        }

        // New player or session expired
        WebFunctions.RedirectToItchAuthorizationPage(LeaderboardConfig.LeaderboardUrl);
    }

    public void DisableOnlineLeaderboard()
    {
        state = ClientState.Offline;
        playerId = null;
        sessionSecret = null;
        leaderboardEnabledByUser = false;

        if (!Application.isEditor)
        {
            WebFunctions.PersistLeaderboardsEnabled(false);
        }
    }

    private IEnumerator TryRecoverExistingSession(string sessionSecret)
    {
        Debug.Log($"Attempting session recovery for {sessionSecret}");
        yield return GetSessionDetails((details) =>
        {
            Debug.Log($"Session recovery for {sessionSecret} was succesful");

            this.sessionSecret = sessionSecret;
            playerName = details.playerName;
            playerId = details.playerId;
            state = ClientState.Online;
            leaderboardEnabledByUser = true;

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
            callback.Invoke(GetOfflineLeaderboardlistFor(levelName));
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
            callback.Invoke(GetOfflineLeaderboardlistFor(levelName));
            yield break;
        }

        AssertSession();

        yield return Get(callback, $"session/{sessionSecret}/leaderboard/neighbours?take={take}&skip={skip}&levelName={levelName}");
    }

    private LeaderboardList GetOfflineLeaderboardlistFor(string levelName)
    {
        var replay = offlineReplays.Find(r => r.levelName == levelName);
        if (replay == null)
        {
            return new LeaderboardList
            {
                items = Array.Empty<LeaderboardListEntry>(),
                totalCount = 0
            };
        }
        else
        {
            return new LeaderboardList
            {
                items = new[]
                {
                    OfflineReplayToLeaderboardListEntry(replay)
                },
                totalCount = 1
            };
        }
    }

    /// <returns>All known leaderboard entries for the player (non paged)</returns>
    public IEnumerator GetPersonalRecords(Action<LeaderboardList> callback)
    {
        if (IsOffline)
        {
            callback.Invoke(new LeaderboardList
            {
                items = offlineReplays
                    .Select(OfflineReplayToLeaderboardListEntry)
                    .ToArray(),
                totalCount = offlineReplays.Count
            });
            yield break;
        }

        AssertSession();

        yield return Get(callback, $"session/{sessionSecret}/leaderboard/personal");
    }
    #endregion

    private LeaderboardListEntry OfflineReplayToLeaderboardListEntry(OfflineReplay offlineReplay)
    {
        return new LeaderboardListEntry
        {
            gameRevision = LeaderboardConfig.GameRevision,
            levelId = -1,
            levelName = offlineReplay.levelName,
            playerId = PlayerId,
            playerName = PlayerName,
            rank = 1,
            replayId = offlineReplay.replayId,
            timeInMilliseconds = offlineReplay.timeInMilliseconds
        };
    }

    #region Replays
    /// <returns>The ID of the created replay</returns>
    public IEnumerator CreateReplay(Action<CreatedReplay> callback, int timeInMilliseconds, string levelName, string data)
    {
        AssertValidLevelName(levelName);

        if (IsOffline)
        {
            var replay = offlineReplays.Find(r => r.levelName == levelName);
            if (replay == null)
            {
                replay = new OfflineReplay
                {
                    replayId = ++offlineId,
                    data = data,
                    levelId = -1,
                    levelName = levelName,
                    playerId = PlayerId,
                    playerName = PlayerName,
                    timeInMilliseconds = timeInMilliseconds,
                };
                offlineReplays.Add(replay);
            }
            else if (timeInMilliseconds < replay.timeInMilliseconds)
            {
                replay.timeInMilliseconds = timeInMilliseconds;
                replay.data = data;
            }

            callback.Invoke(new CreatedReplay
            {
                id = replay.replayId
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
            var replay = offlineReplays.Find(r => r.replayId == replayId);
            if (replay == null)
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
    public IEnumerator CheckServerHealth(Action<bool> callback)
    {
        var request = UnityWebRequest.Get($"{LeaderboardConfig.LeaderboardUrl}/health");
        request.timeout = 1;
        yield return request.SendWebRequest();

        isServerHealthy = request.responseCode == 200;
        callback?.Invoke(isServerHealthy);
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
                if (request.responseCode >= 200 && request.responseCode < 300)
                {
                    var model = JsonUtility.FromJson<T>(request.downloadHandler.text);
                    if (model == null)
                    {
                        throw new InvalidOperationException($"Response model was null for {request.downloadHandler.text}");
                    }
                    callback.Invoke(model);
                }
                else
                {
                    Debug.Log($"GET request to {path} failed with status code {request.responseCode}");
                }
                break;
            default:
                throw new InvalidOperationException("Unknown unity web request result type");
        }
    }

    private IEnumerator Post<T>(Action<T> callback, string path, object model)
    {
        // The UnityWebRequest.Post method is borked, use PUT and correct it afterwards
        var request = UnityWebRequest.Put($"{baseApiUrl}/{path}", JsonUtility.ToJson(model));
        request.method = "POST";
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
                if (request.responseCode >= 200 && request.responseCode < 300)
                {
                    var responseModel = JsonUtility.FromJson<T>(request.downloadHandler.text);
                    if (responseModel == null)
                    {
                        throw new InvalidOperationException($"Response model was null for {request.downloadHandler.text}");
                    }
                    callback.Invoke(responseModel);
                }
                else
                {
                    Debug.Log($"POST request to {path} failed with status code {request.responseCode}");
                }
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
