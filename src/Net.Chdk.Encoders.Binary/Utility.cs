using System;
using System.IO;

namespace Net.Chdk.Encoders.Binary
{
    internal class Utility
    {
        public const int ChunkSize = 0x400;

        public static readonly byte[] Prefix = new byte[] { 0 };

        public static readonly int[][] Offsets =
        {
            new[] { 4,6,1,0,7,2,5,3 }, // original flavor
			new[] { 5,3,6,1,2,7,0,4 }, // nacho cheese sx200is, ixus100_sd780, ixus95_sd1200, a1100, d10
			new[] { 2,5,0,4,6,1,3,7 }, // mesquite bbq ixus200_sd980, sx20 (dryos r39)
			new[] { 4,7,3,2,6,5,0,1 }, // cool ranch a3100 (dryos r43)
			new[] { 3,2,7,5,1,4,6,0 }, // cajun chicken s95, g12, sx30 (dryos r45)
			new[] { 0,4,2,7,3,6,5,1 }, // spicy wasabi sx220, sx230, ixus310 (dryos r47)
			new[] { 7,1,5,3,0,6,4,2 }, // sea salt & vinegar sx40hs, sx150is (dryos r49)
			new[] { 6,3,1,0,5,7,2,4 }, // spicy habenaro sx260hs (dryos r50)
			new[] { 1,0,4,6,2,3,7,5 }, // tapatio hot sauce sx160is (dryos r51)
			new[] { 3,6,7,2,4,5,1,0 }, // blazin' jalapeno a1400 (dryos r52)
			new[] { 0,2,6,3,1,4,7,5 }, // guacamole sx510hs (dryos r52)
			new[] { 2,7,0,6,3,1,5,4 }, // (dryos r54)
			new[] { 6,5,3,7,0,2,4,1 }, // oyster sauce ixus160_elph160 (dryos r55)
			new[] { 7,4,5,0,2,1,3,6 }, // jeronymo sx530 (dryos r55)
			new[] { 5,0,2,1,7,3,4,6 }, // sonic sour cream, g5x (dryos R58)
		};

        public static int MaxVersion => Offsets.Length;

        public static void Validate(Stream inStream, Stream outStream, int version)
        {
            if (inStream == null)
                throw new ArgumentNullException(nameof(inStream));
            if (outStream == null)
                throw new ArgumentNullException(nameof(outStream));
            if (version < 0 || version > MaxVersion)
                throw new ArgumentOutOfRangeException(nameof(version));
        }

        public static byte Dance(byte input, int index)
        {
            if ((index % 3) != 0)
                return (byte)(input ^ 0xff);
            if ((index % 2) == 0)
                return (byte)(input ^ 0xa0);
            return (byte)((byte)(input >> 4) | (byte)(input << 4));
        }
    }
}
