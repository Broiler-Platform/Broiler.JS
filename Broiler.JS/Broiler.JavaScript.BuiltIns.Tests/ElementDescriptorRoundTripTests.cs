using System.Runtime.CompilerServices;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Dense indexed storage keeps bare values rather than descriptors
// (docs/performance-roadmap.md P2-3): the attributes, the absent accessors and the key are
// implied by the storage mode, and a full descriptor is rebuilt whenever one is asked for.
// That trade only holds if the rebuild is exact and if every non-default descriptor really
// does leave the compact store first, so this covers the observable descriptor surface —
// reading descriptors back, defining them, and crossing between the two representations.
public class ElementDescriptorRoundTripTests
{
    private static void Load() => RuntimeHelpers.RunClassConstructor(typeof(Clr.DefaultClrInterop).TypeHandle);

    // `d(o, k)` flattens an element's descriptor into value,writable,enumerable,configurable
    // plus whether it is an accessor at all.
    private const string Describe =
        "function d(o, k) { var x = Object.getOwnPropertyDescriptor(o, k); return x === undefined ? 'none' " +
        ": [x.value, x.writable, x.enumerable, x.configurable, 'get' in x].join(','); } ";

    private static string Eval(string source)
    {
        Load();
        using var ctx = new JSContext();
        return ctx.Eval(Describe + source).ToString();
    }

    // ── the rebuilt descriptor ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("var a = [5];", "0")]
    [InlineData("var a = []; a[0] = 5;", "0")]
    [InlineData("var a = []; a.push(5);", "0")]
    [InlineData("var a = new Array(4); a[2] = 5;", "2")]
    [InlineData("var a = {}; a[0] = 5;", "0")]
    [InlineData("var a = [1, 2, 3].map(function () { return 5; });", "1")]
    [InlineData("var a = []; a[3] = 5;", "3")]
    public void ADenseElementDescribesItselfAsAPlainDataProperty(string prepare, string key)
        => Assert.Equal("5,true,true,true,false", Eval(prepare + " d(a, " + key + ");"));

    [Fact(Timeout = 600000)]
    public void AHoleHasNoDescriptor()
        => Assert.Equal("none", Eval("var a = [1, 2]; delete a[0]; d(a, 0);"));

    [Fact(Timeout = 600000)]
    public void ADescriptorSurvivesTheMoveToSparseStorage()
        // Going sparse rewrites every dense slot into a real descriptor; the ones that were
        // already there have to come out exactly as they went in.
        => Assert.Equal("1,true,true,true,false|9,true,true,true,false", Eval(
            "var a = [1, 2, 3]; a[100000] = 9; d(a, 0) + '|' + d(a, 100000);"));

    [Fact(Timeout = 600000)]
    public void ADescriptorSurvivesACustomDescriptorElsewhere()
        // Defining one non-default element moves the whole array out of compact storage.
        => Assert.Equal("1,true,true,true,false", Eval(
            "var a = [1, 2]; Object.defineProperty(a, 1, { value: 7, writable: false }); d(a, 0);"));

    // ── non-default descriptors keep working ──────────────────────────────────────────

    [Theory]
    [InlineData("{ value: 7, writable: false, enumerable: true, configurable: true }", "7,false,true,true,false")]
    [InlineData("{ value: 7, writable: true, enumerable: false, configurable: true }", "7,true,false,true,false")]
    [InlineData("{ value: 7, writable: true, enumerable: true, configurable: false }", "7,true,true,false,false")]
    // An omitted field leaves the existing attribute alone, so this one stays fully default.
    [InlineData("{ value: 7 }", "7,true,true,true,false")]
    public void ADefinedElementKeepsItsAttributes(string descriptor, string expected)
        => Assert.Equal(expected, Eval(
            "var a = [1, 2]; Object.defineProperty(a, 0, " + descriptor + "); d(a, 0);"));

