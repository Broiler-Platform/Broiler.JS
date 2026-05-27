# Testsuite optimization opportunities

This note documents bottlenecks in the current Broiler.JS compliance testsuite
and lists actionable improvements aimed at raising the number of gaps and
glitches discovered per execution cycle. Each recommendation includes the
concrete code or process it targets and a rationale that explains the expected
impact on discovery rate, so progress can be measured against today's runs.

## Today's pipeline at a glance

The compliance pipeline ([`docs/compliance/process.md`](process.md)) is built
around three layers:

1. Repository unit/integration tests via `dotnet test Broiler.JS.slnx`.
2. The pinned `test262` runner at
   [`scripts/compliance/run_test262.py`](../../scripts/compliance/run_test262.py),
   driven by per-area manifests and by the
   `--all-script-host-verifiable` full-suite mode that is sharded by the
   `.github/workflows/test262.yml` workflow.
3. The cross-engine matrix at
   [`scripts/compliance/compare_engines.py`](../../scripts/compliance/compare_engines.py).

The discussion below focuses on layer 2 because it dominates wall-clock time and
is where most new gaps surface.

## Observed bottlenecks limiting bug/gap discovery

- **Strictly serial in-shard execution.** `run_selected_tests` in
  `scripts/compliance/run_test262.py` iterates the selected paths in a single
  `for` loop and shells out to `BroilerJS.dll` one test at a time
  (`run_test262.py:727`). On modern multi-core CI runners that leaves most CPU
  cores idle, so the only practical lever to widen breadth today is to add more
  shards (more machines), not to use the cores already paid for.
- **Deterministic, ordered shards.** Both the shard split
  (`apply_shard`, `run_test262.py:426`) and the in-shard iteration order are
  fully deterministic. Identical orderings on every run mean that
  order-sensitive flakes, hash-iteration assumptions, GC/timing-dependent bugs,
  and "first failure masks the rest" effects rarely surface unless a developer
  manually re-runs with a different selection.
- **Failure list is rerun-first but never re-prioritized inside the full run.**
  The full-script-host workflow saves failing paths to
  `scripts/compliance/test262-failures.txt` and reruns them
  first (`docs/compliance/process.md:23`), which is great for regression
  triage, but the subsequent full pass still walks every test in upstream
  order. New regressions in historically fragile areas are therefore not
  surfaced any earlier than regressions in stable areas.
- **Whole categories of upstream tests are skipped.** `docs/compliance/process.md:22`
  lists `negative` metadata tests, host-harness-dependent (`$262`) tests, and
  `module`/`raw` tests as intentionally excluded from the sharded full-suite
  run because the raw script-host runner does not yet validate them. Those
  excluded buckets are where many real conformance gaps still live; nothing in
  the current pipeline observes them at all.
- **No intra-test randomization or scenario fuzzing.** Repository tests and the
  test262 driver both execute fixed inputs, so a single run will not surface
  regressions that only appear under randomized property ordering, GC pressure,
  alternative locale data, or different number/string edge cases.

## Recommendations

The recommendations are intentionally incremental so they can land one at a
time without destabilizing the existing evidence trail. Each one is paired with
a measurable success signal that can be tracked from existing JSON summaries
produced by `run_test262.py` or from CI wall-clock data.

### 1. Parallelize within a shard with a configurable worker pool

- **Where to change.** `run_selected_tests` and `main` in
  `scripts/compliance/run_test262.py`; the workflow inputs in
  `.github/workflows/test262.yml`.
- **What to do.** Replace the inner `for` loop with a `concurrent.futures`
  process pool sized by a new `--max-workers` flag (default `1` to preserve
  current behavior, `os.cpu_count()` when opted in). Keep `run_test` itself
  unchanged so each test still runs in its own subprocess with the existing
  timeout, memory-cap, and harness-cache contracts; only the orchestration
  loop becomes parallel. Aggregate progress logs by completion order rather
  than submission order.
- **Why it raises discovery rate.** The same shard budget covers more tests in
  the same wall-clock window, so the existing nightly workflow can lift either
  the per-shard size or the per-test timeout without spending more CI minutes.
  Measurable signals: shard wall-clock time reported by
  `.github/workflows/test262.yml`, and the `executed` total in
  the JSON summary growing for a fixed shard budget.

### 2. Add seeded ordering and a `--shuffle-seed` mode

- **Where to change.** `select_paths` / `apply_shard` in
  `scripts/compliance/run_test262.py` and the workflow dispatch inputs.
- **What to do.** Add a `--shuffle-seed <int>` flag. When set, deterministically
  shuffle the selected paths with `random.Random(seed).shuffle(...)` before
  sharding, and record the seed in the JSON summary and the saved failure
  manifest. Default seed `0` preserves today's deterministic order.
