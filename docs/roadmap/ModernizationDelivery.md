# Broiler.JS modernization delivery roadmap

A phased delivery plan for turning the performance, assembly, concurrency, and JavaScript
execution-profile findings into reviewable increments.

> **Authority rule.** [`Modernization.md`](Modernization.md) owns cross-track dependencies,
> decisions, and program gates. The owning plan/status pair owns scope and evidence. This
> document is the requested **delivery view**: it groups that work into executable waves but
> creates no second state ledger. If wording conflicts, follow `Modernization.md`,
> [`Measurement.md`](Measurement.md), and the owning plan/status pair.

The `DEL-*` labels below are work-breakdown labels, not replacements for `MOD-M*`, phase,
assembly, or validation IDs. Every implementation change cites its authoritative item and
updates the owning status evidence in the same change.

---

## 1. Outcomes and delivery rules

The program is complete only when Broiler.JS has:

1. one mechanically checked account of what is proposed, implemented, validated, accepted,
   deferred, or cancelled;
2. a decision-grade performance baseline with immutable source identity, exact comparison
   rows, A/A validity, resource metrics, and semantic guardrails;
3. an acyclic, build-proven assembly graph with a backend-neutral semantic front end and one
   enforceable IL/dynamic-code boundary;
4. an explicit full-IL composition and one selected Broiler.VM JavaScript composition:
   `execution-only`, `narrow-runtime-compiler`, or `general-runtime-compiler`;
5. bounded parallel work over independent contexts without concurrent execution of one
   `JSContext`;
6. Worker agents with explicit lifecycle, scheduling, resource, and unsupported-surface
   contracts;
7. profile-led improvements to the current IL engine before optional JavaScript-profile
   persistence or adaptivity is funded; and
8. a terminal JavaScript built-in composition decision followed only by the JavaScript work
   that decision funds. Generic Broiler.VM core and its WebAssembly built-in are separately
   owned and cannot be cancelled by this roadmap.

Apply these rules in every delivery wave:

- Correctness, architecture, capability, performance, and historical evidence are different
  evidence classes. A check mark in one class does not close another.
- Performance/resource results follow [`Measurement.md`](Measurement.md); hosted three-sample
  spread is smoke, not an acceptance threshold.
- Build vertical slices and keep a sequential/previous-backend fallback until the owning gate
  accepts removal.
- One context has one executing entrant. Parallelism comes from independent contexts,
  realm-neutral preparation, or explicit Worker agents.
- A new assembly must enforce a dependency, deployment, AOT, ownership, or test boundary. Size
  alone is not a reason to split.
- Production expansion of the JavaScript bytecode profile beyond the existing seed does not
  start before the terminal MOD-M9 composition decision. Generic Broiler.VM core and the
  WebAssembly built-in follow `Broiler.VM/docs/roadmap.md`; they are not gated here.

## 2. Delivery map

```text
DEL-0 truth/governance ─┬─> DEL-1 baseline
                        ├─> DEL-2 target graph
                        └─> DEL-8 discovery and correctness fixtures

DEL-1 + DEL-2 ─┬─> DEL-3 IL/AOT isolation ─> DEL-4 package/AOT evidence
               ├─> DEL-5 compile-ahead census and decision
               └─> DEL-8 acceptance (DEL-2 only for boundary-changing slices)

DEL-5 artifact/shared-state census ─> DEL-6 context safety ─> DEL-7 Workers

DEL-3 + finite DEL-4 evidence + DEL-5 decision
      + DEL-8 items 1 (front-end/startup) and 8 (backend comparison) ─> DEL-9 JavaScript composition decision
DEL-6 ── shared/adaptive JavaScript-state work only ───────────> DEL-9/10/11

Broiler.VM foundation + DEL-9 selected JavaScript composition
      + any composition-required DEL-4 boundary ─> DEL-10 JavaScript-profile correctness/baseline
                                                 └─> JavaScript-applicable DEL-11 branches after their own gates
all accepted product slices ─> DEL-12 release and recertification
```

This map mirrors the authoritative dependencies in `Modernization.md`; it is not a looser
alternative. DEL-1 and DEL-2 are the first parallel lanes. DEL-5 may reproduce the existing
host slice and complete its census while DEL-2 runs, but its terminal decision needs the
applicable target-graph evidence. DEL-6 starts from DEL-5's accepted artifact/shared-state
census and does not wait for compile-ahead to be a performance success. DEL-8 discovery may
start after DEL-0; acceptance needs DEL-1, boundary-changing slices need DEL-2, and shared-state
slices need DEL-6. Because DEL-8 is continuous, DEL-9 waits only for the named finite inputs in
the map, not for every DEL-8 package. DEL-9 selects the JavaScript built-in's composition;
DEL-10 is scoped to that selection and DEL-11 remains a set of independently optional
JavaScript-profile branches. None of those outcomes cancels or narrows generic Broiler.VM core
or the WebAssembly built-in owned by `Broiler.VM/docs/roadmap.md`.

