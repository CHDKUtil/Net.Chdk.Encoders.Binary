using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System.IO;
using System.Runtime.CompilerServices;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryEncoder : BinaryEncoderDecoder, IBinaryEncoder
    {
        private const int OffsetLength = 8;
        private const int OffsetShift = 2;
        private const int ChunkSize = 0x400;

        public BinaryEncoder(IBootProvider bootProvider, ILoggerFactory loggerFactory)
            : base(bootProvider, loggerFactory.CreateLogger<BinaryEncoder>())
        {
        }

        public void Encode(Stream decStream, Stream encStream, byte[] decBuffer, byte[] encBuffer, uint? offsets)
        {
            Validate(encStream: encStream, decStream: decStream, offsets: offsets);

            if (TryCopy(decStream, encStream, offsets))
                return;

            Logger.Log(LogLevel.Trace, "Encoding {0} with 0x{1:x}", FileName, offsets);
            Encode(decStream, encStream, decBuffer, encBuffer, offsets.Value);
        }

        public void Encode(byte[] decBuffer, byte[] encBuffer, uint? offsets)
        {
            Validate(decBuffer: decBuffer, encBuffer: encBuffer, offsets: offsets);

            if (TryCopy(decBuffer, encBuffer, offsets))
                return;

            Logger.Log(LogLevel.Trace, "Encoding {0} with 0x{1:x}", FileName, offsets);
            Encode(decBuffer, encBuffer, offsets.Value);
        }

        private void Encode(Stream decStream, Stream encStream, byte[] decBuffer, byte[] encBuffer, uint offsets)
        {
            encStream.Write(Prefix, 0, Prefix.Length);

            int size;
            while ((size = decStream.Read(decBuffer, 0, ChunkSize)) > 0)
            {
                Encode(decBuffer, encBuffer, 0, size, offsets);
                encStream.Write(encBuffer, 0, size);
            }
        }

        private void Encode(byte[] decBuffer, byte[] encBuffer, uint offsets)
        {
            var prefixLength = Prefix.Length;
            var bufferLength = decBuffer.Length;

            for (var i = 0; i < prefixLength; i++)
                encBuffer[i] = Prefix[i];

            var start = prefixLength;
            while (start <= bufferLength - ChunkSize)
            {
                Encode(decBuffer, encBuffer, start, offsets);
                start += ChunkSize;
            }
            Encode(decBuffer, encBuffer, start, bufferLength - start, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Encode(byte[] decBuffer, byte[] encBuffer, int start, uint offsets)
        {
            for (var disp = 0; disp < ChunkSize; disp += OffsetLength)
                EncodeRun(decBuffer, encBuffer, start, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Encode(byte[] decBuffer, byte[] encBuffer, int start, int size, uint offsets)
        {
            for (var disp = 0; disp < size; disp += OffsetLength)
                EncodeRun(decBuffer, encBuffer, start, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeRun(byte[] decBuffer, byte[] encBuffer, int start, int disp, uint offsets)
        {
            for (var index = 0; index < OffsetLength; index++)
            {
                var offset = (int)(offsets >> (index << OffsetShift) & (OffsetLength - 1));
                encBuffer[start + disp + offset] = Dance(decBuffer[start + disp + index], disp + index);
            }
        }
    }
}
