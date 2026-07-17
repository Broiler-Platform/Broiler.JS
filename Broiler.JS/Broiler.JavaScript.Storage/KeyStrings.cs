using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Broiler.JavaScript.Ast.Misc;

namespace Broiler.JavaScript.Storage;

/// <summary>
/// Process-wide property-name interning. Reads use a concurrent table plus a lock-free
/// ID-to-entry snapshot; the publication lock is taken only for an intern miss.
/// </summary>
public static class KeyStrings
{
    public readonly static KeyString __proto__;
    public readonly static KeyString length;
    public readonly static KeyString Number;
    public readonly static KeyString BigInt;
    public readonly static KeyString Object;
    public readonly static KeyString toString;
    public readonly static KeyString String;
    public readonly static KeyString substring;
    public readonly static KeyString Array;
    public readonly static KeyString join;
    public readonly static KeyString Function;
    public readonly static KeyString apply;
    public readonly static KeyString call;
    public readonly static KeyString callee;
    public readonly static KeyString bind;
    public readonly static KeyString Boolean;
    public readonly static KeyString Math;
    public readonly static KeyString Reflect;
    public readonly static KeyString Date;
    public readonly static KeyString Symbol;
    public readonly static KeyString Promise;
    public readonly static KeyString then;
    public readonly static KeyString @catch;
    public readonly static KeyString JSON;
    public readonly static KeyString parse;
    public readonly static KeyString stringify;
    public readonly static KeyString toJSON;
    public readonly static KeyString RegExp;
    public readonly static KeyString test;
    public readonly static KeyString index;
    public readonly static KeyString input;
    public readonly static KeyString lastIndex;
    public readonly static KeyString Error;
    public readonly static KeyString message;
    public readonly static KeyString stack;
    public readonly static KeyString RangeError;
    public readonly static KeyString SyntaxError;
    public readonly static KeyString TypeError;
    public readonly static KeyString URIError;
    public readonly static KeyString EvalError;
    public readonly static KeyString ReferenceError;
    public readonly static KeyString ArrayBuffer;
    public readonly static KeyString Int8Array;
    public readonly static KeyString Uint8Array;
    public readonly static KeyString Uint8ClampedArray;
    public readonly static KeyString Int16Array;
    public readonly static KeyString Uint16Array;
    public readonly static KeyString Int32Array;
    public readonly static KeyString Uint32Array;
    public readonly static KeyString Float32Array;
    public readonly static KeyString Float64Array;
    public readonly static KeyString DataView;
    public readonly static KeyString Map;
    public readonly static KeyString Set;
    public readonly static KeyString WeakRef;
    public readonly static KeyString WeakMap;
    public readonly static KeyString WeakSet;
    public readonly static KeyString valueOf;
    public readonly static KeyString name;
    public readonly static KeyString prototype;
    public readonly static KeyString constructor;
    public readonly static KeyString defineProperty;
    public readonly static KeyString deleteProperty;
    public readonly static KeyString FinalizationRegistry;
    public readonly static KeyString configurable;
    public readonly static KeyString enumerable;
    public readonly static KeyString @readonly;
    public readonly static KeyString writable;
    public readonly static KeyString @assert;
    public readonly static KeyString native;
    public readonly static KeyString value;
    public readonly static KeyString done;
    public readonly static KeyString get;
    public readonly static KeyString set;
    public readonly static KeyString undefined;
    public readonly static KeyString NaN;
    public readonly static KeyString @null;
    public readonly static KeyString getPrototypeOf;
    public readonly static KeyString ownKeys;
    public readonly static KeyString setPrototypeOf;
    public readonly static KeyString @global;
    public readonly static KeyString globalThis;
    public readonly static KeyString Module;
    public readonly static KeyString module;
    public readonly static KeyString resolve;
    public readonly static KeyString require;
    public readonly static KeyString @default;
    public readonly static KeyString import;
    public readonly static KeyString exports;
    public readonly static KeyString Generator;
    public readonly static KeyString next;
    public readonly static KeyString @throw;
    public readonly static KeyString @return;
    public readonly static KeyString weekday;
    public readonly static KeyString year;
    public readonly static KeyString month;
    public readonly static KeyString day;
    public readonly static KeyString hour;
    public readonly static KeyString minute;
    public readonly static KeyString second;
    public readonly static KeyString eval;
    public readonly static KeyString encodeURI;
    public readonly static KeyString encodeURIComponent;
    public readonly static KeyString decodeURI;
    public readonly static KeyString decodeURIComponent;
    public readonly static KeyString isFinite;
    public readonly static KeyString isNaN;
    public readonly static KeyString parseFloat;
    public readonly static KeyString parseInt;
    public readonly static KeyString arguments;
    public readonly static KeyString Infinity;
    public readonly static KeyString console;
    public readonly static KeyString @debug;
    public readonly static KeyString log;
    public readonly static KeyString clr;
    public readonly static KeyString @true;
    public readonly static KeyString @false;
    public readonly static KeyString bubbles;
    public readonly static KeyString detail;
    public readonly static KeyString cancelable;
    public readonly static KeyString composed;
    public readonly static KeyString capture;
    public readonly static KeyString deferred;
    public readonly static KeyString once;
    public readonly static KeyString raw;

