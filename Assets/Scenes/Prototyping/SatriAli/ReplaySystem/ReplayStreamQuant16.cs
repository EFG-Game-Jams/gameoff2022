using Replay.StreamExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        private class WriterQuant16 : WriterBasic
        {
            private readonly int quantise;

            public WriterQuant16(ReplayStream stream)
                : base(stream)
            {
                quantise = stream.descriptor.quantise;
            }

            protected override void WriteToStream(float value, int channel)
            {
                float fquant = value * quantise;
                int iquant = Mathf.RoundToInt(fquant);

                Debug.Assert(iquant >= short.MinValue);
                Debug.Assert(iquant <= short.MaxValue);

                stream.dataStream.WriteShort((short)iquant);
            }
        }

        private class ReaderQuant16 : ReaderBasic
        {
            private readonly int quantise;

            public ReaderQuant16(ReplayStream stream)
                : base(stream)
            {
                quantise = stream.descriptor.quantise;
            }

            protected override float ReadFromStream(int channel)
            {
                return (float)stream.dataStream.ReadShort() / quantise;
            }
        }
    }
}