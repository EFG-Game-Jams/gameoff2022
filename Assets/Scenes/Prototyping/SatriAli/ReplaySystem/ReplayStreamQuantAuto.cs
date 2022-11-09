using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        private static class QuantAuto
        {
            public const int MinValue8 = -(1 << 5);
            public const int MaxValue8 = (1 << 5) - 1;

            public const int MinValue16 = -(1 << 13);
            public const int MaxValue16 = (1 << 13) - 1;

            public const int MinValue24 = -(1 << 21);
            public const int MaxValue24 = (1 << 21) - 1;

            public const int MinValue32 = -(1 << 29);
            public const int MaxValue32 = (1 << 29) - 1;
        }

        private class WriterQuantAuto : WriterBasic
        {
            protected readonly int quantise;
            protected readonly bool isAngle;

            public WriterQuantAuto(ReplayStream stream)
                : base(stream)
            {
                quantise = stream.descriptor.quantise;
                isAngle = (stream.descriptor.interpolation == InterpolationType.Angular);
            }

            protected override void WriteToStream(float value, int channel)
            {                
                int quantised = Quantise(value);
                WriteQuantised(quantised);
            }

            protected int Quantise(float value)
            {
                if (isAngle)
                    value = Mathf.DeltaAngle(0f, value); // to +/- 180°
                float fquant = value * quantise;
                Debug.Assert(fquant >= QuantAuto.MinValue32 && fquant <= QuantAuto.MaxValue32);
                return Mathf.RoundToInt(fquant);
            }

            protected void WriteQuantised(int quantised)
            {
                int byteCount = 4;
                if (quantised >= QuantAuto.MinValue8 && quantised <= QuantAuto.MaxValue8)
                    byteCount = 1;
                else if (quantised >= QuantAuto.MinValue16 && quantised <= QuantAuto.MaxValue16)
                    byteCount = 2;
                else if (quantised >= QuantAuto.MinValue24 && quantised <= QuantAuto.MaxValue24)
                    byteCount = 3;

                int uquant = Mathf.Abs(quantised);
                int typeInfo = byteCount - 1;
                int encoded = (uquant << 3) | (quantised < 0 ? 0b100 : 0) | typeInfo;
                for (int i = 0; i < byteCount; ++i)
                {
                    int byteValue = (encoded >> (i * 8)) & 0xFF;
                    stream.dataStream.WriteByte((byte)byteValue);
                }
            }
        }

        private class ReaderQuantAuto : ReaderBasic
        {
            private readonly int quantise;

            public ReaderQuantAuto(ReplayStream stream)
                : base(stream)
            {
                quantise = stream.descriptor.quantise;
            }

            protected override float ReadFromStream(int channel)
            {
                int quantised = ReadQuantised();
                return Unquantise(quantised);
            }

            protected float Unquantise(int quantised)
            {
                return (float)quantised / quantise;
            }

            protected int ReadQuantised()
            {
                int firstByte = stream.dataStream.ReadByte();
                int typeInfo = firstByte & 0b011;
                int signBit = firstByte & 0b100;
                int byteCount = typeInfo + 1;

                int encoded = firstByte;
                for (int i = 1; i < byteCount; ++i)
                    encoded |= stream.dataStream.ReadByte() << (i * 8);

                int uquant = encoded >> 3;
                return (signBit != 0 ? -uquant : uquant);
            }
        }
    }
}