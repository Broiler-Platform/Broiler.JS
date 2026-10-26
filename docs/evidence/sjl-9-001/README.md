# Evidence bundle SJL-9-001 (Broiler.JS) - locale data and the Intl algorithms

**Milestone:** SJL-9 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-9-locale-data-and-intl.md`: its Broiler.JS clause 9,
clause 8 under invariant globalization, and SJL-9.6's "the Intl reference datasets in both engines" for this engine.

**Rule:** [`decision-rule.md`](decision-rule.md), committed at SJL-9.1 before any code of the area:
- part A is test262 per path in UTC, with negative-metadata tests;
- part B is the same in `America/New_York`;
- part C is Broiler.VM's Intl reference datasets, run in this engine against Broiler.VM's answers. SJL-9.2 copies
  them into `conformance/datasets/intl/`; the time zones' program is in
  [`conformance/datasets/time/`](../../../conformance/datasets/time/README.md) since SJL-8.4;
- part D is part C's programs, and test262's `intl402/default-locale-is-supported.js`, under invariant
  globalization.

The data home's clauses 1 to 3 and the home clauses 4 to 6 are judged by their own mechanisms, the tests SJL-9.2 and
SJL-9.3 commit, each with its witness. Clauses 7 and 10 are read from the tree and from `Broiler.JS/CHANGELOG.md`. The
rule names them and does not decide them.

**Area verdict:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", "Locale data and the Intl
algorithms (SJL-9.1)", with Broiler.VM's JSD-0082,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0082-locale-data-and-the-intl-algorithms-move-to-the-shared-libraries.md`,
and its correspondence from code, retained in Broiler.VM's bundle of the same name
(`Broiler.VM/docs/evidence/sjl-9-001/correspondence.md`, with every probe and each engine's runs). The home's design
document is [`Broiler.ECMAScript/docs/intl/design.md`](../../../Broiler.ECMAScript/docs/intl/design.md).

**The pins.** ECMA-402 at JSD-0082 decision D14's pin, `tc39/ecma402` `3d4c558180f643d7dea75f011a6355fb285f377e`,
which already holds the Intl Locale Info proposal, with the Intl era and month code proposal added at its last commit on
or before 2026-05-08; SJL-9.2 records both in `conformance/vectors/sources/ecma402.pin`. Intl over Temporal follows
`tc39/proposal-temporal@e8cc03fc` (JSD-0078's decision D1). test262 `ccaac100`. The locale data is CLDR 48.2.0, which
the data home pins from SJL-9.2.

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

## The data committed with the rule

Tasks 1.2 and 1.3 of SJL-9.1 wrote them on 2026-10-20, on branch `sjl-9-1-data`, which this change merges unchanged:
`07604dcf3cd56640ee0446d4446d966413f7b886`, `4661031fad500d984be0abee5d1812e6cd9f0760`,
`ec36f18229b5ebf2b6ca95ae9569da2d50590078`, `c059d19404dce3829d6301ce6dfdfc102db656a9` and
`846538f6969b5512438a06b41f015bebef08e3b7`.
- [`divergence-groups.tsv`](divergence-groups.tsv): every group of Broiler.VM's divergences files for the Intl
  reference datasets, each file read from Broiler.VM `01444a590147b4bcdc033ea82c60d477924c752d`'s objects and named by
  its blob, with the group's lines, its header quoted whole, and its class for part C: `spec`, `coverage` or `zone`.
  Every divergence line falls in exactly one group. Part C's class V reads it with the corrections below, both as the
  rule commit holds them.
- [`reference.txt`](reference.txt): the reference part C and the Intl vectors name (decision D17), the newest Node.js
  release whose `process.versions.cldr` begins `48`, which is v26.12.0: its tarball's URL and SHA-256, retrieved twice
  with the same digest, and its `process.versions`. It answers CLDR 48.0, not 48.2.0, which part C's class R states.
- [`locales/`](locales/): every locale tag the passing `test/intl402` files and the reference datasets' programs name,
  with the file that names it (`intl402.tsv`, `datasets.tsv`), their union (`union.tsv`) and the literals not counted
  (`uncounted.tsv`), written by [`scripts/intl/enumerate_locales.py`](../../../scripts/intl/enumerate_locales.py), whose
  two runs over the same inputs write the same bytes. **The report they were enumerated from** is bundle
  [`sjl-10-001`](../sjl-10-001/README.md)'s after report, `test262/after/test262-results.json.gz` (run 39252310861 at
  `00b65dd0`), as each file's header says. Bundle [`sjl-12-001`](../sjl-12-001/README.md)'s `base/part-a/` report
  (run 40196127730 at `4d813f2a`), a later valid run, is the population the correspondence maps, and the two give the
  same verdict on every `test/intl402` path and variant (the correspondence's finding F17). Decision D5's lists read
  these files. The enumeration is run again before SJL-9.2 generates any table, on the newest valid UTC whole-suite
  report with negative tests of this repository's default branch at that time: SJL-9's base report is taken only at
  SJL-9.5, after its merge (decision D4), so it cannot be the one read.

The files are text, committed whole. `reference.txt` carries its own digests, and `divergence-groups.tsv` names each
source file by its blob, so the bundle keeps no `hashes.txt`.

## Corrections to the committed data, 2026-10-26

**`divergence-groups.tsv` keeps the bytes it was committed with.** The correspondence's findings F16 and F10 found
three of its rows stale, and JSD-0082 records their readings as the owner's of 2026-10-26 (finding F16, record only;
decisions D21 and D32). Part C's class V reads the table "with the dated corrections of that table in this bundle's
README, the table and the corrections both as the rule commit holds them": these, as the rule commit holds this
README. A correction added here after the rule commit is a record and does not bind part C; a change to how part C reads
the table is a revision of the rule, a new dated file (its section "Revisions"). A row is named by its line in the file.

- **Line 49**, the group of `src/tests/cldr/locales/divergences.txt` lines 37-60. The table classes it `coverage`, with
  the note "calendar: Broiler.VM formats the Gregorian calendar alone (JSD-0045)" and the header "The profile formats
  the Gregorian calendar alone (JSD-0045). ECMA-402 (15.5.9 CalendarsOfLocale) keeps of a region's calendars in use
  those AvailableCalendars has, so the profile answers gregory where Node, whose ICU formats them all, lists the
  region's others too." **Read its class as `spec`.** The note and the header are stale: Broiler.VM's DateTimeFormat has
  resolved every calendar `supportedValuesOf` lists since SJL-10.4, and both engines answer `["gregory"]` because
  neither holds CLDR's calendar preference data (row L16). This engine's line after its cut-over equals Broiler.VM's,
  and part C fails one that differs. Broiler.VM's header stays as committed through SJL-9's window, because Broiler.VM's
  rule admits no comment change in that file outside its classes Z8 and L18; JSD-0082 records it as stale.
- **Line 50**, the group of the same file's lines 62-92. The table classes it `coverage`, with the note "tailoring:
  Broiler.VM's Collator offers phonebk for German only (JSD-0043)" and the header "The profile's Collator offers
  phonebk for German and no other tailoring (JSD-0043). ECMA-402 (15.5.10 CollationsOfLocale) answers the collator's
  own collations for a locale it supports, and emoji and eor for one it does not, so the profile answers those where
  Node lists ICU's." **The class stays `coverage`**, for Broiler.VM's Collator data, `phonebk` for German alone and the
  languages `de` and `en`. The header's "emoji and eor for one it does not" is the misreading row L18 corrects: for a
  language the Collator does not support, CollationsOfLocale answers DefaultLocale's collations, and `emoji` and `eor`
  only for `und`. Broiler.VM rewrites those lines at SJL-9.4 (JSD-0082 decision D32), after this table's commit. Part
  C's population is Broiler.VM's datasets at the commit that carries Broiler.VM's rule, so the expected answers part C
  reads stay. This engine answers its own Collator's list, which keeps its host path until SJL-17 (decision D33 (c)),
  and class V admits its line when it equals the retained answer, or, for a `collations` line with no `co` keyword,
  the sorted list of the collations this engine's `Intl.Collator` resolves for the line's Locale (rows L17 and L40).
  The second reading is the one the `zh`, `zh-Hant`, `zh-TW`, `zh-Hans-SG` and `ko` lines need, the program's cases 20
  to 23 and 25: this engine's Collator resolves `phonetic` and `trad` for Chinese, which ICU's list lacks, and not
  `searchjl` for Korean, so after the cut-over those lines equal neither the retained answer nor Broiler.VM's
  `[emoji,eor]`.
- **Line 43**, the group of `src/tests/cldr/durations/divergences.txt` lines 205-219. **The class stays `coverage`.**
  Its note begins "a numbering system's own symbols, which Broiler.VM's data lacks and Broiler.JS's carries (decision
  D21)". "Broiler.JS's carries" is true only after SJL-9.5, under decision D21 as JSD-0082 revises it: at the base this
  engine writes Broiler.VM's line, the `latn` symbols with Arabic digits (rows N8a and U16). The note's requests to task
  1.5 are answered by class V as the rule files it:
  - class V names decision D21;
  - it admits a line of such a group equal to the expected answer with each of the locale's `latn` number symbols
    replaced by the numbering system's own CLDR 48.2.0 symbol, which covers the lines that also carry the draft's sign
    of the first group;
  - it admits a line its group's note names as carrying a difference the pinned text decides only when the answer is
    the line the note names, so the two `resolvedOptions` lines must equal Broiler.VM's.

