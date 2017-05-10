using System;
using System.IO;
using static Net.Chdk.Encoders.Binary.Utility;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryDecoder
    {
        public static int MaxVersion => Utility.MaxVersion;

        public static void Decode(Stream inStream, Stream outStream, int version)
        {
            if (inStream == null)
                throw new ArgumentNullException(nameof(inStream));
            if (outStream == null)
                throw new ArgumentNullException(nameof(outStream));
            if (version < 0 || version > MaxVersion)
                throw new ArgumentOutOfRangeException(nameof(version));

            if (version == 0)
                inStream.CopyTo(outStream);
            else
                Decode(inStream, outStream, Offsets[version - 1]);
        }

        private static void Decode(Stream inStream, Stream outStream, int[] offsets)
        {
            var inBuffer = new byte[ChunkSize];
            var outBuffer = new byte[ChunkSize];
            int size;
            inStream.ReadByte();
            while ((size = inStream.Read(inBuffer, 0, ChunkSize)) > 0)
            {
                for (var start = 0; start < size; start += offsets.Length)
                    for (var index = 0; index < offsets.Length; index++)
                        outBuffer[start + index] = Dance(inBuffer[start + offsets[index]], start + index);
                outStream.Write(outBuffer, 0, size);
            }
        }
    }
}
