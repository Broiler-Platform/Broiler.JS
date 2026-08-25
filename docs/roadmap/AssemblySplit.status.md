# Assembly split — status

**Structural implementation landed through S-6; S-7 acceptance remains open.**
`Broiler.JavaScript.Expressions` exists, contains no `System.Reflection.Emit`, and the six
dependents were re-pointed as recorded below. Sections 0 and 7
below are the build's findings; sections 1–6 are the symbol analysis
[`AssemblySplit.md`](AssemblySplit.md) was designed against, run 2026-08-07, **left as
written so the two can be compared** — §0 lists the four places it was wrong.

> The evidence half of [`AssemblySplit.md`](AssemblySplit.md). Completed structural steps
> are history, not work to repeat; the current next action is S-7.
>
> **Sections 1–6 are symbol analysis, not a build.** They establish that no *reference*
> crosses the proposed split. They do **not** establish that the two projects compile —
> `internal` visibility, `partial` types spanning both sides, and source-generator output are
> invisible to them. **Step S-0 is what found out; §0 is its result.**

---

## State

| | |
|---|---|
| Steps started | **8 of 8** |
| Structural steps landed | **7** — S-0 … S-6 |
| Acceptance open | **S-7 · pinned test262 and repository validation** |
| Analysis | **complete; four corrections in §0, none fatal** |
| Decision 1 | taken as written — namespaces preserved, no consumer `using` changed |
| Decision 2 | **taken as written** — the emitter keeps `Broiler.JavaScript.ExpressionCompiler`; the model is the new `Broiler.JavaScript.Expressions` |

---

## 0 · What S-0 found

**Both projects compile, and the split is real at the binary level.** Scanning the built
assemblies for references to the three `System.Reflection.Emit` facades:

| Assembly | References |
|---|---|
| `Broiler.JavaScript.ExpressionCompiler` | `System.Reflection.Emit`, `.ILGeneration`, `.Lightweight` |
| `Broiler.JavaScript.Expressions` | **none** |

`Ast`, `Storage` and `Parser` likewise reference none, and none of the four reaches the
emitter transitively. S-6's architecture tests assert exactly this
(`AssemblySplitValidationTests`), and they are not vacuous — the emitter's three references
are what makes the negative meaningful.

### The four corrections to the analysis below

1. **§1's "no model→emitter back-edge behind a convenience method" is wrong.**
   `LinqExtensions.cs` — assigned model-side in §3 — calls `CompileInAssembly()`,
   `Compile()` and `CompileWithNestedLambdas()` on `BExpression<T>`, all three of which are
   emitter-side extension methods. It is emitter-side. So is `DynamicHelper.cs`, which wraps
   it. (`DynamicHelper` turns out to have no callers at all; it was moved rather than
   deleted, because deleting is not a move.)
2. **There were four file cuts, not two.** §3 and §4 name `TypeExtensions.cs` and
   `Runtime/ExpressionCompilationBackend.cs`. Two more were forced by the build:
   - `ExpressionCompiler.cs` declares `IMethodRepository`, and `Closures` — model-side, and
     used by four other model files — holds one. Its only Emit-typed member was
     `RegisterNew(DynamicMethod, …)`; **`DynamicMethod` derives from `MethodInfo`, which is
     `System.Reflection`, not `System.Reflection.Emit`**, so the interface moved to the model
     with that one parameter widened. `MethodRepository.RuntimeMethod.Method` widened to
     match. Every value stored is still a `DynamicMethod`.
   - `Runtime/RuntimeMethodBuilder.cs` also declares `ClosureRewriteDiagnostics`, which
     counts `LambdaRewriter`'s walk. Both sides report into it, so it was extracted to a
     model-side file of its own beside the walk it counts.
3. **The directory counts in §2 and §6 are stale.** `Generator/` holds 53 files, not 33;
   `Expressions/` holds 56. The assignment is unaffected — all 53 are emitter-side.
4. **S-3's premise is narrower than stated.** `ExpressionCompilationBackends.Get()`'s only
   caller was `RuntimeAssembly.CompileWithNestedLambdas`, which is *itself* emitter-side — so
   the `switch` **could** have stayed put, emitter-side, and compiled. Registration was
   implemented anyway, as directed, and it earned its keep: it is what let
   `Broiler.JavaScript.Runtime` drop its emitter reference entirely rather than keep it for
   the one call in `DictionaryCodeCache`. S-4 asked for that; without S-3 it was not
   available.

