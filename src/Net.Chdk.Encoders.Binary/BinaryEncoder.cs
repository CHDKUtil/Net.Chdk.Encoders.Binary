using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System.IO;

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
            Encode(decStream, encStream, Offsets[version - 1]);
        }

        public void Encode(byte[] decBuffer, byte[] encBuffer, int version)
        {
            Validate(decBuffer: decBuffer, encBuffer: encBuffer, version: version);

            if (TryCopy(decBuffer, encBuffer, version))
                return;

            Logger.Log(LogLevel.Trace, "Encoding {0} version {1}", FileName, version);
            Encode(decBuffer, encBuffer, Offsets[version - 1]);
        }

        private void Encode(Stream decStream, Stream encStream, int[] offsets)
        {
            var decBuffer = new byte[ChunkSize];
            var encBuffer = new byte[ChunkSize];

            encStream.Write(Prefix, 0, Prefix.Length);

            int size;
            while ((size = decStream.Read(decBuffer, 0, ChunkSize)) > 0)
            {
                Encode(decBuffer, encBuffer, 0, size, offsets);
                encStream.Write(encBuffer, 0, size);
            }
        }

        private void Encode(byte[] decBuffer, byte[] encBuffer, int[] offsets)
        {
            var prefixLength = Prefix.Length;
            var bufferLength = decBuffer.Length;

            for (var i = 0; i < prefixLength; i++)
                encBuffer[i] = Prefix[i];

            int size;
            for (var start = prefixLength; start < bufferLength + ChunkSize; start += ChunkSize)
            {
                size = ChunkSize <= bufferLength - start ? ChunkSize : bufferLength - start;
                Encode(decBuffer, encBuffer, start, size, offsets);
            }
        }

        private static void Encode(byte[] decBuffer, byte[] encBuffer, int start, int size, int[] offsets)
        {
            for (var start0 = start; start < start0 + size; start += OffsetLength)
                for (var index = 0; index < OffsetLength; index++)
                    encBuffer[start + offsets[index]] = Dance(decBuffer[start + index], start + index - start0);
        }
    }
}
