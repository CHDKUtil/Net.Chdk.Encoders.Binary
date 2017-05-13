using Chimp.Logging;
using Net.Chdk.Providers.Boot;
using System.IO;
using System.Linq;

namespace Net.Chdk.Encoders.Binary
{
    public sealed class BinaryDecoder : BinaryEncoderDecoder, IBinaryDecoder
    {
        public BinaryDecoder(IBootProvider bootProvider, ILoggerFactory loggerFactory)
            : base(bootProvider, loggerFactory.CreateLogger<BinaryDecoder>())
        {
        }

        public bool Decode(Stream inStream, Stream outStream, int version)
        {
            Validate(inStream: inStream, outStream: outStream, version: version);

            if (version == 0)
            {
                Logger.Log(LogLevel.Trace, "Copying {0} contents", FileName);
                inStream.CopyTo(outStream);
                return true;
            }

            Logger.Log(LogLevel.Trace, "Decoding {0} version {1}", FileName, version);
            return Decode(inStream, outStream, Offsets[version - 1]);
        }

        private bool Decode(Stream encStream, Stream decStream, int[] offsets)
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
