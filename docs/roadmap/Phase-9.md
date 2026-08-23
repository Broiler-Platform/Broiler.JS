# Phase 9 — VM 4.0: optional adaptive IL/bytecode execution

Decide whether starting selected work in bytecode and promoting it to IL improves product
startup/resource use on hosts where dynamic code is permitted. Separately decide whether an
explicit IL-to-bytecode deoptimization ABI is feasible and valuable. OSR remains a later,
independent high-risk option.

Phase 9 is not automatically authorized by a Phase 6 VM decision. An `execution-only-go`
funds a precompiled bytecode product and does not by itself fund runtime IL tiering. A
`narrow-runtime-go` or `full-go` may enter this phase only when its product/host profile also
includes a dynamic-code-capable composition and names adaptivity as a requirement.

> The plan half of [`Phase-9.status.md`](Phase-9.status.md). No Phase 9 measurement or
> feasibility result exists.
> Part of the [performance and benchmark roadmap](Roadmap.md); the four VM phases are
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69).

---

## Capability is not adaptivity

Phases 6–8 can provide JavaScript on a platform where IL emission is prohibited. Phase 9
targets hosts where both approved bytecode and IL compositions exist:

- **tier-up question:** is VM-first plus promotion better than the accepted current IL
  startup/lazy-compilation path? and
- **deopt question:** can emitted IL explicitly materialize a semantically complete VM frame
  at supported guards, and does that capability retire a real current limitation?

These questions have different entry evidence. A negative tier-up result does not prove
deoptimization infeasible; a technically feasible deopt path does not prove it is worth its
metadata, guard, code-size, or maintenance cost.

## What is already built — a control-plane seed, not this tier

| Existing facility | Useful part | Missing for Phase 9 |
|---|---|---|
| `FunctionTieringController` | per-realm threshold/budget/counters and delegate replacement | bytecode-function descriptor, safe cross-backend compilation, owner-local feedback, cancellation/publication policy |
| source-text tiering hook | an opt-in experiment and retained original delegate | realm/function identity correctness; it reparses a function as a fresh top-level program and therefore refuses observable self/scope cases |
| restart and in-method fallback contracts | proven limited fallbacks and deopt counters | resume-at-bytecode-PC state materialization |
| `CallFrameStack` / `FrameToken` | stack-trace identity and low-allocation engine bookkeeping | operand stack, live locals/environments, completion/handler state, bytecode PC |
| current type feedback | historical population evidence | realm/function ownership, thread safety, snapshot/invalidation/eviction under MOD-M6 |

The current controller can remain the budget/transition coordinator after its semantics are
proved, but Phase 9 is not “largely wiring.” Compilation identity, frame state, safepoints,
publication, fallback, and lifetime are the main work.

## Items

| # | Item | Size | State |
|---|---|---|---|
| **9-0** | **Decide whether VM-first promotion beats accepted current-IL alternatives** | M | ❌ **not started; may cancel 9-1/9-2** |
| **9-1** | Owner-local hotness counters and promotion state | S–M | ❌ |
| **9-2** | Promote bytecode → IL from a realm-correct compiled-function descriptor | L | ❌ |
| **9-3** | **Feasibility spike, then explicit IL → bytecode deoptimization** | L spike; XL implementation | ❌ |
| **9-4** | OSR entry-stub feasibility and any measured implementation | **XL** | ❌ |
| **9-5** | Retire or retain the restart compromise after 9-3 evidence | M | ❌ |

### 9-0 · Is VM-first promotion worth it? — **the performance gate**

Under MOD-M1, compare on the same accepted modern/product workload manifest:

1. VM-only;
2. VM-first with promotion enabled over a predeclared threshold curve;
3. current IL eager behavior where still supported;
4. the accepted current-IL lazy/deferred-function path; and
5. bytecode persistence/cache arms where relevant, identified explicitly.

Report first-context/first-script/first-paint and steady-state time with allocation, GC,
peak/steady memory, retained code/source/IR/bytecode, code and package size, compilation queue
and latency, promotion count/failure, and p50/p95/p99. Separate foreground and any approved
background compilation. Compare the promoted and non-promoted results to the independent
expected-result manifest, not only to each other.

Use CoreCLR dynamic-code-capable hosts for the tier-up comparison. Native AOT-only targets
remain VM-only controls; they cannot execute item 9-2 and must not be averaged into a tier-up
claim.

Write the primary product metric, minimum relevant effect/equivalence budget, resource
guardrail precedence, and maintenance/retained-code ceiling before running the curve. If the
current IL lazy path captures the benefit, the effect remains below measurement resolution,
or every useful threshold violates a resource guardrail, cancel 9-1/9-2.

### 9-1 · Hotness and transition state with explicit ownership

Count function invocation and, only if a funded consumer exists, loop back edges. The state
belongs to the bytecode function/program or realm, is not serialized, and is reclaimed with
the code/cache entry. Define atomic transition states, duplicate-promotion suppression,
cancellation, failure/backoff, and quiescent publication.

This item depends on MOD-M6 whenever compilation/installation can overlap work or contexts run
in parallel. One context remains single-entrant; a replacement delegate is installed only at
an approved quiescent boundary.

### 9-2 · Promote from a compiled-function descriptor, not reparsed source

The shared Phase 6 front end must retain a backend-neutral `CompiledFunctionDescriptor` (or
equivalent) containing the function's semantic IR and identity inputs: realm, source span,
strictness, lexical/private environment shape, home object, module state, arguments/eval
requirements, and stable site/safepoint mapping.

