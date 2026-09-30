using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Broiler.JavaScript.Ast.Misc;

// Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=4; Fingerprint=TBF
// Broiler-Falsified-If: an index at or past Length returns an element of the backing array instead of throwing, exposing stale entries beyond the span
// Broiler-Human:        PENDING
public readonly struct ArraySpan<T>(T[] items, int length) : IEnumerable<T>
{
    public readonly int Length = length;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Length;
    }

    public static ArraySpan<T> Empty;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Falsified-If: reading the span at index Length returns the backing array's next slot instead of throwing, exposing a stale entry beyond the span
    // Broiler-Human:        PENDING
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref items[index];
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=4; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public string Join(string separator = ", ")
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Length; i++)
        {
            ref var item = ref this[i];
            if (i > 0)
                sb.Append(separator);

            sb.Append(item);
        }

        return sb.ToString();
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public static ArraySpan<T> From(params T[] items) => new(items, items.Length);

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public Enumerator GetEnumerator() => new(items, Length);

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(items, Length);

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(items, Length);

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public T FirstOrDefault()
    {
        if (Length == 0)
            return default;

        return items[0];
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public T LastOrDefault()
    {
        if (Length == 0)
            return default;

        return items[Length - 1];
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public bool Any() => Length > 0;

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=3; Fingerprint=TBF
    // Broiler-Falsified-If: ToArray on ArraySpan.Empty or a default instance, whose backing array is null, throws NullReferenceException instead of returning an empty array
    // Broiler-Human:        PENDING
    public T[] ToArray()
    {
        if (Length == items.Length)
            return items;

        var copy = new T[Length];
        Array.Copy(items, copy, Length);

        return copy;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
    // Broiler-Human:        PENDING
    public struct Enumerator(T[] items, int length) : IEnumerator<T>
    {
        private int index = -1;

        // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
        // Broiler-Human:        PENDING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(out T item)
        {
            if (++index < length)
            {
                item = items[index];
                return true;
            }

            item = default;
            return false;
        }

        // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
        // Broiler-Human:        PENDING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(out T item, out int i)
        {
            if (++index < length)
            {
                i = index;
                item = items[index];
                return true;
            }

            i = -1;
            item = default;
            return false;
        }

        // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
        // Broiler-Human:        PENDING
        public readonly T Current => items[index];

        // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=1; Fingerprint=TBF
        // Broiler-Human:        PENDING
        readonly object IEnumerator.Current => items[index];

        // Broiler-AI:           Origin=Ported; IP=Medium; Security=None; Resources=0; Fingerprint=TBF
        // Broiler-Human:        PENDING
        public readonly void Dispose() { }

        // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
        // Broiler-Human:        PENDING
        public bool MoveNext() => ++index < length;

        // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=0; Fingerprint=TBF
        // Broiler-Human:        PENDING
        public void Reset() => index = -1;
    }

    // Broiler-AI:           Origin=Ported; IP=Medium; Security=Low; Resources=3; Fingerprint=TBF
    // Broiler-Human:        PENDING
    internal void Copy(T[] copy, int start)
    {
        if (Length == 0)
            return;

        Array.Copy(items, 0, copy, start, Length);
    }
}
