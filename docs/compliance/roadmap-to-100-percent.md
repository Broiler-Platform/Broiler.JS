# Roadmap to 100% compliance

This document turns the remaining compliance work into a small-step execution plan. It is intentionally evidence-first: Broiler.JS should not claim “100% compliant” until the public-suite totals, raw logs, and remaining known gaps all agree.

## Progress tracker

Use this checklist as the issue-level summary for roadmap progress. Update the matching item here whenever a roadmap bucket is completed or split into linked sub-issues.

- [ ] 1. Pin and automate `test262`
- [ ] 2. Add `engine262` cross-check coverage
- [ ] 3. Publish raw artifacts
- [ ] 4. Build the comparison matrix
- [ ] 5. Implement `for await (...)`
- [x] 6. Resolve the async object accessor parser note
- [ ] 7. Fix BigInt comparison parse failures
- [ ] 8. Finish non-strict/global semantics validation
- [ ] 9. Align unresolved-reference behavior in `+` and `===`
- [ ] 10. Verify promise jobs and async scheduling
- [x] 11. Finish `Array.isArray`
- [ ] 12. Validate `Intl`
- [ ] 13. Prove `Proxy` invariants and revocation behavior
- [ ] 14. Cover typed arrays, `ArrayBuffer`, and `DataView`
- [ ] 15. Finish `RegExp.escape` and related `RegExp` conformance
- [ ] 16. Validate error subclassing
- [x] 17. Convert the remaining open gaps into tracked batches
- [ ] 18. Define the final “ready to claim” checklist

## Issue update template

When posting a progress checkpoint in the tracking issue, use this format so blockers, milestones, and linked work stay consistent:

- **Status:** not started / in progress / blocked / done
- **Roadmap item:** `<number and title from the checklist above>`
- **Completed since last update:** `<tests, fixes, or docs landed>`
- **Blockers:** `<none or list>`
- **Links:** `<PRs, sub-issues, failing suite directory, regression test, implementation file>`
- **Next checkpoint:** `<next concrete step>`

## What “100% compliant” means here

Treat the goal as all of the following being true at the same time:

1. `dotnet test Broiler.JS.slnx` passes.
2. The repository-local compatibility scripts still pass.
3. A pinned `test262` run is recorded in `docs/compliance/dashboard.md`.
4. The recorded `test262` scope has no remaining unexpected failures in the supported host mode.
5. `docs/compliance/known-gaps.md` no longer lists open language or built-in mismatches for the supported scope.
6. Raw logs or CI artifacts are linked from `docs/compliance/dashboard.md`.

If any one of those is still missing, the project is not ready to publish a full compliance claim.

## Execution rules for every gap

Apply the same lifecycle to every remaining item:

1. Reproduce the failing behavior with a minimal public-suite file or a minimal local script.
2. Add or extend a focused xUnit regression test in the nearest `*.Tests` project.
3. Fix the implementation in the parser, compiler, runtime, or built-in layer.
4. Re-run the focused regression test.
5. Re-run the relevant public-suite subset.
6. Update `docs/compliance/dashboard.md` with the new totals.
7. Remove or narrow the matching entry in `docs/compliance/known-gaps.md`.

## Phase 1: finish measurement and reporting first

The first blocker to a “100%” claim is missing measurement, not just missing behavior.

### 1. Pin and automate `test262`

1. Create a repeatable harness command that runs the Broiler host against a pinned `test262` commit.
2. Store the pinned commit SHA in the compliance workflow or release process instead of using `main`.
3. Start with the already-executed failing directories:
   - `test/language/expressions/addition`
   - `test/language/expressions/strict-equals`
   - `test/built-ins/Array/isArray`
   - `test/built-ins/RegExp/escape`
4. Expand from those directories to broader language and built-in coverage after each bucket is clean.
5. Record the exact command, suite revision, totals, and exclusions in `docs/compliance/dashboard.md`.

### 2. Add `engine262` cross-check coverage

1. Define a small Broiler-vs-engine262 comparison command for language semantics that are hard to judge from local tests alone.
2. Start with async behavior, reference resolution, and global/non-strict semantics.
3. Record the command, revision, and totals in `docs/compliance/dashboard.md`.
4. Keep the first run small; widen it only after the workflow is stable.

### 3. Publish raw artifacts

