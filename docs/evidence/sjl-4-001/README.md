# Evidence bundle SJL-4-001 (Broiler.JS) - the numerics area

**Milestone:** SJL-4 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-4-numerics.md`: its home clauses 1 to 3 and
its Broiler.JS clauses 4 to 7. **Rules:** [`decision-rule.md`](decision-rule.md) (test262 per path) and
[`reference-arms.md`](reference-arms.md) (the library-level comparison), both committed at SJL-4.1
before any code they judge. **Area verdict:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md),
"Area verdicts", with Broiler.VM's JSD-0067 and JSD-0068. **Owner:** the Broiler.JS maintainer; one
person holds every role (the programme's EX-30, recorded in Broiler.VM). **Reviewer:** none, and nothing
here has been read by anyone but its author.

**State:** collected, 2026-10-14, at SJL-4.6, from a clean checkout of the after commit `bc2d663`.
**Part A (clause 5) is NOT MET under the rule as written**, on one path that improved and none that
regressed: `staging/sm/TypedArray/slice-bitwise-same.js` fails at the base and passes after, in both
variants, because the engine's own `slice` now follows the specification, and no class of the rule
admits that (section 7). Clauses 1 to 4, 6 and 7 hold (section 4). The first after run, at `2e0f885`,
found a regression the cut-over had made, and was stopped; the fix is `bc2d663` (section 7). Broiler.VM
keeps its own bundle of the same name for its own half, judged by its own rule; neither bundle reads the
other's runs. *(Revised 2026-10-14, at SJL-4.6. The paragraph read: "**State:** collection in progress.
Nothing here is a verdict until its section says so. **Taken:** the engine-level vectors of SJL-4.2
(section 1) and the reference-arm comparison of SJL-4.3 (section 2). The rest is added as SJL-4.4 to
SJL-4.6 take it.", with the note of its revision of 2026-10-13, which quoted its earlier sentences:
"**Taken:** the engine-level vectors of SJL-4.2 (section 1). The rest is added as SJL-4.3 to SJL-4.6
take it.")*

## 1. Engine-level vectors (SJL-4.2)

The shared corpus gains the numerics area at revision 6 and classifies each engine's first run on it at
revision 7 ([`REVISIONS.md`](../../../conformance/vectors/REVISIONS.md)).

| File | What it is |
|---|---|
| [`engine/run-revision-6.tsv.gz`](engine/run-revision-6.tsv.gz), [`engine/run-revision-6.log`](engine/run-revision-6.log) | This engine's first run on the area, at revision 6 (`e3892d6`), through `Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release: 22,634 vectors, 22,576 match, 4 classified divergences (the number area's), 54 unclassified differences. The test fails, as a first run on unclassified vectors does |
| [`engine/run-revision-7.tsv.gz`](engine/run-revision-7.tsv.gz), [`engine/run-revision-7.log`](engine/run-revision-7.log) | The run at revision 7 (`b725bb0`), which classifies the 54: 22,576 match, 58 classified divergences, 0 unclassified differences, and the test passes |
| [`engine/retired-divergences.tsv`](engine/retired-divergences.tsv) | The 391 `divergence.broiler-vm` fields of the number area that revision 6 retires, under their reasons: Broiler.VM's SJL-3.4 cut-over corrects them (its JSD-0066 section 4 and its bundle `sjl-3-001`) |

The 54 differences, under the reasons of
[`scripts/conformance/numerics-divergences.txt`](../../../scripts/conformance/numerics-divergences.txt):
18 `asUintN` results at widths of 2^32 or more, 9 cube roots, 5 BigInt powers past a 32-bit exponent,
5 BigInt property keys beyond 2^32 - 1 or below 0, 5 cases of `1 ** NaN` and of 1 to an infinity,
5 hypotenuses, 5 `expm1` and `log1p` results, and 2 hexadecimal StringToBigInt texts of 262,144 digits
that do not complete within the runner's five seconds. Every one is answered by SJL-4.5. Four
differences between the engines that the area verdict did not classify are Broiler.VM's JSD-0068.

## 2. The reference-arm comparison (SJL-4.3)

**MET**, under [`reference-arms.md`](reference-arms.md) as revised before any run by
[`reference-arms-revised-2026-10-13.md`](reference-arms-revised-2026-10-13.md):
[`reference-arms-run/`](reference-arms-run/README.md). S, the numerics area of
`Broiler.ECMAScript.Algorithms`, was run against test-only copies of both engines' code at their SJL-3
merges (arm J, Broiler.JS `4899f9c`; arm V, Broiler.VM `a04b2e5`). The run covered the rule's
population of doubles, binary16 patterns, Math inputs, precise sums, exact roundings and BigInts, and
its charges.

- **Every difference is classified.** Of 8,064,422 differences, 7,259,262 are corrections and 805,160
  are approximations both admitted, each covered by an `impl-defined` vector of corpus revision 6.
- **Arm V's BigInt charges equal S's on every input**, as JSD-0067 decision 3 requires.
- **The first run found a defect of S**, an exact upper end rounded down under "zero", and one of the
  comparison's entry points. Both were fixed and the whole comparison run again; both runs are retained.
- **Both negative controls were reported.**

What the corrections say about each engine is in [`reference-arms-run/README.md`](reference-arms-run/README.md)
section 3; SJL-4.4 and SJL-4.5 list the observable ones with their cut-overs.

## 3. Identity (SJL-4.6)

| Field | Value |
|---|---|
| Evidence bundle | SJL-4-001 (Broiler.JS's) |
| Base | Bundle [`sjl-3-001`](../sjl-3-001/README.md)'s after run, taken at `b598028`, whose shell `ef67693` builds identically but for the commit each assembly names (that bundle's section 5), and its re-run of the rows that timed out |
| The rule's base condition | `git diff --name-only ef67693 1eb5262` under `Broiler.JS/` and `Broiler.ECMAScript/` names one file, `Broiler.JS/Broiler.JavaScript.Vectors.Tests/Runners/BroilerJsEngine.cs`, the vectors runner, which the shell's build does not read. **It holds**, so the base was not taken again ([`identity.txt`](identity.txt)) |
| After | `bc2d663`: SJL-4.5's cut-over, from `0aa8369` (the integer conversions and Float16) through the merges of the Math kernels (`c565360`), the BigInt kernel (`21a72c2`), the exact rounding (`e4094ec`) and the surviving copies (`eb3ff34`), with corpus revision 9 (`f2c3597`), the records (`4721c23`, `2e0f885`), and the fix of the typed arrays' same-type copies that section 7's first run found (`bc2d663`). Every run of this bundle is taken from a clean checkout of it |
| Rule commit | `1eb5262`, an ancestor of the after commit; the rule and the comparison script are unchanged between the two ([`identity.txt`](identity.txt)) |
| Shared library | `Broiler.ECMAScript.Algorithms` at the after commit. Broiler.VM's committed feed holds `0.2.0-preview.1`, packed from `01209c4`. The library at the after commit differs from it by one change, `ff7416b`: `BigIntPolicy.RepresentationLimit` is 2^31 - 128 bits, where it was 2^31 - 1, which the BigInt cut-over found (`Broiler.ECMAScript/CHANGELOG.md`, "Changed"). Section 5's pack is of the after commit's library, under the same version string, into an isolated feed; it is not published |
| Shared vectors | `conformance/vectors` at corpus revision 9 ([`REVISIONS.md`](../../../conformance/vectors/REVISIONS.md)) |
| RID | `linux-x64`, one machine, four logical processors |

## 4. The clauses, and what holds each

| Clause | What it asks | Where | State |
|---|---|---|---|
| 1 | Every sub-area that was not refused is implemented under the conventions | [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts"; section 5 | **Met**: the four sub-areas, all accepted at SJL-4.1, are in `Broiler.ECMAScript.Algorithms` at `bc2d663` |
| 2 | Its vectors and reference arms pass under the area rule | sections 2 and 5 | **Met**: every vector of corpus revision 9 at library level, under the JIT and Native AOT alike; the reference arms MET (section 2) |
| 3 | The `SL` rules, the API baseline and package validation pass, and the Native AOT probe runs | section 5 | **Met** at `bc2d663` |
| 4 | The deletion list is absent and the public dispositions are recorded | section 9; [`docs/public-api.md`](../../public-api.md) | **Met**: every entry absent; the dispositions recorded, no public member removed |
| 5 | test262 per path meets the area rule | section 7, [`test262/`](test262/) | **Not met under the rule as written**: no regression, and one path improved in both variants, which no class admits |
| 6 | Engine-level vectors pass | section 6 | **Met**: no difference at corpus revision 9, four classified divergences SJL-14 answers |
| 7 | Every correction is listed | [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md) | **Met**: each correction the corpus names with its vectors, each other with its tests, and the copies' correction section 7 found |

The pristine consumers of section 8 and the package checks of section 5 are the engine's and the
library's, as SJL-3's were.

## 5. The home: the library at the after commit

From a clean checkout of `bc2d663`, into [`home/`](home/):

| Check | Result | Where |
|---|---|---|
| The shared filter's tests, built in Release | **318 passed, none failed**: SL1 to SL10 each in both directions and the register holding them `Active`, the API baseline in both directions and package validation on for every packed shared library (SL6), the generated data byte-identical to its generator (SL7), the area's own tests, and `Every_numerics_vector_passes_against_the_algorithms_library` | [`home/build.log`](home/build.log), [`home/tests.log`](home/tests.log) |
| The corpus generators' own tests | 62, OK | [`home/generator-tests.log`](home/generator-tests.log) |
| The Native AOT probe, `linux-x64` | published with no warning, and the publish directory holds no managed assembly; the image ran corpus revision 9, **22,634 vectors, no failure**, and its report is byte-identical to the same probe's under the JIT | [`home/probe-publish-linux-x64.log`](home/probe-publish-linux-x64.log), [`home/probe-run-linux-x64.log`](home/probe-run-linux-x64.log), [`home/probe-report-aot.tsv.gz`](home/probe-report-aot.tsv.gz) |
| The shared set at `0.2.0-preview.1` | packed from the after commit with package validation, restored from a local feed alone into an isolated cache, and its consumer run | [`home/pristine-consumer-shared.log`](home/pristine-consumer-shared.log) |

The pack is of the after commit's library, which differs from the package on Broiler.VM's feed by
`ff7416b` alone (section 3); it is not published.

## 6. Engine-level vectors at revision 9

**No difference.** [`engine/vectors-revision-9.tsv.gz`](engine/vectors-revision-9.tsv.gz) and
[`engine/vectors-tests.log`](engine/vectors-tests.log): `Broiler.JavaScript.Vectors.Tests` at
`bc2d663` over corpus revision 9, **22,634 vectors: 22,630 match, 4 classified divergences, 0
differences**, and the test's four cases pass, its negative control among them. The four are
`numbers/literal/0082`, `0084`, `0088` and `0090`, a `NonOctalDecimalIntegerLiteral` accepted in
strict mode code, which the changelog lists as not corrected here and SJL-14 answers, as at SJL-3.
Corpus revision 9 retired the 54 `divergence.broiler-js` fields of section 1 that SJL-4.5 corrects;
[`engine/retired-divergences-revision-9.tsv`](engine/retired-divergences-revision-9.tsv) lists each
under its reason. The rows are byte-identical to the run taken at the cut-over before its fix.

## 7. Test262 per path (rule part A)

**NOT MET under the rule as written; no row regressed.** [`test262/comparison.json`](test262/comparison.json) and
[`test262/comparison.log`](test262/comparison.log): the after run against bundle sjl-3-001's after
run, through `scripts/compliance/compare_test262_paths.py` as the rule committed it, with each run's
timed-out rows replaced by their re-runs and [`test262/classifications.json`](test262/classifications.json)
read. **96,720 rows (48,360 paths, each in both variants): two changed, one path in both
variants, and no row passing at the base fails, times out or is skipped after.**

| Part A's condition | This run |
|---|---|
| Both runs complete | eight of eight shards reported in each, no incomplete shard, no configuration failure, one suite revision, `ccaac100` ([`test262/merge.log`](test262/merge.log); the base's in sjl-3-001) |
| The script finds them comparable | yes ([`test262/compare-checks.log`](test262/compare-checks.log): the after run against itself changes nothing, and a planted regression is reported); the fields it records and does not compare are the same in both: .NET SDK 10.0.401, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1, C3 or C5, and listed | **not met**: the two rows below are in no class (the classification file is empty) |

**The changed rows.**

| Path | Variant | Base | After |
|---|---|---|---|
| `test/staging/sm/TypedArray/slice-bitwise-same.js` | original | failed | passed |
| `test/staging/sm/TypedArray/slice-bitwise-same.js` | terser | failed | passed |

The test slices typed arrays of binary32 and binary64 NaNs of every kind into arrays of the same
type, and asks for the same bits. At the base, `slice` copied each element through a double, which
quiets a signalling binary32 NaN: `0x7F800001` came out `0x7FC00001`. After `bc2d663` it moves
bytes, as `%TypedArray%.prototype.slice` step 14.g asks, and the bits are kept. The changelog lists
the correction ("The typed arrays' copies of the same element type").

**Why the rows have no class.** C1 admits a row "because the shared implementation follows the
specification where this engine did not". These rows pass because the engine's own `slice` now
follows the specification: binary32 elements never reach the shared library, whose binary16 read is
what made the byte copy necessary (the stopped run, below). The class's wording does not reach them, and
the rule as written counts every other difference as a regression, an improvement included. So part
A is **not met**, on one improved path and no regressed one. Admitting these rows would take a
dated revision of the rule, written after this run and so weaker evidence, as Broiler.VM's bundle
did for its own part A. That is the owner's decision; this bundle does not take it.

**The after run.** [`test262/drive.sh`](test262/drive.sh) took it at `bc2d663`, in the base's
configuration with four workers per shard, from 2026-10-14 07:04 to 13:05
([`test262/drive.log`](test262/drive.log)), on the otherwise idle machine; its shell is hashed in
[`test262/shell-binaries.sha256`](test262/shell-binaries.sha256). It took shards 4 to 7 first,
which the stopped run below had not reached, and then 0 to 3; the order changes nothing a shard runs.
The merged totals, which do not decide part A: 94,229 variant cases passed, 1,252 failed, 2 timed out
and 1,237 skipped.

**Timeouts.** As at the base, one path timed out in both variants:
`test/staging/sm/regress/regress-1507322-deep-weakmap.js`. Re-run alone with one worker at the after
commit, it timed out again in both ([`test262/rerun-timeouts.json`](test262/rerun-timeouts.json) and
its log), so the rows compare timed out against timed out.

**The stopped run.** A first after run, at `2e0f885`, the cut-over before its fix, ran from 2026-10-14
02:20. Two machine restarts interrupted its shard 3, which ran again from its start each time with the
same binaries, re-hashed first. It was stopped at 06:24, during shard 4: shards 0 to 3 had shown
`test/built-ins/TypedArray/prototype/slice/bit-precision.js` passing at the base and failing after, in
both variants, a regression no class admits. The cut-over had made a NaN read from a `Float16Array`
the platform's quiet NaN, and `slice`, `copyWithin` and the constructor given a typed array copied
through the value, so a binary16 NaN came back with its sign set and its payload dropped. `bc2d663`
makes the three move bytes when the element types match, as the specification asks, and this run was
taken again from its start at that commit. What the stopped run left is retained in
[`test262/stopped-at-2e0f885/`](test262/stopped-at-2e0f885/): its driver and log, its self-check, the
binaries' hashes before each resume, both interrupted logs (identical: each attempt died between the
same two failures, logged at 04:29 and 04:51), shards 0 to 3's statuses, logs and rows
(`paths-shards-0-to-3.tsv.gz`), the stopped shard 4's log, and a plain join of shards 0 to 3 with
the base (`comparison-shards-0-to-3.log`). It decides nothing. Before this run started, the fix was
checked on `TypedArray/prototype/slice`, `copyWithin` and `set`, `TypedArrayConstructors` and 17
staging tests ([`test262/fix-check/`](test262/fix-check/)): its first version broke
`slice/resize-count-bytes-to-zero.js`, an empty copy past a shrunk buffer's end, and was corrected before
`bc2d663`; the committed version changes only the two rows above.

## 8. The pristine consumer

**Met.** [`engine/pristine-consumer.log`](engine/pristine-consumer.log): the runtime graph of
`Broiler.JavaScript.All` packed from `bc2d663` into 18 local packages, the shared
`Broiler.ECMAScript.Algorithms` among them, restored into a clean consumer and run: it executed
JavaScript and printed its answer.

## 9. The simplification check

**The deletion check.** [`simplification/deletion-check.log`](simplification/deletion-check.log)
searches each entry of the area's Broiler.JS deletion list (Broiler.VM
`docs/javascript-shared-libraries/milestones/sjl-4-numerics.md`, SJL-4.5) in the file or folder that
held it, with a pattern for what the entry deleted. **Every entry is absent.** The lines that remain
are of three kinds:

| Entry | What remains | Reading |
|---|---|---|
| `JSValue.ToUint32(double)`'s body | `ToUint32(double d) => IntegerConversions.ToUint32(d)` | the internal member, a forwarder |
| The inline clamp in `JSUint8ClampedArray`; the inline `(Half)` casts | nothing | absent |
| `JSMath`'s kernels | a comment naming what was removed | absent |
| `JSValue.Power` and the direct `Math.Pow` emission | two comments: `BExpression.Power` calls `MathKernels.Power`, never `Math.Pow`, and `LambdaConverter` says why `Expression.Power` without a method is not used | absent |
| The rounding copies | comments naming what was removed, and where the library's call replaced each | absent |
| `JSBigInt`'s members | `ToNumber`, an obsolete forwarder to `BigIntMath.ToNumber` with the message decision SJL-D-25 (b) gives, and comments; `AsIntN` and `AsUintN` are the built-ins, whose bodies call `BigIntMath.AsIntN` and `AsUintN` | the bodies are absent; the public member stays as SJL-D-25 asks |
| `JSBigIntExtensions.CompareToNumber` | two calls of `BigIntMath.CompareToNumber`, and a comment | absent |

**The public dispositions** are recorded in [`docs/public-api.md`](../../public-api.md), "Public API
changes, 2026-10-13": `JSNumericOperators.ToInt32` and `ToUint32` and `JSValue.IntValue` and
`UIntValue` stay as forwarders (SJL-D-25 (a)); `JSMath.Hypot(double, double)` and
`JSBigInt.ToNumber(BigInteger)` stay as obsolete forwarders (SJL-D-25 (b)); no public member is
removed, so the decision's consumer check is not needed.

**The duplicate search.** [`simplification/duplicate-search.log`](simplification/duplicate-search.log)
lists every construct in the engine's product sources that approximates a Math function the library
computes, converts binary16 or a BigInt by the platform, rounds by a mode spelled as a string or by
the platform's midpoint rule, or writes the 32-bit wrap out: **45 hits at `bc2d663`**, the same as at `2e0f885`. A search taken
before the after commit, at `728e958`, found the copies that `eb3ff34` moved to the library: the
hand-written conversions of a dozen built-ins, `CreateListFromArrayLike`'s fast path, and Intl's
notations dividing by a power of ten in doubles. The changelog lists the results each changes
("The copies the search for survivors found").

**None of the 45 is a surviving duplicate of the area:**

| Class | Hits | Where | Reading |
|---|---|---|---|
| An integrality test | 7 | `x == Math.Truncate(x)` or its negation, once each in `JSTypedArray` (a canonical numeric index), `JSBigInt` (`NumberToBigInt`'s `IsIntegralNumber`), `JSIntl`, `JSTemporalDuration`, `JSTemporalInstant`, `FunctionTiering` and `TaggedValuePrototype` | `Math.Truncate` is exact; nothing is converted or rounded |
| `Math.trunc` | 2 | `JSMath.Trunc` | `Math.trunc` is `Math.Truncate`, exact, and not one of the area's kernels; its `Decimal` case is the `m` literal's, kept by decision SJL-D-17 |
| Intl's mathematical value and its option | 18 | `JSIntl`: `BigInteger.Pow(10, k)` and `BigInteger.Parse` of a decimal string's digits (12); compact notation's decimal exponent and a duration's sub-second fraction in doubles, by `Math.Pow(10, n)` (4); the rounding mode's name `"halfExpand"` as `GetOption`'s default and the resolved option's initial value (2) | the exact decimal arithmetic of the Intl mathematical value, which the milestone leaves to SJL-9 ("Does not do"); the rounding is `ExactRounding`'s |
| Intl's time zone name | 1 | `JSIntlDateTimeFormatEngine.GmtOffset`: `Math.Round(offsetMs / 60000.0)` | an offset's minutes for `timeZoneName`, SJL-9's; the note below |
| A range limit, not a wrap | 2 | `JSIntl` and `JSTemporalDuration`: `IsValidDuration`'s bound of 2^32 on years, months and weeks | a comparison with 2^32; nothing wraps |
| Temporal's own exact values | 7 | `BigInteger.Parse` of a fraction's digits and `BigInteger.Pow(10, n)` (`JSTemporalDuration`, `JSTemporalInstant`), and the epoch-nanosecond bounds as `BigInteger` constants (`JSTemporalInstant`, `JSTemporalPlainDateTime`, `JSTemporalZonedDateTime`) | Temporal's quantities, item SJL-10's; the rounding is `ExactRounding`'s |
| Comments | 4 | `JSIntl` (3) and `JSTemporalZonedDateTime` (1), naming the mode `"halfExpand"` | no code |
| The engine's tagged value | 1 | `TaggedValuePrototype`: `unchecked((int)(uint)(bits & PayloadMask))` | reads a 32-bit payload out of the value's bits; no Number is converted |
| The changelog | 3 | `Broiler.JS/CHANGELOG.md`, the corrections' examples | documentation |

**A note outside the area.** `GmtOffset` rounds an offset to whole minutes with .NET's `Math.Round`,
whose ties go to even, so an offset of a whole number of minutes and thirty seconds, such as Monrovia's
-0:44:30 before 1972, is named as -0:44. How `timeZoneName` names an offset with seconds is the
implementation's choice and item SJL-9's question; it is recorded, not changed here.

## 10. What this bundle does not claim

- **Nothing about Broiler.VM**, whose half is its own bundle, judged by its own rule.
- **A speed claim of any kind.** SJL-4 measures nothing.
- **Review**: the reviewer field says none and means it.
