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

**State, 2026-10-16, at SJL-7.2: part A's base is taken, and nothing here is a verdict.**
- Part A's after run follows SJL-7.5's cut-over.
- Part B's arms are measured at SJL-7.6, when the after arm exists, because the rule interleaves the arms
  inside each repetition.

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
