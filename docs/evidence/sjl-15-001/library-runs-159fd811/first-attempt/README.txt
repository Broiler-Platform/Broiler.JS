The steps taken at 159fd811f74d0f43e7ed7327081267a96f8f1196 that did not run through, 2026-11-28, kept as they were left.
None of them produced an output any reading rests on; each step was taken again, whole, in the folders above. Every
attempt is kept, as the rule's part G reading of the hosted half asks of its own run.

- run/, control/ and part-c/ (steps lib-f-run, lib-f-control and lib-g-c of scripts/all-runs.v1.sh, with all-runs.rc,
  that driver's record): every `dotnet build` and `dotnet test` stopped at once with "MSBUILD : error MSB1025: An
  internal failure occurred while running MSBuild." (System.IO.Directory.CreateTempSubdirectory): scripts/lib.sh set
  TMPDIR to <step>/tmp and then deleted <step> before extracting the archive, so the folder TMPDIR named did not exist.
  No project was built, no test ran and no corpus was parsed (part-c's runs failed for want of the probe's assembly).
  The driver was stopped by its process id before its next step began. lib.sh was revised (its newtree makes <step>/tmp
  again) and the steps taken again as lib-f-run-2, lib-f-control-2 and lib-g-c-2. Part F's tools (step lib-f-tools,
  part-f/tools/), which need no build and no temporary folder, had run through, and were not taken again.
- part-d-local-stopped/ (step lib-g-d-2 of scripts/all-runs.sh): its check of 8 GiB free before the first build waited
  with its own archive of the whole commit on disk, after another stage's worktree had taken the margin; it was stopped
  by its process ids before any build or run, and its tree deleted. scripts/lib.sh's newtree was revised to check 8 GiB
  plus the archive's size before extracting, and part D's and part E's steps to archive the commit less its top-level
  docs/ and logs/, which none of their commands reads; the step was taken again as lib-g-d-3 (scripts/all-runs-3.sh).
- rules-pack-script-failed/ (step lib-e-rules-3 of scripts/all-runs-3.sh): the architecture tests, the pristine consumer
  and the assurance check ran through; the bash rendering of eng/pack.ps1 stopped before packing, because its packing
  loop evaluated each project again for IsPackable alone, which msbuild prints as a bare value and not as JSON. The
  rendering was revised (rules/pack-shared.sh's header) and the whole step taken again as lib-e-rules-4
  (scripts/all-runs-4.sh).

Added 2026-11-28, after review: all-runs.rc's "rc=" field, here and in ../logs/all-runs.rc, is the exit status of the
$(date ...) substitution in the same echo, not the step's, so it reads 0 for every step that ended, the steps above
among them. Each step's own exit status is ../logs/<label>.exit.txt (and ../logs/runs.tsv): lib-f-run 1, lib-f-control
5, lib-g-c 82 (part-c/logs/commit.txt reads first-failure=82) and lib-e-rules-3 1; lib-g-d-2, stopped by its process
ids, has none. The first driver's lib-g-k1 has a start line and no end: the driver was stopped before run.sh took the
lock (../logs/gate.log has no line for it). The records are kept as they are (../README.md, "Disclosures").
