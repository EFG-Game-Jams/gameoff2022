using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

public class GamemodeHub : MonoBehaviour
{
    [Header("References")]
    [SerializeField] SatriProtoPlayer player;
    [SerializeField] PlayerData uiData;

    [Header("Ground tutorial")]
    [SerializeField] PlayerTrigger gtTriggerStart;
    [SerializeField] PlayerTrigger gtTriggerFinish;
    [SerializeField] PlayerTrigger gtTriggerCancel;
    [SerializeField] TMPro.TextMeshProUGUI gtGuiLastTime;
    [SerializeField] TMPro.TextMeshProUGUI gtGuiBestTime;
   
    private enum TimerType
    {
        None,
        GroundTutorial,
    }
    private TimerType activeTimer;
    private double activeTimerStart;

    [System.Serializable]
    private struct PersistentData
    {
        public double groundTutorialBestTime;
    }
    private PersistentData persistentData;
    private const string PersistentDataName = "data_hub";

    private static Dictionary<string, int> raceLastTimes;
    public static void SetRaceLastTime(string name, int timeMs)
    {
        raceLastTimes ??= new Dictionary<string, int>();
        raceLastTimes[name.ToLowerInvariant()] = timeMs;
    }
    public static bool TryGetRaceLastTime(string name, out int timeMs)
    {
        timeMs = 0;
        return (raceLastTimes != null && raceLastTimes.TryGetValue(name.ToLowerInvariant(), out timeMs));
    }
    public static string GetRaceLastTimeString(string name)
    {
        if (TryGetRaceLastTime(name, out int timeMs))
            return FormatTime(timeMs / 1000.0);
        else
            return "N/A";
    }

    public void EventSetLauncherEnabled(bool enabled) => player.GetComponent<SatriProtoPlayerLauncher>().IsEnabled = enabled;
    public void EventFullyRegenLauncher() =>  player.GetComponent<SatriProtoPlayerLauncher>().RegenFully();

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        uiData.levelTimerText = "";
        uiData.levelTimerExtraInfo = "";
        uiData.levelNumberText = "";

        PersistentDataLoad();
        gtGuiLastTime.text = FormatMonoText("N/A");        

        SetupTriggers();

        if (playerReturnFromRaceSnapshot.HasValue)
        {
            // returning from race
            player.SetTransform(playerReturnFromRaceSnapshot.Value);
            playerReturnFromRaceSnapshot = null;
        }
        else
        {
            // starting fresh
            EventSetLauncherEnabled(false);
        }

        MusicPlayer musicPlayerPrefab = Resources.Load<MusicPlayer>("MusicPlayer");
        Instantiate(musicPlayerPrefab);
    }

    void Update()
    {
        if (activeTimer != TimerType.None)
            uiData.levelTimerText = FormatTime(Time.fixedTimeAsDouble - activeTimerStart);
    }

    private void SetupTriggers()
    {
        gtTriggerStart.onTriggerEnter.AddListener((GameObject gameObject, double fixedTime) =>
        {
            activeTimer = TimerType.GroundTutorial;
            activeTimerStart = fixedTime;
            uiData.levelNumberText = "GT";
        });
        gtTriggerFinish.onTriggerEnter.AddListener((GameObject gameObject, double fixedTime) =>
        {
            gtTriggerStart.ResetTrigger();
            gtTriggerFinish.ResetTrigger();

            if (activeTimer != TimerType.GroundTutorial)
                return;

            double time = Time.fixedTimeAsDouble - activeTimerStart;
            string timeString = FormatTime(time);
            string timeStringMono = FormatMonoText(timeString);
            gtGuiLastTime.text = timeStringMono;
            uiData.levelTimerText = timeString;
            uiData.levelNumberText = "";

            if (persistentData.groundTutorialBestTime <= 0 || time < persistentData.groundTutorialBestTime)
            {
                gtGuiBestTime.text = timeStringMono;
                persistentData.groundTutorialBestTime = time;
                PersistentDataSave();
            }

            activeTimer = TimerType.None;
            activeTimerStart = 0;
            StartCoroutine(CoHideHudTimer());
        });
        gtTriggerCancel.onTriggerEnter.AddListener((GameObject gameObject, double fixedTime) =>
        {
            gtTriggerStart.ResetTrigger();
            gtTriggerFinish.ResetTrigger();

            if (activeTimer != TimerType.GroundTutorial)
                return;

            uiData.levelTimerText = "";
            uiData.levelNumberText = "";

            activeTimer = TimerType.None;
            activeTimerStart = 0;
        });
    }

    private IEnumerator CoHideHudTimer()
    {
        yield return new WaitForSeconds(5);
        if (activeTimer == TimerType.None)
            uiData.levelTimerText = "";
    }

    public static string FormatTime(double time)
    {
        System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(time);
        return string.Format("{0:D2}:{1:D2}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
    }
    private static string FormatMonoText(string text)
    {
        return $"<mspace=.15>{text}</mspace>";
    }

    [ContextMenu("Persistent data load")]
    private void PersistentDataLoad()
    {
        PersistentDataUtil.TryLoad(PersistentDataName, out persistentData, e => Debug.LogWarning($"GamemodeHub.PersistentDataLoad exception : {e.Message}"));
        gtGuiBestTime.text = FormatMonoText(persistentData.groundTutorialBestTime > 0 ? FormatTime(persistentData.groundTutorialBestTime) : "N/A");
    }
    [ContextMenu("Persistent data save")]
    private void PersistentDataSave()
    {
        PersistentDataUtil.TrySave(PersistentDataName, persistentData, e => Debug.LogWarning($"GamemodeHub.PersistentDataSave exception : {e.Message}"));
    }
    [ContextMenu("Persistent data delete")]
    private void PersistentDataDelete()
    {
        PersistentDataUtil.TryDelete(PersistentDataName, e => Debug.LogWarning($"GamemodeHub.PersistentDataSave exception : {e.Message}"));
        gtGuiBestTime.text = FormatMonoText("N/A");
    }

    private static SatriProtoPlayer.TransformSnapshot? playerReturnFromRaceSnapshot;
    public static void ReturnFromRace()
    {
        SceneBase.SwitchScene("HubScene");
    }
    public static void BeginRace(string name, SatriProtoPlayer.TransformSnapshot returnTransform)
    {
        playerReturnFromRaceSnapshot = returnTransform;
        SceneBase.SwitchScene(name);
    }
    public static void BeginReplay(ReplayDownload replay, SatriProtoPlayer.TransformSnapshot returnTransform)
    {
        playerReturnFromRaceSnapshot = returnTransform;
        SceneBase.SwitchScene(replay.levelName, () =>
        {
            FindObjectOfType<SceneBase>().ReplaySystem.Playback(replay.data);
            FindObjectOfType<GamemodeRace>().SetReplay(replay);
        });
    }
}
