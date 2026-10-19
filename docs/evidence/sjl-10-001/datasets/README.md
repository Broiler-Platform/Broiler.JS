# Part C's base run, and the after run

**Rule:** [`../decision-rule.md`](../decision-rule.md), part C, the reference datasets. **Population:**
[`conformance/datasets/time/`](../../../../conformance/datasets/time/README.md), Broiler.VM's `temporal.js` and
`temporal-calendars.js` with their retained polyfill answers and divergences as Broiler.VM's `14072b0` holds them,
each held by its SHA-256 in `datasets.pin`. **Harness:**
[`scripts/conformance/run_reference_datasets.py`](../../../../scripts/conformance/run_reference_datasets.py) with
`--bundle sjl-10-001`, as committed in `80dff79f`, which extended it to the second program. The base was taken on
2026-10-18 at milestone SJL-10.2, and the after run on 2026-10-19 at SJL-10.6. The rule judges the after run, at the
commit that lands SJL-10.5's cut-over, and reads the base without judging it. The bundle's README, section 9, reads
the after run under the rule.

*(Revised 2026-10-19, at SJL-10.6: the title read "Part C's base run", and the sentences after the harness read
"Taken on 2026-10-18 at milestone SJL-10.2. Nothing here is a verdict: the rule judges the after run, at the commit
that lands SJL-10.5's cut-over, and reads the base without judging it." The after run and its rows below are added.)*

| Folder | What it holds |
|---|---|
| [`base/`](base/) | **The base**, at the rule's commit `1d27064d`: its shell built in Release by the harness in a clean detached worktree of that commit, and the harness run from this branch. Each program's every line (`<program>.lines.tsv`), answer, standard error and driver; the counts (`summary.json`, `report.txt`); the identity (`identity.txt`, `shell-binaries.sha256`). Also [`observations.txt`](base/observations.txt), written by [`observe.py`](base/observe.py): the differing lines grouped by the correspondence row each seems to be a case of, by the shape of its operation. It is an observation, not a classification |
| [`after/`](after/) | **The after run**, at `00b65dd0`, the merge that lands SJL-10.5's cut-over: its shell built in Release by the harness in a clean detached worktree of that commit, and the harness run from this branch, whose harness, driver and copies are the base's byte for byte. The same files as the base's, and its comparison with the base by the harness's `compare` (`compare-with-base/`, its two line reports gzipped). No line differs from its expected answer, so nothing is sorted or observed beside it |

| Program | Lines | Equal to the expected answer at the base | Differing | After, at `00b65dd0` |
|---|---|---|---|---|
| `temporal.js` | 1,629 | 1,622 | 7 | 1,629 equal, 0 differing; the 7 lines that differed at the base are equal, and none equal at the base differs |
| `temporal-calendars.js` | 3,999 | 3,896 | 103 | 3,999 equal, 0 differing; the 103 lines that differed at the base are equal, and none equal at the base differs |

Both programs complete in both runs, with nothing on standard error. Each run's negative control changed line 1 of
the expected answer in memory, an equal line, and the comparison showed it, and no other line, as a difference. All
70 of `temporal-calendars.js`'s divergences are used in both runs, as Broiler.VM's check asks. The after run's
`temporal.js` answer is the retained polyfill answer, `temporal.polyfill-e8cc03fc.txt`, byte for byte.

*(Revised 2026-10-19, at SJL-10.6: the table had no after column, and the paragraph spoke of the base alone, "Both
programs complete, with nothing on standard error. Each negative control changed line 1 of the expected answer in
memory, an equal line, and the comparison showed it, and no other line, as a difference. All 70 of
`temporal-calendars.js`'s divergences are used, as Broiler.VM's check asks.")*

**Which lines differ, and under which rows** ([`observations.txt`](base/observations.txt)). The rule says "every
line this engine answered otherwise when the correspondence ran the two programs is a row SJL-10.5 corrects - in
temporal.js row D6, and in temporal-calendars.js rows C5, C15, C16 and C17, F4 and D11". Each differing line has
the shape of the operation one of those rows names, and each row has the count the correspondence's run found. No
differing line falls outside them.

| Program | Row | Lines | What this engine answers |
|---|---|---|---|
| `temporal.js` | D6 | 7 | P1Y2M3W4DT5H6M7.008009010S rounded to months relative to 2020-01-31 in 7 of 9 modes: P1Y2M30D, where P1Y3M is expected |
| `temporal-calendars.js` | C5 | 5 | `from M13` constrained to a date in `indian`, `islamic-civil`, `islamic-tbla`, `islamic-umalqura` and `persian`, where a `RangeError` is expected |
| | C15 | 15 | `toPlainYearMonth` keeps the date's own ISO day, 2023-03-15, in every calendar but ISO 8601 |
| | C16 | 4 | `toPlainMonthDay` in `gregory`, `buddhist`, `roc` and `japanese` gives an ISO 8601 month-day |
| | C17 | 2 | a month-day's `toPlainDate` in `buddhist` and `roc` reads the calendar year as an ISO year |
| | F4 | 55 | `until … round months` ignores the rounding, five lines in each of 11 non-ISO calendars |
| | D11 | 22 | `Duration` `round` and `total` relative to a date in another calendar throw a plain `Error`, two lines in each of the same 11 |

**Cross-checks with Broiler.VM's correspondence.**
- **This engine's answers.** Both programs' answers are byte-identical to the correspondence's own runs of them in
  this engine, `Broiler.VM/docs/evidence/sjl-10-001/correspondence-runs/syntax/d-retained-temporal.bjs.txt.gz` and
  `d-retained-temporal-calendars.bjs.txt.gz`, each without its `exit=0` line. The 103 differing lines of the second
  are the line numbers its `d-retained-temporal-calendars.compare.txt` lists.
- **The expected answers.** The harness's expected answers equal Broiler.VM's own answers in those runs,
  `d-retained-temporal.vm.txt.gz` and `d-retained-temporal-calendars.vm.txt.gz`, on all 1,629 and 3,999 lines.

**No check at this branch's head.** SJL-8.4 ran one because its head carried new shared code. This branch changes
no engine file: `git diff --name-only 1d27064d 80dff79f` lists the three copies, the pin, the harness, its driver
and tests, `.gitattributes`, `THIRD_PARTY_NOTICES.md` and the copies' README.

**Why a fresh build.** A Release build of `d3464413` was at hand, whose product sources are `1d27064d`'s. The rule
takes the base with "the shell built there", at the commit that carries it, and a base taken at any other commit is
void. So the shell was built in a new detached worktree of `1d27064d` instead.

**How it was taken**, from this repository's root, on 2026-10-18 from 11:56Z, with no `LANG` and `TZ` unset in the
harness, which sets `TZ=UTC` for the shell:

```bash
git worktree add --detach <base tree> 1d27064d0a1f57608a7af20e37eac91bd8887baa
python3 scripts/conformance/run_reference_datasets.py run --bundle sjl-10-001 --tree <base tree> --build --label base --out <base>
python3 -I docs/evidence/sjl-10-001/datasets/base/observe.py docs/evidence/sjl-10-001/datasets/base > docs/evidence/sjl-10-001/datasets/base/observations.txt
```

The build log is not retained: the build ended with 0 errors, and `identity.txt` names its command. The after run
is taken the same way at the cut-over's commit and joined with `base/` by the harness's `compare`.

**The after run**, on 2026-10-19 from 13:45Z, from this repository's root at `00b65dd0`, with no `LANG` and `TZ` unset
in the harness, which sets `TZ=UTC` for the shell:

```bash
git worktree add --detach <after tree> 00b65dd055a277859d5325ead6338a040bcf2f30
python3 scripts/conformance/run_reference_datasets.py run --bundle sjl-10-001 --tree <after tree> --build --label after --out <after>
python3 scripts/conformance/run_reference_datasets.py compare --base docs/evidence/sjl-10-001/datasets/base --after <after> --out <after>/compare-with-base
```

The shell was built by the harness from the clean worktree, the first build there, and ended with 0 errors; the
build log is not retained, as the base's is not. The two compared line reports are retained gzipped, as bundle
`sjl-8-001`'s after run retained its own. The collector of this bundle has no step for part C, as bundle
`sjl-8-001`'s had none: its after run was taken by these commands too.
