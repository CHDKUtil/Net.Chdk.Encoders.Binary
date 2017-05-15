using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryEncoder : BinaryEncoderDecoder, IBinaryEncoder
    {
        public BinaryEncoder(IBootProvider bootProvider, ILoggerFactory loggerFactory)
            : base(bootProvider, loggerFactory.CreateLogger<BinaryEncoder>())
        {
        }

        public void Encode(Stream decStream, Stream encStream, ulong? offsets)
        {
            Validate(encStream: encStream, decStream: decStream, offsets: offsets);

            if (TryCopy(decStream, encStream, offsets))
                return;

            Logger.Log(LogLevel.Trace, "Encoding {0} with 0x{1:x}", FileName, offsets);
            Encode(decStream, encStream, offsets.Value);
        }

        public void Encode(byte[] decBuffer, byte[] encBuffer, ulong[] ulBuffer, ulong? offsets)
        {
            Validate(decBuffer: decBuffer, encBuffer: encBuffer, offsets: offsets);

            if (TryCopy(decBuffer, encBuffer, offsets))
                return;

            Logger.Log(LogLevel.Trace, "Encoding {0} with 0x{1:x}", FileName, offsets);
            Encode(decBuffer, encBuffer, ulBuffer, offsets.Value);
        }

        private unsafe void Encode(Stream decStream, Stream encStream, ulong offsets)
        {
            var decBuffer = new byte[ChunkSize];
            var encBuffer = new byte[ChunkSize];

            encStream.Write(Prefix, 0, Prefix.Length);

            fixed (byte* pDecBuffer = decBuffer)
            fixed (byte* pEncBuffer = encBuffer)
            {
                ulong* uDecBuffer = (ulong*)pDecBuffer;
                ulong* uEncBuffer = (ulong*)pEncBuffer;
                int size;
                while ((size = decStream.Read(decBuffer, 0, ChunkSize)) > 0)
                {
                    Encode(uDecBuffer, uEncBuffer, 0, size, offsets);
                    encStream.Write(encBuffer, 0, size);
                }
            }
        }

        private unsafe void Encode(byte[] decBuffer, byte[] encBuffer, ulong[] ulBuffer, ulong offsets)
        {
            var prefixLength = Prefix.Length;
            var bufferLength = decBuffer.Length;

            for (var i = 0; i < prefixLength; i++)
                encBuffer[i] = Prefix[i];

            fixed (ulong* pDecBuffer = ulBuffer)
            fixed (ulong* pEncBuffer = &ulBuffer[ChunkSize / OffsetLength])
            {
                var start = prefixLength;
                while (start <= bufferLength - ChunkSize)
                {
                    Encode(decBuffer, encBuffer, pDecBuffer, pEncBuffer, start, offsets);
                    start += ChunkSize;
                }
                Encode(decBuffer, encBuffer, pDecBuffer, pEncBuffer, start, bufferLength - start, offsets);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Encode(byte[] decBuffer, byte[] encBuffer, ulong* pDecBuffer, ulong* pEncBuffer, int start, ulong offsets)
        {
            Marshal.Copy(decBuffer, start, new IntPtr((void*)pDecBuffer), ChunkSize);
            Encode(pDecBuffer, pEncBuffer, start, offsets);
            Marshal.Copy(new IntPtr((void*)pEncBuffer), encBuffer, start, ChunkSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Encode(byte[] decBuffer, byte[] encBuffer, ulong* pDecBuffer, ulong* pEncBuffer, int start, int size, ulong offsets)
        {
            Marshal.Copy(decBuffer, start, new IntPtr((void*)pDecBuffer), size);
            Encode(pDecBuffer, pEncBuffer, start, size, offsets);
            Marshal.Copy(new IntPtr((void*)pEncBuffer), encBuffer, start, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Encode(ulong* pDecBuffer, ulong* pEncBuffer, int start, ulong offsets)
        {
            for (var disp = 0; disp < ChunkSize / OffsetLength; disp++)
                EncodeRun(pDecBuffer, pEncBuffer, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Encode(ulong* pDecBuffer, ulong* pEncBuffer, int start, int size, ulong offsets)
        {
            for (var disp = 0; disp < size / OffsetLength; disp++)
                EncodeRun(pDecBuffer, pEncBuffer, disp, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void EncodeRun(ulong* decBuffer, ulong* encBuffer, int disp, ulong offsets)
        {
            var dec = decBuffer[disp];
            var enc = 0ul;
            for (var index = 0; index < OffsetLength; index++)
            {
                var offset = (int)(offsets >> (index << OffsetShift) & (OffsetLength - 1));
                enc += ((ulong)(Dance((byte)(dec >> (index << OffsetShift)), (disp << OffsetShift) + index)) << (offset << OffsetShift));
            }
            encBuffer[disp] = enc;
        }
    }
}
