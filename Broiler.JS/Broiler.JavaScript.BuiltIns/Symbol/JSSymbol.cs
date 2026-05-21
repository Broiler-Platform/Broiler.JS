using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Engine.Core;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using System;
using System.Threading;

namespace Broiler.JavaScript.BuiltIns.Symbol;

[JSBaseClass("Object")]
[JSFunctionGenerator("Symbol")]
public partial class JSSymbol: JSValue, IJSSymbol
{
    private static int SymbolID = 1;
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

    public JSSymbol(string description) : base((JSEngine.Current as IJSExecutionContext)?.ObjectPrototype)
    {
        this.description = description;
        Key = (uint)Interlocked.Increment(ref SymbolID);
    }

    public override JSValue TypeOf() => JSConstants.Symbol;

    public override bool Equals(object obj)
    {
        if (obj is JSSymbol s)
            return s.Key == Key;

        return false;
    }

    public override bool Equals(JSValue value) => ReferenceEquals(this, value);
    public override int GetHashCode() => (int)Key;

    public override JSValue InvokeFunction(in Arguments a)
        => throw JSEngine.NewTypeError($"{ToDescriptiveString()} is not a function");

    public override JSValue CreateInstance(in Arguments a) => throw JSEngine.NewTypeError("Symbol is not a constructor");

    public override bool StrictEquals(JSValue value) => ReferenceEquals(this, value);

    public override string ToString() => description ?? string.Empty;
}
