# Part C's base run, a check at SJL-8.4's head, and the after run

**Rule:** [`../decision-rule.md`](../decision-rule.md), part C, the reference datasets. **Population:**
[`conformance/datasets/time/`](../../../../conformance/datasets/time/README.md), Broiler.VM's `timezones.js` and
`temporal.js` with their retained answers and divergences as Broiler.VM's `9508bd5` holds them, each held by its
SHA-256 in `datasets.pin`. **Harness:**
[`scripts/conformance/run_reference_datasets.py`](../../../../scripts/conformance/run_reference_datasets.py) as
committed in `90ff6ab1`. Taken on 2026-10-17 at milestone SJL-8.4. Nothing here is a verdict: the rule judges the
after run, at the commit that lands SJL-8.6's cut-over, and reads the base without judging it. The bundle's
README, section 9, reads the after run under the rule.

*(Revised 2026-10-18, at SJL-8.7: the title read "Part C's base run, and a check at SJL-8.4's head", and the
after run, the observed run at `5f96cd40` and their rows below are added.)*

| Folder | What it holds |
|---|---|
| [`base/`](base/) | **The base**, at the rule's commit `15a1bd8f`: its shell built in Release by the harness in a clean detached worktree of that commit, and the harness run from this branch. Each program's every line (`<program>.lines.tsv`), answer, standard error and driver; the counts (`summary.json`, `report.txt`); the identity (`identity.txt`, `shell-binaries.sha256`). Also [`observations.txt`](base/observations.txt), written by [`observe.py`](base/observe.py): the differing lines grouped by what each difference looks like, with the correspondence rows they seem to be cases of. It is an observation, not a classification |
| [`head-check/`](head-check/) | The same at this branch's head, `90ff6ab1`, whose engine sources and build inputs are the base's: the run, its comparison with the base by the harness's `compare` (`compare-with-base/`, its two line reports gzipped), and [`binaries.txt`](head-check/binaries.txt), which shows why the shell's binaries hash differently while the answers do not |
| [`after/`](after/) | **The after run**, at `79a0506c`, the merge that lands row Z16's correction, the last of SJL-8.6: its shell built in Release by the harness in a clean detached worktree of that commit, and the harness run from this branch, whose harness, driver and copies are the base's byte for byte. The same files as the base's; its comparison with the base by `compare` (`compare-with-base/`, its two line reports gzipped); and [`classify.py`](after/classify.py)'s sorting of every differing line by the kind of its difference ([`classification.tsv`](after/classification.tsv), [`classification.txt`](after/classification.txt)), with the probe it ran in the after shell ([`probe.js`](after/probe.js), [`probe.answer.tsv`](after/probe.answer.tsv)). The script assigns no class |
| [`observed-5f96cd40/`](observed-5f96cd40/) | **Observed, not judged**: the run at `5f96cd40`, the merge of the cut-over and corpus revision 23, taken on 2026-10-17 from 23:10Z the same way, with its comparison and its sorting by the same `classify.py`. [`both-kinds.tsv`](observed-5f96cd40/both-kinds.tsv) lists its 128 lines that differed by both the resolved identifier (Z8) and the `longOffset` name's dropped seconds (Z16), the lines that brought row Z16 into SJL-8.6 (the bundle's README, section 5) |

| Program | Lines | Equal to the expected answer at the base | Differing | At the head | After, at `79a0506c` | Observed at `5f96cd40` |
|---|---|---|---|---|---|---|
| `timezones.js` | 610 | 51 | 559 | the same answer, byte for byte; 0 lines moved | 458 equal, 152 differing, each by the resolved identifier alone; 407 lines differing at the base are equal, none equal at the base differs, and 133 of the 152 moved since the base | 84 equal, 526 differing: 24 by the resolved identifier alone, 374 by dropped seconds alone, 128 by both |
| `temporal.js` | 1,629 | 1,622 | 7 | the same answer, byte for byte; 0 lines moved | 1,622 equal, 7 differing; the answer is the base's byte for byte | the same: the base's answer byte for byte |

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

The after run, on 2026-10-18 from 07:20Z, from this repository's root, with no `LANG` and `TZ` unset in the
harness (which sets `TZ=UTC` for the shell):

```bash
git worktree add --detach <after tree> 79a0506c870302cefa6d7105146c65feab898879
python3 scripts/conformance/run_reference_datasets.py run --tree <after tree> --build --label after --out <after>
python3 scripts/conformance/run_reference_datasets.py compare --base docs/evidence/sjl-8-001/datasets/base --after <after> --out <after>/compare-with-base
python3 -I docs/evidence/sjl-8-001/datasets/after/classify.py --after <after> --compare <after>/compare-with-base \
    --shell <after tree>/Broiler.JS/Broiler.JavaScript/bin/Release/net10.0/BroilerJS.dll --out <classification>
```

Both programs completed with nothing on standard error, and each negative control showed its changed line, and no
other, as a difference. The two compared line reports are retained gzipped, as the head check's are. The run at
`5f96cd40` was taken by the same commands at that commit, on 2026-10-17 from 23:10Z, and its files are kept as
written then; `both-kinds.tsv` is its `classification.tsv`'s header and the 128 rows whose kind is
`resolved-only-Z8 and seconds-only-Z16`.
