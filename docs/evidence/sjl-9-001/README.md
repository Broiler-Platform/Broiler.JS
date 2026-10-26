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

## The data home

SJL-9.2b built the data home, [`Broiler.ECMAScript/data/intl/`](../../../Broiler.ECMAScript/data/intl/README.md), on
2026-10-26, on branch `sjl-9-2b-home` (JSD-0082's decisions D5, D10 to D12 and D21). The data home's clauses 1 to 3 are
judged by the tests it commits, each with its control; this section records how its inputs were read and how its byte
identity with Broiler.VM was shown. It holds no size: the tables' sizes are in the change's pull request.

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
  tarball was retrieved from the npm registry twice, at 11:18:15 and 11:18:51 UTC; the two retrievals are
  byte-identical, each tarball's SHA-512 is the pin's `json-integrity` line for its package, and each of Broiler.VM's
  files from those packages equals its tarball member. The `timezone.json` entry and file equal the time home's.
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
  lacks; calendar type names for `aa`, `ab`, `en-Shaw`, `ff`, `gv`, `jbo`, `ln`, `pap` and `und`, and metazone names
  and GMT formats for those and `yi`. Broiler.VM's code assumed every locale has them; the generator writes none for
  these, and its output for Broiler.VM's list is unchanged. For `sr`, `sr-Cyrl` and `sr-Latn` the `hms` duration
  pattern is `h.mm.ss` while `timeSeparator` is `:`, and `en-DK`'s separator is `.` while the pattern it inherits is
  `h:mm:ss`: decision D34 reads the separator, so neither moves, and the tables carry both fields.

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
