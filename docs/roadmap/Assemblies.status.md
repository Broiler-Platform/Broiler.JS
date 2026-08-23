# Assembly restructure — status

**Current interpretation:** A-1's structural implementation has landed through S-6 and A-3's
explicit registry slice landed with it; final S-7 validation remains open. The initial A-0
census below is preserved as a dated snapshot, but its conclusion that the wider plan needed
no rescope is superseded: the proposed Base and Core folds create project cycles. MOD-M2 must
produce the replacement graph before further consolidation.

> The evidence half of [`Assemblies.md`](Assemblies.md). **The plan document is the one to
> act from.** Nothing here is a measurement in [`Measurement.md`](Measurement.md)'s sense —
> a line count and a reference edge are facts about the tree, not results — and nothing here
> is a claim about feasibility, cost or performance.

---

## State

| | |
|---|---|
| Items started | **3** — A-0, A-1, A-3 |
| Structurally landed | **A-1 through S-6**; A-3's explicit registration slice |
| Acceptance open | **A-1/S-7** — pinned test262 and repository validation |
| Superseded | The initial A-0 no-rescope conclusion and A-5 Core fold |
| Blocked on | **MOD-M2 verified graph** — project shells must resolve the Base↔Ast and Core↔Parser cycles and establish FrontEnd/Semantics ownership |
| Next | Finish S-7 independently; then build MOD-M2 shells and a whole-tree/profile-classified Emit, dynamic-code, and name-probing census |

**A-1's structural change has landed; its acceptance has not.**
`Broiler.JavaScript.Expressions` exists, carries the expression model,
and **contains no `System.Reflection.Emit`** — verified against the built assembly, not
against `using` directives. `Ast`, `Storage`, `Parser`, `Runtime` and `Engine` reference it
instead of the emitter. `LinqExpressions` legitimately needs both, exactly as
[`AssemblySplit.md`](AssemblySplit.md)'s S-4 anticipated. The full account, including four
corrections to the symbol analysis, is in
[`AssemblySplit.status.md`](AssemblySplit.status.md) §0.

**So the sentence this whole plan was written against is no longer true.** "There is no
subset of the current graph that runs JavaScript without dynamic code" was the finding below;
`Expressions` + `Ast` + `Parser` + `Storage` is now such a subset at the assembly-reference
level. That is not yet a running bytecode configuration — that is A-7 — but the edge that
made it impossible is gone.

**That does not validate the old target diagram.** `Storage → Ast → Expressions` makes the
proposed `Storage + Expressions` Base merge cyclic; `Engine → Parser → Runtime` makes the
proposed `Engine + Runtime` Core merge cyclic. In addition, `Compiler` contains shared
binding/scope/early-error/hoisting/analysis work that cannot be moved wholesale behind an IL
boundary if bytecode is to share language semantics. These are the MOD-M2 rescope inputs.

### A-0, sub-question by sub-question

| | Question | Answer |
|---|---|---|
| **1** | Is the model/emitter split clean? | **Yes, by building.** Four things symbol analysis missed, none fatal — a real model→emitter back-edge (`LinqExtensions`), two extra file cuts, and ~20 members widened to `public`. `AssemblySplit.status.md` §0 |
| **2** | What do `Runtime`, `Storage`, `Ast`, `Parser` actually consume? | **Answered by construction** — they compile against `Expressions` alone. Counting types was the proxy; the compiler is the answer |
| **3** | Can the proposed shared/runtime profile be AOT-clean? | **Partially answered only.** The dated analyzer census found 52 warning sites across the selected closure, 9 `IL3050`; it was not a whole-program publish and cannot validate the now-superseded Core grouping |
| **4–5** | `LinqExpressions`/`Compiler` split and the wider graph | **Not answered; MOD-M2 owns it.** Separate shared FrontEnd/Semantics from IL-specific lowering using project shells |

### Sub-question 3 — the preliminary analyzer census

