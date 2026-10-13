# Adapter guide: `Broiler.ECMAScript.Algorithms` — number conversion

An engine's adapter is written against this guide, not against the library's source: if an adapter
needs something this guide does not say, the guide is incomplete and is corrected first. The
obligation is the programme's (`Broiler.VM/docs/javascript-shared-libraries/governance.md` section
10 and `architecture.md` section 5); the conventions every operation follows are in
[`conventions.md`](../../docs/conventions.md); the area's design is
`Broiler.VM/docs/javascript-shared-libraries/areas/numbers.md`.

- **Library and version:** `Broiler.ECMAScript.Algorithms` `0.1.0-preview.1` and later, API baseline
  [`PublicAPI.txt`](../PublicAPI.txt), namespace `Broiler.ECMAScript.Numbers`.
- **Specification pins:** ECMA-262 at commit `0248456c` (`conformance/vectors/sources/ecma262.pin`);
  Unicode 17.0.0 (`conformance/vectors/sources/unicode.pin`).
- **Data versions:** `NumberParsing.UnicodeVersion` = `"17.0.0"`, the version of the white-space set's
  `Space_Separator` members (convention 13).
- **Consumers:** Broiler.JS: `JSNumberPrototype`, `JSGlobal` and `JSNumberStatic`, `NumberParser`'s
  public forwarders, the parser's literal sites and `Storage/KeyStrings` (milestone SJL-3.5).
  Broiler.VM: `JsRealm.Number`, `JsRealm.Global`, `JsEngine.Conversions`, `JsRealm.Json`,
  `JsRealm.String`, `JsObject.IsArrayIndex`, `JsRealm.Object`'s canonical numeric test, and the
  compiler's literal sites in `SliceTokenizer` and `JsParser` (milestone SJL-3.4).
- **Vectors:** `conformance/vectors/numbers/`, corpus revision 3 or later. The library-level runner
  is `Broiler.ECMAScript/runners/AlgorithmsLibrary.cs`, which is also the reference adapter: every
  operation below is called there exactly as an engine calls it.

**What no operation here does**, by convention 2: no `ToNumber`, `ToString`, `ToIntegerOrInfinity`
or `ToInt32` of an argument, no property read, no error object, and no string the engine's heap
holds. The adapter does those, in the order the specification gives, and only then calls the
library.

---

## `NumberText.ToString` and `TryFormat` — ECMA-262 §6.1.6.1.20 `Number::toString(x, 10)`

### Signature

```csharp
public static string ToString(double value);
public static bool TryFormat(double value, Span<char> destination, out int written);
public const int MaximumLength = 26;
```

### What the caller must have done first

Nothing: the operation takes any Number. A caller with a `ToString(argument)` to do has already
established that the argument is a Number.

### Preconditions

None. `TryFormat` answers `false`, writing nothing, when the buffer is shorter than the result;
`MaximumLength` characters always suffice.

### Work and limits

- **Meter:** none. The result is at most 26 characters whatever the value.
- **Limits:** none needed.

### Result

