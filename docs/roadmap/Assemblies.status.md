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
| Items started | **0** |
| Items landed | **0** |
| Blocked on | item **A-0**, of which **sub-question 1 is now answered** — see below |

**A-0's first sub-question — is the `ExpressionCompiler` split clean? — is answered: yes**,
by symbol analysis on 2026-08-07. **It, and item A-1, now have their own roadmap:**
[`AssemblySplit.md`](AssemblySplit.md) and
[`AssemblySplit.status.md`](AssemblySplit.status.md), which carry the file-by-file
assignment, the steps and the exit gate.

A-0's other four sub-questions are still open, and **the decisive one remains sub-question
3** — the AOT warning count for `Runtime` + `Engine`. `AssemblySplit.md`'s step S-6 produces
the first half of that answer for the new model project.

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

1. **Whether the A-1 split is clean** — build it and record what breaks, starting with the
   six `Reflection.Emit` files in `Core/` and `Runtime/`.
2. **Type-level consumption**, not `using` counts, for `Ast`, `Storage`, `Runtime`, `Parser`.
3. **The AOT warning count for `Runtime` + `Engine` today**, with `IsAotCompatible` and the
   trim analyzer turned on. **This is the number that decides whether the plan is sound**: if
   the runtime is deeply reflective for reasons unrelated to the emitter, the requirement
   needs re-scoping and phases 6–9 need re-pricing.
4. **The `LinqExpressions` split** — how much of its 4 306 lines is backend-neutral.
5. **A price for A-9**, both variants: assembly/package ids only, versus the full namespace
   rename.