Line 63's note, the `segments` group, asks task 1.5 for a reading, and the rule gives it: class O is read first for
every line of `segments.js`. Line 73's note, the deprecated Hijri calendars, asks for no reading; it names a change to
list, and decision D25 answers it: this engine's move from `gregory` to `islamic-tbla` is an implementation-defined
change (C3), row T33, listed in its changelog at SJL-9.5.

**The operation's name.** The headers of the files under `locales/`, and `scripts/intl/enumerate_locales.py`, name
ECMA-402's IsStructurallyValidLanguageTag, which the pinned text calls IsWellFormedLanguageTag. Records cite the pinned
name (decision D14 as revised). The files are kept as generated and the script as committed; the operation is the
same.

## The engine-level vectors, 2026-10-26 (SJL-9.2a)

Corpus revision 27, `6311031f99c62bb26bc22215e29323c278394605`, adds the Intl area: 3,496 vectors in
[`conformance/vectors/intl/`](../../../conformance/vectors/intl/), one file per sub-area of JSD-0082. Revision 28
classifies every difference of both engines' first runs on it. These runs are task 2.3 of the plan's SJL-9.2, the basis
of revision 28's `divergence` fields. They are no part of the rule's parts A to D, and nothing here is a verdict.
[`engine/commands.txt`](engine/commands.txt) records how each run was taken.

