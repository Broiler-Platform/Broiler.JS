using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// #921 P2 — public class auto-accessors (`accessor x = v`, decorators proposal). An
// auto-accessor desugars to a private backing field plus a getter/setter pair on the
// home object (prototype for instance, constructor for static). Previously a public
// auto-accessor was compiled as a plain data field, so `C.prototype` had no accessor
// (`Object.getOwnPropertyDescriptor(C.prototype,'x').get` was undefined).
// (test262 staging/decorators/public-auto-accessor.js)
public class Issue921AutoAccessorTests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    [Fact]
    public void InstanceAutoAccessorReadsAndWritesBackingField()
    {
        Assert.Equal("undefined,1,2,5", Eval(@"
            class C { accessor x0; accessor x1 = 1; accessor 'x2' = 2; accessor 1 = 5; }
            var c = new C();
            [String(c.x0), c.x1, c.x2, c[1]].join(',')").ToString());
        Assert.Equal("43", Eval(@"
            class C { accessor x = 1; } var c = new C(); c.x = 43; '' + c.x").ToString());
    }

    [Fact]
    public void AutoAccessorIsAnAccessorOnThePrototype()
    {
        Assert.Equal("function,function,undefined", Eval(@"
            class C { accessor x = 1; }
            var d = Object.getOwnPropertyDescriptor(C.prototype, 'x');
            var own = Object.getOwnPropertyDescriptor(new C(), 'x');
            [typeof d.get, typeof d.set, String(own)].join(',')").ToString());
    }

    [Fact]
    public void RedeclaredAutoAccessorLastWins()
    {
        Assert.Equal("1", Eval(@"class C { accessor x = 0; accessor x = 1; } '' + new C().x").ToString());
    }

    [Fact]
    public void StaticAutoAccessorOnConstructorWithDerivedBrandCheck()
    {
        Assert.Equal("9,10", Eval(@"
            class S { static accessor x0 = 9; }
            var before = S.x0; S.x0 = 10; [before, S.x0].join(',')").ToString());
        // A static accessor read through a derived class throws a TypeError: the
        // backing private field lives only on the base constructor.
        Assert.True(Eval(@"
            class S { static accessor x = 1; } class D extends S {}
            var threw = false; try { D.x; } catch (e) { threw = e instanceof TypeError; } threw").BooleanValue);
    }

    [Fact]
    public void InheritedAndOverriddenInstanceAutoAccessors()
    {
        Assert.Equal("7,8", Eval(@"
            class B { accessor y = 7; } class D extends B { accessor z = 8; }
            var d = new D(); [d.y, d.z].join(',')").ToString());
    }

    [Fact]
    public void ComputedAndSymbolAutoAccessorNames()
    {
        Assert.Equal("11,12", Eval(@"
            var sym = Symbol(); var nm = 'cn';
            class C { accessor [nm] = 11; accessor [sym] = 12; }
            var c = new C(); [c.cn, c[sym]].join(',')").ToString());
    }

    [Fact]
    public void AutoAccessorGetterOverridesLaterUserSetterWins()
    {
        // get x; accessor x; set x  →  getter = auto-accessor, setter = user-defined.
        Assert.Equal("2,2,3", Eval(@"
            class A { #x = 1; get x(){ return this.#x } accessor x = 2; set x(v){ this.#x = v } xValue(){ return this.#x } }
            var a = new A(); var r0 = a.x; a.x = 3; [r0, a.x, a.xValue()].join(',')").ToString());
    }
}
