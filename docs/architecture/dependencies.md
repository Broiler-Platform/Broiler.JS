# Assembly boundaries and dependencies

Project files are the dependency-graph source of truth. This document records the
intended direction and the cross-assembly seams that a contributor must preserve.

> **The first graph change has landed; the wider target remains a hypothesis.**
> `Broiler.JavaScript.Expressions` now owns the expression model and its built reference
> closure contains no `System.Reflection.Emit`. `Ast`, `Storage`, `Parser`, `Runtime`, and
> `Engine` reference that model rather than the emitter. The structural implementation is
> recorded in [`AssemblySplit.status.md`](../roadmap/AssemblySplit.status.md); its final S-7
> validation remains open.
>
> This proves an Emit-free bytecode-compiler reference closure, not a complete runnable or
> Native-AOT engine. `AssemblyCodeCache` and `ILPack` still contain Emit in other profiles,
> and the original [`Assemblies.md`](../roadmap/Assemblies.md) Base/Core merge sketch is
> cyclic. The verified project-shell graph in modernization MOD-M2 must replace that sketch
> before any further assembly move is treated as approved.

## Layers

| Layer | Main assemblies | Responsibility |
| --- | --- | --- |
| Expression foundation | `Expressions` | Backend-neutral expression model; structurally split from the IL emitter |
| IL-emission foundation | `ExpressionCompiler` | Dynamic-code emitter; optional-backend implementation, not a lower-layer foundation |
| Syntax and storage | `Ast`, `Storage`, `Parser` | Syntax trees, keys/property storage, scanning and parsing |
| Runtime | `Runtime` | JavaScript values, arguments, properties, registries, and lower-layer contracts |
| Engine | `Engine` | Contexts/realms, evaluation, host options, bootstrap, caches, and execution services |
| Lowering | `LinqExpressions`, `Compiler` | Runtime-aware expression builders and JavaScript-to-executable lowering |
| Language features | `BuiltIns`, `Globals`, `Extensions` | ECMAScript objects, global surface, and property/runtime extensions |
| Host features | `Modules`, `ModuleExtensions`, `Clr`, `Debugger`, `Network`, `NodePollyfill` | Optional host capabilities |
| Distribution | `All`, `Minimal`, CLI and samples | Package/profile composition and executable hosts |
| Alternate capability | `Portable`, `Portable.Compiler` | Limited numeric bytecode seed; its compiler now has an Emit-free reference closure |

The graph is not a perfectly linear stack: the current lowering assemblies bridge
Engine and Runtime, and several host assemblies compose BuiltIns. A new reference is
acceptable only when it follows ownership and does not force a lower layer to know a
concrete optional feature.

### Boundary hypotheses pending the verified MOD-M2 graph

The previous fifteen-assembly diagram is retained as a superseded design record in
[`Assemblies.md`](../roadmap/Assemblies.md). It must not be implemented directly:

- merging `Storage` with `Expressions` creates a return edge through `Storage → Ast →
  Expressions`;
- merging `Runtime` with `Engine` creates a return edge through `Engine → Parser → Runtime`;
  and
- moving all of `Compiler` into an IL assembly would strand binding, early-error, hoisting,
  scope, and analysis work that the IL and bytecode back ends must share.

The project-shell spike must prove an acyclic graph around these canonical hypotheses:

| Logical boundary | Candidate contents | Rule to prove |
| --- | --- | --- |
| Expression model | `Expressions` | Backend-neutral and Emit-free; already structurally established |
| Syntax and storage | `Ast`, `Storage` | Remain separate until the real edges prove a safe consolidation |
| FrontEnd/Semantics | `Parser` plus backend-neutral binding, scope, early-error, hoisting, and analysis code extracted from `Compiler`/lowering | Shared by IL and bytecode; references neither back end |
| Runtime and engine services | `Runtime`, `Engine` | Remain separate unless project shells prove a consolidation acyclic |
| IL back end | expression emitter, IL-specific lowering, `AssemblyCodeCache`, and `ILPack` | The only profile allowed to use `System.Reflection.Emit` |
| Bytecode back end | portable format, compiler, verifier, and interpreter | Depends on shared semantics, never on IL |
| Composition and features | hosting, built-ins, optional hosts, full/AOT profiles | Back ends are registered explicitly; reduced profiles publish their omissions |

The names are provisional; dependency direction and ownership are the contract. A complete
source/IL census, project-shell build, architecture tests, and actual bytecode-only
publish-and-run gate decide whether the hypotheses become the target.

## Boundary rules

- `Expressions` must remain independent of JavaScript runtime types and
  `System.Reflection.Emit`.
- **Do not add a dependency on the emitter `ExpressionCompiler` from a lower or shared
  semantic layer.** If code needs the expression model it belongs against `Expressions`; if
  it needs emitter types, it belongs in the IL profile.
- `Ast`, `Storage`, `Parser`, and `Runtime` must not construct concrete built-ins,
  globals, CLR adapters, module hosts, or debuggers.
- **Built-ins wrap BCL types; they never derive from them.** `JSValue` is the protocol every
  JavaScript value implements, and `JSObject` carries the prototype, shape, property and
  element storage a value needs. The rationale, and why deriving is impossible rather than
  merely unwise, is [Why the built-ins are not .NET types](builtins-vs-clr-types.md).
- `Engine` owns context/bootstrap policy but receives feature implementations through
  registries, manifests, interfaces, and factories.
- Built-ins belong in `BuiltIns`; host-global registration belongs in `Globals`.
- Optional hosts should reference the smallest required assemblies instead of using
  `All` internally.
- The bytecode compiler and interpreter remain independent of the dynamic-code path. Sharing
  the parser and FrontEnd/Semantics is intended; reaching the IL emitter is not.
- Nested DateTime, Regex, and Unicode components are implementation dependencies of
  feature code; they must not leak into lower-layer public contracts unnecessarily.

## Cross-assembly seams

The supported communication mechanisms are:

- `IBuiltInRegistry`, `BuiltInManifest`, and `BuiltInFeatureDescriptor`;
- `JavaScriptBootstrap`, `JavaScriptContextBuilder`, and bootstrap profiles;
- Runtime-owned typed factory delegates populated by feature initializers;
- `DefaultBuiltInRegistry` extension points;
- compiler/backend and CLR registration interfaces; and
- narrowly scoped module-initializer wiring documented in
  [Module initializers and bootstrap](module-initializers.md).

Avoid reflection by assembly/type name in new code. Existing compatibility probing and the
whole-tree dynamic-code census are tracked by the [assembly roadmap](../roadmap/Assemblies.md).

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
startup-host reports described in [performance guidance](../roadmap/Measurement.md).
