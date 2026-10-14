# Broiler.ECMAScript Code Assurance

GENERATED - DO NOT EDIT MANUALLY. Regenerate with
`dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.JS/Broiler.ECMAScript`, which rewrites this file,
`HUMAN_REVIEW.md`, `assurance.manifest.json` and every generated source header from the
product tree.

**No code unit in this component carries a decision on its human line yet.** This report
records that absence precisely. It is not a claim that the code is reviewed, assured or safe,
and the figures below are the measurement of how far from that claim the per-unit record is.

## Summary

| Metric | Value |
|---|---:|
| Files scanned | 32 |
| Files not covered | 0 |
| Files carrying an annotation | 32 |
| Code units | 440 |
| Relevant | 356 |
| Exempt by predicate | 84 |
| Annotated | 356 of 356 (100%) |
| Human reviewed | 0 of 356 (0%) |
| Unverified | 356 |

## Review states

| State | Count |
|---|---:|
| NEW | 0 |
| AI_ASSESSED | 0 |
| HUMAN_PENDING | 356 |
| HUMAN_APPROVED_PENDING_FINGERPRINT | 0 |
| VERIFIED | 0 |
| STALE | 0 |
| EXEMPT | 84 |

## IP risk

| Value | Units |
|---|---:|
| None | 0 |
| Low | 361 |
| Medium | 0 |
| High | 0 |
| Unknown | 0 |
| *not annotated* | 0 |

## Security risk

| Value | Units |
|---|---:|
| None | 0 |
| Low | 148 |
| Medium | 173 |
| High | 40 |
| Critical | 0 |
| *not annotated* | 0 |

## Resource impact

| Metric | Value |
|---|---:|
| Maximum | 9 / 10 |
| Average over annotated units | 1.5 / 10 |
| Units scored | 356 |

## High-security review areas

