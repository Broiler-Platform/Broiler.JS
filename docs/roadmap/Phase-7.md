# Phase 7 — Broiler.VM JavaScript profile 2.0: make the approved interpreter shippable

Establish whether the correct Phase 6 JavaScript interpreter meets its approved capability
and deployment-composition performance and resource thresholds, then improve only the
measured costs. Reuse the JavaScript runtime's semantic operations and storage
representations, while giving bytecode-local caches and
feedback explicit realm/function/program ownership.

This phase owns JavaScript-profile execution evidence and JavaScript-specific optimization
state. The common Broiler.VM catalog, profile-selection, execution-session lifecycle, and
cross-profile resource contracts remain owned by `Broiler.VM/docs/roadmap.md`; satisfying
them is an entry/integration dependency, not work claimed complete here.

The acceptance lane follows the selected Broiler.VM JavaScript composition. Under
`execution-only`, Phase 7 measures the published precompiled-JavaScript execution
composition and never treats absence of a runtime source compiler as a defect. Under
`narrow-runtime-compiler` or `general-runtime-compiler`, it additionally measures the
approved source-to-result runtime-compiler path. All three compositions still require
the same correctness, resource, and independent-oracle discipline for their declared scope.

These deployment/compiler compositions are not `JavaScriptBootstrapProfile` values. The latter
select JavaScript realm built-ins/realization policy and are orthogonal inputs that must be
recorded in each JavaScript-profile measurement.

> The plan half of [`Phase-7.status.md`](Phase-7.status.md). No Phase 7 baseline exists and
> no item is scheduled.
> Part of the [performance and benchmark roadmap](Roadmap.md); the four JavaScript-profile
> VM phases are
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69).

---

## What can be reused — and what cannot

Shapes, property storage, dense elements, string representations, and the generic semantic
operations live below the IL emitter and are candidates for reuse. Reusing their behavior is
important: a bytecode arm that invents a second object model will become a second JavaScript.

Existing emitted-site state is different. Current inline-cache and type-feedback tables use
process-wide integer site indexes and mutable static storage. Phase 7 must not embed those
process-global identities in bytecode or serialized artifacts, and it must not share mutable
entries across realms merely because the generic property operation is shared.

The rule is:

> Reuse semantic operations and stable runtime representations. Own mutable optimization
> state with the compiled bytecode program/function or realm, and reclaim it with that owner.

| Runtime facility | VM integration rule |
|---|---|
| object shapes and property maps | reuse immutable/stable shape semantics; never serialize a live shape id |
| property read/store fast paths | allocate **program-relative** cache slots backed by function/program/realm-owned side tables |
| dense element storage | call the same semantic slow paths, then add a measured direct dense path |
| interned property names | serialize text; re-intern on load; never persist a process-local `KeyString.Key` |
| strings, numbers, objects, symbols | use the accepted 6-2 `ValueSlot` ABI and runtime conversions |
| type feedback | wait for MOD-M6 ownership/snapshot/invalidation/lifetime rules; do not consume the current static table unchanged |

## Items

| # | Item | Size | State |
|---|---|---|---|
| **7-0** | **Take the decision-grade JavaScript-profile baseline and set product thresholds** | M | ❌ **not started; blocks optional optimization** |
| **7-1** | Realm/function-owned property read and store ICs | L | ❌ |
| **7-2** | Element opcodes over dense storage | M | ❌ |
| **7-3** | Constant pool with load-time property-name interning | S–M | ❌ |
| **7-4** | Numeric `ValueSlot` arithmetic that avoids intermediate boxes | L | ❌ |
| **7-5** | Stack or register VM — decide from measured traffic and total cost | L–XL | ❌ |
| **7-6** | Call, construct, and closure fast paths | M–L | ❌ |

### 7-0 · Decision-grade baseline — **the entry measurement**

Use MOD-M1's stable-host, same-machine controls and predeclared decision rule. A fixed
`--repetitions 3` Octane run is not an acceptance protocol.

