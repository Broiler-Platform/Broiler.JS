# Human Review: Broiler.ECMAScript

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.JS/Broiler.ECMAScript`, which rewrites this file,
`CODE-ASSURANCE.md`, `assurance.manifest.json` and every generated source header from the
product tree.

> **Status: PENDING.** Human-reviewed: 0 of 355 relevant units. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript --release`
> fails while any relevant unit is without a decision bound to its current fingerprint.

## 1. How To Use This File

Read it; do not edit it. A decision about a code unit is the `// Broiler-Human:` line on that
unit's declaration, and every table below is read out of those lines. There is nothing here
to fill in and nothing here to leave blank.

## 2. How A Review Is Recorded

In one place: the `// Broiler-Human:` line of the assurance annotation that sits on the
declaration being read. Nothing in this file is edited by hand, no second document carries a
per-item checklist, and no list of permitted aliases exists to be added to.

```csharp
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=4A3BFD
// Broiler-Falsified-If: a negative value reaches the running total
// Broiler-Human:        PENDING
```

The last line has four shapes. A human writes three of them; the generator writes the fourth
and may never invent an alias, which the check asserts in both directions.

| Line | Meaning |
|---|---|
| `PENDING` | Nobody has recorded a decision for this unit. The generator leaves it exactly as it stands. |
| `<alias>` | A human states their own alias and leaves the machine field to the generator, which fills it with the declaration's fingerprint at the next run. |
| `<alias>; Fingerprint=<six hex>` | A decision bound to one exact version of one declaration. |
| `STALE; Previous=<alias>@<fingerprint>` | Written by the generator when the code moved after a decision. Only a human clears it, by stating their alias again. |

A human may state their own `IP=`, `Security=`, `Resources=`, `CS=` and `CL=` assessment beside their alias,
which is how a reader disagrees with the machine assessment on the line above: an assessment is
a comment and moves no fingerprint, so there is nowhere else to say it.

**No branch, commit or tag is recorded in this file.** Each decision names the fingerprint of
the declaration it was made against, and the state machine compares that value with the
declaration as it now stands. A commit says a tree moved; a fingerprint says whether this unit
did, which is the narrower and the more useful of the two.

## 3. Summary

| Metric | Value |
|---|---:|
| Files scanned | 32 |
| Code units | 439 |
| Relevant | 355 |
| Exempt | 84 |
| Assessed | 355 of 355 (100%) |
| Human reviewed | 0 of 355 (0%) |
| Unverified | 355 |
| Aliases naming a decision | 0 |

## 4. Review States

One row per state of the machine that reads the two lines. The states are computed from the
annotations and the current fingerprints; nothing stores them.

| State | Units |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 355 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 84 |

## 5. Aliases In The Tree

No alias appears on a human line anywhere in the product tree. Nobody has recorded a
decision about any unit of this component.

## 6. Coverage By File

One row per covered file, carrying that file's generated header. `Unverified` counts the
relevant units in a state that blocks a release.

| File | Units | Relevant | Exempt | Unverified | IP risk | Security risk | Criteria |
|---|---:|---:|---:|---:|---|---|---:|
| `Broiler.ECMAScript.Algorithms/Failure.cs` | 6 | 3 | 3 | 3 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Limits.cs` | 8 | 6 | 2 | 6 | Low | High | 2/2 |
| `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` | 28 | 27 | 1 | 27 | Low | High | 17/17 |
| `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` | 6 | 5 | 1 | 5 | Low | High | 6/6 |
| `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` | 17 | 17 | 0 | 17 | Low | High | 9/9 |
| `Broiler.ECMAScript.Algorithms/Numbers/BigNumber.cs` | 22 | 20 | 2 | 20 | Low | High | 1/1 |
| `Broiler.ECMAScript.Algorithms/Numbers/DoubleBits.cs` | 3 | 3 | 0 | 3 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/ExactRounding.cs` | 12 | 12 | 0 | 12 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/Fdlibm.cs` | 26 | 26 | 0 | 26 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/FixedDigits.cs` | 10 | 10 | 0 | 10 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/Float16.cs` | 9 | 7 | 2 | 7 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/IntegerConversions.cs` | 15 | 15 | 0 | 15 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/MathKernels.cs` | 9 | 9 | 0 | 9 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` | 25 | 21 | 4 | 21 | Low | High | 3/3 |
| `Broiler.ECMAScript.Algorithms/Numbers/NumberText.cs` | 12 | 12 | 0 | 12 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/NumericKeys.cs` | 4 | 4 | 0 | 4 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/NumericLiterals.cs` | 19 | 13 | 6 | 13 | Low | High | 2/2 |
| `Broiler.ECMAScript.Algorithms/Numbers/PreciseSum.cs` | 15 | 7 | 8 | 7 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/RadixDigits.cs` | 16 | 16 | 0 | 16 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/RoundingMode.cs` | 16 | 2 | 14 | 2 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/ShortestDecimal.cs` | 13 | 13 | 0 | 13 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Numbers/StrWhiteSpace.cs` | 4 | 4 | 0 | 4 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/ResultStatus.cs` | 4 | 1 | 3 | 1 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/WorkCharge.cs` | 1 | 1 | 0 | 1 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Conformance/Ascii.cs` | 15 | 15 | 0 | 15 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/LibraryRun.cs` | 11 | 9 | 2 | 9 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/Outcome.cs` | 11 | 7 | 4 | 7 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/TypedValue.cs` | 24 | 11 | 13 | 11 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorCorpus.cs` | 16 | 11 | 5 | 11 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorFile.cs` | 23 | 18 | 5 | 18 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorRun.cs` | 17 | 12 | 5 | 12 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorValidator.cs` | 22 | 18 | 4 | 18 | Low | Low | 0/0 |

