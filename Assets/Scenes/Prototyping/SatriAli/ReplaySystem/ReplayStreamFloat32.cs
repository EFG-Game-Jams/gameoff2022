using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        private class WriterFloat32 : Writer
        {
            private float[] channels;

            public WriterFloat32(ReplayStream stream)
                : base(stream)
            {
                channels = new float[stream.descriptor.stride];
            }

            protected override void SetChannel(int channel, float value, bool commit = false)
            {
                channels[channel] = value;

                if (commit)
                {
                    for (int i = 0; i < channels.Length; ++i)
                        stream.WriteFloat(channels[i]);
                }
            }
        }

        public class ReaderFloat32 : Reader
        {
            private float[] channels;

            public ReaderFloat32(ReplayStream stream)
                : base(stream)
            {
                channels = new float[stream.descriptor.stride];
            }

            protected override float GetChannel(int channel, bool pump = false)
            {
                if (pump && stream.CanRead)
                    for (int i = 0; i < channels.Length; ++i)
                        channels[i] = stream.ReadFloat();

                return channels[channel];
            }
        }
    }
}