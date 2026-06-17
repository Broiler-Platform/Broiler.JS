using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Issue #830 (problem 5): a private field lives in [[PrivateElements]], outside the ordinary
// property model, so Object.preventExtensions / seal / freeze never lock it — its value stays
// writable. Mirrors test262 staging/sm/PrivateName/modify-non-extensible. Private methods are
// still non-writable and getter-only private accessors still throw on assignment.
public class Issue830PrivateNameIntegrityTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    [Theory]
    // A private field stays writable through every integrity level.
    [InlineData("class C { #a = 0; set(v){ this.#a = v; } get(){ return this.#a; } } const o = new C(); Object.preventExtensions(o); o.set(5); o.get();", "5")]
    [InlineData("class C { #a = 0; set(v){ this.#a = v; } get(){ return this.#a; } } const o = new C(); Object.seal(o); o.set(7); o.get();", "7")]
    [InlineData("class C { #a = 0; set(v){ this.#a = v; } get(){ return this.#a; } } const o = new C(); Object.freeze(o); o.set(9); o.get();", "9")]
    // Freezing still reports the object as frozen, and freezes its public properties.
    [InlineData("class C { #a = 0; pub = 1; } const o = new C(); Object.freeze(o); Object.isFrozen(o);", "true")]
    [InlineData("'use strict'; class C { #a = 0; pub = 1; } const o = new C(); Object.freeze(o); try { o.pub = 2; } catch (e) {} o.pub;", "1")]
    public void PrivateFieldWritableThroughIntegrityLevels(string source, string expected)
        => Assert.Equal(expected, Eval(source));

    [Theory]
    // A private method cannot be reassigned, even before freezing — a TypeError.
    [InlineData("class C { #m(){} go(){ try { this.#m = 1; return 'no-throw'; } catch (e) { return e.constructor.name; } } } new C().go();", "TypeError")]
    // A getter-only private accessor is a TypeError on assignment.
    [InlineData("class C { get #x(){ return 1; } go(){ try { this.#x = 1; return 'no-throw'; } catch (e) { return e.constructor.name; } } } new C().go();", "TypeError")]
    // A private accessor with a setter runs the setter.
    [InlineData("class C { #v=0; set #x(v){ this.#v = v; } go(){ this.#x = 8; return this.#v; } } new C().go();", "8")]
    // Writing a private name the object lacks is still a brand-check TypeError.
    [InlineData("class C { #a = 0; } class D { go(o){ try { o.#a = 1; return 'no-throw'; } catch (e) { return e.constructor.name; } } #a; } new D().go(new C());", "TypeError")]
    public void PrivateMethodAndAccessorSemantics(string source, string expected)
        => Assert.Equal(expected, Eval(source));
}