- `Broiler.ECMAScript.LengthLimit.Admits(long)` in `Broiler.ECMAScript.Algorithms/Limits.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.DepthLimit.Admits(int)` in `Broiler.ECMAScript.Algorithms/Limits.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Multiply(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Divide(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Remainder(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Power(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.LeftShift(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.SignedRightShift(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.AsIntN(ulong, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.AsUintN(ulong, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.FromIntegralNumber(double)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.ToNumber(BigInteger, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.CompareToNumber(BigInteger, double, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.LinearCost(int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.ProductCost(int, int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.LeafCost(int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Bounded(BigInteger, BigIntPolicy, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntMath.Shift(BigInteger, BigInteger, BigIntPolicy, WorkCharge?, out BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntMath.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.RepresentationLimit` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.BigIntPolicy(long)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.Unbounded` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.MaximumBits` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntPolicy.Admits(BigInteger)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntPolicy.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ToString(BigInteger, int, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.StringToBigInt(ReadOnlySpan<char>, BigIntPolicy, WorkCharge?, out BigInteger, out int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ToDecimalString(BigInteger, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.AppendDecimal(StringBuilder, BigInteger, int, int, List<BigInteger>, List<int>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.AppendRadix(StringBuilder, BigInteger, int, int, int, ulong, int, List<BigInteger>, List<int>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ParseRadix(ReadOnlySpan<char>, int, BigIntPolicy, WorkCharge?, out BigInteger, out int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ParseDecimal(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigIntText.ParseDecimalPart(ReadOnlySpan<char>, int, List<BigInteger>, List<int>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/BigIntText.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.BigNumber.ShiftLeft(int)` in `Broiler.ECMAScript.Algorithms/Numbers/BigNumber.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumberParsing.StringToNumber(ReadOnlySpan<char>, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumberParsing.ParseInt(ReadOnlySpan<char>, int, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumberParsing.Integer(ReadOnlySpan<char>, int)` in `Broiler.ECMAScript.Algorithms/Numbers/NumberParsing.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumericLiterals.Evaluate(ReadOnlySpan<char>, bool, LengthLimit, out NumericLiteral, out Failure, WorkCharge?)` in `Broiler.ECMAScript.Algorithms/Numbers/NumericLiterals.cs` - Security=High, human line PENDING
- `Broiler.ECMAScript.Numbers.NumericLiterals.BigInt(ReadOnlySpan<char>, int, NumericLiteralForm, LengthLimit, WorkCharge?, out NumericLiteral, out Failure)` in `Broiler.ECMAScript.Algorithms/Numbers/NumericLiterals.cs` - Security=High, human line PENDING

## Falsification criteria

| Metric | Value |
|---|---:|
| Units carrying a criterion | 40 |
| Units required to carry one | 40 |
| Required and missing | 0 |

A `Broiler-Falsified-If:` line states, at the declaration, the observation that would make
the unit wrong. `Security=High` says a unit is risky, which is a set and not a test; the
criterion is the test. It is required where `Security` is `High` or `Critical` and written
nowhere else: `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript` names every unit that owes one and carries none, and
every unit below `High` that carries one.

The line is a comment, so it is outside every fingerprint by construction: rewording a
criterion moves no recorded value here, in a file header or in
`assurance.manifest.json`, and invalidates nothing. That is the intended reading - a
criterion is an instruction to whoever reads the unit, not part of what a review is bound to.

## Exemption

Exemption is decided by one predicate in `CSharpAssuranceScanner`, not per unit, so
that the rule is reviewable in one place rather than in several hundred.

| Case | Units |
|---|---:|
| TrivialPropertyOrAccessor | 13 |
| ParameterAssigningConstructor | 0 |
| TrivialExpressionBodiedMember | 3 |
| CompilerSuppliedRecordOrEnumMember | 10 |
| DelegatingOverrideOrOperator | 0 |
| InsideAssemblyMarker | 0 |
| FieldDeclaringStorage | 8 |
| EnumMemberOfADeclaredVocabulary | 50 |
| DeclaredInSource | 0 |

## Per-unit exemptions

| Metric | Value |
|---|---:|
| Per-unit exemptions | 0 |

A per-unit `EXEMPT=<reason>` line exempts one unit by a reason a human wrote, for what the
predicate cannot see. Nothing mechanical checks that the reason is true, that it describes
the unit it sits on, or that it says anything at all, so every use is counted and named
here.

No unit in this component states a per-unit exemption.

## Files not covered

No file under a covered project's directory, and no file a covered project compiles in
through a `<Compile Include>` it states, is left out of the record.

## Change detection

`assurance.manifest.json` lists **every** code unit in the 2 covered assemblies -
440 of them, exempt and relevant alike - with the fingerprint of its declaration.
This manifest is a change-detection record, not a review. A unit listed there is watched, not reviewed:
the entry records what the declaration's tokens hashed to when the generator last ran, and
nothing else. What the manifest adds is that a unit the exemption predicate treats as
trivial is no longer invisible: a semantic change to one moves a value in a generated file
the check compares byte for byte. `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript` holds the manifest to the tree.

Beside the units it lists **every covered file** - 32 of them - with a
fingerprint over the complete token stream of its compilation unit. A unit entry exists only
for a declaration kind the scanner enumerates, and an enumeration is a whitelist: an
`[assembly: ...]` attribute is a member of nothing and can be in no unit at all.
Nothing in a covered file can change without something moving here, whatever kind of declaration it is. Comments are outside the stream, because a token's
text is its own characters, so the generated header above and the annotation lines below move
no file fingerprint - which is what lets one generation be a fixed point.

## Verification

The generator and the check are one computation: `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript` works out what the
generator would write and compares it with the tree byte for byte, so a record edited by
hand, or left behind by code that moved, is reported rather than trusted.

| Mode | Command | Effect |
|---|---|---|
| Generate | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance generate --root Broiler.JS/Broiler.ECMAScript` | Fills every `Fingerprint=TBF`, refreshes a decision the code has outrun into `STALE; Previous=...`, rewrites the generated headers, `HUMAN_REVIEW.md`, `assurance.manifest.json` and this file. |
| Check | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript` | Reports every generated artefact that is not byte-identical to what the generator would produce, every relevant unit with no annotation, every annotation this system cannot read, every fingerprint out of date and every unit at the top of the security vocabulary without a criterion, or below it with one. |
| Release | `dotnet run --project Broiler.Code/src/Broiler.Code.Review.Cli -c Release -- assurance check --root Broiler.JS/Broiler.ECMAScript --release` | The check, and additionally every relevant unit left in a state that blocks a release. |

The fingerprint is six hex characters - 24 bits - of SHA-256 over the declaration's token
texts, joined by single spaces. Trivia is excluded because a token's text is its own
characters and never the comments or whitespace around it, so `dotnet format` moves no
fingerprint and an annotation is never part of what it describes. The value answers whether a
unit changed since it was reviewed. It is not a collision-free identifier across units and it
is not a cryptographic commitment.