- **Why it raises discovery rate.** Rotating order across nightly runs surfaces
  order-sensitive bugs (hash iteration, lazy initialization, leaked globals
  between tests) that the current fixed sequence hides. The seed in the
  summary keeps every run reproducible, which is required for the
  evidence-based process described in
  [`docs/compliance/process.md`](process.md). Measurable signal: count of new
  failures whose JSON `selection` records a seed different from `0`.

### 3. Prioritize historically fragile and recently changed areas

- **Where to change.** New helper in `scripts/compliance/run_test262.py` that
  reads `scripts/compliance/test262-failures.txt` and the
  `git diff` of `Broiler.JS/Broiler.JavaScript.*` since the previous green
  commit; integrate into `select_paths` via an opt-in `--prioritize-fragile`
  flag.
- **What to do.** Move tests under directories that map to recently modified
  built-in/runtime areas, plus the saved failure list, to the front of the
  selection before sharding. Existing rerun-first behavior continues to act on
  the failure list, but the full sweep now hits the most likely regression
  surface within the first few percent of each shard.
- **Why it raises discovery rate.** Surfacing regressions in the first minutes
  of a run, instead of after several hours of stable tests, lets shorter PR
  CI budgets find them too. Measurable signal: median index (relative to
  shard size) of the first failure per shard, exported alongside the JSON
  summary.

### 4. Close the script-host coverage blockers, starting with negative metadata

- **Where to change.** `run_test`, `select_paths`, and the script-host filters
  in `scripts/compliance/run_test262.py`; matching documentation updates in
  `docs/compliance/process.md`.
- **What to do.** Land the small pieces of host support that today exclude
  whole buckets: parse `negative:` frontmatter and treat a matching thrown
  error as a pass; teach the runner to dispatch `module`/`raw` flags to the
  appropriate Broiler entry points; add the minimum `$262` harness needed for
  the most common host-harness tests. Expose each new bucket behind its own
  flag so they can be enabled incrementally as Broiler closes the underlying
  gaps.
- **Why it raises discovery rate.** Each bucket is currently invisible to CI,
  so the very first run that enables it typically reports many new real
  failures. Measurable signal: rise in `selectedCountBeforeSharding` reported
  by `run_test262.py` after each bucket flag is enabled, and the new failures
  filed against `docs/compliance/known-gaps.md`.

### 5. Add randomized scenario coverage to repository tests

- **Where to change.** `Broiler.JS/Broiler.JavaScript.BuiltIns.Tests` and
  `Broiler.JS/Broiler.JavaScript.Compiler.Tests` (the two largest existing
  unit-test projects).
- **What to do.** Introduce a small, seeded property-based layer (for example
  one parameterized fixture per area that generates inputs from a fixed
  seed) for high-risk areas already tracked in the stored memories: arguments
  mapping, destructuring assignment values, array mutator prototype chains,
  RegExp `matchAll` iterator, JSON.parse error mapping, and property-key
  interning. Record the seed in the test name so failures are reproducible
  and surface under the existing `dotnet test` evidence command.
- **Why it raises discovery rate.** Property-based fixtures explore many more
  edge cases per execution than the current hand-written examples, and they
  are run inside the existing CI step, so no new infrastructure or budget is
  required. Measurable signal: number of distinct seeds that produced a new
  failure per release cycle.

## Putting it together

Items 1 and 2 are the highest-leverage starting point: parallelism widens
breadth on the same CI spend, and seeded ordering surfaces a category of bugs
the pipeline cannot currently see. Items 3–5 each layer on additional
discovery without changing the published evidence format. Every change above
keeps the existing reproducibility guarantees from
[`docs/compliance/process.md`](process.md): runs remain deterministic given a
seed, JSON summaries gain enough metadata (worker count, seed, prioritization
mode, enabled buckets) to reproduce any reported failure.

## Implementation status

| # | Recommendation | Status | CLI flag | Workflow wired |
|---|----------------|--------|----------|----------------|
| 1 | Parallel worker pool | ✅ Implemented | `--max-workers N` | `MAX_WORKERS` env in both workflows |
| 2 | Seeded ordering | ✅ Implemented | `--shuffle-seed N` | `SHUFFLE_SEED` env in both workflows |
| 3 | Fragile-area prioritization | ✅ Implemented | `--prioritize-fragile` | `PRIORITIZE_FRAGILE` input in full-script-host |
| 4 | Negative-metadata coverage | ✅ Implemented | `--include-negative` | `INCLUDE_NEGATIVE` input in full-script-host |
| 5 | Property-based repository tests | ✅ Implemented | — | xUnit `[Theory]` + `[MemberData]` in BuiltIns.Tests and Compiler.Tests |
