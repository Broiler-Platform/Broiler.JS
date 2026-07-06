# Performance and IL roadmap

Broiler.JS compiles JavaScript to .NET IL at runtime. The long-term performance goal is
not simply to emit more IL, but to make the path from JavaScript semantics to optimized
.NET execution easier to reason about, measure, cache, debug, and eventually precompile.

This roadmap combines two related tracks:

- runtime performance work found during the codebase audit;
- a smoother compiler platform story using Roslyn and first-party .NET IL APIs where
  they fit Broiler.JS better than local infrastructure.

## Design direction

Keep Broiler.JS semantics in Broiler.JS. Roslyn should not become the normal
`JavaScript -> C# -> IL` runtime path, because JavaScript semantics, dynamic binding,
realm behavior, proxies, abrupt completions, and ECMAScript edge cases are already
owned by Broiler's parser, runtime, and compiler layers.

Instead, shape the compiler around an explicit backend boundary:

```text
JavaScript source
  -> Broiler parser / AST
  -> semantic lowering
  -> Broiler IR
  -> .NET backend
       - DynamicMethod backend for short-lived eval/function bodies
       - collectible assembly backend for larger cacheable code
       - metadata/PDB backend for persistent cache artifacts
       - optional Roslyn C# backend for debugging and diagnostics
       - offline/precompiled backend for Native AOT scenarios
```

Current code already points in this direction:

- `Broiler.JavaScript.ExpressionCompiler` defines a compact expression model and IL
  generator.
- `Broiler.JavaScript.Compiler` owns JavaScript lowering and compiler services.
- `AssemblyCodeCache` already uses collectible dynamic assemblies for generated code.
- `Broiler.JavaScript.JSClassGenerator` already uses Roslyn source-generator patterns
  for runtime registration infrastructure.

## Phase 0: measurement baseline

Add repeatable benchmarks before optimizing shared runtime behavior.

Initial project:

- `Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks`

Target benchmark groups:

- `JSContext` creation and first-use initialization.
- Compile cache hit, miss, and eviction scenarios.
- Property get/set, `Object.keys`, `Object.values`, spread, rest, and sparse arrays.
- Array iteration callbacks and promise-heavy callback dispatch.
- RegExp, Intl, Date/Temporal construction, and formatting loops.
- Existing JInt/Dromaeo scripts as regression smoke tests, not only ad hoc stopwatch
  runs.

Acceptance gates:

- A BenchmarkDotNet project for the JavaScript engine exists in the solution.
- Benchmarks can run locally in Release without external services.
- Each later phase records before/after numbers for at least one benchmark group.

Run a focused smoke check:

```bash
dotnet run -c Release --project Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks -- --filter *ScriptEvaluationBenchmarks.EvalCacheHit*
```

## Phase 1: low-risk runtime wins

Focus on changes with small semantic blast radius and clear hot-path potential.

Completed slice:

- Object spread/rest ordinary-object element copying now enumerates stored sparse
  indices instead of scanning through `elements.Length`.
- Regression tests cover high sparse array indices and CopyDataProperties key-snapshot
  behavior for accessors that add later indices during copying.
- The default dictionary code cache now uses a structural source-span key instead of
  rebuilding a large interpolated string key for each lookup, while preserving serialized
  miss-time compilation for existing compiler metadata caches.

Candidates:

- Replace dense scans over sparse element storage in object spread/rest and related
  copy paths with stored-key enumeration.
- Add tests for high sparse indices, getters, holes, symbols, and property order.
- Hoist repeated `KeyStrings.GetOrCreate("literal")` calls into generated or static
  readonly key fields where the key is known at compile time.
- Rework generated-code cache keys so cache lookup does not rebuild large interpolated
  strings on every access.
- Reduce allocation churn in `Arguments` construction where built-ins repeatedly call
  small fixed-arity helpers.

Acceptance gates:

- Sparse element paths avoid length-sized loops.
- Conformance tests cover the optimized observable behavior.
- Benchmarks show no regression in ordinary dense arrays or object copy operations.

## Phase 2: Roslyn generators and analyzers

Use Roslyn where it is strongest: compile-time code shaping, validation, and developer
feedback.

Completed slice:

- Added analyzer `BJS0001` to the existing `Broiler.JavaScript.JSClassGenerator`
  analyzer package. It reports informational diagnostics for semantic
  `Broiler.JavaScript.Storage.KeyStrings.GetOrCreate("literal")` calls inside loops,
  so stable hot-path keys can be hoisted into generated, static, or local `KeyString`
  values.
- The diagnostic intentionally starts at `Info` severity and skips generated code, so
  it can guide optimization work without blocking normal builds. Projects can promote
  it later through analyzer configuration once the first cleanup pass is complete.

Source-generator opportunities:

- Generate `KeyString` constants for known built-in names and option names.
- Generate prototype registration tables and property descriptors from attributes.
- Generate Intl, Temporal, and built-in option maps from declarative metadata.
- Generate fast switch tables for stable built-in names instead of repeated map lookups.

Analyzer opportunities:

- Warn when hot code calls `KeyStrings.GetOrCreate` with a literal inside loops or
  built-in registration paths.
- Warn when sparse element storage is traversed through length-sized loops.
- Warn when built-ins allocate avoidable `Arguments` wrappers in fixed-arity paths.
- Warn when new compiler infrastructure depends directly on `Reflection.Emit` without
  going through the backend abstraction.
- Warn when runtime-codegen-only APIs are used in code that is intended to support
  Native AOT or offline compilation.

Acceptance gates:

- Generator output is deterministic and covered by snapshot or integration tests.
- Analyzer warnings are documented and can be promoted to errors for selected projects.
- Existing source-generator work is extended instead of duplicated.

## Phase 3: backend boundary and IL diagnostics

Make the IL pipeline easier to inspect and evolve.

Completed slice:

- Added an expression-compiler backend boundary around the existing Broiler expression
  tree compilation step. `ExpressionCompilationBackend.DynamicMethod` remains the
  default runtime path, and `ExpressionCompilationBackend.CollectibleAssembly` exposes
  the existing collectible assembly path as a named backend choice.
- Added `ExpressionCompilationOptions` and `ExpressionCompilationResult<T>` so callers
  can request IL/expression diagnostics explicitly through `CaptureDiagnostics`.
- Runtime nested-lambda compilation now avoids creating unused trace writers unless
  diagnostics are requested.
- Tests cover both backend choices for the same expression tree and verify that the
  dynamic-method backend can return textual IL/expression diagnostics.

Work items:

- Define a small backend interface below Broiler's semantic IR.
- Keep the existing `ILGenerator` path as the primary JIT runtime backend.
- Prefer `DynamicMethod` for short-lived generated bodies that do not need assembly
  identity or persistence.
- Keep collectible assemblies for larger generated units and cacheable code.
- Evaluate `System.Reflection.Metadata.Ecma335.MetadataBuilder` for persistent cache
  and PDB generation, with the goal of reducing custom IL-packaging surface area over
  time.
- Add an optional Roslyn C# backend that emits readable C# for diagnostics, debugging,
  and compiler test reduction. This backend is a tool, not the hot execution path.
- Add an IL verification/debug dump mode that can be enabled per compiled script.

Acceptance gates:

- The JavaScript compiler can target at least two backend implementations in tests.
- Debug output can map generated IL or C# back to source locations.
- Persistent compiled artifacts have a clear compatibility/version key.

## Phase 4: context startup and built-in initialization

Reduce per-context setup cost without weakening realm isolation.

Completed slice:

- Hoisted stable `KeyString` lookups out of `JSContext` construction and the default
  built-in registry's feature-gating/iterator setup path. This removes repeated
  string-to-key map lookups for fixed names such as `Array`, `Promise`, `Iterator`,
  `structuredClone`, `fromBase64`, `groupBy`, and `values` from each new context.
- Added a startup-surface regression test that checks built-in global descriptors and
  default-disabled feature gates after context creation.
- Deferred true lazy Temporal/Intl namespace materialization to a later slice because
  preserving data-property descriptors, enumeration order, and per-realm constructor
  identity requires a lazy data-property representation in object storage rather than
  replacing data properties with accessors.

Candidates:

- Split immutable built-in metadata from per-realm mutable object state.
- Lazily initialize heavy feature areas such as Intl, Temporal, and optional modules.
- Use generated registration tables to reduce reflection and string-key work during
  context construction.
- Explore cloneable realm templates only after tests prove that prototypes, accessors,
  intrinsics, and user mutation remain isolated per context.

Acceptance gates:

- `JSContext` creation has a benchmark target and measured improvement.
- Realm isolation tests cover mutation of prototypes, globals, accessors, and feature
  flags across multiple contexts.
- Lazy initialization does not change observable property order or availability.

## Phase 5: AOT and offline compilation path

Native AOT and other restricted environments cannot rely on runtime IL generation as
the only execution strategy.

Direction:

- Keep the normal JIT-enabled runtime path optimized for desktop/server .NET.
- Add an offline/precompiled mode that can compile known scripts before deployment.
- Keep or improve an interpreter/fallback path for dynamic `eval` when runtime codegen
  is unavailable.
- Use analyzers to keep runtime-codegen dependencies out of assemblies meant to work
  in AOT-hosted configurations.

Acceptance gates:

- Runtime-codegen requirements are documented per package.
- AOT-sensitive APIs are isolated behind explicit capability checks.
- A small offline compilation proof of concept can execute a precompiled script without
  `Reflection.Emit` at runtime.

## Priority order

1. Build the benchmark baseline.
2. Fix sparse element copy paths and cache-key allocation issues.
3. Extend Roslyn source generation for key constants and registration tables.
4. Add analyzers that keep known performance traps from reappearing.
5. Introduce the backend boundary while preserving the current ILGenerator path.
6. Move persistent assembly output toward first-party metadata/PDB APIs.
7. Add offline/AOT capability only after the backend split is stable.

## Risks to manage

- JavaScript observable behavior can make obvious-looking optimizations invalid.
  Proxy traps, getters, property order, holes, symbols, and abrupt completions must be
  covered before landing storage or enumeration changes.
- Roslyn-generated code can make builds faster to run but harder to understand if the
  generated surface is too large. Keep generated code deterministic, documented, and
  inspectable.
- Backend abstraction should not make the hot IL path slower. Introduce it at compile
  boundaries, not inside per-instruction emission loops.
- Persistent compiled artifacts need versioning tied to Broiler runtime semantics, not
  only source text.

## Useful Microsoft platform pieces

- Roslyn source generators and analyzers for build-time code shaping and validation.
- `Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Emit` for optional diagnostic C#
  output, not as the primary runtime compiler.
- `System.Reflection.Emit.DynamicMethod` for short-lived runtime-generated methods.
- Collectible dynamic assemblies for cacheable runtime-generated code.
- `System.Reflection.Metadata.Ecma335.MetadataBuilder` for persistent assembly and PDB
  generation.
- `System.Reflection.MetadataLoadContext` for inspecting assemblies without loading
  them into the normal execution context.