**The host.** Every run here was taken on one host: Ubuntu 24.04.5 LTS (Linux 6.18.44, x86_64, 4 logical CPUs) with
ICU 74.2, the package `libicu74` 74.2-1ubuntu3.1, the only ICU installed, which .NET's globalization loads. This
engine's Intl reads the host's ICU where it uses the host's data (rows N1b, N2b, N3c and T16 among others), so these
runs, and revision 28's classification of them, are of this host. No run here was taken on a host with another ICU.
The 11 differences that CI's hosts showed before the merge, where this engine's answer follows such a host's ICU, are
classified from CI's runs (the third dated note below).

**This engine's runs** of `Broiler.JS/Broiler.JavaScript.Vectors.Tests` are in [`engine/`](engine/). Each was built
once in Release with `--disable-build-servers`, in a scratch tree written by `git archive`, and run with `--no-build`
in one evaluating process, with no `LANG`, under `TZ=UTC` and under `TZ=America/New_York`. No commit of SJL-9.2a
changes a path under `Broiler.JS/`: the engine's code is the branch base's, `915dfc14`, in revision 27's runs, and the
integration base's, `8a95076c`, in revision 28's. The two zones' reports are byte for byte the same.

| Revision | Corpus commit | Vectors | Match | Classified divergences | Unclassified | Not implemented |
|---|---|---|---|---|---|---|
| 27 | `6311031f` | 106,808 | 105,059 | 4 (the number area's, SJL-14's) | 1,745, all in the Intl area | 0 |
| 28 | `0c8feb64` | 106,808 | 105,060, 11 of them divergences that no longer hold here | 1,748 (the Intl area's 1,744 and the number area's 4) | 0 | 0 |

Each run's negative control changes `numbers/to-fixed/0001` in a copy of the corpus, and it is reported as the one
unclassified difference. The files are `run-revision-<N>-utc.log` and `.tsv.gz`, `run-revision-<N>-new-york.log` and
`.tsv.gz`, and `negative-control-revision-<N>-utc.tsv` and `-new-york.tsv`, for N 27 and 28. Revision 27's logs are
as the runs wrote them; revision 28's were written whole by the run's script, with a header that names the commits,
the build, the host and the command, and a footer with the exit code. Revision 28's runs read the corpus as corrected
before the merge, at `0c8feb64db3ee7ebaa62bbc263340cb5fea0ec86`, in a scratch tree written by `git archive` of that
commit, whose engine is the integration base's, `8a95076c` (it differs from `915dfc14`'s only by compiler changes).
Every vector renders the value it rendered at revision 27. The 11 divergences classified from CI's hosts (the third
dated note below) report that they no longer hold here, and every other row is the one the runs on `2f240fec` gave.

