# Broiler.VM's reference datasets of the Intl area

**Owner:** the Broiler.JS maintainer. **Reviewer:** none.

These are the population of parts C and D of **bundle `sjl-9-001`'s rule**,
[`docs/evidence/sjl-9-001/decision-rule.md`](../../../docs/evidence/sjl-9-001/decision-rule.md), committed at
milestone SJL-9.1. Part C takes "Broiler.VM's reference datasets of the area, byte for byte as Broiler.VM's commit that
carries its own bundle sjl-9-001 rule holds them, each with Broiler.VM's retained answer and divergences file:
src/tests/cldr/{dates, displaynames, durations, lists, locales, numbers, plurals, relativetimes, segments}/ and
src/tests/temporal/{intl, calendars-intl, zone-names}/, which SJL-9.2 copies into conformance/datasets/intl/ with their
digests and the pin conformance/datasets/intl/datasets.pin; and src/tests/cldr/timezones/, which
conformance/datasets/time/ holds since SJL-8.4 under block 1 of its datasets.pin". Part D runs part C's programs again
under invariant globalization. Milestone SJL-9.2 (work package SJL-9.2a) copied the twelve programs here on
2026-10-26, from Broiler.VM's `4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f`, the merge of Broiler-Platform/Broiler.VM#171,
which carries that rule and JSD-0082. The thirteenth program, `timezones.js`, stays in
[`conformance/datasets/time/`](../time/README.md), and the harness reads it from there. Broiler.VM's `orderings`
dataset is collation's, SJL-17's, and is not copied (JSD-0082, decision D16).

They sit beside the corpus, [`conformance/vectors/`](../../vectors/README.md), and are not part of it: a vector pins one
operation's outcome with its provenance, while these are whole programs and the answers a reference implementation
gave, with Broiler.VM's divergences from them.

## What is here

Each program is three files: the program, the answer the reference gave for it, and the lines on which Broiler.VM
answers otherwise (`<program>.divergences.txt` here, `divergences.txt` beside the program in Broiler.VM). Broiler.VM's
`README.md` beside each program says what it covers and how its answer was produced; in short:

