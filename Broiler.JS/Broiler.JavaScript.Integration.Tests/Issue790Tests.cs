using Broiler.JavaScript.Engine;
using Xunit;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/790
//
// Fixed here:
//   * Problem 4 — a leap second (:60) parsed from a Temporal string collapses to :59 instead of
//     throwing (ParseISODateTime), so e.g. Temporal.PlainTime.from("2016-12-31T23:59:60") is 23:59:59.
//   * Problem 6 — a designator-less time string that is also a valid year-month ("2021-12") or
//     month-day ("1130") is ambiguous and is rejected as a PlainTime unless prefixed with "T". A space
//     is not accepted as a substitute for the time designator.
//   * Problem 14 — a UTC ("Z") designator is not valid for a (zone-less) PlainTime ("09:00:00Z").
//   * Problem 9 — Temporal {PlainDate,PlainYearMonth,PlainTime,Instant}.prototype.{since,until} read
//     every option (largestUnit, roundingIncrement, roundingMode, smallestUnit, in that order) and
//     coerce each before validating any against the type's allowed unit group, so a disallowed unit is
//     reported only after all options have been read (GetDifferenceSettings option-reading order).
//   * Problem 13 — Temporal.PlainDate / Temporal.PlainYearMonth prototype since/until now round the
//     calendar date difference to smallestUnit at the given roundingIncrement / roundingMode (RoundRelative
//     Duration over the ISO day axis: NudgeToCalendarUnit + BubbleRelativeDuration, day-based since a plain
//     date has no time or zone), e.g. later.since(earlier, {smallestUnit:"year"}) truncates 1y1m1d to 1y.
//     Implemented for the ISO calendar; the arithmetic calendars still return the unrounded difference.
//   * Problem 12 — Temporal.ZonedDateTime.prototype.equals canonicalizes IANA time-zone identifiers
//     before comparing (TimeZoneEquals): backward aliases such as "Asia/Calcutta" and "Asia/Kolkata"
//     resolve to the same primary identifier (via the IANA→Windows→IANA mapping) and compare equal, while
//     a distinct same-offset zone ("Asia/Colombo") does not. The stored identifier is preserved.
//   * Problem 3 — %Iterator.prototype%.{map,filter,flatMap} now close the underlying iterator when the
//     mapper/predicate (or, for flatMap, GetIteratorFlattenable) throws — IfAbruptCloseIterator — calling
//     its return() exactly once and swallowing a throwing return so the original error propagates.
//   * Problem 5 — Promise.{all,allSettled,race,any} no longer close the iterator when stepping it throws
//     (reading next()/done/value): IteratorStepValue sets the record's [[done]], so a step error must NOT
//     trigger IteratorClose, whereas a per-element body error still does. A `stepping` flag set only
//     around the step distinguishes the two in the combinator's catch.
//   * Problems 7/8 — the %TypedArray%.prototype iteration methods (every, some, forEach, map, filter,
//     find, findIndex, reduce) capture the length once at the start, then read each index with an
//     ordinary [[Get]] (ReadElement → undefined past the live length). Growing the backing resizable
//     buffer mid-iteration no longer visits the new elements; shrinking reads the now-out-of-bounds
//     indices as undefined instead of truncating the iteration.
//   * Problem 11 — (a) Iterator.prototype.take/drop throw a TypeError for a non-object receiver before
//     coercing the limit. (b) The %TypedArray% array iterator (values/keys/entries, hence for-of) re-checks
//     the view each step and throws a TypeError once a resize leaves it out of bounds (a length-tracking
//     view instead just yields fewer elements).
//   * Problem 2 — several resizable-buffer behaviours:
//     - [[PreventExtensions]] on a non-fixed-length typed array (length-tracking, or any view onto a
//       non-shared resizable buffer) returns false, so Object.{preventExtensions,seal,freeze} throw a
//       TypeError even when the view is empty.
//     - %TypedArray%.prototype.slice re-validates after coercing start/end (capturing the source length
//       once): a fixed-length view shrunk out of bounds throws a TypeError, a length-tracking view copies
//       the still-in-bounds elements (the rest of the result zero-filled).
//     - %TypedArray%.prototype.set throws a TypeError when the *source* typed array was left out of bounds.
//     - the generic Array.prototype.{values,keys,entries} applied to a typed array now produce the
//       TypedArray array-iterator, so iterating one over an out-of-bounds view throws (CreateArrayIterator).
//     (Remaining Problem-2 files — slice speciesctor-resize edge, typed-array ctor from a resizable source,
//     Atomics cannot-suspend, the sm-staging cases — are left as documented out of scope.)
public class Issue790Tests
{
    private static string ErrorName(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval($"let __t='NONE'; try {{ {code} }} catch (e) {{ __t = e.constructor.name; }} __t").ToString();
    }

    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ── Problem 4: a string leap second collapses to :59 ─────────────────────────

