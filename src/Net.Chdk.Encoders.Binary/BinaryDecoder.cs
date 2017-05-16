using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System.IO;
using System.Runtime.CompilerServices;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryDecoder : BinaryEncoderDecoder, IBinaryDecoder
    {
        public BinaryDecoder(IBootProvider bootProvider, ILoggerFactory loggerFactory)
            : base(bootProvider, loggerFactory.CreateLogger<BinaryDecoder>())
        {
        }

        public bool ValidatePrefix(byte[] encBuffer, int size)
        {
            if (size < Prefix.Length)
                return false;
            for (var i = 0; i < Prefix.Length; i++)
                if (encBuffer[i] != Prefix[i])
                    return false;
            return true;
        }

        public bool Decode(Stream encStream, Stream decStream, byte[] encBuffer, byte[] decBuffer, uint? offsets)
        {
            Validate(encStream: encStream, decStream: decStream, offsets: offsets);

            if (TryCopy(encStream, decStream, offsets))
                return true;

            Logger.Log(LogLevel.Trace, "Decoding {0} with 0x{1:x}", FileName, offsets);
            return Decode(encStream, decStream, encBuffer, decBuffer, offsets.Value);
        }

        public bool Decode(byte[] encBuffer, byte[] decBuffer, uint? offsets)
        {
            Validate(encBuffer: encBuffer, decBuffer: decBuffer, offsets: offsets);

            if (TryCopy(encBuffer, decBuffer, offsets))
                return true;

            Logger.Log(LogLevel.Trace, "Decoding {0} with 0x{1:x}", FileName, offsets);
            return Decode(encBuffer, decBuffer, offsets.Value);
        }

        private bool Decode(Stream encStream, Stream decStream, byte[] encBuffer, byte[] decBuffer, uint offsets)
        {
            var size = encStream.Read(encBuffer, 0, Prefix.Length);
            if (!ValidatePrefix(encBuffer, size))
                return false;

            while ((size = encStream.Read(encBuffer, 0, ChunkSize)) > 0)
            {
                DecodeChunk(encBuffer, decBuffer, offsets);
                decStream.Write(decBuffer, 0, size);
            }

            return true;
        }

        private bool Decode(byte[] encBuffer, byte[] decBuffer, uint offsets)
        {
            var prefixLength = Prefix.Length;
            var bufferLength = encBuffer.Length;

            if (!ValidatePrefix(encBuffer, bufferLength))
                return false;

            var start = prefixLength;
            while (start <= bufferLength - ChunkSize)
            {
                Decode(encBuffer, decBuffer, start, offsets);
                start += ChunkSize;
            }
            Decode(encBuffer, decBuffer, start, bufferLength - start, offsets);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DecodeChunk(byte[] encBuffer, byte[] decBuffer, uint offsets)
        {
            for (var index = 0; index < decBuffer.Length; index++)
            {
                var offset = (int)(offsets >> ((index % 8) << OffsetShift) & (OffsetLength - 1));
                decBuffer[index] = Dance(encBuffer[(index & ~7) + offset], index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Decode(byte[] encBuffer, byte[] decBuffer, int start, uint offsets)
        {
            for (var disp = 0; disp < ChunkSize; disp += OffsetLength)
                DecodeRun(encBuffer, decBuffer, start, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Decode(byte[] encBuffer, byte[] decBuffer, int start, int size, uint offsets)
        {
            for (var disp = 0; disp < size; disp += OffsetLength)
                DecodeRun(encBuffer, decBuffer, start, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DecodeRun(byte[] encBuffer, byte[] decBuffer, int start, int disp, uint offsets)
        {
            for (var index = 0; index < OffsetLength; index++)
            {
                var offset = (int)(offsets >> (index << OffsetShift) & (OffsetLength - 1));
                decBuffer[start + disp + index] = Dance(encBuffer[start + disp + offset], disp + index);
            }
        }
    }
}
