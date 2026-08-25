# Assembly split — `ExpressionCompiler` into model and emitter

`Broiler.JavaScript.ExpressionCompiler` has been structurally split into the
**expression-tree model** (`Broiler.JavaScript.Expressions`) and the **IL emitter**. This
removed the emitter from the portable-compiler reference closure. It is a necessary
precondition for a bytecode-only Native-AOT configuration, not proof that such a complete
configuration publishes or runs.

> **Implementation state:** S-0 through S-6 have landed; S-7 validation remains open. This
> document preserves the implementation plan. [`AssemblySplit.status.md`](AssemblySplit.status.md)
> is authoritative for the build's four corrections and current evidence; do not repeat
> completed structural steps from the historical wording below.

---

## Why this is one job and not part of a larger one

Before the split, `Broiler.JavaScript.ExpressionCompiler` had **no project references**, and `Ast`,
`Storage`, `Runtime`, `Parser`, `Engine` and `LinqExpressions` all depended on it. It contained
`System.Reflection.Emit` in ~40 files. **An AST assembly depended on an IL emitter**, so no
subset of that graph could parse JavaScript without a dynamic-code dependency.

The wider assembly work remains downstream of that fix, but its original Base/Core grouping
is superseded pending MOD-M2's verified graph. The split was scoped to:

- preserve namespaces and consumer `using` directives;
- separate the expression model from all Emit references at the binary boundary;
- record every build-forced deviation rather than conceal it as a move; and
- close only after identical pinned test262 and repository validation in S-7.

## What the original analysis found

The claims below are preserved inputs. The build corrected the claimed convenience-method
back-edge, number of cuts, directory counts, and registration premise; read
[`AssemblySplit.status.md`](AssemblySplit.status.md) §0 before using them.

**1 · The emitter is a closed cluster.** `ILWriter`/`ILTryBlock`/`ILWriterLabel` reference
only each other; `RuntimeAssembly` nothing; `MethodRepository`, `DeferredMethod` and
`LambdaMethodBuilder` only emitter-side files. The model names **no** emitter type, and the
expression nodes have **no `Compile()`** — there is no back-edge hiding behind a convenience
method.

**2 · Only 3 of 19 root files carry `Reflection.Emit`**, and only one of the three has to be
*cut* rather than moved: `TypeExtensions.cs`, whose Emit is a single extension method on
`TypeBuilder`.

**3 · The back-end contract already exists, and it is already public.**
`Runtime/ExpressionCompilationBackend.cs` declares

```csharp
public interface IExpressionCompilationBackend
{
    ExpressionCompilationBackend Backend { get; }
    ExpressionCompilationResult<T> Compile<T>(BExpression<T> expression, ExpressionCompilationOptions options);
}
```

with **both implementations `internal`** — `DynamicMethodExpressionCompilationBackend` and
`CollectibleAssemblyExpressionCompilationBackend`. The file splits along its own
public/internal line. *The engine already has a back-end abstraction; it has one back end's
worth of implementations behind it.*

## The two decisions taken for the landed implementation

**Decision 1 — preserve namespaces. Move assembly membership only.**

Every model type keeps its `Broiler.JavaScript.ExpressionCompiler[.Core|.Expressions|…]`
namespace and simply ships from a different assembly. Splitting a namespace across two
assemblies is ordinary in .NET, and it means **no consumer source changes at all** — not one
`using`. The whole diff becomes two `.csproj` files, a set of file moves, and two file cuts.

*That is also what makes the exit gate strong:* if behaviour can change, it will show up as a
test262 count, and there is nothing else the change could plausibly have done.

**Decision 2 — the emitter keeps the existing name; the model is the new project.**

| | Project | Contents |
|---|---|---|
| **New** | `Broiler.JavaScript.Expressions` | the model — remains a separate boundary unless MOD-M2 proves another acyclic ownership |
| **Existing, keeps its name** | `Broiler.JavaScript.ExpressionCompiler` | the emitter — candidate for the future IL backend boundary after MOD-M2 |

Extracting *outward* means the emitter's own consumers (`Compiler`, `LinqExpressions`,
`Engine`) see no project rename at all, and the new assembly is purely additive. **Do not
combine this with the `Broiler.JS.*` rename** — that is A-9, it is a breaking change to every
package id, and bundling it would destroy this change's one cheap correctness argument.

## The implemented structural target

```
  BEFORE                                    AFTER

  Ast ─────────┐                            Ast ─────────┐
  Storage ─────┤                            Storage ─────┤
  Runtime ─────┼──▶ ExpressionCompiler      Runtime ─────┼──▶ Expressions   (model, no Emit)
  Parser ──────┤     (model + EMITTER)      Parser ──────┤         ▲
  Engine ──────┤                            Engine ──────┘         │
  LinqExpr ────┘                                                   │
                                            LinqExpr ──┐           │
                                            Compiler ──┴──▶ ExpressionCompiler (EMITTER only)
```

