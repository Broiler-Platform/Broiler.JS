# Phase 6 — VM 1.0: a correct bytecode interpreter

**The first phase of a second execution tier.** Phases 0–5 make the IL path faster; phases
6–9 build a path that can run **where there is no IL path at all**. This one delivers the
whole language on it, correctly, and says nothing about speed.

> The plan half of [`Phase-6.status.md`](Phase-6.status.md) — which records that **no
> measurement exists yet**, and carries the entry measurement this phase is blocked on.
> Part of the [performance and benchmark roadmap](Roadmap.md); the four VM phases are
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69).

---

## Why this exists

**Broiler.JS today has no general JavaScript execution path on a platform that forbids
`System.Reflection.Emit`.** That is not a performance gap; it is a *capability* gap, and it
is the only reason to build a bytecode VM into an engine that already emits IL.

The evidence is in the tree:

| | |
|---|---|
| The compiler back end | `Broiler.JavaScript.ExpressionCompiler` is an **IL writer** — `DynamicMethod`, `ILGenerator`, `System.Reflection.Emit` in ~20 files. There is no second back end. |
| The AOT-safe path today | `Broiler.JavaScript.Portable` — **301 lines, 20 opcodes, `double`-only**: `PushConstant`, `LoadArgument`, `LoadLocal`, `StoreLocal`, `Duplicate`, `Pop`, five arithmetic, six comparison, `Jump`, `JumpIfFalse`, `Return`. |
| What it does **not** implement | The JavaScript object model, strings, properties, arrays, calls, closures, exceptions, modules, async/generators, host callbacks, `eval`, runtime compilation. |

**So `Portable` is a numeric expression evaluator, not an engine**, and
[`Measurement.md`](Measurement.md) already says in as many words that it must not be
described as Native AOT support for the full engine. Phase 6 is what would make that
description true.

### The second reason, which is not about AOT at all

**Phase 4's item 4-3 was re-specified because this phase does not exist.** V8-style
deoptimization — bail out mid-function by reconstructing an interpreter frame — is *not
expressible* in Broiler.JS today, because tier-1 locals are CLR locals of an IL method and
`CallFrame` carries no JavaScript values. 4-3 became a restart contract plus an in-method
fallback branch instead, and 4-4 is still gated behind that compromise.

**A bytecode VM is exactly the interpreter frame 4-3 could not find.** Phase 9 collects
that; phase 6 is what creates it.

### What this phase is *not*

- **Not a speed-up.** On any platform where `Reflection.Emit` works, an interpreter is
  **slower** than IL + RyuJIT, and nothing in phases 6–8 changes that. Do not put a VM
  number next to an IL number and call it a win. The VM's competitor is *not running at
  all*.
- **Not a second semantics.** The single largest risk here is that the VM becomes a second,
  subtly different JavaScript. See the exit gate.

### The precondition: the assembly graph forbids the deliverable

**Building this VM without [`Assemblies.md`](Assemblies.md) first would produce an
interpreter that still cannot be published Native AOT.** `Broiler.JavaScript.Portable.Compiler`
references `Parser`, `Parser` references `ExpressionCompiler`, and `ExpressionCompiler` is
the IL emitter — so the bytecode compiler transitively depends on `System.Reflection.Emit`
**today**.

**[`AssemblySplit.md`](AssemblySplit.md) removes that edge** — it is items A-0 and A-1,
planned end to end and already analyzed — and A-7's AOT gate can be built and turned green
*before* this phase starts — on the existing 20 opcodes, proving the graph rather than the
engine. **That converts phase 6 from "build a VM and hope the packaging works" into "grow an
assembly that already publishes."**

**Do A-0 and A-1 before item 6-2.**

**Owner assemblies:** `Broiler.JS.Bytecode` and `Broiler.JS.Bytecode.Compiler` (today
`Broiler.JavaScript.Portable` and `.Portable.Compiler`; both to grow considerably),
consuming `Broiler.JS.Base`, `.Ast`, `.Parser`, `.Core` and `.BuiltIns` unchanged.

## Items

