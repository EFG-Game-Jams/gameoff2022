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
            timerStart = Time.timeSinceLevelLoadAsDouble;
    }
    public void EventEndTimer()
    {
        if (!timerEnd.HasValue)
            timerEnd = Time.timeSinceLevelLoadAsDouble;
    }
    public void EventReset()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Update()
    {
        double time = 0f;
        if (timerEnd.HasValue)
            time = timerEnd.Value - timerStart.Value;
        else if (timerStart.HasValue)
            time = Time.timeSinceLevelLoadAsDouble - timerStart.Value;

        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        string timeText = string.Format("{0:D2}:{1:D2}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        timerDisplay.text = timeText;
    }
}
