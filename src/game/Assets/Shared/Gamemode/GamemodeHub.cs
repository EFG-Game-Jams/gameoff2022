using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamemodeHub : MonoBehaviour
{
    [Header("References")]
    [SerializeField] SatriProtoPlayer player;
    [SerializeField] PlayerData uiData;

    [Header("Ground tutorial")]
    [SerializeField] PlayerTrigger gtTriggerStart;
    [SerializeField] PlayerTrigger gtTriggerFinish;
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
    private string PersistentDataPath => System.IO.Path.Combine(Application.persistentDataPath, "data_hub");

    public void EventSetLauncherEnabled(bool enabled) => player.GetComponent<SatriProtoPlayerLauncher>().IsEnabled = enabled;

    void Start()
    {
        uiData.levelTimerText = "";
        uiData.levelNumberText = "";

        PersistentDataLoad();
        gtGuiLastTime.text = FormatMonoText("N/A");
        gtGuiBestTime.text = FormatMonoText(persistentData.groundTutorialBestTime > 0 ? FormatTime(persistentData.groundTutorialBestTime) : "N/A");

        SetupTriggers();

        EventSetLauncherEnabled(false);
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
    }

    private IEnumerator CoHideHudTimer()
    {
        yield return new WaitForSeconds(5);
        if (activeTimer == TimerType.None)
            uiData.levelTimerText = "";
    }

    private string FormatTime(double time)
    {
        System.TimeSpan timeSpan = System.TimeSpan.FromSeconds(time);
        return string.Format("{0:D2}:{1:D2}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
    }
    private string FormatMonoText(string text)
    {
        return $"<mspace=.15>{text}</mspace>";
    }

    private void PersistentDataLoad()
    {
        persistentData = default;
        try
        {
            string json = System.IO.File.ReadAllText(PersistentDataPath);
            persistentData = JsonUtility.FromJson<PersistentData>(json);
            Debug.Log($"GamemodeHub.PersistentDataLoad {PersistentDataPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"GamemodeHub.PersistentDataLoad exception : {e.Message}");
        }
    }
    private void PersistentDataSave()
    {
        try
        {
            string json = JsonUtility.ToJson(persistentData, false);
            System.IO.File.WriteAllText(PersistentDataPath, json);
            Application.ExternalEval("_JS_FileSystem_Sync();");
            Debug.Log($"GamemodeHub.PersistentDataSave {PersistentDataPath} {json}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"GamemodeHub.PersistentDataSave exception : {e.Message}");
        }
    }

    [ContextMenu("Delete persistent data")]
    private void PersistentDataDelete()
    {
        try
        {
            string path = PersistentDataPath;
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                Debug.Log($"GamemodeHub.PersistentDataDelete {PersistentDataPath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"GamemodeHub.PersistentDataDelete exception : {e.Message}");
        }
    }
}
