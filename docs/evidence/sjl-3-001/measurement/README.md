# Bundle SJL-3-001 (Broiler.JS), measurement sub-bundle: part B, the pilot's measurement

**One evidence class: a measurement.** Its lane is a four-processor `linux-x64` container. That is
not a controlled lane in [`Measurement.md`](../../../roadmap/Measurement.md)'s sense, so in that
document's classes this sub-bundle is **smoke**. [Part B of the rule](../decision-rule.md#part-b---broilerjss-measurement-template-t5)
says a smoke run is void for the part and is not judged: **clause 7's measurement half stays open**
until a controlled lane runs this protocol. The run was still taken in full, for what its conditions,
its configuration checks and its arithmetic show.

## What was run

- **Arms.** B, the base: the benchmark project of the rule commit `1410079e`, whose engine projects
  are byte-identical to the base commit `44a214dc` (`manifest.txt` checks it). A, the after commit
  `ef676932`, with the workloads, `Program.cs` and `BenchmarkConfiguration.cs` as the rule commit
  has them (`manifest.txt` checks it too). A-again: A's build, measured again under another lane name,
  the A/A lane.
- **Workloads.** The six of `NumberConversionBenchmarks`: `ToString`, `ToNumber` and `Literals`, each
  with its control.
- **Protocol.** Seven repetitions. In each, B, A and A-again each run once, in an order that rotates
  by repetition: B, A, A-again; then A, A-again, B; then A-again, B, A; and again. One run is one
  launch of the benchmark executable with `--filter *NumberConversionBenchmarks*` and
  `BROILER_BENCHMARK_PROFILE=baseline`. It runs all six workloads, each in BenchmarkDotNet's own child
  process, under the baseline job of `BenchmarkConfiguration.cs` at the rule commit.
  `--number-conversion-answers` ran before and after every run.
- **When, and beside what.** 2026-10-13, 12:53 to 14:24, by `collect.py --only measure`. Nothing else
  of this programme ran on the machine: Broiler.VM's part C had ended at 12:52, no build server was
  running when the step started, and the step's own builds were run with node reuse and the shared
  compiler off, so none outlived them.

## Files

| File | What it is |
|---|---|
| `manifest.txt` | the commits, the identity checks above, the SHA-256 of every assembly of each arm, and the protocol; written and made read-only before the first run |
| `conditions.tsv` | every answers check: repetition, lane, before or after, whether the answers equal B's first check, and whether the effective configuration does |
| `answers.json` | B's first check: each workload's answer and the process's effective configuration |
| `runs.tsv` | every workload of every run: BenchmarkDotNet's median and mean time per operation, allocated bytes per operation and collection counts |
| `environments.txt` | the distinct BenchmarkDotNet host environment and hardware-intrinsics records across all runs |
| `summary.tsv` | Q per arm, N, D and E, in nanoseconds |
| `runs/` | each run's full BenchmarkDotNet JSON report and its console output, compressed |
| `collect.log.gz` | every command the step ran |
| `hashes.txt` | the SHA-256 of every file the step wrote; this README was written after the step, and the bundle's `manifest.json` covers it |

## The reading

**Clause 1 is not met: the lane is not a controlled lane, so part B is void and decides nothing.**
Its other clauses are read below for what they show:

| Clause | On this run |
|---|---|
| 2. Every run's condition passes, before and after, for every arm | **holds**: all 43 checks find the answers equal to B's first check |
| 3. Every configuration check identical | **holds**: one effective configuration in all 43 checks, and one BenchmarkDotNet host environment record across all 21 runs |
| 4. Every run completed every workload, and none has issues | **holds**: 21 runs of six workloads each, 126 rows in `runs.tsv`; no run reports a workload with issues |
| 5. D <= N for `ToString`, `ToNumber` and `Literals` | holds for `ToString` and `ToNumber`; **not for `Literals`** |

| Workload | Q(B) ms | Q(A) ms | Q(A-again) ms | N ms | D ms | E(B) ms | E(A) ms |
|---|---|---|---|---|---|---|---|
| `ToString` | 208.379 | 52.330 | 53.156 | 0.826 | -156.049 | 201.478 | 45.493 |
| `ToNumber` | 37.366 | 20.511 | 22.565 | 2.054 | -16.856 | 28.180 | 11.268 |
| `Literals` | 1.959 | 2.049 | 2.067 | 0.018 | **+0.091** | 0.212 | 0.253 |

**What the arithmetic says, deciding nothing.** On this lane, A's `Literals` workload is slower than
B's by more than the A/A difference, and its attribution E grows too, by about half as much. The workload
compiles a script of numeric literals, and the cut-over moved the literal values into the shared
library. A controlled lane would judge this under clause 5, and the rule reads a failure there as a
regression that SJL-3 answers before it closes. This run is void, so it cannot say whether that
regression exists, only that a controlled lane should look at `Literals` first. On `ToString` and
`ToNumber`, A is faster than B by far more than the A/A difference. That too is a smoke reading and
no claim.

**What it does not claim.** A speed of anything; a comparison with Broiler.VM, whose measurement is
its own; and anything about a lane other than this one.