**Broiler.VM** was run locally at its rule commit, `4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f`. Its conformance root
was built in Release with `--disable-build-servers` from a scratch archive of that commit, and run on a scratch archive
of revision 27 with a pin of that archive (`--vectors <archive> --expect <pin>`). It matches 106,765 of the 106,808
vectors. The other 43 differ, all in the Intl area. On revision 28, read the same way from a scratch archive of
`2f240fec` and its pin, it matches 106,767 and classifies 41, with no unclassified difference and no divergence that no
longer holds: the two vectors of row N5b it differed on now expect its answer (below). Each negative control changes
`intl/numbers-and-text/decimal/0001` in memory (`--mutate`), and it is reported as one more, unclassified, difference.
The runs and the scratch pins are retained as `engine/broiler-vm-*`, as the basis of those fields; revision 28's logs
begin with a header that names Broiler.VM's commit, its build, the pin's SHA-256, the host and the command, and end
with the exit code and the time. They are not a reading of Broiler.VM's half: its own bundle judges that, and takes its
runs when its pin moves to a merged revision (the plan's task 2.4).

**Revision 28** classifies every difference under the correspondence's rows: those of the runs here, and the 11 that
CI's hosts showed before the merge (the third dated note below). The reasons are in
[`scripts/conformance/intl-divergences.txt`](../../../scripts/conformance/intl-divergences.txt). Each names the row
whose answer the engine gives in the difference, one of the rows the vector's note names, the engine's answer as that
row records it, the class JSD-0082 gives the row, and the work package that answers it.

| Engine | Sub-area | Rows (vectors) | Vectors |
|---|---|---|---|
| Broiler.JS | (a) Locales | L26 (27), L24 (20), L9 with L8 (18), L45 with L2 (15), L21 (14), L7 (11), L29 (9), L19 with L20 (8), L5 (8), L30 (7), L15 (6), L18 with L17 (6), L12 (5), L14 (5), L17 (5), L19 to L21 (4), L2 (3), L25 (3), L6 (3), L8 (3), L10 (2), L37 (2) | 184 |
| | (b) Values and plurals | M1a (17), P5b (17), M5 (14), P6 (14), P2 (12), P1 (11), P3 (10), M3 (6), M7 (6), P5a (5), M2b (4), M4 (4), P7 (4), M1b (3), M6 with M1a (2) | 129 |
| | (c) Numbers and text | S8 (137), N4 (126), M5 (83), N6d (74), N5d (51), N3c (45), S1b (44), S4b (40), N1b (23), N6c (20), N5b (18), N3b (16), N6a with N6f (13), S9 (12), N9a (10), N9b (5), N2b (4), N2c (4), N8b (4), N10 (3), N6b (3), S5 (3), N2b with M5 (2), N7b (2), N8a with D21 (2), N9c (2), N9d (2), N11 (1), N13 with M5 (1), N14 with N1b (1), N17c (1), N5c (1), N6e (1), N9e (1) | 755 |
| | (d) Date-times | T30 with T31 (93), T22 (78), T16 with T14 (76), T20 (65), T16 (62), T10 (32), T14 with T15 (31), T12 (29), T35 (14), T35 with T36 (14), T36 (12), T39 (10), T34 (8), T13 (6), T19 (6), T23 (6), T24 (6), T14 (5), T2 (5), T29 (5), T18 (4), T33 (4), T16 with T12 (3), T17 (3), T27 (3), T26 (2), T37 (2), T4 (1), T7 (1) | 586 |
| | (e) Durations | U15 (48), U1 (29), U13 with U14 (6), U3 (6), U16 (5), U7 (4), U2 (2), U9 (1) | 101 |
| | **All** | | **1,755** |
| Broiler.VM | (a) Locales | L9 (18), L18 (5) | 23 |
| | (d) Date-times | Z8 (18) | 18 |
| | **All** | | **41** |

- **Broiler.JS, by class.** Corrections of Broiler.JS (C1): 555. Data policy (C5): 832. Both: 135 (L19 to L21; T14
  with T15 or T16; T35 with T36; N2b with M5). Implementation-defined (C3): 140, whose vectors hold the answer the
  shared library takes. The interval patterns and the range separator (T30, C3, with T31, C5): 93. SJL-9.5 answers
  every one. 940 of the 1,755 are table-family vectors of the 85 extra locales (decision D5).
- **A table-family vector is classified by what differs in it**, not by every row its family reaches, and its note
  names the family's rows with their classes. Of the percent vectors, 83 differ only in percent's default fraction
  digits, `"25.6%"` for `"26%"` (M5), and 2 in the pattern as well (N2b with M5). Of the day-period vectors, the 62 of
  a locale whose default cycle is a 24-hour one, whose expectation drops the period (`"15"`, `"15 Uhr"`), are T20; the
  11 others are T16, 5 of them with T14 (English names). Of the time patterns, 14 are the hour cycle (T10), 5 U+202F
  alone (T12), 2 day-period names (T14), 5 the separator (T16) and 3 T16 with T12. Of the date patterns, the 7 of
  English locales are T16 alone, the others T16 with T14.
- **Five readings tie a difference to the row that explains it.** L18 with L17: `getCollations` answers `["default"]`
  for every locale. L9 with L8: the subdivision aliases this engine's hand table lacks. N13 with M5: the significant
  digits compact notation sets. M6 with M1a: 1.35 rounded by its binary value. N8a with decision D21: a numbering
  system's own symbols. N13, M6 and N8a are rows the correspondence calls the same at the base; their vectors' notes
  name the second row as well.
- **The one abort.** `intl/locales/aliases/0072`, `Intl.getCanonicalLocales("en-t-sh")`, ends the evaluation with an
  `IndexOutOfRangeException`, rendered `abort:`. Row L10 records exactly that.
- **This engine's collations.** The `per-engine.broiler-js` expectation of the `impl-defined` vectors of rows L17, L18
  and L40 is the list this engine's own `Intl.Collator` resolves, which its adapter hands CollationsOfLocale (decision
  D33 (c)) and `Intl.supportedValuesOf("collation")` lists (row L40, engine policy): `emoji` and `eor` for every
  locale, with `phonebk` for `de`, `unihan` for `ja` and `ko`, `trad` for `hu`, and `phonetic`, `pinyin`, `stroke`,
  `trad`, `unihan` and `zhuyin` for `zh`. A probe of the built shell, each value of `supportedValuesOf("collation")`
  that `new Intl.Collator(tag, {collation})` resolves, gives the same lists. This engine answers L40's list, so L40 has
  no divergence; its `getCollations` answers `["default"]`, which L17 and L18 classify.
