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

No Broiler.VM integration is planned for this component, and none changes that statement.
`JavaScript` and `WebAssembly` will be VM **language profiles**; execution-only and
runtime-compiler choices will be deployment/compiler **compositions**; neither is a
`JavaScriptBootstrapProfile`. Planned package names and APIs remain hypotheses until the
this component's assembly and AOT gates close.

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
