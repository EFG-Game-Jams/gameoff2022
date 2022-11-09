using Replay.StreamExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        private class WriterFloat32 : WriterBasic
        {
            public WriterFloat32(ReplayStream stream)
                : base(stream)
            {
            }

            protected override void WriteToStream(float value, int channel)
            {
                stream.dataStream.WriteFloat(value);
            }
        }

        private class ReaderFloat32 : ReaderBasic
        {
            public ReaderFloat32(ReplayStream stream)
                : base(stream)
            {
            }

            protected override float ReadFromStream(int channel)
            {
                return stream.dataStream.ReadFloat();
            }
        }
    }
}