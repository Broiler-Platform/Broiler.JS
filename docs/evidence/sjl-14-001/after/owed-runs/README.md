# The runs owed to the after readings, taken on 2026-11-24 (reading aids and one base-side run; retained)

**Taken:** 2026-11-24, 03:36Z to 03:50Z, after every after run of [`../local/`](../local/README.md) and part A's after
run, by the reading stage on branch `sjl-14-after`, with the default branch's head read as the after commit
`83a9bd82a5d258d3001468095ebf43f9efc3e9a7` before each ([`chain.log`](chain.log)). Each through the machine-wide lock
and idle gate ([`../local/scripts/run.sh`](../local/scripts/run.sh), the same script; run.sh's records in
[`logs/`](logs/runs.tsv)), from a clean `git archive` on disk outside every working tree, removed when its step ended,
builds in Release with no build server, every run under the drivers' `env -i` with `TZ=UTC` and no `LANG`, `LC_*` or
`BROILER_*` variable, by [`scripts/rstep.sh`](scripts/rstep.sh) under [`scripts/rchain.sh`](scripts/rchain.sh). None is
a run of a part at the after commit; each is named by the reading that needs it, in the bundle README's "W2's after
commit, recorded on 2026-11-24".

| Step | What | Commit | Why | Outputs |
|---|---|---|---|---|
| `dtextbase` | bundle `sjl-12-001`'s parse-corpus tool, built as the base's `tools` step built it, run with `--dump --order manifest --text`, as the after driver's `dtext` ran it at the after commit | the base commit `69a8a13b` | part D's dump lines at the base: part C's base text (`dumps-p-b.txt.gz`, by digest in the base records) holds the parser trees only, and part D's dump hashes are of the parser trees and of the trees after the compiler's for-loop pass | [`dtextbase/`](dtextbase/identity-with-base-d1.txt): its report's rows identical line by line to the base's retained `part-d/parse-corpus-d1.tsv.gz` (the report by digest in [`not-retained.sha256.txt`](not-retained.sha256.txt), the text by digest in `dtextbase/text.sha256.txt`) |
| `jsealbase` | Broiler.JSeal's suites against the base candidate feed, under the base's own harness (`../../base/local/scripts/bstep.sh`'s `jseal` step as its attempt 3 ran it) with the after run's one addition, a TRX of each test project | the base commit's candidate feed, kept since the base run | part G compares per test, and the base run kept no passed test by name (`../../base/local/retained.txt`) | [`jsealbase/`](jsealbase/jseal-trx.tsv): the feed checked against the base's digests, the candidate directory the base run's byte for byte, the consumer's report and both TRX files; its report against the base run's, leaf by leaf ([`jsealbase/report-against-base-run.txt`](jsealbase/report-against-base-run.txt)) |
| `jsingle` | a single-case reading aid ([`scripts/single-case/`](scripts/single-case/Program.cs)) compiling one file through `CoreScript.Compile` as part J's session does, in a process of its own, with nothing else compiling, waiting past the session's bound | the after commit | part J: what each finding is, and whether the timeouts reproduce unloaded | [`jsingle/cases.tsv`](jsingle/cases.tsv): the seed T037823 and one of its findings, the seed V000002 and each of its findings |

**`jsealbase` was taken twice.** Its first attempt stopped at this stage's own check of the base feed, before any
consumer command (exit 3): the check listed only the feed's `.nupkg` files against the base's digest list, which also
names `.snupkg` files. The check was corrected to list every file, `rstep.sh` written again (its first text is
[`scripts/rstep.sh.before-fix`](scripts/rstep.sh.before-fix), whose SHA-256 is its line in
[`scripts/as-started.sha256.txt`](scripts/as-started.sha256.txt); `dtextbase`, the first attempt and `jsingle` ran that
text), and the step taken again; the first attempt's folder and run.sh files are kept in
[`jsealbase-attempt-1/`](jsealbase-attempt-1/base-feed-check.txt).

[`hashes.txt`](hashes.txt) gives the SHA-256 of every file here, and a [`.gitattributes`](.gitattributes) keeps the
bytes as committed. Outside the repository, by digest: the base commit's dump text
(`/home/user/js-w2read-build/69a8a13b/text/dumps-p.txt.gz`, `dtextbase/text.sha256.txt`).

*(Noted 2026-11-24, after the comparisons, in review of this record.)* The sentence above names the base commit's
dump text as kept "Outside the repository, by digest". On 2026-11-24, once part D's comparison was done, that text was
removed with the build folder that held it, `/home/user/js-w2read-build/`; it remains named by digest only
(`dtextbase/text.sha256.txt`), and part D's tables that read it regenerate only after `dtextbase` is taken again
(the bundle README's part D, and `../part-d/README.md`). The sentence about `rstep.sh.before-fix` above first read
"whose SHA-256 is the first one recorded in"; it is the line for `rstep.sh.before-fix` in
[`scripts/as-started.sha256.txt`](scripts/as-started.sha256.txt), which now carries a header saying what it records.

*(Added 2026-11-24, later, by the records stage of the owner's answers 12 to 15 of 2026-11-24, on branch
`sjl-14-after`.)* Two further folders are retained here, each a record of runs taken after every after run by others
than this folder's stage, copied unchanged from the session's notes (every file's SHA-256 equal to its source's when
copied), each named by its SHA-256 in [`hashes.txt`](hashes.txt). Neither is a run of a part.
- [`verifier-retake/`](verifier-retake/runs/logs/runs.tsv): the review of this folder's stage took the two commits'
  dump texts again, each from a clean `git archive` built in Release through the same `run.sh` (lock and idle gate,
  `env -i` with `TZ=UTC`), as `dtext` and `dtextbase` ran them ([`scripts/vstep.sh`](verifier-retake/scripts/vstep.sh),
  2026-11-24, 05:21Z to 05:27Z), and the single-case aid `jsingle` again
  ([`scripts/vsingle.sh`](verifier-retake/scripts/vsingle.sh), 05:36Z to 05:43Z). The texts' digests,
  [`runs/69a8a13b/text.sha256.txt`](verifier-retake/runs/69a8a13b/text.sha256.txt) and
  [`runs/83a9bd82/text.sha256.txt`](verifier-retake/runs/83a9bd82/text.sha256.txt), are
  `7343c06232cd45b2efa4b91d601fb3e512016c0344fd2992a357eefa3127c50d` (the base commit's) and
  `0db1abaf25b28b9224a2187aae79d87f3f40b7f4023acb4781ed5ba88afb65f4` (the after commit's), the digests
  [`dtextbase/text.sha256.txt`](dtextbase/text.sha256.txt) and `../local/dtext/text.sha256.txt` record for the texts
  part D's tables were read from: the texts' bytes are reproduced by a fresh build and run at each commit. The texts
  themselves were removed with the verifier's build folder; the build logs, the binaries' digests and run.sh's records
  are here. The single-case re-take gives every file the same SHA-256, outcome, type, message and `within-bound` as
  [`jsingle/cases.tsv`](jsingle/cases.tsv) (only the times differ).
- [`jbase/`](jbase/README.md): a reading aid for part J at the **base** commit `69a8a13b`, taken for the owner's
  question 14 (2026-11-24, 05:49Z to 05:56Z, through the same `run.sh`): the seed V000002 and its 22 findings of the
  after session, each compiled alone in a process of its own by this folder's single-case aid
  ([`scripts/single-case/`](scripts/single-case/Program.cs)) built inside a clean archive of the base commit, with the
  same bound and cap as `jsingle` ([`jbase/scripts/jbase.sh`](jbase/scripts/jbase.sh), SHA-256
  `aa172d0cdc20e86be9384afb51bb992e1ef16e6019595084322617d348ee433c`; [`jbase/runs/cases.tsv`](jbase/runs/cases.tsv),
  SHA-256 `eee7329431f40c66fd22b7af74c1d6c8bbab961775fab9c0b4af399a87f7c415`; run.sh's records in
  [`jbase/logs/`](jbase/logs/runs.tsv)). Its README is its author's note as written. A reading aid taken after the
  after runs: the seed and 21 of its mutants are accepted at the base and compile past the bound there as after; the
  mutant `V000002-47.js` is refused at the base by the old parser and accepted after.
