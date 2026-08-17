# Contributing to Broiler.JS

Thank you for considering contributing to Broiler.JS! This document explains
how to run the repository's conformance workflow and the same checks locally.

## CI workflow — test262

The repository uses a unified `test262` workflow
(`.github/workflows/test262.yml`). It is intentionally started manually from
GitHub Actions; it does not run automatically for pull requests or pushes.

A dispatch can select the entire supported script-host suite, one assembly,
semicolon-separated path/glob subsets, Test262 feature metadata, one shard, or
the saved failures. Timeout, memory, worker, shuffle, negative-test, and
fragile-first controls are also exposed by the dispatch form. By default each
eligible test runs twice: once unchanged and once through the workflow's
lockfile-pinned Terser using the `test262-safe-mangle-v1` syntax-minification and
identifier-mangling profile (compression is disabled). Select `minifier: none` for an
original-only diagnostic run; that narrower profile is reported but cannot rewrite the
canonical failure manifest.

For an untargeted run, the workflow can follow a two-phase approach:

1. **Rerun previously failed paths** — paths recorded in
   `scripts/compliance/test262-failures.txt` are executed first.
2. **Run the selected suite** — proceeds only when the saved failures pass.
3. **Retry abnormal shards** — a shard that produces no conclusive report is
   rerun once; ordinary test failures are not retried.
4. **Merge and report** — retry-aware JSON and Markdown artifacts are produced,
   with separate common-failure, highest-impact, and timeout reports.
5. **Persist failures** — only conclusively executed paths are refreshed in the
   tracked failure list, so a subset, single shard, or infrastructure failure
   cannot erase out-of-scope entries. Persistence is serialized and skipped if
   the branch contains source changes newer than the tested commit.
6. **Set the verdict** — a terminal job passes only when the requested full
   phase ran and its authoritative latest-attempt merge is completely green.

## Assembly manifest

The mapping from assembly names to test262 path prefixes lives in
`scripts/compliance/test262-assemblies.json`.  When adding a new assembly or
changing responsibilities, update this file and the workflow's assembly input.

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