    private readonly struct Entry(StringSpan name, KeyMetadata metadata)
    {
        internal readonly StringSpan Name = name;
        internal readonly KeyMetadata Metadata = metadata;
    }

    private static readonly ConcurrentDictionary<string, KeyString> InternTable = new(StringComparer.Ordinal);
    private static readonly object PublicationLock = new();
    private static Entry[] entries = new Entry[256];
    private static int nextID;

    static KeyStrings()
    {
        foreach (var field in typeof(KeyStrings).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(KeyString))
                field.SetValue(null, InternTable.GetOrAdd(field.Name, static value => InternMiss(value)));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyString GetOrCreate(in StringSpan key)
    {
        var text = key.Value ?? string.Empty;
        return InternTable.GetOrAdd(text, static value => InternMiss(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet(in StringSpan key, out KeyString result)
        => InternTable.TryGetValue(key.Value ?? string.Empty, out result);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyString GetName(uint id) => new(id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringSpan GetNameString(uint id)
    {
        var snapshot = Volatile.Read(ref entries);
        return id < snapshot.Length ? snapshot[id].Name : StringSpan.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyMetadata GetMetadata(uint id)
    {
        var snapshot = Volatile.Read(ref entries);
        return id < snapshot.Length ? snapshot[id].Metadata : default;
    }

    private static KeyString InternMiss(string text)
    {
        var id = (uint)Interlocked.Increment(ref nextID);
        var entry = new Entry(text, Classify(text));

        lock (PublicationLock)
        {
            var snapshot = entries;
            if (id >= snapshot.Length)
            {
                var grown = new Entry[System.Math.Max(snapshot.Length * 2, checked((int)id + 1))];
                System.Array.Copy(snapshot, grown, snapshot.Length);
                snapshot = grown;
                Volatile.Write(ref entries, snapshot);
            }

            snapshot[id] = entry;
        }

        return new KeyString(id);
    }

    private static KeyMetadata Classify(string text)
    {
        var isArrayIndex = TryGetArrayIndex(text.AsSpan(), out var arrayIndex);
        var isCanonicalNumeric = TryGetCanonicalNumericIndex(text, isArrayIndex, arrayIndex, out var numericIndex);
        return new KeyMetadata(
            text.Length != 0 && text[0] == '\u0001',
            isArrayIndex,
            arrayIndex,
            isCanonicalNumeric,
            numericIndex,
            StableOrdinalHash(text));
    }

    private static bool TryGetArrayIndex(ReadOnlySpan<char> text, out uint value)
    {
        value = 0;
        if (text.IsEmpty)
            return false;
        if (text[0] == '0')
            return text.Length == 1;

        ulong parsed = 0;
        foreach (var ch in text)
        {
            if (ch is < '0' or > '9')
                return false;
            parsed = parsed * 10 + (uint)(ch - '0');
            if (parsed >= uint.MaxValue)
                return false;
        }

        value = (uint)parsed;
        return true;
    }

    private static bool TryGetCanonicalNumericIndex(
        string text,
        bool isArrayIndex,
        uint arrayIndex,
        out double value)
    {
        if (isArrayIndex)
        {
            value = arrayIndex;
            return true;
        }
        if (text == "-0")
        {
            value = -0.0;
            return true;
        }
        if (text == "NaN")
        {
            value = double.NaN;
            return true;
        }
        if (text == "Infinity" || text == "+Infinity")
        {
            value = double.PositiveInfinity;
            return text[0] != '+';
        }
        if (text == "-Infinity")
        {
            value = double.NegativeInfinity;
            return true;
        }
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return false;

        // Covers the overwhelmingly common canonical integer/decimal forms. Typed arrays
        // retain their exact runtime formatter check for exponent spellings not matched here.
        return value.ToString("R", CultureInfo.InvariantCulture) == text;
    }

    private static int StableOrdinalHash(string text)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var ch in text)
            {
                hash ^= ch;
                hash *= 16777619;
            }
            return (int)hash;
        }
    }
}
