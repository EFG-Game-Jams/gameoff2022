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
                stream.dataStream.WriteFloatQuantised16(value, quantise);
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
                return stream.dataStream.ReadFloatQuantised16(quantise);
            }
        }
    }
}