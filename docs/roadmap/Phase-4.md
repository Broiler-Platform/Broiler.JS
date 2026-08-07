# Phase 4 — speculation

**Target: everything**, and it is the difference between ~100× and ~10×. Blocker **B2**.
The second scope exclusion this campaign overturns.

> The plan half of [`Phase-4.status.md`](Phase-4.status.md), which carries the evidence
> behind every state below. Part of the
> [performance and benchmark roadmap](Roadmap.md).

---

## What this phase is for

Two findings make it more tractable than it looks.

**The tiering scaffolding already exists and is general.** `Runtime/FunctionTiering.cs` has
`FunctionTieringController` with an invocation threshold, a per-realm budget, a
retained-code cap, delegate replacement and `RecordDeoptimization` counters, gated behind
`JSContextOptions.FunctionTiering` — disabled by default, and **it must retain the original
delegate as the semantic fallback.**

**But there is no optimizing compiler behind it.** `JSFunction.RecompileForTiering` with
`numericPlan == null` re-runs `CoreScript.Compile` on `({source})` with a one-shot cache — it
recompiles *the same code the same way*. **Tier-2 was a hook, not a tier.**

That is a good position: the bookkeeping, budget and safety-fallback policy are built and
tested; what was missing is the part that makes entering tier-2 worth anything.

### The premise, measured — and then re-measured