### `internal` visibility — every member the split made inaccessible

**No `InternalsVisibleTo` was added.** The one that already existed
(`InternalsVisibleTo("Broiler.JavaScript.BuiltIns")`, for `JSClassBuilder.Initialize`) moved
to the model with `AssemblyInfo.cs`, because the type it exists for moved; the emitter now
grants none at all. Everything else became `public`:

| Member | Side that lost access | Why it cannot be one side's |
|---|---|---|
| `TypeExtensions` (the class) | emitter | `ILWriter` needs `Quoted`/`GetFriendlyName`; twelve `Expressions/` files need `GetFriendlyName` |
| `Closures.repositoryField`, `.boxesField`, `.constructor` | emitter | the emitter emits access *through* these handles |
| `BExpression.CallNew` | emitter | `ExpressionCompiler.cs` and `LambdaMethodBuilder.cs` build closure-construction trees |
| `BExpression<T>.WithThis<T1>`, `BLambdaExpression.WithThis` | emitter | `RuntimeAssembly` rebinds `this` before emitting |
| `BLambdaExpression.As<T>`, `.SetupAsClosure`, `.ClosureRewritten` | emitter | the emitter drives closure setup |
| `ClosureRepository.TryGet` | emitter | `ILCodeGenerator` resolves parameters through it |
| `ILSpecializationDiagnostics.RecordDenseIntegerSwitch`, `.RecordStringHashSwitch` | emitter | the counters describe IL the emitter chose |
| `ClosureRewriteDiagnostics.Rewrote`, `.Skipped`, `.BeginRepeatWalk`, `.EndRepeatWalk`, `.CaptureCreated` | both | the model reports one counter, the emitter the other four |
| `ExpressionCompilationBackends` (the class) | emitter | it is the registry the emitter registers into |

These are engine-internal by convention, not by accessibility. That is a real widening of the
public surface and the honest price of the split; the alternative was the trapdoor.

### Partial types and the source generator

- **No `partial` type spans the split.** `LinqConverter`/`LinqConverters` are wholly within
  `Converters/` (model); `ILCodeGenerator`'s 30-odd parts are wholly within `Generator/`
  (emitter).
- **`JSClassGenerator` emits nothing into either project.** It is referenced as an analyzer by
  `Runtime`, `Engine` and `BuiltIns`; neither `ExpressionCompiler` nor `Expressions`
  references it, and neither has generated output.

### What it unblocked, measured rather than asserted

**The `Broiler.JavaScript.Portable.Compiler` reference closure is Emit-free.** Scanning that
built closure—the bytecode compiler dependency slice relevant to this split—shows:

| Assembly in the closure | `System.Reflection.Emit` |
|---|---|
| `Portable.Compiler`, `Portable`, `Parser`, `Ast`, `Storage`, `Runtime`, `Expressions` | **none, all seven** |

Before this change, `Portable.Compiler` → `Parser` → `ExpressionCompiler` put the emitter in
that closure, which is why `Broiler.JavaScript.Portable` had to exist as an island with no
references at all. **That edge is gone**, and
`Broiler.JavaScript.Portable.Tests.BytecodeClosureIsEmitFreeTests` fails if it returns.

This is not a working AOT configuration — that is [`Assemblies.md`](Assemblies.md)'s A-7 —
but it is the precondition A-7 could not previously be attempted without.

It is also **not a whole-tree claim**. `AssemblyCodeCache`, `ILPack`, shells, hosts, generated
source, and other composition profiles remain in A-4/MOD-M2's dynamic-code census. Their Emit
references must move into the approved IL backend boundary or be explicitly excluded before A-7 can
pass.

### Where the emitter reference survives, and why

`Ast`, `Storage`, `Parser`, `Runtime` and `Engine` reference the model only.
**`LinqExpressions` legitimately needs both** — `LinqExpressionsAssemblyInitializer` compiles
the trees it builds — exactly as S-4 anticipated. The remaining references are the composition
root (`Broiler.JavaScript`), `ModuleExtensions`, the benchmarks and two test projects.

**On registration and load order.** The IL back ends register from a `[ModuleInitializer]`,
which the CLR runs when the emitter assembly is first touched — not at process start. Nothing
forces that to happen before `DictionaryCodeCache` asks for a back end, so the guarantee is
compositional rather than static: every configuration that can compile JavaScript reaches the
emitter through `LinqExpressions`, whose own module initializer compiles. If some future
configuration does not, `Get()` throws naming the missing back end rather than falling back
silently — which is the intended behaviour, since a bytecode-only configuration registers a
bytecode back end instead. The 4 584 integration tests and 2 118 built-in tests all compile
through `DictionaryCodeCache`, so the path is exercised, not merely argued.

