using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

        public bool Decode(byte[] encBuffer, byte[] decBuffer, ulong[] ulBuffer, uint? offsets)
        {
            Validate(encBuffer: encBuffer, decBuffer: decBuffer, offsets: offsets);

            if (TryCopy(encBuffer, decBuffer, offsets))
                return true;

            Logger.Log(LogLevel.Trace, "Decoding {0} with 0x{1:x}", FileName, offsets);
            return Decode(encBuffer, decBuffer, ulBuffer, offsets.Value);
        }

        private unsafe bool Decode(Stream encStream, Stream decStream, int[] offsets)
        {
            var encBuffer = new byte[ChunkSize];
            var decBuffer = new byte[ChunkSize];
            var ulBuffer = new ulong[ChunkSize / OffsetLength * 2];

            var size = encStream.Read(encBuffer, 0, Prefix.Length);
            if (!ValidatePrefix(encBuffer, size))
                return false;

            fixed (ulong* pEncBuffer = ulBuffer)
            fixed (ulong* pDecBuffer = &ulBuffer[ChunkSize / OffsetLength])
            {
                var uOffsets = GetOffsets(offsets);
                while ((size = encStream.Read(encBuffer, 0, ChunkSize)) > 0)
                {
                    Decode(encBuffer, decBuffer, pEncBuffer, pDecBuffer, size, uOffsets);
                    decStream.Write(decBuffer, 0, size);
                }
            }

            return true;
        }

        private unsafe bool Decode(byte[] encBuffer, byte[] decBuffer, ulong[] ulBuffer, uint offsets)
        {
            var prefixLength = Prefix.Length;
            var bufferLength = encBuffer.Length;

            if (!ValidatePrefix(encBuffer, bufferLength))
                return false;

            fixed (ulong* pEncBuffer = ulBuffer)
            fixed (ulong* pDecBuffer = &ulBuffer[ChunkSize / OffsetLength])
            {
                var start = prefixLength;
                while (start <= bufferLength - ChunkSize)
                {
                    Decode(encBuffer, decBuffer, pEncBuffer, pDecBuffer, start, offsets);
                    start += ChunkSize;
                }
                Decode(encBuffer, decBuffer, pEncBuffer, pDecBuffer, start, bufferLength - start, offsets);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Decode(byte[] encBuffer, byte[] decBuffer, ulong* pEncBuffer, ulong* pDecBuffer, int start, uint offsets)
        {
            Marshal.Copy(encBuffer, start, new IntPtr((void*)pEncBuffer), ChunkSize);
            for (var disp = 0; disp < ChunkSize / OffsetLength; disp++)
                DecodeRun(pEncBuffer, pDecBuffer, disp, offsets);
            Marshal.Copy(new IntPtr((void*)pDecBuffer), decBuffer, start, ChunkSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Decode(byte[] encBuffer, byte[] decBuffer, ulong* pEncBuffer, ulong* pDecBuffer, int start, int size, uint offsets)
        {
            Marshal.Copy(encBuffer, start, new IntPtr((void*)pEncBuffer), size);
            for (var disp = 0; disp < size / OffsetLength; disp++)
                DecodeRun(pEncBuffer, pDecBuffer, disp, offsets);
            Marshal.Copy(new IntPtr((void*)pDecBuffer), decBuffer, start, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void DecodeRun(ulong* encBuffer, ulong* decBuffer, int disp, uint offsets)
        {
            var enc = encBuffer[disp];
            var dec = 0ul;
            for (var index = 0; index < OffsetLength; index++)
            {
                var offset = (int)(offsets >> (index << OffsetShift) & (OffsetLength - 1));
                dec += ((ulong)(Dance((byte)(enc >> (offset << BufferShift)), (disp << BufferShift) + index)) << (index << BufferShift));
            }
            decBuffer[disp] = dec;
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
