using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        private class WriterQuantDelta : WriterQuantAuto
        {
            private int[] currentQuantised;

            public WriterQuantDelta(ReplayStream stream)
                : base(stream)
            {
                currentQuantised = new int[stream.descriptor.stride];
            }

            protected override void WriteToStream(float value, int channel)
            {
                int quantised = Quantise(value);
                int delta = quantised - currentQuantised[channel];
                WriteQuantised(delta);
                currentQuantised[channel] = quantised;
            }
        }
        
        private class ReaderQuantDelta : ReaderQuantAuto
        {
            private int[] currentQuantised;

            public ReaderQuantDelta(ReplayStream stream)
                : base(stream)
            {
                currentQuantised = new int[stream.descriptor.stride];
            }

            protected override float ReadFromStream(int channel)
            {
                int delta = ReadQuantised();
                currentQuantised[channel] += delta;
                return Unquantise(currentQuantised[channel]);
            }
        }
    }
}