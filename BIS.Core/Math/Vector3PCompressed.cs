using BIS.Core.Streams;

namespace BIS.Core.Math
{

    public class Vector3PCompressed
    {
        private int value;
        private const float scaleFactor = -1.0f / 511.0f;

        public float X
        {
            get
            {
                int x = value & 0x3FF;
                if (x > 511) x -= 1024;
                return x * scaleFactor;
            }
        }

        public float Y
        {
            get
            {
                int y = (value >> 10) & 0x3FF;
                if (y > 511) y -= 1024;
                return y * scaleFactor;
            }
        }

        public float Z
        {
            get
            {
                int z = (value >> 20) & 0x3FF;
                if (z > 511) z -= 1024;
                return z * scaleFactor;
            }
        }

        public static implicit operator Vector3P(Vector3PCompressed src)
        {
            int x = src.value & 0x3FF;
            int y = (src.value >> 10) & 0x3FF;
            int z = (src.value >> 20) & 0x3FF;
            if (x > 511) x -= 1024;
            if (y > 511) y -= 1024;
            if (z > 511) z -= 1024;

            return new Vector3P(x * scaleFactor, y * scaleFactor, z * scaleFactor);
        }

        public static implicit operator int(Vector3PCompressed src)
        {
            return src.value;
        }

        public static implicit operator Vector3PCompressed(int src)
        {
            return new Vector3PCompressed(src);
        }

        public Vector3PCompressed(int value)
        {
            this.value = value;
        }
        public Vector3PCompressed(BinaryReaderEx input)
        {
            value = input.ReadInt32();
        }
    }
}
