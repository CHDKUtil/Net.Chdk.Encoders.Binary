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

        public bool Decode(Stream encStream, Stream decStream, byte[] encBuffer, byte[] decBuffer, ulong? offsets)
        {
            Validate(encStream: encStream, decStream: decStream, offsets: offsets);

            if (TryCopy(encStream, decStream, offsets))
                return true;

            Logger.Log(LogLevel.Trace, "Decoding {0} with 0x{1:x}", FileName, offsets);
            return Decode(encStream, decStream, encBuffer, decBuffer, offsets.Value);
        }

        public bool Decode(byte[] encBuffer, byte[] decBuffer, ulong[] ulBuffer, ulong? offsets)
        {
            Validate(encBuffer: encBuffer, decBuffer: decBuffer, offsets: offsets);

            if (TryCopy(encBuffer, decBuffer, offsets))
                return true;

            Logger.Log(LogLevel.Trace, "Decoding {0} with 0x{1:x}", FileName, offsets);
            return Decode(encBuffer, decBuffer, ulBuffer, offsets.Value);
        }

        private unsafe bool Decode(Stream encStream, Stream decStream, byte[] encBuffer, byte[] decBuffer, ulong offsets)
        {
            var size = encStream.Read(encBuffer, 0, Prefix.Length);
            if (!ValidatePrefix(encBuffer, size))
                return false;

            fixed (byte* pEncBuffer = encBuffer)
            fixed (byte* pDecBuffer = decBuffer)
            {
                ulong* uEncBuffer = (ulong*)pEncBuffer;
                ulong* uDecBuffer = (ulong*)pDecBuffer;
                while ((size = encStream.Read(encBuffer, 0, ChunkSize)) > 0)
                {
                    Decode(uEncBuffer, uDecBuffer, size, offsets);
                    decStream.Write(decBuffer, 0, size);
                }
            }

            return true;
        }

        private unsafe bool Decode(byte[] encBuffer, byte[] decBuffer, ulong[] ulBuffer, ulong offsets)
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

        private unsafe void Decode(byte[] encBuffer, byte[] decBuffer, ulong* pEncBuffer, ulong* pDecBuffer, int start, ulong offsets)
        {
            Marshal.Copy(encBuffer, start, new IntPtr((void*)pEncBuffer), ChunkSize);
            Decode(pEncBuffer, pDecBuffer, start, offsets);
            Marshal.Copy(new IntPtr((void*)pDecBuffer), decBuffer, start, ChunkSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Decode(byte[] encBuffer, byte[] decBuffer, ulong* pEncBuffer, ulong* pDecBuffer, int start, int size, ulong offsets)
        {
            Marshal.Copy(encBuffer, start, new IntPtr((void*)pEncBuffer), size);
            Decode(pEncBuffer, pDecBuffer, start, size, offsets);
            Marshal.Copy(new IntPtr((void*)pDecBuffer), decBuffer, start, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Decode(ulong* encBuffer, ulong* decBuffer, int start, ulong offsets)
        {
            for (var disp = 0; disp < ChunkSize / OffsetLength; disp++)
                DecodeRun(encBuffer, decBuffer, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Decode(ulong* encBuffer, ulong* decBuffer, int start, int size, ulong offsets)
        {
            for (var disp = 0; disp < size / OffsetLength; disp++)
                DecodeRun(encBuffer, decBuffer, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void DecodeRun(ulong* encBuffer, ulong* decBuffer, int disp, ulong offsets)
        {
            var enc = encBuffer[disp];
            var dec = 0ul;
            for (var index = 0; index < OffsetLength; index++)
            {
                var offset = (int)(offsets >> (index << OffsetShift) & (OffsetLength - 1));
                dec += ((ulong)(Dance((byte)(enc >> (offset << OffsetShift)), (disp << OffsetShift) + index)) << (index << OffsetShift));
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
