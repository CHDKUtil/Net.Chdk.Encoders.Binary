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

        public void Encode(byte[] decBuffer, byte[] encBuffer, int version)
        {
            Validate(decBuffer: decBuffer, encBuffer: encBuffer, version: version);

            if (TryCopy(decBuffer, encBuffer, version))
                return;

            Logger.Log(LogLevel.Trace, "Encoding {0} version {1}", FileName, version);
            var offsets = CopyOffsets(version);
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
                int size;
                while ((size = decStream.Read(decBuffer, 0, ChunkSize)) > 0)
                {
                    Encode(pDecBuffer, pEncBuffer, 0, size, offsets);
                    encStream.Write(encBuffer, 0, size);
                }
            }
        }

        private unsafe void Encode(byte[] decBuffer, byte[] encBuffer, int[] offsets)
        {
            var prefixLength = Prefix.Length;
            var bufferLength = decBuffer.Length;

            for (var i = 0; i < prefixLength; i++)
                encBuffer[i] = Prefix[i];

            fixed (byte* pDecBuffer = decBuffer)
            fixed (byte* pEncBuffer = encBuffer)
            {
                for (var start = prefixLength; start < bufferLength + ChunkSize; start += ChunkSize)
                {
                    if (start <= bufferLength - ChunkSize)
                        Encode(pDecBuffer, pEncBuffer, start, offsets);
                    else
                        Encode(pDecBuffer, pEncBuffer, start, bufferLength - start, offsets);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Encode(byte* decBuffer, byte* encBuffer, int start, int[] offsets)
        {
            for (var disp = 0; disp < ChunkSize; disp += offsets.Length)
                for (var index = 0; index < offsets.Length; index++)
                    encBuffer[start + disp + offsets[index]] = Dance(decBuffer[start + disp + index], disp + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Encode(byte* decBuffer, byte* encBuffer, int start, int size, int[] offsets)
        {
            for (var disp = 0; disp < size; disp += offsets.Length)
                for (var index = 0; index < offsets.Length; index++)
                    encBuffer[start + disp + offsets[index]] = Dance(decBuffer[start + disp + index], disp + index);
        }
    }
}