---

## 3. Delivery phases

### DEL-0 — Reconcile truth and install drift checks

**Maps to:** MOD-M0 and the initial MOD-M10 checks.

**Objective:** make every later task start from the real graph and current evidence rather
than from historical delivery prose.

### Work

1. Create the single machine-readable modernization item source required by MOD-M0-1 and
   generate its human-readable index.
2. Replace fixed-count ownership validation with referential-integrity validation. Use
   [`Phase-0.status.md`](Phase-0.status.md) for the current failing assertion/count; unknown,
   duplicate-semantic, unowned, or missing-manifest entries must fail directly.
3. Reproduce the suspected `TypedArray.prototype.set` overlap/offset wrong-answer case, add the
   focused regression first, and fix correctness if reproduced. Record a terminal correctness
   result without waiting for performance baselining.
4. Generate the current project graph, public surface, package identities, and profile
   closures. Date-label preserved historical graphs and measurements.
5. Finish the ExpressionCompiler split's S-7 manifest-by-manifest validation and consumer/API
   checks without treating the landed structural change as unstarted.
6. Keep aggregate compile-ahead and Worker code classified as implemented subsets in
   [`Concurrency.status.md`](Concurrency.status.md); do not promote them to accepted
   MOD-M5–MOD-M7.
7. Add repository-wide Markdown target/case/anchor checks, duplicate item-ID checks, and stale
   plan/status-state checks.

### Exit gate

- One index answers state, owner, blocker, next action, and evidence for every modernization
  item.
- Generated graph/API/package facts agree with architecture and public-API documentation.
- S-7 has exact manifest-by-manifest equivalence with its pre-split baseline; public API,
  package contents, and representative pristine source and binary consumers have reproducible
  baselines. An unexplained conformance or consumer delta blocks exit.
- The ownership configuration test validates relationships rather than a historical row count.
- The `TypedArray.prototype.set` regression has a terminal reproduced/fixed or not-reproduced
  result; any optional bulk-copy optimization remains in DEL-8.
- Every aggregate compile-ahead/Worker subset is classified as implemented, validated,
  accepted, or open without using “built” as a synonym for accepted.
- Documentation integrity checks pass on a case-sensitive filesystem.

### DEL-1 — Establish the decision-grade baseline

**Maps to:** MOD-M1 and Phase 0 items 0-7/0-8/0-11.

**Objective:** make regression, equivalence, and resource decisions reproducible.

### Work

1. Provision the controlled win-x64, linux-x64, and linux-arm64 lanes required by MOD-M1;
   scope an interim result only to the arms actually run, without silently shrinking the
   declared support obligation.
2. Before either arm runs, attest candidate/control source and archive hashes, recursive
   submodule SHAs, clean-tree proof or a checksummed patch plus untracked-input manifest,
   immutable corpus/harness revisions, resolved dependency graph, generated-source hashes,
   SDK/runtime, build-output hash, configuration, TFM, RID, publish/bootstrap/backend identity,
   and every setting deliberately under test.
3. Record lane identity and effective state: CPU model, microcode, physical/logical topology,
   memory, OS, power/governor and thermal policy, affinity, CPU features, tiering/PGO/R2R,
   GC mode/heap count, and runner identity. Requested and effective values must agree.
4. Expand the matrix into executed jobs for relevant x64 CPU features enabled and disabled,
   an AdvSimd-capable supported Arm64 host, and applicable workstation/server GC arms. Fail a
   requested arm whose measured child cannot attest the requested effective state.
5. Check in the MOD-M1-6 modern-workload applicability/exclusion manifest, including the
   compatible JetStream subset, script-heavy product lifecycle fixtures, independent-context
   scaling, and focused probes. Keep Octane as historical continuity rather than the priority
   source.
6. Calibrate A/A envelopes per lane × workload × metric and regression-test the exact-row,
   all-repetition comparator with deliberately seeded timing and allocation failures. A failed
   envelope invalidates a run; it does not widen the candidate threshold.
