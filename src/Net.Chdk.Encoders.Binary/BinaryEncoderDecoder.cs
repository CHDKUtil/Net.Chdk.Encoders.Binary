using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System;
using System.IO;

namespace Net.Chdk.Encoders.Binary
{
    public abstract class BinaryEncoderDecoder
    {
        protected const int OffsetLength = 8;
        protected const int ChunkSize = 0x400;

        protected ILogger Logger { get; }

        private IBootProvider BootProvider { get; }

        public BinaryEncoderDecoder(IBootProvider bootProvider, ILogger logger)
        {
            BootProvider = bootProvider;
            Logger = logger;
        }

        public int MaxVersion => Offsets.Length;

        protected int[][] Offsets => BootProvider.Offsets;

        protected byte[] Prefix => BootProvider.Prefix;

        protected string FileName => BootProvider.FileName;

        protected void Validate(Stream inStream, Stream outStream, int version)
        {
            if (inStream == null)
                throw new ArgumentNullException(nameof(inStream));
            if (outStream == null)
                throw new ArgumentNullException(nameof(outStream));
            if (version < 0 || version > MaxVersion)
                throw new ArgumentOutOfRangeException(nameof(version));
        }

        protected void Validate(byte[] decBuffer, byte[] encBuffer, int version)
        {
            if (decBuffer == null)
                throw new ArgumentNullException(nameof(decBuffer));
            if (encBuffer == null)
                throw new ArgumentNullException(nameof(encBuffer));
            if (version < 0 || version > MaxVersion)
                throw new ArgumentOutOfRangeException(nameof(version));
        }

        protected bool TryCopy(Stream inStream, Stream outStream, int version)
        {
            if (version == 0)
            {
                Logger.Log(LogLevel.Trace, "Copying {0} contents", FileName);
                inStream.CopyTo(outStream);
                return true;
            }
            return false;
        }

        protected bool TryCopy(byte[] inBuffer, byte[] outBuffer, int version)
        {
            if (version == 0)
            {
                Logger.Log(LogLevel.Trace, "Copying {0} contents", FileName);
                Array.Copy(inBuffer, outBuffer, outBuffer.Length);
                return true;
            }
            return false;
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
