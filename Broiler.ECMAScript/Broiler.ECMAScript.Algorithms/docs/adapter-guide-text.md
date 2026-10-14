# Adapter guide: `Broiler.ECMAScript.Algorithms` — text

An engine's adapter is written against this guide, not against the library's source: if an adapter
needs something this guide does not say, the guide is incomplete and is corrected first. The
obligation is the programme's (`Broiler.VM/docs/javascript-shared-libraries/governance.md` section
10 and `architecture.md` section 5); the conventions every operation follows are in
[`conventions.md`](../../docs/conventions.md); the area's design is
`Broiler.VM/docs/javascript-shared-libraries/areas/text.md`, and its verdict JSD-0070 in Broiler.VM's
profile, with its six sub-areas: whitespace and UTF-16, padding and indices, substitution templates,
URI, base64 and hex, and JSON lexing.

- **Library and version:** `Broiler.ECMAScript.Algorithms` `0.1.0-preview.3` and later, API baseline
  [`PublicAPI.txt`](../PublicAPI.txt), namespace `Broiler.ECMAScript.Text`.
- **Specification pins:** ECMA-262 at commit `0248456c` (`conformance/vectors/sources/ecma262.pin`),
  whose clauses include the `Uint8Array` base64 and hex codecs and `JSON.rawJSON`.
- **Data versions:** none. `TrimString` reads the `StrWhiteSpaceChar` set, whose Unicode version is
  `NumberParsing.UnicodeVersion` (milestone SJL-3).
- **Consumers:** listed here at the cut-overs, milestones SJL-5.4 (Broiler.VM) and SJL-5.5
  (Broiler.JS); JSD-0070's X6 lists the members each replaces.
- **Vectors:** `conformance/vectors/text/`, corpus revision 11 or later. The library-level runner,
  `Broiler.ECMAScript/runners/AlgorithmsLibrary.Text.cs`, is also the reference adapter: every
  operation below is called there as an engine calls it. The vectors whose outcome is an engine's
  conversion order, option reading, typed-array validation, raw-JSON brand or string ceiling are
  engine-level only.

**What no operation here does**, by convention 2: no `ToString`, `ToIntegerOrInfinity`, `ToLength` or
`ToBoolean` of an argument, no `GetOptionsObject` or option read, no typed-array validation, bounds or
detachment check, no property read or definition, no iteration, no call of a reviver, replacer,
`toJSON` or replacement function, and no error object. The adapter does those, in the order the
specification gives, and only then calls the library. **What stays in the engines** (JSD-0070, decision
2): the drivers whose steps a script can observe - `JSON.parse`'s object building and reviver,
`JSON.stringify`, `split`, `replace`'s evaluation of the template's parts - and string search, whose
ordinal base class library calls are the algorithm.

**Charges** are Broiler.VM's units before its cut-over (JSD-0070, decision 3), so that an exhaustion
verdict does not move. An engine with no budget passes `null`.

**Policies are parameters** (decision 4): the longest String an engine holds is the `LengthLimit` it
passes to `StringPadding.Pad`; its JSON depth is the `DepthLimit` it passes to `JsonReader`. Broiler.VM
passes 2^24 code units, and 512 for `JSON.parse` and 1024 for its JSON module translation; Broiler.JS
chooses its own at SJL-5.5.

---

## `Utf16` — ECMA-262 §6.1.4, §11.1.4, §22.1.3.31, §22.2.7.3

### Signature

```csharp
public readonly record struct CodePointRecord(int CodePoint, int CodeUnitCount, bool IsUnpairedSurrogate);
public const char ReplacementCharacter = (char)0xFFFD;
public static CodePointRecord CodePointAt(ReadOnlySpan<char> text, int position);
public static bool IsWellFormed(ReadOnlySpan<char> text, WorkCharge? charge = null);
public static string ToWellFormed(string text, WorkCharge? charge = null);
public static long AdvanceStringIndex(ReadOnlySpan<char> text, long index, bool unicode);
```

`TrimString` (§22.1.3.32.1) is `NumberParsing.TrimStrWhiteSpace(text, TrimEnds)`, beside the set it
reads; `trim`, `trimStart` and `trimEnd` call it with `Both`, `Start` and `End`.

### What the caller must have done first