The IL backend compiles that descriptor and installs only a delegate proven equivalent for
the original function object/environment. Do not reparse the function text as a fresh
top-level script and steal the delegate from a second function object; the current tiering
hook documents why that changes observable identity and scope.

Retain the VM/original delegate as fallback, bound retained code and metadata by the
per-realm budget, and release failed/evicted candidates. Run source, cached-bytecode,
promoted, promotion-failure, and disabled arms through the independent oracle.

### 9-3 · IL → bytecode deoptimization — **feasibility before promise**

Having a VM frame as a destination is necessary but not sufficient. Managed code has no
supported general mechanism for inspecting arbitrary live CLR locals at a guard. The IL
backend must explicitly materialize every supported live value.

Before implementation, prove one end-to-end guarded loop/function with a documented ABI:

- a canonical bytecode PC/safepoint independent of peephole or quickened opcode addresses;
- a generated `DeoptState` containing live locals/operand values, lexical environment,
  receiver/arguments/new-target state, completion value, and handler/finally state;
- IL guard code that spills/passes that state before leaving the compiled frame;
- verifier/source/debug metadata tying every materialized slot to the shared IR; and
- a VM resume entry that reconstructs the exact frame without replaying observable effects.

Define the supported safepoint subset explicitly. Guards inside exception filters/handlers,
`finally`, host calls, async/generator suspension, direct eval, or other complex regions are
excluded until separately proved. Force every supported guard to fail and compare the
resumed answer/effects to uninterrupted VM and IL controls.

If the spike cannot materialize source state within the predeclared code-size, runtime,
conformance, and maintenance ceilings, record `no-go` for 9-3. Do not describe the existence
of an interpreter as proof that deoptimization is implementable.

### 9-4 · OSR — not the reverse of deoptimization

OSR needs compiled loop-header entry points that accept a validated VM-state schema; an
ordinary function delegate that enters at the function start is insufficient. Before any XL
implementation, measure the population and duration of long-running loops that would benefit
after normal function-level promotion is available.

The feasibility design must specify:

- which loop headers and nesting/handler/suspension states are eligible;
- entry-stub calling convention and mapping from VM slots/environments to IL locals;
- behavior for a guard failure immediately after OSR and prevention of tier thrashing;
- debugger/stack/source identity across the transition; and
- code/metadata/cache lifetime and per-realm budgets.

An inverse-looking mapping is not accepted as a design. Defer or cancel OSR when the measured
population or attainable ceiling does not justify its separate compiler entry paths.

### 9-5 · Re-evaluate the restart fallback

After 9-3 is validated, compare its supported guard set and cost with the existing restart
and in-method fallback contracts. Retire restart only where deopt fully subsumes it and the
fallback remains semantically sound. Keeping the cheaper limited mechanism is an allowed
terminal decision.

## Order

```text
runtime-capable Phase 6 outcome + accepted Phase 7 baseline
  ├→ 9-0 VM-first/promotion decision curve
  │    └→ 9-1 owned transition state → 9-2 descriptor-based promotion
  │                                      └→ 9-4 OSR population + separate entry-stub feasibility
  └→ 9-3 explicit deopt feasibility spike
       ├→ stop/defer if CLR-state materialization or budgets fail
       └→ validated deopt implementation → 9-5 restart decision
```

9-3 may have a correctness/optimizer-enablement case independent of 9-0's speed result, but
it still requires its own product requirement, feasibility proof, budget, and terminal
decision. It is never “free payback” from building a VM. Item 9-4 starts only after normal
function-level promotion is validated, but it does not depend on 9-3 succeeding; its loop
population, entry ABI, and fallback behavior remain a separate high-risk decision.

## Exit gate

1. Item 9-0 has a predeclared threshold curve and paired MOD-M1 result against the accepted
   current-IL lazy/eager alternatives, with time and resource evidence.
2. Every enabled tier configuration—VM-only, IL-only, VM+promotion, promotion failure,
   deopt where supported, and persisted/non-persisted variants—matches the independent
   expected-result manifest and has no unexplained IL/VM delta.
3. Promotion compiles a realm-correct descriptor, preserves function/environment identity,
   retains a valid fallback, publishes at a quiescent boundary, and reaches a memory plateau
   under its count/byte budgets.
4. Deoptimization is accepted only for explicitly enumerated safepoints with generated live
   state, forced-failure coverage, and no replayed observable effects.
5. OSR has a separate measured population and feasibility/ABI result; it may remain deferred
   even when deopt is accepted.
6. Tier-up remains opt-in until its supported semantics and fallback behavior are
   release-tested. Every experiment ends accepted, opt-in, deferred, cancelled, or removed.

## Dependencies

- Depends on a runtime-capable Phase 6 outcome (`narrow-runtime-go` or `full-go`) that names
  a dynamic-code-capable composition, the shared semantic IR/function descriptor, stable
  canonical bytecode positions, and Phase 7's accepted baseline. An `execution-only-go`
  alone does not authorize this phase.
- Depends on MOD-M1 for performance decisions and MOD-M6 for feedback/hotness ownership, concurrent
  contexts, shared artifacts, background work, and quiescent installation.
- Does not require optional Phase 8 speed items, but any peephole/quickening/persistence work
  must preserve canonical PCs and metadata compatibility.
- Pays back into current IL optimization only after 9-3's explicit state-materialization
  gate passes; until then, current restart/in-method fallbacks remain authoritative.
