using Chimp.Logging;

namespace Net.Chdk.Encoders.Binary
{
    sealed class BinaryEncoder : BinaryEncoderDecoder, IBinaryEncoder
    {
        private const int OffsetLength = 8;
        private const int OffsetShift = 2;
        private const int ChunkSize = 0x400;

        public BinaryEncoder(ILoggerFactory loggerFactory)
            : base(loggerFactory.CreateLogger<BinaryEncoder>())
        {
        }

        public void Encode(byte[] decBuffer, byte[] encBuffer, uint offsets)
        {
            Validate(decBuffer: decBuffer, encBuffer: encBuffer, offsets: offsets);

            Logger.Log(LogLevel.Trace, "Encoding {0} with 0x{1:x}", FileName, offsets);

            for (var index = 0; index < decBuffer.Length; index++)
            {
                var offset = (int)(offsets >> ((index % OffsetLength) << OffsetShift) & (OffsetLength - 1));
                encBuffer[(index & ~(OffsetLength - 1)) + offset] = Dance(decBuffer[index], index % ChunkSize);
            }
        }
    }
}
