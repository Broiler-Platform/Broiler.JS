# Broiler.JS performance engineering roadmap

Status: audited roadmap, July 2026
Audit snapshot: repository commit `0f249f19`, .NET SDK 10.0.302 / runtime 10.0.10

Broiler.JS already has several good foundations: a runtime IL compiler, a named
expression-compilation backend boundary, generated built-in registration code, a
structural code-cache key, fixed-arity inline argument storage, and a pinned conformance
workflow. The next large gains will not come from adding SIMD everywhere. They will
come from removing work from the most frequent semantic paths, improving data layout,
and only then applying vectorization to operations that really are contiguous and
side-effect free.

This document is an implementation roadmap. Every proposal is tied to current code,
a measurement gate, and the JavaScript behavior that must remain observable.

## Executive decisions

1. **Optimize dispatch and storage before exotic code generation.** A normal function
   call currently reads a process environment variable and changes ambient strict-mode
   state. Ordinary property-existence checks can create JavaScript descriptor objects.
   Dense arrays use a sparse radix structure. These dominate more workloads than an
   isolated arithmetic intrinsic.
2. **Build packed/holey arrays and shapes with inline caches.** These are the highest
   long-term throughput opportunities. Introduce them incrementally, with explicit
   dictionary-mode and Proxy/exotic slow paths.
3. **Use modern .NET APIs before handwritten intrinsics.** `Span<T>`,
   `MemoryExtensions`, `SearchValues<T>`, `BinaryPrimitives`, `FrozenDictionary`,
   `CollectionsMarshal`, and the runtime's own vectorized implementations are portable
   and easier to keep correct. Add `Vector128/256/512` or Arm intrinsics only where a
   benchmark proves an additional gain.
4. **Do not split `BuiltIns` into handwritten and generated assemblies as it stands.**
   Generated files are partial definitions of the handwritten types, so they must be
   compiled together. A separate generated DLL would also leave the eager per-realm
   object construction cost untouched. Split by independently loadable feature area
   after adding spec-correct lazy data-property cells.
5. **Keep the normal runtime IL path.** Roslyn is valuable for generators, analyzers,
   and an optional diagnostic C# backend; it should not become the normal
   JavaScript-to-C# execution route.
6. **Treat ReadyToRun and Native AOT as different products.** ReadyToRun can improve
   startup of the static host and built-ins, but not runtime-emitted script bodies.
   Native AOT cannot use `Reflection.Emit` or dynamic assembly loading, so it requires
   an interpreter or offline-precompiled script mode rather than a publish switch.

## Audit evidence

### Repository and startup snapshot

| Observation | Current evidence | Consequence |
|---|---|---|
| Built-ins dominate static code | `Broiler.JavaScript.BuiltIns.dll` is about 1,245 KiB in the current Release output. The next largest Broiler assembly is `Broiler.Unicode.Properties.dll` at about 393 KiB. | Startup and working-set work should distinguish core language objects from Intl, Temporal, RegExp, and binary data. |
| Heavy source areas are concentrated | Handwritten/generated BuiltIns source contains about 54.7k lines. Temporal is about 10.8k, Intl 7.6k, RegExp 5.8k, and Array 5.5k lines. | Feature boundaries are more useful than a handwritten/generated boundary. |
| Generated registration is substantial and eager | `BuiltIns/Generated` contains 62 generated files, about 6.8k lines and 397 kB of source. It emits about 720 `JSFunction` construction sites, 468 `KeyString` fields, and 60 eager `CreateClass(context)` calls. | Splitting the emitted source alone cannot reduce per-context object creation. Generate compact descriptors and lazy registrars instead. |
| Context construction allocates heavily | A local diagnostic `ContextStartupBenchmarks.CreateContext` run on .NET 10 x64/AVX2 measured a warmed mean of 490.4 microseconds and 2.23 MiB allocated per context. A one-sample cold-process run was 138.7 ms and 2.23 MiB. | Track cold process, first context, and subsequent contexts separately. The one-sample cold number is not a stable baseline. |
| Benchmark infrastructure exists but is broad-grained | `Broiler.JavaScript.Engine.Benchmarks` has context, script-cache, object/array, Promise, built-in-heavy, and JInt smoke groups with `MemoryDiagnoser`. | Add direct primitive microbenchmarks, cold-start jobs, disassembly, EventPipe, and architecture-specific runs. |
| Runtime loading is implicit | `Engine/Core/JSEngine.cs` probes assembly names with `Assembly.Load` and runs module constructors; large module initializers populate global delegate registries. | Replace magic string loading with generated manifests and explicit capability/bootstrap APIs before trimming or feature satellites. |
| Immutable data is emitted as large object graphs | Current generated CLDR data constructs a dictionary with roughly 4.5k entries, while Unicode property data stores/parses large encoded range strings. | Generate compact sorted/perfect-hash tables or binary blobs plus offsets, and load feature shards on demand. |
| Modern runtime primitives are largely unused | The audited engine source has no uses of `BinaryPrimitives`, `SearchValues`, `FrozenDictionary`, `CollectionsMarshal`, `ArrayPool`, or explicit `Vector128/256/512` intrinsics. | Adopt portable, runtime-tuned primitives before maintaining custom architecture-specific loops. |
| Current R2R trade-off is large | A local framework-dependent `win-x64` audit grew from 19,070,950 bytes to 45,687,390 bytes with ReadyToRun (+139.6%); BuiltIns grew from 1,274,880 to 3,223,552 bytes (+153%). | R2R must be an evidence-based host publish profile, not a repository-wide default. These local size results must be reproduced with cold-start measurements. |

The DLL sizes are current build-output observations, not package-size guarantees. Record
the exact commit, RID, publish mode, and framework-dependent/self-contained choice in
future size reports.

The context command used for the local diagnostic was
`dotnet run -c Release --project Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks -- --filter *ContextStartupBenchmarks* --job Dry --join`
on Windows x64, .NET 10.0.10, AVX2, and concurrent server GC. The R2R and trim numbers
were one-machine audit probes and their temporary output was not retained. All numbers
in this section are therefore illustrative evidence for Phase 0—not release baselines
or acceptance gates. Phase 0 must retain commands, MSBuild properties, CPU/GC/job data,
and machine-readable results.

### Hot-path evidence

