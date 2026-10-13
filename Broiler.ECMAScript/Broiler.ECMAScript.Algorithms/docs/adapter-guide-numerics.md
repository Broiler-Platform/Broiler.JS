# Adapter guide: `Broiler.ECMAScript.Algorithms` — numerics

An engine's adapter is written against this guide, not against the library's source: if an adapter
needs something this guide does not say, the guide is incomplete and is corrected first. The
obligation is the programme's (`Broiler.VM/docs/javascript-shared-libraries/governance.md` section
10 and `architecture.md` section 5); the conventions every operation follows are in
[`conventions.md`](../../docs/conventions.md); the area's design is
`Broiler.VM/docs/javascript-shared-libraries/areas/numerics.md`, and its verdict JSD-0067 in
Broiler.VM's profile, with its four sub-areas: integer conversions and Float16, the Math kernels,
exact rounding, and the BigInt kernel.

- **Library and version:** `Broiler.ECMAScript.Algorithms` `0.2.0-preview.1` and later, API baseline
  [`PublicAPI.txt`](../PublicAPI.txt), namespace `Broiler.ECMAScript.Numbers`.
- **Specification pins:** ECMA-262 at commit `0248456c` (`conformance/vectors/sources/ecma262.pin`).
  The rounding modes cite Temporal and ECMA-402 by clause for their names and tables; neither is
  pinned by the corpus, and no vector of this area takes its expectation from them.
- **Data versions:** none.
- **Consumers:** listed here at the cut-overs, milestones SJL-4.4 (Broiler.VM) and SJL-4.5
  (Broiler.JS); JSD-0067's X1 lists the members each replaces.
- **Vectors:** `conformance/vectors/numerics/`, corpus revision 8 or later. The library-level runner
  is `Broiler.ECMAScript/runners/AlgorithmsLibrary.cs`, which is also the reference adapter: every
  operation below is called there exactly as an engine calls it. The vectors of `Math.fround`,
  `Math.clz32`, `Math.imul` and `ToPropertyKey`, and the width-policy vectors, are engine-level only.

**What no operation here does**, by convention 2: no `ToNumber`, `ToBigInt`, `ToIndex` or
`ToString` of an argument, no property read, no iteration, no error object, and no string the
engine's heap holds. The adapter does those, in the order the specification gives, and only then
calls the library. **What the library does not share**: `Math.fround`, `clz32`, `imul`, `max`,
`min`, `sign` and the trigonometric and logarithmic families, whose specified results the platform
already computes (JSD-0067, "What this rejects").

---

## `IntegerConversions` — ECMA-262 §7.1.5 to §7.1.12, §7.1.15, §7.1.16, §7.1.20, §7.1.22

### Signature

```csharp
public const long MaximumSafeInteger = 9007199254740991;
public static int ToInt32(double number);       public static uint ToUint32(double number);
public static short ToInt16(double number);     public static ushort ToUint16(double number);
public static sbyte ToInt8(double number);      public static byte ToUint8(double number);
public static byte ToUint8Clamp(double number);
public static double ToIntegerOrInfinity(double number);
public static long ToLength(double number);
public static bool TryToIndex(double number, out long index);
public static long ToBigInt64(BigInteger value);
public static ulong ToBigUint64(BigInteger value);
```

### What the caller must have done first

| Step | Clause | What the adapter does |
|---|---|---|
| 1 | Each clause's step 1 | `ToNumber(argument)` (or `ToBigInt` for `ToBigInt64` and `ToBigUint64`); an abrupt completion propagates |
| 2 | §7.1.22 `ToIndex`, an undefined value | 0, without calling the library |
| 3 | §7.1.22 step 2 | When `TryToIndex` answers `false`, throws the RangeError |

### Preconditions

None: every double is accepted, and every `BigInteger`.

### Work and limits

- **Meter:** none. Every conversion is constant work.

### Result