| Program | Broiler.VM's directory | What it covers | The answer |
|---|---|---|---|
| `dates` | `src/tests/cldr/dates/` | `Intl.DateTimeFormat` in `en`, `en-US`, `de` and `de-DE` with the `-u-hc`, `-u-nu` and `-u-ca` keywords, over the whole time-value range: `format`, `formatToParts`, ranges and `resolvedOptions` | `dates.icu-77.1.txt`, Node 22.22.0 (ICU 77.1, CLDR 47.0) |
| `displaynames` | `src/tests/cldr/displaynames/` | `Intl.DisplayNames` in `en` and `de`, every type and style, both fallbacks and language displays, and the errors ECMA-402 states | `displaynames.icu-77.1.txt`, the same Node |
| `durations` | `src/tests/cldr/durations/` | `Intl.DurationFormat` in `en` and `de`: every style, per-unit styles, displays, fractional digits and a numbering system, and the errors | `durations.icu-77.1.txt`, the same Node |
| `lists` | `src/tests/cldr/lists/` | `Intl.ListFormat` in the four locales, every type and style, as strings and parts, and the errors | `lists.icu-77.1.txt`, the same Node |
| `locales` | `src/tests/cldr/locales/` | `Intl.Locale` over tags of every shape: identifiers, parts, keywords, likely subtags and the information methods | `locales.icu-77.1.txt`, the same Node |
| `numbers` | `src/tests/cldr/numbers/` | `Intl.NumberFormat` in the four locales, the `-u-nu` keyword and the `numberingSystem` option: `format`, `formatToParts`, ranges and `resolvedOptions` | `numbers.icu-77.1.txt`, the same Node |
| `plurals` | `src/tests/cldr/plurals/` | `Intl.PluralRules` in the four locales, cardinal and ordinal, under digit options, rounding and notations | `plurals.icu-77.1.txt`, the same Node |
| `relativetimes` | `src/tests/cldr/relativetimes/` | `Intl.RelativeTimeFormat` in the four locales, every style and numeric option, in every unit | `relativetimes.icu-77.1.txt`, the same Node |
| `segments` | `src/tests/cldr/segments/` | `Intl.Segmenter` by grapheme, word and sentence in `en` and `de` (SJL-7's; the rule reads its lines in class O) | `segments.icu-77.1.txt`, the same Node |
| `temporal-intl` | `src/tests/temporal/intl/` | Intl over Temporal objects in English and German, the Gregorian and ISO 8601 calendars | `temporal-intl.polyfill-e8cc03fc.txt`, the Temporal proposal's reference polyfill at `e8cc03fc` on the same Node |
| `calendar-formats` | `src/tests/temporal/calendars-intl/` | `Intl.DateTimeFormat` in English and German in the fourteen CLDR calendars of the Intl era and month code proposal's Table 1 other than Gregorian and ISO 8601 | `calendar-formats.polyfill-e8cc03fc.txt`, the same polyfill on the same Node |
| `zone-names` | `src/tests/temporal/zone-names/` | Time zones' names in English and German under the four `timeZoneName` styles that name a zone, and a few full formats | `zone-names.polyfill-e8cc03fc.txt`, the same polyfill on the same Node |

[`datasets.pin`](datasets.pin) holds each copy's length, SHA-256, Git blob id and Broiler.VM path, the Broiler.VM commit
it was copied from, and which three files make each program's dataset.

**Each line of a program is one operation and its answer**, written `<what> => <answer>`. Broiler.VM's slice compiler
runs each program in its own engine and holds every line to the retained answer with the divergences applied
(`IntlChecks.RetainedMatches`: the checks `intl/i2/...`, `intl/i3/...`, `intl/i4/...`, `temporal/t2/...`,
`temporal/t4/...` and `temporal/t5/...`). That expected answer is the one part C compares this engine with. Bundle
`sjl-9-001`'s [`divergence-groups.tsv`](../../../docs/evidence/sjl-9-001/divergence-groups.tsv) classes every group of
these divergences files, each file named by its blob, which is the one pinned here.

## How the copies are held

- **By their digests.** `datasets.pin` records each copy's SHA-256, from the bytes `git cat-file blob
  4cf9ca4:<path>` wrote in a Broiler.VM checkout, and each one's Git blob id, which `git -C <Broiler.VM> rev-parse
  4cf9ca4:<path>` prints. Broiler.VM's `01444a5`, whose objects `divergence-groups.tsv` read, and its `cd1928d`, the
  revision the correspondence ran, hold the same blobs. The harness refuses to run when a copy differs from its line,
  and `scripts/conformance/tests/test_reference_datasets.py` holds every copy to its line, and its blob id to its
  bytes, on every run of the conformance scripts' tests, and to Broiler.VM's tree where a Broiler.VM checkout is beside
  this repository (or named by `BROILER_VM`). By hand, in this directory:

  ```bash
  awk '$1 == "file" { print $4 "  " $2 }' datasets.pin | sha256sum -c
  ```

- **Without conversion.** The thirty-six copies are `-text` in [`.gitattributes`](../../../.gitattributes), so no
  end-of-line filter touches them on any platform. They have LF line endings; most are ASCII, and some hold UTF-8 text
  beyond it.
- **Never edited.** The rule names the Broiler.VM commit it takes its copies from. Another answer or program would be
  another copy from another named commit, and a new rule; a pin line is added, never changed.

## Running them

[`scripts/conformance/run_reference_datasets.py`](../../../scripts/conformance/run_reference_datasets.py) is the
harness the rule names, with its driver,
[`reference_dataset_driver.js`](../../../scripts/conformance/reference_dataset_driver.js), as for the time area's
datasets. `--bundle sjl-9-001` runs the twelve programs here, then `timezones.js` from `conformance/datasets/time/`,
each in its own directory, with `TZ=UTC` and with `LANG`, `LANGUAGE` and every `LC_` variable removed.
`--invariant-globalization`, part D's configuration, also sets `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` for the
shell; a run without it removes that variable from the shell's environment, and each run's outputs say which it was.

```bash
# Part C: one run, in the shell built in Release from a checkout (this one by default); --build builds it first:
python3 scripts/conformance/run_reference_datasets.py run --bundle sjl-9-001 --tree <checkout> --build --label base --out <dir>
# Part D: the same programs under invariant globalization:
python3 scripts/conformance/run_reference_datasets.py run --bundle sjl-9-001 --invariant-globalization --tree <checkout> --label base --out <dir>
# An after run against a base run of the same kind, line by line:
python3 scripts/conformance/run_reference_datasets.py compare --base <base dir> --after <after dir> --out <dir>
# The copies of both collections against their pins:
python3 scripts/conformance/run_reference_datasets.py verify
```

**The harness decides nothing.** It counts equal and differing lines. Part C's classes V, R, Z8, O and H are assigned
by a person, and its base run is read, not judged; part D's verdict reads the runs' completion and their lines.

## Licence

The twelve programs and their divergences files are Broiler.VM's, by the same contributors as this repository, under
the Apache License 2.0 (their SPDX headers say so; the licence text is [`LICENSE`](../../../LICENSE)). The answers are
what ICU 77.1, through Node, and the Temporal proposal's reference polyfill, through the same Node's ICU, printed for
those programs, one line per operation: they reproduce none of either's code or text, and no CLDR file.
[`THIRD_PARTY_NOTICES.md`](../../../THIRD_PARTY_NOTICES.md) records their sources. Nothing here ships: no project
builds or packs this directory.
