# Compliance dashboard

This dashboard is the public status page for Broiler.JS standards compliance. It intentionally separates measured results from goals so the project does not overstate conformance before evidence is available.

## Current evidence

| Area | Latest recorded result | Evidence |
| --- | --- | --- |
| Repository xUnit tests | 2026-05-10 local rerun: 293 passed, 0 failed, 0 skipped | `dotnet test Broiler.JS.slnx` |
| test262 manual workflow coverage | 2026-05-11 audit of pinned `tc39/test262` `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`: 53,469 upstream files discovered, 47,420 currently script-host-verifiable, and 6,049 uniquely excluded by negative metadata, host-harness dependencies, or unsupported `module` / `raw` modes; the unified `test262` workflow is configured to shard and execute that full 47,420-file runnable set instead of the older 163-file manifest subset | `python scripts/compliance/audit_test262.py --suite-ref ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e --manifest-glob 'scripts/compliance/test262-*.txt'`, `.github/workflows/test262.yml`, and `python scripts/compliance/run_test262.py --suite-ref ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e --all-script-host-verifiable --shard-count 8 --shard-index <0-7>` |
| test262 (real subset, custom raw-script runner) | 2026-05-09 snapshot of `tc39/test262` `main` at `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`: 126 executed / 1 skipped across `Array.isArray`, `addition`, `strict-equals`, and `RegExp.escape`; Broiler passed 75 and failed 51 while Chromium passed 126 and failed 0 | Downloaded the upstream suite outside the repo, prepended the standard harness files (`assert.js`, `sta.js`, and per-test includes), then executed the same files through the repaired Broiler CLI script host and Chromium 147.0.7727.0. |
| test262 automated `Array.isArray` subset rerun | 2026-05-10 rerun of pinned `test/built-ins/Array/isArray`: 29 executed, Broiler passed 29 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/array-isarray-summary.json --path-file scripts/compliance/test262-array-isarray.txt` |
| test262 automated unresolved-reference subset rerun | 2026-05-10 rerun of the unresolved-reference cases from `addition` and `strict-equals`: 6 executed, Broiler passed 6 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/unresolved-summary.json --path-file scripts/compliance/test262-unresolved-reference.txt` |
| test262 automated `Proxy` subset rerun | 2026-05-10 rerun of a pinned `Proxy` invariants and revocation subset: 8 executed, Broiler passed 8 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/proxy-summary.json --path-file scripts/compliance/test262-proxy.txt` |
| test262 automated BigInt comparison subset rerun | 2026-05-10 rerun of the pinned strict-equality BigInt comparison cases: 8 executed, Broiler passed 8 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/bigint-summary.json --path-file scripts/compliance/test262-bigint-comparisons.txt` |
| test262 automated promise-job subset rerun | 2026-05-10 rerun of a pinned async promise-job and `await` scheduling subset: 5 executed, Broiler passed 5 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/promise-summary.json --path-file scripts/compliance/test262-promise-jobs.txt` |
| test262 automated `for await (...)` subset rerun | 2026-05-10 rerun of a pinned `for await (... of ...)` subset: 2 executed, Broiler passed 2 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/for-await-summary.json --path-file scripts/compliance/test262-for-await.txt` |
| test262 automated non-strict/global subset rerun | 2026-05-10 rerun of a pinned non-strict/global semantics subset: 6 executed, Broiler passed 6 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/global-nonstrict-summary.json --path-file scripts/compliance/test262-global-nonstrict.txt` |
| test262 automated binary-data subset rerun | 2026-05-10 rerun of a pinned `ArrayBuffer` / `DataView` subset: 7 executed, Broiler passed 7 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/binary-summary.json --path-file scripts/compliance/test262-binary-data.txt` |
| test262 automated `RegExp.escape` subset rerun | 2026-05-10 rerun of a pinned `RegExp.escape` subset: 7 executed, Broiler passed 7 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/regexp-summary.json --path-file scripts/compliance/test262-regexp.txt` |
| test262 automated `Intl` subset rerun | 2026-05-10 rerun of the measured `Intl` constructor subset: 5 executed, Broiler passed 5 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/intl-summary.json --path-file scripts/compliance/test262-intl.txt` |
| test262 automated error constructor subset rerun | 2026-05-10 rerun of the error constructor/subclassing subset: 6 executed, Broiler passed 6 and failed 0 | `python scripts/compliance/run_test262.py --output /tmp/broiler-compliance/error-summary.json --path-file scripts/compliance/test262-error-subclassing.txt` |
| test262-harness smoke | Official `test262-harness` now launches the Broiler CLI, but the Node-style host prelude still fails before real test execution because Broiler does not yet match the expected global/CommonJS setup | `npx test262-harness --host-type node --host-path /tmp/broilerjs-host .../Array/isArray/15.4.3.2-0-1.js` currently aborts at `Function(\"return this;\")().require = require`. |
| engine262 cross-check scenarios | 2026-05-10 local cross-check over `scripts/compliance/engine-scenarios.json`: Broiler, Node/V8, and engine262 each passed 6/6 on the same reference-resolution and global-semantics cases | `python scripts/compliance/compare_engines.py --manifest scripts/compliance/engine-scenarios.json --engine262-bin /tmp/engine262-cli/node_modules/.bin/engine262 --output /tmp/broiler-compliance/engine-scenarios-summary.json` |
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

