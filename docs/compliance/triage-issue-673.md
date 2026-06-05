# test262 triage — issue #673

This document triages the ten most-common `test262` failure categories reported
by the automated full script-host runner in
[issue #673](https://github.com/MaiRat/Broiler.JS/issues/673).

It follows the first step of the [gap lifecycle](known-gaps.md#gap-lifecycle):
*documented failing suite area*. Each category below records the observed
exception, a representative failing file, a root-cause analysis traced to the
implementation, and the concrete next step (minimal repro test → fix → public
suite rerun). The tracked-batch index in
[`roadmap-to-100-percent.md`](roadmap-to-100-percent.md#tracked-gap-batches)
carries the canonical per-bucket rows.

- **Workflow run:** https://github.com/MaiRat/Broiler.JS/actions/runs/27043367528
- **Suite ref:** `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`
- **Artifact:** `test262-logs`

The line numbers quoted in the issue (`Throw @ 115`, `Compile @ 205/206`,
`InitializeFactories @ 17`) are locations inside Broiler's own host/runtime, so
they group failures by *where the exception surfaced*, not by a single shared
cause. The categories below are split by actual root cause where the samples
diverge.

## Severity / confidence summary

| # | Category | Root-cause confidence | Implementation area |
| --- | --- | --- | --- |
| 1 | Structural `deepEqual` mismatches | low — needs per-file repro | `BuiltIns/Array`, generator `yield*` |
| 2 | `IteratorClose` on generator `return()` | high | `GeneratorsV2/GeneratorRewriter.cs` |
| 3 | `Intl.DateTimeFormat` range formatting | medium | `BuiltIns/Intl/JSIntl.cs` |
| 4 | `finally` abrupt completion override | high | `ExpressionCompiler` try/finally |
| 5 | Direct `eval` var injection + lref timing | high | engine/runtime eval + identifier resolution |
| 6 | `SameValue(false, true)` mismatches | low — needs per-file repro | mixed |
| 7 | `#` private name after Unicode id | high (shares #9) | `Parser/CharExtensions.cs` |
| 8 | `Unexpected token … constructor` | low — needs per-file repro | `Parser` class body |
| 9 | Unicode ID_Start identifiers | high | `Parser/CharExtensions.cs` (Unicode data version) |
| 10 | `Value is not iterable` | medium | iterator protocol / `yield*` |

## Category 2 — IteratorClose on generator `return()` (high confidence)

- **Exception:** `Test262Error: Expected SameValue(«0», «1») to be true`
- **Samples:** `language/expressions/assignment/dstr/array-rest-iter-rtrn-close.js`,
  `language/expressions/assignment/dstr/array-elem-trlg-iter-rest-rtrn-close.js`,
  `language/statements/for-of/dstr/array-rest-iter-rtrn-close.js`,
  `staging/sm/generators/yield-iterator-close.js`

These tests drive a generator that suspends on a `yield` placed *inside* a
destructuring rest target, e.g.:

```js
function* g() {
  result = [ x , ...{}[yield] ] = vals;   // yield inside the rest target
}
iter = g();
iter.next();            // consumes one element of `vals`
result = iter.return(999);   // abrupt "return" completion through the yield
```

Per `IteratorClose`, when the abrupt `return` unwinds while the iterator is not
`done`, `iterator.return()` must be called exactly once (`returnCount === 1`).
The reported value is `0`, so the close never runs.

Broiler already emits the correct close machinery for the **throw** and
**break/return-out-of-loop** paths:

- Array destructuring wraps its element assignments in a `TryCatchFinally`
  with a "close if not done" finally and a "close-ignoring-errors then rethrow"
  catch — `Broiler.JS/Broiler.JavaScript.Compiler/Expressions/FastCompiler.VisitAssignmentExpression.cs:546`.
- `for-of` does the same — `Broiler.JS/Broiler.JavaScript.Compiler/Statements/FastCompiler.VisitFor.cs:166`.

The gap is specific to a generator `return()` *resuming a suspended `yield`*:
the generator state machine produced by
`Broiler.JS/Broiler.JavaScript.LinqExpressions/LinqExpressions/GeneratorsV2/GeneratorRewriter.cs`
does not run the enclosing `finally` (and therefore the IteratorClose) when the
resumption is a return-completion rather than a normal value or a thrown error.

- **Next step:** add a generator-return regression in
  `Broiler.JavaScript.Runtime.Tests` (or `BuiltIns.Tests`) asserting
  `returnCount === 1` for the snippet above, then make the rewriter unwind
  return-completions through enclosing `try/finally` regions before reaching the
  generator boundary.

## Category 4 — `finally` abrupt completion must override a pending throw (high confidence)

- **Exception:** `Test262Error: ... ` surfacing as a bare `JSException: ex1`
- **Samples:** `language/statements/try/S12.14_A9_T2.js`,
  `S12.14_A10_T2.js`, `S12.14_A11_T2.js`, `S12.14_A12_T2.js`

The decisive sub-case (CHECK#6 of `S12.14_A9_T2`):

```js
do {
  try { c6 += 1; throw "ex1"; }
  finally { fin6 = 1; continue; }   // abrupt "continue" must discard the throw
  fin6 = -1;
} while (c6 < 2);
```

Per the spec, when a `finally` block completes abruptly (here `continue`), its
completion *replaces* any pending completion from the `try` block — so the
thrown `"ex1"` is discarded and the loop continues. Broiler instead lets the
original `"ex1"` propagate uncaught, which is why the failure surfaces as a raw
`JSException` whose message is the thrown string `ex1`.

- **Implementation area:** the branch-out-of-`finally` machinery in the IL
  generator — `Broiler.JS/Broiler.JavaScript.ExpressionCompiler/Generator/ILCodeGenerator.VisitTryCatchFinally.cs`
  and `Broiler.JS/Broiler.JavaScript.ExpressionCompiler/Core/ILTryBlock.cs`
  (`Branch`, which defers jumps out of a `finally` until after `endfinally`).
- **Next step:** add a parser/runtime regression covering `continue`, `break`,
  and `return` inside `finally` over a pending throw, then ensure a deferred
  jump out of `finally` clears the in-flight exception.

## Category 5 — direct `eval` var injection + compound-assignment lref timing (high confidence)

- **Exception:** `Test262Error: #1: innerX === 2. Actual: 4`
- **Samples:** `language/expressions/compound-assignment/S11.13.2_A6.4_T1.js`
  (and `A6.5`, `A6.7`, `A6.8`)

```js
function testCompoundAssignment() {
  var x = 3;
  var innerX = (function () {
    x += (eval("var x = 2;"), 1);   // lref for x captured before RHS runs
    return x;                       // sees the eval-injected local x === 2
  })();
  // expected: innerX === 2, outer x === 4
}
```

Two requirements combine here:

1. Non-strict **direct `eval`** must inject `var x` into the *calling
   function's* variable environment.
2. The compound-assignment `x += …` must capture its reference (`lref`) to the
   outer `x` **before** evaluating the right-hand side, so `PutValue` targets the
   outer binding (→ outer `x === 4`), while the later `return x` resolves
   dynamically to the eval-injected local (→ `innerX === 2`).

Broiler returns `innerX === 4`, i.e. `return x` still resolves statically to the
outer binding and the eval-injected local is never created/visible. This needs
direct-eval binding injection plus dynamic identifier resolution in functions
that contain a direct `eval`.

- **Implementation area:** eval semantics in the engine/runtime and identifier
  binding in `Broiler.JavaScript.Compiler` (functions containing a direct
  `eval` must not fully statically bind names that eval could shadow).
- **Next step:** add a focused regression for direct-eval `var` injection, then
  the lref-timing case; this is a larger feature and should be split into its own
  tracked batch.

## Categories 7 & 9 — Unicode ID_Start identifiers and `#` private names (high confidence)

- **Exceptions:** `Unexpected token Identifier: var at 205, 4` (cat 9);
  `Unexpected token Hash: # at 206, 3` (cat 7)
- **Samples:** `language/identifiers/start-unicode-15.1.0.js`,
  `start-unicode-16.0.0.js`, `start-unicode-17.0.0.js`, and their `-class.js`
  variants.

These files declare thousands of variables (or class private names) whose first
character is a supplementary-plane code point that is `ID_Start` under Unicode
15.1 / 16.0 / 17.0. The parse fails partway through (line ~205/206), i.e. *some*
of the code points are accepted and one is not.

`Broiler.JS/Broiler.JavaScript.Parser/CharExtensions.cs:88` classifies identifier
start characters with `char.GetUnicodeCategory(...)`, which is bound to the
Unicode version shipped with the host .NET runtime (.NET 8 ≈ Unicode 15.x).
Code points added or re-categorised in 15.1 / 16.0 / 17.0 are therefore not
recognised, so:

- the non-class test stops at the first unrecognised `ID_Start` and reports the
  following `var` as unexpected (cat 9); and
- the class variant leaves the private-name `#<char>` unconsumed, so the scanner
  reports the `#` (`Hash`) token as unexpected (cat 7).

The empty top-level `Broiler.Unicode` project is the natural home for a
version-pinned `ID_Start` / `ID_Continue` table so identifier classification no
longer depends on the host runtime's Unicode version.

- **Next step:** add a parser regression that declares one variable per affected
  code point, populate `Broiler.Unicode` with a pinned derived-property table,
  and route `IsIdentifierStart` / `IsIdentifierPart` through it.

## Category 3 — `Intl.DateTimeFormat` range formatting (medium confidence)

- **Exception:** `JSException: Cannot get property value of undefined`
- **Samples:** `intl402/DateTimeFormat/prototype/formatRange/en-US.js`,
  `formatRange/fractionalSecondDigits.js`, `formatRangeToParts/en-US.js`,
  `formatRangeToParts/fractionalSecondDigits.js`,
  `formatToParts/offset-timezone-correct.js`

`formatRange`/`formatRangeToParts` exist in
`Broiler.JS/Broiler.JavaScript.BuiltIns/Intl/JSIntl.cs`, so this is a divergence
inside an already-implemented surface (e.g. an option such as
`fractionalSecondDigits`, time-zone offset handling, or a resolved-options field
read from an `undefined` slot) rather than a wholly missing method.

- **Next step:** reproduce each file locally, capture which property read returns
  `undefined`, and extend the existing `Intl` regressions in
  `Broiler.JavaScript.Integration.Tests`.

## Categories 1, 6, 8, 10 — require per-file reproduction

These buckets group files that share only the surfaced engine location, not a
single root cause. Best current hypotheses:

- **Cat 1 (structural mismatch):** `Array.prototype.concat` `@@isConcatSpreadable`
  handling, `Array.from` over proxies/strings, `yield*` delegation values, and
  `Object.entries` ordering — `BuiltIns/Array/*` and generator delegation.
- **Cat 6 (`SameValue(false, true)`):** mixed — private-method vs computed-property
  ordering, `arguments` property access, `RegExp` `lastIndex` on match/replace,
  `NumberFormat` `compactDisplay`.
- **Cat 8 (`Unexpected token … constructor`):** a class-body form the parser
  mis-handles; the samples are unrelated to Unicode, so triage each (e.g. getter
  named after `constructor`, accessor/static ordering).
- **Cat 10 (`Value is not iterable`):** iterator-protocol retrieval for `yield*`
  over an arbitrary expression, `Array.from` on a primitive, and `Map` from an
  iterable.

- **Next step:** pull each sample with the pinned runner, reduce to a minimal
  local script, and promote the confirmed root causes into their own rows in the
  tracked-batch table.
