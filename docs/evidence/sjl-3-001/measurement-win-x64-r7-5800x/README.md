# Bundle SJL-3-001 (Broiler.JS), measurement sub-bundle on lane `win-x64-r7-5800x`: part B, judged

**One evidence class: a measurement**, taken on the controlled lane
[`win-x64-r7-5800x`](../../lanes/win-x64-r7-5800x/README.md): one machine, an eight-core AMD Ryzen 7 5800X desktop,
and one RID, `win-x64`, recorded as [`Measurement.md`](../../../roadmap/Measurement.md)'s section "Controlled lanes and
effective configuration" asks, before the first run, and confirmed by the owner, RID included, on 2026-11-17 before
any run. In that document's classes this is an acceptance comparison. **It is
[part B](../decision-rule.md#part-b---broilerjss-measurement-template-t5)'s first judged run**, at the rule's own
arms, not a re-run: the run in [`measurement/`](../measurement/README.md), taken on the programme's container, is
void and was not judged, and this sub-bundle neither replaces nor reads it.

**The reading: MET.** Every clause of part B holds (below).

## What was run

- **Arms.** B, the base: the benchmark project of the rule commit `1410079e`, whose engine projects are
  byte-identical to the base commit `44a214dc`. A, the after commit `ef676932`, with the workloads, `Program.cs` and
  `BenchmarkConfiguration.cs` as the rule commit has them. A-again: A's build, measured again under another lane name,
  the A/A lane. `manifest.txt` checks each identity, and names every assembly of each arm by SHA-256. Each arm was a
  clean worktree of its commit, checked out with the repository's own line endings.
- **Workloads.** The six of `NumberConversionBenchmarks`: `ToString`, `ToNumber` and `Literals`, each with its control.
- **Protocol.** Part B's, as `collect.py`'s measure step runs it. Seven repetitions. In each, B, A and A-again each
  run once, in an order that rotates by repetition: B, A, A-again; then A, A-again, B; then A-again, B, A; and again.
  One run is one launch of the benchmark executable with `--filter *NumberConversionBenchmarks*`, `--buildTimeout
  1800` and `BROILER_BENCHMARK_PROFILE=baseline`, built in Release and run under the JIT. It runs all six workloads,
  each in BenchmarkDotNet's own child process, under the baseline job of `BenchmarkConfiguration.cs` at the rule
  commit. `--number-conversion-answers` ran before and after every run. `manifest.txt` was written and made read-only
  before the first run.
- **How.** `collect.py --only measure` does not fit this lane: it writes into `measurement/`, deleting what is there
  first, and writes "lane: smoke" into the manifest. [`collect-lane.py`](../collect-lane.py), committed before the run,
  runs the same step with `collect.py`'s collector and helpers, and departs from it only where it says so: the folder
  is this one, created new, and nothing is deleted; the manifest's lane line names this lane, its record, and the
  SHA-256 of each of the record's files, in place of "lane: smoke"; builds run with node reuse, the MSBuild server and
  the shared compiler off, as the void run's did, and every child's output is decoded as UTF-8; and the run window is
  recorded. The arms, their checks, the rotation, the answers, the filter, the profile, the build timeout, the parsing
  and the arithmetic are `collect.py`'s, line for line.
- **When, and beside what.** 2026-11-17, from 20:30:28Z to 21:30:18Z, in one collection, which nothing interrupted.
  The owner closed the desktop applications that were running but three, left open and idle by the owner's choice (an
  editor's background process, the Edge browser and the TortoiseGit cache), and was asked to leave the machine
  untouched until the end; the build servers were shut down (`dotnet build-server shutdown`) before the start, and no other Claude
  session was running. At the window's start and end (`window-start.txt`, `window-end.txt`), the CPU load read 3 % and
  4 %, no .NET build process was alive, and nothing used more than 1 % of one logical processor over a ten-second
  sample but the Claude desktop app, which started the collection and waited idle until it ended, and the sampler's
  own PowerShell. Before every run (`window-runs.tsv`), no .NET build process was alive. The lane's capture, taken
  again at both ends, matches its record but for the boot time's last second, which Windows computes from the uptime:
  the same microcode, firmware, OS build, power plan, SDK and runtime. What the record does not show is the window
  between those samples.
- **Not controlled, and recorded rather than changed** (the lane's README): Precision Boost, SMT, the hypervisor and
  virtualization-based security, Defender's real-time scanning, and Windows' own services. No setting was changed for
  the run; the High performance plan was already active.

## Files

| File | What it is |
|---|---|
| `manifest.txt` | the commits, the identity checks above, the SHA-256 of every assembly of each arm, the protocol, the lane and its record's hashes, and the build settings; written and made read-only before the first run |
| `window-start.txt`, `window-end.txt` | the run window at its start and end: the lane's capture again, the load, the processes that used the CPU over a ten-second sample, and the .NET build processes alive |
| `window-runs.tsv` | every run's start time, and the .NET build processes alive just before it |
| `conditions.tsv` | every answers check: repetition, lane, before or after, whether the answers equal B's first check, and whether the effective configuration does |
| `answers.json` | B's first check: each workload's answer and the process's effective configuration |
| `runs.tsv` | every workload of every run: BenchmarkDotNet's median and mean time per operation, allocated bytes per operation and collection counts |
| `environments.txt` | the distinct BenchmarkDotNet host environment and hardware-intrinsics records across all runs |
| `summary.tsv` | Q per arm, N, D and E, in nanoseconds |
| `runs/` | each run's full BenchmarkDotNet JSON report and its console output, compressed |
| `collect.log.gz` | every command the step ran, with each run's exit code |
| `hashes.txt` | the SHA-256 of every file the step wrote; this README was written after the step, and the bundle's `manifest.json` covers it |

## The reading: MET

| Clause | On this run |
|---|---|
| 1. The lane is a controlled lane, recorded as `Measurement.md` requires | **holds**: the lane's record names the CPU model and microcode, the core topology, the memory, the OS and kernel, the power plan and thermal policy, and the .NET SDK and runtime; it was captured before the run, its files are pinned by hash in the read-only manifest, and the owner confirmed the lane and its RID before any run. Each run's effective configuration is in `conditions.tsv` (clause 3) |
| 2. Every run's condition passes, before and after, for every arm | **holds**: all 43 checks (B's first, and before and after each of the 21 runs) find the answers equal to B's first check |
| 3. Every configuration check identical across arms, lanes and repetitions | **holds**: one effective configuration in all 43 checks (`win-x64`, X64, .NET 10.0.15, server GC, sixteen processors, vector acceleration on, the tiering, PGO and ReadyToRun settings at their defaults), and one BenchmarkDotNet host environment record, with one hardware-intrinsics record, across all 21 runs |
| 4. Every run completed every workload, and BenchmarkDotNet reported no workload with issues | **holds**: 21 runs, each exiting 0, of six workloads each, 126 rows in `runs.tsv`; no run prints "Benchmarks with issues", and every run's "Config Issues" section is empty |
| 5. D <= N for `ToString`, `ToNumber` and `Literals` | **holds** for all three: on `ToString` and `ToNumber`, D is negative and far below N; on `Literals`, D is positive and below N, so `Literals` is below resolution and meets the clause |

| Workload | Q(B) ms | Q(A) ms | Q(A-again) ms | N ms | D ms | D <= N | E(B) ms | E(A) ms |
|---|---|---|---|---|---|---|---|---|
| `ToString` | 91.2153 | 26.6403 | 27.0079 | 0.3677 | -64.5750 | yes | 87.3264 | 22.7554 |
| `ToNumber` | 21.2076 | 12.0371 | 12.1067 | 0.0696 | -9.1705 | yes | 15.9149 | 6.7358 |
| `Literals` | 0.8126 | 0.8159 | 0.8212 | 0.0054 | +0.0033 | yes, and \|D\| <= N | 0.0837 | 0.0852 |

On `Literals`, in microseconds: N is 5.364 and D is +3.312.

**Readings the clauses rest on.**

- **Clause 3 and BenchmarkDotNet's processor field.** The host environment record names the processor "Unknown
  processor" and leaves its core counts empty: BenchmarkDotNet 0.14 reads them through `wmic`, which this Windows build
  does not ship. The record is identical in all 21 runs, so the clause holds; the processor is named by the lane's
  record, and each process's own processor count is in `conditions.tsv`.
- **Clause 4 and BenchmarkDotNet's warnings.** Every run prints one configuration warning, "The exporter CsvExporter is
  already present in configuration", and one run, r2's B, prints a `MinIterationTime` warning on its `ToString`
  workload, whose shortest iteration took 90.098 ms where BenchmarkDotNet recommends at least 100 ms. Both are
  analyser warnings, not workloads with issues, and the void run shows the same two (`measurement/`), where they were
  read the same way.
- **Clause 1 and the lane.** The lane is the owner's workstation, declared for this run window and quieted for it,
  not a host dedicated outside it; its README says what is not controlled. The clause rests on that record and on the
  owner's confirmation, and no one else has reviewed it.

**What the arithmetic says, deciding nothing.** On this lane the after arm is faster than the base on `ToString` and
`ToNumber` by far more than the A/A difference, and on `Literals` the two arms differ by less than the A/A difference.
The void run had put `Literals` first for a controlled lane to look at: on this lane it is below resolution. The
attribution E moves with Q on `ToString` and `ToNumber`, and on `Literals` by less than N.

**Guardrails, deciding nothing.** BenchmarkDotNet's allocated bytes per operation, the median over each arm's seven
runs: `ToString` 106,024,403 at B and 20,198,238 at A (20,198,247 at A-again); `ToNumber` 32,831,004 at B and
15,359,467 at A (15,359,456 at A-again); `Literals` 1,342,618 at B and 1,324,699 at A (1,324,697 at A-again). The
memory diagnoser reports 0 bytes for six `ToNumber` runs (one of B's, two of A's, three of A-again's) and for some
controls' runs, in all three arms alike; `runs.tsv` keeps every run's figure and collection counts.

**What it does not claim.** A speed of anything: part B makes no performance claim, and a MET says that the after arm
is held to the base within this lane's own A/A noise on each workload, nothing more. Nothing about linux-x64, linux-arm64
or any machine but this one; and nothing compared with Broiler.VM, whose measurement is its own.
