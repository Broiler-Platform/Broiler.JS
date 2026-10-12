# Bundle SJL-0-001 (Broiler.JS), measurement sub-bundle: the pilot's base arm

**One evidence class: a measurement.** Its lane, a four-processor `linux-x64` container, is not a
controlled lane in [`Measurement.md`](../../../roadmap/Measurement.md)'s sense, so in that document's
classes this sub-bundle is **smoke**: it accepts and rejects nothing about a performance change. It
is the base reading [part B of SJL-3's rule](../../sjl-3-001/decision-rule.md#part-b---broilerjss-measurement-template-t5)
describes, taken so the workloads, the harness and the base's noise floor exist before any code
moves. **It decides nothing about SJL-3**, which rebuilds arm B and runs it rotated with its after arm
on a controlled lane.

## What was run

- **Arms.** B, the base: the benchmark project of the rule commit `1410079e`, whose engine projects
  are byte-identical to the base commit `44a214dc` (`manifest.txt` lists every file under
  `Broiler.JS/` the two commits differ in, all in the benchmark project). B-again: the same build,
  measured again under another lane name, the A/A lane.
- **Workloads.** The six of `NumberConversionBenchmarks` as committed with the rule: `ToString`,
  `ToNumber` and `Literals`, each with its control.
- **Protocol.** Seven repetitions per lane, the lanes rotated by repetition (B first in even
  repetitions, B-again first in odd ones). One run is one launch of the benchmark executable with
  `--filter *NumberConversionBenchmarks*` and `BROILER_BENCHMARK_PROFILE=baseline`, which runs all six
  workloads, each in BenchmarkDotNet's own child process, under the baseline job of
  `BenchmarkConfiguration.cs`. `--number-conversion-answers` ran before and after every run.
- **The machine** was otherwise idle: the test262 run had finished before the first run started.

## Files

| File | What it is |
|---|---|
| `manifest.txt` | the base and rule commits, the files they differ in under `Broiler.JS/`, the SHA-256 of every assembly of the arm, and the protocol; written and made read-only before the first run |
| `conditions.tsv` | every answers check: repetition, lane, before or after, whether the answers equal B's first check, and whether the effective configuration does |
| `answers.json` | B's first check: each workload's answer and the process's effective configuration |
| `runs.tsv` | every workload of every run: BenchmarkDotNet's median and mean time per operation, allocated bytes per operation and collection counts |
| `environments.txt` | the distinct BenchmarkDotNet host environment and hardware-intrinsics records across all runs |
| `summary.tsv` | Q per lane, the A/A difference, E(B) and the reading |
| `runs/` | each run's full BenchmarkDotNet JSON report and its console output, compressed |
| `collect.log.gz` | every command the step ran |
| `hashes.txt` | the SHA-256 of every file the step wrote; this README was written after the step, and the bundle's `manifest.json` covers it |

## The reading

**The conditions held.** Every one of the 28 checks, before and after each of the 14 runs, found
the answers equal to B's first check and the effective configuration unchanged. BenchmarkDotNet
reported one host environment record across all runs, executed all six workloads in every run, and
reported no workload with issues.

**The one decision, stated in part B of the rule before any run:** each workload's attribution
E(B) = Q(B) - Q(control, B) is recorded as the base's figure when |E(B)| exceeds the A/A difference
|Q(B) - Q(B-again)|, and as below resolution otherwise. **`ToString` and `ToNumber` read `recorded`;
`Literals` reads `below resolution`**: on this lane, the literal values' share of compiling the
workload's source does not stand out from the A/A difference. The figures are in `summary.tsv` and
nowhere else.

**What it does not claim.** A speed of anything; a comparison with Broiler.VM, whose measurement is
its own; and anything about a lane other than this one.