---

## 1 · The emitter is a closed cluster

Every emitter-side type, and everything outside `Generator/` that names it:

| Emitter type | Named by |
|---|---|
| `ILWriter`, `ILTryBlock`, `ILWriterLabel` | **only each other** |
| `RuntimeAssembly` | **nobody** |
| `MethodRepository` | `Runtime/ExpressionCompilationBackend.cs`, `Runtime/RuntimeMethodBuilder.cs`, `ExpressionCompiler.cs` — all emitter-side |
| `DeferredMethod` | `MethodRepository`, `RuntimeMethodBuilder` — emitter-side |
| `LambdaMethodBuilder` | `ExpressionCompiler.cs` — emitter-side |

**No model-side file names any of them.** Three further checks, all clean:

- **`Expressions/`, `Converters/`, `SL/`, `ClosureSeparator/` name no emitter type** — not
  `Generator.*`, `ILCodeGenerator`, `ILWriter`, nor `LambdaMethodBuilder`.
- **The expression nodes have no `Compile()`.** The only `.Compile()` in the project is in
  `LinqExtensions.cs`, on `System.Linq.Expressions.Expression<T>`. There is no model→emitter
  back-edge behind a convenience method, which is the usual way a split like this fails.
- **`Ast`, `Storage`, `Parser` and `Runtime` name zero emitter types between them.** They use
  the utilities in `Core/`, the model in `Expressions/`, and five root-namespace types
  (`StackGuard`, `StackSegment`, `Attributes`, `Closures`, `CompilationStack`) — **none of
  which contain `Reflection.Emit`.**

## 2 · Where `Reflection.Emit` actually is

| Location | Files containing it |
|---|---|
| `Generator/` | **33 of 33** |
| `Core/` | 3 — `ILWriter.cs`, `ILTryBlock.cs`, `ILWriterLabel.cs` |
| `Runtime/` | 3 — `RuntimeAssembly.cs`, `MethodRepository.cs`, `DeferredMethod.cs` |
| root | 3 — see below |

`Expressions/`, `Converters/`, `SL/` and `ClosureSeparator/` contain **none**.

**`Core/` is a utilities directory with three emitter files filed in it.** Its other twelve —
`DisposableAction`, `EnumerableSequence`, `FastEnumerableExtensions`, `FastEnumerator`,
`IFastEnumerable`, `IFastEnumerator`, `LinkedStack`, `LinkedStackItem`,
`ReferenceEqualityComparer`, `Sequence`, `SequenceExtensions`, `SingleElementSequence` — are
the fast-enumeration helpers the whole engine uses, and they are model-side.

## 3 · The 19 root files, assigned

Only **three** carry `Reflection.Emit`, and only one has to be cut:

| File | Emit | LOC | Side |
|---|---:|---:|---|
| `ExpressionCompiler.cs` | 1 | 189 | **emitter** |
| `LambdaMethodBuilder.cs` | 1 | 54 | **emitter** |
| `TypeExtensions.cs` | 2 | 107 | **cut** — the Emit is one method, `CreateMethod(this TypeBuilder, …)` at lines 82–83; the other ~105 lines are model |
| `DeferredCaptureLayout.cs` | 0 | 499 | model — **item 1-1's capture layout** |
| `LambdaRewriter.cs` | 0 | 416 | model — **item 1-4's quadratic fix, and the mechanism item 1-1's remaining half is blocked on** |
| `CompilationStack.cs` | 0 | 329 | model |
| `Utils.cs` | 0 | 194 | model |
| `StackSegment.cs` | 0 | 112 | model |
| `StackGuard.cs` | 0 | 80 | model — **item 1-2's recursion guard** |
| `StringHashExtensions.cs` | 0 | 46 | model |
| `Attributes.cs` | 0 | 45 | model |
| `GenericHelper.cs` | 0 | 50 | model |
| `ILSpecializationDiagnostics.cs` | 0 | 39 | model |
| `LinqExtensions.cs` | 0 | 28 | model |
| `DynamicHelper.cs` | 0 | 21 | model |
| `Closures.cs` | 0 | 15 | model |
| `LocationAttribute.cs` | 0 | 12 | model |
| `IMethodBuilder.cs` | 0 | 9 | model — the relay seam; references only `BExpression` and `IFastEnumerable` |
| `AssemblyInfo.cs` | 0 | 3 | model |

