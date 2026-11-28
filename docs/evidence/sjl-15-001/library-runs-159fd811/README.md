# Parts F and G, and bundle `sjl-13-001`'s part E, at stage 8's commit, `159fd811`

**Taken:** 2026-11-28, after the merge of SJL-15.2's stage 8 (Broiler-Platform/Broiler.JS#1151) and of its part B
records (Broiler-Platform/Broiler.JS#1152), for [`../decision-rule.md`](../decision-rule.md)'s parts F and G and for
bundle `sjl-13-001`'s part E as the rule applies it ("bundle `sjl-13-001`'s parts B (the vectors at library level), C
(determinism), D (the Native AOT probe), E (the rules) and H (the contract's checklist) are applied to the model at the
library commit for the home's clauses"). They are taken under decision D-js-library-runs, "at stage 8's commit, the
library commit if no model commit lands before the base commit", and read as decisions D-js-library-readings,
D-js-part-f-tools, D-js-adapter-final, D-js-vector-listing and D-js-digest-model read them
(`docs/roadmap/SharedLibraries.md`, "Shared static semantics (SJL-15)", recorded at stage 8 before any run of parts F
and G). Their readings are in the bundle's [`README.md`](../README.md), the note of 2026-11-28 on parts F and G at
`159fd811`. Nothing here changes the rule, a tool or a product file. This folder is frozen once merged: a run taken
again goes to a new folder.

## The commit

`159fd811f74d0f43e7ed7327081267a96f8f1196` (tree `d1d0461bf52a648b3810506e354b2fdca59f0c4b`), the merge of
Broiler-Platform/Broiler.JS#1151 (head `c8518398920616492243cc9ab34af9df5b0f0669`, first parent
`b92ef028327b7420d4f489cb805ef5270623e760`), the tenth model commit. Checked on 2026-11-28 after `git fetch`, with
`origin/main` at `2e4a3c9ece1d7dda129cad95a25f40524355e250` (Broiler-Platform/Broiler.JS#1152, stage 8's part B records,
whose first parent is `159fd811`):
- the loop over the default branch's first-parent merges from the fifth model commit `4d8aa93a` that lists each merge
  changing a path under `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*` lists exactly `84d84cc3`, `30af0dad`,
  `2c48beb1`, `950bea8f` and `159fd811` ([`commands.txt`](commands.txt) gives the loop), and nothing from `159fd811` to
  `origin/main` changes such a path, so no model commit has landed after it (decision D-js-last-model-commit);
- the rule, its revision, `dispositions.md`, `correspondence-broiler-js.md`, `corrections.md` and `corrections/`,
  bundles `sjl-13-001` and `sjl-12-001`, the parse corpus's manifest and inputs, both workflows, `ParseCorpus.cs` and
  `UnicodeDataHome.cs` are as at `d3e64b4d` at both `159fd811` and `origin/main`, no rule file is added, and bundle
  `sjl-12-001`'s tool is the tree `5ede61145de01136368c49354acc1de1c469fcb6` at both;
- part F's tools at `159fd811` are the bundle's [`../tools/model_check.py`](../tools/model_check.py) and
  [`../tools/row_sites.py`](../tools/row_sites.py) as stage 8 last logged them, their digests in
  [`../hashes.txt`](../hashes.txt), and [`../tools/broiler-vm-inputs.sha256`](../tools/broiler-vm-inputs.sha256) names
  Broiler.VM `c1b33a71db0e91494e8e588a6ad48c2f11dd4c22`, whose JSD-0080 and SJL-15 milestone file the checklist read,
  each with its digest as listed ([`part-f/tools/model-check.txt`](part-f/tools/model-check.txt), its first lines).

## What was run

Every local step from a clean `git archive` of `159fd811` on disk, outside every working tree, built in Release with no
build server, and run under `env -i`, so with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*` variable
([`env.txt`](env.txt)), one step at a time through the machine's run script, which holds the build lock behind its idle
gate (the same `run.sh`, `gate.sh`, `machine.sh` and `timed.py` as bundle `sjl-14-001`'s
[`bridge/scripts/`](../../sjl-14-001/bridge/scripts/run.sh), byte for byte, the scripts part B's folders name, kept here
in [`scripts/`](scripts/run.sh)), each step's build tree deleted at its end. The steps of part G's part D local half and
of bundle `sjl-13-001`'s part E archived the commit less its top-level `docs/` and `logs/`, records which none of their
commands reads, to keep the machine's disk margin. test262 at `ccaac100` was read from Broiler.VM's archive, checked by
its SHA-256 against the manifest's `pin-test262-archive` line, and each input against the manifest's SHA-256 by the
probe. The commands are in [`commands.txt`](commands.txt); the scripts and the predeclared patches in
[`scripts/`](scripts/lib.sh) and [`patches/`](patches/control-f-ee0018.patch); the gate's records and the drivers'
record in [`logs/`](logs/all-runs.rc) (whose `rc=` field is not the step's exit status: see "Disclosures" below).
The steps that did not run through, none producing an output a reading rests on, are kept in
[`first-attempt/`](first-attempt/README.txt) with what stopped each, and were taken again whole.

| Folder | Part | What it holds |
|---|---|---|
| [`part-f/tools/`](part-f/tools/model-check.txt) | F | The checklist's listing (`model-check.txt`), the site listing (`row-sites.txt`), the Broiler.VM inputs' digests (`vm-inputs.sha256`), and both of bundle `sjl-13-001`'s part H's controls over scratch archives, each with its diffs and listing (`control-1*`, `control-2*`) |
| [`part-f/tools-disk-check/`](part-f/tools-disk-check/README.txt) | F, a check | Part F's tools step taken again after review with the disk checked before every extraction, over archives of the paths the tools read (`scripts/verify-controls.sh`), its listings compared with `part-f/tools/`'s; it decides nothing (see "Disclosures") |
| [`part-f/run/`](part-f/run/listing.txt) | F | `Broiler.ECMAScript.Conformance.Tests`' library-level run: its `trx` log (`run/run.trx.gz`), its build and test logs, and the per-vector listing taken from the `trx` (`listing.txt`, decision D-js-vector-listing) |
| [`part-f/control/`](part-f/control/listing.txt) | F | The rule's part F control, EE0018's report suppressed in a scratch archive (`applied.patch`): its run and listing, the revert (`revert-check.txt`, `revert/`) and the reverted run's listing against the run's (`revert-listing-against-run.txt`) |
| [`part-g/part-c/`](part-g/part-c/sha256.txt) | G (bundle `sjl-13-001`'s part C) | The runs of the probe's corpus digest mode, whole, gzipped, with their logs and SHA-256 (`sha256.txt` names the files before compression), SL2 and SL9 (`sl2-sl9.log`), and `K1/` and `K2/`, each with its patch, its runs, the rule its patch is tested against and its revert |
| [`part-g/part-d/local/`](part-g/part-d/local/sha256.txt) | G (bundle `sjl-13-001`'s part D, local half) | The linux-x64 Native AOT and JIT publications' logs, the runs' logs, the Native AOT report gzipped, the reports' SHA-256 and comparison, and `warnings.txt` |
| [`part-g/part-d/workflow/`](part-g/part-d/workflow/run.txt) | G (bundle `sjl-13-001`'s part D, hosted half) | Shared libraries' push run `46082299097` at `159fd811`, by GET only: the run, its first attempt, the attempt's jobs, every job of every attempt, the artifacts' listing (`*.json`), every job's log gzipped (`jobs/`), each artifact checked against the API's digest and size (`artifact-digests.tsv`), both `native-aot-probe` artifacts' files (each report gzipped) and every artifact file's SHA-256 (`artifacts/sha256.txt`); `run.txt`, `probe-runs.txt` and `warnings.txt` read from them |
| [`rules/`](rules/architecture-tests.log) | bundle `sjl-13-001`'s part E, as checks | The architecture tests, `eng/pack.ps1 -ProjectSet Shared` rendered in bash (`pack-shared.sh`, its diff from bundle `sjl-13-001`'s rendering in `pack-shared.sh.vs-sjl-13-001.diff`), the packages' SHA-256, the pristine consumer, the assurance check, and the workflow's own pack and assurance jobs' logs (`hosted/`) |

## What the runs say

- **Part F's checklist** (`part-f/tools/model-check.txt`): every input as listed; the listing of the milestone's facts
  passes, every item of every fact having its member; JSD-0080's rows pass, "the caller's lexical names" found at stage
  5c's members; no engine-namespace line; no engine row with a producer; no problem, exit 0. **Bundle `sjl-13-001`'s
  part H's controls** are both reported: control 1 lists "named-evaluation names	a definition's computed key	absent",
  the facts' listing fails and its line reads
  "outcome	control-absent	Broiler.ECMAScript.Semantics.SemanticModel.NamedEvaluationKey(	reported"; control 2 lists
  "engine	Broiler.VM.Witness.EngineStub sealed class", the engine listing fails and its line reads
  "outcome	control-engine	Broiler.VM.Witness.EngineStub	reported" (each as predeclared at stage 8; the separators are
  tabs, as the listings write them).
- **The site listing** (`part-f/tools/row-sites.txt`): every `semantics` row of the stage table has a site in the
  library's code but EA0006, listed "no-site	EA0006	run-time" with its reason quoted from the rule's part F, and EP0017
  and EP0018, each listed "no-site ... carried" with its `SemanticRows.cs` entry and its reason quoted from
  `SemanticRows.cs` ("EP0017 and EP0018 have no report of their own: no goal is ShadowRealm's, and EA0003, EA0004 or
  EE0162 refuses their source first"); the stage table's EA0008 cell is printed with "stale for the library: a site
  exists".
- **The library-level run** (`part-f/run/`): every test passes; the per-vector listing lands in the `trx` log, and every
  vector the adapter answers reads `match` at the parse.
- **The rule's part F control** (`part-f/control/`): the only failing test is
  `SemanticsAdapterTests.Every_answered_semantics_vector_agrees_at_the_parse`, its failure naming exactly
  `semantics/expressions/ee0018/0004`, `…/0005` and `…/0006`, and the listing shows exactly those as `difference`;
  reverted, `Broiler.ECMAScript/` equals a fresh archive's, every test passes, and the whole listing is the unpatched
  run's, byte for byte.
- **Part G's part C half** (`part-g/part-c/`): run 1, run 2 (`--reverse`) and run 3 (`--two-threads`) are the same
  bytes, and each reports no problem, run 3 none between its concurrent parses, so, as part C's MET line asks, "every
  input gives one hash in all four parses"; SL2 and SL9 pass. K1: run 2 differs from run 1, and SL2 refuses the patched
  library (`SL2_No_shared_library_holds_mutable_static_state` fails, `K1/sl2.log`). K2: run 1 against run 2 differ, so
  the comparison of the runs' hashes, taken in separate processes, reports the difference, as bundle `sjl-13-001`'s
  first revision restates K2; SL9 passes under it (`K2/sl9.log`), which the restated K2 does not ask to fail. Each
  reverted: `Broiler.ECMAScript/` equals a fresh archive's, and run 1 taken again is part C's run 1, byte for byte.
  These runs compare each input's digest across the runs and read no outcome against bundle `sjl-13-001`'s
  classification.
- **Part G's part D half, locally** (`part-g/part-d/local/`): both publications succeed with no line naming a warning,
  no coded warning and no IL code in either log, and no managed assembly in the Native AOT folder; the Native AOT
  image's, the JIT publication's and the JIT build's reports are the same bytes, and each run reports no failure.
- **Part G's part D half, hosted** (`part-g/part-d/workflow/`): the run's first attempt, its only attempt, concluded
  `success`, every job `success`, both `native-aot-probe` jobs (linux-x64 `140783796376`, win-x64 `140783796520`) with
  every step `success`; in each, neither publication's log in the artifact nor either publish step in the job's log has
  a line matching the workflow's `warning [A-Z]+[0-9]+`, a `warning` in any letter case, or an IL code; each leg's
  Native AOT, JIT-published and JIT-built reports are the same bytes, the same as the local runs', and each run reports
  no failure (`probe-runs.txt`). Every artifact matches the API's digest and size.
- **Bundle `sjl-13-001`'s part E, as checks** (`rules/`): every architecture test passes, SL1 to SL10 with their
  witnesses refused and SL6's public API equal to `PublicAPI.txt` in both directions; the shared packages pack with
  package validation and pass the rendering's checks; the pristine consumer passes; the assurance check reads
  "Broiler.ECMAScript: no violations."; the workflow's own pack and assurance jobs at `159fd811` succeeded.

## Disclosures

*(Added 2026-11-28, after review of the runs above, before this folder merged; no output above changes, and no reading
changes.)*
- **The disk margin at bundle `sjl-13-001`'s part H's controls.** Part F's tools (step `lib-f-tools`,
  [`part-f/tools/`](part-f/tools/model-check.txt)) ran under the first version of [`scripts/lib.sh`](scripts/lib.sh),
  whose `newtree` checked 8 GiB free before extracting its own archive but not the archive's size, and
  [`scripts/part-f-tools.sh`](scripts/part-f-tools.sh) extracts two more whole archives of the commit, `control-1/` and
  `control-2/`, each of 1370900480 bytes, with no check of the disk before either. Its
  [`disk.log`](part-f/tools/logs/disk.log) has 10909274112 bytes free before the first archive and 6696357888 bytes
  before the tree's delete, when control 2 had ended; so control 1 ran (07:07:11Z) with about 8.17e9 bytes free (about
  7.6 GiB) and control 2 (07:07:18Z) with about 6.70e9 (about 6.2 GiB), both below the 8 GiB this stage's machine rule
  asks before each run, though above its 6 GiB floor. The checklist and site listing ran before either control was
  extracted, with about 9.54e9 bytes free. The step is not replaced, and its outputs stay the ones part F is read on:
  the disk now free could not hold the script's three whole trees under the revised check. Instead it was checked, on
  2026-11-28 after review, by [`part-f/tools-disk-check/`](part-f/tools-disk-check/README.txt), the same script with
  the disk checked before every extraction and run and each archive holding only the paths the tools read: every
  listing, both controls' among them, is the same bytes as `part-f/tools/`'s, and the controls' diffs differ only in the
  time on their `+++` line. It decides nothing. The owner is told, in the records change's description. The steps
  after `lib-f-tools` ran under the revised checks (`disk.log` in each folder).
- **The drivers' record.** In [`logs/all-runs.rc`](logs/all-runs.rc) and
  [`first-attempt/all-runs.rc`](first-attempt/all-runs.rc), each step's `rc=` field is the exit status of the `$(date
  ...)` substitution in the same `echo`, not the step's, so it reads 0 for every step that ended. Each step's own exit
  status is in `logs/<label>.exit.txt` and in [`logs/runs.tsv`](logs/runs.tsv). The steps that did not run through
  exited: `lib-f-run` 1, `lib-f-control` 5, `lib-g-c` 82 (`first-attempt/part-c/logs/commit.txt` reads
  `first-failure=82`) and `lib-e-rules-3` 1; `lib-g-d-2` was stopped by its process ids and has no exit status; and
  the first driver's `lib-g-k1` has a start line and no end, the driver having been stopped before `run.sh` took the
  lock (`logs/gate.log` has no line for it). Every step a reading rests on exited 0. The raw logs are kept as they
  are.
- **The session's note of Precondition 5.** This stage's working note quoting the owner's direction and stage 8's
  readings (the note the stage's brief asks for "before any run") was written at 2026-11-28T07:25:14Z, after part F's
  steps, part G's part C runs and K1 had run, while K2's builds were under way. It quotes only records of stage 8's
  that are unchanged since `159fd811`, where they were recorded before any run of parts F and G.
