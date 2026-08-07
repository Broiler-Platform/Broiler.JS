# Phase 8 — VM 3.0: a highly optimized interpreter

The catalogue's VM 3.0 stage — type feedback, adaptive opcodes, superinstructions, PGO —
plus the bytecode cache, which is the one row that makes a VM **beat** the IL path at
something. **Every item here is gated on a histogram this phase's own 8-0 produces**, and
none of them may be started from the catalogue's star ratings.

> The plan half of [`Phase-8.status.md`](Phase-8.status.md) — which records that **no
> measurement exists yet**, and carries the entry measurement this phase is blocked on.
> Part of the [performance and benchmark roadmap](Roadmap.md); the four VM phases are
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69).

---

## Why this phase is written as a gate rather than a list

**This is where a catalogue does the most damage.** The five techniques below are the ones a
bytecode-VM article ranks highest, and this campaign has now refuted three
catalogue ⭐⭐⭐⭐⭐ rows on measurement — the call-site cache at literally zero, shapes and ICs
as *already built and inert*, and unboxed numbers as right about the target and wrong about
the site four times running.

**So the phase's structure is deliberate**: one measurement, then items drawn *from* it.
Nothing here is scheduled. The item list below is a menu of what 8-0 might justify, with the
population each would need in order to be worth building.

**And one number sizes the whole phase before it starts.** In the IL path, removing **54.0%
of everything the corpus allocates** bought **3.1% of the time**, and collection is
1.8–2.0% of the driver. A VM's ratios will differ — that is 8-0's job to establish — but the
discipline does not: **bid with a rate (ns per operation, ms per GB), never with a share.**

**Owner assemblies:** `Broiler.JavaScript.Portable`, `.Portable.Compiler`, consuming
`Runtime/TypeFeedback.cs` from item 4-1.

## Items

| # | Item | Size | Justified only if 8-0 shows… | State |
|---|---|---|---|---|
| **8-0** | **Decompose the VM's execution time** | M | — it is the gate | ❌ **not started; blocks the entire phase** |
| **8-1** | Type feedback in the VM | M | operations are polymorphic at run time but monomorphic per site | ❌ |
| **8-2** | Adaptive (quickened) opcodes | L | a measurable share of generic opcodes see one operand type | ❌ |
| **8-3** | Superinstructions | M | frequent opcode *pairs*, from a measured histogram | ❌ |
| **8-4** | Dispatch-table layout and opcode ordering | S | dispatch is a measurable share at all | ❌ |
| **8-5** | PGO the AOT image on real JS workloads | S–M | the AOT image's own code layout costs something | ❌ |
| **8-6** | **A bytecode cache** | M | startup is the metric anybody cares about — **and it usually is** | ❌ |

### 8-0 · Decompose the VM's execution time — **the gate**

Phase 7's 7-0 establishes the VM's baseline and its ratio to the IL path. **8-0 is
different: it attributes that time.** Per suite and in aggregate:

- **dispatch** — the switch, the branch predictor, the loop overhead;
- **operand decode** — reading the instruction and its operands;
- **the operations themselves**, split by opcode family: property, element, arithmetic,
  call, allocation, control flow;
- **allocation**, reported beside time, per family.

**Then a bigram histogram of executed opcode pairs**, which is 8-3's entire justification and
costs almost nothing to collect once the loop is instrumented.

**Learn phase 4's lesson about instruments before choosing one.** A sampling profiler does
**not** decompose this engine: it inflated the driver ~29%, its biggest frame was its own
rendezvous point, and compiled JavaScript does not symbolicate. A VM is far more tractable —
the interpreter loop is one method, and counters inside it are exact — so **instrument the
loop, do not sample it.** That advantage is real and is worth stating: it is the one place
the VM tier is *easier* to measure than the IL tier.

**Also take the denominator honestly.** Phase 3 and phase 4's headline numbers were computed
over **7 of 15 suites and never said so**; widened, 65.4% of the boxes were outside the
seven and Gameboy alone was 1.32× the whole measured corpus. **Run all fifteen.**

### 8-1 · Type feedback in the VM

`Runtime/TypeFeedback.cs` already exists (item 4-1) and records receiver shapes at reads and
callee identities at calls. **Consume it; do not build a second one.**

**The premise is already measured and it is weaker than it looks:** 93.5% of reads and 96.7%
of calls are monomorphic by execution weight over seven suites — **80.11% and 86.35% over
twelve.** Still high enough to found the work, but the phase's own budget should be computed
against 80%, not 93.5%.

### 8-2 · Adaptive (quickened) opcodes

