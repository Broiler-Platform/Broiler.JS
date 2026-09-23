# Known compliance gaps

This file groups current gaps without duplicating issue-specific investigation history.
Exact failing paths live in `scripts/compliance/test262-failures.txt`; update this summary
when that manifest changes materially.

## Active semantic clusters

The current failure manifest includes work in these areas:

- **ECMAScript module semantics — implemented; what remains is listed here.** A module graph
  is now loaded, linked and evaluated as ES2024 16.2.1.5 specifies (`JSModuleContext`,
  `JSModule.Record.cs`): every specifier goes through `Resolve` (no raw-specifier cache
  answer; `module`/`clr` are reached through the default `Resolve`, and a context built with
  `registerBuiltInModules: false` registers neither); a missing or ambiguous import is a
  SyntaxError at link time, before any body runs; imports are live, immutable indirect
  bindings; the namespace is the module namespace exotic object; module environments are
  instantiated before evaluation, so cycles see hoisted functions and a cross-module TDZ; top-level
  `await` follows the asynchronous evaluation algorithm, and an evaluation error is cached and
  rethrown to every later importer. Module code is strict, `this` is `undefined`, top-level
  `var`/function declarations live in the module environment, and no CommonJS binding is
  visible — `require`, `module`, `exports`, `__filename` and `__dirname` exist only in a
  CommonJS module (one loaded by `require`, or a `.cjs` key). Measured on the pinned ref
  (`--include-negative`, local Release host) over `test/language/{module-code,import,export}`
  and `test/language/expressions/dynamic-import`: 788 → 1095 of 1667 pass (module-host
  files 327 → 634 of 755; `module-code` 310 → 580 of 596), no test regressed. The stricter
  cover grammar (below) brings this to 1097 of 1667 (module-host 636, `module-code` 582).
  Still failing there:
  - **`import()` from a script in the test262 `--script-host`** (about 450 files): that host is
    a plain `JSContext`, which has no module loader, and the runner assembles a script test
    into a temporary file, where its `_FIXTURE.js` siblings would not resolve anyway. A
    `JSModuleContext` routes script-code `import()` through its loader; the shell's script
    host does not use one.
  - **Proposals not implemented:** `import defer` (92), `import bytes` (5) and text imports (5).
  - **Top-level `for await`** fails with an "invalid program" IL error (13 files), the
    generator-rewrite defect the host-coverage notes below already record.
  - **Parser leniency that is not module-specific:** `yield` as an IdentifierReference in
    strict code (`parse-err-yield`). (`()` accepted as an expression statement, so that
    `export default function () {}();` parsed, is fixed: the cover grammar's `()`, trailing
    comma and rest element are SyntaxErrors unless `=>` follows — `parse-err-invoke-anon-{fun,gen}-decl`
    now pass.)
  - **Not covered by a failing file but known:** an anonymous `export default class {}` is
    named `default` after its definition is evaluated, so its own static initializers still
    see an empty `name`; a CommonJS module imported from a module exposes only `default`
    (its `module.exports`), with no named exports detected from its source; and module
    evaluation settles through the engine's async-function driver. That driver now takes the
    specification's one job per `await` and settles a single promise (see "Await and
    async-generator job ordering" below); the jobs an importer takes to resume after it have
    not been re-measured against the specification.
  - **One record per key, whichever loader reaches it first.** A `.js` key that `require`
    loads first becomes a CommonJS record, and a later `import` of it gets only `default`;
    a key that `import` loads first is an ECMAScript module, and a later `require` of it
    throws "require() of the ECMAScript module … is not supported". Node decides the kind
    from the file (extension and `package.json` `type`) instead. So `require()` of an
    ECMAScript module file is no longer supported: before this change `require('./m.js')` of
    a file with `export` returned its exports (`.default` for the default export), and now it
    either parses the file as CommonJS and throws a SyntaxError ("'export' may only appear at
    the top level of a module") or, if the file was imported first, throws the TypeError
    above. Node 22+ supports synchronous `require(esm)`.
  - **The shell's entry file is a module.** `BroilerJS file.js` (without `--script-host`)
    runs the file as an ECMAScript module, so `require` is not defined there; use `import`,
    or name a CommonJS file `.cjs`. The shell's bundled `.js` modules (`buffer`,
    `base64-js`, `ieee754`) are CommonJS: `import b from 'buffer'` gives their
    `module.exports` (`b.Buffer`), and a named import such as `import { Buffer }` is a link
    SyntaxError (no named-export detection, as above). Before this change those imports
    hung.