The modular conversions are the truncation modulo 2^n read in the width, exactly for every double
(no rounding through a narrower type), 0 for NaN, the infinities and the zeros. `ToUint8Clamp` rounds
to nearest with ties to even, 0 for NaN. `ToIntegerOrInfinity` never answers -0. `ToLength` answers 0
to 2^53 − 1. `TryToIndex` answers `ToIntegerOrInfinity(number)` when it is 0 to 2^53 − 1.
`ToBigInt64` and `ToBigUint64` answer the BigInt modulo 2^64, as `NumericToRawBytes` writes it.

---

## `Float16.Encode`, `Decode` and `Round` — ECMA-262 §25.1.3.17 `NumericToRawBytes`, §25.1.3.15 `RawBytesToNumeric`, §21.3.2.17 `Math.f16round`

### Signature

```csharp
public const ushort CanonicalNaN = 0x7E00;
public static ushort Encode(double value, Float16NanPolicy nanPolicy);
public static double Decode(ushort bits);
public static double Round(double value);
public enum Float16NanPolicy { Canonical, PreservePayload }
```

### What the caller must have done first

`ToNumber` of the value (`Math.f16round`, `DataView.prototype.setFloat16`, a `Float16Array` store);
the byte order and the buffer are the engine's.

### Preconditions

None.

### Work and limits

- **Meter:** none.

### Result

`Encode` rounds once, from the double's bits, to the nearest binary16 with ties to even, overflowing
to an infinity at 65,520; never through binary32, which would round twice. A NaN is written under the
policy: `Canonical` writes `0x7E00`; `PreservePayload` keeps the sign and the payload's ten most
significant bits and sets the quiet bit (`sign | 0x7E00 | (fraction >> 42)`). `Decode` is exact, -0
included; every NaN pattern decodes to NaN. `Round` is `Decode(Encode(value))` for a non-NaN value,
and NaN for NaN.

---

## `MathKernels` — ECMA-262 §21.3.2 and §6.1.6.1.3

### Signature

```csharp
public static double Round(double x);                         // Math.round, §21.3.2.28
public static double Power(double @base, double exponent);    // Number::exponentiate, §6.1.6.1.3
public static double Hypot(ReadOnlySpan<double> values);      // Math.hypot, §21.3.2.18
public static double Cbrt(double x);                          // Math.cbrt, §21.3.2.9
public static double Expm1(double x);                         // Math.expm1, §21.3.2.15
public static double Log1p(double x);                         // Math.log1p, §21.3.2.21
```

### What the caller must have done first

| Operation | What the adapter does |
|---|---|
| All | `ToNumber` of each argument, in order; an abrupt completion propagates. `Math.hypot` converts **every** argument before it looks at any value (§21.3.2.18 steps 2–3), then passes the converted span |
| `Power` | Nothing more: it is the one entry for `**` and `Math.pow` (JSD-0067 decision 5), compiled fast paths included. A BigInt `**` is `BigIntMath.Power` |

### Preconditions

None.

### Work and limits

- **Meter:** none. Each does bounded work for its inputs (JSD-0067 decision 3); `Hypot` is linear in
  the span. An engine that charges around a Math call keeps that charge in its adapter.

### Result and precision

| Operation | Result |
|---|---|
| `Round` | The specification's: a tie toward +Infinity; -0 for -0.5 ≤ x < -0 and for -0; the argument itself when it is not finite or already integral |
| `Power` | The clause's special cases exactly (`1 ** NaN` and `(-1) ** ±Infinity` are NaN), then the platform's `Math.Pow`, the approximation the clause's last step admits |
| `Hypot` | +Infinity if any value is infinite, else NaN if any is NaN, else +0 if every one is a zero; otherwise within one unit in the last place, and exact whenever the exact value is a double. No intermediate overflows or underflows |
| `Cbrt` | Correctly rounded; NaN, the infinities and the zeros are themselves |
| `Expm1`, `Log1p` | Within one unit in the last place over the whole domain, near zero included: fdlibm 5.3's (ported; notice in `THIRD_PARTY_NOTICES.md`), not the platform's `double.ExpM1` and `LogP1` |

