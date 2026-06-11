namespace Broiler.JavaScript.Runtime;

/// <summary>
/// A simple integer-key enumerator that yields sequential numbers from 0 to length-1.
/// Used by string values, JSArrayPrototype, and JSTypedArray for key enumeration.
/// </summary>
public struct IntKeyEnumerator(int length) : IElementEnumerator
{
    private int index = -1;

    public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
    {
        if (++this.index < length)
        {
            hasValue = true;
            index = (uint)this.index;
            value = JSValue.CreateNumber(index);
            return true;
        }
        hasValue = false;
        index = 0;
        value = JSUndefined.Value;
        return false;
    }

    public bool MoveNext(out JSValue value)
    {
        if (++index < length)
        {
            value = JSValue.CreateNumber(index);
            return true;
        }
        value = JSUndefined.Value;
        return false;
    }

    public bool MoveNextOrDefault(out JSValue value, JSValue @default)
    {
        if (++index < length)
        {
            value = JSValue.CreateNumber(index);
            return true;
        }
        value = @default;
        return false;
    }

    public JSValue NextOrDefault(JSValue @default)
    {
        if (++index < length)
        {
            return JSValue.CreateNumber(index);
        }
        return @default;
    }
}

/// <summary>
/// Like <see cref="IntKeyEnumerator"/> but yields the indices as String property
/// keys ("0", "1", …) rather than Numbers. Property-key enumeration (for-in,
/// Object.keys, spread) must observe string keys; a string primitive's own
/// index properties are enumerated through this.
/// </summary>
public struct StringIndexKeyEnumerator(int length) : IElementEnumerator
{
    private int index = -1;

    public bool MoveNext(out bool hasValue, out JSValue value, out uint index)
    {
        if (++this.index < length)
        {
            hasValue = true;
            index = (uint)this.index;
            value = JSValue.CreateString(index.ToString());
            return true;
        }
        hasValue = false;
        index = 0;
        value = JSUndefined.Value;
        return false;
    }

    public bool MoveNext(out JSValue value)
    {
        if (++index < length)
        {
            value = JSValue.CreateString(((uint)index).ToString());
            return true;
        }
        value = JSUndefined.Value;
        return false;
    }

    public bool MoveNextOrDefault(out JSValue value, JSValue @default)
    {
        if (++index < length)
        {
            value = JSValue.CreateString(((uint)index).ToString());
            return true;
        }
        value = @default;
        return false;
    }

    public JSValue NextOrDefault(JSValue @default)
    {
        if (++index < length)
        {
            return JSValue.CreateString(((uint)index).ToString());
        }
        return @default;
    }
}
