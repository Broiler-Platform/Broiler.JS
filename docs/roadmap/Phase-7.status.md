# Phase 7 — Broiler.VM JavaScript profile 2.0: make the approved interpreter shippable — status

**No Phase 7 measurement exists. Nothing in Phase 7 has been built, measured, or attempted.**

This status record covers only the JavaScript built-in profile. Common Broiler.VM
catalog/composition, execution-session lifecycle, and cross-profile resource-policy evidence
belongs to `Broiler.VM/docs/roadmap.md`; no such work, and no WebAssembly-profile work, is
claimed here.

> The evidence half of [`Phase-7.md`](Phase-7.md). Historical IL-path findings cited by the
> plan remain in their Phase 0–5 status records; they are priors to test against the future
> JavaScript profile, not measurements of an interpreter that does not yet exist.

---

## State

| | |
|---|---|
| Items started | **0** |
| Items landed | **0** |
| Measurements taken | **0** |
| Blocked on | a selected Broiler.VM JavaScript composition (`execution-only`, `narrow-runtime-compiler`, or `general-runtime-compiler`), accepted Phase 6 JavaScript scope/exit evidence, accepted Broiler.VM core integration, and MOD-M1 acceptance lanes |
| Mutable-site work additionally blocked on | applicable MOD-M6 IC/feedback ownership and lifetime gates |
| Next action after entry gates | item **7-0 · decision-grade uninstrumented JavaScript-profile baseline** |

The former instruction to begin with a three-repetition, all-fifteen Octane run is
superseded. Octane remains historical continuity; MOD-M1's supported modern/product workload
manifest and paired stable-host protocol govern future acceptance.

For `execution-only`, unsupported source-compilation modes are recorded as outside the
approved JavaScript composition, not as missing results. A `narrow-runtime-compiler` or
`general-runtime-compiler` composition adds
the applicable in-process source-to-result measurement arms.

A runtime-compiler no-go selects `execution-only`; Phase 7 still measures its declared
precompiled execution path and does not change Broiler.VM core, WebAssembly, or another
built-in profile. These compositions are also distinct from `JavaScriptBootstrapProfile`;
the selected realm bootstrap profile is an orthogonal measurement input.

---

## Entry measurement — item 7-0

Before an optional Phase 7 implementation is designed, record:

1. **Approved JavaScript execution identity** — Broiler.VM language profile `JavaScript`,
   deployment/compiler composition, feature manifest, `JavaScriptBootstrapProfile`, source
   revision, package closure, and JavaScript bytecode format version.
2. **Uninstrumented end-to-end modes** — cold source compile/run, warm source compile/run,
   precompiled execution, and cache-hit execution when 8-6 exists. Missing modes are marked
   unsupported rather than silently omitted.
3. **Pipeline decomposition** — parse/shared semantics, bytecode lowering, verification,
   deserialization, installation/bootstrap, execution, and end-to-end product milestone.
4. **Representative corpus** — product startup/steady-state cases, supported JetStream 3
   shell cases, focused engine probes, and accepted conformance fixtures. Octane is a
   separately labelled continuity column.
5. **Two platform views** — same-machine CoreCLR IL/JavaScript-profile diagnostic comparison
   and actual published Native AOT Broiler.VM JavaScript results on every claimed target
   RID/device.
6. **Resource evidence beside time** — allocation, GC, peak/steady RSS or working set,
   committed/virtual memory, code/package/bytecode size, frame depth/bytes, and applicable
   p50/p95/p99.
7. **Predeclared decision** — primary metric, target or equivalence budget, guardrail
   precedence, missing-row failure, and confirmation-run rule from MOD-M1.

Detailed opcode-family, type, and bigram instrumentation belongs to 8-0. If 7-0 enables
instrumentation for a coarse opportunity count, its uninstrumented control and perturbation
must be recorded separately.

---

## Evidence conditions for the planned items

| Item | Evidence required before implementation can be accepted |
|---|---|
| 7-1 owned ICs | program-relative slot design; no persisted process ids; realm/function lifetime tests; concurrent-context and eviction plateau evidence under MOD-M6 |
| 7-2 dense elements | measured element population/hit ceiling; sparse/proxy/exotic/typed-array correctness matrix; generic fallback |
| 7-3 constants/interning | text serialized and re-interned on load; size/startup/retained-string result; no raw `KeyString`, shape, or IC id persisted |
| 7-4 numeric slots | accepted 6-2 ABI, coercion/order fixtures, avoided-allocation count and paired time result |
| 7-5 stack/register decision | traffic, dispatch/decode, frame bytes, bytecode size, implementation/verification/debug/deopt cost, and end-to-end target |
| 7-6 calls/closures | approved call-surface manifest; function identity/realm/environment tests; call/frame attribution and product result |

No existing process-global cache or feedback table is recorded here as reusable “unchanged.”
The runtime semantics may be reused; mutable site state must first gain explicit ownership,
snapshot/invalidation behavior, and reclamation.

---

## Historical evidence retained as priors

The older plan cited Phase 2–4 findings about property-cache semantics, root boxing, and IL
call-envelope cost. Those measurements remain valid for the revisions, corpora, and IL path
recorded in their owning status files. They justify fixtures and questions, not a
JavaScript-profile VM design:

- property and store caches need own/prototype/creation/read-modify-write coverage;
- allocation share alone does not predict elapsed-time benefit;
- avoiding boxes depends on where values are materialized and consumed; and
- the current IL call envelope does not establish the cost of a VM frame.

When 7-0 runs, this section should link the immutable MOD-M1 evidence bundle and record which of
those priors transferred, failed to transfer, or remained below resolution.