7. Collect process-isolated candidate/control arms with exact compatible manifests, all
   repetitions, balanced order, raw logs, timeout/budget records, allocation, GC, working
   set/RSS, committed/virtual memory, thread/queue counts, code/package/publish size, and
   applicable p50/p95/p99. Keep cold versus warm and cached versus uncached populations
   separate. Primary timing arms are uninstrumented.
8. Run the same focused semantic fixtures and owning pinned test262 rows on candidate and
   control. Keep profiled/EventPipe runs as separate matched diagnostic arms, quantify their
   observer effect, and never substitute them for the primary result.
9. Version the assembly/package baseline: direct/transitive edges, file and IL/metadata bytes,
   public types, package contents, publish bytes, loaded assemblies, cold-context time, and
   working set.
10. Store schema-versioned, checksummed summaries and raw benchmark, diagnostic, build,
   publish, comparison, and conformance artifacts in durable retention for the supported
   release and at least the two previous accepted baselines.
11. Classify the initial comparison provisionally. Repeat a provisionally qualifying result in
   a fresh confirmation run before acceptance, then emit exactly `accept`, `reject`,
   `equivalent`, `below-resolution`, or `invalid-run` under the predeclared decision rule.

### Exit gate

- A clean same-build null control passes on every claimed lane.
- The exact-row comparator requires compatible-manifest equality and fails closed on missing,
  extra, duplicate, timed-out, invalid, or otherwise incomparable rows.
- Seeded timing/allocation regressions fail, and both arms' exact semantic and conformance rows
  are attached to the immutable comparison.
- A complete evidence bundle can reproduce one decision without relying on hosted-run spread,
  Chromium, or Jint as a normalizer.
- Resource and conformance guardrails are predeclared and green; instrumented arms have passed
  fidelity/observer-effect checks but are not the primary timing evidence.
- The durable evidence index can regenerate a published summary and names its retention policy.

### DEL-2 — Prove the target assembly and semantic-front-end graph

**Maps to:** MOD-M2, A-0/A-1/A-3, and the AssemblySplit validation remainder.

**Objective:** replace the superseded assembly sketch with an acyclic build before moving more
production files.

### Work

1. Generate direct/transitive `ProjectReference` graphs with public types, package identity,
   target profile, and forbidden edges.
2. Build empty target project shells and a minimal fake backend. Move no production code until
   the shells restore and compile.
3. Resolve `Storage → Ast → Expressions` before considering a Storage/Expressions fold.
4. Inventory `Parser → Runtime`; remove/invert it where sound or preserve Parser, Runtime, and
   Engine as separate boundaries.
5. Specify a backend-neutral FrontEnd/Semantics boundary for binding, scopes, early errors,
   hoisting, free-name/numeric analysis, shared IR, and lowering.
6. Prove the LinqExpressions and backend-contract cuts without exposing `DynamicMethod`, Emit,
   or realm-captured assumptions.
7. Record assembly/package/namespace/type-forwarding decisions and add acyclicity/tier tests.

### Preferred boundary order

1. validate the landed Expressions/emitter split;
2. prove Primitives/Expressions plus FrontEnd/Semantics shells;
3. prove IL and minimal test-backend adapters;
4. preserve Ast, Parser, Storage, Runtime, and Engine until an edge-removal or measured product
   result justifies a fold; and
5. evaluate Hosting/CLI and optional BuiltIns satellites only after the graph is stable.

### Exit gate

- Project shells and the generated target graph are acyclic and identical.
- IL and the fake backend consume one neutral JavaScript semantic contract; a JavaScript
  Broiler.VM profile adapter need not reference IL.
- Every proposed merge/split has a compatibility and product-value disposition.
- Architecture tests enforce all allowed/forbidden edges.

### DEL-3 — Isolate IL/dynamic code and prove AOT compositions

**Maps to:** MOD-M3 and A-4/A-6/A-7/A-8.

**Objective:** turn “AOT-safe” from a prose label into an analyzer plus publish-and-run result.

### Work

1. Generate and classify every Emit, dynamic-code, trim-warning, reflection, assembly-load,
   string-based type/backend discovery, and generated-registration site.
2. Replace magic-name discovery with explicit or generated registration.
3. Move IL lowering/emission, `AssemblyCodeCache`, Linq IL adaptation, and ILPack behind the one
   approved IL composition boundary.
4. Make portable analyzer and closure checks fail on IL, Roslyn, NuGet scripting, ILPack,
   unapproved reflection, or undocumented loading.
5. Publish and execute the current numeric portable sample for the predeclared RID matrix with
   warnings as errors. Label it execution-only evidence, not full JavaScript support.
