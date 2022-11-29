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
    [SerializeField] AudioSource countdownSfx;

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

    public void OnInputExit(UnityEngine.InputSystem.InputValue value)
    {
        if (timeUntilUnlock > countdownTime)
            return; // starting delay

        if (activeReplay != null || timeUntilUnlock > 0f)
            GamemodeHub.ReturnFromRace(); // exit
        else
            SceneBase.ReloadScene(); // reset
    }

    void Start()
    {
        replayable = GetComponent<Replayable>();
        player = FindObjectOfType<SatriProtoPlayer>();
        playerLauncher = player.GetComponent<SatriProtoPlayerLauncher>();

        Cursor.lockState = CursorLockMode.Locked;

        var levelBounds = FindObjectOfType<LevelBounds>();
        Debug.Assert(levelBounds != null, "LevelBounds not found in scene");
        if (replayable.ShouldRecord)
            levelBounds.onPlayerOutOfBounds.AddListener(OnPlayerOutOfBounds);

        foreach (var trigger in finishTriggers)
            trigger.onTriggerEnter.AddListener(OnFinishTriggerEntered);

        timeUntilUnlock = countdownTime + countdownDelay;
        //uiData.levelNumberText = SceneBase.ActiveSceneName;
        uiData.levelTimerText = "";
        UpdateLevelTextDisplay();

        if (replayable.ShouldRecord)
        {
            player.SetLocks(true, true);
            playerLauncher.IsEnabled = false;
        }

        if (activeReplay != null)
        {
            Debug.Assert(replayable.ShouldPlayback);
            //uiData.levelNumberText += $" - replay\n<size=50%>player: {activeReplay.playerName}</size>";
            StartCoroutine(CoReplay());
        }
    }    

    void FixedUpdate()
    {
        // countdown
        if (timeUntilUnlock > 0f)
        {
            float prevTime = timeUntilUnlock;
            float newTime = prevTime - Time.fixedDeltaTime;

            if (Mathf.Floor(prevTime) != Mathf.Floor(newTime))
            {
                if (newTime <= 0f)
                    countdownSfx.pitch = 1.31f;
                countdownSfx.Play();
            }                

            timeUntilUnlock = newTime;
            SetTimerDisplay(Mathf.Min(timeUntilUnlock, countdownTime));

            if (prevTime > countdownTime && newTime <= countdownTime)
                UpdateLevelTextDisplay();

            if (timeUntilUnlock <= 0)
            {
                // start
                timerStart = Time.fixedTimeAsDouble;
                player.SetLocks(false, false);
                playerLauncher.IsEnabled = !disableLauncher;
                UpdateLevelTextDisplay();
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

    private void UpdateLevelTextDisplay()
    {
        string text = SceneBase.ActiveSceneName;
        if (activeReplay != null)
            text += $" - replay\n<size=50%>player - {activeReplay.playerName}</size>";
        if (timeUntilUnlock <= countdownTime)
            text += $"<size=25%>\nPress <i>Backspace</i> to {(timeUntilUnlock <= 0f && activeReplay == null ? "Restart" : "Return To HUB")}</size>";
        uiData.levelNumberText = text;
    }

    private void SetTimerDisplay(double time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        string timeText = string.Format("{0:D2}:{1:D2}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);        
        uiData.levelTimerText = timeText;
    }
    private void UpdateTimerDisplay()
    {        
        SetTimerDisplay(GetRaceTime());
    }
    private double GetRaceTime()
    {
        if (timeUntilUnlock > 0)
            return 0;
        double endTime = timerEnd > 0 ? timerEnd : Time.fixedTimeAsDouble;
        double raceTime = endTime - timerStart;
        if (activeReplay != null)
            raceTime = Math.Min(raceTime, activeReplay.timeInMilliseconds / 1000.0);
        return raceTime;
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
            StartCoroutine(CoUploadReplayAndExit());

        player.SetLocks(true, false);
        playerLauncher.IsEnabled = false;
    }

    private IEnumerator CoUploadReplayAndExit()
    {
        yield return new WaitForSeconds(postFinishDelay);

        float timeS = (float)(timerEnd - timerStart);
        int timeMs = Mathf.CeilToInt(timeS * 1000);
        string levelName = SceneBase.ActiveSceneName.ToLowerInvariant();

        if (timeS > MaxAllowedReplayTime)
        {
            Debug.LogWarning("Replay is longer than maximum storable limit");
        }
        else
        {
            string replayData = SceneBase.Current.ReplaySystem.Data.ToJson();

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

        GamemodeHub.SetRaceLastTime(levelName, timeMs);
        GamemodeHub.ReturnFromRace();
    }

    private IEnumerator CoReplay()
    {
        double raceTime = activeReplay.timeInMilliseconds / 1000.0;
        while (GetRaceTime() < raceTime)
            yield return null;
        yield return new WaitForSeconds(postFinishDelay);
        GamemodeHub.ReturnFromRace();
    }
}