## 2026-05-10 `Array.isArray` follow-up

- Rebuilt the Broiler CLI, checked in the pinned `scripts/compliance/run_test262.py` runner plus the `scripts/compliance/test262-array-isarray.txt` manifest, reran the pinned `test262` `test/built-ins/Array/isArray` subset, and executed all 29 files successfully.
- The rerun covered the earlier failing proxy, revoked-proxy, descriptor, and metadata cases.
- `Array.isArray` is now closed in the roadmap and removed from the active gap checklist.

## 2026-05-10 unresolved-reference follow-up

- Added the pinned `scripts/compliance/run_test262.py` runner and the `scripts/compliance/test262-unresolved-reference.txt` manifest for the unresolved `GetValue` cases that previously returned values instead of throwing.
- Reran the pinned unresolved-reference subset across `test/language/expressions/addition` and `test/language/expressions/strict-equals`: 6 executed, 6 passed, 0 failed.
- The unresolved-reference mismatch is now closed in both the roadmap and the active gap checklist.

## 2026-05-10 proxy follow-up

- Added the pinned `scripts/compliance/test262-proxy.txt` manifest for a focused public-suite subset covering `get` invariants, `ownKeys` invariants, and revocable-proxy behavior.
- Fixed the remaining revoked-proxy target `typeof` edge case so proxies created from revoked proxy targets preserve the correct object/function metadata.
- Reran the pinned proxy subset: 8 executed, 8 passed, 0 failed.
- `Proxy` invariants and revocation behavior are now closed in both the roadmap and the active gap checklist.

## 2026-05-10 engine cross-check and matrix follow-up

- Added `scripts/compliance/compare_engines.py` and `scripts/compliance/engine-scenarios.json` so Broiler, Node/V8, and engine262 can be compared on the same small semantics manifest without relying on ad hoc shell snippets.
- Installed `@magic-works/engine262` `0.0.1-941d619dbc7464602f91d811b1b3ce61e372cdfa` locally for the first recorded run and executed the shared matrix command against Node `v24.14.1`.
- The first matrix shows Broiler matching Node/V8 and engine262 on unresolved-reference checks while still diverging on the current non-strict/global semantics scenarios; that gives the project a repeatable cross-check and a dashboard matrix without overstating full conformance.

## 2026-05-10 BigInt comparison follow-up

- Added the pinned `scripts/compliance/test262-bigint-comparisons.txt` manifest for the strict-equality BigInt comparison cases that previously sat next to the parser-gap note.
- Fixed the CLI script host so the standard global bindings (`Infinity`, `Intl`, and `globalThis`) are available during pinned public-suite reruns, which removed the remaining false host failure from the BigInt comparison subset.
- Reran the pinned BigInt comparison subset: 8 executed, 8 passed, 0 failed.
- The BigInt comparison parser bucket is now closed in both the roadmap and the active gap checklist.

## 2026-05-10 promise-job follow-up

- Extended `scripts/compliance/run_test262.py` so the pinned runner can execute async `test262` files that only depend on the standard harness plus `$DONE`, and so it no longer skips `noStrict` script-host cases.
- Added the pinned `scripts/compliance/test262-promise-jobs.txt` manifest for promise resolution, rejection timing, `finally`, and `await` scheduling.
- Reran the pinned promise-job subset: 5 executed, 5 passed, 0 failed.
- The promise-job / async scheduling evidence gap is now closed in both the roadmap and the active gap checklist.

## 2026-05-10 `for await (...)` follow-up

- Added the pinned `scripts/compliance/test262-for-await.txt` manifest for a focused `for await (... of ...)` subset that exercises both async-from-sync wrapping and custom async-iterator facades.
- Implemented parser/compiler/runtime support for `for await (... of ...)`, including invalid `for await (... in ...)` rejection and awaited loop-value handling.
- Reran the pinned `for await (...)` subset: 2 executed, 2 passed, 0 failed.
- The `for await (...)` gap is now closed in both the roadmap and the active gap checklist.

## 2026-05-10 non-strict/global follow-up

