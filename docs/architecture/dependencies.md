# Assembly boundaries and dependencies

Project files are the dependency-graph source of truth. This document records the
intended direction and the cross-assembly seams that a contributor must preserve.

This document owns this component's assembly dependency rules. **No dependency edge runs between
this engine and Broiler.VM in either direction.** The shared JavaScript libraries this repository
hosts (`Broiler.ECMAScript.*`, planned in [`SharedLibraries.md`](../roadmap/SharedLibraries.md)) are consumed by
Broiler.VM's JavaScript profile family as well as by this engine, and they reference neither engine.
In this file, *JavaScript bootstrap profile* and *deployment/compiler composition* are separate
concepts.

*Revised 2026-10-12, at milestone SJL-0 of the shared JavaScript libraries programme (decisions
SJL-D-1 and SJL-D-9, taken on 2026-10-11). The paragraph read: "Broiler.VM is a separate component
with no dependency edge to this one in either direction, and nothing here plans an integration with
it." The engines still share no edge; what changed is that libraries built here, which belong to
neither engine, have a Broiler.VM consumer.*

> **The first graph change has landed; the wider target remains a hypothesis.**
> `Broiler.JavaScript.Expressions` now owns the expression model and its built reference
> closure contains no `System.Reflection.Emit`. `Ast`, `Storage`, `Parser`, `Runtime`, and
> `Engine` reference that model rather than the emitter. The structural implementation is
> recorded in [`AssemblySplit.status.md`](../roadmap/AssemblySplit.status.md); its final S-7
> validation remains open.
>
> This proves an Emit-free bytecode-compiler reference closure, not a complete runnable or
> Native-AOT engine. `AssemblyCodeCache` and `ILPack` still contain Emit in dynamic-code
> backend assemblies,
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
| Distribution | `All`, `Minimal`, CLI and samples | Package/bootstrap composition and executable hosts |
| Alternate capability seed | `Portable`, `Portable.Compiler` | Limited numeric JavaScript bytecode seed; its compiler now has an Emit-free reference closure. It is not the generic Broiler.VM core or WebAssembly profile |

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
| FrontEnd/Semantics | `Parser` plus backend-neutral binding, scope, early-error, hoisting, and analysis code extracted from `Compiler`/lowering | Shared by IL and bytecode; references neither back end. **It becomes the shared frontend, `Broiler.ECMAScript.Frontend`**, consumed by both of this engine's back ends (the IL compiler and the portable compiler) and by Broiler.VM's lowering; the programme's SJL-12 to SJL-15 deliver it *(revised 2026-10-12)* |
| Runtime and engine services | `Runtime`, `Engine` | Remain separate unless project shells prove a consolidation acyclic |
| IL back end | expression emitter, IL-specific lowering, `AssemblyCodeCache`, and `ILPack` | The only backend boundary allowed to use `System.Reflection.Emit` |
| Composition and features | hosting, built-ins, optional hosts, IL and NativeAOT compositions | Back ends and VM language profiles are registered explicitly; reduced compositions publish their omissions |

The names are provisional; dependency direction and ownership are the contract. A complete
source/IL census, project-shell build, architecture tests, and actual bytecode-only
publish-and-run gate decide whether the hypotheses become the target.

## Boundary rules

- `Expressions` must remain independent of JavaScript runtime types and
  `System.Reflection.Emit`.
- **Do not add a dependency on the emitter `ExpressionCompiler` from a lower or shared
  semantic layer.** If code needs the expression model it belongs against `Expressions`; if
  it needs emitter types, it belongs in the IL backend boundary.
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
- The numeric bytecode path remains independent of the dynamic-code path. Sharing the parser
  and FrontEnd/Semantics is intended; reaching the IL emitter is not.
- The Regex and Unicode components are **published packages**, not nested components:
  `BuiltIns` references `Broiler.Regex`, `Broiler.Unicode.Cldr.LocaleData` and
  `Broiler.Unicode.Segmentation`; `Runtime` references `Broiler.Unicode.Algorithms`, the Unicode area's
  code package, and embeds the tables it reads (`Runtime/Unicode/JSUnicodeTableData.g.cs`, generated from
  [`data/unicode/pins/`](../../data/unicode/pins/README.md) by the owner's generator, which
  `Runtime.Tests` alone references, as `Broiler.Unicode.Generation`). No project references a package that
  carries Unicode tables. `Broiler.Unicode.Properties` and `Broiler.Unicode.Emoji.StringProperties` still
  arrive through `Broiler.Regex`'s package dependencies until milestone SJL-11 moves Broiler.Regex onto the
  owner's code packages, and no code of this engine calls them; `UnicodeDependencyTests` holds that by the
  assemblies' references. They are implementation dependencies of feature code and must not leak into
  lower-layer public contracts unnecessarily. No project references `Broiler.DateTime`. *(Corrected
  2026-10-12: this bullet read "Nested DateTime, Regex, and Unicode components are implementation
  dependencies of feature code; they must not leak into lower-layer public contracts unnecessarily.")*
  *(Revised 2026-10-16, at milestone SJL-7.5, when the Unicode area moved to Broiler.Unicode's code
  packages under decision SJL-D-6 (c): the bullet's first sentence read "`BuiltIns` references
  `Broiler.Regex` and `Broiler.Unicode.Cldr.LocaleData`, and receives `Broiler.Unicode.Properties` and
  `Broiler.Unicode.Emoji.StringProperties` through `Broiler.Regex`'s package dependencies, calling both
  directly as well.")*
- `Ast` references the shared `Broiler.ECMAScript.Algorithms`, as `Parser`, `Runtime`, `BuiltIns`,
  `Storage` and `Expressions` do: `StringSpan`'s obsolete trims forward to its white-space set. *(Added
  2026-10-16, at milestone SJL-7.5.)*
- A shared `Broiler.ECMAScript.*` library references no `Broiler.JavaScript.*` project or package
  and nothing in Broiler.VM (the programme's rule SL1); this engine references it as any consumer
  does.

## Cross-assembly seams

The supported communication mechanisms are:

- `IBuiltInRegistry`, `BuiltInManifest`, and `BuiltInFeatureDescriptor`;
- `JavaScriptBootstrap`, `JavaScriptContextBuilder`, and bootstrap profiles;
- Runtime-owned typed factory delegates populated by feature initializers;
- `DefaultBuiltInRegistry` extension points;
- compiler/backend and CLR registration interfaces;
- the backend registration contract described above; and
- narrowly scoped module-initializer wiring documented in
  [Module initializers and bootstrap](module-initializers.md).

Avoid reflection by assembly/type name in new code. Product capability registration is explicit
at the composition root; module-initializer or satellite-load ordering is not the VM profile
model. Existing compatibility probing and the whole-tree dynamic-code census are tracked by the
[assembly roadmap](../roadmap/Assemblies.md).

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
