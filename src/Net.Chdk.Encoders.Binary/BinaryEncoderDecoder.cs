using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Net.Chdk.Encoders.Binary
{
    public abstract class BinaryEncoderDecoder
    {
        protected const int OffsetLength = 8;
        protected const int OffsetShift = 3;
        protected const int ChunkSize = 0x400;

        protected ILogger Logger { get; }

        private IBootProvider BootProvider { get; }

        public BinaryEncoderDecoder(IBootProvider bootProvider, ILogger logger)
        {
            BootProvider = bootProvider;
            Logger = logger;
        }

        public int MaxVersion => Offsets.Length;

        private int[][] Offsets => BootProvider.Offsets;

        protected byte[] Prefix => BootProvider.Prefix;

        protected string FileName => BootProvider.FileName;

        protected void Validate(Stream decStream, Stream encStream, ulong? offsets)
        {
            if (decStream == null)
                throw new ArgumentNullException(nameof(decStream));
            if (encStream == null)
                throw new ArgumentNullException(nameof(encStream));
        }

        protected void Validate(byte[] decBuffer, byte[] encBuffer, ulong? offsets)
        {
            if (decBuffer == null)
                throw new ArgumentNullException(nameof(decBuffer));
            if (encBuffer == null)
                throw new ArgumentNullException(nameof(encBuffer));
        }

        protected bool TryCopy(Stream inStream, Stream outStream, ulong? offsets)
        {
            if (offsets == null)
            {
                Logger.Log(LogLevel.Trace, "Copying {0} contents", FileName);
                inStream.CopyTo(outStream);
                return true;
            }
            return false;
        }

        protected bool TryCopy(byte[] inBuffer, byte[] outBuffer, ulong? offsets)
        {
            if (offsets == null)
            {
                Logger.Log(LogLevel.Trace, "Copying {0} contents", FileName);
                Array.Copy(inBuffer, outBuffer, outBuffer.Length);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ulong GetOffsets(int[] offsets)
        {
            var uOffsets = 0ul;
            for (var index = 0; index < offsets.Length; index++)
                uOffsets += (ulong)offsets[index] << (index << OffsetShift);
            return uOffsets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
