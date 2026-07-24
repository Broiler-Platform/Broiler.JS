# Contributing built-ins

Built-in implementations live in
`Broiler.JS/Broiler.JavaScript.BuiltIns/<feature>/`. Keep a new type beside the
feature it implements and follow the neighboring namespace and partial-class layout.

## Choose the owning assembly

Use `Broiler.JavaScript.BuiltIns` for ECMAScript objects, constructors, prototypes, and
their algorithms. Do not place parser grammar, generic runtime storage, compiler
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
