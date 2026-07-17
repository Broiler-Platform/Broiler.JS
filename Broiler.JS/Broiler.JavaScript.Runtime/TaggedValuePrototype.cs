using System;
using System.Runtime.InteropServices;

namespace Broiler.JavaScript.Runtime;

/// <summary>
/// Eight-byte scalar-only NaN-boxing prototype. It is intentionally internal and is
/// not used by JSValue storage: object references, strings, symbols, and BigInts remain
/// outside the feasibility boundary.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct TaggedValuePrototype : IEquatable<TaggedValuePrototype>
{
    private const ulong TagMask = 0xffff_0000_0000_0000;
    private const ulong PayloadMask = 0x0000_ffff_ffff_ffff;
    private const ulong Int32Tag = 0x7ff9_0000_0000_0000;
    private const ulong BooleanTag = 0x7ffa_0000_0000_0000;
    private const ulong NullTag = 0x7ffb_0000_0000_0000;
    private const ulong UndefinedTag = 0x7ffc_0000_0000_0000;
    private const ulong CanonicalNaN = 0x7ff8_0000_0000_0000;

    private readonly ulong bits;

    private TaggedValuePrototype(ulong bits) => this.bits = bits;

    public static TaggedValuePrototype FromDouble(double value)
    {
        var rawBits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
        if (rawBits != 0x8000_0000_0000_0000
            && value >= int.MinValue
            && value <= int.MaxValue
            && value == Math.Truncate(value))
            return new TaggedValuePrototype(Int32Tag | unchecked((uint)(int)value));

        return new TaggedValuePrototype(double.IsNaN(value)
            ? CanonicalNaN
            : rawBits);
    }

    public static TaggedValuePrototype FromBoolean(bool value)
        => new(BooleanTag | (value ? 1UL : 0UL));

    public static TaggedValuePrototype Null => new(NullTag);
    public static TaggedValuePrototype Undefined => new(UndefinedTag);

    public bool IsInt32 => (bits & TagMask) == Int32Tag;
    public bool IsBoolean => (bits & TagMask) == BooleanTag;
    public bool IsNull => bits == NullTag;
    public bool IsUndefined => bits == UndefinedTag;
    public bool IsNumber => IsInt32 || (bits & TagMask) is not (BooleanTag or NullTag or UndefinedTag);

    public double DoubleValue => IsInt32
        ? unchecked((int)(uint)(bits & PayloadMask))
        : BitConverter.Int64BitsToDouble(unchecked((long)bits));

    public bool BooleanValue => IsBoolean && (bits & 1) != 0;

    public static bool TryFromJSValue(JSValue value, out TaggedValuePrototype tagged)
    {
        if (value.IsNumber)
        {
            tagged = FromDouble(value.DoubleValue);
            return true;
        }
        if (value.IsBoolean)
        {
            tagged = FromBoolean(value.BooleanValue);
            return true;
        }
        if (value.IsNull)
        {
            tagged = Null;
            return true;
        }
        if (value.IsUndefined)
        {
            tagged = Undefined;
            return true;
        }

        tagged = default;
        return false;
    }

    public JSValue ToJSValue()
    {
        if (IsNumber)
            return JSValue.CreateNumber(DoubleValue);
        if (IsBoolean)
            return BooleanValue ? JSValue.BooleanTrue : JSValue.BooleanFalse;
        if (IsNull)
            return JSValue.NullValue;
        return JSValue.UndefinedValue;
    }

    public bool Equals(TaggedValuePrototype other) => bits == other.bits;
    public override bool Equals(object obj) => obj is TaggedValuePrototype other && Equals(other);
    public override int GetHashCode() => bits.GetHashCode();
}
