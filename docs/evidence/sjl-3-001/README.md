# Evidence bundle SJL-3-001 (Broiler.JS) - the number-conversion pilot

**Milestone:** SJL-3 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-3-number-conversion-pilot.md`: its home
clauses 1 to 3, its Broiler.JS clauses 4 to 7, and this repository's half of clause 14 - work
packages SJL-3.3, SJL-3.5, SJL-3.6 and SJL-3.7. **Rules:** [`decision-rule.md`](decision-rule.md)
(part A, test262 per path; part B, the measurement) and [`reference-arms.md`](reference-arms.md) (the
library-level comparison), both committed at SJL-0.9 before any code they judge, and confirmed
unchanged at SJL-3.2 ([`rules-in-force.log`](rules-in-force.log)). **Collected by**
[`collect.py`](collect.py), from a separate clean checkout of the after commit. **Owner:** the
Broiler.JS maintainer; one person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

**State:** collection in progress. Each run is added as it is taken, and the table of section 2 says
which have been; nothing here is a verdict until its section says so. Broiler.VM keeps its own bundle
of the same name for its own half, judged by its own rule; neither bundle reads the other's runs.

## 1. Identity

| Field | Value |
|---|---|
| Evidence bundle | SJL-3-001 (Broiler.JS's) |
| Base | Bundle [`sjl-0-001`](../sjl-0-001/README.md)'s base commit, `44a214dc`, and its retained runs |
| After | `ef67693`: the cut-over (`93df3fd`) and the two corrections the duplicate search of SJL-3.7 found (`b598028`, `ef67693`). Every run of this bundle is taken from a clean checkout of it, except the test262 run, taken from `b598028`, whose shell `ef67693` builds identically but for the commit each assembly is stamped with (section 5) |
| Rule commit | `1410079`, an ancestor of the after commit; the rule, the comparison script and the workloads are unchanged between the two ([`identity.txt`](identity.txt)) |
| Shared package | `Broiler.ECMAScript.Algorithms` `0.1.0-preview.1`, the version Broiler.VM's committed feed holds, packed from `93df3fd`; its sources are unchanged at `ef67693` |
| Shared vectors | `conformance/vectors/numbers` at corpus revision 5 ([`REVISIONS.md`](../../../conformance/vectors/REVISIONS.md)) |
| RID | `linux-x64`, one machine, four logical processors |

## 2. The clauses, and what holds each

| Clause | What it asks | Where | State |
|---|---|---|---|
| 1 | The library under the conventions; SL1 to SL10, the API baseline and package validation pass; the Native AOT probe runs the vectors | section 3, `home/` | **Met** at `ef67693` |
| 2 | Every number vector passes at library level; every reference-arm difference classified | section 3; [`reference-arms-run/`](reference-arms-run/README.md) | **Met**: every vector of corpus revision 5 at library level; the reference arms MET at revision 4 |
| 3 | A preview package on Broiler.VM's committed feed | Broiler.VM's `eng/packages/` and its pin, which names `93df3fd` | **Done** at SJL-3.3 |
| 4 | `Parser`, `Runtime` and `BuiltIns` reference the library; every member of the deletion list absent; every disposition applied | section 8; [`docs/public-api.md`](../../public-api.md) | the deletion list: **every member absent**; the references and dispositions are in the cut-over commit |
| 5 | Test262 per path: no path passing at the base fails after; every improvement listed | section 5, [`test262/`](test262/) | **Met**: part A MET, no row of 96,720 changed |
| 6 | Engine-level vectors pass; every correction in the changelog | section 4; [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md) | **Met**: no difference, four classified divergences SJL-14 answers |
| 7 | The pristine consumer and package checks; the measurement meets its rule | sections 3, 6 and 7 | the consumers and package checks: **met**; the measurement pending, and void on this machine (section 7) |
| 14 | Each adapter's review finds no algorithm; no intra-engine duplicate of the area survives | section 8 | **reviewed**: no algorithm in an adapter, and no duplicate after `ef67693` |

## 3. The home: the library at the after commit

From a clean checkout of `ef67693`, into [`home/`](home/):

| Check | Result | Where |
|---|---|---|
| The shared filter's tests, built in Release | **274 passed, none failed**: SL1 to SL10 each in both directions and the register holding them `Active`, the API baseline in both directions and package validation on for every packed shared library (SL6), the generated data byte-identical to its generator (SL7), the area's own tests, and `Every_number_vector_passes_against_the_algorithms_library` | [`home/tests.log`](home/tests.log) |
| The corpus generator's own tests | 28, OK | [`home/generator-tests.log`](home/generator-tests.log) |
| The Native AOT probe, `linux-x64` | published with no warning; the publish directory holds no managed assembly; the image ran every vector of corpus revision 5, **10,141 matches**, and its report is byte-identical to the same probe's under the JIT | [`home/probe-publish-linux-x64.log`](home/probe-publish-linux-x64.log), [`home/probe-run-linux-x64.log`](home/probe-run-linux-x64.log), [`home/probe-report-aot.tsv.gz`](home/probe-report-aot.tsv.gz) |
| The shared set at `0.1.0-preview.1` | packed with package validation, restored from a local feed alone into an isolated cache, and its consumer run | [`home/pristine-consumer-shared.log`](home/pristine-consumer-shared.log) |

The reference-arm comparison of clause 2 is [`reference-arms-run/`](reference-arms-run/README.md),
classified at corpus revision 4: MET.

## 4. Engine-level vectors

**No difference.** [`engine/vectors-revision-5.tsv.gz`](engine/vectors-revision-5.tsv.gz) and
[`engine/vectors-tests.log`](engine/vectors-tests.log): `Broiler.JavaScript.Vectors.Tests` at
`ef67693` over corpus revision 5, **10,141 vectors: 10,137 match, 4 classified divergences, 0
differences**. The four are `numbers/literal/0082`, `0084`, `0088` and `0090`, a
`NonOctalDecimalIntegerLiteral` accepted in strict mode code (`strict-non-octal-decimal`), which the
changelog lists as not corrected here and SJL-14 answers. Corpus revision 5 retired every
`divergence.broiler-js` field the cut-over corrected;
[`engine/retired-divergences.tsv`](engine/retired-divergences.tsv) lists each retired vector under its
reason, and the changelog lists each correction. The rows are byte-identical to the run taken at the
cut-over's commit.

## 5. Test262 per path (rule part A)

**MET.** [`test262/comparison.json`](test262/comparison.json) and
[`test262/comparison.log`](test262/comparison.log): the after run against bundle sjl-0-001's base
run, through `scripts/compliance/compare_test262_paths.py` as the rule committed it, with each run's
timed-out rows replaced by their re-runs and the classification file
[`test262/classifications.json`](test262/classifications.json) read. **96,720 rows (48,360 paths, each
in both variants): none changed.** No row is a regression and none needed a class, so the
classification file is empty and the comparison used no entry of it.

| Part A's condition | This run |
|---|---|
| Both runs complete | eight of eight shards reported in each, no incomplete shard, no configuration failure, one suite revision, `ccaac100` ([`test262/merge.log`](test262/merge.log); the base's in sjl-0-001) |
| The script finds them comparable | yes, exit 0; the fields it records and does not compare are the same in both: .NET SDK 10.0.401, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1, C3 or C5, and listed | no verdict changed |

**The after run.** [`test262/drive.sh`](test262/drive.sh) took it at `b598028`, in the base's
configuration with four workers per shard, shard after shard from 2026-10-12 22:42 to 2026-10-13
10:12 ([`test262/drive.log`](test262/drive.log)). The machine restarted twice. The first restart cut
shard 1 short; it was run again from its start with the same binaries, re-hashed before the resume
([`test262/interrupted-shard-1.log`](test262/interrupted-shard-1.log),
[`test262/shell-binaries.before-resume.sha256`](test262/shell-binaries.before-resume.sha256)). The
second came after shard 7 had ended, while the driver's merge was running; that merge wrote nothing,
and the same command was then run separately, as the drive log records. The merged totals, which do
not decide part A: 94,227 variant cases passed, 1,254 failed, 2 timed out and 1,237 skipped.

**Timeouts.** One path timed out in both variants in both runs:
`test/staging/sm/regress/regress-1507322-deep-weakmap.js`. Re-run alone with one worker at each run's
commit, it timed out again each time ([`test262/rerun-timeouts.json`](test262/rerun-timeouts.json) and
its log; the base's in sjl-0-001). The comparison replaced each row with its re-run, so these rows
compare timed out against timed out.

**The shell.** The run's shell was built at `b598028`. The after commit, `ef67693`, changes
`PortableCompiler.cs`, its tests and the changelog, none of which the shell loads. Before the run
ended, this section said that "the bundle will show the shell's binaries rebuilt at `ef67693` hashing
the same". A plain rebuild cannot show that, because every first-party assembly carries its commit:
the SDK's informational version (`+<commit>`), SourceLink's PDB identity, and in `BroilerJS.dll`
Nerdbank.GitVersioning's version and commit id.
[`test262/shell-rebuild/shell-rebuild.log`](test262/shell-rebuild/shell-rebuild.log), written by
[`shell-rebuild.sh`](test262/shell-rebuild/shell-rebuild.sh), rebuilds the shell four times in the
directory the run built it in:

| Build | Result |
|---|---|
| `b598028`, as the run built it | all 39 binaries hash as the run's (`shell-binaries.sha256`) |
| `ef67693`, as the run built it | the 16 first-party assemblies differ; `BroilerJS.dll` reads `+ef6769322b` |
| `ef67693` and `b598028`, with the source-control queries off, so that neither is stamped with its commit | **all 39 hash alike** |

So `ef67693` builds the run's shell exactly, except for the commit each assembly names. Two earlier
attempts are retained beside the log, each with its script and log. The first pinned only the source
revision and differed in the PDBs. The second left Nerdbank.GitVersioning on, and `BroilerJS.dll`
alone differed.

**A stopped run.** A first after run, at the cut-over commit `93df3fd`, started on 2026-10-12 and was
stopped about eleven minutes into shard 0. The duplicate search had found `b598028`'s defect, and that
run would have been taken without its fix. It reported no shard. What it left is retained in
[`test262/stopped-at-93df3fd/`](test262/stopped-at-93df3fd/) and decides nothing.

## 6. The pristine consumer

**Met.** [`engine/pristine-consumer.log`](engine/pristine-consumer.log): the runtime graph of
`Broiler.JavaScript.All` packed from `ef67693` into 18 local packages, the shared
`Broiler.ECMAScript.Algorithms` among them, restored into a clean consumer and run: it executed
JavaScript and printed its answer.

## 7. The measurement sub-bundle (rule part B)

Pending, and **void on this machine whatever its figures**: part B judges only a controlled lane, and
this container is not one ([`Measurement.md`](../../roadmap/Measurement.md), "Controlled lanes and
effective configuration"), so clause 7's measurement half stays open until a controlled lane runs it.
The runs are still taken, three arms rotated, for what the condition and the configuration checks
show.

## 8. The simplification check (SJL-3.7)

**The deletion check.** [`simplification/deletion-check.log`](simplification/deletion-check.log)
searches every member of the area's Broiler.JS deletion list (Broiler.VM
`docs/javascript-shared-libraries/areas/numbers.md` section 7) as a whole word in the engine's
product sources: **all 24 are absent**, `NumberParser` declares only its three public members, the
forwarders decision SJL-D-25 keeps each call the library (`TryCoerceToUInt32` below), the obsolete
`DecimalToBase`, `DecimalToArbitrarySystem` and `JSTrim` forward to it, and one `parseInt`/`parseFloat`
pair remains, `JSGlobal`'s, which `Number.parseInt` and `Number.parseFloat` are.

**The adapter review.** Each adapter was read at `ef67693` against the area's adapter definition
(coercion, errors, charging, object creation, and no algorithm):

| Adapter | What it holds | Algorithm |
|---|---|---|
| `JSNumberPrototype`: `toString`, `toFixed`, `toExponential`, `toPrecision` | `thisNumberValue`; `ToIntegerOrInfinity` of the argument in each method's order (toFixed's range check before the value, toExponential's and toPrecision's non-finite answer after the coercion and before the range check); the `RangeError`s; the `JSString` | none: the text is `NumberText`'s |
| `JSGlobal`: `parseInt`, `parseFloat` | `ToString` of the argument; `ToInt32` of the radix, once | none |
| `NumberParser`: `CoerceToNumber`, `TryGetArrayIndex` | one call each | none |
| `NumberParser.TryCoerceToUInt32` | this public API's own acceptance policy, unchanged from the base: a leading `0` before another digit refused, an all-white-space string refused, an integral value below 2^32 - 1 accepted | none: the value is `CoerceToNumber`'s, the white space `NumberParsing`'s; no digit is read into a value and nothing is rounded |
| `FastScanner.CommitNumberToken`, `FastCompiler.CreateClass`'s literal property names | the scan, the diagnostics, the token; a BigInt's value is the compiler's | none: `NumericLiterals.Evaluate` and `NumberText.ToString` |
| `JSNumber`, `JSString`, `JSValue`, `JSJSON`: a number's text in concatenation, keys and JSON | the string and the key cache; JSON's `null` for a non-finite value | none |
| `JSStringPrototype`: `trim`, `trimStart`, `trimEnd`; `JSBigInt`'s StringToBigInt white space | the `this` coercion; the original string when nothing is trimmed | none: `NumberParsing.TrimStrWhiteSpace` |
| `JSBigInt(string)` | the sign | none: `NumericLiterals.Evaluate` |
| `KeyStrings`, `JSTypedArray`: array indices and canonical numeric keys | the key's metadata cache | none: `NumericKeys` |

**The duplicate search.** [`simplification/duplicate-search.log`](simplification/duplicate-search.log)
lists every construct in the engine's product sources that converts between numbers and text, rounds
by powers of ten, or classifies white space with a table other than the library's: 66 hits at
`ef67693`. It found two duplicates, both corrected in this repository before the after commit and
listed in the changelog:

- **`==` between a Number and a String** compared .NET's text of the number with the string in three
  fast paths, so `NaN == "NaN"` was `true` (`b598028`).
- **`PortableCompiler.EmitCSharp`** wrote each number constant of generated C# in .NET's `"R"`
  format, which does not read back as the same double for `2**-25`, and wrote an infinite constant as
  `Infinity`, which C# does not compile (`ef67693`).

**None of the 66 hits left is a surviving duplicate of the area:**

| Class | Hits | Where | Reading |
|---|---|---|---|
| Names, paths, options and diagnostics | 28 | the compiler's deferred-body diagnostics and direct-eval names, the debugger, the engine's initialisers, the class generator, the expression utilities' input, modules (a `Guid`'s `"N"` format), the key-value store, the portable compiler's and program's arguments, `NullishAccess`'s message, the code cache, the shell's context and REPL, the package manager, `JSError`'s file name, `JSFunction`'s location | no ECMAScript value is converted |
| Other areas' code | 31 | BigInt (`JSBigInt`, 3: StringToBigInt's digits and `toString`'s radix table, SJL-4), Intl (`JSIntl`, 14, and the date-time format engine, 1: SJL-9), Temporal (7: SJL-10), RegExp (`JSRegExp`, 4: the source's escaping and named groups' `\u` escapes, SJL-11), and the lexer's white space (`FastScanner`, 2: SJL-13) | each is answered by its own milestone; areas/numbers.md section 1 does not name them |
| Kept by decision SJL-D-17 | 3 | `JSDecimal` | the `m` literal's `Decimal`, a non-standard extension |
| The compiler's own encoding | 1 | `JSArray.Template`'s `'d'` case | reads back the IEEE-754 bits the compiler wrote, not a formatted number |
| A trim with no caller in the engine | 3 | `StringSpan.Trim`, `TrimStart`, `TrimEnd` (`char.IsWhiteSpace`) | public members of an AST type, which no number conversion calls since the cut-over (`NumberParser` called `Trim` at the base); trimming beyond the shared predicate is SJL-5's |

**Two findings outside the area.** The lexer's white space (`FastScanner`) is .NET's
`char.IsWhiteSpace` and U+FEFF, so it admits U+0085 between tokens, which ECMAScript's `WhiteSpace`
does not (`eval("1\u0085+ 1")` is `2`, a SyntaxError in node); and `StringSpan`'s trims use the same set. Both are recorded for SJL-13 and SJL-5, not
changed here.

## 9. What this bundle does not claim

- **Nothing about Broiler.VM**, whose half is its own bundle, judged by its own rule.
- **A speed claim of any kind**: part B makes none, and on this machine it is void.
- **Review**: the reviewer field says none and means it.