### Predecision exit gate

- The inventory has no unowned site and the portable closure has no forbidden edge.
- The execution-only sample publishes and runs on every selected RID with zero unexplained
  warnings.
- The current full-IL/runtime-compiler-capable composition and the numeric execution-only proof
  are named distinctly in code, packages, docs, and CI; neither is presented as an approved
  post-MOD-M9 JavaScript composition.

### DEL-4 — Split packages only where the boundary pays

**Maps to:** MOD-M4 and the remaining assembly-plan items.

**Objective:** improve ownership and optional deployment without turning assembly count into a
metric.

### Candidate sequence

1. Split backend-neutral hosting/context/bootstrap contracts from CLI, Roslyn, NuGet, CSX, and
   default-backend composition.
2. Validate the LinqExpressions neutral/IL package boundary created by DEL-3.
3. Spike Temporal, Intl, and RegExp separately; inventory resources, generators, registries,
   public types, and bootstrap behavior before approving any move.
4. Evaluate CLR interop as an optional reflective satellite if it materially simplifies the
   portable closure.
5. Keep Runtime and Engine separately baselinable; fold them only for a measured product result
   that outweighs lost ownership clarity.

### Per-split gate

- pristine source and previously compiled consumers;
- API diff, package contents/graph, identity/type-forwarding decision;
- focused tests, affected test262 manifests, full/reduced bootstrap-global snapshots;
- cold startup, first context/use, package/publish bytes, working set, loaded metadata, and
  repeated create/dispose plateau; and
- named owner plus an architecture rule the new boundary enforces.

Cancel a split whose deployment, AOT, test, ownership, or baselining value is not measurable.

### DEL-5 — Decide bounded compile-ahead

**Maps to:** MOD-M5 and [`Concurrency.md`](Concurrency.md).

**Objective:** accept, defer, or cancel background preparation without allowing concurrent
execution of one context.

### Work

1. Treat the existing aggregate `ScriptCompileAhead` result as a prototype: useful evidence,
   not acceptance.
2. Publish the first supported input contract: immutable external classic scripts whose final
   bytes, URL, mode, backend, and options are known. Name every excluded source/mode/backend
   shape and preserve source/document installation and execution order on the realm owner.
3. Measure representative product critical paths with cold and warm
   background-off/1/2/4/auto arms and one host-wide scheduler shared by all subsystems.
4. Audit `JSContext.CodeCache` lifetime/keying plus all compiler-touched global/static state,
   registries, feedback switches, site allocation, and generated-delegate captures.
5. Share only proven realm-neutral artifacts; finalize realm/context installation at a quiescent
   boundary.
6. Define same-key single-flight, cancellation, invalidation, eviction, retry, and exact
   syntax-error/reporting order while unrelated keys continue.
7. Measure queue/compile/install/overlap time, active/parked workers, stack mappings, duplicates,
   cancellations, cache hits/evictions, p50/p95/p99, allocation, GC, RSS, and plateau.

### Exit gate

- Per-context peak execution remains one and background work is bounded globally.
- Serialized/background results, applicable test262 rows, and syntax-error
  type/message/location/reporting order are exact across deterministic stress.
- Same-key requests perform one bounded compilation per live entry generation, unrelated keys
  progress, and failed/cancelled/invalidated/evicted generations retry only under the published
  policy.
- The cold/warm paired interval resolves and clears the predeclared user-visible decision
  boundary while committed/virtual memory, RSS, stack, GC, and p95/p99 remain inside the host
  ceilings, or the feature is deferred/cancelled with its measurements preserved.
- The supported contract and exclusions are published; the feature remains opt-in until the
  complete gate passes and always retains the exact synchronous/background-off path.

### DEL-6 — Make independent contexts and optimizer state safe

**Maps to:** MOD-M6.

**Objective:** scale independent JavaScript work without cross-realm state corruption or leaks.

### Work

1. Inventory every context entry path, async/host continuation, cache, IC, type-feedback table,
   mutable opcode/quickening state, compiled artifact, and reclamation route.
2. Give site IDs, ICs, feedback, adaptive state, and mutable code explicit function/realm/context
   ownership. Keep immutable canonical artifacts separate from mutable sidecars.
3. Define publication, snapshot, invalidation, eviction, weak/stable identity, teardown, and
   quiescent migration rules.
4. Run randomized serial-versus-parallel equivalence, shared/unshared-cache stress, repeated
   create/dispose, cancellation, failure, and long memory-plateau tests.
