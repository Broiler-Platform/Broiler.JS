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
