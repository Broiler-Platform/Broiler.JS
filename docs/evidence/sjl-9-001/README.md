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

**This engine's runs** of `Broiler.JS/Broiler.JavaScript.Vectors.Tests` are in [`engine/`](engine/). Each was built
once in Release with `--disable-build-servers`, in a scratch tree written by `git archive` of revision 27's commit,
and run with `--no-build` in one evaluating process, with no `LANG`, under `TZ=UTC` and under `TZ=America/New_York`.
The engine's code is the branch base's, `915dfc14`: no commit of SJL-9.2a changes a path under `Broiler.JS/`. The two
zones' reports are byte for byte the same.

| Revision | Vectors | Match | Classified divergences | Unclassified | Not implemented |
|---|---|---|---|---|---|
| 27 | 106,808 | 105,059 | 4 (the number area's, SJL-14's) | 1,745, all in the Intl area | 0 |

Each run's negative control changes `numbers/to-fixed/0001` in a copy of the corpus, and it is reported as the one
unclassified difference. The files are `run-revision-27-utc.log` and `.tsv.gz`, `run-revision-27-new-york.log` and
`.tsv.gz`, and `negative-control-revision-27-utc.tsv` and `-new-york.tsv`. Each log's first line records how the run
was taken.

**Broiler.VM** was run locally at its rule commit, `4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f`. Its conformance root
was built in Release with `--disable-build-servers` from a scratch archive of that commit, and run on a scratch archive
of revision 27 with a pin of that archive (`--vectors <archive> --expect <pin>`). It matches 106,765 of the 106,808
vectors. The other 43 differ, all in the Intl area. Its negative control changes `intl/numbers-and-text/decimal/0001`
in memory (`--mutate`), and it is reported as one more difference. The runs and the scratch pin are retained as
`engine/broiler-vm-*`, as the basis of those fields. They are not a reading of Broiler.VM's half: its own bundle judges
that, and takes its runs when its pin moves to a merged revision (the plan's task 2.4).

**Revision 28** classifies every difference under the correspondence's rows. The reasons are in
[`scripts/conformance/intl-divergences.txt`](../../../scripts/conformance/intl-divergences.txt). Each names the row it
confirms, the engine's answer as that row records it, the class JSD-0082 gives the row, and the work package that
answers it.

| Engine | Sub-area | Rows (vectors) | Vectors |
|---|---|---|---|
| Broiler.JS | (a) Locales | L26 (27), L24 (20), L9 with L8 (18), L45 with L2 (15), L21 (14), L7 (11), L29 (9), L19 with L20 (8), L5 (8), L30 (7), L15 (6), L18 with L17 (6), L12 (5), L14 (5), L17 (5), L19 to L21 (4), L2 (3), L25 (3), L6 (3), L8 (3), L10 (2), L37 (2), L40 (1) | 185 |
| | (b) Values and plurals | M1a (17), P5b (17), M5 (14), P6 (14), P2 (12), P1 (11), P3 (7), M3 (6), M7 (6), P5a (5), M2b (4), M4 (4), P7 (4), M1b (3), M6 with M1a (2) | 126 |
| | (c) Numbers and text | S8 (137), N4 (126), N2b (89), N6d (72), N5d (51), N3c (44), S1b (44), S4b (40), N1b (23), N6c (20), N5b (18), N3b (16), N6a with N6f (13), S9 (12), N9a (10), N9b (5), N2c (4), N8b (4), N10 (3), N6b (3), S5 (3), N7b (2), N8a with D21 (2), N9c (2), N9d (2), N11 (1), N13 with M5 (1), N14 with N1b (1), N17c (1), N5c (1), N6e (1), N9e (1) | 752 |
| | (d) Date-times | T30 with T31 (93), T16 with T14 (78), T17 (76), T22 (76), T16 (44), T14 with T15 (31), T16 with T12 (29), T12 (24), T10 (16), T35 (14), T35 with T36 (14), T36 (12), T39 (10), T34 (8), T13 (6), T19 (6), T23 (6), T24 (6), T2 (5), T29 (5), T18 (4), T33 (4), T14 (3), T20 (3), T27 (3), T26 (2), T37 (2), T4 (1), T7 (1) | 582 |
| | (e) Durations | U15 (47), U1 (29), U13 with U14 (6), U3 (6), U16 (5), U7 (4), U2 (2), U9 (1) | 100 |
| | **All** | | **1,745** |
| Broiler.VM | (a) Locales | L9 (18), L18 (5) | 23 |
| | (c) Numbers and text | N5b (2) | 2 |
| | (d) Date-times | Z8 (18) | 18 |
| | **All** | | **43** |

- **Broiler.JS, by class.** Corrections of Broiler.JS (C1): 470. Data policy (C5): 968. Both: 135 (L19 to L21; T14
  with T15 or T16; T35 with T36). Implementation-defined (C3): 78, whose vectors hold the answer the shared library takes. The interval patterns
  and the range separator (T30, C3, with T31, C5): 93. Engine policy: 1 (L40). SJL-9.5 answers every one but L40's.
  L40 is the Collator's collations, and each engine keeps its own Collator until SJL-17 (SJL-D-24 (c)), so SJL-17
  answers it. 929 of the 1,745 are table-family vectors of the 85 extra locales (decision D5).
- **Five readings tie a difference to the row that explains it.** L18 with L17: `getCollations` answers `["default"]`
  for every locale. L9 with L8: the subdivision aliases this engine's hand table lacks. N13 with M5: the significant
  digits compact notation sets. M6 with M1a: 1.35 rounded by its binary value. N8a with decision D21: a numbering
  system's own symbols. N13, M6 and N8a are rows the correspondence calls the same at the base, and their differences
  are those rows'.
- **The one abort.** `intl/locales/aliases/0072`, `Intl.getCanonicalLocales("en-t-sh")`, ends the evaluation with an
  `IndexOutOfRangeException`, rendered `abort:`. Row L10 records exactly that.
- **Eleven vectors gain a `per-engine.broiler-js` field.** They are `impl-defined` vectors of rows L17, L18 and L40,
  whose notes compare this engine with the reference. A divergence field on an `impl-defined` vector needs the
  engine's `per-engine` field beside it (SCHEMA.md section 5), so each gains one holding `out`. The generator writes it
  only where the vector's note says so.
- **Broiler.VM.** Z8, L9 and L18 are its three corrections (decisions D15 and D32), and SJL-9.4 answers them. N5b is
  implementation-defined: `de` `2 kb/Stone` where CLDR's composition, the shared library's, writes `2 kb/st`.
  SJL-9.4's adoption of the library answers it. 25 vectors differ in both engines (L9, L18 and N5b).

**What the vectors say of the area record.**
- **Every difference falls under a row the area verdict names, and none needs a new row.** So the vectors falsify no
  class of JSD-0082.
- **Every row with a behavioural difference is reached** by at least one vector. The generator's `check_rows` and its
  test check this.
- **51 of the 52 rows JSD-0082 calls corrections of Broiler.JS are confirmed** by at least one vector of this engine.
  The 52nd, L44, is a correction only under invariant globalization, which no vector run sets; part D holds it.
- **Broiler.VM's three corrections, Z8, L9 and L18, are each confirmed.**
- **Every vector of the other rows the verdict calls the same matches in both engines.**

## State

**2026-10-26, at SJL-9.1: the rule and its data only. Nothing here is a verdict, and nothing has been taken.**
- The bases of parts A and B are taken at the base commit, the first parent of the merge that lands SJL-9.5, by
  `test262 evidence` with `include_negative` true, in UTC and in `America/New_York`, promptly after that merge and
  together with the after runs (decision D4).
- Parts C and D run locally at the base commit and the after commit, and part C's programs run once in the reference
  `reference.txt` names.
- SJL-9.2 copies the datasets and adds the vectors; SJL-9.3 builds the library; SJL-9.5 is this engine's cut-over,
  one merge, after Broiler.VM's SJL-9.4 (decision D28).
- When SJL-9.2 and SJL-9.3 write `Broiler.ECMAScript/README.md`'s rows for the data home and the package, it gains
  its pointer to the home's design document, `docs/intl/design.md`, as it has one to `docs/frontend/design.md`. SJL-9.1
  leaves that file alone: it is outside the paths SJL-9.1 changes.

**The commits named so far:**
- **The rule commit** carries [`decision-rule.md`](decision-rule.md) and this README. It is named here, by all forty
  digits, when the change that carries it has merged.
- The data's own commits, above.

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
- `controls/`: each control a part or a home clause runs, with its patch, its output and its revert.

**It holds no figure until a run is taken**, and then only in the run's own files and this README's reading of them. A
test262 suite total is never quoted: parts A and B are decided per path, and totals decide nothing.

The owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", under which
JSD-0082 records its decisions, changes no rule: nothing is accepted without review, and SJL-9 may not meet its gate
before SJL-7 and SJL-8 meet theirs, and so not before SJL-3 meets SJL-3's.

Broiler.VM judges its own half in its own bundle of the same name. This bundle reads none of its runs.

*2026-10-26, at SJL-9.2a:* "This bundle reads none of its runs" holds for the rule's parts. `engine/broiler-vm-*` are
Broiler.VM's runs of the shared vectors that SJL-9.2a took as the basis of revision 28's `divergence.broiler-vm`
fields, as bundle `sjl-10-001` retains its own. They are not a reading of Broiler.VM's half.
