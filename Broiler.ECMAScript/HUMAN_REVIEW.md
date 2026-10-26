# Human Review: Broiler.ECMAScript

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.JS/Broiler.ECMAScript`, which rewrites this file,
`CODE-ASSURANCE.md`, `assurance.manifest.json` and every generated source header from the
product tree.

> **Status: PENDING.** Human-reviewed: 0 of 1380 relevant units. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript --release`
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
| Files scanned | 106 |
| Code units | 1829 |
| Relevant | 1380 |
| Exempt | 449 |
| Assessed | 1380 of 1380 (100%) |
| Human reviewed | 0 of 1380 (0%) |
| Unverified | 1380 |
| Aliases naming a decision | 0 |

## 4. Review States

One row per state of the machine that reads the two lines. The states are computed from the
annotations and the current fingerprints; nothing stores them.

| State | Units |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 1380 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 449 |

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
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarDate.cs` | 2 | 1 | 1 | 1 | None | None | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarDuration.cs` | 1 | 1 | 0 | 1 | None | None | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarFields.cs` | 7 | 1 | 6 | 1 | None | None | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarFieldsType.cs` | 4 | 1 | 3 | 1 | None | None | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarId.cs` | 17 | 1 | 16 | 1 | None | None | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarOverflow.cs` | 3 | 1 | 2 | 1 | None | None | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarStatus.cs` | 15 | 1 | 14 | 1 | None | None | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarSystem.cs` | 20 | 12 | 8 | 12 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarSystemYear.cs` | 13 | 8 | 5 | 8 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CalendarUnit.cs` | 5 | 1 | 4 | 1 | None | None | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/CopticCalendarSystem.cs` | 14 | 10 | 4 | 10 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/EastAsianCalendarSystem.cs` | 14 | 10 | 4 | 10 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/GregorianCalendarSystem.cs` | 9 | 7 | 2 | 7 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/HebrewCalendarSystem.cs` | 16 | 14 | 2 | 14 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/HijriCalendarSystem.cs` | 12 | 9 | 3 | 9 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/ICalendarArithmetic.cs` | 3 | 3 | 0 | 3 | None | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/IndianCalendarSystem.cs` | 8 | 7 | 1 | 7 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/MonthCode.cs` | 7 | 5 | 2 | 5 | Low | High | 1/1 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/PersianCalendarSystem.cs` | 10 | 8 | 2 | 8 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Calendars/TemporalCalendars.cs` | 74 | 60 | 14 | 60 | Low | High | 1/1 |
| `Broiler.ECMAScript.Algorithms/Temporal/DurationCore.cs` | 8 | 8 | 0 | 8 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Durations/DurationMath.Relative.cs` | 18 | 18 | 0 | 18 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Durations/DurationMath.cs` | 36 | 36 | 0 | 36 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Durations/DurationRecords.cs` | 32 | 11 | 21 | 11 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Durations/ITimeZoneArithmetic.cs` | 4 | 4 | 0 | 4 | None | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Durations/TemporalDifferences.cs` | 22 | 22 | 0 | 22 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/Durations/TemporalTimeZone.cs` | 6 | 5 | 1 | 5 | None | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/IsoArithmetic.cs` | 27 | 27 | 0 | 27 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/TemporalFormat.cs` | 28 | 28 | 0 | 28 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` | 103 | 56 | 47 | 56 | Low | High | 36/36 |
| `Broiler.ECMAScript.Algorithms/Temporal/TemporalLimits.cs` | 18 | 18 | 0 | 18 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/TemporalOptionRules.Durations.cs` | 16 | 16 | 0 | 16 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/TemporalOptionRules.cs` | 27 | 18 | 9 | 18 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Temporal/TemporalRecords.cs` | 26 | 6 | 20 | 6 | Low | None | 0/0 |
| `Broiler.ECMAScript.Algorithms/Text/Base64Codec.cs` | 20 | 13 | 7 | 13 | Low | High | 2/2 |
| `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` | 51 | 19 | 32 | 19 | Low | High | 6/6 |
| `Broiler.ECMAScript.Algorithms/Text/JsonText.cs` | 8 | 7 | 1 | 7 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Text/ReplacementTemplate.cs` | 11 | 4 | 7 | 4 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Text/StringPadding.cs` | 12 | 10 | 2 | 10 | Low | High | 1/1 |
| `Broiler.ECMAScript.Algorithms/Text/UriCodec.cs` | 17 | 14 | 3 | 14 | Low | High | 1/1 |
| `Broiler.ECMAScript.Algorithms/Text/Utf16.cs` | 14 | 14 | 0 | 14 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Time/CalendarYears.cs` | 58 | 39 | 19 | 39 | Medium | High | 1/1 |
| `Broiler.ECMAScript.Algorithms/Time/EcmaDateFormatter.cs` | 13 | 13 | 0 | 13 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Time/EcmaDateParser.cs` | 23 | 18 | 5 | 18 | Low | High | 2/2 |
| `Broiler.ECMAScript.Algorithms/Time/EcmaTime.cs` | 37 | 37 | 0 | 37 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Time/IsoCalendarMath.cs` | 16 | 16 | 0 | 16 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Algorithms/Time/LocalTime.cs` | 15 | 12 | 3 | 12 | Low | High | 1/1 |
| `Broiler.ECMAScript.Algorithms/Time/OffsetTimeZones.cs` | 14 | 10 | 4 | 10 | Low | High | 3/3 |
| `Broiler.ECMAScript.Algorithms/Time/TimeZoneDatabase.cs` | 73 | 50 | 23 | 50 | Low | High | 3/3 |
| `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` | 39 | 30 | 9 | 30 | Low | High | 5/5 |
| `Broiler.ECMAScript.Algorithms/WorkCharge.cs` | 1 | 1 | 0 | 1 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Conformance/Ascii.cs` | 15 | 15 | 0 | 15 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/LibraryRun.cs` | 11 | 9 | 2 | 9 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/Outcome.cs` | 11 | 7 | 4 | 7 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/TypedValue.cs` | 24 | 11 | 13 | 11 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorCorpus.cs` | 16 | 11 | 5 | 11 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorFile.cs` | 23 | 18 | 5 | 18 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorRun.cs` | 17 | 12 | 5 | 12 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Conformance/VectorValidator.cs` | 24 | 20 | 4 | 20 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.Collation.cs` | 39 | 35 | 4 | 35 | Low | High | 3/3 |
| `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.Dates.cs` | 13 | 13 | 0 | 13 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.cs` | 39 | 39 | 0 | 39 | Low | High | 3/3 |
| `Broiler.ECMAScript.Generation/Intl/CldrUcd.cs` | 29 | 24 | 5 | 24 | Low | High | 2/2 |
| `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` | 17 | 13 | 4 | 13 | Low | High | 7/7 |
| `Broiler.ECMAScript.Generation/Intl/IntlDataVersions.cs` | 5 | 5 | 0 | 5 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Generation/Intl/IntlGeneratorOptions.cs` | 11 | 5 | 6 | 5 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Generation/Intl/IntlLocaleList.cs` | 9 | 5 | 4 | 5 | Low | High | 1/1 |
| `Broiler.ECMAScript.Generation/Intl/IntlTableFiles.cs` | 4 | 4 | 0 | 4 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Generation/Intl/IntlTableSource.cs` | 2 | 2 | 0 | 2 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Generation/Intl/IntlTables.cs` | 9 | 3 | 6 | 3 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Generation/Intl/IntlUcdPin.cs` | 10 | 7 | 3 | 7 | Low | High | 4/4 |
| `Broiler.ECMAScript.Generation/Time/ArchiveReading.cs` | 9 | 9 | 0 | 9 | Low | High | 2/2 |
| `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` | 16 | 12 | 4 | 12 | Low | High | 6/6 |
| `Broiler.ECMAScript.Generation/Time/CalendarTableGenerator.cs` | 32 | 29 | 3 | 29 | Low | High | 2/2 |
| `Broiler.ECMAScript.Generation/Time/CalendarTableSource.cs` | 2 | 2 | 0 | 2 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Generation/Time/CldrPin.cs` | 12 | 8 | 4 | 8 | Low | High | 4/4 |
| `Broiler.ECMAScript.Generation/Time/TableSourceOptions.cs` | 9 | 2 | 7 | 2 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Generation/Time/TableSourceWriter.cs` | 8 | 8 | 0 | 8 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Generation/Time/TimeDataVersions.cs` | 5 | 5 | 0 | 5 | Low | Low | 0/0 |
| `Broiler.ECMAScript.Generation/Time/TzdbCompiler.cs` | 26 | 25 | 1 | 25 | Low | Medium | 0/0 |
| `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` | 15 | 12 | 3 | 12 | Low | High | 6/6 |
| `Broiler.ECMAScript.Generation/Time/TzdbTableGenerator.cs` | 19 | 14 | 5 | 14 | Low | High | 2/2 |
| `Broiler.ECMAScript.Generation/Time/TzdbTableSource.cs` | 2 | 2 | 0 | 2 | Low | Low | 0/0 |

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
- `Broiler.ECMAScript.Temporal.MonthCode.Parse(ReadOnlySpan<char>, out MonthCode)` in `Broiler.ECMAScript.Algorithms/Temporal/Calendars/MonthCode.cs` - Security=High, Spec=Temporal s12.2.1 via Broiler.VM JsTemporal.MonthCodeOf, `6D1E6C`, PENDING
  - Falsified if: a string other than M, two ASCII digits and an optional L, or the string M00, parses as a month code