- **Broiler.VM.** Z8, L9 and L18 are its three corrections (decisions D15 and D32), and SJL-9.4 answers them. 23
  vectors differ in both engines (L9 and L18). Row N5b is implementation-defined, and every implementation-defined row
  keeps Broiler.VM's answer under its rule, which admits no C3 (decision D35). Its run differed from ICU on `de`
  `kilobit-per-stone`, short and narrow: `2 kb/Stone`, the denominator's long singular where the short width has no
  one-form, for ICU's `2 kb/st`. So both engines' expectations of row N5b are Broiler.VM's composition, which the
  generator reads from CLDR 48.2.0, and this engine's N5b divergence stays.

**What the vectors say of the area record.**
- **Every difference falls under a row the area verdict names, and none needs a new row.** So the vectors falsify no
  class of JSD-0082. Both engines' runs on revision 28 leave no difference unclassified.
- **Every row with a behavioural difference is reached** by at least one vector. The generator's `check_rows` and its
  test check this.
- **51 of the 52 rows JSD-0082 calls corrections of Broiler.JS are confirmed** by at least one vector of this engine.
  The 52nd, L44, is a correction only under invariant globalization, which no vector run sets; part D holds it.
- **Broiler.VM's three corrections, Z8, L9 and L18, are each confirmed.**
- **Every vector of the other rows the verdict calls the same matches in both engines.**

*2026-10-26, at SJL-9.2a, before the merge:* review found that revision 28 as first committed (`2cc85342`, its runs
retained at `11f1cf26`) expected ICU's `2 kb/st` of Broiler.VM on row N5b, where decision D35 keeps Broiler.VM's answer;
compared this engine's collations with ICU's list, where decision D33 (c) reads this engine's own Collator; and
classified each table-family difference by the family's row, not by what differs. Revision 28 was corrected in
`3f9d5953` (and its row 28 names the host in `2f240fec`), and both engines' runs on it replace the first ones in
`engine/`. Revision 27's two logs of this engine were first committed with a first line ("== start 2026-10-26T12:47Z
TZ=UTC at 6311031f… (a scratch tree written by git archive …)") and a last line ("== end exit=1") that the run did not
write, added afterwards from the run's records; they are now as the run wrote them, and `engine/commands.txt` records
how the run was taken.

*2026-10-26, at SJL-9.2a, before the merge:* the shared libraries' Native AOT probe found that two vectors of row L23,
`intl/locales/values/0003` and `0004`, named the op `Intl.Locale.prototype.getTimeZones`, which the time-zones area's
vectors name for the zones joined. The library-level runner answers a vector by its op and inputs, so it answered them
in that rendering where the probe requires "not implemented". Revision 28 was corrected in place: they now name
`Intl.Locale.prototype.getTimeZones, JSON` and `Intl.Locale.prototype.getTimeZones, typeof`, and nothing else in the
corpus changed. An engine-level run evaluates a vector's `script`, not its `op`, so the runs on `2f240fec` in `engine/`
are runs of the corrected revision as well.

*2026-10-26, at SJL-9.2a, before the merge:* CI's run 40400894567 of the integration of SJL-9.2a and SJL-9.2b
(Broiler-Platform/Broiler.JS#1080, at `7b75e237`) failed in both `engine vectors` jobs, each on differences no run here
showed, every one where this engine's answer follows the host's ICU and not the pinned CLDR:
- **`engine vectors (windows-latest)`, job 121846364314**: Windows Server 2025 (10.0.26100), image
  `windows-2025-vs2026` 20261021.278.1, whose ICU is the OS's `icu.dll`. Neither the job's log nor the image's
  software list states that ICU's version. Ten vectors, all table-family vectors of `aa` and `pap`: rows T10 (2, `h23`
  for `h12`), T22 (2, `"GMT-05:00"` for `"Eastern Standard Time"`), U15 (1, `"1 hours, 2 minutes, 3 seconds"`), N6d
  (2, `"1,234,567"` for `"1.2M"`) and P3 (3, `["other"]` alone). They are exactly the ten that a scratch run of this
  engine on this host with Microsoft's ICU 72.1.0.3 loaded app-locally, a stand-in for Windows' `icu.dll`, had
  predicted before the push.
- **`engine vectors (ubuntu-latest)`, job 121846364071**: Ubuntu 26.04.1, image `ubuntu-26.04` 20261012.163.1, whose
  software list names `libicu-dev` 78.2-2ubuntu1, so ICU 78.2; the job's log names no ICU. One vector, `en-FR`'s
  currency pattern, row N3c: `"-€1 234,50"` (U+202F) for `"-€1,234.50"`.

