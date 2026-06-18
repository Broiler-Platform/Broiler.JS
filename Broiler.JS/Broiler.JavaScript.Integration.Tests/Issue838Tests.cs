using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/838
//
// Fixed here — the Date string-representation cluster:
//
//   Problems 96, 98, 99 (Date.prototype.toString / toUTCString / toDateString serialize
//   year -1 to "-0001") — the human-readable Date formatters rendered through the backing
//   System.DateTimeOffset (`value.ToString(...)`), which only spans years 1–9999. Dates
//   outside that window are stored with the MinValue sentinel in `value` and the real
//   ECMAScript time in `rawTimeMs`; the formatters' `value == InvalidDate` guard therefore
//   misreported every such valid date as "Invalid Date", and even in range they could not
//   print proleptic years. toString, toUTCString, toDateString and toTimeString now compute
//   their fields from the ECMAScript time value via JSDateMath (the same path toISOString
//   already used) and guard on `double.IsNaN(GetTimeMs())`, so the full Date range renders
//   with a spec-compliant year ("-0001", four-digit minimum with a leading sign when
//   negative).
//
//   Problem 95 (Date.parse(new Date(0).toString()) === 0) — Date.prototype.toString emits
//   the implementation-defined zone shape "… GMT+0000 (Coordinated Universal Time)", but the
//   parser's offset specifier needs a colon ("+00:00") and could not skip the trailing
//   parenthesised zone name, so the engine's own toString output did not round-trip through
//   Date.parse. The parser now normalises that shape (inserts the colon, drops the zone-name
//   parenthetical) before matching, so toString/toTimeString output round-trips while the ISO
//   forms, the UTC string, and the negative-zero extended-year rejection are unaffected.
//
//   Problem 67 (await/yield in arrow-function parameters) — under the cover grammar the arrow
//   parameter list is parsed as an ordinary parenthesised expression, so an AwaitExpression /
//   YieldExpression inside it (e.g. `(a = await 1) => {}` in an async function) was accepted by
//   the parser and only blew up at code generation with a NotImplementedException. The refined
//   ArrowParameters are now checked for a contained await/yield (not crossing nested function /
//   class boundaries), which is the spec early error, so it is a SyntaxError. Valid uses — await
//   in a nested async arrow's default, in the arrow body, or `await`/`yield` as identifiers
//   outside async/generator code — are unaffected.
//
//   Problem 68 (`for await` requires a for-of head) — the for-statement parser rejected a
//   for-in head with `await` but accepted a C-style head (`for await (;;)`,
//   `for await (init; test; update)`), which is a SyntaxError. The parser now rejects any
//   `for await` head whose form is not for-of; valid `for await (... of ...)` (including a
//   `using` binding) and an ordinary C-style `await using` declaration are unaffected.
//
//   Problem 49 (object rest destructuring does not read excluded keys) — `let { a, ...rest } =
//   obj` was lowered as "copy every own enumerable property, then delete the destructured
//   keys", so the excluded key's descriptor and value were observably read first (a Proxy
//   source's getOwnPropertyDescriptor/get traps fired for it, and an ordinary accessor's
//   getter ran a second time). It now performs CopyDataProperties with the destructured keys
//   excluded up front (§7.3.25): the rest copy skips them before any descriptor/value read.
//
//   Problem 84 (Intl.NumberFormat compact notation, en) — compact units were only defined for
//   the CJK locales, so an English compact format fell back to the full number ("1500000",
//   formatToParts length 5). The English short scale (10^3 K, 10^6 M, 10^9 B, 10^12 T) is now
//   wired in, so format(1500000) is "1.5M" and formatToParts(987654321) is [integer "988",
//   compact "M"] (length 2). de-DE compact (Problem 85, needs "Mio."-style data) and
//   PluralRules compact (Problem 83) remain out of scope.
//
//   Problem 91 (Intl.NumberFormat currency fraction digits in resolvedOptions) — the formatter
//   already used a currency's CLDR minor-unit count, but resolvedOptions reflected the decimal
//   default (0/3), so JPY reported maximumFractionDigits 3 while it formatted with 0. The
//   fraction-digit resolution is now shared between the formatter and resolvedOptions and seeded
//   with style-dependent defaults (currency → its minor-unit digit count), so e.g. JPY reports
//   0/0, USD 2/2, BHD 3/3, and explicit overrides resolve against those defaults.
//
//   Problem 69 (Intl.supportedValuesOf("collation")) — the collation enumeration returned an
//   empty list even though the Collator accepts the standard CLDR collations, so a collation
//   accepted by Collator (e.g. "big5han") was missing from supportedValuesOf. It now returns
//   the recognised collation set (the same KnownCollations the Collator resolves against),
//   sorted ascending and excluding the reserved "standard"/"search".
//
//   Problems 73, 79 (Unicode keyword canonicalization for calendar / collation) — keyword
//   type-value aliases were not applied to the "-u-" extension during tag canonicalization, so
//   a deprecated calendar id such as "islamicc" was kept verbatim by Intl.Locale.calendar,
//   getCanonicalLocales and DateTimeFormat ("islamicc" instead of "islamic-civil";
//   "ethiopic-amete-alem" instead of "ethioaa"). The alias substitution now runs over the
//   extension (and over the DateTimeFormat calendar option). For collation, an unsupported
//   "-u-co-"/option value (e.g. "invalid") was reflected verbatim instead of resolving to the
//   "default" collation; the resolved collation is now validated against the known CLDR
//   collation set (deprecated aliases like "phonebook" → "phonebk" are canonicalized first).
//
//   Problem 92 (Intl.NumberFormat currency-code canonicalization) — a well-formed currency
//   code was reflected verbatim, so `{ currency: "usd" }` resolved to "usd". Per spec the
//   code is canonicalized to upper case ("USD"); resolvedOptions and the formatter now upper-
//   case it. Problem 70 (invalid NumberFormat "style" option) — style was read without
//   validation, so `{ style: "invalid" }` was silently accepted; it is now read through
//   GetOption against the sanctioned set ("decimal"/"percent"/"currency"/"unit"), so an
//   invalid value is a RangeError (covering Number/BigInt.prototype.toLocaleString too).
//
//   Problem 88 (%Iterator.prototype%[@@iterator] toString conforms to NativeFunction syntax)
//   — the function was created with the name "Symbol.iterator", so Function.prototype.toString
//   produced "function Symbol.iterator() { [native code] }". A bare dotted name is not valid
//   NativeFunction syntax; a symbol-keyed built-in's name is the bracketed well-known symbol
//   description "[Symbol.iterator]" (as RegExp.prototype[@@replace] already used "[Symbol.
//   replace]"). The name is now "[Symbol.iterator]", so both .name and toString conform.
//
//   Problems 52, 53 (Intl.Locale.prototype.getTextInfo / getWeekInfo returned empty objects)
//   — both methods were stubs returning {}, so Object.keys(...) was empty. getTextInfo now
//   returns { direction } ("ltr"/"rtl", chosen from the locale's script, else its language);
//   getWeekInfo now returns { firstDay, weekend, minimalDays } in spec key order (§1.4.x of
//   the Intl Locale Info additions). CLDR's full per-region tables are not bundled, so the
//   values are reasonable defaults (ISO Monday=1..Sunday=7, Saturday+Sunday weekend, one
//   minimal day) with a Sunday-first region table; the property shapes match the spec.
//
//   String.prototype.replace / replaceAll $-substitution for a string searchValue — the
//   non-functional replacement of a string (non-RegExp) searchValue was inserted verbatim,
//   skipping GetSubstitution (§22.1.3.18.1), so "$$", "$&", "$`" and "$'" were emitted
//   literally (e.g. 'abc'.replace('b', '[$&]') returned "a[$&]c" instead of "a[b]c"). Both
//   methods now run the template through GetSubstitution; with no captures, $n and $<name>
//   correctly remain literal. (Related to the issue's replace failures; not one of the
//   numbered problems but the same String/replace compliance area.)
//
//   Problem 35 (Array.prototype[@@unscopables] should not include "with") — the change-
//   array-by-copy proposal added toReversed/toSorted/toSpliced to the @@unscopables list but
//   deliberately NOT "with" ("with" is a reserved word that can never name a binding shadowed
//   inside a `with` statement). The engine's list carried an extra "with" entry, so the
//   property set did not match the spec (§23.1.3.40). Removed it; the Array.prototype.with
//   method itself is unaffected.
//
//   Problem 97 (Date.prototype.toLocale{String,DateString,TimeString} throw the same
//   exceptions as Intl.DateTimeFormat) — these methods are specified to construct an
//   Intl.DateTimeFormat, so an invalid locales argument must surface the constructor's
//   error: null → TypeError (ToObject(null)), a malformed language tag → RangeError. The
//   Broiler .NET fast path (taken when no options object is supplied) skipped that step and
//   treated a null locale like undefined, so `new Date(0).toLocaleString(null)` returned a
//   string instead of throwing. The fast path now validates the locales argument through the
//   same CanonicalizeLocaleList the Intl constructor uses, while preserving the spec's step
//   order (a NaN date still returns "Invalid Date" before any locale validation).
//
// Out of scope: the remaining problems in the issue are unrelated engine areas
// (Temporal/Intl/CLDR ordering and data, RegExp/Unicode property classes, with/Proxy
// environment records, SpiderMonkey-shell harness globals, etc.).
public class Issue838Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    // ---- Problems 96/98/99: negative-year human-readable Date strings ----

    [Fact]
    public void ToStringSerializesNegativeYearWithSignedFourDigitYear()
        => Assert.Equal("Fri Jan 01 -0001 00:00:00 GMT+0000 (Coordinated Universal Time)",
            Eval("new Date(Date.UTC(-1, 0, 1)).toString()"));

    [Fact]
    public void ToUTCStringSerializesNegativeYear()
        => Assert.Equal("Fri, 01 Jan -0001 00:00:00 GMT",
            Eval("new Date(Date.UTC(-1, 0, 1)).toUTCString()"));

    [Fact]
    public void ToDateStringSerializesNegativeYear()
        => Assert.Equal("Fri Jan 01 -0001", Eval("new Date(Date.UTC(-1, 0, 1)).toDateString()"));

    [Fact]
    public void NegativeYearDateIsNotReportedInvalid()
        => Assert.Equal("false", Eval(
            "var s = new Date(Date.UTC(-1, 0, 1)).toString(); String(s === 'Invalid Date')"));

    // ---- in-range dates and the genuine NaN case are unchanged ----

    [Fact]
    public void EpochToStringStillRendersInUtcContainer()
        => Assert.Equal("Thu Jan 01 1970 00:00:00 GMT+0000 (Coordinated Universal Time)",
            Eval("new Date(0).toString()"));

    [Fact]
    public void EpochToUTCStringUnchanged()
        => Assert.Equal("Thu, 01 Jan 1970 00:00:00 GMT", Eval("new Date(0).toUTCString()"));

    [Fact]
    public void EpochToDateStringUnchanged()
        => Assert.Equal("Thu Jan 01 1970", Eval("new Date(0).toDateString()"));

    [Fact]
    public void InvalidDateStillSerializesToInvalidDate()
        => Assert.Equal("Invalid Date,Invalid Date,Invalid Date", Eval(
            "var d = new Date(NaN);" +
            "d.toString() + ',' + d.toUTCString() + ',' + d.toDateString()"));

    // ---- Problem 95: toString output round-trips through Date.parse ----

    [Fact]
    public void DateParseRoundTripsToStringAtEpoch()
        => Assert.Equal("0", Eval("String(Date.parse(new Date(0).toString()))"));

    [Fact]
    public void DateParseRoundTripsToStringAtArbitraryInstant()
        => Assert.Equal("true", Eval(
            "var d = new Date(1687000000000); String(Date.parse(d.toString()) === d.getTime())"));

    [Fact]
    public void DateParseRoundTripsToStringForPreEpochInstant()
        => Assert.Equal("true", Eval(
            "var d = new Date(-5000000000); String(Date.parse(d.toString()) === d.getTime())"));

    [Fact]
    public void DateParseHonoursExplicitNonZeroOffsetInToStringShape()
        => Assert.Equal("28800000", Eval(
            "String(Date.parse('Thu Jan 01 1970 00:00:00 GMT-0800 (Pacific Standard Time)'))"));

    // ---- guard: the normalisation does not regress the existing ISO / UTC / NaN paths ----

    [Fact]
    public void IsoStringStillParses()
        => Assert.Equal("1686825000000", Eval("String(Date.parse('2023-06-15T10:30:00Z'))"));

    [Fact]
    public void UtcStringStillParses()
        => Assert.Equal("0", Eval("String(Date.parse('Thu, 01 Jan 1970 00:00:00 GMT'))"));

    [Fact]
    public void NegativeZeroExtendedYearStillRejected()
        => Assert.Equal("true", Eval("String(isNaN(Date.parse('-000000-03-31T00:45Z')))"));

    // ---- Problem 97: Date.prototype.toLocale* throws the same exceptions as DateTimeFormat ----

    [Fact]
    public void ToLocaleStringNullLocaleThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try { new Date(0).toLocaleString(null); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ToLocaleDateStringNullLocaleThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try { new Date(0).toLocaleDateString(null); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ToLocaleTimeStringNullLocaleThrowsTypeError()
        => Assert.Equal("TypeError", Eval(
            "try { new Date(0).toLocaleTimeString(null); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ToLocaleStringNullLocaleMatchesDateTimeFormatConstructor()
        => Assert.Equal("true", Eval(
            "function err(fn){ try { fn(); return 'no-throw'; } catch (e) { return e.constructor.name; } }" +
            "String(err(function(){ new Date(0).toLocaleString(null); }) ===" +
            "       err(function(){ new Intl.DateTimeFormat(null); }))"));

    [Fact]
    public void ToLocaleStringMalformedTagThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "try { new Date(0).toLocaleString('i'); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void InvalidDateReturnsInvalidDateBeforeLocaleValidation()
        => Assert.Equal("Invalid Date", Eval("new Date(NaN).toLocaleString(null)"));

    [Fact]
    public void ToLocaleStringStillWorksWithNoArgsAndValidLocale()
        => Assert.Equal("string,string", Eval(
            "(typeof new Date(0).toLocaleString()) + ',' + (typeof new Date(0).toLocaleString('en-US'))"));

    [Fact]
    public void ToLocaleStringStillFormatsThroughIntlOptions()
        => Assert.Equal("1970", Eval(
            "new Date(0).toLocaleString('en-US', { year: 'numeric', timeZone: 'UTC' })"));

    // ---- Problem 35: Array.prototype[@@unscopables] matches the spec list (no "with") ----

    [Fact]
    public void ArrayUnscopablesMatchesSpecListWithoutWith()
        => Assert.Equal(
            "at,copyWithin,entries,fill,find,findIndex,findLast,findLastIndex," +
            "flat,flatMap,includes,keys,toReversed,toSorted,toSpliced,values",
            Eval("Object.keys(Array.prototype[Symbol.unscopables]).join(',')"));

    [Fact]
    public void ArrayUnscopablesDoesNotIncludeWith()
        => Assert.Equal("false", Eval("String('with' in Array.prototype[Symbol.unscopables])"));

    [Fact]
    public void ArrayUnscopablesHasNullPrototype()
        => Assert.Equal("true", Eval(
            "String(Object.getPrototypeOf(Array.prototype[Symbol.unscopables]) === null)"));

    [Fact]
    public void ArrayWithMethodItselfStillExistsAndWorks()
        => Assert.Equal("function,1,9,3", Eval(
            "(typeof Array.prototype.with) + ',' + [1, 2, 3].with(1, 9).join(',')"));

    // ---- String.prototype.replace / replaceAll $-substitution for a string searchValue ----

    [Fact]
    public void ReplaceStringSearchExpandsMatchedSubstitution()
        => Assert.Equal("a[b]c", Eval("'abc'.replace('b', '[$&]')"));

    [Fact]
    public void ReplaceStringSearchExpandsDollarDollar()
        => Assert.Equal("a$c", Eval("'abc'.replace('b', '$$')"));

    [Fact]
    public void ReplaceStringSearchExpandsPrecedingAndFollowing()
        => Assert.Equal("ab[ab]d,a[cd]cd", Eval(
            "'abcd'.replace('c', '[$`]') + ',' + \"abcd\".replace('b', \"[$']\")"));

    [Fact]
    public void ReplaceStringSearchLeavesCaptureAndNamedPatternsLiteral()
        => Assert.Equal("a$1c,a$<x>c,a$zc,ax$c", Eval(
            "'abc'.replace('b','$1') + ',' + 'abc'.replace('b','$<x>') + ',' +" +
            "'abc'.replace('b','$z') + ',' + 'abc'.replace('b','x$')"));

    [Fact]
    public void ReplaceStringSearchFunctionalReplacementStillWorks()
        => Assert.Equal("a[b]c", Eval("'abc'.replace('b', function (m) { return '[' + m + ']'; })"));

    [Fact]
    public void ReplaceAllStringSearchExpandsSubstitution()
        => Assert.Equal("a[-]b[-]c", Eval("'a-b-c'.replaceAll('-', '[$&]')"));

    [Fact]
    public void ReplaceAllStringSearchExpandsPrecedingPerMatch()
        => Assert.Equal("a[a]a[aba]", Eval("'abab'.replaceAll('b', '[$`]')"));

    [Fact]
    public void ReplaceAllEmptySearchExpandsSubstitutionAtEachPosition()
        => Assert.Equal("--a--b--", Eval("'ab'.replaceAll('', '-$&-')"));

    // ---- Problem 52: Intl.Locale.prototype.getTextInfo ----

    [Fact]
    public void GetTextInfoReturnsObjectWithDirectionKey()
        => Assert.Equal("direction", Eval("Object.keys(new Intl.Locale('en').getTextInfo()).join(',')"));

    [Fact]
    public void GetTextInfoDirectionIsLtrForLatinAndRtlForArabicHebrew()
        => Assert.Equal("ltr,rtl,rtl", Eval(
            "new Intl.Locale('en').getTextInfo().direction + ',' +" +
            "new Intl.Locale('ar').getTextInfo().direction + ',' +" +
            "new Intl.Locale('he').getTextInfo().direction"));

    [Fact]
    public void GetTextInfoExplicitScriptOverridesLanguageDirection()
        => Assert.Equal("rtl,ltr", Eval(
            "new Intl.Locale('az-Arab').getTextInfo().direction + ',' +" +
            "new Intl.Locale('ar-Latn').getTextInfo().direction"));

    [Fact]
    public void GetTextInfoDirectionIsAnOrdinaryDataProperty()
        => Assert.Equal(
            "{\"value\":\"ltr\",\"writable\":true,\"enumerable\":true,\"configurable\":true}",
            Eval("JSON.stringify(Object.getOwnPropertyDescriptor(new Intl.Locale('en').getTextInfo(), 'direction'))"));

    // ---- Problem 53: Intl.Locale.prototype.getWeekInfo ----

    [Fact]
    public void GetWeekInfoReturnsSpecKeysInOrder()
        => Assert.Equal("firstDay,weekend,minimalDays", Eval(
            "Object.keys(new Intl.Locale('en-US').getWeekInfo()).join(',')"));

    [Fact]
    public void GetWeekInfoFirstDayIsNumberAndWeekendIsArray()
        => Assert.Equal("number,true,number", Eval(
            "var w = new Intl.Locale('en-US').getWeekInfo();" +
            "(typeof w.firstDay) + ',' + Array.isArray(w.weekend) + ',' + (typeof w.minimalDays)"));

    [Fact]
    public void GetWeekInfoFirstDayIsSundayForUsAndMondayForGermany()
        => Assert.Equal("7,1", Eval(
            "new Intl.Locale('en-US').getWeekInfo().firstDay + ',' +" +
            "new Intl.Locale('de-DE').getWeekInfo().firstDay"));

    // ---- Problem 88: %Iterator.prototype%[@@iterator] uses the bracketed symbol name ----

    private const string IteratorPrototypeIterator =
        "Object.getPrototypeOf(Object.getPrototypeOf([].values()))[Symbol.iterator]";

    [Fact]
    public void IteratorPrototypeSymbolIteratorNameIsBracketed()
        => Assert.Equal("[Symbol.iterator]", Eval(IteratorPrototypeIterator + ".name"));

    [Fact]
    public void IteratorPrototypeSymbolIteratorToStringConformsToNativeFunctionSyntax()
        => Assert.Equal("function [Symbol.iterator]() { [native code] }",
            Eval(IteratorPrototypeIterator + ".toString()"));

    [Fact]
    public void IteratorPrototypeSymbolIteratorStillReturnsThis()
        => Assert.Equal("true", Eval("var it = [].values(); String(it[Symbol.iterator]() === it)"));

    [Fact]
    public void IterationStillWorksAfterRename()
        => Assert.Equal("1,2,3", Eval("[...[1, 2, 3]].join(',')"));

    // ---- Problem 92: Intl.NumberFormat canonicalizes the currency code to upper case ----

    [Fact]
    public void NumberFormatCanonicalizesLowercaseCurrencyToUpper()
        => Assert.Equal("USD,EUR,USD", Eval(
            "new Intl.NumberFormat('en', { style: 'currency', currency: 'usd' }).resolvedOptions().currency + ',' +" +
            "new Intl.NumberFormat('en', { style: 'currency', currency: 'Eur' }).resolvedOptions().currency + ',' +" +
            "new Intl.NumberFormat('en', { style: 'currency', currency: 'uSd' }).resolvedOptions().currency"));

    [Fact]
    public void NumberFormatCanonicalizesCurrencyEvenWhenStyleNotCurrency()
        => Assert.Equal("JPY", Eval(
            "new Intl.NumberFormat('en', { currency: 'jpy' }).resolvedOptions().currency"));

    [Fact]
    public void NumberFormatStillFormatsCurrencyGivenLowercaseCode()
        => Assert.Equal("$5.00", Eval(
            "new Intl.NumberFormat('en-US', { style: 'currency', currency: 'usd' }).format(5)"));

    // ---- Problem 70: an invalid NumberFormat "style" option is a RangeError ----

    [Fact]
    public void NumberFormatInvalidStyleThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "try { new Intl.NumberFormat('en', { style: 'invalid' }); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void NumberToLocaleStringInvalidStyleThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "try { (1).toLocaleString('en', { style: 'invalid' }); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void BigIntToLocaleStringInvalidStyleThrowsRangeError()
        => Assert.Equal("RangeError", Eval(
            "try { (1n).toLocaleString('en', { style: 'invalid' }); 'no-throw'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void NumberFormatValidStylesStillResolve()
        => Assert.Equal("decimal,percent,unit", Eval(
            "new Intl.NumberFormat('en').resolvedOptions().style + ',' +" +
            "new Intl.NumberFormat('en', { style: 'percent' }).resolvedOptions().style + ',' +" +
            "new Intl.NumberFormat('en', { style: 'unit', unit: 'meter' }).resolvedOptions().style"));

    [Fact]
    public void NumberFormatCurrencyStyleStillRequiresCurrencyAndRejectsMalformedCode()
        => Assert.Equal("TypeError,RangeError", Eval(
            "function err(f){ try { f(); return 'no-throw'; } catch (e) { return e.constructor.name; } }" +
            "err(function(){ new Intl.NumberFormat('en', { style: 'currency' }); }) + ',' +" +
            "err(function(){ new Intl.NumberFormat('en', { style: 'currency', currency: 'US' }); })"));

    // ---- Problem 73: deprecated calendar keyword values are canonicalized ----

    [Fact]
    public void LocaleCanonicalizesDeprecatedCalendarKeyword()
        => Assert.Equal("islamic-civil,ethioaa", Eval(
            "new Intl.Locale('en-u-ca-islamicc').calendar + ',' +" +
            "new Intl.Locale('en-u-ca-ethiopic-amete-alem').calendar"));

    [Fact]
    public void GetCanonicalLocalesCanonicalizesCalendarKeyword()
        => Assert.Equal("en-u-ca-islamic-civil,en-u-ca-ethioaa", Eval(
            "Intl.getCanonicalLocales('en-u-ca-islamicc')[0] + ',' +" +
            "Intl.getCanonicalLocales('en-u-ca-ethiopic-amete-alem')[0]"));

    [Fact]
    public void DateTimeFormatCanonicalizesCalendarOptionAndTag()
        => Assert.Equal("islamic-civil,islamic-civil", Eval(
            "new Intl.DateTimeFormat('en', { calendar: 'islamicc' }).resolvedOptions().calendar + ',' +" +
            "new Intl.DateTimeFormat('en-u-ca-islamicc').resolvedOptions().calendar"));

    [Fact]
    public void SupportedCalendarStillResolvesAndUnsupportedStillFallsBack()
        => Assert.Equal("chinese,gregory", Eval(
            "new Intl.DateTimeFormat('en-u-ca-chinese').resolvedOptions().calendar + ',' +" +
            "new Intl.DateTimeFormat('en-u-ca-hebrew').resolvedOptions().calendar"));

    // ---- Problem 79: an unsupported collation resolves to "default" ----

    [Fact]
    public void CollatorUnsupportedCollationResolvesToDefault()
        => Assert.Equal("default,default", Eval(
            "new Intl.Collator('en-u-co-invalid').resolvedOptions().collation + ',' +" +
            "new Intl.Collator('en', { collation: 'invalid' }).resolvedOptions().collation"));

    [Fact]
    public void CollatorKnownCollationIsReflected()
        => Assert.Equal("phonebk,zhuyin", Eval(
            "new Intl.Collator('de-u-co-phonebk').resolvedOptions().collation + ',' +" +
            "new Intl.Collator('zh', { collation: 'zhuyin' }).resolvedOptions().collation"));

    [Fact]
    public void CollatorDefaultCollationAndCompareUnaffected()
        => Assert.Equal("default,-1", Eval(
            "new Intl.Collator('en').resolvedOptions().collation + ',' +" +
            "new Intl.Collator('en').compare('a', 'b')"));

    [Fact]
    public void LocaleCanonicalizesCollationAliasInTag()
        => Assert.Equal("phonebk", Eval("new Intl.Locale('de-u-co-phonebk').collation"));

    // ---- Problem 69: Intl.supportedValuesOf("collation") ----

    [Fact]
    public void SupportedValuesOfCollationIncludesBig5han()
        => Assert.Equal("true", Eval("String(Intl.supportedValuesOf('collation').includes('big5han'))"));

    [Fact]
    public void SupportedValuesOfCollationIsSortedAndExcludesReserved()
        => Assert.Equal("true,false", Eval(
            "var a = Intl.supportedValuesOf('collation');" +
            "String(JSON.stringify(a) === JSON.stringify([...a].sort())) + ',' +" +
            "String(a.includes('standard') || a.includes('search'))"));

    [Fact]
    public void EverySupportedCollationIsAcceptedByCollator()
        => Assert.Equal("true", Eval(
            "var sv = Intl.supportedValuesOf('collation'); var ok = true;" +
            "sv.forEach(function (c) {" +
            "  if (new Intl.Collator('en', { collation: c }).resolvedOptions().collation !== c) ok = false; });" +
            "String(ok)"));

    [Fact]
    public void SupportedValuesOfCollationOmitsUnsupportedValue()
        => Assert.Equal("false", Eval("String(Intl.supportedValuesOf('collation').includes('invalid'))"));

    // ---- Problem 91: currency fraction digits reflected in resolvedOptions ----

    [Fact]
    public void CurrencyResolvedFractionDigitsUseMinorUnitCount()
        => Assert.Equal("0/0,2/2,3/3,4/4", Eval(
            "function f(c){ var r = new Intl.NumberFormat('en', { style: 'currency', currency: c })" +
            ".resolvedOptions(); return r.minimumFractionDigits + '/' + r.maximumFractionDigits; }" +
            "[f('JPY'), f('USD'), f('BHD'), f('CLF')].join(',')"));

    [Fact]
    public void CurrencyFractionDigitsHonourLowercaseCode()
        => Assert.Equal("0/0", Eval(
            "var r = new Intl.NumberFormat('en', { style: 'currency', currency: 'jpy' })" +
            ".resolvedOptions(); r.minimumFractionDigits + '/' + r.maximumFractionDigits"));

    [Fact]
    public void CurrencyExplicitMinimumRaisesAgainstMinorUnitDefault()
        => Assert.Equal("2/2", Eval(
            "var r = new Intl.NumberFormat('en', { style: 'currency', currency: 'JPY', minimumFractionDigits: 2 })" +
            ".resolvedOptions(); r.minimumFractionDigits + '/' + r.maximumFractionDigits"));

    [Fact]
    public void CurrencyExplicitMaximumLowersAgainstMinorUnitDefault()
        => Assert.Equal("0/0", Eval(
            "var r = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })" +
            ".resolvedOptions(); r.minimumFractionDigits + '/' + r.maximumFractionDigits"));

    [Fact]
    public void CurrencyResolvedFractionDigitsAgreeWithFormatting()
        => Assert.Equal("¥1,235,$3.00,$3", Eval(
            "new Intl.NumberFormat('en-US', { style: 'currency', currency: 'JPY' }).format(1234.5) + ',' +" +
            "new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(3) + ',' +" +
            "new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(3)"));

    [Fact]
    public void DecimalFractionDigitsUnchanged()
        => Assert.Equal("0/3,2/3", Eval(
            "var a = new Intl.NumberFormat('en').resolvedOptions();" +
            "var b = new Intl.NumberFormat('en', { minimumFractionDigits: 2 }).resolvedOptions();" +
            "a.minimumFractionDigits + '/' + a.maximumFractionDigits + ',' +" +
            "b.minimumFractionDigits + '/' + b.maximumFractionDigits"));

    // ---- Problem 84: English compact notation ----

    [Fact]
    public void EnglishCompactFormatsWithShortScaleSuffixes()
        => Assert.Equal("1.5M,12K,1K,1B,1.5T", Eval(
            "var nf = new Intl.NumberFormat('en', { notation: 'compact' });" +
            "[nf.format(1500000), nf.format(12345), nf.format(1000)," +
            " nf.format(1000000000), nf.format(1500000000000)].join(',')"));

    [Fact]
    public void EnglishCompactFormatToPartsHasIntegerAndCompactParts()
        => Assert.Equal("integer:988|compact:M", Eval(
            "new Intl.NumberFormat('en', { notation: 'compact' }).formatToParts(987654321)" +
            ".map(function (p) { return p.type + ':' + p.value; }).join('|')"));

    [Fact]
    public void EnglishCompactFormatToPartsLengthIsTwo()
        => Assert.Equal("2", Eval(
            "String(new Intl.NumberFormat('en-US', { notation: 'compact' }).formatToParts(987654321).length)"));

    [Fact]
    public void CompactBelowThousandAndZeroAndNegativeAreHandled()
        => Assert.Equal("999,0,-2.5M", Eval(
            "var nf = new Intl.NumberFormat('en', { notation: 'compact' });" +
            "nf.format(999) + ',' + nf.format(0) + ',' + nf.format(-2500000)"));

    [Fact]
    public void CompactResolvedOptionsAndStandardNotationUnaffected()
        => Assert.Equal("compact,short,1,500,000", Eval(
            "var r = new Intl.NumberFormat('en', { notation: 'compact' }).resolvedOptions();" +
            "r.notation + ',' + r.compactDisplay + ',' + new Intl.NumberFormat('en').format(1500000)"));

    // ---- Problem 49: object rest does not read excluded keys ----

    [Fact]
    public void ObjectRestDoesNotCallProxyGetOwnPropertyDescriptorForExcludedKeys()
        => Assert.Equal("b,c", Eval(
            "var log = [];" +
            "var p = new Proxy({ a: 1, b: 2, c: 3 }, {" +
            "  ownKeys: function (t) { return Reflect.ownKeys(t); }," +
            "  getOwnPropertyDescriptor: function (t, k) { log.push(k); return Reflect.getOwnPropertyDescriptor(t, k); } });" +
            "var { a, ...r } = p; log.join(',')"));

    [Fact]
    public void ObjectRestDoesNotReReadExcludedAccessor()
        => Assert.Equal("a,b | {\"b\":2}", Eval(
            "var calls = [];" +
            "var o = { get a() { calls.push('a'); return 1; }, get b() { calls.push('b'); return 2; } };" +
            "var { a, ...rest } = o;" +
            "calls.join(',') + ' | ' + JSON.stringify(rest)"));

    [Fact]
    public void ObjectRestExcludesStringIndexAndSymbolKeys()
        => Assert.Equal("{\"1\":\"b\",\"2\":\"c\"},false", Eval(
            "var s = Symbol('s');" +
            "var o = { 0: 'a', 1: 'b', 2: 'c' }; o[s] = 1;" +
            "var { 0: z, [s]: w, ...rest } = o;" +
            "JSON.stringify(rest) + ',' + (s in rest)"));

    [Fact]
    public void ObjectRestComputedKeyEvaluatedOnceAndExcluded()
        => Assert.Equal("1|{\"b\":2}", Eval(
            "var log = [];" +
            "var k = function () { log.push('k'); return 'a'; };" +
            "var { [k()]: x, ...rest } = { a: 1, b: 2 };" +
            "log.length + '|' + JSON.stringify(rest)"));

    [Fact]
    public void PlainObjectSpreadAndRestOnlyStillCopyEverything()
        => Assert.Equal("{\"a\":1,\"b\":2},{\"a\":1,\"b\":2}", Eval(
            "var spread = { ...{ a: 1, b: 2 } };" +
            "var { ...rest } = { a: 1, b: 2 };" +
            "JSON.stringify(spread) + ',' + JSON.stringify(rest)"));

    // ---- Problem 68: `for await` is only valid with a for-of head ----

    private static string SyntaxCheck(string source)
        => "try { eval(" + Quote(source) + "); 'no-throw'; } catch (e) { e.constructor.name; }";

    private static string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    [Fact]
    public void ForAwaitWithCStyleHeadIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(SyntaxCheck("async function* g(){ for await (;;) ; }")));

    [Fact]
    public void ForAwaitWithCStyleInitHeadIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(SyntaxCheck("async function f(){ for await (var i=0;i<1;i++) ; }")));

    [Fact]
    public void ForAwaitWithForInHeadIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(SyntaxCheck("async function f(){ for await (var x in {}) ; }")));

    [Fact]
    public void ForAwaitOfHeadIsStillValid()
        => Assert.Equal("ok", Eval(
            "try { eval('async function f(){ for await (var x of []) ; }'); 'ok'; } catch (e) { e.constructor.name; }"));

    [Fact]
    public void ForAwaitUsingOfHeadAndCStyleAwaitUsingStillValid()
        => Assert.Equal("ok,ok", Eval(
            "function chk(s){ try { eval(s); return 'ok'; } catch (e) { return e.constructor.name; } }" +
            "chk('async function f(){ for await (using x of []) ; }') + ',' +" +
            "chk('async function f(){ for (await using x = null; false;) ; }')"));

    [Fact]
    public void PlainForLoopsWithoutAwaitAreUnaffected()
        => Assert.Equal("3,ab,6", Eval(
            "var s=0; for (var i=0;i<3;i++) s+=i;" +
            "var k=''; for (var x in {a:1,b:2}) k+=x;" +
            "var t=0; for (var y of [1,2,3]) t+=y;" +
            "s + ',' + k + ',' + t"));

    // ---- Problem 67: await/yield in arrow parameters is a SyntaxError ----

    [Fact]
    public void AwaitInAsyncArrowParameterDefaultIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(SyntaxCheck("async function f(){ return (a = await 1) => {}; }")));

    [Fact]
    public void AwaitRegexInAsyncArrowParameterDefaultIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(SyntaxCheck("async function f(){ return (a = await /r/g) => {}; }")));

    [Fact]
    public void YieldInGeneratorArrowParameterDefaultIsSyntaxError()
        => Assert.Equal("SyntaxError", Eval(SyntaxCheck("function* g(){ return (a = yield 1) => {}; }")));

    [Fact]
    public void AwaitInNestedAsyncArrowDefaultIsAllowed()
        => Assert.Equal("ok", Eval(
            "function chk(s){ try { eval(s); return 'ok'; } catch (e) { return e.constructor.name; } }" +
            "chk('async function f(){ var g = (a = (async () => await 1)) => a; }')"));

    [Fact]
    public void AwaitInArrowBodyAndPlainArrowsAreAllowed()
        => Assert.Equal("ok,ok,ok", Eval(
            "function chk(s){ try { eval(s); return 'ok'; } catch (e) { return e.constructor.name; } }" +
            "chk('async function f(){ var g = async () => await 1; }') + ',' +" +
            "chk('var g = (a = 5) => a;') + ',' +" +
            "chk('async function f(){ return (await 1); }')"));
}
