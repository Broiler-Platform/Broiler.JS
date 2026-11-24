# Part J's corrected session at the after commit, taken on 2026-11-24 (corrected after the findings were seen; retained)

**Taken:** 2026-11-24, 06:03Z to 06:27Z, by the records stage of the owner's answers 12 to 15 of 2026-11-24 on branch
`sjl-14-after`, after every after run of [`../local/`](../local/README.md), the first session's findings and part J's
first reading ("Part J: read NOT MET on the retained runs", the bundle README's "W2's after commit, recorded on
2026-11-24") had been seen. **Why:** the owner's answer 14 of 2026-11-24 chose "Fix tool + revise V2 (Recommended)":
"Commit a corrected session that gives module seeds a module record, run it at 83a9bd82 (no rule change; recorded as
corrected after the findings were seen), and write a dated revision for V000002's compile-past-bound by name." Part
J's configuration, "a seeded mutation session, committed to the bundle, compiling each mutant through
CoreScript.Compile at the after commit in Release", is unchanged; the session is the bundle's tool as corrected by
`53afd382f2559d8c27cc42cbaf496af5d4ed161d` ("SJL-14: part J's session compiles module seeds with a module record
(answer 14)"; [`../../tools/fuzz/README.md`](../../tools/fuzz/README.md)'s note of 2026-11-24). **What it weakens:**
the tool was changed knowing what its first session found, so this session is weaker evidence than one committed
before any run; the first session's records stay as they are in `../local/fuzz/` and `../local/fcontrol/`, and its
first reading stays on record.

**How it was taken**, exactly as the after driver's `fuzz` and `fcontrol` steps took the first session
(`../local/scripts/astep.sh`), by [`scripts/jstep.sh`](scripts/jstep.sh) under [`scripts/jchain.sh`](scripts/jchain.sh),
each step through the machine-wide lock and idle gate ([`../local/scripts/run.sh`](../local/scripts/run.sh), the same
script; run.sh's records in [`logs/`](logs/runs.tsv)), with the default branch's head read as the after commit before
each ([`chain.log`](chain.log)): a clean `git archive` of `83a9bd82a5d258d3001468095ebf43f9efc3e9a7` on disk outside
every working tree, with the corrected tool's three files written over their paths from their blobs at the tool commit
(each step's `overlay.txt`: `Program.cs` blob `87417ea2cc9fc569c57363f2498bb52247923a85`, where the after commit has
`b3f99874a450aeac722df66f7ca8acc80280b90b`; `README.md` blob `936ee5aadb66847c671027196ab2d07c5df18528`, where it has
`7c24548fd81344a9fe80da337dbe5f85cd337d86`; `fuzz.csproj` blob `dfd151058a84f6d9c719fb18308a55321ab3c50c`, unchanged;
the tool commit's tree differs from the after commit's only under `docs/`); built in Release with no build server; the
session run under the drivers' `env -i` with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*` variable, over the same
`inputs/fuzz-seeds.txt`, the same test262 checkout (`/home/user/js-w2after-build/suite/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`,
the after driver's copy), the header's bound per compile and the program's default session seed (no
`--session-seed`), as the first. **The control**, as the first: in a second clean copy, `BridgeTreeBuilder.Template`
throws `System.InvalidOperationException` on a template literal ([`fcontrol/control.patch`](fcontrol/control.patch),
the first control's edit byte for byte), built and run as the session; then the file written again from the after
commit's blob and its digest checked, and the copy compared with a clean archive of the after commit carrying the same
tool files ([`fcontrol/revert-check.txt`](fcontrol/revert-check.txt)). **One departure of order**, for the disk rule
(8 GiB free before any build): the clean archive the revert is compared with is taken after the session and the build
output's removal, where `astep.sh` took it before the build; the comparison is the same, `diff -r -q -x bin -x obj`.
The scripts as started are named by digest in [`scripts/as-started.sha256.txt`](scripts/as-started.sha256.txt); they
were not changed. Both steps exited 0 ([`done/`](done/fuzz)).

**Retained** by [`../local/`](../local/README.md)'s convention: every file of the run folder unchanged at its path,
none over 2 MB; [`hashes.txt`](hashes.txt) gives every file's SHA-256, and a [`.gitattributes`](.gitattributes) keeps
the bytes as committed. [`findings.txt`](findings.txt), by [`read_jc.py`](read_jc.py), reads both sessions' findings
by seed, type and message, against the first session's mutant by mutant, and under the rule's revision of 2026-11-24.
The reading is the bundle README's.
