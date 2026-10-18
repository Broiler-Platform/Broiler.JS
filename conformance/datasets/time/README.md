# Broiler.VM's reference datasets of the time area

**Owner:** the Broiler.JS maintainer. **Reviewer:** none.

These are the populations of part C of two bundles' rules.
- **Bundle `sjl-8-001`'s rule**,
  [`docs/evidence/sjl-8-001/decision-rule.md`](../../../docs/evidence/sjl-8-001/decision-rule.md), committed at
  milestone SJL-8.1 in `15a1bd8f`: "Broiler.VM's `src/tests/cldr/timezones/timezones.js` and
  `src/tests/temporal/temporal.js`, byte for byte as Broiler.VM's commit 9508bd5 holds them, with Broiler.VM's
  retained answer and divergences file for each; SJL-8.4 copies them into this repository with their digests,
  and a copy whose digest differs is not the population". Milestone SJL-8.4 copied them on 2026-10-17.
- **Bundle `sjl-10-001`'s rule**,
  [`docs/evidence/sjl-10-001/decision-rule.md`](../../../docs/evidence/sjl-10-001/decision-rule.md), committed
  at milestone SJL-10.1 in `1d27064d`: "Broiler.VM's src/tests/temporal/temporal.js and
  src/tests/temporal/calendars/temporal-calendars.js, byte for byte as Broiler.VM's commit 14072b0 holds them,
  each with Broiler.VM's retained polyfill answer and divergences file". The first is SJL-8.4's copy, "copied
  from Broiler.VM's 9508bd5, whose three blobs are 14072b0's". Milestone SJL-10.2 copied the second, with its
  answer and its divergences, on 2026-10-18; its divergences file is "named temporal-calendars.divergences.txt
  here, as SJL-8.4 named the first's". `timezones.js` is not that rule's.

They sit beside the corpus, [`conformance/vectors/`](../../vectors/README.md), and are not part of it: a
vector pins one operation's outcome from the specification or an exact computation, while these are three whole
programs and the answers two reference implementations gave, with Broiler.VM's divergences from them.

## What is here

| File | Broiler.VM's path, at `9508bd5` for the first six and at `14072b0` for the last three | What it is |
|---|---|---|
| `timezones.js` | `src/tests/cldr/timezones/timezones.js` | For each of tzdb 2026e's 597 Zone and Link names, the identifier `Intl.DateTimeFormat` resolves it to and the `longOffset` name it formats at eight instants (1850, 1900, 1940, 1975, 2000, January and July 2026, 2150); then nine regions' zones, an identifier in upper case, an unknown and a non-ASCII one, and the identifiers `Intl.supportedValuesOf` lists. 610 lines |
| `timezones.icu-77.1.txt` | `src/tests/cldr/timezones/timezones.icu-77.1.txt` | Node 22.22.0's answer (ICU 77.1, CLDR 47.0, tzdb 2025b), produced on 2026-10-05 by `node -e "const fs=require('fs');process.stdout.write((0,eval)(fs.readFileSync('timezones.js','utf8'))+'\n')"` |
| `timezones.divergences.txt` | `src/tests/cldr/timezones/divergences.txt` | The 51 lines on which Broiler.VM answers otherwise, each as Broiler.VM answers it, under its reason: the primary identifier (45), Morocco's permanent +00 in tzdb 2026e (2), and the region lists and listed identifiers (4) |
| `temporal.js` | `src/tests/temporal/temporal.js` | Temporal in the ISO 8601 and Gregorian calendars: durations rounded and totalled, dates and their arithmetic, times and date-times rounded, seven time zones at five instants near transitions with wall times in gaps and folds, instants rounded, year-months and month-days. 1,629 lines |
| `temporal.polyfill-e8cc03fc.txt` | `src/tests/temporal/temporal.polyfill-e8cc03fc.txt` | The answer of the Temporal proposal's reference polyfill, `polyfill/lib` of [tc39/proposal-temporal](https://github.com/tc39/proposal-temporal) at `e8cc03fc970a65a3359e8870e3b35e687ac94e55`, on Node 22.22.0, produced on 2026-10-05 |
| `temporal.divergences.txt` | `src/tests/temporal/divergences.txt` | The lines on which Broiler.VM answers otherwise: none |
| `temporal-calendars.js` | `src/tests/temporal/calendars/temporal-calendars.js` | Temporal in the fifteen calendars of the Intl era and month code proposal's Table 1 other than ISO 8601: the calendar fields of 137 ISO dates from 501 BCE to the year 10000; dates from fields (ordinal months, month codes, a thirteenth month, a leap month, eras, and the errors of a missing or a disagreeing field), each added nine durations to, differenced to five dates in four largest units and rounded, changed by `with`, made into year-months and month-days, and a duration rounded and totalled relative to it; and month-days for fifteen month codes. 3,999 lines |
| `temporal-calendars.polyfill-e8cc03fc.txt` | `src/tests/temporal/calendars/temporal-calendars.polyfill-e8cc03fc.txt` | The answer of the same polyfill at the same revision, on Node 22.22.0, produced on 2026-10-05. The polyfill computes every calendar but ISO 8601 by formatting dates with Node's `Intl.DateTimeFormat`, whose ICU4C is 77.1 |
| `temporal-calendars.divergences.txt` | `src/tests/temporal/calendars/divergences.txt` | The 70 lines on which Broiler.VM answers otherwise, each as Broiler.VM answers it, under its reason: Chinese and Korean years outside the published ones (64), Chinese years within them where ICU4C's astronomy differs from the observatories' calendars (5), and the Hebrew year 5806 (1). On each, ICU4X 2.3.0 answers as Broiler.VM does |
| `datasets.pin` | | Each copy's length, SHA-256, Git blob id and Broiler.VM path, the Broiler.VM commit it was copied from, and which three files make each program's dataset |

