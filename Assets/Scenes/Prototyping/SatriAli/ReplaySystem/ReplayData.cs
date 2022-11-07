using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Replay
{
    public class ReplayData
    {
        [System.Serializable]
        private class Serialised
        {
            [System.Serializable]
            public class ObjectData
            {
                public int uid;
                public ReplayStream.Serialised[] streams;
            }
            public ObjectData[] objects;
        }

        private Dictionary<int, List<ReplayStream>> objectStreams = new();

        public string ToJson(bool prettyPrint = false)
        {
            Serialised data = new();

            data.objects = new Serialised.ObjectData[objectStreams.Count];
            int objectRank = 0;
            foreach (var objectPair in objectStreams)
            {
                Serialised.ObjectData objectData = new();
                data.objects[objectRank++] = objectData;

                objectData.uid = objectPair.Key;

                List<ReplayStream> streams = objectPair.Value;
                objectData.streams = new ReplayStream.Serialised[streams.Count];
                for (int i = 0; i < streams.Count; ++i)
                    objectData.streams[i] = streams[i].Serialise();
            }

            return JsonUtility.ToJson(data, prettyPrint);
        }
        public void FromJson(string json)
        {
            Serialised data = JsonUtility.FromJson<Serialised>(json);

            objectStreams = new(data.objects.Length);
            foreach (var objectData in data.objects)
            {
                List<ReplayStream> streams = new(objectData.streams.Length);
                foreach (var streamData in objectData.streams)
                    streams.Add(new ReplayStream(streamData));
                objectStreams.Add(objectData.uid, streams);
            }
        }

        internal void Clear()
        {
            objectStreams = new();
        }

        internal void AddStream(int uid, ReplayStream stream)
        {
            if (!objectStreams.TryGetValue(uid, out List<ReplayStream> streams))
            {
                streams = new(1);
                objectStreams.Add(uid, streams);
            }
            streams.Add(stream);
        }
        internal bool TryGetStream(int uid, string name, out ReplayStream stream)
        {
            stream = null;

            if (!objectStreams.TryGetValue(uid, out List<ReplayStream> streams))
                return false;

            foreach (var s in streams)
            {
                if (s.descriptor.name != name)
                    continue;
                stream = s;
                return true;
            }

            return false;
        }
    }
}
