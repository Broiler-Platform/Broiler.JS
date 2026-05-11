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
2. Audit the pinned `test262` checkout with `python scripts/compliance/audit_test262.py --suite-ref <sha> --suite-root <local-test262-checkout> --manifest-glob 'scripts/compliance/test262-*.txt'` to record the full upstream testcase count, the script-host-verifiable subset, and how much of that surface the pinned manifests cover.
3. Use the pinned runner at `python scripts/compliance/run_test262.py --suite-ref <sha> --path-file <manifest.txt>` for script-host-compatible non-negative `test262` subsets, including async and `noStrict` files that only depend on `$DONE` and standard harness includes.
4. Use `python scripts/compliance/compare_engines.py --manifest scripts/compliance/engine-scenarios.json --engine262-bin <path-to-engine262>` for the shared Broiler-vs-V8-vs-engine262 cross-check matrix.
5. Clone or cache larger public suites outside the source tree or as CI cache inputs when broader coverage is needed; do not vendor large external suites without a license and update policy.
6. Record the exact suite revision, command line, host options, environment, upstream discovered count, and manifest coverage percentage in `docs/compliance/dashboard.md`.
7. File or update issues for failing feature areas and link them from `docs/compliance/known-gaps.md`.
8. Treat any newly failing previously-passing test as a regression unless a suite update intentionally changed expected behavior.

## Reporting format

Each run should report:

- Date and commit under test.
- Suite name and upstream revision.
- Total discovered upstream test files and the percentage covered by the manifest(s) being reported.
- Total, passed, failed, skipped, timed out, and unsupported counts.
- Feature tags or directories with the largest failure counts.
- Raw logs or artifact location.
- Follow-up owner or issue link for regressions.