On the owner's decision, taken as recommended under the direction of 2026-10-18, revision 28 classifies these 11 in
place, before the merge, as revision 15 classified the differences only the New York run showed: each as data policy
(C5) under the row the vector's note names, answered by SJL-9.5. Their reasons in `intl-divergences.txt` name the
host, the run and the job, and that the vector matches on this host. Each of the 11 matches here, where a run reports
its divergence as no longer holding, which the runner does not fail on. "The host" above read "No run here was taken
on a host with another ICU, such as CI's `engine vectors (windows-latest)` job has, and a difference only such a host
shows is not classified here."; it now says that these 11 are classified from CI's runs. The counts of the table
above include them; before this note they read P3 (7), N6d (72), N3c (44), T22 (76), T10 (30) and U15 (47), sub-area
totals of 126, 752, 582 and 100 and 1,744 in all, data policy (C5) 821, and 929 table-family vectors. The CI hosts'
reports differ from this host's in no other vector but classified ones, which the runner does not fail on: Windows
renders 3 of them otherwise and reports 1 as no longer holding; Ubuntu 26.04 renders 18 of them otherwise and reports
10 as no longer holding. Broiler.VM's runs here are unaffected: its conformance root is built with invariant
globalization, its Intl reads only its own CLDR 48.2.0 tables, and the 11 gain only `divergence.broiler-js` fields.
This engine's runs on revision 28 were taken again on the corrected revision, at `0c8feb64`, under both zones: no
difference is unclassified, the 11 report that they no longer hold, and every other row and both negative controls are
those of the runs on `2f240fec` they replace (in `engine/` at `d113a7ce`). The paragraph on revision 28's runs above
read "Revision 28's runs read the corpus at its commit, `2f240fecc502af7d843077ec664b1b7a65c45bb1`, in a scratch tree
of the same kind, whose `conformance/vectors` was replaced by that commit's; the engine's code is unchanged. Every
vector renders the value it rendered at revision 27, and no divergence field reports that it no longer holds.", and
the table's row 28 named the corpus commit `2f240fec` with 105,060 matches; "This engine's runs" read "The engine's
code is the branch base's, `915dfc14`: no commit of SJL-9.2a changes a path under `Broiler.JS/`."

## The data home

