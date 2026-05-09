# Broiler.JS

Broiler.JS is a modular JavaScript engine for .NET. The codebase is organized as small assemblies for parsing, AST representation, runtime values, storage, compiler services, built-ins, module loading, CLR interop, debugging, and the aggregate package.

## Documentation map

- [Public API reference](docs/public-api.md) lists supported packages, entry points, and module boundaries.
- [Architecture overview](docs/architecture/overview.md) explains the engine layers and satellite assemblies.
- [Compliance process](docs/compliance/process.md) defines how Broiler.JS is validated against public JavaScript conformance suites.
- [Compliance dashboard](docs/compliance/dashboard.md) is the maintained status report for test suites, known gaps, and regression tracking.
- [Known compliance gaps](docs/compliance/known-gaps.md) records areas that need implementation work before claiming full ECMAScript compliance.
- [Compliance roadmap to 100%](docs/compliance/roadmap-to-100-percent.md) breaks the remaining compliance work into small execution steps.

## Building and testing

Restore and run the .NET test projects with:

```bash
dotnet test Broiler.JS.slnx
```

Compliance runs are intentionally documented separately because public suites such as test262 are large external inputs and should be pinned by commit in CI or release evidence.