**Three of the model-side files are owned by phase 1** — `StackGuard.cs` (item 1-2),
`LambdaRewriter.cs` (item 1-4) and `DeferredCaptureLayout.cs` (item 1-1). All three are
Emit-free and move cleanly, so **the split and item 1-1 do not contend.**

## 4 · The back-end contract already exists, and it is already public

`Runtime/ExpressionCompilationBackend.cs`, 122 lines, splits along its own public/internal
line:

| Public — model side | Internal — emitter side |
|---|---|
| `enum ExpressionCompilationBackend { DynamicMethod, CollectibleAssembly }` | `static class ExpressionCompilationBackends` — a `switch` factory over the two below |
| `sealed class ExpressionCompilationOptions` — `Backend`, `CaptureDiagnostics`, `EnableJavaScriptTailCalls` | `sealed class DynamicMethodExpressionCompilationBackend` — uses `MethodRepository`, `RuntimeMethodBuilder`, `CompileToBoundDynamicMethod`, `Closures`, `LambdaRewriter` |
| `sealed class ExpressionCompilationResult<T>` — `Value`, `Backend`, `IL`, `Expression`, `HasDiagnostics` | `sealed class CollectibleAssemblyExpressionCompilationBackend` — uses `CompileInAssembly()` |
| **`interface IExpressionCompilationBackend`** — `Backend`, `Compile<T>(BExpression<T>, ExpressionCompilationOptions)` | |

```csharp
public interface IExpressionCompilationBackend
{
    ExpressionCompilationBackend Backend { get; }
    ExpressionCompilationResult<T> Compile<T>(BExpression<T> expression, ExpressionCompilationOptions options);
}
```

**The engine already has a back-end abstraction. It has one back end's worth of
implementations behind it, and both are `internal`.** That is the single most useful thing
this analysis found: [`Assemblies.md`](Assemblies.md)'s item A-3 — *declare the back-end
contract* — is largely **already done**, and what remains of it is step S-3's registration.

**The one thing that is not a move:** `ExpressionCompilationBackends.Get()` is a hard
`switch` to the two IL implementations and cannot stay on the model side. It is `internal`,
so replacing it with registration is not an API break — but it is the one place this change
could reintroduce reflective discovery, which is the hazard the whole restructure exists to
remove.

## 5 · The residue: two files in `Broiler.JavaScript.Runtime`

The only places a layer below the back end touches something IL-specific:

| | |
|---|---|
| `Runtime/JSCode.cs:11` | `JSCompilationOptions` takes `ExpressionCompilationBackend Backend = ExpressionCompilationBackend.DynamicMethod` — **inside the code-cache key** |
| `Runtime/DictionaryCodeCache.cs:148` | calls `compiler().CompileWithNestedLambdas(new ExpressionCompilationOptions { … })` |

**After S-3 the enum and the options are model-side, so both may compile unchanged** —
check before changing anything.

**`JSCode` / `JSCodeCompiler` / `DictionaryCodeCache` is already the runtime's seam to the
compiler.** The runtime hands a compiler a program and caches what comes back; the seam is
not missing, **it is typed to one back end.**

*(`Broiler.JavaScript.Runtime` has its own `TypeExtensions.cs`. It does not use
`ExpressionCompiler`'s — the apparent hit in an earlier count was that file matching its own
name.)*

## 6 · Sizes

| | ~LOC |
|---|---:|
| `Broiler.JavaScript.ExpressionCompiler` today | **11 863** |
| → model, to `Broiler.JavaScript.Expressions` | ≈ **5 200** |
| → emitter, staying in `ExpressionCompiler` | ≈ **4 600** |

The remainder is `obj/`, generated output, and the two cut files' unassigned halves.

| Directory | LOC | Side |
|---|---:|---|
| `Generator/` | 3 576 | emitter |
| `Expressions/` | 3 218 | model |
| root (19 files) | 2 248 | 16 model / 2 emitter / 1 cut |
| `Core/` | 1 270 | 12 files model / 3 emitter |
| `Runtime/` | 819 | emitter, minus the public half of one file |
| `Converters/` | 513 | model |
| `SL/` | 185 | model |
| `ClosureSeparator/` | 34 | model |

---

## 7 · The AOT and trim warning count

