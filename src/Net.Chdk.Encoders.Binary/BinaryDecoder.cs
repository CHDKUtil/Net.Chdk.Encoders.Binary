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
            var offsets = CopyOffsets(version);
            return Decode(encStream, decStream, offsets);
        }

        public bool Decode(byte[] encBuffer, byte[] decBuffer, int version)
        {
            Validate(encBuffer: encBuffer, decBuffer: decBuffer, version: version);

            if (TryCopy(encBuffer, decBuffer, version))
                return true;

            Logger.Log(LogLevel.Trace, "Decoding {0} version {1}", FileName, version);
            var offsets = CopyOffsets(version);
            return Decode(encBuffer, decBuffer, offsets);
        }

        private unsafe bool Decode(Stream encStream, Stream decStream, int[] offsets)
        {
            var encBuffer = new byte[ChunkSize];
            var decBuffer = new byte[ChunkSize];

            var size = encStream.Read(encBuffer, 0, Prefix.Length);
            if (!ValidatePrefix(encBuffer, size))
                return false;

            fixed (byte* pEncBuffer = encBuffer)
            fixed (byte* pDecBuffer = decBuffer)
            fixed (int* pOffsets = offsets)
            {
                while ((size = encStream.Read(encBuffer, 0, ChunkSize)) > 0)
                {
                    Decode(pEncBuffer, pDecBuffer, 0, size, pOffsets);
                    decStream.Write(decBuffer, 0, size);
                }
            }

            return true;
        }

        private unsafe bool Decode(byte[] encBuffer, byte[] decBuffer, int[] offsets)
        {
            var prefixLength = Prefix.Length;
            var bufferLength = encBuffer.Length;

            if (!ValidatePrefix(encBuffer, bufferLength))
                return false;

            fixed (byte* pEncBuffer = encBuffer)
            fixed (byte* pDecBuffer = decBuffer)
            fixed (int* pOffsets = offsets)
            {
                for (var start = prefixLength; start < bufferLength + ChunkSize; start += ChunkSize)
                {
                    if (start <= bufferLength - ChunkSize)
                        Decode(pEncBuffer, pDecBuffer, start, pOffsets);
                    else
                        Decode(pEncBuffer, pDecBuffer, start, bufferLength - start, pOffsets);
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Decode(byte* encBuffer, byte* decBuffer, int start, int* offsets)
        {
            for (var disp = 0; disp < ChunkSize; disp += OffsetLength)
                for (var index = 0; index < OffsetLength; index++)
                    decBuffer[start + disp + index] = Dance(encBuffer[start + disp + offsets[index]], disp + index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Decode(byte* encBuffer, byte* decBuffer, int start, int size, int* offsets)
        {
            for (var disp = 0; disp < size; disp += OffsetLength)
                for (var index = 0; index < OffsetLength; index++)
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
