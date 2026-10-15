# Evidence bundle SJL-5-001 (Broiler.JS) - the text area

**Milestone:** SJL-5 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-5-text-algorithms.md`: its home clauses and
its Broiler.JS clauses. **Rules:** [`decision-rule.md`](decision-rule.md) (test262 per path) and
[`reference-arms.md`](reference-arms.md) (the library-level comparison), both committed at SJL-5.1 before
any code they judge, the latter revised before any run by
[`reference-arms-revised-2026-10-14.md`](reference-arms-revised-2026-10-14.md); their state before the
first code of the area is [`rules-in-force.log`](rules-in-force.log). **Area verdict:**
[`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", with Broiler.VM's JSD-0070.
**Owner:** the Broiler.JS maintainer; one person holds every role (the programme's EX-30, recorded in
Broiler.VM). **Reviewer:** none, and nothing here has been read by anyone but its author.

**State:** collected, 2026-10-15, at SJL-5.6, from a clean checkout of the after commit `1ebde70`.
**The clauses this repository holds are met**: home clauses 1 and 2 and Broiler.JS clauses 3 to 5
(section 6), and **part A of the rule, clause 4, is MET under the rule as written**, with no revision
(section 9). The first after run, at `473dc46`, the merge that landed SJL-5.5's cut-over, found a defect
of this engine's Intl that correction X2 had uncovered, and under the rule it is NOT MET; the owner had
the defect fixed in `1ebde70`, the after run taken again there, and confirmed `1ebde70` as this bundle's
after commit (sections 5 and 9). Broiler.VM keeps its own bundle of the same name for its own half,
judged by its own rule; neither bundle reads the other's runs. *(Revised 2026-10-15, at SJL-5.6. The
paragraph read: "**State:** collection in progress. Nothing here is a verdict until its section says so.
**Taken:** the test262 base (section 1), the engine-level runs on the text area (section 2) and the
reference-arm comparison, which is **MET** (section 3). The after run and the rest of SJL-5.6 are added
when they are taken. Broiler.VM keeps its own bundle of the same name for its own half, judged by its own
rule; neither bundle reads the other's runs.", with the note of its revision of 2026-10-14, which quoted
its earlier sentences: "**Taken:** the test262 base (section 1) and the first engine-level runs on the
text area (section 2). The reference-arm comparison, the after run and the rest of SJL-5.6 are added
when they are taken.")*

## 1. The test262 base (part A)

| | |
|---|---|
| Commit | `3309b15`, the commit that carries the rule, before any code of SJL-5 landed |
| Taken | 2026-10-14, 16:49Z to 18:16Z, by `.github/workflows/test262-evidence.yml` (run 38601306943), in the rule's configuration: eight shards over `--all-script-host-verifiable`, terser at its pinned version, the per-test timeout of 30 seconds, no memory limit, four workers |
| Retained | [`test262/base/`](test262/base/): the workflow's artifact `test262-evidence`, every file unchanged, with [`imported.txt`](test262/base/imported.txt), which records its digest |
| Shell | one digest of the shell's binaries across the eight shards and the re-run ([`shell-hashes.log`](test262/base/shell-hashes.log)) |
| Timed out | one path, `test/staging/sm/regress/regress-1507322-deep-weakmap.js`, in both variants; run again alone with one worker, it timed out again, and the re-run replaces its rows ([`rerun-timeouts.log`](test262/base/rerun-timeouts.log)) |

