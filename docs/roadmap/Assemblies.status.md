# Assembly restructure — status

**No item has been started.** What follows is a census of the graph as it stands, read off
the project files and the source tree on 2026-08-07, and it is the evidence
[`Assemblies.md`](Assemblies.md) is designed against.

> The evidence half of [`Assemblies.md`](Assemblies.md). **The plan document is the one to
> act from.** Nothing here is a measurement in [`Measurement.md`](Measurement.md)'s sense —
> a line count and a reference edge are facts about the tree, not results — and nothing here
> is a claim about feasibility, cost or performance.

---

## State

| | |
|---|---|
| Items started | **3** — A-0, A-1, A-3 |
| Items landed | **2** — **A-0** and **A-1** |
| Partly landed | **A-3** — its step S-3 is done; re-pointing the rest of tier 0–1 remains |
| Blocked on | **nothing.** A-0 is answered and did not trigger its re-scope condition |
| Next | **A-4** — the two `Assembly.Load` probes A-0 found are invisible to the analyzer, which makes them the real blocker |

**A-1 has landed.** `Broiler.JavaScript.Expressions` exists, carries the expression model,
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

### A-0, sub-question by sub-question

| | Question | Answer |
|---|---|---|
| **1** | Is the model/emitter split clean? | **Yes, by building.** Four things symbol analysis missed, none fatal — a real model→emitter back-edge (`LinqExtensions`), two extra file cuts, and ~20 members widened to `public`. `AssemblySplit.status.md` §0 |
| **2** | What do `Runtime`, `Storage`, `Ast`, `Parser` actually consume? | **Answered by construction** — they compile against `Expressions` alone. Counting types was the proxy; the compiler is the answer |
| **3** | Can `Broiler.JS.Core` be AOT-clean at all? | **Answered — and the plan does not need re-scoping.** 52 warnings across tier 0–1, only 9 of them the kind no annotation fixes. **But the count understates the work** — see below |
| **4–5** | `LinqExpressions` split size; the rest | Not started |

### Sub-question 3 — the decisive number

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

**So A-0's re-scope trigger does not fire.** The runtime is *not* deeply reflective for
reasons unrelated to the emitter; it is reflective at its CLR-interop boundary, which is
where a JavaScript engine that binds to .NET types is expected to be.

### The caveat that matters more than the number

**Not one of the 52 is `Assembly.Load`, and that is a problem with the measurement, not a
result.** `Broiler.JavaScript.Engine` contains two name-based assembly probes —
`Core/JSEngine.cs`'s `TryLoadAssembly` (which even records
`StartupOptimizationDiagnostics.RecordCompatibilityAssemblyProbe()`) and
`FastParser/Compiler/DefaultJSCompiler.cs`'s `EnsureCompilerAssemblyLoaded`, which loads
`"Broiler.JavaScript.Compiler"` by string and runs its module constructor so a
`[ModuleInitializer]` can register the real pipeline.

**The trim analyzer emits nothing for either.** `Assembly.Load(string)` is a runtime lookup it
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

## The finding that shapes the plan

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

**`Portable.Compiler` references `Parser`, which references `ExpressionCompiler`** — so even
the bytecode compiler transitively depends on the IL emitter today. That edge is exactly what
A-1 removes, and it is why the AOT gate (A-7) cannot pass before A-1 lands.

`samples/Broiler.JavaScript.NativeAotSample` sets `PublishAot=true` and is the place A-7's
gate grows.

---

## What A-0 must add to this file

1. ~~**Whether the A-1 split is clean**~~ — **done.** It is, and A-1 has landed;
   [`AssemblySplit.status.md`](AssemblySplit.status.md) §0 records what broke and how.
2. ~~**Type-level consumption** for `Ast`, `Storage`, `Runtime`, `Parser`~~ — **moot.**
   All four compile against `Expressions` alone, which is a stronger answer than the count
   would have been.
3. **The AOT warning count for `Runtime` + `Engine` today**, with `IsAotCompatible` and the
   trim analyzer turned on. **This is the number that decides whether the plan is sound**: if
   the runtime is deeply reflective for reasons unrelated to the emitter, the requirement
   needs re-scoping and phases 6–9 need re-pricing. **Still open, and now the only blocker.**
   The model's half of the answer is 18 warnings, none of them probing — see State above.
4. **The `LinqExpressions` split** — how much of its 4 306 lines is backend-neutral. A-1
   established that *some* of it is not: it is the one tier-0/1 assembly that still needs the
   emitter, because `LinqExpressionsAssemblyInitializer` compiles the trees it builds.
5. **A price for A-9**, both variants: assembly/package ids only, versus the full namespace
   rename. **A-1 deliberately did not touch this** — the new project is
   `Broiler.JavaScript.Expressions`, not `Broiler.JS.Base`, so A-9 remains one undivided
   breaking change rather than a half-done one.
