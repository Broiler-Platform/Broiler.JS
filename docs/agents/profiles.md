# Agent profiles

This document defines standardized GitHub Copilot agent profiles for Broiler.JS.
Each profile describes the scenario it targets, the assemblies it should touch,
the tests it owns, and the guardrails an agent must respect. Profiles compose
with the repository-wide rules in
[`.github/copilot-instructions.md`](../../.github/copilot-instructions.md).

See [`usage-guide.md`](usage-guide.md) for how contributors invoke these
profiles and how their effectiveness is measured.

## Profile index

| Profile                                     | Primary scenario                                                |
| ------------------------------------------- | --------------------------------------------------------------- |
| [builtins-author](#builtins-author)         | Implement or fix ECMAScript built-ins                           |
| [compiler-fixer](#compiler-fixer)           | Parser, AST, scope, and compiler/codegen changes                |
| [runtime-engineer](#runtime-engineer)       | Engine, runtime values, storage, and execution semantics        |
| [module-integrator](#module-integrator)     | Module loader, CommonJS/ESM, CLR interop, and extensions        |
| [compliance-triage](#compliance-triage)     | test262 / public-suite triage and dashboard updates             |
| [workflow-maintainer](#workflow-maintainer) | CI, scripts, packaging, and developer tooling                   |
| [docs-curator](#docs-curator)               | Documentation, public API surface, and contributor guidance     |

## builtins-author

- **Scope:** `Broiler.JavaScript.BuiltIns/`, `Broiler.JavaScript.Globals/`,
  matching `*.BuiltIns.Tests` project.
- **Use when:** adding a missing built-in, aligning a built-in with the spec,
  or closing a compliance gap in a global constructor / prototype.
- **Must do:**
  - Reference the relevant ECMAScript clause or test262 path in the test name
    or PR description.
  - Add unit coverage in `Broiler.JavaScript.BuiltIns.Tests` (or an integration
    test when cross-assembly behavior is involved).
  - Run `dotnet test Broiler.JS/Broiler.JavaScript.BuiltIns.Tests/Broiler.JavaScript.BuiltIns.Tests.csproj -m:1`.
- **Must not:** introduce a dependency from a core engine project onto
  `Broiler.JavaScript.BuiltIns`; register behavior by editing core types
  instead of using the module initializer pattern.

## compiler-fixer

- **Scope:** `Broiler.JavaScript.Parser/`, `Broiler.JavaScript.Ast/`,
  `Broiler.JavaScript.Compiler/`, `Broiler.JavaScript.ExpressionCompiler/`,
  `Broiler.JavaScript.LinqExpressions/`, matching `*.Compiler.Tests`.
- **Use when:** fixing a parse error, lexical scope / TDZ behavior, strict
  mode handling, codegen for a syntactic construct, or function metadata
  (e.g. `length`, `name`).
- **Must do:**
  - Add a focused test in `Broiler.JavaScript.Compiler.Tests`.
  - Run `dotnet test Broiler.JS/Broiler.JavaScript.Compiler.Tests/Broiler.JavaScript.Compiler.Tests.csproj -m:1`.
  - For changes that affect generated code, also run the built-ins regression
    suite to catch indirect breakage.
- **Must not:** modify AST node contracts without updating every consumer in
  the compiler and parser projects.

## runtime-engineer

- **Scope:** `Broiler.JavaScript.Engine/`, `Broiler.JavaScript.Runtime/`,
  `Broiler.JavaScript.Storage/`.
- **Use when:** changing context lifetime, value boxing/coercion, property
  descriptors, iterator protocol plumbing, or arguments-object semantics.
- **Must do:**
  - Add tests in the smallest matching `*.Tests` project that exercises the
    runtime entry point.
  - Run the built-ins and compiler suites together — runtime changes
    frequently affect both surfaces.
- **Must not:** add references from runtime/engine assemblies to feature
  satellites.

## module-integrator

- **Scope:** `Broiler.JavaScript.Modules/`, `Broiler.JavaScript.ModuleExtensions/`,
  `Broiler.JavaScript.Clr/`, `Broiler.JavaScript.Extensions/`, host packages.
- **Use when:** adjusting CommonJS/ESM resolution, assert/console/timer
  modules, CLR interop, or external host integrations.
- **Must do:**
  - Register behavior through the module initializer / registration delegate
    pattern documented in
    [`../architecture/module-initializers.md`](../architecture/module-initializers.md).
  - Add tests in `Broiler.JavaScript.Modules.Tests` or the dedicated host
    integration project.
- **Must not:** import module-extension code from a core engine project.

## compliance-triage

- **Scope:** `docs/compliance/`, `scripts/compliance/`, test262 result
  artifacts, dashboards.
- **Use when:** triaging public-suite regressions, updating the compliance
  dashboard, or carving a known gap into actionable tasks.
- **Must do:**
  - Follow [`../compliance/process.md`](../compliance/process.md) for suite
    pinning, sharding, and evidence storage.
  - Open follow-up tasks against the appropriate implementation profile
    (`builtins-author`, `compiler-fixer`, `runtime-engineer`, …) rather than
    fixing root causes in this profile.
- **Must not:** run the full external test suite as part of an unrelated
  change; never commit `.cache/test262/` or `.artifacts/`.

## workflow-maintainer

- **Scope:** `.github/workflows/`, `scripts/`, `Directory.Build.props`,
  packaging metadata, `logs/`.
- **Use when:** changing CI matrix, timeouts, caching, tooling versions, or
  the developer scripts used to drive compliance runs.
- **Must do:**
  - Keep workflow permissions least-privilege (`permissions:` block on every
    job).
  - Pin third-party actions by major version; pin test262 by commit SHA.
  - Validate workflow YAML locally before pushing (`yamllint` or
    `actionlint` if available).
- **Must not:** loosen permissions or remove pinning to make a workflow pass.

## docs-curator

- **Scope:** `README.md`, `docs/`, doc-comment surface of public APIs.
- **Use when:** clarifying a contributor flow, syncing docs with shipped
  behavior, or documenting a new public API.
- **Must do:**
  - Cross-link new pages from the documentation map in `README.md`.
  - Match the existing tone and heading depth of the surrounding docs.
- **Must not:** introduce build/test instructions that contradict
  `.github/copilot-instructions.md`; update both together if the canonical
  commands actually changed.

## Selecting a profile

If a task spans multiple profiles, pick the profile that owns the failing test
or the assembly receiving the largest change, and follow the guardrails of any
other profile whose code is also touched. When no profile fits — for example,
a one-line typo fix in `README.md` — fall back to the repository-wide
instructions.
