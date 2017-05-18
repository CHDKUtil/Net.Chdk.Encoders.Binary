using Chimp.Logging;
using Net.Chdk.Providers.Boot;

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

        public void Decode(byte[] encBuffer, byte[] decBuffer, uint offsets)
        {
            Validate(encBuffer: encBuffer, decBuffer: decBuffer, offsets: offsets);

            Logger.Log(LogLevel.Trace, "Decoding {0} with 0x{1:x}", FileName, offsets);

            for (var index = 0; index < decBuffer.Length; index++)
            {
                var offset = (int)(offsets >> ((index % OffsetLength) << OffsetShift) & (OffsetLength - 1));
                decBuffer[index] = Dance(encBuffer[(index & ~(OffsetLength - 1)) + offset], index % ChunkSize);
            }
        }
    }
}
