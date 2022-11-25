using Replay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util.EnumeratorExtensions;

public class GamemodeRace : MonoBehaviour
{
    public const float MaxAllowedReplayTime = 300f;

    [SerializeField] bool disableLauncher;
    [SerializeField] float countdownTime;
    [SerializeField] float countdownDelay;
    [SerializeField] float postFinishDelay;
    [SerializeField] PlayerTrigger[] finishTriggers;
    [SerializeField, Tooltip("Prevents replay uploads and returning to HUB in the editor")] bool devMode;
    [SerializeField] PlayerData uiData;

    private Replayable replayable;
    //private ReplayEventList replayTimerEvent;

    private ReplayDownload activeReplay;

    private SatriProtoPlayer player;
    private SatriProtoPlayerLauncher playerLauncher;

    private float timeUntilUnlock;
    private double timerStart;
    private double timerEnd;

    private bool DevModeActive => devMode && Application.isEditor;

    public void SetReplay(ReplayDownload replay)
    {
        activeReplay = replay;
    }

    void Start()
    {
        replayable = GetComponent<Replayable>();
        player = FindObjectOfType<SatriProtoPlayer>();
        playerLauncher = player.GetComponent<SatriProtoPlayerLauncher>();

        var levelBounds = FindObjectOfType<LevelBounds>();
        Debug.Assert(levelBounds != null, "LevelBounds not found in scene");
        if (replayable.ShouldRecord)
            levelBounds.onPlayerOutOfBounds.AddListener(OnPlayerOutOfBounds);

        foreach (var trigger in finishTriggers)
            trigger.onTriggerEnter.AddListener(OnFinishTriggerEntered);

        uiData.levelNumberText = SceneBase.ActiveSceneName;
        uiData.levelTimerText = "";

        if (replayable.ShouldRecord)
        {
            player.SetLocks(true, true);
            playerLauncher.IsEnabled = false;
        }

        timeUntilUnlock = countdownTime + countdownDelay;                
    }

    void FixedUpdate()
    {
        // countdown
        if (timeUntilUnlock > 0f)
        {
            float prevTime = timeUntilUnlock;
            float newTime = prevTime - Time.fixedDeltaTime;

            // todo: countdown sounnd effect

            timeUntilUnlock = newTime;
            SetTimerDisplay(Mathf.Min(timeUntilUnlock, countdownTime));

            if (timeUntilUnlock <= 0)
            {
                // start
                timerStart = Time.fixedTimeAsDouble;
                player.SetLocks(false, false);
                playerLauncher.IsEnabled = !disableLauncher;
            }
            else if (timeUntilUnlock <= countdownTime)
            {
                // unlock aim
                player.SetLocks(true, false);
            }
        }

        // race
        if (timeUntilUnlock <= 0f)
            UpdateTimerDisplay();
    }

    private void SetTimerDisplay(double time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        string timeText = string.Format("{0:D2}:{1:D2}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);        
        uiData.levelTimerText = timeText;
    }
    private void UpdateTimerDisplay()
    {
        double endTime = timerEnd > 0 ? timerEnd : Time.fixedTimeAsDouble;
        double raceTime = endTime - timerStart;
        if (activeReplay != null)
            raceTime = Math.Min(raceTime, activeReplay.timeInMilliseconds / 1000.0);
        SetTimerDisplay(raceTime);
    }

    private void OnPlayerOutOfBounds(GameObject playerGo, double fixedTime)
    {
        Debug.Assert(replayable.ShouldRecord);
        SceneBase.ReloadScene();
    }

    private void OnFinishTriggerEntered(GameObject playerGo, double fixedTime)
    {
        timerEnd = fixedTime;
        UpdateTimerDisplay();

        if (!DevModeActive)
        {
            var replayData = SceneBase.Current.ReplaySystem.Data.ToJson();
            StartCoroutine(CoUploadReplayAndExit(replayData));
        }

        player.SetLocks(true, false);
        playerLauncher.IsEnabled = false;
    }

    private IEnumerator CoUploadReplayAndExit(string replayData)
    {
        yield return null;

        float timeS = (float)(timerEnd - timerStart);
        int timeMs = Mathf.CeilToInt(timeS * 1000);
        string levelName = SceneBase.ActiveSceneName.ToLowerInvariant();

        if (timeS > MaxAllowedReplayTime)
        {
            Debug.LogWarning("Replay is longer than maximum storable limit");
        }
        else
        {
            yield return LeaderboardClient.GetClient()
                .CreateReplay(
                    replay => {
                        Debug.Log($"Replay {replay.id} uploaded to server");
                    },
                    timeMs, levelName, replayData)
                .OnException(e => {
                    Debug.LogWarning($"Replay upload error: {e.Message}");
                });
        }

        yield return new WaitForSeconds(postFinishDelay);

        GamemodeHub.SetRaceLastTime(levelName, timeMs);
        GamemodeHub.ReturnFromRace();
    }
}