The shortest digits that read back as the value, of several shortest the nearest, and of two
equally near the even one (the specification's note 2), placed by steps 6 to 12. `-0` gives `"0"`.

---

## `NumberText.ToRadixString` — ECMA-262 §6.1.6.1.20 `Number::toString(x, radix)` and §21.1.3.6 `Number.prototype.toString`

### Signature

```csharp
public static string ToRadixString(double value, int radix);
```

### What the caller must have done first

| Step | Clause | What the adapter does |
|---|---|---|
| 1 | §21.1.3.6 step 1 | `thisNumberValue(this value)`; a TypeError propagates |
| 2–3 | steps 2–3 | An undefined radix is 10; otherwise `ToIntegerOrInfinity(radix)`, whose abrupt completion propagates |
| 4 | step 4 | A radix outside 2 to 36 throws the engine's RangeError |

### Preconditions

`radix` is 2 to 36; outside it the library throws `ArgumentOutOfRangeException`, which an adapter
that has done step 4 never sees. Radix 10 is `ToString(value)`.

### Work and limits

- **Meter:** none. The result is bounded by the value: at most about 1,100 characters (radix 2 of
  the smallest subnormal). An engine that charges the string it creates charges it as before.
- **Limits:** none needed.

### Result

The fewest digits in the radix that read back as the value, of several the nearest, ties to the
even last digit, placed positionally (no exponent at a radix other than 10). This is the
specification's step 5 with the note's recommendation applied at every radix.

---

## `NumberText.ToFixed`, `ToExponential` and `ToPrecision` — ECMA-262 §21.1.3.3, §21.1.3.2, §21.1.3.5

### Signature

```csharp
public static string ToFixed(double value, int fractionDigits);          // 0 to 100
public static string ToExponential(double value);                        // fractionDigits undefined
public static string ToExponential(double value, int fractionDigits);    // 0 to 100
public static string ToPrecision(double value, int precision);           // 1 to 100
```

### What the caller must have done first

| Operation | Steps | What the adapter does, in this order |
|---|---|---|
| `toFixed` | §21.1.3.3 steps 1–4 | `thisNumberValue`; `ToIntegerOrInfinity(fractionDigits)`; a count outside 0 to 100 throws RangeError (before the value is looked at: `(NaN).toFixed(101)` throws) |
| `toExponential` | §21.1.3.2 steps 1–5 | `thisNumberValue`; `ToIntegerOrInfinity(fractionDigits)`; **a non-finite value returns `Number::toString(x)` before the range check** (`(Infinity).toExponential(101)` is `"Infinity"`), which the library also does, so the adapter may call it; then a count outside 0 to 100 throws RangeError. An undefined `fractionDigits` calls the one-argument overload |
| `toPrecision` | §21.1.3.5 steps 1–6 | `thisNumberValue`; an undefined precision returns `ToString(x)`; `ToIntegerOrInfinity(precision)`; a non-finite value returns `Number::toString(x)`; then a precision outside 1 to 100 throws RangeError |

### Preconditions

The digit count is in its range; outside it the library throws `ArgumentOutOfRangeException`. Any
Number is accepted: a non-finite value gives `Number::toString(x)`, and `toFixed` of a magnitude of
10^21 or more gives `Number::toString(x)` (step 9).

### Work and limits

- **Meter:** none. The results are at most about 120 characters.
- **Limits:** none needed.

### Result

The exact value rounded with the specification's tie rule (the larger candidate). `toFixed` drops
the sign of `-0`; `toExponential` and `toPrecision` write `-0` as `0`.

---

## `NumberParsing.StringToNumber` — ECMA-262 §7.1.4.1.1 `StringToNumber`

### Signature

```csharp
public static double StringToNumber(ReadOnlySpan<char> text, WorkCharge? charge = null);
```

### What the caller must have done first

`ToNumber`'s dispatch on the argument's type (§7.1.4): this operation is the String case only.

### Preconditions

None.

### Work and limits

- **Meter:** charged the text's length once, before any work, when the text is not empty. `null`
  charges nothing.
- **Limits:** none needed: the work is linear in the text.

### Result

The correctly rounded value of a `StringNumericLiteral` after `StrWhiteSpace` is trimmed from both
ends; `+0` for an empty or white-space-only string; NaN for any other string. Non-decimal prefixes
(`0x`, `0o`, `0b`) are read exactly at any length; a separator is refused (NaN).

---

## `NumberParsing.ParseFloat` and `ParseInt` — ECMA-262 §19.2.4 and §19.2.5

### Signature

```csharp
public static double ParseFloat(ReadOnlySpan<char> text, WorkCharge? charge = null);
public static double ParseInt(ReadOnlySpan<char> text, int radix, WorkCharge? charge = null);
```

### What the caller must have done first

| Step | Clause | What the adapter does |
|---|---|---|
| 1 | §19.2.4 step 1, §19.2.5 step 1 | `ToString(string)`; an abrupt completion propagates. `null` becomes `"null"` |
| 2 | §19.2.5 step 8 | `ToInt32(radix)`; an undefined radix is passed as 0, which is what `ToInt32(undefined)` is |

### Preconditions

None. `ParseInt` answers NaN itself for a radix that is not 0 and outside 2 to 36 (step 8).

### Work and limits

- **Meter:** each charges the text's length once, before any work, when the text is not empty.
- **Limits:** none needed. A digit run whose value passes 2^1100 is `+Infinity` without being
  accumulated further.

### Result

`ParseFloat`: the correctly rounded value of the longest `StrDecimalLiteral` after leading white
space, or NaN. `ParseInt`: the value of the longest run of radix digits after leading white space,
a sign and (at radix 0 or 16) a `0x` prefix, **correctly rounded at every radix**: the specification
admits an approximation beyond twenty decimal digits and at radixes other than 2, 4, 8, 10, 16 and
32, and this library gives the exact value there too, as both engines' plans require. `-0` for a
negative zero; NaN for no digit.

---

## `NumberParsing.IsStrWhiteSpaceChar` and `TrimStrWhiteSpace` — ECMA-262 §7.1.4.1 `StrWhiteSpaceChar`

### Signature

```csharp
public const string UnicodeVersion = "17.0.0";
public static bool IsStrWhiteSpaceChar(char c);
public static ReadOnlySpan<char> TrimStrWhiteSpace(ReadOnlySpan<char> text, TrimEnds ends);
```

### Result

The specification's `WhiteSpace` (TAB, VT, FF, ZWNBSP U+FEFF and every `Space_Separator` code point
of Unicode 17.0.0) and `LineTerminator` (LF, CR, U+2028, U+2029) sets, **never the runtime's Unicode
data**: U+0085 and U+180E are not members. `TrimStrWhiteSpace` slices the text; it allocates nothing.
`String.prototype.trim` beyond this predicate is milestone SJL-5's.

