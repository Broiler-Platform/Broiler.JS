# Compliance dashboard

This dashboard is the public status page for Broiler.JS standards compliance. It intentionally separates measured results from goals so the project does not overstate conformance before evidence is available.

## Current evidence

| Area | Latest recorded result | Evidence |
| --- | --- | --- |
| Repository xUnit tests | 2026-05-09 local baseline on commit `2907ab8fee53adfeb9af0d1974eab5052a97c241`: 247 passed, 0 failed, 0 skipped | `dotnet test Broiler.JS.slnx --no-build --logger trx --results-directory /tmp/broiler-tests-final` |
| test262 (real subset, custom raw-script runner) | 2026-05-09 snapshot of `tc39/test262` `main` at `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`: 126 executed / 1 skipped across `Array.isArray`, `addition`, `strict-equals`, and `RegExp.escape`; Broiler passed 75 and failed 51 while Chromium passed 126 and failed 0 | Downloaded the upstream suite outside the repo, prepended the standard harness files (`assert.js`, `sta.js`, and per-test includes), then executed the same files through the repaired Broiler CLI script host and Chromium 147.0.7727.0. |
| test262-harness smoke | Official `test262-harness` now launches the Broiler CLI, but the Node-style host prelude still fails before real test execution because Broiler does not yet match the expected global/CommonJS setup | `npx test262-harness --host-type node --host-path /tmp/broilerjs-host .../Array/isArray/15.4.3.2-0-1.js` currently aborts at `Function(\"return this;\")().require = require`. |
| engine262 tests | Pending harness integration | Add command and totals after the first run. |
| JInt compatibility/performance scripts | 2026-05-09 local comparison: 11 executed, Broiler passed 11 and Chromium passed 11 | Ran every script in `Broiler.JS/OtherTests/JIntPerfTests/Scripts` through the repaired Broiler script host and Chromium 147.0.7727.0. |
| Comparative engine checks | First Chromium comparison recorded: Broiler diverged on 51 of 126 executed test262 subset files and matched Chromium on all 11 local JInt compatibility scripts | Use the detailed failure buckets below to drive follow-up issues before broader engine comparisons are added. |

## 2026-05-09 local comparison run

- Broiler commit under test: `2907ab8fee53adfeb9af0d1974eab5052a97c241`.
- test262 source snapshot: `tc39/test262` `main` at `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`.
- Chromium reference engine: `Chromium 147.0.7727.0`.
- The custom runner intentionally executed the real upstream test files with standard harness collateral, but only for a filtered raw-script subset; async, module, and host-harness-dependent tests were excluded from this first pass.

### Largest failing buckets in the executed test262 subset

1. `test/language/expressions/addition` — 29 Broiler failures
2. `test/built-ins/RegExp/escape` — 9 Broiler failures
3. `test/language/expressions/strict-equals` — 7 Broiler failures
4. `test/built-ins/Array/isArray` — 6 Broiler failures

### Representative divergences vs Chromium

- `Array.isArray(Array.prototype)` returned `false` in Broiler but passed in Chromium.
- `Array.isArray` proxy and revocable-proxy cases failed in Broiler.
- Multiple `+` tests showed incorrect wrapper-object / symbol / bigint coercion (`new Number(1) + 1`, `Object(1n) + 1`, `Symbol.toPrimitive` ordering).
- Multiple unresolved-reference tests in `addition` and `strict-equals` returned values instead of throwing `ReferenceError`.
- Some BigInt comparison cases still failed during parsing, including `===`-based tests that Chromium accepted.
- `RegExp.escape` diverged on initial-character escaping, punctuator escaping, surrogate handling, and several property-descriptor checks.

## Compliance workstreams

- [x] Review the documents in `docs/compliance/` against the current repository layout and validation process.
- [x] Record the repository-test baseline required by `process.md`.
- [ ] Add a pinned `test262` harness run and publish suite totals here.
- [ ] Add an `engine262` cross-check run and publish suite totals here.
- [ ] Expand syntax-compliance coverage for parser gaps called out in `known-gaps.md`.
- [ ] Expand built-in compliance coverage for the high-risk areas called out in `known-gaps.md`.
- [ ] Publish a comparative engine matrix for release-time regression tracking.

## Regression tracking

- A compliance run is complete only when raw logs and summary totals are linked from this file.
- Failing tests that represent unsupported syntax, missing built-ins, host limitations, or known bugs must be summarized in `known-gaps.md`.
- A result row should never claim “fully compliant” or “most compliant” unless the linked public suites and comparison data support that claim.

## Next automation steps

1. Add a CI job that restores Broiler.JS and runs repository tests.
2. Add a separate scheduled compliance job that downloads or caches pinned test262 inputs.
3. Publish machine-readable totals as CI artifacts and update this dashboard for releases.
4. Track pass-rate deltas per suite revision to detect regressions.
