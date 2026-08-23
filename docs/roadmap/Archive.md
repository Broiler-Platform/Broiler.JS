# Archive — the two superseded plans

> ## ⚠️ Nothing in this file is the current plan.
>
> **The current cross-track execution plan is [`Modernization.md`](Modernization.md)**;
> [`Roadmap.md`](Roadmap.md) is the campaign catalogue and historical crosswalk. They have
> **corrected several diagnoses and replaced the acceptance protocol below**. Where this archive
> disagrees with either current document, the current owning phase/status pair and
> [`Measurement.md`](Measurement.md) win. In particular, “two runs inside a configured band” is
> historical smoke evidence, not present-day acceptance. Nothing else here has been back-ported.
>
> It is kept, in full and unedited apart from this banner and the link rewrites the
> consolidation required, for three things the merge deliberately did **not** carry
> forward — see [`Roadmap.md`](Roadmap.md#appendix-b--traceability), which maps every
> P-item to where it went:
>
> - **The measurements.** Every P0–P3 figure, and the reasoning that produced it.
> - **The defect narratives** the merge dropped as history rather than plan: the
>   `SAUint32Map<T>` sentinel, the Debug-build stack-trace-on-throw, the six pre-existing
>   test failures, the three frame-recycling defects. Only their *transferable* lessons were
>   lifted, into [`measurement.md §3.5`](Measurement.md#35-standing-measurement-lessons).
> - **Two scope exclusions that were overturned.** §9 excludes parsing/compilation and a
>   speculating tier. Both are superseded — they are phases 1 and 4 of the current plan — and
>   [`performance.md §1` §1.1](Roadmap.md#1-what-the-merge-produces-that-neither-document-had) explains why the exclusion was
>   reasonable when written and what the probe corpus could not see.
>
> *This banner is the annotation `Roadmap.md` recorded as owed and could not
> apply:* it noted that this file "is labelled only there, because it is inside the submodule
> and this repository cannot annotate it without a pointer bump". The plan now lives in the
> submodule too, so the label goes where it belongs.

---

This roadmap covers **steady-state script execution speed**: the cost of running JavaScript
once it has been parsed and compiled. It is a new, evidence-led plan, not a continuation of
the phase 0–5 optimization campaign referenced in [`docs/roadmap/component.md`](Component.md).

> `docs/roadmap/component.md` §4 currently states that "the optimization implementation phases are
> complete" and that only release *evidence* remains. That is accurate for the work those
> phases scoped (storage layouts, startup, packaging, SIMD, tiering experiments). It is not
> accurate as a statement about the engine's execution speed. The measurements below show
> that the two structures those phases delivered for hot-path execution — the object-shape
> layout and the property inline cache — are **inert for most real JavaScript**, and that
> three unrelated pieces of always-on bookkeeping dominate the interpreter loop. §4 of the
> main roadmap should be amended to point here.

- Owner assemblies: `Broiler.JavaScript.Runtime`, `.Engine`, `.BuiltIns`, `.Storage`, `.Compiler`
- Acceptance protocol: unchanged — [`Measurement.md`](Measurement.md) governs what may
  be *claimed*. Nothing in this document closes on the numbers below.
- Status: **P0 implemented** (§4). **P1 implemented** (§5). **P2 implemented in full**, including the unboxed-`double`-locals item that was
  filed as "longer term" and turned out to be the largest win here (20–25× on a counted
  loop, which now allocates nothing), plus two larger defects found along the way
  ([§6.5](#65--found-while-implementing-p2)); P2-2 landed only after the reasons it had been
  declined were re-examined and both found wrong (§6). **P3's premise was disproved by
  measurement** — the scopes it blamed cost nothing — and the per-call activation record they
  were hiding was replaced with a shadow stack instead (§7).
  **No phase is *closed*.** Every one of them still owes acceptance evidence that has not been
  collected, and the engineering each deliberately left behind is collected in
  [§8.1](#81--open-items).

---

## 1. Verdict

Yes — there is substantial, unrealized headroom, and most of it is not exotic.

Four defects account for the majority of the gap. None of them are "make the JIT smarter"
problems; three are always-on bookkeeping that can be removed or made lazy, and one is a
property-write path that silently disables the engine's own optimization for the most common
way JavaScript builds objects.

**The three P0 items are implemented** (see the status note on each). Measured on the probes
in [Appendix A](#appendix-a--reproducing-the-measurements), before against after:

| Hot path | Before | After | Factor | Alloc before | Alloc after |
|---|---:|---:|---:|---:|---:|
| Plain function call (sloppy) | 945 ms | 327 ms | **2.9×** | 1 784 B/call | **264 B** |
| Closure call | 953 ms | 357 ms | **2.7×** | 1 816 B/call | **296 B** |
| Prototype method call | 861 ms | 370 ms | **2.3×** | 1 632 B/call | **264 B** |
| Built-in call (`Math.max`) | 443 ms | 217 ms | **2.0×** | 400 B/call | **176 B** |
| Empty `for` loop | 426 ms | 210 ms | **2.0×** | 96 B/iter | 96 B/iter |
| Own property read | 491 ms | 333 ms | **1.5×** | 128 B/iter | 128 B/iter |
| Integer arithmetic | 476 ms | 342 ms | **1.4×** | 128 B/iter | 128 B/iter |
| Class field read | 215 ms | 160 ms | **1.3×** | — | — |
| `script:stopwatch` (real script) | 976 ms | 669 ms | **1.5×** | 736 MB | **264 MB** |

Timings are the **slower** of two post-change runs against a single pre-change run; run-to-run
variance on this container is 10–15%, so treat the factors as approximate. Allocation is
deterministic — the byte counts were identical across runs and are quoted exactly.

Full suite after the change: **6 824 tests, 6 818 passing, 0 new failures** (the 6 failures were
pre-existing: 5 ICU/locale-data dependent, 1 in ModuleExtensions — all six are resolved now, see
§8). 21 of those tests are new and cover the reworked behaviour.

**P1 is implemented too.** It targets a
different axis — the inline cache, which P0 left untouched — so it is measured by hit rate
rather than by wall clock. Over 200k-iteration monomorphic sites:

| Site | Before P1 | After P1 |
|---|---:|---:|
| `var o = {}; o.x = 1` then read `o.x` | **0** hits / 200 000 misses | 199 999 / 1 |
| `class C { constructor(v){ this.v = v } }` then read `c.v` | **0** / 200 000 | 199 999 / 1 |
| `P.prototype.get` — inherited method call | **0** / 200 001 | 399 998 / 3 |
| class method call | **0** / 200 000 | 399 998 / 2 |
| dictionary-mode fallbacks (inherited method loop) | 200 006 | **0** |

Allocation for the object shape that suffered most: building a three-field object through a
constructor cost **6 595 bytes** before P1 and **1 480** after, against 1 328 for the
equivalent object literal — the gap between "assigned" and "literal" is essentially closed.

**P2 delivered both what it planned and what it did not.** P2-1 and P2-4 landed as written,
P2-3 as something smaller than written, and P2-2 only after an earlier decision to decline it
was revisited and overturned (§6). Working through the phase also
turned up two defects on the array paths, found by measuring rather than reading, and larger
than anything P2 had scoped ([§6.5](#65--found-while-implementing-p2)):

| | Before | After |
|---|---:|---:|
| **P2-4** `s = s + x` × 20 000 — was quadratic | 1 604 ms / 3.20 GB / 913 gen2 | **10.7 ms / 4.4 MB / 0 gen2** |
| **P2-4** `script:dromaeo-object-string` | 4 733 ms / 15.9 GB | **1 662 ms / 1.5 GB** |
| 200 000 `pop()`s — length shrink was O(n) per call, so the loop was quadratic | 466 427 ms | **640 ms** (729×) |
| Filling a fresh array, per element — the indexed twin of P1-1 | 1 350 B | **145 B** (9×) |
| `push`, per element (P2-1 plus the above) | 3 803 B | **1 480 B** |
| `script:dromaeo-object-array` | 5 564 ms | **646 ms** (8.6×) |
| `script:array-stress` | 422 ms | **113 ms** (3.7×) |

---

## 2. How the evidence was collected

Single-machine, in-process timing and allocation counting via
`GC.GetAllocatedBytesForCurrentThread()`, plus the engine's own
`PropertyOptimizationDiagnostics` counters. Each scenario is compiled and run once to warm,
then measured on a second evaluation in the same context.

- "Before" = commit `833b74a`; "after" = the P0 implementation. Both `Release`, .NET SDK 10.0.110
- Linux x64, 4 × Intel Xeon @ 2.80 GHz, 15 GB RAM, containerized
- Each engine build was measured from a clean `bin`/`obj` so no stale assembly could be mixed in

**These numbers are for prioritization only.** Timings vary 10–15% run to run on a shared
4-core container; the reported "after" figures are the slower of two runs, so the factors are
conservative but approximate. Allocation counts are deterministic and repeated exactly across
runs. The two `dromaeo-object-*` scenarios are GC-dominated and swung ±25% in both directions
— no claim in this document rests on them.

Any *published* performance claim must go through the repeatability and semantic gates in
[`Measurement.md`](Measurement.md) (two runs inside the configured band, fresh-process
lifecycle samples, the release RID matrix, and the semantic owners named in
`eng/performance/ownership.json`). Nothing here has been through those gates.

---

## 3. Baseline (pre-P0, commit `833b74a`)

The state the findings below were diagnosed against, kept for reference. Times are for the
iteration counts in [Appendix A](#appendix-a--reproducing-the-measurements); compare columns,
not absolute values.

| Scenario | ms | bytes/iteration |
|---|---:|---:|
| `loop-empty` (3M) | 426 | 96 |
| `arith-add` (3M) | 476 | 128 |
| `prop-own-get` (3M) | 491 | 128 |
| `prop-own-set` (3M) | 557 | 96 |
| `class-field` (3M) | 215 | 43 |
| `array-rw` (1M) | 270 | 194 |
| `fn-call` (1M) | 945 | 1 784 |
| `fn-call-strict` (1M) | 451 | 488 |
| `closure-call` (1M) | 953 | 1 816 |
| `proto-method-call` (1M) | 861 | 1 632 |
| `builtin-call` (1M) | 443 | 400 |
| `obj-alloc` (500k) | 308 | 1 328 |
| `array-push` (500k) | 1 500 | 4 028 |
| `string-concat` (200k) | 121 | 278 |
| Fresh `JSContext` | 1.20 ms | 1 866 174 |

Inline-cache behaviour on a monomorphic constant-key read site, 200k iterations:

| Site | Cache hits | Misses | Dictionary fallbacks | Prototype invalidations |
|---|---:|---:|---:|---:|
| `{x:1,y:2}` then `o.x` | 199 999 | 1 | 2 | 800 013 |
| `P.prototype.get()` call | **0** | 200 001 | 200 006 | 1 200 030 |
| `class C { constructor(v){this.v=v} }` then `c.v` | **0** | 200 000 | 5 | 800 028 |

---

## 4. P0 — always-on bookkeeping in the hot path — **implemented**

All three items below are implemented and covered by repository tests. Each keeps its original
problem statement and evidence, followed by a **Status** note recording what shipped, what was
deliberately rejected, and what was left behind. They are not *closed*: see §8 for the
acceptance evidence still owed.

### P0-1 · Every value allocation invalidates every inline cache

`JSValue`'s constructor assigns `BasePrototypeObject`, whose setter unconditionally calls
`JSObject.NotifyPrototypeChainMutation()` — even when the prototype being assigned is `null`,
which is the case for every primitive.

`Broiler.JavaScript.Runtime/JSValue.cs` — `BasePrototypeObject` and the `JSValue(JSValue)`
constructor, as they were before the fix:

```csharp
public virtual JSValue BasePrototypeObject
{
    set
    {
        (value as JSObject)?.MarkUsedAsPrototype();
        prototypeChain = CreatePrototypeObject?.Invoke(value);
        JSObject.NotifyPrototypeChainMutation();   // ← unconditional
    }
}

protected JSValue(JSValue prototype) => BasePrototypeObject = prototype ?? GetCurrentPrototype();
```

So **every `JSNumber`, `JSString`, and `JSBoolean` allocation** performs a delegate invoke
plus three `Interlocked` increments on process-wide statics
(`indexedPrototypeVersion`, `prototypeMutationVersion`, and the diagnostics counter). A
200k-iteration loop reading one property records **800 013 prototype invalidations** — four
per iteration, one per intermediate value.

Two of those three counters are pure waste:

- `prototypeMutationVersion` is written on every allocation and **read by nothing** except
  `PropertyOptimizationDiagnostics.Snapshot()`. The property inline cache does not consult it
  (`ObjectShape.cs`, `PropertyInlineCache.Get`, validates the shape id only). It is a
  counter with no consumer.
- The diagnostics counters themselves are unconditional `Interlocked.Increment` calls on
  shared statics, taken on **every** cache hit and every cache miss
  (`ObjectShape.cs`, `RecordCacheHit`/`RecordCacheMiss`). On a multi-threaded host this is a
  cache-line ping-pong on the engine's hottest line.

`indexedPrototypeVersion` does have a real consumer — `JSArray.CanUseDenseElementFastPath()`
(`BuiltIns/Array/JSArray.cs:30`) — but assigning a *null* prototype cannot add an indexed
property to anyone's prototype chain, so the null case need not bump it.

**Fix.** Return early from the setter when `value is null`; make the diagnostics counters
opt-in behind a static switch (or `[Conditional]`) so they cost nothing when disabled; and
either delete `prototypeMutationVersion` or give it its real job (see P1-2, where a
prototype-validity generation is exactly what a prototype-chain cache needs).

**Measured.** `loop-empty` 426→210 ms, `arith-add` 476→342 ms, `prop-own-get` 491→333 ms,
`array-rw` 270→206 ms, `class-field` 215→160 ms. Prototype invalidations 800 013 → 3.

**Risk: low.** The only removed behaviour on the null path is a global version bump that
provably cannot invalidate anything.

**Status: implemented.**

- `JSValue.BasePrototypeObject` returns early when the assigned prototype is `null`, with the
  soundness argument recorded inline at the call site.
- `PropertyOptimizationDiagnostics` gains an `Enabled` switch, defaulting to **off**, plus an
  `Enable()` scope that restores the previous setting. This is a public-surface change: the
  counters now read zero unless a caller opts in. `Reset()` deliberately does not change it.
  The two tests in `Phase3CompilerSpecializationTests` that assert on the counters were
  updated to take the scope.
- **Deferred at the time, done in P1:** `JSObject`'s own `BasePrototypeObject` override also
  bumped the version on every object allocation. P1-2 needed a prototype version that is
  stable in a loop that allocates, so it now distinguishes a brand-new object adopting its
  first prototype (`prototypeChain` was null and the object is not yet anyone's prototype)
  from a real `[[SetPrototypeOf]]`. Sound for the array fast path too: a fresh object cannot
  be in any existing chain, and becoming someone's prototype publishes the version via
  `MarkUsedAsPrototype`.

**Owners.** `Broiler.JavaScript.Runtime` · semantic owner `Broiler.JavaScript.Runtime.Tests`.

---

### P0-2 · `AsyncLocal<int>` written twice per JavaScript function call

`Broiler.JavaScript.Engine/Core/JSEngine.cs`, as it was before the fix:

```csharp
private static readonly AsyncLocal<int> _strictModeDepth = new();
```

`JSFunction.InvokeFunction` enters `using (JSEngine.EnterStrictMode(current.IsStrictMode))`
on every call, and the scope's constructor and `Dispose` each write `_strictModeDepth.Value`.

An `AsyncLocal<T>` **set** is not a field store. It boxes the value, copies the
`AsyncLocalValueMap`, and allocates a fresh `ExecutionContext` — three allocations and a
non-trivial amount of work, twice per JS call. This is why a strict call allocates 488 bytes
before it has done anything.

**Measured** (replacing it with `[ThreadStatic]` purely to size the cost):
`fn-call-strict` 390→234 ms and 488→264 bytes/call; `builtin-call` 322→175 ms and
400→176 bytes/call; `proto-method-call` 825→632 ms.

**Status: implemented — but not by swapping the storage.** The scope now stores a `bool` and
**only writes on an actual strict/sloppy transition**:

- The counter was only ever read as `depth > 0`, and the scope assigned
  `enabled ? previous + 1 : 0`. That is exactly a boolean assignment — the observable value
  after entry is `enabled` for any prior depth — so storing the boolean directly is
  behaviour-preserving, and it is what makes the common case a no-op. With a counter,
  strict-calling-strict still moved 1 → 2 and had to write.
- It remains an `AsyncLocal`, so ExecutionContext capture still carries it; nothing about
  async flow changed. Reads are cheap, writes are what cost, and real code changes strictness
  rarely — calls within one strictness level are the norm and now write nothing.
- Nine tests in `StrictModeFlowTests` cover the transition shapes: strict-from-sloppy,
  sloppy-from-strict, restoration after each kind of nested call, and a same-strictness chain
  (the case the optimization makes free).

While writing those tests a **separate pre-existing gap** surfaced, verified identical on the
unmodified engine and unrelated to this change: **async and generator bodies never enter the
runtime strict-mode scope at all.** `JSFunction.InvokeFunction` wraps ordinary calls in
`EnterStrictMode`, but the rewritten async/generator drivers do not, so a failing `[[Set]]`
inside a `'use strict'` async function or generator does not throw — even in the async
function's synchronous prefix, before any `await`, and in a generator before any `yield`. It is
pinned green as `KnownGap_AsyncAndGeneratorBodiesDoNotEnterRuntimeStrictMode` so it cannot
change silently, and belongs in the compliance failure manifest rather than here.

(Also observed while testing: async continuations do not run at all under in-process `Eval` or
`Execute`, so post-`await` strictness is not currently observable from a unit test.)

**Rejected: `[ThreadStatic]`.** It measures well but is wrong — it would lose the value when an
async function resumes on a different thread. The eventual right answer is to stop using
ambient state for this at all:

- Strictness is a **static property of the code being compiled**, not of the dynamic call
  stack. The compiler already knows it. `JSValue`'s set accessors consult it dynamically via
  `IsStrictModeEnabled` (`JSValue.cs:54`), which is what forces the ambient variable to exist.
- Preferred: thread the strict flag through the emitted code — pass it to the property-set
  helpers the compiler emits, so no ambient read is needed on the hot path.
- Interim: keep the value on the existing per-call `CallStackItem` / context frame, which is
  already allocated and already flows correctly through async resumption, and only fall back
  to an `AsyncLocal` read when no frame is present.

Either way the invariant to preserve is the one documented on `StrictModeScope`: a sloppy
callee entered from a strict caller must drop back to non-strict. `test262-strict-mode.txt`
is the gating manifest, and async/generator resumption needs explicit coverage.

**Owners.** `Broiler.JavaScript.Engine` + `Broiler.JavaScript.Compiler` · semantic owner
`Broiler.JavaScript.Compiler.Tests`.

---

### P0-3 · Legacy `f.caller` / `f.arguments` materialized on every sloppy call

`BuiltIns/Function/JSFunction.cs`, inside `InvokeFunction`, as it was before the fix:

```csharp
var trackLegacyCaller = current.HasLegacyCallerArguments;
if (trackLegacyCaller)
{
    current.SetLegacyCaller(previousExecutingFunction);
    current.SetLegacyArguments(CreateLegacyArgumentsObject(currentArguments));
}
```

Every ordinary non-strict function carries these Annex B properties, so on **every sloppy
call** the engine eagerly builds a complete arguments object — a `JSObject` plus one indexed
property per argument plus a `length` — and writes it, and the caller, into the function's own
property table. It is then overwritten with `null` in the `finally`. `CreateInstance` does the
same on every `new`.

Almost no program ever reads `f.arguments`. The engine pays for it unconditionally.

A secondary effect shows up in the counters: `CreateLegacyArgumentsObject` installs `length`
with `ConfigurableValue` attributes, which is not the default data-property set, so
`TrackShapeDataProperty` calls `AbandonObjectShape()` on the object it just built. The
prototype-method-call probe records **200 006 dictionary fallbacks in a 200 000-iteration
loop** for this reason. Note this is the *throwaway* arguments object being dictionary-ized,
not a receiver whose shape any inline cache depends on — the function object itself is a
`JSFunction`, which the shape tracker skips entirely. The wasted work is real; the inline
cache is not the victim. (P1-1 is what actually keeps caches from working.)

**Measured** (tracking disabled purely to size it): `fn-call` 678→272 ms and
1 560→264 bytes/call; `proto-method-call` 632→309 ms and 1 408→264 bytes/call;
`closure-call` 682→278 ms and 1 592→296 bytes/call.

**Risk: medium. Deleting the behaviour is not an option** — `forbidden-ext/b2/*` in test262
reads these properties directly and requires that the access not throw, and the "non-strict
`f.arguments` is non-null while `f` is on the stack" behaviour is web reality.

The fix is to make it **lazy**, not to remove it:

1. Store the in-flight `Arguments` (a struct already on the stack) and the caller reference in
   plain fields on `JSFunction`, not as observable properties. Push/pop is then a few field
   writes.
2. Back the observable `caller` / `arguments` properties with values that materialize the
   arguments object **on read**, from those fields — without turning them into accessor
   properties, which would change their observable descriptor shape.

Care is needed on two points the current code gets right: a strict caller must be reported as
`null` rather than through a throwing accessor, and the properties must still shadow
`Function.prototype`'s poison-pill accessors. Recursion also needs saved values restored on
exit rather than cleared.

**Status: implemented.**

- A new `IDeferredPropertyValue` (Runtime) is a property value that is recomputed on each read
  and resolved by `JSValue.ResolvePropertyValue`. Crucially the property stays an ordinary
  **data** property — `[[Get]]`, `[[GetOwnProperty]]` and `Object.getOwnPropertyDescriptor` all
  still report `value`/`writable`, not `get`/`set` — so the observable descriptor shape the
  Annex B tests check is unchanged. An accessor-backed design would not have preserved that.
- `JSFunction` keeps the in-flight caller and `Arguments` in plain fields, pushed and popped
  around [[Call]] and [[Construct]]. `PushLegacyFrame` returns the previous frame and
  `PopLegacyFrame` restores it, so an outer invocation's values become visible again when a
  recursive inner one returns — the previous code cleared to `null` instead, which is why
  `RecursiveInvocation_RestoresTheOuterFrameWhenTheInnerReturns` fails on the old engine and
  passes on the new one. That is the one intentional behaviour change here, and it is a fix.
- The strict-caller filter still runs on entry (it depends on the calling function, which is
  only known then); only the arguments *object* is deferred.
- Three unrelated code paths cast `JSProperty.value` straight to `JSValue`
  (`JSPropertyExtensions` ×2, `CoreInternalHelpers`) and would have thrown on a deferred cell;
  all three now route through `ResolvePropertyValue`, which also makes them handle
  `LazyDataPropertyCell` consistently.
- Twelve tests in `LegacyCallerArgumentsTests` cover: null off-stack, live while running,
  descriptor shape and attributes, descriptor read while on stack, strict-caller hiding,
  recursion, throwing invocations, `[[Construct]]`, redefinition freezing, strict functions
  having no own property at all, rejected assignment, unmapped-ness, and non-enumerability.
- Measured after implementation: `fn-call` 945→327 ms and **1 784→264 bytes/call**;
  `proto-method-call` 861→370 ms and **1 632→264 bytes/call**; `closure-call` 953→357 ms and
  **1 816→296 bytes/call**. Dictionary fallbacks in the prototype-method-call probe:
  200 006 → 4.

**Owners.** `Broiler.JavaScript.BuiltIns` · semantic owner `Broiler.JavaScript.BuiltIns.Tests`,
manifest `test262-strict-mode.txt` plus the Annex B forbidden-extension tests.

---

## 5. P1 — make shapes and inline caches actually work — **implemented**

The shape system exists and is correct; it is simply almost never reachable. This is the
largest *remaining* win after P0 and the one that needs design rather than deletion.

All four items are implemented, covered by `PropertyShapeCacheTests` on the read side and
`PropertyStoreCacheTests` on the write side. Two carry a documented remainder: P1-3 caches
overwrites but not the shape transition that *creating* a property needs, and P1-4's double
storage is still there. As with P0 these are not *closed*: §8 lists the acceptance evidence
still owed.

### P1-1 · An ordinary property write destroys the object's shape

This is the headline defect. Measured on unmodified `HEAD`, 100k reads of a monomorphic
constant-key site:

| How the property got there | IC hits | IC misses |
|---|---:|---:|
| `var o = {x:1}` (object literal) | 99 999 | 1 |
| `var o = {}; o.x = 1` | **0** | 100 000 |
| `function C(){ this.x = 1 }` (constructor) | **0** | 100 000 |
| `var o = {x:1}; o.y = 2` (literal, then one assignment) | **0** | 100 000 |
| `var o = Object.create(null); o.x = 1` | 99 999 | 1 |

The `Object.create(null)` row is the tell, and it identifies the cause exactly.

`JSObject.SetValue(KeyString, …)` does not find the property own, so it recurses into the
prototype: `prototypeObject.SetValue(name, value, receiver ?? this, throwError)`
(`Runtime/JSObject.PropertyStorage.cs:796`). The recursion bottoms out at `%Object.prototype%`,
which calls `SetKeyStringOnReceiver` with `this` = `%Object.prototype%` and `target` = the real
receiver. Because `!ReferenceEquals(target, this)`, the write takes the generic receiver path
(`:813`) and lands in `DefineReceiverDataProperty` (`:1103`), whose `else` branch does:

```csharp
var descriptor = CreateDataDescriptor(value, attributes);
var result = target.DefineProperty(name, descriptor);
```

and `DefineProperty(in KeyString, JSObject)` opens with `AbandonObjectShape()`
(`:1335`).

So a plain `obj.x = 1`:

1. allocates a `JSString` for the key (`name.ToJSValue()`),
2. allocates a descriptor `JSObject` carrying `value`/`writable`/`enumerable`/`configurable`,
3. immediately re-reads those four properties back out of it, and
4. **permanently** drops the receiver into dictionary mode.

With a null prototype there is no prototype to recurse into, `target == this` holds, the fast
path is taken, and the shape survives — hence 99 999 hits on that row alone.

The allocation cost is visible directly:

| | ms / 100k | bytes/op |
|---|---:|---:|
| `{a:i, b:i, c:i}` literal | 141 | 1 266 |
| `this.a=i; this.b=i; this.c=i` in a constructor | 404 | **6 595** |

Same three properties on the same object layout, **5.2× the allocation** and 2.9× the time,
purely because of the write path.

**Consequence.** Constructors, `class` fields, and any incremental object building — that is,
most idiomatic JavaScript — get zero benefit from shapes or inline caches today.

**Fix.**

1. In `SetKeyStringOnReceiver`, when `target` is an ordinary `JSObject` and the resolved
   attributes are the default data-property set, write directly through the shape-tracking
   path (`ownProperties.Put` + `TrackShapeDataProperty`) instead of round-tripping through a
   descriptor object. The `ReferenceEquals(target, this)` branch already does exactly this
   (`:1105`); the receiver-mismatch branch needs the same treatment when the receiver is
   ordinary and no proxy/accessor is involved.
2. Do not call `AbandonObjectShape()` in `DefineProperty` when the descriptor being defined is
   a plain data property with default attributes on an ordinary object — add the slot instead.
3. Keep the descriptor round-trip only where it is observable: proxies, accessors,
   non-default attributes, non-extensible targets, integer-indexed exotics.

**Risk: medium-high.** This is the spec-sensitive path: `OrdinarySetWithOwnDescriptor`,
receiver mismatch, proxy `set`/`defineProperty` trap ordering, and the extensibility check
comments already in the file document real test262 failures that were fixed by routing through
`DefineProperty`. The fast path must be entered only when it is provably unobservable. Gate on
`test262-properties-proxy.txt` and the full `Broiler.JavaScript.Runtime.Tests` suite.

**Status: implemented, both halves.**

- `DefineProperty(in KeyString, JSObject)` no longer abandons the shape. It reads the property
  map through the field rather than `GetOwnProperties()` (whose `create: true` guard exists for
  callers in *other* assemblies that could write behind the tracker's back) and, after the
  write, hands the resulting property to `TrackShapeDataProperty`, which is the single
  decision point for track-or-abandon. Only two cases are decided at the call site, both
  abandoning: an accessor, whose `value` field holds the getter, and a preserved
  `LazyDataPropertyCell`.
- The descriptor round-trip is gone for the ordinary case:
  `TrySetOrdinaryReceiverDataProperty` applies a receiver-mismatch write directly when the
  target is an exact `JSObject`, reproducing the generic path's decisions exactly
  (`GetReceiverAttributes` provably reconstructs the existing property's own attributes for a
  data property, so they are simply reused). It declines — and the generic path runs
  unchanged — for a Proxy or any exotic, an array-index key, a private name, or a pending
  lazy cell.
- Every `ownProperties` mutation was audited to confirm it still either tracks or abandons:
  `FastAddValue` tracks; `FastAddProperty`, `FastAddLazyDataProperty`, `FastAddDeferredValue`,
  `Delete`, and `GetOwnProperties(create: true)` abandon; freeze and seal route through
  `DefineProperty` with non-default attributes, which abandons.
- `TrackShapeDataProperty` now accepts **any** plain data property rather than only the exact
  default attribute set. That restriction quietly excluded every prototype object in the
  engine — a function's `prototype` carries a non-enumerable `constructor`, and class methods
  are non-enumerable — so each one abandoned its shape as it was built and no inherited method
  could ever be cached. A slot records where the value lives; writable/enumerable/configurable
  do not move it. Accessors are still excluded.

**Found while doing this, not fixed here:** `Reflect.set(base, k, v, receiver)` where the
receiver has no own `k` gives the new property the *base's* attributes instead of the
all-true attributes `CreateDataProperty` mandates. Verified identical before the change, so
it is pre-existing and unrelated; it belongs in the compliance manifest.

---

### P1-2 · The inline cache does not cover prototype lookups — so method calls never hit

`PropertyInlineCache.Get` (`Runtime/ObjectShape.cs`) validates an **own** data slot only.
A prototype method is by definition not an own property of the receiver, so `obj.method()`
misses unconditionally: **0 hits / 200 001 misses** in the probe, plus two `Interlocked`
increments per miss for the diagnostics.

Worse, method calls do not reach the cache at all. `JSValueBuilder.InvokeMethod`
(`LinqExpressions/JSValueBuilder.cs:186`) emits `Expression.MakeIndex(targetTemp, method, name)`
— the raw `this[KeyString]` indexer — while only `CachedIndex` (plain member reads,
`FastCompiler.VisitMemberExpression.cs:61`) goes through the cache.

**Fix.** Add a prototype-lookup cache entry kind: `(receiverShapeId, holderObject,
holderSlot, prototypeGeneration)`. Validate it with a receiver shape check plus a global
prototype-mutation generation — which is precisely what the currently-unconsumed
`prototypeMutationVersion` from P0-1 should become, once it is only bumped on *real*
prototype-chain mutation rather than on every value allocation. Then route `InvokeMethod`'s
callee read through the cache.

**Risk: medium.** Correct invalidation is the whole problem: `Object.setPrototypeOf`, a
`__proto__` write, shadowing a prototype method with an own property, and prototype mutation
anywhere in the chain must all invalidate. A per-chain generation counter is the conservative
choice for a first cut.

**Status: implemented.** A cache entry is now either an own-slot entry (null holder, guarded
by the receiver's shape id alone) or a prototype entry carrying the holder, the holder's slot,
and three guards:

1. **the receiver's shape id** — "nothing here shadows the key". Sound because a shape-mode
   object's tracked keys are exactly its own named properties: any untrackable addition
   abandons the shape. Dictionary-mode and empty shapes are refused outright, since both ids
   are shared by every object in that state.
2. **the receiver's immediate prototype, by reference** — two receivers can reach the *same*
   shape id and still have different prototypes (`Object.create(a).v=1` versus
   `Object.create(b).v=1`), which no mutation counter would ever notice. Without this the
   cache silently returns the wrong object's property; it is covered by
   `TwoReceiversSharingAShapeButNotAPrototype_StayDistinct`.
3. **the global prototype version** — every `[[SetPrototypeOf]]` and every property mutation
   on an object used as a prototype publishes to it. Deliberately coarse: one prototype
   mutation anywhere retires every prototype entry in the process.

The chain walk during population uses the raw prototype link rather than the virtual
`GetPrototypeOf`, so warming a cache cannot fire a Proxy's `getPrototypeOf` trap, and it stops
at any holder that is not an exact `JSObject`. It also stops at a holder that *has* the key
but not as a plain tracked slot — tested by presence, not by value, so a holder whose own
value is `undefined` still shadows what is above it.

`JSValueBuilder.InvokeMethod` now routes the callee read of `o.m()` through the same cache a
bare `o.m` uses, for the non-optional-chain path and non-private keys (a private name's key is
a per-class-evaluation variable, so caching it would only drive the site megamorphic).

---

### P1-3 · There is no store (put) inline cache — **implemented**

`CachedIndex` exists only for reads. Every property write performs a full generic lookup.
Once P1-1 restores shapes on the write path, a monomorphic store cache
(`shapeId → slot`, plus a shape-transition cache for the "adds a new property" case) becomes
straightforward and is worth roughly what the read cache is worth.

**Status: implemented for overwrites. The transition case is not, and is written up below.**

This item was left open once, with a note that a store has no single emission point the way a
read does — assignments are built as `IndexExpression` targets in several places, so lowering
them to a helper call meant changing all of them or intercepting index assignment inside
`ILCodeGenerator`. That reading was pessimistic. The sites do funnel through two helpers,
`CreateMemberAssignmentTarget` and `CreateMemberExpression`, and only the first of those
carries plain `obj.name = value`; the rest are compound assignments and destructuring forms
that need the target to stay assignable because they *read* it as well. Routing the simple
form is one eligibility predicate shared by two call sites, and the target does not have to
stay assignable there because nothing reads it back.

**Where a store's time actually went.** Measured before changing anything, against a
three-million-iteration loop whose own overhead was subtracted:

| | ns per operation |
|---|---|
| cached read `o.x` | 8.7 |
| store `o.x = i` | **29.6** |
| store to a longer, later-interned key | **39.8** |

The key-length sensitivity is the tell. `PropertySequence` is an `SAUint32Map`, a radix-4
trie, so a lookup costs about one step per two bits of the interned key id. The old fast path
walked it **twice** — `GetValue` to read the descriptor, then `Put` to write it back — and
then `TrackShapeDataProperty` re-resolved the key a third time through the shape's
`Dictionary<uint,int>` to find the slot it was about to write. On top of that every store went
through the virtual `JSValue.set_Item`, which resolves the ambient strict flag by invoking a
delegate that reads an `AsyncLocal<bool>`.

**What landed.**

*A store cache, `PropertyInlineCacheSite.Set(site, target, key, value)*`. The write twin of the
read cache: same bounded four-entry polymorphic table, same megamorphic retirement, and the
entry is just `(ShapeId, Slot)`. There is no prototype form — a write that resolves on the
chain either runs a setter or creates an own property, and neither is a slot write on a
holder.

*The hit path never consults strict mode.* This is what makes the change worth more than the
lookups it saves. The strict flag only decides how a **rejected** write is reported, and an
entry is only taken when the write is known to succeed, so a hit can skip the indexer
entirely — and with it the `AsyncLocal` read. A miss still goes through `target[key] = value`
verbatim, so rejection, the strict `TypeError`, and the primitive-assignment throw all behave
exactly as before. No strict-mode semantics were touched.

*A single descriptor lookup, written through the ref it already produced.* Both the cache hit
path and the generic fast path now do `ref var own = ref ownProperties.GetValue(key)` and
assign through that ref, instead of looking the node up again through `Put`.

*The frozen check is gone.* `IsFrozen()` was only reached once the property had already been
established as a writable data property — which a frozen object cannot have — so it could
never return true there. It is not a cheap test either: `ObjectStatus.Frozen` is never
actually set anywhere, so `IsFrozen()` falls through to enumerating own properties, elements
and symbols.

**The guard the shape does not give you.** A shape id answers *which slot*, never *may I write
to it*. `Object.defineProperty(o, 'x', { writable: false })` and `Object.freeze(o)` rewrite
attributes in place and deliberately keep the shape, because a slot records where the value
lives and read-only does not move it. So `TryWriteShapeSlot` reads the descriptor on every hit
and declines on `IsReadOnly` — which is also the lookup that supplies the attributes to
preserve, so it costs nothing extra. Without that, a frozen object would keep accepting writes
through a warm site.

**Sites that can never hit retire themselves.** The first cut installed an entry whenever the
key resolved to a tracked slot — including read-only ones, which the shape tracks like any
other data property. The entry was then consulted and declined on every subsequent store, and
`o.x = i` against a non-writable property measured *slower* than before the cache (178 → 195
ms). Two changes fixed it: an entry is only installed for a slot that is currently writable,
and a site that fails to install four times without ever having installed anything retires
itself. That second rule is what keeps a store through an inherited setter — ordinary in
class-based code — from paying a failed guard and a failed install forever.

**Measured**, fastest of seven runs against the same tree with only this change reverted. Each
row is three million stores unless noted; the empty-loop row is there so the loop's own cost
can be subtracted.

| | Before | After |
|---|---|---|
| *(empty loop, for subtraction)* | *118 ms* | *111 ms* |
| `o.x = i`, monomorphic | 207 ms | **154 ms** |
| the same with a longer property name | 228 ms | **141 ms** |
| three fields in rotation | 165 ms | **95 ms** |
| polymorphic site, three shapes | 367 ms | **262 ms** |
| shadowing an inherited data property | 233 ms | **162 ms** |
| read-modify-write (`t = o.x; o.x = o.y; o.y = t`) | 244 ms | **170 ms** |
| `this.x = v` through a method call | 674 ms | **595 ms** |
| store that runs an inherited setter | 687 ms | **603 ms** |
| `o.x += 1` — compound, keeps the old lowering | 256 ms | **220 ms** |
| computed `o[k] = i` — not cacheable | 185 ms | **177 ms** |
| non-writable target — site retires itself | 163 ms | 163 ms |

Net of the loop, that is **29.6 ns → 14.4 ns** per store (2.1×), and **36.6 ns → 10.2 ns**
(3.6×) for the longer property name. The long-name row is the one that matters for real code:
`x` is interned early and sits near the root of the trie, while an application's property
names do not.

The last three rows are paths that deliberately do not end up using the cache. They improve
only from the single-lookup change, or not at all, and none regress — which was the point of
the retirement rule.

**Not implemented: the shape-transition cache.** Creating a property — `var o = {}; o.x = i`
in a loop, or a constructor assigning its fields — still misses every time, because the entry
records the shape the object has *after* the store while the next fresh object arrives with
the shape it had *before*. Caching that needs an `oldShapeId → (newShape, slot)` entry plus
the `shapeSlots` growth, which is a different mechanism from the one here. Nothing regressed
by leaving it: fresh-object creation measured 73 → 67 ms purely from the generic path getting
cheaper, and a three-field constructor 173 → 177 ms, inside this container's noise band.

**Also out of scope, deliberately:** compound assignment (`o.x += 1`), update expressions
(`o.x++`), computed keys, `super`, optional chains and private names all keep the existing
lowering. Compound and update forms read the target as well as write it, so the expression has
to stay assignable; giving them a cache means splitting each into a cached read plus a cached
store, which changes the short-circuit forms (`||=`, `&&=`, `??=`) and is its own change.
`o.x++` is currently the most expensive of these at ~270 ms and is the obvious next item.

41 tests in `PropertyStoreCacheTests` split along the two halves this needs. That the hot
shapes hit: seven ways of creating the property, writes through `this`, shadowing an inherited
data property, a four-shape polymorphic site, and the two retirement rules. That a write can
still be refused or redirected *after* the site is warm: `writable: false`, `freeze`, `seal`,
redefinition as an accessor, a setter appearing on the prototype, `delete`, `preventExtensions`,
dictionary mode, Proxy receivers and Proxies in the chain, arrays, typed arrays, functions and
primitives, computed and array-index and private keys, `super`, plus that attributes,
enumeration order, JSON, read caches and prototype visibility all still see the write, and
that the base is evaluated once and before the value.

Full suite after the change: **7 032 tests, 7 026 passing, 0 new failures** — the same 6
pre-existing failures as every phase before it (5 ICU/locale-data dependent, 1 in
ModuleExtensions).

---

### P1-4 · The shape "fast path" is itself a dictionary lookup

```csharp
// Runtime/ObjectShape.cs
private readonly Dictionary<uint, int> slots;
public bool TryGetSlot(uint key, out int slot) => slots.TryGetValue(key, out slot);
```

`JSObject.TryReadShapeSlot` (`Runtime/JSObject.cs:185`) then performs, on every *hit*: a
`GetType()` call, a shape-id compare, an `IsDictionary` check, **a dictionary lookup**, a slot
compare, a bounds check and a null check. A validated inline-cache hit should be a shape-id
compare followed by an array index — the cached slot is what the cache is *for*; re-resolving
the key through a dictionary discards the benefit.

Two further structural costs:

- `TrackShapeDataProperty` writes the value into `shapeSlots` **in addition to** the
  `PropertySequence` entry (`Runtime/JSObject.cs:157`), so every tracked object stores each
  value twice and must keep the two in sync.
- Shapes are restricted to `GetType() == typeof(JSObject)` (`:132`, `:163`, `:172`, `:187`),
  which excludes `JSArray`, `JSFunction`, and every built-in exotic. Class instances happen to
  be plain `JSObject` and so qualify — but P1-1 disqualifies them anyway.

**Status: implemented for the hit path; the double storage remains.** `TryReadShapeSlot` is now
a shape-id compare, a bounds check and an array index — the key parameter is gone, along with
the `GetType()` call and the dictionary re-resolution. Sound because an `ObjectShape` is
immutable and its key-to-slot map fixed at construction, so equal shape ids necessarily agree
on which slot a key occupies; everything that could invalidate the mapping routes through
`AbandonObjectShape`, which swaps in the dictionary shape and changes the id. The
`ObjectShape.Empty` and `.Dictionary` ids are never recorded by a cache entry, and both carry
an empty slot array, so the bounds check rejects them anyway.

**Still open:** `TrackShapeDataProperty` continues to write each value into `shapeSlots` *in
addition to* the `PropertySequence` entry, so a tracked object stores every value twice and
has to keep the two in sync. Collapsing them to one storage is the remaining half of this item.
Shape eligibility is also still limited to exact `JSObject`, which excludes `JSArray` and
`JSFunction`.

**Original fix note.** Trust the shape id on the cache-hit path and index `shapeSlots`
directly (the shape id
already implies the key→slot mapping — that is the invariant a shape provides). Make the slot
array the single storage for tracked data properties. Then widen shape eligibility beyond
exact `JSObject` once the write path is fixed.

---

## 6. P2 — allocation on built-in and value paths — **mostly implemented**

All four items are implemented. P2-3 landed as a much smaller change than it was filed as, and
P2-2 landed only after the two reasons it had been declined were re-examined and both found
wrong; each carries a status note saying what changed. Working through the phase also turned up
two defects larger than anything it had scoped, written up in
[§6.5](#65--found-while-implementing-p2).

### P2-1 · `Array.prototype.push` allocates a full descriptor per element — **implemented**

`BuiltIns/Array/JSArrayPrototype.Modification.cs:183`:

```csharp
array.SetValue(arrayIndex, a.GetAt(index), array, true);
if (array.GetOwnPropertyDescriptor(CreateNumber(arrayIndex)).IsUndefined)
    mustSetLengthThroughProperty = true;
```

`GetOwnPropertyDescriptor` materializes a descriptor `JSObject` (via
`JSObjectCoreExtensions.PropertyToJSValue`) with four properties — solely to ask *did the
element land?* — and `CreateNumber(arrayIndex)` allocates a `JSNumber` per element just to be
the key.

Measured: **4 046 bytes per `push`**, versus 1 382 bytes for the equivalent `a[i] = v`.
`array-push` is the slowest scenario in the whole baseline.

**Fix.** Replace the probe with a descriptor-free existence check (`elements.TryGetValue` /
`HasOwnProperty(in PropertyKey)`) on the `uint` key directly. This is the same
"descriptor-free" theme as the `descriptor-free-has-property` P0 item already in
`eng/performance/ownership.json` — the pattern was not applied here. Audit the other built-ins
for the same shape: `GetOwnPropertyDescriptor(...).IsUndefined` used as a presence test.

**Risk: low.** Local to `Push`; gate on `test262-arrays.txt`.

**Status: implemented.** The probe is now `array.HasOwnProperty(arrayIndex)`, which answers the
same question off the element table with no allocation and stays virtual so an exotic subclass
would still be consulted. `array-push` fell from 3 803 to 2 660 bytes per element on the spot,
and to about 1 480 once the indexed-write defect in §6.5 was fixed as well.

The audit for the same pattern found no other instance in a hot loop —
`GetOwnPropertyDescriptor(...).IsUndefined` appears about a dozen more times, but all of them
are one-time bootstrap or registry checks.

---

### P2-2 · No small-number cache; every arithmetic result is a heap allocation — **implemented**

`BuiltIns/BuiltInsAssemblyInitializer.cs:119`:

```csharp
JSValue.CreateNumber = static v => new JSNumber(v);
```

Every arithmetic operation allocates, through a `Func<double, JSValue>` indirection. An empty
`for` loop costs **96 bytes per iteration**; `s = s + i` costs 128.

**Fix, cheapest first:**

1. Cache small integers (say −128…1024) and the common constants. `JSNumber` already has
   static `Zero`/`One`/`Two`/`MinusOne` fields that nothing on the arithmetic path consults.
   Loop counters and array indices hit this range constantly.
2. Replace the `CreateNumber` delegate on emitted arithmetic with a direct call to a static
   factory — `JSNumberBuilder.New` already emits `Expression.New(_ctor, …)` for
   compile-time-typed operands, but the `JSValue.Add`/`Subtract`/… runtime helpers all go
   through the delegate.
3. Longer term, unboxed `double` locals: `FastCompiler.ToNativeExpression`
   (`Compiler/Expressions/FastCompiler.VisitBinaryExpression.cs:141`) currently reports
   "is a number" **only for literals**, so `a + b` on two numeric locals never takes the
   native `double` path. Local numeric type inference would let whole expression trees stay
   unboxed. This overlaps the existing `tiered-unboxed-locals` P3 item.

**Risk:** (1) and (2) are low — but note `-0` must not be conflated with `0`, and `JSNumber`
identity must not become observable where the spec requires fresh values. (3) is a real
compiler change.

**Status: implemented, after an earlier decision to decline it. Both of the reasons for that
decision turned out to be wrong, and the record of them is kept below because the way they
were wrong is the useful part.**

**Wrong reason 1: "the payoff is narrow."** The earlier note reasoned that a cache only helps
values inside its range while the benchmark loops here count to millions, and put the benefit
at "roughly 20% of that scenario's allocation". That was inferred from the artificial
count-to-a-million loops rather than measured. Instrumenting the `JSNumber` constructor and
bucketing every value a workload creates gives the actual share falling in −128…1023:

| | in range |
|---|---|
| `fib(25)` | **100.0%** |
| `dromaeo-object-array` | 85.7% |
| reading a 256-element array | 75.1% |
| `JSON.parse`/`stringify` round trip | 61.5% |
| sorting 5 000 small integers | 66.9% |
| filling an array by index | 57.3% |
| `charCodeAt` in a loop | 43.0% |
| reading three object fields | 43.0% |
| `i % 100` accumulator | 33.4% |
| `for (i = 0; i < 1e6; i++)` | **0.1%** |
| floating-point accumulation | **0.1%** |

So the two scenarios the estimate was based on are the only two that see nothing. Everything
resembling ordinary code — indices, character codes, small counters, recursion depths — is
between a third and all of its number allocations.

**Wrong reason 2: "the cross-realm hazard has to be fixed first."** This one was right about
the mechanism and wrong about the conclusion. `JSPrimitive.ResolvePrototype()` does assign
`BasePrototypeObject = GetPrototype()`, and `JSNumber.GetPrototype()` does read
`%Number.prototype%` out of the *current* realm — but reading the ten members of `JSPrimitive`
carefully shows `ResolvePrototype()` is called immediately before **every** read of
`prototypeChain`. The field is a scratch variable refilled per access, not a cache, so an
instance shared between realms on one thread is already correct: it is re-derived each time
and never consulted stale. What is not safe is sharing one between *threads*, where the write
and the read can interleave. That is a data race, not a logic error, and it is removed by
scoping the table to the thread rather than by rewriting how primitives resolve prototypes.

With that, a cached number is exactly as exposed as an ordinary freshly-allocated one, which
is the right bar — every object this engine allocates already carries the same
single-threaded-per-context contract.

**One member did treat the field as a cache, and it was a real bug.** `GetMethod` resolved
only when `prototypeChain` was still null:

```csharp
if (prototypeChain == null)
    BasePrototypeObject = GetPrototype();
```

So whichever realm first looked a method up on a given primitive owned it for every realm
afterwards. That is already live for the eight process-wide `JSNumber` singletons — `Zero`,
`One`, `Two`, `MinusOne`, `NaN`, both infinities and `NegativeZero` are handed straight to
script — and caching 1 153 more values would have turned an obscure latent bug into a routine
one. Fixed to re-resolve like every other member, and pinned by a test that interleaves two
realms over the same cached instances.

**What landed.** `JSNumber.Create(double)` returns a `[ThreadStatic]` cached instance for an
integer in −128…1024, allocating otherwise. It is reached from all three places numbers are
made: the `CreateNumber` delegate (which the `Increment`/`Subtract`/`Multiply`/bitwise/modulo
helpers in `JSValue` already used), `JSNumber`'s own `AddValue`/`Negate` overrides, and
`JSNumberBuilder.New`, which now emits a call to the factory rather than `newobj`.

The range test runs on the double before the int conversion, so a miss costs two compares:
a loop counting to a million misses on all but its first thousand iterations, and the miss
path is the one that has to stay cheap. Negative zero is excluded explicitly — it survives
`(int)value` round-tripping because IEEE says `0 == -0`, and `Object.is` and `1 / x` both tell
the two apart.

**Measured — allocation**, byte-exact and reproduced identically across runs:

| | Before | After | |
|---|---|---|---|
| `dromaeo-object-array` | 24 193 992 B | **4 947 624 B** | −79.6% |
| reading a 256-element array | 33 016 808 B | **8 254 600 B** | −75.0% |
| filling an array by index | 44 807 968 B | **19 142 304 B** | −57.3% |
| `fib(25)` | 60 216 784 B | **33 027 664 B** | −45.2% |
| `charCodeAt` in a loop | 44 803 672 B | **25 537 688 B** | −43.0% |
| `i % 100` accumulator | 192 003 528 B | **127 936 424 B** | −33.4% |
| sorting 5 000 small integers | 17 209 944 B | **14 854 520 B** | −13.7% |
| `for (i = 0; i < 1e6; i++)` | 128 003 528 B | 127 905 096 B | −0.1% |

Gen0 collections fall with them: 11 → 7 on the accumulator loop, 2 → 1 filling an array,
1 → 0 reading one, 3 → 1 on `fib`.

**Measured — throughput, and how three earlier readings of it were wrong.** This is worth
recording because the measurement was harder than the change. Sequential A/B on this
container produced, in order: the cache is 18–29% *slower*; the factory indirection alone is
8–20% slower; and then re-running the unmodified baseline showed it had drifted 1–15% slower
than itself, monotonically, over the same period. The container gets slower as a session
proceeds, so any A-then-B comparison charges the drift to whichever ran second.

Interleaving the two arms inside one process removed the drift and produced the opposite
result — the cache faster on 12 of 13 scenarios, including a 6% "improvement" on a loop where
it allocates nothing and can only add work. That impossible number exposed the next flaw: the
cached arm always ran first within each pair. Alternating the order flipped the sign back
again.

What finally held up is an ABBA schedule with no `GC.Collect` between the timed runs inside a
block — the per-run collect had been charging each arm for cleaning up after the other:

| | miss rate | Δ time |
|---|---|---|
| `for (i = 0; i < 1e6; i++)` | ~100% miss | +1.5% |
| reading a 256-element array | ~25% miss | −0.7% |
| `fib(25)` | ~0% miss | −0.6% |
| `s = (s + i) & 1023` | 50% miss, 50% hit | +6.1% |

Those signs match the mechanism for the first time: a miss costs the two compares, a hit saves
the allocation, and the one outlier creates one of each per iteration. **No throughput claim
is made in either direction.** The effect is within what this container can resolve, and the
acceptance protocol in `Measurement.md` — two runs inside the configured band on the
release RID matrix — is what a throughput claim would need. The allocation column is
deterministic and is the result this item is closed on.

56 tests in `SmallNumberCacheTests`. The value side: eleven ways of producing negative zero
and distinguishing it from zero, non-integers, NaN, both infinities, magnitudes past 2^53,
every integer across both range boundaries, and the exact sum of the whole cached range. The
identity side: a primitive still refuses properties and still throws on a strict assignment,
`Object(5)` and `new Number(5)` still produce distinct wrappers, and Map/Set still key by
SameValueZero. The realm side: two contexts with different `Number.prototype` patches,
interleaved over the same cached instances, through both a method call and a dynamic property
read.

Full suite after the change: **7 032 tests, 7 026 passing, 0 new failures** — the same 6
pre-existing failures as every phase before it (5 ICU/locale-data dependent, 1 in
ModuleExtensions).

**Item (2) of the original fix list** is done as a side effect: the emitted path now calls
`JSNumber.Create` rather than `newobj`, which is what routing it through the cache required.

---

### P2-2 item 3 · Unboxed `double` locals — **implemented**

The last item of P2-2's fix list, and by a wide margin the largest win in this document.
`ToNativeExpression` reported "is a number" only for literals, so `a + b` on two numeric
locals never took the native `double` path — every intermediate value in a numeric expression
was a heap-allocated `JSNumber`.

**The analysis.** A `var` local is held in a CLR `double` when the compiler can prove it only
ever holds a number. `NumericLocalAnalysis` is an optimistic fixed point: every candidate
starts out assumed numeric and is dropped as soon as anything could give it another type, and
because dropping one invalidates the assignments that read it, the sweep repeats until nothing
changes. Starting optimistic is what lets a self-referential counter — `i = i + 1`, which
depends on itself — come out numeric at all.

**The hard part was not the type, it was `var` hoisting.** A `var` is observably `undefined`
from function entry until its initializer runs, and `undefined` is not a double. Rather than a
definite-assignment dataflow, the analysis requires the declaration to be a direct statement of
the function body (or the init of a top-level `for`) and requires no reference to the name to
appear textually before it. Together those mean the initializer has always run before any read:
a preceding top-level statement either completes — and none of them mentions the name — or
leaves the function, in which case nothing after it runs. A declaration nested inside an `if`
or a loop is not eligible, which is why `mandelbrot-ish` below barely moves.

**Storage, and why the change stayed small.** `VariableScope.Expression` becomes a *boxing
read* of the double, so the hundreds of places that consume a binding as a `JSValue` keep
working untouched; only the writes and the arithmetic reach for the raw storage. That choice
also made the change self-policing: an unconverted write path is an assignment to a method
call, which the IL backend rejects loudly. The first run failed 38 of 68 probes with
`Assignment target Call is not supported` — every one a write site, none a wrong answer.

**Measured**, fastest of 11 runs against the same tree with only this change reverted:

| | Before | After | | Allocation |
|---|---|---|---|---|
| `for (i…) s += i` | 64.9 ms | **3.1 ms** | 20.9× | 128 MB → **3 264 B** |
| `for (i…) s = s + i` | 72.5 ms | **2.9 ms** | 25.0× | 128 MB → **3 264 B** |
| float accumulation (`nbody`-ish) | 40.2 ms | **3.9 ms** | 10.3× | 67 MB → **3 264 B** |
| `t += i % 100` | 123.9 ms | **17.3 ms** | 7.2× | 128 MB → **3 264 B** |
| `s = (s + i) & 1023` | 128.9 ms | **55.7 ms** | 2.3× | 128 MB → 32 MB |
| filling an array by index | 44.4 ms | **19.6 ms** | 2.3× | 19 MB → **7 760 B** |
| `charCodeAt` in a loop | 62.2 ms | **43.5 ms** | 1.4× | 25.5 MB → 6.4 MB |
| nested `var` in a loop body | 692.9 ms | 611.8 ms | 1.1× | unchanged |
| reading a 256-element array | 21.8 ms | 24.1 ms | 0.9× | unchanged |

A counted loop over numeric locals now allocates **nothing at all** — the 3 264 bytes are the
`Eval` call itself, not the loop. This is the one item in the document whose timing is far
outside what the container's noise could manufacture, so unlike P2-2 and P2-3 the wall-clock
numbers are quoted as real; they still owe the release matrix before being *claimed* under
`Measurement.md`.

**Three things are deliberately left native-free**, because a CLR double operator would give
the wrong answer:

- **`<=` and `>=`.** The backend emits an ORDERED compare, which answers true when either
  side is NaN — every relational comparison involving NaN is false in JavaScript. Caught by a
  probe, not by the suite: the full run was green with these lowered natively and wrong.
  `<` and `>` do not have the problem and are what a loop test uses.
- **Bitwise and shift operators.** They run ToInt32 first, whose modulo-2³² wrapping a plain
  cast does not reproduce. This is why `s = (s + i) & 1023` keeps one allocation per iteration.
- **Speculative compilation.** Deciding whether a subtree is native is done on the *syntax*
  before anything is visited, because compiling a subtree and discarding it would leak what
  the visit allocated on the way — an inline-cache site per discarded attempt, among other
  compile-time state.

102 tests in `NumericLocalTests`, split the same way the risk is. That the right shapes
specialize: counted/while/do-while loops, prefix and postfix update in value and statement
position, all twelve compound assignment operators, and arithmetic chains. That the awkward
values survive: every relational comparison against NaN, both zeroes, the infinities, `%` with
a negative left operand, `%` and `/` by zero, `2 ** -1`. That a specialized local is
indistinguishable from an ordinary number when it escapes — into an array, an object, JSON, a
method call, a string concatenation, a comparison against a member or a string. And that the
analysis refuses everything it must: a name later holding a string, object, null or undefined;
a name read before its initializer; a `var` declared inside an `if`; closures, `with`, direct
eval; for-in and for-of heads; parameters; and `delete`/`typeof` on the binding itself.

Full suite after the change: **7 190 tests, 7 184 passing, 0 new failures** — the same 6
pre-existing failures. Two runs during this work each showed one *additional* failure
(`Issue709Tests`, then `EngineModuleImportBindingTests`), both a `NullReferenceException` in
identifier resolution inside a `body-:0,0` frame, both passing in isolation and neither
reproducing on a re-run. That is the signature of the shared-mutable-static sentinel bug in
the interning maps recorded in §6.5 — one instance of which was fixed there — so a third copy
of that pattern is likely still in the tree. It is pre-existing and unrelated to this change,
but it is now visible often enough to be worth hunting down on its own.

**Found after it landed: an assignment's value was a raw double.** A scalar-replaced local
lives in a CLR `double`, so assigning to it produces a double-typed expression. In statement
position that is the whole point; everywhere else it is wrong, because an assignment is an
expression and its value is the assigned value. Every consumer of it got handed a raw double
where a `JSValue` was required, and the CLR rejected the resulting method outright —
`var r = (n = 5)` threw `InvalidProgramException`, and `f(n = 5)`, `return (n = 5)` and
`if ((n = 5))` failed the same way, the last two surfacing as a `NullReferenceException` where
the mismatch reached a null local first. Compound assignment had it too: `var r = (n += 1)`,
`n *= 2`, `f(n -= 4)`.

The 102 tests above did not catch it because they exercise the specialized shapes in statement
position, which is exactly where the lowering is correct. `var r = (n++)` always worked —
`InternalVisitUpdateExpression` already boxed its result — and that asymmetry is what should
have been the tell.

Fixed in `8228b0da` by boxing the result and only the result, with a one-shot hint marking the
positions where the value is provably discarded (an `ExpressionStatement`, or a `for` update
clause, whose expression *is* the assignment). There the store stays an unboxed double, so the
hot path is byte-identical to the numbers above.

**Still open.** A `var` declared inside a block or loop body is not eligible, which is the
gap `mandelbrot-ish` sits in and would need definite-assignment analysis to close. Parameters
are never specialized, so a numeric function argument stays boxed. And `let`/`const` are
excluded to avoid reasoning about TDZ. (The nested-function gate was the fourth item on this
list; it is closed below.)

### The eligibility gate — **narrowed from "has a closure" to "that closure names this binding"**

Measured while benchmarking P3: `IsScalarReplacementEligible` rejected a function containing
*any* nested function, and "any" was literal — a declaration, a function expression or an arrow;
referenced or never referenced; written before the loop or after it. All six shapes measured the
same, and none of them can capture a counter they never mention.

| enclosing function contains | per loop iteration, before |
|---|---:|
| nothing else | 0.8 B |
| `function f(){}` | 96.9 B |
| `var f = function(){}` | 97.0 B |
| `var f = () => 1` | 96.9 B |
| `function f(){ return 1; }`, never referenced | 97.0 B |
| `function f(){}` written *after* the loop | 96.9 B |

That is most real code: a counted loop in a function that also defines a helper boxed its
counter every iteration, which is why the P3 floors in §7 sit at ~96 B/iteration rather than
zero.

**Status: implemented.** A nested function is now scanned rather than treated as a wall. Every
name it mentions is excluded from scalar replacement; the rest of the function's `var`s are
unaffected. A nested function that can reach a name it does *not* mention — one containing a
direct eval, a `with`, or a `debugger` — still disqualifies the enclosing function outright, as
does everything else the original gate refused.

The scan is deliberately cruder than a real free-variable analysis: it cannot tell a capture of
the outer `i` from the nested function's own parameter `i`, or from a property named `i`. That
costs a little specialization and buys the property that matters, which is that capturing a
binding requires naming it — so collecting every name is sound by construction, and it stays
sound as the AST grows node types.

Interleaved ABBA, **one scenario per process**, eight runs per arm, medians:

| | before | after | | allocation |
|---|---:|---:|---:|---|
| `s += i` ×1M | 85.7 ms | 2.8 ms | **−96.8%** | 127.9 MB → 6.0 KB |
| `s += i*i/3-i*2+1` ×1M | 213.4 | 2.8 | **−98.7%** | 287.9 MB → 6.0 KB |
| nbody-ish ×300k | 49.6 | 6.9 | −86.1% | 67.1 MB → 6.0 KB |
| `s += i%256` ×200k | 26.2 | 3.1 | −88.4% | 25.5 MB → 6.0 KB |
| `a[i%256] = i%256` ×200k | 40.9 | 18.4 | −55.1% | 19.1 MB → 10.5 KB |
| `charCodeAt` ×200k | 55.7 | 36.1 | −35.2% | 25.5 MB → 6.4 MB |
| `a[i&255] = 1` ×200k | 31.0 | 21.5 | −30.5% | |
| `h(i)` ×500k | 118.5 | 87.3 | −26.3% | 75.9 MB → 44.0 MB |
| `s += a[j]` ×205k | 18.8 | 14.9 | −20.7% | |
| `a.push(1)` ×200k | 172.5 | 157.8 | −8.5% | |

Every scenario improves; the arithmetic loops stop allocating altogether.

**One scenario per process is load-bearing, not fussiness.** Measured the ordinary way — several
scenarios in one process, each with its own `JSContext` — the same build reported `s += a[j]` as
**+122% slower** and `a[i&255]` as **+98% slower**, stably, across repeated interleaved runs.
Both are artifacts: earlier scenarios leave the process warmed in ways that flatter whichever
build runs them, and no amount of ABBA within one process removes it, because the contamination
is in the process rather than in the order. I had those numbers written up as real regressions
before splitting the harness. This is the same failure that made P2-2's first three measurements
wrong, in a new disguise.

#### Tests

The behavioural half is nearly worthless here — a capture that gets scalarized still computes the
right answer, because the lambda rewriter boxes captured locals and the numeric analysis
independently rejects any name a nested function writes a non-number to. Removing the exclusion
entirely leaves every *behavioural* test green, including forty-five hand-written capture shapes.
So the tests assert the **counts** (`CompilerSpecializationDiagnostics`), which is what the rule
actually changes:

- a nested function naming neither local leaves both specialized (fails if the old gate is restored);
- a nested function naming one local leaves exactly the other specialized;
- a nested function containing eval/`with`/`debugger` still refuses everything;
- a name reached through any of twelve containers — variable initializer, object literal, accessor,
  switch clause, try block, catch block, default parameter, destructuring default, template
  literal, a further nesting, class method, class field — is refused.

Both directions are mutation-tested: restoring the old gate fails five tests, and disabling the
exclusion fails two.

#### Is the exclusion needed at all?

Worth asking, because nothing above proves it. It was kept as a conservative measure — capture
works regardless, via boxing — and forty-five capture shapes could not tell the two settings
apart. So it was removed outright and the whole suite re-run.

It does not survive that. Beyond the two tests above it breaks a **pre-existing** one,
`Phase3CompilerSpecializationTests.ScalarReplacement_UsesRawLocals_OnlyWhenBindingsAreUnobservable`,
which asserts zero raw locals across four guarded snippets and reports one. The snippet is

```js
var b = (function () { var x = 3; return function () { return x; }; })()();
```

— a closure that **escapes**, returned and called after the enclosing function has already
returned. Every probe written for this change called its closures while the enclosing frame was
still live, which is exactly why none of them could tell the difference; the case that can was
sitting in the suite from Phase 3, with the invariant in its name.

The value is still right without the exclusion (`b` is 3 either way), so this is not a
demonstrated miscompile. What it demonstrates is that the codebase already decided this
question — a binding a closure can observe does not go in a raw local — and that the deciding
case is one the obvious tests do not reach. The exclusion stays.

Full suite **7 241 tests, 7 241 passing, 0 failures**. test262 was extended for this change to
the areas it actually touches — `statements/function`, `expressions/function`,
`arrow-function`, `statements/for`, `statements/variable`, `statements/class` and
`expressions/assignment` — on top of the generator/async/eval/Error/call set: **7 324 passing,
42 failing**, and the 42 are the same 42 as before the change, cluster for cluster. The seven
added directories contributed 5 434 passing tests and no failures at all. One further test
(`statements/try/tco-catch.js`) timed out in the parallel run and passes three times out of
three on its own — a CPU-bound tail-call test hitting the 30-second limit under four workers,
not a regression.

---

### P2-3 · Dense element storage is 4× larger than it needs to be — **implemented**

`ElementArray` stores `JSProperty[]` for dense (packed/holey) arrays. `JSProperty` is
attributes + key + `get` + `set` + `value` — **32 bytes** — where a default-descriptor dense
array needs only the 8-byte value reference. A 1 000-element array occupies 32 KB instead of
8 KB, which is the difference between fitting in L1 and not.

`ElementArray` already tracks `hasCustomDescriptors`. When it is false the backing store can be
a `JSValue[]`, promoted to `JSProperty[]` on the first non-default descriptor. This is the
element-storage analogue of P1-4.

**Risk: medium** — touches every element read/write path. `test262-arrays.txt`.

**Status: implemented — but not as the dual representation described above.**

This item was deferred once, on two grounds, and both are worth recording because only one of
them survived contact with the code.

The first was a **priority** argument and it still stands: the indexed-write fix in §6.5 had
already removed the dominant term. Filling a fresh array cost about 1 350 bytes an element and
now costs ~145, of which ~96 is the loop counter's own `JSNumber`. So the backing slot is about
a sixth of what remains, and shrinking it is worth ~17% of a fill rather than the 4× the
framing above suggests. That is why this landed last in P2 rather than first, and why the
result below is a footprint win far more than a throughput one.

The second was a **feasibility** argument — that the `JSProperty` type leaks out of
`ElementArray` through `ref`-returning members, so the change would reach into `JSObject`'s
element paths too. That one was wrong in its details, and checking it is what made the item
small.

**What the deferral missed.** `Set` computes
`custom = property.Attributes != EnumerableConfigurableValue` and passes it to
`PrepareSlot(index, forceDictionary: custom)`. So a non-default descriptor does not get stored
densely with its attributes alongside — it moves the *entire array* to dictionary mode first.
The dense store was therefore **already** exclusively plain writable/enumerable/configurable
data properties, and had been all along. There was nothing to promote and no second
representation to add: the mode transition that a dual store would have introduced already
existed. `JSProperty[]` simply became `IPropertyValue[]`, and the descriptor is rebuilt on read
from the value plus the two facts the storage mode already implies.

Reconstruction has to be exact, so the write path checks it rather than assuming it. A dense
slot is taken only when the property is *reproducible from its value alone*:

```csharp
private static bool IsDenseRepresentable(in JSProperty property)
    => property.value != null
        && property.set == null
        && (property.get == null || ReferenceEquals(property.get, property.value));
```

The `get` clause looks odd and is the point of the check. Every `JSProperty` constructor derives
a data property's accessor as `value as IPropertyAccessor`, so a stored `get` is only ever the
value itself or null — those two cases are the whole reconstructible set, and two reference
compares decide it without a type test. Anything else (a real setter, a getter that is not the
value, a null value) falls to the dictionary, where it is stored in full. That is strictly more
conservative than the old `Attributes`-only test, so the compact store can never lose a
descriptor it should have kept.

The key is synthesized as the slot index. No element consumer reads it, and the previous behaviour was inconsistent anyway: `Put` stored key 0 while
`Set(key.Index, …)` stored the index, and array `shift`/`unshift` moved properties between
indices without rewriting it. Deriving it from the slot makes it right by construction.

**Where the `ref` really leaked.** `ElementArray.Get` returned `ref JSProperty`, and eight
call sites took it. Seven only read through the ref, so `Get` returns by value now; one of
those (`JSObject.Delete`) did not use the result at all. The eighth,
`JSObjectExtensions.AddProperty(uint, getter, setter)`, genuinely wrote through it, and was
rewritten to `Set` — which is a fix in passing, since writing through the ref bypassed
`hasCustomDescriptors` and left an array claiming default descriptors while holding an
accessor. The other ref-returning member, `Put(uint)`, turned out to have no callers at all;
it still forces dictionary mode, as documented.

Returning by value also removes a live hazard: on a miss, `Get` handed back
`ref JSProperty.Empty` — a **mutable static field** — so any caller writing through the ref
would have corrupted the shared "not found" sentinel for the whole process. That is the same
shape as the `StringMap` and `SAUint32Map` sentinel bugs recorded in §6.5.

**Measured.** Seven runs of each scenario, medians, against the same tree with only this
change reverted:

| | Before | After | |
|---|---|---|---|
| `new Array(1000)`, allocation per array | 33 640 B | 9 064 B | **−73%** |
| Fill a dense 200 k array | 35 981 256 B | 23 398 440 B | −35% |
| Allocate and fill `new Array(1000)` ×2 000 | 259 587 584 B | 210 435 584 B | −19% |
| `dromaeo-object-array` | 137 391 592 B | 119 007 592 B | −13% |

The per-array figure is the whole change in one number: 24 576 bytes saved is exactly
1024 × (32 − 8), the backing store and nothing else.

**On the timings: they are reported as unchanged, deliberately.** Medians moved between −21%
and +6% across the scenario set, but re-running the *unmodified* baseline twice gave
dromaeo-object-array at 121.1 ms and 134.0 ms and array-stress at 1 889 ms and 2 110 ms — ~11%
run-to-run variance on identical binaries, straddling every "after" number. Nothing in the time
column clears that noise floor in either direction, so nothing in it is claimed. The
allocation column is byte-exact and reproduced identically across both baseline runs, which is
why the table above is allocation only. A throughput claim needs the release matrix under
`Measurement.md`, not this probe.

**Left alone deliberately.** Sorting an array with holes writes `JSProperty.Empty` through
`Set`, whose attributes are `Empty` rather than `EnumerableConfigurableValue`, so it forces
dictionary mode and permanently disables the bulk-mutation paths for that array. That is
pre-existing, orthogonal to storage width, and changing it would change sort behaviour — it is
noted here rather than fixed under a storage item.

70 new tests cover it from both sides. 27 in `CompactElementStorageTests` pin the storage
invariant directly — that every non-default descriptor and every accessor pair leaves dense
mode, that a plain value in dictionary mode still counts as a default descriptor, that the
rebuilt descriptor carries the right attributes, key and derived accessor, that holes read as
empty on all five read paths, and that promotion preserves every value and its ordering. 43 in
`ElementDescriptorRoundTripTests` assert the same thing through JavaScript, where the rebuild
is actually observable: `getOwnPropertyDescriptor` over seven ways of producing a dense
element, descriptors surviving the move to sparse storage and the arrival of a custom
descriptor elsewhere, accessors round-tripping to values and back, `Object.keys` ordering,
`for…in`, `propertyIsEnumerable`, `fill`/`copyWithin`/`reverse`/`sort`, freeze and seal, and
that string exotics, typed arrays and mapped `arguments` are untouched.

Full suite after the change: **6 991 tests, 6 985 passing, 0 new failures** — the same 6
pre-existing failures as every phase before it (5 ICU/locale-data dependent, 1 in
ModuleExtensions).

---

### P2-4 · Strings are flat; repeated concatenation is quadratic — **implemented**

`JSString` wraps a plain .NET `string` (`BuiltIns/String/JSString.cs:17`), so `s += x` in a
loop copies the whole accumulated string every iteration. `dromaeo-object-string` allocates
**16 GB** and triggers ~660 gen0 / ~330 gen2 collections in a single run — the only scenario in
the baseline that reaches gen2 at all.

**Fix.** A concatenation rope (`JSConcatString` holding left/right and a length, flattened
lazily on first indexed access or CLR string materialization) is the standard remedy and what
every production engine does.

**Risk: medium-high** — `JSString` is load-bearing across the built-ins, and `KeyString`
interning, `DoubleValue` caching, and the `ToKey` fast paths all assume a flat backing string.
Worth scoping as its own change, after P0 and P1.

Re-measured before the change: `s = s + 'abcdefgh'` twenty thousand times allocated **160 KB
per concatenation** — 3.2 GB in total for a 160 KB result — and provoked ~915 gen1 and ~913
gen2 collections. It was the only workload in the set to reach gen2 at all, and unlike
everything else in P2 it was genuinely quadratic rather than merely wasteful, so the cost grew
with the program's data rather than with its instruction count.

**Status: implemented.**

A `JSString` now holds *either* a flat `string` or a pending `Rope` — never both — and the
`value` member changed from a field to a property that materializes on first demand. That
detail is what kept the change small: the roughly forty reads of `value` inside `JSString`
kept compiling unchanged, and the field was never visible outside its own file.

Design notes, in the order they mattered:

- **Ropes are left-leaning, and `Right` is always an already-flat string.** That is exactly the
  shape `s = s + x` produces. Joining a right operand that is itself pending flattens that
  side rather than nesting, which keeps `Flatten` a simple spine descent instead of a tree
  walk — worth the occasional extra copy for how much simpler it makes the invariant.
- **Flattening is iterative.** The spine is exactly as deep as the number of appends, so a
  recursive flatten would overflow the stack on precisely the workload this exists for. It
  walks the spine writing each segment into a `string.Create` buffer back-to-front, one
  O(total) pass. `AVeryDeepConcatenationFlattensWithoutOverflowingTheStack` pins this at
  100 000 appends.
- **A threshold of 64 characters.** A rope node plus its `JSString` costs on the order of a
  hundred bytes, so deferring a short join loses; `'Hello, ' + name` stays a plain copy. An
  accumulating string passes the threshold within a few iterations and is O(1) per append from
  then on.
- **`Length` never flattens** — the rope carries its own total. So `s.length`, the emptiness
  tests in the concatenation paths, and every internal range check stay free while a join is
  still pending.
- **Publication order.** The flat value is written before the rope reference is dropped, both
  with volatile writes, so a reader that observes a null rope is guaranteed to see the string.
  Dropping the rope is what lets the whole chain of intermediate nodes become garbage.

| | Before | After |
|---|---:|---:|
| `s = s + 'abcdefgh'` × 20 000 | 1 604 ms | **10.7 ms** (150×) |
| — allocation | 3.20 GB | **4.4 MB** (734×) |
| — gen0 / gen1 / gen2 collections | 926 / 915 / 913 | **0 / 0 / 0** |
| `script:dromaeo-object-string` | 4 733 ms | **1 662 ms** (2.8×) |
| — allocation | 15.9 GB | **1.5 GB** (10.6×) |
| — gen2 collections | 693 | **59** |

40 tests in `StringConcatenationRopeTests` assert that a deferred string is indistinguishable
from the flat one it stands for: indexing, slicing, searching, splitting, regex matching, JSON
round-trip, use as a property key, `===` against an eagerly built equal string, coercion and
relational comparison, `+` with numbers and objects, the `TypeError` on a Symbol, and that
extending one does not disturb it or any sibling derived from the same prefix.

---

### 6.5 · Found while implementing P2

None of these was in the plan. The first two are larger than everything P2 had scoped and were
found by measuring the array paths rather than by reading them; the third was found by chasing
an intermittent test failure that turned out not to be intermittent at all.

#### A removed key could come back holding null

Both radix maps — `SAUint32Map<T>` and `StringMap<T>` — keep each slot's keys ordered, so
inserting a smaller key displaces the node sitting there down to a child:

```csharp
var oldKey = node.Key;
var oldValue = node.Value;
node.Key = originalKey;
node.State = NodeState.Filled;
node.Value = default;
ref var newChild = ref GetNode(oldKey, true);
newChild.Key = oldKey;
newChild.Value = oldValue;
newChild.State |= NodeState.HasValue;   // ← unconditional
```

`RemoveAt`/`TryRemove` clear `HasValue` but deliberately **keep the node's `Key`**. So the
displaced node can be a *deleted* entry whose `Value` is already `default`, and asserting
`HasValue` on the relocated copy resurrected it as a live entry holding **null**. The next
lookup of that deleted key reported a hit with a null value, which surfaced far away as a
`NullReferenceException` on the caller's first dereference — in the engine,
`JSContext.ResolveIdentifierOrUndefined`, whose `globalVars` entries are removed when a
direct-eval binding is torn down. `Count` was wrong too: the resurrected entry was never
counted.

**It was not a race.** It presented as one — two different tests in two different assemblies
failing once each across seven full-suite runs, both passing in isolation — which is what the
two earlier sentinel bugs in this same file had looked like, so it was initially filed as a
third instance of that pattern. It is not. Whether the displacement happens at all depends on
the numeric order of the interned keys and on which bindings have been deleted, so it moves
with test ordering and looks intermittent while being fully deterministic. A randomized
insert/remove/probe loop reproduced it on the **first** trial once the right question was
asked.

Fixed in three places (one in `SAUint32Map`, two copies in `StringMap`) by carrying the flag
across instead of asserting it. `StorageTests` now pins both maps with a randomized
insert/remove/probe sequence; both tests fail on the unfixed code.

---

#### Shrinking an array's `length` scanned the whole element table

`JSArray.DefineLengthProperty` deleted the tail by enumerating **every stored element**,
collecting the in-range indices into a `List<uint>`, and sorting it. The comment explained why:
walking `[newLength, oldLength)` directly would loop billions of times when shrinking a
sparse array from `2**32-1`. True — but the correction was total, so the scan also ran when the
range was a single index.

Every `pop()` shrinks the length by one. So each pop scanned and sorted the entire array, and
popping *n* elements was O(n²): **200 000 pops took 466 seconds**.

Fixed by picking the smaller side — walk the range when `oldLength - newLength` is at most the
number of stored elements, otherwise scan the stored elements as before. Both the dense-pop and
the huge-sparse-shrink cases are cheap, and the deletion order (high→low, halting at the first
non-configurable element) is unchanged.

**466 427 ms → 640 ms, a 729× improvement**, on a benchmark that also does 200 000 pushes.

#### Storing into an absent element did a descriptor round-trip — the indexed twin of P1-1

Exactly the defect P1-1 fixed for named properties, on the element path, and missed because P1
only looked at `SetKeyStringOnReceiver`. When the element is not already present,
`JSObject.SetValue(uint, …)` finds nothing own, recurses into the prototype, bottoms out at
`%Object.prototype%`, and re-enters `SetIndexOnReceiver` with the real array as a *foreign*
receiver — which allocated a `JSNumber` for the key and a four-property descriptor object, per
element.

The signature was unmistakable once measured: storing into an element that already existed cost
**0 bytes** over the loop's own overhead, while storing into a fresh one cost **~1 350 bytes** —
constant per element, independent of array size, so not a growth or resizing problem.

Fixed with `TrySetOrdinaryReceiverIndexedProperty`, mirroring the named version: it writes the
element table directly when the target's indexed `[[DefineOwnProperty]]` is the ordinary one,
and declines otherwise. Eligibility is a virtual, `SupportsOrdinaryIndexedWrite`, defaulting to
an exact-`JSObject` test and overridden by `JSArray` — so integer-indexed exotics, mapped
`arguments`, and proxies all keep the descriptor path, and each is covered by a test.

| | before | after |
|---|---:|---:|
| `new Array(n)` + fill, per element | 1 350 B | **145 B** |
| `[]` + fill, per element | 1 382 B | **182 B** |
| `[]` + `push`, per element | 2 680 B | **1 480 B** |

---

## 7. P3 — call-path structure — **premise disproved; the activation record was the cost, and is now a shadow stack**

The original item read:

> `JSFunction.InvokeFunction` wraps every call in four `using` scopes (`EnterRealm`,
> `EnterStrictMode`, `PushWithFallbackScopes`, `PushWithScopes`, plus a conditional
> `SuspendWithScopes`), a `JSEngine.Current as JSContext` type test, and a
> `try`/`catch (NullReferenceException)`/`finally` — the last of which also blocks inlining of
> the whole method. […] hoist a fast path for the overwhelmingly common case that skips
> straight to the invocation delegate, and keep today's full path as the fallback.

**That was built, measured, and reverted.** The scopes are not the cost.

### What the measurement showed

The fast path was implemented as described — a `CanUseFastInvoke()` guard (no legacy tracking,
no captured `with` scopes, realm already current, not script-host mode) selecting a stripped
invocation loop that kept only the executing-function record, the strict-mode scope, the
`NullReferenceException` translation, and the tail-call trampoline. Three runs of each
scenario, with and without, medians in ms:

| Scenario | Without | With | With (repeat) |
|---|---:|---:|---:|
| `f()` | 457 | 466 | 466 |
| `f()` strict | 402 | 371 | 357 |
| `o.m()` | 508 | 485 | **538** |
| `p.m()` inherited | 602 | 572 | **635** |
| arrow | 354 | 349 | **369** |
| `map` callback | 3 556 | 3 389 | 3 586 |
| recursion, depth 20 | 517 | 542 | 533 |

The results swing in both directions by more than the effect being looked for — `o.m()` was
485 ms on one run with the fast path and 538 ms on the next, against 508 ms without it. There
is no signal. Allocation was byte-identical, which is the tell: the scopes never allocated in
the first place.

Reverted rather than kept. It was ~100 lines duplicating the tail-call trampoline, which would
have to stay in sync with the general one forever, in exchange for nothing measurable.

### Why the premise was wrong

Two things, both checkable:

- `PushWithScopes(null)` and `PushWithFallbackScopes(null)` **return `null` without
  allocating**, and `SuspendWithScopes()` returns `null` unless a `with` is actually active.
  For an ordinary function all three are a null test. Only `EnterRealm` and `EnterStrictMode`
  do anything, and after P0-2 the strict scope writes only on a transition.
- A `try`/`finally` that does not throw is close to free on .NET. Its cost is in inhibiting
  some optimizations, not in per-call work — nothing like the "five nested scopes" framing
  implied.

### What the per-call cost actually is

An empty-bodied function still allocates **80 bytes per call**, and the body makes no
difference — `f(){}`, `f(){return 1}`, `f(){return undefined}` and `f(){var x;}` all measure
176 bytes against a 96-byte loop floor. It is fixed per-invocation overhead.

It is the `CallStackItem` that every compiled function body allocates on entry (emitted by
`CallStackItemBuilder`). Its layout accounts for the measurement exactly:

| | bytes |
|---|---:|
| object header | 16 |
| `Parent`, `NewTarget`, `context`, `FileName`, `directEvalBindings` (5 refs) | 40 |
| `Function` (`StringSpan`: string ref + 2 ints) | 16 |
| `Line`, `Column` | 8 |
| **total** | **80** |

So the call path's remaining cost is one activation record per invocation, not the scope
machinery around it.

### Why that is not a contained change

Frames are pushed and popped strictly LIFO (`Pop` restores `context.Top = Parent`), which
makes per-depth pooling look obvious. It is not safe as things stand:

- a **generator or async body suspends mid-frame** and resumes later, so its frame outlives
  the synchronous call;
- **direct eval captures the frame** — the compiler passes `scope.Top.StackItem` as the
  activation owner, and `RegisterDirectEvalBinding` stores bindings on it;
- `new.target` is handed off through it during construction.

Reusing a frame whose identity any of those still holds would corrupt live state. Shrinking it
instead does not help much either — every field is genuinely used, and the two obvious
candidates (`FileName`, `directEvalBindings`) are one reference each.

The real fix is to stop allocating an activation record per call at all: keep frame data on the
CLR stack and materialize a `CallStackItem` only when something actually asks for one (a throw
capturing a trace, a direct eval, a generator suspending). That is a redesign of the engine's
activation record, with its own test pass, and it should be filed as its own item rather than
carried under "call-path structure".

### The activation record — **first attempt: recycling rather than redesign**

The paragraph above proposed materializing frames lazily. That was not necessary. Frames are
now **rented from a free list** and returned by the `Pop` their own call already emitted, which
gets the same result — no allocation on the ordinary path — without moving frame data anywhere
or changing what a frame is.

Measured against a floor with the identical loop shape, so the loop's own cost cancels:

| | before | after |
|---|---:|---:|
| `f()` — no arguments | 80.0 B/call | **0.0** |
| `f(a)` | 136.0 B/call | **56.0** |
| `o.m(a)` | 136.0 B/call | **56.0** |
| arrow `(a) => a` | 136.0 B/call | **56.0** |

The 56 bytes that remain are argument passing, not the frame. Whole-scenario allocation falls
**7–65%** (`fib(27)` −65%, a 200-deep recursion loop −65%, a 1M-iteration method call −41%,
`new C()` −7%).

Throughput is **unchanged** — between −3.0% and +2.9% across nine scenarios with the median at
zero, measured ABBA-interleaved in one process against a runtime switch, for the reasons P2-2
documents. The first version was consistently ~1.5% *slower*: renting and releasing touched two
`[ThreadStatic]` fields each, and four thread-local lookups per call cost more than the gen0
bump they replace. Putting the head and the count in one holder object behind a single
`[ThreadStatic]` cell removed that. So this is an allocation win, banked at no throughput cost —
not a speedup.

#### What made it hard, and the two rules that came out of it

The deferral above was right that frames escape; it was wrong about which escape matters.
`new.target` is read straight out of the frame and never outlives it, and direct-eval bindings
are cleared by `Pop`. What actually breaks recycling is that **a frame's lifetime is not always
a synchronous span**:

- a generator or async body is pushed once, when primed, and then leaves the stack at *every*
  suspension without running its `finally` — stranding itself, and everything it had called,
  holding parent links to frames that later return;
- `JSGenerator.MoveNext` restores the `Top` it captured on entry, and the body it just ran may
  have popped that frame — so the engine can legitimately be sitting on a frame that has
  already returned.

Three defects came out of that, and every one of them appeared as a corrupted or looping parent
chain rather than as a wrong answer — two only as an intermittent hang, which is why the local
suite was no help at all: it stayed green through all three.

1. **Re-pointing a suspended body's parent on resumption** — the first attempt at keeping
   parent links fresh. It is wrong: an async body resumes from a microtask continuation whose
   current top is unrelated to its caller, so re-linking splices the body into a foreign chain
   and `Pop` then restores *that* frame as the top. 17 test262 async-generator files failed.
2. **A stale parent link followed into a reissued frame.** Once the object is handed to another
   call, a walk from a stranded frame leaves the generator into an unrelated live chain — and
   if that call was made from inside the resumed body, the chain closes into a cycle and every
   walker spins. **Rule: a link records the parent's push count and is followed only while that
   count still matches** (`CallStackItem.Caller`, which the five walkers now use instead of
   `Parent`). Reissuing a frame invalidates every link into it without having to find them, and
   a stale link reads as "no caller" — exactly what the pre-pooling engine reported, since `Pop`
   had nulled that frame's own `Parent`.
3. **A frame reissued while it was still the top**, via the `MoveNext` restore above. The next
   call then links itself under *itself*, and a one-element cycle passes any stamp check.
   **Rule: a released frame is never anybody's parent** — a dead top is treated as "no caller".

Generator and async bodies additionally opt out of recycling altogether. Theirs is the one frame
rented in one synchronous span and released in another — possibly on another thread, since an
async continuation may resume anywhere — and the pool is thread-local with a plain release-once
flag. That costs one un-recycled frame per generator instance, against zero per ordinary call.

Diagnosis was by bisection, not by reading: the failing test262 file was reduced to a standalone
script, run 20–40 times per configuration to get a failure *rate* rather than a verdict, and the
cycle was then dumped frame by frame. The first two hypotheses — thread migration, and the extra
field clearing in `Pop` — were both wrong and both were discarded on the numbers.

#### Evidence

- Full suite **7 212 tests, 7 212 passing, 0 failures**, including 13 new owned tests.
- test262 over generators, async generators, async functions, `eval-code`, `Error`, calls,
  `try` and the Intl surfaces: **2 412 passing, 51 failing, 0 timed out** — byte-identical to
  the pre-change baseline on the overlapping set, nothing newly failing, and no test newly
  timing out (a hang shows up there as a timeout, so that column is the one that matters).
- The specific file that exposed rules 2 and 3 — `built-ins/AsyncGeneratorPrototype/throw/
  this-val-not-async-generator.js` — went from 10/20 hanging to **40/40 clean**.
- Both rules are mutation-tested: deleting either makes exactly one owned test fail. That check
  was worth running — the JavaScript-level tests pass with *either* rule removed, because the
  corruption needs a job-queue interleaving the xUnit host does not reproduce, so the rules are
  also asserted directly against the frame API.

The lazy-materialization redesign is no longer needed for allocation. It would still be the way
to remove the frame's remaining *work* (the push/pop bookkeeping itself), which this does not
touch — but there is no measured cost there to remove.

### The activation record, again — **redesigned as a shadow stack; the bookkeeping was worth 11%**

The paragraph above was wrong, and measurably so. I expected the redesign to be neutral because
allocation was already zero and the remaining bookkeeping had measured as noise. It is not
noise: replacing the per-call object with an array slot is worth **3–15% of wall clock on
call-heavy code**, median ≈ 11%.

A frame now lives in `CallFrameStack` — a growable `CallFrame[]` owned by the context — and a
running call holds a `FrameToken` struct, which is an ordinary CLR local. Push is a bounds check
and some field writes; pop is `depth = slot`.

Interleaved ABBA at process granularity, two independent builds, ten runs each, medians:

| | pooled | shadow stack | |
|---|---:|---:|---:|
| `f()` empty, 1M | 144.1 ms | 125.5 ms | **−12.9%** |
| `f(a)`, 1M | 159.8 | 147.3 | −7.8% |
| `o.m(a)`, 1M | 172.1 | 153.1 | −11.1% |
| `p.m(a)` inherited, 1M | 197.9 | 182.6 | −7.7% |
| arrow, 1M | 136.1 | 115.1 | **−15.5%** |
| `fib(27)` | 97.0 | 84.6 | −12.8% |
| `map` callback, 300k | 166.7 | 161.4 | −3.2% |
| `new C()`, 500k | 278.3 | 264.6 | −4.9% |
| recursion depth 200 | 61.6 | 52.9 | −14.2% |

Allocation is unchanged at **0 B for an argument-less call** and 56 B for `f(a)` — the pool had
already taken that to zero, and this keeps it there without a pool.

Why it is faster, having predicted it would not be: the pooled path did two thread-local lookups
per call and wrote ten fields of a heap object, five of them references and so each behind a GC
write barrier, plus the free-list link. The array path writes a slot and bumps an integer.
Thread-local access and write barriers are individually small and collectively not noise. The
earlier "no measured cost" reading came from comparing pooling against *allocating*, which is the
wrong pair: it showed that recycling costs about what allocating costs, not that either is free.

#### What the array removes

Both rules recycling needed are gone, and gone by construction rather than by being enforced:

- A stale link cannot exist, because there are no links. The caller of frame *i* is frame
  *i−1*; the chain is the array's own order. So no push stamping, and no cycle to hang a walker.
- A dead frame cannot be handed to a second owner, because a slot has no identity to confuse.
  `Pop` names the frame to unwind *to*, so a call whose callees were stranded by a suspension
  still restores its caller's depth exactly, rather than depending on each of them to pop.
- `JSGenerator.MoveNext` saves and restores a **depth** instead of a frame reference, and
  restoring is defined to only ever unwind. That is what made the old design's worst bug
  possible — it restored a `Top` pointing at a frame the body had already popped, which was then
  reissued and linked under itself.

Generator and async bodies still need a heap frame (`CallStackItem`, now reduced to just that
role): their slot does not survive a suspension, so their state lives off to the side and the
slot points at it. One allocation per generator instance, none per ordinary call.

#### A second, separable win

Resolving a name against the context walked the frame chain looking for direct-eval bindings, on
*every* such lookup. The stack now carries a flag saying whether any live frame has one, and
skips the walk when none does — which is almost always. Measured on its own it is worth **−8%**
on a native-builtin loop (`Math.max` in a 300k loop: 30.2 → 27.7 ms) and nothing anywhere else,
so it is reported separately rather than folded into the 11% above. It is an independent
optimization that the array made obvious, not a consequence of it.

#### Evidence

- Full suite **7 213 tests, 7 213 passing, 0 failures**, green on the first complete run of the
  redesign.
- test262 over generators, async generators, async functions, `eval-code`, `Error`, calls, `try`
  and the Intl surfaces: **2 412 passing, 51 failing, 0 timed out** — identical to the pooled
  design, nothing newly failing, nothing newly timing out.
- The three invariant tests were rewritten, because the two they replaced pinned rules that no
  longer exist. They now cover what the array can still get wrong: a suspendable frame losing
  its slot and retaking one under a different caller, unwinding refusing to grow back into
  abandoned slots, and popping past stranded callees.

---

## 8. Sequencing and exit gates

| Phase | Items | Expected | Gate |
|---|---|---|---|
| ~~**A**~~ | ~~P0-1, P0-3~~ | **Done** — 2.0–2.9× on call paths, 6× less call allocation | Full `dotnet test` green (6 824 tests, 0 new failures); 21 new owned tests. test262 manifests still owed |
| ~~**B**~~ | ~~P0-2~~ | **Done** — folded into the same change | `StrictModeFlowTests` covers every transition shape; test262 manifests still owed |
| ~~**C**~~ | ~~P1-1, P1-4~~ | **Done** — cache reaches constructor/class code (0 → ~100% hit rate); constructor-built objects 6 595 → 1 480 bytes | Full `dotnet test` green; `PropertyShapeCacheTests` asserts the hit rates and every staleness path. P1-4's double storage still open |
| ~~**D**~~ | ~~P1-2~~, ~~P1-3~~ | P1-2 **done** — inherited and class method calls hit the cache. P1-3 **done** — constant-key stores go through a store cache; 2.1× on a monomorphic store, 3.6× when the property name is not a one-character early-interned key. The shape-transition case is written up as not implemented | `PropertyShapeCacheTests` covers `setPrototypeOf`, prototype mutation, own-property shadowing, delete, freeze, accessor redefinition, polymorphic and megamorphic sites; `PropertyStoreCacheTests` covers the write side |
| ~~**E**~~ | ~~P2-1~~, ~~P2-2~~ | P2-1 **done**, plus the two array defects in §6.5 (729× on repeated `pop`, 9× on array fill). P2-2 **done** — small integers are minted once per thread; 33–80% less allocation on index- and counter-heavy code, and a latent cross-realm `GetMethod` bug fixed on the way. Throughput deliberately unclaimed. Its item-3 eligibility gate was later narrowed from "contains a nested function" to "a nested function names this binding" (§6.6), which is where the throughput turned up: counted loops in functions that also define a helper — most real code — went **−9% to −99%**, the arithmetic ones stopping allocation entirely | `IndexedWriteAndLengthTests` covers integrity levels, foreign receivers, exotics and length-shrink; `SmallNumberCacheTests` covers negative zero, the range boundaries, primitive identity and per-realm prototypes; `test262-arrays` still owed |
| ~~**F**~~ | ~~P2-3~~, ~~P2-4~~, ~~P3~~ | P2-4 **done** — repeated concatenation is no longer quadratic (150× on the accumulation loop, 10.6× less allocation on `dromaeo-object-string`). P2-3 **done** — a dense element is one reference instead of a 32-byte descriptor; `new Array(1000)` allocates 73% less, and the deferral's feasibility objection turned out not to hold. P3 **done** — the scopes it blamed cost nothing, but the 80-byte per-call `CallStackItem` they hid was the whole fixed cost of an argument-less call; the frame is now a slot in a context-owned array addressed by a struct token, so that call allocates **nothing** (`f(a)` 136 → 56 bytes, whole-scenario allocation −7% to −65%) and call-heavy code runs **3–15% faster** (median ≈ 11%) | `StringConcatenationRopeTests`, `CompactElementStorageTests`, `ElementDescriptorRoundTripTests`, `CallFrameStackTests` (frame-lifetime invariants asserted directly against the frame API); P3 additionally gated on a test262 run over generators/async/eval/Error/calls showing no new failure and no new timeout. `test262` string and array coverage and the full matrix per `Measurement.md` still owed |

Each phase adds an entry to `eng/performance/ownership.json` with its benchmark and semantic
owner, and closes only under the acceptance rules in `Measurement.md` — two runs inside
the configured band, on the release RID matrix, with allocation, latency and working set
reported together.

**Every phase A–F is implemented and covered by repository tests. None of them is *closed*.**
The numbers in this document come from an ad-hoc in-process harness on a shared container and
are not acceptance evidence. P1-1 in particular touches `OrdinarySetWithOwnDescriptor`, the
single most spec-sensitive path in the engine, and the local suite is not a substitute for
test262 there.

---

### 8.1 · Open items

Verified against the tree at `cdb2fd41` on 2026-08-01. Every row was checked against the
repository rather than inferred from the write-ups above. Rows marked **Done** were closed on
2026-08-01 as part of working through this list; the rest are still absent or unfinished.

#### Acceptance evidence

| Owed | State in the tree |
|---|---|
| Pinned test262 over `test262-arrays`, `test262-properties-proxy`, `test262-strict-mode`, `test262-realm-isolation` | **Done** — 8 313 tests, **zero engine failures**. Getting there took three tooling fixes; see [§8.2](#82--the-pinned-test262-run). The Annex B forbidden-extension paths P0-3 names are still not in any manifest |
| A `PropertyOperationBenchmarks` / `FunctionCallBenchmarks` comparison | Not run. Every file under `BenchmarkDotNet.Artifacts/results/` dates from 2026-07-16 and belongs to the phase 4–5 campaign |
| Two runs inside the configured band on the release RID matrix (win-x64, linux-x64, linux-arm64), reporting time, allocation and working set together, per [`Measurement.md`](Measurement.md) | Not collected. Everything above is one container, one machine, an ad-hoc harness |
| An `eng/performance/ownership.json` entry per phase, naming its benchmark and semantic owner — which the paragraph above this section requires | **Done.** Fifteen entries added, one per item rather than one per phase, since the file is item-scoped: `prototype-invalidation-on-allocation`, `ambient-strict-mode-writes`, `deferred-legacy-caller-arguments`, `shape-preserving-property-writes`, `prototype-lookup-inline-cache`, `property-store-inline-cache`, `shape-slot-direct-read`, `descriptor-free-array-push`, `indexed-write-fast-path`, `array-length-shrink`, `small-integer-cache`, `numeric-local-doubles`, `compact-dense-elements`, `string-concatenation-rope`, `call-frame-shadow-stack`. The pre-existing `tiered-unboxed-locals` (P3) is the same work as `numeric-local-doubles` and should be retired when the phase 0–5 evidence is next revisited — it was left alone rather than silently retargeted |
| Appendix A's permanent home for the probes under `Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks`, wired into `eng/performance/phase0.json` | Not created. Phase C's shape-hit-rate result is still exactly the one-off observation Appendix A warned against leaving it as |

Until the benchmark and RID-matrix rows exist, no phase can close, and nothing in this document
may be *claimed* under `Measurement.md`. The test262 row is the semantic half and it is
now green.

#### Engineering deliberately left behind

Each of these is argued where it was decided; they are collected here so the remaining work is
countable in one place.

| Item | What remains | Verified |
|---|---|---|
| **P0-2** | Strictness is still ambient. The scope writes only on a transition, but `JSValue`'s set accessors still resolve it through an `AsyncLocal<bool>`. The preferred fix — threading the compiler's static knowledge into the property-set helpers it emits, so the hot path reads nothing — is not started | `JSEngine.cs:223` |
| **P1-3** | The shape-transition cache. Creating a property still misses every time; there is no `oldShapeId → (newShape, slot)` entry anywhere in `Runtime` | no match in `Broiler.JavaScript.Runtime` |
| **P1-3** | `o.x++`, `o.x += 1`, computed keys, `super`, optional chains and private names keep the old lowering. `o.x++` was measured the most expensive of them and is the obvious next item | §5 P1-3 |
| **P1-4** | The double storage. `TrackShapeDataProperty` still writes each value into `shapeSlots` *as well as* the `PropertySequence` entry, so a tracked object stores every value twice and has to keep the two in sync | `JSObject.cs:97`, `:188` |
| **P1-4** | Shape eligibility is still `GetType() == typeof(JSObject)`, so `JSArray`, `JSFunction` and every built-in exotic are excluded | `JSObject.cs:203` |
| **P2-2 item 3** | A `var` declared inside a block or loop body (needs definite-assignment analysis), function parameters, and `let`/`const` (TDZ) are all ineligible | §6 |
| **P3** | Lazy frame materialization. The shadow stack removed the *allocation*; the push/pop bookkeeping itself is untouched. There is no measured cost there, so this is a candidate rather than a task | §7 |

#### Two gaps — **filed, but not where P0-2 and P1-1 said to file them**

P0-2 says the async/generator strict-mode gap "belongs in the compliance failure manifest
rather than here", and P1-1 says the same of `Reflect.set` giving a receiver's new property the
*base's* attributes instead of the all-true set `CreateDataProperty` mandates. Both are still
live at `cdb2fd41` — a probe through the script host reproduces each in a few lines — but the
instruction cannot be followed as written, for two separate reasons.

`scripts/compliance/test262-failures.txt` is **generated** by `.github/workflows/test262.yml`
from a run's own results. A hand-written entry would be overwritten by the next run, and an
entry only appears there if some test262 file actually fails.

For the strict-mode gap none does: the four gating manifests do not reach generator or async
bodies at all. For `Reflect.set` the reason is sharper — **no test262 file at the pinned ref
reaches the case.** `Reflect/set/creates-a-data-descriptor.js` does exercise the receiver path,
but with an *empty* target, where step 4.d of OrdinarySet supplies the default all-true
`ownDesc` and the engine is correct; the deviation needs a target that already has an own data
property with non-default attributes, so that step 5.f runs with a real `ownDesc` to copy from.
`Reflect/set/different-property-descriptors.js` covers only an accessor on the receiver. The
engine passes every file in `Reflect/set/`.

So both are pinned by repository tests instead, which is what "cannot change silently" actually
requires here:

- `StrictModeFlowTests.KnownGap_AsyncAndGeneratorBodiesDoNotEnterRuntimeStrictMode` (already
  existed);
- `ReflectSetReceiverAttributesTests` (new, three tests): the gap itself, the value half that
  is already correct, and the empty-target case test262 does cover — the contrast that
  localizes the defect to attribute propagation rather than to `CreateDataProperty`.

Each asserts today's wrong answer deliberately, and names what the expectation becomes when it
is fixed. Neither is a performance item; they are recorded here only because this document is
where they were found.

#### Reproducing the green suite

The subsection below reports 7 199 tests, all passing. The tree has grown since; a full
`dotnet test Broiler.JS.slnx -c Release` on 2026-08-01 runs **7 284 tests across 13 projects,
7 281 passing**, three of which are the `ReflectSetReceiverAttributesTests` added above. The
three failures are host-environment rather than engine defects, and none is in a path this
document touches:

- `ReproTests.Repro` — a debugging leftover that appends to a hardcoded `D:\Broiler.JS\`
  path and asserts nothing;
- `Issue838Tests.EpochToStringStillRendersInUtcContainer` and
  `…ToStringSerializesNegativeYearWithSignedFourDigitYear` — both assume a UTC host.

Worth knowing before attributing either to a change here. Note also that
`Broiler.JS/BroilerJS.sln` cannot restore — it references `Broiler.Regex` and
`Broiler.Regex.Tests` at paths that do not exist — so `Broiler.JS.slnx` at the repository root
is the solution to run.

---

### 8.2 · The pinned test262 run

Run on 2026-08-01 at `cdb2fd41`, suite ref `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`,
`Release` script host, 8 workers, 30 s per-test timeout:

```sh
python scripts/compliance/run_test262.py --path-file scripts/compliance/test262-<name>.txt \
  --suite-root <pinned checkout> \
  --broiler-dll Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll \
  --max-workers 8
```

| Manifest | Executed | Passed | Failed | Skipped | Timed out | Engine failures |
|---|---:|---:|---:|---:|---:|---:|
| `test262-arrays` | 3 160 | 3 134 | 17 | 0 | 9 | **0** |
| `test262-properties-proxy` | 3 988 | 3 950 | 38 | 13 | 0 | **0** |
| `test262-strict-mode` | 1 066 | 1 040 | 26 | 27 | 0 | **0** |
| `test262-realm-isolation` | 99 | 96 | 3 | 4 | 0 | **0** |
| | **8 313** | **8 220** | **84** | **44** | **9** | **0** |

**Every one of the 84 failures needs `$262`** — `createRealm`, `detachArrayBuffer`, or a
harness include that uses one (`detachArrayBuffer.js`). The raw script host does not provide
that object, and the runner already computes `isScriptHostVerifiable` for exactly this reason;
it just does not *filter* on it outside `--all-script-host-verifiable`, so with an explicit
manifest they run and fail. **All 9 timeouts are already tracked** — they are lines 7–15 of
`scripts/compliance/test262-failures.txt`, nine for nine, the integer-limit `slice`/`unshift`/
`reduceRight`/`toReversed` cases CI has carried for a while. Nothing here is attributable to
any change in this document.

That is the semantic gate P1-1 asked for. `test262-properties-proxy` is the one that matters
most: it covers `OrdinarySetWithOwnDescriptor`, receiver mismatch, and the Proxy trap ordering
that P1-1 warned "the local suite is not a substitute for", and all 3 988 of its tests pass.

#### Three tooling defects, all of which had to be fixed before any of this could run

None of these are engine defects, and finding them is most of what the run cost.

1. **Two manifests could not run at all.** `test262-strict-mode.txt` named
   `test/built-ins/FunctionPrototype` and `test262-realm-isolation.txt` named
   `test/built-ins/globalThis`. Neither directory has ever existed in test262 — the real paths
   are `Function/prototype` (already covered by the `Function` entry, so the line was simply
   deleted) and `global`. `_expand_path` raises `FileNotFoundError` on the first missing path,
   so **both manifests aborted before running a single test.** This is the concrete reason no
   run against them was ever recorded, and it means these two have never gated anything.
2. **`_FIXTURE.js` files were executed as tests.** test262's INTERPRETING.md says files ending
   in `_FIXTURE.js` are not tests — they are modules other tests import, and some are
   deliberately un-runnable alone. `list_paths` already excluded them via
   `include_fixtures=False`, but directory expansion for `--path-file` did not, so
   `test262-realm-isolation` reported three phantom failures under `ShadowRealm/…/importValue/`.
   CI uses `--path-file` too, so this affected it as well.
3. **The assembled script was written with newline translation.** `read_text` opens test
   sources with `newline=""` and a comment explaining that the
   `Function.prototype.toString` line-terminator tests assert on exact source bytes — but the
   `NamedTemporaryFile` that writes the assembled script back out used the default, so on
   Windows every `\n` became `\r\n`. That turned an LF test into CRLF and a CRLF test into
   CR-CRLF, failing both; the CR-only test has no `\n` to translate and passed, which is what
   made the pattern legible. Invisible on the Linux CI, where `os.linesep` is already `\n`.

Defects 2 and 3 are why the first pass of this run reported five failures that looked like
engine defects and were not. The lesson is the one §8 already states in the other direction: a
failing test is a claim, and here the claim was about the harness.

**Still not covered.** P0-3 gates on "the Annex B forbidden-extension tests" and no manifest
names them; `test/annexB/built-ins/Function` and the `forbidden-ext/b2` paths would need
adding to `test262-strict-mode.txt`. That is left as an open item rather than done silently,
because widening a gating manifest changes what CI enforces.

---

### Fixed along the way, unrelated to any phase

`SAUint32Map<T>` held its not-found sentinel in a plain mutable static. `GetNode` returns it by
`ref`, including from the create path, so `Put`/`Save` could set `HasValue` and store a value
straight into it — after which every later miss on any map of that `T` reported a false hit
with stale contents. That surfaced as an intermittent `NullReferenceException` resolving a
global binding, from a completely unrelated test, only in a full parallel run. It is the same
defect `StringMap.Empty` already carries a fix for (issue #1428, the `body-:0,0` frame); this
second copy of the pattern had been missed. Now thread-local and reset at every `GetNode`
entry, matching the existing fix.

### A Debug build wrote a CLR stack trace to stderr on every JavaScript `throw`

Three places captured `new System.Diagnostics.StackTrace(true)` and dumped it to `Console.Error`
under `#if DEBUG`, unconditionally: `JSException.Throw`, and the `this[KeyString]` getters on
`JSNull` and `JSUndefined`. Nothing in the repository reads that output.

All three sit on *ordinary control flow*, not on failures. A `throw` caught by a `try` is how
JavaScript reports expected conditions; `x.y` on `undefined` is how feature detection asks
whether something exists. A script doing either in a loop paid, per occurrence, a stack walk
with `fNeedFileInfo: true` — which reads the PDBs — plus the formatting and the write.

The output was also actively misleading. It goes to stderr, where a tool or a human reasonably
reads any output as a failure signal, and it is large enough to push the real result out of
anything piped through `head`. Diagnosing an unrelated problem, I twice concluded from it that
`try`/`catch` was broken in Debug builds. It is not: with stderr separated, the same probe
prints `caught 1` / `caught 2` / `tf` and exits 0. Two rounds of bisection went into a defect
that did not exist.

It is now behind `JSException.LogThrows`, defaulted from `BROILER_LOG_THROWS=1`, still inside
`#if DEBUG` — the diagnostic is genuinely useful when you are asking *where did this throw come
from*, which is why it stays reachable rather than being deleted. Release builds are unchanged;
they never compiled it.

### The six pre-existing failures — resolved; the suite is green

Every phase above reports "0 new failures" against a standing baseline of **6 pre-existing
failures** (5 ICU/locale-data dependent, 1 in ModuleExtensions). That baseline is gone: the
suite now runs **7 199 tests, 7 199 passing, 0 failures**.

One was a real defect. Five were tests asserting behaviour the engine is right to refuse, each
checked against the pinned test262 suite rather than against my reading of the spec — the six
deciding test262 files were run directly through `scripts/compliance/run_test262.py` and all
six pass.

**The defect.** `ModuleBuilder.ExportValue` marshalled its argument at *record* time
(`value.Marshal()`), which reaches `JSValue.CreateString` — a static delegate the BuiltIns
assembly wires in a `[ModuleInitializer]`. Building a module before anything had touched the
engine hit the unwired delegate and threw `NullReferenceException`. It now records the .NET
value and marshals in `AddModuleToContext`, matching its two sibling methods (`ExportType` and
`ExportFunction` both already deferred) and its `default:` case, which was already the intended
destination. Deferring also fixes a quieter problem: the conversion now happens against the
context the module is registered with, and `Type` and `JSFunctionDelegate` values reach their
explicit switch cases instead of falling through to a generic proxy.

**The five wrong expectations.** In each case the assertion is not merely unsupported — it
contradicts a test262 vector the engine passes:

- **`ru-Armn-SU` → `ru-Armn-AM`.** The likely-subtags lookup tries `<language>` before
  `und-<script>`, so a present language always wins and the answer is `ru-Armn-RU`.
  `complex-region-subtag-replacement.js` makes the script-conditional case `und-Armn-SU` →
  `und-Armn-AM` — with no language to lose to — and pairs it with `en-SU` → `en-RU`;
  `Locale/likely-subtags.js` pins the same ordering with `en-Arab` → `en-Arab-US`, not
  `en-Arab-EG`. Changing the lookup order to satisfy the old assertion would have broken both.
- **`{currency:'jpy'}` with no `style` → `"JPY"`.** `SetNumberFormatUnitOptions` sets
  `[[Currency]]` only when style is `"currency"`, so `resolvedOptions` omits the property
  entirely; `NumberFormat/prototype/resolvedOptions/basic.js` asserts exactly that with
  `verifyProperty(actual, "currency", undefined)`. The code is still *validated* — the
  replacement test keeps that half, which is what the original was reaching for.
- **`en-u-ca-hebrew` → `gregory`** (asserted twice). True when written; the formatting engine
  has since gained the Hebrew calendar and renders it correctly
  (`new Intl.DateTimeFormat('en-u-ca-hebrew', …).format(new Date(2017,11,12))` is
  `"Kislev 24, 5778"`). `resolvedOptions/calendar.js` *requires* `hebrew` to round-trip, so
  restoring the fallback would have broken conformance. Both tests now use an identifier that
  is genuinely outside the available set, and a new test pins the Hebrew rendering.
- **`fr-FR` with `hour12:true` → `h11`.** `hour12` selects the locale's preferred clock of the
  requested kind from CLDR's `<hours>` data (`[[hourCycle12]]` / `[[hourCycle24]]`), not a
  cycle derived from the locale default. `hourCycle-default.js` asserts the 24-hour clock is
  `"h23"` in every locale and the 12-hour clock is `"h12"` in every locale *except* `ja` —
  which is precisely the rule `Prefers11HourCycle` already implements.

The lesson worth keeping is the one from P2-2's measurement and the `NaN <= x` bug in the
unboxed-locals work: a failing test is a claim, not a verdict, and the pinned conformance suite
settles it faster and more reliably than reasoning from spec text. My first reading of
ECMA-402's `hour12` clause said the engine was wrong; the test262 vector says it is right.

**A seventh defect, found next to the currency one.** Reading the currency rule turned up its
mirror image three lines below: `resolvedOptions` reflected `unit` *unconditionally*, where
`SetNumberFormatUnitOptions` sets `[[Unit]]` only for style `"unit"`. So
`new Intl.NumberFormat('en', {unit:'meter'}).resolvedOptions().unit` returned `"meter"`, as did
the same option under `style:"percent"` and even under `style:"currency"`, where the property
then sat next to a live currency group. The sibling `unitDisplay` was already gated correctly,
and construction-time validation was already unconditional and correct — only the reflection
was wrong, so the fix is the one missing `style == "unit"` guard, with `unit` and `unitDisplay`
folded into a single block mirroring the currency one above it. No test in the tree covered it
in either direction; `NumberFormat/constructor-unit.js` and `constructor-unitDisplay.js` pin it
upstream, and three repository tests now cover the drop, the round-trip and the validation.

---

## 9. Explicitly out of scope

- **Parsing and compilation.** Fresh-context startup is 1.20 ms and `script:evaluation` runs in
  37 ms; neither showed up as a bottleneck. `ParserCompilerBenchmarks` already covers this.
- **A real JIT / tiered compilation.** The existing opt-in function tiering and the
  `Broiler.JavaScript.Portable` numeric subset stay as they are. Everything above is
  achievable in the current architecture.
- **Anything that trades conformance for speed.** Every item here is a
  same-observable-behaviour change. Where the spec-visible surface is genuinely at risk
  (P0-3, P1-1, P1-2) the risk is called out and the gating manifest named.
- **Security.** Broiler.JS is not a sandbox, and none of this changes that.

---

## Appendix A — reproducing the measurements

Each scenario is `ctx.Eval`'d once to warm and compile, then measured on a second evaluation.
Timing is `Stopwatch`; allocation is `GC.GetAllocatedBytesForCurrentThread()` deltas after a
forced gen2 collection. Cache behaviour is read from
`PropertyOptimizationDiagnostics.Snapshot()` after `Reset()`.

```js
// loop-empty            (3M)  var s=0; for (var i=0;i<3000000;i++) { s=i; } return s;
// arith-add             (3M)  var s=0; for (var i=0;i<3000000;i++) { s=s+i; } return s;
// prop-own-get          (3M)  var o={x:1,y:2}; ... s=s+o.x;
// prop-own-set          (3M)  var o={x:1};     ... o.x=i;
// fn-call               (1M)  function f(a){return a;}            ... s=s+f(i);
// fn-call-strict        (1M)  'use strict'; function f(a){return a;} ...
// closure-call          (1M)  var k=1; var f=function(a){return a+k;} ...
// proto-method-call     (1M)  function P(v){this.v=v;} P.prototype.get=function(){return this.v;};
// class-field           (3M)  class C { constructor(v){this.v=v;} } ... s=s+c.v;
// builtin-call          (1M)  s = Math.max(s, i);
// array-rw              (1M)  var a=new Array(1000); ... s=s+a[i%1000];
// obj-alloc            (500k) last = {a:i, b:i+1, c:i+2};
// array-push           (500k) a.push(i);
// string-concat        (200k) s = 'x' + i;
```

Real-world scripts are the repository's own
`Broiler.JS/OtherTests/JIntPerfTests/Scripts/*.js`, each in a fresh `JSContext`, with the
Dromaeo harness stubs (`startTest`/`test`/`endTest`/`prep`) prepended — the same set
`JIntSmokeBenchmarks` uses.

The shape/inline-cache table in §5 comes from five 100k-iteration read loops differing only in
how the property was created, each in its own context, with the counters reset between runs.

A permanent home for these probes belongs in
`Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks` alongside the existing
`PropertyOperationBenchmarks` and `FunctionCallBenchmarks`, wired into
`eng/performance/phase0.json`, so that the shape-hit-rate assertions in §8's phase C become a
CI gate rather than a one-off observation.


---

# Part two — the Octane roadmap (phases 0–5, superseded)

> **The second of the two source plans.** It was `tests/octane/roadmap.md` in the aggregate
> repository until 2026-08-07, and it is here for the same reason part one is: it is a
> superseded plan, and the two are more useful together than one repository apart. It
> contributed the phase 0–5 structure, the ordering, and the metric —
> [`Roadmap.md`](Roadmap.md) is what those became.
>
> **Its diagnoses have been corrected there and the corrections are not back-ported.** Item
> 1-2 below, for instance, still names source *size* as the cause of the compiler's stack
> overflow and still cites a Mandreel failure that does not reproduce on linux-x64.
> `tests/octane/benchmarks.md` stays in the aggregate repository and is **not** superseded —
> it is the live per-benchmark reference, beside the harness it describes.

Companion to `tests/octane/benchmarks.md`, which describes what each
benchmark does and where Broiler's time goes. This document is the plan that
follows from it.

**"Smooth" is used in two senses here, and both are goals:**

1. **A smooth run** — all 17 scores, every run, no crashes, no timeouts, and a
   known noise band. **Phase 0 (§2) is implemented**; it owes only a workflow run
   to produce the baseline everything else is measured against.
2. **A smooth curve** — the per-suite deficit currently spans **45× to 4646×**, a
   ~100× spread. That spread is the finding: it says the losses are concentrated
   in two subsystems rather than spread evenly, and therefore that they are
   addressable in a defined order.

The second point is the whole argument for this plan. Because the suite total is
a **geometric mean**, flattening the curve and raising the total are the same
work: moving MandreelLatency from 14.5 to 1000 is worth more to the total than
tripling every score that is already above 300.

> **Scope discipline.** Octane was retired by its authors in 2017 precisely
> because engines began optimizing for its shapes. Every item below is justified
> by a *mechanism* that matters to real JavaScript, with the benchmark used as
> evidence that the mechanism is missing — never as the target. Items that would
> only move Octane are called out and excluded in [§7](#7-non-goals).

---

## 1. The metric

Track three numbers per run, not one:

| Metric | Last committed run | Target |
|---|---|---|
| **Geomean** over all 17 scores | 244 (est. with `0046`; 245 over the 12 that complete) | — |
| **Scores reported** | 12 / 17 | **17 / 17** |
| **Spread** = worst suite ÷ best suite, measured as ×-slower-than-Chromium | 4646 / 45 ≈ **103×** | **< 5×** |

The spread is the "smoothness" number and it is the one this roadmap is
organized around. A run where every suite is uniformly 150× off is a far
healthier engine than today's, at a similar geomean, because it means no single
subsystem is pathological.

All three are now emitted by `run-octane.mjs` into `results/<platform>/comparison.md` and
`comparison.json` (§2.4), so the trend comes out of the run rather than being
reconstructed by hand. The "last committed run" column above is stale in the way
§2.1 describes and will be superseded by the first Phase 0 gate run.

---

## 2. Phase 0 — make the run complete and repeatable — **implemented**

**Nothing else on this list can be measured until this is done.** Phase 0 was
mostly not engineering: most of the code already existed, and the item that
looked like the biggest blocker turned out to be already finished.

Status at 2026-08-01: **0-1 to 0-5 are done.** What Phase 0 still owes is the
one thing it cannot do from a checkout — a workflow run (§2.6).

### 0-1 · Land the pending `Broiler.JS` patches — **already landed**

An earlier draft of this roadmap claimed the three pending patches were blocked
on egress scope and that the pinned pointer did not carry them. **That was
wrong**, and the correction matters because it changes what the committed
results mean.

The pinned pointer **is** `cdb2fd41`, which *is* patch 0048's commit, and both
`7ef80c03` (0046) and `8228b0da` (0047) are its ancestors:

```sh
git ls-tree HEAD Broiler.JS                                  # → cdb2fd41
git -C Broiler.JS merge-base --is-ancestor 7ef80c03 cdb2fd41  # → yes
git -C Broiler.JS merge-base --is-ancestor 8228b0da cdb2fd41  # → yes
```

The pointer was bumped in `2d9f39ca` on **2026-08-01 11:45**. The committed
Octane results were generated **2026-07-31 20:28** — about 15 hours earlier. So
the five failures in `results/` are a **stale result set, not a stale pointer**.

The three patch files and their index rows have been deleted from `patches/`,
per that directory's own instruction to remove a patch once its pointer is
bumped; a short "recently cleared" table records what landed where.

Independent confirmation that the landing was clean: `ff819e06` refreshed
`tests/wpt-baseline/failed-tests.json` right after the bump, for a **net 36
fewer WPT failures** (50 removed, 14 added). Those patches changed `+`, `==`,
the `for` head and `eval` scoping, which is exactly the surface WPT exercises
indirectly — and the surface moved the right way.

**Remaining action: none in the tree.** See §2.6.

### 0-2 · Make the stack reserve the default in the shell — **already on**

Verified rather than changed. `Broiler.JavaScript/Program.cs` runs script-host
JavaScript on a thread it sizes itself and opts into the budget explicitly:

```csharp
private const int ScriptHostStackBytes = 16 * 1024 * 1024;
…
MaxStackUsageBytes = ScriptHostStackBytes - ScriptHostStackReserveBytes,
```

So the reserve is active in exactly the configuration the Octane workflow builds.
`JSContextOptions.MaxStackUsageBytes` still defaults to **0 (disabled)** for
embedders, which is correct: a host that does not control its JavaScript thread's
stack size cannot pick a number.

Why this matters beyond Crypto: Octane's harness is literally
`catch (e) { suite.NotifyError(e) }`, and .NET runs a catch handler as a funclet
*on top of* the frames it is handling. Without a reserve the handler has no
stack, its first call throws again, and the second throw escapes the `try` — so
**any** benchmark that overflows takes its whole suite's other benchmarks with
it.

### 0-3 · Record each suite's real time budget — **implemented**

`scripts/octane-suites.json` entries now accept an optional `timeoutSec`, and
`--timeout` became a **floor** rather than an override
(`suiteTimeoutSec()` in `run-octane.mjs` returns `max(global, suite)`), so a
debugging run can still widen everything at once without editing the manifest.

Set only where a measured duration needs it, at roughly 3× the observed time:

| Suite | Measured under Broiler | Budget |
|---|--:|--:|
| Mandreel | 313 s | 1200 s |
| zlib | 647 s | 1800 s |

Every other suite fits inside the 180 s default, and Chromium finishes all of
them in about 2 s. Before this, a local `--only Mandreel` run at defaults
reported a spurious `timeout`, and the full run only passed because CI was
overriding the global timeout to 1800 s — which also meant a genuine hang
anywhere else had 30 minutes to look like work.

The budget a suite ran under is now written into its log and its status record,
so a `timeout` verdict can be read without reconstructing the invocation.

### 0-4 · Quantify run-to-run noise — **implemented**

Every score in `results/` came from a **single run**, and the phases below will
be judged on 20–50% deltas. There was no basis for calling any delta real.

`--repetitions <n>` (default 1) now runs each suite n times and reports the
**median** score per benchmark plus the observed **spread**
(`(max − min) / median`, as a percentage). `--noise-band <pct>` (default 7.5,
matching the baseline profile in `eng/performance/phase0.json`) sets the
threshold above which a benchmark is flagged `⚠` in `comparison.md`. Both are
plumbed through `run-octane-benchmarks.sh` and exposed as a workflow input.

Three decisions worth knowing:

- **A default run is unchanged, byte for byte.** With one repetition the median
  is the sample, no stability data is emitted, no spread column appears, and the
  log keeps its `<suite>.log` name.
- **Each repetition keeps its own log** (`<suite>.rep1.log`, …), so a flake keeps
  the evidence of the run that failed instead of having it overwritten by the run
  that passed.
- **A suite is `ok` only if it was `ok` every time.** Anything else reports the
  first bad run and records `statusPerRepetition`; a suite that mixes verdicts is
  marked `flaky`. Averaging a flake into a pass is the failure mode this whole
  harness exists to avoid.

`comparison.md` now also leads with the three numbers from §1 — scores reported
out of the expected total, geomean, and the **spread** between the best and worst
suite — so the smoothness metric is produced by the run rather than computed by
hand afterwards.

Expect the two latency scores to be the noisy ones, and treat that as data: a
wide band on SplayLatency is itself a pause-distribution result.

### 0-5 · Check the code cache against CodeLoad's intent — **checked; no problem**

CodeLoad `eval`s the same jQuery and Closure source repeatedly, and the engine
has a `DictionaryCodeCache`. If that cache were hit across iterations the score
would be measuring cache lookup rather than compilation, and every Phase 1 number
taken from it would be meaningless.

It is not installed. In `Broiler.JavaScript/Program.cs` the line is present but
commented out:

```csharp
// DictionaryCodeCache.Current = new AssemblyCodeCache();
```

So `--script-host` compiles from source every time and **CodeLoad is a genuine
compile-throughput measurement**. Phase 1 can be judged on it directly.

Worth re-checking if that line is ever uncommented — the shell would then be
measuring something else, and this is not the kind of change that announces
itself in a benchmark score.

### 2.6 · What Phase 0 still owes: a run

Everything above is in the tree. The gate is not, because it cannot be produced
from a checkout:

**Run the Octane workflow and commit the refreshed results.** Until then the
committed numbers describe an engine that no longer exists, and the geomean, the
coverage count and the spread in §1 are all quoting a superseded run.

```text
Actions → Octane Benchmarks → Run workflow
  engines:         chromium,broiler
  timeout_seconds: 180          # Mandreel and zlib now raise their own
  repetitions:     3            # the first run that can distinguish signal
```

**Exit gate for Phase 0:**

1. **17 of 17 scores reported** — the five previously failing suites complete.
2. **No `timeout` status** at the default 180 s floor.
3. **A per-suite noise band on record**, and the suites that exceed it named.
4. `comparison.md` reporting coverage, geomean and spread.

Only then is there a baseline that Phases 1–4 can be measured against.

## 3. Phase 1 — the front end

**Targets: MandreelLatency (4646×), CodeLoad (371×), Mandreel (300×).** Owns the
two worst scores in the suite outright, and is the item with the clearest value
outside Octane: this is page-load time.

Owner assemblies: `Broiler.JavaScript.Parser`, `.Compiler`, `.BuiltIns`.

### 1-1 · Lazy function compilation — *the single highest-leverage item*

**Target.** CodeLoad and MandreelLatency are *designed* so this is the dominant
term — jQuery defines thousands of functions and calls almost none of them. A
large multiple on both is the expected outcome; if it is not, the measurement is
wrong before the change is. Mandreel's 313 s should fall substantially, and
Typescript and PdfJS should improve on load. **Steady-state execution does not
change at all** — do not expect Richards or DeltaBlue to move.

**Where.**

| File | Role |
|---|---|
| `Broiler.JavaScript.Parser/FastParser.Function.cs` | where a body is parsed today; needs a skip-with-errors mode |
| `Broiler.JavaScript.Compiler/Declarations/FastCompiler.CreateFunction.cs` | where the body is compiled eagerly |
| `Broiler.JavaScript.BuiltIns/Function/JSFunction.cs` | already carries `source` and already recompiles from it for tiering — the raw material for deferring is present |
| `Broiler.JavaScript.Engine` code cache | keyed on whole scripts; needs to key on function spans |

**Work.**

1. Pre-parse a function body far enough to find its extent and binding structure,
   without generating code.
2. Record source span + captured scope on the `JSFunction`.
3. Compile on first invocation, memoized per function-span.
4. Force eager treatment for the cases in *Risk* below.

**Risk — all four are spec-visible, and the first is the bulk of the work.**

- **Early errors must stay eager.** A syntax error inside a never-called function
  is still a `SyntaxError` at parse time. The pre-parser has to be a real parser
  for error purposes while skipping code generation. This is the part most
  likely to regress test262.
- **Scope capture.** A deferred body must compile against the scope chain as it
  was at closure creation, not at first call.
- **Direct `eval`** inside a deferred body can introduce bindings into enclosing
  scopes. The pre-parser must detect it and opt that function out.
- **Generators and async bodies** suspend mid-frame; confirm deferral composes
  with the `GeneratorRewriter` before assuming it does.

**Verify.** Full test262 over the four pinned manifests with **no new failure and
no new timeout** — the local suite is not sufficient for an early-error change.
Plus `ParserCompilerBenchmarks` before/after, and a CodeLoad number taken with
the code cache confirmed off (§2.5).

**Size: XL.** The only item here that is a genuine sub-project.

### 1-2 · Stop AST-recursive compilation from overflowing

**Target.** Mandreel, which today can die outright: `global_init` is one
generated function of **152,948 lines**, and compiling it has been observed to
overflow the CLR stack with a JavaScript stack only eight frames deep — the
compiler recursing over the AST, not the program recursing.

**Where.** `Broiler.JavaScript.Compiler/FastCompiler*.cs` visitors;
`Broiler.JavaScript/Program.cs` for the mitigation.

**Work.** Two steps, and the first is worth landing on its own:

1. **Mitigation (S).** Compile on a thread with a chosen stack size, exactly as
   the shell already does for *execution* (`ScriptHostStackBytes`, 16 MiB). Turns
   a crash into a slow success.
2. **Real fix (M).** An explicit worklist in the visitor for the shapes that nest
   without bound — long statement lists, deep binary-expression chains, giant
   `switch`. Compiler stack depth should be a function of source *nesting*, not
   source *size*.

**Verify.** A generated 200k-line single-function script compiles at the default
shell stack size without overflow. Add it as a compiler test fixture — this is
exactly the kind of thing that silently regresses.

### 1-3 · Reduce compile cost per byte — *only after 1-1*

**Do not start here.** If 1-1 lands, most source is never compiled at all and
the remaining throughput may not justify a pipeline change.

**Work.** Measure first, with the existing `ParserCompilerBenchmarks`, splitting
the cost three ways: parse, expression-tree construction, IL emission. The
measurement names the target; committing to one now would be guessing.

**Size: unknown by construction.** Re-scope after 1-1's numbers land.

---

## 4. Phase 2 — the call and property paths

**Targets: DeltaBlue (601×), Richards (433×), Box2D (170×).** These three are
dominated by the cost of making a call and reading a property. Every item below
is already named as open in
[`Archive.md` §8.1](Archive.md)
— this phase is a set of contained changes to structures that already exist and
already work on the sites they cover. **Best effort-to-value ratio on the list
after Phase 1.**

Owner assemblies: `Broiler.JavaScript.Runtime`, `.Compiler`, `.Engine`.

| # | Item | Where | Why it matters here | Size |
|---|---|---|---|---|
| **2-1** | **Shape-transition cache** — an `oldShapeId → (newShape, slot)` entry. Absent entirely: there is no such map anywhere in `Runtime` | `Runtime/ObjectShape.cs`, `Runtime/JSObject.PropertyStorage.cs` | *Creating* a property misses every time, so every constructor that builds an object field-by-field misses on **every field**. Richards' `TaskControlBlock`, DeltaBlue's constraints, RayTrace's `Vector`, Box2D's `b2Vec2` are all exactly this shape | M |
| **2-2** | **Widen shape eligibility** past `GetType() == typeof(JSObject)` | `Runtime/JSObject.cs` — `TryGetShapeSlot` | `JSArray`, `JSFunction` and every built-in exotic are excluded from shape tracking wholesale. **Start with `JSArray`** — it is on the hot path of five benchmarks | M |
| **2-3** | **Remove the double storage** | `Runtime/JSObject.cs` — `TrackShapeDataProperty` | Every tracked object writes each value into `shapeSlots` *and* the `PropertySequence`, storing twice and paying to keep them in sync. Pure removal | S |
| **2-4** | **Extend the store cache** to `o.x++`, `o.x += 1`, computed keys, `super`, optional chains, private names | `.Compiler` lowering; `Runtime/ObjectShape.cs` | All keep the old uncached lowering. `o.x++` measured the most expensive of them and is pervasive in Gameboy and Box2D | M |
| **2-5** | **Get strictness off the property-write path** | `Engine/Core/JSEngine.cs:223`; `JSValue` set accessors | P0-2 removed the redundant *writes*, but set accessors still **resolve** an `AsyncLocal<bool>` per write. The preferred fix — thread the compiler's static knowledge into the emitted set helpers so the hot path reads nothing — is not started | M |
| **2-6** | **Monomorphic call-site caching** | `BuiltIns/Function/JSFunction.cs` — `InvokeFunction`, `SelectInvocationDelegate` | Callee resolution repeats per call. **Prerequisite for inlining in Phase 4** | M |

**Sequence.** 2-1 first (largest single win, and it is the missing half of a
structure that otherwise works), then 2-3 (pure removal, near-zero risk), then
2-2, 2-4, 2-5, 2-6.

**Verify — per item, not per phase.**

- An `eng/performance/ownership.json` entry naming its benchmark and semantic
  owner. The file is item-scoped and already has fifteen such entries; match that
  granularity.
- Coverage in `PropertyShapeCacheTests` / `PropertyStoreCacheTests` for every
  invalidation path: `setPrototypeOf`, prototype mutation, own-property
  shadowing, `delete`, freeze, accessor redefinition, polymorphic and megamorphic
  sites.
- **P1-1 already touches `OrdinarySetWithOwnDescriptor`, the single most
  spec-sensitive path in the engine, and 2-1 to 2-4 touch it again.** test262
  over `test262-properties-proxy` and `test262-strict-mode` is not optional here.

**Exit criterion: DeltaBlue and Richards inside 200×.** They are the outliers on
a curve whose median is ~180×, and this phase is the reason they are.

---

## 5. Phase 3 — value representation

**Targets: Crypto (301×), zlib (340×), RayTrace (291×), EarleyBoyer (270×),
Splay (152×), NavierStokes (104×).** The largest total win in the plan and the
largest change. Deliberately after Phases 1 and 2 because those are contained
and this is not.

The root fact: `JSValue` is `public abstract partial class JSValue` — a CLR
reference type. There is no tagged-value representation, so a number that leaves
a local becomes a heap allocation. Baseline: integer arithmetic allocated
**128 bytes per iteration**; an empty `for` loop, 96.

Owner assemblies: `Broiler.JavaScript.Storage`, `.Runtime`, `.Compiler`.

### 3-1 · Unboxed backing stores for dense arrays — **start here**

**Where.** `Broiler.JavaScript.Storage/ElementArray.cs` — `private IPropertyValue[] dense`.

P2-3 made each element one reference instead of a 32-byte descriptor, which was
a real win, but a dense array of a million doubles is still a million heap
objects behind a million interface references.

**Work.** A typed backing store (`double[]`, `int[]`) chosen on first store, with
an elements-kind tag on `ElementArray`, transitioning to `IPropertyValue[]` on
the first non-numeric write. Standard, well-understood machinery.

**Target.** Crypto's 28-bit digit arrays, NavierStokes' grids, and the
typed-array-shaped heaps in zlib, Mandreel and Gameboy. **The most contained item
in the phase and the one covering the most benchmarks** — which is why it goes
first.

**Verify.** `test262-arrays` and `test262-binary-data`;
`CompactElementStorageTests`, `ElementDescriptorRoundTripTests`,
`IndexedWriteAndLengthTests` for integrity levels, foreign receivers, exotics and
length-shrink. Report allocation per element alongside time.

**Size: L.**

### 3-2 · Unboxed doubles in shape slots

The object-field twin of 3-1: `shapeSlots` holds `JSValue` references, so
`vector.x = 1.5` allocates. This is what RayTrace and Box2D need, and it
**composes with 2-1** — a shape that knows a slot is a double can store it raw,
so land 2-1 first and this gets cheaper.

**Where.** `Runtime/JSObject.cs`, `Runtime/ObjectShape.cs`. **Size: L.**

### 3-3 · Widen the unboxed-locals eligibility gate

P2-2 item 3 currently covers a function-top-level `var` not named by any nested
closure. Named as open in §8.1: **function parameters**, `let`/`const` (needs TDZ
analysis), and `var` declared inside a block or loop body (needs
definite-assignment analysis).

**Parameters are the valuable one** — every numeric helper takes them, and every
Octane benchmark is full of numeric helpers. Do parameters first and treat the
other two as separate items.

**Where.** `Broiler.JavaScript.Compiler` — the P2-2 eligibility gate.
**Watch:** patch 0047 exists because this codegen path produced invalid IL when
an unboxed local reached value position. Widening the gate widens that exposure;
`InvalidProgramException` is the failure signature to test for. **Size: M.**

### 3-4 · A tagged value representation — *scope and cost, do not start*

The real fix, and a multi-quarter redesign of the engine's most fundamental type
with every built-in downstream of it.

**Write it up and cost it at the end of Phase 3**, once 3-1 to 3-3 have shown how
much of the gap survives unboxed arrays, fields and locals. It is entirely
possible the answer is "less than expected", and that is worth knowing *before*
committing to the redesign rather than after. **Size: XL.**

---

## 6. Phase 4 — speculation

**Target: everything, and it is the difference between ~100× and ~10×.**

The most speculative part of the plan in both senses. Two findings make it more
tractable than it looks.

**The tiering scaffolding already exists and is general.**
`Runtime/FunctionTiering.cs` has `FunctionTieringController` with an invocation
threshold, a per-realm budget, a retained-code cap, delegate replacement, and
`RecordDeoptimization` counters, gated behind `JSContextOptions.FunctionTiering`
(disabled by default).

**But there is no optimizing compiler behind it.**
`JSFunction.RecompileForTiering` with `numericPlan == null` re-runs
`CoreScript.Compile` on `({source})` with a one-shot cache — it recompiles *the
same code the same way*, so it cannot be faster. The only real specialization is
the `NumericLoopPlan` path. **Tier-2 today is a hook, not a tier.**

That is a good position: the bookkeeping, budget and safety-fallback policy are
built and tested; what is missing is the part that makes entering tier-2 worth
anything.

| # | Item | Where | Note | Size |
|---|---|---|---|---|
| **4-3** | **Deoptimization** — **do this first** | `Runtime/FunctionTiering.cs`, `Engine/CallFrames.cs` | The safety net that makes everything else legal. Must bail out **mid-function** when a guard fails; the current model can only swap the delegate for the *next* call. This is the gating item for the entire phase | XL |
| **4-1** | **Type feedback collection** | `Runtime/ObjectShape.cs`, `.Compiler` sites | The inline caches already observe shapes at property sites. Extend to record and retain observed shapes, callee identities, and numeric-vs-generic outcomes per site | L |
| **4-2** | **A specializing tier-2 compile** | `BuiltIns/Function/JSFunction.cs` — replace the `numericPlan == null` branch | Consume 4-1's feedback: monomorphic property access → shape check plus direct slot read; arithmetic → raw `double`/`int` where feedback says so | XL |
| **4-4** | **Inlining of small JS callees** at monomorphic sites | `.Compiler` | What Richards and DeltaBlue actually need. Strictly downstream of 4-3, 4-1, 4-2, and of **2-6** | XL |

**Do not start 4-2 before 4-3 has a design.** Speculation without a mid-function
bailout is either unsound or restricted to functions with no observable side
effect before the guard — which excludes everything worth optimizing.

**Verify.** Deopt correctness before any speculation ships: a test that forces
every guard to fail at every point in a function body and asserts the fallback
produces the interpreter's answer. Then the full test262 matrix — this phase can
break anything.

---

## 7. Non-goals

Stated explicitly so effort does not drift into them.

- **GC work.** SplayLatency at 45× is the *best* result in the suite and Splay's
  throughput at 152× beats the median. The .NET collector is handling a workload
  it was never tuned for well. The allocation **rate** is a severe problem — that
  is Phase 3, and it is a problem with what the engine asks the collector to do,
  not with the collector.
- **asm.js or WebAssembly special-casing** for Mandreel and zlib. Recognizing
  asm.js type annotations would move two scores and is exactly the
  optimize-for-the-benchmark behaviour that got Octane retired. Phases 3 and 4
  reach the same code through general mechanisms.
- **Regex, until late.** `Broiler.Regex`'s backtracking interpreter costs one
  score, measured against Octane's *lowest* reference baseline. When it is
  reached: profile `Matching/Matcher.cs` against the Octane corpus first to
  separate backtracking strategy from per-step interpretive overhead, then
  compile the common subset (literal prefixes, character classes, bounded
  quantifiers) with the interpreter as fallback. It also sits on PdfJS's and
  Typescript's paths, so its value is larger than its one score suggests.
- **Chasing the geomean directly.** If a change raises the total without raising
  the worst scores, it has not smoothed anything.

---

## 8. Sequencing

| Phase | Order within it | Size | Unblocks / expected effect | Exit gate |
|---|---|---|---|---|
| **0** ✅ | 0-1 … 0-5 **implemented** | — | Everything. 12 → **17 scores**, known noise band | Owes only a workflow run: all 17 scores, no timeout at the 180 s floor, per-suite band on record (§2.6) |
| **1** | 1-2 mitigation → **1-1** → 1-2 real fix → 1-3 measure | XL | The two worst scores in the suite; page-load time generally | test262 over the four pinned manifests, no new failure **and no new timeout**; MandreelLatency and CodeLoad out of the tail |
| **2** | **2-1** → 2-3 → 2-2 → 2-4 → 2-5 → 2-6 | M each | The Richards/DeltaBlue/Box2D cluster | An ownership entry and owned tests **per item**; test262 properties/strict-mode; **DeltaBlue and Richards inside 200×** |
| **3** | **3-1** → 3-3 → 3-2, then *cost* 3-4 | L–XL | Uniform lift across arithmetic and allocation-heavy suites | `test262-arrays`, `test262-binary-data`; allocation reported per item alongside time |
| **4** | **4-3 design first** → 4-1 → 4-2 → 4-4 | XL | The remaining order of magnitude | Deopt correctness proven before any speculation ships; full test262 matrix |
| **5** | profile → compile the common subset | L | RegExp, plus PdfJS and Typescript | Octane regex corpus profiled **before** any rewrite |

**Dependencies.** Phases 1 and 2 are independent of each other and of Phase 5,
and can run in parallel. 3-2 is cheaper after 2-1. Phase 4 depends on 2-6
(4-4), on 4-3 (everything else in the phase), and benefits from 3-1/3-2 having
established unboxed representations for it to speculate into.

**The bolded item in each phase is the one to start with**, and in three of the
four it is not the one that sounds most important: 1-1 over 1-3, 2-1 over 2-6,
4-3 over 4-2. Each of those orderings is argued where the item is described.

**Every phase closes under [`Measurement.md`](Measurement.md)**,
unchanged: two runs inside the configured band, on the release RID matrix
(win-x64, linux-x64, linux-arm64), reporting time, allocation and working set
together, with an `eng/performance/ownership.json` entry naming each item's
benchmark and semantic owner. Note that the existing roadmap's phases A–F are all
*implemented* and none is *closed* for exactly this reason — the RID-matrix and
BenchmarkDotNet rows are still owed there, and this plan should not add to that
debt.

**A standing warning from the existing roadmap, which applies to every phase
here:** P3's premise — that the scope machinery around every call was the cost —
was built, measured, and disproved; the real cost was an 80-byte activation
record it was hiding. Measure before implementing, and be willing to throw the
implementation away.

---

_Sources: `tests/octane/benchmarks.md`, `tests/octane/results/`,
[`Archive.md`](Archive.md),
[`Measurement.md`](Measurement.md),
`patches/README.md`. Code sites verified against the
`Broiler.JS` checkout at `45f4f679`._
