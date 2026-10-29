using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/663
//
// Fixed here:
//   * Problem 7  — RegExp.prototype[Symbol.match] and [Symbol.replace] computed
//                  their flags from the individual flag accessors (global,
//                  ignoreCase, …) instead of `ToString(Get(rx, "flags"))`. A user
//                  `flags` getter (or a `flags` value whose ToString throws) was
//                  therefore never observed, so its error did not propagate.
//   * Problem 9  — Array.prototype[Symbol.unscopables] was an ordinary object
//                  (Object.prototype as [[Prototype]]); per spec it is created
//                  with OrdinaryObjectCreate(null), so getPrototypeOf(...) is null.
//   * Problem 10 — Intl service constructors (NumberFormat, Collator,
//                  DateTimeFormat, DurationFormat, …) did not validate the
//                  `localeMatcher` option, so an invalid value was silently
//                  accepted instead of throwing a RangeError. Validated centrally
//                  in ValidateConstructorArguments.
//
// Problems 1 (sm deepEqual harness), 2 (Intl.DateTimeFormat formatRange — needs
// CLDR), 3 (IteratorClose on abrupt completion during destructuring rest), 4
// (Date.toISOString extended-year range — DateTime-backed), 5 (abrupt completion
// in `finally` overriding a pending throw) and 6 (compound-assignment PutValue
// ordering with a direct-eval var binding) are triaged in the issue and remain
// out of scope for this change (architectural / harness-specific).
public class Issue663Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problem 7: flags errors propagate from @@match / @@replace ----

    [Fact]
    public void SymbolMatchPropagatesFlagsGetterError()
        => Assert.Equal("CustomError", Eval(
            "function CustomError(){} "
            + "var obj={ get flags(){ throw new CustomError(); },"
            + " get global(){ throw new Error('global read'); } };"
            + "(function(){ try { RegExp.prototype[Symbol.match].call(obj); return 'no-throw'; }"
            + "catch(e){ return e instanceof CustomError ? 'CustomError' : 'other'; } })()"));

    [Fact]
    public void SymbolMatchPropagatesFlagsToStringError()
        => Assert.Equal("CustomError", Eval(
            "function CustomError(){} "
            + "var toStringThrows={ [Symbol.toPrimitive](){ throw new CustomError(); } };"
            + "var re=/./; Object.defineProperties(re,{"
            + " flags:{ get(){ return toStringThrows; } },"
            + " global:{ get(){ throw new Error('global read'); } } });"
            + "(function(){ try { re[Symbol.match](''); return 'no-throw'; }"
            + "catch(e){ return e instanceof CustomError ? 'CustomError' : 'other'; } })()"));

    [Fact]
    public void SymbolReplacePropagatesFlagsGetterError()
        => Assert.Equal("CustomError", Eval(
            "function CustomError(){} "
            + "var obj={ get flags(){ throw new CustomError(); },"
            + " get global(){ throw new Error('global read'); } };"
            + "(function(){ try { RegExp.prototype[Symbol.replace].call(obj, '', ''); return 'no-throw'; }"
            + "catch(e){ return e instanceof CustomError ? 'CustomError' : 'other'; } })()"));

    // A normal RegExp still reads flags through the property and matches/replaces.
    [Fact]
    public void SymbolMatchStillWorksForNormalRegExp()
        => Assert.Equal("a,a", Eval("'abab'.match(/a/g).join(',')"));

    [Fact]
    public void SymbolReplaceStillWorksForNormalRegExp()
        => Assert.Equal("XbXb", Eval("'abab'.replace(/a/g, 'X')"));

    // ---- Problem 9: Array.prototype[@@unscopables] has a null prototype ----

    [Fact]
    public void UnscopablesHasNullPrototype()
        => Assert.Equal("true", Eval(
            "String(Object.getPrototypeOf(Array.prototype[Symbol.unscopables]) === null)"));

    // value.js / array-find-from-last.js: the spec list is present as enumerable,
    // writable, configurable data properties valued `true`.
    // §23.1.3.40: the change-array-by-copy proposal added toReversed/toSorted/toSpliced to
    // the @@unscopables list but NOT "with" (a reserved word that can never name a binding
    // shadowed inside a `with` statement), so "with" must be absent (see issue #838 Problem 35).
    [Fact]
    public void UnscopablesListsTheFullSpecSetAsEnumerableTrue()
        => Assert.Equal("true", Eval(
            "var u=Array.prototype[Symbol.unscopables];"
            + "var names=['copyWithin','entries','fill','find','findIndex','findLast',"
            + "'findLastIndex','flat','flatMap','includes','keys','values',"
            + "'toReversed','toSorted','toSpliced','at'];"
            + "var ok=names.every(function(n){"
            + " var d=Object.getOwnPropertyDescriptor(u,n);"
            + " return d && d.value===true && d.writable && d.enumerable && d.configurable; });"
            + "String(ok && !('with' in u))"));

    // ---- Problem 10: invalid localeMatcher is a RangeError ----

    [Fact]
    public void NumberFormatRejectsInvalidLocaleMatcher()
        => Assert.Equal("RangeError", Eval(
            "(function(){ try { new Intl.NumberFormat('en', { localeMatcher: 'invalid' }); return 'no-throw'; }"
            + "catch(e){ return e.constructor.name; } })()"));

    [Fact]
    public void CollatorRejectsInvalidLocaleMatcher()
        => Assert.Equal("RangeError", Eval(
            "(function(){ try { new Intl.Collator('en', { localeMatcher: 'invalid' }); return 'no-throw'; }"
            + "catch(e){ return e.constructor.name; } })()"));

    [Fact]
    public void DateTimeFormatRejectsInvalidLocaleMatcher()
        => Assert.Equal("RangeError", Eval(
            "(function(){ try { new Intl.DateTimeFormat('en', { localeMatcher: 'invalid' }); return 'no-throw'; }"
            + "catch(e){ return e.constructor.name; } })()"));

    // Valid localeMatcher values are accepted.
    [Fact]
    public void NumberFormatAcceptsValidLocaleMatcher()
        => Assert.Equal("object", Eval(
            "typeof new Intl.NumberFormat('en', { localeMatcher: 'lookup' })"));

    [Fact]
    public void DateTimeFormatAcceptsBestFitLocaleMatcher()
        => Assert.Equal("object", Eval(
            "typeof new Intl.DateTimeFormat('en', { localeMatcher: 'best fit' })"));
}
