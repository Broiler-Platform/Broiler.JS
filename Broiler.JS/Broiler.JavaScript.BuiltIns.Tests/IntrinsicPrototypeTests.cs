using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// An object the engine creates itself — an array literal, the array `split` returns, a regular
// expression literal, the TypeError a failed property read throws, the wrapper `Object('s')`
// makes, the prototype a primitive's property lookup walks — is created with the realm's
// intrinsic prototype (%Array.prototype%, %TypeError.prototype%, %String.prototype%, …: the
// specification's ArrayCreate, RegExpCreate, ThrowTypeError, ToObject and GetValue all name the
// intrinsic, never "the current value of the global `Array`").
//
// A C# built-in constructed without an explicit prototype resolved one from the CURRENT global
// binding of its class name, so guest code that replaced or deleted `globalThis.Array` (or any
// other constructor global) changed the prototype of every array the engine made afterwards,
// and made primitive property lookups read the impostor's `prototype`. Only Promise and
// ArrayBuffer had been pinned to the realm's own prototype. Every built-in class registered on
// the realm global now records its prototype at creation, and engine-created instances use it.
public class IntrinsicPrototypeTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(source).ToString();
    }

    // Replaces (or deletes) the global named `global`, evaluates `make` and reports whether the
    // object it returns has the prototype `intrinsic` named before the global was touched.
    private static string AfterGlobalIsReplaced(string global, string make, string intrinsic, bool delete = false)
        => Eval($$"""
            var saved = globalThis.{{global}};
            var intrinsic = {{intrinsic}};
            var getPrototypeOf = Object.getPrototypeOf;
            var outcome;
            try {
                {{(delete
                    ? $"delete globalThis.{global};"
                    : $"globalThis.{global} = function Impostor() {{}}; globalThis.{global}.prototype = {{ impostor: true }};")}}
                var made = {{make}};
                var proto = getPrototypeOf(made);
                outcome = proto === intrinsic ? 'intrinsic' : proto && proto.impostor ? 'impostor' : 'other';
            } catch (e) {
                outcome = 'threw ' + e;
            } finally {
                globalThis.{{global}} = saved;
            }
            outcome;
        """);

    private const string Caught = "(function () { try { %s } catch (e) { return e; } })()";

    private static string Catch(string statement) => Caught.Replace("%s", statement);

    [Theory]
    [InlineData("Array", "[1, 2]", "Array.prototype")]
    [InlineData("Array", "'a,b'.split(',')", "Array.prototype")]
    [InlineData("Array", "Object.keys({ a: 1 })", "Array.prototype")]
    [InlineData("Array", "/a/.exec('a')", "Array.prototype")]
    [InlineData("Array", "JSON.parse('[1]')", "Array.prototype")]
    [InlineData("RegExp", "/a/g", "RegExp.prototype")]
    [InlineData("Object", "JSON.parse('{\"a\":1}')", "Object.prototype")]
    [InlineData("Object", "({ a: 1 })", "Object.prototype")]
    public void EngineCreatedObjectsTakeTheIntrinsicPrototype(string global, string make, string intrinsic)
        => Assert.Equal("intrinsic", AfterGlobalIsReplaced(global, make, intrinsic));

    [Theory]
    [InlineData("TypeError", "null.x")]
    [InlineData("RangeError", "new Array(-1)")]
    [InlineData("SyntaxError", "eval('(')")]
    [InlineData("ReferenceError", "undeclaredBindingForThisTest")]
    [InlineData("URIError", "decodeURI('%')")]
    public void EngineThrownErrorsTakeTheIntrinsicPrototype(string global, string statement)
    {
        Assert.Equal("intrinsic", AfterGlobalIsReplaced(global, Catch(statement), global + ".prototype"));
        Assert.Equal("intrinsic", AfterGlobalIsReplaced(global, Catch(statement), global + ".prototype", delete: true));
    }

    [Theory]
    [InlineData("String", "Object('s')")]
    [InlineData("Number", "Object(1)")]
    [InlineData("Boolean", "Object(true)")]
    [InlineData("BigInt", "Object(1n)")]
    [InlineData("Symbol", "Object(Object.getOwnPropertySymbols(Array.prototype)[0])")]
    public void PrimitiveWrappersTakeTheIntrinsicPrototype(string global, string make)
    {
        Assert.Equal("intrinsic", AfterGlobalIsReplaced(global, make, global + ".prototype"));
        Assert.Equal("intrinsic", AfterGlobalIsReplaced(global, make, global + ".prototype", delete: true));
    }

    [Fact(Timeout = 600000)]
    public void PrimitivePropertyLookupsUseTheIntrinsicPrototype()
        => Assert.Equal("ABC,1.5,true,1,Symbol(x),3", Eval("""
            var saved = [String, Number, Boolean, BigInt, Symbol];
            var sym = Symbol('x');
            String = Number = Boolean = BigInt = Symbol = function Impostor() {};
            var r = ['abc'.toUpperCase(), (1.5).toFixed(1), true.toString(), (1n).toString(),
                     sym.toString(), 'abc'.length].join(',');
            String = saved[0]; Number = saved[1]; Boolean = saved[2]; BigInt = saved[3]; Symbol = saved[4];
            r;
        """));

    [Theory]
    [InlineData("new Uint8Array(2).subarray(0)")]
    [InlineData("new Uint8Array(2).map(function (x) { return x; })")]
    [InlineData("new Uint8Array(2).slice(0)")]
    [InlineData("new Uint8Array(2).filter(function () { return true; })")]
    public void TypedArraySpeciesDefaultsTakeTheIntrinsicPrototype(string make)
        => Assert.Equal("intrinsic", Eval($$"""
            var U8 = Uint8Array, intrinsic = Uint8Array.prototype, outcome;
            try {
                var source = new U8(2);
                Uint8Array = function Impostor() {}; Uint8Array.prototype = { impostor: true };
                var made = {{make.Replace("new Uint8Array(2)", "source")}};
                outcome = Object.getPrototypeOf(made) === intrinsic ? 'intrinsic' : 'other';
            } catch (e) { outcome = 'threw ' + e; } finally { Uint8Array = U8; }
            outcome;
        """));

    [Theory]
    [InlineData("(function* () {})()")]
    [InlineData("[].values().map(function (x) { return x; })")]
    [InlineData("'a'.matchAll(/a/g)")]
    public void IteratorsInheritFromTheIntrinsicIteratorPrototype(string make)
        => Assert.Equal("true", Eval($$"""
            var saved = Iterator, intrinsic = Iterator.prototype, outcome;
            try {
                Iterator = function Impostor() {}; Iterator.prototype = { impostor: true };
                var made = {{make}};
                outcome = intrinsic.isPrototypeOf(made) && !Object.prototype.isPrototypeOf.call(Iterator.prototype, made);
            } catch (e) { outcome = 'threw ' + e; } finally { Iterator = saved; }
            String(outcome);
        """));

    // SpeciesConstructor(O, %C%) falls back to the intrinsic %C% when O.constructor is undefined.
    [Theory]
    [InlineData("ArrayBuffer", "new saved(4)", "made.slice(0)")]
    [InlineData("SharedArrayBuffer", "new saved(4)", "made.slice(0)")]
    [InlineData("Promise", "saved.resolve(1)", "made.finally(function () {})")]
    public void SpeciesDefaultsAreTheIntrinsicConstructor(string global, string make, string derive)
        => Assert.Equal("intrinsic", Eval($$"""
            var saved = {{global}}, outcome;
            try {
                var made = {{make}};
                Object.defineProperty(made, 'constructor', { value: undefined });
                {{global}} = function Impostor() { return {}; };
                var derived = {{derive}};
                outcome = Object.getPrototypeOf(derived) === saved.prototype ? 'intrinsic' : 'other';
            } catch (e) { outcome = 'threw ' + e; } finally { {{global}} = saved; }
            outcome;
        """));

    // Promise.any rejects with an AggregateError created from the intrinsic %AggregateError%
    // (the specification's ThrowCompletion of a newly created AggregateError), not from whatever
    // the global binding holds when the last input rejects.
    [Fact(Timeout = 600000)]
    public async System.Threading.Tasks.Task PromiseAnyRejectsWithTheIntrinsicAggregateError()
    {
        Load();
        using var ctx = new JSContext();
        var result = await ctx.ExecuteAsync("""
            (async () => {
                var saved = AggregateError, outcome;
                AggregateError = function Impostor() { return { impostor: true }; };
                try { await Promise.any([Promise.reject(1)]); outcome = 'resolved'; }
                catch (e) { outcome = Object.getPrototypeOf(e) === saved.prototype ? 'intrinsic' : e && e.impostor ? 'impostor' : 'other'; }
                finally { AggregateError = saved; }
                return outcome;
            })()
            """);
        Assert.Equal("intrinsic", result.ToString());
    }

    [Fact(Timeout = 600000)]
    public void WithStatementReadsTheIntrinsicUnscopables()
        => Assert.Equal("outer", Eval("""
            var x = 'outer', r;
            var o = { x: 'inner' };
            o[Symbol.unscopables] = { x: true };
            var saved = Symbol;
            Symbol = function Impostor() {};
            try { with (o) { r = x; } } finally { Symbol = saved; }
            r;
        """));

    [Fact(Timeout = 600000)]
    public void ScriptConstructionStillFollowsNewTarget()
        => Assert.Equal("true,true,true,true", Eval("""
            class MyArray extends Array {}
            class MyError extends TypeError {}
            class MyMap extends Map {}
            var saved = Map; Map = function Impostor() {};
            var r = [Object.getPrototypeOf(new MyArray()) === MyArray.prototype,
                     Object.getPrototypeOf(new MyError()) === MyError.prototype,
                     Object.getPrototypeOf(new MyMap()) === MyMap.prototype,
                     Object.getPrototypeOf(new saved()) === saved.prototype].join(',');
            Map = saved;
            r;
        """));

    [Fact(Timeout = 600000)]
    public void EachRealmKeepsItsOwnIntrinsics()
    {
        Load();
        using var first = new JSContext();
        using var second = new JSContext();
        first.Eval("globalThis.firstArrayPrototype = Array.prototype; Array = function Impostor() {};");
        Assert.Equal("true", first.Eval("String(Object.getPrototypeOf([]) === firstArrayPrototype)").ToString());
        Assert.Equal("true", second.Eval("String(Object.getPrototypeOf([]) === Array.prototype)").ToString());
    }
}
