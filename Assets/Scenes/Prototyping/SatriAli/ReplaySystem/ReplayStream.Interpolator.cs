using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        private struct Interpolator
        {
            private InterpolationType interpolation;
            private int keyframeInterval;
            private int samplesSinceLastRead;
            private float muPerSample;
            private float prevValue;
            private float nextValue;

            public bool NeedsSample => (samplesSinceLastRead <= 0);

            public Interpolator(InterpolationType interpolation, int keyframeInterval)
            {
                this.interpolation = interpolation;
                this.keyframeInterval = (keyframeInterval > 0 ? keyframeInterval : 1);

                samplesSinceLastRead = -1;
                muPerSample = 1f / this.keyframeInterval;

                prevValue = 0;
                nextValue = 0;
            }

            public void AddSample(float value)
            {
                if (samplesSinceLastRead < 0)
                {
                    // initialisation
                    prevValue = value;
                    nextValue = value;
                }
                else
                {
                    // new sample
                    prevValue = nextValue;
                    nextValue = value;
                }
            }

            public float Step()
            {
                float mu = Mathf.Clamp01(samplesSinceLastRead * muPerSample);
                samplesSinceLastRead = (samplesSinceLastRead + 1) % keyframeInterval;
                switch (interpolation)
                {
                    case InterpolationType.Linear:
                        return Mathf.LerpUnclamped(prevValue, nextValue, mu);
                    case InterpolationType.Angular:
                        return Mathf.LerpAngle(prevValue, nextValue, mu);
                    case InterpolationType.None:
                        return prevValue;
                    default:
                        throw new System.NotImplementedException();
                }
            }
        }
    }
}
