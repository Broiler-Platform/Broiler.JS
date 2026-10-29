using System;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/691
//
// Fixed here (Problem 10 — Array.prototype.map / filter on array-likes whose
// indexed elements are inherited from the prototype chain):
//
//   Array.prototype.map and Array.prototype.filter iterate per spec with
//   HasProperty(O, k) then Get(O, k), both of which walk the prototype chain.
//   The implementation instead used JSObject.TryGetElement, which only inspects
//   an object's OWN indexed storage. So when map/filter were applied to an
//   array-like whose length and indexed elements live on the prototype — most
//   visibly a boxed primitive `this` (ToObject(false) is a Boolean wrapper that
//   inherits Boolean.prototype[0] / Boolean.prototype.length) — every index was
//   treated as a hole, the callback never ran, and the result array came back
//   empty (testResult[0] === undefined). Both methods now use the
//   prototype-walking TryGetArrayLikeElement helper already used by every /
//   some / forEach / reduce.
//
// Out of scope (architectural / CLDR / deep parser, matching prior triage in
// #683 / #685 / #687 / #689): the private-* brand-check families,
// super-*-reference-null, AnnexB eval binding re-init / skip-early-err,
// scope-param-elem-var, derived-class-return-override,
// computed-property-abrupt-completion, NumberFormat signDisplay "negative"
// currency CLDR formatting, and the staging/sm negative SyntaxError grab-bag.
public class Issue691Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- map visits indices inherited from the prototype chain ----

    // The exact shape of built-ins/Array/prototype/map/15.4.4.19-1-3.js: map
    // applied to a boolean primitive, whose boxed wrapper inherits its index 0
    // and length from Boolean.prototype.
    [Fact]
    public void MapAppliedToBooleanPrimitiveVisitsInheritedIndex()
        => Assert.Equal("true", Eval(
            "Boolean.prototype[0] = true;"
            + "Boolean.prototype.length = 1;"
            + "var testResult = Array.prototype.map.call(false, function(val, idx, obj) {"
            + "  return obj instanceof Boolean;"
            + "});"
            + "String(testResult[0])"));

    // The callback's value argument must be the inherited element value, not
    // undefined.
    [Fact]
    public void MapPassesInheritedElementValueToCallback()
        => Assert.Equal("99", Eval(
            "Number.prototype[0] = 99;"
            + "Number.prototype.length = 1;"
            + "var r = Array.prototype.map.call(0, function(val) { return val; });"
            + "String(r[0])"));

    // A plain array-like object whose indexed property is only present on its
    // prototype is iterated too.
    [Fact]
    public void MapVisitsIndexInheritedFromObjectPrototype()
        => Assert.Equal("inherited", Eval(
            "var proto = { 0: 'inherited' };"
            + "var arrayLike = Object.create(proto);"
            + "arrayLike.length = 1;"
            + "var r = Array.prototype.map.call(arrayLike, function(v) { return v; });"
            + "r[0]"));

    // Own holes are still skipped: only present indices (own or inherited) run.
    [Fact]
    public void MapStillSkipsHolesWithNoInheritedIndex()
        => Assert.Equal("a,,c", Eval(
            "var a = ['a', , 'c'];"
            + "var r = a.map(function(v) { return v; });"
            + "r.join(',')"));

    // ---- filter shares the same iteration semantics ----

    [Fact]
    public void FilterAppliedToBooleanPrimitiveVisitsInheritedIndex()
        => Assert.Equal("1", Eval(
            "Boolean.prototype[0] = true;"
            + "Boolean.prototype.length = 1;"
            + "var r = Array.prototype.filter.call(false, function(val, idx, obj) {"
            + "  return obj instanceof Boolean;"
            + "});"
            + "String(r.length)"));

    [Fact]
    public void FilterVisitsIndexInheritedFromObjectPrototype()
        => Assert.Equal("keep", Eval(
            "var proto = { 0: 'keep' };"
            + "var arrayLike = Object.create(proto);"
            + "arrayLike.length = 1;"
            + "var r = Array.prototype.filter.call(arrayLike, function() { return true; });"
            + "r[0]"));
}