- **An async body after an awaited rejection — fixed.** The async-function driver resumed a
  body with `Throw`, which already runs it to its next `await`, and then resumed it again
  with the iterator result `Throw` returned. After a caught awaited rejection the next `await`
  produced `{ value, done }` at once instead of waiting, and a second awaited rejection never
  reached the body's `catch`. A thenable whose `then` threw rejected the function's own promise
  instead of the `await`. Both are fixed in `JSAsyncFunction`; covered by
  `AsyncResumeAfterRejectionTests` (and module top-level `await`, which runs on the same driver).
- **Await and async-generator job ordering — fixed; what remains.** `await` is the spec's
  Await: PromiseResolve(%Promise%, v) plus a reaction that resumes the body in the reaction
  job itself, so it takes one job for a native promise or a plain value, never consults
  `Promise.prototype.then`, and calls a thenable's `then` in a job. The async-function driver
  used to call `then` on the awaited value (synchronously for a thenable) and queue the
  resumption as a second job, and resolved a new promise per await step with the next one,
  which adopted the function's completion two jobs per step late. Async generators use the same
  Await (an internal `await` of a plain value resumed at once before), settle one promise per
  request, and queue next/return/throw requests made while a step is running (a second `next()`
  used to resume the body at the first step's pending `await`). A direct `for await` over an
  async generator steps it through its (internal) next, so an `await` inside the generator —
  including an `await using` disposal — is no longer seen by the loop as an iteration value.
  `yield*` in an async generator over an async iterator (a native async generator, or any
  iterator from @@asyncIterator) Awaits each next/throw/return result of the delegate and yields
  its value without awaiting it again, as §15.5.5 does: a native async-generator delegate used to
  be stepped synchronously, so its awaits and `await using` disposals surfaced as values, its
  return value was lost and throw() reported a missing `throw` method; any other async
  iterator's result was read as a record unless its promise had already settled. throw() and
  return() on the delegating generator reach the delegate's own throw()/return() (a missing
  `throw` closes the delegate, then throws a TypeError; a missing `return` awaits the value).
  `return expr` in an async generator awaits `expr`, and return(v) awaits `v` — at a yield,
  where a rejection is thrown into the body, and on a generator that has not started or has
  completed, where a rejection rejects the request. `for await` awaits each value only for a sync
  iterable; a value of an async iterator used to be awaited again (one job per iteration late, and
  a promise yielded as a value was unwrapped). `await using`: every Await of the disposal is the
  enclosing function's own (one job each); a resource of null or undefined records needsAwait (an
  Await(undefined) before a following sync resource or at the end), also for
  `AsyncDisposableStack.prototype.use(null)`; the block's thrown error is seeded into the
  disposal, so a disposer error becomes `SuppressedError(disposerError, bodyError)`; the
  @@dispose fallback's return value is discarded by `AsyncDisposableStack.prototype.use` as
  well; and a module whose only top-level await is an `await using` is an async module.
  `Atomics.waitAsync` and `Blob.prototype.text` settle their promises on the calling thread
  instead of from a thread-pool continuation. Covered by `AsyncJobOrderTests` (expected
  orderings from the specification, Node 24 as a cross-check) and `ModuleSemanticsTests`.
  Still open:
  - `for await` does not call an async generator's `return()` on `break` (its `finally` never
    runs), and a native async generator used by `for await` or `yield*` skips the observable
    @@asyncIterator/`next` lookups.
  - `yield*` over a sync iterable in an async generator keeps the enumerator path instead of an
    async-from-sync iterator (test262 `yield-star-sync-*` and
    `yield-promise-reject-next-yield-star-sync-iterator` still fail).
  - `Array.fromAsync` is a synchronous approximation: it steps its input with the sync iterator
    protocol and unwraps only already-settled promises, so over an async generator each `await`
    (and `await using` disposal) in the body becomes an element.
  - The sync-iterable fallback of `for await` completes one job early on its last (done) step.
  - Jumps out of protected regions in the async rewrite (unchanged by this work): in an async
    function a `finally` after a `catch` that returns is skipped, and where the body awaits (or
    has an `await using`), `continue` out of a `try`/`finally` and `continue` or a labeled
    `break` out of a `using` / `await using` block skip the `finally` or the disposal.
  - `new Function('await using x = …')` is accepted (it should be a SyntaxError) and disposes
    synchronously.
  - `Atomics.waitAsync` reports "timed-out" at once instead of after the timeout.
  - Host `Task`-backed promises (CLR interop, `fetch`) settle when the host task completes, as
    an external event, and only their reactions are jobs; dynamic `import()` settles its
    promise from the loader's `Task` continuation the same way.
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
- **A Directive Prologue that an empty statement ends after a directive — fixed.**
  `function f(){ 'x'; ; 'use strict'; return typeof this }` ran strict, where the `;` ends
  the prologue and `'use strict'` is an ordinary expression statement. The prologue scan was
  right and the AST was wrong: a statement that had already ended AT its own `;` consumed a
  second one, so that empty statement never reached the statement list and the two literals
  became adjacent directives. `function f(){ 'x'; ; ; 'use strict'; … }` was read correctly
  because its second `;` survived, which is what made the defect look like a prologue-scan
  bug. `FastParser.Statement` now consumes the terminator only when the statement did not
  end on one. No test262 file at the pinned ref covers this; `Track1LanguageTests` does.
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
- **A parameter, `var`, `let` or catch parameter named `undefined` — fixed.** The compiler
  folded every identifier named `undefined` to the undefined value, so a binding of that
  name read as undefined however it had been initialised:
  `(function (undefined) { return undefined; })(5)` answered undefined. `undefined` is a
  non-writable non-configurable property of the global object rather than a keyword, so the
  fold is still taken for a free reference — and not for a name that resolves to a binding,
  nor under a `with` whose object may supply one. Covered by `Track1LanguageTests`.
  `ScalarParameterTests` pinned the old answer as a known gap and now asserts the spec's.