1. Add a CI or release job that uploads the raw compliance logs.
2. Make `docs/compliance/dashboard.md` link to those artifacts instead of only local command lines.
3. Fail the compliance job if totals are missing or artifact upload fails.

### 4. Build the comparison matrix

1. Reuse the same filtered test set across Broiler, Chromium/V8, SpiderMonkey, JavaScriptCore, Jint, and engine262.
2. Record pass/fail totals per engine in one table in `docs/compliance/dashboard.md`.
3. Use that matrix only after the commands are identical enough to be comparable.

## Phase 2: close parser gaps

These are the fastest remaining items because the parser already points to the unsupported areas.

### 5. Implement `for await (...)`

Primary source: `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Parser/FastParser.ForStatement.cs`

1. Replace the current `await` rejection path in `ForStatement`.
2. Decide the AST shape to represent `for await (... of ...)` without weakening existing `for`, `for...in`, or `for...of` parsing.
3. Add parser tests for:
   - valid `for await (x of y) {}`
   - valid declaration forms
   - invalid `for await (... in ...)`
   - invalid use outside async-allowed contexts if applicable
4. Add execution tests that prove iteration semantics, early exit, and awaited values.
5. Run the matching `test262` `for-await-of` subset.
6. Update `known-gaps.md` only after parser and execution results both pass.

### 6. Resolve the async object accessor parser note

Primary source: `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Parser/FastParser.ObjectLiteral.cs`

1. Confirm the intended supported syntax against `test262` and a reference engine before changing the parser.
2. Keep rejecting `async get foo()` / `async set foo()` if the public suite confirms they are invalid ECMAScript syntax.
3. Add parser regressions proving those invalid forms are rejected in object literals and class bodies.
4. Add parser coverage proving valid async methods named `get` and `set` still parse in object literals and class bodies.
5. Document the distinction so the compliance notes do not treat invalid syntax as an implementation gap.

### 7. Fix BigInt comparison parse failures

Primary source: parser handling for `===` and BigInt literals, especially valid `StrictlyEqual` cases noted in the dashboard.

1. Reproduce each currently failing BigInt comparison parse from the recorded subset.
2. Add parser regressions for every valid `===` form that still throws.
3. Fix tokenization or expression parsing so valid BigInt comparisons survive to runtime.
4. Re-run the exact failing `test262` files before widening the subset.

## Phase 3: close execution-semantic gaps

### 8. Finish non-strict/global semantics validation

Primary source: `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/OtherTests/JIntPerfTests/Program.cs`

1. Enumerate every behavior that still differs between strict and non-strict global execution in Broiler.
2. Add focused integration tests for:
   - bare-call `this`
   - implicit globals
   - assignment to undeclared identifiers
   - `Function` constructor bodies
   - global `var` bindings
3. Compare those behaviors against Chromium and `test262`.
4. Run a non-strict-focused public-suite subset after each fix.
5. Remove the gap only when the public-suite evidence matches the local regressions.

### 9. Align unresolved-reference behavior in `+` and `===`

1. Use the failing `test262` cases mentioned in `known-gaps.md` as the first regression list.
2. Add execution tests proving `ReferenceError` is thrown for unresolved reads in:
   - addition
   - strict equality
   - grouped expressions
   - left/right operand order variations
3. Inspect compiler fast paths for native-number/native-string shortcuts so they do not bypass identifier resolution.
4. Re-run the `addition` and `strict-equals` subsets until no unresolved-reference mismatches remain.

### 10. Verify promise jobs and async scheduling

1. The current promise job queue entry points are already identified in the runtime, including promise reaction posting, async-function continuation queuing, and the synchronous async pump used by script execution.
2. Focused local regressions now cover:
   - microtask ordering
   - nested promise resolution
   - `async`/`await` continuation order
   - rejection timing
3. Cross-check those results with `engine262` or a reference engine.
4. Add public-suite coverage before closing the gap.

## Phase 4: close built-in conformance gaps

### 11. Finish `Array.isArray`

1. Start from the currently known failing cases:
   - `Array.prototype`
   - proxies
   - revoked proxies
   - constructor/descriptor metadata
2. Focused built-in regressions now cover each of those cases, including nested proxies and descriptor metadata.
3. Keep the current array-brand check and proxy unwrapping path aligned with those regressions.
4. The pinned `test262` `Array/isArray` subset is now clean on the recorded 2026-05-10 rerun, so this item is closed.