| Operation | Clause | What the adapter does |
|---|---|---|
| `codePointAt`, `at`, the String iterator | §22.1.3.4, §22.1.5.1 | `ToString` of the receiver; `ToIntegerOrInfinity` of the position; `undefined` when it is outside the String, before `CodePointAt` |
| `isWellFormed`, `toWellFormed` | §22.1.3.10, §22.1.3.31 | `ToString` of the receiver |
| `RegExp.prototype[@@match]`, `[@@replace]`, `[@@split]`, `[@@matchAll]` | §22.2.7.3 | `ToLength` of `lastIndex`, and the flags' `fullUnicode`, before `AdvanceStringIndex` |

### Preconditions

- `CodePointAt`'s position is an index of the text; `AdvanceStringIndex`'s index is from 0 to 2^53 - 1,
  and may be past the text's end.

### Work and limits

- **Meter:** `IsWellFormed` and `ToWellFormed` charge the text's length plus one, first. `CodePointAt`
  and `AdvanceStringIndex` charge nothing; Broiler.VM's `codePointAt` charges its own one unit.
- **Limits:** none. `ToWellFormed`'s result has the text's length.

### Statuses and identifiers

None: every operation completes.

## `StringPadding.Pad` — ECMA-262 §22.1.3.17.2 StringPad

### Signature

```csharp
public enum PadPlacement { Start = 0, End = 1 }
public static ResultStatus Pad(string text, long maxLength, string filler, PadPlacement placement, LengthLimit limit, out string result, WorkCharge? charge = null);
```

### What the caller must have done first

| Step | Clause | What the adapter does |
|---|---|---|
| 1 | §22.1.3.17.1 step 1 | `RequireObjectCoercible` and `ToString` of the receiver |
| 2 | step 2 | `ToLength(maxLength)` |
| 3 | step 4 | when `maxLength` does not exceed the String's length, answer the String: **before the filler is converted** |
| 4 | steps 5 and 6 | the filler: `" "` when undefined, otherwise `ToString(fillString)` |

### Preconditions

- `maxLength` is from 0 to 2^53 - 1; the text and the filler are not null.

### Work and limits

- **Meter:** the result's length, and at least one unit, before it is built; nothing when it refuses.
- **Limits:** `limit` is the engine's longest String. A result longer than it, or than .NET's largest
  String (2^30 - 33 code units), is `LimitExceeded`; nothing is built. An empty filler answers the String
  whatever the length (correspondence row P2).

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | `result` is the padded String, or the String itself | — | the String | the String |
| `LimitExceeded` | the pad is longer than the limit | `RangeError` | `RangeError` | `RangeError` "Invalid string length" |

## `RelativeIndex` — the relative-index pattern of ECMA-262 §22.1.3.22 and its kin

### Signature

```csharp
public static long Clamped(double relative, long length);
public static bool TryElement(double relative, long length, out long index);
public static long LastSearchStart(double relative, long length);
```

### What the caller must have done first

`ToIntegerOrInfinity` of the argument (an `undefined` end is the length, before the call); the
length's own reading (`ToLength`, a typed array's length, a String's length).

| Member | Built-ins |
|---|---|
| `Clamped` | String `slice`, `substr`'s start; Array and typed-array `slice`, `subarray`, `fill`, `copyWithin`, `splice`, `toSpliced`, `indexOf`, `includes`; `ArrayBuffer` and `SharedArrayBuffer` `slice` |
| `TryElement` | String, Array and typed-array `at`; Array and typed-array `with`, whose adapter raises `RangeError` when it answers false |
| `LastSearchStart` | Array and typed-array `lastIndexOf` with a `fromIndex` (an absent one starts at the length minus one) |

### Preconditions

- The relative value is integral or infinite, never NaN; the length is from 0 to 2^53 - 1.

### Work and limits

None; constant time.

## `ReplacementTemplate.Parse` — ECMA-262 §22.1.3.19.1 GetSubstitution, the template's half

### Signature

```csharp
public enum ReplacementPartKind { Literal, Dollar, Match, Before, After, Capture, NamedCapture }
public readonly record struct ReplacementPart(ReplacementPartKind Kind, int Start, int Length, int CaptureIndex);
public static ReplacementPart[] Parse(ReadOnlySpan<char> template, int captureCount, bool hasNamedCaptures, WorkCharge? charge = null);
```

### What the caller must have done first

`ToString` of the replacement, at the specification's step (for `replaceAll`, **before** the search -
correspondence row S1); the captures' count `m`, and whether the result's named captures are undefined.
A String pattern has no captures and no named captures.

### What the caller does with the parts