5. Report scaling, contention, tail latency, process allocation, RSS/virtual memory, GC, and
   retained state under 1/2/4/auto contexts.

### Exit gate

- Every entry route obeys one owner/exclusion policy.
- No mutable optimizer state crosses realms without semantic and lifetime proof.
- Independent contexts reproduce serialized results and reach a stable teardown plateau.

### DEL-7 — Finish Workers as isolated agents

**Maps to:** MOD-M7A; MOD-M7B remains a separate optional decision.

**Objective:** turn the aggregate first Worker slice into a product-supported agent model.

### Work

1. Specify one context, realm, job queue/event loop, and owner thread per Worker agent.
2. Pin creation, startup, FIFO messaging, structured-clone/transfer failure atomicity, errors,
   termination races, timers, imports, and teardown.
3. Apply one host-wide worker/compile/render budget with overload/backpressure behavior.
4. Run applicable Worker WPT/test262 scope, randomized lifecycle stress, throughput/tail tests,
   and repeated create/terminate memory plateaus.
5. Publish the unsupported surface: module/shared/nested workers, `MessagePort` transfer,
   worker animation frames, network-script policy, and cross-agent shared memory as applicable.

Cross-agent `SharedArrayBuffer`/Atomics is **MOD-M7B**, entered only with a separate ECMAScript
memory-model ADR, shared backing-store design, no-tear/order/waiter litmus tests, x64/Arm64
stress, and security/resource review. Do not infer it from ordinary Worker completion.

### Exit gate

- The declared lifecycle, per-port/task-source FIFO, permitted interleavings, error,
  cancellation, drop/drain, close, and termination policies hold under deterministic stress.
- Structured clone covers the supported cyclic graph; successful transfers detach exactly
  once, while failed transfers expose no partial detachment.
- Applicable Worker/structured-clone WPT and repository tests pass at multiple worker counts.
- The shared host cap is respected and repeated create/terminate reaches a stable memory
  plateau within the startup, tail, stack, RSS, and committed/virtual-memory budgets.
- Cross-agent shared memory remains unavailable until the separate MOD-M7B gate passes.

### DEL-8 — Optimize the current IL engine from measured populations

**Maps to:** MOD-M8 and the open Phase 1–5 candidates.

**Objective:** fund the least complex current-engine work that clears a product threshold before
building another execution engine.

### Ordered candidates

1. Finish MOD-M8-1's deferred parse/tree-capture mechanism only against the pinned MOD-M1-6
   modern shell and product startup populations; retain eager semantic checks and exact errors,
   and account for retained source/capture cost plus the sequential handoff tax.
2. Resolve MOD-M8-2's fixed call-envelope soundness question before widening speculative
   eligibility; do not treat a historical hit rate as a correctness proof.
3. Decompose MOD-M8-3's fixed RegExp allocation envelope into named `ExecMatch` and
   `BuildExecResult` regions before selecting a reduction.
4. Continue MOD-M8-4 storage/value work only where current modern/product execution-weighted
   populations and the live-memory case show an attainable consumer; do not extrapolate from
   box shares or historical seven-suite subsets.
5. After DEL-0's terminal correctness result, measure MOD-M8-5's optional overlap-safe
   `TypedArray.prototype.set` bulk-copy path with exact element/buffer restrictions.
6. Run MOD-M8-6's bounded polymorphic-cache experiment with coverage, misses, contention, and
   retention; consider a megamorphic cache only if the current profile has a material population.
7. Evaluate MOD-M8-7 intrinsics/SIMD only with feature-on/off x64 and supported Arm64 arms,
   scalar fallback, code-size/startup guardrails, and an executing workload population.
8. Run MOD-M8-8's DynamicMethod-versus-collectible-assembly comparison for cold compile, warm
   throughput, tiering/PGO, unloadability, code size, and memory. Treat ReadyToRun, dynamic PGO,
   and Native AOT PGO as separate deployment configurations, keeping build-time and runtime
   claims separate.

### Per-package exit gate

- current executed population, cost attribution, attainable ceiling, semantic owner, primary
  metric/direction, declared workload/RIDs, and separately labelled historical-corpus evidence;
- focused semantic regression plus owning test262 scope;
- paired uninstrumented MOD-M1 result that resolves and clears the predeclared threshold;
- allocation/GC/RSS/code/package/startup/tail guardrails; and
- explicit `accept`, retained opt-in experiment with owner/expiry, `defer`, or `remove` terminal
  state; failed experimental switches do not accumulate.

### DEL-9 — Select the JavaScript built-in composition on Broiler.VM

