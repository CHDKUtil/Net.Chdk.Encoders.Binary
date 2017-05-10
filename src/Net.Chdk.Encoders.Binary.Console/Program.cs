using System;
using System.IO;

namespace Net.Chdk.Encoders.Binary
{
    static class Program
    {
        static void Main(string[] args)
        {
            string inFile = null;
            string outFile = null;
            int? version = null;
            bool? decode = null;
            if (!TryParseArgs(args, out inFile, out outFile, out version, out decode))
            {
                Usage();
                return;
            }

            try
            {
                if (decode.HasValue && decode.Value)
                    Decode(inFile, outFile, version.Value);
                else
                    Encode(inFile, outFile, version.Value);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        private static void Encode(string inFile, string outFile, int version)
        {
            using (var inStream = File.OpenRead(inFile))
            using (var outStream = File.OpenWrite(outFile))
            {
                BinaryEncoder.Encode(inStream, outStream, version);
            }
        }

        private static void Decode(string inFile, string outFile, int version)
        {
            using (var inStream = File.OpenRead(inFile))
            using (var outStream = File.OpenWrite(outFile))
            {
                BinaryEncoder.Decode(inStream, outStream, version);
            }
        }

        private static bool TryParseArgs(string[] args, out string inFile, out string outFile, out int? version, out bool? decode)
        {
            inFile = null;
            outFile = null;
            version = null;
            decode = null;
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg[0] == '-')
                {
                    if (!TryParseFlag(arg, ref decode))
                    {
                        Console.Error.WriteLine("Invalid flag {0}", arg);
                        return false;
                    }
                }
                else if (inFile == null)
                {
                    inFile = arg;
                }
                else if (outFile == null)
                {
                    outFile = arg;
                }
                else if (!TryParseVersion(arg, ref version))
                {
                    Console.Error.WriteLine("Invalid version {0}", arg);
                    return false;
                }
            }
            return version != null;
        }

        private static bool TryParseFlag(string arg, ref bool? decode)
        {
            if (arg.Length != 2)
                return false;

            switch (arg[1])
            {
                case 'e':
                    return SetEncode(ref decode);
                case 'd':
                    return SetDecode(ref decode);
                default:
                    return false;
            }
        }

        private static bool TryParseVersion(string arg, ref int? version)
        {
            if (version != null)
                return false;

            int tempVer;
            if (!int.TryParse(arg, out tempVer) || tempVer < 0 || tempVer > BinaryEncoder.MaxVersion)
                return false;

            version = tempVer;
            return true;
        }

        private static bool SetEncode(ref bool? decode)
        {
            if (decode != null)
                return false;

            decode = false;
            return true;
        }

        private static bool SetDecode(ref bool? decode)
        {
            if (decode != null)
                return false;

            decode = true;
            return true;
        }

        private static void Usage()
        {
            Console.WriteLine("Usage: <infile> <outfile> <version> [-e|-d]");
        }
    }
}
