# Extraction pattern

Use this pattern when code in a lower-level assembly currently knows a concrete type
that belongs to a higher-level feature assembly.

## Dependency rule

Foundation/runtime assemblies must not reference feature assemblies to construct a
concrete built-in or host integration. The lower layer owns a narrow contract; the
higher layer supplies the implementation through an explicit registry, interface,
factory, or manifest.

Examples already in the repository include:

- runtime `JSValue` factory delegates supplied by BuiltIns;
- compiler/lower-layer builder types initialized with concrete feature types;
- `DefaultBuiltInRegistry` extension points for globals, structured clone, console, and
  iterator helpers;
- `IBuiltInRegistry` plus `BuiltInManifest` for explicit feature composition; and
- `JSEngine.ClrInterop` for the CLR implementation.

## Extraction steps

### 1. Prove ownership

Search all construction, static-member, reflection, serialization, and test references.
Decide which assembly owns the public contract and which owns the concrete behavior.
Moving files without this decision merely relocates coupling.

### 2. Define the smallest seam

Prefer, in order:

1. an existing interface or registry;
2. a manifest/descriptor for a composable feature;
3. a typed factory or delegate owned by the lower layer; or
4. a deliberately small adapter interface.

Avoid `object`, reflection by type name, service locators, and broad callbacks that expose
feature internals.

### 3. Move implementation and preserve behavior

Move the concrete type to the feature assembly and follow its current namespace policy.
Update project references so dependency flow remains downward. Source namespace
preservation can reduce churn, but there is no general cross-assembly type-forwarding
escape hatch when it would create a circular reference; treat binary compatibility as
an explicit package decision.

### 4. Register safely

Use generated registration or an explicit bootstrap manifest where possible. When a
module initializer is necessary:

- append rather than overwrite other registrations;
- make initialization idempotent;
- assume no ordering between assemblies; and
- keep the fallback/error clear when the feature assembly is absent.

`DefaultBuiltInRegistry.AddProto` remains public and static so satellites can attach
prototype methods without reversing the project-reference direction.

### 5. Verify the boundary

Add architecture tests for forbidden references and integration tests that load the
relevant assemblies, create more than one realm, and exercise both presence and absence
of optional features. Verify property order, descriptors, identity, errors, and lazy
realization where applicable.

## File-splitting guidance

Large types may be split into partial files inside one assembly when that improves
ownership without creating a new module. Use behavior-oriented names such as
`JSArrayPrototype.Iteration.cs` or `JSObject.PropertyStorage.cs`; keep fields,
constructors, and core invariants in the primary file. A partial-file split is not an
assembly extraction and must not be presented as one.

## Completion checklist

- No new upward project reference or assembly-name probe was introduced.
- The lower layer exposes only the narrow contract it needs.
- The feature works through explicit bootstrap and the supported compatibility path.
- Missing optional features fail deterministically.
- Architecture, unit, integration, package-consumer, and compliance tests pass.
- Documentation describes the current seam, not the migration history.
