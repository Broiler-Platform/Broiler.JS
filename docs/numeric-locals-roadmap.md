# Numeric-local specialization roadmap

Holding a JavaScript local that provably only ever holds a number in a CLR `double` instead of
a heap-allocated `JSValue`. This is a sub-area of
[`docs/performance-roadmap.md`](performance-roadmap.md) §6 (P2-2 item 3), split out because the
item shipped with three named gaps and closing them is a sequence of independent changes rather
than one.

- Owner assemblies: `Broiler.JavaScript.Compiler`
- Semantic owner: `Broiler.JavaScript.Compiler.Tests` (`NumericLocalTests`)
- Ownership entry: `numeric-local-doubles` in `eng/performance/ownership.json`
- Acceptance protocol: unchanged — [`docs/performance.md`](performance.md) governs what may be
  *claimed*. **Nothing in this document closes on the numbers below**; they come from an
  in-process probe on one Windows host and are for prioritization only.
- Status: **N1 implemented** (§3). **N2, N3 open** (§4), sized and sequenced.

---

## 1. The mechanism, and the one thing it owes

`NumericLocalAnalysis` proves which of a function's `var` locals only ever hold a number.
`FastCompiler.VisitBlock` then creates those as `typeof(double)` locals, and
`FastCompiler.VisitBinaryExpression` keeps arithmetic over them unboxed.

The whole optimization rests on one debt. A specialized local is **hoisted to `0d`, not
`undefined`** — a double cannot represent `undefined`. So the analysis owes the compiler this
guarantee, and every rule below exists to pay it:

> No read of a specialized name can happen before its initializer has run.

A violation is a miscompilation, not a lost optimization: the program silently sees `0` where
JavaScript says `undefined`. That asymmetry is why every rule here is conservative, and why the
test file is split into "should specialize" and "must refuse" halves with the second treated as
the load-bearing one.

