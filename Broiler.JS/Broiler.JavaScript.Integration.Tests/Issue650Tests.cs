using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/650
//
// Problem 9: `with`-statement scoping. A `var x = init` declaration inside a
// function that is nested in a `with` block, whose name collides with a
// property of the with-object, was storing the initializer into the
// with-object instead of the function-local variable. Reads of the name
// correctly resolved to the local (which the with-object property shadows),
// so the local stayed `undefined` — e.g. test/language/statements/with/
// S12.10_A1.7_T2.js failed with `result === undefined`.
public class Issue650Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // A function-local `var value = init` shadows the same-named with-object
    // property: the initializer must land in the local, and the read must see it.
    [Fact]
    public void VarInitializerInFunctionShadowsWithObjectProperty()
        => Assert.Equal("value", Eval(
            "var o={value:'v'}; var r; with(o){ var f=function(){ var value='value'; return value; }; r=f(); } r"));

    // The with-object property is left untouched when a function-local shadows it.
    [Fact]
    public void ShadowingVarInitializerDoesNotMutateWithObject()
        => Assert.Equal("value|v", Eval(
            "var o={value:'v'}; with(o){ var f=function(){ var value='value'; return value; }; f(); } '' + (function(){var value='value';return value;})() + '|' + o.value"));

    // A `var` whose declaration scope is at/above the with boundary still
    // assigns through the with-object (the legitimate dynamic-store case).
    [Fact]
    public void VarInitializerAtWithLevelStillStoresIntoWithObject()
        => Assert.Equal("L|L", Eval(
            "var o={value:'v'}; var r; with(o){ var value='L'; r=value; } r + '|' + o.value"));

    // A function-local var with no with-object collision is unaffected.
    [Fact]
    public void NonCollidingLocalVarInWithIsLocal()
        => Assert.Equal("Z", Eval(
            "var o={a:1}; var r; with(o){ var f=function(){ var zzz='Z'; return zzz; }; r=f(); } r"));

    // Full structural check mirroring S12.10_A1.7_T2: the function returns its
    // own `var value`, and the with-object's `value` stays 'myObj_value'.
    [Fact]
    public void WithNestedFunctionReturnsLocalVarNotWithProperty()
        => Assert.Equal("value|myObj_value", Eval(@"
var myObj = { value: 'myObj_value' };
var result;
with (myObj) {
  var f = function () {
    p1 = 'x1';
    var p4 = 'x4';
    var value = 'value';
    return value;
  };
  result = f();
}
result + '|' + myObj.value"));
}
