# Broiler.JS execution-performance roadmap

This roadmap covers **steady-state script execution speed**: the cost of running JavaScript
once it has been parsed and compiled. It is a new, evidence-led plan, not a continuation of
the phase 0–5 optimization campaign referenced in [`docs/roadmap.md`](roadmap.md).

> `docs/roadmap.md` §4 currently states that "the optimization implementation phases are
> complete" and that only release *evidence* remains. That is accurate for the work those
> phases scoped (storage layouts, startup, packaging, SIMD, tiering experiments). It is not
> accurate as a statement about the engine's execution speed. The measurements below show
> that the two structures those phases delivered for hot-path execution — the object-shape
> layout and the property inline cache — are **inert for most real JavaScript**, and that
> three unrelated pieces of always-on bookkeeping dominate the interpreter loop. §4 of the
> main roadmap should be amended to point here.

- Owner assemblies: `Broiler.JavaScript.Runtime`, `.Engine`, `.BuiltIns`, `.Storage`, `.Compiler`
- Acceptance protocol: unchanged — [`docs/performance.md`](performance.md) governs what may
  be *claimed*. Nothing in this document closes on the numbers below.
- Status: **P0 implemented** (§4). **P1 implemented** apart from a compiler-level store cache
  (§5). P2–P3 are planned and not started.

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

Full suite after the change: **6 824 tests, 6 818 passing, 0 new failures** (the 6 failures are
pre-existing: 5 ICU/locale-data dependent, 1 in ModuleExtensions). 21 of those tests are new
and cover the reworked behaviour.

