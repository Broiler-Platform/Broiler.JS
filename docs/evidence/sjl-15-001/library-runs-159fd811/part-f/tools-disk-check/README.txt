A check, taken 2026-11-28 after review, of part F's tools step (step lib-f-tools, ../tools/) with the disk checked before
every extraction. It decides nothing, and nothing in ../tools/ is replaced: part F's readings rest on ../tools/ as taken.

Why: ../tools/ ran under the first version of scripts/lib.sh, and scripts/part-f-tools.sh extracts its two control
archives with no check of the disk, so bundle sjl-13-001's part H's controls ran with less than the 8 GiB free that the
stage's machine rule asks before each run (../README.md, "Disclosures"; ../tools/logs/disk.log).

What: scripts/verify-controls.sh, a copy of scripts/part-f-tools.sh changed in three ways only (its added comment, after the header, says so):
its build tree is /home/user/js-s158-lib-build/verify-controls/; every archive of 159fd811f74d0f43e7ed7327081267a96f8f1196
holds only the paths the two tools read (Broiler.ECMAScript, docs/evidence/sjl-15-001/tools,
docs/evidence/sjl-15-001/decision-rule.md and conformance/vectors/semantics), so that three trees fit in the margin; and
before each control's extraction it waits for 8 GiB plus the archive's size, and checks 8 GiB again before each control's
run (logs/disk.log). Through the machine's run script and its gate, as every step here (run/). The script is kept as
it ran; its comment's "NOT retained" was written before its outputs were kept here.

Result: model-check.txt, row-sites.txt, vm-inputs.sha256, control-1.txt, control-2.txt and control-2-EngineStub.cs.txt
are the same bytes as ../tools/'s; the three control diffs differ from ../tools/'s only in the time on their "+++"
header line, the time the scratch file was written. Each tool exited as in ../tools/ (logs/runs.tsv: model-check and
row-sites 0, control-1 and control-2 1).
