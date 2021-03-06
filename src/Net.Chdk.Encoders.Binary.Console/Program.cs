﻿using Chimp.Logging;
using Microsoft.Extensions.DependencyInjection;
using Net.Chdk.Providers.Boot;
using System;
using System.IO;

namespace Net.Chdk.Encoders.Binary
{
    static class Program
    {
        private const int ChunkSize = 0x400;

        static void Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddBootProvider()
                .AddBinaryEncoder()
                .AddBinaryDecoder()
                .AddSingleton<ILoggerFactory>(NoOpLoggerFactory.Instance)
                .BuildServiceProvider();

            var encoder = serviceProvider.GetService<IBinaryEncoder>();
            var decoder = serviceProvider.GetService<IBinaryDecoder>();

            string inFile = null;
            string outFile = null;
            int? version = null;
            bool? decode = null;
            if (!TryParseArgs(args, encoder, out inFile, out outFile, out version, out decode))
            {
                Usage();
                return;
            }

            var encBuffer = new byte[ChunkSize];
            var decBuffer = new byte[ChunkSize];

            var offsets = GetOffsets(serviceProvider, version);
            try
            {
                if (decode.HasValue && decode.Value)
                    Decode(decoder, inFile, outFile, encBuffer, decBuffer, offsets);
                else
                    Encode(encoder, inFile, outFile, decBuffer, encBuffer, offsets);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        private static void Encode(IBinaryEncoder encoder, string inFile, string outFile, byte[] decBuffer, byte[] encBuffer, uint offsets)
        {
            using (var inStream = File.OpenRead(inFile))
            using (var outStream = File.OpenWrite(outFile))
            {
                encoder.Encode(decBuffer, encBuffer, offsets);
            }
        }

        private static void Decode(IBinaryDecoder decoder, string inFile, string outFile, byte[] encBuffer, byte[] decBuffer, uint offsets)
        {
            using (var inStream = File.OpenRead(inFile))
            using (var outStream = File.OpenWrite(outFile))
            {
                decoder.Decode(encBuffer, decBuffer, offsets);
            }
        }

        private static uint GetOffsets(IServiceProvider serviceProvider, int? version)
        {
            var bootProvider = serviceProvider.GetService<IBootProvider>();
            var offsets = bootProvider.GetOffsets("PS");
            return GetOffsets(offsets[version.Value - 1]);
        }

        private static uint GetOffsets(int[] offsets)
        {
            var uOffsets = 0u;
            for (var index = 0; index < offsets.Length; index++)
                uOffsets += (uint)offsets[index] << (index << 2);
            return uOffsets;
        }

        private static bool TryParseArgs(string[] args, IBinaryEncoder encoder, out string inFile, out string outFile, out int? version, out bool? decode)
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
                else if (!TryParseVersion(arg, encoder, ref version))
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

        private static bool TryParseVersion(string arg, IBinaryEncoder encoder, ref int? version)
        {
            if (version != null)
                return false;

            int tempVer;
            if (!int.TryParse(arg, out tempVer) || tempVer < 1)
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