SJL-9.2b built the data home, [`Broiler.ECMAScript/data/intl/`](../../../Broiler.ECMAScript/data/intl/README.md), on
2026-10-26, on branch `sjl-9-2b-home` (JSD-0082's decisions D5, D10 to D12 and D21). The data home's clauses 1 to 3 are
judged by the tests it commits, each with its control; this section records how its inputs were read and how its byte
identity with Broiler.VM was shown. The tables' sizes and the archives' totals are in [`home/sizes.txt`](home/sizes.txt),
as JSD-0082's decision D5 says: "Sizes are measured by the generator at SJL-9.2 and stated in Broiler.JS's bundle,
which has no budget rule." No other record states them.

- **The locale enumeration, run again** (decision D5; "The data committed with the rule" above). Before any table was
  generated, `scripts/intl/enumerate_locales.py` ran over bundle [`sjl-12-001`](../sjl-12-001/README.md)'s
  `after/part-a/` report (run 40231802611 at `c29d653f`), the newest valid UTC whole-suite report with negative tests
  of the default branch, with Broiler.VM's datasets at `4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f`. Every row of
  `intl402.tsv`, `datasets.tsv`, `union.tsv` and `uncounted.tsv` equals the committed file's; only the header lines
  naming the report differ. Two runs wrote the same bytes. The committed files stand, and the home's
  `locales/broiler-js.txt` reads them as decision D5 says.
- **The CLDR pin** (decision D10). `cldr.pin`'s first block is Broiler.VM's `src/tests/cldr/pins/cldr.pin` at
  `4cf9ca44`, from its `data` line to its last `file` line, byte for byte, and each file it names is a copy of
  Broiler.VM's archived file; the generator's tests hold the block to its SHA-256 at that commit. The second block
  names the main-data files Broiler.JS's list needs from the same sixteen `cldr-json` 48.2.0 packages. Each package's
  tarball was retrieved from the npm registry twice on 2026-10-26; the two retrievals are byte-identical, each
  tarball's SHA-512 is the pin's `json-integrity` line for its package, and each of Broiler.VM's files from those
  packages equals its tarball member. Broiler.VM's 33 other npm files come from `cldr-core` (17) and `cldr-bcp47` (16),
  which the extension does not read: those two tarballs were retrieved twice on 2026-10-26 for this comparison alone,
  each pair byte-identical and each SHA-512 the package's `json-integrity` line, and each of the 33 files equals its
  member. So every npm file of Broiler.VM's block equals its member of a tarball its integrity line holds. The third
  block names the `common/main` XML files decision D21 reads (below). The `timezone.json` entry and file equal the time
  home's.
- **The UCD copies** (decision D12). `UnicodeData.txt`, `PropList.txt` and the licence are Broiler.VM's copies at
  `4cf9ca44`, and each `file` line of `ucd.pin` equals the line Broiler.Unicode's pin gives the same file, which the
  generator's tests read through `UnicodePin.Owner()`.
- **Byte identity with Broiler.VM** (decision D11). Broiler.VM's committed `JsCldrTables.g.cs` at `4cf9ca44` has the
  SHA-256 `f8f8b1228edb3de1a3174434362b8f3b657938e8cd29189bec110fb1584fae20`; its assurance generator adds a header
  before `using System;` and fills each `Fingerprint=` value. Three readings give the same SHA-256,
  `839bdc22afca187b58252f2487cd31c4b613396cd6596ffdcb6622fe1b534561`:
  1. Broiler.VM's own `CldrTableGenerator`, `UnicodeDatabase` and `CldrPin`, built from its tests at `4cf9ca44` in a
     scratch project outside both repositories and run twice over Broiler.VM's pinned files;
  2. the committed file from `using System;` on, each fingerprint put back to `TBF`;
  3. this home's `IntlTableSource.Write` with Broiler.VM's options and locale list, over the home's copies of
     Broiler.VM's files and the UCD copies, which `IntlTableSourceTests` holds against that digest, with its control
     (one changed byte, one changed summary), and over two runs and two threads.
- **Broiler.JS's tables**, `generated/`: one file per table and `tables.manifest`, written with Broiler.JS's list and
  every numbering system's symbols (decision D21). Rule SL7 regenerates them and compares them byte for byte, its
  witness refused; `IntlCompletenessTests` holds exit clause 3 for both lists, with decision D34's keys for every locale
  of Broiler.JS's list and the values D34 and D21 name (two-digit hours for `af` and `lt`; `.` for `da`, `en-DK`, `id`
  and `si`; root's `arab` symbols under `und`; `ko`'s, `sv`'s, `ur`'s and `nb`'s own `arab` symbols).
- **Decision D21's symbols, read from CLDR's XML.** A `cldr-json` `numbers.json` carries the symbols of a locale's
  default, native, traditional and finance numbering systems only, and decision D21 asks for each system's own symbols
  through the locale's parent chain to root. CLDR 48.2.0's `common/main` XML states `arab` or `arabext` symbols, and for
  `sv` a `fullwide` clock separator, for 14 locales of the list whose `numbers.json` leaves those systems out: `ar`,
  `ar-EG`, `az`, `fa`, `fr`, `fr-CA`, `ko`, `nb`, `no`, `sv`, `ur`, `uz`, `uz-Cyrl` and `zh-Hant`. Read with root's
  symbols alone, the table gave 59 of their values as root's or as the `latn` ones. The pin's third block archives the
  XML files of the list's 67 main-data locales other than `und` and of `en-001`, the one locale on their parent chains
  outside the list: 68 files, retrieved twice on 2026-10-26 from the CLDR repository at the tag `release-48-2` into two
  directories found byte-identical, with the `root.xml` retrieved beside them equal to Broiler.VM's. The generator
  writes each locale's own values as `symbols@<system>.<name>` lines, the first value up its chain below root, and
  `IntlCompletenessTests` holds every symbol of every main-data locale, read as a formatter reads the table (the
  locale's line, then `und`'s, then its `latn` symbol), to CLDR's XML resolution, with a removed line as its control;
  the same test finds every system `numbers.json` carries equal to the XML's resolution. The reading takes values at
  the `contributed` level `cldr-json` carries: `fr.xml`'s `arab` and `arabext` minus signs, `arab` percent sign,
  `arabext` plus sign and both approximately signs are draft `unconfirmed`, so `fr` and `fr-CA` keep root's for those
  (12 values), as `ab.xml`'s unconfirmed `latn` decimal separator `,` is absent from `ab/numbers.json` and as the
  reference Node 22 (ICU 77.1) writes `fr-u-nu-arab`'s minus sign as root's, U+061C U+002D. The reference writes
  `ko-u-nu-arab`'s minus sign as U+200F U+002D and `sv-u-nu-arab`'s as U+061C U+2212 with U+00A0 between groups, as the
  table now does. **Two readings depart from JSD-0082's text**, for the owner to confirm: decision D21 names only
  `root.xml` beside `cldr-json` ("the generator reads root's from that file"), and the other locales' XML files are
  this home's reading of its "through the locale's parent chain to root"; and decision D10 describes the superset's
  extra files as "from the same packages", while these come from the CLDR repository at the tag Broiler.VM's own
  `root.xml` came from, which Broiler.VM's `repository-source` line names. Each of Broiler.VM's entries is unchanged.
- **What CLDR 48.2.0 does not carry**, read, not decided: 34 per-locale `languages.json`, `scripts.json`,
  `territories.json` or `variants.json` files of `cldr-localenames-full` for locales of Broiler.JS's list, which the
  home's [`cldr/README.md`](../../../Broiler.ECMAScript/data/intl/cldr/README.md) lists and the display-name table
  lacks; calendar type names for `aa`, `ab`, `en-Shaw`, `ff`, `gv`, `jbo`, `ln`, `pap` and `und`, and metazone names,
  the GMT zone's names among them, for those and `yi` (their GMT formats are there). Broiler.VM's code assumed every
  locale has them; the generator writes none for
  these, and its output for Broiler.VM's list is unchanged. For `sr`, `sr-Cyrl` and `sr-Latn` the `hms` duration
  pattern is `h.mm.ss` while `timeSeparator` is `:`, and `en-DK`'s separator is `.` while the pattern it inherits is
  `h:mm:ss`: decision D34 reads the separator, so neither moves, and the tables carry both fields.
- **Decision D13's table**, `Broiler.JS/Broiler.JavaScript.BuiltIns/Intl/JSIntlDefaultNumberingSystems.g.cs`, which
  the shared data absorbs at SJL-9.5, names a default numbering system for 54 locales. Only `ar-EG`, `fa` and `sa` are
  on decision D5's list, so the shared tables carry no default for the other 51 (`bn`, `mr`, `ne`, `my` and the
  regional `ar-*` among them): a reading SJL-9.3 and SJL-9.5 take up, which the plan's dated note names.

