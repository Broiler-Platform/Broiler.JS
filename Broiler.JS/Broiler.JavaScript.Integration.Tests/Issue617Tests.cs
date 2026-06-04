using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/617
public class Issue617Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    // Problem 1: Annex B block-level function hoisting in (in)direct eval must
    // preserve a pre-existing non-enumerable global property descriptor, only
    // updating its value (CreateGlobalVarBinding + SetMutableBinding, B.3.3.3).
    [Theory]
    [InlineData("eval('{ function f(){} }')")]
    [InlineData("(0,eval)('{ function f(){} }')")]
    [InlineData("eval('if(true) function f(){} else function _f(){}')")]
    public void AnnexBEvalPreservesNonEnumerableGlobal(string evalCall)
    {
        var code = "Object.defineProperty(this,'f',{value:1,writable:true,enumerable:false,configurable:true});"
            + evalCall + ";"
            + "Object.getOwnPropertyDescriptor(this,'f').enumerable;";
        Assert.False(Eval(code).BooleanValue);
    }

    // Problem 2: for a numeric binary operator, ToNumeric(lhs) must complete
    // (and may throw) before the rhs operand is coerced.
    [Theory]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("/")]
    [InlineData("%")]
    [InlineData("&")]
    [InlineData("|")]
    [InlineData("^")]
    [InlineData("<<")]
    [InlineData(">>")]
    [InlineData(">>>")]
    [InlineData("**")]
    public void ToNumericOfLeftOperandThrowsBeforeRightIsCoerced(string op)
    {
        // lhs is a Symbol (ToNumeric -> TypeError); rhs valueOf throws a distinct
        // error. The lhs TypeError must win.
        var code = "var t='';try{ Symbol() " + op
            + " ({valueOf(){throw new RangeError('rhs');}}); }"
            + "catch(e){ t = (e instanceof TypeError) ? 'TypeError' : e.message; } t;";
        Assert.Equal("TypeError", Eval(code).ToString());
    }

    // Problem 3A: a block-level function declaration must not leak to the global
    // object in strict-mode code (Annex B does not apply in strict mode).
    [Theory]
    [InlineData("eval('\"use strict\"; { function f(){} }'); typeof f;")]
    [InlineData("'use strict'; eval('{ function f(){} }'); typeof f;")]
    [InlineData("'use strict'; { function f(){} } typeof f;")]
    public void StrictBlockFunctionDoesNotLeakToGlobal(string code)
    {
        Assert.Equal("undefined", Eval(code).ToString());
    }

    // ...but sloppy-mode block functions still hoist (Annex B), and genuine
    // top-level function declarations always bind.
    [Theory]
    [InlineData("{ function f(){} } typeof f;")]
    [InlineData("function f(){} typeof f;")]
    [InlineData("eval('{ function f(){} }'); typeof f;")]
    public void SloppyBlockAndTopLevelFunctionsStillBind(string code)
    {
        Assert.Equal("function", Eval(code).ToString());
    }

    // Problem 3B: reading super.x when the home object's prototype is null is not
    // object-coercible and must throw a TypeError (MakeSuperPropertyReference).
    [Fact]
    public void SuperPropertyWithNullHomePrototypeThrowsTypeError()
    {
        var code = "var caught;"
            + "var obj={ method(){ try{ super.x; }catch(e){caught=e;} } };"
            + "Object.setPrototypeOf(obj,null);"
            + "obj.method();"
            + "(typeof caught)+':'+(caught && caught.constructor.name);";
        Assert.Equal("object:TypeError", Eval(code).ToString());
    }
}