---

## `NumericLiterals.Evaluate` — ECMA-262 §12.9.3 `NumericLiteral` and §12.9.3.1 early errors, §12.9.3.2 `NumericValue`

### Signature

```csharp
public static ResultStatus Evaluate(ReadOnlySpan<char> text, bool strict, LengthLimit bigIntBits,
    out NumericLiteral literal, out Failure failure, WorkCharge? charge = null);
public readonly record struct NumericLiteral(NumericLiteralKind Kind, NumericLiteralForm Form, double Number, BigInteger BigInt);
public enum NumericLiteralKind { Number, BigInt }
public enum NumericLiteralForm { Decimal, NonDecimal, LegacyOctal, NonOctalDecimal }
```

### What the caller must have done first

Scan the token: the text is exactly one literal's source characters, the `n` suffix included, and
nothing after it. Scanning, the token's position and the engine's diagnostic stay in the front end
(the milestone changes no parser structure); the library re-checks the grammar and answers the
value.

### Preconditions

None: any text is accepted, and a text that is not one `NumericLiteral` is `Malformed`.

### Work and limits

- **Meter:** charged the text's length before any work, and a BigInt's digits again before its value
  is built.
- **Limits:** `bigIntBits` bounds a BigInt literal's bit length. A literal wider than it is refused
  as `LimitExceeded` before its value is built when its digit count already exceeds the limit, and
  after otherwise.

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | `literal` holds the value: `Number` for a Number literal, `BigInt` for a BigInt literal; `Form` says which grammar form it was | — | Its literal node's value | Its literal expression's value |
| `Malformed` | Not one literal, or an early error | SyntaxError | Its scanner's diagnostic at the token, the identifier rendered as its own text | `MalformedNumericLiteral` at the token |
| `LimitExceeded` | A BigInt literal wider than `bigIntBits` | None: the engine's resource policy | Not reached (it passes no limit; see below) | Its "BigInt literal wider than N bits" refusal |

### Message identifiers

`failure.Position` is the offset in the text where the grammar stopped.

| Identifier | When |
|---|---|
| `number.literal-empty` | The text is empty |
| `number.literal-start` | The text does not begin with a digit or a `.` and a digit |
| `number.literal-digits` | A digit is missing where the grammar needs one, or a separator is not between two digits |
| `number.literal-exponent` | An exponent indicator without digits |
| `number.literal-trailing` | Characters follow a complete literal (`3in`, `1.5n`, `0_1`, `07.5`, and `01n`, a legacy octal followed by `n`) |
| `number.bigint-suffix` | An `n` that is not the last character, or after a decimal form that takes none (`08n`, `1n2`) |
| `number.legacy-octal-in-strict-code` | `010` in strict code (§12.9.3.1) |
| `number.non-octal-decimal-in-strict-code` | `08` or `09.5` in strict code (§12.9.3.1) |
| `number.bigint-too-wide` | A BigInt literal wider than `bigIntBits` (`LimitExceeded`) |

### Engine-specific behaviour

| Parameter | Broiler.JS passes | Broiler.VM passes | Why they differ |
|---|---|---|---|
| `bigIntBits` | `new LengthLimit(long.MaxValue)`: BigInt width is unbounded (decision SJL-D-15 (a)) | Its constant ceiling, `Format.JsFormat.CeilingBigIntConstantBytes * 8` (65,536 bits) | Each engine's resource policy |
| `strict` | The function's strictness | The validator's strictness | — |

**Broiler.JS's `m` (Decimal) literal suffix is not the library's** (decision SJL-D-17): the scanner
recognises it and Broiler.JS computes its value as it does today; the text without the suffix is not
passed to `Evaluate`.

---

## `NumericKeys.TryParseArrayIndex` and `IsCanonicalNumericString` — ECMA-262 §6.1.7 (array index) and §7.1.21 `CanonicalNumericIndexString`

### Signature

```csharp
public const uint MaximumArrayIndex = 4294967294;
public static bool TryParseArrayIndex(ReadOnlySpan<char> key, out uint index);
public static bool IsCanonicalNumericString(ReadOnlySpan<char> key, out double value);
```

### Result

`TryParseArrayIndex`: whether the key is the canonical string of an integer from 0 to 2^32 − 2
(`"0"`, `"1"`, ..., `"4294967294"`; not `"01"`, `"-0"`, `"1.0"` or `"4294967295"`).
`IsCanonicalNumericString`: whether `CanonicalNumericIndexString(key)` is not undefined, that is
whether `ToString(ToNumber(key))` is the key or the key is `"-0"`, with the Number in `value` (`-0`
for `"-0"`). Neither allocates. **Neither is metered**: a key is bounded by the engine before it is
one.

---

*Written 2026-10-12, at milestone SJL-3.3. Revisions of this guide are dated in place, quoting the
text they replace.*