### 12. Validate `Intl`

1. Decide the supported ECMA-402 scope that Broiler can realistically claim.
2. Run an ECMA-402-focused suite or filtered `test262` `intl402` subset.
3. Split results into:
   - implemented and correct
   - implemented but divergent
   - intentionally unsupported
4. Do not claim full internationalization compliance until the supported scope is measured.

### 13. Prove `Proxy` invariants and revocation behavior

1. Add regression tests around:
   - `get`/`set` invariants
   - non-configurable property checks
   - `ownKeys` invariants
   - revoked proxy failures
2. Run the matching public-suite subset.
3. Close the gap only after both local and public tests agree.

### 14. Cover typed arrays, `ArrayBuffer`, and `DataView`

1. Build a file-by-file checklist for constructors, indexed access, endian reads/writes, and detach behavior.
2. Add missing local tests first.
3. Then run the relevant `test262` built-in directories.
4. Treat detach/transfer edge cases as release blockers because they often expose host/runtime bugs.

### 15. Finish `RegExp.escape` and related `RegExp` conformance

1. Start from the dashboard’s already-known divergences:
   - initial-character escaping
   - punctuator escaping
   - surrogate handling
   - descriptor checks
2. Add local regression tests for each failing public case.
3. Fix `RegExp.escape` behavior first.
4. Expand to flags, captures, and Unicode edge cases after the escape subset is clean.
5. Re-run the same `test262` files after every change.

### 16. Validate error subclassing

1. Add regression coverage for `Error`, `TypeError`, `ReferenceError`, and custom subclass chains.
2. Verify constructor names, prototype chains, `instanceof`, and message propagation.
3. Run the matching public-suite subset.

## Phase 5: turn the work into a release gate

### 17. Convert the remaining open gaps into tracked batches

1. Create one issue or tracked checklist entry per gap bucket.
2. Link each issue back to:
   - the failing suite directory
   - the local regression test
   - the implementation file
3. Keep `docs/compliance/known-gaps.md` short by linking to issue details instead of expanding prose there.

#### Tracked gap batches

Use this checklist as the canonical per-bucket index until the work moves into dedicated issues. Each row names the public-suite scope, the nearest local regression or validation source, and the implementation area that should change with the next fix.