| | Model → `Broiler.JavaScript.Expressions` | Emitter → stays in `ExpressionCompiler` |
|---|---|---|
| Directories | `Expressions/`, `Converters/`, `SL/`, `ClosureSeparator/` | `Generator/` (33 files) |
| `Core/` | 12 files — the fast-enumeration and stack utilities | `ILWriter.cs`, `ILTryBlock.cs`, `ILWriterLabel.cs` |
| `Runtime/` | — | `RuntimeAssembly.cs`, `MethodRepository.cs`, `DeferredMethod.cs`, `RuntimeMethodBuilder.cs` |
| Root | 16 files, incl. `LambdaRewriter.cs`, `DeferredCaptureLayout.cs`, `CompilationStack.cs`, `StackGuard.cs`, `IMethodBuilder.cs` | `ExpressionCompiler.cs`, `LambdaMethodBuilder.cs` |
| Cut files | `TypeExtensions.cs` (~105 lines), `Runtime/ExpressionCompilationBackend.cs` (the public half) | `TypeExtensions.CreateMethod`, `ExpressionCompilationBackend.cs` (the internal half) |
| ~LOC | **≈ 5 200** | **≈ 4 600** |
| `Reflection.Emit` | **none** | all of it |

## Steps — preserved implementation plan

| # | Step | Size |
|---|---|---|
| **S-0** | Confirm by building, not by symbols | S |
| **S-1** | Create `Broiler.JavaScript.Expressions`; move the model files | M |
| **S-2** | Cut `TypeExtensions.cs` | S |
| **S-3** | Cut `Runtime/ExpressionCompilationBackend.cs` at its public/internal line | S |
| **S-4** | Re-point the six dependent projects | S |
| **S-5** | Resolve the two residue files in `Runtime` | S–M |
| **S-6** | Lock it: an architecture test, and the AOT analyzer on the new project | S |
| **S-7** | Verify: identical test262 counts, manifest by manifest | S |

### S-0 · Confirm by building, not by symbols

**The analysis behind this document is symbol analysis. It proves that no *reference*
crosses the split; it does not prove the two projects compile.** Three things it cannot
see, and each can turn an S into an L:

1. **`internal` visibility.** Types the model and emitter share as `internal` become
   inaccessible across an assembly boundary. Either they are genuinely one side's, or they
   become `public`, or an `InternalsVisibleTo` is added — **and the third option is a
   trapdoor**: it works, it hides a coupling the split exists to remove, and it will still be
   there when someone asks why `Base` cannot be trimmed. Prefer the first two.
2. **`partial` types spanning the two sides.** A partial class with one file in `Generator/`
   and one in `Expressions/` cannot be split without merging or extracting an interface.
3. **The source generator.** `JSClassGenerator` runs against several projects; check whether
   it produces anything into `ExpressionCompiler`.

**Timebox this and record what it finds in
[`AssemblySplit.status.md`](AssemblySplit.status.md).** The cheapest way to run it is to do
S-1 crudely on a throwaway branch and read the compiler errors.

### S-1 · Create the project and move the model files

Assignment above; per-file detail in [`AssemblySplit.status.md`](AssemblySplit.status.md).

**Move files; change no code.** Namespaces stay as they are (decision 1), so a moved file
should differ only in which `.csproj` globs it. `Broiler.JavaScript.Expressions` gets
`IsTrimmable` and `IsAotCompatible` set from the start — the point is to find out what it
reports.

**`ExpressionCompiler` then references `Expressions`**, which is the edge reversal this whole
document is about.

### S-2 · Cut `TypeExtensions.cs`

Its `Reflection.Emit` is one method — `CreateMethod(this TypeBuilder, …)`, lines 82–83.
That method moves to the emitter (a new `TypeBuilderExtensions.cs` is cleaner than a partial
class spanning assemblies); the other ~105 lines move to the model. Same namespace both
sides, so callers are unaffected.

### S-3 · Cut `ExpressionCompilationBackend.cs` at its public/internal line

**The file already separates itself.** Everything `public` is backend-neutral data and the
contract; everything `internal` is an IL implementation.

| To the model | To the emitter |
|---|---|
| `enum ExpressionCompilationBackend` | `internal static class ExpressionCompilationBackends` (the factory) |
| `class ExpressionCompilationOptions` | `internal sealed class DynamicMethodExpressionCompilationBackend` |
| `class ExpressionCompilationResult<T>` | `internal sealed class CollectibleAssemblyExpressionCompilationBackend` |
| **`interface IExpressionCompilationBackend`** | |

**One thing here is not a move, and it is the only real design work in this document.**
`ExpressionCompilationBackends.Get()` is a hard `switch` over the two IL implementations.
Once the interface is model-side and the implementations are emitter-side, that switch cannot
live in the model — **so the back end must be *registered*, not resolved by a switch.**

Keep the registration minimal and explicit: a settable factory or a registry the IL assembly
populates from a module initializer. **Do not reach for reflection or assembly-name probing** —
that is precisely what defeats trimming and Native AOT, and
[`Assemblies.md`](Assemblies.md)'s item A-4 exists to remove the probing that is already
there. It is `internal`, so none of this is an API break.

