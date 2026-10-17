# Part C's base run, and a check at SJL-8.4's head

**Rule:** [`../decision-rule.md`](../decision-rule.md), part C, the reference datasets. **Population:**
[`conformance/datasets/time/`](../../../../conformance/datasets/time/README.md), Broiler.VM's `timezones.js` and
`temporal.js` with their retained answers and divergences as Broiler.VM's `9508bd5` holds them, each held by its
SHA-256 in `datasets.pin`. **Harness:**
[`scripts/conformance/run_reference_datasets.py`](../../../../scripts/conformance/run_reference_datasets.py) as
committed in `90ff6ab1`. Taken on 2026-10-17 at milestone SJL-8.4. Nothing here is a verdict: the rule judges the
after run, at the commit that lands SJL-8.6's cut-over, and reads the base without judging it.

| Folder | What it holds |
|---|---|
| [`base/`](base/) | **The base**, at the rule's commit `15a1bd8f`: its shell built in Release by the harness in a clean detached worktree of that commit, and the harness run from this branch. Each program's every line (`<program>.lines.tsv`), answer, standard error and driver; the counts (`summary.json`, `report.txt`); the identity (`identity.txt`, `shell-binaries.sha256`). Also [`observations.txt`](base/observations.txt), written by [`observe.py`](base/observe.py): the differing lines grouped by what each difference looks like, with the correspondence rows they seem to be cases of. It is an observation, not a classification |
| [`head-check/`](head-check/) | The same at this branch's head, `90ff6ab1`, whose engine sources and build inputs are the base's: the run, its comparison with the base by the harness's `compare` (`compare-with-base/`, its two line reports gzipped), and [`binaries.txt`](head-check/binaries.txt), which shows why the shell's binaries hash differently while the answers do not |

| Program | Lines | Equal to the expected answer at the base | Differing | At the head |
|---|---|---|---|---|
| `timezones.js` | 610 | 51 | 559 | the same answer, byte for byte; 0 lines moved |
| `temporal.js` | 1,629 | 1,622 | 7 | the same answer, byte for byte; 0 lines moved |

Both programs complete in both runs, with nothing on standard error. Each run's negative control changed one
line of the expected answer in memory (line 26 of `timezones.js`, line 1 of `temporal.js`, each an equal line)
and the comparison showed it, and no other line, as a difference.

**Cross-checks with Broiler.VM's correspondence.** The base's `timezones.js` answer is byte-identical to the
correspondence's own run of the program in this engine,
`Broiler.VM/docs/evidence/sjl-8-001/correspondence-runs/tz/z11-reference-timezones.bjs.txt` without its `exit=0`
line, and its 51 equal lines are the count the rule quotes. The harness's expected answer equals Broiler.VM's own
answer in that run, `z11-reference-timezones.vm.txt`, on all 610 lines.

**How they were taken**, from this repository's root:

```bash
git worktree add --detach <base tree> 15a1bd8f329f12238b093def5d1ddcd3e1dffe3d
python3 scripts/conformance/run_reference_datasets.py run --tree <base tree> --build --label base --out <base>
python3 scripts/conformance/run_reference_datasets.py run --build --label head-check --out <head-check>
python3 scripts/conformance/run_reference_datasets.py compare --base <base> --after <head-check> --out <head-check>/compare-with-base
python3 -I docs/evidence/sjl-8-001/datasets/base/observe.py docs/evidence/sjl-8-001/datasets/base > docs/evidence/sjl-8-001/datasets/base/observations.txt
```

The build logs are not retained: each build ended with 0 errors, and `identity.txt` names its command. The after
run is taken the same way at the cut-over's commit and joined with `base/` by `compare`.
