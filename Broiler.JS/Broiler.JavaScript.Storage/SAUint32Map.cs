using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Broiler.JavaScript.Storage;


public struct SAUint32Map<T>
{
    public const int NodeBlock = 4;

    [DebuggerDisplay("{Key}: {Value}")]
    public struct KeyValue
    {
        public uint Key;
        public T Value;
    }

    internal enum NodeState : byte
    {
        Empty = 0,
        Filled = 1,
        HasValue = 4
    }

    static Node Empty = new();

    [DebuggerDisplay("{Key}={Value}")]
    internal struct Node
    {
        public readonly bool HasValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (State & NodeState.HasValue) > 0;
        }

        /// <summary>
        /// Current key
        /// </summary>
        public uint Key;
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
        public VirtualArray Children;
    }

    private VirtualMemory<Node> nodes;
    private int liveCount;

    // first set of roots
    private VirtualArray roots;

    public T this[uint index]
    {
        get
        {
            ref var node = ref GetNode(index);
            return node.HasValue ? node.Value : default;
        }
    }

    public readonly bool IsNull => nodes.IsEmpty;

    public readonly int Count => liveCount;

    public readonly int Capacity => nodes.Capacity;

    public readonly int UsedNodeCount => nodes.UsedCount;


    public IEnumerable<KeyValue> All
    {
        get
        {
            foreach (var (k, v) in AllValues())
                yield return new KeyValue { Key = k, Value = v };
        }
    }

    public readonly ValueEnumerable AllValues() => new(nodes);

    public readonly struct ValueEnumerable : IEnumerable<(uint Key, T Value)>
    {
        private readonly VirtualMemory<Node> nodes;

        internal ValueEnumerable(VirtualMemory<Node> nodes) => this.nodes = nodes;

        public ValueEnumerator GetEnumerator() => new(nodes);

        IEnumerator<(uint Key, T Value)> IEnumerable<(uint Key, T Value)>.GetEnumerator() => GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct ValueEnumerator : IEnumerator<(uint Key, T Value)>
    {
        private readonly VirtualMemory<Node> nodes;
        private readonly int usedCount;
        private int index = -1;
        private (uint Key, T Value) current;

        internal ValueEnumerator(VirtualMemory<Node> nodes)
        {
            this.nodes = nodes;
            usedCount = nodes.UsedCount;
        }

        public readonly (uint Key, T Value) Current => current;

        readonly object System.Collections.IEnumerator.Current => current;

        public bool MoveNext()
        {
            while (++index < usedCount)
            {
                var node = nodes.GetAt(index);
                if (!node.HasValue)
                    continue;

                current = (node.Key, node.Value);
                return true;
            }

            current = default;
            return false;
        }

        public readonly void Dispose() { }

        public void Reset()
        {
            index = -1;
            current = default;
        }
    }

    public bool HasKey(uint key)
    {
        ref var node = ref GetNode(key);
        return node.HasValue;
    }

    public bool TryGetValue(uint key, out T value)
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

    public bool TryRemove(uint key, out T value)
    {
        ref var node = ref GetNode(key);
        if (node.HasValue)
        {
            value = node.Value;
            node.Value = default;
            node.State = NodeState.Filled;
            liveCount--;
            return true;
        }

        value = default;
        return false;
    }

    public void Save(uint key, T value)
    {
        ref var node = ref GetNode(key, true);
        if (!node.HasValue)
            liveCount++;
        node.Value = value;
        node.State |= NodeState.HasValue;
    }

    public ref T Put(uint key)
    {
        ref var node = ref GetNode(key, true);
        if (!node.HasValue)
            liveCount++;
        node.State |= NodeState.HasValue;
        return ref node.Value;
    }

    public ref T GetRefOrDefault(uint key, ref T def) 
    {
        ref var node = ref GetNode(key);
        if (node.HasValue)
            return ref node.Value;

        return ref def;
    }

    public bool RemoveAt(uint key)
    {
        ref var node = ref GetNode(key);
        if (node.HasValue)
        {
            node.State = NodeState.Filled;
            node.Value = default;
            liveCount--;
            return true;
        }

        return false;
    }


    private ref Node GetNode(uint originalKey, bool create = false)
    {
        ref var node = ref Empty;

        if (roots.IsEmpty) 
        { 
            if (!create) 
                return ref Empty;

            // extend...
            roots = nodes.Allocate(4);
            nodes[roots, 0].State = NodeState.Filled;
        }

        if (originalKey == 0)
        {
            node = ref nodes[roots, 0];
            return ref node;
        }

        var leaves = roots;

        // let us walk the nodes...
        for (long key = originalKey; key > 0; key >>= 2)
        {
            var index = (int)(key & 0x3);
            node = ref nodes[leaves, index];
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

                if (node.Key > originalKey)
                {
                    // need to make this non recursive...
                    var oldKey = node.Key;
                    var oldValue = node.Value;
                    // var oldChild = node.Children;
                    node.Key = originalKey;
                    node.State = NodeState.Filled;
                    node.Value = default;
                    ref var newChild = ref GetNode(oldKey, true);
                    newChild.Key = oldKey;
                    newChild.Value = oldValue;
                    // var newChildren = newChild.Children;
                    // newChild.Children = oldChild;
                    newChild.State |= NodeState.HasValue;
                    // this is case when array is resized
                    // and we still might have reference to old node
                    node = ref nodes[leaves, index];
                    // node.Children = newChildren;
                    return ref node;
                }

                node.State |= NodeState.Filled;
                if (node.Children.IsEmpty)
                {
                    var c = nodes.Allocate(4);
                    // allocation may have moved node
                    node = ref nodes[leaves, index];
                    node.Children = c;
                }
            }

            var next = node.Children;
            if (next.IsEmpty)
                return ref Empty;

            leaves = next;
        }

        if (node.Key == originalKey)
            return ref node;

        return ref Empty;
    }

    public void Resize(int size)
    {
        if (size < 0)
            return;

        // right align to 4 bits..
        size = ((size / 4)+1)*4;
        nodes.SetCapacity(size);
    }
}