| Batch | Public suite / evidence scope | Local regression / validation source | Implementation area |
| --- | --- | --- | --- |
| `measurement-test262` | Pinned `test262` automation for `test/language/expressions/addition`, `test/language/expressions/strict-equals`, `test/built-ins/Array/isArray`, and `test/built-ins/RegExp/escape` | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Integration.Tests/M8ValidationTests.cs` | `/home/runner/work/Broiler.JS/Broiler.JS/docs/compliance/process.md`, `/home/runner/work/Broiler.JS/Broiler.JS/docs/compliance/dashboard.md`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript/Program.cs` |
| `measurement-engine262` | First `engine262` smoke/cross-check command and totals | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/BuiltInsTests.cs` promise/unresolved-reference regressions | `/home/runner/work/Broiler.JS/Broiler.JS/docs/compliance/dashboard.md`, `/home/runner/work/Broiler.JS/Broiler.JS/docs/compliance/process.md` |
| `measurement-artifacts` | Raw log publication for compliance runs | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Integration.Tests/M8ValidationTests.cs` | `/home/runner/work/Broiler.JS/Broiler.JS/docs/compliance/dashboard.md`, `/home/runner/work/Broiler.JS/Broiler.JS/docs/compliance/process.md` |
| `measurement-matrix` | Comparative engine matrix for the shared filtered suite set | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Integration.Tests/M8ValidationTests.cs` | `/home/runner/work/Broiler.JS/Broiler.JS/docs/compliance/dashboard.md`, `/home/runner/work/Broiler.JS/Broiler.JS/docs/compliance/known-gaps.md` |
| `parser-for-await` | `test/language/statements/for-await-of` and related parser coverage | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Parser.Tests/ParserTests.cs` | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Parser/FastParser.ForStatement.cs` |
| `semantics-global-nonstrict` | Non-strict/global behavior cross-checks plus matching public-suite coverage | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/OtherTests/JIntPerfTests/Program.cs` | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/OtherTests/JIntPerfTests/Program.cs`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript/Program.cs` |
| `semantics-reference-resolution` | `test/language/expressions/addition` and `test/language/expressions/strict-equals` unresolved-reference cases | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/BuiltInsTests.cs` (`Unresolved_Identifier_Reads_Throw_ReferenceError_*`) | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Compiler/Expressions/FastCompiler.VisitBinaryExpression.cs`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Engine/JSContext.cs` |
| `semantics-bigint-comparisons` | Remaining BigInt comparison files from the recorded public subset | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/BuiltInsTests.cs` (`Prefixed_BigInt_Literals_Parse_And_Compare_Correctly`, `BigInt_Relational_Comparisons_*`) | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Parser/FastScanner.cs`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Compiler/Expressions/FastCompiler.VisitBinaryExpression.cs` |
| `semantics-promise-jobs` | Promise-job / async scheduling public-suite coverage and reference-engine rerun | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/BuiltInsTests.cs` (`Promise_Reactions_Run_After_Synchronous_Code`, `Promise_Nested_Resolution_Assimilates_Inner_Promise`, `Promise_Rejection_Handlers_Run_In_Microtask_Order`) | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns/Promise/JSPromise.cs`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns/Promise/JSPromiseStatic.cs`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Engine/JSContext.cs` |
| `builtins-intl` | Filtered `test262` `intl402` / ECMA-402 scope decision | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Integration.Tests/M7ValidationTests.cs` | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns/Intl/JSIntl.cs`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Globals/JSGlobal.cs` |
| `builtins-proxy` | Proxy invariant and revocation public-suite subset | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/BuiltInsTests.cs` (`Proxy_Revoked_Get_Set_And_ObjectKeys_Throw_TypeError` and nearby proxy invariant checks) | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns/Proxy/JSProxy.cs` |
| `builtins-binary-data` | Typed-array, `ArrayBuffer`, and `DataView` built-in subset with detach/transfer cases | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/BuiltInsTests.cs` (`DataView_*`, `ArrayBuffer_Transfer_*`, `StructuredClone_Transfer_*`) | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSArrayBuffer.cs`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns/Array/Typed/JSTypedArray.cs`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns/DataView/DataView.cs` |
| `builtins-regexp` | `test/built-ins/RegExp/escape` plus wider `RegExp` built-in subset | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/BuiltInsTests.cs` (`RegExp_Escape_*`, `RegExp_Flags_Are_Normalized_*`, `RegExp_Sticky_*`) | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs`, `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.Parser/RegExpValidator.cs` |
| `builtins-error-subclassing` | Error constructor / subclass public-suite subset | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/BuiltInsTests.cs` (`Error_Constructors_Preserve_Names_Prototypes_And_Messages`, `Custom_Error_Subclass_Chains_Preserve_Instanceof_And_Message`) | `/home/runner/work/Broiler.JS/Broiler.JS/Broiler.JS/Broiler.JavaScript.BuiltIns/Error/JSError.cs` |

### 18. Define the final “ready to claim” checklist

Do not publish a 100% compliance statement until this exact checklist is complete:

1. `dotnet test Broiler.JS.slnx` passes.
2. `Broiler.JS/OtherTests/JIntPerfTests` passes from its own directory.
3. Pinned `test262` totals are published.
4. `engine262` totals are published if it remains part of the comparison story.
5. The comparison matrix is published.
6. `docs/compliance/known-gaps.md` has no open items in the supported scope.
7. `docs/compliance/dashboard.md` links to raw artifacts.
8. The release notes describe the exact scope of the claim.

## Suggested execution order

To minimize churn, do the remaining work in this order:

1. Pin and automate `test262`.
2. Close `Array.isArray`.
3. Close unresolved-reference behavior in `+` and `===`.
4. Close the remaining BigInt comparison parser failures.
5. Close `RegExp.escape`.
6. Implement `for await (...)`.
7. Keep the async object accessor parser note closed.
8. Validate non-strict/global semantics broadly.
9. Validate promise job queue behavior.
10. Expand to `Proxy`, typed arrays, `ArrayBuffer`, `DataView`, and error subclassing.
11. Measure `Intl`.
12. Publish artifacts and the comparison matrix.

This order keeps the fastest measured wins first, reduces repeated public-suite reruns, and delays the broadest built-in surfaces until the harness and reporting pipeline are stable.
