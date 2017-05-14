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
        private static void Decode(byte[] encBuffer, byte[] decBuffer, int start, int[] offsets)
        {
            for (var disp = 0; disp < ChunkSize; disp += offsets.Length)
                for (var index = 0; index < offsets.Length; index++)
                    decBuffer[start + disp + index] = Dance(encBuffer[start + disp + offsets[index]], disp + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Decode(byte[] encBuffer, byte[] decBuffer, int start, int size, int[] offsets)
        {
            for (var disp = 0; disp < size; disp += offsets.Length)
                for (var index = 0; index < offsets.Length; index++)
                    decBuffer[start + disp + index] = Dance(encBuffer[start + disp + offsets[index]], disp + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ValidatePrefix(byte[] encBuffer, int size)
        {
            if (size < Prefix.Length)
                return false;
            for (var i = 0; i < Prefix.Length; i++)
                if (encBuffer[i] != Prefix[i])
                    return false;
            return true;
        }
    }
}