`Add` → `AddNumber` after observation, and the same for the property and element families.
**This is the row the catalogue marks n/a for the IL path** — there are no opcodes to adapt —
and it is the clearest thing a VM tier makes newly possible.

**Its analogue in the IL path was measured and refused.** Item 4-2c specialized arithmetic
from type feedback and came to **0.119% of the corpus**, net negative at the `+` rate,
because the failing guard costs **18.567×**. In a VM the guard is cheaper and the baseline
is slower, so the arithmetic is different — **but 4-2c's population census transfers, and it
is the population 8-2 has to beat.** Read it before designing this.

### 8-3 · Superinstructions

Fuse frequent opcode pairs into one. **Justified only by 8-0's bigram histogram**, and by
nothing else — the set of pairs worth fusing is a property of the emitted bytecode, so a
list copied from another engine is a list for another engine's compiler.

Cap the count. Each fused instruction is a new opcode, a new handler, a new case in the
dual-arm gate, and a permanent maintenance cost.

### 8-4 · Dispatch-table layout and opcode ordering

The catalogue rates this "small–medium effect, low effort". **Do it only if 8-0 says
dispatch is measurable at all**, and expect it not to be: in .NET the `switch` is a jump
table and the operations behind it are heavy.

### 8-5 · PGO the AOT image on real JS workloads

The catalogue's "PGO for the VM itself" — profile-guided optimization of the Native AOT
image, driven by real JavaScript rather than by a microbenchmark. **Low effort, and it is
the one item here that needs no new engine code**, only a build pipeline and a
representative workload.

**It requires phase 0's 0-8 hardware.** A PGO claim is a performance claim on a release
configuration, and [`Measurement.md`](Measurement.md) requires the RID matrix.

### 8-6 · A bytecode cache — **the item most likely to matter**

Serialize compiled bytecode and reuse it, so a second run of the same script skips
compilation entirely.

**This is the one row in the whole catalogue where the VM tier can beat the IL tier**, and
the campaign has already measured why. Phase 1 found that:

- **84–99.7% of a script's functions are never invoked** once it has been evaluated;
- deferring IL emission alone took jQuery to **0.661×**, PdfJS 0.689×, Box2D 0.636×;
- **CodeLoad — the benchmark that measures exactly this — is 371× off Chromium**, and even
  after item 1-1's emission half it is 228× and among the worst four scores in the suite.

**Bytecode is serializable in a way emitted IL is not.** Item 1-1's deferral is Broiler.JS
working around the absence of this row.

**One warning, from phase 0's item 0-5:** `DictionaryCodeCache.Current` is present but
commented out in the shell's `Program.cs`, and **CodeLoad is only a genuine compile-throughput
measurement while it stays that way.** A bytecode cache that is live during a CodeLoad run
turns the benchmark into a measurement of cache lookup, and *nothing in the score will say
so.* Report cached and uncached runs separately, always.

## Order

```
8-0 decompose ← BLOCKS THE PHASE, and chooses which items below exist at all
  ├→ 8-6 bytecode cache — schedule independently; its justification is startup, not 8-0
  ├→ 8-1 feedback → 8-2 adaptive opcodes   (only against 4-2c's population)
  ├→ 8-3 superinstructions                 (only from 8-0's bigram histogram)
  └→ 8-4 dispatch layout, 8-5 PGO          (only if 8-0 measures them; 8-5 needs 0-8)
```

**8-6 is the exception and should probably go first.** Its value is startup, its
justification is already measured in phase 1, and it does not depend on 8-0 at all.

## Exit gate

1. **Every item cites the 8-0 measurement that justified it**, with a population and a rate.
   An item that cannot is not built. This is the phase's whole point.
2. **The dual-arm test262 gate still holds** (6-8). 8-2 in particular changes what an opcode
   does at run time; that is exactly the surface 4-2a's wrong-answer bug lived on.
3. **Cached and uncached runs reported separately** wherever 8-6 is live.
4. **All fifteen suites**, not seven. The corpus error that qualified phases 3 and 4 must not
   be repeated in a new phase that has the chance to avoid it.
5. Everything closes under [`Measurement.md`](Measurement.md).

## Dependencies

Depends on phases 6 and 7. Consumes item 4-1's `TypeFeedback` unchanged. 8-5 depends on
phase 0's item **0-8**, which is not satisfied on any RID. **Nothing in phase 9 depends on
this phase** — tier-up needs a correct VM, not a fast one — so 8 and 9 may be re-ordered if
the AOT platforms turn out to matter less than the tier-up does.
