# Contributing built-ins

Built-in implementations live in
`Broiler.JS/Broiler.JavaScript.BuiltIns/<feature>/`. Keep a new type beside the
feature it implements and follow the neighboring namespace and partial-class layout.

## Choose the owning assembly

Use `Broiler.JavaScript.BuiltIns` for ECMAScript objects, constructors, prototypes, and
their algorithms. A new built-in derives from `JSObject` (or `JSPrimitive`) and
**wraps** whatever BCL type does its data — it never derives from one. Why that is a rule
rather than a habit, and why `class JSArray : System.Array` does not even compile, is
[Why the built-ins are not .NET types](builtins-vs-clr-types.md). Do not place parser grammar, generic runtime storage, compiler
lowering, host globals, CLR interop, or module loading there merely because a built-in
uses it.

If the feature can be optional, define the lowest-layer contract first and keep the
concrete implementation in the feature assembly. See
[Extraction pattern](extraction-pattern.md).

## Define the generated surface

Most constructors use one of the source-generator attributes:

```csharp
[JSClassGenerator("Example")]
public partial class JSExample : JSObject
{
}
```

Use `JSFunctionGenerator` when the global is callable as a function. Follow an existing
type for prototype methods, accessors, symbols, constructor length/name metadata, and
feature flags. `Register = false` is for types that are exposed through another
intrinsic rather than installed as a global.

Generated code and handwritten partial definitions compile into the same assembly; do
not create a second “generated built-ins” assembly.

## Registration and initialization

Normal generated built-ins are discovered through
`DefaultBuiltInRegistry` and its generated registration descriptors. Use
`BuiltInsAssemblyInitializer` only for cross-layer factories, shared intrinsic identity,
or compatibility metadata that cannot be expressed by the generator.

Available integration points include:

- `DefaultBuiltInRegistry.AdditionalRegistrations` for satellite registration;
- `ConsoleFactory`, `IntlFactory`, `StructuredCloneExtension`, and
  `IteratorPrototypeSetup` for established cross-assembly seams;
- `DefaultBuiltInRegistry.AddProto` for attaching a native prototype function; and
- `BuiltInManifest`/`BuiltInFeatureDescriptor` for explicitly composable features.

### Intrinsic constructors and prototypes

An object the engine creates itself takes the realm's intrinsic prototype, never the
current value of a global: generated `CreateClass` code records each registered class's
constructor and prototype on its `JSContext` (`RegisterIntrinsic`), and built-in code reads
them back through `Intrinsics.Prototype`/`Intrinsics.Constructor`. A class that lives on a
namespace object instead of the global (the generated Temporal classes, which are
`Register = false`, and Intl's hand-built constructors in `JSIntl`) is recorded under its
qualified name (`Temporal.PlainDate`, `Intl.NumberFormat`) by the code that builds the
namespace, and is read through `Intrinsics.TemporalPrototype` /
`Intrinsics.IntlPrototype`, which create the realm's one namespace object first when a lazy
profile has not realized it yet.

The registry is deliberately internal to the engine (`internal` on `JSContext`, visible to
the engine's own assemblies through `InternalsVisibleTo`): no host outside the engine reads
or writes it, and a public mutator would let any host code replace a realm's intrinsics.
The first registration of a name wins, so a second `CreateClass` under the same name, or a
host class generated under a built-in's name, installs an ordinary global binding without
changing what the engine creates.

Do not add a new static delegate when an existing manifest, registry, interface, or
factory contract expresses the dependency. Do not depend on module-initializer order;
initializers must be safe when assemblies load in a different order.

## Tests

Add the narrowest tests that prove the algorithm and its observable metadata:

- `Broiler.JavaScript.BuiltIns.Tests` for the built-in itself;
- parser/compiler tests when new syntax or lowering is required;
- integration tests for registration, cross-assembly identity, realms, or host behavior;
- a focused pinned test262 manifest or path for standards behavior.

Test constructor/prototype descriptors, name/length, symbols, coercion order, abrupt
completion, subclassing/species, Proxy traps, and cross-realm identity where applicable.

## Checklist

- Cite the relevant ECMAScript or ECMA-402 clause in tests or review notes.
- Preserve the dependency direction documented in
  [Architecture overview](overview.md).
- Use generated metadata for the ordinary case.
- Add explicit slow paths for Proxy/exotic behavior when optimizing.
- Update `docs/public-api.md` only when the external .NET surface changes.
- Update `docs/compliance/known-gaps.md` and the dashboard when closing or discovering a
  public-suite gap.
- Run the focused tests and the affected pinned compliance shard before merge.