Run the supported workload manifest produced by MOD-M1:

- representative product first-context, first-script, first-paint, and steady-state cases;
- the compatible JetStream 3 shell subset and focused engine probes;
- the accepted conformance/capability surface; and
- Octane only as historical continuity, never as the sole priority signal.

Report cold and warm modes separately, and separate the pipeline:

1. parsing and shared semantic analysis;
2. bytecode lowering and verification;
3. deserialization/verification when a cache arm exists;
4. installation/bootstrap;
5. execution; and
6. end-to-end source-to-result or launch-to-product milestone.

For each mode report wall time/throughput with allocation, GC, peak and steady working set,
committed/virtual memory, code/package/bytecode size, maximum frame depth, and p50/p95/p99
where the workload has multiple operations.

Use two kinds of platform evidence:

- **diagnostic comparison:** IL and JavaScript bytecode on the same CoreCLR machine, same
  source, same JavaScript capability manifest, same `JavaScriptBootstrapProfile`, and same
  time window; and
- **product acceptance:** the published Native AOT Broiler.VM JavaScript composition on every
  claimed target RID/device,
  against its absolute product SLO and its previous accepted VM baseline once one exists.

The IL ratio is context, not the product gate on a platform where IL cannot run. Before any
candidate change, write down the primary metric, minimum relevant effect/equivalence budget,
guardrail precedence, and target threshold. An interpreter that misses the approved product
ceiling after the attainable opportunity is priced can be rejected rather than optimized
indefinitely.

7-0 may collect coarse counts needed to decide whether an item is even plausible. Detailed
opcode/type/bigram instrumentation belongs to 8-0 and must not contaminate this uninstrumented
baseline.

### 7-1 · Property inline caches with explicit lifetime

Give constant-key property opcodes a program-relative cache-slot operand. The running
`BytecodeProgram` or function resolves that operand against a side table owned by the
program/function or realm. The table starts cold after deserialization and is reclaimed with
the code/cache entry.

Reuse the runtime's proven property semantics and regression fixtures: own and prototype
reads, property creation and overwrite stores, constructor statics, read-modify-write,
accessors, proxies/exotics, private names, nullish failures, and prototype mutation. Do not
copy a process-global emitted-site index or a warmed IC entry into persisted bytecode.

This item depends on MOD-M6's ownership and independent-context gates whenever shared code,
multiple contexts, background work, or Workers are in scope. Add tests that load separately
compiled programs whose first local cache slot is the same number, run them in separate
realms concurrently, evict them, and verify no answer or retained state crosses owners.

### 7-2 · Element opcodes over dense storage

Use dedicated element opcodes only where 7-0 shows a material population. They must preserve
generic semantics for sparse arrays, holes, inherited indexes, proxies/exotics, canonical
numeric keys, typed arrays, detach/grow behavior, and shared-memory restrictions.

The direct dense path is a fast arm with the generic runtime operation as the semantic
fallback. Measure its hit rate and absolute operation rate, not merely the fraction of one
suite.

### 7-3 · Constant pool and property-name interning

The Phase 6 format reserves a constant-pool representation, but persistence stores the
property-name text, not the current process's integer key. Loading verifies the string entry,
interns it in the current process/realm policy, and stores the resulting runtime key in the
loaded program's non-serialized resolved-constant table.

Measure parse/lowering/deserialization time, bytecode bytes, retained strings, and property
operation cost before accepting compact encodings. The bytecode-cache compatibility contract
is Phase 8 item 8-6.

### 7-4 · Numeric arithmetic through the accepted `ValueSlot` ABI

Implement arithmetic fast paths against Phase 6's accepted value-slot representation. Test
operand types at the semantic coercion point, evaluate every operand once in source order,
compute raw numeric intermediates where the ABI permits, and materialize the JavaScript
Number at the observable root or storage boundary.

