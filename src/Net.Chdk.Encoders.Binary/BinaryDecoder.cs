using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System.IO;
using System.Runtime.CompilerServices;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryDecoder : BinaryEncoderDecoder, IBinaryDecoder
    {
        private const int OffsetLength = 8;
        private const int OffsetShift = 2;
        private const int BufferShift = 3;
        private const int ChunkSize = 0x400;

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
            Validate(encStream: encStream, decStream: decStream, encBuffer: encBuffer, decBuffer: decBuffer, offsets: offsets);

            if (TryCopy(encStream, decStream, offsets))
                return true;

            Logger.Log(LogLevel.Trace, "Decoding {0} with 0x{1:x}", FileName, offsets);
            return Decode(encStream, decStream, encBuffer, decBuffer, offsets.Value);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DecodeChunk(byte[] encBuffer, byte[] decBuffer, uint offsets)
        {
            for (var index = 0; index < decBuffer.Length; index++)
            {
                var offset = (int)(offsets >> ((index % OffsetLength) << OffsetShift) & (OffsetLength - 1));
                decBuffer[index] = Dance(encBuffer[(index & ~(OffsetLength - 1)) + offset], index);
            }
        }
    }
}