**P1 is implemented too**, apart from a compiler-level store cache (see P1-3). It targets a
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
[`docs/performance.md`](performance.md) (two runs inside the configured band, fresh-process
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

P1-1, P1-2 and P1-4 are implemented and covered by `PropertyShapeCacheTests`. P1-3 is
implemented only as a runtime fast path, not as the compiler-level store cache described
below — see its status note for why. As with P0 these are not *closed*: §8 lists the
acceptance evidence still owed.

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

### P1-3 · There is no store (put) inline cache

`CachedIndex` exists only for reads. Every property write performs a full generic lookup.
Once P1-1 restores shapes on the write path, a monomorphic store cache
(`shapeId → slot`, plus a shape-transition cache for the "adds a new property" case) becomes
straightforward and is worth roughly what the read cache is worth.

**Status: partially implemented — a runtime fast path, not an inline cache. Still open.**

The estimate above was wrong about the shape of the work. A read has one emission site,
`CachedIndex`, so routing it through a cache was a two-line change. A *store* has no such
choke point: assignments are built as `IndexExpression` targets in many places — plain member
assignment, compound assignment, destructuring and object patterns, `for-in`/`for-of` targets,
class field initializers — and lowering them to a cached helper call means either changing all
of those sites or intercepting index assignment inside `ILCodeGenerator`. That is a materially
larger and riskier surface than the rest of P1, and it was not attempted here.

What was done instead is contained to the runtime and needs no compiler change:
`JSObject.SetValue(KeyString, …)` now short-circuits the overwhelmingly common case — an
existing, writable own data property on an exact `JSObject` being overwritten through itself.
The general path resolved that same property *twice*, once in `SetValue` and again inside
`SetKeyStringOnReceiver`, before reaching the identical `Put`. Worth about 13% on
`prop-own-set` (477 → 417 ms).

The real store cache remains open, and should be scoped on its own rather than folded into a
phase with three unrelated items.

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

## 6. P2 — allocation on built-in and value paths

### P2-1 · `Array.prototype.push` allocates a full descriptor per element

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

---

### P2-2 · No small-number cache; every arithmetic result is a heap allocation

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

---

### P2-3 · Dense element storage is 4× larger than it needs to be

`ElementArray` stores `JSProperty[]` for dense (packed/holey) arrays. `JSProperty` is
attributes + key + `get` + `set` + `value` — **32 bytes** — where a default-descriptor dense
array needs only the 8-byte value reference. A 1 000-element array occupies 32 KB instead of
8 KB, which is the difference between fitting in L1 and not.

`ElementArray` already tracks `hasCustomDescriptors`. When it is false the backing store can be
a `JSValue[]`, promoted to `JSProperty[]` on the first non-default descriptor. This is the
element-storage analogue of P1-4.

**Risk: medium** — touches every element read/write path. `test262-arrays.txt`.

---

### P2-4 · Strings are flat; repeated concatenation is quadratic

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

---

## 7. P3 — call-path structure

`JSFunction.InvokeFunction` (`BuiltIns/Function/JSFunction.cs`) wraps every call in four
`using` scopes (`EnterRealm`, `EnterStrictMode`, `PushWithFallbackScopes`, `PushWithScopes`,
plus a conditional `SuspendWithScopes`), a `JSEngine.Current as JSContext` type test, and a
`try`/`catch (NullReferenceException)`/`finally` — the last of which also blocks inlining of
the whole method.

After P0-2 and P0-3 remove the two expensive scopes, the remainder is worth restructuring:
hoist a fast path for the overwhelmingly common case (ordinary function, same realm, no `with`
scopes, no legacy tracking, not a tail-call target) that skips straight to the invocation
delegate, and keep today's full path as the fallback. The `catch (NullReferenceException)` →
`ReferenceError` translation in particular should be established once per compilation rather
than per call if it can be.

Sequence this **after** P0; measuring it before then would just be measuring P0-2 and P0-3.

---

## 8. Sequencing and exit gates

| Phase | Items | Expected | Gate |
|---|---|---|---|
| ~~**A**~~ | ~~P0-1, P0-3~~ | **Done** — 2.0–2.9× on call paths, 6× less call allocation | Full `dotnet test` green (6 824 tests, 0 new failures); 21 new owned tests. test262 manifests still owed |
| ~~**B**~~ | ~~P0-2~~ | **Done** — folded into the same change | `StrictModeFlowTests` covers every transition shape; test262 manifests still owed |
| ~~**C**~~ | ~~P1-1, P1-4~~ | **Done** — cache reaches constructor/class code (0 → ~100% hit rate); constructor-built objects 6 595 → 1 480 bytes | Full `dotnet test` green; `PropertyShapeCacheTests` asserts the hit rates and every staleness path. P1-4's double storage still open |
| **D** | ~~P1-2~~, P1-3 | P1-2 **done** — inherited and class method calls hit the cache. P1-3 open: only a runtime fast path landed, not a store cache | `PropertyShapeCacheTests` covers `setPrototypeOf`, prototype mutation, own-property shadowing, delete, freeze, accessor redefinition, polymorphic and megamorphic sites |
| **E** | P2-1, P2-2 | `push` and arithmetic allocation | `test262-arrays`; `-0` and number-identity coverage |
| **F** | P2-3, P2-4, P3 | Memory footprint, string-heavy code, call structure | Full matrix per `docs/performance.md` |

Each phase adds an entry to `eng/performance/ownership.json` with its benchmark and semantic
owner, and closes only under the acceptance rules in `docs/performance.md` — two runs inside
the configured band, on the release RID matrix, with allocation, latency and working set
reported together.

**Phases A–C and most of D are implemented and covered by repository tests, but none are
*closed*.** Closing them still requires the pinned test262 run over `test262-arrays`,
`test262-properties-proxy`, `test262-strict-mode` and `test262-realm-isolation` (plus the
Annex B forbidden-extension paths), a
`PropertyOperationBenchmarks`/`FunctionCallBenchmarks` comparison, and the two-run
repeatability evidence on the release RID matrix. The numbers in this document come from an
ad-hoc in-process harness on a shared container and are not acceptance evidence. P1-1 in
particular touches `OrdinarySetWithOwnDescriptor`, the single most spec-sensitive path in the
engine, and the local suite is not a substitute for test262 there.

### Fixed along the way, unrelated to any phase

`SAUint32Map<T>` held its not-found sentinel in a plain mutable static. `GetNode` returns it by
`ref`, including from the create path, so `Put`/`Save` could set `HasValue` and store a value
straight into it — after which every later miss on any map of that `T` reported a false hit
with stale contents. That surfaced as an intermittent `NullReferenceException` resolving a
global binding, from a completely unrelated test, only in a full parallel run. It is the same
defect `StringMap.Empty` already carries a fix for (issue #1428, the `body-:0,0` frame); this
second copy of the pattern had been missed. Now thread-local and reset at every `GetNode`
entry, matching the existing fix.

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
