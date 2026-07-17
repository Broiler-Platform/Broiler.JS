namespace Broiler.JavaScript.Storage;

/// <summary>Immutable classification computed once when a property name is interned.</summary>
public readonly struct KeyMetadata
{
    public readonly bool IsPrivateName;
    public readonly bool IsArrayIndex;
    public readonly uint ArrayIndex;
    public readonly bool IsCanonicalNumericIndex;
    public readonly double CanonicalNumericIndex;
    public readonly int StableOrdinalHash;

    internal KeyMetadata(
        bool isPrivateName,
        bool isArrayIndex,
        uint arrayIndex,
        bool isCanonicalNumericIndex,
        double canonicalNumericIndex,
        int stableOrdinalHash)
    {
        IsPrivateName = isPrivateName;
        IsArrayIndex = isArrayIndex;
        ArrayIndex = arrayIndex;
        IsCanonicalNumericIndex = isCanonicalNumericIndex;
        CanonicalNumericIndex = canonicalNumericIndex;
        StableOrdinalHash = stableOrdinalHash;
    }
}