Measured 2026-08-07 by building `Broiler.JavaScript.Engine` and its project references with
`-p:IsAotCompatible=true -p:IsTrimmable=true`, unannotated and unsuppressed. Counts are
**distinct warning sites**, deduplicated across the analyzer's repeated passes.

| Assembly | Warnings | Of which `IL3050` |
|---|---:|---:|
| `Broiler.JavaScript.Runtime` | **29** | 3 |
| `Broiler.JavaScript.Expressions` | **18** | 4 |
| `Broiler.JavaScript.Engine` | **5** | 2 |
| **Tier 0–1 total** | **52** | **9** |

**43 of the 52 are annotation-shaped** (`IL2060`/`IL2070`/`IL2075`/`IL2080`): reflection over
a `Type` the code already holds, retirable with `DynamicallyAccessedMembers` and no behaviour
change. **The 9 `IL3050`s are the ones that are not free** — `MakeGenericMethod` (4),
`MakeGenericType` (3), `MakeArrayType` (2) and one `Expression.NewArrayInit`. Those are
`RequiresDynamicCode`: no annotation removes them, the construction has to be avoided or made
reachable from concrete instantiations.

**And it is concentrated, not pervasive.** `Runtime`'s 29 sit in five files, four of which are
CLR interop rather than the JavaScript object model:

| | |
|---|---:|
| `Runtime/TypeExtensions.cs` | 14 |
| `Runtime/JSObjectBuilder.cs` | 6 |
| `Runtime/MethodProvider.cs`, `Runtime/JSValueToClrConverter.cs` | 4 each |
| `Engine/CoreInternalHelpers.cs` | 4 |
| `Runtime/JSVariable.cs`, `Engine/JSDynamicMetaData.cs` | 1 each |

**Historical interpretation, now narrowed.** This census suggested reflection was
concentrated at CLR interop; it did not prove the proposed Core merge acyclic or a
whole-program bytecode/AOT profile clean. MOD-M2 therefore respecifies the graph for independent
reasons, and A-7 remains the only publish-and-run proof.

### The caveat that matters more than the number

**Not one of the 52 is `Assembly.Load`, and that is a problem with the measurement, not a
result.** The original census identified two name-based probes in
`Broiler.JavaScript.Engine` —
`Core/JSEngine.cs`'s `TryLoadAssembly` (which even records
`StartupOptimizationDiagnostics.RecordCompatibilityAssemblyProbe()`) and
`FastParser/Compiler/DefaultJSCompiler.cs`'s `EnsureCompilerAssemblyLoaded`, which loads
`"Broiler.JavaScript.Compiler"` by string and runs its module constructor so a
`[ModuleInitializer]` can register the real pipeline.

**Those two are examples, not an exhaustive inventory.** Other profiles and generated code
must be scanned for `Assembly.Load`, `Type.GetType`, `System.Reflection.Emit`, and related
dynamic-code paths, including `AssemblyCodeCache` and `ILPack`. The trim analyzer emits
nothing for the two Engine probes: `Assembly.Load(string)` is a runtime lookup it
cannot see, so **the warning count is a floor, not a ceiling**, and a green analyzer run would
not mean a working Native AOT publish. This is direct evidence for **A-4 being a blocker
rather than hygiene**, exactly as [`Assemblies.md`](Assemblies.md) promoted it — and it is why
**A-7's gate has to be an application that publishes and *runs*, not a warning count.**

Two further limits on this number, stated so it is not over-read:

- It is the **per-assembly analyzer**, not whole-program `PublishAot` analysis. The latter
  sees across assembly boundaries and will find more.
- It covers tier 0–1 only. `BuiltIns`, `Clr`, `Modules` and the compiler are unmeasured, and
  `Clr` is interop by definition.

---

## Historical pre-split finding that shaped A-1

> Snapshot from 2026-08-07, retained for provenance. Present-tense statements in this
> section describe the graph before `Broiler.JavaScript.Expressions` landed; §State above is
> current.

