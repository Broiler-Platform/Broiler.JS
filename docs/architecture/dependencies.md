# Assembly boundaries and dependencies

Project files are the dependency-graph source of truth. This document records the
intended direction and the cross-assembly seams that a contributor must preserve.

> **The graph below is the one that exists. A replacement is planned and not started:**
> [`docs/roadmap/Assemblies.md`](../roadmap/Assemblies.md) re-lays the engine as
> `Broiler.JS.Base` / `.Core` / `.IL` / `.Bytecode` and their satellites, so an application
> may reference either back end or both — and a **bytecode-only application publishes as
> Native AOT**.
>
> **The rule that change turns on contradicts the first row of the table below.**
> `ExpressionCompiler` is described here as the "expression foundation", and it is: it has
> **no project references**, and `Ast`, `Storage`, `Runtime`, `Parser`, `Engine` and
> `LinqExpressions` all depend on it. **It is also the IL emitter** — `System.Reflection.Emit`
> in ~40 files — so *an AST assembly depends on an IL emitter*, and **no subset of this graph
> runs JavaScript without dynamic code.** That is why `Portable` exists as a separate island.
>
> **[`docs/roadmap/AssemblySplit.md`](../roadmap/AssemblySplit.md) plans the fix**: separate
> the expression *model* (≈5 200 lines, no `Reflection.Emit`) from the *emitter* (≈4 600
> lines, all of it), putting the first at the bottom of the graph and the second above the
> runtime. It is analyzed file by file, preserves namespaces so no consumer changes, and is
> verified by identical test262 counts. **Until it lands, treat "backend-neutral" in the
> table below as an aspiration rather than a property.**

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

### The planned layering

Fifteen assemblies in five tiers, with **strictly downward** dependencies. Planned, not
built — [`Assemblies.md`](../roadmap/Assemblies.md) is the plan and its item **A-0** may
re-scope it.

| Tier | Assemblies | Rule |
| --- | --- | --- |
| 4 · composition | `Broiler.JS.All` (IL + bytecode), `Broiler.JS.Aot` (bytecode only) | The only place a back end is chosen |
| 3 · host features | `Broiler.JS.Hosting`, `.Clr`, `.Modules`, `.ModuleExtensions`, `.Debugger`, `.Network`, `.NodePolyfill` | **`Hosting` references neither back end** |
| 2 · back ends | `Broiler.JS.IL` · `Broiler.JS.Bytecode` + `.Bytecode.Compiler` | Mutually optional; both implement a contract declared in `Core` |
| 1 · semantics | `Broiler.JS.Core`, `Broiler.JS.BuiltIns` + `.Intl` `.Temporal` `.RegExp` | **Must not reference either back end** |
| 0 · foundation | `Broiler.JS.Base`, `Broiler.JS.Ast`, `Broiler.JS.Parser` | No JavaScript semantics in `Base` |

**The rule the whole design turns on:**

> **`Broiler.JS.IL` is the only assembly permitted to reference `System.Reflection.Emit`.**
> Everything at tier 0 and tier 1 must be `IsAotCompatible` with no trim or AOT warnings, so
> that an application referencing only the bytecode back end publishes as Native AOT.

Two corollaries that are easy to violate by accident: a back end must be **registered by the
application, never discovered by assembly or type name** (reflective discovery defeats
trimming and AOT), and **no assembly should exceed roughly 36 000 lines** — `BuiltIns` is
64 432 today, which is what the `.Intl` / `.Temporal` / `.RegExp` satellites address.

Architecture tests lock all of this; see item A-8.

## Boundary rules

- `ExpressionCompiler` must remain independent of JavaScript runtime types.
- **Do not add a new dependency on `ExpressionCompiler` from a lower layer.** Every such
  edge is one the planned split has to remove, and each one makes
  [`Assemblies.md`](../roadmap/Assemblies.md)'s item A-1 larger. If a lower layer needs the
  expression *model*, say so in the pull request so the split can account for it; if it
  needs the *emitter*, it is in the wrong layer.
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
tracked for retirement in the [roadmap](../roadmap/Component.md).

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
