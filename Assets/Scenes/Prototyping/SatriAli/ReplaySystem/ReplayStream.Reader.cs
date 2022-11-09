using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        public abstract class Reader
        {
            protected readonly ReplayStream stream;
            private readonly Interpolator[] interpolators;

            private Reader()
            {
            }
            protected Reader(ReplayStream stream)
            {
                this.stream = stream;

                interpolators = new Interpolator[stream.descriptor.stride];
                for (int i = 0; i < interpolators.Length; ++i)
                    interpolators[i] = new Interpolator(stream.descriptor.interpolation, stream.descriptor.keyframeInterval);
            }

            public float ReadFloat()
            {
                Debug.Assert(stream.descriptor.stride == 1);

                UpdateInterpolators();

                return interpolators[0].Step();
            }
            public Vector2 ReadVector2()
            {
                Debug.Assert(stream.descriptor.stride == 2);

                UpdateInterpolators();

                Vector2 result;
                result.x = interpolators[0].Step();
                result.y = interpolators[1].Step();
                return result;
            }
            public Vector3 ReadVector3()
            {
                Debug.Assert(stream.descriptor.stride == 3);

                UpdateInterpolators();

                Vector3 result;
                result.x = interpolators[0].Step();
                result.y = interpolators[1].Step();
                result.z = interpolators[2].Step();
                return result;
            }

            private void UpdateInterpolators()
            {
                if (!interpolators[0].NeedsSample)
                    return;

                Pump();
                for (int i = 0; i < interpolators.Length; ++i)
                    interpolators[i].AddSample(GetChannel(i));
            }

            protected abstract void Pump();
            protected abstract float GetChannel(int channel);
        }

        private abstract class ReaderBasic : Reader
        {
            private float[] channels;

            public ReaderBasic(ReplayStream stream)
                : base(stream)
            {
                channels = new float[stream.descriptor.stride];
            }

            protected sealed override float GetChannel(int channel)
            {
                return channels[channel];
            }

            protected sealed override void Pump()
            {
                if (!stream.CanRead)
                    return;

                for (int i = 0; i < channels.Length; ++i)
                    channels[i] = ReadFromStream(i);
            }

            protected abstract float ReadFromStream(int channel);
        }
    }
}