**Maps to:** MOD-M9 and the JavaScript built-in composition gate in
`Broiler.VM/docs/roadmap.md`.

**Objective:** select how Broiler.JS composes its built-in JavaScript profile with the
separately owned Broiler.VM foundation. This decision scopes JavaScript source compilation
and deployment; it does not decide whether generic Broiler.VM core or the WebAssembly
built-in exists.

### Finite entry bundle

DEL-9 starts only after DEL-3's predecision IL/AOT evidence, DEL-4's finite package/AOT
dispositions, DEL-5's compile-ahead decision, DEL-8 item 1's front-end/startup outcome, and
DEL-8 item 8's current-backend comparison are each accepted, deferred, cancelled, or below
resolution. It does not wait for continuous DEL-8 work. DEL-6 is additionally required only
for a JavaScript decision or follow-on slice that shares artifacts, consumes adaptive
IC/type-feedback state, or runs with concurrent contexts/Workers. Broiler.VM foundation and
WebAssembly-profile delivery follow their own entry gates independently.

### Decision bundle

1. Name dynamic-code-prohibited platforms, product scenarios, required JavaScript surface,
   deployment/startup/memory thresholds, staffing ceiling, conflict precedence, and the pinned
   product workload plus capability/conformance manifest, exclusions, and pass threshold.
2. Measure current IL, execution-only portable seed, relevant external/managed baselines, and
   supported deployment alternatives without presenting unlike engines as candidate controls.
3. Verify the required Broiler.VM profile and execution contracts, then price the JavaScript
   side separately: shared semantic-front-end extraction, value/frame mapping and GC roots,
   completion, exceptions/suspension/modules/eval/debugging/host interop, profile verification
   and persistence, AOT closure, and explicit IL-to-VM deopt-state feasibility.
4. Record exactly one outcome:
   - `execution-only` — verified precompiled JavaScript artifacts, with no runtime source,
     direct-eval, or Function-constructor compilation;
   - `narrow-runtime-compiler` — only the declared JavaScript compiler/language/host manifest;
     or
   - `general-runtime-compiler` — the declared general JavaScript runtime-compiler manifest
     and maintenance budget.

### Exit gate

- One ADR names the JavaScript composition, pinned capability/conformance manifest and threshold,
  supported/unsupported surface, RIDs, owner, budget, gates, rollback, and recertification
  triggers.
- The ADR identifies every DEL-4 hosting/compiler/tool boundary required to implement the
  selected JavaScript composition, or explicitly records that no additional split is required.
- Every JavaScript-profile item outside the selected manifest is cancelled or separately
  gated. The ADR cannot cancel, narrow, or claim completion of generic Broiler.VM core or the
  WebAssembly built-in.
- No AOT analyzer/sample result is presented as proof of an untested runtime-compiler closure.

### DEL-10 — Build JavaScript-profile correctness and its uninstrumented baseline

**Maps to:** the selected JavaScript correctness/baseline work on the Broiler.VM foundation
owned by `Broiler.VM/docs/roadmap.md`.

**Objective:** establish shared JavaScript semantics, a reconstructable JavaScript-profile
value/frame ABI within Broiler.VM's generic lifecycle, a verified JavaScript profile format, and an
attributable interpreter baseline before JavaScript-profile adaptive or persistence work.

### Work

1. Implement and validate every JavaScript-composition-required DEL-4
   hosting/compiler/tool boundary named by the MOD-M9 ADR; if none is required, retain the
   proved composition and its recorded no-split decision.
2. Migrate IL to the shared production JavaScript semantic IR before JavaScript-profile
   bytecode duplicates an analysis. Generic VM core does not own ECMAScript semantics.
3. Specify the JavaScript profile's own value/frame ABI: GC roots, environments,
   completion/unwinding, suspension, debugger and host state, and canonical source/bytecode
   identities. Broiler.VM core supplies lifecycle/result/resource contracts, not a universal
   language value or frame ABI.
4. Build the JavaScript profile's versioned bounded verifier contributions and minimal
   vertical opcode slices against an independent expected-result harness, using rather than
   redefining the Broiler.VM foundation contracts.
5. Complete the selected JavaScript hard-semantics manifest; unimplemented features fail
   deterministically.
6. Publish and run the selected JavaScript capability manifest under the selected deployment/
   compiler composition on its declared AOT RID matrix; this is the postdecision closure proof
   that the DEL-3 numeric seed deliberately did not claim.
