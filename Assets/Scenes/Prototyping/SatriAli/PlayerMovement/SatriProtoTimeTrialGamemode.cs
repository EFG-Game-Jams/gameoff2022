using Replay;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SatriProtoTimeTrialGamemode : MonoBehaviour
{
    [SerializeField] TextMesh timerDisplay;
    private double? timerStart;
    private double? timerEnd;

    public void EventStartTimer()
    {
        if (!timerStart.HasValue)
            timerStart = Time.fixedTime;// Time.timeSinceLevelLoadAsDouble;
    }
    public void EventEndTimer()
    {
        if (!timerEnd.HasValue)
            timerEnd = Time.fixedTime;// Time.timeSinceLevelLoadAsDouble;
    }
    public void EventReset()
    {
        SceneBase.ReloadScene();
    }
    public void EventReplay()
    {
        var replayData = FindObjectOfType<SceneBase>().ReplaySystem.Data.ToJson(true);
        Debug.Log($"Replay data length : {replayData.Length}");
        SceneBase.ReloadScene(() =>
        {
            ReplaySystem replaySystem = FindObjectOfType<SceneBase>().ReplaySystem;
            replaySystem.Playback(replayData);
        });
    }

    private void Update()
    {
        double time = 0f;
        if (timerEnd.HasValue)
            time = timerEnd.Value - timerStart.Value;
        else if (timerStart.HasValue)
            time = /*Time.timeSinceLevelLoadAsDouble*/Time.fixedTime - timerStart.Value;

        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        string timeText = string.Format("{0:D2}:{1:D2}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        timerDisplay.text = timeText;
    }
}