`Broiler.JavaScript.Expressions` was created with `IsTrimmable` and `IsAotCompatible` set from
the start, so this number is what the analyzers say about the model **as it stands**, with
nothing annotated and nothing suppressed.

**18 warnings, at 15 sites.** (A 19th, `CS0419`, is a pre-existing ambiguous `cref` in a doc
comment on `DeferredCaptureLayout.cs` and has nothing to do with trimming.)

| Code | Count | What it is |
|---:|---:|---|
| `IL2070` | 6 | `Type.GetMethod`/`GetConstructor` on a `Type` with no `DynamicallyAccessedMembers` annotation |
| `IL3050` | 4 | `MakeGenericType`, `MakeArrayType`, `MakeGenericMethod` — `RequiresDynamicCode` |
| `IL2080` | 3 | the same, reached through a field rather than a parameter |
| `IL2075` | 3 | `GetField`/`GetProperty` on an unannotated `Type` |
| `IL2060` | 1 | `MakeGenericMethod` that cannot be statically analyzed |

| Site | Warnings |
|---|---:|
| `Expressions/BExpression.cs` | 6 |
| `Expressions/JSClassBuilder.cs` | 4 |
| `ClosureSeparator/Box.cs` | 3 |
| `GenericHelper.cs` | 2 |
| `Expressions/BNewArrayExpression.cs`, `BNewArrayBoundsExpression.cs`, `TypeExtensions.cs` | 1 each |

**What this is evidence of, for [`Assemblies.md`](Assemblies.md)'s item A-0.** The count is
small and it is concentrated: two thirds of it is in three files, and every one of the 18 is
reflection over a `Type` the model already holds — not assembly probing, not
`Assembly.Load`, not anything that needs a rooting descriptor. The `IL2xxx` group is the
annotation-shaped kind: adding `DynamicallyAccessedMembers` to the `Type`-carrying members of
`BExpression` would retire most of it without changing behaviour.

**The four `IL3050`s are the ones that are not free.** `MakeGenericType`, `MakeArrayType` and
`MakeGenericMethod` are `RequiresDynamicCode`: they are warnings about Native AOT, not about
trimming, and no annotation removes them — the construction has to be avoided or the call
sites have to be reachable from concrete instantiations. That is the real question A-0 was
asking, and the answer is **four sites, all in generic/array type construction, none in the
expression walk itself.**

So: **cheap on the trim side, and bounded rather than open-ended on the AOT side.** This does
not settle whether a bytecode-only Native AOT configuration works — it settles that the
expression model is not what would stop it.

---

## 8 · Deviations from "the diff is `.csproj` files and file moves"

Exit gate 4 asks for these to be named. Beyond the four file cuts in §0:

| Change | Why it is not a move |
|---|---|
| `IMethodRepository.RegisterNew` and `MethodRepository.RuntimeMethod.Method` widened from `DynamicMethod` to `MethodInfo` | required to get the interface out of the emitter; source-compatible for callers, and every stored value is still a `DynamicMethod` |
| `ExpressionCompilationBackends.Get` resolves from a registry instead of a `switch` | S-3, as directed. Registration is an explicit call from a `[ModuleInitializer]` in the assembly that owns the implementations — no reflection, no assembly-name probing |
| `DictionaryCodeCache.Compile` calls `ExpressionCompilationBackends.Get(…).Compile(…)` instead of `CompileWithNestedLambdas(…)` | S-5. Drops one nested `CompilationStack.Run`, which ran inline and is immediately re-entered by the enclosing one |
| ~20 members widened to `public` | tabulated in §0; the alternative was `InternalsVisibleTo` |
| `M13_ExpressionCompiler_RemainsMonolithic` rewritten | it asserted the monolith this change removes. M13's "no-go on decomposition" is superseded by [`AssemblySplit.md`](AssemblySplit.md); the test now asserts the split, and the legacy validation milestones `M6`/`M7` (not modernization MOD-M6/MOD-M7) have allowed-reference lists that name the model |

**One unrelated repair.** The tree did not build at the commit this branch started from:
`cece6f2c` ("Update docs") removed `Broiler.JavaScript.Ast`'s only `ProjectReference` while
leaving `Ast` using `ExpressionCompiler.Core` types, so `Ast` and everything above it failed
with 76 `CS0246`s. S-4 supplies the reference `Ast` needs — to `Expressions` — so the repair
and the step are the same edit. The baseline for S-7 was taken with the removed line restored.
