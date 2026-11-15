# Part B's runs: the bridge's identity with the switch off (SJL-14.2's tail)

**Taken:** 2026-11-15, after W1 and after the two merges to `Broiler.ECMAScript/Broiler.ECMAScript.Frontend*/` that
followed it, for [`../decision-rule.md`](../decision-rule.md)'s part B. Its reading, and the reading of "dark commit" it
rests on, are in the bundle's [`README.md`](../README.md), "Part B, judged on 2026-11-15". Nothing here changes the
rule, a tool or a product file.

## What part B asks

"bundle sjl-12-001's tool docs/evidence/sjl-12-001/tools/parse-corpus, byte-unchanged and built as this file's header
says, in Release against each commit's projects, on one thread with a 256 MB stack, no LANG and TZ=UTC: entry points P
and K, and the --dump mode (before and after the compiler's for-loop pass), at W1's first parent and at W1, the switch
at its default; the same at each dark commit and its first parent; and every test project of Broiler.JS.slnx at W1 by
its CI run; at each dark commit and its first parent, the side-by-side tool's entry points B and K with the switch on
are also run, retained and read, not judged here". MET "if and only if, at W1 and at every dark commit, both runs are
complete and identical in every field, and W1's CI run of every test project passed."

## The commits

| Folder | Commit | What it is |
|---|---|---|
| [`e2ed7fae/`](e2ed7fae/) | `e2ed7fae6384b484b3b9773fd89dd33e40c01884` | W1's first parent, the merge of Broiler-Platform/Broiler.JS#1101 |
| [`8e5519d8/`](8e5519d8/) | `8e5519d8cf81808856e32f2275fcb4d3b98a73f3` | W1, the merge of Broiler-Platform/Broiler.JS#1102 |
| [`6f7284da/`](6f7284da/) | `6f7284daa2d2fbec3104a9498d5f7c989f956946` | the first parent of #1104, the merge of Broiler-Platform/Broiler.JS#1103 (records only) |
| [`29061d65/`](29061d65/) | `29061d65f0ca41c5bd93074083f2018ccedd5c57` | the merge of Broiler-Platform/Broiler.JS#1104, and the first parent of #1105 |
| [`35da77ec/`](35da77ec/) | `35da77eca574f5cbd1163c5cd26bd19c3b69e8d5` | the merge of Broiler-Platform/Broiler.JS#1105 |
| [`e4b6b49e/`](e4b6b49e/) | `e4b6b49ed290983e02525ca1a9318fd46517dc2c` | the default branch's head on 2026-11-15, the merge of Broiler-Platform/Broiler.JS#1116: read, not judged, outside part B |

`29061d65` is both the merge #1104 and the first parent of #1105: it was run once, and that run serves both pairs.
Every product path each merge since W1 changed, against its first parent, is in
[`merges-since-w1-paths.txt`](merges-since-w1-paths.txt) (a path is marked `product` by the rule's list of the product
paths, decision D42).

## What was run

- **W1's CI run of every test project**, in [`w1-ci/`](w1-ci/commands.txt): the runs GitHub Actions made on the push of
  W1 to the default branch, read by read-only API calls and kept as the API returned them (each run's record, its jobs
  with their steps, its artifacts with their digests, and its job logs), with
  [`w1-ci/test-projects.tsv`](w1-ci/test-projects.tsv), every test project of `Broiler.JS.slnx` at W1 with the result
  line each test job wrote for it. CI (`.github/workflows/ci.yml`) runs `Broiler.JS`'s test projects on Linux and
  Windows and the engine corpus in its vectors jobs; the workflow Shared libraries runs `Broiler.ECMAScript`'s test
  projects, which CI's test step leaves to it. Every run concluded `success`, and every test project has a passing
  result line on both systems. The vectors jobs raised warnings that the workflow raises without failing (vectors run
  again alone, and the Windows report against the Linux one); the logs hold them.
- **The local identity runs**, per commit, by [`scripts/commit.sh`](scripts/commit.sh) (called once per commit by
  [`scripts/all.sh`](scripts/all.sh), each call holding the machine's build lock behind its idle gate,
  [`scripts/run.sh`](scripts/run.sh) and [`scripts/gate.sh`](scripts/gate.sh)): a clean `git archive` of the commit on
  `/dev/shm`, bundle `sjl-12-001`'s tool built from it unchanged (its folder's tree is the same object at every commit
  here, `5ede6114`) in Release, outside the tree, with no build server, and run with `env -i`, so with `TZ=UTC` and no
  `LANG`, `LC_*` or `BROILER_*` variable, on the tool's own thread of 256 MB:
  - `parse-corpus-c.tsv.gz`, the default mode: entry points P and K for every input and variant of
    `conformance/parse/corpus.manifest`;
  - `parse-corpus-d1.tsv.gz`, `--dump --order manifest`: P's tree and the for-loop pass's tree, hashed by the
    canonical dump.
  The suite the manifest's test262 rows read is Broiler.VM's archive of test262 at the manifest's pin, its SHA-256
  checked against the manifest's (`logs/commit.txt`). The switch is at its default in both runs.
- **The switch-on outputs** at #1104's and #1105's merges and their first parents, `switch-on-b-kon.tsv.gz`: the
  side-by-side tool (`../tools/side-by-side`, the same tree at every commit here) at entry points B and K-on with
  `--dump`, built and run the same way. At W1 the side-by-side run of all four entry points is
  [`../side-by-side/w1/`](../side-by-side/w1/README.md).
- **The comparisons**, by [`scripts/identity.sh`](scripts/identity.sh), in [`identity/`](identity/): each report
  complete (its last line `#% end` with its row count, the manifest's digest the repository's), the side-by-side tool's
  `compare.py --same` over P's and K's rows (outcome, exception type and message) and over the dump rows (P's outcome
  and both hashes), and every row line byte for byte. The headers differ only in each product assembly's module version
  id, which names the build. The switch-on outputs are compared the same way (`compare.py --same` over B's and K-on's
  rows), read and not judged.
- **The head, read** ([`e4b6b49e/`](e4b6b49e/), [`identity/head-read-8e5519d8-e4b6b49e.txt`](identity/head-read-8e5519d8-e4b6b49e.txt)):
  the same two runs at the default branch's head, set against W1's. It is no part of part B: the merges between W1 and
  the head that change product paths outside `Frontend*/` are not dark commits by any reading, and part B does not cover
  them; this run reads whether any of them moved a switch-off outcome over the corpus, for part C at the base commit to
  judge.

Each commit's `logs/` holds its record (`commit.txt`), the commands as run (`commands.log`), each step's exit, time and
peak memory (`runs.tsv`, `*.time.txt`), the build's and the runs' output, the built binaries' SHA-256
(`binaries.sha256.txt`), the reports' SHA-256 (`binaries-and-reports.sha256.txt`), and the gate's records of the machine
before and after (`gate-*.txt`). [`logs/`](logs/) holds the gate's log for every hold of the lock this stage took,
the side-by-side text run's among them. [`hashes.txt`](hashes.txt) gives the SHA-256 of every file here.

## What the runs say

Every pair is identical in every field: at W1 against its first parent, at #1104 against its first parent and at #1105
against its first parent, both the default-mode and the dump reports are complete, and their rows are byte for byte the
same. The switch-on outputs are the same at all three commits where they were taken, and the same as the B and K-on rows
of the side-by-side run at W1. The head's switch-off rows are the same as W1's.
