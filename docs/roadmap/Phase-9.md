# Phase 9 — VM 4.0: an adaptive two-tier engine

The catalogue's VM 4.0 stage: hotness counters, tier-up, deoptimization, OSR. **And it is
where the VM track pays back into the IL track**, because a bytecode VM is precisely the
interpreter frame that phase 4's item 4-3 could not find and had to design around.

> The plan half of [`Phase-9.status.md`](Phase-9.status.md) — which records that **no
> measurement exists yet**, and carries the entry measurement this phase is blocked on.
> Part of the [performance and benchmark roadmap](Roadmap.md); the four VM phases are
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69).

---

## Why this phase is the reason the other three might be worth it

Phases 6–8 buy a capability: JavaScript on platforms that forbid `Reflection.Emit`. That is
worth doing if such a platform matters, and worth nothing if it does not. **Phase 9 is
different — it improves the engine on platforms that already work.**

### The deoptimization argument

Phase 4's item 4-3 asked for V8-style deoptimization: bail out mid-function by
reconstructing an interpreter frame. It was **re-specified because that is not expressible
in Broiler.JS**:

> tier-1 locals are CLR locals of an IL method, and `CallFrame` carries no JavaScript values

So 4-3 became a **restart contract** (4-3a) plus an **in-method fallback branch** (4-3b),
and both landed. But the restart contract is sound only if the body has no suspendable
frames — a condition that was held by **two unrelated accidents**, two ordinary refactors
away from breaking — and item **4-4 is still deferred** partly behind that compromise.

**Phase 6 creates the interpreter frame.** With a VM tier, deoptimization has somewhere to
land: a guard fails in specialized IL, the engine reconstructs a *bytecode* frame at the
corresponding program point, and execution continues in the interpreter. That is the
mechanism 4-3 was written for and could not have.

### The tier-up argument

Today the engine compiles **everything** to IL eagerly — item 1-1's deferral half exists to
soften exactly that, and it is still open. A two-tier engine inverts the default: **run in
the VM, compile only what is hot.** That reaches the same target from the other side, and it
is what every production JavaScript engine does.

**But the baseline is different from the catalogue's.** In the catalogue's staging, tier 1
is a VM and tier 2 is IL. Here **IL is what already exists and is fast**, so phase 9's real
question is not "is IL faster than bytecode" — it is *"is starting in the VM and promoting
cheaper than compiling everything eagerly?"* That is 9-0.

**Owner assemblies:** `Runtime/FunctionTiering.cs`, `Engine/CallFrames.cs`,
`BuiltIns/Function/JSFunction.cs`, `.Portable`, `.Compiler`, `.ExpressionCompiler`.

## What is already built

**Most of the tiering machinery exists and is general** — phase 4 built it, and it was
designed without a VM in mind but not against one:

| | Where | State |
|---|---|---|
| Invocation threshold, per-realm budget, retained-code cap, delegate replacement, `RecordDeoptimization` counters | `Runtime/FunctionTiering.cs` (`FunctionTieringController`) | ✅ built, tested, **off by default**, and **must retain the original delegate as the semantic fallback** |
| The restart contract | item 4-3a | ✅ landed |
| A generic fallback branch inside a specialized method | item 4-3b | ✅ landed; 4-2b is its first consumer |
| Type feedback per site | item 4-1 | ✅ landed |
| A frame model addressed by a struct token | phase 4's shadow stack (`CallFrameStack`, `FrameToken`) | ✅ landed — an argument-less call allocates **0 B** |
| A hotness counter driving a race, in production | phase 5's item 5 | ✅ shipped, default off |

**So phase 9 is largely wiring**, and the genuinely new work is 9-3 and 9-4.

## Items

| # | Item | Size | State |
|---|---|---|---|
| **9-0** | **Is tier-up worth it here?** — the entry measurement | M | ❌ **not started; blocks the phase, and may cancel it** |
| **9-1** | Hotness counters in the VM, feeding `FunctionTieringController` | S | ❌ |
| **9-2** | Promote bytecode → IL where dynamic code is permitted | M | ❌ |
| **9-3** | **Deoptimize IL → bytecode** — the item phase 4 could not build | L | ❌ |
| **9-4** | OSR — switch a running hot loop into compiled code | **XL** | ❌ |
| **9-5** | Retire 4-3a's restart compromise, if 9-3 subsumes it | M | ❌ |

### 9-0 · Is tier-up worth it here? — **the entry measurement, and it may cancel the phase**

Three numbers, and the phase does not start without them:

1. **Start-in-VM-and-promote against compile-everything-eagerly**, on the real corpora, for
   time *and* allocation *and* peak working set. The IL path's eager compilation is the
   incumbent and it is fast; the VM's advantage is that **84–99.7% of a script's functions
   are never invoked**, so most of that compilation is wasted.
2. **How that compares to item 1-1's deferral half**, which reaches the same waste without a
   second tier and is already half-built. **If 1-1 gets most of it, 9-2 is redundant** —
   and finding that out costs a measurement rather than an M.
