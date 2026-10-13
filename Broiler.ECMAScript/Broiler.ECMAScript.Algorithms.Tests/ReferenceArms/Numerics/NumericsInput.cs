using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics;

/// <summary>
/// One input of an operation of bundle sjl-4-001's reference-arm comparison: the population line it
/// came from, decoded, and the parameter the operation runs it at. Which fields are set depends on the
/// class (see <see cref="NumericsPopulation"/>).
/// </summary>
internal sealed record NumericsInput
{
    /// <summary>The population class the line belongs to.</summary>
    public required string Class { get; init; }

    /// <summary>The line's number in its class, from 0.</summary>
    public required long Line { get; init; }

    /// <summary>The line as the population writes it.</summary>
    public required string Raw { get; init; }

    public double A { get; init; }

    public double B { get; init; }

    public double[]? Values { get; init; }

    public ushort Half { get; init; }

    public BigInteger X { get; init; }

    public BigInteger Y { get; init; }

    public BigInteger Z { get; init; }

    public BigInteger W { get; init; }

    public string? Text { get; init; }

    /// <summary>The parameter the operation runs the line at (a mode, a radix, a sign), or -1.</summary>
    public int Parameter { get; init; } = -1;

    /// <summary>A second parameter, where an operation takes two (a sign beside a mode), or -1.</summary>
    public int Parameter2 { get; init; } = -1;

    /// <summary>What the listing shows for the parameters and any derived input: a constructed text's recipe.</summary>
    public string Detail { get; init; } = "-";
}

/// <summary>
/// How results are written, so that two implementations are compared exactly by comparing text: a
/// double by its bit pattern with every NaN as one, a binary16 by its bits, an integer by its value, a
/// BigInt by its value in hexadecimal, a charge after a bar, and a refusal by its kind.
/// </summary>
/// <remarks>
/// A BigInt is written in hexadecimal rather than decimal (the rule's "by its decimal text"), which is
/// as exact and is linear in the value's width; the revision of 2026-10-13 records the change. A field
/// longer than <see cref="ListedLength"/> characters is listed by its length and SHA-256, and compared
/// whole.
/// </remarks>
internal static class Render
{
    public const int ListedLength = 4096;

    public static string F64(double value) =>
        double.IsNaN(value) ? "NaN" : "f64:" + BitConverter.DoubleToUInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);

    public static string F16(ushort bits) => "f16:" + bits.ToString("X4", CultureInfo.InvariantCulture);

    public static string Int(long value) => "int:" + value.ToString(CultureInfo.InvariantCulture);

    public static string Int(ulong value) => "int:" + value.ToString(CultureInfo.InvariantCulture);

    public static string Int(BigInteger value) => "int:" + value.ToString(CultureInfo.InvariantCulture);

    /// <summary>An integer held in a long, exactly.</summary>
    public static string Number(long value) => value == 0 ? "num:0" : "num:" + value.ToString(CultureInfo.InvariantCulture);

    /// <summary>A mathematical value held in a double: an integer exactly, or an infinity.</summary>
    public static string Number(double value)
    {
        if (double.IsNaN(value))
            return "NaN";
        if (double.IsInfinity(value))
            return value > 0 ? "num:+Infinity" : "num:-Infinity";
        if (value == 0)
            return double.IsNegative(value) ? "num:-0" : "num:0";
        return Math.Truncate(value) == value
            ? "num:" + new BigInteger(value).ToString(CultureInfo.InvariantCulture)
            : "num:" + F64(value);
    }

    public static string Big(BigInteger value)
    {
        if (value.IsZero)
            return "bigint:0";
        var magnitude = BigInteger.Abs(value).ToString("x", CultureInfo.InvariantCulture).TrimStart('0');
        return value.Sign < 0 ? "bigint:-" + magnitude : "bigint:" + magnitude;
    }

    public static string Charged(string result, ulong charge) => result + "|charge:" + charge.ToString(CultureInfo.InvariantCulture);

    public const string RangeError = "error:RangeError";

    public const string Undefined = "undefined";

    /// <summary>A field as the listing writes it: escaped, or by its length and digest when long.</summary>
    public static string Listed(string text)
    {
        if (text.Length <= ListedLength)
            return NumericsPopulation.Escape(text);
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        return "len:" + text.Length.ToString(CultureInfo.InvariantCulture) + ":sha256:" + digest;
    }
}

/// <summary>A meter that sums what it is charged, for S and for arm V alike.</summary>
internal sealed class Meter
{
    public ulong Total { get; private set; }

    public void Charge(ulong units) => Total += units;
}

/// <summary>The signed hexadecimal integers of the population: an optional "-", then lower-case digits.</summary>
internal static class Hex
{
    public static BigInteger Parse(ReadOnlySpan<char> text)
    {
        var negative = text.Length > 0 && text[0] == '-';
        var digits = negative ? text[1..] : text;
        var magnitude = BigInteger.Parse("0" + digits.ToString(), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        return negative ? -magnitude : magnitude;
    }

    public static IReadOnlyList<BigInteger> Fields(string line)
    {
        var parts = line.Split(' ');
        var values = new BigInteger[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            values[i] = Parse(parts[i]);
        return values;
    }
}
