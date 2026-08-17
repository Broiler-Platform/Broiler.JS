# Known compliance gaps

This file groups current gaps without duplicating issue-specific investigation history.
Exact failing paths live in `scripts/compliance/test262-failures.txt`; update this summary
when that manifest changes materially.

## Active semantic clusters

The current failure manifest includes work in these areas:

- RegExp literal parsing, Annex B escape handling, and Unicode ignore-case behavior;
- Array `slice`, `unshift`, `toReversed`, `reduceRight`, and near-maximum length
  semantics, including Proxy-created results;
- comment and regular-expression literal lexical edge cases;
- labeled/unlabeled `continue` and block-scoped loop bindings;
- direct and indirect `eval`, including lexical environments and `new.target`;
- Annex B block-scoped function and catch/`var` behavior; and
- string/array legacy edge cases represented by the staging tests.

Older triage also identified `Intl.DateTimeFormat` range/parts behavior and
SameValue/Proxy ordering cases. Keep them here only while a current reproduction or
linked issue remains; do not rely on deleted issue snapshots as evidence.

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

  Two engine defects behind that measurement are fixed (a promise reaction whose body is
  a tail call was dropped entirely; the `let`-head loop scoping described below), but one
  remains open and is the largest single contributor: **`obj.method(await p)` — a call
  through a member expression with an `await` among its arguments — is silently skipped**,
  while `plain(await p)` and `var v = await p; obj.method(v)` both run. `assert.sameValue(
  await p, x)` is that shape, so the assertion never executes. Minimal repro:

  ```js
  var log = [], obj = { hit(v) { log.push(v); } };
  (async function () { obj.hit(await Promise.resolve(1)); })();   // logs nothing
  ```

## Gap lifecycle

For every gap:

1. record an upstream path and pinned suite revision;
2. add a minimal test in the owning repository project;
3. implement the fix in the narrowest parser/compiler/runtime/built-in layer;
4. rerun the focused path and affected full shard;
5. update `test262-failures.txt` and `dashboard.md`.

The active execution order and exit gates are in
[the repository roadmap](../roadmap/component.md).