- **An indexed read off `null` or `undefined` answered `undefined` instead of throwing —
  fixed.** `var u; var k = 3; u[k]` evaluated to undefined, where 6.2.5.5 has ToObject(base)
  throw a TypeError before the key is looked at. One shape was affected: an index the compiler
  can hold unboxed — a numeric local, a loop counter, a constant-folded expression — reaches
  `JSValue.GetElementByNumber`, whose unboxed arm calls `GetValue(uint, …)` directly, and
  `JSUndefined`/`JSNull` override the `this[uint]` INDEXER to throw but not that virtual, whose
  base answers `undefined` for a value with no prototype chain. A constant index, a string key,
  and a non-integral or out-of-range index all threw throughout, which is why it read as a
  missing message rather than a missing throw. A nullish base now takes the boxed arm, which
  throws and names the key. Covered by `IndexedNullishReadTests`.
- **A nullish property access named only some keys — fixed.** "Cannot read properties of
  undefined" named a string, number or symbol key and nothing else, so `u[k]` said nothing
  about `k` when it held a boolean, `null`, `undefined` or a BigInt; the assignment form named
  no key at all. Every primitive is named now, a symbol as `Symbol(s)`, and a write appends
  "(setting 'k')" — an object key is still left undescribed rather than coerced, since its
  `toString`/`@@toPrimitive` is user code the spec does not run here. Covered by
  `UndefinedPropertyReadMessageTests`.
- **`Reflect.set` receiver attributes — fixed.** See
  [`Phase-2.status.md`](../roadmap/Phase-2.status.md) and
  `ReflectSetReceiverAttributesTests`, which now pins the correct answer.

Measured against the pinned ref rather than assumed (local Debug host,
`--include-negative`), these older entries do not currently reproduce:

- **`new.target` inside a direct eval nested in eval-compiled code.**
  `eval("(function(){ return typeof eval('new.target') })()")` answers `undefined`, and
  `staging/sm/class/newTargetEval.js` passes. It is in the failure manifest from an older
  run; confirm against a current CI run before removing the path.
- **Array `slice`, `unshift`, `toReversed`, `reduceRight` and near-maximum length
  semantics.** All four directories pass except
  `slice/create-proto-from-ctor-realm-array.js` (a cross-realm species case).
- **Comment and regular-expression literal lexical edge cases**, and **labeled/unlabeled
  `continue` and block-scoped loop bindings.** `test/language/comments`,
  `test/language/asi` and the `for`/`if`/`while`/`do-while`/`labeled` statement directories
  pass apart from the early-error cluster below.

What those runs did surface, and what the roadmap's track 1 should carry instead:

- **Early errors that never fire — fixed.** `for (const x;;)`, a body `var` shadowing the
  head's lexical name, a labelled function declaration as a loop body, `export` inside `eval`,
  and a direct `var` colliding with a lexical binding of the same name — about 30 files across
  `test/language/statements/{for,if,labeled}`, `test/language/eval-code` and
  `test/language/global-code`, each of which reaches its `$DONOTEVALUATE()` — are now rejected.
  The var/lexical collision (in either order and at every scope), the labelled-function loop
  body, and `export` in script/eval code were the VarDeclaredNames∩LexicallyDeclaredNames,
  loop-parser and export mechanisms tracked by the roadmap's Track 1. The last member to land
  was the GLOBAL-lexical half of the collision: a direct eval's global `var`/function colliding
  with a top-level `let`/`const`/class. `JSContext.Register` rejected it for an indirect eval,
  but a direct eval binds the name as a captured lexical and skips that registration, so the
  compiler now emits the equivalent `EnsureNoGlobalLexicalConflictForEvalVar` check in the eval's
  hoisting prelude. Covered by `Track1LanguageTests`; the `BuiltInsTests` cases that pinned the
  old shadowing behaviour now assert the SyntaxError.

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

The coverage gaps that made a result untrustworthy — an async test that could not fail,
and the `$262`, `module` and `raw` files that were reported as skipped — are closed. What
remains is listed at the end of this section.