It evaluates them in order, as GetSubstitution's steps do: a literal is `template[Start..Start+Length]`;
`Dollar` is `$`; `Match` the matched substring; `Before` the String before the position; `After` the
String from `min(position + matchLength, length)`; `Capture` the capture `CaptureIndex` (from one), or
nothing when it is undefined; `NamedCapture` `Get(namedCaptures, name)`, `ToString`'d, once per
occurrence and left to right, or nothing when undefined. Broiler.VM charges each expansion its length
plus one and holds the result to its ceiling, as its `RegExpAppend` did.

### Work and limits

- **Meter:** the template's length plus one, first. Broiler.VM passes its meter for a regular
  expression's replacement and `null` for a String pattern's, whose template it did not charge.

## `UriCodec` — ECMA-262 §19.2.6.5 Encode, §19.2.6.6 Decode, §B.2.1.1 escape, §B.2.1.2 unescape

### Signature

```csharp
public const string ReservedAndNumberSign = ";/?:@&=+$,#";
public static ResultStatus Encode(ReadOnlySpan<char> text, ReadOnlySpan<char> extraUnescaped, out string result, out Failure failure, WorkCharge? charge = null);
public static ResultStatus Decode(ReadOnlySpan<char> text, ReadOnlySpan<char> preserveEscapeSet, out string result, out Failure failure, WorkCharge? charge = null);
public static string Escape(ReadOnlySpan<char> text, WorkCharge? charge = null);
public static string Unescape(ReadOnlySpan<char> text, WorkCharge? charge = null);
public static int HexDigitValue(char unit);
```

### What the caller must have done first

`ToString` of the argument. `encodeURI` passes `ReservedAndNumberSign` as `extraUnescaped`,
`encodeURIComponent` the empty set; `decodeURI` passes it as `preserveEscapeSet`, `decodeURIComponent`
the empty set.

### Work and limits

- **Meter:** one unit before each step (a surrogate pair is one step of `Encode`, an escape one step of
  `Unescape`), and for `Decode` one more before each continuation octet is read.

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | `result` is the String | — | the String | the String |
| `Malformed` | see the identifiers | `URIError` | `URIError` | `URIError` "URI malformed" |

### Message identifiers

| Identifier | When | Position |
|---|---|---|
| `uri.unpaired-surrogate` | `Encode` reaches a lone surrogate | the surrogate |
| `uri.malformed-escape` | `Decode` reaches `%` without two hexadecimal digits after it, or a sequence ends early | that `%` |
| `uri.invalid-octet` | a lead octet that starts no sequence (80 to BF, F8 to FF), or a continuation that is not 10xxxxxx | its `%` |
| `uri.invalid-utf8` | a sequence that is overlong, a surrogate or above U+10FFFF | the sequence's first `%` |

## `Base64Codec` — the `Uint8Array` base64 and hex clauses of ECMA-262

### Signature

```csharp
public enum Base64Alphabet { Base64 = 0, Base64Url = 1 }
public enum LastChunkHandling { Loose = 0, Strict = 1, StopBeforePartial = 2 }
public static int DecodedLengthBound(int textLength);
public static ResultStatus FromBase64(ReadOnlySpan<char> text, Base64Alphabet alphabet, LastChunkHandling lastChunkHandling, Span<byte> destination, out int read, out int written, out Failure failure, WorkCharge? charge = null);
public static ResultStatus FromHex(ReadOnlySpan<char> text, Span<byte> destination, out int read, out int written, out Failure failure, WorkCharge? charge = null);
public static string ToBase64(ReadOnlySpan<byte> bytes, Base64Alphabet alphabet, bool omitPadding, WorkCharge? charge = null);
public static string ToHex(ReadOnlySpan<byte> bytes, WorkCharge? charge = null);
```

### What the caller must have done first

| Built-in | What the adapter does |
|---|---|
| `fromBase64`, `fromHex` | refuse a non-String with `TypeError`; for base64, `GetOptionsObject` and the `alphabet` and `lastChunkHandling` options in order, each refusing a value that is not one of the names with `TypeError`; decode into a buffer of `DecodedLengthBound(length)` bytes; on `Completed`, a new `Uint8Array` of the bytes written |
| `setFromBase64`, `setFromHex` | `ValidateUint8Array`, the String, the options as above, and the destination's bounds and detachment; decode into the view's bytes, whose length is `maxLength`; **store the bytes written even when the status is not `Completed`**, then throw or answer `{ read, written }` |
| `toBase64`, `toHex` | `ValidateUint8Array`, the options (`alphabet`, then `omitPadding`), `GetUint8ArrayBytes` |