Broiler.VM's `README.md` beside each program says more of what it covers and how its answer was produced. The
three divergences files are each `divergences.txt` there, so each is named after its program here; no other
name is changed, and no byte. Broiler.VM's `431c20e`, where its SJL-8 rule's bases were taken, holds the first
six blobs. Its `14072b0` holds the three of `temporal.js`'s dataset, and its `99c9c9f`, where its SJL-10 rule's
bases are taken, holds the same blobs of both Temporal programs. Broiler.VM keeps a fourth file beside
`temporal-calendars.js`, ICU4X's answer for its 2,055 conversion lines, which its check
`temporal/t3/calendar-conversions-match-icu4x` reads; neither rule here takes it, and it is not copied.

**Each line of a program is one operation and its answer**, written `<what> => <answer>`. Broiler.VM's slice
compiler runs each program in its own engine and holds every line to the retained answer with the divergences
applied (`IntlChecks.RetainedMatches`, checks `tzdb/time-zone-offsets-match-icu`,
`temporal/t1/temporal-matches-the-reference-polyfill` and `temporal/t3/calendars-match-the-reference-polyfill`).
That expected answer is the one each rule compares this engine with.

## How the copies are held

- **By their digests.** [`datasets.pin`](datasets.pin) records each copy's SHA-256, from the bytes
  `git cat-file blob <commit>:<path>` wrote in a Broiler.VM checkout, and each one's Git blob id, which
  `git -C <Broiler.VM> rev-parse <commit>:<path>` prints; `<commit>` is the `source-commit` of the copy's block
  in the pin, `9508bd5` for the first six and `14072b0` for the last three. The harness refuses to run when a
  copy differs from its line, and `scripts/conformance/tests/test_reference_datasets.py` holds every copy to its
  line, and its blob id to its bytes, on every run of the conformance scripts' tests. By hand, in this
  directory:

  ```bash
  awk '$1 == "file" { print $4 "  " $2 }' datasets.pin | sha256sum -c
  ```

- **Without conversion.** The nine copies are `-text` in [`.gitattributes`](../../../.gitattributes), so no
  end-of-line filter touches them on any platform. They are ASCII with LF line endings.
- **Never edited.** Each rule names the Broiler.VM commit it takes its copies from. Another answer or program
  would be another copy from another named commit, and a new rule; a pin line is added, never changed.

## Running them

[`scripts/conformance/run_reference_datasets.py`](../../../scripts/conformance/run_reference_datasets.py) is
the harness the rule names, with its driver,
[`reference_dataset_driver.js`](../../../scripts/conformance/reference_dataset_driver.js). For each program it
starts `dotnet BroilerJS.dll --script-host` on the driver, in this directory, with `TZ=UTC` and with `LANG`,
`LANGUAGE` and every `LC_` variable removed. The driver reads the program's text with the shell's `read`,
evaluates it by indirect eval, `(0, eval)(text)`, as the reference commands above do in Node, and prints the
completion value. The harness splits that value into lines, builds the expected answer and compares them line
by line, exactly as `RetainedMatches` does; its docstring states each step with the code it follows.

```bash
# One run, in the shell built in Release from a checkout (this one by default); --build builds it first:
python3 scripts/conformance/run_reference_datasets.py run --tree <checkout> --build --label base --out <dir>
# An after run against a base run, line by line:
python3 scripts/conformance/run_reference_datasets.py compare --base <base dir> --after <after dir> --out <dir>
# The copies against datasets.pin:
python3 scripts/conformance/run_reference_datasets.py verify
```

A run writes, per program, `<program>.lines.tsv` (every line: its number, whether it is equal, whether its
expected text is the retained line or a divergence, its key, the expected text and the shell's), the answer as
`<program>.answer.txt`, the shell's standard error, and the driver it ran; and for the run, `summary.json`,
`report.txt`, `identity.txt` (the shell's commit, its binaries' digests, the SDK, the machine, the zone and
locale variables, the copies' digests) and `shell-binaries.sha256`. Each run also changes one line of the
expected answer in memory and records that the comparison shows it as a difference. A compare writes, per
program, `<program>.compare.tsv`: each line after against its expected answer and against the same line at the
base, with the counts in `compare.json` and `compare.txt`.

**The harness decides nothing.** It counts equal and differing lines. The rule's classes D1, D2 and D3 are
assigned by a person, and the base run, retained in
[`docs/evidence/sjl-8-001/datasets/base/`](../../../docs/evidence/sjl-8-001/datasets/base/), is read but not
judged.

## Licence

The three programs are Broiler.VM's, by the same contributors as this repository, under the Apache License 2.0
(their SPDX headers say so; the licence text is [`LICENSE`](../../../LICENSE)). The answers are what ICU 77.1,
through Node, and the reference polyfill printed for those programs, one line per operation: they reproduce
none of either's code or text. [`THIRD_PARTY_NOTICES.md`](../../../THIRD_PARTY_NOTICES.md) records their
sources, as Broiler.VM's notices record the polyfill's. Nothing here ships: no project builds or packs this
directory.
