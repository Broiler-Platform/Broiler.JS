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

**State, 2026-10-16, at SJL-7.3: part A's base is taken, and the engine-level vectors' first run is classified;
nothing here is a verdict.**
- Part A's after run follows SJL-7.5's cut-over.
- Part B's arms are measured at SJL-7.6, when the after arm exists, because the rule interleaves the arms
  inside each repetition.

*(Revised 2026-10-16, at SJL-7.3. The state's heading read "at SJL-7.2: part A's base is taken, and nothing here is
a verdict.")*

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
