using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Replay
{
    public class Replayable : MonoBehaviour
    {
        [Header("System - read only")]
        [SerializeField] ReplaySystem replaySystem;
        [SerializeField] int replayUID;

        [Header("Components")]
        [Tooltip("Components which should only be enabled during recording")]
        [SerializeField] Component[] enabledInRecordOnly;
        [Tooltip("Components which should only be enabled during playback")]
        [SerializeField] Component[] enabledInPlaybackOnly;

        [Header("Data")]
        [SerializeField] ReplayStream.Descriptor[] streamDescriptors;
        [SerializeField] string[] eventNames;

        public ReplaySystem System => replaySystem;
        public int UID => replayUID;
        public ReplaySystem.ReplayMode Mode => replaySystem.Mode;

        private ReplayStream[] streams;
        private ReplayEventList[] eventLists;

        public ReplayStream.Writer GetWriter(string name)
        {
            Debug.Assert(Mode == ReplaySystem.ReplayMode.Record);
            Debug.Assert(streams != null);
            
            foreach (var stream in streams)
                if (stream.descriptor.name == name)
                    return stream.GetWriter();

            return null;
        }
        public ReplayStream.Reader GetReader(string name)
        {
            Debug.Assert(Mode == ReplaySystem.ReplayMode.Playback);
            Debug.Assert(streams != null);

            foreach (var stream in streams)
                if (stream.descriptor.name == name)
                    return stream.GetReader();

            return null;
        }

        private void Awake()
        {
            Debug.Assert(replaySystem != null);
            Debug.Assert(replayUID != 0);

            SetupData();
            UpdateComponents();
        }

        public void OnReplaySystemConfigure(ReplaySystem system, int uid)
        {
            replaySystem = system;
            replayUID = uid;            
        }

        private void UpdateComponents()
        {
            bool isRecording = (Mode == ReplaySystem.ReplayMode.Record);
            foreach (var component in enabledInRecordOnly)
                SetComponentEnabled(component, isRecording);
            foreach (var component in enabledInPlaybackOnly)
                SetComponentEnabled(component, !isRecording);
        }
        private void SetComponentEnabled(Component component, bool enabled)
        {
            if (component is Behaviour behaviour)
                behaviour.enabled = enabled;
            else if (component is Collider collider)
                collider.enabled = enabled;
            else
                throw new NotImplementedException($"Replayable.SetComponentEnabled doesn't currently support component {component.name}");
        }

        private void SetupData()
        {
            streams = new ReplayStream[streamDescriptors.Length];
            eventLists = new ReplayEventList[eventNames.Length];

            if (Mode == ReplaySystem.ReplayMode.Record)
            {
                for (int i = 0; i < streamDescriptors.Length; ++i)
                {
                    ReplayStream stream = new ReplayStream(streamDescriptors[i]);
                    replaySystem.SetRecordingStream(this, stream);
                    streams[i] = stream;
                }

                for (int i = 0; i < eventNames.Length; ++i)
                {
                    ReplayEventList eventList = new ReplayEventList(eventNames[i], System);
                    replaySystem.SetRecordingEventList(this, eventList);
                    eventLists[i] = eventList;
                }
            }
            else
            {
                for (int i = 0; i < streamDescriptors.Length; ++i)
                    streams[i] = replaySystem.GetPlaybackStream(this, streamDescriptors[i]);

                for (int i = 0; i < eventNames.Length; ++i)
                    eventLists[i] = replaySystem.GetPlaybackEventList(this, eventNames[i]);
            }
        }
    }
}