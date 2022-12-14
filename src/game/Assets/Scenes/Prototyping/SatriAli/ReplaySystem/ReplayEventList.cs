using Replay.StreamExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Replay
{
    // this is both used to read and write for simplicity
    // if you try to do both at once... things will no doubt go wrong
    public class ReplayEventList
    {
        [System.Serializable]
        internal struct Serialised
        {
            public string name;
            public string data;
        }

        public struct PayloadNone : IStreamable
        {
            public void ReadFromStream(Stream s) {}
            public void WriteToStream(Stream s) {}
        }

        public readonly string Name;
        public readonly ReplaySystem replaySystem;
        private Stream dataStream;
        private uint nextEventFrame = uint.MaxValue;

        public long Size => dataStream.Length;

        internal ReplayEventList(string name, ReplaySystem system, bool asReadOnly = false, bool nullStream = false)
        {
            Name = name;
            replaySystem = system;
            if (nullStream)
                dataStream = System.IO.Stream.Null;
            else if (asReadOnly)
                dataStream = new MemoryStream(new byte[0], false);
            else
                dataStream = new MemoryStream();
        }
        internal ReplayEventList(in Serialised serialised, ReplaySystem system)
        {
            Name = serialised.name;
            replaySystem = system;
            byte[] bytes = Convert.FromBase64String(serialised.data);
            bytes = Decompress(bytes);
            dataStream = new MemoryStream(bytes, false);
        }

        internal Serialised Serialise()
        {
            Debug.Assert(dataStream is MemoryStream);

            byte[] bytes = (dataStream as MemoryStream).ToArray();
            bytes = Compress(bytes);
            string base64 = Convert.ToBase64String(bytes);

            return new Serialised { name = Name, data = base64 };
        }

        private byte[] Compress(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new System.IO.Compression.GZipStream(mso, System.IO.Compression.CompressionLevel.Optimal))
                {
                    msi.CopyTo(gs);
                }
                return mso.ToArray();
            }
        }
        private byte[] Decompress(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new System.IO.Compression.GZipStream(msi, System.IO.Compression.CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }
                return mso.ToArray();
            }
        }

        public void Write<T>(T eventData)
            where T : struct, IStreamable
        {
            dataStream.WriteUint(replaySystem.FixedFrameCount);
            dataStream.WriteStruct(eventData);
        }

        public void Write()
        {
            Write(new PayloadNone());
        }

        public bool TryRead<T>(out T eventData)
            where T : struct, IStreamable
        {
            eventData = default;

            if (nextEventFrame == uint.MaxValue)
            {
                if (dataStream.Position < dataStream.Length)
                    nextEventFrame = dataStream.ReadUint();
                else
                    return false;
            }

            Debug.Assert(nextEventFrame >= replaySystem.FixedFrameCount, "Replay event was not processed during the correct frame");
            if (nextEventFrame > replaySystem.FixedFrameCount)
                return false; // event is in the future

            eventData = dataStream.ReadStruct<T>();
            nextEventFrame = uint.MaxValue;
            return true;
        }

        public bool TryRead()
        {
            return TryRead(out PayloadNone _);
        }
    }
}