**Note for later:** the enum's two values are IL-specific (`DynamicMethod`,
`CollectibleAssembly`) and it participates in the runtime's **code-cache key**. A bytecode
back end is a third value, not a special case, and phase 8's item 8-6 will want to persist
that cache — so keep the key extensible rather than boolean.

### S-4 · Re-point the six dependent projects

`Ast`, `Storage`, `Runtime`, `Parser`, `Engine` and `LinqExpressions` change their
`ProjectReference` from `ExpressionCompiler` to `Expressions`. **`Engine` and
`LinqExpressions` may legitimately need both**; `Ast`, `Storage` and `Parser` must not, and
S-6's architecture test is what keeps that true.

**No `using` changes**, by decision 1. A file touched in this step is a `.csproj`.

### S-5 · Resolve the two residue files

**These are the only places a layer below the back end touches something IL-specific.** After
S-3 the enum and options are model-side, so both may already compile unchanged — **check
before changing anything.**

| File | What it does | If it still needs work |
|---|---|---|
| `Runtime/JSCode.cs:11` | `JSCompilationOptions` takes `ExpressionCompilationBackend Backend = DynamicMethod` inside the code-cache key | S-3 makes the enum model-side; likely no change |
| `Runtime/DictionaryCodeCache.cs:148` | calls `compiler().CompileWithNestedLambdas(new ExpressionCompilationOptions { … })` | route through `IExpressionCompilationBackend` rather than the concrete entry point |

**`JSCode` / `JSCodeCompiler` / `DictionaryCodeCache` is the runtime's seam to the compiler
and it already exists** — the runtime hands a compiler a program and caches what comes back.
This step generalizes an interface rather than inventing one, which is most of what
[`Assemblies.md`](Assemblies.md)'s item A-3 was scoped to do.

### S-6 · Lock it

Two assertions, in the existing architecture-test project, **written in this step and not
later**:

- **`Broiler.JavaScript.Expressions` does not reference `System.Reflection.Emit`**, and
  neither do `Ast`, `Storage` or `Parser`.
- The transitive closure of `Ast`, `Storage` and `Parser` does not contain
  `ExpressionCompiler`.

Then turn on `IsAotCompatible` and the trim analyzer over the new project and **record the
warning count** — including zero. That number is the first real evidence about whether the
rest of [`Assemblies.md`](Assemblies.md) is cheap or expensive, and it is the thing item A-0
most wants to know.

### S-7 · Verify

**A move that changes a test count is not a move.** Run the pinned test262 manifests and
require the counts to be **identical, manifest by manifest** — not merely green. Then the
full repository suite.

If a count moves, something in S-2, S-3 or S-5 was a change rather than a move. Find it
rather than re-baselining.

## Exit gate

1. **`Broiler.JavaScript.Expressions` exists, contains no `System.Reflection.Emit`**, and
   `Ast`, `Storage` and `Parser` reference it instead of `ExpressionCompiler`.
2. **An architecture test asserts both**, and fails if either regresses.
3. **test262 counts identical over every pinned manifest.** The repository suite green.
4. **No consumer source change** outside the two cut files and the residue — the diff is
   `.csproj` files and file moves. If it is not, say why in the pull request.
5. **The AOT/trim warning count for the new project is recorded** in
   [`AssemblySplit.status.md`](AssemblySplit.status.md), whatever it is.
6. **No performance claim.** If a number moves it is measured under
   [`Measurement.md`](Measurement.md) like any other; a project-file change should not move
   one, and if it does, that is a finding.

## Risks

- **`internal` visibility and `partial` types** — the two things symbol analysis cannot see,
  and the reason S-0 exists. **`InternalsVisibleTo` is the trapdoor**: it makes the build
  pass while preserving the coupling.
- **Scope creep into A-9.** The `Broiler.JS.*` rename is a breaking change to every package
  id. Bundling it here trades a verifiable move for an unverifiable one.
- **The factory becoming reflective.** S-3's registration is the one place this change could
  quietly reintroduce the exact hazard the restructure exists to remove.
- **A large diff reviewed as a rewrite.** Land S-1 as a pure move in its own commit, with the
  cuts and the re-pointing after it, so the history shows which parts could not have changed
  behaviour.

## What this unblocks

- **[`Assemblies.md`](Assemblies.md)** — A-1's structure and A-3's explicit registration
  slice landed here. The wider A-5/A-6 shape is now gated by MOD-M2's acyclic graph and shared
  FrontEnd/Semantics extraction; A-7 remains a publish-and-run gate.
- **[Phase 6](Phase-6.md)** — the old `Portable.Compiler → Parser → ExpressionCompiler`
  emitter edge is gone. The portable-compiler closure is Emit-free, so the remaining work
  can test packaging without duplicating the split; a complete AOT runtime still depends on
  the revised assembly graph and VM scope decision.
- **Nothing in phases 0–5**, and it blocks nothing in them. `LambdaRewriter.cs` and
  `DeferredCaptureLayout.cs` — item 1-4's fix and item 1-1's capture layout — are Emit-free
  and move to the model side, so **item 1-1 can be finished before, during or after this
  split without interference.**