The comparison with the after run is section 9, and the after run's commit, `1ebde70`, is section 5's.
*(Revised 2026-10-15, at SJL-5.6. The sentence read: "The comparison with the after run is added when the
after run is taken, at the commit that lands SJL-5.5's cut-over.")*

## 2. Engine-level vectors: the first runs on the text area (SJL-5.2)

The engine-level runner, `Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release, read **no vector** of
corpus revision 10: its validator refused the four clause ids with `%` that the text area cites
([`engine/run-revision-10.log`](engine/run-revision-10.log)). Revision 11 widens the schema's clause ids
and changes no vector. On it, at `753d42e`
([`engine/run-revision-11.log`](engine/run-revision-11.log),
[`engine/run-revision-11.tsv.gz`](engine/run-revision-11.tsv.gz)): **42,943 vectors, 42,897 match, 4
classified divergences, 42 unclassified differences.** The 4 are the number area's
`strict-non-octal-decimal` fields, which stay with SJL-14. The negative control
([`engine/negative-control-revision-11.tsv`](engine/negative-control-revision-11.tsv)) runs the 280
vectors of `numbers/to-fixed.vectors` from a copy of the corpus in which `numbers/to-fixed/0001`'s
expected value is changed. That vector is the one verdict that moves, and it moves to a difference.

Corpus revision 12 classifies the 42, each under the row of bundle `sjl-5-001`'s correspondence from code
(Broiler.VM's `docs/evidence/sjl-5-001/correspondence.md`) that it confirms. SJL-5.5 answers each one:

| Row | Vectors | What this engine does |
|---|---|---|
| B1 | 6 `text/set-from-hex` | `setFromHex` parses its whole input before honouring the destination |
| B2 | `text/options/0001`, `0002`, `0006` | an options argument that is not an object is ignored |
| B3 | `text/options/0003`, `0004` | a `null` option is read as the default |
| B4 | `text/options/0009` to `0012` | a detached array encodes as empty or decodes nothing |
| J1 | `text/parse-depth/0011` to `0022` | nesting past 64 is refused |
| J2 | `text/parse/2364`, `2365`, `text/parse-surrogate/0001`, `0003` | an escaped lone surrogate is refused |
| J3 | `text/parse/2367`, `2368`, `text/parse-surrogate/0002`, `0004` | a raw lone surrogate lets a .NET exception through |
| J6 | `text/raw-json/0005`, `0006` | a look-alike object is taken for raw JSON |
| J7 | `text/raw-json/0003` | the brand's property is not enumerable |
| P5 | `text/last-index-of/0002` | `fromIndex` is ignored with a third argument |
| S2 | `text/named-to-string/0001` | a named group's value is converted without `Symbol.toPrimitive` |
| S3 | `text/absent-replace/0001`, `0002` | a RegExp whose `Symbol.replace` is absent is matched as a pattern |

**The vectors refute row J4**: `JSON.parse("1e400")` and `JSON.parse("-1e400")` answer the infinities,
as the specification gives them (`text/parse-number/0001`, `0002`). The plan's record of the corrections
says so in a dated correction. Rows P4, X1 and X2 have no vector, and row J5's three vectors match because
the run was on Linux. Every difference is one the area verdict classified, so its falsifier does not
fire. *(Revised 2026-10-14, when the run on revision 12 was taken. The paragraph ended: "The run on
revision 12 is added to this section when it is taken.")*

**Every vector of corpus revision 12 has a classified result in this engine.** At `9ae67ee`, which carries
revision 12 ([`engine/run-revision-12.log`](engine/run-revision-12.log),
[`engine/run-revision-12.tsv.gz`](engine/run-revision-12.tsv.gz)): **42,943 vectors, 42,897 match, 46
classified divergences, 0 unclassified differences**, and the runner's four tests pass. The 46 are the 42
fields above and the number area's 4. The negative control
([`engine/negative-control-revision-12.tsv`](engine/negative-control-revision-12.tsv)) moves
`numbers/to-fixed/0001`'s verdict alone, to a difference.

## 3. The reference-arm comparison (SJL-5.3): MET

[`reference-arms-run/`](reference-arms-run/README.md) holds it. The rule is
[`reference-arms.md`](reference-arms.md), as revised before any run by
[`reference-arms-revised-2026-10-14.md`](reference-arms-revised-2026-10-14.md). Run at `95f4387`, the
comparison lists 80,013 differences between S and the arms. Every one is class (a), a correction that
S makes, under the rows of the correspondence: B1, S3, J1, J2 and J3 of this engine's arm, and P2 of
Broiler.VM's. Arm V's charges are S's wherever its values are. Both negative controls were reported and
left unclassified. The run under `en-US` gives the same listing, byte for byte.

**The first run did not meet the rule**, and the bundle keeps it (`reference-arms-run/first-run/`). The
classifier could not read the comparison's shortened listing of a long field, so it put 3,158 differences
in class (c) and left 13,227 unclassified. S was right on all of them. The classifier was corrected at
`95f4387`, before the whole comparison ran again.

## 4. The cut-over's vectors (SJL-5.5)

On corpus revision 12, with the product sources of the change that lands SJL-5.5's cut-over
([`engine/cut-over-revision-12.log`](engine/cut-over-revision-12.log),
[`engine/cut-over-revision-12.tsv.gz`](engine/cut-over-revision-12.tsv.gz)): **42,943 vectors, 42,939
match, 4 classified divergences, 0 unclassified differences**, and the runner's four tests pass. Each of
the 42 fields of section 2 reports "divergence no longer holds"; the 4 are the number area's, as before.
Every correction is listed in [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md), and corpus
revision 13 retires the fields.

**Every vector of corpus revision 13 has the result its class expects.** At `bb1fc66`, which carries
revision 13 and retires the 42 fields with Broiler.VM's five
([`engine/retired-divergences-revision-13.tsv`](engine/retired-divergences-revision-13.tsv)), with the
same product sources ([`engine/run-revision-13.log`](engine/run-revision-13.log),
[`engine/run-revision-13.tsv.gz`](engine/run-revision-13.tsv.gz)): **42,943 vectors, 42,939 match, 4
classified divergences, 0 unclassified differences**, and the runner's four tests pass. The 4 are the
number area's.

## 5. The after arm (SJL-5.6)

| Field | Value |
|---|---|
| Evidence bundle | SJL-5-001 (Broiler.JS's) |
| Base | Section 1: the workflow's run at `3309b15`, the commit that carries the rule, before any code of SJL-5 landed, with its re-run of the rows that timed out |
| After | `1ebde70`: SJL-5.5's cut-over (`37ece76`) and corpus revision 13 (`bb1fc66`), as merged into the default branch in `473dc46`, with the fix of the defect the first after run found (`1ebde70`, section 9). Its product sources differ from the cut-over's in four files, which [`identity.txt`](identity.txt) lists: `JSIntl.cs`, the generated `JSIntlDefaultNumberingSystems.g.cs`, their tests and the changelog. Every local run of this bundle is taken from a clean checkout of it |
| Why that commit | The rule takes the after run "at the commit that lands SJL-5.5's cut-over, named in bundle sjl-5-001". The first after run was taken at `473dc46`, the merge that landed it; it found one path regressed in both variants, a defect of this engine's Intl that correction X2 had uncovered (section 9). The owner chose, on 2026-10-15, to fix the defect and take the after run again rather than revise the rule, as bundle `sjl-4-001` did when its first after run found a regression: its after commit carries that fix. The owner confirmed the same day, once that run's verdict was known to be NOT MET, that this bundle's after commit is `1ebde70`. The first after run is kept whole, with its comparison, and what the rule makes of it is stated in section 9 |
| Rule commit | `3309b15`, an ancestor of the after commit; the rule and the comparison script are unchanged between the two ([`identity.txt`](identity.txt)) |
| Shared library | `Broiler.ECMAScript.Algorithms` at the after commit, whose code is that of the package published to nuget.org as `0.1.0-preview.3`, from `6e20998`: `git diff 6e20998 1ebde70 -- Broiler.ECMAScript` is empty. Section 7's pack is of that code under the same version, into an isolated feed |
| Shared vectors | `conformance/vectors` at corpus revision 13 ([`REVISIONS.md`](../../../conformance/vectors/REVISIONS.md)) |
| RID | `linux-x64`: the workflow's runners for test262, and for the local runs one machine with four logical processors |

## 6. The clauses, and what holds each

| Clause | What it asks | Where | State |
|---|---|---|---|
| 1 | Every sub-area that was not refused is implemented under the conventions, and its vectors and reference arms pass under the area rule | [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts"; sections 3 and 7 | **Met**: the six sub-areas, all accepted at SJL-5.1, are in `Broiler.ECMAScript.Algorithms` at `1ebde70`; every vector of corpus revision 13 passes at library level, under the JIT and Native AOT alike; the reference arms MET (section 3) |
| 2 | The `SL` rules, the API baseline and package validation pass, and the Native AOT probe runs | section 7 | **Met** at `1ebde70` |
| 3 | The deletion list is absent | section 11 | **Met**: every entry absent at `1ebde70`; what remains is adapters and comments |
| 4 | test262 per path meets the area rule | section 9, [`test262/`](test262/) | **Met** under part A of the rule as written, at the after commit `1ebde70`: no regression, and every changed row C1. The first after run, at `473dc46`, the merge that landed the cut-over, is **not met**, on one path regressed in both variants, a defect `1ebde70` fixes; section 5 says why the after commit is `1ebde70` |
| 5 | Every correction is listed | [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md) | **Met**: each correction the milestone lists, under its row of the correspondence: the JSON nesting limit (J1), lone surrogates (J2, J3), CRLF in indented output (J5, on 2026-10-14), partial writes in `setFromHex` (B1), the `split` limit (X1), the ordinal search (X2) and forgeable raw JSON (J6), with B2 to B4, J7, P4, P5, S2 and S3; `1e400` is not a correction, the vectors having refuted J4 (section 2). The numbering systems and the Temporal digits section 9 found are listed too |

Clauses 6 to 9 are Broiler.VM's, judged in its bundle of the same name. None is accepted: review is
deferred, and SJL-5 may not meet its gate before SJL-3 meets SJL-3's.

## 7. The home: the library at the after commit

From a clean checkout of `1ebde70`, into [`home/`](home/):

| Check | Result | Where |
|---|---|---|
| The shared filter's tests, built in Release | **388 passed, none failed** (110, 246 and 32 in the three projects): SL1 to SL10 each in both directions and the register holding them `Active`, the API baseline in both directions and package validation on for every packed shared library (SL6), the generated data byte-identical to its generator (SL7), the area's own tests, and `Every_text_vector_passes_against_the_algorithms_library` | [`home/build.log`](home/build.log), [`home/tests.log`](home/tests.log) |
| The corpus generators' own tests | 81, OK | [`home/generator-tests.log`](home/generator-tests.log) |
| The Native AOT probe, `linux-x64` | published with no warning, and the publish directory holds no managed assembly; the image ran corpus revision 13, **42,943 vectors, no failure**, and its report is byte-identical to the same probe's under the JIT | [`home/probe-publish-linux-x64.log`](home/probe-publish-linux-x64.log), [`home/probe-run-linux-x64.log`](home/probe-run-linux-x64.log), [`home/probe-report-aot.tsv.gz`](home/probe-report-aot.tsv.gz) |
| The shared set at `0.1.0-preview.3` | packed from the after commit with package validation, restored from a local feed alone into an isolated cache, and its consumer run | [`home/pristine-consumer-shared.log`](home/pristine-consumer-shared.log) |

The same checks, taken earlier the same day at `6b0d06f` (the cut-over's product sources), gave the same
results; this folder holds the later ones, and commit `e1b4af7` the earlier.

## 8. Engine-level vectors at the after commit

**No difference.** [`engine/vectors-revision-13.tsv.gz`](engine/vectors-revision-13.tsv.gz) and
[`engine/vectors-tests.log`](engine/vectors-tests.log): `Broiler.JavaScript.Vectors.Tests` at `1ebde70`
over corpus revision 13, **42,943 vectors: 42,939 match, 4 classified divergences, 0 differences**, and
the test's four cases pass, its negative control among them. The four are `numbers/literal/0082`,
`0084`, `0088` and `0090`, a `NonOctalDecimalIntegerLiteral` accepted in strict mode code, which the
changelog lists as not corrected here and SJL-14 answers, as at SJL-3 and SJL-4. The rows are
byte-identical to section 4's run at `bb1fc66`, the cut-over's product sources: the Intl fix moves no
vector.

## 9. Test262 per path (rule part A)

**MET under the rule as written, at the after commit `1ebde70`; no row regressed.**
[`test262/comparison.json`](test262/comparison.json) and
[`test262/comparison.log`](test262/comparison.log): the after run against the base of section 1, through
`scripts/compliance/compare_test262_paths.py` as the rule committed it, with each run's timed-out rows
replaced by their re-runs and [`test262/classifications.json`](test262/classifications.json) read.
**96,720 rows (48,360 paths, each in both variants): 8 changed, every one admitted as C1, 0 unclassified,
and no row passing at the base fails, times out or is skipped after.**

| Part A's condition | This run |
|---|---|
| Both runs complete | eight of eight shards reported in each, no incomplete shard, no configuration failure, one suite revision, `ccaac100`, and one shell in each, its binaries' hashes the same in every shard and in the re-run ([`test262/after/merge.log`](test262/after/merge.log), [`test262/after/shell-hashes.log`](test262/after/shell-hashes.log); the base's in [`test262/base/`](test262/base/)) |
| The script finds them comparable | yes ([`test262/compare-checks.log`](test262/compare-checks.log): the after run against itself changes nothing, and a planted regression is reported, exit 1); the fields it records and does not compare are the same in both: .NET SDK 10.0.402, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1, C3 or C5, and listed | **met**: the 8 rows below, all C1, each with its reason in the classification file, which names no row the comparison did not change |

**The changed rows.**

| Path | Variants | Base | After | Class | Why |
|---|---|---|---|---|---|
| `test/built-ins/Uint8Array/prototype/setFromBase64/detached-buffer.js` | original, terser | failed | passed | C1 | this engine's correction B4 (the correspondence's row B4): ECMA-262's ValidateUint8Array and GetUint8ArrayBytes throw a `TypeError` for a detached or out-of-bounds array, where `setFromBase64` answered `{ read: 0, written: 0 }`. The shared codecs' adapters validate the array first since `37ece76`. Vectors: `text/options/0009` to `0012`, reason `js-detached-codec`, which corpus revision 13 retires. The changelog: "A detached or out-of-bounds array is a `TypeError` to the codecs" |
| `test/built-ins/Uint8Array/prototype/setFromHex/detached-buffer.js` | original, terser | failed | passed | C1 | the same, for `setFromHex` |
| `test/built-ins/Uint8Array/prototype/toBase64/detached-buffer.js` | original, terser | failed | passed | C1 | the same, for `toBase64`, which answered `""` |
| `test/built-ins/Uint8Array/prototype/toHex/detached-buffer.js` | original, terser | failed | passed | C1 | the same, for `toHex`, which answered `""` |

The classification file names each row with its reason in full. **Class C5** lists no row: the owner's
data decision of 2026-10-15 below, the dated data decision the class names, moves no verdict against
the base. The one row it was taken for passes at the base and after (below), and the targeted check
found no other row it moves. **Class C3** lists no row.

**The after run.** Taken by `.github/workflows/test262-evidence.yml` at `1ebde70`, in the rule's
configuration ([`test262/after/run.txt`](test262/after/run.txt)): run
[38712175322](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/38712175322), dispatched
2026-10-15 09:20Z and completed 10:41Z, `--all-script-host-verifiable` over eight shards, terser 5.50.0,
30 seconds per test, no memory limit, four workers, the .NET SDK 10.0.402, on the runner image the base
ran on (ubuntu24 20261004.327.1). It is imported into [`test262/after/`](test262/after/) unchanged, every
file of the workflow's artifact, with [`imported.txt`](test262/after/imported.txt), which records its
digest. **Timeouts.** As at the base, one path timed out in both variants,
`test/staging/sm/regress/regress-1507322-deep-weakmap.js`; the workflow ran it again alone with one
worker at the same commit, and it timed out again in both
([`test262/after/rerun-timeouts.log`](test262/after/rerun-timeouts.log)), so the rows compare timed out
against timed out. The merged totals, which do not decide part A: 94,245 variant cases passed, 1,244
failed, 2 timed out and 1,229 skipped, where the base's were 94,237, 1,252, 2 and 1,229.

**A dispatch that ran nothing.** Run
[38710768494](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/38710768494), dispatched first
for `1ebde70`, was given the commit's short form, which `actions/checkout` reads as a branch or tag name:
every job stopped at its checkout, nothing was built and no test ran. The run above was dispatched with
the full name. Nothing of the failed run is retained.

**The first after run, at `473dc46`: it found a defect, and under the rule it is NOT MET.** The
workflow took it at `473dc46`, the merge that landed SJL-5.5's cut-over, from 2026-10-15 07:07Z to
08:24Z (run [38696639019](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/38696639019)),
in the same configuration. [`test262/first-after-473dc46/`](test262/first-after-473dc46/) keeps it
whole, and [`test262/first-after-473dc46-comparison/`](test262/first-after-473dc46-comparison/) its
comparison with the base, through the same script, reading the same classification file: **96,720
rows, 10 changed, 8 admitted, 2 regressions**. The 8 are the C1 rows above. The 2 are one path in both
variants:

| Path | Variants | Base | At `473dc46` | At `1ebde70` |
|---|---|---|---|---|
| `test/intl402/DateTimeFormat/prototype/format/temporal-objects-no-time-clip-non-latin-numerals.js` | original, terser | passed | failed | passed |

The test formats the largest and the smallest `Temporal.PlainDate`, both outside a `Date`'s range, with
`new Intl.DateTimeFormat("ar-EG", ...)`, and asks with `includes` whether each result holds its day and
month in Arabic-Indic digits, such as `"١٣"` for the 13th. This engine wrote it in Latin digits, `"13"`,
for two reasons: every locale's default numbering system was `latn`, where CLDR gives `ar-EG` `arab`, and
a Temporal value's digits never went through the numbering system, where a `Date`'s did. **At the base
the test passed only by accident**: `String.prototype.includes` compared by the current culture, and a
culture-sensitive search in .NET finds `"١٣"` in `"13"`, which an ordinal one does not (checked with the
.NET 10 runtime). Correction X2, the ordinal search the milestone lists, made the comparison exact, and
the defect showed. A row that passes at the base and fails after is a regression whatever its cause, and
no class admits it, so **the first after run does not meet part A**. That run, at the commit that landed
the cut-over, is the one the rule's words name; section 5 says why this bundle's after run is at
`1ebde70` instead.

**The fix, `1ebde70`.** At the owner's direction: `DateTimeFormat`'s `format` and `formatToParts`, and
the Temporal types' `toLocaleString`, write a Temporal value's digits through the numbering system, as
a `Date`'s were; and each locale's default numbering system is CLDR's, release 47.0.0, by a table that
`scripts/intl/generate_default_numbering_systems.py` generates from the `cldr-json` release's
`cldr-numbers-full` package, pinned by its registry integrity: 54 locales whose default differs from
their truncated parent's, which reproduce all 739 of CLDR's. **The data is a decision**: the owner
decided on 2026-10-15 that this engine takes the default numbering systems from CLDR, recorded in
[`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "A data decision: each locale's default
numbering system is CLDR's (SJL-5.6)", which item SJL-9 moves into the shared locale data. That record
is the dated data decision part A's class C5 names. The changelog lists both changes ("Each locale's
default numbering system is CLDR's"), [`THIRD_PARTY_NOTICES.md`](../../../THIRD_PARTY_NOTICES.md) names
the data under the Unicode License v3, and `DefaultNumberingSystemTests` holds 20 cases. The BuiltIns and
Integration test projects passed at the fix, 3,175 and 5,287 tests.

**The targeted check.** Before the after run, [`test262/fix-check/`](test262/fix-check/) ran
`test/intl402` and the 235 paths outside it that use Intl, 3,559 rows in the original variant, with
the cut-over's shell and the fix's ([`targeted.log`](test262/fix-check/targeted.log),
[`drive.sh`](test262/fix-check/drive.sh)). The fix moves one row, the path above, from failed to
passed, and the fix's shell gives the base run's verdict on all 3,559. A plain join; it decides
nothing.

## 10. The pristine consumer

**Met.** [`engine/pristine-consumer.log`](engine/pristine-consumer.log): the runtime graph of
`Broiler.JavaScript.All` packed from `1ebde70` into 18 local packages, the shared
`Broiler.ECMAScript.Algorithms` among them, restored into a clean consumer from that feed alone and
run: it printed `42`. The shared set's own consumer is section 7's.

## 11. The simplification check

**The deletion check.** [`simplification/deletion-check.log`](simplification/deletion-check.log)
searches each entry of the area's Broiler.JS deletion list (Broiler.VM
`docs/javascript-shared-libraries/milestones/sjl-5-text-algorithms.md`, SJL-5.5) in the file or folder
that held it, with a pattern for what the entry deleted. **Every entry is absent.** The lines
that remain are adapters and comments:

