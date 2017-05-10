using System.IO;
using static Net.Chdk.Encoders.Binary.Utility;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryEncoder
    {
        public static int MaxVersion => Utility.MaxVersion;

        public static void Encode(Stream inStream, Stream outStream, int version)
        {
            Validate(inStream: inStream, outStream: outStream, version: version);

            if (version == 0)
            {
                inStream.CopyTo(outStream);
                return;
            }

            Encode(inStream, outStream, Offsets[version - 1]);
        }

        private static void Encode(Stream decStream, Stream encStream, int[] offsets)
        {
            var decBuffer = new byte[ChunkSize];
            var encBuffer = new byte[ChunkSize];

            encStream.Write(Prefix, 0, Prefix.Length);

            int size;
            while ((size = decStream.Read(decBuffer, 0, ChunkSize)) > 0)
            {
                for (var start = 0; start < size; start += offsets.Length)
                    for (var index = 0; index < offsets.Length; index++)
                        encBuffer[start + offsets[index]] = Dance(decBuffer[start + index], start + index);
                encStream.Write(encBuffer, 0, size);
            }
        }
    }
}
