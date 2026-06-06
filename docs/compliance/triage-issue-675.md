# test262 triage — issue #675

This document triages the ten most-common `test262` failure categories reported
by the automated full script-host runner in
[issue #675](https://github.com/MaiRat/Broiler.JS/issues/675).

It follows the first step of the [gap lifecycle](known-gaps.md#gap-lifecycle):
*documented failing suite area*. The tracked-batch index in
[`roadmap-to-100-percent.md`](roadmap-to-100-percent.md#tracked-gap-batches)
carries the canonical per-bucket rows (`gap-675-missing-builtins`,
`gap-675-overlap-673`).

- **Workflow run:** https://github.com/MaiRat/Broiler.JS/actions/runs/27057115266
- **Suite ref:** `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`
- **Artifact:** `test262-logs`

The line numbers quoted in the issue (`Throw @ 115`, `Compile @ 205/206`,
`InitializeFactories @ 17`, `InvokeMethod @ 59`) are locations inside Broiler's
own host/runtime, so they group failures by *where the exception surfaced*, not
by a single shared cause.

## Summary

| # | Issue #675 problem | Status | Maps to |
| --- | --- | --- | --- |
| 1 | Structural `deepEqual` mismatch (`Array.from`, `delegating-yield-*`, `object/entries`) | `delegating-yield-*` **fixed** in #673; rest open | #673 cat 1 |
| 2 | `Cannot get property value of undefined` — `Intl.DateTimeFormat` range formatting | open | #673 cat 3 |
| 3 | `ex1` — `finally` abrupt completion over a pending throw | **fixed** | #673 cat 4 |
| 4 | `innerX === 2. Actual: 4` — direct-`eval` var injection | open | #673 cat 5 |
| 5 | `SameValue(false, true)` mismatches | open (mixed) | #673 cat 6 |
| 6 | `Unexpected token Hash: #` — Unicode `ID_Start` + private names | open | #673 cat 7 |
| 7 | `Unexpected token Identifier: var` — Unicode `ID_Start` | open | #673 cat 9 |
| 8 | `Method add not found in 1` — `Set.prototype.add` | **fixed** | new |
| 9 | `Method select not found in [object Intl.PluralRules]` | **fixed** | new |
| 10 | `Method withResolvers not found in … Promise` | **fixed** | new |

## Problems 1, 2, 4–7 — carried over from issue #673

These reproduce root causes already triaged in
[`triage-issue-673.md`](triage-issue-673.md); the issue #675 sample paths are the
same files (or near-identical variants). Their status is tracked by the
`gap-673-*` rows in the roadmap:

- **Problem 1** (structural `deepEqual`) → #673 category 1. The
  `staging/sm/generators/delegating-yield-*` sub-cause is **fixed** (a `yield*`
  expression now evaluates to the delegated iterator's return value). The
  `Array/from_proxy.js`, `Array/from_string.js` and `object/entries.js` samples
  remain open and are tracked under `gap-673-triage-remaining`. (`Object.entries`
  itself reproduces correctly locally; the `object/entries.js` failure is a
  narrower sub-case still to be reduced.)
- **Problem 2** (`Intl.DateTimeFormat` `formatRange`/`formatRangeToParts`) → #673
  category 3, tracked under `gap-673-intl-range`. Locally `formatRange` returns
  the raw epoch-millisecond values joined by an en dash instead of formatted
  dates, so making these pass needs real locale formatting (a stub today).
- **Problem 4** (direct-`eval` `var` injection + compound-assignment lref
  timing) → #673 category 5, tracked under `gap-673-eval-injection`.
- **Problem 5** (`SameValue(false, true)`) → #673 category 6, mixed root causes
  tracked under `gap-673-triage-remaining`.
- **Problems 6 & 7** (`#` private names and `var` after a supplementary-plane
  `ID_Start` code point) → #673 categories 7 & 9, tracked under
  `gap-673-unicode-identifiers` (needs a version-pinned Unicode derived-property
  table in `Broiler.Unicode`).

No new work is landed for these here; see the #673 triage for the per-category
root-cause analysis and next steps.

## Problem 3 — `finally` abrupt completion must override a pending throw (fixed)

- **Exception:** `JSException @ Throw` surfacing as a bare `ex1`
- **Samples:** `language/statements/try/S12.14_A9_T2.js`, `A10_T2.js`,
  `A11_T2.js`, `A12_T2.js`

The decisive sub-case (CHECK#6 of `S12.14_A9_T2`):

```js
do {
  try { c6 += 1; throw "ex1"; }
  finally { fin6 = 1; continue; }   // abrupt "continue" must discard the throw
} while (c6 < 2);
```

Per §14.15, when a `finally` block completes abruptly (here `continue`), its
completion *replaces* the pending completion of the `try`/`catch` — so the thrown
`"ex1"` is discarded and the loop continues. Broiler instead let the original
`"ex1"` propagate uncaught.

**Root cause.** `try/finally` is emitted as a real CLR `finally`;
`continue`/`break`/`return` out of the finally records a *deferred jump*
(`ILTryBlock.Branch` sets `finallyJumpState` and branches to `endfinally`) that is
dispatched *after* `EndExceptionBlock`. On the normal path, `endfinally` falls
through to that dispatch and the jump wins. But when a CLR exception is unwinding,
`endfinally` **re-raises** it and never reaches the dispatch — so the jump is lost
and the throw escapes.

**Fixed.** `ILCodeGenerator.VisitTryCatchFinally` now detects a finally that can
complete abruptly (`FinallyBranchScanner`: a `return`, or a `break`/`continue`
whose target label is declared outside the finally) and asks
`ILWriter.BeginTry` to wrap the whole construct in an **outer `try`/`catch
(Exception)`** guard. On the exception path the guard inspects `finallyJumpState`:
if the finally requested a jump it discards the exception and falls through to the
deferred-jump dispatch (now emitted outside the outer region); otherwise it
re-raises. The guard is *only* emitted when the finally actually branches out, so
the common `try/finally` path — its `SavedLocal` result, tail-call transparency
and nested-try handling — is byte-for-byte unchanged. The scan never produces a
false negative (only labels declared inside the finally are treated as internal).

Covered by `Issue675Tests.cs` (`FinallyContinue_*`, `FinallyReturn_*`,
`FinallyBreak_*`, `NonBranchingFinally_*`, `NestedBranchingFinally_*`,
`CatchRethrow_ThenFinallyContinue_*`). Implementation:
`Broiler.JS/Broiler.JavaScript.ExpressionCompiler/Generator/ILCodeGenerator.VisitTryCatchFinally.cs`,
`.../Generator/FinallyBranchScanner.cs`,
`.../Core/ILWriter.cs`, `.../Core/ILTryBlock.cs`.

## Problems 8–10 — new missing or incorrect built-ins (fixed)

These three buckets are *not* present in issue #673. Each is a missing or
spec-incorrect built-in surface; all three are fixed in this change, with
regressions in
`Broiler.JS/Broiler.JavaScript.Integration.Tests/Issue675Tests.cs`.

### Problem 8 — `Set.prototype.add` must return the Set

- **Exception:** `JSException @ InvokeMethod` — `Method add not found in 1`
- **Samples:** `built-ins/Set/prototype/add/preserves-insertion-order.js`,
  `Set/prototype/clear/clears-all-contents.js`,
  `Set/prototype/forEach/iterates-in-insertion-order.js`

Per §24.2.4.1, `Set.prototype.add ( value )` returns the Set (`Return S`) so
calls can be chained: `set.add(1).add(2).add(3)`. Broiler's `JSSet.Add` returned
the *value* instead, so `set.add(1)` evaluated to `1` and the chained `.add(2)`
was dispatched on the number `1` — surfacing as `Method add not found in 1`. The
sample files all build their fixture with a chained `add`, so the whole file
aborts before its assertions run.

**Fixed.** `JSSet.Add` now returns `this`
(`Broiler.JS/Broiler.JavaScript.BuiltIns/Set/JSSet.cs`). The internal callers
(`union`/`intersection`/`difference`/…) already ignore the return value, so the
change is local to the public method. `WeakSet.prototype.add` had the identical
bug (returned the value) and is fixed the same way
(`Broiler.JS/Broiler.JavaScript.BuiltIns/Set/JSWeakSet.cs`).

### Problem 9 — `Intl.PluralRules.prototype.select` was missing

- **Exception:** `JSException @ InvokeMethod` —
  `Method select not found in [object Intl.PluralRules]`
- **Samples:** `intl402/PluralRules/prototype/select/non-finite.js`,
  `select/notation.js`, `select/tainting.js`

The `Intl.PluralRules` prototype exposed `resolvedOptions` and `selectRange` but
not `select`, the primary method of the type. Calling `pr.select(n)` therefore
failed to resolve the method.

**Fixed.** `select` is now installed on the prototype
(`Broiler.JS/Broiler.JavaScript.BuiltIns/Intl/JSIntl.cs`). It validates the
receiver, coerces the argument with `ToNumber`, and resolves the CLDR plural
category via `JSIntlPluralRules.SelectCategory`. Non-finite inputs resolve to
`"other"` (matching `ResolvePlural`); finite inputs use the English (`en`) rules
already reflected by `resolvedOptions().pluralCategories` — cardinal →
`one`/`other`, ordinal → `one`/`two`/`few`/`other` — consistent with the
engine's locale approximation. `select` performs no user-observable property
reads beyond the spec's `ToNumber`, so the `tainting` sample is unaffected.

### Problem 10 — `Promise.withResolvers` was missing

- **Exception:** `JSException @ InvokeMethod` —
  `Method withResolvers not found in function Promise() { [native code] }`
- **Samples:** `built-ins/Promise/withResolvers/promise.js`,
  `withResolvers/resolvers.js`, `withResolvers/result.js`

`Promise.withResolvers` (§27.2.4.10, ES2024) was not implemented.

**Fixed.** A static `withResolvers` is added to the `Promise` constructor
(`Broiler.JS/Broiler.JavaScript.BuiltIns/Promise/JSPromiseStatic.cs`). It reuses
the existing `CreatePromiseFromConstructor` (NewPromiseCapability) machinery to
build the promise via the receiver constructor and capture its resolve/reject
functions, then returns a plain object with `promise`, `resolve` and `reject`
data properties. Because the method body reads only `this`, the inferred
function `length` is `0`, as the spec requires.

## Building this repo for local repro

As recorded in [`triage-issue-673.md`](triage-issue-673.md#building-this-repo-for-local-repro),
two prerequisites are needed before `dotnet test` works: the git submodules
(`git submodule update --init --recursive`) and a Roslyn ≥ 5.3.0 compiler for the
`JSClassGenerator` source generator.
