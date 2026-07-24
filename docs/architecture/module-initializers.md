# Module initializers and bootstrap

Broiler.JS uses .NET module initializers to connect assemblies without circular project
references. They run when an assembly is loaded, not in a globally guaranteed sequence.
Every initializer must therefore be order-tolerant and safe to execute once per loaded
module.

## Main registrations

| Assembly | Initializer responsibility |
| --- | --- |
| Engine | Runtime factories, errors, execution context, object/value helpers, code-cache and compiler access |
| LinqExpressions | Expression builders and JS-to-CLR delegate conversion |
| BuiltIns | Default built-in registry, generated descriptors, concrete JS value factories, intrinsic/prototype patches |
| Globals | Append generated global registrations |
| Extensions | Register fast-property expression helpers |
| Compiler | Register the `FastCompiler` implementation with `DefaultJSCompiler` |
| CLR | Install the CLR interop implementation, proxy marshalling, and CLR module provider |
| Modules | Register the concrete arguments builder used by module lowering |

Engine also contains small initializers for `JSValue`, `CoreScript`, and
`PropertySequence` delegates. Tests have their own culture initializers; those are not
part of product bootstrap.

## Explicit host bootstrap

New hosts should use `JavaScriptBootstrap.CreateContextBuilder()` and choose a
`JavaScriptBootstrapProfile`. Explicit bootstrap makes the selected feature graph and
registry visible and avoids relying on compatibility assembly probing.

Module initializers remain necessary for internal cross-assembly seams and legacy host
compatibility. They are not a substitute for an explicit product capability model.

## Adding or changing an initializer

1. Put the delegate/interface in the lowest assembly that needs the operation.
2. Keep its signature narrow and typed.
3. Register the concrete implementation in the assembly that owns it.
4. Append to multicast-style registrations instead of replacing earlier contributors.
5. Use `??=` only where the first compatible implementation intentionally wins.
6. Make absence behavior explicit: supported fallback, missing-capability result, or a
   clear exception.
7. Add tests for more than one assembly load/use path and more than one realm.

Never assume that touching an unrelated type will load a satellite. If a required
initializer must have run before first use, the owning explicit bootstrap or a narrowly
documented compatibility loader must load that assembly.

## Diagnostics

When a delegate is unexpectedly null:

- confirm the assembly is present in the output and actually loaded;
- inspect the selected bootstrap profile and manifest;
- verify trimming did not remove a dynamically reached path;
- check that a later initializer did not overwrite an earlier registration; and
- reproduce with the explicit context builder before changing load order.

Integration tests verify the documented registry extension points and compiler/CLR
initialization. Architecture tests should fail if a refactor replaces these seams with a
reverse project reference.
