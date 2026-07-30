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

---

## 1. Verdict

Yes — there is substantial, unrealized headroom, and most of it is not exotic.

Four defects account for the majority of the gap. None of them are "make the JIT smarter"
problems; three are always-on bookkeeping that can be removed or made lazy, and one is a
property-write path that silently disables the engine's own optimization for the most common
way JavaScript builds objects.

Measured on the probes in [Appendix A](#appendix-a--reproducing-the-measurements):

| Hot path | Now | With the three P0 fixes prototyped | Factor |
|---|---:|---:|---:|
| Plain function call (sloppy) | 945 ms | 248 ms | **3.8×** |
| Closure call | 953 ms | 264 ms | **3.6×** |
| Prototype method call | 861 ms | 300 ms | **2.9×** |
| Built-in call (`Math.max`) | 443 ms | 188 ms | **2.4×** |
| Empty `for` loop | 426 ms | 192 ms | **2.2×** |
| Own property read | 491 ms | 281 ms | **1.7×** |
| Integer arithmetic | 476 ms | 317 ms | **1.5×** |
| `script:stopwatch` (real script) | 976 ms | 630 ms | **1.6×** |

Allocation falls at least as sharply: a sloppy JS function call allocates **1 784 bytes**
today and **264 bytes** with the P0 fixes prototyped.

These prototypes were throwaway measurement patches and have been reverted; two of the three
are not shippable as written (see the risk notes per item). They establish the *size of the
prize*, not the design.

Beyond P0, the shape/inline-cache system needs real work: today it never fires for
constructor-assigned fields, class instances, prototype methods, or any property write.

---

## 2. How the evidence was collected

Single-machine, in-process timing and allocation counting via
`GC.GetAllocatedBytesForCurrentThread()`, plus the engine's own
`PropertyOptimizationDiagnostics` counters. Each scenario is compiled and run once to warm,
then measured on a second evaluation in the same context.

- Commit `833b74a`, `Release`, .NET SDK 10.0.110
- Linux x64, 4 × Intel Xeon @ 2.80 GHz, 15 GB RAM, containerized

**These numbers are for prioritization only.** They are single-run, on a shared 4-core
container, and the two `dromaeo-object-*` scenarios in particular are GC-dominated and swung
±25% between runs in both directions — no claim in this document rests on them. Any
*published* performance claim must go through the repeatability and semantic gates in
[`docs/performance.md`](performance.md) (two runs inside the configured band, fresh-process
lifecycle samples, the release RID matrix, and the semantic owners named in
`eng/performance/ownership.json`).

---

## 3. Baseline

Times are for the iteration counts in [Appendix A](#appendix-a--reproducing-the-measurements);
compare columns, not absolute values.

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

## 4. P0 — always-on bookkeeping in the hot path

### P0-1 · Every value allocation invalidates every inline cache

`JSValue`'s constructor assigns `BasePrototypeObject`, whose setter unconditionally calls
`JSObject.NotifyPrototypeChainMutation()` — even when the prototype being assigned is `null`,
which is the case for every primitive.

`Broiler.JavaScript.Runtime/JSValue.cs:433` and `:634`:

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
  (`ObjectShape.cs:172`, `PropertyInlineCache.Get`, validates the shape id only). It is a
  counter with no consumer.
- The diagnostics counters themselves are unconditional `Interlocked.Increment` calls on
  shared statics, taken on **every** cache hit and every cache miss
  (`ObjectShape.cs:181`, `:187`). On a multi-threaded host this is a cache-line ping-pong on
  the engine's hottest line.

`indexedPrototypeVersion` does have a real consumer — `JSArray.CanUseDenseElementFastPath()`
(`BuiltIns/Array/JSArray.cs:30`) — but assigning a *null* prototype cannot add an indexed
property to anyone's prototype chain, so the null case need not bump it.

**Fix.** Return early from the setter when `value is null`; make the diagnostics counters
opt-in behind a static switch (or `[Conditional]`) so they cost nothing when disabled; and
either delete `prototypeMutationVersion` or give it its real job (see P1-2, where a
prototype-validity generation is exactly what a prototype-chain cache needs).

**Measured.** `loop-empty` 426→207 ms, `arith-add` 476→294 ms, `prop-own-get` 491→300 ms,
`array-rw` 270→169 ms, `class-field` 215→155 ms. Prototype invalidations 800 013 → 4.

**Risk: low.** The only removed behaviour on the null path is a global version bump that
provably cannot invalidate anything. Requires a full `dotnet test` pass plus the
`test262-arrays` and `test262-properties-proxy` manifests. Making the diagnostics opt-in is a
public-surface change to `PropertyOptimizationDiagnostics` — the counters become meaningful
only when enabled, which must be documented.

**Owners.** `Broiler.JavaScript.Runtime` · semantic owner `Broiler.JavaScript.Runtime.Tests`.

---

### P0-2 · `AsyncLocal<int>` written twice per JavaScript function call

`Broiler.JavaScript.Engine/Core/JSEngine.cs:211`:

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

**Risk: high — `[ThreadStatic]` is not a correct fix.** `AsyncLocal` was presumably chosen so
strict-mode state flows across `await` boundaries; a thread-static would lose it when an async
function resumes on a different thread. The shippable design is to stop using ambient state
for this at all:

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

`BuiltIns/Function/JSFunction.cs:790`, inside `InvokeFunction`:

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

There is a second, larger cost hiding here. `SetLegacyArguments` uses `FastAddValue`, which
calls `TrackShapeDataProperty`, and the read-only attribute set forces `AbandonObjectShape()`.
In the prototype-method-call probe this fires **200 006 dictionary fallbacks in a
200 000-iteration loop** — the legacy bookkeeping is destroying shape information once per
call. Disabling it drops that to **5**.

**Measured** (tracking disabled purely to size it): `fn-call` 678→272 ms and
1 560→264 bytes/call; `proto-method-call` 632→309 ms and 1 408→264 bytes/call;
`closure-call` 682→278 ms and 1 592→296 bytes/call.

**Risk: medium. Deleting the behaviour is not an option** — `forbidden-ext/b2/*` in test262
reads these properties directly and requires that the access not throw, and the "non-strict
`f.arguments` is non-null while `f` is on the stack" behaviour is web reality.

The fix is to make it **lazy**, not to remove it:

1. Store the in-flight `Arguments` (a struct already on the stack) and the caller reference in
   plain fields on `JSFunction`, not as observable properties. Push/pop is then two field
   writes.
2. Back the observable `caller` / `arguments` properties with accessors that materialize the
   arguments object **on read**, from those fields.
3. Because they stop being data properties written through `FastAddValue`, the shape is no
   longer abandoned — which is where most of the win actually comes from.

Care is needed on two points the current code gets right: a strict caller must be reported as
`null` rather than through a throwing accessor, and the properties must still shadow
`Function.prototype`'s poison-pill accessors. Recursion also needs a stack of saved values,
not a single field.

**Owners.** `Broiler.JavaScript.BuiltIns` · semantic owner `Broiler.JavaScript.BuiltIns.Tests`,
manifest `test262-strict-mode.txt` plus the Annex B forbidden-extension tests.

---

## 5. P1 — make shapes and inline caches actually work

The shape system exists and is correct; it is simply almost never reachable. This is the
largest *remaining* win after P0 and the one that needs design rather than deletion.

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
(`Runtime/JSObject.PropertyStorage.cs:679`). The recursion bottoms out at `%Object.prototype%`,
which calls `SetKeyStringOnReceiver` with `this` = `%Object.prototype%` and `target` = the real
receiver. Because `!ReferenceEquals(target, this)`, the write takes the generic receiver path
(`:797`) and lands in `DefineReceiverDataProperty` (`:1087`), whose `else` branch does:

```csharp
var descriptor = CreateDataDescriptor(value, attributes);
var result = target.DefineProperty(name, descriptor);
```

and `DefineProperty(in KeyString, JSObject)` opens with `AbandonObjectShape()`
(`:1317`).

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
   (`:1089`); the receiver-mismatch branch needs the same treatment when the receiver is
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

---

### P1-2 · The inline cache does not cover prototype lookups — so method calls never hit

`PropertyInlineCache.Get` (`Runtime/ObjectShape.cs:172`) validates an **own** data slot only.
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

---

### P1-3 · There is no store (put) inline cache

`CachedIndex` exists only for reads. Every property write performs a full generic lookup.
Once P1-1 restores shapes on the write path, a monomorphic store cache
(`shapeId → slot`, plus a shape-transition cache for the "adds a new property" case) becomes
straightforward and is worth roughly what the read cache is worth.

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

**Fix.** Trust the shape id on the cache-hit path and index `shapeSlots` directly (the shape id
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

`JSFunction.InvokeFunction` (`BuiltIns/Function/JSFunction.cs:777`) wraps every call in four
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
| **A** | P0-1, P0-3 | 2–3.5× on call- and allocation-heavy paths | Full `dotnet test`; `test262-arrays`, `test262-properties-proxy`, `test262-strict-mode` + Annex B forbidden-extension tests; no regression in `PropertyOperationBenchmarks`, `FunctionCallBenchmarks` |
| **B** | P0-2 | ~1.5× further on call paths | `test262-strict-mode`; explicit async/generator strict-mode-resumption coverage |
| **C** | P1-1, P1-4 | Inline cache becomes reachable for constructor/class code; ~5× less allocation on constructor-built objects | `test262-properties-proxy`; `PropertyOptimizationDiagnostics` shows non-zero hits for the constructor and class-field probes |
| **D** | P1-2, P1-3 | Method calls and property writes hit cache | `test262-properties-proxy`, `test262-realm-isolation`; targeted invalidation tests (`setPrototypeOf`, `__proto__`, own-property shadowing) |
| **E** | P2-1, P2-2 | `push` and arithmetic allocation | `test262-arrays`; `-0` and number-identity coverage |
| **F** | P2-3, P2-4, P3 | Memory footprint, string-heavy code, call structure | Full matrix per `docs/performance.md` |

Each phase adds an entry to `eng/performance/ownership.json` with its benchmark and semantic
owner, and closes only under the acceptance rules in `docs/performance.md` — two runs inside
the configured band, on the release RID matrix, with allocation, latency and working set
reported together.

Ordering matters: **C must follow A**, because P0-3's legacy-arguments write is itself
abandoning shapes 200 000 times in the probe and would mask any P1-1 improvement.

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
