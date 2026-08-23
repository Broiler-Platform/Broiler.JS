# Phase 6 — VM 1.0: a correct bytecode interpreter

Phase 6 is the first implementation phase of a second execution back end. It starts only
after [`Modernization.md`](Modernization.md)'s MOD-M9 decision records one of the three positive
outcomes: `execution-only-go`, `narrow-runtime-go`, or `full-go`. A `no-go` cancels phases
6–9; it does not leave this phase “open but unscheduled.”

The deliverable is the JavaScript capability profile approved by that decision, correctly
executed without dynamic code. An `execution-only-go` funds a correct product that executes
verified, precompiled bytecode and does **not** require a parser or source compiler at run
time. A `narrow-runtime-go` additionally funds a deliberately constrained runtime source
compiler. A `full-go` funds the approved general runtime-compiler surface. Neither narrow
outcome may be described as general JavaScript support.

> The plan half of [`Phase-6.status.md`](Phase-6.status.md). That status record remains the
> evidence source and currently records that no phase item has started.
> Part of the [performance and benchmark roadmap](Roadmap.md); the four VM phases are
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69).

---

## Why this exists

Broiler.JS has no general JavaScript execution path on platforms that prohibit
`System.Reflection.Emit`. The existing `Broiler.JavaScript.Portable` project is an
execution-only numeric seed: it proves that a small, immutable bytecode program can run
without dynamic code, not that the JavaScript compiler/runtime graph or general language
surface can publish and run under Native AOT.

That is a capability gap first. On platforms where the IL path works, a bytecode tier may
later reduce startup work or provide a deoptimization target, but those are separate Phase 8
and Phase 9 decisions. Phase 6 closes on correctness and deployability, not speed.

### 6-0 is the MOD-M9 decision, not a second scoping exercise

MOD-M9's terminal ADR satisfies item 6-0. Do not repeat the study after the implementation has
been funded. Before any other item starts, the ADR must name:

1. required platforms and RIDs, including which actually prohibit dynamic code;
2. one of the compilation profiles below, or both;
3. the required ECMAScript, module, host, debugging, and interop capability manifest;
4. whether `eval`, the Function constructor, dynamic import, top-level await, generators,
   async functions, and runtime source compilation are supported or intentionally excluded;
5. conformance and resource thresholds, a maintenance ceiling, and named owners; and
6. the alternatives considered, including retaining the IL path, persisted IL where
   supported, and a deliberately narrow portable product.

| MOD-M9 outcome | Profile | Contains at run time | What its AOT gate proves |
|---|---|---|---|
| `execution-only-go` | **Bytecode execution-only** | runtime/interpreter plus verified precompiled bytecode; no parser or compiler | the approved precompiled surface executes under Native AOT |
| `narrow-runtime-go` | **Bytecode runtime-compiler, constrained** | parser, shared semantic front end, bytecode lowering, verifier, runtime, and interpreter for the named subset | approved source is compiled and executed inside the published Native AOT application |
| `full-go` | **Bytecode runtime-compiler, general** | parser, shared semantic front end, bytecode lowering, verifier, runtime, and interpreter for the approved general surface | approved general source is compiled and executed inside the published Native AOT application |

The current Native AOT sample belongs to the first row. It must not be used as evidence for
the second until it references and invokes the runtime compiler closure.

### The assembly prerequisite has changed

The expression-model/emitter split has landed: `Portable.Compiler` no longer reaches the IL
emitter through `Parser`. That removes the original impossible edge, but it is not the full
AOT gate. Phase 6 still waits for the applicable MOD-M2/MOD-M3/MOD-M4 outcomes:

- an acyclic, build-proven target graph and backend-neutral semantic boundary;
- explicit backend registration with no magic-name discovery in a portable profile;
- a real IL boundary for every Emit-using runtime component and tool;
- backend-neutral hosting contracts separated from CLI/Roslyn/NuGet composition; and
- a compiler library separated from the optional bytecode command-line tool.

Do not hard-code the aspirational `Base`/`Core` merge into this phase. The graph ADR produced
by MOD-M2 is authoritative.

## Items