### Work and limits

- **Meter:** a decode, the text's length plus one before its loop; nothing when the destination is empty,
  or for hex when the length is odd. `ToBase64` the byte count times four thirds plus four; `ToHex` twice
  the byte count plus one. Broiler.VM charges its own copy of the view's bytes.
- **Limits:** the destination is the decode's `maxLength`. An encoding longer than .NET's largest String
  is a precondition (`ArgumentOutOfRangeException`); an engine's ArrayBuffer cannot hold that many bytes.

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | `read` and `written` are the clause's | — | the array, or `{ read, written }` | the same |
| `Malformed` | see the identifiers; `written` bytes are in the destination | `SyntaxError` | `SyntaxError` | `SyntaxError`, its message keyed on the identifier |

### Message identifiers

`base64.invalid-character`, `base64.wrong-alphabet` (`+` or `/` under base64url),
`base64.misplaced-padding`, `base64.incomplete-padding`, `base64.text-after-padding`, `base64.extra-bits`,
`base64.unpadded-chunk`, `base64.single-character-chunk`, `hex.odd-length` and `hex.invalid-digit`, each
with the position of the code unit at fault (the text's end for a chunk that ends it).

## `JsonReader` — ECMA-262 §25.5.1 JSON.parse's grammar

### Signature

```csharp
public enum JsonToken { None, BeginObject, EndObject, BeginArray, EndArray, PropertyName, String, Number, True, False, Null, EndOfText }
public ref struct JsonReader
{
    public JsonReader(ReadOnlySpan<char> text, DepthLimit maximumDepth, WorkCharge? charge = null);
    public JsonToken Token { get; }
    public string StringValue { get; }
    public double NumberValue { get; }
    public int TokenStart { get; }
    public int TokenEnd { get; }
    public int Depth { get; }
    public Failure Failure { get; }
    public ResultStatus Read();
    public static ResultStatus Validate(ReadOnlySpan<char> text, DepthLimit maximumDepth, out Failure failure, WorkCharge? charge = null);
}
```

### What the caller must have done first, and does around it

`ToString` of the text. Then it reads tokens to `EndOfText` and builds the values: an ordinary object for
`BeginObject` with `CreateDataProperty` for each `PropertyName` and the value after it (a repeated key's
last value, at its first position); an Array for `BeginArray`; the String and Number values. A reviver,
the parse records (`TokenStart` and `TokenEnd` give a primitive's source text) and `JSON.rawJSON`'s brand
are the engine's. `JSON.rawJSON` validates its text with `Validate` after its own start and end tests.
Broiler.VM's compile-time JSON module translation reads the tokens and writes its module source.

### Work and limits

- **Meter:** one unit for each value, before its depth is checked; one for each `:` and `,`; each run of
  white space skipped, by its length; a String's whole source length, quotes included, when it closes; a
  Number's text through `StringToNumber`.
- **Limits:** a value deeper than `maximumDepth` (the document is depth 0) is `LimitExceeded`, before it
  is read. The reader does not recurse.

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | `Token` and its value are set | — | builds | builds |
| `Malformed` | the grammar stops at `Failure.Position` | `SyntaxError` | `SyntaxError` | `SyntaxError` "... at position n"; a module is refused `InvalidJsonModule` |
| `LimitExceeded` | a value deeper than the limit | none: the engine's policy | `RangeError` | `RangeError`; a module is refused `NestingTooDeep` |

### Message identifiers

`json.unexpected` (a code unit the grammar has no place for, or the end of the text: the position says
which), `json.unterminated-string`, `json.control-character`, `json.bad-escape`,
`json.bad-unicode-escape` and `json.too-deep`.

## `JsonText` — ECMA-262 §25.5.2.3 QuoteJSONString, §25.5.2.1 steps 6 to 8

### Signature

```csharp
public const int MaximumGap = 10;
public static string Quote(ReadOnlySpan<char> value, WorkCharge? charge = null);
public static string GapFromNumber(double space);
public static string GapFromString(string space);
```

### What the caller must have done first

For the gap, `JSON.stringify` unwraps a Number object with `ToNumber` and a String object with `ToString`
(steps 4 and 5); any other `space` gives the empty gap without a call. The driver - `toJSON`, the
replacer, the property list, the cycle check, the raw-JSON brand and the joining of members, with a line
feed and never the platform's line ending (correspondence row J5) - is the engine's.

### Work and limits

- **Meter:** `Quote` charges the String's length plus one, first.