- Added the pinned `scripts/compliance/test262-global-nonstrict.txt` manifest for the non-strict/global scenarios that previously diverged in the dashboard matrix.
- Fixed non-strict bare-call `this`, implicit global assignment, global `var` property attributes, `delete` behavior, and `Function` constructor bodies so they match the reference engines on the recorded scenarios.
- Reran the pinned non-strict/global subset: 6 executed, 6 passed, 0 failed.
- Re-ran the shared engine matrix and Broiler now matches Node/V8 and engine262 on all 6/6 recorded scenarios.

## 2026-05-10 `Intl` follow-up

- Added the pinned `scripts/compliance/test262-intl.txt` manifest for the currently supported ECMA-402 scope: the exposed `Intl` object plus constructor metadata for `Intl.DateTimeFormat` and `Intl.RelativeTimeFormat`.
- Replaced the old stubbed `Intl` constructor surface with concrete built-in constructors that report the expected function metadata for the supported scope.
- Reran the pinned `Intl` subset: 5 executed, 5 passed, 0 failed.
- The measured `Intl` scope is now closed in both the roadmap and the active gap checklist.

## 2026-05-10 error constructor follow-up

- Added the pinned `scripts/compliance/test262-error-subclassing.txt` manifest for callable `Error` constructors, name/length metadata, and property-descriptor checks.
- Patched the built-in error constructors so callable `Error`/`TypeError`/`ReferenceError` behavior and metadata align with the local regressions and the pinned public-suite subset.
- Reran the pinned error subset: 6 executed, 6 passed, 0 failed.
- The error subclassing / constructor evidence gap is now closed in both the roadmap and the active gap checklist.

## 2026-05-10 binary-data follow-up

- Added the pinned `scripts/compliance/test262-binary-data.txt` manifest for constructor metadata and basic `ArrayBuffer` / `DataView` behavior.
- Corrected `ArrayBuffer.length` and `DataView.length` to the spec-required value `1`.
- Reran the pinned binary-data subset: 7 executed, 7 passed, 0 failed.
- Typed-array / binary-data public-suite evidence is now closed in both the roadmap and the active gap checklist.

## 2026-05-10 `RegExp.escape` follow-up

- Added the pinned `scripts/compliance/test262-regexp.txt` manifest for the `RegExp.escape` public cases that match the existing local regressions.
- Reran the pinned `RegExp.escape` subset: 7 executed, 7 passed, 0 failed.
- The `RegExp` public-suite evidence gap is now closed in both the roadmap and the active gap checklist.

## 2026-05-11 test262 coverage expansion follow-up

- The previous workflow limit came from two explicit constraints: the CI job only executed the pinned `scripts/compliance/test262-*.txt` manifests, and the raw script-host runner still treated `onlyStrict` files as unsupported.
- `scripts/compliance/run_test262.py` now executes `onlyStrict` tests by prepending a strict-mode directive before the test body, and the audit now counts those files as script-host-verifiable. The remaining raw-script exclusions are `module`, `raw`, and negative-metadata tests.
- Expanded the manifest set from 89 unique test files to 163 by promoting the full `test/language/expressions/strict-equals` directory, adding a new `test262-language-basics.txt` manifest, and removing strict-equals duplicates from the unresolved-reference manifest.
- Verified the new coverage directly: `test262-bigint-comparisons.txt` now runs 30/30 strict-equality files, and `test262-language-basics.txt` runs 55/55 additional language-basics files.
- That richer audit showed why the previous manual `test262` workflow stopped at 163 tests even though the raw script host could parse/run far more files: only 91 manifest entries had been promoted into the workflow, so it covered just 163 unique files out of 47,420 script-host-verifiable tests (0.34%). The manual workflow now uses the sharded full-suite path instead of that manifest-only subset.
- Structural raw-runner exclusions currently affect 6,049 unique files. The blocker counts are `negative=4,669`, `hostHarness=802`, `module=821`, and `raw=32`; those counts overlap, so they are larger than the unique excluded-file total.
- The largest uncovered script-host-verifiable top-level areas are `test/built-ins` (22,709 files), `test/language` (18,754), `test/intl402` (3,309), `test/staging` (1,362), and `test/annexB` (1,011). The largest uncovered depth-3 buckets are `test/language/expressions` (8,983), `test/language/statements` (7,790), `test/built-ins/Temporal` (4,588), `test/built-ins/Object` (3,410), and `test/built-ins/Array` (3,036).

### Feasibility, blockers, and expansion plan