- `Broiler.ECMAScript.Temporal.TemporalCalendars.Canonicalize(ReadOnlySpan<char>, out CalendarId)` in `Broiler.ECMAScript.Algorithms/Temporal/Calendars/TemporalCalendars.cs` - Security=High, Spec=Temporal s12.1.1, Intl era and month code proposal 1.1.1 via Broiler.VM JsTemporal.CanonicalizeCalendar, `E8A556`, PENDING
  - Falsified if: an identifier that differs from a Table 1 type or alias in anything but the case of ASCII letters canonicalises to a calendar
- `Broiler.ECMAScript.Temporal.TemporalGrammar` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ISO 8601 grammar via Broiler.VM JsTemporalParser, `4DD523`, PENDING
  - Falsified if: a string Broiler.VM's JsTemporalParser refuses is accepted for a goal, a string it accepts is refused, or an accepted string answers a field other than Broiler.VM's
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParse(ReadOnlySpan<char>, ReadOnlySpan<TemporalGoal>, out IsoParseResult?, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ParseISODateTime via Broiler.VM JsTemporalParser.Parse, `2B62FC`, PENDING
  - Falsified if: the meter is charged other than the text's length before the first character is read, a later goal is tried after an earlier one matched, or a calendar other than the first u-ca annotation's is answered
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseInstant(ReadOnlySpan<char>, out BigInteger, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ParseTemporalInstant via Broiler.VM JsTemporal.ToInstant, `3765EF`, PENDING
  - Falsified if: an instant string within the range is refused, or one outside it, or more than 10^8 days from the epoch after its offset, is accepted
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseTimeZoneString(ReadOnlySpan<char>, out TimeZoneIdentifierRecord, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ParseTemporalTimeZoneString via Broiler.VM JsTemporal.ToTimeZoneIdentifier, `E00095`, PENDING
  - Falsified if: a string that is neither a time zone identifier nor an ISO string of one of the six goals names a zone, or the zone answered is not the annotation's, then UTC for Z, then the offset's
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseTimeZoneIdentifier(ReadOnlySpan<char>, out TimeZoneIdentifierRecord, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ParseTimeZoneIdentifier via Broiler.VM JsTemporal.TimeZoneFromIdentifier, `232199`, PENDING
  - Falsified if: a string the grammar's TimeZoneIdentifier does not match whole is accepted, or an offset identifier answers a name
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseCalendarString(ReadOnlySpan<char>, out string?, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ParseTemporalCalendarString via Broiler.VM JsTemporal.ParseCalendarString, `41C0C1`, PENDING
  - Falsified if: a string that neither ParseISODateTime over the six goals nor AnnotationValue accepts is accepted, or the calendar answered is not the first u-ca annotation's
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseOffsetString(ReadOnlySpan<char>, out long)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ToOffsetString and ParseDateTimeUTCOffset via Broiler.VM JsTemporal.OffsetString and JsTemporalParser.OffsetNanoseconds, `880839`, PENDING
  - Falsified if: a string other than a whole UTCOffset with sub-minute precision is accepted, or an accepted offset answers other than its exact nanoseconds
- `Broiler.ECMAScript.Temporal.TemporalGrammar.TryParseDuration(ReadOnlySpan<char>, out DurationFields, out TemporalParseError, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ParseTemporalDurationString via Broiler.VM JsTemporalParser.Duration, `38C6AC`, PENDING
  - Falsified if: a string the TemporalDurationString grammar refuses is accepted (a time designator with no part, a fraction not on the last time part, parts out of order), or a part is not its exact value rounded once
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsTimeZoneIdentifier(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal TimeZoneIdentifier via Broiler.VM JsTemporalParser.IsTimeZoneIdentifier, `FADD2D`, PENDING
  - Falsified if: a string other than a whole UTCOffset without seconds or a TimeZoneIANAName is accepted
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsUtcOffset(ReadOnlySpan<char>, bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal UTCOffset via Broiler.VM JsTemporalParser.IsUtcOffset, `1F7E11`, PENDING
  - Falsified if: a string other than a whole UTCOffset of the precision is accepted, including Z, U+2212 or a digit other than ASCII
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsAnnotationValue(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal AnnotationValue via Broiler.VM JsTemporalParser.IsAnnotationValue, `E3B3B1`, PENDING
  - Falsified if: an empty string, an empty component or a character other than an ASCII letter, digit or separating hyphen is accepted
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parse(ReadOnlySpan<char>, ReadOnlySpan<TemporalGoal>, out IsoParseResult?)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ParseISODateTime via Broiler.VM JsTemporalParser.Parse, `DB3BE7`, PENDING
  - Falsified if: the annotation checks are applied to a goal other than the first matched, or the record's fields are not the matched parse nodes'
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsWholeTimeZoneIdentifier(ReadOnlySpan<char>)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal TimeZoneIdentifier via Broiler.VM JsTemporalParser.IsTimeZoneIdentifier, `2684CF`, PENDING
  - Falsified if: a string other than a whole UTCOffset without seconds or a TimeZoneIANAName is accepted
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsAnnotationValueCore(ReadOnlySpan<char>)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal AnnotationValue via Broiler.VM JsTemporalParser.IsAnnotationValue, `B98621`, PENDING
  - Falsified if: an empty string, an empty component or a character other than an ASCII letter, digit or separating hyphen is accepted
- `Broiler.ECMAScript.Temporal.TemporalGrammar.IsAnnotationKey(ReadOnlySpan<char>)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal AnnotationKey via Broiler.VM JsTemporalParser.IsAnnotationKey, `4375B0`, PENDING
  - Falsified if: a key that is empty, starts other than with a lower-case ASCII letter or an underscore, or holds a character outside the key's set is accepted
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Duration(ReadOnlySpan<char>, out DurationFields)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ParseTemporalDurationString via Broiler.VM JsTemporalParser.Duration, `A6B684`, PENDING
  - Falsified if: a string the TemporalDurationString grammar refuses is accepted, or a part answers other than its exact value rounded once to a Number
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal ISO 8601 grammar productions via Broiler.VM JsTemporalParser, `E21966`, PENDING
  - Falsified if: a production matches text its grammar rule does not derive, fails on text it derives where Broiler.VM's matches, or leaves the position other than after its match or at its start
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.AnnotatedDateTime(bool, bool, bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal AnnotatedDateTime via Broiler.VM JsTemporalParser.AnnotatedDateTime, `30151F`, PENDING
  - Falsified if: a date-time without its required time, offset or zone annotation matches, Z matches where the goal has no Z, or text remains after the match
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.AnnotatedTime()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal AnnotatedTime and its early errors via Broiler.VM JsTemporalParser.AnnotatedTime, `8D86A1`, PENDING
  - Falsified if: a designator-less time that is also a whole DateSpecMonthDay or DateSpecYearMonth matches, or Z matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.AnnotatedYearMonth()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal AnnotatedYearMonth via Broiler.VM JsTemporalParser.AnnotatedYearMonth, `45E67D`, PENDING
  - Falsified if: a year-month followed by anything but annotations matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.AnnotatedMonthDay()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal AnnotatedMonthDay via Broiler.VM JsTemporalParser.AnnotatedMonthDay, `0C76E0`, PENDING
  - Falsified if: a month-day followed by anything but annotations matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateYear()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal DateYear via Broiler.VM JsTemporalParser.DateYear, `4FFA3B`, PENDING
  - Falsified if: a year other than four ASCII digits or an ASCII sign and six is matched, or -000000 is
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateMonth()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal DateMonth via Broiler.VM JsTemporalParser.DateMonth, `60B3B8`, PENDING
  - Falsified if: a month other than two ASCII digits from 01 to 12 is matched
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateDay()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal DateDay via Broiler.VM JsTemporalParser.DateDay, `21B999`, PENDING
  - Falsified if: a day other than two ASCII digits from 01 to 31 is matched
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateSpec()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal DateSpec and IsValidDate via Broiler.VM JsTemporalParser.DateSpec, `3F3E7B`, PENDING
  - Falsified if: a date mixing the extended and basic separators, or a date that is not a valid ISO date, matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateSpecYearMonth()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal DateSpecYearMonth via Broiler.VM JsTemporalParser.DateSpecYearMonth, `313B7C`, PENDING
  - Falsified if: a year-month other than a DateYear, an optional hyphen and a DateMonth matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateSpecMonthDay()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal DateSpecMonthDay and IsValidMonthDay via Broiler.VM JsTemporalParser.DateSpecMonthDay, `14CC14`, PENDING
  - Falsified if: a month-day that is not a day of 1972 matches, or one with a single leading hyphen
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.Fraction(bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal TemporalDecimalFraction via Broiler.VM JsTemporalParser.Fraction, `B60AF2`, PENDING
  - Falsified if: a fraction of no digit or of ten or more digits matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.TimeSpec()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal TimeSpec via Broiler.VM JsTemporalParser.TimeSpec, `BC60CA`, PENDING
  - Falsified if: a time mixing the extended and basic separators matches beyond its hour and minute, an hour above 23, a minute above 59 or a second above 60 matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.UtcOffset(bool, out bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal UTCOffset via Broiler.VM JsTemporalParser.UtcOffset, `A4AF4D`, PENDING
  - Falsified if: an offset with a sign other than ASCII + or -, an hour above 23, a minute or second above 59, or seconds without sub-minute precision matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.TimeZoneIanaName()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal TimeZoneIANAName via Broiler.VM JsTemporalParser.TimeZoneIanaName, `B4B31D`, PENDING
  - Falsified if: a name with an empty component, a component starting with a digit, a hyphen or a plus sign, or a character outside ASCII letters, digits and . _ - + / matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.TimeZoneIdentifier()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal TimeZoneIdentifier via Broiler.VM JsTemporalParser.TimeZoneIdentifier, `4F1405`, PENDING
  - Falsified if: an identifier other than a UTCOffset without seconds or a TimeZoneIANAName matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.Annotations()` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal TimeZoneAnnotation and Annotations via Broiler.VM JsTemporalParser.Annotations, `91C026`, PENDING
  - Falsified if: an empty or flag-only annotation, a zone annotation after another annotation or one the TimeZoneIdentifier grammar refuses, or a key or value off the grammar matches
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.DateTimeOffset(bool)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal DateTimeUTCOffset via Broiler.VM JsTemporalParser.DateTimeOffset, `A9D204`, PENDING
  - Falsified if: Z or z matches where the goal has no UTCDesignator
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.Digits(int)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal DecimalDigit via Broiler.VM JsTemporalParser.Digits, `A86237`, PENDING
  - Falsified if: a character other than an ASCII digit is read as one
- `Broiler.ECMAScript.Temporal.TemporalGrammar.Parser.Bounded(int)` in `Broiler.ECMAScript.Algorithms/Temporal/TemporalGrammar.cs` - Security=High, Spec=Temporal Hour and MinuteSecond via Broiler.VM JsTemporalParser.Bounded, `757D0F`, PENDING
  - Falsified if: two digits above the maximum are read
- `Broiler.ECMAScript.Text.Base64Codec.FromBase64(ReadOnlySpan<char>, Base64Alphabet, LastChunkHandling, Span<byte>, out int, out int, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/Base64Codec.cs` - Security=High, Spec=ECMA-262 Uint8Array base64, `2974B4`, PENDING
  - Falsified if: a decode writes past the destination, reports a read count other than the clauses', or completes or fails where the clauses do not
- `Broiler.ECMAScript.Text.Base64Codec.FromHex(ReadOnlySpan<char>, Span<byte>, out int, out int, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/Base64Codec.cs` - Security=High, Spec=ECMA-262 Uint8Array base64, `C63508`, PENDING
  - Falsified if: a decode writes past the destination, reads beyond what it wrote, or completes over a text the clauses refuse
- `Broiler.ECMAScript.Text.JsonReader` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, Spec=ECMA-262 25.5.1, `3D7898`, PENDING
  - Falsified if: a text outside JSON.parse's grammar is read to its end, or the reader recurses so that a deep text exhausts the stack instead of meeting the limit
- `Broiler.ECMAScript.Text.JsonReader.Read()` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, Spec=ECMA-262 25.5.1, `9C4E6F`, PENDING
  - Falsified if: a text outside the JSON grammar completes, a text inside it fails below the depth limit, a value deeper than the limit is read, or a charge falls other than where Broiler.VM's reader charged it
- `Broiler.ECMAScript.Text.JsonReader.Validate(ReadOnlySpan<char>, DepthLimit, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, Spec=ECMA-262 25.5.1, `FB9241`, PENDING
  - Falsified if: the answer differs from reading the text to its end with a reader
- `Broiler.ECMAScript.Text.JsonReader.ReadValue()` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, Spec=ECMA-262 25.5.1, `06C6F1`, PENDING
  - Falsified if: a value deeper than the limit is read, or the unit a value is charged falls after its depth check
- `Broiler.ECMAScript.Text.JsonReader.ReadNumber()` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, Spec=ECMA-262 25.5.1, `C7413D`, PENDING
  - Falsified if: a number outside the JSON grammar is read, or one inside it is not StringToNumber of its text
- `Broiler.ECMAScript.Text.JsonReader.ReadString(out string)` in `Broiler.ECMAScript.Algorithms/Text/JsonReader.cs` - Security=High, Spec=ECMA-262 25.5.1, `EE036E`, PENDING
  - Falsified if: a raw control character or an escape JSON does not define is admitted, or an escape is not resolved to its code unit
- `Broiler.ECMAScript.Text.StringPadding.Pad(string, long, string, PadPlacement, LengthLimit, out string, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/StringPadding.cs` - Security=High, Spec=ECMA-262 22.1.3.17.2, `0D98D3`, PENDING
  - Falsified if: a String longer than the limit is built or charged, an empty filler is not answered with the String itself, or the filler is not repeated and truncated to exactly maxLength minus the String's length
- `Broiler.ECMAScript.Text.UriCodec.Decode(ReadOnlySpan<char>, ReadOnlySpan<char>, out string, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Text/UriCodec.cs` - Security=High, Spec=ECMA-262 19.2.6.6, `230B5B`, PENDING
  - Falsified if: an overlong sequence, an encoded surrogate or a value above U+10FFFF completes, a preserved character's escape is decoded, or a well-formed sequence is refused
- `Broiler.ECMAScript.Time.CalendarYears.GetYear(ICalendarTables, TableCalendar, long, out CalendarYear)` in `Broiler.ECMAScript.Algorithms/Time/CalendarYears.cs` - Security=High, Spec=JSD-0076, Intl era and month code proposal Table 1 via Broiler.VM JsCalendarSystem.Year, `4EB1E8`, PENDING
  - Falsified if: a year inside a table's range is not the table's, a year outside it is not its source's, or a year of magnitude above the bound completes
- `Broiler.ECMAScript.Time.EcmaDateParser` in `Broiler.ECMAScript.Algorithms/Time/EcmaDateParser.cs` - Security=High, Spec=ECMA-262 21.4.1.32, `0047BF`, PENDING
  - Falsified if: a string outside the format is read by the strict dialect, a string of the format is refused or read at another time value, or Broiler.VM's dialect answers differently from that engine's DateParseText on a string other than a rendering of a negative year
- `Broiler.ECMAScript.Time.EcmaDateParser.Parse(ReadOnlySpan<char>, DateParseDialect, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Time/EcmaDateParser.cs` - Security=High, Spec=ECMA-262 21.4.3.2, `5C16E2`, PENDING
  - Falsified if: the meter is charged other than the text's length before the first character is read, or charged for an empty text
- `Broiler.ECMAScript.Time.EcmaLocalTime.Utc(double, ILocalTimeProvider)` in `Broiler.ECMAScript.Algorithms/Time/LocalTime.cs` - Security=High, Spec=ECMA-262 21.4.1.26, `708CAB`, PENDING
  - Falsified if: a repeated local time is read at a later instant than its earliest, a skipped local time is read at an offset other than the one before its transition, or a local time with one instant is read at another
- `Broiler.ECMAScript.Time.OffsetTimeZones.IsTimeZoneOffsetString(ReadOnlySpan<char>)` in `Broiler.ECMAScript.Algorithms/Time/OffsetTimeZones.cs` - Security=High, Spec=JSD-0076, ECMA-262 21.4.1.33.1 via Broiler.VM JsRealm.DateTimeFormat.ParseOffset, `5FE091`, PENDING
  - Falsified if: a string outside the UTCOffset grammar, one with a digit outside 0 to 9 among them, is accepted, or one of the grammar is refused
- `Broiler.ECMAScript.Time.OffsetTimeZones.TryParseOffsetIdentifier(ReadOnlySpan<char>, out int)` in `Broiler.ECMAScript.Algorithms/Time/OffsetTimeZones.cs` - Security=High, Spec=JSD-0076, Temporal ParseTimeZoneIdentifier via Broiler.VM JsTemporal.ParseZone, `203719`, PENDING
  - Falsified if: an identifier with seconds, with hours above 23 or minutes above 59, with a digit outside 0 to 9, or mixing the extended and basic forms is accepted, or the minutes of an accepted one are not its sign times its hours and minutes
- `Broiler.ECMAScript.Time.OffsetTimeZones.Parse(ReadOnlySpan<char>, out int, out bool)` in `Broiler.ECMAScript.Algorithms/Time/OffsetTimeZones.cs` - Security=High, Spec=JSD-0076, ECMA-262 21.4.1.33 via Broiler.VM JsRealm.DateTimeFormat.ParseOffset, `5DD51E`, PENDING
  - Falsified if: a string outside the UTCOffset grammar is read as an offset, one of it is refused, or the seconds flag or the minutes read differ from the string's
- `Broiler.ECMAScript.Time.TimeZoneDatabase.TryFind(ReadOnlySpan<char>, out TimeZoneId)` in `Broiler.ECMAScript.Algorithms/Time/TimeZoneDatabase.cs` - Security=High, Spec=JSD-0076, ECMA-402 6.5.2 via Broiler.VM JsTimeZones.Find, `DDC576`, PENDING
  - Falsified if: a name holding a character above U+007F is found, a name differing from an identifier in anything but ASCII case is found, or an identifier in any ASCII case is not found
- `Broiler.ECMAScript.Time.TableZoneRules.NextTransition(long, long, bool)` in `Broiler.ECMAScript.Algorithms/Time/TimeZoneDatabase.cs` - Security=High, Spec=JSD-0076, Temporal GetNamedTimeZoneNextTransition via Broiler.VM JsZone.NextTransition, `FC4F84`, PENDING
  - Falsified if: a transition at or before the instant is answered, one after the limit is answered, a later transition is answered where an earlier one changes the offset, or none is answered after 2037 where the recurring rules change the offset
- `Broiler.ECMAScript.Time.TableZoneRules.PreviousTransition(long, bool)` in `Broiler.ECMAScript.Algorithms/Time/TimeZoneDatabase.cs` - Security=High, Spec=JSD-0076, Temporal GetNamedTimeZonePreviousTransition via Broiler.VM JsZone.PreviousTransition, `30B7EE`, PENDING
  - Falsified if: a transition at or after the instant is answered, an earlier transition is answered where a later one before the instant changes the offset, or none is answered after 2037 where the recurring rules change the offset
- `Broiler.ECMAScript.Time.ZonedTime.GetPossibleEpochNanoseconds(ITimeZoneRules, IsoDateTime, Span<BigInteger>, out int)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, Spec=JSD-0076, Temporal GetPossibleEpochNanoseconds and ECMA-262 21.4.1.20 via Broiler.VM JsTemporal.PossibleEpochNs, `57989D`, PENDING
  - Falsified if: an instant the wall time names in the zone is left out, an instant it does not name is written, the instants are not ascending, or an instant outside the range is written with Completed
- `Broiler.ECMAScript.Time.ZonedTime.DisambiguatePossibleEpochNanoseconds(ITimeZoneRules, ReadOnlySpan<BigInteger>, IsoDateTime, Disambiguation, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, Spec=JSD-0076, Temporal DisambiguatePossibleEpochNanoseconds via Broiler.VM JsTemporal.Disambiguate, `DD12D0`, PENDING
  - Falsified if: a repeated time is not its earlier instant under compatible or earlier or its later under later, a skipped time is not moved by the gap in the mode's direction, or reject answers an instant for a time that is repeated or skipped
- `Broiler.ECMAScript.Time.ZonedTime.GetStartOfDay(ITimeZoneRules, IsoDate, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, Spec=JSD-0076, Temporal GetStartOfDay via Broiler.VM JsTemporal.StartOfDay, `515EE5`, PENDING
  - Falsified if: a day whose midnight exists starts at another instant, a day whose midnight is skipped starts other than at the transition that skips it, or a day with no instant answers one
- `Broiler.ECMAScript.Time.ZonedTime.Possible(ITimeZoneRules, IsoDateTime, Span<BigInteger>, out int)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, Spec=JSD-0076, ECMA-262 21.4.1.20 via Broiler.VM JsTemporal.PossibleEpochNs, `F0F2D2`, PENDING
  - Falsified if: a candidate is kept whose instant does not have the offset it was made from, an offset in force a day before or after the wall time is not sampled, or a candidate outside the range completes
- `Broiler.ECMAScript.Time.ZonedTime.Disambiguate(ITimeZoneRules, ReadOnlySpan<BigInteger>, IsoDateTime, Disambiguation, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Time/ZonedTime.cs` - Security=High, Spec=JSD-0076, Temporal DisambiguatePossibleEpochNanoseconds via Broiler.VM JsTemporal.Disambiguate, `0AFBEC`, PENDING
  - Falsified if: the gap's two offsets are not read a day before and a day after the wall time, or the moved time's first instant is not answered under earlier and its last otherwise
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.RootCollation.Read(string)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.Collation.cs` - Security=High, Spec=JSD-0082 D11 root collation via Broiler.VM CldrTableGenerator, `33AB30`, PENDING
  - Falsified if: a line that maps nothing, repeats a key or has a weight wider than its encoding is read without an exception
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.RootCollation.Elements(string)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.Collation.cs` - Security=High, Spec=JSD-0082 D11 root collation via Broiler.VM CldrTableGenerator, `D4DC2B`, PENDING
  - Falsified if: an element with a lower-case or missing hexadecimal digit, or another marker than . or *, is read, or one the expression matches is not
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.Tailoring.Apply(RootCollation, CldrUcd, string, IReadOnlyDictionary<string, string>)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.Collation.cs` - Security=High, Spec=JSD-0082 D11 tailorings via Broiler.VM CldrTableGenerator, `1FC4E4`, PENDING
  - Falsified if: rule syntax outside the supported subset is accepted, or a tailored weight that runs into the next root weight is written
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.Generate(IntlCldrPin, IReadOnlyDictionary<string, byte[]>, CldrUcd, IntlGeneratorOptions)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.cs` - Security=High, Spec=JSD-0082 D11 CLDR tables via Broiler.VM CldrTableGenerator, `E3BEF1`, PENDING
  - Falsified if: tables are written from an archive the pin does not describe, or with Broiler.VM's options they differ from Broiler.VM's generator's
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.Archive.Bytes(string)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.cs` - Security=High, Spec=JSD-0082 D11 CLDR tables, `ADFB0A`, PENDING
  - Falsified if: a file the pin does not name is read, or its absence is answered with empty data
- `Broiler.ECMAScript.Generation.Intl.CldrTableGenerator.Text(string, string, IEnumerable<string>)` in `Broiler.ECMAScript.Generation/Intl/CldrTableGenerator.cs` - Security=High, Spec=JSD-0082 D11 CLDR tables via Broiler.VM CldrTableGenerator, `1713B4`, PENDING
  - Falsified if: a line holding a quote, a control character or a character above U+007E, or a table with no line, is written
- `Broiler.ECMAScript.Generation.Intl.CldrUcd.Read(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/CldrUcd.cs` - Security=High, Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase, `E61D3D`, PENDING
  - Falsified if: a UnicodeData.txt record with a field missing, an unknown category, or a decomposition on a range, or a Unified_Ideograph code point that is not an assigned letter, is read without an exception
- `Broiler.ECMAScript.Generation.Intl.CldrUcd.UnicodeDataEntries(string)` in `Broiler.ECMAScript.Generation/Intl/CldrUcd.cs` - Security=High, Spec=JSD-0082 D12 UCD subset via Broiler.VM UnicodeDatabase, `AA1255`, PENDING
  - Falsified if: a line of a field count other than fifteen, or a Last record no First record opened, is read without an exception
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, Spec=JSD-0082 D10 D12 CLDR pin via Broiler.VM CldrPin, `574FE4`, PENDING
  - Falsified if: a file whose length, SHA-256 or stated version differs from the pin, or a pin of another CLDR or UCA version, is returned as the pinned one without an exception
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin.Parse(string)` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, Spec=JSD-0082 D10 CLDR pin via Broiler.VM CldrPin, `871D66`, PENDING
  - Falsified if: a malformed file line or a repeated key is accepted
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, Spec=JSD-0082 D10 D12 CLDR pin via Broiler.VM CldrPin, `874F2C`, PENDING
  - Falsified if: a file whose length, hash or stated version differs from the pin, or a pin of another version or UCA version, yields no violation
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, Spec=JSD-0082 D10 CLDR pin via Broiler.VM CldrPin, `22A1C6`, PENDING
  - Falsified if: files with a violation are returned instead of refused
- `Broiler.ECMAScript.Generation.Intl.IntlCldrPin.VersionViolation(string, byte[])` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, Spec=JSD-0082 D10 D12 CLDR pin via Broiler.VM CldrPin, `13894D`, PENDING
  - Falsified if: a package, supplemental or UCA file of another release is answered null
- `Broiler.ECMAScript.Generation.Intl.IntlPinText` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, Spec=JSD-0082 D10 D12 pin grammar via Broiler.VM CldrPin, `6A20EE`, PENDING
  - Falsified if: a file line that is not a path, a decimal length and a digest, or a single-valued key that repeats or has no value, is parsed without an exception
- `Broiler.ECMAScript.Generation.Intl.IntlPinText.Parse(string, string)` in `Broiler.ECMAScript.Generation/Intl/IntlCldrPin.cs` - Security=High, Spec=JSD-0082 D10 D12 pin grammar via Broiler.VM CldrPin, `333008`, PENDING
  - Falsified if: a malformed file line or a repeated key is accepted
- `Broiler.ECMAScript.Generation.Intl.IntlLocaleList.Parse(string)` in `Broiler.ECMAScript.Generation/Intl/IntlLocaleList.cs` - Security=High, Spec=JSD-0082 D5 D11 locale list, `6C3047`, PENDING
  - Falsified if: a list out of ordinal order, with a repeated tag, a malformed tag or line, or no locale, is accepted
- `Broiler.ECMAScript.Generation.Intl.IntlUcdPin` in `Broiler.ECMAScript.Generation/Intl/IntlUcdPin.cs` - Security=High, Spec=JSD-0082 D12 UCD pin, `AEF610`, PENDING
  - Falsified if: a file whose length or SHA-256 differs from the pin, a PropList.txt of another version, or a pin of another Unicode version, is returned as the pinned one without an exception
- `Broiler.ECMAScript.Generation.Intl.IntlUcdPin.Parse(string)` in `Broiler.ECMAScript.Generation/Intl/IntlUcdPin.cs` - Security=High, Spec=JSD-0082 D12 UCD pin, `07D170`, PENDING
  - Falsified if: a malformed file line or a repeated key is accepted
- `Broiler.ECMAScript.Generation.Intl.IntlUcdPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/IntlUcdPin.cs` - Security=High, Spec=JSD-0082 D12 UCD pin, `DA6542`, PENDING
  - Falsified if: a file whose length or hash differs from the pin, a pin of another version, or a pin that does not name a file the generator reads, yields no violation
- `Broiler.ECMAScript.Generation.Intl.IntlUcdPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Intl/IntlUcdPin.cs` - Security=High, Spec=JSD-0082 D12 UCD pin, `67FB7E`, PENDING
  - Falsified if: files with a violation are returned instead of refused
- `Broiler.ECMAScript.Generation.Time.ArchiveReading` in `Broiler.ECMAScript.Generation/Time/ArchiveReading.cs` - Security=High, Spec=JSD-0053 tz pin via Broiler.VM TzdbPin, `CD64C8`, PENDING
  - Falsified if: a member of a tarball is answered with bytes other than the ones its tar entry holds, or a link, a directory or an unnamed member is answered as a file
- `Broiler.ECMAScript.Generation.Time.ArchiveReading.RegularFiles(byte[], IReadOnlySet<string>)` in `Broiler.ECMAScript.Generation/Time/ArchiveReading.cs` - Security=High, Spec=JSD-0053 tz pin via Broiler.VM TzdbPin, `78B9DB`, PENDING
  - Falsified if: an entry that is not a regular file, or whose name is not wanted, is answered, or a wanted file's bytes differ from its tar entry's
- `Broiler.ECMAScript.Generation.Time.CalendarPin` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin, `BFC182`, PENDING
  - Falsified if: a crate or a member whose length, digests or stated name and version differ from the pin is extracted, or returned as the pinned one, without an exception
- `Broiler.ECMAScript.Generation.Time.CalendarPin.Parse(string)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin, `084A93`, PENDING
  - Falsified if: a malformed archive or member line, or a repeated key, is accepted
- `Broiler.ECMAScript.Generation.Time.CalendarPin.Extract(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin, `87E5C0`, PENDING
  - Falsified if: a crate whose length or digests differ from the pin is decompressed, or a member is answered without its own length and hash compared
- `Broiler.ECMAScript.Generation.Time.CalendarPin.ArchiveViolations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin, `5AF285`, PENDING
  - Falsified if: a crate one byte or one length away from the pin's, or a crate of another version, yields no violation
- `Broiler.ECMAScript.Generation.Time.CalendarPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin, `1EB55D`, PENDING
  - Falsified if: a member whose length or hash differs from the pin, or a Cargo.toml stating another name or version, yields no violation
- `Broiler.ECMAScript.Generation.Time.CalendarPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarPin.cs` - Security=High, Spec=JSD-0056 calendar pin via Broiler.VM CalendarPin, `6B0B95`, PENDING
  - Falsified if: members with a violation are returned instead of refused
- `Broiler.ECMAScript.Generation.Time.CalendarTableGenerator` in `Broiler.ECMAScript.Generation/Time/CalendarTableGenerator.cs` - Security=High, Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator, `CDF266`, PENDING
  - Falsified if: the table is written from a crate member that failed its pin's check, or a year that does not follow the one before it, or whose new year falls outside the layout, is written instead of refused
- `Broiler.ECMAScript.Generation.Time.CalendarTableGenerator.Generate(CalendarPin, IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CalendarTableGenerator.cs` - Security=High, Spec=JSD-0056 calendar tables via Broiler.VM CalendarTableGenerator, `5E7CCA`, PENDING
  - Falsified if: a table is answered for a crate member whose length or hash differs from the pin
- `Broiler.ECMAScript.Generation.Time.CldrPin` in `Broiler.ECMAScript.Generation/Time/CldrPin.cs` - Security=High, Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin, `FAF53A`, PENDING
  - Falsified if: a file whose length or SHA-256 differs from the pin, or a pin of another CLDR version, is returned as the pinned one without an exception
- `Broiler.ECMAScript.Generation.Time.CldrPin.Parse(string)` in `Broiler.ECMAScript.Generation/Time/CldrPin.cs` - Security=High, Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin, `4ECE1D`, PENDING
  - Falsified if: a malformed file line or a repeated key is accepted
- `Broiler.ECMAScript.Generation.Time.CldrPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CldrPin.cs` - Security=High, Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin, `558790`, PENDING
  - Falsified if: a file whose length or hash differs from the pin, or a pin of another version, yields no violation
- `Broiler.ECMAScript.Generation.Time.CldrPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/CldrPin.cs` - Security=High, Spec=JSD-0027 s5 CLDR pin via Broiler.VM CldrPin, `22A1C6`, PENDING
  - Falsified if: files with a violation are returned instead of refused
- `Broiler.ECMAScript.Generation.Time.TzdbPin` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, Spec=JSD-0053 tz pin via Broiler.VM TzdbPin, `790869`, PENDING
  - Falsified if: a tarball or a member whose length, SHA-256, SHA-512 or stated version differs from the pin is extracted, or returned as the pinned one, without an exception
- `Broiler.ECMAScript.Generation.Time.TzdbPin.Parse(string)` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, Spec=JSD-0053 tz pin via Broiler.VM TzdbPin, `72E0AE`, PENDING
  - Falsified if: a malformed member line or a repeated key is accepted
- `Broiler.ECMAScript.Generation.Time.TzdbPin.Extract(byte[])` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, Spec=JSD-0053 tz pin via Broiler.VM TzdbPin, `CF0796`, PENDING
  - Falsified if: a tarball whose length or digests differ from the pin is decompressed, or a member is answered without its own length and hash compared
- `Broiler.ECMAScript.Generation.Time.TzdbPin.TarballViolations(byte[])` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, Spec=JSD-0053 tz pin via Broiler.VM TzdbPin, `394CF4`, PENDING
  - Falsified if: a tarball one byte or one length away from the pin's yields no violation
- `Broiler.ECMAScript.Generation.Time.TzdbPin.Violations(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, Spec=JSD-0053 tz pin via Broiler.VM TzdbPin, `4DB748`, PENDING
  - Falsified if: a member whose length, hash or stated version differs from the pin yields no violation
- `Broiler.ECMAScript.Generation.Time.TzdbPin.Verified(IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/TzdbPin.cs` - Security=High, Spec=JSD-0053 tz pin via Broiler.VM TzdbPin, `FE7B8A`, PENDING
  - Falsified if: members with a violation are returned instead of refused
- `Broiler.ECMAScript.Generation.Time.TzdbTableGenerator` in `Broiler.ECMAScript.Generation/Time/TzdbTableGenerator.cs` - Security=High, Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator, `A7C108`, PENDING
  - Falsified if: a table is written from a tzdb member or a CLDR file that failed its pin's check, or two runs over the same inputs write different bytes
- `Broiler.ECMAScript.Generation.Time.TzdbTableGenerator.Generate(TzdbPin, IReadOnlyDictionary<string, byte[]>, CldrPin, IReadOnlyDictionary<string, byte[]>)` in `Broiler.ECMAScript.Generation/Time/TzdbTableGenerator.cs` - Security=High, Spec=JSD-0053 tz tables via Broiler.VM TzdbTableGenerator, `CA2F6C`, PENDING
  - Falsified if: tables are answered for a tzdb member or a CLDR file whose length or hash differs from its pin

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

560 of the 1380 assessed units declare `Origin=AI`. Reading a declaration is the only thing
that makes it read.
