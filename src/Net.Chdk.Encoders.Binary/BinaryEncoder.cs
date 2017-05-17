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
            Validate(encStream: encStream, decStream: decStream, decBuffer: decBuffer, encBuffer: encBuffer, offsets: offsets);

            if (TryCopy(decStream, encStream, offsets))
                return;

            Logger.Log(LogLevel.Trace, "Encoding {0} with 0x{1:x}", FileName, offsets);
            Encode(decStream, encStream, decBuffer, encBuffer, offsets.Value);
        }

        private void Encode(Stream decStream, Stream encStream, byte[] decBuffer, byte[] encBuffer, uint offsets)
        {
            encStream.Write(Prefix, 0, Prefix.Length);

            int size;
            while ((size = decStream.Read(decBuffer, 0, ChunkSize)) > 0)
            {
                Encode(decBuffer, encBuffer, offsets);
                encStream.Write(encBuffer, 0, size);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Encode(byte[] decBuffer, byte[] encBuffer, uint offsets)
        {
            for (var index = 0; index < decBuffer.Length; index++)
            {
                var offset = (int)(offsets >> ((index % OffsetLength) << OffsetShift) & (OffsetLength - 1));
                encBuffer[(index & ~(OffsetLength - 1)) + offset] = Dance(decBuffer[index], index);
            }
        }
    }
}