7. Take the JavaScript-profile uninstrumented baseline, then add only measured IC, element,
   numeric, constant-pool, and call/closure work. Mutable IC or feedback-consuming work starts
   only after the applicable DEL-6 ownership, snapshot, invalidation, and lifetime gate.
8. Make the JavaScript profile's explicit stack-machine-versus-register-machine total-cost
   decision from the accepted ABI, executed traffic, bytes fetched, dispatch/decode and
   generated-code evidence, frame/GC cost, compiler/verifier/debugger complexity, and
   code-size/startup budget. If the separate diagnostic evidence is still required, carry the
   decision forward rather than guessing; this decision does not prescribe the WebAssembly
   profile's execution representation.

### Exit gate

- Expected, IL, and Broiler.VM JavaScript-profile outcomes agree for the declared capability
  manifest.
- Malformed or resource-exhausting JavaScript-profile artifacts fail safely and
  deterministically through the accepted VM boundary.
- The selected JavaScript AOT composition publishes and runs its real representative workload.
- The JavaScript-profile uninstrumented baseline is accepted before its persistence or
  adaptive-interpretation branches begin.

### DEL-11 — Add optional JavaScript persistence and adaptivity as independent branches

**Maps to:** the JavaScript-applicable persistence and adaptive work in
`Broiler.VM/docs/roadmap.md`. Emitted-IL tiering, deoptimization, and OSR are JavaScript-only
integration branches.

**Objective:** fund only the JavaScript-profile mechanisms whose own populations and product
outcomes justify their complexity. Generic Broiler.VM mechanisms and WebAssembly-profile work
remain separately owned and require their own evidence.

### Branch entry gates

- JavaScript-profile persistence requires an accepted profile format/verifier plus a MOD-M1
  cold-start/repeated-compile or precompiled-load opportunity whose paired interval resolves
  and clears the startup boundary. It is independent of the adaptive-interpreter diagnostic
  gate.
- JavaScript-profile feedback, quickening, superinstruction, dispatch, and PGO proposals
  require the accepted uninstrumented baseline and separate calibrated diagnostic evidence:
  executed population, attributed cost, attainable ceiling, predeclared decision rule, and
  measured observer effect.
- Feedback, quickening, mutable ICs, shared artifacts, concurrent contexts, or background
  installation additionally require the applicable DEL-6 ownership, snapshot, invalidation,
  reclamation, and quiescent-publication gates. This does not block an immutable single-context
  baseline or an otherwise independent persistence experiment.
- JavaScript function promotion, deoptimization, and OSR are eligible only after
  `narrow-runtime-compiler` or `general-runtime-compiler`, in a dynamic-code-capable
  composition with an explicit product adaptivity requirement and the separate IL-tier entry
  evidence. `execution-only` authorizes only its composition-applicable persistence and adaptive
  interpreter branches, never IL tiering. WebAssembly does not enter this JavaScript IL-tier
  branch.

### Branches

- **JavaScript-profile persistence:** canonical verified format, semantic cache key,
  checksums/bounds, atomic replace, and no process-local IDs. Runtime-compiler compositions
  may recompile source after a bad cache; execution-only compositions report a defined load
  failure and accept a fresh verified artifact.
- **Owned JavaScript feedback and quickening:** immutable canonical bytecode plus owner-scoped
  feedback/quickening sidecars, exact generic fallback, stable
  source/exception/suspension/debugger identity, and measured reset/reclamation.
- **JavaScript superinstructions:** candidates generated from measured JavaScript opcode
  n-grams, with dispatches removed, bytecode/code-size cost, verifier/debugger/source-map
  impact, and a capped maintained set.
- **JavaScript dispatch layout and encoding:** plain-switch versus supported alternatives only
  where calibrated evidence and generated-code inspection identify material dispatch/decode
  cost on each claimed JIT/AOT RID.
- **JavaScript-profile Native AOT PGO:** trained and untrained images from the representative
  target workload, with training identity, startup, throughput, image size, and
  generalization guardrails.
- **JavaScript function promotion to IL:** only after its own performance gate; measure the
  threshold curve, use owned state and a compiled-function descriptor, publish at a quiescent
  boundary, retain the VM fallback, and bound failure/backoff and lifetime resources.
- **JavaScript IL-to-VM deoptimization:** run the bounded end-to-end state-materialization
  feasibility spike first. Only a passing spike may fund explicit `DeoptState`, guards,
  live-value/environment/continuation reconstruction, forced-failure matrix, and restart
  comparison. Record `no-go` for this branch and stop if CLR-live-state materialization or the
  predeclared code-size, runtime, conformance, or maintenance ceiling fails.