| Entry | What remains | Reading |
|---|---|---|
| `IsWellFormedUtf16` and `ToWellFormedUtf16` | nothing | absent |
| `StringPad`'s builder | `StringPad`, the adapter over the library's `StringPadding.Pad` under this engine's length policy, its two callers (`padEnd`, `padStart`) and its comment; the one `StringBuilder` is a comment in `repeat`, which writes its result directly | the builder is absent; the adapter stays, as [`docs/public-api.md`](../../public-api.md) says |
| `JSString.GetSubstitution` and the initializer's local `GetSubstitution` | nothing | absent |
| The `UriHelper` class | a comment in an integration test that names the deletion | absent |
| The base64 and hex cores of `JSUInt8Array` | `GetBase64Alphabet`, the adapter's reading of the `alphabet` option into the library's `Base64Alphabet`, which correction B3 changed, and its three callers | the cores are absent; the option reading is the adapter's |
| `JSJSON.QuoteString` | nothing | absent |
| `JSJsonParser`'s use of `System.Text.Json` for guest JSON, with its exception mapping | a comment saying what the reader was until SJL-5.5 | absent |

**The public dispositions** are recorded in [`docs/public-api.md`](../../public-api.md), "Public API
changes, 2026-10-15": `JSRegExp.Replace(string, string)` stays, its body over the shared template
parser (SJL-D-25), with `JSRegExp.Replace(string, JSValue)` and the delegate `JsonParserReceiver`
unchanged; no public member is removed.

