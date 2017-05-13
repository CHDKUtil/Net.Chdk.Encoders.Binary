using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System.IO;
using System.Linq;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryDecoder : BinaryEncoderDecoder, IBinaryDecoder
    {
        public BinaryDecoder(IBootProvider bootProvider, ILoggerFactory loggerFactory)
            : base(bootProvider, loggerFactory.CreateLogger<BinaryDecoder>())
        {
        }

        public bool Decode(Stream encStream, Stream decStream, int version)
        {
            Validate(encStream: encStream, decStream: decStream, version: version);

            if (TryCopy(encStream, decStream, version))
                return true;

            Logger.Log(LogLevel.Trace, "Decoding {0} version {1}", FileName, version);
            return Decode(encStream, decStream, Offsets[version - 1]);
        }

        public bool Decode(byte[] encBuffer, byte[] decBuffer, int version)
        {
            Validate(encBuffer: encBuffer, decBuffer: decBuffer, version: version);

            if (TryCopy(encBuffer, decBuffer, version))
                return true;

            Logger.Log(LogLevel.Trace, "Decoding {0} version {1}", FileName, version);
            return Decode(encBuffer, decBuffer, Offsets[version - 1]);
        }

        private bool Decode(Stream encStream, Stream decStream, int[] offsets)
        {
            var encBuffer = new byte[ChunkSize];
            var decBuffer = new byte[ChunkSize];

            var size = encStream.Read(encBuffer, 0, Prefix.Length);
            if (!ValidatePrefix(encBuffer, size))
                return false;

            while ((size = encStream.Read(encBuffer, 0, ChunkSize)) > 0)
            {
                Decode(encBuffer, decBuffer, 0, size, offsets);
                decStream.Write(decBuffer, 0, size);
            }

            return true;
        }

        private bool Decode(byte[] encBuffer, byte[] decBuffer, int[] offsets)
        {
            var prefixLength = Prefix.Length;
            var bufferLength = encBuffer.Length;

            if (!ValidatePrefix(encBuffer, bufferLength))
                return false;

            int size;
            for (var start = prefixLength; start < bufferLength + ChunkSize; start += ChunkSize)
            {
                size = ChunkSize <= bufferLength - start ? ChunkSize : bufferLength - start;
                Decode(encBuffer, decBuffer, start, size, offsets);
            }

            return true;
        }

        private static void Decode(byte[] encBuffer, byte[] decBuffer, int start, int size, int[] offsets)
        {
            for (var start0 = start; start < start0 + size; start += OffsetLength)
                for (var index = 0; index < OffsetLength; index++)
                    decBuffer[start + index] = Dance(encBuffer[start + offsets[index]], start + index - start0);
        }

        private bool ValidatePrefix(byte[] encBuffer, int size)
        {
            return !(size < Prefix.Length || Enumerable.Range(0, Prefix.Length).Any(i => encBuffer[i] != Prefix[i]));
        }
    }
}