| # | Item | Size | State |
|---|---|---|---|
| **6-0** | **Adopt the terminal MOD-M9 capability/profile ADR** | S | ❌ **not started; blocks the phase and may cancel it** |
| **6-1** | Extract the production shared semantic IR and migrate the IL arm to it | **XL** | ❌ |
| **6-2** | Specify and prove the VM `ValueSlot`, frame, environment, and call ABI | L–XL | ❌ |
| **6-3** | Build a versioned bytecode format and verifier | L | ❌ |
| **6-4** | Add bytecode lowering and the interpreter in vertical semantic slices | **XL** | ❌ |
| **6-5** | Add correctness-preserving baseline folding and peephole simplification | M | ❌ |
| **6-6** | Implement abrupt completion, exceptions, `try`/`catch`/`finally`, and unwinding | L–XL | ❌ |
| **6-7** | Implement the approved hard surface: suspension, modules, eval, debugging, and host interop | **XL** or scoped exclusions | ❌ |
| **6-8** | Build the independent three-way conformance gate | M | ❌ |

### 6-1 · Production shared semantic IR — **before the bytecode ISA**

Sharing an AST is not sharing JavaScript semantics. Parsing, early errors, strictness,
binding, scope and environment layout, hoisting, private names, direct-eval rules, free-name
analysis, and backend-neutral lowering must have one production implementation.

Extract that implementation behind an immutable or explicitly owned IR and migrate the IL
arm to it first. The IL arm must retain its pinned conformance result before bytecode
lowering begins. A fake backend from MOD-M2 proves dependency shape; it does not satisfy this
production gate.

The IR must also carry stable function identity and compilation context: source span,
strict/async/generator flags, lexical and private environment shape, home-object requirements,
module state, source locations, and cache-key inputs. Phase 9 must not recover these later by
re-parsing a function as a fresh top-level program.

### 6-2 · The VM value and frame ABI — **before encoding opcodes**

The numeric seed uses `double` locals and an operand stack. A general interpreter cannot
simply replace those with `JSValue[]` and still claim that numeric arithmetic boxes only at
the root. Decide and prove:

- the tagged/value-slot representation for Number and managed-reference values;
- GC rooting and lifetime for operand slots, locals, environments, arguments, and constants;
- call, construct, tail-call, host-call, and return conventions;
- frame ownership, recursion/resource limits, and interaction with the engine's shadow stack;
- completion records and handler state for `return`, `break`, `continue`, and `throw`;
- heap representation and resumption contract for generators and async functions; and
- stable source, exception, suspension, debugger, deopt, and OSR safepoints.

Use correctness fixtures and focused JIT/Native-AOT representation measurements before
freezing the format. A representation decision is not accepted because it looks compact.

### 6-3 · A versioned format and verifier

Start with the first end-to-end semantic slice and grow the format with the interpreter.
Do not enumerate a “whole-language” opcode set in advance of the shared IR and ABI.

The verifier is a trust boundary even when cache files are locally produced. It must reject
malformed or resource-hostile programs before execution, covering at least:

- magic and schema/semantic version, feature profile, and bounded section sizes;
- opcode and operand kinds, constant/local/function indexes, and instruction boundaries;
- reachable and unreachable control-flow validity and consistent stack/value states;
- exception-region nesting, `finally` continuations, and suspension/resume targets;
- maximum operand stack, locals, frames, constants, and aggregate allocation; and
- source/debug/deopt metadata that refers only to valid canonical bytecode positions.

Add malformed-input unit tests and coverage-guided fuzzing. The internal format may evolve
during Phase 6; compatibility is promised only when a persisted-cache version is accepted.

### 6-4 · Lowering and interpreter — build vertical slices

For each approved semantic slice:

`shared IR → bytecode lowering → verification → interpreter slow path → expected-result tests`

Start with a plain, readable dispatch loop. Do not assume a C# `switch` becomes a jump table
on every JIT/AOT RID; Phase 8 measures the generated dispatch before changing it. Reuse
runtime semantic operations where their ownership and AOT closure are valid, but do not
reuse process-global emitted-site indexes or mutable feedback as bytecode state.

Fast paths required to make an operation usable may live beside its slow path, but Phase 6
makes no throughput claim. Optional specialization waits for Phase 7 or Phase 8 evidence.

### 6-5 · Baseline folding and peephole simplification

Implement only transformations needed to avoid obviously redundant baseline bytecode, and
give every transformation an unsimplified control plus exact semantic fixtures. Preserve
observable evaluation order, coercion, exceptions, source positions, handler regions, and
canonical safepoint identity. Extensions beyond that baseline wait for Phase 8's measured
opcode population.

### 6-6 · Abrupt completion and unwinding

