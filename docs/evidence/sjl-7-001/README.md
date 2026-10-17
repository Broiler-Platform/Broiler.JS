# Evidence bundle SJL-7-001 (Broiler.JS) - the Unicode area

**Milestone:** SJL-7 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-7-unicode.md`: its Broiler.JS clause 7, and the
measurement its work package SJL-7.6 asks of this engine.

**Rule:** [`decision-rule.md`](decision-rule.md), committed at SJL-7.1 in `3916bd2b` before any code of the
area: part A is test262 per path with negative-metadata tests, and part B is the measurement over
`UnicodeBenchmarks`.

**Area verdict:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", with Broiler.VM's
JSD-0074 and its correspondence from code, retained in Broiler.VM's bundle of the same name.

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in
Broiler.VM). **Reviewer:** none, and nothing here has been read by anyone but its author.

**State, 2026-10-17, at SJL-7.6: the after arm is collected, from clean checkouts of the after commit `a34b1f78`,
which lands SJL-7.5's cut-over (section 4).** **Clause 7 is met under part A of [`decision-rule.md`](decision-rule.md)
as written**, with no revision: of the whole suite's 106,058 rows, 140 changed, each from failed to passed and each a
correction of class C1 the changelog lists, and none regressed (section 6). Clauses 5, 6 and 8 are met as section 5
reads them, with findings that section 9 names for other milestones. Every vector of corpus revision 20 has the
result its class expects at the after commit (section 10).
- **Part B, the measurement, is void**: this machine is not a controlled lane. Its arithmetic finds `Normalize`
  about three times and `Casing` about twice as slow after the cut-over, far beyond the A/A floor, and
  `Identifiers` unchanged (section 11). JSD-0031's fifth reason stays open for this engine.

Nothing is accepted: review is deferred, and SJL-7 may not meet its gate before SJL-3 meets SJL-3's.

*(Revised 2026-10-16, at SJL-7.3. The state's heading read "at SJL-7.2: part A's base is taken, and nothing here is
a verdict." Revised again at SJL-7.5; it read "at SJL-7.3: part A's base is taken, and the engine-level vectors'
first run is classified; nothing here is a verdict.", and its first item "Part A's after run follows SJL-7.5's
cut-over.")*

*(Revised 2026-10-17, later at SJL-7.6, when sections 10 and 11 were added. The heading read "collection in
progress", and its list "Not yet taken:" named part B and the engine-level vectors at corpus revision 20.)*
*(Revised 2026-10-17, at SJL-7.6. The state read "**State, 2026-10-16, at SJL-7.5: part A's base is taken, the
engine-level vectors' first run is classified, and this engine's cut-over is recorded with its run on the vectors;
nothing here is a verdict.**", with the items "Part A's after run is taken at SJL-7.6, at the commit that lands the
cut-over." and "Part B's arms are measured at SJL-7.6, when the after arm exists, because the rule interleaves the
arms inside each repetition.")*

Broiler.Unicode judges the owner's clauses in its own bundle of the same name, and Broiler.VM judges its own
half in its own. This bundle reads neither's runs.

## 1. Part A's base

| | |
|---|---|
| Commit | `3916bd2b`, the commit that carries the rule, before any code of SJL-7 landed. Against SJL-6's after commit `4d70821e`, it changes four files outside the documents: the workflow's `include_negative` input, and part B's workloads and their `--unicode-answers` in the benchmark project, all of which the rule commits |
| Taken | 2026-10-16, 15:22Z to 16:59Z, by `.github/workflows/test262-evidence.yml` ([run 38923462925](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/38923462925), attempt 1). It was dispatched from this branch at `3916bd2b` itself, so the workflow is the rule commit's, and the commit was named by all forty digits ([`run.txt`](test262/base/run.txt)) |
| Configuration | The rule's: eight shards over `--all-script-host-verifiable` with `--include-negative`, terser 5.50.0 at its pinned version, the per-test timeout of 30 seconds, no memory limit, four workers, and the suite at `ccaac100`. `time_zone` was empty, and `includeNegative` is true in `run.txt`, in the merged results and in the re-run |
| Runner | Image `ubuntu24` 20261012.334.1, Linux X64, four logical processors, .NET 10.0.402 |
| Retained | [`test262/base/`](test262/base/): the workflow's artifact `test262-evidence`, every file unchanged. [`imported.txt`](test262/base/imported.txt) records the digest of the zip as downloaded, which is the digest GitHub records |
| Complete | All eight expected shards reported; none is incomplete, none was retried, and there is no configuration failure. The merged results name one suite revision ([`merge.log`](test262/base/merge.log), and `suiteRefs` in `test262-results.json.gz`) |
| Zone | Every shard in the expected zone: TZ unset, UTC +0000 at both instants, tzdata 2026c ([`host-zone.log`](test262/base/host-zone.log)) |
| Shell | One digest of the shell's binaries across the eight shards and the re-run ([`shell-hashes.log`](test262/base/shell-hashes.log)) |
| Timed out | One path, `test/staging/sm/regress/regress-1507322-deep-weakmap.js`, in both variants. Run again alone with one worker, it timed out again, and the re-run replaces its rows ([`rerun-timeouts.log`](test262/base/rerun-timeouts.log)) |

**What the run holds.** These figures are a reading; under the rule, totals decide nothing.
- 53,029 paths in two variants: 106,058 rows.
- Original: 51,410 passed, 1,618 failed, 1 timed out.
- Terser: 46,596 passed, 561 failed, 1 timed out, 5,871 skipped.

**Against SJL-6's after run.** That run is bundle `sjl-6-001`'s, taken at `4d70821e` without negative tests.
- The 96,720 rows the two runs share have the same verdict in both.
- The negative-metadata tests add 4,669 paths, 9,338 rows: 3,761 passed, 935 failed and 4,642 skipped.

So this run is SJL-6's after run, plus the negative-metadata tests, as it should be for the same product
sources. No earlier run stands for this base, because none included them: the rule says so.

Part A's after run is taken at the commit that lands SJL-7.5's cut-over, by the same workflow with the same
inputs. `scripts/compliance/compare_test262_paths.py` then compares it with this base, path by path and variant
by variant.

## 2. Engine-level vectors: the first run on the Unicode area (SJL-7.3)

Corpus revision 18 (`b69eb709`) adds the Unicode area: 37,738 vectors in five files under
`conformance/vectors/unicode/`, from `scripts/conformance/generate_unicode_vectors.py`. The engine-level runner,
`Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release, ran on it at that commit under `TZ=UTC`, as this
repository's tests run. None of the area's vectors depends on the time zone.

| Run | Result | Files |
|---|---|---|
| Revision 18, UTC | 88,465 vectors: 84,783 match, 4 classified divergences, 3,678 unclassified differences | [`engine/run-revision-18-utc.log`](engine/run-revision-18-utc.log), [`engine/run-revision-18-utc.tsv.gz`](engine/run-revision-18-utc.tsv.gz) |
| Its negative control | one vector's expected value changed in memory, reported as an unclassified difference | [`engine/negative-control-revision-18.tsv`](engine/negative-control-revision-18.tsv) |

The 4 are the number area's `strict-non-octal-decimal` fields, which stay with SJL-14. Every one of the 3,678
is in the Unicode area. Broiler.VM's run on the same revision matched every vector; its bundle of the same name
retains it.