## State

**2026-10-26, at SJL-9.1: the rule and its data only. Nothing here is a verdict, and nothing has been taken.**
- The bases of parts A and B are taken at the base commit, the first parent of the merge that lands SJL-9.5, by
  `test262 evidence` with `include_negative` true, in UTC and in `America/New_York`, promptly after that merge and
  together with the after runs (decision D4).
- Parts C and D run locally at the base commit and the after commit, and part C's programs run once in the reference
  `reference.txt` names.
- SJL-9.2 copies the datasets and adds the vectors; SJL-9.3 builds the library; SJL-9.5 is this engine's cut-over,
  one merge, after Broiler.VM's SJL-9.4 (decision D28).
- `Broiler.ECMAScript/README.md` has its pointer to the home's design document, `docs/intl/design.md`, beside its
  pointer to `docs/frontend/design.md`: SJL-9.2b added it with the data home's row. SJL-9.3 writes the package's row.
  *(Revised 2026-10-26, at SJL-9.2b; the item read "When SJL-9.2 and SJL-9.3 write `Broiler.ECMAScript/README.md`'s
  rows for the data home and the package, it gains its pointer to the home's design document, `docs/intl/design.md`,
  as it has one to `docs/frontend/design.md`. SJL-9.1 leaves that file alone: it is outside the paths SJL-9.1
  changes.")*

**2026-10-26, at SJL-9.2b: the data home is built**, on branch `sjl-9-2b-home` ("The data home" above), with
`home/sizes.txt` beside it. Its clauses are judged by the tests it commits; nothing here is a verdict, and no run of
parts A to D has been taken.

**The commits named so far:**
- **The rule commit** carries [`decision-rule.md`](decision-rule.md) and this README. It is named here, by all forty
  digits, when the change that carries it has merged.
- The data's own commits, above.
- SJL-9.2b's commits, on branch `sjl-9-2b-home`, are named here by all forty digits when the change that carries them
  has merged. *(Added 2026-10-26, at SJL-9.2b.)*

No other commit is named yet.

*2026-10-26, at SJL-9.2a:* the sentence above, "No other commit is named yet.", held until SJL-9.2a. The corpus
revisions' commits and the commits the engine-level runs were taken at are named in section "The engine-level vectors".
Those runs are in `engine/`, which the list below did not foresee. They are no part's runs.

**What the bundle will hold:**
- `base/`: parts A and B's base runs, each imported unchanged with the digest of its zip;
- `after/`: parts A and B's after runs, each comparison's report, the classification file, and the table of every C1,
  C3 and C5 row with its row of the correspondence;
- `datasets/`: part C's runs at both commits, the reference's run, and the table of every V, R, Z8, O and H line;
- `invariant/`: part D's runs at both commits, `default-locale-is-supported.js` among them;
- `algorithms/`: the summary of SJL-9.3's differential check against Broiler.VM's code (decision D22);
- `controls/`: each control a part or a home clause runs, with its patch, its output and its revert;
- `home/`: the data home's sizes, `sizes.txt`, as decision D5 states them (added 2026-10-26, at SJL-9.2b).

**It holds no figure of a run until that run is taken**, and then only in the run's own files and this README's
reading of them. The data home's figures are those of its inputs and tables, which decision D5 asks this bundle to
state: `home/sizes.txt`, and the counts "The data home" above quotes from the home's files. A test262 suite total is
never quoted: parts A and B are decided per path, and totals decide nothing. *(Revised 2026-10-26, at SJL-9.2b, when
the data home's figures came in; the paragraph began "**It holds no figure until a run is taken**, and then only in
the run's own files and this README's reading of them.")*

The owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", under which
JSD-0082 records its decisions, changes no rule: nothing is accepted without review, and SJL-9 may not meet its gate
before SJL-7 and SJL-8 meet theirs, and so not before SJL-3 meets SJL-3's.

Broiler.VM judges its own half in its own bundle of the same name. This bundle reads none of its runs.

*2026-10-26, at SJL-9.2a:* "This bundle reads none of its runs" holds for the rule's parts. `engine/broiler-vm-*` are
Broiler.VM's runs of the shared vectors that SJL-9.2a took as the basis of revision 28's `divergence.broiler-vm`
fields, as bundle `sjl-10-001` retains its own. They are not a reading of Broiler.VM's half.
