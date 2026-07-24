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
| `OtherTests/JIntPerfTests` | Repository compatibility and legacy performance scenarios |

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
`noStrict` files, `onlyStrict`, per-test timeout, optional POSIX memory limits,
`--max-workers`, `--shuffle-seed`, `--prioritize-fragile`, and expected-error handling
through `--include-negative`.

Tests requiring `$262` host hooks remain host-harness exclusions. The `module` and `raw`
flags require separate host modes and are not validated by the ordinary script host.
Do not count excluded files as passes.

## CI and failure lifecycle

`.github/workflows/test262.yml` is the unified manual and post-merge workflow. It can
scope work through `scripts/compliance/test262-assemblies.json`, shard the full runnable
selection, publish per-shard JSON/logs, and rerun saved failures first.

`scripts/compliance/test262-failures.txt` is generated from the latest tracked failures
and timeouts. A path may be removed only after:

1. a minimal repository regression exists;
2. the focused public-suite reproduction passes;
3. the affected full shard passes; and
4. the dashboard is updated with the new evidence.

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
