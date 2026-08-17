# Compliance process

Broiler.JS compliance claims require repository tests plus a pinned public-suite run.
Every published result must identify the Broiler commit, suite revision, host mode,
selection, command, environment, totals, and raw artifact.

## Required evidence

| Evidence | Purpose |
| --- | --- |
| `dotnet test Broiler.JS.slnx` | Repository unit, architecture, integration, and regression tests |
| test262 at a commit SHA | ECMAScript and ECMA-402 conformance |
| `scripts/compliance/engine-scenarios.json` | Small Broiler/Node/engine262 semantic cross-check |
| `benchmarks/Broiler.JavaScript.Engine.Benchmarks` | Repository compatibility and legacy performance scenarios, over the `OtherTests/JIntPerfTests/Scripts` corpus |

test262 is the release conformance source. The other suites are useful cross-checks and
must not be added together into a synthetic “percent compliant” figure.

## Audit a suite revision

```powershell
python scripts/compliance/audit_test262.py `
  --suite-ref <sha> `
  --manifest-glob "scripts/compliance/test262-*.txt"
```

Pass `--suite-root <checkout>` to reuse a local checkout. The audit reports discovered
tests, runnable host modes, blocker counts, and manifest coverage.

## Run focused and full selections

Focused manifest:

```powershell
python scripts/compliance/run_test262.py `
  --suite-ref <sha> `
  --path-file scripts/compliance/test262-properties-proxy.txt
```

Full script-host-verifiable selection:

```powershell
python scripts/compliance/run_test262.py `
  --suite-ref <sha> `
  --all-script-host-verifiable `
  --shard-count 8 `
  --shard-index 0
```

Use `--shard-index -1` to run every shard locally. The runner supports async and
`noStrict` files, `onlyStrict`, semicolon-separated `--subset` path/glob filters,
Test262 metadata filters through `--features` and `--feature-match`, per-test timeout,
optional POSIX memory limits, `--max-workers`, `--shuffle-seed`,
`--prioritize-fragile`, and expected-error handling through `--include-negative`.
`--minifier terser --terser-module <package-directory>` adds a Terser-transformed
variant for each eligible script-host test; `--minifier-timeout-seconds` bounds each
transformation independently. The fixed `test262-safe-mangle-v1` profile applies
syntax minification and identifier mangling with compression disabled. The original
variant always runs.

Tests requiring `$262` host hooks remain host-harness exclusions. The `module` and `raw`
flags require separate host modes and are not validated by the ordinary script host.
Do not count excluded files as passes.

## CI and failure lifecycle

`.github/workflows/test262.yml` is the unified manual workflow. It can scope work through
`scripts/compliance/test262-assemblies.json`, path/glob subsets, and Test262 feature
metadata; shard the runnable selection; rerun saved failures first; retry an abnormal
shard once; execute original plus lockfile-pinned Terser variants by default; and
publish per-shard plus merged JSON/Markdown artifacts. It does not run automatically
after a merge or for a pull request.

Triage output is split into four focused issues: the most common normalized failure
groups, the biggest severity/impact groups, the size-ranked timeouts, and the
Terser-only failures. The last one lists only base paths whose original source passes
while the minified variant fails, times out, or cannot be transformed, so a
minification-specific defect is never buried in a mixed-variant report.
`terser_only_problems_limit` bounds its ranked case list, which leads with the smallest
minified body because that is the cheapest reduction.

The canonical merged JSON records the exact Broiler and test262 commits, workflow URL,
selection filters/scope, resource options, worker/shuffle settings, and runner
OS/architecture/.NET version, plus the selected minifier profile, pinned Terser
version, and transformation timeout. Cross-shard configuration drift and selections
that run no tests are configuration failures rather than green results. A terminal
verdict uses this authoritative merge, so a recovered retry can heal an initial job
failure while a missing full phase cannot pass accidentally.

`scripts/compliance/test262-failures.txt` is generated from tracked failures and
timeouts. CI refreshes only paths conclusively executed by the authoritative phase;
out-of-scope, skipped, cancelled, and incomplete-shard entries are preserved. A path
may be removed only after:

1. a minimal repository regression exists;
2. the focused public-suite reproduction passes;
3. the affected full shard passes; and
4. the dashboard is updated with the new evidence.

Manifest persistence consumes the canonical merged artifact, is serialized per branch,
and uses a compare-and-swap push. Only the canonical Terser-enabled profile may clear
the path-only manifest: an original-only pass cannot erase a Terser-only failure. If
source files changed after the tested commit, the workflow leaves the newer branch
untouched instead of applying stale measurements.

Treat a newly failing previously-passing test as a regression unless a pinned suite
update intentionally changed the expectation.

## Cross-engine comparison

```powershell
python scripts/compliance/compare_engines.py `
  --manifest scripts/compliance/engine-scenarios.json `
  --engine262-bin <path-to-engine262>
```

Record engine versions and do not treat agreement between engines as a replacement for
the specification or test262.

## Reporting

Every result published in `dashboard.md` must include:

- Broiler commit and dirty state;
- suite name and exact revision;
- OS, architecture, .NET version, and relevant host options;
- selected host mode, paths/filters, shard count/index, worker count, and shuffle seed;
- discovered, selected-before-sharding, passed, failed, skipped, unsupported, and
  timed-out totals;
- blocker counts for `$262`, `module`, `raw`, or other exclusions, noting overlaps;
- the highest-impact failure buckets and follow-up issue/owner; and
- raw log or CI artifact location.

Large upstream suites stay outside the source tree or in CI caches. Do not vendor them
without an explicit license and update policy.
