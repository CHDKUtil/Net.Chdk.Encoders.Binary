using System;
using System.IO;
using System.Linq;
using static Net.Chdk.Encoders.Binary.Utility;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryDecoder
    {
        public static int MaxVersion => Utility.MaxVersion;

        public static bool Decode(Stream inStream, Stream outStream, int version)
        {
            if (inStream == null)
                throw new ArgumentNullException(nameof(inStream));
            if (outStream == null)
                throw new ArgumentNullException(nameof(outStream));
            if (version < 0 || version > MaxVersion)
                throw new ArgumentOutOfRangeException(nameof(version));

            if (version == 0)
            {
                inStream.CopyTo(outStream);
                return true;
            }

            return Decode(inStream, outStream, Offsets[version - 1]);
        }

        private static bool Decode(Stream inStream, Stream outStream, int[] offsets)
        {
            var inBuffer = new byte[ChunkSize];
            var outBuffer = new byte[ChunkSize];

            var length = Prefix.Length;
            var size = inStream.Read(inBuffer, 0, length);
            if (size < length || Enumerable.Range(0, length).Any(i => inBuffer[i] != Prefix[i]))
                return false;

            while ((size = inStream.Read(inBuffer, 0, ChunkSize)) > 0)
            {
                for (var start = 0; start < size; start += offsets.Length)
                    for (var index = 0; index < offsets.Length; index++)
                        outBuffer[start + index] = Dance(inBuffer[start + offsets[index]], start + index);
                outStream.Write(outBuffer, 0, size);
            }

            return true;
        }
    }
}