## 7. Decisions Recorded

No unit in this component carries a decision on its human line. Every one of them reads
`PENDING`.

## 8. Decisions The Code Has Outrun

No unit carries a decision that the code has since moved past.

## 9. Where A Decision Is Required First

The units at the top of the security vocabulary, with the observation that would show each
one wrong and the human line it carries. The set is read from the assessments rather than
written out, so a unit that becomes `High` joins it at the next generation.

- `Broiler.ECMAScript.LengthLimit.Admits(long)` in `Broiler.ECMAScript.Algorithms/Limits.cs` - Security=High, Spec=SJL architecture s4, `CD14A4`, PENDING
  - Falsified if: Admits returns true for a length greater than Maximum, so an operation that consults it produces a result longer than its caller allowed
- `Broiler.ECMAScript.DepthLimit.Admits(int)` in `Broiler.ECMAScript.Algorithms/Limits.cs` - Security=High, Spec=SJL architecture s4, `F3198B`, PENDING
  - Falsified if: Admits returns true for a depth greater than Maximum, so an operation that consults it nests deeper than its caller allowed and can exhaust the stack
- `Broiler.ECMAScript.Numbers.BigIntMath` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt, `CC2B29`, PENDING
  - Falsified if: an operation builds a value wider than the policy before refusing it, or charges other units than Broiler.VM's JsBigInt charges for the same operands
