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
            Validate(inStream: inStream, outStream: outStream, version: version);

            if (version == 0)
            {
                inStream.CopyTo(outStream);
                return true;
            }

            return Decode(inStream, outStream, Offsets[version - 1]);
        }

        private static bool Decode(Stream encStream, Stream decStream, int[] offsets)
        {
            var encBuffer = new byte[ChunkSize];
            var decBuffer = new byte[ChunkSize];

            var length = Prefix.Length;
            var size = encStream.Read(encBuffer, 0, length);
            if (size < length || Enumerable.Range(0, length).Any(i => encBuffer[i] != Prefix[i]))
                return false;

            while ((size = encStream.Read(encBuffer, 0, ChunkSize)) > 0)
            {
                for (var start = 0; start < size; start += offsets.Length)
                    for (var index = 0; index < offsets.Length; index++)
                        decBuffer[start + index] = Dance(encBuffer[start + offsets[index]], start + index);
                decStream.Write(decBuffer, 0, size);
            }

            return true;
        }
    }
}