Three further limits are properties of the *lowering* rather than the analysis, are deliberate,
and are documented in the main roadmap: `<=`/`>=` are never lowered natively (a CLR ordered
compare answers true for NaN, where JavaScript says false), bitwise and shift operators are not
(ToInt32's modulo-2³² wrap is not a cast), and eligibility is decided on syntax before anything
is visited (compiling a subtree and discarding it would leak compile-time state).

Async functions and generators are excluded from scalar replacement entirely
(`TryPlanScalarReplacement`), so nothing here interacts with suspension.

---

## 2. Measured state

Probe in [Appendix A](#appendix-a--reproducing-the-measurements), best of 5 runs, repeated
across process launches; every figure below was identical on both launches.

**What N1 changed** — 1 000 000 iterations, `Release`, Windows x64:

| Scenario | Before N1 | After N1 | |
|---|---:|---:|---:|
| `var` declared at function-body level (control) | 7 ms | 7 ms | — |
| `var` declared in a loop body | 33 ms | 18 ms | **1.8×** |
| `var` declared in an `if` block | 38 ms | 21 ms | **1.8×** |
| mandelbrot-ish (every working local nested) | 121 ms | 25 ms | **4.8×** |

The last row is the case the main roadmap named as sitting in this gap; it was recorded at 1.1×
when P2-2 item 3 shipped.

**What is still on the table** — 3 000 000 iterations, each pair differing in exactly one thing:

| Pair | Eligible form | Refused form | Gap |
|---|---:|---:|---:|
| **N3** arithmetic over a numeric operand | 2 ms (local) | 66 ms (parameter) | **33×** |
| **N3** the same, parameter copied to a local first | — | 66 ms | **no rescue** |
| **N3** loop bound | 2 ms (literal) | 20 ms (parameter) | **10×** |
| **N2** loop with a nested declaration | 89 ms (`var`) | 218 ms (`let`) | **2.4×** |

The `2 ms` readings sit near the timer's resolution, so treat those two ratios as "an order of
magnitude", not as 33.0× and 10.0×. The direction and the rough size are what the sequencing
rests on; neither is a claim under `docs/performance.md`. The `let` row is the one measured
between two comfortably-resolved numbers.

The "no rescue" row is the important one for sequencing: `var a = p` is numeric only when `p`
already is, and a parameter never is, so a parameter poisons every local derived from it. A
function that takes its numbers as arguments — which is most functions — gets nothing from this
optimization today, no matter how it is written internally.

---

## 3. N1 · Declarations nested in a block — **implemented**

Eligibility was "the declaration must be a direct statement of the **function body** (or the
init of a top-level `for`)". A `var` in a loop or `if` body — most real code — was refused.

**The rule now.** A declaration is offered by whichever `AstBlock` directly contains it, and
leaving that block **closes** the name: any later reference disqualifies it.

**Why that is sound.** One property of JavaScript carries it: *to reach statement N of a block
you must first have executed statements 1..N-1 of that same block*, each of which either
completed or transferred control out of the block entirely. There is no way to jump into the
middle of a block. So a reference that is (a) textually after the declaration and (b) inside the
declaring block is necessarily preceded by the initializer having run. A loop re-runs the
declaration at the top of every iteration, so repetition is fine; a read *after* the loop is
refused, because the loop may have run zero times.

**Two things this got wrong on the first pass**, both caught by probes rather than by the suite:

- **A `switch` case clause is not a block.** Its statements hang off the clause, and entering at
  a later `case` skips the earlier ones, so rule (a)'s premise fails —
  `switch (2) { case 1: var s = 10; case 2: return s; }` must read `undefined`. This is handled
  structurally rather than by a special case: case-clause statements are not in an `AstBlock`,
  so they are never offered. A *braced* block inside a case is still only ever entered at its
  top, and stays eligible.
- **Compound assignment and update read the old value.** `x += 1` and `x++` are writes that
  first read, so they need the same guarantee a plain read does. The first cut checked only
  "declared", not "still open", which would have compiled
  `if (c) { var x = 5; } x += 1;` to `1` instead of `NaN`. Both now go through one
  `IsReadable` predicate.

**Evidence.** 20 new tests in `NumericLocalTests`, split the way the risk is: the newly eligible
shapes assert *both* the value and that specialization actually happened
(`CompilerSpecializationDiagnostics`), while the refusals assert the value alone. Both
directions are mutation-tested — deleting the close-check fails 8 tests (7 new, 1 pre-existing);
reverting to body-only offering fails exactly the 3 that assert specialization counts. A
15-case correctness probe covering every hazard shape was also cross-checked against node,
which agrees on all 15.

**Not a gap, checked and dismissed:** a single-statement (unbraced) body is not a block, so a
declaration in one is never offered. This looked like a fourth gap and is not: an unbraced body
holds exactly one statement, so a name declared there can only be used within that same
declaration list, and any use after it is outside the block and refused anyway. A block nested
*inside* an unbraced body — `for (…) if (c) { var t = 1; use(t); }` — is a block and is
eligible. Measured: no difference between the braced and unbraced forms.

---

## 4. Open items

### N2 · `let` and `const` are excluded — **2.4× on an otherwise identical loop**

`OfferBlockDeclarations` matches `FastVariableKind.Var` only, so the same loop written with
`let` is refused outright. Measured at 89 ms versus 218 ms on a 3 000 000-iteration loop with a
nested declaration.

**Why it was excluded.** TDZ. A `let` is not merely `undefined` before its initializer — it
*throws* a `ReferenceError`, so the debt in §1 gets stricter rather than looser: a specialized
`let` read before initialization must still throw, and a raw double has no way to represent
"not yet initialized".

**Why it is nonetheless the cheaper of the two open items.** N1's rules already prove exactly
the property TDZ needs. A name whose every reference is textually after its declaration and
inside the declaring block can never be read in its temporal dead zone — that is the same
proof, not a new one. So for the subset N1 already admits, `let` needs no TDZ reasoning at all:
the analysis has *already* established that no read precedes the initializer.

**Design sketch.** Extend the `Kind` match to `Let`/`Const`, and keep everything else. The
places that need care are the ones where a `let` differs from a `var` in more than TDZ:

- **Per-iteration bindings.** `for (let i = …)` creates a fresh binding per iteration, which the
  closure rewriter relies on. The scalar-replacement gate already excludes any name a nested
  function mentions, so a captured per-iteration `let` is out of scope before this is reached —
  but that ordering must be asserted, not assumed.
- **`const` reassignment** is an early error, so a `const` has exactly one assignment and is the
  *easiest* case — worth landing first on its own.
- **Block scoping proper.** A `let` in a block is not visible after it, so the "closed" rule
  becomes a real scope rule rather than a conservative approximation. That means N2 can be
  *less* conservative than N1, not more.

**Risk: medium.** The gate is `test262-language-basics.txt` plus `statements/for`,
`statements/variable`, and the `let`/`const` TDZ directories. A wrong answer here is a missing
`ReferenceError`, which is observable.

### N3 · Parameters are never specialized — **33×, and it poisons everything downstream**

`Collect` rejects every parameter name outright: "the value arrives as a `JSValue` and nothing
here proves it is a number." That is true, and it is the single largest remaining gap —
not because parameters are hot in themselves, but because `IsNumeric(identifier)` requires the
name to be a candidate, so **every local derived from a parameter is refused too**. Copying a
parameter into a local does not help: measured 66 ms either way against 2 ms for a genuine
local.

A function that takes its numbers as arguments gets nothing from this optimization today.

**Why it is hard.** The analysis is a whole-function syntactic proof with no call-site
information. A parameter's type is a property of the *callers*, which the compiler does not see;
`f(1)` and `f('x')` compile to the same function. So unlike N1 and N2 there is no proof to be
had from the function body alone, and the shape of the answer has to be different.

**Three candidate designs, cheapest first.**

1. **Guard-and-fall-back at entry.** Compile one body. At entry, test each candidate parameter
   with a type check; if all pass, run a specialized prologue that unboxes them into raw
   doubles, otherwise bind them boxed as today. This needs the body to be compiled *twice* (a
   specialized and a generic version) or needs every use site to be able to read either
   representation — which is the whole difficulty. Cheap to describe, not cheap to build.
2. **A specialized entry point.** Emit a second entry that takes `double` parameters directly,
   and call it from sites the compiler can prove pass numbers. This is the honest version but
   requires call-site knowledge the current compiler does not thread through, and interacts with
   `arguments`, `Function.prototype.apply`, and every reflective call path.
3. **Narrow it to what is provable locally.** A parameter with a numeric *default* that is
   never reassigned to a non-number is still not provably numeric — the caller can pass
   anything. There is no sound local subset. This option does not exist; it is recorded so the
   next person does not spend an afternoon rediscovering that.

**Recommendation: do not start N3 without deciding the design first.** It is the largest win and
the largest change, it is the one item here that cannot be done as a conservative extension of
the existing proof, and design 1 versus design 2 is a question about the compiler's architecture
rather than about this analysis. It should be filed as its own item with its own plan.

**Risk: high.** Every calling convention path is in scope.

---

## 5. Sequencing

| | Item | Expected | Risk | Gate |
|---|---|---|---|---|
| ~~1~~ | ~~N1 nested declarations~~ | **Done** — 1.8× on a loop-body local, 4.8× on the nested-loop-nest shape | med | `NumericLocalTests`, mutation-tested both directions; full suite; test262 language/statements |
| 2 | **N2a `const`** | a `const` has exactly one assignment — the easiest case, and it proves out the `Kind` change | low | as N1, plus `test262-language-basics` |
| 3 | **N2b `let`** | up to 2.4× on loops written with `let` | med | above, plus the TDZ directories |
| 4 | **N3 parameters** | up to 33×, and unblocks every local derived from a parameter | **high** | design decision first; then the full calling-convention surface |

N2a before N2b is deliberate: it lands the `Kind` widening and its test coverage against the
variant that cannot be reassigned, so the per-iteration-binding and TDZ questions are faced on
their own rather than together with it.

Nothing here closes under `docs/performance.md` without the release RID matrix; see
[`docs/performance-roadmap.md`](performance-roadmap.md) §8.1, where the benchmark and matrix
evidence for this whole area is still outstanding.

---

## Appendix A — reproducing the measurements

Two probes, both run through the script host
(`dotnet Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll --script-host <file>`).
Each scenario is called once to warm and compile, then timed as the best of five runs.

Two things the probes have to get right, both learned by getting them wrong first:

- **Loop bounds must be literals.** A bound held in a global (`var N = 1000000` at script level)
  adds a property lookup per iteration that swamps the effect being measured — it made a 1.8×
  difference read as no difference at all.
- **Results must be accumulated into a sink.** Without one the CLR can eliminate a pure loop
  whose result is discarded, which showed up as two implausible 0 ms readings.

Scenario source is inline in the probes; the shapes are:

```js
// N1 control     var t at function-body level, arithmetic in a counted loop
// N1 loop body   for (var i…) { var t = i * 2; s = s + t; }
// N1 if block    for (var i…) { if (i > 1) { var t = i; s = s + t; } }
// N1 mandelbrot  nested loops, every working local declared in the inner body
// N2 let loop    the N1 loop-body shape with let for all three names
// N3 operand     var p = 3            versus   function (p)      — s = s + p * 2
// N3 copy        function (p) { var a = p; … }                   — s = s + a * 2
// N3 bound       for (var i = 0; i < 3000000; …)  versus  i < n  where n is a parameter
```

Specialization counts — as opposed to timings — are read from
`CompilerSpecializationDiagnostics.Snapshot().NumericLocals`, which is what `NumericLocalTests`
asserts on so that "it got faster" and "it specialized" are checked separately.