- **JavaScript OSR:** only after validated promotion and its own hot-loop population,
  loop-entry ABI, state mapping, guard behavior, identity, and anti-thrashing gate. It does
  not depend on deoptimization succeeding.

Each JavaScript-profile branch may end as accepted, experimental with an owner/expiry,
deferred, or no-go. Success in one branch is not evidence for another, and no branch outcome
changes the existence or support decision of generic Broiler.VM core or the WebAssembly
built-in.

### DEL-12 — Release, rollback, and continuous recertification

**Maps to:** MOD-M10 and the product/release portions of `Component.md`.

### Work

1. Keep generated graph, state index, API/package baselines, closure inventories, and supported
   profile manifests in required CI checks.
2. Require feature switches and a tested rollback path for compile-ahead, adaptive
   JavaScript-profile state, profile persistence, JavaScript IL tiering, deoptimization, OSR,
   and Worker exposure.
3. Re-run the applicable evidence bundle after hardware, OS, microcode, power/thermal policy,
   compiler/runtime/SDK, dependency graph, RID, publish settings, compiler backend, effective
   JIT/tiering/PGO/ReadyToRun/GC/CPU-feature state, bootstrap profile, scheduler, assembly/package
   graph, public API, capability manifest, or harness/corpus revision changes.
4. Publish Broiler.JS support tables for IL, `execution-only`, any selected
   runtime-compiler/AOT composition, Workers, and explicitly unsupported shared-memory/host
   surfaces. Link rather than duplicate the separately owned generic Broiler.VM and
   WebAssembly-profile support records.
5. Archive delivery narrative after durable decisions move into current architecture/support
   documents; retain raw evidence and ADRs.

### Exit gate

- Pristine consumers restore/build/run for every supported package composition.
- CI fails on documented-graph, API/package, AOT-closure, link/ID, or profile-manifest drift.
- Every shipped optimization/capability has an owner, rollback, resource ceiling, and
  recertification trigger.

---

## 4. Recommended first three increments

### Increment 1 — truth before movement

- DEL-0 state/index/link/ID work;
- the immediate `TypedArray.prototype.set` correctness regression and terminal result;
- S-7 plus source/binary consumer validation;
- DEL-1 lane manifests, source identity, exact comparator, and A/A calibration; and
- close the ownership referential-integrity validation defect recorded in
  [`Phase-0.status.md`](Phase-0.status.md).

**Handoff:** a trustworthy state index and one reproducible null/candidate evidence bundle.

### Increment 2 — prove boundaries and ownership

- DEL-2 project shells, cycle resolution, FrontEnd/Semantics and fake-backend contracts;
- DEL-3 dynamic-code/reflection census and execution-only publish/run matrix;
- DEL-4's finite package/AOT dispositions required by the JavaScript composition decision;
- DEL-5/DEL-6 compiler/global-state, cache/artifact, context-entry, and reclamation census.

**Handoff:** a build-proven target graph, classified AOT closure, and explicit concurrency
ownership model. No broad production split or production JavaScript-profile expansion is
required in this increment; generic Broiler.VM delivery remains separately owned.

### Increment 3 — take the measured decisions

- accept/defer/cancel bounded compile-ahead;
- close independent-context safety before promoting Worker support;
- record explicit accepted/deferred/cancelled/below-resolution outcomes for MOD-M8-1's
  front-end/startup work and MOD-M8-8's current-backend comparison, then run any other
  highest-value DEL-8 packages through MOD-M1; and
- assemble the finite DEL-9 JavaScript-composition decision bundle.

**Handoff:** supported Worker/compile-ahead scope, terminal current-engine dispositions, and
the terminal JavaScript-composition ADR that scopes DEL-10 and the independently gated DEL-11
branches precisely, without cancelling or claiming generic Broiler.VM or WebAssembly work.

## 5. Evidence handoff checklist

Every completed delivery slice records:

- authoritative `MOD-M*`/phase/assembly item and accountable owner;
- exact source, dependency, build, runtime, RID, and effective-setting identity;
- focused correctness plus affected conformance manifests;
- candidate/control/null/instrumented arms as applicable, with raw evidence location;
- time, allocation, GC, RSS/working set, code/package size, and applicable tail/resource cap;
- public API/package/profile/unsupported-surface effect;
- decision outcome, rollback, remaining risk, and recertification triggers; and
- updated owning `*.status.md` evidence and generated state index in the same change.

If any field is not applicable, record why. An empty field is not an implicit pass.
