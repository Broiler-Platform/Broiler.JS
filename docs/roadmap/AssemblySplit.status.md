# Assembly split — status

**Not started.** What follows is the analysis [`AssemblySplit.md`](AssemblySplit.md) is
designed against: a symbol-level study of `Broiler.JavaScript.ExpressionCompiler`, run
2026-08-07.

> The evidence half of [`AssemblySplit.md`](AssemblySplit.md). **The plan document is the
> one to act from.**
>
> **This is symbol analysis, not a build.** It establishes that no *reference* crosses the
> proposed split. It does **not** establish that the two projects compile — `internal`
> visibility, `partial` types spanning both sides, and source-generator output are invisible
> to it, and each can turn an S into an L. **Step S-0 is what finds out**, and its result
> belongs in this file.

---

## State

| | |
|---|---|
| Steps started | **0** |
| Steps landed | **0** |
| Blocked on | **S-0 · Confirm by building** |
| Analysis | **complete, and it says the split is clean** |

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

## What S-0 must add to this file

1. **Whether the two projects compile** — the whole point of S-0.
2. **Every `internal` type the split would make inaccessible**, and for each: whose side it
   is, or whether it becomes `public`. **Record any `InternalsVisibleTo` added, and why** —
   each one preserves a coupling the split exists to remove.
3. **Any `partial` type spanning both sides.**
4. **Whether `JSClassGenerator` emits into `ExpressionCompiler`.**
5. **The AOT and trim warning count for the new model project** (step S-6), including zero.
   This is the number [`Assemblies.md`](Assemblies.md)'s item A-0 most wants, and the first
   real evidence about whether the rest of the restructure is cheap or expensive.