    [Fact(Timeout = 600000)]
    public void AnAccessorElementStaysAnAccessor()
        => Assert.Equal("42,undefined,true,true", Eval(
            "var a = []; var seen; " +
            "Object.defineProperty(a, 0, { get: function () { return 42; }, " +
            "set: function (v) { seen = v; }, configurable: true }); " +
            "a[0] = 9; " +
            "var x = Object.getOwnPropertyDescriptor(a, 0); " +
            "[a[0], String(x.value), typeof x.get === 'function', seen === 9].join(',');"));

    [Fact(Timeout = 600000)]
    public void RedefiningAnAccessorBackToAValueRestoresADataProperty()
        => Assert.Equal("7,true,true,true,false", Eval(
            "var a = []; Object.defineProperty(a, 0, { get: function () { return 1; }, configurable: true }); " +
            "Object.defineProperty(a, 0, { value: 7, writable: true, enumerable: true, configurable: true }); " +
            "d(a, 0);"));

    [Fact(Timeout = 600000)]
    public void ANonConfigurableElementCannotBeDeleted()
        => Assert.Equal("false,1", Eval(
            "var a = [1]; Object.defineProperty(a, 0, { configurable: false }); [delete a[0], a[0]].join(',');"));

    // ── enumeration and ownKeys ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("var a = [1, 2, 3];", "0,1,2")]
    [InlineData("var a = [1, 2, 3]; delete a[1];", "0,2")]
    [InlineData("var a = []; a[2] = 1; a[0] = 1;", "0,2")]
    [InlineData("var a = []; a[7] = 1; a[100000] = 1; a[3] = 1;", "3,7,100000")]
    public void KeysComeOutInAscendingIndexOrder(string prepare, string expected)
        => Assert.Equal(expected, Eval(prepare + " Object.keys(a).join(',');"));

    [Fact(Timeout = 600000)]
    public void ANonEnumerableElementIsHiddenButStillOwned()
        => Assert.Equal("1|0,1|true", Eval(
            "var a = [1, 2]; Object.defineProperty(a, 0, { enumerable: false }); " +
            "[Object.keys(a).join(','), Object.getOwnPropertyNames(a).slice(0, 2).join(','), " +
            "a.hasOwnProperty(0)].join('|');"));

    [Fact(Timeout = 600000)]
    public void PropertyIsEnumerableReadsTheRebuiltDescriptor()
        => Assert.Equal("true,false,false", Eval(
            "var a = [1, 2]; Object.defineProperty(a, 1, { enumerable: false }); " +
            "[a.propertyIsEnumerable(0), a.propertyIsEnumerable(1), a.propertyIsEnumerable(5)].join(',');"));

    [Fact(Timeout = 600000)]
    public void ForInSkipsHolesAndNonEnumerables()
        => Assert.Equal("0,3", Eval(
            "var a = [1, 2, 3, 4]; delete a[1]; Object.defineProperty(a, 2, { enumerable: false }); " +
            "var s = []; for (var k in a) s.push(k); s.join(',');"));

    // ── bulk mutation keeps descriptors intact ────────────────────────────────────────

    [Theory]
    [InlineData("a.fill(9, 1, 3);", "1,9,9,4", "9,true,true,true,false")]
    [InlineData("a.copyWithin(0, 2);", "3,4,3,4", "4,true,true,true,false")]
    [InlineData("a.reverse();", "4,3,2,1", "3,true,true,true,false")]
    [InlineData("a.sort(function (x, y) { return y - x; });", "4,3,2,1", "3,true,true,true,false")]
    public void BulkOperationsLeaveOrdinaryElementsBehind(string operation, string contents, string descriptor)
        => Assert.Equal(contents + "|" + descriptor, Eval(
            "var a = [1, 2, 3, 4]; " + operation + " a.join(',') + '|' + d(a, 1);"));

    [Fact(Timeout = 600000)]
    public void FillRebuildsDefaultDescriptors()
        => Assert.Equal("9,9,9|9,true,true,true,false", Eval(
            "var a = new Array(3); a.fill(9); a.join(',') + '|' + d(a, 1);"));

