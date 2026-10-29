using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/703
//
// Fixed here:
//
// Problem 6 (RegExp groups) — the `groups` object on a match result, its named
//   entries, and `indices.groups` were installed with ordinary [[Set]] and the
//   groups object inherited Object.prototype. Per RegExpBuiltinExec / MakeIndices-
//   Array the groups object is ObjectCreate(null) and every property is created
//   with CreateDataProperty, so an inherited setter on Array/Object.prototype is
//   never triggered and a `__proto__` group name becomes an ordinary own property.
//
// Problem 3 (derived ctor return) — `return <call>` in a class constructor is
//   compiled as a proper tail call, yielding a JSTailCall sentinel. The sentinel is
//   a JSObject, so the [[Construct]] return-value normalization saw it as an object
//   and let it pass through unchecked — e.g. `return Symbol()` in a derived ctor did
//   not throw. NormalizeConstructorReturn now resolves the tail call before applying
//   the object/`this`/TypeError semantics.
//
// Problem 7 (for-of array iterator) — the array element enumerator used by for-of
//   and spread snapshotted the length at construction, so entries pushed during
//   traversal were not visited (and a shrink did not end iteration early). Array
//   iterators are live (CreateArrayIterator re-reads the length each step); the
//   enumerator now reads the length dynamically.
//
// Problem 6 (proxy Object.keys) — the Proxy [[OwnPropertyKeys]] trap path returned
//   every trap key even for Object.keys/values/entries, which must filter by each
//   key's [[GetOwnProperty]] [[Enumerable]] attribute (EnumerableOwnPropertyNames).
//   A non-enumerable key reported by the trap is now excluded from Object.keys while
//   the ownKeys invariant is still validated over the full key set.
public class Issue703Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    private static string Catch(string source)
        => Eval("var r; try { " + source + " r = 'ok'; } catch (e) { r = e.constructor.name; } r;").ToString();

    // ---- Problem 6: RegExp groups created with CreateDataProperty ----

    // Setting `result.groups` must not trigger an inherited setter on Array.prototype.
    [Fact]
    public void RegExpGroupsAreDefinedNotSet()
        => Assert.Equal("0", Eval(
            "var counter = 0;" +
            "Object.defineProperty(Array.prototype, 'groups', { set() { counter++; } });" +
            "/(?<x>.)/.exec('a'); counter;").ToString());

    // The groups object has a null prototype; a `__proto__` group is an own property.
    [Fact]
    public void RegExpGroupsObjectHasNullProtoAndPlainProtoKey()
        => Assert.Equal("a,true", Eval(
            "var g = /(?<__proto__>.)/.exec('a').groups;" +
            "[g.__proto__, Object.getPrototypeOf(g) === null].join(',');").ToString());

    // Named-group properties are not created with [[Set]] (no inherited setter fires).
    [Fact]
    public void RegExpNamedGroupPropertiesAreDefinedNotSet()
        => Assert.Equal("0", Eval(
            "var counter = 0;" +
            "Object.defineProperty(Object.prototype, 'x', { set() { counter++; }, configurable: true });" +
            "/(?<x>.)/.exec('a'); counter;").ToString());

    // The `indices.groups` object also uses CreateDataProperty and a null prototype.
    [Fact]
    public void RegExpIndicesGroupsAreDefinedWithNullProto()
        => Assert.Equal("0,true", Eval(
            "var counter = 0;" +
            "Object.defineProperty(Array.prototype, 'groups', { set() { counter++; } });" +
            "var ind = /(?<x>.)/d.exec('a').indices;" +
            "[counter, Object.getPrototypeOf(ind.groups) === null].join(',');").ToString());

    // ---- Problem 3: derived constructor return-value normalization ----

    // `return Symbol()` (a tail-positioned call) from a derived ctor throws TypeError.
    [Fact]
    public void DerivedConstructorReturnSymbolCallThrows()
        => Assert.Equal("TypeError", Catch(
            "class B {} class D extends B { constructor() { super(); return Symbol(); } } new D();"));

    // A non-object primitive returned via a tail call still throws.
    [Fact]
    public void DerivedConstructorReturnPrimitiveCallThrows()
        => Assert.Equal("TypeError", Catch(
            "class B {} class D extends B { constructor() { super(); return String(5); } } new D();"));

    // An object returned via a tail call passes through unchanged.
    [Fact]
    public void DerivedConstructorReturnObjectCallPassesThrough()
        => Assert.Equal("tag", Eval(
            "class B {} class D extends B { constructor() { super(); return Object({ t: 'tag' }); } }" +
            "new D().t;").ToString());

    // A base constructor ignores a non-object returned via a tail call (returns this).
    [Fact]
    public void BaseConstructorReturnPrimitiveCallReturnsThis()
        => Assert.Equal("true", Eval(
            "class B { constructor() { this.ok = true; return Symbol(); } } new B().ok;").ToString());

    // ---- Problem 7: array for-of iterator is live ----

    // Entries pushed during for-of traversal are visited.
    [Fact]
    public void ForOfVisitsEntriesPushedDuringTraversal()
        => Assert.Equal("2", Eval(
            "var array = [0], count = 0, first = 0, second = 1;" +
            "for (var x of array) { first = second; second = null;" +
            "  if (first !== null) array.push(1); count += 1; } count;").ToString());

    // Shrinking the array during traversal ends iteration early.
    [Fact]
    public void ForOfStopsWhenArrayShrinks()
        => Assert.Equal("2", Eval(
            "var a = [1, 2, 3, 4, 5], count = 0;" +
            "for (var y of a) { count++; if (y === 2) a.length = 2; } count;").ToString());

    // Spread of an array is unchanged.
    [Fact]
    public void SpreadOfArrayUnchanged()
        => Assert.Equal("1,2,3", Eval("[...[1, 2, 3]].join(',');").ToString());

    // ---- Problem 6: Object.keys over a Proxy filters by enumerability ----

    // A non-enumerable key reported by the ownKeys trap is excluded from Object.keys.
    [Fact]
    public void ObjectKeysOnProxyFiltersNonEnumerableTrapKeys()
        => Assert.Equal("0", Eval(
            "var target = {};" +
            "Object.defineProperty(target, 'prop', { value: 3, enumerable: false, configurable: true });" +
            "var proxy = new Proxy(target, { ownKeys() { return ['prop']; } });" +
            "Object.preventExtensions(target);" +
            "Object.keys(proxy).length;").ToString());

    // getOwnPropertyNames still reports the non-enumerable key.
    [Fact]
    public void ObjectGetOwnPropertyNamesOnProxyKeepsNonEnumerable()
        => Assert.Equal("a,b", Eval(
            "var t = {};" +
            "Object.defineProperty(t, 'a', { value: 1, enumerable: true, configurable: true });" +
            "Object.defineProperty(t, 'b', { value: 2, enumerable: false, configurable: true });" +
            "Object.getOwnPropertyNames(new Proxy(t, {})).join(',');").ToString());
}
