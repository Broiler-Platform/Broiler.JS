# Compliance process

Broiler.JS compliance is measured with repository tests plus public JavaScript conformance suites. The process is evidence-based: every published claim must include the suite name, suite revision, host command, filters, pass/fail totals, and links to raw output or CI artifacts.

## Required suites

| Suite | Source | Purpose | Status |
| --- | --- | --- | --- |
| test262 | https://github.com/tc39/test262 | ECMAScript language and built-in conformance | Required for release evidence; pin by commit SHA. |
| engine262 tests | https://github.com/engine262/engine262 | Independent ECMAScript semantic checks and examples | Optional cross-check suite. |
| Jint compatibility/performance scripts | Repository `OtherTests/JIntPerfTests` | Regression and comparative behavior checks already carried in this repository | Available locally. |
| Dromaeo-derived scripts | Repository `OtherTests/JIntPerfTests/Scripts` | Legacy performance/regression scenarios | Available locally with license notices. |

## Running compliance evidence

1. Restore and run repository tests with `dotnet test Broiler.JS.slnx`.
2. Audit the pinned `test262` checkout with `python scripts/compliance/audit_test262.py --suite-ref <sha> --manifest-glob 'scripts/compliance/test262-*.txt'`. Pass `--suite-root <local-test262-checkout>` when a local checkout is already available; otherwise the audit downloads and caches the pinned suite archive under the tool's temp directory before computing totals, blocker counts, and uncovered-bucket breakdowns.
3. Use the pinned runner at `python scripts/compliance/run_test262.py --suite-ref <sha> --path-file <manifest.txt>` for script-host-compatible non-negative `test262` subsets, including async and `noStrict` files plus `onlyStrict` files that only depend on `$DONE` and standard harness includes, but excluding known host-harness dependencies such as `$262`.
4. For dynamic full-suite breadth, use `python scripts/compliance/run_test262.py --suite-ref <sha> --all-script-host-verifiable --shard-count <N> --shard-index <I>` (pass `--shard-index -1` to run every selected shard locally) or dispatch `.github/workflows/test262.yml`; the unified `test262` workflow discovers the entire current `test262` tree for the selected ref and runs every currently script-host-verifiable file in deterministic shards without maintaining manifests by hand.
5. The script-host runner now executes each test in its own subprocess with a default 30-second wall-clock timeout. Use `--test-timeout-seconds <seconds>` to tune that limit locally; timed-out tests are terminated and reported separately from ordinary failures.
6. On POSIX hosts, pass `--memory-limit-mb <MiB>` to apply an opt-in per-test address-space cap in the child process. The full-suite workflows leave that cap disabled by default because the .NET host reserves a large virtual address space on GitHub runners, but they still disable core dumps and apply CPU/time-based process limits for every test. Pass a non-zero value only after verifying that the chosen limit still lets the Broiler host start on the target machine.
7. Some upstream `test262` files are still intentionally excluded from that sharded full-suite run because the current raw script-host runner does not yet validate them correctly: negative-metadata tests need expected-error handling, host-harness-dependent tests need richer `$262`/host support, and `module` / `raw` tests need different host modes.
8. For per-assembly orchestration (e.g. only the parser or runtime), dispatch the unified `.github/workflows/test262.yml` and pick the desired `assembly` input (`all`, `parser`, `compiler`, `runtime`, `builtins`, `intl`, or `annexb`); selecting a single assembly scopes the full phase to that assembly's test262 paths instead of the entire script-host-verifiable suite. Run the same selection locally with `python scripts/compliance/list_test262_assemblies.py --manifest scripts/compliance/test262-assemblies.json --paths-for <assembly> --output /tmp/<assembly>.txt` followed by `python scripts/compliance/run_test262.py --path-file /tmp/<assembly>.txt`. The assembly-to-test262-path mapping is stored in `scripts/compliance/test262-assemblies.json`; add or refine an entry there when an assembly's responsibilities change.
9. The pinned manifest files remain useful for focused local reruns and debugging via `python scripts/compliance/run_test262.py --path-file <manifest.txt>`. The unified `.github/workflows/test262.yml` uses the same sharded script-host mode for manual dispatches (with `assembly`, `rerun_failed_only`, `shard_index`, `test_timeout_seconds`, and `memory_limit_mb` inputs) and for automatic `push` runs on `main`, which covers pull-request merges into the default branch. That workflow persists the latest failing or timed-out testcase paths from full-suite runs in `scripts/compliance/test262-failures.txt`; with `rerun_failed_only` left at its default `true`, the next run reruns that saved list first and only falls back to the full suite once the saved failures all pass — implementing the post-PR incremental "fix-first, then full" loop. Its merged `test262-logs` artifact stores flat per-phase shard files such as `rerun-shard-0.json`, `rerun-shard-0.log`, `full-shard-0.json`, `full-shard-0.log`, and matching `.exit-code.txt` files for easier aggregation, then runs `dotnet run --project logs/LogParser -- <merged-log-dir> --highest-impact-problem` and uses the `ISSUE_TOKEN` secret to file the generated issue-ready markdown for the highest-impact failure category when a common parsed exception is detected.
10. Use `python scripts/compliance/compare_engines.py --manifest scripts/compliance/engine-scenarios.json --engine262-bin <path-to-engine262>` for the shared Broiler-vs-V8-vs-engine262 cross-check matrix.
11. Clone or cache larger public suites outside the source tree or as CI cache inputs when broader coverage is needed; do not vendor large external suites without a license and update policy.
12. Record the exact suite revision, command line, host options, environment, upstream discovered count, and manifest coverage percentage in `docs/compliance/dashboard.md`.
13. File or update issues for failing feature areas and link them from `docs/compliance/known-gaps.md`.
14. Treat any newly failing previously-passing test as a regression unless a suite update intentionally changed expected behavior.

## Testsuite optimization opportunities

See [`testsuite-optimization.md`](testsuite-optimization.md) for documented
bottlenecks in the current pipeline and actionable recommendations (parallel
in-shard execution, seeded ordering, fragile-area prioritization, script-host
coverage of negative/module/raw buckets, and seeded property-based fixtures)
aimed at raising the number of gaps and glitches discovered per execution cycle.

## Reporting format

Each run should report:

- Date and commit under test.
- Suite name and upstream revision.
- Total discovered upstream test files and the percentage covered by the manifest(s) being reported.
- Unique script-host exclusions plus the blocker counts that caused those exclusions (`negative`, `hostHarness`, `module`, `raw`, etc.); note when counts overlap.
- Total, passed, failed, skipped, timed out, and unsupported counts.
- When a run is sharded, the shard count/index and the number of runnable files before sharding.
- Feature tags or directories with the largest failure counts.
- The largest uncovered script-host-verifiable top-level areas/buckets so manifest growth can be prioritized.
- Raw logs or artifact location.
- Follow-up owner or issue link for regressions.
