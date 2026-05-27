# Contributing to Broiler.JS

Thank you for considering contributing to Broiler.JS! This document explains
how the CI pipeline works and what to expect when you open a pull request.

## CI workflow — incremental test262 checks

The repository uses a unified `test262` workflow
(`.github/workflows/test262.yml`) that adapts its behaviour depending on the
trigger:

### Pull requests

When a pull request is opened or updated against `main`, the workflow:

1. **Detects changed assemblies** — a `detect-changes` job inspects the files
   modified by the PR and maps them to the Broiler.JS assembly names defined in
   `scripts/compliance/test262-assemblies.json`.

   | Project directory                          | Assembly name(s)              |
   |--------------------------------------------|-------------------------------|
   | `Broiler.JS/Broiler.JavaScript.Parser/`    | `parser`                      |
   | `Broiler.JS/Broiler.JavaScript.Compiler/`  | `compiler`                    |
   | `Broiler.JS/Broiler.JavaScript.Runtime/`    | `runtime`                     |
   | `Broiler.JS/Broiler.JavaScript.BuiltIns/`  | `builtins`, `intl`, `annexb`  |

2. **Runs incremental test262** — an `incremental` job runs the test262 paths
   mapped to each affected assembly.  This gives fast feedback for the most
   common changes.

3. **Runs the full suite** — only if all incremental jobs pass, the `run-full`
   job executes the complete test262 suite across all shards.

#### Edge cases — global changes

Changes to shared infrastructure files (workflow definitions, compliance
scripts, root build files, the CLI entry point, engine, linqexpressions,
modules, or globals projects) are treated as affecting **all** assemblies.  The
incremental job runs every assembly scope before the full suite proceeds.

### Push to `main` and manual dispatch

After a merge to `main` (or a manual `workflow_dispatch`), the workflow
follows the original two-phase approach:

1. **Rerun previously failed paths** — paths recorded in
   `scripts/compliance/test262-failures.txt` are executed first.
2. **Run full suite** — only proceeds when the rerun phase succeeds.
3. **Persist failures** — the updated failure list is committed back to the
   repository for the next run.

## Assembly manifest

The mapping from assembly names to test262 path prefixes lives in
`scripts/compliance/test262-assemblies.json`.  When adding a new assembly or
changing responsibilities, update this file and the `detect-changes` job in
the workflow.

## Running tests locally

```bash
# Full .NET test suite
dotnet test Broiler.JS.slnx

# test262 for a single assembly (e.g. parser)
python scripts/compliance/list_test262_assemblies.py \
  --manifest scripts/compliance/test262-assemblies.json \
  --paths-for parser --output /tmp/parser-paths.txt

python scripts/compliance/run_test262.py \
  --suite-root <path-to-test262> \
  --broiler-dll <path-to-BroilerJS.dll> \
  --path-file /tmp/parser-paths.txt \
  --shard-count 1 --shard-index 0
```
