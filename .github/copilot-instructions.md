# Repository-wide instructions for GitHub Copilot agents

These instructions apply to every Copilot or automated coding agent acting on
this repository. They complement the more detailed
[agent profiles](../docs/agents/profiles.md) and
[usage guide](../docs/agents/usage-guide.md).

## Project shape

Broiler.JS is a modular JavaScript engine for .NET. The codebase is split into
small assemblies with narrow module boundaries; see
[`docs/architecture/overview.md`](../docs/architecture/overview.md). The most
relevant layers for agent work are:

- **Storage / AST** — `Broiler.JavaScript.Storage`, `Broiler.JavaScript.Ast`
- **Parser / Runtime** — `Broiler.JavaScript.Parser`, `Broiler.JavaScript.Runtime`
- **Engine / Compiler** — `Broiler.JavaScript.Engine`,
  `Broiler.JavaScript.ExpressionCompiler`, `Broiler.JavaScript.Compiler`
- **Feature satellites** — `Broiler.JavaScript.BuiltIns`,
  `Broiler.JavaScript.Modules`, `Broiler.JavaScript.ModuleExtensions`,
  `Broiler.JavaScript.Clr`, `Broiler.JavaScript.Extensions`
- **Tests** — every production assembly has a matching `*.Tests` project

## Modularity rules (do not violate)

- Core engine projects must not reference feature satellites such as
  `Broiler.JavaScript.BuiltIns`.
- Feature satellites must extend behavior through the documented module
  initializer / registration delegate pattern, not by editing core runtime
  types directly. See
  [`docs/architecture/module-initializers.md`](../docs/architecture/module-initializers.md)
  and [`docs/architecture/extraction-pattern.md`](../docs/architecture/extraction-pattern.md).
- Land compliance fixes in the narrowest owning assembly and add a test in the
  matching `*.Tests` project.

## Build, lint, and test commands

Restore and run the full solution test suite:

```bash
dotnet test Broiler.JS.slnx
```

Targeted validation that agents should prefer while iterating:

```bash
# Runtime / built-in regressions
dotnet build Broiler.JS/Broiler.JavaScript/Broiler.JavaScript.csproj -c Release -m:1
dotnet test  Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/Broiler.JavaScript.BuiltIns.Tests.csproj -m:1

# Strict-mode / TDZ / compiler regressions
dotnet test  Broiler.JS/Broiler.JavaScript.Compiler.Tests/Broiler.JavaScript.Compiler.Tests.csproj -m:1

# Ad-hoc script execution against a built CLI
dotnet Broiler.JS/Broiler.JavaScript/bin/Release/net8.0/BroilerJS.dll --script-host <file>
```

Compliance work (test262 etc.) is gated separately; never run the full external
suite as part of a routine change — follow
[`docs/compliance/process.md`](../docs/compliance/process.md).

## Coding conventions

- C# code targets the configuration set in `Directory.Build.props`. Do not
  silently change nullable, language version, or warning levels.
- Match existing naming and file layout — one public type per file in the
  assembly that owns it.
- Add tests next to the assembly that owns the behavior; do not relax existing
  tests to make a change pass.
- Keep ECMAScript-observable semantics aligned with the relevant spec clause.
  When fixing a compliance gap, reference the clause or test262 path in the
  test name or PR description.

## Workflow conventions

- Make the smallest change that fully addresses the task. Do not refactor
  unrelated code.
- Update documentation in `docs/` when you change observable behavior, public
  API surface, or build/test procedures.
- Run the targeted test commands listed above before reporting progress; run
  `dotnet test Broiler.JS.slnx` only when a change spans multiple assemblies.
- Never commit build artifacts (`bin/`, `obj/`, `.artifacts/`, `.cache/`),
  test262 checkouts, or local logs.

## Where to look first for common tasks

| Task                                     | Start here                                                       |
| ---------------------------------------- | ---------------------------------------------------------------- |
| Add or fix a built-in                    | `Broiler.JavaScript.BuiltIns/` + `*.BuiltIns.Tests`              |
| Parser / scanner change                  | `Broiler.JavaScript.Parser/`                                     |
| Compiler / expression lowering           | `Broiler.JavaScript.Compiler/` + `*.Compiler.Tests`              |
| Module loader / CommonJS / ESM           | `Broiler.JavaScript.Modules/` + `*.Modules.Tests`                |
| CLR interop                              | `Broiler.JavaScript.Clr/`                                        |
| Compliance dashboard / known gaps        | `docs/compliance/`                                               |
| Workflow / CI                            | `.github/workflows/`, `scripts/compliance/`                      |
