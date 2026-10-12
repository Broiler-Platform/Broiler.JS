# Public API reference

Broiler.JS exposes its public API through .NET assemblies rather than a single monolith. Consumers should reference the smallest assembly that matches their integration point, or `Broiler.JavaScript.All` when they want the aggregate distribution.

## Primary packages and boundaries

| Assembly | Boundary | Public surface to use |
| --- | --- | --- |
| `Broiler.JavaScript.All` | Aggregate package | Convenience reference for applications that want the complete engine surface. |
| `Broiler.JavaScript.Minimal` | Reduced package graph | Deliberately non-conformant host package for consumers that accept the documented minimal profile. |
| `Broiler.JavaScript.Engine` | Execution host | `JSEngine`, `JSContext`, `JavaScriptBootstrap`, context builders/profiles, feature flags, and engine-level extensions. |
| `Broiler.JavaScript.Runtime` | Runtime values | `JSValue`, `Arguments`, value factories, and runtime conversion helpers. |
| `Broiler.JavaScript.Parser` | Source parsing | `FastParser` and parser primitives that produce AST structures. |
| `Broiler.JavaScript.Ast` | Syntax tree model | Expression, statement, pattern, and misc AST nodes used by parser and compiler projects. |
| `Broiler.JavaScript.Expressions` | Backend-neutral expression model | `BExpression` nodes, converters, and backend contracts used by the JavaScript compiler and execution back ends. |
| `Broiler.JavaScript.ExpressionCompiler` | Dynamic-IL emission backend | The `System.Reflection.Emit` implementation; consumers that need only the model should reference `Broiler.JavaScript.Expressions`. |
| `Broiler.JavaScript.LinqExpressions` | JavaScript expression lowering helpers | Builders that connect runtime values to the expression compiler. |
| `Broiler.JavaScript.Compiler` | Compilation | `FastCompiler` and compiler services that lower parsed programs into executable forms. |
| `Broiler.JavaScript.BuiltIns` | ECMAScript built-ins | Built-in object implementations such as `Array`, `Map`, `Set`, `Promise`, `RegExp`, `String`, `Symbol`, and typed arrays. |
| `Broiler.JavaScript.Globals` | Global functions and objects | The generated global surface registered into a full context. |
| `Broiler.JavaScript.Extensions` | Runtime/property helpers | Optional fast-property and host extension helpers used by higher layers. |
| `Broiler.JavaScript.Modules` | Module system | Module registry/cache behavior and module graph execution. |
| `Broiler.JavaScript.ModuleExtensions` | Host module extensions | Optional host integrations that extend module loading without coupling them to core execution. |
| `Broiler.JavaScript.Clr` | .NET interop | CLR binding and conversion services for host-object access. |
| `Broiler.JavaScript.Debugger` | Debugging | Debugger-facing abstractions and testable debugging support. |
| `Broiler.JavaScript.Storage` | Property storage | Property key and storage primitives used across runtime and engine code. |
| `Broiler.JavaScript.Portable` | Offline numeric bytecode runtime | Reflection-free interpreter for the explicitly limited portable numeric language. |
| `Broiler.JavaScript.Portable.Compiler` | Offline portable compiler | Converts the supported numeric JavaScript subset to validated portable bytecode before deployment. |

## Compatibility promise

The public API follows assembly boundaries: lower-level projects (`Storage`, `Ast`, `Parser`, `Runtime`) must not depend on higher-level feature assemblies. New APIs should preserve this direction so compliance work can improve individual language areas without forcing a monolithic dependency graph.

`JavaScriptBootstrapProfile.Full` is the supported full host surface.
`FullEager` is a comparison/compatibility mode, and `Minimal` intentionally omits
features. `Broiler.JavaScript.Portable` is not the full engine and must not be presented
as Native AOT support for general JavaScript. See
[Performance measurement and execution modes](roadmap/Measurement.md).

**No Broiler.VM integration of this engine is planned**: no package of this engine references
Broiler.VM, and Broiler.VM references none of this engine's packages. What is planned is the
**shared JavaScript libraries** this repository will host, `Broiler.ECMAScript.Algorithms`,
`.Intl`, `.Frontend` and the test-only `.Conformance`, which name neither engine and which both this
engine and Broiler.VM's JavaScript profile consume ([`SharedLibraries.md`](roadmap/SharedLibraries.md)).
They version separately from the `Broiler.JavaScript.*` packages, and their compatibility promise -
semantic versioning, an API baseline and package validation against the last published version,
which none of this engine's current packages has - is added here at the programme's milestone SJL-2.
`JavaScript` and `WebAssembly` are Broiler.VM **language profiles**; execution-only and
runtime-compiler choices are deployment/compiler **compositions**; neither is a
`JavaScriptBootstrapProfile`.

*Revised 2026-10-12, at milestone SJL-0 of the shared JavaScript libraries programme. The paragraph
read: "No Broiler.VM integration is planned for this component, and none changes that statement.
`JavaScript` and `WebAssembly` will be VM **language profiles**; execution-only and runtime-compiler
choices will be deployment/compiler **compositions**; neither is a `JavaScriptBootstrapProfile`.
Planned package names and APIs remain hypotheses until the this component's assembly and AOT gates
close."*

## The shared libraries' packages (`Broiler.ECMAScript.*`)

