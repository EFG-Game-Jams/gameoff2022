using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Replay.StreamExtensions
{
    public interface IStreamable
    {
        void WriteToStream(Stream s);
        void ReadFromStream(Stream s);
    }

    public static class StreamExtensions
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        private struct UnionFloat32
        {
            [System.Runtime.InteropServices.FieldOffset(0)] public float asFloat;
            [System.Runtime.InteropServices.FieldOffset(0)] public int asInt;
        }

        public static void WriteShort(this Stream s, short value)
        {
            s.WriteByte((byte)(value & 0xFF));
            s.WriteByte((byte)((value >> 8) & 0xFF));
        }
        public static void WriteInt(this Stream s, int value)
        {
            s.WriteByte((byte)(value & 0xFF));
            s.WriteByte((byte)((value >> 8) & 0xFF));
            s.WriteByte((byte)((value >> 16) & 0xFF));
            s.WriteByte((byte)((value >> 24) & 0xFF));
        }
        public static void WriteUint(this Stream s, uint value)
        {
            s.WriteByte((byte)(value & 0xFF));
            s.WriteByte((byte)((value >> 8) & 0xFF));
            s.WriteByte((byte)((value >> 16) & 0xFF));
            s.WriteByte((byte)((value >> 24) & 0xFF));
        }
        public static void WriteFloat(this Stream s, float value)
        {
            WriteInt(s, (new UnionFloat32 { asFloat = value }).asInt);
        }
        public static void WriteVector2(this Stream s, Vector2 value)
        {
            WriteFloat(s, value.x);
            WriteFloat(s, value.y);
        }
        public static void WriteVector3(this Stream s, Vector3 value)
        {
            WriteFloat(s, value.x);
            WriteFloat(s, value.y);
            WriteFloat(s, value.z);
        }
        public static void WriteFloatQuantised16(this Stream s, float value, int quantise)
        {
            float fquant = value * quantise;
            int iquant = Mathf.RoundToInt(fquant);

            Debug.Assert(iquant >= short.MinValue);
            Debug.Assert(iquant <= short.MaxValue);

            s.WriteShort((short)iquant);
        }
        public static void WriteStruct<T>(this Stream s, in T value) where T : struct, IStreamable
        {
            value.WriteToStream(s);
        }

        public static short ReadShort(this Stream s)
        {
            return (short)(
                s.ReadByte() |
                (s.ReadByte() << 8)
            );
        }
        public static int ReadInt(this Stream s)
        {
            return (
                s.ReadByte() |
                (s.ReadByte() << 8) |
                (s.ReadByte() << 16) |
                (s.ReadByte() << 24)
            );
        }
        public static uint ReadUint(this Stream s)
        {
            return (
                (uint)s.ReadByte() |
                ((uint)s.ReadByte() << 8) |
                ((uint)s.ReadByte() << 16) |
                ((uint)s.ReadByte() << 24)
            );
        }
        public static float ReadFloat(this Stream s)
        {
            return (new UnionFloat32 { asInt = ReadInt(s) }).asFloat;
        }
        public static Vector2 ReadVector2(this Stream s)
        {
            Vector2 v = default;
            v.x = ReadFloat(s);
            v.y = ReadFloat(s);
            return v;
        }
        public static Vector3 ReadVector3(this Stream s)
        {
            Vector3 v = default;
            v.x = ReadFloat(s);
            v.y = ReadFloat(s);
            v.z = ReadFloat(s);
            return v;
        }
        public static float ReadFloatQuantised16(this Stream s, int quantise)
        {
            return (float)s.ReadShort() / quantise;
        }
        public static T ReadStruct<T>(this Stream s) where T : struct, IStreamable
        {
            T value = default;
            value.ReadFromStream(s);
            return value;
        }
    }
}
