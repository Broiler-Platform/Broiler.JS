# Broiler.JS roadmap

This file is the current, unfinished-work roadmap. Completed compliance campaigns,
performance phases, issue triage notes, and rename logs are represented by Git history
and regression tests rather than retained as active plans.

## Sources of truth

- `scripts/compliance/test262-failures.txt` is the current tracked-failure manifest.
- `docs/compliance/dashboard.md` records publishable compliance evidence.
- `docs/compliance/known-gaps.md` groups active semantic and host-coverage gaps.
- `eng/performance/phase0.json` and `eng/performance/ownership.json` define performance
  jobs and semantic owners.
- `docs/performance.md` explains how to collect comparable evidence.

Do not duplicate a changing test count here. A roadmap item closes only after a local
regression, the relevant pinned public-suite run, and an updated dashboard agree.

## 1. Close the supported test262 failure set

The checked-in failure manifest still contains real failures across RegExp, array length
limits and mutation, comments/regular-expression literals, `continue`, direct `eval`,
lexical environments, and Annex B behavior.

For each failure cluster:

1. reproduce against the pinned suite revision;
2. reduce it to the narrowest repository test;
3. fix the owning parser, compiler, runtime, or built-in layer;
4. rerun the focused cluster and the affected full shard; and
5. remove the path from the manifest only when CI confirms the fix.

The older issue-673/675 documents were removed because they mixed closed and open
states. Any still-relevant direct-eval, `Intl.DateTimeFormat` range, SameValue, or Proxy
ordering defect must be tracked through the current failure manifest or a linked issue,
not resurrected from those snapshots.

Exit gate: the pinned supported-mode run has no unexpected failures and
`test262-failures.txt` contains no test paths.

## 2. Expand host-mode coverage

Negative-metadata execution is implemented behind `--include-negative`. The remaining
structural exclusions are:

- tests requiring richer `$262` host hooks;
- `module` tests, which need module-host execution and expected-result handling; and
- `raw` tests, which need their own harness and host semantics.

Add these modes independently. Each must report selected, skipped, passed, failed, and
timed-out totals and preserve the upstream metadata in result JSON.

Exit gate: every test262 file is either executed by an appropriate host mode or has a
specific, published scope exclusion. Release workflows enable the supported modes by
default and publish totals for the pinned suite revision.

## 3. Finish RegExp backend adoption

Broiler.Regex is routed only for a conservative set of semantic gaps; the .NET
translator still handles the rest and still owns `Split`/`Replace`.

The component-owned work is tracked in
[`Broiler.Regex/docs/roadmap.md`](../Broiler.Regex/docs/roadmap.md).
Broiler.JS owns the integration gate:

- route only features the native engine implements and tests;
- compare both backends during expansion;
- move `Exec`, `Split`, and `Replace` to one match-data abstraction; and
- retire the translator only after the pinned RegExp corpus is clean.

## 4. Performance and deployment evidence

The optimization implementation phases are complete, but release evidence and several
product decisions remain:

- collect repeatable baselines on Windows x64, Linux x64, and Linux Arm64;
- exercise x64 with AVX2 enabled and disabled and Arm64 with AdvSimd where claimed;
- keep allocation, latency, working set, publish bytes, and code size together;
- resolve or explicitly scope linker warnings before claiming trimmed support;
- remove legacy magic-name assembly probing after a documented compatibility window;
- decide whether feature satellites beyond the sample materially improve startup and
  working set; and
- keep function tiering, tagged-value experiments, and the portable Native AOT subset
  opt-in until their supported semantics and fallback behavior are release-tested.

No performance change closes on a one-machine smoke result. Use the repeatability and
semantic gates in `docs/performance.md`.

## 5. API, package, and preview readiness

- Keep `docs/public-api.md` aligned with shipped assemblies and bootstrap profiles.
- Add pristine-consumer tests for every package intended for external use.
- Document breaking changes to assembly or bootstrap behavior before release.
- Run the complete repository, compliance, packaging, trim, and benchmark gates.
- Update `HUMAN_REVIEW.md` for the exact release commit and scope.

Broiler.JS is not a security sandbox. Compliance and performance completion must never
be presented as isolation of untrusted scripts.