**4-1 settled what the rest of the phase rests on:** 93.5% of reads and 96.7% of calls are
monomorphic by execution weight. **Over seven suites.** Re-taken over twelve it is
**80.11% and 86.35%** — still high enough to found the phase, and the correction matters
because the census corpus every phase-3 and phase-4 headline was computed over was 7 of 15
and never said so. (Mandreel had been aborting the census host with an uncatchable stack
overflow, since phase 0's stack reserve is a property of the *shell*.)

**The phase's own warning, from its own measurements:** the whole read path is ≤ ~9% of
Octane's execution time here and the whole call path ≤ ~5.5%. **4-4's ceiling is smaller than
the phase assumed.**

**Owner assemblies:** `Runtime/TypeFeedback.cs`, `Runtime/ObjectShape.cs`,
`Runtime/FunctionTiering.cs`, `Engine/CallFrames.cs`, `BuiltIns/Function/JSFunction.cs`,
`.Compiler`, `.ExpressionCompiler`, `.LinqExpressions`.

## Items

| # | Item | State | Size |
|---|---|---|---|
| **4-3** | **Deoptimization** — the safety net that makes everything else legal | ✅ **4-3a and 4-3b both landed** | ~~XL~~ **S + M–L** |
| **4-1** | **Type feedback collection** — receiver shapes at reads, callee identities at calls | ✅ **landed**, and it settles the phase's premise | L |
| **4-2** | **A specializing tier-2 compile** | ✅ **4-2a + 4-2b landed; 4-2c refuted at 0.119%** | ~~XL~~ **S + L + refuted** |
| **4-5** | **The fixed cost of a call** | ⚠️ **the largest measured target left in the phase** — attributed, and mostly blocked | **S landed; rest blocked** |
| **4-4** | **Inlining of small JS callees** | ⛔ **measured, not started** — ceiling 2.43% against 4-5's 8.06% | XL |

### 4-3 · Deoptimization — ✅ landed, and it was mis-specified

**"Bail out mid-function by reconstructing an interpreter frame" is not expressible here** —
there is no interpreter frame. Tier-1 locals are **CLR locals of an IL method**, and
`CallFrame` carries no JavaScript values. Re-specified as:

- **4-3a** (S) — the **restart contract**, which the pilot already implements. ✅
- **4-3b** (M–L) — a **generic fallback branch inside the specialized method**. ✅ Only 4-3b
  gates 4-4, and 4-2b is its first consumer.

**4-3a found a real hazard and it is worth keeping in view:** restart is only sound if the
body has no suspendable frames, and that condition was held by **two unrelated accidents** —
two ordinary refactors away from an async function returning a number instead of a Promise.
Whoever touches this must re-check the condition, not assume it.

**The frame work is a prerequisite nobody filed as one.** The activation record is now a slot
in `CallFrameStack` addressed by a `FrameToken` struct, and the three invariants that redesign
asserts — a suspendable frame retaking a slot under a different caller, unwinding refusing to
grow back into abandoned slots, and popping past stranded callees — are exactly the surface
4-3 has to preserve.

### 4-1 · Type feedback collection — ✅ landed

The inline caches already observe shapes at property sites but did not *retain* them. Now
recorded per site and kept. Callee identity was phase 2's 2-6 until that item was measured:
**there is no repeated callee resolution to remove**, so recording it is feedback and nothing
else, and it pays only once 4-2 and 4-4 consume it.

**Still open per site:** the numeric-vs-generic signal. The *aggregate* was collected for a
phase-3 ranking (50.1% of cache-answered reads are numeric, 98% of them Box2D's); the
per-site version is not built.

### 4-2 · A specializing tier-2 compile — ✅ two halves landed, one refuted

**Measuring the branch it was told to replace found it produced *wrong answers*** — DeltaBlue
died on the shipping tier-2 hook. That is 4-2a, fixed and landed.

**4-2b** specializes a monomorphic read to a shape check plus a direct slot read: **44.7% of
the corpus's executed reads at 0.818× each**, which is **0.83% of suite time** — real, and
below the noise floor.

**4-2c is refused and the refusal is thorough.** The arithmetic half's population is 3-1's
`NoSavingToMake` refusal; its census was another seven-suite 100.00% that reads **92.10%**
over twelve with a **0.46%–100%** spread; specializing all of it is **0.119% of the corpus**
and is net negative at the `+` rate, whose failing guard costs **18.567×**. The relational
lead is closed with it at **0.022%**, so **the whole generic binary-operator surface is
0.475%.** Do not reopen this without a new population.

### 4-5 · The fixed cost of a call — **the item to work on**

**The phase's largest measured target: 6.50% of the corpus by bookkeeping, 8.06% by
surface** — 3.3× 4-4's ceiling, which is what decides the order between them.

**The ablation falsified most of the item's own premise, which is why it is now precise.** A
call costs **142 ns before any argument** plus 17.1 ns each. Priced individually: five nested
`using` scopes cost **0.011 ns**, EH 0.73 ns, dispatch 0.68 ns, ThreadStatic reads free. **The
prologue is not where the cost is**, and 2-6 is confirmed directly.

**What was real and is fixed:** an `AsyncLocal<bool>` read at **7.0 ns** against a
`[ThreadStatic]` at 0.31 ns, on every call, documented in `JSEngine` as *"reads are cheap"* —
**wrong by 24×**. Mirrored into a ThreadStatic keeping the AsyncLocal as the carrier: **0.22%
of the corpus**, pinned by 9 tests that also pass on the unmodified engine.

**What is left, and what blocks it.**

- **92% of the remaining bookkeeping is Annex B `caller`/`arguments`.** Its named fix was
  priced at **0.20%** and refused.
- **The 1.46% that is left is gated on a soundness question nobody has answered** — not on a
  design and not on effort.
- A further **~85% of a call's fixed cost is unattributable from outside the engine.** That
  half is blocked on a profiler, and **a sampling profiler does not decompose this engine**:
  it inflates the driver ~29%, its biggest frame is its own rendezvous point, and compiled
  JavaScript does not symbolicate.

**Next action: answer the soundness question on the 1.46%, or build in-engine attribution.**
Do not buy another ablation — one useful residue is already recorded: removing two struct
copies bought 1.83 ns against a replica's 8.19 ns each, so *a struct copy in the source is not
a struct copy in the code.*

### 4-4 · Inlining of small JS callees — ⛔ measured, do not start

What Richards and DeltaBlue look like they need. **Measured before starting.** Of 6 194 758
invocations, **37% are to native builtins with no body to inline**; 3 902 620 have a
JavaScript callee, 64.0% of those from a promoted function; a hand-inlined control says
inlining saves 149 ns each.

**Ceiling: 1.89% over seven suites, re-taken as 2.43% over the twelve that run.** *Larger*
over the wider corpus — the promotion gate reaches 42.1% of JavaScript calls rather than
64.0%, but the never-counted suites are far call-denser per millisecond.

**Inlining is expressible here** — unlike 4-3's deopt, the mechanism exists — so the blocker
is **value**, not feasibility. It splits into 4-4a (the stack-trace question) and 4-4b
(AST-level inlining). **4-5 addresses more calls for less risk at 3.3× the ceiling. Do 4-5
first.**

## Order

```
4-3 design ✅ → 4-1 ✅ → 4-3a ✅ → 4-3b ✅ → 4-2a ✅ → 4-2b ✅ → 4-2c ⛔ refuted
                                                   → 4-5 ← the item to work on
                                                   → 4-4 (measured, deferred behind 4-5)
```

**Start with 4-3, not 4-2** — the safety net before the speculation. That ordering held and
is why 4-2a's wrong-answer bug was caught by a contract rather than by a benchmark.

## Exit gate

- **Deopt correctness proven *before* any speculation ships**: a test that forces every guard
  to fail at every point in a function body and asserts the fallback produces the
  unspecialized answer. ✅ satisfied by 4-3's design spike, 4-3a and 4-3b, all landed before
  4-2 began.
- **The full test262 matrix — this phase can break anything.**
- Function tiering stays **opt-in** until its supported semantics and fallback behaviour are
  release-tested, per [`Measurement.md`](Measurement.md).

## Dependencies

Depends on **4-3 for everything in the phase** and on **4-1 for 4-4's callee feedback**
(what was 2-6 is now inside 4-1). Benefits from 3-1/3-2 having established unboxed
representations to speculate into.
