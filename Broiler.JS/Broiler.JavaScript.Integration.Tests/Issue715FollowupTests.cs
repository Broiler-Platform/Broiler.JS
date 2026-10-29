using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for the second batch of https://github.com/MaiRat/Broiler.JS/issues/715
// problems (P3, P8, P9):
//
//   P3 — RegExp inline modifier groups `(?<add>-<remove>:…)` must be a SyntaxError
//   when a flag is repeated within a group, or appears in both the added and
//   removed sets (e.g. `(?s-s:a)`). .NET accepts these; JS validation was missing.
//
//   P8 — Key enumeration (Object.keys / getOwnPropertyNames / for-in) must never
//   invoke the iterator protocol: a non-callable own `@@iterator`
//   (`o[Symbol.iterator] = 'x'`) used to make KeyEnumerator throw
//   "@@iterator is not a function" (it called the iterator-aware
//   GetElementEnumerator). Also, Object.keys / for-in over a Proxy must skip
//   symbol keys instead of stringifying them.
//
//   P9 — `yield*` must re-yield the delegated iterator's result object unchanged
//   (GeneratorYield(innerResult)) instead of re-boxing it into a fresh
//   { value, done }. Two bugs: the delegation unwrapped+rewrapped the result, and
//   `return yield* X` dropped the delegate flag entirely (so the operand surfaced
//   as a plain yield value). `return await X` in an async generator dropped the
//   await flag the same way.
public class Issue715FollowupTests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- P3: RegExp modifier-group validation ----

    [Fact]
    public void ModifierSameFlagAddedAndRemovedThrows()
        => Assert.Equal("SyntaxError", Eval("try { RegExp('(?s-s:a)',''); 'no throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ModifierSameFlagAddedAndRemovedThrows_I()
        => Assert.Equal("SyntaxError", Eval("try { RegExp('(?im-m:a)',''); 'no throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ModifierDuplicateFlagInGroupThrows()
        => Assert.Equal("SyntaxError", Eval("try { RegExp('(?ss:a)',''); 'no throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ValidModifierGroupStillWorks()
        => Assert.Equal("true", Eval("(new RegExp('(?i-s:a)','')).test('A').toString();"));

    [Fact]
    public void NonCapturingGroupStillWorks()
        => Assert.Equal("true", Eval("(new RegExp('(?:a)','')).test('a').toString();"));

    // ---- P8: key enumeration must not use @@iterator; proxies skip symbols ----

    [Fact]
    public void ObjectKeysWithNonCallableIteratorDoesNotThrow()
        => Assert.Equal("0", Eval("var o = {}; o[Symbol.iterator] = 'List'; Object.keys(o).length.toString();"));

    [Fact]
    public void ForInWithNonCallableIteratorDoesNotThrow()
        => Assert.Equal("0", Eval("var o = {}; o[Symbol.iterator] = 'List'; var n = 0; for (var k in o) n++; n.toString();"));

    [Fact]
    public void ForInSkipsSymbolKeys()
        => Assert.Equal("", Eval(
            "var o = {}; o[Symbol.for('m')] = 1; o[Symbol('s')] = 2; o[Symbol.iterator] = 'x';"
            + " var hit = ''; for (var k in o) hit += String(k); hit;"));

    [Fact]
    public void ObjectKeysOnProxySkipsSymbolTrapKeys()
        => Assert.Equal("a,0", Eval(
            "var h = { ownKeys: function(t){ return ['a','0',Symbol.for('m'),Symbol('s'),Symbol.iterator]; },"
            + " getOwnPropertyDescriptor: function(t,k){ return {configurable:true,enumerable:true,value:0,writable:true}; } };"
            + " Object.keys(new Proxy({}, h)).join(',');"));

    [Fact]
    public void ForInOverProxySkipsSymbols()
        => Assert.Equal("PASS", Eval(
            "var o = {}; o[Symbol.for('m')] = 1; o[Symbol.iterator] = 'x';"
            + " var p = new Proxy(o, {}); var hit = ''; for (var k in p) hit += String(k); hit === '' ? 'PASS' : 'FAIL ' + hit;"));

    [Fact]
    public void ReflectOwnKeysStillReturnsSymbols()
        => Assert.Equal("true", Eval(
            "var s = Symbol('x'); var o = {}; o.a = 1; o[s] = 2;"
            + " (Reflect.ownKeys(new Proxy(o, {})).indexOf(s) !== -1).toString();"));

    // ---- P9: yield* re-yields the delegated result object without re-boxing ----

    [Fact]
    public void YieldStarDoesNotReboxDelegatedResult()
        => Assert.Equal("PASS", Eval(DelegatingYieldHarness
            + "function* yr(e){ return yield* results(e); }"
            + " shape(collect(yr(expected)), expected);"));

    [Fact]
    public void YieldStarDeepChainDoesNotRebox()
        => Assert.Equal("PASS", Eval(DelegatingYieldHarness
            + "function* yr(e, n){ return yield* n ? yr(e, n - 1) : results(e); }"
            + " shape(collect(yr(expected, 20)), expected);"));

    [Fact]
    public void YieldStarOverProxyDoesNotRebox()
        => Assert.Equal("PASS", Eval(DelegatingYieldHarness
            + "function r7(rs){ var i=0; function it(){return this;} function next(){return rs[i++];} var ret={next:next}; ret[Symbol.iterator]=it; return ret; }"
            + "function* yr(e){ return yield* new Proxy(r7(e), {}); }"
            + " var got=[]; var r; var it=yr(expected); do { r=it.next(); got.push(r); } while(!r.done);"
            + " shape(got, expected);"));

    [Fact]
    public void YieldStarPreservesResultIdentity()
        => Assert.Equal("true", Eval(DelegatingYieldHarness
            + "function* yr(e){ return yield* results(e); }"
            + " (yr(expected).next() === expected[0]).toString();"));

    private const string DelegatingYieldHarness = @"
        function shape(got, expected){
          if (got.length !== expected.length) return 'len ' + got.length;
          for (var i = 0; i < got.length; i++) {
            var gk = Object.keys(got[i]).sort().join(','), ek = Object.keys(expected[i]).sort().join(',');
            if (gk !== ek) return 'keys[' + i + '] ' + gk + ' vs ' + ek;
            if (got[i].value !== expected[i].value) return 'value[' + i + ']';
            if (got[i].done !== expected[i].done) return 'done[' + i + ']';
          }
          return 'PASS';
        }
        function results(rs){ var i = 0; var iter = { next: function(){ return rs[i++]; } }; var ret = {}; ret[Symbol.iterator] = function(){ return iter; }; return ret; }
        function collect(iterable){ var ret = []; var r; var it = iterable[Symbol.iterator](); do { r = it.next(); ret.push(r); } while (!r.done); return ret; }
        var expected = [{ value: 1 }, { value: 34, done: true }];
    ";
}
