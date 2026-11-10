# Bundle SJL-9-001's decision rule, revised again before its judged runs: one line of part C this engine answers from CLDR's time data under row L19, by the owner's ruling "Dated part C revision"

**Written:** 2026-11-10, at the owner's decision of 2026-11-10, **after** SJL-9.5's stage 4 local pre-check of parts C and
D had been seen, and **before** any judged run of any part was taken: no base run and no after run that
[`decision-rule.md`](decision-rule.md) names exists. The rule's own "Revisions" paragraph makes this weaker evidence than the
rule itself, and it is said here. It was written at the owner's direction: the owner ruled on 2026-11-10, **"Dated part C
revision"**, that this engine's answer to one line of part C, `locales:128` ("11 fr-CA hourCycles"), which follows CLDR
48.2.0's time data under row L19 since the owner's ruling of 2026-11-09 "Fix both consumers"
(Broiler-Platform/Broiler.JS#1108), be admitted by name, in a dated revision filed on the default branch before any judged
run, as the ruling of 2026-11-09 on `numbering-system.js` was. The alternative not taken was **"Keep old answer"**.
**Owner:** the Broiler.JS maintainer. **Reviewer:** none; every role is one person (the programme's EX-30), and this revision
does not claim that anyone independent has read it. Nothing in Broiler.VM's rule changes here.

**The rule revised** is `docs/evidence/sjl-9-001/decision-rule.md` as blob
`34dfc4f7dda6dca4b23bb971ff0aeefdba537b9e`, the file the rule commit (`50978882`, its last change `117a6f7d`) and this
revision's base (`b9ae4b5a`, the merge of Broiler-Platform/Broiler.JS#1108) both hold. **The earlier revision**,
[`decision-rule-revised-2026-11-09.md`](decision-rule-revised-2026-11-09.md), is blob
`ccb8be49771e8afd68dbe4c07278df2a1617e0fd`, the file its last change (`ef013613`) and this revision's base both hold. It
revises parts A and B and says "Part C is not touched by this file"; that stays true of that file, and its class L, parts A
and B as it restates them, and its reading stand. Neither file is rewritten, nor
[`divergence-groups.tsv`](divergence-groups.tsv) (blob `e53fab575808e1cc467c0be467e1d81ccb13bf82`) or the dated corrections
of that table in the bundle's README; this file is read beside them.

## What was seen, and where

**The pre-check is not evidence.** SJL-9.5's stage 4 ran parts C and D locally, as a check before its judged runs, on branch
`sjl-9-5-cutover`, which has not merged: at its commits `28cfaa3081f2b98dab7d6d61ad236897121217d3` (its merge of the default
branch at `b9ae4b5a`), `ddd970e135e93a25f4662a6053179746275cdac4` and `21bed66c2d17e3f9b453bef6e516bd8a62ca0d27`, read
against its stage 3's head `1f99fa5dee93b086f4e7762f41b80699e05adbac` and its stage 1's base answers at
`6f7284daa2d2fbec3104a9498d5f7c989f956946`. None of those commits is the base commit or the after commit, no part reads
those runs, and this bundle does not retain them. They are named here only as what was seen.

1. **The line.** Part C's `locales` program is `conformance/datasets/intl/locales.js`, with Broiler.VM's retained answer
   `locales.icu-77.1.txt` (blob `eb4c3125c3b814e939e299b0025975d598f1a76a`) and Broiler.VM's divergences
   `locales.divergences.txt` (blob `d3d115d90467f17345f396e86ed5a3dc0e753c15`), the copies
   `conformance/datasets/intl/datasets.pin` pins from Broiler.VM's `4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f`, the commit
   that carries Broiler.VM's own bundle sjl-9-001 rule. A line of the program is a line of its answer, the text it prints, as
   the rule reads "a line of segments.js"; `locales.js` itself has 78 lines. Line 128 of the program's answer, the harness's
   `locales:128`, is the line whose key is `11 fr-CA hourCycles`; `locales.js` prints it at its line 68. The retained
   answer's line 128 is ICU 77.1's `11 fr-CA hourCycles => [h23]`, and the divergences file's line 115 replaces it with
   Broiler.VM's `11 fr-CA hourCycles => [h12,h23]`, in the group the table's line 52 marks `spec`, "ECMA-402's hour cycles
   from CLDR's time data" (Broiler.VM's `src/tests/cldr/locales/divergences.txt`, lines 102-161). So the line's expected
   answer is `11 fr-CA hourCycles => [h12,h23]`.
2. **What this engine answered in the pre-check.** `[h12]` at stage 1's base; `[h12,h23]` at stage 3's head, the only one of
   these commits where it equalled the expected answer; and `[h23,h12]` from stage 4's merge of the default branch on, at each
   of the three stage 4 commits. Part D's runs of the same program answered the same, with no `abort:` line. No other line of
   part C or part D moved between stage 3's head and stage 4's commits.
3. **Why it moved.** Row L19 of the correspondence (Broiler.VM's `docs/evidence/sjl-9-001/correspondence.md`), a
   data-policy row (C5) in JSD-0082's sub-area (a): `getHourCycles` with no keyword answers from CLDR 48.2.0's time data.
   Since the owner's ruling of 2026-11-09 "Fix both consumers", which Broiler-Platform/Broiler.JS#1108 merged at
   `b9ae4b5a`, the shared library's `LocaleInfo.HourCycles` reads the time data for the locale's language and region before
   the region's alone, where it read the region's alone. CLDR 48.2.0's `supplemental/timeData.json` has an entry for
   `fr-CA`, preferred `H` and allowed `H h hB`, which the data home writes as `fr-CA|H h hB|H`
   (`Broiler.ECMAScript/data/intl/generated/time-data.txt`, line 263); the region's entry, `CA|h hb H hB|h` (line 39), is
   the one that gave `[h12,h23]`. So `[h23,h12]` is the pinned data's own answer: `h23` preferred, then `h12`.
4. **Why the rule as written fails it.** Part C says:

   > every other difference is a failure, including a line in a spec group of divergence-groups.tsv that differs from Broiler.VM's divergence (other than a line of segments.js that class O admits), a line of V's or R's kind that no reading of class V admits and that does not equal the reference's line, and a line that differs from its expected answer for any other reason

   > MET if and only if every program completes after with every line, and every line that differs from its expected answer is in V, R, Z8, O or H and is listed.

   Classes V and R read only lines of a group the table marks coverage, Z8 only a formatter's resolved `timeZone`, O only
   lines of `segments.js`, and H only a value the default-locale provider supplies. The line is in a `spec` group and differs
   from Broiler.VM's divergence, so under the rule as written it is a failure, and part C is NOT MET however the rest of its
   run reads.
5. **Part D does not judge the line.** Part D says:

   > MET if and only if every program completes after with every line, no line after is an abort: line (a host exception escaping the evaluation), so that no answer fails for want of host data (the area design's section 3.3), and the test262 file passes after in every variant the runner runs.

   It asks that every line complete and that none be an `abort:` line, and compares no line with its expected answer. The
   line's answer `[h23,h12]` is neither missing nor an `abort:` line, so part D is not revised.

## What changed

1. **Part C admits exactly that line, as a new class L19.** Class L19 admits one line: line 128 of the `locales` program's
   answer (the harness's `locales:128`), the line whose key is `11 fr-CA hourCycles`, whose expected answer, as the pinned
   copies hold it, is `11 fr-CA hourCycles => [h12,h23]`, and only when its answer after is exactly
   `11 fr-CA hourCycles => [h23,h12]`, CLDR 48.2.0's time data for `fr-CA` under row L19 (C5, CLDR's own data). It is listed
   by name, with its group (the table's line 52), row L19, this ruling, its expected answer, its answer after, its answer at
   the base and the reference's line. Any other answer of that line is not in L19. No other line is ever in L19: not another
   line of row L19, not another `hourCycles` line of the program or of its group, not a line of any other program, and not
   the same line under part D, which does not need it. The class is named after its row, and it admits one line of that row,
   not the row. Nothing else widens.
2. **How the harness carries it.** `scripts/conformance/run_reference_datasets.py` runs unchanged, and this revision changes
   no file the rule fixes. The harness decides no class: it reports the line as differing from its expected answer, as it
   reports every line that does. Part C's table of V, R, Z8, O and H lines lists it in one more row, as class L19, and the
   bundle's README reads that line, and only that line, as L19. The rule's comparison is not revised; it reads:

   > comparison: per program and line, the line after against its expected answer; and per line that still differs, the line after against the same line at the base and against the reference's line

   and part C, restated below, keeps it word for word. The revised passages of part C are the failure passage and the MET
   passage, which item 4 of "What was seen" quotes, and the new class line L19.
3. **Part C's expected answer for that line is read from Broiler.VM**, as the rule says: the retained answer with
   Broiler.VM's divergences applied, as Broiler.VM's `4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f` holds them, copied here byte
   for byte and pinned by digest; "a copy whose digest differs is not the population". It is not read from Broiler.VM's tree
   at any later commit. Broiler.VM's own answer moves the same way at its next re-pin of the shared library, since its
   `getHourCycles` calls the same `LocaleInfo.HourCycles`: Broiler.VM's `src/Broiler.VM.Profile.JavaScript/JsRealm.Locale.cs`
   at `68cad8baf7403125d44d5007441d30ff07bc4edf`, Broiler.VM's SJL-9 after commit (the merge of
   Broiler-Platform/Broiler.VM#190), lines 127-128, and the same at `df75f8cea79aa2452ca22892d47d8afa5a14c93f`, the head of
   Broiler.VM's default branch when this was written. At `4cf9ca44`, the commit the copies are pinned from, the method
   answered from the profile's own `HourCyclesOf` (lines 154-157 and 487), which reads the time data for the preferred
   regions; the divergences file the copies hold records that method's answer, `[h12,h23]`. After that re-pin the line reads
   as it does now: its expected answer stays `[h12,h23]`, because the population is the pinned copy, so the line still
   differs and is still admitted only by L19; this engine's `[h23,h12]` then equals Broiler.VM's own answer. If Broiler.VM
   rewrites its divergences file's line for the move, that file is not this part's population, and the copy here is not
   replaced.
4. **Broiler.VM's records this ruling touches.** Broiler.VM's JSD-0082 gains its note of the rulings of 2026-11-09, and of
   this one, at its next re-pin, as this repository's plan already says. Broiler.VM's own rule judges its own datasets, and
   nothing here touches it.
5. **The owner's other ruling of 2026-11-10, "Accept, follows CLDR".** `fa`'s default `persian` calendar writes
   `{minute: "2-digit", second: "2-digit"}` as `۳:۰۵`, with `minute` resolved `"numeric"` (rows T16 and T13). Neither part
   C nor part D reads that answer: no program of part C formats a date in `fa`. The `locales` program's `fa-IR` lines are
   `Intl.Locale` information, its `calendars` line being `getCalendars` (row L16), and `calendar-formats.js` formats the
   `persian` calendar in `en` and `de` alone. So this revision admits nothing for that ruling, which needs no rule text.

**Nothing else changed:** the populations, the configurations, the comparison, classes V, R, Z8, O and H, parts A and B as the
revision of 2026-11-09 restates them, part D, the base and after commits, the copies and their pins, `divergence-groups.tsv`
and its dated corrections, and every other line of the rule are as they were.

## The alternatives not taken

- **"Keep old answer"**, the alternative the owner did not take: this engine would answer `[h12,h23]` for `fr-CA`, the
  region's entry, against the pinned CLDR 48.2.0 time data, which names an entry for `fr-CA`. That would undo row L19's
  "Fix both consumers" for this engine, or add an engine-side exception to the shared library's answer.
- **Admitting the line by its row or its group.** This revision does not: the ruling names the line, and class L19 admits it
  by name.

## Part C, restated with class L19

```rule
evidence class: conformance parity (not a measurement)
population: Broiler.VM's reference datasets of the area, byte for byte as Broiler.VM's commit that carries its own bundle sjl-9-001 rule holds them, each with Broiler.VM's retained answer and divergences file: src/tests/cldr/{dates, displaynames, durations, lists, locales, numbers, plurals, relativetimes, segments}/ and src/tests/temporal/{intl, calendars-intl, zone-names}/, which SJL-9.2 copies into conformance/datasets/intl/ with their digests and the pin conformance/datasets/intl/datasets.pin; and src/tests/cldr/timezones/, which conformance/datasets/time/ holds since SJL-8.4 under block 1 of its datasets.pin; a copy whose digest differs is not the population
configuration: each program's text evaluated by indirect eval in the shell BroilerJS built in Release, through scripts/conformance/run_reference_datasets.py --bundle sjl-9-001 and its driver, which print the program's completion value; no LANG and TZ=UTC; at the base commit (the base, retained and read) and at the after commit (judged); the expected answer of each line is Broiler.VM's: the retained answer with Broiler.VM's divergences applied, which Broiler.VM's slice checks hold its own answer equal to; and once, at the after commit's time, each program run in the reference this bundle's reference.txt names (the newest Node.js release on CLDR 48, by its tarball's SHA-256), whose output is retained
comparison: per program and line, the line after against its expected answer; and per line that still differs, the line after against the same line at the base and against the reference's line
admitted difference classes:
  V Broiler.VM's coverage - a line whose expected answer comes from a divergence in a group this bundle's divergence-groups.tsv marks coverage, read with the dated corrections of that table in this bundle's README, the table and the corrections both as the rule commit holds them, so that a correction added after the rule commit does not bind this part (a locale, a calendar or a tailoring Broiler.VM's data lacks, JSD-0082 decision D5, or a numbering system's own symbols, which Broiler.VM's data lacks, decision D21), where this engine's answer after equals the retained answer, or, in a group of a numbering system's symbols, equals the expected answer with each of the locale's latn number symbols (the decimal and grouping separators, the signs and the percent sign) replaced by the same symbol of that numbering system in CLDR 48.2.0, or, in the group of a Collator's tailorings (src/tests/cldr/locales/divergences.txt lines 62-92, the table's line 50), is a collations line of a Locale with no co keyword whose answer after equals, sorted in code-unit order, the list of the collations this engine's Intl.Collator resolves for that Locale: each value c of Intl.supportedValuesOf("collation") for which new Intl.Collator(tag, {collation: c}).resolvedOptions().collation is c, tag being the Locale's toString(), in the same shell at the after commit (rows L17 and L40; the Collator's data is SJL-17's, and the shared CollationsOfLocale reads this engine's Collator's list, JSD-0082 decision D33 (c)); a line the group's note names as also carrying a difference the pinned text decides is admitted only when its answer after is the line that note names; each listed with its group, and a collations line admitted by the Collator's list listed with that list
  R the release - a line of V's kind whose answer after equals neither, and equals the reference's line: the retained answers are ICU 77.1's on CLDR 47.0, and this engine's data is CLDR 48.2.0; each listed with the three lines; the reference answers CLDR 48.0 (reference.txt), so a line whose value CLDR changed between 48.0 and 48.2.0 equals no line of the reference and is in no class
  Z8 the resolved identifier - a line whose difference is only the timeZone a formatter reports for an identifier that is not its own primary, where this engine answers the identifier as given (JSD-0082 decision D15), against Broiler.VM's base answer, its primary; each listed
  O outside the area - a line of segments.js whose answer after equals its answer at the base (SJL-7's Intl.Segmenter), whatever group of divergence-groups.tsv holds it; class O is read first for every line of segments.js; each listed
  H the default-locale provider - a line whose difference is only a value the default-locale provider supplies, which decision SJL-D-19 and JSD-0082 decision D7 keep this engine's; each listed with the provider's answer
  L19 the one admitted line, by the owner's ruling of 2026-11-10, "Dated part C revision" - line 128 of the locales program's answer (the harness's locales:128), the line whose key is 11 fr-CA hourCycles, in the spec group of divergence-groups.tsv's line 52, whose expected answer, as the pinned copies hold it, is "11 fr-CA hourCycles => [h12,h23]", and only when its answer after is exactly "11 fr-CA hourCycles => [h23,h12]", CLDR 48.2.0's time data for fr-CA (preferred H, allowed H h hB), which row L19 reads since the owner's ruling of 2026-11-09, "Fix both consumers" (data policy C5); listed by name with its group, row L19, this ruling, its expected answer, its answer after, its answer at the base and the reference's line; any other answer of that line is not in L19, and no other line is: not another line of row L19, not another hourCycles line, and not a line of any other program
every other difference is a failure, including a line in a spec group of divergence-groups.tsv that differs from Broiler.VM's divergence (other than a line of segments.js that class O admits, and the one line class L19 admits), a line of V's or R's kind that no reading of class V admits and that does not equal the reference's line, and a line that differs from its expected answer for any other reason
MET if and only if every program completes after with every line, and every line that differs from its expected answer is in V, R, Z8, O, H or L19 and is listed.
NOT MET otherwise.
```

**How it is read.** As the rule's part C is read, with one more row in part C's table of V, R, Z8, O and H lines: the
class-L19 line, with its program and line, its group, row L19, this ruling, its expected answer, its answer after, its
answer at the base and the reference's line. A part C run whose `locales:128` answers anything other than
`11 fr-CA hourCycles => [h23,h12]` after, and differs from its expected answer, is read as the rule as written reads it.
Parts A and B are read as the revision of 2026-11-09 says, and part D as the rule says.

## The rule it revises, quoted whole

> # Bundle SJL-9-001's decision rule: locale data and the Intl algorithms, judged by test262 per path in UTC and in a zone with daylight-saving time, by the reference datasets, and under invariant globalization
>
> **Written:** 2026-10-26, at milestone SJL-9 (work package SJL-9.1) of the shared JavaScript libraries programme,
> with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records ("Locale data and the Intl
> algorithms (SJL-9.1)") and Broiler.VM's JSD-0082. It was written before any code of the area exists: no
> locale-data generator in this repository, no shared Intl member, no cut-over. **Milestone judged:** SJL-9,
> `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-9-locale-data-and-intl.md`: its Broiler.JS clause 9
> ("test262 `intl402` per path meets the area rule"), clause 8's reading under invariant globalization ("Host
> globalization remains only behind the optional providers"), and SJL-9.6's "the Intl reference datasets in both
> engines" for this engine. **Owner:** the Broiler.JS maintainer. **Reviewer:** none. Every role this rule names is held
> by one person (the programme records this as Broiler.VM's exception EX-30), and this rule does not claim that anyone
> independent has read it.
>
> **What this file is.** Four predeclared decisions, one per part.
> - **The templates.** Parts A and B are in template T2 of the programme's validation document,
>   `Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4. Part C is a vector agreement in T3's sense,
>   over the reference datasets Broiler.VM retains. Part D is a robustness check of the providers. It has no template
>   letter.
> - **Each part decides alone**, and none reads another's runs.
> - **There is no measurement part.** SJL-9's exit gate has no measurement clause (JSD-0082 decision D23).
> - **Other rules.** The data home's clauses (1 to 3) and the home clauses (4 to 6) are judged by their own mechanisms:
>   the tests SJL-9.2 and SJL-9.3 commit, each with its witness. Clauses 7 and 10 are read from the tree and
>   `Broiler.JS/CHANGELOG.md`. Broiler.VM commits its own rule for its own engine. None is read here.
> - **It holds no figure.**
>
> **The correspondence this rule classifies by** is Broiler.VM's `docs/evidence/sjl-9-001/correspondence.md`, classified
> by JSD-0082's section "Every row, classified". It was read from code in both engines at the revisions it names, and
> every behavioural cell its rows rest on was run in this engine's shell. Its rows are named here by their letters:
> - L for locale identifiers, negotiation and Locale info;
> - M for the Intl mathematical value and digit options;
> - P for plural rules and ranges;
> - N for number formatting;
> - S for list, relative-time and display-name formatting;
> - T for date-time formatting;
> - U for duration formatting;
> - Z8 for the formatter's resolved time zone.
>
> **The pins.**
> - ECMA-402 is the text JSD-0082's decision D14 pins, in this repository's `conformance/vectors/sources/ecma402.pin`:
>   `tc39/ecma402` at its last `main` commit on or before 2026-05-08, which already holds the Intl Locale Info
>   proposal, and the Intl era and month code proposal at its own last commit on or before that date.
> - Rows S7 and T8 are the two where a test262 file both engines pass asks for an answer that text does not give.
>   They are read as JSD-0082's decision D33 reads them.
> - Intl over Temporal follows `tc39/proposal-temporal@e8cc03fc` (JSD-0078's decision D1).
> - The suite is test262 `ccaac100`.
> - The locale data is CLDR 48.2.0, generated for this engine's list (decision D5).
>
> **Why two zones.** A formatter's default time zone is the host's (decision SJL-D-19), and the cut-over moves every
> formatting of a date in it.
> - **Part A** is the canonical configuration, in the runner's own zone, UTC.
> - **Part B** is the same with the host's zone set to `America/New_York`.
>
> **Why negative tests are included.** Bundles `sjl-7-001` to `sjl-10-001` took their runs with them, so every run of
> this bundle is comparable with theirs.
>
> **The commits, named before they exist** (JSD-0082 decision D4).
> - **The rule commit** is the commit that carries this file.
> - **The after commit** is the default-branch merge that lands SJL-9.5's cut-over, one merge (decision D3), named in
>   bundle `sjl-9-001`.
> - **The base commit** is the after commit's first parent. It must meet three conditions:
>   - it descends from the rule commit;
>   - it descends from the commit bundle `sjl-12-001` names as this repository's after commit;
>   - the after commit is its only first-parent successor that changes a path under `Broiler.JS/Broiler.JavaScript.*/`,
>     under `Broiler.ECMAScript/` outside `Broiler.ECMAScript/docs/`, or `Directory.Packages.props`. If the cut-over had
>     to land in more than one merge, each is SJL-9.5's and is named in bundle `sjl-9-001`, and the base is the first
>     one's first parent.
> - A run names its tested commit in its own `run.txt`, so each is dispatched at any time after its commit exists.
>
> A part whose base was taken at any other commit, or whose base commit fails those conditions, is void.
>
> ## Part A - test262 parity per path, in UTC, with negative tests (template T2)
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser; test/intl402 is in it, and a regression on any other path fails this part too
> configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as the base commit carries it and unchanged at the after commit, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified in every shard and in the re-run, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, UTC, shown by its host-zone record; both runs' host-zone records read alike, line for line: TZ unset, UTC +0000 at both instants, and one tzdata release; the base run is taken at the base commit and the after run at the after commit, on the same RID; each run's artifact test262-evidence is imported unchanged, with the digest of the zip as downloaded
> comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; the script refuses two runs whose includeNegative differs
> timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
> admitted difference classes:
>   C1 correction - a row failing at the base passes after, because the shared library follows the pinned text where this engine did not: each row JSD-0082's section "Every row, classified" puts in its column "Correction of Broiler.JS"; each one listed with its path, its variant and its row, and listed in the changelog (decision SJL-D-16)
>   C3 implementation-defined - a row failing at the base passes after, because the shared library's choice where the specification leaves one to the implementation (best-fit matching, pattern selection, interval patterns, display-name fallbacks) replaces this engine's: each row in the column "Implementation-defined", listed with its path, its variant, its row and the vector that classifies it
>   C5 data policy by decision - a row failing at the base passes after, because the tables the shared generator writes from CLDR 48.2.0 for this engine's locale list (JSD-0082 decision D5) replace host ICU data, the Broiler.Unicode.Cldr.LocaleData package, the CLDR 47.0.0 numbering-system table (decision D13) or a hand-written table: each row in the column "Data policy", listed with its path, its variant, its row and the decision
> the resolved time zone is not a class: this engine already reports the identifier as given, which decision D15 keeps, so a verdict that moves on it is a regression
> every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, whatever its class, and a row whose verdict moves between failed, timed out and skipped without a class
> MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
> NOT MET otherwise; and if the two runs' host-zone records differ, because the runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the after run's zone record.
> ```
>
> ## Part B - test262 parity per path, in a zone with daylight-saving time (template T2)
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: part A's
> configuration: part A's, with time_zone America/New_York in both dispatches, so that the workflow sets TZ to it for every shard and for the re-run; both runs' host-zone records read alike, line for line: TZ America/New_York, EST -0500 at 2021-01-15T12:00:00Z, EDT -0400 at 2021-07-15T12:00:00Z, and one tzdata release; the base run is taken at the base commit, and the after run at the after commit
> comparison: part A's
> timeouts: part A's
> admitted difference classes: part A's, over the same rows; the rows whose answer depends on the host's zone are read in this part's zone: row T27, a correction of this engine (the default zone's names), is in C1 and is listed per variant, and row T11, the host-zone provider, is an engine policy this engine keeps (decision SJL-D-19) and is in no class, so a verdict that moves on it is a regression
> every other difference is a regression, as in part A
> MET if and only if both runs are complete as part A says, every shard of each in the zone, the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.
> NOT MET otherwise; and if the two runs' host-zone records differ, the part is not judged until the base is taken again at the same commit in the after run's zone record.
> ```
>
> ## Part C - the reference datasets (vector agreement, T3's sense)
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: Broiler.VM's reference datasets of the area, byte for byte as Broiler.VM's commit that carries its own bundle sjl-9-001 rule holds them, each with Broiler.VM's retained answer and divergences file: src/tests/cldr/{dates, displaynames, durations, lists, locales, numbers, plurals, relativetimes, segments}/ and src/tests/temporal/{intl, calendars-intl, zone-names}/, which SJL-9.2 copies into conformance/datasets/intl/ with their digests and the pin conformance/datasets/intl/datasets.pin; and src/tests/cldr/timezones/, which conformance/datasets/time/ holds since SJL-8.4 under block 1 of its datasets.pin; a copy whose digest differs is not the population
> configuration: each program's text evaluated by indirect eval in the shell BroilerJS built in Release, through scripts/conformance/run_reference_datasets.py --bundle sjl-9-001 and its driver, which print the program's completion value; no LANG and TZ=UTC; at the base commit (the base, retained and read) and at the after commit (judged); the expected answer of each line is Broiler.VM's: the retained answer with Broiler.VM's divergences applied, which Broiler.VM's slice checks hold its own answer equal to; and once, at the after commit's time, each program run in the reference this bundle's reference.txt names (the newest Node.js release on CLDR 48, by its tarball's SHA-256), whose output is retained
> comparison: per program and line, the line after against its expected answer; and per line that still differs, the line after against the same line at the base and against the reference's line
> admitted difference classes:
>   V Broiler.VM's coverage - a line whose expected answer comes from a divergence in a group this bundle's divergence-groups.tsv marks coverage, read with the dated corrections of that table in this bundle's README, the table and the corrections both as the rule commit holds them, so that a correction added after the rule commit does not bind this part (a locale, a calendar or a tailoring Broiler.VM's data lacks, JSD-0082 decision D5, or a numbering system's own symbols, which Broiler.VM's data lacks, decision D21), where this engine's answer after equals the retained answer, or, in a group of a numbering system's symbols, equals the expected answer with each of the locale's latn number symbols (the decimal and grouping separators, the signs and the percent sign) replaced by the same symbol of that numbering system in CLDR 48.2.0, or, in the group of a Collator's tailorings (src/tests/cldr/locales/divergences.txt lines 62-92, the table's line 50), is a collations line of a Locale with no co keyword whose answer after equals, sorted in code-unit order, the list of the collations this engine's Intl.Collator resolves for that Locale: each value c of Intl.supportedValuesOf("collation") for which new Intl.Collator(tag, {collation: c}).resolvedOptions().collation is c, tag being the Locale's toString(), in the same shell at the after commit (rows L17 and L40; the Collator's data is SJL-17's, and the shared CollationsOfLocale reads this engine's Collator's list, JSD-0082 decision D33 (c)); a line the group's note names as also carrying a difference the pinned text decides is admitted only when its answer after is the line that note names; each listed with its group, and a collations line admitted by the Collator's list listed with that list
>   R the release - a line of V's kind whose answer after equals neither, and equals the reference's line: the retained answers are ICU 77.1's on CLDR 47.0, and this engine's data is CLDR 48.2.0; each listed with the three lines; the reference answers CLDR 48.0 (reference.txt), so a line whose value CLDR changed between 48.0 and 48.2.0 equals no line of the reference and is in no class
>   Z8 the resolved identifier - a line whose difference is only the timeZone a formatter reports for an identifier that is not its own primary, where this engine answers the identifier as given (JSD-0082 decision D15), against Broiler.VM's base answer, its primary; each listed
>   O outside the area - a line of segments.js whose answer after equals its answer at the base (SJL-7's Intl.Segmenter), whatever group of divergence-groups.tsv holds it; class O is read first for every line of segments.js; each listed
>   H the default-locale provider - a line whose difference is only a value the default-locale provider supplies, which decision SJL-D-19 and JSD-0082 decision D7 keep this engine's; each listed with the provider's answer
> every other difference is a failure, including a line in a spec group of divergence-groups.tsv that differs from Broiler.VM's divergence (other than a line of segments.js that class O admits), a line of V's or R's kind that no reading of class V admits and that does not equal the reference's line, and a line that differs from its expected answer for any other reason
> MET if and only if every program completes after with every line, and every line that differs from its expected answer is in V, R, Z8, O or H and is listed.
> NOT MET otherwise.
> ```
>
> ## Part D - the providers under invariant globalization
>
> ```rule
> evidence class: robustness (not a comparison of runs)
> population: part C's programs; and test262 ccaac100's test/intl402/default-locale-is-supported.js, which asserts, for each Intl service constructor the file tests (Intl.Collator, Intl.NumberFormat and Intl.DateTimeFormat, and Intl.PluralRules, Intl.RelativeTimeFormat and Intl.ListFormat where they exist), that its default locale is among its available locales (row L44, JSD-0082 decision D7; Intl.Collator's available locales come from the same available-locale provider as every other service's)
> configuration: part C's harness with --invariant-globalization, which sets DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 for the shell; and scripts/compliance/run_test262.py over that one test262 path, through the same shell, with DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 in the environment the runner hands the shell; for both, no LANG and TZ=UTC; at the base commit (retained and read) and at the after commit (judged)
> MET if and only if every program completes after with every line, no line after is an abort: line (a host exception escaping the evaluation), so that no answer fails for want of host data (the area design's section 3.3), and the test262 file passes after in every variant the runner runs.
> NOT MET otherwise.
> ```
>
> **How the parts are read.** As bundle `sjl-10-001`'s rule reads its parts, once per part. The bundle retains:
> - each run;
> - each comparison's JSON report;
> - the classification file;
> - a table naming each C1, C3 and C5 row with its row of the correspondence;
> - part C's table of V, R, Z8, O and H lines;
> - part C's probe of the collations this engine's `Intl.Collator` resolves for each Locale of a `collations` line class
>   V admits by that list, with its run at the after commit;
> - part D's runs of `default-locale-is-supported.js`, at both commits.
>
> The script decides no class. A classification of a row that passed at the base is reported as refused. Totals do not
> decide either test262 part, and **the two parts' runs are never compared with each other**. Part C's base run is
> retained and read, not judged: on the base, the programs differ wherever this engine's Intl does, on the lines the
> correspondence names.
>
> **The test262 files of rows S7, T8 and L17**, which ask for an answer the pinned text does not give, or for more
> than it requires (JSD-0082 decision D33). They keep their base verdicts in parts A and B, and no class admits a
> move:
> - **Row S7**: `RelativeTimeFormat/prototype/format/en-us-numeric-always.js` and
>   `RelativeTimeFormat/prototype/formatToParts/en-us-numeric-always.js`, which asserts the same magnitude with no
>   `minusSign` part;
> - **Row T8**: `DateTimeFormat/prototype/resolvedOptions/hourCycle-dateStyle.js` and `hourCycle.js`;
> - **Row L17**: `Locale/prototype/getCollations/output-array-values.js`, which asks `en` for at least one collation.
>   The pinned text allows none. The file keeps passing only if this engine's `Intl.Collator`, which keeps its host
>   path here, lists a collation for `en`. The same list is what part C's class V admits for a `collations` line of the
>   locales program's group of a Collator's tailorings: with the adapter decision D33 (c) names, this engine's `zh`,
>   `zh-Hant`, `zh-TW`, `zh-Hans-SG` and `ko` lines answer its own Collator's lists, which equal neither the retained
>   answers (ICU's) nor Broiler.VM's `[emoji,eor]`.
>
> ## Revisions
>
> A rule revised after any run it judges has been seen is a new dated file in this directory. It quotes this one whole
> and says it was written afterwards, and its decision is then weaker evidence.
>
> ## What this file does not decide
>
> - **The data home's clauses**: the pinned, hashed inputs and byte-identical regeneration (rule SL7, with its witness),
>   the public CLDR version, and the agreed locale set's completeness. Each is held by a test SJL-9.2 commits.
> - **The home clauses**: each sub-area under the conventions, its vectors at library level, the SL rules and the Native
>   AOT probe. Each is held by a test or a CI job SJL-9.3 commits.
> - **This engine's deletion list, its changelog and `docs/public-api.md`.** They are read from the tree at SJL-9.6.
> - **Broiler.VM's test262, reference datasets, closures and rules.** Broiler.VM's rule judges them.
> - **The vectors.** SJL-9.2 adds them, and they may falsify the area record.
> - **Collation**, which is SJL-17's. `Intl.Collator`'s collation data and comparison, and `localeCompare`, keep their
>   host path here. `Intl.Collator`'s default and available locales are decision D7's providers', as every service's,
>   and part D's test262 file reads them.
> - **Answers under a host culture.** No part runs under one. JSD-0082's decision D7 has every reader of DefaultLocale
>   read one default-locale provider, casing included (row L35). SJL-9.5's tests hold that, not this file.
> - **A speed claim of any kind.** SJL-9 measures nothing.
