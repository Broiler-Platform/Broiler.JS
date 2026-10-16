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

**State, 2026-10-16, at SJL-7.5: part A's base is taken, the engine-level vectors' first run is classified, and
this engine's cut-over is recorded with its run on the vectors; nothing here is a verdict.**
- Part A's after run is taken at SJL-7.6, at the commit that lands the cut-over.
- Part B's arms are measured at SJL-7.6, when the after arm exists, because the rule interleaves the arms
  inside each repetition.

*(Revised 2026-10-16, at SJL-7.3. The state's heading read "at SJL-7.2: part A's base is taken, and nothing here is
a verdict." Revised again at SJL-7.5; it read "at SJL-7.3: part A's base is taken, and the engine-level vectors'
first run is classified; nothing here is a verdict.", and its first item "Part A's after run follows SJL-7.5's
cut-over.")*

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
