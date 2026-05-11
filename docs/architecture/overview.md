# Architecture overview

Broiler.JS uses a layered architecture derived from earlier YantraJS concepts but now split into explicit assemblies with narrow module boundaries.

## Layers

1. **Storage and AST foundations**: `Broiler.JavaScript.Storage` and `Broiler.JavaScript.Ast` define reusable primitives with no dependency on concrete built-ins.
2. **Parsing and runtime model**: `Broiler.JavaScript.Parser` turns source text into AST structures, while `Broiler.JavaScript.Runtime` models JavaScript values and arguments.
3. **Engine and compiler**: `Broiler.JavaScript.Engine`, `Broiler.JavaScript.ExpressionCompiler`, and `Broiler.JavaScript.Compiler` coordinate execution, compilation, and host contexts.
4. **Feature satellites**: `Broiler.JavaScript.BuiltIns`, `Broiler.JavaScript.Modules`, `Broiler.JavaScript.ModuleExtensions`, `Broiler.JavaScript.Clr`, `Broiler.JavaScript.Extensions`, and optional host packages add behavior through module initializers and registration delegates.

## Modularity rules

- Core engine projects must not reference feature satellites such as `Broiler.JavaScript.BuiltIns`.
- Feature satellites may register additional behavior through documented delegates rather than editing core runtime types directly.
- Compliance fixes should land in the narrowest owning assembly and include tests in the matching `*.Tests` project.

## YantraJS migration status

Broiler.JS retains historical inspiration from YantraJS, but the current repository is organized around assembly boundaries, module initializers, and extracted feature packages. Future migration notes should describe behavior or API compatibility explicitly rather than assuming source-level equivalence with YantraJS.