---

## `PreciseSum` — ECMA-262 §21.3.2.34 `Math.sumPrecise`

### Signature

```csharp
public sealed class PreciseSum
{
    public PreciseSum();
    public void Add(double value);
    public double Result();
}
```

### What the caller must have done first

The clause's iteration is the engine's, because it runs guest code: `GetIterator`, each `IteratorStep`,
the TypeError for a value that is not a Number (closing the iterator), and the RangeError once the
count passes 2^53 − 1. The adapter creates one accumulator per call, `Add`s each Number as it arrives,
and reads `Result` once the iterator is done.

### Preconditions

None. An accumulator is not thread-safe and is used by one call.

### Work and limits

- **Meter:** none. Each `Add` is bounded by the exponent range: the expansion holds at most about 40
  partials. An engine that charges per element keeps that charge in its adapter.

### Result

NaN if any value was NaN or both infinities were added; an infinity if one was; -0 when nothing was
added or every value was -0; otherwise the exact sum rounded once to the nearest Number, ties to even.
A finite exact sum past the largest double's rounding range is an infinity; partials that overflow on
the way never make the result an infinity on their own.

---

## `ExactRounding` and `RoundingMode` — ECMA-262 §6.1.6.1; Temporal §13.8, §13.10, §13.25 to §13.28; ECMA-402 §15.5.11 to §15.5.14

### Signature

```csharp
public static bool TryParseRoundingMode(ReadOnlySpan<char> name, out RoundingMode mode);
public static RoundingMode Negate(RoundingMode mode);
public static UnsignedRoundingMode GetUnsignedRoundingMode(RoundingMode mode, bool negative);
public static BigInteger ApplyUnsignedRoundingMode(BigInteger numerator, BigInteger denominator,
    BigInteger lower, BigInteger upper, UnsignedRoundingMode mode);
public static BigInteger RoundNumberToIncrement(BigInteger numerator, BigInteger denominator,
    BigInteger increment, RoundingMode mode);
public static BigInteger RoundNumberToIncrementAsIfPositive(BigInteger numerator, BigInteger denominator,
    BigInteger increment, RoundingMode mode);
public static double ToDouble(BigInteger numerator, BigInteger denominator);
public enum RoundingMode { Ceil, Floor, Expand, Trunc, HalfCeil, HalfFloor, HalfExpand, HalfTrunc, HalfEven }
public enum UnsignedRoundingMode { Zero, Infinity, HalfZero, HalfInfinity, HalfEven }
```

### What the caller must have done first

