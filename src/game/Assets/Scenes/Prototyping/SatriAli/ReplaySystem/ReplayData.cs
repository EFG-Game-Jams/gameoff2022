using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Replay
{
    public class ReplayData
    {
        public struct Metrics
        {
            public int objectCount;
            public int streamCount;
            public int eventListCount;
            public long streamBytes;
            public long eventListBytes;
            public long totalBytes;
        }

        [System.Serializable]
        private class Serialised
        {
            [System.Serializable]
            public class ObjectData
            {
                public int uid;
                public ReplayStream.Serialised[] streams;
                public ReplayEventList.Serialised[] eventLists;
            }
            public ObjectData[] objects;
        }

        private class ObjectRuntimeData
        {
            public List<ReplayStream> streams = new();
            public List<ReplayEventList> eventLists = new();
        }
        private Dictionary<int, ObjectRuntimeData> objectRuntimeData = new();

        public Metrics GetMetrics()
        {
            Metrics m = default;

            foreach (var pair in objectRuntimeData)
            {
                ++m.objectCount;

                foreach (var stream in pair.Value.streams)
                {
                    ++m.streamCount;
                    m.streamBytes += stream.Size;
                }

                foreach (var eventList in pair.Value.eventLists)
                {
                    ++m.eventListCount;
                    m.eventListBytes += eventList.Size;
                }
            }
            m.totalBytes = m.streamBytes + m.eventListBytes;

            return m;
        }

        public string ToJson(bool prettyPrint = false)
        {
            Serialised data = new();

            data.objects = new Serialised.ObjectData[objectRuntimeData.Count];
            int objectRank = 0;
            foreach (var objectPair in objectRuntimeData)
            {
                Serialised.ObjectData objectData = new();
                data.objects[objectRank++] = objectData;

                objectData.uid = objectPair.Key;

                List<ReplayStream> streams = objectPair.Value.streams;
                objectData.streams = new ReplayStream.Serialised[streams.Count];
                for (int i = 0; i < streams.Count; ++i)
                    objectData.streams[i] = streams[i].Serialise();

                List<ReplayEventList> eventLists = objectPair.Value.eventLists;
                objectData.eventLists = new ReplayEventList.Serialised[eventLists.Count];
                for (int i = 0; i < eventLists.Count; ++i)
                    objectData.eventLists[i] = eventLists[i].Serialise();
            }

            return JsonUtility.ToJson(data, prettyPrint);
        }
        public void FromJson(string json, ReplaySystem replaySystem)
        {
            Serialised data = JsonUtility.FromJson<Serialised>(json);

            objectRuntimeData = new(data.objects.Length);
            foreach (var objectData in data.objects)
            {
                ObjectRuntimeData runtimeData = new();

                foreach (var streamData in objectData.streams)
                    runtimeData.streams.Add(new ReplayStream(streamData));

                foreach (var eventListData in objectData.eventLists)
                    runtimeData.eventLists.Add(new ReplayEventList(eventListData, replaySystem));

                objectRuntimeData.Add(objectData.uid, runtimeData);
            }
        }

        internal void Clear()
        {
            objectRuntimeData = new();
        }

        private ObjectRuntimeData GetOrCreateRuntimeData(int uid)
        {
            if (!objectRuntimeData.TryGetValue(uid, out ObjectRuntimeData data))
            {
                data = new();
                objectRuntimeData.Add(uid, data);
            }
            return data;
        }

        internal void AddStream(int uid, ReplayStream stream)
        {
            Debug.Assert(!TryGetStream(uid, stream.descriptor.name, out ReplayStream _));
            GetOrCreateRuntimeData(uid).streams.Add(stream);
        }
        internal bool TryGetStream(int uid, string name, out ReplayStream stream)
        {
            stream = null;

            if (!objectRuntimeData.TryGetValue(uid, out ObjectRuntimeData data))
                return false;

            foreach (var s in data.streams)
            {
                if (s.descriptor.name != name)
                    continue;
                stream = s;
                return true;
            }

            return false;
        }

        internal void AddEventList(int uid, ReplayEventList eventList)
        {
            Debug.Assert(!TryGetEventList(uid, eventList.Name, out ReplayEventList _));
            GetOrCreateRuntimeData(uid).eventLists.Add(eventList);
        }
        internal bool TryGetEventList(int uid, string name, out ReplayEventList eventList)
        {
            eventList = null;

            if (!objectRuntimeData.TryGetValue(uid, out ObjectRuntimeData data))
                return false;

            foreach (var e in data.eventLists)
            {
                if (e.Name != name)
                    continue;
                eventList = e;
                return true;
            }

            return false;
        }
    }
}