| Area | Current path | Finding |
|---|---|---|
| Function invocation | `BuiltIns/Function/JSFunction.cs:727-773` | Each ordinary invocation checks `BROILER_SCRIPT_HOST`, enters realm/strict scopes, installs with-scopes, and then invokes the body. `JSEngine.EnterStrictMode` uses `AsyncLocal<int>` and writes on entry and disposal. |
| Property existence | `Runtime/JSValue.cs:733-746`, `Runtime/JSObject.PropertyStorage.cs:195-220`, `Runtime/JSObjectCoreExtensions.cs:17-45` | Ordinary `HasProperty`/`HasOwn` can convert an internal property to a JS-visible descriptor, allocating a `JSObject` and descriptor properties just to answer a Boolean question. |
| Key lookup | ``Storage/ConcurrentStringMap`1.cs``, `Storage/KeyStrings.cs`, `Runtime/JSObject.PropertyStorage.cs:1117-1165` | Interned-key reads go through locking/name resolution and repeat private-name and canonical-index classification on property access. |
| Dense arrays | `Storage/ElementArray.cs`, `Storage/SAUint32Map.cs:164-255`, `BuiltIns/Array/JSArray.cs:335-430` | Every element, including packed sequential elements, is stored in a 2-bit radix map and repeatedly tree-walked during dense iteration. |
| Sparse enumeration | `Storage/VirtualMemory.cs:12`, `Storage/SAUint32Map.cs:80-90` | `VirtualMemory.Count` exposes backing capacity rather than the used high-water mark, so enumeration can scan unused slots. |
| Property deletion | `Storage/PropertySequence.cs:167-218` | Deletion scans the singly linked insertion-order list for the predecessor, making delete-heavy workloads quadratic. |
| Map and Set | `Runtime/UniqueID.cs`, `BuiltIns/Map/JSMap.cs`, `BuiltIns/Set/JSSet.cs` | Identity/equality keys are rendered as strings and placed in a custom string map, allocating and hashing text on operations that should use SameValueZero directly. |
| Typed writes | `BuiltIns/Array/Typed/JSFloat64Array.cs` and sibling typed-array files; `BuiltIns/DataView/DataView.cs` | Multi-byte writes use `BitConverter.GetBytes` plus `Array.Copy`, creating a temporary byte array for each write. |
| Parser identifiers and numbers | `Parser/FastKeywordMap.cs`, `Parser/FastScanner.cs`, `Parser/NumberCoercion.cs`, `Ast/Misc/StringSpan.cs` | Normal unescaped identifiers still build/copy text for keyword and scope lookups; numeric paths materialize strings and, in places, a `StringReader`. |
| Code cache | `Runtime/DictionaryCodeCache.cs` | The process-wide cache is unbounded, retains source/delegates, serializes unrelated misses behind one compile lock, and hashes the full source on lookup. |
| Switch lowering | `ExpressionCompiler/Generator/ILCodeGenerator.VisitSwitch.cs` | Recognized integer and string switches are emitted as linear comparisons rather than IL jump tables or hash buckets. |
| Local variables | `Compiler/Scope/FastFunctionScope.cs`, `Runtime/JSVariable.cs` | Bindings are represented by `JSVariable` cells even when they do not escape, adding allocation and TDZ/read-only branches to ordinary local access. |

## Success metrics and non-negotiable gates

The baseline phase must replace estimates below with stable measurements. Initial
directional goals are:

| Metric | Initial target | Gate |
|---|---:|---|
| Warm `JSContext` creation | At least 50% less managed allocation; improve time without increasing first-use latency | Multi-realm isolation tests; all default globals/descriptors/order unchanged |
| Ordinary zero/one-argument JS call | At least 25% lower time and 0 B/op in the steady path | Strict/sloppy, recursion, async continuation, realm crossing, `caller`, `arguments`, `with`, and tail-call tests |
| Ordinary own property hit and `in` | 0 B/op; at least 2x faster for stable shapes | Accessor receiver, prototype mutation, Proxy trap order, symbols/private names |
| Packed array read/write/map/reduce | At least 2x throughput on 1k+ elements and materially lower bytes/element | Holes, inherited indexed accessors, descriptors, length edge cases, species, and abrupt completion |
| Typed-array/DataView writes | No temporary byte arrays; at least 2x for sequential multi-byte writes | Both endiannesses, unaligned offsets, resizable/detached buffers, BigInt, Half, and NaN payload tests |
| Parser | At least 30% fewer allocated bytes/source byte on identifier-heavy and numeric-heavy bundles | Exact grammar, escaped identifiers, numeric separators/radices/rounding, source locations |
| Map/Set primitive/object operations | 0 key-string allocation and at least 2x for numeric/object keys | SameValueZero for `NaN` and signed zero; reference identity; live iteration under mutation |
| Static publish/startup experiments | Report startup, working set, file count, and total bytes together | Do not accept a time win that silently causes an unacceptable size or steady-state regression |

Every optimization PR must run the nearest unit/integration tests and the relevant
pinned test262 subset. Storage, Proxy, compiler, or call-frame changes require the full
script-host-verifiable test262 matrix before release. Performance results must include
commit, runtime version, OS/RID, CPU features, GC mode, and benchmark source.

## Priority map

`S`, `M`, `L`, and `XL` indicate relative engineering size, not elapsed-time promises.

| Priority | Work item | Impact | Size | Semantic risk | Dependency |
|---|---|---:|---:|---:|---|
| P0 | Replace per-call environment lookup with immutable engine/context options | High | S | Low | Direct call benchmark |
| P0 | Add descriptor-free ordinary `HasOwnProperty`/`HasProperty` paths | High | M | Medium | Proxy/exotic override contract |
| P0 | Make typed-array/DataView scalar writes allocation-free | High | S-M | Low-Medium | Endianness test matrix |
| P0 | Separate `VirtualMemory` used count from capacity; add struct enumeration | Medium | S | Low | Storage microbenchmark |
| P0 | Fast-path unescaped identifier/keyword and numeric parsing with spans | High | M | Medium | Parser allocation benchmark |
| P0 | Add bounded cache metrics and correct source/location compatibility keys | High operational value | M | Medium | Cache workload corpus |
| P1 | Replace Map/Set string identity with a SameValueZero comparer | High | M | Medium | Equality/iteration tests |
| P1 | Simplify WeakMap/WeakSet around ephemeron storage | High GC/memory | M | Medium | Forced-GC and cycle tests |
| P1 | Publish lock-free key metadata for interned names | High | M-L | Medium | Key interning stress tests |
| P1 | Make insertion-order deletion O(1) and enumeration allocation-free | Medium-High | M | Medium | Mutation-order tests |
| P1 | Add packed, holey, and sparse array element kinds | Very high | XL | High | Indexed-prototype versioning |
| P1 | Split common function-call fast path from semantic slow paths | Very high | L | High | Explicit execution-state model |
| P1 | Generated built-in descriptors and spec-correct lazy values | High startup/memory | L | High | Property-storage lazy cell |
| P2 | Scalar-replace noncaptured locals | High | L | High | Scope escape classification |
| P2 | Emit real dense/sparse/string switch dispatch | Medium-High | M | Medium | Backend tests/IL diagnostics |
| P2 | Shapes, slots, prototype versions, and mono/poly inline caches | Transformative | XL | High | Stable object layout/invalidation |
| P2 | Feature-based built-in satellite assemblies | High startup/size for selective hosts | L-XL | High | Lazy values and explicit bootstrap |
| P2 | Incrementalize generators and clean the project/package graph | Medium build/startup | M-L | Low-Medium | Build/package baselines |
| P3 | Guarded unboxed numeric locals and engine-level tiering | Transformative on compute | XL | Very high | Shapes, feedback, deoptimization |
| P3 | Tagged/value-oriented internal JS value representation | Transformative memory/GC | XL | Very high/API-breaking | Allocation profiles and migration plan |
| P3 | Offline/precompiled or interpreter Native AOT mode | Strategic | XL | High | Backend/capability split |

## Workstream A: measurement and performance CI

### A1. Separate lifecycle measurements

Measure these independently:

- cold process to first executed script;
- static assembly load and module-initializer cost;
- first `JSContext` in a process;
- subsequent `JSContext` construction;
- first parse/compile/evaluate;
- code-cache hit, miss, concurrent miss, and eviction;
- first access to each lazily loaded built-in area;
- steady-state execution after tiered JIT warmup.

The current context benchmark mixes useful steady construction data with BDN process
startup. Add explicit cold-start jobs or a small child-process harness rather than
interpreting one number as both.

### A2. Add direct microbenchmarks

Add benchmark classes for:

- function calls: native/script, 0-4/5/16 arguments, strict/sloppy, same/cross realm,
  captured `with`, recursive, callback, and tail-call paths;
- property operations: own/prototype hit/miss, get/set/has/delete, string/index/symbol,
  accessor, Proxy, 1/4/16/100-property objects, and 1/2/4/16 receiver shapes;
- arrays: packed/holey/sparse, high indices, push/pop/shift/splice/map/reduce/sort,
  inherited indexed accessors, and species/callback slow paths;
- Map/Set: numeric, string, object, symbol, BigInt, mixed keys, mutation during iteration;
- typed arrays and DataView: each width, aligned/unaligned, sequential/random, overlap,
  both endiannesses, detach/resize checks;
- parser/compiler: parse-only and compile-only inputs, identifiers, numbers, strings,
  comments, switches, local counts, cache concurrency;
- introspection and copy: `Object.keys/values/entries`, descriptors, spread/rest, for-in;
- built-ins: RegExp, JSON, Intl, Temporal, Promise jobs, and base64/hex binary helpers.

Do not benchmark every path by repeatedly calling `Eval`; that hides primitive costs
behind parsing, compilation, and global lookup. Keep both direct microbenchmarks and
end-to-end scripts.

Also make the cache-hit benchmark representative: its current
`LocalDictionaryCodeCache` calls `JSCode.Key`, rebuilding an interpolated source/argument
string on every probe, while the production default uses a structural key. Benchmark
the actual default cache and isolate hash/equality cost in a separate case.

### A3. Add diagnostics and matrices

- Keep `MemoryDiagnoser`; add opt-in disassembly jobs for IL/JIT-sensitive changes.
- Keep `ShortRunJob` for developer smoke only. Use longer, separately launched jobs
  with verified warmup/JIT state for tiering, PGO, cache, and sub-microsecond claims.
- Use EventPipe/`dotnet-trace` for allocation type, GC pause, contention, assembly-load,
  exception, and JIT events.
- Use hardware counters where the host supports them: branch misses, cache misses,
  instructions, and cycles. Treat unavailable virtualized-CI counters as optional data,
  not a pass/fail signal.
- Test Windows x64, Linux x64, and Linux Arm64. Include x64 AVX2 enabled/disabled and
  Arm64 AdvSimd-capable hosts when SIMD paths change.
- Compare workstation and server GC, tiered PGO on/off, framework-dependent and
  self-contained publish, and ReadyToRun on/off for deployment work.
- Store compact JSON/CSV baselines and use noise-aware thresholds: fail on a confirmed
  material regression, not a single noisy sample.

## Workstream B: function calls, arguments, and execution state

### B1. Move host mode into explicit options (P0)

`JSFunction.InvokeFunction`, construction, `InvokeSuper`, and the IL tail-call emitter
query `BROILER_SCRIPT_HOST`. Read that environment value once at the application/CLI
boundary and convert it to an immutable `JSEngineOptions`/`JSContextOptions` value.
Tests that need both modes should construct both option sets rather than mutate process
state between invocations.

Acceptance:

- no per-call/runtime query of `BROILER_SCRIPT_HOST`; resolve it into explicit options
  at the host boundary;
- generated code receives a stable compilation option and cache keys include it;
- direct function-call benchmarks cover both modes;
- changing the environment after creating a context has documented, deterministic
  behavior.

### B2. Split the ordinary call fast path (P1)

Most calls are same-realm, have no captured `with` scopes, do not need legacy
`caller`/`arguments` state, and retain the current strictness. Encode these conditions
on the function and call frame, then branch once into:

1. a small ordinary body-invocation/trampoline loop;
2. a cold semantic path for realm changes, dynamic scopes, legacy state, host mode,
   debugger hooks, and error translation.

`CurrentContextScope` already avoids the `AsyncLocal` write for a same-realm call, but
strict-mode depth writes for each invocation. Prefer a thread-affine execution frame
for synchronous nested calls and synchronize ambient state only at public/native/async
boundaries. Do not remove `ExecutionContext` propagation until async tests demonstrate
the replacement.

### B3. Keep and extend the fixed-arity argument design

`Runtime/Arguments.cs` already stores `this` plus four arguments inline; do not replace
it with a generic pooled array. Improve the remaining cases:

- generated 0-4 argument delegate overloads or a direct call-frame representation;
- an immutable slice/view for `CopyForCall` instead of shifting 5+ arguments;
- an `ArgumentsBuilder` for spread/apply/bind that grows with pooled temporary storage;
- direct builtin overloads for fixed-arity helpers;
- pool only temporary buffers that cannot escape into a JavaScript `arguments` object.

Benchmark the 0-4 cases independently so an optimization for spread does not regress
the dominant small-call path.

### B4. Scalar-replace locals before redesigning all values (P2)

Classify bindings as local-only, captured, direct-eval-visible, `with`-visible, global,
or debugger-visible. Emit a direct `JSValue` IL local for local-only bindings, plus an
initialization bit only where lexical TDZ semantics require it. Allocate `JSVariable`
cells for the other categories.

After this is correct, specialize proven numeric locals as `int`/`double` and box only
at an observable escape. Profile-guided type guards and deoptimization belong in a
later phase; static proof should land first.

## Workstream C: properties, keys, shapes, and inline caches

### C1. Descriptor-free internal existence checks (P0)

Add internal operations such as:

```csharp
bool TryGetOwnProperty(PropertyKey key, out JSProperty property);
bool HasOwnProperty(KeyString key);
bool HasOwnProperty(uint index);
bool HasProperty(PropertyKey key);
```

Ordinary objects should inspect internal storage and walk prototypes without creating
a JS-visible descriptor. Proxy and exotic objects must override the operation and
preserve traps, receivers, and abrupt completion. Only APIs such as
`Object.getOwnPropertyDescriptor` should materialize descriptor objects.

Use the typed index overloads in hole checks inside array built-ins to avoid boxing or
stringifying numeric keys. A successful ordinary `in`/`has` benchmark should allocate
0 B/op.

### C2. Attach metadata to interned keys (P1)

At intern time, compute and publish immutable metadata with each key ID:

- private-name bit;
- array-index classification and parsed `uint` value;
- canonical numeric-index classification/value for typed arrays;
- optionally a stable ordinal hash used by storage.

Use a lock-free published ID-to-metadata array and a concurrency-safe text-to-ID
intern table. Reads must not take `ReaderWriterLockSlim`. Benchmark intern misses under
contention separately from overwhelmingly common intern hits.

For static keyword/name maps, evaluate a generated length/character switch or a
`FrozenDictionary` held in a `static readonly` field. On .NET 10, frozen collections
and alternate span lookups are optimized for read-mostly data, but generated switches
can still win for very small fixed vocabularies; benchmark both.

### C3. Introduce shapes and slots incrementally (P2)

Use immutable shape transitions for ordinary fast objects:

- shape ID plus ordered property metadata;
- contiguous value slots;
- transitions for adding common data properties;
- dictionary mode for deletes, unusual descriptors, accessors, or excessive churn;
- object/prototype mutation versions for invalidation.

The first inline cache should be intentionally narrow: a constant-key, ordinary own
data-property read, guarded by shape ID. Then add:

1. prototype data/method reads guarded by receiver shape and prototype-chain version;
2. writes to existing writable slots;
3. shape transition writes;
4. accessor calls with the original receiver;
5. polymorphic caches for two to four shapes;
6. a megamorphic/global lookup cache.

Proxy, exotic, dictionary-mode, revoked, or invalidated cases use the existing generic
path. Keep the cache at the emitted call site or in a compact side table; do not add a
virtual cache lookup to every generic storage operation.

### C4. Fix order/deletion and introspection

- Replace the singly linked predecessor scan in `PropertySequence` with prev/next
  handles, or an append-only order vector with tombstones and measured compaction.
- Add allocation-free struct enumerators for internal paths.
- Give ordinary-object `Object.keys/values/entries` a guarded path that reads internal
  enumerable flags directly. Retain the descriptor/trap path for Proxy and exotic
  objects, including exact interleaving of gets and traps.
- Preserve ECMAScript order: integer indices ascending, other strings by insertion,
  symbols by insertion, and delete/re-add at the end.

## Workstream D: array and element storage

### D1. Correct capacity versus occupancy (P0)

Expose `Capacity`, used high-water mark, and live count separately in `VirtualMemory`.
Make `SAUint32Map` enumeration visit only used/live nodes. Add regression tests for a
large reserved capacity with low occupancy and deletions.

### D2. Benchmark the radix map against current .NET collections

Do not assume either the custom trie or `Dictionary<uint,T>` wins universally. Compare:

- current `SAUint32Map<T>`;
- `Dictionary<uint,T>` using `CollectionsMarshal.GetValueRefOrAddDefault` where safe;
- 2-8 inline entries followed by a dictionary;
- segmented/chunked indexed storage;
- ordered sparse storage when enumeration frequency justifies it.

Measure 0/1/4/16/100/10k entries, sequential/random hits and misses, insert/delete,
enumeration, bytes/entry, and cache misses. Never retain a `CollectionsMarshal` ref
across a resize or callback.

### D3. Add element kinds (P1)

Recommended representation:

```text
Packed values       -> contiguous JSValue slots, no holes, default descriptors
Holey values        -> contiguous slots plus hole state/bitmap
Dictionary elements -> sparse indices and/or non-default descriptors/accessors
```

Transition packed to holey on deletion/gaps and to dictionary mode based on index,
density, descriptor, and capacity thresholds. Avoid automatic sparse-to-dense
conversion until profiling shows it is useful.

Fast built-in loops may operate on contiguous storage only when guards prove:

- ordinary array and unmodified indexed semantics;
- no relevant indexed properties/accessors on the prototype chain;
- no Proxy/exotic receiver;
- no callback-observable mutation skipped by the optimization;
- correct holes, species, length, and abrupt-completion behavior.

Use an indexed-prototype version to invalidate packed-array assumptions when code adds
or removes a numeric property on `Array.prototype` or another relevant prototype.

## Workstream E: Map, Set, caches, and concurrency

### E1. SameValueZero Map/Set keys (P1)

Replace `UniqueID` string keys with a dedicated comparer over `JSValue`:

- normalize `+0` and `-0`;
- make all numeric NaNs equal for Map/Set purposes;
- compare strings by ordinal content;
- compare objects by reference and use `RuntimeHelpers.GetHashCode`;
- compare Symbols and BigInts according to their identity/value semantics.

Keep an ordered entry vector with tombstones for live iteration. Use
`CollectionsMarshal.GetValueRefOrAddDefault` only if callbacks cannot observe an
invalid ref and no ref survives a resize. WeakMap/WeakSet require weak identity storage,
not the same strong dictionary.

### E2. Simplify WeakMap and WeakSet ephemeron storage (P1)

The current weak collections combine `ConditionalWeakTable`, `WeakReference`,
finalizable wrappers, a string-keyed map, and explicit locking. Prototype a direct
`ConditionalWeakTable<JSValue, Box<JSValue>>` for WeakMap and a shared sentinel value
for WeakSet. `ConditionalWeakTable` already provides reference-identity ephemeron
semantics and thread safety.

Validate key-to-value-to-key cycles, delete/reinsert, forced full collections,
finalizer-queue activity, symbol eligibility, and realm teardown. Benchmark operation
throughput and retained objects after GC; a faster lookup that accidentally keeps keys
alive is not acceptable.

### E3. Make the compiled-code cache bounded and observable (P0/P1)

The default cache needs:

- configurable limits by entries, retained source bytes, and estimated generated-code
  bytes;
- LRU/clock-style eviction or an equivalent bounded policy;
- hit, miss, duplicate-wait, compile-time, entry-size, and eviction metrics;
- per-engine ownership by default, with an opt-in process-shared cache;
- a compatibility key containing engine semantic version, compilation/strict/host
  options, feature flags, TFM/RID-relevant backend mode, and source/argument identity;
- correct source-location behavior: the current compiled body captures `JSCode.Location`
  for script metadata/stack frames while the structural key omits it. Include location
  and relevant debug/source-map identity, or redesign the body to receive location
  metadata separately so identical source can be shared safely;
- retention accounting for the complete backing source string held by a `StringSpan`,
  not only the slice length. Either normalize/copy a small slice or charge/deduplicate
  the full retained source in cache limits;
- a fast reference-identity tier for repeated use of the same source object, followed
  by a measured ordinal span hash/equality tier;
- per-key `Lazy` duplicate suppression without one process-wide lock around unrelated
  compiles.

Remove the global compilation lock only after isolating or synchronizing mutable
compiler metadata. Stress with many engines compiling the same and different scripts.

### E4. Repair persistent assembly caching

`AssemblyCodeCache` should not use a shared non-thread-safe SHA instance, full UTF-8
temporary arrays, LINQ hex formatting, default-context permanent loads, or reflective
`Invoke` for the steady path. Move toward:

- incremental/content hashing and a versioned manifest;
- atomic temp-write plus rename and cross-process duplicate handling;
- `System.Reflection.Metadata`/`PEBuilder` and portable PDBs for first-party output;
- a collectible `AssemblyLoadContext` where unloading is required;
- `CreateDelegate` after validation instead of repeated reflection invocation;
- integrity checks and cache quarantine on incompatible/corrupt artifacts.

Persistent code is an optimization cache, never a source of truth.

### E5. Concurrency model

Document `JSContext` as thread-affine unless a separate design explicitly changes it.
Avoid locks in single-realm execution. Parallelize independent parse/compile requests
after compiler state is isolated; do not transparently execute one JavaScript realm on
multiple threads. Share immutable CLDR/time-zone/keyword metadata across realms and
keep mutable prototypes, constructors, job queues, and intrinsics per realm.

## Workstream F: parser, strings, JSON, RegExp, Intl, and Temporal

### F1. Parser span fast paths (P0)

- Scan normal ASCII/unescaped identifiers as `ReadOnlySpan<char>` and allocate a string
  only when the identifier enters a persistent symbol/scope table.
- Replace `FastKeywordMap`'s reflection-built `ConcurrentDictionary` and `.Value`
  materialization with a generated keyword classifier or measured frozen/span lookup.
- Parse decimal and radix literals directly from spans. Use `double.TryParse` with
  explicit invariant settings where semantics match; manually handle JavaScript-only
  syntax and exact rounding. Copy only literals containing separators when necessary.
- Use `SearchValues<char>`/`IndexOfAny` to jump between comment, quote, escape, newline,
  and identifier delimiters while preserving exact source line/column accounting.
- Make `FastList.Contains`/`IndexOf` inspect `[0..Count]`, not the unused backing array,
  and replace safe compile-time buffers with `ArrayPool<T>` only after lifetime review.

### F2. Strings and JSON

Use `string.IndexOf`, `MemoryExtensions`, `SearchValues`, `string.Create`, and span-based
copying for operations that are ordinal and side-effect free. Split ASCII fast paths
from Unicode/Rune-aware slow paths only where ECMAScript uses code units versus code
points correctly.

For JSON, benchmark the existing parser before substituting `System.Text.Json` pieces.
Any borrowed scanner/number/string primitive must preserve JavaScript reviver order,
duplicate-key behavior, surrogate handling, number conversion, and error locations.
Avoid building intermediate CLR object graphs.

### F3. RegExp

- Add a bounded cache keyed by pattern, flags, Unicode mode/version, and engine semantic
  version if equivalent compiled forms are currently rebuilt.
- Keep the low-startup interpreted .NET regex path for one-shot translated patterns,
  and benchmark promotion of frequently reused compatible patterns to compiled regex
  after a threshold. Bound compiled-code memory and record first-match cost.
- Use `RegexOptions.NonBacktracking` only for a proven semantics-compatible subset; do
  not silently change backreferences, lookarounds, captures, or backtracking behavior.
- GeneratedRegex is appropriate only for static engine-owned patterns, not user input.
- Benchmark compile, first match, steady match, catastrophic inputs, captures, and
  allocations separately.

### F4. Intl and Temporal

Share immutable parsed locale, Unicode, and time-zone tables process-wide. Cache
canonicalized locale identifiers and formatter skeletons with explicit bounds. Keep
per-realm constructors/prototypes and mutable observable objects isolated.

Perform caching only after ECMAScript option coercion/property access has occurred in
the specified order. A formatter cache must not skip getters, Proxies, exceptions, or
locale canonicalization side effects.

## Workstream G: IL generation and compiler specialization

### G1. Preserve and strengthen the backend boundary

Target shape:

```text
JavaScript source
  -> parser / AST
  -> semantic lowering
  -> Broiler IR
  -> backend
       - DynamicMethod: short-lived runtime code
       - collectible assembly: larger runtime units
       - metadata/PDB: persistent cache or offline artifacts
       - diagnostic C#: readable reductions/debugging only
       - interpreter/offline backend: no-runtime-codegen hosts