| Step | What the adapter does |
|---|---|
| Option read | `GetOption(options, "roundingMode", ...)`: the property read and its `ToString` are the engine's; `TryParseRoundingMode` answers `false` where the clause throws the RangeError, and an absent option is the caller's default |
| The quantity | Expresses the value as an exact ratio: a duration in nanoseconds over a unit's length, a decimal as an integer over a power of ten. **Never a double** |
| The increment | Validates it (Temporal's `ValidateTemporalRoundingIncrement`) and throws its RangeError first |

### Preconditions

Thrown as `ArgumentOutOfRangeException` when they do not hold; an adapter that has done the steps
above never sees one.

- A denominator is positive (`ToDouble`: not zero; a negative one negates the quotient).
- An increment is positive.
- `ApplyUnsignedRoundingMode`: `lower` ≤ value ≤ `upper` and `lower` < `upper`; for `HalfEven`,
  `lower` is a multiple of `upper − lower` (`NudgeToCalendarUnit` passes a window wider than one).

### Work and limits

- **Meter:** none. The work is a few divisions of the operands; their size is the caller's.

### Result

The rounded multiple, exactly; ties are decided exactly. An exact end is itself under every mode,
the upper one included, which the clause leaves to its callers *(added 2026-10-13, later the same
day, with the library's correction)*. `RoundNumberToIncrementAsIfPositive` rounds
every value as a positive one, as an instant rounds. `ToDouble` is the Number nearest the ratio, ties
to even, over the whole range: subnormals included, +0 for a zero numerator, and an infinity at or
past the largest double's rounding range (Temporal's `DivideTimeDuration` is this conversion).

---

## `BigIntMath`, `BigIntText` and `BigIntPolicy` — ECMA-262 §6.1.6.2, §7.1.14, §21.2.2.1, §21.2.2.2

### Signature

```csharp
public readonly record struct BigIntPolicy(long maximumBits)   // 1 to RepresentationLimit
{
    public const long RepresentationLimit = (1L << 31) - 128;
    public static BigIntPolicy Unbounded { get; }
    public long MaximumBits { get; }
    public bool Admits(BigInteger value);
}

// Each binary operation, and Negate and BitwiseNot with one operand:
public static ResultStatus Add(BigInteger left, BigInteger right, BigIntPolicy policy,
    WorkCharge? charge, out BigInteger result);
//   Subtract, Multiply, Divide, Remainder, Power, BitwiseAnd, BitwiseOr, BitwiseXor,
//   LeftShift, SignedRightShift; Negate, BitwiseNot
public static ResultStatus AsIntN(ulong bits, BigInteger value, BigIntPolicy policy, WorkCharge? charge, out BigInteger result);
public static ResultStatus AsUintN(ulong bits, BigInteger value, BigIntPolicy policy, WorkCharge? charge, out BigInteger result);
public static BigInteger FromIntegralNumber(double number);
public static double ToNumber(BigInteger value, WorkCharge? charge);
public static int CompareToNumber(BigInteger value, double number, WorkCharge? charge);
public static int Words(BigInteger value);   public static int WordsOf(long bits);
public static ulong LinearCost(int words);   public static ulong ProductCost(int leftWords, int rightWords);
public static ulong LeafCost(int words);

public static string BigIntText.ToString(BigInteger value, int radix, WorkCharge? charge);
public static ResultStatus BigIntText.StringToBigInt(ReadOnlySpan<char> text, BigIntPolicy policy,
    WorkCharge? charge, out BigInteger value, out int tooWideSign);
```

### What the caller must have done first

| Operation | What the adapter does, in this order |
|---|---|
| Every binary operator | `ToPrimitive` and `ToNumeric` of both operands; a BigInt mixed with a Number throws the TypeError (§13.15.3 `ApplyStringOrNumericBinaryOperator` step 6); `>>>` on BigInts throws the TypeError (`BigInt::unsignedRightShift`) |
| `Divide`, `Remainder` | A zero divisor throws the RangeError |
| `Power` | A negative exponent throws the RangeError |
| `AsIntN`, `AsUintN` | `ToIndex(bits)`, then `ToBigInt(bigint)`, in that order (§21.2.2.1 steps 1–2) |
| `FromIntegralNumber` | `NumberToBigInt`'s check: a Number that is not an integral Number throws the RangeError |
| `ToNumber` | `Number(value)` of a BigInt; nothing first |
| `CompareToNumber` | NaN and the infinities are answered by the engine (`IsLessThan` steps 3–4, `IsLooselyEqual`) before the call |
| `BigIntText.ToString` | `thisBigIntValue`; an undefined radix is 10; `ToIntegerOrInfinity(radix)`; a radix outside 2 to 36 throws the RangeError |
| `StringToBigInt` | `ToString` of the argument where the caller has a value, not a String |

### Preconditions

Thrown as `ArgumentOutOfRangeException` when they do not hold: a zero divisor, a negative exponent,
a non-integral or non-finite Number for `FromIntegralNumber`, a non-finite Number for
`CompareToNumber`, a radix outside 2 to 36, and a policy width outside 1 to `RepresentationLimit`.
**Never pass `default(BigIntPolicy)`**: its width is 0, which admits only `0n` and `-1n`.

### Work and limits

- **Meter:** every operation charges **before** it computes, in Broiler.VM's `JsBigInt` units
  (JSD-0067 decision 3): a value's size is `Words(x)`, its width in 64-bit words plus one; a pass over
  it is `LinearCost`, a multiplication or division `ProductCost`, a text leaf `LeafCost`. An
  operation refused part-way (`Power`, a long parse) has charged the work it did before the refusal.
  `null` charges nothing.
- **Limits:** the policy bounds every result's width, `BigInteger.GetBitLength` (the two's-complement
  length without the sign bit: under a policy of 2^20 bits, `-(2n ** 1048576n)` is admitted and
  `2n ** 1048576n` is not). An operation whose operands already prove the result too wide refuses
  before it allocates. No policy builds a value wider than `RepresentationLimit` bits, 2^31 - 128:
  a .NET `BigInteger` holds at most 2^31 - 64 bits and throws `OverflowException` past them, and an
  operation computes at most four bits past its policy before it refuses, so under
  `BigIntPolicy.Unbounded` every over-width result is `LimitExceeded` and never a .NET exception.