    [Theory]
    [InlineData("Temporal.PlainTime.from('2016-12-31T23:59:60').toString()", "23:59:59")]
    [InlineData("Temporal.PlainTime.from('23:59:60').toString()", "23:59:59")]
    [InlineData("Temporal.PlainTime.from('2016-12-31T23:59:60', {overflow:'reject'}).toString()", "23:59:59")]
    [InlineData("Temporal.PlainTime.compare('23:59:60', '23:59:59')", "0")]
    public void PlainTime_LeapSecondString_Collapses(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ── Problem 6: ambiguous designator-less time strings require a "T" prefix ────

    [Theory]
    [InlineData("Temporal.PlainTime.from('2021-12')")]
    [InlineData("Temporal.PlainTime.from('1214')")]
    [InlineData("Temporal.PlainTime.from('0229')")]
    [InlineData("Temporal.PlainTime.from('1130')")]
    [InlineData("Temporal.PlainTime.from('12-14')")]
    [InlineData("Temporal.PlainTime.from('202112')")]
    [InlineData("Temporal.PlainTime.from('2021-12[-12:00]')")]
    [InlineData("Temporal.PlainTime.from('202112[UTC]')")]
    // a space is not accepted as a substitute for the time designator
    [InlineData("Temporal.PlainTime.from(' 2021-12')")]
    public void PlainTime_AmbiguousString_Throws(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    [Theory]
    // with the "T" designator the same strings parse as a time
    [InlineData("Temporal.PlainTime.from('T2021-12').toString()", "20:21:00")]
    [InlineData("Temporal.PlainTime.from('T1130').toString()", "11:30:00")]
    // unambiguous strings (not a valid year-month/month-day) parse without a designator
    [InlineData("Temporal.PlainTime.from('2021-13').toString()", "20:21:00")]
    [InlineData("Temporal.PlainTime.from('1314').toString()", "13:14:00")]
    [InlineData("Temporal.PlainTime.from('1232').toString()", "12:32:00")]
    [InlineData("Temporal.PlainTime.from('0230').toString()", "02:30:00")]
    [InlineData("Temporal.PlainTime.from('0000').toString()", "00:00:00")]
    public void PlainTime_UnambiguousString_Parses(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // ── Problem 14: a UTC ("Z") designator is invalid for a PlainTime ────────────

    [Theory]
    [InlineData("Temporal.PlainTime.from('2019-10-01T09:00:00Z')")]
    [InlineData("Temporal.PlainTime.from('2019-10-01T09:00:00Z[UTC]')")]
    [InlineData("Temporal.PlainTime.from('09:00:00Z[UTC]')")]
    [InlineData("Temporal.PlainTime.from('09:00:00Z')")]
    public void PlainTime_UtcDesignator_Throws(string code)
        => Assert.Equal("RangeError", ErrorName(code));

    // a plain numeric offset is merely ignored (not a UTC designator)
    [Fact]
    public void PlainTime_NumericOffset_Ignored()
        => Assert.Equal("09:00:00", Eval("Temporal.PlainTime.from('09:00:00+01:00').toString()"));

    // ── Problem 9: since/until read all options before validating ────────────────

    private const string Observer =
        "const reads=[]; const options={" +
        "get largestUnit(){reads.push('largestUnit');return 'hour';}," +
        "get roundingIncrement(){reads.push('roundingIncrement');return 1;}," +
        "get roundingMode(){reads.push('roundingMode');return 'trunc';}," +
        "get smallestUnit(){reads.push('smallestUnit');return SMALLEST;}};" +
        "let threw='NONE'; try{ EXPR }catch(e){threw=e.constructor.name;} reads.join(',')+'|'+threw";

    private static string Order(string expr, string smallest)
        => Eval(Observer.Replace("EXPR", expr).Replace("SMALLEST", smallest));

    [Fact]
    public void PlainDate_Since_ReadsAllOptionsBeforeValidating()
        => Assert.Equal("largestUnit,roundingIncrement,roundingMode,smallestUnit|RangeError",
            Order("new Temporal.PlainDate(2000,1,1).since(new Temporal.PlainDate(2001,1,1), options)", "'day'"));

    [Fact]
    public void PlainDate_Until_ReadsAllOptionsBeforeValidating()
        => Assert.Equal("largestUnit,roundingIncrement,roundingMode,smallestUnit|RangeError",
            Order("new Temporal.PlainDate(2000,1,1).until(new Temporal.PlainDate(2001,1,1), options)", "'day'"));

    [Fact]
    public void PlainYearMonth_Since_ReadsAllOptionsBeforeValidating()
        => Assert.Equal("largestUnit,roundingIncrement,roundingMode,smallestUnit|RangeError",
            Order("new Temporal.PlainYearMonth(2000,1).since(new Temporal.PlainYearMonth(2001,1), options)", "'month'"));

    [Fact]
    public void PlainTime_Since_ReadsAllOptionsBeforeValidating()
        // largestUnit 'hour' is valid for PlainTime, so make smallestUnit the disallowed (date) unit
        => Assert.Equal("largestUnit,roundingIncrement,roundingMode,smallestUnit|RangeError",
            Order("new Temporal.PlainTime(1,0,0).since(new Temporal.PlainTime(2,0,0), options)", "'day'"));

    [Fact]
    public void Instant_Since_ReadsAllOptionsBeforeValidating()
        => Assert.Equal("largestUnit,roundingIncrement,roundingMode,smallestUnit|RangeError",
            Order("new Temporal.Instant(0n).since(new Temporal.Instant(1000n), {" +
                "get largestUnit(){reads.push('largestUnit');return 'week';}," +
                "get roundingIncrement(){reads.push('roundingIncrement');return 1;}," +
                "get roundingMode(){reads.push('roundingMode');return 'trunc';}," +
                "get smallestUnit(){reads.push('smallestUnit');return 'nanosecond';}})", "'day'"));

    // ── Problem 13: since/until round the date difference to smallestUnit ────────

    [Theory]
    // test262: later.since(earlier, {smallestUnit:'year'}) with earlier=2000-05-02, later=2001-06-03
    // → trunc of 1y1m1d = 1y.
    [InlineData("new Temporal.PlainDate(2001,6,3).since(new Temporal.PlainDate(2000,5,2), {smallestUnit:'year'})", "P1Y")]
    [InlineData("new Temporal.PlainDate(2001,6,3).until(new Temporal.PlainDate(2000,5,2), {smallestUnit:'year'})", "-P1Y")]
    [InlineData("new Temporal.PlainDate(2000,5,2).until(new Temporal.PlainDate(2001,6,3), {smallestUnit:'year'})", "P1Y")]
    // month rounding
    [InlineData("new Temporal.PlainDate(2000,1,1).until(new Temporal.PlainDate(2000,3,15), {smallestUnit:'month'})", "P2M")]
    [InlineData("new Temporal.PlainDate(2000,1,1).until(new Temporal.PlainDate(2000,2,20), {smallestUnit:'month', roundingMode:'ceil'})", "P2M")]
    [InlineData("new Temporal.PlainDate(2000,1,1).until(new Temporal.PlainDate(2000,2,20), {smallestUnit:'month', roundingMode:'halfExpand'})", "P2M")]
    // week rounding (largestUnit auto-resolves to week)
    [InlineData("new Temporal.PlainDate(2000,1,1).until(new Temporal.PlainDate(2000,1,20), {smallestUnit:'week'})", "P2W")]
    [InlineData("new Temporal.PlainDate(2000,1,1).until(new Temporal.PlainDate(2000,1,20), {smallestUnit:'week', roundingMode:'halfExpand'})", "P3W")]
    // year rounding with increment
    [InlineData("new Temporal.PlainDate(2000,1,1).until(new Temporal.PlainDate(2003,1,1), {smallestUnit:'year', roundingIncrement:2})", "P2Y")]
    // default (no options) is unchanged: exact day difference
    [InlineData("new Temporal.PlainDate(2000,1,1).until(new Temporal.PlainDate(2001,3,5))", "P429D")]
    [InlineData("new Temporal.PlainDate(2000,1,1).until(new Temporal.PlainDate(2001,3,5), {largestUnit:'year'})", "P1Y2M4D")]
    public void PlainDate_DifferenceRounding(string code, string expected)
        => Assert.Equal(expected, Eval(code + ".toString()"));

    [Theory]
    // PlainYearMonth difference rounds to year (1y1m trunc → 1y)
    [InlineData("new Temporal.PlainYearMonth(2000,5).until(new Temporal.PlainYearMonth(2001,6), {smallestUnit:'year'})", "P1Y")]
    [InlineData("new Temporal.PlainYearMonth(2001,6).since(new Temporal.PlainYearMonth(2000,5), {smallestUnit:'year'})", "P1Y")]
    [InlineData("new Temporal.PlainYearMonth(2000,5).until(new Temporal.PlainYearMonth(2001,6), {smallestUnit:'year', roundingMode:'halfExpand'})", "P1Y")]
    [InlineData("new Temporal.PlainYearMonth(2000,5).until(new Temporal.PlainYearMonth(2001,11), {smallestUnit:'year', roundingMode:'halfExpand'})", "P2Y")]
    // default: exact year+month
    [InlineData("new Temporal.PlainYearMonth(2000,5).until(new Temporal.PlainYearMonth(2001,6))", "P1Y1M")]
    public void PlainYearMonth_DifferenceRounding(string code, string expected)
        => Assert.Equal(expected, Eval(code + ".toString()"));

    // ── Problem 12: ZonedDateTime.equals canonicalizes IANA identifiers ──────────

    [Theory]
    // Asia/Calcutta and Asia/Kolkata are IANA aliases of the same zone → equal (object and string args).
    [InlineData("calcutta.equals(kolkata)", "true")]
    [InlineData("calcutta.equals(kolkata.toString())", "true")]
    [InlineData("kolkata.equals(calcutta)", "true")]
    [InlineData("kolkata.equals(calcutta.toString())", "true")]
    // Asia/Colombo is a distinct zone (same offset) → not equal.
    [InlineData("calcutta.equals(colombo)", "false")]
    public void ZonedDateTime_Equals_CanonicalizesIanaIdentifiers(string expr, string expected)
    {
        var setup =
            "const calcutta = Temporal.ZonedDateTime.from('2020-01-01T00:00:00+05:30[Asia/Calcutta]');" +
            "const kolkata = Temporal.ZonedDateTime.from('2020-01-01T00:00:00+05:30[Asia/Kolkata]');" +
            "const colombo = Temporal.ZonedDateTime.from('2020-01-01T00:00:00+05:30[Asia/Colombo]');";
        Assert.Equal(expected, Eval(setup + expr));
    }

    // The identifier itself is preserved (only equality canonicalizes).
    [Fact]
    public void ZonedDateTime_TimeZoneId_PreservesAlias()
        => Assert.Equal("Asia/Calcutta",
            Eval("Temporal.ZonedDateTime.from('2020-01-01T00:00:00+05:30[Asia/Calcutta]').timeZoneId"));

    // ── Problem 3: Iterator helpers close the source when the callback throws ────

    // Returns "threw,callbackCalls,returnCalls" after one next() on a helper whose callback throws.
    private static string HelperThrow(string helper)
        => Eval(
            "let r=0,c=0;" +
            "const u={next(){return {done:false,value:1};},return(){r++;return {done:true};}," +
            "[Symbol.iterator](){return this;}};" +
            "let t=0; try{ Iterator.from(u)." + helper + "(x=>{c++; throw new Error('cb');}).next(); }catch(e){t=1;}" +
            "`${t},${c},${r}`");

    [Theory]
    [InlineData("map")]
    [InlineData("filter")]
    [InlineData("flatMap")]
    public void IteratorHelper_CallbackThrows_ClosesSourceOnce(string helper)
        => Assert.Equal("1,1,1", HelperThrow(helper));

    // When both the mapper and the source's return() throw, the original (mapper) error propagates and
    // the close error is swallowed (IteratorClose with a throw completion).
    [Fact]
    public void IteratorHelper_CallbackThrows_CloseErrorSwallowed()
        => Assert.Equal("cb", Eval(
            "const u={next(){return {done:false,value:1};},return(){throw new Error('ret');}," +
            "[Symbol.iterator](){return this;}};" +
            "let m='none'; try{ Iterator.from(u).map(x=>{throw new Error('cb');}).next(); }catch(e){m=e.message;} m"));

    // ── Problem 5: Promise combinators do NOT close the iterator on a step error ─

    // returnCount after a combinator iterates an iterator whose step (done/value getter) throws.
    private static string ComboStepThrow(string combinator, string poison)
        => Eval(
            "let r=0;" +
            "const iterable={[Symbol.iterator](){return {next(){return " + poison + ";}, return(){r++; return {done:true};}};}};" +
            "Promise." + combinator + "(iterable).then(()=>{},()=>{}); String(r)");

    [Theory]
    [InlineData("all")]
    [InlineData("race")]
    [InlineData("allSettled")]
    [InlineData("any")]
    public void Promise_StepDoneThrows_DoesNotClose(string combinator)
        => Assert.Equal("0", ComboStepThrow(combinator, "{get done(){throw new Error('step');}}"));

    [Theory]
    [InlineData("all")]
    [InlineData("race")]
    [InlineData("allSettled")]
    [InlineData("any")]
    public void Promise_StepValueThrows_DoesNotClose(string combinator)
        => Assert.Equal("0", ComboStepThrow(combinator, "{done:false, get value(){throw new Error('val');}}"));

    // ── Problems 7/8: TypedArray iteration methods capture length once ───────────

    // Collects each callback value while the callback resizes the backing resizable buffer at iteration
    // `at`. A grow must not extend the iteration; a shrink reads the now-out-of-bounds indices as
    // undefined. Uses iterate-all methods (whose callback return value never short-circuits); reduce
    // observes the element as the 2nd callback argument, so it is exercised separately below.
    private static string IterResize(string method, int newBytes, int at)
        => Eval(
            "const rab=new ArrayBuffer(4,{maxByteLength:8});" +
            "const ta=new Int8Array(rab); ta[0]=0; ta[1]=2; ta[2]=4; ta[3]=6;" +
            "let v=[];" +
            "ta." + method + "((x,i)=>{ v.push(x); if(i===" + at + ") rab.resize(" + newBytes + "); return true; });" +
            "v.map(x=>x===undefined?'u':x).join(',')");

    [Theory]
    [InlineData("map")]
    [InlineData("forEach")]
    [InlineData("filter")]
    public void TypedArray_GrowMidIteration_DoesNotExtend(string method)
        // grow to 8 bytes after the 2nd element: still exactly 4 callbacks for [0,2,4,6].
        => Assert.Equal("0,2,4,6", IterResize(method, 8, 1));

    [Theory]
    [InlineData("map")]
    [InlineData("forEach")]
    [InlineData("filter")]
    public void TypedArray_ShrinkMidIteration_ReadsUndefined(string method)
        // shrink to 2 bytes after the 1st element: indices 2,3 are out of bounds → undefined.
        => Assert.Equal("0,2,u,u", IterResize(method, 2, 0));

    // reduce observes the element as its 2nd callback argument and visits all indices.
    [Fact]
    public void TypedArray_Reduce_GrowMidIteration_DoesNotExtend()
        => Assert.Equal("0,2,4,6", Eval(
            "const rab=new ArrayBuffer(4,{maxByteLength:8});" +
            "const ta=new Int8Array(rab); ta[0]=0; ta[1]=2; ta[2]=4; ta[3]=6;" +
            "let v=[]; ta.reduce((acc,x,i)=>{ v.push(x); if(i===1) rab.resize(8); return acc; }, 0);" +
            "v.map(x=>x===undefined?'u':x).join(',')"));

    [Fact]
    public void TypedArray_Reduce_ShrinkMidIteration_ReadsUndefined()
        => Assert.Equal("0,2,u,u", Eval(
            "const rab=new ArrayBuffer(4,{maxByteLength:8});" +
            "const ta=new Int8Array(rab); ta[0]=0; ta[1]=2; ta[2]=4; ta[3]=6;" +
            "let v=[]; ta.reduce((acc,x,i)=>{ v.push(x); if(i===0) rab.resize(2); return acc; }, 0);" +
            "v.map(x=>x===undefined?'u':x).join(',')"));

    // ── Problem 11: TypeErrors that were previously not thrown ───────────────────

    // Iterator.prototype.take/drop require an Object receiver — checked before the limit is coerced.
    [Theory]
    [InlineData("Iterator.prototype.take.call(null, 1)")]
    [InlineData("Iterator.prototype.take.call(0, 1)")]
    [InlineData("Iterator.prototype.drop.call(null, 1)")]
    [InlineData("Iterator.prototype.drop.call(0, 1)")]
    // a non-object receiver is a TypeError even when the limit itself is invalid (NaN)
    [InlineData("Iterator.prototype.take.call(null, NaN)")]
    public void IteratorTakeDrop_NonObjectReceiver_Throws(string code)
        => Assert.Equal("TypeError", ErrorName(code));

    // A fixed-length TypedArray view goes out of bounds when its resizable buffer shrinks; its array
    // iterator's next() then throws a TypeError mid-iteration (CreateArrayIterator).
    [Theory]
    [InlineData("values")]
    [InlineData("keys")]
    [InlineData("entries")]
    public void TypedArray_FixedLengthIterator_ShrinkMidIteration_Throws(string method)
        => Assert.Equal("TypeError", ErrorName(
            "const rab=new ArrayBuffer(4,{maxByteLength:8});" +
            "const ta=new Int8Array(rab,0,4); ta[0]=0;ta[1]=2;ta[2]=4;ta[3]=6;" +
            "let n=0; for(const x of ta." + method + "()){ if(++n===1) rab.resize(2); }"));

    // for-of over a fixed-length view that shrinks likewise throws.
    [Fact]
    public void TypedArray_ForOf_FixedLength_ShrinkMidIteration_Throws()
        => Assert.Equal("TypeError", ErrorName(
            "const rab=new ArrayBuffer(4,{maxByteLength:8});" +
            "const ta=new Int8Array(rab,0,4); ta[0]=0;ta[1]=2;ta[2]=4;ta[3]=6;" +
            "let n=0; for(const x of ta){ if(++n===1) rab.resize(2); }"));

    // A length-tracking view simply yields fewer elements after a shrink — no throw.
    [Fact]
    public void TypedArray_LengthTrackingIterator_ShrinkMidIteration_YieldsSubset()
        => Assert.Equal("0,2|none", Eval(
            "const rab=new ArrayBuffer(4,{maxByteLength:8});" +
            "const ta=new Int8Array(rab); ta[0]=0;ta[1]=2;ta[2]=4;ta[3]=6;" +
            "let out=[]; let err='none';" +
            "try{ for(const x of ta){ out.push(x); if(out.length===1) rab.resize(2); } }catch(e){err=e.constructor.name;}" +
            "`${out.join(',')}|${err}`"));

    // ── Problem 2: a non-fixed-length typed array cannot be made non-extensible ──

    [Theory]
    // length-tracking view onto a resizable buffer — even when currently empty
    [InlineData("Object.preventExtensions(new Int8Array(new ArrayBuffer(0,{maxByteLength:8})))")]
    [InlineData("Object.seal(new Int8Array(new ArrayBuffer(4,{maxByteLength:8})))")]
    [InlineData("Object.freeze(new Int8Array(new ArrayBuffer(4,{maxByteLength:8})))")]
    // fixed-length *view* onto a resizable buffer is also not fixed-length
    [InlineData("Object.freeze(new Int8Array(new ArrayBuffer(4,{maxByteLength:8}), 0, 4))")]
    public void TypedArray_ResizableBacked_PreventExtensions_Throws(string code)
        => Assert.Equal("TypeError", ErrorName(code));

    [Theory]
    // a view onto a fixed-length buffer is fixed-length: an empty one can be frozen / sealed
    [InlineData("(Object.isFrozen(Object.freeze(new Int8Array(0))))", "true")]
    [InlineData("(Object.isSealed(Object.seal(new Int8Array(0))))", "true")]
    [InlineData("(function(){Object.preventExtensions(new Int8Array(0)); return 'ok';})()", "ok")]
    public void TypedArray_FixedLengthBuffer_Extensibility_Succeeds(string code, string expected)
        => Assert.Equal(expected, Eval(code));

    // slice re-validates after coercing start/end: a fixed-length view shrunk out of bounds throws,
    // a length-tracking view returns the still-in-bounds elements (the rest zero-filled).
    [Fact]
    public void TypedArray_Slice_FixedLength_CoercedShrink_Throws()
        => Assert.Equal("TypeError", ErrorName(
            "const rab=new ArrayBuffer(4,{maxByteLength:8});" +
            "const ta=new Int8Array(rab,0,4); ta[0]=1;ta[1]=2;ta[2]=3;ta[3]=4;" +
            "const evil={valueOf(){ rab.resize(2); return 0; }};" +
            "ta.slice(evil)"));

    [Fact]
    public void TypedArray_Slice_LengthTracking_CoercedShrink_ZeroFills()
        => Assert.Equal("1,2,0,0", Eval(
            "const rab=new ArrayBuffer(4,{maxByteLength:8});" +
            "const ta=new Int8Array(rab); ta[0]=1;ta[1]=2;ta[2]=3;ta[3]=4;" +
            "const evil={valueOf(){ rab.resize(2); return 0; }};" +
            "Array.from(ta.slice(evil)).join(',')"));

    // set with a source typed array left out of bounds by a resize is a TypeError.
    [Fact]
    public void TypedArray_Set_OutOfBoundsSource_Throws()
        => Assert.Equal("TypeError", ErrorName(
            "const rab=new ArrayBuffer(8,{maxByteLength:16});" +
            "const src=new Int8Array(rab,4,4); const dst=new Int8Array(8);" +
            "rab.resize(2);" + // src (offset 4) is now out of bounds
            "dst.set(src)"));

    // the generic Array.prototype.{values,keys,entries} over a typed array follow TypedArray iterator
    // semantics: a fixed-length view that a resize pushes out of bounds throws when iterated.
    [Theory]
    [InlineData("values")]
    [InlineData("keys")]
    [InlineData("entries")]
    public void ArrayPrototypeIterator_OverOutOfBoundsTypedArray_Throws(string method)
        => Assert.Equal("TypeError", ErrorName(
            "const rab=new ArrayBuffer(4,{maxByteLength:8});" +
            "const ta=new Int8Array(rab,0,4); rab.resize(2);" + // fixed-length view now out of bounds
            "Array.from(Array.prototype." + method + ".call(ta))"));
}
