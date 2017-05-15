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
        protected const int OffsetShift = 2;
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

        protected void Validate(Stream decStream, Stream encStream, int version)
        {
            if (decStream == null)
                throw new ArgumentNullException(nameof(decStream));
            if (encStream == null)
                throw new ArgumentNullException(nameof(encStream));
            if (version < 0 || version > MaxVersion)
                throw new ArgumentOutOfRangeException(nameof(version));
        }

        protected void Validate(byte[] decBuffer, byte[] encBuffer, uint? offsets)
        {
            if (decBuffer == null)
                throw new ArgumentNullException(nameof(decBuffer));
            if (encBuffer == null)
                throw new ArgumentNullException(nameof(encBuffer));
            Validate(offsets);
        }

        private static void Validate(uint? offsets)
        {
            if (offsets == null)
                return;
            var value = offsets.Value;
            for (var i = 0; i < OffsetLength; i++)
            {
                if ((value & 0x0f) > 7)
                    throw new ArgumentOutOfRangeException(nameof(offsets));
                value >>= (1 << OffsetShift);
            }
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

        protected bool TryCopy(byte[] inBuffer, byte[] outBuffer, uint? offsets)
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
        protected static uint GetOffsets(int[] offsets)
        {
            var uOffsets = 0u;
            for (var index = 0; index < offsets.Length; index++)
                uOffsets += (uint)offsets[index] << (index << OffsetShift);
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
