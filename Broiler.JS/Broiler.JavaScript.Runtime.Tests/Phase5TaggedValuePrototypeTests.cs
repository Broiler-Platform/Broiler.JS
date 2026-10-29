using System.Runtime.InteropServices;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Runtime.Tests;

public sealed class Phase5TaggedValuePrototypeTests
{
    [Fact]
    public void ScalarPrototypeIsEightBytesAndRoundTripsSupportedValues()
    {
        Assert.Equal(8, Marshal.SizeOf<TaggedValuePrototype>());

        var integer = TaggedValuePrototype.FromDouble(42);
        var number = TaggedValuePrototype.FromDouble(3.25);
        var nan = TaggedValuePrototype.FromDouble(double.NaN);
        var negativeZero = TaggedValuePrototype.FromDouble(-0d);
        var boolean = TaggedValuePrototype.FromBoolean(true);

        Assert.True(integer.IsInt32);
        Assert.Equal(42, integer.DoubleValue);
        Assert.Equal(3.25, number.DoubleValue);
        Assert.True(double.IsNaN(nan.DoubleValue));
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(-0d),
            BitConverter.DoubleToInt64Bits(negativeZero.DoubleValue));
        Assert.True(boolean.BooleanValue);
        Assert.True(TaggedValuePrototype.Null.IsNull);
        Assert.True(TaggedValuePrototype.Undefined.IsUndefined);
    }

    [Fact]
    public void PrototypeRejectsReferencesInsteadOfHidingAGcHandlePolicy()
    {
        using var context = new JSContext();
        Assert.True(TaggedValuePrototype.TryFromJSValue(JSValue.CreateNumber(-7), out var number));
        Assert.Equal(-7, number.ToJSValue().DoubleValue);
        Assert.True(TaggedValuePrototype.TryFromJSValue(JSValue.BooleanFalse, out var boolean));
        Assert.False(boolean.ToJSValue().BooleanValue);
        Assert.False(TaggedValuePrototype.TryFromJSValue(new JSObject(), out _));
    }
}