```

Keep backend selection outside per-instruction emission. Make diagnostics opt-in so
normal compilation does not allocate trace writers. Add IL verification, method-size,
local-count, exception-region, and source-map output to a debug artifact.

### G2. Emit real switch dispatch (P2)

- Dense integer cases: normalize once, range-check, emit `OpCodes.Switch`.
- Sparse integers: sorted decision tree/binary search or measured hash dispatch.
- Strings: compute one stable hash, switch on buckets, then equality-test collisions.
- Very small switches: retain linear compares when they produce less IL and win.

Benchmark 4/16/64/256 cases with first/middle/last/miss inputs, dense/sparse integers,
string collisions, JIT time, IL size, and execution time. Preserve duplicate cases,
fallthrough, default placement, `NaN`, signed zero, and coercion behavior.

### G3. Engine-level tiering and guarded numeric code (P3)

Do not depend on runtime tiered PGO to optimize runtime-generated methods without
measurement. Prototype Broiler-level tiering instead:

1. emit a quick generic baseline body;
2. collect compact call-site/type/loop counters;
3. recompile hot functions with shape and numeric guards;
4. atomically replace the function delegate;
5. fall back/deopt to generic helpers on guard failure.

Start with arithmetic locals statically proven not to be captured, visible to eval, or
used as BigInt/object/string. Preserve `-0`, NaN, infinities, coercion, overflow, and
exception behavior. Cap recompilations and code memory to avoid tiering thrash.

### G4. Long-term value representation experiment (P3)

The class-based `JSValue` hierarchy makes numeric-heavy code allocation-sensitive.
Before an API-breaking rewrite, capture allocation-type profiles and compare:

- current objects plus specialized unboxed compiler locals;
- small-integer/constant caches;
- an internal tagged readonly struct with object/reference payload;
- a platform-specific NaN-boxing experiment only if managed-GC interaction and
  portability are demonstrably safe.

Prefer an internal representation behind stable public APIs. This is an experiment,
not a prerequisite for the earlier roadmap.

## Workstream H: CPU, SIMD, and memory layout

### H1. Remove scalar allocation first (P0)

Replace typed-array/DataView `BitConverter.GetBytes` plus `Array.Copy` with
`BinaryPrimitives.Write*Endian`, `BitConverter.TryWriteBytes`,
`MemoryMarshal.Write`, or carefully measured `Unsafe.WriteUnaligned` over the backing
span. Reads can use matching span APIs and bit-conversion helpers. Prefer explicit
endianness where the JavaScript API specifies it.

Use the safest API that produces equivalent code. Confirm disassembly before choosing
`Unsafe`; the JIT often lowers `BinaryPrimitives` and span copies optimally.

Replace the manual `Math.clz32` bit-propagation/table algorithm in
`BuiltIns/Objects/JSMath.cs` with `BitOperations.LeadingZeroCount` after a zero/random
input microbenchmark. The BCL maps it to LZCNT/CLZ-class instructions where available
and keeps a portable fallback; this is a small, low-risk example of CPU-specific work
that should be delegated to the runtime.

### H2. Add guarded bulk paths

Good candidates for runtime/BCL vectorization are:

- same-kind typed-array copy/set/subarray and overlapping memmove;
- raw byte fill/clear/copy and ArrayBuffer transfer;
- strict-compatible base64/hex scan/decode, with a forgiving JavaScript slow path;
- parser delimiter and ASCII classification scans;
- ordinal string search/count/replace building blocks;
- dense numeric internal loops only after an unboxed representation exists.

Prioritize `copyWithin`, `fill`, `reverse`, same-type `set`, and `slice` in
`JSTypedArray.prototype.cs`. Use view spans rather than copying an entire backing buffer,
and snapshot only when conversion plus actual overlap requires it. For default typed
sorting, a stable typed merge/radix experiment with pooled scratch is reasonable, but
the custom-comparator path must remain generic and observable.

For `Uint8Array` base64/hex helpers, call span overloads over the actual view and use
.NET's vectorized codecs for the fully compatible fast case. Keep the existing
JavaScript-aware parser for partial writes, whitespace, padding, URL alphabets, and
malformed-input modes. Test 16-byte, 1 KiB, and 1 MiB payloads plus every error mode.

General `Array.prototype.map/filter/reduce`, arbitrary property loops, Proxy operations,
and callbacks are not SIMD candidates because user code and abrupt completion are
observable per element.

### H3. Explicit intrinsic policy

1. Implement a correct scalar/span baseline.
2. Verify that .NET does not already vectorize the operation.
3. Add a portable `Vector<T>` or `Vector128/256/512` implementation only for a measured
   gap.
4. Dispatch once outside the loop using `IsSupported` and data-size thresholds.
5. Keep x64 and Arm64 behavior equivalent and retain a scalar fallback.
6. Test unsupported-ISA jobs, short/tail lengths, alignment, overlap, endianness, and
   deterministic exceptions.

Avoid hard-coding AVX2 as the engine baseline. .NET 10 exposes newer x64 intrinsics and
improves its own vectorized `SearchValues`/`MemoryExtensions` implementations, but
portable library code should benefit automatically before Broiler takes on
architecture-specific maintenance.

### H4. Cache and object layout

- Favor contiguous slots and arrays over pointer-heavy tree nodes on dense data.
- Keep hot tags/shape IDs/lengths together; move debug names and rarely used descriptor
  metadata to cold objects or side tables.
- Split large fast methods from cold throw/error/Proxy paths so the JIT can inline the
  common case without code bloat.
- Apply `AggressiveInlining`, `AggressiveOptimization`, `NoInlining`, or
  `SkipLocalsInit` only with disassembly and benchmark evidence. Blanket attributes can
  increase code size and startup cost.
- Pool compiler/parser buffers with clear ownership. Do not pool identity-bearing or
  user-visible JavaScript objects.
- Seal internal leaf types where polymorphism is not required and verify that the JIT
  devirtualizes the call. Use `readonly`, `ref readonly`, `scoped`, and `ref struct`
  builders for provably non-escaping parser/compiler temporaries; do not force them onto
  values that must cross async, closure, debugger, or public API boundaries.
- Consider `[InlineArray]` only for measured tiny internal buffers. The existing
  explicit four-argument layout is already specialized and should not be rewritten
  merely to use a newer language feature.

## Workstream I: built-in assembly and startup architecture

### I1. Why `BuiltIns.Generated.dll` is not the right first split

The generated types are `partial class` definitions containing `CreateClass` methods
for handwritten built-ins. C# partial types cannot span assemblies. Moving those files
would require redesigning generator output into composition/delegation anyway. More
importantly, `Names.RegisterAll` would still eagerly construct the same per-realm
functions and prototypes, while an extra DLL adds metadata, load, relocation, and call
boundaries.

Therefore:

- keep current partial output beside its handwritten type in the near term;
- change the generator to emit compact immutable descriptors and one registrar per
  feature area;
- measure emitted method count, IL bytes, startup allocations, and JIT work;
- split assemblies only where a feature can remain unloaded/unrealized.

### I2. Add spec-correct lazy data properties

An accessor masquerading as a global constructor is observably wrong. Add an internal
lazy **data-property cell** that already has the correct key, attributes, and property
order but realizes its per-realm value on the first operation that needs that value.

Required behavior:

- `Reflect.ownKeys`, `Object.keys`, and enumeration see the property in the correct
  order without constructing its value;
- `get`, descriptor-value retrieval, or a dependent intrinsic realizes once per realm;
- redefining/deleting the property before realization follows normal data-property
  rules and can cancel the factory;
- recursive/concurrent initialization is deterministic;
- one realm never receives another realm's constructor/prototype;
- factories and immutable metadata may be shared, mutable JS objects may not.

A core lazy cell must not hold a satellite delegate, `Type`, registrar object, or a
module-initializer registration: any of those can load/root the feature assembly before
realization. Store only a core-owned feature ID and data needed for the observable
property. Resolve that ID through the host/bootstrap loader on first realization and
verify the boundary with `AssemblyLoad` events.

Implement this before lazy Intl/Temporal or assembly splitting.

### I3. Proposed feature assemblies

After lazy cells and explicit bootstrap exist, prototype this dependency direction:

```text
Broiler.JavaScript.Runtime / Storage / Engine
                 |
