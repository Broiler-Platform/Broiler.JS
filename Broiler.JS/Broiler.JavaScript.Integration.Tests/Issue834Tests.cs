using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/834
//
// Fixed here:
//
//   Problem 85 (arr[4294967295] Expected SameValue(«undefined», «100»)) and the
//   related Problem 84 (arrObj.hasOwnProperty("4294967295")) — root cause shared.
//
//   4294967295 (== 2^32 - 1 == uint.MaxValue) is a valid uint, but per spec it is
//   NOT an array index: array indices are the integers 0 .. 2^32 - 2. Only those
//   live in an array/object's dense uint element storage; 2^32 - 1 must be an
//   ordinary string-keyed property "4294967295".
//
//   JSNumber.ToKey routed 4294967295 through the uint element path (`(uint)n == n`
//   is true for it), while the string path (JSString.ToKey / NumberParser.
//   TryGetArrayIndex) correctly treated it as a string key. The two paths then
//   disagreed: `o[4294967295] = v` stored a uint element that `o["4294967295"]`,
//   `Object.keys`, and `hasOwnProperty("4294967295")` could not see, and a value
//   defined under the string key was invisible to numeric access `o[4294967295]`.
//   ToKey now excludes uint.MaxValue, matching the string path.
//
// Out of scope: the remaining ~99 problems in the issue are unrelated engine
// areas (Temporal/Intl/CLDR ordering, RegExp/Unicode, Proxy trap ordering, etc.).
public class Issue834Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 85: numeric access reaches a string-defined 2^32-1 property ----

    [Fact]
    public void NumericReadSeesPropertyDefinedAtMaxUintViaString()
        => Assert.Equal("100", Eval(
            "var a = []; Object.defineProperties(a, { '4294967295': " +
            "{ value: 100, writable: true, enumerable: true, configurable: true } }); String(a[4294967295])"));

    // ---- Problem 84: hasOwnProperty agrees with the define ----

    [Fact]
    public void HasOwnPropertyAtMaxUintIsTrueAndLengthUnaffected()
        => Assert.Equal("true,0", Eval(
            "var a = []; Object.defineProperty(a, '4294967295', " +
            "{ value: 1, writable: true, enumerable: true, configurable: true }); " +
            "a.hasOwnProperty('4294967295') + ',' + a.length"));

    // ---- the shared root cause: numeric and string access must agree at 2^32-1 ----

    [Fact]
    public void NumericSetIsVisibleViaStringKey()
        => Assert.Equal("true", Eval("var o = {}; o[4294967295] = 1; String(o.hasOwnProperty('4294967295'))"));

    [Fact]
    public void StringSetIsVisibleViaNumericAccess()
        => Assert.Equal("7", Eval("var o = {}; o['4294967295'] = 7; String(o[4294967295])"));

    [Fact]
    public void MaxUintKeyIsEnumeratedByObjectKeys()
        => Assert.Equal("4294967295", Eval("var o = {}; o[4294967295] = 1; Object.keys(o).join(',')"));

    [Fact]
    public void AssigningMaxUintIndexOnArrayDoesNotChangeLength()
        => Assert.Equal("0,5", Eval("var a = []; a[4294967295] = 5; a.length + ',' + String(a[4294967295])"));

    // ---- guard against over-correction: 2^32-2 is still a real array index ----

    [Fact]
    public void LargestValidArrayIndexStillExtendsLength()
        => Assert.Equal("4294967295", Eval("var a = []; a[4294967294] = 3; String(a.length)"));
}