The shared JavaScript libraries live in [`Broiler.ECMAScript/`](../Broiler.ECMAScript/README.md),
apart from this engine's projects, and are consumed by this engine and by Broiler.VM's JavaScript
profile. They are not part of the `Broiler.JavaScript.*` set above and do not share its version.

| Package | State | Public surface |
| --- | --- | --- |
| `Broiler.ECMAScript.Algorithms` | Contract types only, `0.1.0-preview.1` the first version the stream will publish | `WorkCharge`, `ResultStatus`, `Failure`, `LengthLimit`, `DepthLimit` |
| `Broiler.ECMAScript.Conformance` | Test-only; not packed at this version | The corpus reader, the validator, the engine-level and library-level runners and their report |

**Their compatibility promise is stronger than this engine's**, and it is enforced rather than stated
([the conventions](../Broiler.ECMAScript/docs/conventions.md), 16):

- **Semantic versioning.** A public API removal or a binary-breaking change is a major version; a
  new API is a minor version; a correction toward the specification is a minor version with its own
  changelog entry under "Observable changes"; a data-version change is a minor version with its own
  entry under "Data". The changelog is [`Broiler.ECMAScript/CHANGELOG.md`](../Broiler.ECMAScript/CHANGELOG.md),
  with the headings Added, Changed, Observable changes and Data in every entry.
- **An API baseline, compared in both directions.** Each library's public types and members, read
  from its built assembly, are committed as `PublicAPI.txt` beside its project file. Rule SL6 fails
  the build's tests on anything public the baseline does not record and on anything the baseline
  records that no longer exists. An intended change regenerates the baseline
  (`BROILER_ECMASCRIPT_UPDATE_API=1 dotnet test Broiler.ECMAScript/Broiler.ECMAScript.Architecture.Tests`)
  and is reviewed as the API change it is.
- **Package validation** runs in every pack (`EnablePackageValidation`). Until a first version is
  published it runs without a baseline; from then on `PackageValidationBaselineVersion` names the
  last published version, and a binary-breaking change fails the pack.
- **Binary compatibility inside a major version is mandatory**, because a process that hosts both
  engines loads one copy of each shared assembly.

*Added 2026-10-12, at milestone SJL-2.13, as the paragraph above said it would be.*

## Public API changes, 2026-09-22 (module semantics, intrinsics, async ordering)

**Removed, and source-breaking for a consumer of the published preview packages.** The old
module plumbing was replaced by module records, so `JSModuleExports` (`Broiler.JavaScript.Runtime`),
`ModuleCache` (`Broiler.JavaScript.Modules`) and `JSModuleExportsBuilder`
(`Broiler.JavaScript.LinqExpressions`) are gone. They exposed an exports bag and a cache flag that
the specification's module records replace: a module now owns its environment, its namespace object
and its evaluation state, and nothing outside the engine writes an export.

**Added.** `Broiler.JavaScript.Modules` gains `JSModuleNamespace` (the module namespace exotic
object), `JSModuleImportBinding` (a live, immutable import binding), `IJSModuleEnvironment`, and the
`ModuleKind` and `ModuleStatus` enumerations that a host reads to see where a graph stands.
`Broiler.JavaScript.Runtime` gains `IAsyncDelegateIterator`, which `JSIterator` implements so that
`yield*` in an async generator can await each step of its delegate.

**Changed, and source-breaking for an external implementer.** `IJSDisposableStack`
(`Broiler.JavaScript.Runtime`) gains `DisposeStep`, `TakeAwaitValue`, `RecordDisposeError` and
`CompleteDisposal`, which `await using` needs in order to spend one job per awaited resource;
`JSDisposableStack` implements them. An implementer of that interface outside this repository has to
add them.

**Behaviour a consumer can observe without recompiling.** `Promise.resolve` now adopts thenables,
`await` resumes in the reaction job and so takes one job for a native promise where it used to take
two, and the debugger's V8 projections report 0-based columns (their line numbers stay 1-based; the
protocol's are 0-based, and that half of the convention is recorded in
[known gaps](compliance/known-gaps.md)).

## Diagnostics surface

| Type | Assembly | Consumer | Notes |
| --- | --- | --- | --- |
| `NullishAccess` | `Broiler.JavaScript.Runtime` | Hosts tuning diagnostics | Records the source text of each emitted constant-key property access so a `null`/`undefined` TypeError can name the expression it came from — `Cannot get property enabled of undefined (evaluating 'config.server.tls.enabled')`. `Enabled` (default `true`) stops new accesses being recorded; `Reset()` drops what has been recorded. No effect on evaluation, and no cost on a successful access. Descriptions are spans into source, so they keep that source reachable: a host that evaluates a stream of unrelated programs should `Reset()` between them. |

`NullishAccess` describes only the accesses that take the constant-key read and store
caches: `a.b.c`, `a.b.c = v`, `a.b.c op= v`, `a.b.c++`, and the receiver read of
`a.b.c()`. A computed access (`a[k]`), a parenthesized base, a `super` access, an
optional-chain link, and the "is not a function" raised at a call keep the message they
had. The type's own remarks say why each is excluded.

## Documentation updates for new APIs

When adding or changing a public type, update this file with the owning assembly, the intended consumer, and any compliance impact. If the API implements a specific ECMAScript feature, link the related compliance test area from `docs/compliance/process.md` or record a gap in `docs/compliance/known-gaps.md`.
