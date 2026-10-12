# Evidence bundle SJL-0-001 (Broiler.JS)

**Milestone:** SJL-0 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-0-records-decisions-and-baselines.md`:
the Broiler.JS half of work package SJL-0.8's baselines, which clause 9 asks to be retained at a named
commit. **Collected:** 2026-10-12, from a separate clean checkout of the base commit `44a214dc`, by
[`collect.py`](collect.py) in this directory. **What it decides:** nothing, except the one
predeclared reading of the measurement sub-bundle in [`measurement/`](measurement/). A base is not a
verdict: every later comparison of the programme reads it under a rule committed before the code it
judges. **Nothing here is accepted**: one person holds every role the programme names, and nothing
here has been read by anyone but its author.

This is the first bundle in this repository's `docs/evidence/`. Broiler.VM keeps its own bundle of
the same name, `Broiler.VM/docs/evidence/sjl-0-001/`, for its own half; neither bundle reads the
other's runs.

## 1. Identity

| Field | Value |
|---|---|
| Evidence bundle | SJL-0-001 (Broiler.JS's) |
| Milestone | SJL-0, Broiler.JS exit-gate clause 9 |
| Base commit | `44a214dc5b04a5c8db556dc9b57894beaeb20d0c`, the head of `main` when the programme's records were filed, checked out as a separate clean working tree for every build, pack and listing of sections 3 to 5. Section 2 says which build the test262 run used |
| Rule commit | `1410079e4808ca7fc10677ef996a95b77e571a11`, which committed the decision rules of [SJL-1](../sjl-1-001/decision-rule.md) and [SJL-3](../sjl-3-001/decision-rule.md), the reference-arm rule, the per-path comparison script and the measurement's workloads, before this bundle and before any code they judge. Its engine projects are byte-identical to the base's (`measurement/manifest.txt`) |
| Rules it is a base for | [`../sjl-3-001/decision-rule.md`](../sjl-3-001/decision-rule.md), part A (test262 per path) and part B (the measurement) |
| Suite | test262 at `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`, extracted from the archive Broiler.VM pins for the same revision; its SHA-256 matches that pin (`identity.txt`) |
| RID | `linux-x64`, one machine, four logical processors; `identity.txt` names the SDK, the runtime, Node and Terser |
| Owner | the Broiler.JS maintainer; one person holds every role (the programme's EX-30, recorded in Broiler.VM) |
| Reviewer | none |
| Evidence class | baselines: conformance, package identity and API identity, with no decision; and one measurement, in a sub-bundle of its own, whose lane is smoke in [`Measurement.md`](../../roadmap/Measurement.md)'s classes |

## 2. What it retains, against SJL-0.8's list

| SJL-0.8 item (Broiler.JS) | Retained | Where |
|---|---|---|
| 1. Whole-suite test262 at the pinned revision, in the canonical CI configuration, per path | **Yes**, a whole run of every shard, merged complete | section 3 |
| 2. A public API listing of every packable assembly | **Yes**, an interim generated listing, kept in the bundle | section 5 |
| 3. Package contents and nuspecs of every packable project | **Yes**, every packable project packed from the base | section 4 |
| 4. Measurements of the pilot's operations under SJL-0.9's rule | **Yes**, as a sub-bundle, on a smoke lane | section 6 |

## 3. Test262, per path

`test262/` holds the run. Its configuration is the test262 workflow's full phase at the base
(`.github/workflows/test262.yml` and `.github/actions/run-test262-shard/action.yml`): a `--self-check`
first; then `scripts/compliance/run_test262.py --all-script-host-verifiable` over eight shards, the
`terser` variant at Terser 5.50.0, the workflow's default per-test timeout, minifier timeout and
memory limit, negative-metadata tests not included; the eight reports merged by
`scripts/compliance/merge_test262_shards.py`. **One setting differs from CI: four workers per shard
instead of one**, recorded in every report. It changes no verdict but a timeout, and the rule handles
timeouts by a re-run alone.

| File | What it is |
|---|---|
| `test262-results.json.gz` | the merged report: every (path, variant) row with its verdict, and the merge's run configuration |
| `paths.tsv.gz` | the same rows as a table: path, variant, host mode, verdict |
| `merge.log` | the merge's one-line reading. Complete: eight shards reported, none incomplete, no configuration failure, one suite revision |
| `test262-full-shard-N-status.json`, `test262-full-shard-N.log.gz` | each shard's exit code and its runner log. Every shard exited 1, which the runner returns whenever a row fails; a report is conclusive with failures in it |
| `self-check.log` | the async-protocol self-check, which passed before any shard ran |
| `rerun-timeouts.json`, `rerun-timeouts.log` | the one path whose two variants timed out, `test/staging/sm/regress/regress-1507322-deep-weakmap.js`, run again alone with one worker at the base: **both variants time out again**, so the timeout is the base's and not the machine's load. Part A of the rule reads this re-run through the comparison script's `--base-rerun` |
| `compare-checks.log` | `scripts/compliance/compare_test262_paths.py`, committed with the rule, on this report: against itself it finds no changed row, and against a copy with one passing row turned to failed it reports exactly that row as a regression and exits 1 |
| `drive.sh`, `drive-resume.sh`, `drive.log`, `interrupted-shard-6.log`, `shell-binaries.sha256` | how the run was taken; see below |

**How it was taken.** The run was started by `drive.sh` before the collector existed, with the
commands its `test262` step now runs, and the collector imported it (`--test262-from`). The shell it
ran is `BroilerJS.dll` built in Release from this repository's working tree at the base commit, before
any change of this milestone touched an engine project: every commit since the base changes nothing
under `Broiler.JS/` but the benchmark project, which the shell does not build. `shell-binaries.sha256`
names every assembly the shell loaded. **A container restart interrupted shard 6** on 2026-10-12 at
09:18 UTC, after shards 0 to 5 had completed and been written. The shell's binaries were checked
against `shell-binaries.sha256` after the restart and were unchanged, and `drive-resume.sh` ran
shards 6 and 7 again from the start with the same commands. The interrupted shard's partial log is
`interrupted-shard-6.log`, and no row of it is in the merged report. A shard's selection is
deterministic, so a shard run again selects the same paths.

**The runner's reading** is in `merge.log` and in the merged report's summary, and nowhere outside
this bundle. **No document compares it with Broiler.VM's runner**: the two runners' totals are not
comparable, and the programme forbids the comparison.

## 4. Packages

`packages/` holds every project of `Broiler.JS.slnx` that MSBuild evaluates as packable at the base,
twenty, each packed in Release from the base checkout with `dotnet pack`:

- `packables.tsv`: each package's identity, its project, the SHA-256 of its `.nupkg` and its entry
  count;
- `nuspecs/`: each package's nuspec, as packed;
- `contents/`: each package's entries, with their sizes and SHA-256;
- `checks.txt`: the checks `eng/pack.ps1` makes, re-implemented in the collector because `pack.ps1`
  needs PowerShell, which this machine does not have. Every check passes: one version across the
  packages, unique ids, one archive per project, the right identity in each nuspec, a description,
  `README.md` and `icon.png` in each package, internal dependencies at the packed version, an
  assembly, its API documentation and its symbols package where the project builds one;
- `pack.log.gz`: every command and its output.

The `.nupkg` files themselves are not retained; their contents are, entry by entry, with hashes.
Two of the twenty, `Broiler.JavaScript.All` and `Broiler.JavaScript.Minimal`, are composition
packages with no build output of their own.

## 5. The public API listing

`api/` holds one listing per packable assembly, eighteen: the twenty packages less the two
composition packages. **It is interim**, as SJL-0.8 allows: this repository has no API baseline yet,
and SJL-2 adopts the tool its baselines use from then on. The listing was generated by a throwaway
console project, built outside the base checkout, that references every packable project of the
base and prints each assembly's public surface with PublicApiGenerator 11.4.6 (assembly attributes
left out). The project and its program are retained as `api-lister.csproj` and `Program.cs`, and its
build log as `build.log.gz`. Each listing is of the `net10.0` build, the only target framework any of
the eighteen has.

## 6. The measurement sub-bundle

[`measurement/`](measurement/README.md) holds one evidence class, a measurement, with its own README,
`manifest.txt` (made read-only before the first run), `runs.tsv` (every workload of every run),
`conditions.tsv` (the answers and the effective configuration, checked before and after every run),
`summary.tsv` and `hashes.txt`. It is **the base reading of part B** of
[SJL-3's rule](../sjl-3-001/decision-rule.md): the six workloads of `NumberConversionBenchmarks`,
committed with the rule in `1410079e` after the base, which is why it ran from a checkout of that
commit, whose engine projects are the base's. Arm B and an A/A lane B-again, the same build, rotated
by repetition, seven runs each, on an otherwise idle machine after the test262 run had finished.

**Its lane is smoke** in [`Measurement.md`](../../roadmap/Measurement.md)'s classes: a container is not
a controlled lane. That is enough for what the rule asks of the base reading, and no more.

**Its one decision, stated in the rule before any run:** each workload's attribution
E(B) = Q(B) - Q(control, B) is recorded as the base's figure when |E(B)| exceeds the A/A difference
|Q(B) - Q(B-again)|, and as below resolution otherwise. **`ToString` and `ToNumber` read `recorded`,
and `Literals` reads `below resolution`**; the figures are in `measurement/summary.tsv` and nowhere
else. Every answer held before and after every run, the effective configuration never changed, and
every run executed all six workloads. **It decides nothing about SJL-3**: that milestone rebuilds B
and runs it rotated with its after arm, on a controlled lane, as part B's clause 1 requires.

## 7. What this bundle excludes, and does not claim

**Not retained:**

- **A run in the canonical CI environment.** The test262 run is the workflow's configuration on this
  machine, with four workers per shard; no GitHub-hosted run exists at the base commit.
- **The `.nupkg` and `.snupkg` files**, whose entries are retained with hashes (section 4).
- **A pristine consumer, package validation against a previous version, and a Native AOT probe.**
  SJL-0.8 does not ask for them in Broiler.JS's half; SJL-2 and SJL-3 add them.
- **A measurement on a controlled lane.** Section 6's lane is smoke in `Measurement.md`'s classes.
- **Any run on another RID.** One machine, `linux-x64`.
- **Broiler.VM's half of SJL-0.8.** Broiler.VM's own bundle `sjl-0-001` holds it, in its own
  repository.

**Not claimed:**

- **No verdict on any milestone.** A base is read by a later rule; SJL-0's gate is read off its
  clauses by the programme's ledger and by
  [`SharedLibraries.status.md`](../../roadmap/SharedLibraries.status.md).
- **No figure outside this directory.** The test262 reading is in `merge.log` and the merged report,
  the measurement's figures in `measurement/summary.tsv`, and no plan or ledger quotes either.
- **No comparison of the two engines**, in conformance or in speed.
- **No review.** Nothing here has been read by anyone but its author.

## 8. How it was collected

```text
python3 docs/evidence/sjl-0-001/collect.py --tree <a checkout of 44a214dc> \
    --bench-tree <a checkout of 1410079e> --suite-archive <the pinned test262 archive> \
    --terser-module <terser's node module> [--test262-from <a run taken by drive.sh>] \
    --out docs/evidence/sjl-0-001 [--only STEP ...]
```

The base and the rule commit were separate working trees of this repository, clean throughout. The
collector ran from this repository's working tree in five invocations, and was corrected between
them; the retained `collect.py` is its last revision, and no correction changed what an earlier step
had already written and kept:

1. `packages` and `api`, at low priority while the test262 run took the machine. `packages`
   completed; `api` did not compile its throwaway project, which lacked implicit usings;
2. `api` again alone, after its project gained them;
3. `identity`, `test262` (importing the run) and `measure`, after the test262 run had finished and on
   an otherwise idle machine. Before this invocation the measure step's path to the benchmark
   executable was made absolute, and the test262 import learned to keep the resume script, the
   hashes and the timeout re-run;
4. `test262` again alone, after the collector learned to write `<run>`, `<repo>` and `<scratch>` for
   paths the first import had left absolute; its outputs replace the first import's;
5. `manifest`, after this README and the measurement sub-bundle's README were written.

Paths are written `<tree>` for the base's checkout, `<bench>` for the rule commit's, `<repo>` for the
collector's own checkout, `<suite>` for the extracted suite, `<run>` for the imported run's directory
and `<scratch>` for working directories. `manifest.json` names every retained file by the SHA-256 of
its bytes.
