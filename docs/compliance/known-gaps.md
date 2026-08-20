# Known compliance gaps

This file groups current gaps without duplicating issue-specific investigation history.
Exact failing paths live in `scripts/compliance/test262-failures.txt`; update this summary
when that manifest changes materially.

## Active semantic clusters

The current failure manifest includes work in these areas:

- **Per-eval compilation cost.** Every direct `eval` compiles a fresh `DynamicMethod`, and
  JITting it is nearly all of the cost of a small eval — ~21 ms in a Release build for a
  body that is one call, where the same text parses in ~15 µs. Three shapes now avoid it:
  an inert body (nothing but literals, or `new.target`), a body with no statements, and a
  body that is only `eval("…")` over a constant, which is the same evaluation as that
  constant and is run as one. Everything else still pays per call. What would remove the
  rest is reusing a compiled eval body across calls, keyed on the source text together with
  the compile-time scope facts `DirectEvalSupport.Execute` is handed.
- **A `delete` of an eval-introduced `var`, read back through the closure that deleted it.**
  `(function(){ var f = eval("var y = 5; (function (w) { return w ? y : eval('delete y') })");
  f(0); return f(1) })()` throws a ReferenceError where the name should resolve outward to
  the global. The delete tears the binding down, and a closure the eval compiled holds that
  cell directly rather than re-resolving the name. `staging/sm/eval/exhaustive-fun-normalcaller-direct-normalcode`
  passes because the function it deletes through also has an outer binding to fall back to.
- **`new.target` inside a direct eval nested in eval-compiled code.**
  `eval("(function(){ return typeof eval('new.target') })()")` is a SyntaxError, because the
  outer program's "reject new.target" (right for an eval's own top level) reaches the inner
  call site, which sits inside a function and should allow it.
- **A Directive Prologue that an empty statement ends after a directive.**
  `function f(){ 'x'; ; 'use strict'; return typeof this }` runs strict, where the `;` ends
  the prologue and `'use strict'` is an ordinary expression statement. `function f(){ ;
  'use strict'; … }` (no directive before the `;`) and `function f(){ 'x'; 0; 'use strict';
  … }` are both read correctly, so it is the combination that is wrong, and the compiler's
  own prologue scan (`FastCompiler.HasUseStrictDirective`) stops at the empty statement as
  it should — the strictness is being decided somewhere else.
- **Symbol-keyed own properties enumerate in SYMBOL creation order, not PROPERTY creation
  order.** OrdinaryOwnPropertyKeys orders symbol keys "in ascending chronological order of
  property creation"; the symbol map is a sparse map keyed by the symbol's own id, and
  `GetOwnPropertyKeysInListOrder` sorts by that id to keep the answer deterministic. The two
  agree unless a symbol is created before the property that uses it while another symbol
  property is defined in between — `var later = Symbol(); o[Symbol()] = 1; o[later] = 2`
  answers `later` first. `built-ins/Object/assign/strings-and-symbol-order` is the case that
  finds it, once ADVANCED hoists one of its two `Symbol()` calls above the other. Fixing it
  needs a per-object record of the order symbol properties were added (the map cannot carry
  it: its enumeration is node-allocation order, which is key-clustered, not insertion order),
  so it is a property-storage change rather than a sort to delete.
- Array `slice`, `unshift`, `toReversed`, `reduceRight`, and near-maximum length
  semantics, including Proxy-created results;
- comment and regular-expression literal lexical edge cases; and
- labeled/unlabeled `continue` and block-scoped loop bindings.

Older triage also identified `Intl.DateTimeFormat` range/parts behavior and
SameValue/Proxy ordering cases. Keep them here only while a current reproduction or
linked issue remains; do not rely on deleted issue snapshots as evidence.

## Deliberate deviations

These are not gaps. They are places where the engine answers a pinned-suite test the way
the current specification and the major engines do, and the test does not.

- **`annexB/language/function-code/block-decl-func-skip-arguments`.** The test asserts that
  a block-level `function arguments(){}` in a sloppy function leaves the arguments object
  in place afterwards, and quotes the pre-2021 FunctionDeclarationInstantiation that
  appended `"arguments"` to _parameterNames_ — the list Annex B's "and _F_ is not an
  element of _parameterNames_" condition tests. Current 10.2.11 appends it to
  _paramBindings_ instead, so the Annex B copy-out runs and the function value replaces the
  arguments object. V8 and SpiderMonkey both answer as Broiler does (`node` prints
  `function arguments() {}` after the block), and the `staging/sm` cases covering the same
  shape assert the current wording. The file is unchanged at test262 HEAD, so it is not a
  `KNOWN_INCORRECT_TESTS` entry either: it is the one expected failure of an
  original-variant full run, and the reasoning lives next to the code in
  `FastCompiler.AppendAnnexBOuterBindingAssignments`.

## Host-coverage gaps

- `$262` host-harness helpers are incomplete.
- `module` tests need a module-host mode.
- `raw` tests need raw-harness semantics.
- Negative-metadata support exists but must be enabled and reported by release runs.
- **An `async` test cannot currently fail, so its result is not evidence.** The runner
  injects a `$DONE` that settles a promise and appends it as the script's completion
  value; `--script-host` evaluates and discards that value, and reports no unhandled
  rejection, so a rejected `$DONE(error)` and a `$DONE` that is never called both exit 0.
  The suite has 5581 `async`-flagged files. Measured over a random sample of 200 against
  the standard marker protocol (a `$DONE` that prints `Test262:AsyncTestComplete` /
  `Test262:AsyncTestFailure:`, which the runner then requires on stdout): 168 pass either
  way, 26 report a real assertion failure, and 2 never settle — so on the order of 780
  currently-counted passes are not passes. Switching protocols is the fix; it is a
  deliberate, visible correction to the headline number and wants its own change, not a
  quiet one. Until then, treat `flags: [async]` results as unverified.

  Four engine defects behind that measurement are fixed. A promise reaction whose body is a
  tail call was dropped entirely; the `let`-head loop scoping described below; and the two
  that made a call whose ARGUMENTS suspend not be the call the source wrote —
  `obj.method(await p)` invoked the nested call in its own arguments instead (the receiver
  and callee temps come from a pool that nested call is handed back, which is only safe
  while both values sit on the evaluation stack), and an argument list of more than four
  arguments, or of any length containing a spread, is built as an array initializer that
  nothing hoisted, so the suspension inside it produced an InvalidProgramException.
  `assert.sameValue(await p, x)` is the first shape, so those assertions did not execute at
  all; `Broiler.JavaScript.Integration.Tests/SuspendedCallArgumentTests.cs` covers both.

  The measurement above predates those fixes and should be retaken with the marker protocol
  before the totals here are quoted again.

## Gap lifecycle

For every gap:

1. record an upstream path and pinned suite revision;
2. add a minimal test in the owning repository project;
3. implement the fix in the narrowest parser/compiler/runtime/built-in layer;
4. rerun the focused path and affected full shard;
5. update `test262-failures.txt` and `dashboard.md`.

The active execution order and exit gates are in
[the repository roadmap](../roadmap/component.md).
