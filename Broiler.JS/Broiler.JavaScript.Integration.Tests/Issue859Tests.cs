using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/859 — test262 script-host
// failures fixed across three passes:
//
//  • Problem 1: a Temporal type's positional `calendar` argument (the constructor) must be a
//    bare calendar identifier String, NOT a full Temporal ISO string. Only the `calendar`
//    field of a from / with property bag accepts an ISO string (per ToTemporalCalendarSlotValue);
//    the constructor uses the stricter CanonicalizeUValue grammar.
//  • Problem 2: TypedArray.prototype.fill re-validates the receiver's in-bounds state AFTER
//    coercing value / start / end — a `valueOf` that resizes the underlying resizable buffer
//    underneath a fixed-length view is now a TypeError per spec steps 10–12.
//  • Problem 3: the bare "islamic" calendar identifier is an Intl.DateTimeFormat-only locale
//    fallback. Temporal requires an unambiguous variant ("islamic-civil", "islamic-tbla",
//    "islamic-umalqura"), so "islamic" alone in Temporal must be a RangeError.
//  • Problem 4: the hebrew calendar has 12 regular months ("M01".."M12") plus the leap
//    "M05L" (Adar I); "M13" never exists and was silently being accepted, returning a
//    nonsensical month ordinal. It is now a RangeError.
//  • Problem 5: in a Temporal property bag, monthCode is a String-typed field
//    (PrepareCalendarFields "to-monthcode" conversion). A non-string value (e.g. monthCode:5)
//    is a TypeError before the format / calendar-suitability checks that raise a RangeError.
//  • Problem 6: String.prototype.replace's non-functional replaceValue is ToString-coerced
//    BEFORE the search, even when the search does not match — so a `{ toString() { calls++ } }`
//    sees its toString invoked exactly once whether or not the substring is found.
//  • Problem 8: class-field ASI in `field = lhs \n in \n rhs`. The `in` operator binds across
//    the newlines so the field initializer is `lhs in rhs`; the binary-expression lowering
//    used to over-consume the trailing line terminator and then trip the field parser's
//    end-of-statement check on the next field's leading identifier.
//  • Problem 9: a parenthesized optional chain `(a?.b)()` closes the chain at the parens but
//    preserves the inner member's Reference, so the call must invoke `a.b` with `this = a`
//    (matching the non-optional `(a.b)()`). The previous lowering compiled the parenthesized
//    chain through the no-this call path and lost the receiver.
//  • Problem 13: `Object.prototype` itself is a non-writable data property of the Object
//    constructor (writable: false, enumerable: false, configurable: false). Every other
//    built-in constructor was already installing its prototype as ReadonlyValue; Object alone
//    inherited JSFunction's writable default and had to be re-installed.
//  • Problem 11: in a relativeTo property bag, `offset` (like monthCode) is required to BE
//    a String — a Number / null / Boolean / BigInt is now a TypeError, ahead of the
//    RangeError that a malformed offset STRING produces. Both pass through ToPrimitive on
//    Objects so a custom toString is still honoured.
//  • Problem 14: a relativeTo property bag passed to Duration.round / Duration.total used to
//    drop the era / eraYear fields when normalising the bag, so a non-finite eraYear (e.g.
//    Infinity) never triggered its RangeError and the downstream date-fields resolution
//    raised a "missing year (or era and eraYear)" TypeError instead. They are now copied
//    (and coerced) in alphabetical order alongside the other date fields.
//  • Problem 15: ArrayBuffer / SharedArrayBuffer constructors check ToIndex(length) against
//    2^53-1 first (spec, before object creation), but the host-side allocation bound
//    (length > int.MaxValue) is part of CreateByteDataBlock and runs AFTER
//    OrdinaryCreateFromConstructor reads NewTarget.prototype.
//  • Problem 17: a `?.[expr]` link must not evaluate `expr` when the chain has already
//    short-circuited; the side effects of the computed key are observable per spec only on
//    the non-short-circuit path.
//  • Problem 19: the RegExp constructor reads NewTarget.prototype during RegExpAlloc, AFTER
//    capturing source/flags but BEFORE RegExpInitialize ToStrings the flags argument — so a
//    flags object whose toString observes the prototype getter sees it as already fired.
//  • Problem 20: DataView re-validates byteOffset / byteLength against the buffer's CURRENT
//    byte length after OrdinaryCreateFromConstructor fires NewTarget's prototype getter — a
//    getter that resizes the buffer underneath the view turns a previously-valid range into
//    a RangeError per spec steps 13/14a.
//  • Problem 21: BigInt.prototype.toString (radix) and BigInt.prototype.toLocaleString
//    (reserved) both have an optional parameter, so per spec their "length" property is 0
//    (not 1 / 2 as inferred from the C# argument-accessor calls).
//  • Problem 22: a "t" subtag INSIDE a privateuse area (e.g. "cmn-hans-cn-x-t-u") is a
//    private subtag, not a malformed transformed extension — the language-tag validator now
//    stops scanning at the "x" singleton that terminates the extension area.
//  • Problem 26: a derived constructor's `eval("eval('super()')")` (nested direct eval
//    calling super()) used to throw "Cannot access 'this' before initialization" because
//    the OUTER eval's compilation did not forward its inherited super-call capability to
//    the nested inner eval — `allowSuperCall` for the inner eval requires either pending
//    member inits on the root scope (visible at the constructor compile site, but not at
//    the outer eval's compile site) or, now, a runtime signal that the surrounding direct
//    eval was itself granted super-call capability.
//  • Problem 30: a `\P{X}` inside a character class (e.g. `[\p{Hex}\P{Hex}]`) used to be
//    rejected because .NET regex disallows nested negated classes. The translator now emits
//    the BMP COMPLEMENT of X's ranges as a plain class fragment, recovering the negation
//    without needing nesting.
//  • Problem 23: an unqualified `arguments` reference inside a direct eval used to resolve
//    against the eval program's own (empty, unmapped) arguments object, so probes such as
//    `arguments.callee` reached the strict-mode throw-type-error poison instead of the
//    ENCLOSING function's mapped arguments. The compiler now routes the eval-side
//    `arguments` lookup through the runtime scope-chain resolver and materializes the
//    enclosing function's `arguments` binding eagerly whenever its body (or parameter
//    list, or any inner arrow function — arrows inherit `arguments`) contains a direct
//    eval, so the binding actually exists for the scope walker to find.
public class Issue859Tests
{
    private static JSValue Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code);
    }

    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(
            "(function(){ try { " + code + "; return 'no throw'; } catch (e) { return e.constructor.name; } })()")
            .ToString();
    }

    // ───────────── Problem 21: BigInt.prototype.toString / toLocaleString length ─────────────

    [Fact]
    public void BigIntPrototypeToStringLengthIsZero()
        => Assert.Equal("0", Eval("'' + BigInt.prototype.toString.length").ToString());

    [Fact]
    public void BigIntPrototypeToLocaleStringLengthIsZero()
        => Assert.Equal("0", Eval("'' + BigInt.prototype.toLocaleString.length").ToString());

    // ───────────── Problem 3: bare "islamic" calendar is rejected by Temporal ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from({ year: 1446, month: 7, day: 1, calendar: 'islamic' })")]
    [InlineData("Temporal.PlainDateTime.from({ year: 1446, month: 7, day: 1, calendar: 'islamic' })")]
    [InlineData("Temporal.PlainMonthDay.from({ monthCode: 'M07', day: 1, calendar: 'islamic' })")]
    [InlineData("Temporal.PlainYearMonth.from({ year: 1446, month: 7, calendar: 'islamic' })")]
    public void BareIslamicCalendarIsRejected(string call)
        => Assert.Equal("RangeError", ErrorName(call));

    [Fact]
    public void SuffixedIslamicCalendarIsAccepted()
        => Assert.Equal("islamic-civil", Eval(
            "Temporal.PlainDate.from({ year: 1446, month: 7, day: 1, calendar: 'islamic-civil' }).calendarId").ToString());

    // ───────────── Problem 4: hebrew "M13" is invalid (the leap month is "M05L") ─────────────

    [Theory]
    [InlineData(5784)] // a leap year (5784 = 2023–2024)
    [InlineData(5783)] // a non-leap year
    public void HebrewMonthCodeThirteenIsRejected(int year)
        => Assert.Equal("RangeError", ErrorName(
            $"Temporal.PlainDate.from({{ year: {year}, monthCode: 'M13', day: 1, calendar: 'hebrew' }})"));

    [Fact]
    public void HebrewLeapMonthCodeIsAccepted()
        => Assert.Equal("M05L", Eval(
            "Temporal.PlainDate.from({ year: 5784, monthCode: 'M05L', day: 1, calendar: 'hebrew' }).monthCode").ToString());

    // ───────────── Problem 5: a non-string monthCode is a TypeError ─────────────

    [Theory]
    [InlineData("Temporal.PlainDate.from({ year: 2024, monthCode: 5, day: 1 })")]
    [InlineData("Temporal.PlainDateTime.from({ year: 2024, monthCode: 5, day: 1 })")]
    [InlineData("Temporal.PlainMonthDay.from({ monthCode: 5, day: 1 })")]
    [InlineData("Temporal.PlainYearMonth.from({ year: 2024, monthCode: 5 })")]
    [InlineData("Temporal.PlainDate.from({ year: 2024, monthCode: true, day: 1 })")]
    [InlineData("Temporal.PlainDate.from({ year: 1446, monthCode: 7, day: 1, calendar: 'islamic-civil' })")]
    public void NonStringMonthCodeIsTypeError(string call)
        => Assert.Equal("TypeError", ErrorName(call));

    // ───────────── Problem 14: Duration relativeTo eraYear Infinity is a RangeError ─────────────

    [Theory]
    [InlineData("Temporal.Duration.from({ months: 1 }).round({ largestUnit: 'days', relativeTo: { era: 'ce', eraYear: Infinity, month: 1, day: 1, calendar: 'gregory' } })")]
    [InlineData("Temporal.Duration.from({ months: 1 }).total({ unit: 'days', relativeTo: { era: 'ce', eraYear: Infinity, month: 1, day: 1, calendar: 'gregory' } })")]
    [InlineData("Temporal.Duration.from({ months: 1 }).round({ largestUnit: 'days', relativeTo: { era: 'ce', eraYear: -Infinity, month: 1, day: 1, calendar: 'gregory' } })")]
    [InlineData("Temporal.Duration.from({ months: 1 }).round({ largestUnit: 'days', relativeTo: { era: 'ce', eraYear: NaN, month: 1, day: 1, calendar: 'gregory' } })")]
    public void DurationRelativeToInfiniteEraYearIsRangeError(string call)
        => Assert.Equal("RangeError", ErrorName(call));

    // ───────────── Problem 6: String.prototype.replace ToString-coerces non-functional replaceValue
    //                          before searching (so toString runs even on a no-match) ─────────────

    [Fact]
    public void ReplaceCoercesNonFunctionalReplaceValueEvenWithoutMatch()
        => Assert.Equal("1,", Eval(
            "(function(){var n=0;var rv={toString:function(){n+=1;return 'b';}};var r=''.replace('a',rv);return n+','+r;})()").ToString());

    [Fact]
    public void ReplaceCoercesRegExpObjectToStringEvenWithoutMatch()
        => Assert.Equal("1", Eval(
            "(function(){var n=0;var rv=/$/;var old=rv.toString.bind(rv);rv.toString=function(){n+=1;return old();};''.replace('a',rv);return '' + n;})()").ToString());

    // ───────────── Problem 9: parenthesized optional chain preserves `this` for the outer call

    [Theory]
    [InlineData("a?.b().c")]
    [InlineData("(a?.b)().c")]
    [InlineData("a.b?.().c")]
    [InlineData("(a.b)?.().c")]
    [InlineData("a?.b?.().c")]
    [InlineData("(a?.b)?.().c")]
    public void OptionalCallPreservesThis(string expr)
        => Assert.Equal("42", Eval(
            "(function(){const a={b(){return this._b;},_b:{c:42}};return '' + (" + expr + ");})()").ToString());

    // ───────────── Problem 13: Object.prototype is a non-writable property of Object ─────────────

    [Fact]
    public void ObjectPrototypeIsNonWritable()
        => Assert.Equal("false,false,false", Eval(
            "(function(){var d=Object.getOwnPropertyDescriptor(Object,'prototype');return d.writable+','+d.enumerable+','+d.configurable;})()").ToString());

    [Fact]
    public void ObjectPrototypeAssignmentIsSilentlyIgnoredInLooseMode()
        => Assert.Equal("true", Eval(
            "(function(){var before=Object.prototype;Object.prototype={};return '' + (Object.prototype===before);})()").ToString());

    // ───────────── Problem 15: ArrayBuffer / SharedArrayBuffer fire NewTarget.prototype
    //                          before the host-side data-block RangeError ─────────────

    [Fact]
    public void ArrayBufferReadsNewTargetPrototypeBeforeAllocationCheck()
        => Assert.Equal("EvalError", ErrorName(
            "var nt = Object.defineProperty(function(){}.bind(null),'prototype',{get(){throw new EvalError('p');}});"
            + "Reflect.construct(ArrayBuffer, [7 * 1125899906842624], nt)"));

    [Fact]
    public void SharedArrayBufferReadsNewTargetPrototypeBeforeAllocationCheck()
        => Assert.Equal("EvalError", ErrorName(
            "var nt = Object.defineProperty(function(){}.bind(null),'prototype',{get(){throw new EvalError('p');}});"
            + "Reflect.construct(SharedArrayBuffer, [7 * 1125899906842624], nt)"));

    // ───────────── Problem 17: a `?.[expr]` computed key is not evaluated when the chain
    //                          short-circuits ─────────────

    [Fact]
    public void OptionalComputedKeyIsNotEvaluatedWhenChainShortCircuits()
        => Assert.Equal("0", Eval(
            "(function(){let touched=0,count=0;const obj={get a(){return undefined;}};"
            + "for(;count<1;obj?.a?.[touched++]){count++;}return '' + touched;})()").ToString());

    // ───────────── Problem 19: RegExp constructor reads NewTarget.prototype before
    //                          ToString(flags) (RegExpInitialize runs AFTER RegExpAlloc) ─────────────

    [Fact]
    public void RegExpConstructorReadsPrototypeBeforeFlagsToString()
        => Assert.Equal("true,true,true", Eval(
            "(function(){var didLookup=false;var re=/a/;"
            + "var flags={toString(){return didLookup ? 'g' : 'X';}};"
            + "var nt=Object.defineProperty(function(){}.bind(null),'prototype',{get(){didLookup=true;return RegExp.prototype;}});"
            + "var r=Reflect.construct(RegExp,[re,flags],nt);"
            + "return didLookup+','+(Object.getPrototypeOf(r)===RegExp.prototype)+','+(r.flags==='g');})()").ToString());

    // ───────────── Problem 20: DataView re-checks bounds against the (possibly resized)
    //                          buffer after OrdinaryCreateFromConstructor ─────────────

    [Fact]
    public void DataViewBoundsAreRecheckedAfterPrototypeAccessResizesTheBuffer()
        => Assert.Equal("RangeError", ErrorName(
            "var buf=new ArrayBuffer(3,{maxByteLength:3});"
            + "var nt=Object.defineProperty(function(){}.bind(null),'prototype',{get(){buf.resize(2);return DataView.prototype;}});"
            + "Reflect.construct(DataView,[buf,1,2],nt)"));

    [Fact]
    public void DataViewOutOfRangeOffsetStillThrowsBeforePrototypeAccess()
        => Assert.Equal("RangeError", ErrorName(
            "var nt=Object.defineProperty(function(){}.bind(null),'prototype',{get(){throw new EvalError('p');}});"
            + "Reflect.construct(DataView,[new ArrayBuffer(0),10],nt)"));

    // ───────────── Problem 22: a "t" subtag inside a privateuse area is a private subtag,
    //                          not a malformed transformed extension ─────────────

    [Theory]
    [InlineData("cmn-hans-cn-t-ca-u-ca-x-t-u")]
    [InlineData("cmn-hans-cn-x-t-u")]
    [InlineData("cmn-hans-cn-u-ca-x-t-u")]
    [InlineData("aa-a-foo-x-a-foo-bar")]
    public void PrivateUseSubtagsAreNotValidatedAsTransformedExtensions(string tag)
        => Assert.Equal("ok", Eval(
            "(function(){try{new Intl.Collator(['" + tag + "']);return 'ok';}catch(e){return 'fail:'+e.message;}})()").ToString());

    // ───────────── Problem 1: positional `calendar` argument rejects ISO strings ─────────────

    [Theory]
    [InlineData("new Temporal.PlainDate(2000, 5, 2, '')")]
    [InlineData("new Temporal.PlainDate(2000, 5, 2, '1997-12-04[u-ca=iso8601]')")]
    [InlineData("new Temporal.PlainDate(2000, 5, 2, 'notacal')")]
    [InlineData("new Temporal.PlainDate(2000, 5, 2, '11111111')")]
    [InlineData("new Temporal.PlainDate(2000, 5, 2, '1111-11-11')")]
    [InlineData("new Temporal.PlainDateTime(2000, 5, 2, 0, 0, 0, 0, 0, 0, '1997-12-04[u-ca=iso8601]')")]
    [InlineData("new Temporal.PlainMonthDay(5, 2, '1997-12-04[u-ca=iso8601]')")]
    [InlineData("new Temporal.PlainYearMonth(2000, 5, '1997-12-04[u-ca=iso8601]')")]
    [InlineData("new Temporal.ZonedDateTime(0n, 'UTC', '1997-12-04[u-ca=iso8601]')")]
    public void TemporalPositionalCalendarRejectsIsoStrings(string call)
        => Assert.Equal("RangeError", ErrorName(call));

    [Fact]
    public void TemporalPositionalCalendarNonStringIsTypeError()
        => Assert.Equal("TypeError", ErrorName("new Temporal.PlainDate(2000, 5, 2, 42)"));

    // ───────────── Problem 2: TypedArray.fill re-validates after argument coercion ─────────────

    [Fact]
    public void TypedArrayFillRejectsOutOfBoundsAfterValueOfResize()
        => Assert.Equal("TypeError", ErrorName(
            "var rab=new ArrayBuffer(16,{maxByteLength:32});"
            + "var fl=new Int32Array(rab,0,4);"
            + "var evil={valueOf(){rab.resize(8);return 3;}};"
            + "fl.fill(evil, 1, 2)"));

    [Fact]
    public void TypedArrayFillRejectsOutOfBoundsAfterStartValueOfResize()
        => Assert.Equal("TypeError", ErrorName(
            "var rab=new ArrayBuffer(16,{maxByteLength:32});"
            + "var fl=new Int32Array(rab,0,4);"
            + "var evil={valueOf(){rab.resize(8);return 1;}};"
            + "fl.fill(3, evil, 2)"));

    // ───────────── Problem 8: class-field ASI when the initializer continues with `in` ─────

    [Fact]
    public void ClassFieldsAsiWithInOperatorAcrossNewlines()
        => Assert.Equal("true,false,false,false", Eval(
            "(function(){var x=0,y=1,z=[42];"
            + "class C{ a = x\nin\nz\nb = y\nin\nz }"
            + "var c=new C();"
            + "return [c.a, c.b, Object.prototype.hasOwnProperty.call(c,'in'), Object.prototype.hasOwnProperty.call(c,'z')].join(',');"
            + "})()").ToString());

    // ───────────── Problem 11: Duration relativeTo offset must BE a String ─────────────

    [Theory]
    [InlineData("0")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("1000n")]
    public void DurationRelativeToNonStringOffsetIsTypeError(string offsetExpr)
        => Assert.Equal("TypeError", ErrorName(
            "Temporal.Duration.from({hours:24}).round({largestUnit:'years',relativeTo:"
            + "{year:2021,month:10,day:28,offset:" + offsetExpr + ",timeZone:'UTC'}})"));

    [Theory]
    [InlineData("'00:00'")]    // missing sign
    [InlineData("'+0'")]        // too short
    [InlineData("'-000:00'")]   // too long
    [InlineData("'+00:0000'")]  // inconsistent separator
    public void DurationRelativeToMalformedOffsetStringIsRangeError(string offsetExpr)
        => Assert.Equal("RangeError", ErrorName(
            "Temporal.Duration.from({hours:24}).round({largestUnit:'years',relativeTo:"
            + "{year:2021,month:10,day:28,offset:" + offsetExpr + ",timeZone:'UTC'}})"));

    // ───────────── Problem 30: `\P{X}` inside a character class expands to the BMP complement

    [Fact]
    public void RegExpNegatedPropertyInsideCharacterClass()
        => Assert.Equal("true,true,true,true", Eval(
            "(function(){return ["
            + "/[\\p{Hex}\\P{Hex}]/u.test('\\u{1D306}'),"  // every code point matches the union
            + "/[\\P{Hex}]/u.test('z'),"                     // 'z' is not Hex
            + "/[\\P{Hex}]/u.test('A') === false,"           // 'A' IS Hex
            + "/[\\p{ASCII}\\P{ASCII}]/u.test('A')"
            + "].join(',');})()").ToString());

    // ───────────── Problem 26: nested `eval("eval('super()')")` binds the constructor's `this`

    [Fact]
    public void NestedEvalSuperCallInitializesDerivedConstructorThis()
        => Assert.Equal("true,true", Eval(
            "(function(){"
            + "var captured;"
            + "new class extends class {} {"
            + "  constructor() {"
            + "    eval(\"eval('super()')\");"
            + "    captured = [this === eval('this'), this === (()=>this)()];"
            + "  }"
            + "}();"
            + "return captured.join(',');"
            + "})()").ToString());

    [Theory] // every nesting pattern (arrow / eval / nested eval) of an eval super-call must bind `this`
    [InlineData("(()=>eval('super()'))()")]
    [InlineData("(()=>(()=>super())())()")]
    [InlineData("eval('(()=>super())()')")]
    [InlineData("eval(\"eval('super()')\")")]
    public void NestedEvalSuperCallVariantsDoNotLeaveThisInTdz(string superCall)
        => Assert.Equal("ok", Eval(
            "(function(){"
            + "new class extends class {} {"
            + "  constructor() { " + superCall + "; if (this !== eval('this')) throw 'fail'; }"
            + "}();"
            + "return 'ok';"
            + "})()").ToString());

    // ───────────── Problem 23: `arguments` inside a direct eval reaches the outer function ─────────────

    [Fact] // baseline: an eval that reads `arguments[0]` must see the calling function's arg
    public void DirectEvalSeesEnclosingFunctionArguments()
        => Assert.Equal("hello", Eval(
            "(function(){ return eval('arguments[0]'); })('hello')").ToString());

    [Fact] // a non-strict function never references `arguments` itself, but the eval still must
    public void DirectEvalMaterialisesArgumentsEvenWhenOuterDoesNotName()
        => Assert.Equal("3", Eval(
            "(function(){ return eval('arguments.length'); })('a','b','c').toString()").ToString());

    [Fact] // arguments.callee inside an eval inside a sloppy function returns that function
    public void DirectEvalArgumentsCalleeIsEnclosingFunction()
        => Assert.Equal("true", Eval(
            "(function outer(){ return eval('arguments.callee') === outer; })().toString()").ToString());

    [Fact] // the eval-side `arguments` is the same object as the outer's, not a fresh empty one
    public void DirectEvalArgumentsIsSameObjectAsOuterArguments()
        => Assert.Equal("true", Eval(
            "(function(){ return eval('arguments') === arguments; })(1,2).toString()").ToString());

    [Fact] // a parameter-initializer direct eval also reaches the call-site `arguments`
    public void ParameterInitializerDirectEvalSeesArguments()
        => Assert.Equal("seed", Eval(
            "(function(x = eval('arguments[0]')){ return x; })('seed')").ToString());

    [Fact] // strict mode: the eval-side `arguments` is the outer's (unmapped) arguments object
    public void StrictDirectEvalSeesEnclosingArguments()
        => Assert.Equal("first", Eval(
            "(function(){ 'use strict'; return eval('arguments[0]'); })('first')").ToString());
}
