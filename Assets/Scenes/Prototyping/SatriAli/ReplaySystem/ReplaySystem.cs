using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Replay
{
    [DefaultExecutionOrder(1000)]
    public class ReplaySystem : MonoBehaviour
    {
        [Header("Scene replayables")]
        [SerializeField] Replayable[] sceneReplayables;

        [Header("Runtime replayables")]
        [SerializeField] Replayable[] prefabReplayables;

        public enum ReplayMode
        {
            Record,
            Playback,
        }

        public ReplayMode Mode { get; private set; } = ReplayMode.Record;
        public ReplayData Data { get; private set; } = new();

        public int DynamicFrameCount { get; private set; }
        public int FixedFrameCount { get; private set; }

        private void OnValidate()
        {
            // assign negative IDs to all registered scene replayables
            for (int i = 0; i < sceneReplayables.Length; ++i)
            {
                sceneReplayables[i].OnReplaySystemConfigure(this, -(i + 1));
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(sceneReplayables[i]);
#endif
            }
        }

        private void Update()
        { 
            ++DynamicFrameCount;
        }

        private void FixedUpdate()
        {
            ++FixedFrameCount;
        }

        public void Record()
        {
            Mode = ReplayMode.Record;
            Data.Clear();
        }
        public void Playback(string json)
        {
            Mode = ReplayMode.Playback;
            Data.FromJson(json);
        }

        internal void SetRecordingStream(Replayable replayable, ReplayStream stream)
        {
            Debug.Assert(Mode == ReplayMode.Record);
            Debug.Assert(replayable != null && replayable.UID != 0);
            Debug.Assert(stream != null);

            Data.AddStream(replayable.UID, stream);
        }
        internal ReplayStream GetPlaybackStream(Replayable replayable, in ReplayStream.Descriptor defaultDescriptor)
        {
            Debug.Assert(Mode == ReplayMode.Playback);
            Debug.Assert(replayable != null && replayable.UID != 0);

            if (!Data.TryGetStream(replayable.UID, defaultDescriptor.name, out ReplayStream stream))
                stream = new ReplayStream(defaultDescriptor, asReadOnly: true);
            return stream;
        }
    }
}