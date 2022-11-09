using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        public abstract class Writer
        {
            protected readonly ReplayStream stream;
            private readonly int commitInterval;
            private int samplesSinceLastCommit;

            private Writer()
            {
            }
            protected Writer(ReplayStream stream)
            {
                this.stream = stream;
                commitInterval = stream.descriptor.keyframeInterval;
            }

            public void Write(float value)
            {
                Debug.Assert(stream.descriptor.stride == 1);
                SetChannel(0, value);
                if (ShouldWriteSample())
                    Commit();
            }
            public void Write(Vector2 value)
            {
                Debug.Assert(stream.descriptor.stride == 2);
                SetChannel(0, value.x);
                SetChannel(1, value.y);
                if (ShouldWriteSample())
                    Commit();
            }
            public void Write(Vector3 value)
            {
                Debug.Assert(stream.descriptor.stride == 3);
                SetChannel(0, value.x);
                SetChannel(1, value.y);
                SetChannel(2, value.z);
                if (ShouldWriteSample())
                    Commit();
            }

            public void Finish()
            {
                if (commitInterval != 0 && samplesSinceLastCommit != 1)
                    Commit();
                Flush();
            }

            private bool ShouldWriteSample()
            {
                if (commitInterval == 0)
                    return true;

                bool shouldWrite = (samplesSinceLastCommit == 0);
                samplesSinceLastCommit = (samplesSinceLastCommit + 1) % commitInterval;
                return shouldWrite;
            }

            protected abstract void SetChannel(int channel, float value);
            protected abstract void Commit();
            protected abstract void Flush();
        }

        private abstract class WriterBasic : Writer
        {
            private float[] channels;

            public WriterBasic(ReplayStream stream)
                : base(stream)
            {
                channels = new float[stream.descriptor.stride];
            }

            protected sealed override void SetChannel(int channel, float value)
            {
                channels[channel] = value;
            }

            protected sealed override void Commit()
            {
                for (int i = 0; i < channels.Length; ++i)
                    WriteToStream(channels[i], i);
            }

            protected sealed override void Flush()
            {
            }

            protected abstract void WriteToStream(float value, int channel);
        }
    }
}