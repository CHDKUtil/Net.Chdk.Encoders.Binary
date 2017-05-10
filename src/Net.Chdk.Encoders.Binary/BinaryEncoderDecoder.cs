using Net.Chdk.Providers.Boot;
using System;
using System.IO;

namespace Net.Chdk.Encoders.Binary
{
    public abstract class BinaryEncoderDecoder
    {
        protected const int ChunkSize = 0x400;

        private IBootProvider BootProvider { get; }

        public BinaryEncoderDecoder(IBootProvider bootProvider)
        {
            BootProvider = bootProvider;
        }

        public int MaxVersion => Offsets.Length;

        protected int[][] Offsets => BootProvider.Offsets;

        protected byte[] Prefix => BootProvider.Prefix;

        protected void Validate(Stream inStream, Stream outStream, int version)
        {
            if (inStream == null)
                throw new ArgumentNullException(nameof(inStream));
            if (outStream == null)
                throw new ArgumentNullException(nameof(outStream));
            if (version < 0 || version > MaxVersion)
                throw new ArgumentOutOfRangeException(nameof(version));
        }

        protected static byte Dance(byte input, int index)
        {
            if ((index % 3) != 0)
                return (byte)(input ^ 0xff);
            if ((index % 2) == 0)
                return (byte)(input ^ 0xa0);
            return (byte)((byte)(input >> 4) | (byte)(input << 4));
        }
    }
}