- **`async` results follow test262's own marker protocol.** `$DONE` is
  `harness/doneprintHandle.js`, injected into every `flags: [async]` test the way
  INTERPRETING.md requires, and it PRINTS `Test262:AsyncTestComplete` or
  `Test262:AsyncTestFailure:<error>`. The runner reads those markers: no marker is
  `neverSettled`, two are `completedTwice`, a failure marker is `reportedFailure`, and a
  file that neither settles nor returns is ended by the per-test timeout. Every one of
  those is a failure, and the completion kind rides on the result as `asyncCompletion`.

  It replaces a completion-value protocol under which an async test could not fail: `$DONE`
  settled a promise the assembled script ended in, `--script-host` evaluates and discards a
  script's completion value and reports no unhandled rejection, so `$DONE(error)` and a
  `$DONE` that was never called both exited 0 and were counted as passes.

  **The correction, measured.** Over a seeded random sample of 400 of the suite's 5487
  script-goal `async` files (`random.Random(20260823)`, pinned suite ref, Debug script
  host): 359 pass, 39 report a failure, 2 exit non-zero having never settled. 10.3% of
  async results were passes that are not passes — on the order of 560 files across the
  async corpus (95% interval ≈ 400–730). The earlier estimate of ~780 came from a 200-file
  sample taken before four engine defects were fixed, and is superseded by this one.

  `scripts/compliance/fixtures/async-protocol/` holds the fixtures that keep it honest —
  deliberately failing, rejecting, never-settling, double-completing, dying-after-completing
  and never-returning — with the verdict each must produce. `run_test262.py --self-check`
  runs them against a built engine and every CI shard runs it before the shard.

  **Nothing else moved.** The same seeded 600-file sample of the corpus that neither names
  a host mode nor reaches a `$262` hook, run under both the old and the new runner and host
  (`random.Random(19730401)`, `--include-negative`): 37 verdicts changed, all in the
  intended direction. 33 failures became passes — negative `phase: parse` tests, which the
  engine had been rejecting correctly and which now match on the SyntaxError they raise —
  and 4 passes became failures, every one an async test reporting a real assertion failure
  (`Array.fromAsync` contents, a dynamic import that cannot resolve, a private async
  generator's `yield*`). No test regressed.

- **`module` and `raw` are host modes, not exclusions.** A module test runs where it sits
  in the suite under `--module-host`, with its harness handed to `--preload` as a script so
  `assert` and `$DONE` are globals its module body and its `_FIXTURE.js` imports can see; a
  raw test is handed the file's own unmodified bytes. Each mode's selected, executed,
  passed, failed, skipped and timed-out totals are published separately
  (`hostModeSummary`).

  What the previously-skipped files do when they run (1494 of them: 824 module, 30 raw, 640
  reaching a `$262` hook; local Debug script host, pinned ref, `--include-negative`, so
  read it as the shape of the work rather than as a published rate): raw 30/30 pass, the
  `$262` files 515 pass and 125 do not, module 332 pass and 492 do not. **None of that is
  host-coverage work** — it is engine work these modes made visible, in five clusters:
  141 module early errors that do not fire (`dup-bound-names`, `await` as a module
  identifier, JSON module validation); 109 files whose specifier the parser rejects
  (`import defer`, import attributes); 52 that hang, nearly all dynamic `import()` of a
  module that exports a class or function; 22 NullReferenceException crashes in module
  namespace and ambiguous-export paths; and 12 "invalid program" IL failures on top-level
  `for await`. The `$262` failures are mostly cross-realm identity and missing-throw cases.

- **`$262` is defined for the hooks this host can answer honestly:** `global`,
  `createRealm` (a real second realm — the current-context restore is what keeps the caller
  in its own), `detachArrayBuffer` (via the witness `transfer()` performs), `evalScript` and
  `gc`. A test is excluded for the hook it names rather than for mentioning `$262`.

- **An uncaught JavaScript error is reported by name.** The host prints
  `Uncaught <name>: <message>` on stderr and exits 1, so a `negative: phase: parse,
  type: SyntaxError` test is matched on the type it raised. A SyntaxError raised while
  compiling carries no JavaScript stack to name it, so every parse-phase negative test used
  to fail on the diagnostic while rejecting the program exactly as it should.

What is still not covered:

- `$262.agent` (112 files): multi-agent Atomics needs a second agent with its own event
  loop. Owned by [Concurrency.md](../roadmap/Concurrency.md), not by host coverage.
- `$262.IsHTMLDDA` (42 files): the engine has no object whose `typeof` is `"undefined"`.
- `$262.AbstractModuleSource` (8 files): needs module source objects the engine does not
  implement.
- Negative-metadata execution is implemented but still opt-in (`--include-negative`);
  release runs must pass it.

Four engine defects found while measuring the async corpus are fixed. A promise reaction
whose body is a tail call was dropped entirely; the `let`-head loop scoping described
below; and the two that made a call whose ARGUMENTS suspend not be the call the source
wrote — `obj.method(await p)` invoked the nested call in its own arguments instead (the
receiver and callee temps come from a pool that nested call is handed back, which is only
safe while both values sit on the evaluation stack), and an argument list of more than four
arguments, or of any length containing a spread, is built as an array initializer that
nothing hoisted, so the suspension inside it produced an InvalidProgramException.
`assert.sameValue(await p, x)` is the first shape, so those assertions did not execute at
all; `Broiler.JavaScript.Integration.Tests/SuspendedCallArgumentTests.cs` covers both.

## Gap lifecycle

For every gap:

1. record an upstream path and pinned suite revision;
2. add a minimal test in the owning repository project;
3. implement the fix in the narrowest parser/compiler/runtime/built-in layer;
4. rerun the focused path and affected full shard;
5. update `test262-failures.txt` and `dashboard.md`.

The active execution order and exit gates are in
[the repository roadmap](../roadmap/Component.md).
