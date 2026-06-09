using Broiler.JavaScript.BuiltIns.Function;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Broiler.JavaScript.BuiltIns.Symbol;

[JSBaseClass("Object")]
[JSFunctionGenerator("Symbol")]
public partial class JSSymbol: JSPrimitive, IJSSymbol
{
    private static int SymbolID = 1;
    private static readonly ConcurrentDictionary<uint, JSSymbol> SymbolsByKey = new();
    private readonly string description;
    public readonly uint Key;

    uint IJSSymbol.Key => Key;

    public override bool BooleanValue => true;

    public override bool IsSymbol => true;

    public override double DoubleValue => throw JSEngine.NewTypeError("Cannot convert a Symbol value to a number.");

    public override string StringValue => throw JSEngine.NewTypeError("Cannot convert a Symbol value to a string.");

    public override uint UIntValue => throw JSEngine.NewTypeError("Cannot convert a Symbol value to a uint32.");

    internal override PropertyKey ToKey(bool create = true) => this;

    public static implicit operator PropertyKey(JSSymbol key) => PropertyKey.FromSymbol(key);

    internal string Description => description;

    internal string ToDescriptiveString() => description == null ? "Symbol()" : $"Symbol({description})";

    public JSSymbol(string description) : base()
    {
        this.description = description;
        Key = (uint)Interlocked.Increment(ref SymbolID);
        SymbolsByKey[Key] = this;
    }

    // A Symbol primitive's wrapper prototype is Symbol.prototype (which itself
    // chains to Object.prototype). Resolved lazily like the other primitives so
    // that well-known symbols created during bootstrap don't depend on
    // Symbol.prototype already existing. Previously the prototype was hard-wired
    // to Object.prototype, so e.g. `sym[key]` skipped Symbol.prototype entirely.
    protected override JSValue GetPrototype()
        => ((JSEngine.Current as JSObject)?[Names.Symbol] as JSFunction)?.prototype;

    internal static IJSSymbol? FromKey(uint key) => SymbolsByKey.TryGetValue(key, out var symbol) ? symbol : null;

    public override JSValue TypeOf() => JSConstants.Symbol;

    public override bool Equals(object obj)
    {
        if (obj is JSSymbol s)
            return s.Key == Key;

        return false;
    }

    public override bool Equals(JSValue value)
    {
        if (ReferenceEquals(this, value))
            return true;

        // Abstract equality (§7.2.15): when one operand is a Symbol and the other
        // an Object, the object is coerced with ToPrimitive before comparison, so a
        // symbol equals its own boxed wrapper (`sym == Object(sym)`). Delegate to
        // the object, whose Equals performs that coercion.
        if (value.IsObject)
            return value.Equals(this);

        return false;
    }
    public override int GetHashCode() => (int)Key;

    public override JSValue InvokeFunction(in Arguments a)
        => throw JSEngine.NewTypeError($"{ToDescriptiveString()} is not a function");

    public override JSValue CreateInstance(in Arguments a) => throw JSEngine.NewTypeError("Symbol is not a constructor");

    public override bool StrictEquals(JSValue value) => ReferenceEquals(this, value);

    public override string ToString() => description ?? string.Empty;
}