| # | Item | Size | State |
|---|---|---|---|
| **6-0** | **Scope the target** — which platforms, which surface, how much of a real page | S | ❌ **not started, and it blocks everything else in this phase** |
| **6-1** | A bytecode format for the whole language | L | ❌ |
| **6-2** | A second compiler back end, sharing the front end | **XL** | ❌ |
| **6-3** | The interpreter loop and its dispatch | M | ❌ |
| **6-4** | Fast paths and slow paths per opcode | L | ❌ |
| **6-5** | Constant folding and a bytecode peephole pass | M | ❌ |
| **6-6** | Exceptions, `try`/`finally` and unwinding | L | ❌ |
| **6-7** | Generators, async and suspension | **XL** | ❌ |
| **6-8** | The dual-arm conformance gate | M | ❌ |

### 6-0 · Scope the target — **do this before anything else**

**The entry measurement, and the phase must not start without it.** Phase 0's rule applies
in full: this is the largest body of work anywhere in this roadmap, and it is currently
justified by an argument rather than a number.

It must answer, in writing:

1. **Which platforms actually forbid dynamic code**, for this product — iOS, `PublishAot`
   with `IsDynamicCodeSupported=false`, WASM, a locked-down host? `samples/Broiler.JavaScript.NativeAotSample`
   already sets `PublishAot=true`; establish what it can and cannot run today.
2. **What surface a real page needs.** Take the WPT and Octane corpora and count which
   constructs appear. A VM that cannot run generators is not a VM anybody can ship.
3. **Whether the IL path can be kept instead.** Is there a supported configuration —
   ReadyToRun, a persisted assembly, an interpreter for IL — that reaches these platforms
   without a second engine? **If yes, phases 6–9 should be abandoned**, and finding that out
   costs an S rather than several XL.

**Do not proceed to 6-1 until 6-0 has a written answer**, and record it in
[`Phase-6.status.md`](Phase-6.status.md).

### 6-1 · A bytecode format for the whole language

Extend `PortableOpCode` from a 20-opcode `double` machine to a full instruction set:
values and the constant pool, object and property operations, element operations, calls and
`new`, closures and environments, the full control-flow set, `try`/`catch`/`finally`,
iterators and destructuring.

**Two decisions to take on a measurement rather than a preference:**

- **Stack machine or register machine.** `PortableInterpreter` is a stack machine today.
  The catalogue rates a register VM "high effect, high effort". **Defer this to phase 7's
  7-5** — build VM 1.0 on the stack machine that exists, and move only if 7-0's profile says
  operand traffic is the cost.
- **Operand encoding.** Compactness is a startup and cache property, not a throughput one.
  Measure it in phase 8, not here.

**Correctness first, and deliberately so:** the whole of phase 8 exists to make this format
fast. Do not pre-optimize an instruction set that has not run a real program.

### 6-2 · A second compiler back end — **XL, and the one that decides the phase's cost**

A `PortableCompiler` that consumes the **same AST** `FastCompiler` does.

**Share everything above the back end**, and this is not a style preference — it is what
keeps the two paths one language: the parser, the AST, scope and binding analysis, the
early-error passes, `NumericLocalAnalysis`, and the hoisting rules. **Fork nothing that
decides semantics.** Every construct the front end resolves once is a construct the two arms
cannot disagree about.

**Expected cost.** `FastCompiler` plus `.ExpressionCompiler` is the accumulated work of the
whole engine's history. A second back end is not that large — the semantics are already
decided — but it is the largest single item in this roadmap and should be sized honestly
before it is scheduled.

### 6-3 · The interpreter loop and its dispatch

The catalogue rates dispatch "high effect, low–medium effort". `PortableInterpreter` is
already a `switch` over `instruction.OpCode`, which in .NET is a jump table.

**Do not start here.** Dispatch is the row every bytecode-VM article opens with and it is
worth nothing until the operations behind it are real. Build the loop plainly; phase 8's
8-0 will say whether dispatch is measurable at all.

### 6-4 · Fast paths and slow paths