- `Broiler.ECMAScript.Numbers.BigIntMath.Multiply(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2.4 via Broiler.VM JsBigInt, `672BB1`, PENDING
  - Falsified if: a product whose operands already prove it wider than the policy is computed, or a product is charged other than Broiler.VM's JsBigInt charges it
- `Broiler.ECMAScript.Numbers.BigIntMath.Divide(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2.5 via Broiler.VM JsBigInt, `2597AE`, PENDING
  - Falsified if: a quotient is not truncated toward zero, or a zero divisor reaches the division
- `Broiler.ECMAScript.Numbers.BigIntMath.Remainder(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2.6 via Broiler.VM JsBigInt, `6B4557`, PENDING
  - Falsified if: a remainder's sign is not the dividend's, or a zero divisor reaches the division
- `Broiler.ECMAScript.Numbers.BigIntMath.Power(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2.3 via Broiler.VM JsBigInt, `DFE64E`, PENDING
  - Falsified if: a power is computed without charging each multiplication, or a result past the policy is answered, or a huge exponent of a trivial base is refused
- `Broiler.ECMAScript.Numbers.BigIntMath.LeftShift(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2.9 via Broiler.VM JsBigInt, `E44EA4`, PENDING
  - Falsified if: a left shift allocates a result its widths already prove wider than the policy, a right shift rounds toward zero, or an oversized count reaches the base class library
- `Broiler.ECMAScript.Numbers.BigIntMath.SignedRightShift(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2.10 via Broiler.VM JsBigInt, `2A8A8C`, PENDING
  - Falsified if: x >> n differs from the floor of x / 2^n for any count, or a negative count is not a left shift held to the policy
- `Broiler.ECMAScript.Numbers.BigIntMath.AsIntN(ulong, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 21.2.2.1 via Broiler.VM JsBigInt, `67F831`, PENDING
  - Falsified if: a value that fits the signed width is changed, or a wrapped value takes the wrong sign
- `Broiler.ECMAScript.Numbers.BigIntMath.AsUintN(ulong, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 21.2.2.2 via Broiler.VM JsBigInt, `C84AF8`, PENDING
  - Falsified if: a non-negative value that fits the width is changed, a negative value is answered negative, or a modulus past the policy is allocated
- `Broiler.ECMAScript.Numbers.BigIntMath.FromIntegralNumber(double)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 21.2.1.1.1 via Broiler.VM JsBigInt, `F50D3A`, PENDING
  - Falsified if: an integral Number converts to an integer other than its exact value
- `Broiler.ECMAScript.Numbers.BigIntMath.ToNumber(BigInteger, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 21.1.1.1 via Broiler.VM JsBigInt, `7C0D46`, PENDING
  - Falsified if: a BigInt converts to a Number other than the nearest one with ties to even, or a value at or past 2 ** 1024 - 2 ** 970 converts to a finite Number
- `Broiler.ECMAScript.Numbers.BigIntMath.CompareToNumber(BigInteger, double, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 7.2.13 via Broiler.VM JsBigInt, `B22823`, PENDING
  - Falsified if: a BigInt and a Number compare other than their mathematical values do, or either is rounded to compare
- `Broiler.ECMAScript.Numbers.BigIntMath.LinearCost(int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt, `EFBBAF`, PENDING
  - Falsified if: an operation linear in its operands is charged less for wider operands
- `Broiler.ECMAScript.Numbers.BigIntMath.ProductCost(int, int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt, `E26CC9`, PENDING
  - Falsified if: a multiplication or division of wider operands is charged less than one of narrower operands
- `Broiler.ECMAScript.Numbers.BigIntMath.LeafCost(int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt, `4CB044`, PENDING
  - Falsified if: a wider part is converted for a smaller charge
- `Broiler.ECMAScript.Numbers.BigIntMath.Bounded(BigInteger, BigIntPolicy, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2 via Broiler.VM JsBigInt, `1D392B`, PENDING
  - Falsified if: a value wider than the policy is answered as Completed, or one within it is refused
- `Broiler.ECMAScript.Numbers.BigIntMath.Shift(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, Spec=ECMA-262 6.1.6.2.9 via Broiler.VM JsBigInt, `6DB2EF`, PENDING
  - Falsified if: a left shift the widths prove too wide is computed before it is refused, or a right shift past the value's width answers other than 0n or -1n
- `Broiler.ECMAScript.Numbers.BigIntPolicy` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, Spec=ECMA-262 6.1.6.2, `BE9B4E`, PENDING
  - Falsified if: a value exactly MaximumBits wide is refused, or one a bit wider is admitted
- `Broiler.ECMAScript.Numbers.BigIntPolicy.RepresentationLimit` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, Spec=ECMA-262 6.1.6.2, `CDE036`, PENDING
  - Falsified if: an operation under any policy computes a value, final or intermediate, wider than a .NET BigInteger holds, so a .NET OverflowException can reach a script
- `Broiler.ECMAScript.Numbers.BigIntPolicy.BigIntPolicy(long)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, Spec=ECMA-262 6.1.6.2, `998980`, PENDING
  - Falsified if: a width below 1 or above RepresentationLimit makes a policy
- `Broiler.ECMAScript.Numbers.BigIntPolicy.Unbounded` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, Spec=ECMA-262 6.1.6.2, `42A7C2`, PENDING
  - Falsified if: Unbounded is narrower or wider than RepresentationLimit
- `Broiler.ECMAScript.Numbers.BigIntPolicy.MaximumBits` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, Spec=ECMA-262 6.1.6.2, `EE644C`, PENDING
  - Falsified if: MaximumBits is not the width the policy was made with
- `Broiler.ECMAScript.Numbers.BigIntPolicy.Admits(BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, Spec=ECMA-262 6.1.6.2, `5675C8`, PENDING
  - Falsified if: a value wider than MaximumBits is admitted, so an operation under the policy answers a value past the engine's ceiling
- `Broiler.ECMAScript.Numbers.BigIntText` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt, `07A48D`, PENDING
  - Falsified if: a conversion's text depends on the process's culture, or a conversion's work runs before it is charged
- `Broiler.ECMAScript.Numbers.BigIntText.ToString(BigInteger, int, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt, `040A0D`, PENDING
  - Falsified if: a wider BigInt is converted for the same charge as a narrower one, or the digits differ from the integer in that radix
- `Broiler.ECMAScript.Numbers.BigIntText.StringToBigInt(ReadOnlySpan<char>, BigIntPolicy, WorkCharge?, out BigInteger, out int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt, `BA8558`, PENDING
  - Falsified if: a text outside StringIntegerLiteral answers a value, a text inside it answers an integer other than its mathematical value, or a value past the policy is constructed
- `Broiler.ECMAScript.Numbers.BigIntText.ToDecimalString(BigInteger, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt, `DABCC7`, PENDING
  - Falsified if: the text of a value differs from its invariant decimal digits, or a split or a leaf runs before it is charged
- `Broiler.ECMAScript.Numbers.BigIntText.AppendDecimal(StringBuilder, BigInteger, int, int, List<BigInteger>, List<int>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt, `2E5194`, PENDING
  - Falsified if: a digit is dropped, duplicated or unpadded where a split leaves a remainder with leading zeros
- `Broiler.ECMAScript.Numbers.BigIntText.AppendRadix(StringBuilder, BigInteger, int, int, int, ulong, int, List<BigInteger>, List<int>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, Spec=ECMA-262 6.1.6.2.23 via Broiler.VM JsBigInt, `D941DC`, PENDING
  - Falsified if: a digit is dropped, duplicated or unpadded where a split leaves a remainder with leading zeros
- `Broiler.ECMAScript.Numbers.BigIntText.ParseRadix(ReadOnlySpan<char>, int, BigIntPolicy, WorkCharge?, out BigInteger, out int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt, `9CD8E7`, PENDING
  - Falsified if: a digit outside the radix is admitted, or the packed bits spell an integer other than the digits do
- `Broiler.ECMAScript.Numbers.BigIntText.ParseDecimal(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt, `96C969`, PENDING
  - Falsified if: a digit is dropped or misplaced in a split, or a join is not charged before it runs
- `Broiler.ECMAScript.Numbers.BigIntText.ParseDecimalPart(ReadOnlySpan<char>, int, List<BigInteger>, List<int>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, Spec=ECMA-262 7.1.14 via Broiler.VM JsBigInt, `2B0663`, PENDING
  - Falsified if: the high part is scaled by a power other than ten to the low part's length
- `Broiler.ECMAScript.Numbers.BigNumber.ShiftLeft(int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigNumber.cs` - Security=High, Spec=ECMA-262 6.1.6.1.20, `E4CE1E`, PENDING
  - Falsified if: a shift that would leave the storage writes past it instead of throwing
- `Broiler.ECMAScript.Numbers.NumberParsing.StringToNumber(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` - Security=High, Spec=ECMA-262 7.1.4.1.1, `0E0469`, PENDING
  - Falsified if: a string the specification's StringNumericLiteral grammar refuses is given a value other than NaN, or the meter is charged after the input is read
- `Broiler.ECMAScript.Numbers.NumberParsing.ParseInt(ReadOnlySpan<char>, int, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` - Security=High, Spec=ECMA-262 7.1.4.1.1, `61FCDC`, PENDING
  - Falsified if: a run of digits beyond 2^53 is not rounded to the nearest double with ties to even, or a radix outside 2 to 36 other than 0 gives a value
- `Broiler.ECMAScript.Numbers.NumberParsing.Integer(ReadOnlySpan<char>, int)` in `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` - Security=High, Spec=ECMA-262 19.2.5, `D21CA7`, PENDING
  - Falsified if: a run of digits is rounded twice, or not to the nearest double with ties to even, or is accumulated past 2^1100 instead of answering +Infinity
- `Broiler.ECMAScript.Numbers.NumericLiterals.Evaluate(ReadOnlySpan<char>, bool, LengthLimit, out NumericLiteral, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/NumericLiterals.cs` - Security=High, Spec=ECMA-262 12.9.3, `0957E2`, PENDING
  - Falsified if: a text that is not exactly one NumericLiteral completes, or a legacy form in strict mode code completes
- `Broiler.ECMAScript.Numbers.NumericLiterals.BigInt(ReadOnlySpan<char>, int, NumericLiteralForm, LengthLimit, WorkCharge?, out NumericLiteral, out Failure)` in `Broiler.ECMAScript.Algorithms/Numbers/NumericLiterals.cs` - Security=High, Spec=ECMA-262 12.9.3 via Broiler.VM JsParser.TryBigIntValue, `E930F3`, PENDING
  - Falsified if: a BigInt literal wider than the caller's limit is converted before it is refused, or one within the limit is refused

## 10. What This Record Does Not Say

It is not an approval of the component, and a full table above would not be one either. It
records which declarations somebody stated a decision about, and against which version of
each. It does not record what they read, how long they spent, or whether they were right.

A fingerprint is six hex characters of SHA-256 over a declaration's token texts. It answers
whether a unit changed since a decision was recorded against it. It is not a collision-free
identifier across units and it is not a cryptographic commitment, so it detects a change and
does not resist a forger with commit access.

An assessment is a comment, so changing one moves no fingerprint anywhere, and nothing
mechanical checks that it is right; the check holds its values to their vocabularies and no
further.

261 of the 355 assessed units declare `Origin=AI`. Reading a declaration is the only thing
that makes it read.
