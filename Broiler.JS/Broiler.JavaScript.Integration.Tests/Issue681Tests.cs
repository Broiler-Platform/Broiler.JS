using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/681
//
// Fixed here:
//   * Problem 1 (Intl) — Intl.ListFormat / Segmenter / DisplayNames / DurationFormat
//                  use GetOptionsObject, so a defined non-object options argument is a
//                  TypeError (not coerced like NumberFormat's CoerceOptionsToObject).
//   * Problem 4  — Array destructuring (assignment and for-of) must NOT call the
//                  iterator's return() when the abrupt completion comes from a
//                  throwing next(): a throwing next sets the iterator record's
//                  [[done]] to true, so IteratorClose is skipped.
//   * Problem 6  — Object.defineProperty / defineProperties / Object.create /
//                  Reflect.defineProperty read descriptor fields (value, get, set,
//                  writable, enumerable, configurable) via ToPropertyDescriptor, which
//                  uses [[HasProperty]] and [[Get]] — i.e. inherited and accessor-backed
//                  descriptor fields are honored.
//   * Problem 9  — Date setters read [[DateValue]] but still perform ToNumber on their
//                  arguments before the "if t is NaN, return NaN" step (valueOf runs
//                  exactly once even for an invalid date). setTime, setFullYear and
//                  setUTCFullYear additionally operate on / revive invalid dates.
//
// The remaining problems are out of scope: the private-* families (P1/P3/P7) need a
// real per-evaluation PrivateName identity model (private names are currently modeled
// as shared "#name" string keys); super-property writes to a home object whose
// prototype was changed via setPrototypeOf are not resolved dynamically; the JSON
// BigInt ordering needs a SerializeJSONProperty reordering; and the AnnexB eval / Date
// valueOf-ordering and Intl-mathematical-value families are unrelated root causes.
public class Issue681Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 9: Date setters coerce arguments (valueOf) before the NaN check ----

    [Fact]
    public void DateSetterCallsValueOfOnceOnInvalidDate()
        => Assert.Equal("1|NaN|0", Eval(
            "var dt = new Date(NaN); var n = 0;"
            + "var v = { valueOf: function(){ n++; dt.setTime(0); return 1; } };"
            + "var r = dt.setHours(v);"
            + "n + '|' + r + '|' + dt.getTime()"));

    [Fact]
    public void SetTimeWorksOnInvalidDate()
        => Assert.Equal("0|0", Eval(
            "var dt = new Date(NaN); var r = dt.setTime(0); r + '|' + dt.getTime()"));

    [Fact]
    public void SetFullYearRevivesInvalidDateAndCallsValueOfOnce()
        => Assert.Equal("1|2001|false", Eval(
            "var dt = new Date(NaN); var n = 0;"
            + "var v = { valueOf: function(){ n++; return 2001; } };"
            + "var r = dt.setFullYear(v);"
            + "n + '|' + dt.getFullYear() + '|' + String(r !== r)"));

    [Fact]
    public void SetUtcFullYearRevivesInvalidDate()
        => Assert.Equal("2001-01-01T00:00:00.000Z", Eval(
            "var dt = new Date(NaN); dt.setUTCFullYear(2001); dt.toISOString()"));

    // ---- Problem 6: descriptor fields may be inherited / accessor-backed ----

    [Fact]
    public void DefinePropertyReadsInheritedSetter()
        => Assert.Equal("overrideData", Eval(
            "var data = 'data';"
            + "var proto = { set: function(v){ data = v; } };"
            + "var Ctor = function(){}; Ctor.prototype = proto;"
            + "var child = new Ctor();"
            + "var obj = {};"
            + "Object.defineProperty(obj, 'p', child);"
            + "obj.p = 'overrideData';"
            + "data"));

    [Fact]
    public void DefinePropertyReadsInheritedValue()
        => Assert.Equal("42", Eval(
            "var proto = { value: 42 };"
            + "var Ctor = function(){}; Ctor.prototype = proto;"
            + "var obj = {};"
            + "Object.defineProperty(obj, 'p', new Ctor());"
            + "String(obj.p)"));

    [Fact]
    public void DefinePropertyReadsAccessorBackedDescriptorField()
        => Assert.Equal("7", Eval(
            "var obj = {};"
            + "var desc = { get value(){ return 7; } };"
            + "Object.defineProperty(obj, 'p', desc);"
            + "String(obj.p)"));

    [Fact]
    public void ReflectDefinePropertyReadsInheritedValue()
        => Assert.Equal("42", Eval(
            "var proto = { value: 42 };"
            + "var Ctor = function(){}; Ctor.prototype = proto;"
            + "var obj = {};"
            + "Reflect.defineProperty(obj, 'p', new Ctor());"
            + "String(obj.p)"));

    [Fact]
    public void DefinePropertiesReadsInheritedValue()
        => Assert.Equal("42", Eval(
            "var proto = { value: 42 };"
            + "var Ctor = function(){}; Ctor.prototype = proto;"
            + "var obj = {};"
            + "Object.defineProperties(obj, { p: new Ctor() });"
            + "String(obj.p)"));

    // ---- Problem 4: IteratorClose skipped when next() throws ----

    [Fact]
    public void ArrayDestructuringDoesNotCloseIteratorWhenNextThrows()
        => Assert.Equal("0|1", Eval(
            "var ret = 0, next = 0;"
            + "var it = { next: function(){ next++; throw new Error('x'); },"
            + "           return: function(){ ret++; } };"
            + "var iterable = {}; iterable[Symbol.iterator] = function(){ return it; };"
            + "try { var x; [ x ] = iterable; } catch (e) {}"
            + "ret + '|' + next"));

    [Fact]
    public void ForOfDestructuringDoesNotCloseIteratorWhenNextThrows()
        => Assert.Equal("0|1", Eval(
            "var ret = 0, next = 0;"
            + "var it = { next: function(){ next++; throw new Error('x'); },"
            + "           return: function(){ ret++; } };"
            + "var iterable = {}; iterable[Symbol.iterator] = function(){ return it; };"
            + "try { for (var [ x ] of [iterable]) {} } catch (e) {}"
            + "ret + '|' + next"));

    [Fact]
    public void ArrayDestructuringClosesIteratorWhenNotExhausted()
        => Assert.Equal("1", Eval(
            "var ret = 0;"
            + "var it = { next: function(){ return { done: false, value: 1 }; },"
            + "           return: function(){ ret++; return {}; } };"
            + "var iterable = {}; iterable[Symbol.iterator] = function(){ return it; };"
            + "var x; [ x ] = iterable;"
            + "String(ret)"));

    // ---- Problem 1: Intl GetOptionsObject rejects non-object options ----

    [Fact]
    public void ListFormatRejectsPrimitiveOptions()
        => Assert.Equal("TypeError|TypeError|TypeError|TypeError", Eval(
            "function thr(f){ try { f(); return 'none'; } catch (e) { return e.constructor.name; } }"
            + "thr(function(){ new Intl.ListFormat([], 7); }) + '|' +"
            + "thr(function(){ new Intl.ListFormat([], 'x'); }) + '|' +"
            + "thr(function(){ new Intl.ListFormat([], true); }) + '|' +"
            + "thr(function(){ new Intl.ListFormat([], 123n); })"));

    [Fact]
    public void ListFormatAcceptsObjectAndUndefinedOptions()
        => Assert.Equal("conjunction", Eval(
            "new Intl.ListFormat([], undefined);"
            + "new Intl.ListFormat([], { type: 'conjunction' }).resolvedOptions().type"));
}