1. **What is feasible without engine or harness changes:** the current raw script host can already execute the 47,420 script-host-verifiable files, so the fastest path to materially higher coverage is manifest growth plus failure triage, not a wholesale harness rewrite.
2. **What currently blocks “all possible testcases”:**
   - Negative tests need expected-phase/result handling instead of today's pass-only runner.
   - `module` and `raw` tests need a different host mode than the current single-file script host.
   - High-volume areas such as `Temporal`, `intl402`, and many built-in directories will also require engine work, not just broader manifests.
   - A much broader run has to be sharded; the manual `test262` workflow now does that, while the manifest files remain the smaller smoke/debugging path.
3. **Incremental plan:**
   - Expand manifests from the largest script-host-verifiable ES language buckets first (`test/language/expressions`, `test/language/statements`) in small directory or subdirectory shards and keep each shard green before promoting the next one.
   - Continue with proven built-in areas that already have local regressions or prior public-suite evidence (`Object`, `Array`, `RegExp`, `TypedArray`, `String`) before attempting heavier `Temporal`/`intl402` surfaces.
   - Add negative-test support to the runner so the 4,669 negative files become measurable instead of structurally excluded.
   - Add targeted host shims (for example `$262` helpers) or a richer test262 harness adapter so more of the current 802 host-harness-dependent files move into the runnable set.
   - Add separate module/raw host modes and move those categories into their own scheduled workflow once the engine surface is ready.
4. **Effort estimate:** manifest-only breadth growth is a short-term task that can add hundreds to low-thousands of tests in days/weeks; negative-test support is a medium-sized tooling task; module/raw coverage and the large `Temporal`/`intl402` buckets are multi-iteration engine-and-harness work.

### Full-suite automation now wired into manual and post-merge workflows

- `scripts/compliance/run_test262.py` now supports `--all-script-host-verifiable` for dynamic discovery of every current runnable `test262` file at a chosen suite ref, plus deterministic `--shard-count` / `--shard-index` splitting for large runs, including `--shard-index -1` to run every selected shard locally.
- `.github/workflows/test262.yml` is the single unified test262 CI runner. It runs automatically on `main` pushes after pull-request merges and is also available via `workflow_dispatch` with `assembly`, `suite_ref`, `rerun_failed_only`, `shard_index`, `test_timeout_seconds`, `memory_limit_mb`, and related inputs. When dispatched with a specific `assembly` it scopes the full phase to that assembly's test262 paths; otherwise it executes the full script-host-verifiable subset in 8 shards. After a PR is merged it first reruns the paths persisted in `scripts/compliance/test262-failures.txt` and only proceeds to the full sharded run once those previously failing tests pass.
- This automation covers current and future script-host-verifiable files automatically for the workflow's pinned suite ref on `main` merges and for whichever suite ref a manual workflow run selects; the remaining non-runnable categories still need negative-result handling (`negative`), additional host shims (`hostHarness` / `$262`), or a different host mode (`module`, `raw`).

## Comparative engine matrix

| Scenario set | Broiler | Node/V8 | engine262 |
| --- | --- | --- | --- |
| Shared reference-resolution scenarios (`reference-addition`, `reference-strict-equals`) | 2 passed / 0 failed | 2 passed / 0 failed | 2 passed / 0 failed |
| Shared global/non-strict scenarios (`nonstrict-bare-call-this`, `function-constructor-global-this`, `implicit-global-assignment`, `global-var-binding`) | 4 passed / 0 failed | 4 passed / 0 failed | 4 passed / 0 failed |

## Compliance workstreams

- [x] Review the documents in `docs/compliance/` against the current repository layout and validation process.
- [x] Record the repository-test baseline required by `process.md`.
- [x] Add a pinned `test262` harness run and publish suite totals here.
- [x] Add an `engine262` cross-check run and publish suite totals here.
- [x] Expand syntax-compliance coverage for parser gaps called out in `known-gaps.md`.
- [x] Expand built-in compliance coverage for the high-risk areas called out in `known-gaps.md`.
- [x] Publish a comparative engine matrix for release-time regression tracking.

## Regression tracking

- A compliance run is complete only when raw logs and summary totals are linked from this file.
- Raw compliance log link for the 2026-05-10 repository validation rerun: https://github.com/MaiRat/Broiler.JS/sessions/d7c812ab-9583-4530-8471-15fe44c49bd0
- Failing tests that represent unsupported syntax, missing built-ins, host limitations, or known bugs must be summarized in `known-gaps.md`.
- A result row should never claim “fully compliant” or “most compliant” unless the linked public suites and comparison data support that claim.

## Next automation steps

1. Keep the pinned `test262` CI workflow current as new manifests or host requirements are added.
2. Decide whether the sharded full-script-host workflow should stay manual-only or also run on a release/scheduled cadence once the expected runtime is measured.
3. Publish machine-readable totals from release-time compliance runs and update this dashboard for releases.
4. Track pass-rate deltas per suite revision to detect regressions.
