using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        public enum DataType
        {
            Float32,    // 32 bit float
            //Quant16,    // 16 bit quantised float
        }

        [System.Serializable]
        public struct Descriptor
        {
            [Tooltip("Must be unique within this gameobject")]
            public string name;
            [Tooltip("Stream type\nFloat32 - raw 32-bit\nQuant16 - quantised 16-bit")]
            public DataType dataType;
            [Tooltip("Number of values per sample\n1: float\n2: Vector2\n3: Vector3\netc...")]
            public int stride;
            [Tooltip("Quantisation scale\nValues will be multiplied by this then rounded to the nearest integer\nThis is ignored for non-quantised types")]
            public int quantise;
        }

        [System.Serializable]
        public struct Serialised
        {
            public Descriptor descriptor;
            public string data;
        }

        public readonly Descriptor descriptor;
        private MemoryStream stream;
        private Writer writer;

        public ReplayStream(in Descriptor descriptor, bool asReadOnly = false)
        {
            this.descriptor = descriptor;
            if (asReadOnly)
                stream = new(new byte[0], false);
            else
                stream = new();
        }
        public ReplayStream(in Serialised serialised)
            : this(serialised.descriptor)
        {
            byte[] bytes = System.Convert.FromBase64String(serialised.data);
            stream = new(bytes, false);
        }

        public Serialised Serialise()
        {
            writer?.Flush();
            byte[] bytes = stream.ToArray();
            string base64 = System.Convert.ToBase64String(bytes);
            return new Serialised { descriptor = descriptor, data = base64 };
        }

        public Writer GetWriter()
        {
            Debug.Assert(writer == null);
            Debug.Assert(stream.CanWrite);            
            switch (descriptor.dataType)
            {
                case DataType.Float32:
                    writer = new WriterFloat32(this);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
            return writer;
        }
        public Reader GetReader()
        {
            Debug.Assert(writer == null);
            switch (descriptor.dataType)
            {
                case DataType.Float32:
                    return new ReaderFloat32(this);
                default:
                    throw new System.NotImplementedException();
            }
        }

        public abstract class Writer
        {
            protected readonly ReplayStream stream;

            private Writer() { }
            protected Writer(ReplayStream stream) { this.stream = stream; }

            public void Write(float value)
            {
                Debug.Assert(stream.descriptor.stride == 1);
                SetChannel(0, value, true);
            }
            public void Write(Vector2 value)
            {
                Debug.Assert(stream.descriptor.stride == 2);
                SetChannel(0, value.x);
                SetChannel(1, value.y, true);
            }
            public void Write(Vector3 value)
            {
                Debug.Assert(stream.descriptor.stride == 3);
                SetChannel(0, value.x);
                SetChannel(1, value.y);
                SetChannel(2, value.z, true);
            }

            protected abstract void SetChannel(int channel, float value, bool commit = false);
            public virtual void Flush() { }
        }

        public abstract class Reader
        {
            protected readonly ReplayStream stream;

            private Reader() { }
            protected Reader(ReplayStream stream) { this.stream = stream; }

            public float ReadFloat()
            {
                Debug.Assert(stream.descriptor.stride == 1);                
                return GetChannel(0, true);
            }
            public Vector2 ReadVector2()
            {
                Debug.Assert(stream.descriptor.stride == 2);
                Vector2 result;
                result.x = GetChannel(0, true);
                result.y = GetChannel(1);
                return result;
            }
            public Vector3 ReadVector3()
            {
                Debug.Assert(stream.descriptor.stride == 3);
                Vector3 result;
                result.x = GetChannel(0, true);
                result.y = GetChannel(1);
                result.z = GetChannel(2);
                return result;
            }

            protected abstract float GetChannel(int channel, bool pump = false);
        }

        private bool CanRead => stream.Position < stream.Length;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        private struct UnionFloat32
        {
            [System.Runtime.InteropServices.FieldOffset(0)] public float asFloat;
            [System.Runtime.InteropServices.FieldOffset(0)] public int asInt;
        }

        private void WriteShort(short value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
        }
        private void WriteInt(int value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
        }
        private void WriteFloat(float value)
        {
            WriteInt((new UnionFloat32 { asFloat = value }).asInt);
        }

        private short ReadShort()
        {
            return (short)(
                stream.ReadByte() |
                (stream.ReadByte() << 8)
            );
        }
        private int ReadInt()
        {
            return (
                stream.ReadByte() |
                (stream.ReadByte() << 8) |
                (stream.ReadByte() << 16) |
                (stream.ReadByte() << 24)
            );
        }
        private float ReadFloat()
        {
            return (new UnionFloat32 { asInt = ReadInt() }).asFloat;
        }
    }
}