**Corpus revision 19 classifies the 3,678.** Each is filed under the row of bundle `sjl-7-001`'s correspondence
from code (Broiler.VM's `docs/evidence/sjl-7-001/correspondence.md`) that it confirms, and SJL-7.5 answers each
one. The reasons are in `scripts/conformance/unicode-divergences.txt`.

| Row | Vectors | What this engine does |
|---|---|---|
| P1 | 1,346 | Admits `\p{…}` names that ECMA-262's tables do not hold. Its lookup ignores case, underscores, spaces and hyphens |
| P2 | 20 | Refuses the Script value `Hrkt` (`Katakana_Or_Hiragana`) |
| S2 | 1 | Resolves the names of the properties of strings loosely, as P1 |
| I1 | 2 | Admits U+2E2F, which is Pattern_Syntax, in an escaped identifier |
| I3 | 6 | Admits U+2E2F in a group name and refuses U+11B60 there |
| W1 | 3 | Treats U+0085 as white space, in source text and in `RegExp.escape` |
| C1 | 18 | Does not treat a sigma after U+00AA or U+00BA as final |
| C3 | 50 | Leaves Beria Erfe's case pairs unmapped. Unicode 17 adds them, and its hand patches lack them |
| C4 | 4 | Its Turkic lower case reads the runtime's older data |
| C5 | 5 | Its Soft_Dotted set lacks U+1DF1A, U+1E04C, U+1E04D and U+1E068 |
| C6 | 2 | Passes over U+0903, a mark of class 0, in a casing context |
| C7, C8 | 2, 1 | Hands a list of locales to `CultureInfo`, and reads `null` as the host culture |
| N1 | 50 | Throws on a lone surrogate in `normalize` and `localeCompare` |
| N2 | 582 | Normalizes with the host's ICU, at Unicode 15.1. Every one of these vectors reads a code point that Unicode 16 or 17 adds |
| G1 | 138 | Answers a text as one sentence |
| G2 | 684 | Word boundaries that are not UAX #29's |
| G2, G4 | 747 | Each segment's `isWordLike`, over those word boundaries |
| G3 | 17 | Grapheme clusters from the runtime's older data |

Every difference falls under a row the area verdict names. One row, C3, read the default mappings as agreeing at
Unicode 17. The Beria Erfe vectors show that it does not, and Broiler.VM's correspondence is revised for them.

**On revision 19** (`c976cbea`), the same runner, at that commit, from a clean tree, under `TZ=UTC`:

| Run | Result | Files |
|---|---|---|
| Revision 19, UTC | 88,465 vectors: 84,783 match, 3,682 classified divergences, no unclassified difference, and no divergence that no longer holds | [`engine/run-revision-19-utc.log`](engine/run-revision-19-utc.log), [`engine/run-revision-19-utc.tsv.gz`](engine/run-revision-19-utc.tsv.gz) |
| Its negative control | one vector's expected value changed in memory, reported as an unclassified difference | [`engine/negative-control-revision-19.tsv`](engine/negative-control-revision-19.tsv) |

Each of the 3,678 Unicode-area divergences holds until this engine's cut-over, SJL-7.5. A run after it reports
each one it corrects as no longer holding, and the corpus revision that follows retires them.

## 3. This engine's cut-over (SJL-7.5)

**The change**, on `0.1.0-preview.4` of `Broiler.Unicode.Algorithms` and `Broiler.Unicode.Segmentation` as
nuget.org serves them, over the tables this engine embeds from its own pin, `data/unicode/pins/`:

| Commit | What it moves | Rows |
|---|---|---|
| `3374c933` | The pinned archive, a byte-for-byte copy of the owner's; the tables the owner's generator writes from it, in `Broiler.JavaScript.Runtime`, and the test that regenerates them and compares them byte for byte; the package references | SJL-D-6 (c) |
| `3e213ab4` | Identifiers, group names, the scanner's white space, `RegExp.escape`'s and the native names onto the owner's lexical classes; `CharExtensions`' classes and patch tables deleted | I1 to I4, W1 |
| `664c804e` | `StringSpan.Trim`, `TrimStart` and `TrimEnd` become obsolete forwarders to the shared white-space set | SJL-D-25 |
| `99d84a54` | `normalize` and the collator's decomposition onto the owner's normalization | N1 to N3 |
| `c7423e7c` | The four casing methods onto the owner's case mapping, the locale mapped to the owner's language by this engine's canonicalisation | C1 to C8 |
| `876c4f66` | `\p{…}` names onto the owner's exact lookup, checked by the parser and by the `RegExp` constructor before either engine reads the pattern | P1 to P4, S2 |
| `294c8a5d` | `Intl.Segmenter`'s boundaries and word status onto `UnicodeSegmenter` | G1 to G4 |
| `c7a19d35` | The records: the changelog, `docs/public-api.md`, `docs/architecture/dependencies.md`, the notices | |

A later commit of the same change adds rows P3 and P4, which no vector names, to the changelog; it changes
no product source. Every correction is listed in [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md),
with its row and the vectors that name it. **Rows F2 and W5 are untouched**: no hunk of the change falls in
the translator's case folding, its multiline anchors, its white-space list or the scanner's line terminators,
which are SJL-11's.

**On corpus revision 19**, at `c7a19d35`, from a clean tree, under `TZ=UTC`, 22:09Z to 22:49Z:

| Run | Result | Files |
|---|---|---|
| Revision 19, UTC | 88,465 vectors: 88,461 match, 4 classified divergences, 0 unclassified differences, 0 not implemented | [`engine/cut-over-revision-19-utc.log`](engine/cut-over-revision-19-utc.log), [`engine/cut-over-revision-19-utc.tsv.gz`](engine/cut-over-revision-19-utc.tsv.gz) |
| Its negative control | one vector's expected value changed in memory, reported as an unclassified difference | [`engine/negative-control-cut-over-revision-19.tsv`](engine/negative-control-cut-over-revision-19.tsv) |

**Each of the 3,678 fields of section 2 reports "divergence no longer holds"**, and they are exactly the
fields `unicode-divergences.txt` lists. The 4 are the number area's `numbers/literal/0082`, `0084`, `0088` and
`0090`, as before. The runner fails on the 3,678, as it must until the corpus retires them.

**The milestone's searches**, at `c7a19d35`, each with its command as its first line:

- **The deletion list is absent** ([`cutover/deletion-search.txt`](cutover/deletion-search.txt)): the search
  for its members finds nothing under `Broiler.JS/`, the changelog included.
- **No host Unicode API is left in the areas covered** ([`cutover/host-api-search.txt`](cutover/host-api-search.txt)).
  Its 315 lines are each outside them:

  | Where | Lines | Whose |
  |---|---|---|
  | The .NET translator's case folding, `JSRegExp.cs` 4891, 4902, 5014, 5067, 5076, 5080 and 5087 | 7 | Row F2, SJL-11 |
  | Intl: the host culture as the default locale (`JSIntl.cs:2380`), locale tags' ASCII casing, number, unit and date-time formatting, the date-time pattern reader (`JSIntlDateTimeFormatEngine.cs` 551, 586); `toLocaleString` of Number and Decimal | most of `JSIntl.cs`, `JSIntlDateTimeFormatEngine.cs`, `JSNumber.cs`, `JSNumberPrototype.cs`, `JSDecimal.cs` | SJL-9 |
  | The collator's tie-break, `JSIntl.cs:6230` | 1 | SJL-17 |
  | Temporal's invariant formatting, and its time-zone and calendar names' casing | `Temporal/` | SJL-8, SJL-10 |
  | `DateParser`'s culture parse and `toLocaleString`'s .NET format strings | `DateParser.cs`, `JSDatePrototype.Formatters.cs` | This engine's extensions, decided at SJL-6.5 (SJL-D-17) |
  | The lexer's decimal-digit lookahead after `?.` and `.`, `FastScanner.cs` 627 and 651, and `NumberParser.cs:32` | 3 | The lexer, SJL-14. `char.IsDigit` admits every Nd digit where the grammar admits ASCII; run in the shell after the cut-over, `a ?.٣ : b`, `.٣` and `a?.٣` are SyntaxErrors and `Number("0٣")` is NaN, as the grammar's reading would have them |
  | Invariant-culture formatting of numbers and hex, which reads no Unicode data: Compiler, PortableCompiler, Runtime's `JSVariable.cs`, Modules, JSON, `JSArray.Template.cs`, `JSRegExp.cs` 1979, 1987, 1993 and 4383; the `ToLocaleString(string, CultureInfo)` overrides; a diagnostic's text, `NullishAccess.cs:321`; the Native AOT sample's `bool` | the rest | Outside the area |
  | The changelog's lines naming the deleted code | 7 | Records |

**Tests.** `eng/run-tests.sh Release` under `TZ=UTC` at `c7a19d35`: 17 assemblies, 11,340 tests, none failed
([`cutover/run-tests.log`](cutover/run-tests.log)). One existing test changed: `BuiltInsTests`' unsupported
property escape asserted the translator's "not supported" message, and a `u`-mode `\p{RGI_Emoji}` is now an
early error, so it asserts the SyntaxError. The corpus's Python tests pass.

**Part B's answers are unchanged.** `--unicode-answers` printed the same answers and the same configuration
block at `c976cbea` and at `c7a19d35`, byte for byte ([`cutover/benchmark-answers-base.txt`](cutover/benchmark-answers-base.txt),
[`cutover/benchmark-answers-after.txt`](cutover/benchmark-answers-after.txt)): Casing and its control
24,000, Identifiers and its control 200, Normalize and its control 24,000.

**A local subset of test262, before the hosted after run.** Not part A's evidence, which is the hosted run at
the commit that lands this change; a check that no row regresses, taken before the change lands. The suite at
`ccaac100`, `--all-script-host-verifiable --include-negative`, both variants, three workers, on this machine,
with the shell built at `c976cbea` and at `c7a19d35`, over part A's likely-affected directories in two parts:

| Part | Rows | Changed | Comparison |
|---|---|---|---|
| `language/{statements,expressions}/class/elements/` | 5,924 | 0 | [`cutover/test262-subset/compare-class-elements.json`](cutover/test262-subset/compare-class-elements.json) |
| `built-ins/RegExp/{property-escapes,unicodeSets,named-groups,escape}`, `built-ins/RegExp/prototype/unicodeSets`, `language/{identifiers,white-space,line-terminators,comments,literals/regexp}`, `built-ins/String/prototype/{normalize,toLowerCase,toUpperCase,toLocaleLowerCase,toLocaleUpperCase,localeCompare}`, `intl402/{String/prototype,Collator,Segmenter}`, `built-ins/Function/prototype/toString`, `staging/sm/{RegExp,String}` | 4,010 | 140, each failed → passed | [`cutover/test262-subset/compare-unicode-directories.json`](cutover/test262-subset/compare-unicode-directories.json) |

`scripts/compliance/compare_test262_paths.py` reports no regression; the 140 are admitted as class C1 by
[`cutover/test262-subset/classification.json`](cutover/test262-subset/classification.json), all in the original
variant: 136 negative tests under `built-ins/RegExp/property-escapes/` that the parser now refuses (rows P1, P3
and S2), and 4 under `language/identifiers/vertical-tilde-*` (row I1). The local base agrees with the hosted
base of section 1 on 9,926 of these 9,934 rows; the 8 others are the terser variant of the `*-unicode-17.0.0*`
identifier tests, which this machine skips before and after. **Seven rows are unchanged that a fuller check
would correct**: `generated/strings/*-negative-CharacterClass.js`, a property of strings inside a negated
class, which the parser's check does not refuse, because refusing a direct member of a negated class would
refuse valid patterns such as `[^\p{RGI_Emoji}&&a]`; the `RegExp` constructor still refuses
`[^\p{RGI_Emoji}]`. They fail before and after.

## 4. The after arm (SJL-7.6)

| Field | Value |
|---|---|
| Evidence bundle | SJL-7-001 (Broiler.JS's) |
| Base | Section 1: the workflow's run at `3916bd2b`, the commit that carries the rule, before any code of SJL-7 landed, with its re-run of the rows that timed out |
| After | `a34b1f78` (`a34b1f78cc4bda6bcdfe68ed943af6cc069c1b71`), on this branch: SJL-7.5's cut-over, `3374c933` to `c7a19d35`, and the three commits after it, which add the changelog's rows P3 and P4 (`93f729c5`), this bundle's record of the cut-over and the plan's (`97de502e`), and corpus revision 20 (`a34b1f78`). **Its product sources are `c7a19d35`'s**: under `Broiler.JS/`, `Broiler.ECMAScript/` and `data/unicode/`, and among the package and build properties, `NuGet.config` and `global.json`, the one file that differs is `Broiler.JS/CHANGELOG.md`, a document ([`identity.txt`](identity.txt), which lists every commit and every file). Unlike SJL-6's after commit it is not a merge into the default branch: part A's after run was dispatched from this branch at this commit, named by all forty digits, as the base was at the rule's. Every local run of this bundle is taken from a clean checkout of it |
| Rule commit | `3916bd2b`, an ancestor of the after commit. The rule, the comparison script, the workflow and part B's three files, `UnicodeBenchmarks.cs`, the benchmark `Program.cs` and `BenchmarkConfiguration.cs`, are unchanged between the two, by blob; no file of the benchmark project changed ([`identity.txt`](identity.txt)) |
| Owner | Broiler.Unicode's `Broiler.Unicode.Algorithms` and `Broiler.Unicode.Segmentation` `0.1.0-preview.4`, published to nuget.org from Broiler.Unicode's `6e14f93`, over the Unicode 17.0.0 tables this engine embeds from its own pin (section 7) |
| Shared vectors | `conformance/vectors` at corpus revision 20, which retires this engine's 3,678 divergence fields of the Unicode area ([`engine/retired-divergences-revision-20.tsv`](engine/retired-divergences-revision-20.tsv)) |
| Collector | [`collect.py`](collect.py) as this change commits it, which reuses bundle `sjl-6-001`'s collector, and through it `sjl-5-001`'s, `sjl-4-001`'s, `sjl-3-001`'s and `sjl-0-001`'s, run against the clean checkout; its `measure` step, part B's, follows `sjl-3-001`'s |
| Taken | 2026-10-16 and 17. Part A's after run by the workflow, dispatched 23:56Z and completed at 01:31Z on 2026-10-17; locally, `identity`, `simplification`, `home` and `consumer` from 00:11Z on 2026-10-17 (the last three taken again at 00:19Z with the collector as committed), `test262` and `compare` at 01:33Z. The machine is section 3's, and it ran an engine-level vector run and work in Broiler.VM beside these steps; nothing local here is timed |
| RID | `linux-x64`: the workflow's runners for test262; locally, one machine with four logical processors |

## 5. The clauses, and what holds each

| Clause | What it asks | Where | State |
|---|---|---|---|
| 5 | The deletion list is absent | section 9 | **Met** at `a34b1f78`: every entry of the milestone's list for this engine, with JSD-0074's three additions, is absent from the file or folder that held it; what remains is calls to the owner, adapters and comments, and the cut-over's search for the members' names, repeated, finds nothing |
| 6 | No host Unicode API remains in the areas covered | section 9 | **Met as the area record bounds the areas**: the cut-over's search, repeated at the after commit, finds the same 315 lines, each outside the areas covered, as section 3 lists them. The wider search for a duplicate of the owner's API finds three things the record leaves to other milestones or that lie outside the area: the translator's General_Category inside a class (SJL-11), the line terminators spelled in the scanner and the translator (SJL-14, SJL-11), and the CLR binding's names cased in the host's culture; section 9 names each |
| 7 | test262 per path meets the area rule | section 6, [`test262/`](test262/) | **Met** under part A of the rule as written, at `a34b1f78`: both runs complete and comparable; of 106,058 rows of the whole suite, 140 changed, each from failed to passed, each a correction of class C1 that the changelog lists (rows P1, P3 and I1); none regressed, and C3 and C5 list no row |
| 8 | Every correction is listed | [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md) | **Met**: every correction the area record names for this engine, under its row of the correspondence, P1 to P4, S2, I1, I3, W1, C1, C4 to C8, N1, G1 and G2, with the data-policy rows I2, C2, C3, N2, N3 and G3 and the word status G4; and every row of section 6 in class C1 falls under one of them |

**SJL-7.6's measurement**, part B of the rule, which answers JSD-0031 §4(b)'s fifth reason for this engine: **void**, on a lane that is not controlled, and its arithmetic finds `Normalize` and `Casing` far slower after the cut-over (section 11); the reason stays open for this engine. Clauses 1 to 4 are the owner's, judged in Broiler.Unicode's bundle of the same name, and 9 to 13 are Broiler.VM's, judged in its own. None is accepted: review is deferred, and SJL-7 may not meet its gate before SJL-3 meets SJL-3's.

## 6. Test262 per path (rule part A)

**MET under part A of the rule as written, at the after commit `a34b1f78`; no row regressed.**
[`test262/comparison/comparison.json`](test262/comparison/comparison.json) and
[`test262/comparison/comparison.log`](test262/comparison/comparison.log): the after run against section 1's base,
through `scripts/compliance/compare_test262_paths.py` as the rule committed it at SJL-0.9, with each run's
timed-out rows replaced by their re-runs and [`test262/comparison/classifications.json`](test262/comparison/classifications.json)
read. **106,058 rows (53,029 paths, each in both variants): 140 changed, every one failed at the base and
passes after, every one admitted as C1; 0 unclassified, 0 regressions, and no classification the comparison did
not use.** The script exits 0.

| Part A's condition | This run |
|---|---|
| Both runs complete | eight of eight expected shards reported in each, none incomplete or retried, no configuration failure, one suite revision, `ccaac100` ([`test262/comparison/completeness.log`](test262/comparison/completeness.log), read from each run's own files; [`test262/after/merge.log`](test262/after/merge.log)) |
| One shell in each | the hashes of the shell's binaries are the same in every shard and in the re-run, in each run ([`test262/after/shell-hashes.log`](test262/after/shell-hashes.log)). The two runs' shells differ, as they must: the after run's carries `Broiler.Unicode.Algorithms.dll` and `Broiler.Unicode.Segmentation.dll`, each byte for byte the assembly of the package nuget.org serves (section 7), and not the generator ([`test262/after/shell-binaries.sha256`](test262/after/shell-binaries.sha256)) |
| Every shard in the zone, and the two records alike | `TZ` unset, UTC +0000 at both instants, tzdata 2026c, in every shard of each run, and the records' lines the same ([`test262/comparison/zone-records.log`](test262/comparison/zone-records.log)) |
| `includeNegative` true in both | in `run.txt`, in the merged results and in the re-run, in each run |
| The script finds them comparable | yes ([`test262/comparison/compare-checks.log`](test262/comparison/compare-checks.log): the after run against itself changes nothing, and a planted regression is reported, exit 1); the fields it records and does not compare are the same in both: .NET SDK 10.0.402, four workers, eight shards, Linux, X64 |
| No row passing at the base fails, times out or is skipped after | none |
| Every changed verdict in C1, C3 or C5, and listed | **met**: the 140 rows below, all C1, each with its path, its variant, its correspondence row and the specification clause in the classification file; each row's correction is in the changelog. Classes C3 and C5 list no row |

**The changed rows.** Every one is a negative-metadata test (`phase: parse`, `type: SyntaxError`), in the
original variant: the terser variant skips negative tests, 5,871 rows skipped in both runs. Each test was read
before it was classified. They are, path for path, the 140 rows SJL-7.5's local subset found (section 3): the
hosted run finds no other change anywhere in the suite. The classification file gives each family's reason in
full; in short:

- **Families A to G, 122 rows: rows P1 and P3.** The changelog's "`\p{…}` names are matched exactly" ("A
  literal's is an early error") and "A name no table holds never reaches .NET's `\p`". ECMA-262 22.2.1.1 makes a
  UnicodePropertyValueExpression an early error unless its name, and its value, are in the tables that
  UnicodeMatchProperty and UnicodeMatchPropertyValue read (22.2.2.9), exactly as written. At the base the parser
  replaced each `\p{…}` with a placeholder and checked no name, and the lookup behind it was loose; after, the
  parser checks each name with Broiler.Unicode's exact lookup (`JSUnicodePropertyNames`).
- **Family H, 14 rows: row P1's parse-time check, for the properties of strings.** `\p{…}` naming a property of
  strings is an early error without `v`, and `\P{…}` of one with it (22.2.1.1). The `RegExp` constructor refused
  both at the base already (row S1, an agreement); the parser now does, by the owner's lookup of the properties of
  strings.
- **Family I, 4 rows: row I1**, the changelog's "An identifier subtracts Pattern_Syntax". U+2E2F VERTICAL TILDE is
  Lm and Pattern_Syntax, so neither ID_Start nor ID_Continue (UAX #31; ECMA-262 12.7), literally or as an escape;
  the scanner's classes are now the owner's `EcmaScriptLexical` over the pinned tables.

| Family | Rows | What the test writes | Row |
|---|---|---|---|
| A | 14 | `\p{ASCII=...}`: a binary property with a value | P1, P3 |
| B | 18 | an extension of the grammar: `In` or `Is`, `^`, `:`, a bare `=` | P1, P3 |
| C | 28 | a name or value matched loosely | P1 |
| D | 12 | a non-binary property without a value | P1, P3 |
| E | 6 | a property or value no table holds | P3 |
| F | 36 | a binary property ECMA-262's table does not list | P1, P3 |
| G | 8 | `Block`, `Line_Break`, `FC_NFKC_Closure` | P1, P3 |
| H | 14 | a property of strings under `u`, or negated under `v` | P1 (S1 at run time) |
| I | 4 | U+2E2F in an identifier | I1 |

| Path | Variant | Base | After | Class | Family | Row |
|---|---|---|---|---|---|---|
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_F-negated.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_F.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_Invalid-negated.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_Invalid.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_N-negated.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_N.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_No-negated.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_No.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_T-negated.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_T.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_Y-negated.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_Y.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_Yes-negated.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/binary-property-with-value-ASCII_-_Yes.js` | original | failed | passed | C1 | A | P1, P3 |
| `test/built-ins/RegExp/property-escapes/generated/strings/Basic_Emoji-negative-P.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/Basic_Emoji-negative-u.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/Emoji_Keycap_Sequence-negative-P.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/Emoji_Keycap_Sequence-negative-u.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji-negative-P.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji-negative-u.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji_Flag_Sequence-negative-P.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji_Flag_Sequence-negative-u.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji_Modifier_Sequence-negative-P.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji_Modifier_Sequence-negative-u.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji_Tag_Sequence-negative-P.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji_Tag_Sequence-negative-u.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji_ZWJ_Sequence-negative-P.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/generated/strings/RGI_Emoji_ZWJ_Sequence-negative-u.js` | original | failed | passed | C1 | H | P1 (S1 at run time) |
| `test/built-ins/RegExp/property-escapes/grammar-extension-In-prefix-Block-implicit-negated.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-In-prefix-Block-implicit.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-In-prefix-Script-implicit-negated.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-In-prefix-Script-implicit.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-In-prefix-Script-negated.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-In-prefix-Script.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-Is-prefix-Script-negated.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-Is-prefix-Script.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-circumflex-negation-negated.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-circumflex-negation.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-invalid-negated.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-invalid.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-separator-and-value-only-negated.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-separator-and-value-only.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-separator-negated.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-separator-only-negated.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-separator-only.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/grammar-extension-separator.js` | original | failed | passed | C1 | B | P1, P3 |
| `test/built-ins/RegExp/property-escapes/loose-matching-01-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-01.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-02-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-02.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-03-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-03.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-04-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-04.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-05-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-05.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-06-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-06.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-07-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-07.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-08-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-08.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-09-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-09.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-10-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-10.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-11-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-11.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-12-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-12.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-13-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-13.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-14-negated.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/loose-matching-14.js` | original | failed | passed | C1 | C | P1 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-General_Category-equals-negated.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-General_Category-equals.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-General_Category-negated.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-General_Category.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-Script-equals-negated.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-Script-equals.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-Script-negated.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-Script.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-Script_Extensions-equals-negated.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-Script_Extensions-equals.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-Script_Extensions-negated.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-binary-property-without-value-Script_Extensions.js` | original | failed | passed | C1 | D | P1, P3 |
| `test/built-ins/RegExp/property-escapes/non-existent-binary-property-negated.js` | original | failed | passed | C1 | E | P3 |
| `test/built-ins/RegExp/property-escapes/non-existent-binary-property.js` | original | failed | passed | C1 | E | P3 |
| `test/built-ins/RegExp/property-escapes/non-existent-property-and-value-negated.js` | original | failed | passed | C1 | E | P3 |
| `test/built-ins/RegExp/property-escapes/non-existent-property-and-value.js` | original | failed | passed | C1 | E | P3 |
| `test/built-ins/RegExp/property-escapes/non-existent-property-existing-value-negated.js` | original | failed | passed | C1 | E | P3 |
| `test/built-ins/RegExp/property-escapes/non-existent-property-existing-value.js` | original | failed | passed | C1 | E | P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Composition_Exclusion-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Composition_Exclusion.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Expands_On_NFC-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Expands_On_NFC.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Expands_On_NFD-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Expands_On_NFD.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Expands_On_NFKC-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Expands_On_NFKC.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Expands_On_NFKD-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Expands_On_NFKD.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-FC_NFKC_Closure-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-FC_NFKC_Closure.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Full_Composition_Exclusion-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Full_Composition_Exclusion.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Grapheme_Link-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Grapheme_Link.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Hyphen-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Hyphen.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Alphabetic-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Alphabetic.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Default_Ignorable_Code_Point-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Default_Ignorable_Code_Point.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Grapheme_Extend-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Grapheme_Extend.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_ID_Continue-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_ID_Continue.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_ID_Start-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_ID_Start.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Lowercase-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Lowercase.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Math-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Math.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Uppercase-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Other_Uppercase.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Prepended_Concatenation_Mark-negated.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-binary-property-Prepended_Concatenation_Mark.js` | original | failed | passed | C1 | F | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-property-Block-with-value-negated.js` | original | failed | passed | C1 | G | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-property-Block-with-value.js` | original | failed | passed | C1 | G | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-property-FC_NFKC_Closure-negated.js` | original | failed | passed | C1 | G | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-property-FC_NFKC_Closure.js` | original | failed | passed | C1 | G | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-property-Line_Break-negated.js` | original | failed | passed | C1 | G | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-property-Line_Break-with-value-negated.js` | original | failed | passed | C1 | G | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-property-Line_Break-with-value.js` | original | failed | passed | C1 | G | P1, P3 |
| `test/built-ins/RegExp/property-escapes/unsupported-property-Line_Break.js` | original | failed | passed | C1 | G | P1, P3 |
| `test/language/identifiers/vertical-tilde-continue-escaped.js` | original | failed | passed | C1 | I | I1 |
| `test/language/identifiers/vertical-tilde-continue.js` | original | failed | passed | C1 | I | I1 |
| `test/language/identifiers/vertical-tilde-start-escaped.js` | original | failed | passed | C1 | I | I1 |
| `test/language/identifiers/vertical-tilde-start.js` | original | failed | passed | C1 | I | I1 |

**What did not change, and what part A therefore does not show.** No test under
`built-ins/String/prototype/` (`normalize`, the four casing methods, `localeCompare`), `intl402/String/prototype/`,
`intl402/Collator/` or `intl402/Segmenter/` changed verdict: at the base none of them failed but three
`proto-from-ctor-realm.js` tests of `Intl.Collator` and `Intl.Segmenter`, which are not the area and fail after
too. The corrections of normalization, casing and segmentation (N1 to N3, C1 to C8, G1 to G4) are cases test262 at
`ccaac100` does not hold, or holds in tests the base passed already; SJL-7.3's vectors hold them (sections 2 and 3).

**Rows that fail at both commits**, which no class reads because they do not change:

- the seven `property-escapes/generated/strings/*-negative-CharacterClass.js` rows section 3 names, and
  `property-escapes/character-class-range-end.js` and `character-class-range-no-dash-start.js`, which put a property
  escape at a class range's end;
- **26 negative tests of `language/literals/regexp/named-groups/`**: duplicate, numeric, punctuated, incomplete and
  non-identifier group names in a regular-expression literal. The `RegExp` constructor refuses each such name at the
  base and after (row I3's correction is made there), but the parser makes no early check of a literal's group
  names at either commit, as it made none of property names before the cut-over. Fourteen of the 26 turn on
  whether a code point is ID_Start or ID_Continue. A fuller check would correct them; SJL-7.5 did not make it, and
  nothing here claims it.

**The after run.** Taken by `.github/workflows/test262-evidence.yml` at `a34b1f78`, in the rule's configuration
([`test262/after/run.txt`](test262/after/run.txt)): run
[38983995623](https://github.com/Broiler-Platform/Broiler.JS/actions/runs/38983995623), attempt 1, dispatched
2026-10-16 23:56Z from this branch at `a34b1f78`, named by all forty digits, and completed 2026-10-17 01:31Z, with
`suite_ref` `ccaac100`, four workers, 30 seconds per test, `rerun_timeouts` true, `time_zone` empty and
`include_negative` true: `--all-script-host-verifiable --include-negative` over eight shards, terser 5.50.0, no
memory limit, the .NET SDK 10.0.402, on runner image ubuntu24 20261012.334.1, the base's. **The runner's ICU** is
the image's, `libicu` 74.2-1ubuntu3.1 (ICU 74, Unicode 15.1): neither run printed it, and the image's own software
list names it ([`test262/comparison/runner-icu.txt`](test262/comparison/runner-icu.txt)). It is imported into
[`test262/after/`](test262/after/) unchanged, all 26 entries of the workflow's artifact `test262-evidence` (id
12085520993), with [`imported.txt`](test262/after/imported.txt), which records the digest of the zip as
downloaded, `357f3140…`, the digest GitHub records. **Timeouts.** As at the base, one path timed out in both
variants, `test/staging/sm/regress/regress-1507322-deep-weakmap.js`; the workflow ran it again alone with one
worker, and it timed out again in both ([`test262/after/rerun-timeouts.log`](test262/after/rerun-timeouts.log)), so
the rows compare timed out against timed out. **The merged totals**, which do not decide part A: 98,146 variant
cases passed, 2,039 failed, 2 timed out and 5,871 skipped, where the base's were 98,006, 2,179, 2 and 5,871; by
path, 51,550 passed and 1,478 failed, where the base's were 51,410 and 1,618.

**What it says.** The cut-over moved 140 verdicts of the whole suite, each from failed to passed, each a parse-time
refusal the specification asks for and the base did not make, and moved no other. Part A judges test262 per path
only; the measurement is part B's (section 11).

## 7. The home: Broiler.Unicode's packages and the tables this engine embeds

The home is another repository's: the owner's code packages as nuget.org serves them, over tables this engine
generates with the owner's generator from its own pin and embeds in `Broiler.JavaScript.Runtime`. The owner's
own clauses, its test files, its generation and its reference arms, are judged in its bundle. From a clean
checkout of `a34b1f78`, into [`home/`](home/):

| Check | Result | Where |
|---|---|---|
| The references | `Directory.Packages.props` pins `Broiler.Unicode.Algorithms`, `Broiler.Unicode.Segmentation` and `Broiler.Unicode.Generation` at `0.1.0-preview.4`. `Broiler.JavaScript.Runtime` references the first, `Broiler.JavaScript.BuiltIns` the second, and only the test project `Broiler.JavaScript.Runtime.Tests` the generator. The engine's closure, `Broiler.JavaScript.All`'s, holds the two code packages and, through `Broiler.Regex` `0.1.0-preview.4`, `Broiler.Unicode.Properties` and `Broiler.Unicode.Emoji.StringProperties` `0.1.0-preview.3`, which carry their own data and which no code of this engine calls (the last entry of section 9's deletion check). This engine references no package that carries tables | [`home/packages.log`](home/packages.log) |
| The packages as nuget.org serves them | For each of the owner's three packages at `0.1.0-preview.4`, nuget.org's copy carries nuget.org's repository signature, which `dotnet nuget verify` accepts, and holds, entry by entry, the bytes of the copy this repository restores, plus that signature: the two have one content hash. Each nuspec names the owner's repository at `6e14f93`; `Algorithms` depends on nothing, and `Segmentation` and `Generation` on `Algorithms` alone. *This machine's* package folder took the three from Broiler.VM's committed feed, whose copies carry no signature; that is why `verify` refuses those copies, and why the content hash, not the file's, is compared. The pristine consumer (section 8) restores them from nuget.org itself. **Part A's after run ran them**: its shell's `Broiler.Unicode.Algorithms.dll` and `Broiler.Unicode.Segmentation.dll` are byte for byte the assemblies in these packages, by SHA-256 ([`test262/after/shell-binaries.sha256`](test262/after/shell-binaries.sha256)) | [`home/packages.log`](home/packages.log) |
| The pin | `data/unicode/pins/` against the owner's at `6e14f93`, file by file by git blob: 28 of the owner's 31 files are here byte for byte, `unicode.pin` among them; the owner's README is replaced by this repository's, and its `broiler-vm/` (two files) is not copied, as [the pin's README](../../../data/unicode/pins/README.md) says | [`home/pin.log`](home/pin.log) |
| The embedded tables, and the area's tests | built in Release and run under `TZ=UTC`: **184 tests, none failed**. `UnicodeTableRegenerationTests` (8): the embedded tables regenerate byte for byte from the pin with the owner's generator, two generations write the same bytes, the pin equals the generator package's by file name, length and hash, the provider answers the generated bytes for every table but the folding tables, every owner type this engine reads constructs over the provider and folding does not, and three controls (a changed byte of a pinned input, a changed hash in the pin, a changed byte of the committed tables) are each refused or reported. With them `JSUnicodeTests` (23), `SharedUnicodeTests` (104), `UnicodeDependencyTests` (5), `UnicodeLexicalClassTests` (40) and `StringSpanTrimTests` (4) | [`home/build.log`](home/build.log), [`home/tests.log`](home/tests.log) |

The cut-over's whole test run, 17 assemblies and 11,340 tests at `c7a19d35` (section 3), is not repeated: the
after commit's product sources are that commit's.

## 8. The pristine consumer

**The consumer runs, and so does the Unicode area through it.**
[`engine/pristine-consumer.log`](engine/pristine-consumer.log): the runtime graph of `Broiler.JavaScript.All`
packed from `a34b1f78` into 18 local packages, restored into a clean consumer from that feed and nuget.org
into an isolated package cache, and run: it printed `42`. The packaging script checks that the `Runtime`
package declares `Broiler.Unicode.Algorithms` and the `BuiltIns` package `Broiler.Unicode.Segmentation`.

[`engine/pristine-consumer-unicode.log`](engine/pristine-consumer-unicode.log): the same consumer, from the same
isolated cache and offline, then evaluated twelve expressions, each a correction or a pinned-data answer of the
cut-over, and printed **`ok 12`**: a lone surrogate through `normalize` (N1), a Unicode 16 decomposition (N2),
the final sigma after U+00AA (C1), a list of locales (C7), a loose property name refused (P1),
`Katakana_Or_Hiragana` admitted (P2), `RGI_Emoji` under `v` (S1), U+2E2F refused in an identifier (I1), U+0085
refused as white space (W1), UAX #29's sentences and words (G1, G2) and a grapheme count (G3). In that cache, the
owner's two packages, `Broiler.Unicode.Properties` and the emoji package came from nuget.org, signed, and each
has the content hash of the copy this repository restores.

**A control of the checks**, [`engine/pristine-consumer-checks-control.txt`](engine/pristine-consumer-checks-control.txt):
the same twelve expressions in the shell built at the base `3916bd2b` and at `a34b1f78`. At the base ten fail
or throw; S1 and G3, which the correspondence reads as agreements, hold at both. So the consumer's `ok 12` is
the cut-over's, not a property of any build.

## 9. The simplification check

**The cut-over's searches, repeated.** [`simplification/cutover-searches.log`](simplification/cutover-searches.log)
runs each of section 3's two searches by the command its file records, at `a34b1f78`, and sets its lines against
the cut-over's. **The deletion list's members**: no line, as at `c7a19d35`. **The host Unicode APIs**: 315
lines, 311 of them the cut-over's; the other four are the same four lines of the changelog, six lines further
down since `93f729c5` added rows P3 and P4. Section 3's table of where each line sits, and whose it is, holds
unchanged.

**The deletion check.** [`simplification/deletion-check.log`](simplification/deletion-check.log) searches each
entry of the area's Broiler.JS deletion list (Broiler.VM `docs/javascript-shared-libraries/milestones/sjl-7-unicode.md`,
SJL-7.5, with the three entries JSD-0074 adds) in the file or folder that held it, with a pattern for what the
entry deleted and for the call that replaced it. **Every entry is absent.**

| Entry | What remains | Reading |
|---|---|---|
| The host `string.Normalize` in `JSStringPrototype.Transform` and in the collator's NFD step | `normalize` calls `NormalizeText`, which is the owner's `UnicodeNormalization.Transform` under this engine's length policy; the collator's two NFD calls go through it | absent |
| `JSStringSpecialCasing` usage, and the `CultureInfo` resolution for locale casing | a comment naming the class it replaced, and the owner's `ToUpperCase` and `ToLowerCase`; `JSString`'s `ToLocaleString(string, CultureInfo)` override, which returns the string unchanged and cases nothing | absent |
| `CharExtensions`' identifier classification and its Unicode-17 patch tables | a comment saying the classes are the owner's | absent |
| The white-space lists: the scanner's, `StringSpan.Trim`'s and `JSRegExp.Escape`'s | the scanner and `RegExp.escape` call `JSUnicode.IsWhiteSpace`; `StringSpan`'s three trims forward to the shared `TrimStrWhiteSpace` (SJL-D-25); `StringSpan.IsNullOrWhiteSpace`, .NET's white space, which recognises a missing name in a stack frame's description and in an Annex B name, and classifies no source text; the .NET translator's `\s` set (row W3, SJL-11), which the list does not name | the three lists are absent |
| `JSRegExp`'s identifier classes for group names, and its loose property lookup | group names through `JSUnicode.IsIdentifierStart` and `IsIdentifierPart`; property names through the owner's `UnicodeProperties.TryResolve` and `UnicodeStringProperties.TryResolve` | absent |
| `JSIntl`'s `ComputeGraphemes`, `ComputeWords`, `ComputeSentences` and `IsWordLike` | the owner's `UnicodeSegmenter` over this engine's tables, and `isWordLike` from its `GetWordStatus` | absent |
| JSD-0074: `IsValidNativeFunctionToStringName`'s `char.IsLetter` | the method, checking each code point with `JSUnicode.IsIdentifierStart` and `IsIdentifierPart` | the platform call is absent |
| JSD-0074: the collator's `IsNormalized` | nothing | absent |
| JSD-0074: the translator's loose property lookup with its `NormalizeKey` | `JSUnicodePropertyNames.FirstRefused` before either engine reads the pattern, and the owner's `TryResolve` | absent |
| The code's direct calls of `Broiler.Unicode.Properties` and the emoji package | in product code, nothing: a test's list of the retired assemblies, an integration test's comment and the changelog | absent |

**The duplicate search.** [`simplification/duplicate-search.log`](simplification/duplicate-search.log) searches
the engine's product sources (`Broiler.JS/`; test projects, benchmarks, samples and the generated tables
excluded) in four parts, for anything that would duplicate the owner's API: platform Unicode APIs the cut-over's
search does not name (100 hits), code points of the area's sets and algorithms spelled in code (23), definitions
of the area's predicates and algorithms (26, JSUnicode's own four among them as the search's control), and
property names and aliases spelled as strings (33).

| Hits | Where | What they are |
|---|---|---|
| 47 | `JSIntl` (38), `JSIntlDateTimeFormatEngine` (6), `JSIntlDefaultNumberingSystems.g` (3) | locale tags, Unicode extension keys and zone names compared ignoring ASCII case, the grandfathered tags' pattern, and the collator's `CompareInfo` with `CompareOptions.IgnoreCase`: Intl's (SJL-9) and collation's (SJL-17) |
| 3 | `JSTemporalZonedDateTime` | `UTC` and `Z` compared ignoring ASCII case (SJL-8, SJL-10) |
| 39 | `JSRegExp` (36), `JSRegExp.UnicodeSets` (1), `RegExpValidator` (2) | the .NET translator: its `IgnoreCase` and its case-folding tables over .NET's casing (row F2, SJL-11); its property escapes and its operand resolution, which take each name from the owner's exact lookup and its ranges from the owner's tables; the validator's placeholder for a `\p{...}` whose name `JSUnicodePropertyNames` has already checked |
| 33 | `JSRegExp` | `GeneralCategoryNames`: General_Category's names and aliases mapped to .NET's short spelling (a finding for SJL-11, below) |
| 22 | `JSRegExp` (12), `FastScanner` (8), `CharExtensions.IsLineTerminator` (2: its definition and its body) | the line terminators U+2028 and U+2029 spelled in the scanner, in `source`'s escaping and in the translator's dot and multiline classes, a comment on a group name's U+200C, and the translator's `\s` set (rows W5 and W3) (a finding, below) |
| 14 | `StringExtensions.ToCamelCase` (2), `StringSpan.ToLower` and `ToCamelCase` (3), `StringSpanExtensions` (2), the class generator's `ListExtensions` (2), `CoreInternalHelpers` (2), `MarshalExtensions` (2), `ClrType` (1) | the CLR binding's names: a .NET member's name lower-cased into a script name, and a script object's keys matched to a .NET property's name ignoring case (a finding, below) |
| 2 | the expression compiler's `VariableInfo` and `ILCodeGenerator.VisitParameter` | its own variables looked up by name ignoring case; variables that differ only in case, `a` and `A`, `k` and `K`, `İ`, `i`, `ı` and `I`, stayed apart in the shell at `a34b1f78` ([`probes.txt`](simplification/probes.txt)) |
| 12 | `KeyValueStore` (5), `BroilerJSContext` (2), `PackageManager` (2), `JSModuleContext`, `NuGetMetadataReferenceResolver`, the REPL's prompt | the host's HTTP header names, file extensions, package names and completion |
| 1 | `FastScanner` | `char.IsControl` choosing a diagnostic's wording for an unexpected character |
| 4 | `JSUnicode` | the control: `IsIdentifierStart`, `IsIdentifierPart`, `IsWhiteSpace` and `IsLineTerminator`, each the owner's |
| 4 | `StringSpan.IsNullOrWhiteSpace`, `JSMap.CanonicalizeKey`, `JSValue.ResolvePropertyValue`, `Broiler.JS/CHANGELOG.md` | the first as above; the next two are not the area; the changelog's text |
| 1 | `modules/inbuilt/buffer.js` | a UTF-8 lead byte's bound in the Node `Buffer` module |

**Findings, none of which the cut-over made or an SJL-7 clause reads:**

- **The translator's General_Category inside a class is the host's** (for SJL-11). Where Broiler.Regex cannot
  build a `u` or `v` pattern and the .NET translator runs it, a General_Category value inside a class is handed
  to .NET's own `\p{Xx}`, through the 33-line `GeneralCategoryNames` map, which repeats the gc rows of
  `PropertyValueAliases.txt` that the owner's lookup holds. .NET's class is code-unit based and reads the
  runtime's Unicode data, not the pin. Every other property, and every General_Category outside a class, takes
  its ranges from the owner's tables. The code says so (`JSRegExp.Broiler.cs`), and Broiler.Regex takes every
  `u` and `v` pattern it can build, so this path is a fallback: `[\p{Lu}]`, `[\p{L}]` and `[\p{Ll}]` under `u`,
  on Unicode 16 and 17 letters and on U+1D400, matched in the shell at `a34b1f78`
  ([`simplification/probes.txt`](simplification/probes.txt)). The map was at the base too.
  The record leaves the translator to SJL-11 ("the translator itself stays SJL-11's"), so clause 6 does not read
  it; a stricter reading of "property lookup" would.
- **The line terminators are spelled where the owner's `EcmaScriptLexical.IsLineTerminator` holds them** (rows
  W5 and W3): eight lines of the scanner, `CharExtensions.IsLineTerminator`, `source`'s escaping and the translator's
  classes. The correspondence records agreement on the set, and the record leaves the lexer to SJL-14 and the
  translator to SJL-11; `JSUnicode.IsLineTerminator`, which the scanner's white-space loop calls, is the owner's.
- **The CLR binding's names read the host's culture** (no milestone names it). `ToCamelCase` lower-cases a .NET
  member's leading capitals with `char.ToLower` and `string.ToLower()`, which read the current culture, and the
  marshalling helpers match keys with `ToLower()`: on a Turkish or Azerbaijani host a member named `ID` would
  become `ıd`. `StringSpan.ToLower()` and `KeyValueStore`'s header names do the same. This is not the Unicode
  area as SJL-7 bounds it, and no SJL-7 clause reads it.

## 10. Engine-level vectors at the after commit (corpus revision 20)

**Every vector of corpus revision 20 has the result its class expects.** The engine-level runner,
`Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release, ran at `a34b1f78`, the after commit, which carries
revision 20, from a clean checkout, under `TZ=UTC`, from 23:54Z on 2026-10-16 for 35 minutes. The machine ran
Broiler.VM's vectors and builds beside it; nothing in the run is timed.

| Run | Result | Files |
|---|---|---|
| Revision 20, UTC | 88,465 vectors: 88,461 match, 4 classified divergences, 0 unclassified differences, 0 not implemented, and no divergence that no longer holds | [`engine/run-revision-20-utc.log`](engine/run-revision-20-utc.log), [`engine/run-revision-20-utc.tsv.gz`](engine/run-revision-20-utc.tsv.gz) |
| Its negative control | one vector's expected value changed in memory, `numbers/to-fixed/0001`, reported as an unclassified difference | [`engine/negative-control-revision-20.tsv`](engine/negative-control-revision-20.tsv) |

The 4 are the number area's `numbers/literal/0082`, `0084`, `0088` and `0090`, which stay with SJL-14. The 3,678
fields revision 20 retired ([`engine/retired-divergences-revision-20.tsv`](engine/retired-divergences-revision-20.tsv))
are matches now, as they were reported as no longer holding at revision 19 (section 3). The runner's test passes.
Broiler.VM's run on the same revision, in its bundle of the same name, matches every vector.

## 11. Part B: the measurement (rule part B)

**Void: the lane is not a controlled lane** (clause 1), so part B decides nothing, and JSD-0031 §4(b)'s fifth
reason stays open for this engine. [`measurement/`](measurement/README.md) holds the run, a sub-bundle of its own
evidence class with its own README, manifest and hashes, taken on 2026-10-17 from 01:42Z to 02:58Z with nothing
else of the programme running. Clauses 2 to 4 hold: all 43 answer checks equal to arm B's first, one effective
configuration and one BenchmarkDotNet host record, and 21 runs of six workloads with no issues.

**Its arithmetic would not meet clause 5 on a controlled lane.** `Identifiers` is below resolution (D = -0.034 ms,
N = 0.035 ms). **`Normalize` and `Casing` are far slower after the cut-over**: D = +17.764 ms against N = 0.793 ms
for `Normalize` (Q 9.003 ms at the base, 26.767 ms after), and D = +8.891 ms against N = 0.307 ms for `Casing`
(9.446 ms, 18.337 ms). The arms' ranges do not overlap, and the controls did not move. The base normalized with
the host's ICU and cased through the runtime; the after arm runs the owner's managed algorithms over the pinned
tables. The plan named this exposure before the cut-over; whether the owner's normalization and case mapping are
made faster before a controlled lane runs the protocol is the owner's to decide.

**The collector's step, as it was run.** `collect.py --only measure` follows bundle
`sjl-3-001`'s step with this rule's workloads: arm B is a clean checkout of `3916bd2b`; arm A is one of
`a34b1f78`, where `UnicodeBenchmarks.cs`, the benchmark `Program.cs` and `BenchmarkConfiguration.cs` are the
rule commit's byte for byte (and no file of the benchmark project changed), so A is the after commit as it stands;
A-again is A's build under another lane name. Seven repetitions, each running B, A and A-again once in an order
that rotates by repetition; one run is one launch of the benchmark with `--filter *UnicodeBenchmarks*`,
`BROILER_BENCHMARK_PROFILE=baseline` and `--buildTimeout 1800`, which runs the six workloads; `--unicode-answers`
before and after every run, each set against arm B's first check, answers and configuration both. Before any run
the step refuses arms that are not the rule's, refuses invariant globalization, builds both arms with no build
server left running, and writes `measurement/manifest.txt` (the commits, the identity checks, the SHA-256 of every
assembly of each arm, the protocol and the lane's record: CPU, microcode, topology, memory, OS, governor, thermal
policy, the host's ICU and the `DOTNET_` variables) and makes it read-only. A lane is named on the command line,
`smoke` or `controlled`; a controlled lane needs its record, which the manifest quotes. An earlier `measurement/`
is kept beside the new one as stopped. A dry run at the two clean checkouts found the arms the rule's and printed
the protocol:

```
python3 docs/evidence/sjl-7-001/collect.py --tree <a clean checkout of a34b1f78> \
    --bench-tree <a clean checkout of 3916bd2b> --lane smoke|controlled [--lane-record <the lane's record>] \
    --out docs/evidence/sjl-7-001 --only measure
```

## 12. What this bundle does not claim

- **Nothing about Broiler.VM or the owner.** Broiler.VM's half and the owner's clauses (1 to 4) are judged in
  their own bundles, and this bundle reads neither's runs.
- **Part B's decision, and any speed claim.** Section 11's run is void; its arithmetic is read, and decides
  nothing.
- **The rows that fail at both commits** (section 6): the parser's early check of a literal's group names, and
  of a property of strings directly inside a negated class, is not made, and nothing here says it is.
- **The translator, the lexer and Intl.** The findings of section 9 are named for the milestones that own the
  code; nothing here judges them.
- **A Unicode version other than 17.0.0**, the pin's; and the runners' ICU, 74.2 (Unicode 15.1), only as the
  host data part A's runs loaded: after the cut-over the area covered reads the pin, not the host.
- **A manifest of the measurement.** [`manifest.json`](manifest.json) holds the SHA-256 of every file this bundle
  retains, sections 10 and 11's included; `measurement/` also has its own `hashes.txt`.
- **Review**: the reviewer field says none and means it.