Model JavaScript completion records explicitly. A VM must unwind its own operand/local state
and run `finally` blocks on every applicable exit, including `return`, `break`, `continue`, a
JavaScript throw, and a host exception crossing VM frames.

Test both JS↔host directions, nested handlers, return/throw replacement by `finally`, and all
legal control-flow exits. Exception edges are part of the verifier's control-flow model, not
an interpreter-only afterthought.

### 6-7 · Suspension, modules, eval, debugging, and host interop

This item implements exactly the hard surface approved by 6-0. A `full-go` cannot silently
defer it. An `execution-only-go` or `narrow-runtime-go` records every excluded source and
runtime capability, its deterministic failure mode, and the supported precompilation path
in the public capability manifest.

Generators and async functions preserve the VM frame and handler/completion state across
suspension. Modules preserve live bindings and top-level-await ordering. Direct eval and the
Function constructor either compile through the runtime-compiler profile with the correct
lexical context or are explicitly unsupported by the execution-only profile. Stack traces,
breakpoints, source maps, and host callbacks use the canonical source/bytecode metadata from
6-2 and 6-3.

### 6-8 · The independent conformance gate — **scaffold first**

Build the harness and supported-feature manifest before production bytecode expansion. For
every case it records three facts:

1. the pinned upstream/fixture expected result;
2. the IL-arm result; and
3. the bytecode-arm result.

IL/bytecode agreement is a valuable differential check, but it is not the oracle: two arms
agreeing on the same wrong answer is still a failure. Every unsupported or allowed-failure
case requires an explicit, reviewed manifest entry. Compare completion type/value and
observable effects rather than relying only on textual output.

Wire supported slices into CI from the first opcode. Run them from source, from a serialized
round-trip once persistence exists, and through each claimed Native AOT profile.

## Order

```text
MOD-M9 ADR = 6-0 scope/profile decision ← may cancel the track
  └→ 6-8 harness skeleton + independent expected-result manifest
       └→ 6-1 production shared semantic IR; migrate and validate IL
            └→ 6-2 ValueSlot/frame/environment/call ABI
                 └→ 6-3 minimal versioned format + verifier
                      └→ 6-4 vertical semantic slices
                           ├→ 6-6 abrupt completion and exceptions
                           ├→ 6-7 approved suspension/module/eval/debug/host surface
                           └→ 6-5 baseline simplification, then measured extensions later
```

The harness scaffolding precedes the implementation; its supported manifest grows with each
vertical slice. The format and interpreter evolve together after the semantic IR and ABI are
known.

## Exit gate

1. The pinned expected-result manifest passes on the IL and bytecode arms for the approved
   scope; there are no unexplained shared or VM-only failures.
2. The execution-only AOT application publishes and runs verified precompiled bytecode on
   every claimed RID. If runtime compilation is approved, a separate application includes
   the parser/compiler closure, compiles source inside the Native AOT process, and runs it.
3. Every capability named by 6-0 is executable. An `execution-only-go` or
   `narrow-runtime-go` also publishes each precise exclusion, its deterministic failure
   mode, and the supported precompilation/runtime-compilation boundary.
4. Malformed bytecode, corrupt serialized data, and configured resource-limit cases fail
   deterministically without executing invalid instructions or allocating past the bound.
5. The IL arm's semantic result remains at its accepted pre-extraction baseline.
6. **No performance claim of any kind.** Phase 6 closes on correctness, format safety, and
   deployability. Phase 7 establishes performance under MOD-M1.

## Dependencies and risks

- **Depends on MOD-M9/6-0**, MOD-M2's proven graph/front-end contract, MOD-M3's IL/AOT boundary, and the
  applicable MOD-M4 hosting/package split. The landed model/emitter split is necessary but not
  sufficient.
- **Phase 7 depends on this phase's approved scope and exit evidence.** Phase 9 additionally
  depends on stable frame/safepoint metadata; metadata design here does not promise that
  deoptimization or OSR is feasible.
- **Risk: two engines, one specification.** Mitigated by production shared semantics and an
  independent oracle, not by a shared AST or differential agreement alone.
- **Risk: an execution-only smoke is presented as a compiler graph gate.** Keep the two AOT
  profiles, package closures, and evidence bundles separate.
- **Risk: a format is frozen too early.** Keep it internal/versioned until representative
  scripts and malformed-input gates pass; persist only an explicitly accepted version.
- **Risk: the effort is not recoverable.** The MOD-M9 terminal decision and maintenance ceiling
  exist to price that before several XL items begin.
