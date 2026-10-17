# Bundle SJL-7-001 (Broiler.JS), measurement sub-bundle: part B, the Unicode area's measurement

**One evidence class: a measurement.** Its lane is a four-processor `linux-x64` virtual machine. That is not a
controlled lane in [`Measurement.md`](../../../roadmap/Measurement.md)'s sense, so in that document's classes this
sub-bundle is **smoke**. [Part B of the rule](../decision-rule.md#part-b---broilerjss-measurement-template-t5) says
a smoke run is void for the part and is not judged: **part B decides nothing, and JSD-0031 §4(b)'s fifth reason,
the owner's runtime cost, stays open for this engine** until a controlled lane runs this protocol. The run was
still taken in full, for what its conditions, its configuration checks and its arithmetic show — and its
arithmetic shows the after arm far slower on two workloads (below).

## What was run

- **Arms.** B, the base: the benchmark project of the rule commit `3916bd2b`. A, the after commit `a34b1f78`,
  whose `UnicodeBenchmarks.cs`, benchmark `Program.cs` and `BenchmarkConfiguration.cs` are the rule commit's byte
  for byte, with no file of the benchmark project changed ([`manifest.txt`](manifest.txt) checks both). A-again:
  A's build, measured again under another lane name, the A/A lane. The manifest names every assembly of each arm
  by hash, with the lane's record, and was written and made read-only at 01:42:14Z, before the first run.
- **Workloads.** The six of `UnicodeBenchmarks`: `Normalize`, `Casing` and `Identifiers`, each with its control.
- **Protocol.** Seven repetitions. In each, B, A and A-again each ran once, in an order that rotated by
  repetition. One run is one launch of the benchmark with `--filter *UnicodeBenchmarks*`,
  `BROILER_BENCHMARK_PROFILE=baseline` and `--buildTimeout 1800`, which runs all six workloads, each in
  BenchmarkDotNet's own child process, under the baseline job of `BenchmarkConfiguration.cs` at the rule commit.
  `--unicode-answers` ran before and after every run, with the host's ICU loaded: invariant globalization was off,
  as the rule asks.
- **When, and beside what.** 2026-10-17, the runs from 01:42:15Z to about 02:58Z, by `collect.py --only measure`.
  Nothing else of this programme ran on the machine: its load average was 0.17 when the step started and 1.35,
  the step's own processes, when it ended. The step's builds ran with node reuse, the MSBuild server and the
  shared compiler off, so none outlived them.

## The reading

**Clause 1 is not met: the lane is not a controlled lane, so part B is void and decides nothing.** Its other
clauses are read below for what they show.

| Clause | On this run |
|---|---|
| 2. Every run's condition passes, before and after, for every arm | **holds**: all 43 checks find the answers and the configuration equal to B's first check ([`conditions.tsv`](conditions.tsv), [`answers.json`](answers.json)) |
| 3. Every configuration check identical | **holds**: one effective configuration in all 43 checks, and one BenchmarkDotNet host environment record, with its hardware intrinsics, across all 21 runs ([`environments.txt`](environments.txt)) |
| 4. Every run completed every workload, and none has issues | **holds**: 21 runs of six workloads each, 126 rows in [`runs.tsv`](runs.tsv); no run reports a workload with issues ([`issues.tsv`](issues.tsv)) |
| 5. D <= N for `Normalize`, `Casing` and `Identifiers` | holds for `Identifiers`; **not for `Normalize` and `Casing`**, by far |

[`summary.tsv`](summary.tsv), in milliseconds, Q the median over an arm's seven runs of BenchmarkDotNet's median
time per operation:

| Workload | Q(B) | Q(A) | Q(A-again) | N | D | E(B) | E(A) |
|---|---|---|---|---|---|---|---|
| `Normalize` | 9.003 | 26.767 | 27.560 | 0.793 | **+17.764** | 7.360 | 25.132 |
| `Casing` | 9.446 | 18.337 | 18.030 | 0.307 | **+8.891** | 8.512 | 17.384 |
| `Identifiers` | 1.948 | 1.913 | 1.949 | 0.035 | -0.034 | -0.001 | 0.027 |

**What the arithmetic shows, though it decides nothing.**
- **`Normalize` is about three times slower after the cut-over, and `Casing` about twice.** Their ranges do not
  overlap: `Normalize` ran 8.68 to 9.55 ms in B's seven runs and 26.39 to 28.58 ms in A's and A-again's;
  `Casing` 9.30 to 9.90 ms in B's and 17.25 to 19.52 ms in A's and A-again's. The controls did not move
  (`NormalizeControl` 1.64, 1.64 and 1.63 ms; `CasingControl` 0.93, 0.95 and 0.99 ms), so the difference is the
  work the workloads add: E, the workload less its control, goes from 7.4 to 25.1 ms for `Normalize` and from 8.5
  to 17.4 ms for `Casing`.
- **What the arms compute differs.** Arm B normalizes with the host's ICU, native code, through
  `string.Normalize`, and cases through the runtime's casing and `CultureInfo`, over the host's Unicode 15.1 data;
  arm A runs the owner's managed normalization and case mapping over the pinned Unicode 17.0.0 tables, with the
  maximum result length checked on the whole result. Each workload's answer counts completed
  operations, the same in both arms, so the figures price the same number of operations.
- **Allocation moved with them**: `Normalize` allocated about 3.81 MB per operation in B and 5.90 MB in A;
  `Casing` about 10.75 MB in B and 17.23 MB in A. `Identifiers` allocated less after, 2.30 MB against 2.46 MB.
  The guardrails decide nothing.
- **`Identifiers` is below resolution**: |D| = 0.034 ms against N = 0.035 ms.

**What follows.** The rule's reading for a NOT MET under clause 5 is that it "leaves JSD-0031's fifth reason
standing for this engine, which SJL-7 answers before it closes". This run is void, so it is not that NOT MET; but
a controlled lane that saw what this lane saw would read NOT MET on `Normalize` and `Casing`. The plan
(`docs/roadmap/SharedLibraries.md`, the Unicode area's record) named this exposure before the cut-over. Whether the
owner's normalization and case mapping are made faster before a controlled lane is run, or the cost is accepted,
is the owner's to decide.

## Files

| File | What it is |
|---|---|
| [`manifest.txt`](manifest.txt) | the commits, the identity checks, the SHA-256 of every assembly of each arm, the protocol and the lane's record; written and made read-only before the first run |
| [`conditions.tsv`](conditions.tsv) | every answers check: repetition, lane, before or after, whether the answers and the configuration equal B's first check |
| [`answers.json`](answers.json) | B's first check: each workload's answer and the process's effective configuration |
| [`runs.tsv`](runs.tsv) | every workload of every run: BenchmarkDotNet's median and mean time per operation, allocated bytes per operation and collection counts |
| [`issues.tsv`](issues.tsv) | every run: its exit, when it started, how many workloads it reported, and BenchmarkDotNet's lines on issues |
| [`environments.txt`](environments.txt) | the distinct BenchmarkDotNet host environment and hardware-intrinsics records across all runs |
| [`summary.tsv`](summary.tsv) | Q per arm, N, D and E, in nanoseconds |
| [`runs/`](runs/) | each run's full BenchmarkDotNet JSON report and its console output, compressed |
| [`collect.log.gz`](collect.log.gz) | every command the step ran |
| [`hashes.txt`](hashes.txt) | the SHA-256 of every file the step wrote; this README was written after the step, and the bundle's `manifest.json` covers it |