This is not “free because it is an interpreter.” If the accepted frame representation holds
only boxed `JSValue` references, the claimed intermediate-box saving does not exist and the
item must be redesigned or cancelled. Carry the current IL path's order/coercion fixtures
across and measure absolute avoided allocations plus time.

### 7-5 · Stack machine or register machine

The stack seed does not settle the general VM architecture. Decide only after the accepted
ABI and 7-0/8-0 evidence report:

- executed push/pop/duplicate/load/store traffic and bytes fetched;
- dispatch/decode cost and generated JIT/AOT code;
- frame bytes and GC/reference clearing cost;
- bytecode size and verification complexity; and
- compiler, debugger, exception, suspension, deopt, and maintenance cost.

A register rewrite is accepted only if the predeclared end-to-end target and resource
guardrails pass. Operand traffic having a nonzero count is not sufficient.

### 7-6 · Call, construct, and closure fast paths

Build on the Phase 6 call/environment ABI and the runtime's semantic call paths. Cover
ordinary, strict, arrow, bound, constructor, spread, optional, direct-eval, generator/async,
and host calls that belong to the approved capability manifest. Preserve function identity, realm,
`this`, `new.target`, home object/private environment, arguments behavior, and stack traces.

Report the VM frame cost independently from bootstrap, body execution, and host-call cost.
Do not assume the current IL shadow-stack frame can be reused unchanged as a VM operand/local
frame; it remains useful for engine stack identity and diagnostics.

## Order

```text
Phase 6 accepted JavaScript scope + Broiler.VM core integration + MOD-M1 acceptance lanes
  └→ 7-0 uninstrumented baseline and predeclared thresholds
       ├→ 7-3 constant-pool resolution, if startup/size evidence supports it
       ├→ 7-4 numeric ValueSlot path, if the ABI and allocation rate support it
       ├→ 7-1 owned IC slots → 7-2 dense elements → 7-6 calls
       └→ 7-5 register-machine decision, only after traffic and total-cost evidence
```

MOD-M6 can redesign IC/feedback ownership in parallel with the base 7-0 measurement. Items that
consume mutable site state do not start until its applicable gate is accepted.

## Exit gate

1. The independent expected-result manifest and the IL/JavaScript-profile differential check
   remain green
   for the approved capability surface; serialized reload and Native AOT arms are included
   where applicable.
2. The Broiler.VM JavaScript profile meets the product threshold predeclared by 7-0 on every
   claimed Native AOT
   RID/device, or the phase records a terminal rejection/narrowing decision.
3. Every accepted item has paired MOD-M1 evidence on representative workloads, plus allocation,
   GC, memory, code/package/bytecode-size, and tail-latency guardrails.
4. Mutable IC/feedback state has explicit semantic ownership and lifetime. Repeated
   load/run/evict and context create/dispose tests reach the declared memory plateau.
5. Modern workloads drive acceptance. Octane results, if retained, are labelled historical
   continuity and cannot override the product/JetStream decision.

## Dependencies

- Depends on an accepted Phase 6 JavaScript deployment/compiler composition (`execution-only`,
  `narrow-runtime-compiler`, or `general-runtime-compiler`) and exit evidence, plus MOD-M1's decision-grade
  measurement lanes and the accepted Broiler.VM catalog/session integration contract.
  Runtime-compiler measurements apply only to the latter two compositions. A runtime-compiler
  no-go selects `execution-only`; Phase 7 still measures that composition without changing
  the state of Broiler.VM, WebAssembly, or other built-in profiles.
- 7-1 and any feedback-consuming portion depend on MOD-M6 before concurrent contexts, shared
  compiled artifacts, background tiering, or Workers are advertised.
- Reuses Runtime/Storage semantics, but changes needed for bytecode ownership stay green on
  the IL arm and are recorded by their owning architecture/concurrency evidence.
- Phase 8 depends on this phase's uninstrumented baseline; it may not replace 7-0 with an
  instrumented histogram run.