**`Broiler.JavaScript.ExpressionCompiler` has no project references and six assemblies
depend on it — including the AST and property storage.**

```
Broiler.JavaScript.Ast            -> ExpressionCompiler
Broiler.JavaScript.Storage        -> ExpressionCompiler
Broiler.JavaScript.Runtime        -> Ast, ExpressionCompiler, Storage
Broiler.JavaScript.Parser         -> Ast, ExpressionCompiler, Runtime
Broiler.JavaScript.Engine         -> Ast, ExpressionCompiler, Parser, Runtime, Storage
Broiler.JavaScript.LinqExpressions-> Ast, Engine, ExpressionCompiler, Runtime, Storage
```

It contains `System.Reflection.Emit` in ~40 files. **So there is no subset of the current
graph that runs JavaScript without dynamic code**, which is the whole of why
`Broiler.JavaScript.Portable` exists as a separate island with zero references.

### Where `Reflection.Emit` actually lives inside it

Counted by directory, over files containing `Reflection.Emit`:

| Location | Files with `Reflection.Emit` |
|---|---|
| `Generator/` | **33** |
| `Core/` | 3 |
| `Runtime/` | 3 |
| `ExpressionCompiler.cs`, `LambdaMethodBuilder.cs`, `TypeExtensions.cs` | 3 |

**`Expressions/`, `Converters/`, `SL/` and `ClosureSeparator/` contain none.**

### The split is clean — and it has its own document now

> **Superseded by the build.** The four results below were symbol analysis; result 2 turned
> out to be **wrong** (`LinqExtensions.cs` *is* a model→emitter back-edge behind a
> convenience method — the exact failure mode it claimed was absent), and result 3
> undercounted the cuts. The split still went through. Read
> [`AssemblySplit.status.md`](AssemblySplit.status.md) §0 for what actually happened; this
> section is kept so the two can be compared.

**[`AssemblySplit.status.md`](AssemblySplit.status.md) carries the full analysis**: the
emitter's reference cluster, the 19 root files assigned one by one, the `Core/` and
`Runtime/` splits, the two residue files in `Broiler.JavaScript.Runtime`, and the sizes.
The four results that matter here:

1. **The emitter is a closed cluster**, and no model-side file names any part of it.
2. **The expression nodes have no `Compile()`** — there is no back-edge behind a convenience
   method, which is the usual way this kind of split fails.
3. **`Ast`, `Storage`, `Parser` and `Runtime` name zero emitter types between them.** Only
   **3 of 19 root files** carry `Reflection.Emit`, and only one has to be cut.
4. **The back-end contract already exists and is already public.**
   `IExpressionCompilationBackend` is declared in `Runtime/ExpressionCompilationBackend.cs`
   with both implementations `internal`. **Item A-3 is largely already done**; what remains
   of it is `AssemblySplit.md`'s step S-3.

**The entire residue is two files** — `Runtime/JSCode.cs` and
`Runtime/DictionaryCodeCache.cs` — and they are the runtime's existing seam to the compiler,
typed to one back end.

**This is symbol analysis, not a build.** `AssemblySplit.md`'s step S-0 is what finds out,
and its result belongs in that document.

### What the lower layers consume from it

`using` directives, not type counts — **A-0 must count types**:

| Assembly | Directives into `ExpressionCompiler.*` |
|---|---|
| `Ast` | 21 × `.Core`, 2 × root |
| `Storage` + `Runtime` (combined) | 5 × `.Expressions`, 2 × `.Runtime`, 2 × `.Core`, 2 × root |

---

## Size census

Non-test projects, `*.cs` outside `obj/` and `bin/`, 2026-08-07.

