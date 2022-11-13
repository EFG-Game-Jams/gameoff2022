using Replay;
using Replay.StreamExtensions;
using System;
using System.IO;
using UnityEngine;

public class SatriProtoTimeTrialGamemode : MonoBehaviour
{
    [SerializeField] TextMesh timerDisplay;

    private Replayable replayable;
    private ReplayEventList replayTimerEvent;

    private double fixedTimeAtStart;
    private double? timerStart;
    private double? timerEnd;

    private enum TimerEventType
    {
        Start,
        End,
    }
    private struct TimerEventData : IStreamable
    {
        public TimerEventType eventType;
        public float time;

        public void ReadFromStream(Stream s)
        {
            eventType = (TimerEventType)s.ReadByte();
            time = s.ReadFloat();
        }
        public void WriteToStream(Stream s)
        {
            s.WriteByte((byte)eventType);
            s.WriteFloat(time);
        }
    }

    private void Start()
    {
        replayable = GetComponent<Replayable>();
        replayTimerEvent = replayable.GetEventList("Gamemode.TimerEvent");

        fixedTimeAtStart = Time.fixedTimeAsDouble;
    }

    public void EventStartTimer(GameObject player, double time)
    {
        if (!timerStart.HasValue)
        {
            timerStart = time - fixedTimeAtStart;
            if (replayable.Mode == ReplaySystem.ReplayMode.Record)
                replayTimerEvent.Write(new TimerEventData { eventType = TimerEventType.Start, time = (float)timerStart.Value });
        }
    }
    public void EventEndTimer(GameObject player, double time)
    {
        if (!timerEnd.HasValue)
        {
            timerEnd = time - fixedTimeAtStart;
            if (replayable.Mode == ReplaySystem.ReplayMode.Record)
                replayTimerEvent.Write(new TimerEventData { eventType = TimerEventType.End, time = (float)timerEnd.Value });
        }
    }
    public void EventReset(GameObject player, double time)
    {
        SceneBase.ReloadScene();
    }

    public void EventReplay(GameObject player, double time)
    {
        var replayData = FindObjectOfType<SceneBase>().ReplaySystem.Data.ToJson(true);
        Debug.Log($"Replay data length : {replayData.Length}");

        SceneBase.ReloadScene(() =>
        {
            ReplaySystem replaySystem = FindObjectOfType<SceneBase>().ReplaySystem;
            replaySystem.Playback(replayData);
        });
    }

    private void FixedUpdate()
    {
        if (replayable.Mode == ReplaySystem.ReplayMode.Playback)
        {
            while (replayTimerEvent.TryRead(out TimerEventData data))
            {
                switch (data.eventType)
                {
                    case TimerEventType.Start:
                        timerStart = data.time;
                        break;
                    case TimerEventType.End:
                        timerEnd = data.time;
                        break;
                }
            }
        }
    }

    private void Update()
    {
        double time = 0f;
        if (timerEnd.HasValue)
            time = timerEnd.Value - timerStart.Value;
        else if (timerStart.HasValue)
            time = (Time.fixedTimeAsDouble - fixedTimeAtStart) - timerStart.Value;

        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        string timeText = string.Format("{0:D2}:{1:D2}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        timerDisplay.text = timeText;
    }
}
