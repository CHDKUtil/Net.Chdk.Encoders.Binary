using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System.IO;
using System.Runtime.CompilerServices;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryEncoder : BinaryEncoderDecoder, IBinaryEncoder
    {
        public BinaryEncoder(IBootProvider bootProvider, ILoggerFactory loggerFactory)
            : base(bootProvider, loggerFactory.CreateLogger<BinaryEncoder>())
        {
        }

        public void Encode(Stream decStream, Stream encStream, int version)
        {
            Validate(encStream: encStream, decStream: decStream, version: version);

            if (TryCopy(decStream, encStream, version))
                return;

            Logger.Log(LogLevel.Trace, "Encoding {0} version {1}", FileName, version);
            var offsets = CopyOffsets(version);
            Encode(decStream, encStream, offsets);
        }

        public void Encode(byte[] decBuffer, byte[] encBuffer, ulong? offsets)
        {
            Validate(decBuffer: decBuffer, encBuffer: encBuffer, offsets: offsets);

            if (TryCopy(decBuffer, encBuffer, offsets))
                return;

            Logger.Log(LogLevel.Trace, "Encoding {0} with 0x{1:x}", FileName, offsets);
            Encode(decBuffer, encBuffer, offsets);
        }

        private unsafe void Encode(Stream decStream, Stream encStream, int[] offsets)
        {
            var decBuffer = new byte[ChunkSize];
            var encBuffer = new byte[ChunkSize];

            encStream.Write(Prefix, 0, Prefix.Length);

            fixed (byte* pDecBuffer = decBuffer)
            fixed (byte* pEncBuffer = encBuffer)
            {
                var uOffsets = GetOffsets(offsets);
                int size;
                while ((size = decStream.Read(decBuffer, 0, ChunkSize)) > 0)
                {
                    Encode(pDecBuffer, pEncBuffer, 0, size, uOffsets);
                    encStream.Write(encBuffer, 0, size);
                }
            }
        }

        private unsafe void Encode(byte[] decBuffer, byte[] encBuffer, ulong offsets)
        {
            var prefixLength = Prefix.Length;
            var bufferLength = decBuffer.Length;

            for (var i = 0; i < prefixLength; i++)
                encBuffer[i] = Prefix[i];

            fixed (byte* pDecBuffer = decBuffer)
            fixed (byte* pEncBuffer = encBuffer)
            {
                var start = prefixLength;
                while (start <= bufferLength - ChunkSize)
                {
                    Encode(pDecBuffer, pEncBuffer, start, offsets);
                    start += ChunkSize;
                }
                Encode(pDecBuffer, pEncBuffer, start, bufferLength - start, offsets);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Encode(byte* decBuffer, byte* encBuffer, int start, ulong offsets)
        {
            for (var disp = 0; disp < ChunkSize; disp += OffsetLength)
                EncodeRun(decBuffer, encBuffer, start, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Encode(byte* decBuffer, byte* encBuffer, int start, int size, ulong offsets)
        {
            for (var disp = 0; disp < size; disp += OffsetLength)
                EncodeRun(decBuffer, encBuffer, start, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void EncodeRun(byte* decBuffer, byte* encBuffer, int start, int disp, ulong offsets)
        {
            for (var index = 0; index < OffsetLength; index++)
            {
                var offset = (int)(offsets >> (index << OffsetShift) & (OffsetLength - 1));
                encBuffer[start + disp + offset] = Dance(decBuffer[start + disp + index], disp + index);
            }
        }
    }
}
