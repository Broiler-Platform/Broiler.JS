# Assembly boundaries and dependencies

Project files are the dependency-graph source of truth. This document records the
intended direction and the cross-assembly seams that a contributor must preserve.

## Layers

| Layer | Main assemblies | Responsibility |
| --- | --- | --- |
| Expression foundation | `ExpressionCompiler` | Backend-neutral expression and IL-emission model |
| Syntax and storage | `Ast`, `Storage`, `Parser` | Syntax trees, keys/property storage, scanning and parsing |
| Runtime | `Runtime` | JavaScript values, arguments, properties, registries, and lower-layer contracts |
| Engine | `Engine` | Contexts/realms, evaluation, host options, bootstrap, caches, and execution services |
| Lowering | `LinqExpressions`, `Compiler` | Runtime-aware expression builders and JavaScript-to-executable lowering |
| Language features | `BuiltIns`, `Globals`, `Extensions` | ECMAScript objects, global surface, and property/runtime extensions |
| Host features | `Modules`, `ModuleExtensions`, `Clr`, `Debugger`, `Network`, `NodePollyfill` | Optional host capabilities |
| Distribution | `All`, `Minimal`, CLI and samples | Package/profile composition and executable hosts |
| Alternate capability | `Portable`, `Portable.Compiler` | Offline numeric bytecode and reflection-free interpreter |

The graph is not a perfectly linear stack: the current lowering assemblies bridge
Engine and Runtime, and several host assemblies compose BuiltIns. A new reference is
acceptable only when it follows ownership and does not force a lower layer to know a
concrete optional feature.

## Boundary rules

- `ExpressionCompiler` must remain independent of JavaScript runtime types.
- `Ast`, `Storage`, `Parser`, and `Runtime` must not construct concrete built-ins,
  globals, CLR adapters, module hosts, or debuggers.
- `Engine` owns context/bootstrap policy but receives feature implementations through
  registries, manifests, interfaces, and factories.
- Built-ins belong in `BuiltIns`; host-global registration belongs in `Globals`.
- Optional hosts should reference the smallest required assemblies instead of using
  `All` internally.
- `Portable` remains independent of the full runtime and dynamic-code path.
- Nested DateTime, Regex, and Unicode components are implementation dependencies of
  feature code; they must not leak into lower-layer public contracts unnecessarily.

## Cross-assembly seams

The supported communication mechanisms are:

- `IBuiltInRegistry`, `BuiltInManifest`, and `BuiltInFeatureDescriptor`;
- `JavaScriptBootstrap`, `JavaScriptContextBuilder`, and bootstrap profiles;
- Runtime-owned typed factory delegates populated by feature initializers;
- `DefaultBuiltInRegistry` extension points;
- compiler and CLR registration interfaces; and
- narrowly scoped module-initializer wiring documented in
  [Module initializers and bootstrap](module-initializers.md).

Avoid reflection by assembly/type name in new code. Existing compatibility probing is
tracked for retirement in the [roadmap](../roadmap.md).

## Changing the graph

Before adding a project reference:

1. identify the API and implementation owner;
2. check whether an existing seam already expresses the dependency;
3. verify the reference does not create an optional-feature requirement for a lower
   layer;
4. update package graphs and pristine-consumer tests; and
5. add or update architecture tests that lock the intended boundary.

For extraction work, follow the [extraction pattern](extraction-pattern.md). For new
built-ins, follow [Contributing built-ins](contributing-builtins.md).

## Validation

Run the solution tests plus the focused architecture/integration projects whenever the
graph changes:

```powershell
dotnet test Broiler.JS.slnx
dotnet test Broiler.JS/Broiler.JavaScript.Integration.Tests/Broiler.JavaScript.Integration.Tests.csproj
```

Package changes also require a pristine consumer restore/build and the full/minimal
startup-host reports described in [performance guidance](../performance.md).