    [Fact(Timeout = 600000)]
    public void BulkOperationsRespectANonWritableElement()
        // With a custom descriptor in play the array is no longer eligible for the
        // descriptor-free bulk path, and the spec's per-element rules must reassert.
        => Assert.Equal("TypeError,1", Eval(
            "'use strict'; var a = [1, 2, 3]; Object.defineProperty(a, 0, { writable: false }); " +
            "var e = 'none'; try { a.fill(9); } catch (x) { e = x.constructor.name; } [e, a[0]].join(',');"));

    [Fact(Timeout = 600000)]
    public void SortMovesHolesToTheEnd()
        => Assert.Equal("4,1,2,3,true", Eval(
            "var a = [2, 1]; a[3] = 3; a.sort(); [a.length, a[0], a[1], a[2], !(3 in a)].join(',');"));

    // ── round trips through the whole value ───────────────────────────────────────────

    [Theory]
    [InlineData("[1, [2, 3], { x: 4 }]", "[1,[2,3],{\"x\":4}]")]
    [InlineData("[1, , 3]", "[1,null,3]")]
    [InlineData("[]", "[]")]
    public void JsonSerializationIsUnchanged(string literal, string expected)
        => Assert.Equal(expected, Eval("JSON.stringify(" + literal + ");"));

    [Fact(Timeout = 600000)]
    public void SliceAndConcatProduceOrdinaryElements()
        => Assert.Equal("2,3|3,true,true,true,false", Eval(
            "var a = [1, 2, 3, 4].slice(1, 3).concat([]); a.join(',') + '|' + d(a, 1);"));

    [Fact(Timeout = 600000)]
    public void SpliceKeepsTheSurvivingElementsOrdinary()
        => Assert.Equal("1,x,4|x,true,true,true,false", Eval(
            "var a = [1, 2, 3, 4]; a.splice(1, 2, 'x'); a.join(',') + '|' + d(a, 1);"));

    [Fact(Timeout = 600000)]
    public void SpreadAndDestructuringSeeHolesAsUndefined()
        => Assert.Equal("1,undefined,3|3", Eval(
            "var a = [1, , 3]; var x = a[0], y = a[1], z = a[2]; " +
            "[x, String(y), z].join(',') + '|' + [...a].length;"));

    [Fact(Timeout = 600000)]
    public void ObjectAssignCopiesElementsAsPlainProperties()
        => Assert.Equal("1,2|1,true,true,true,false", Eval(
            "var a = Object.assign({}, [1, 2]); [a[0], a[1]].join(',') + '|' + d(a, 0);"));

    [Fact(Timeout = 600000)]
    public void FreezeReportsEveryElementLockedDown()
        => Assert.Equal("true|1,false,true,false,false", Eval(
            "var a = Object.freeze([1, 2]); Object.isFrozen(a) + '|' + d(a, 0);"));

    [Fact(Timeout = 600000)]
    public void SealLeavesElementsWritable()
        => Assert.Equal("true|1,true,true,false,false", Eval(
            "var a = Object.seal([1, 2]); Object.isSealed(a) + '|' + d(a, 0);"));

    [Fact(Timeout = 600000)]
    public void MappedArgumentsElementsDescribeNormally()
        => Assert.Equal("9,true,true,true,false", Eval(
            "function f() { arguments[0] = 9; return d(arguments, 0); } f(1, 2);"));

    [Fact(Timeout = 600000)]
    public void StringExoticIndicesAreUnaffected()
        => Assert.Equal("b,false,true,false,false", Eval("d(Object('abc'), 1);"));

    [Fact(Timeout = 600000)]
    public void TypedArrayIndicesAreUnaffected()
        => Assert.Equal("5,true,true,true,false", Eval("var a = new Int32Array(4); a[1] = 5; d(a, 1);"));
}
