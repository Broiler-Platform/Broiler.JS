# Phase 6 — VM 1.0: a correct bytecode interpreter — status

**No measurement exists. Nothing in phase 6 has been built, measured, or attempted.**

> The evidence half of [`Phase-6.md`](Phase-6.md). This file exists so the convention
> holds across the whole directory — **a plan document is never the place a number lives** —
> and so the phase's entry measurement has somewhere to land. When the first result arrives,
> it goes here.
>
> **Nothing in the plan document may be quoted as a result.** Every figure it cites is
> borrowed from phases 0-5 and is attributed there. [`Measurement.md`](Measurement.md)
> governs what may be claimed.

---

## State

| | |
|---|---|
| Items started | **0** |
| Items landed | **0** |
| Measurements taken | **0** |
| Blocked on | item **6-0 · Scope the target**, which has not been run |

**This phase is not scheduled.** It is written down so the option is specified rather than
vague, and so the entry measurement below is the first thing anyone doing it would have to
produce. Track two's justification is argued in
[`Roadmap.md`](Roadmap.md#track-two--the-vm-tier-phases-69), and **it is an argument, not a
measurement.**

---

## The entry measurement — item 6-0

**What 6-0 must report, in writing, before item 6-1 is started:**

1. **Which platforms actually forbid dynamic code, for this product.** iOS? `PublishAot`
   with `IsDynamicCodeSupported=false`? WebAssembly? A locked-down embedding? Name them,
   and say which are required rather than merely interesting.
2. **What `samples/Broiler.JavaScript.NativeAotSample` can and cannot run today.** It
   already sets `PublishAot=true`. Establish the current line empirically rather than from
   this document.
3. **What surface a real page needs.** Count the constructs the WPT and Octane corpora
   actually use, and mark which of them `Broiler.JavaScript.Portable`'s twenty opcodes
   reach. The expected answer is *almost none* — it is a `double`-only expression
   evaluator — but the count is what sizes item 6-2.
4. **Whether the IL path can be kept instead.** ReadyToRun, a persisted assembly, an IL
   interpreter, a different publish configuration. **If any of these reaches the required
   platforms, phases 6-9 should be abandoned**, and this measurement is what says so.
5. **Whether generators, async and `eval` are in scope.** Item 6-7 is an XL on its own and
   the answer changes the phase's deliverable.

**Cost of the measurement: S. Cost of skipping it: several XL.**

---

## What is already in the tree

**The current line, read from the tree rather than assumed** (2026-08-07):

| | |
|---|---|
| `Broiler.JavaScript.ExpressionCompiler` | `System.Reflection.Emit` in ~20 files — `DynamicMethod`, `ILGenerator`, `ILWriter`. There is no second back end. |
| `Broiler.JavaScript.Portable` | 301 lines across `PortableInterpreter.cs` and `PortableProgram.cs`. |
| `Broiler.JavaScript.Portable.Compiler` | 391 lines. |
| `PortableOpCode` | **20 opcodes**: `PushConstant`, `LoadArgument`, `LoadLocal`, `StoreLocal`, `Duplicate`, `Pop`, `Add`, `Subtract`, `Multiply`, `Divide`, `Remainder`, `LessThan`, `LessThanOrEqual`, `GreaterThan`, `GreaterThanOrEqual`, `Equal`, `NotEqual`, `Jump`, `JumpIfFalse`, `Return`. |
| The interpreter's shape | a `switch` over `instruction.OpCode` — already the "efficient dispatch" the catalogue's VM 1.0 asks for. |
| Values | `double` only. No object model, strings, properties, arrays, calls, closures, exceptions, modules, async/generators, host callbacks, `eval`, or runtime compilation. |

**This is a file census, not a measurement**, and it is recorded here so 6-0 starts from
facts rather than from the roadmap's prose. It establishes the starting point and **nothing
about feasibility, cost or value.**
