# Phase 8 — Broiler.VM JavaScript profile 3.0: measured optimization and persistence — status

**No Phase 8 measurement exists. Nothing in Phase 8 has been built, measured, or attempted.**

This status record covers JavaScript-profile optimization and persistence only. Common
Broiler.VM catalog/composition, execution-session lifecycle, and cross-profile resource
evidence belongs to `Broiler.VM/docs/roadmap.md`; no common-core or WebAssembly-profile work
is claimed here.

> The evidence half of [`Phase-8.md`](Phase-8.md). Historical figures below are inherited
> from the IL-path campaign and remain attributed to their Phase 0–5 status records. They are
> hypotheses/guardrails for a future JavaScript interpreter, not JavaScript-profile results.

---

## State

| | |
|---|---|
| Items started | **0** |
| Items landed | **0** |
| Measurements taken | **0** |
| 8-1 through 8-5 blocked on | accepted Phase 7 uninstrumented baseline and **8-0** instrumentation/cost evidence |
| 8-6 blocked on | accepted Phase 6 persisted format/verifier plus MOD-M1 cold-startup opportunity; **not** blocked on 8-0 |
| Mutable-state work additionally blocked on | applicable MOD-M6 ownership, invalidation, eviction, and memory-plateau gates |

This corrects the previous contradiction that called 8-0 the gate for every item while also
saying the bytecode cache should proceed independently.

All rows additionally require the accepted Broiler.VM JavaScript catalog/session integration.
A runtime-compiler no-go selects `execution-only`; Phase 8 remains available for that
composition and has no effect on WebAssembly or another built-in profile.

---

## Entry evidence — item 8-0

8-0 does not establish the JavaScript-profile baseline; 7-0 does. It adds diagnostic runs
that report:

1. executed opcode counts and bytes fetched by family and workload;
2. operand-type and slow/fast-path distributions at candidate sites;
3. IC, element, call, completion, allocation, stack/local traffic, and frame high-water
   counts;
4. opcode bigram/trigram histograms for this compiler's emitted stream;
5. generated dispatch/decode form on every claimed CoreCLR and Native AOT RID; and
6. a calibrated absolute rate and attainable end-to-end ceiling for each candidate.

Counters are evidence of population, not direct nanosecond attribution. Every diagnostic
bundle must contain an uninstrumented same-revision control, report diagnostic overhead and
distribution perturbation, and explain how any per-operation cost was calibrated.

The workload manifest is MOD-M1's accepted product/JetStream/focused-probe set. Octane may be
retained as a separately labelled historical arm; a fixed all-fifteen requirement no longer
defines acceptance.

---

## Independent entry evidence — item 8-6

Before persisted bytecode is designed, record:

1. cold and warm JavaScript parse/shared-semantics/lowering/verification cost on product workloads;
2. the predeclared startup or package metric, threshold, and resource guardrails;
3. the accepted JavaScript 6-3 schema/verifier version, Broiler.VM language-profile identity
   `JavaScript`, and deployment/compiler composition; separately, the orthogonal
   `JavaScriptBootstrapProfile` identity;
4. `execution-only` defined-load-failure policy versus
   `narrow-runtime-compiler`/`general-runtime-compiler`
   runtime-compiler fallback policy; and
5. a compatibility/threat/lifecycle ADR covering cache identity, invalidation, malformed
   input, bounded resources, atomic writes, concurrent access, eviction, and reclamation.

For `narrow-runtime-compiler` and `general-runtime-compiler`, the future JavaScript evidence bundle distinguishes cold source
compile, warm uncached compile, verified cache hit, corrupt/incompatible fallback, and
cache-disabled arms. For `execution-only`, it instead distinguishes verified artifact
load/hit, fresh verification with persistence disabled, corrupt/incompatible defined load
failure, and separately labelled offline JavaScript precompilation cost where relevant.
Compiler-throughput results remain cache-disabled unless cache behavior is the named
subject.

---

## Required evidence per item

| Item | Required result |
|---|---|
| 8-1 feedback | owner-relative site ids, snapshot/publication/invalidation rules, weak/stable callee identity, eviction plateau, and measured funded consumer |
| 8-2 quickening | type population, canonical-PC mapping, generic fallback, per-owner mutation/reset, conformance, and end-to-end result |
| 8-3 superinstructions | measured n-gram, dispatch ceiling, code/bytecode-size and verifier/debugger cost, capped accepted set |
| 8-4 dispatch/encoding | generated-code evidence per RID, calibrated dispatch/decode cost, feature/code-size controls, end-to-end decision |
| 8-5 Native AOT PGO | separate training/evaluation workloads, no-PGO/PGO published images, startup/steady/image/memory/conformance result |
| 8-6 persistence | schema/key manifest, text re-interning, no process ids/state persisted, complete verification/fuzz/corruption/fallback/atomic-write/eviction evidence |

---

## Historical evidence retained as priors

The earlier plan recorded these IL-path observations:

- removing a large share of number-box allocation produced a much smaller time change;
- measured property-read/call sites were often monomorphic, but the widened corpus reduced
  the original percentages;
- the current compiler's arithmetic specialization reached a small static population and a
  failing guard was expensive; and
- a sampling profiler materially perturbed the old driver and did not symbolicate emitted JS.

Those results remain in the owning Phase 3/4 status records with their exact numbers,
revisions, and corpus limitations. They justify the rate/ceiling discipline and a preference
for JavaScript-interpreter-internal counts. They do **not** prove that dispatch, feedback, quickening,
superinstructions, or PGO will matter in the future JavaScript interpreter.

When 8-0 or 8-6 first runs, append the immutable MOD-M1 evidence link here and preserve the
unmodified historical section for comparison.