The one row in the catalogue this campaign has confirmed repeatedly: **fast paths are where
the wins are.** Per opcode, handle the common operand types inline and push the spec's rare
cases out of line — the same shape as phase 3's guarded arithmetic tree, and for the same
reason.

**Carry phase 3's finding across:** box at the root, not at the leaves. A VM that boxes
every intermediate will lose to the IL path by a margin no dispatch trick recovers.

### 6-5 · Constant folding and a bytecode peephole pass

Both are catalogue rows the IL path never needed — the CLR JIT does them. **In a VM they
have no such backstop.** Fold constants at compile time; run a peephole pass over the emitted
bytecode. Both are M, both are local, and both should be gated on 8-0's histogram before
being *extended*.

### 6-6 · Exceptions, `try`/`finally` and unwinding

The IL path gets CLR exception handling for free. A VM must model handler ranges, unwind its
own operand stack, and run `finally` blocks on every exit path including `return` and `break`
out of `try`.

**It also has to interoperate**: a host callback that throws a CLR exception through VM
frames, and a JavaScript `throw` that crosses back into host code, both have to work. Test
both directions.

### 6-7 · Generators, async and suspension — **XL, and the item to scope early**

The IL path implements suspension with **CLR state machines**. A VM must suspend and resume
its own frame — which is *easier* in a VM than in IL, and is the one place the VM tier is
structurally advantaged.

**But it interacts with everything**: 4-3a's restart contract is sound only if the body has
no suspendable frames, and that condition was held by two unrelated accidents. Whoever
builds 6-7 must re-derive that condition for the VM, not inherit it.

**Scope this during 6-0**, not after 6-2. If generators are out of reach, the phase's
deliverable is smaller than "the whole language" and the roadmap should say so.

### 6-8 · The dual-arm conformance gate

**The single most important item in the phase**, and it should be built early rather than
last.

Run the pinned test262 manifests on **both** execution paths and **require them to agree** —
not merely "the VM passes", but *the VM and the IL path produce the same result for every
test*. Phase 5's item 5 is the model: 15 cases run on both settings and required to agree,
and they pass on the unmodified engine too.

**Wire it into CI from the first opcode**, so divergence is caught the day it appears rather
than at the end of an XL.

## Order

```
6-0 scope ← BLOCKS EVERYTHING; may cancel the track entirely
  └→ 6-8 dual-arm gate (build the harness before the engine)
       └→ 6-1 format → 6-2 back end → 6-3 loop → 6-6 exceptions → 6-7 suspension
            └→ 6-4 fast/slow paths → 6-5 folding + peephole
```

**6-8 before 6-1** is deliberate. A conformance harness written after an XL lands is a
harness written to agree with whatever was built.

## Exit gate

1. **The pinned test262 supported-mode run passes on the VM arm**, with **no failure the IL
   arm does not also have** — the dual-arm gate, not a standalone pass rate.
2. `samples/Broiler.JavaScript.NativeAotSample` runs a **real script** — one from the WPT or
   Octane corpus, not a numeric expression — with `PublishAot=true` and dynamic code
   disabled.
3. Every construct 6-0 identified as required is executable, or is a **published scope
   exclusion** with a reason.
4. **No performance claim of any kind.** Phase 6 closes on correctness. Speed is phase 7,
   and [`Measurement.md`](Measurement.md) governs both.

## Dependencies and risks

- **Depends on [`AssemblySplit.md`](AssemblySplit.md)** (= [`Assemblies.md`](Assemblies.md)'s
  items A-0 and A-1), without which the phase cannot deliver its headline capability — see
  the precondition above.
- **Depends on nothing in phases 0–5**, and blocks nothing in them. The two tracks are
  independent and can run in parallel — which is also the argument for *not* starting this
  one until phases 1, 3, 4 and 5 have closed their open items.
- **Phase 9 depends on this phase entirely.** So, indirectly, does the deoptimization design
  phase 4 had to work around.
- **Risk: two engines, one specification.** Mitigated by 6-2 (share the front end) and 6-8
  (require agreement), and by nothing else. If either is compromised, stop.
- **Risk: the effort is not recoverable.** 6-0 exists to price that before it is spent.