3. **The promotion threshold's sensitivity.** Phase 5's item 5 is the cautionary tale: a
   per-pattern tiering race, built complete and correct, measured at **1.010× on 3 of 6
   pairs** and shipped **off by default** because no speed-up was worth a retained
   `DynamicMethod`. A per-function tier has the same failure mode at larger scale.

**Note the honest ordering:** 9-3, not 9-2, is the item with the argument behind it. If 9-0
says promotion is not worth it, **9-3 may still be** — deoptimization is a correctness
capability for phase 4, not a speed-up for phase 9.

### 9-1 · Hotness counters in the VM

The interpreter counts invocations and loop back-edges and hands them to
`FunctionTieringController`, which already has the threshold, the budget and the cap.

**S, because the controller exists.** Back-edge counting is what 9-4 later needs; add it
here even if OSR is never built, since it costs one increment in a loop that is already
paying dispatch.

### 9-2 · Promote bytecode → IL

Where `RuntimeFeature.IsDynamicCodeCompiled` is true, recompile a hot function through the
existing `FastCompiler` + `.ExpressionCompiler` path and swap the delegate.

**Two rules inherited from phase 4, both non-negotiable:**

- **Retain the original as the semantic fallback.** This is already
  [`Measurement.md`](Measurement.md)'s rule for function tiering and it is why the existing
  controller is safe.
- **Measure that the promoted arm produces the same answers.** Item 4-2a exists because the
  shipping tier-2 hook produced **wrong answers** — DeltaBlue died on it — and that was found
  by measuring the branch, not by a test.

**On AOT platforms this item is inert by construction**, which is correct: the VM is the
only tier there, and phases 6–8 are what make that acceptable.

### 9-3 · Deoptimize IL → bytecode — **the item phase 4 could not build**

A guard fails in specialized IL; the engine reconstructs a **bytecode** frame at the
equivalent program point and resumes in the interpreter.

**What makes it possible now and not before:** the VM frame holds JavaScript values in a
form the engine controls, so "reconstruct an interpreter frame" is a data transformation
rather than an impossibility.

**What makes it hard:** the **mapping**. Every point in the specialized IL where a guard can
fail needs a corresponding bytecode offset and a rule for materializing the VM frame's locals
and operand stack from the IL frame's state. That mapping has to be produced by the same
compilation that emits the guards, kept in sync with 6-5's peephole pass, and tested at
*every* guard rather than at a sample.

**The test already exists in specification form** — phase 4's exit gate: *a test that forces
every guard to fail at every point in a function body and asserts the fallback produces the
unspecialized answer.* 4-3b satisfies it with an in-method branch; 9-3 must satisfy it
across the tier boundary.

### 9-4 · OSR — **XL, and last**

Switch a *running* hot loop from the VM into compiled code without waiting for the call to
return. The catalogue rates it ⭐⭐, "extremely high effect, very high effort", and marks its
AOT compatibility ⚠️.

**It is the correct last item.** It needs 9-3's mapping in the opposite direction, and its
value is concentrated in long-running loops — which is a population that should be counted
before it is served, exactly as 4-4's was.

### 9-5 · Retire 4-3a's restart compromise, if 9-3 subsumes it

4-3a's restart contract is sound only under a no-suspendable-bodies condition that was held
by accident. **If 9-3 gives the engine real deoptimization, the compromise may be
retired** — which removes a latent hazard from the IL path and is a clean payback from the VM
track into the one that ships today.

**Check, do not assume.** Restart may still be the cheaper path for the cases it covers.

## Order

```
9-0 measure ← BLOCKS THE PHASE, and may cancel 9-1/9-2 while leaving 9-3 justified
  ├→ 9-1 hotness counters → 9-2 promote (only if 9-0 beats item 1-1's deferral)
  └→ 9-3 deoptimize  ← the item with an argument independent of 9-0
        ├→ 9-5 retire the restart compromise (check first)
        └→ 9-4 OSR (last; count the loop population first)
```

## Exit gate

1. **Deopt correctness proven before any tier-up ships** — phase 4's gate, unchanged, now
   applied across the tier boundary: force every guard to fail at every point and assert the
   fallback produces the unspecialized answer.
2. **The full test262 matrix on every tier configuration** — VM only, IL only, and VM with
   promotion enabled. This phase can break anything, and it can break it in a way that only
   appears after N invocations.
3. **Tier-up stays opt-in** until its supported semantics and fallback behaviour are
   release-tested, per [`Measurement.md`](Measurement.md) — the same rule that keeps the
   existing function tiering and phase 5's regex race off by default.
4. **A rate, not a share, and a threshold sensitivity curve.** Phase 5's item 5 shipped off
   by default on exactly this evidence; that outcome must remain available here.

## Dependencies

**Depends on phase 6 in full** and on phase 7 for the VM to be worth promoting *from*.
**Does not depend on phase 8** — tier-up needs a correct VM, not an optimized one, so 8 and
9 may be re-ordered.

**Pays back into phase 4**: 9-3 is the deoptimization 4-3 was specified for, 9-5 retires
4-3a's compromise, and item **4-4** — inlining, currently deferred behind a 2.43% ceiling and
a restricted fallback — should be re-priced once 9-3 lands.