| Project | LOC |
|---|---:|
| `Broiler.JavaScript.BuiltIns` | **64 432** |
| `Broiler.JavaScript.Compiler` | 18 608 |
| `Broiler.JavaScript.Runtime` | 17 920 |
| `Broiler.JavaScript.ExpressionCompiler` | 11 863 |
| `Broiler.JavaScript.Parser` | 9 903 |
| `Broiler.JavaScript.Engine` | 5 536 |
| `Broiler.JavaScript` | 5 392 |
| `Broiler.JavaScript.LinqExpressions` | 4 306 |
| `Broiler.JavaScript.Ast` | 2 884 |
| `Broiler.JavaScript.Storage` | 2 693 |
| `Broiler.JavaScript.JSClassGenerator` | 1 622 |
| `Broiler.JavaScript.Clr` | 1 562 |
| `Broiler.JavaScript.Modules` | 1 251 |
| `Broiler.JavaScript.Debugger` | 1 201 |
| `Broiler.JavaScript.Network` | 776 |
| `Broiler.JavaScript.Globals` | 645 |
| `Broiler.JavaScript.Extensions` | 516 |
| `Broiler.JavaScript.Portable.Compiler` | 391 |
| `Broiler.JavaScript.Portable` | 272 |
| `Broiler.JavaScript.ModuleExtensions` | 121 |
| `Broiler.JavaScript.NodePollyfill` | 107 |
| `Broiler.JavaScript.Feature.Sample` | 36 |
| `Broiler.JavaScript.All`, `.Minimal` | 0 (meta) |

**`BuiltIns` is 43% of the engine** and five times the next largest. Its internal split:

| Inside `BuiltIns` | LOC |
|---|---:|
| `Temporal/` | 12 597 |
| `Intl/` | 8 612 |
| `RegExp/` | 7 029 |
| `Generated/` | 6 897 |
| `Array/` | 6 758 |
| `Date/` | 2 115 |
| `Iterator/` | 1 949 |
| `String/` | 1 926 |
| `Function/` | 1 791 |
| `Number/` | 1 653 |
| `Objects/` | 1 452 |
| `Promise/` | 1 269 |
| `Json/` | 1 173 |
| `Proxy/` | 952 |
| everything else | ~8 000 |

`Temporal` + `Intl` alone are **21 209 lines**, and `Measurement.md` already records that
the `Full` bootstrap profile realizes both **lazily** — the packaging split follows a
decision the engine has already taken at run time.

---

## The bytecode seed

| | |
|---|---|
| `Broiler.JavaScript.Portable` | 272 lines, **zero project references** |
| `Broiler.JavaScript.Portable.Compiler` | 391 lines, references `Ast`, `Parser`, `Portable` |
| `PortableOpCode` | **20 opcodes**, `double`-only |
| `IsTrimmable` | already `true` on `Portable` |

Before A-1, `Portable.Compiler → Parser → ExpressionCompiler` pulled in the IL emitter.
That edge is now gone, and an architecture test protects the Emit-free portable-compiler
closure. This is necessary but not sufficient for A-7: it is neither a complete runtime
profile nor a whole-program Native-AOT publish.

`samples/Broiler.JavaScript.NativeAotSample` sets `PublishAot=true` and is the place A-7's
gate grows.

---

## What the replacement A-0 / MOD-M2 must add to this file

1. **A generated project and source-edge graph**, with the two known cycles called out and
   every proposed consolidation represented by buildable project shells.
2. **A type/file assignment for FrontEnd/Semantics**, including parser, binding, scope,
   early errors, hoisting, numeric-local analysis, and shared IR; the bytecode and IL shells
   must both consume it without referencing each other.
3. **A whole-tree/profile-classified dynamic-code census**, covering analyzer warnings,
   `System.Reflection.Emit`, `Assembly.Load`, `Type.GetType`, `AssemblyCodeCache`, `ILPack`,
   generated source, host assemblies, and composition roots.
4. **Build and architecture-test results** for full IL, bytecode-only/AOT, and mixed
   composition. The preliminary 52-warning census remains useful evidence, but not the
   decision by itself.
5. **The remaining commercial/compatibility decisions:** evidence-led BuiltIns satellites
   and both A-9 rename variants. A-1 deliberately preserved current assembly/package names.
