using Broiler.JavaScript.Ast.Misc;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Storage;

/// <summary>
/// Mapping of uint to uint
/// </summary>
public struct StringMap<T>
{
    private const int Bits = 2;
    private const int Size = 1 << Bits;
    private const int Mask = ~(-1 << Bits);

    enum NodeState : byte
    {
        Empty = 0,
        Filled = 1,
        HasValue = 4
    }

    // The "not found" sentinel returned by ref from GetNode. It MUST stay
    // pristine (State=Empty), but create-path overflow returns can hand it to a
    // caller (Put/Save/indexer) that writes a value into it. A shared static
    // therefore leaks that stale value into every later lookup — on a fresh map
    // (storage==null) GetNode returns this sentinel and TryGetValue reports a
    // false hit with the stale value, without ever matching the key. Over a long
    // in-process run this poisons every subsequent compilation's key resolution
    // (issue #1428: a fresh script's Indices is sized to List.Count but a key
    // resolves to the leaked cumulative index -> IndexOutOfRange in the body
    // lambda). Make it [ThreadStatic] (no cross-thread races) and reset it to a
    // pristine node at each GetNode entry so a stray write can never persist.
    [ThreadStatic]
    private static Node Empty;

    struct Node
    {
        public readonly bool HasValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (State & NodeState.HasValue) > 0;
            }
        }

        /// <summary>
        /// Current key
        /// </summary>
        public HashedString Key;
        public NodeState State;
        /// <summary>
        /// Current value
        /// </summary>
        public T Value;
        /// <summary>
        /// Index of First Child.
        /// All children must be allocated
        /// in advance.
        /// </summary>
        public uint Children;
    }

    private Node[] storage;
    private uint last;

    public readonly bool IsNull => storage == null;

    public readonly IEnumerable<(StringSpan Key, T Value)> AllValues()
    {
        if (storage == null)
            yield break;

        for (int i = 0; i < storage.Length; i++)
        {
            var node = storage[i];
            if (node.HasValue)
                yield return (node.Key.Value, node.Value);
        }
    }

    public ref T Put(in HashedString index)
    {
        ref var node = ref GetNode(index, true);
        node.State |= NodeState.HasValue;
        return ref node.Value;
    }


    public T this[in StringSpan index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ref var node = ref GetNode(index);

            if (node.HasValue)
                return node.Value;

            return default;
        }
        [Obsolete("Use Put")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ref var node = ref GetNode(index, true);
            node.State |= NodeState.HasValue;
            node.Value = value;

        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(in StringSpan key, out T value)
    {
        ref var node = ref GetNode(key);
        if (node.HasValue)
        {
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(in HashedString key, out T value)
    {
        ref var node = ref GetNode(in key);
        if (node.HasValue)
        {
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasKey(in StringSpan key)
    {
        ref var node = ref GetNode(key);
        return node.HasValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemove(in StringSpan key, out T value)
    {
        // HashedString hsName = key;
        ref var node = ref GetNode(key);
        if (node.HasValue)
        {
            value = node.Value;
            node.State = NodeState.Filled;
            return true;
        }

        value = default;
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Save(in HashedString key, T value)
    {
        ref var node = ref GetNode(in key, true);
        node.State |= NodeState.HasValue;
        node.Value = value;
    }

    private ref Node GetNode(in HashedString originalKey, bool create = false)
    {
        // Reset the sentinel so a value written into it by a prior create-path
        // overflow return cannot be observed as a false hit by this lookup.
        Empty = default;
        ref var node = ref Empty;

        if (storage == null)
        {
            if (!create)
                return ref node;

            // extend...
            storage = new Node[16];
            ref var first = ref storage[0];
            first.State = NodeState.Filled;
            first.Key = "";
            last = Size;
        }

        if (originalKey.Value.IsEmpty)
        {
            node = ref storage[0];
            return ref node;
        }

        // let us walk the nodes...
        uint offset = 0;
        int start = originalKey.Hash;

        for(; start != 0; start >>= Bits)
        {
            var index = offset + (start & Mask);
            node = ref storage[index];

            if (node.Key == originalKey)
            {
                if (create)
                {
                    if (node.State == NodeState.Empty)
                        node.State = NodeState.Filled;
                }

                return ref node;
            }

            if (create)
            {
                if (node.State == NodeState.Empty)
                {
                    // lets occupy current node.
                    node.State = NodeState.Filled;
                    node.Key = originalKey;
                    return ref node;
                }

                if (node.Key.CompareToRef(in originalKey) > 0)
                {
                    var oldKey = node.Key;
                    var oldValue = node.Value;
                    var oldChild = node.Children;
                    node.Key = originalKey;
                    node.State = NodeState.Filled;
                    node.Value = default;
                    ref var newChild = ref GetNode(oldKey, true);
                    newChild.Key = oldKey;
                    newChild.Value = oldValue;
                    newChild.State |= NodeState.HasValue;
                    // this is case when array is resized
                    // and we still might have reference to old node
                    node = ref storage[index];
                    return ref node;
                }

                node.State |= NodeState.Filled;
                
                if (node.Children == 0)
                {
                    node.Children = last;
                    last += Size;
                
                    if (last >= storage.Length)
                        Array.Resize(ref storage, storage.Length * 2);
                }
            }

            var next = node.Children;
            if (next == 0)
                return ref Empty;

            offset = next;
        }

        if (node.Key == originalKey)
            return ref node;

        var en = originalKey.Value.GetEnumerator();

        while (en.MoveNext(out var ch))
        {
            int uch = ch;
            for (; uch > 0; uch >>= Bits)
            {
                var index = start + uch & Mask;
                node = ref storage[index];
                if (node.Key == originalKey)
                    return ref node;

                if (create)
                {
                    if (node.State == NodeState.Empty)
                    {
                        // lets occupy current node.
                        node.State = NodeState.Filled;
                        node.Key = originalKey;
                        return ref node;
                    }

                    if (node.Key.CompareToRef(in originalKey) > 0)
                    {
                        var oldKey = node.Key;
                        var oldValue = node.Value;
                        var oldChild = node.Children;
                        node.Key = originalKey;
                        node.State = NodeState.Filled;
                        node.Value = default;
                        ref var newChild = ref GetNode(oldKey, true);
                        newChild.Key = oldKey;
                        newChild.Value = oldValue;
                        newChild.State |= NodeState.HasValue;
                        // this is case when array is resized
                        // and we still might have reference to old node
                        node = ref storage[index];
                        return ref node;
                    }

                    node.State |= NodeState.Filled;

                    if (node.Children == 0)
                    {
                        node.Children = last;
                        last += Size;

                        if (last >= storage.Length)
                            Array.Resize(ref storage, storage.Length + 16);
                    }
                }

                var next = node.Children;
                if (next == 0)
                    return ref Empty;

                offset = next;
            }
        }

        return ref Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveAt(in StringSpan key)
    {
        HashedString hsKey = key;
        ref var node = ref GetNode(in hsKey);
        if(node.HasValue)
        {
            node.State = NodeState.Filled;
            node.Value = default;
            return true;
        }
        return false;
    }
}