### What the caller does with each status

| Status | Meaning | The specification's error | Broiler.JS | Broiler.VM |
|---|---|---|---|---|
| `Completed` | `result` (or `value`) holds the BigInt | — | Its BigInt value | A `JsBigInt` of it |
| `Malformed` | `StringToBigInt` only: the text is not a `StringIntegerLiteral` | None: the clause answers undefined; `BigInt(text)` throws SyntaxError, a comparison answers false or undefined | As the clause | As the clause |
| `LimitExceeded` | The result is wider than the policy | None: the engine's resource policy | RangeError at the representation's limit, never a .NET exception (decision SJL-D-15 (a)) | Its RangeError "Maximum BigInt size exceeded" (`JsEngine.BigIntResult`) |

`StringToBigInt`'s `LimitExceeded` is **reported, not refused**: `tooWideSign` holds the value's sign,
because `BigInt(text)` raises, `==` answers false, and a relational comparison is decided by the sign.

### Result

The specification's integer results: division truncates toward zero, a remainder takes the dividend's
sign, a negative left-shift count is a right shift rounding toward -Infinity, and a right shift past
the value's width is `0n` or `-1n` for any count. `ToNumber` is the nearest Number, ties to even, and
an infinity at or past 2^1024 − 2^970. `CompareToNumber` is exact. `BigIntText.ToString` writes lower
case digits, an optional U+002D and no `n`, whatever the process's culture. `StringToBigInt` trims
`StrWhiteSpace`, answers `0n` for an empty text, takes a sign and leading zeros on a decimal integer,
and no sign on a `0x`, `0o` or `0b` integer.

---

## Engine-specific behaviour

Behaviour one engine needs and the other does not is a parameter, never a fork (governance section 6,
step 7).

| Parameter | Broiler.JS passes | Broiler.VM passes | Why they differ |
|---|---|---|---|
| `BigIntPolicy` | `BigIntPolicy.Unbounded` (decision SJL-D-15 (a)) | `new BigIntPolicy(JsBigInt.MaximumBits)`, 2^20 bits | Each engine's resource policy |
| `WorkCharge` (BigInt) | `null`: it has no meter | Its fuel charge, so its fuel-parity twins and exhaustion verdicts hold | Only Broiler.VM meters guest work |
| `Float16NanPolicy` | `PreservePayload`: it does not normalise | `Canonical`: one quiet NaN, sign cleared | Implementation-defined (`NumericToRawBytes` step 1.b); each engine keeps its reading, as its plan commits it (Broiler.JS's `docs/roadmap/SharedLibraries.md`, "Numerics vectors (SJL-4.2)") |

---

*Written 2026-10-13, at milestone SJL-4.3. Revisions of this guide are dated in place, quoting the
text they replace.*