**The duplicate search.** [`simplification/duplicate-search.log`](simplification/duplicate-search.log)
searches the engine's product sources (`Broiler.JS/`, test projects, benchmarks and samples excluded)
for the constructs that would mean a surviving copy of the area: UTF-16 read or written by the platform
or by the surrogate constants, padding by the platform, base64, hex and URI by the platform, JSON by
`System.Text.Json`, a `\uXXXX` escape written by format string, and the line terminators and byte order
mark spelled as code. Broiler.VM's bundle of the same name searches its own sources with the same
pattern. **154 hits at `1ebde70`; none is a copy the area's deletion list or the correspondence names**:

| Hits | Where | What they are |
|---|---|---|
| 78 | `JSRegExp` (`RegExp/JSRegExp.cs`, 72 hits less `RegExp.escape`'s 3; `JSRegExp.UnicodeSets.cs`, 8; `JSRegExp.Broiler.cs`, 1) | the pattern translator's own readers: it rewrites a pattern's surrogates, code-point escapes, `\p` classes and ranges for .NET's engine, which matches per code unit; the RegExp area is SJL-11's |
| 3 | `JSRegExp.Escape` and its helpers (`RegExp/JSRegExp.cs`) | `RegExp.escape`'s code-point read and its `\uXXXX` escape of a lone surrogate, which the correspondence leaves to SJL-11, as Broiler.VM's bundle does its own |
| 3 | `RegExpValidator` (`Parser/`) | the early errors of a pattern literal's surrogate escapes, SJL-11's |
| 15 | `FastScanner` (10), `CharExtensions` (3), `FastParser.Import` (2) (`Parser/`) | the lexer's code-point reads and its identifier and escape tests (SJL-13), and `ModuleExportNameLiteral`'s early error on an export name that is not well formed (SJL-15), which the correspondence names among the scans outside the area |
| 2 | `JSStringStatic.FromCodePoint` | writing a code point as UTF-16, which the library does not do: its `Utf16` members read code points (`CodePointAt`, `IsWellFormed`, `ToWellFormed`, `AdvanceStringIndex`) |
| 13 | `JSIntl` (4), `JSIntlDateTimeFormatEngine` (1), Temporal's `Instant` (2), `PlainDateTime` (1), `PlainTime` (1), `ZonedDateTime` (3), `JSDateStatic` (1) | zero-padded fraction digits in Intl's, Temporal's and `Date.parse`'s own number reading and writing, not `StringPad`, and Intl's `MapDigit`, which writes a numbering system's digit (SJL-6, SJL-9, SJL-10) |
| 21 | `V8InspectorProtocol` (11), `V8InspectorProtocolProxy` (4), `AssemblyCodeCache` (3), `BroilerJSContext` (2), `JSModuleContext` (1) | the host's own JSON: the debugger's protocol messages, the code cache's manifest, the dependency list and a package's `package.json`, which no script reads through `JSON` |
| 7 | `AssemblyCodeCache` (6), `ILWriter` (1) | hashes in hexadecimal, and an IL offset in a debug listing |
| 2 | `FormData` (`Network/`) | `application/x-www-form-urlencoded` serialization of a form's pairs, a host API's encoding and not `encodeURIComponent` |
| 6 | `modules/inbuilt/buffer.js` | Node's `Buffer` module in JavaScript, its own UTF-8 coding; not the engine's code |
| 1 | `JSJsonParser` | a comment saying what the reader was until SJL-5.5 |
| 3 | `Broiler.JS/CHANGELOG.md` | the corrections' text |

No clause of SJL-5 reads the duplicate search. Among its hits is none of the copies the correspondence
names for this engine: `JSString.CodePointAt`, the String iterator's `CodePointEnumerator.TryNext` and
the seven `AdvanceStringIndex` copies each call the library now.

## 12. What this bundle does not claim

- **Nothing about Broiler.VM**, whose half is its own bundle, judged by its own rule.
- **A speed claim of any kind.** SJL-5 measures nothing; the risk the area design names for this
  engine's JSON parsing speed when it leaves System.Text.Json is answered, if at all, by a later
  measurement under its own rule.
- **Review**: the reviewer field says none and means it.