Broiler.JavaScript.BuiltIns.Core
       |         |          |           |
    Binary     RegExp      Intl       Temporal
                            |             |
                   Unicode / CLDR   DateTime / tzdata

Broiler.JavaScript.All  -> full-profile references to every feature
```

Candidate content:

- **Core:** Object, Function, primitive wrappers, Array, Error, Math, JSON, Reflect,
  Proxy, Promise, Map/Set, iterators, and required shared infrastructure.
- **Binary:** ArrayBuffer, SharedArrayBuffer, DataView, TypedArray, Atomics.
- **RegExp:** engine integration and RegExp built-ins.
- **Intl:** Intl constructors plus CLDR/Unicode dependencies.
- **Temporal:** Temporal plus time-zone/date dependencies.
- **Optional/host:** non-standard globals, web/event helpers, modules, experimental
  proposals where packaging permits.

Current source coupling crosses those proposed boundaries: Intl uses Temporal helpers,
Number formatting reaches Intl, and Date has Temporal integration. Extract narrow
locale/calendar/time-zone provider interfaces or shared non-JS algorithms first; do not
create cyclic satellite references or move observable constructors into shared static
state.

The default/full profile must still expose the supported standard surface. A minimal
embedding profile may deliberately omit features, but must be named/configured as a
different, potentially non-conformant profile. Assembly splitting is successful only
if full-profile cold startup and working set do not regress and selective hosts can
actually avoid loading heavy satellites.

### I4. Replace implicit assembly discovery

Generate an explicit `BuiltInManifest`/bootstrapper containing feature IDs,
dependencies, registrars, semantic version, and trimming annotations. Let hosts choose
a profile through options or an explicit builder. Keep the current module-initializer
probing as a compatibility adapter, then deprecate it.

This removes repeated `Assembly.Load` by magic name, exposes missing-feature errors,
reduces mutable global delegate wiring, and gives trimming/AOT analysis a visible call
graph.

Keep the core manifest free of static metadata references to satellite types. A
full-profile package may deploy all files, but only a core-owned resolver should load a
satellite on first feature realization. Gate the work on actual loaded-assembly events
and working set, not merely the number of DLLs in the publish directory.

Loader traces may show that some always-hot small assemblies cost more as separate
modules than their layering is worth. After explicit bootstrap is in place, experiment
with a consolidated core distribution for Storage/Runtime/Engine/Extensions while
preserving source-project boundaries or public compatibility through type forwarding.
Keep Compiler, CLR, Debugger, and feature satellites separate. Merge only if cold load,
working set, and publish data improve; architectural neatness alone is not a gate.

### I5. Reduce per-realm construction

- Share immutable descriptor/name tables and parsed data.
- Instantiate mutable prototypes, constructors, accessors, and intrinsic identities per
  realm.
- Generate compact loops/tables rather than hundreds of large repeated registration
  method bodies where measurement supports it.
- Source-generate the fixed `KeyStrings` assignments rather than reflecting over fields
  and calling `FieldInfo.SetValue` during static initialization. Reuse existing core key
  fields instead of emitting duplicates, and place cold feature keys in lazy holders.
- Replace generated CLDR dictionaries and encoded Unicode range strings with measured
  compact layouts: sorted key/offset tables, perfect hashes, binary blobs, or
  RVA-backed readonly data. Shard by locale/property so first use of one feature does
  not construct the complete data graph.
- Do not clone a mutable template realm until mutation, accessor identity, prototype
  links, and internal-slot isolation have exhaustive tests.
- Construct disabled experimental methods only when enabled; do not create and then
  delete them during context startup.

## Workstream J: deployment, ReadyToRun, trimming, and Native AOT

### J1. ReadyToRun experiment

Publish representative hosts for `win-x64`, `linux-x64`, and `linux-arm64` with and
without ReadyToRun. Measure cold start, first context, first built-in use, total bytes,
file count, and working set. ReadyToRun reduces JIT work for static assemblies but makes
binaries larger; tiered compilation can later replace frequently used R2R methods.
Runtime-generated JavaScript bodies still need the JIT.

Do not enable R2R globally based on a microbenchmark. Offer it as a documented host
publish profile if the full trade-off is positive.

### J2. Trimming

A local trim audit removed effectively nothing from the current framework-dependent
publish after working around an analyzer-project property-propagation failure. Treat
that as evidence that dynamic loading, module initializers, reflection, and the project
graph currently hide reachability—not as evidence that trimming has no value.

- Replace string-based type/assembly discovery with generated manifests.
- Annotate unavoidable reflection precisely; do not broadly root entire assemblies.
- Add a trimmed sample host and make trim warnings actionable in CI.
- Generate builtin/extension registrars so reachability is explicit.
- Test dynamic feature loading and error messages in both trimmed and untrimmed hosts.

### J3. Native AOT capability modes

Native AOT explicitly does not support runtime `Reflection.Emit` or dynamic assembly
loading. Define engine capabilities rather than hiding this behind exceptions:

```text
DynamicJit        runtime parser + runtime IL emission
OfflineCompiled   build-time generated assembly/metadata for known scripts
Interpreted       runtime parser + no executable-code generation
```

An AOT proof of concept should execute a precompiled script with no `Reflection.Emit`
at runtime, reject or interpret dynamic `eval` according to documented policy, and run
the relevant test262 subset. Keep runtime-codegen references out of the AOT-facing
assembly using analyzers and capability-specific project boundaries.

## Workstream K: generators, build graph, packaging, and tooling

### K1. Make source generation genuinely incremental

`JSClassGenerator` currently collects all matching syntax before generation, so one
attributed-type change can invalidate the entire BuiltIns output. Refactor it to:

- use `ForAttributeWithMetadataName` for semantic discovery;
- transform each type into a small equatable model and emit each class independently;
- collect only compact name/feature registration records for the aggregate manifest;
- keep output deterministic and snapshot-tested;
- record no-op, one-file edit, and clean generator time/allocation baselines.

The generator uses no Workspaces service in the audited source. Reference the smallest
supported `Microsoft.CodeAnalysis.CSharp` surface rather than
`Microsoft.CodeAnalysis.CSharp.Workspaces`, then reassess whether every project needs
the repository-wide pinned compiler package.

### K2. Stop writing normal generated output into the source tree

BuiltIns, Globals, Modules, and Network enable compiler-generated file emission into a
shared `Generated` source directory and run custom add/remove targets. Roslyn already
compiles generator output; this adds disk I/O and risks configuration/TFM parallel-build
clobbering.

- Make persisted generated source an opt-in diagnostics/review mode.
- Write it below a configuration/TFM-specific intermediate directory.
- Remove target choreography that cannot affect the compile that already completed.
- Keep a deterministic snapshot test or CI artifact so generated code remains
  inspectable without becoming ordinary source input.

### K3. Make the package and project graph truthful

Before optimizing package splits, add a pristine-consumer smoke test that installs the
produced packages and executes a representative script. The audit found package/project
metadata that needs explicit resolution:

- BuiltIns metadata references Regex and DateTime while its project marks those
  references private; ensure the NuGet package supplies or declares every runtime
  dependency.
- Decide whether `Broiler.JavaScript.All` is a dependency-only meta-package or a real
  bootstrap facade; its project description and actual references/output should agree.
- Remove declared project references that produce no assembly metadata edge only after
  build, API, and package tests confirm they are unnecessary.
- Keep legacy/new solution files and optional Network/Node projects consistent.

Correctness comes first here: a smaller package that fails to resolve at runtime is not
an optimization.

### K4. Separate engine hosting from heavy CLI tooling

The audited CLI output is roughly 19 MB, with Roslyn scripting plus
NuGet/JSON/dependency tooling accounting for most of it. Move CSX compilation and
package restore into an optional CLI tooling component/plugin; keep the embeddable
engine host free of those dependencies. Remove unused/old NuGet client packages, align
versions, and put embedded sources/debug assets in SourceLink/symbol packages where
appropriate.

Measure engine package, minimal host, full CLI, and optional scripting-tool payloads
separately. This is a distribution-size and cold-load win even when core execution
throughput is unchanged.

### K5. Deterministic build policy

- Add one pinned SDK policy (`global.json`) and one source of truth for TFM/language
  version and central package versions.
- Correct stale build comments before using them as architecture constraints.
- Keep analyzer/generator projects isolated from host publish properties such as
  `PublishTrimmed`, `PublishAot`, and RID when those properties are not applicable.
- Normalize solution/project configuration mapping. The audited root `Release` build
  reported DateTime and Unicode dependencies from `bin/Debug` while Broiler projects
  used `bin/Release`; benchmark and package jobs should reject mixed-configuration
  outputs.
- Track clean build, no-op build, one-BuiltIn edit, generated bytes, compiler server
  reuse, and package/publish sizes in CI.

## Delivery sequence

### Phase 0 — trustworthy baselines and invariants

Implementation entry point: [`../performance/phase0-baselines.md`](../performance/phase0-baselines.md).
It defines the checked-in jobs/schema, child-process lifecycle harness, EventPipe
workloads, build/package/IL/publish collectors, focused conformance manifests, ownership
map, repeatability bands, and CI artifact workflow used as the gate for later phases.

Deliver:

- lifecycle and primitive benchmarks from Workstream A;
- checked-in benchmark configuration and result schema;
- EventPipe allocation/lock/assembly-load profiles for context creation, function
  calls, property access, arrays, parsing, and Map/Set;
- clean/no-op/one-file generator build timings, package graph snapshots, and IL,
  ReadyToRun, and trimmed publish-size baselines;
- focused conformance manifests for properties/Proxy, arrays, Map/Set, parser, binary
  data, strict mode, and realm isolation.

Exit gate: two repeatable baseline runs agree within the documented noise band and all
future work items have a benchmark plus semantic test owner.

### Phase 1 — low-risk hot-path cleanup

Implementation and local verification: [`../performance/phase1-results.md`](../performance/phase1-results.md).

Deliver in small independent PRs:

1. explicit host-mode options and corrected cache keys;
2. allocation-free typed-array/DataView reads and writes;
3. descriptor-free ordinary `HasProperty` with exotic slow paths;
4. `VirtualMemory` occupancy and struct enumeration fix;
5. parser keyword/identifier/number span fast paths and `FastList` count-bound scans;
6. bounded code-cache metrics and retention limits;
7. stop constructing disabled experimental built-ins;
8. split selected cold error paths from fast methods based on disassembly;
9. fix runtime package dependencies and add a pristine-consumer smoke test;
10. isolate persisted generated files, remove unused Workspaces/compiler dependencies,
    and incrementalize the generator.

Exit gate: relevant directional targets improve, allocation profiles confirm the
intended objects disappeared, and pinned focused conformance subsets remain clean.

### Phase 2 — collections, keys, and element layout

Implementation and local verification: [`../performance/phase2-results.md`](../performance/phase2-results.md).

Deliver:

1. SameValueZero Map/Set storage;
2. direct ephemeron WeakMap/WeakSet storage;
3. immutable lock-free key metadata;
4. O(1) property-order deletion and allocation-free enumeration;
5. measured choice of sparse map implementation;
6. packed/holey/dictionary arrays plus indexed-prototype invalidation;
7. guarded dense-array built-in paths.

Exit gate: packed and sparse workloads both meet their targets; memory per element and
property is recorded; mutation/Proxy/hole/species test matrices pass.

### Phase 3 — compiler specialization

Implementation and local verification: [`../performance/phase3-results.md`](../performance/phase3-results.md).

Deliver:

1. noncaptured local scalar replacement;
2. real integer/string switch dispatch;
3. ordinary-object shapes and slots;
4. monomorphic, then bounded polymorphic property inline caches;
5. prototype versioning and invalidation diagnostics;
6. persistent metadata/PDB backend improvements.

Exit gate: generic slow paths remain available and measured; invalidation tests pass;
code size/JIT time stay within explicit budgets; full supported test262 matrix does not
regress.

### Phase 4 — startup and packaging

Implementation and local verification: [`../performance/phase4-results.md`](../performance/phase4-results.md).

Deliver:

1. generated manifests/descriptors;
2. spec-correct lazy data-property cells;
3. lazy Intl/Temporal pilot in the existing assembly;
4. explicit bootstrap/profile API;
5. feature-satellite prototype and full/minimal package measurements;
6. trimmed and ReadyToRun sample-host reports.

Exit gate: full profile remains conformant and does not regress cold startup; selective
hosts demonstrate lower loaded bytes and working set; property order/descriptors and
realm identity are unchanged.

### Phase 5 — advanced execution modes

Implementation and local verification: [`../performance/phase5-results.md`](../performance/phase5-results.md).

Deliver only after earlier foundations are stable:

- Broiler-level hot-function counters, recompilation budgets, and delegate replacement;
- guarded unboxed numeric locals and deoptimization;
- internal tagged-value feasibility prototype;
- offline compilation/interpreter capability for a Native AOT sample.

Exit gate: compute-heavy macrobenchmarks improve without mixed-type cliffs, code-cache
growth is bounded, deoptimization is tested, and the alternate execution mode has a
documented supported language/host surface.

## Completed foundations to preserve

The earlier roadmap delivered useful pieces that should remain:

- sparse CopyDataProperties paths enumerate stored indices instead of scanning array
  length, with high-index/getter/order regressions;
- the default dictionary code cache uses a structural source-span key rather than an
  interpolated source string;
- analyzer `BJS0001` identifies literal `KeyStrings.GetOrCreate` calls inside loops;
- expression compilation exposes named DynamicMethod and collectible-assembly
  backends plus opt-in diagnostics;
- ordinary runtime nested-lambda compilation avoids unused diagnostic writers;
- stable `KeyString` lookups have been hoisted out of selected context/registry startup
  paths;
- startup-surface tests cover global descriptors and default-disabled feature gates.

Extend these designs rather than reintroducing parallel infrastructure.

## Ideas to reject or defer

- **Blanket SIMD/intrinsics:** most JavaScript operations have observable callbacks,
  property lookup, or exceptions. Vectorize proven raw spans, not semantics.
- **A handwritten/generated DLL split without generator redesign:** partial types cannot
  span assemblies and eager registration remains.
- **Eagerly splitting every built-in into a DLL:** more files and loads can make the
  full profile slower. Split only behind lazy realization and measure the full profile.
- **Sharing prototype objects between realms:** user mutation and intrinsic identity
  make this incorrect.
- **Pooling `JSObject`, `JSArray`, arguments arrays, or descriptors that can escape:**
  object identity and retained references make reuse unsafe.
- **Blanket `AggressiveInlining`/`AggressiveOptimization`:** code size and cold startup
  can regress; use JIT evidence.
- **Replacing the JavaScript compiler with JavaScript -> C# -> Roslyn at runtime:** it
  adds source generation/parsing/compilation overhead and does not remove the need for
  Broiler's semantic lowering.
- **Turning on Native AOT for the current engine:** runtime code generation and dynamic
  loading are unsupported; build a distinct capability path.
- **Changing multiple storage representations in one PR:** isolate descriptor-free
  lookup, key metadata, shapes, and packed elements so correctness and performance can
  be attributed.

## Platform references

- Microsoft, [Performance Improvements in .NET 10](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-10/): current JIT, frozen-collection, alternate lookup, `SearchValues`, `MemoryExtensions`, and intrinsic improvements.
- Microsoft, [ReadyToRun deployment overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/ready-to-run): startup/size trade-offs and interaction with tiered compilation.
- Microsoft, [Native AOT deployment overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/): runtime code generation and dynamic loading limitations.
- Microsoft, [`BinaryPrimitives.WriteDoubleLittleEndian`](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.writedoublelittleendian?view=net-10.0): allocation-free span-based binary writes.
- Repository, `docs/compliance/process.md`: pinned test262 and performance-regression process.
