using System.IO;
using UnityEngine;

namespace Replay
{
    public partial class ReplayStream
    {
        public enum DataType
        {
            Float32,
            Quant16,
            QuantAuto,
            QuantDelta,
        }
        public enum InterpolationType
        {
            Linear,
            Angular,
            None,
        }

        [System.Serializable]
        public struct Descriptor
        {
            [Tooltip("Must be unique within this gameobject")]
            public string name;
            [Tooltip("Stream type\nFloat32 - raw 32-bit absolute\nQuant16 - quantised 16-bit absolute\nQuantAuto - quantised 6/14/22/30-bit absolute\nQuantDelta - quantised 6/14/22/30-bit delta")]
            public DataType dataType;
            [Tooltip("Number of values per sample\n1: float\n2: Vector2\n3: Vector3\netc...")]
            public int stride;
            [Tooltip("Quantisation scale\nValues will be multiplied by this then rounded to the nearest integer\nThis is ignored for non-quantised types")]
            public int quantise;
            [Tooltip("Number of raw samples per recorded keyframe")]
            public int keyframeInterval;
            [Tooltip("Interpolation method used to reconstruct samples between keyframes")]
            public InterpolationType interpolation;
        }

        [System.Serializable]
        internal struct Serialised
        {
            public Descriptor descriptor;
            public string data;
        }

        public readonly Descriptor descriptor;
        private MemoryStream dataStream;
        private Writer activeWriter;

        private bool CanRead => dataStream.Position < dataStream.Length;

        internal ReplayStream(in Descriptor descriptor, bool asReadOnly = false)
        {
            this.descriptor = descriptor;
            if (asReadOnly)
                dataStream = new(new byte[0], false);
            else
                dataStream = new();
        }
        internal ReplayStream(in Serialised serialised)
        {
            descriptor = serialised.descriptor;
            byte[] bytes = System.Convert.FromBase64String(serialised.data);
            bytes = Decompress(bytes);
            dataStream = new(bytes, false);
        }

        internal Serialised Serialise()
        {
            activeWriter?.Finish();

            byte[] bytes = dataStream.ToArray();
            bytes = Compress(bytes);
            string base64 = System.Convert.ToBase64String(bytes);

            return new Serialised { descriptor = descriptor, data = base64 };
        }

        private byte[] Compress(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new System.IO.Compression.GZipStream(mso, System.IO.Compression.CompressionLevel.Optimal))
                //using (var gs = new Ionic.Zlib.GZipStream(mso, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestCompression))
                {
                    msi.CopyTo(gs);
                }
                return mso.ToArray();
            }
        }
        private byte[] Decompress(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new System.IO.Compression.GZipStream(msi, System.IO.Compression.CompressionMode.Decompress))
                //using (var gs = new Ionic.Zlib.GZipStream(mso, Ionic.Zlib.CompressionMode.Compress, Ionic.Zlib.CompressionLevel.BestCompression))
                {
                    gs.CopyTo(mso);
                }
                return mso.ToArray();
            }
        }

        public Writer GetWriter()
        {
            Debug.Assert(activeWriter == null);
            Debug.Assert(dataStream.CanWrite);            
            switch (descriptor.dataType)
            {
                case DataType.Float32:
                    activeWriter = new WriterFloat32(this);
                    break;
                case DataType.Quant16:
                    activeWriter = new WriterQuant16(this);
                    break;
                case DataType.QuantAuto:
                    activeWriter = new WriterQuantAuto(this);
                    break;
                case DataType.QuantDelta:
                    activeWriter = new WriterQuantDelta(this);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
            return activeWriter;
        }
        public Reader GetReader()
        {
            Debug.Assert(activeWriter == null);
            switch (descriptor.dataType)
            {
                case DataType.Float32:
                    return new ReaderFloat32(this);
                case DataType.Quant16:
                    return new ReaderQuant16(this);
                case DataType.QuantAuto:
                    return new ReaderQuantAuto(this);
                case DataType.QuantDelta:
                    return new ReaderQuantDelta(this);
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}