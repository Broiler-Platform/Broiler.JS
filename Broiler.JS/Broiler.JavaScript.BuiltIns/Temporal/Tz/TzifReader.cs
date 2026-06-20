using System;
using System.Text;

namespace Broiler.JavaScript.BuiltIns.Temporal.Tz;

// Parser for the TZif (RFC 8536) binary time-zone format used by the IANA database. Reads the
// 64-bit (version 2/3) data block when present so the full historical and future transition range
// is available, falling back to the version-1 block for a plain TZif1 file.
internal static class TzifReader
{
    public static IanaTimeZone Parse(string id, byte[] data)
    {
        if (data.Length < 44 || data[0] != 'T' || data[1] != 'Z' || data[2] != 'i' || data[3] != 'f')
            throw new FormatException($"Not a TZif file: {id}");

        var version = (char)data[4];
        var pos = 20; // magic(4) + version(1) + reserved(15)

        var v1 = ReadHeaderCounts(data, ref pos);

        if (version is '2' or '3')
        {
            // Skip the entire version-1 data block, then the second ("TZif2/3") header, and parse the
            // 64-bit data block.
            pos += V1DataBlockSize(v1);
            if (pos + 20 > data.Length || data[pos] != 'T' || data[pos + 1] != 'Z' || data[pos + 2] != 'i' || data[pos + 3] != 'f')
                throw new FormatException($"Missing TZif2 block: {id}");
            pos += 20;
            var v2 = ReadHeaderCounts(data, ref pos);
            return ParseDataBlock(id, data, ref pos, in v2, timeSize: 8);
        }

        return ParseDataBlock(id, data, ref pos, in v1, timeSize: 4);
    }

    private readonly struct Counts(int isUt, int isStd, int leap, int time, int type, int chars)
    {
        public readonly int IsUtCnt = isUt;
        public readonly int IsStdCnt = isStd;
        public readonly int LeapCnt = leap;
        public readonly int TimeCnt = time;
        public readonly int TypeCnt = type;
        public readonly int CharCnt = chars;
    }

    private static Counts ReadHeaderCounts(byte[] d, ref int pos)
    {
        var isut = ReadInt32(d, ref pos);
        var isstd = ReadInt32(d, ref pos);
        var leap = ReadInt32(d, ref pos);
        var time = ReadInt32(d, ref pos);
        var type = ReadInt32(d, ref pos);
        var chars = ReadInt32(d, ref pos);
        return new Counts(isut, isstd, leap, time, type, chars);
    }

    private static int V1DataBlockSize(in Counts c)
        => c.TimeCnt * 4         // transition times (int32)
         + c.TimeCnt * 1         // transition type indices
         + c.TypeCnt * 6         // ttinfo records
         + c.CharCnt             // abbreviation chars
         + c.LeapCnt * 8         // leap records (int32 + int32)
         + c.IsStdCnt * 1
         + c.IsUtCnt * 1;

    private static IanaTimeZone ParseDataBlock(string id, byte[] d, ref int pos, in Counts c, int timeSize)
    {
        var transitionTimes = new long[c.TimeCnt];
        for (var i = 0; i < c.TimeCnt; i++)
            transitionTimes[i] = timeSize == 8 ? ReadInt64(d, ref pos) : ReadInt32(d, ref pos);

        var transitionTypeIndices = new byte[c.TimeCnt];
        for (var i = 0; i < c.TimeCnt; i++)
            transitionTypeIndices[i] = d[pos++];

        var offsets = new int[c.TypeCnt];
        var isDst = new bool[c.TypeCnt];
        var abbrIndex = new int[c.TypeCnt];
        for (var i = 0; i < c.TypeCnt; i++)
        {
            offsets[i] = ReadInt32(d, ref pos);
            isDst[i] = d[pos++] != 0;
            abbrIndex[i] = d[pos++];
        }

        var abbrevBytes = new byte[c.CharCnt];
        System.Array.Copy(d, pos, abbrevBytes, 0, c.CharCnt);
        pos += c.CharCnt;

        var types = new IanaTimeZone.LocalTimeType[c.TypeCnt];
        for (var i = 0; i < c.TypeCnt; i++)
            types[i] = new IanaTimeZone.LocalTimeType(offsets[i], isDst[i], ReadAbbrev(abbrevBytes, abbrIndex[i]));

        // The "initial" type (before the first transition) is, per RFC 8536, the first non-DST type;
        // if none exists, the first type.
        var initialTypeIndex = 0;
        for (var i = 0; i < c.TypeCnt; i++)
        {
            if (!isDst[i]) { initialTypeIndex = i; break; }
        }

        return new IanaTimeZone(id, transitionTimes, transitionTypeIndices, types, initialTypeIndex);
    }

    private static string ReadAbbrev(byte[] chars, int start)
    {
        var end = start;
        while (end < chars.Length && chars[end] != 0) end++;
        return Encoding.ASCII.GetString(chars, start, end - start);
    }

    private static int ReadInt32(byte[] d, ref int pos)
    {
        var v = (d[pos] << 24) | (d[pos + 1] << 16) | (d[pos + 2] << 8) | d[pos + 3];
        pos += 4;
        return v;
    }

    private static long ReadInt64(byte[] d, ref int pos)
    {
        long v = ((long)d[pos] << 56) | ((long)d[pos + 1] << 48) | ((long)d[pos + 2] << 40) | ((long)d[pos + 3] << 32)
               | ((long)d[pos + 4] << 24) | ((long)d[pos + 5] << 16) | ((long)d[pos + 6] << 8) | d[pos + 7];
        pos += 8;
        return v;
    }
}
