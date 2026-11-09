# Bundle SJL-9-001's decision rule, revised before its judged runs: one test262 file this engine loses under row T12, by the owner's ruling "Follow CLDR"

**Written:** 2026-11-09, at the shared library's merge that answers the owner's decision of 2026-11-09, "Keep + fix
library first", **after** SJL-9.5's local pre-check runs of parts A and B had been seen - test262 over `test/intl402` and
the `toLocale*` directories at its stage 3's commits `129d94cc`, `2e3ada85` and `1f99fa5d`, in UTC and in New York, against
its stage 1's base answers at `6f7284da` - and **before** either judged run of either part was taken: no base run and no
after run that [`decision-rule.md`](decision-rule.md) names exists. The rule's own "Revisions" paragraph makes this weaker
evidence than the rule itself, and it is said here. It was written at the owner's direction: the owner ruled on
2026-11-09, **"Follow CLDR, dated ruling"**, that this engine loses the one test262 file below under data row T12, listed by
name, and that the rule be revised to admit it, before SJL-9.5's judged runs and on the default branch. The alternative not
taken was **"Plain space like V8"**, a substitution of U+0020 for the U+202F CLDR writes, which part C's dates program, whose
expected answers carry U+202F, would then fail. **Owner:** the Broiler.JS maintainer. **Reviewer:** none; every role is one
person (the programme's EX-30), and this revision does not claim that anyone independent has read it. Broiler.VM's record of
the same ruling, a note to its JSD-0082, is made in Broiler.VM by its own stage; nothing in Broiler.VM's rule changes here.

**The rule revised** is `docs/evidence/sjl-9-001/decision-rule.md` as blob
`34dfc4f7dda6dca4b23bb971ff0aeefdba537b9e`, the file the rule commit (`50978882`, its last change `117a6f7d`) and this
revision's base (`055a51c7`) both hold. That file is not rewritten; this one is read beside it.

## What changed, and why

1. **The file.** `test/intl402/DateTimeFormat/prototype/format/numbering-system.js` (test262 `ccaac100`) passes at SJL-9.5's
   base and fails after its cut-over of `Intl.DateTimeFormat` to the shared library, in both zones and from the first commit
   of that cut-over on. Its assertion "en-US-u-nu-hanidec: display all time units in 2-digit" asks the format of
   `en-US-u-nu-hanidec` with `hour`, `minute` and `second` `"2-digit"` to include `"〇二:三五:〇六 AM"`, a plain space before
   the day period. The shared library writes CLDR 48.2.0's `en` time pattern `hh:mm:ss a` as CLDR states it, with U+202F
   NARROW NO-BREAK SPACE before the period: row T12 of the correspondence, a data-policy row (C5), and JSD-0082's decision
   D25, which makes the `atTime` joins and U+202F C5 data rather than corrections. Node, the pinned reference, writes U+202F
   in `formatToParts` and U+0020 in `format` alone, V8's substitution (row T12's reference column); the file asks for that
   substitution. The rule's part A says, and part B follows it:

   > every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, whatever its class, and a row whose verdict moves between failed, timed out and skipped without a class

   > MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.

   and part B:

   > every other difference is a regression, as in part A

   > MET if and only if both runs are complete as part A says, every shard of each in the zone, the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after, and every changed verdict is in C1, C3 or C5 and is listed.

   So under the rule as written, this one row - a path passing at the base that fails after, whatever its class - makes
   parts A and B NOT MET however the rest of the runs read.

   The file loops over `en-US`, `en-US-u-nu-arab`, `en-US-u-nu-deva` and `en-US-u-nu-hanidec` in that order and asserts
   four times for each (its lines 54 to 57), and test262 stops at the first failing assertion. A failure on the hanidec
   line 56 above therefore shows that every earlier assertion passed, and it **masks** one: hanidec's line 57,
   "en-US-u-nu-hanidec: display one numeric-styled unit", which asks the format with `second: "numeric"` alone to include
   `"六"`. The run records only the failing assertion's message, which would read the same whatever made line 56 fail -
   U+202F, or a wrong hanidec digit in the two-digit format - so the message alone shows neither the cause nor that line 57
   would pass. *(This paragraph was added 2026-11-09, at review, before this revision merged.)*
2. **The revision admits exactly that row, in both parts, as a new class L.** Class L admits the one path, in each variant
   the run holds, only when it fails after on the one assertion above, because of the U+202F the data writes, and only when
   a retained probe shows that cause and the masked assertion: at the after commit, in the shell the after run used (the
   binaries whose hashes the run records) and in the part's zone, the probe prints the String of
   `new Intl.DateTimeFormat("en-US-u-nu-hanidec", {hour: "2-digit", minute: "2-digit", second: "2-digit"}).format(new Date(2024, 0, 1, 2, 35, 6, 789))`
   and of the same format with `{second: "numeric"}` alone, each with its code points, and the row is in L only if the
   first is exactly `"〇二:三五:〇六 AM"` with U+202F before `AM` (the file's expected String with U+202F for its U+0020)
   and the second includes `"六"`. The file failing on any other assertion (its `arab`, `deva` or `en-US` lines, or the
   fractional-second one), either probe answer differing, the probe not retained, timing out or being skipped after is not
   in L, and no other path is ever in L: every other row passing at the base that fails, times out or is skipped after stays
   a regression, whatever its class, as the rule says. *(The probe clause was added 2026-11-09, at review, before this
   revision merged: the item read "only when it fails after on the one assertion above, because of the U+202F the data
   writes. The file failing on any other assertion (...), timing out or being skipped after is not in L".)* The shared
   library's own test `DateTimeLocaleDataTests.T12_the_hanidec_format_numbering_system_js_asks_for_differs_only_by_U_202F`
   holds the library to both answers at this merge; it is not the probe, which reads the engine's shell.
3. **How the script carries it.** `scripts/compliance/compare_test262_paths.py` runs unchanged, as committed at SJL-0.9,
   and this revision changes no file the rule fixes. The script admits the labels C1, C3 and C5 alone and reports a
   classification of a row that passed at the base as refused. So the row is classified in the revision's own file,
   `test262/revised-2026-11-09/classifications.json`, beside the original's, with the class C5 and a reason beginning
   "L (T12, the owner's ruling of 2026-11-09)"; the script reports it as a regression with that classification refused, and
   the bundle's README reads that row, and only that row, as class L. The collector's comparison is run again over the runs
   the `test262` step retained, into `test262/revised-2026-11-09/`; it takes no run of its own. The probe of item 2 is
   retained beside it, as `test262/revised-2026-11-09/class-l-probe.<part>.txt`, one file per part. *(The last sentence was
   added 2026-11-09, at review, before this revision merged.)*
4. **Broiler.VM's records this ruling touches.** Its JSD-0082's decision D21 gives as its reason "Broiler.JS passes
   test262's `DateTimeFormat/prototype/format/numbering-system.js` at its base. That file asserts U+066B for
   `en-US-u-nu-arab` (row T37), and template T2 forbids losing it."; this revision admits losing that same file. Its row
   T37 assertion (`arab`'s U+066B) still passes after, and the file is lost on hanidec's U+202F (row T12, decision D25).
   Broiler.VM's own dated note under D21 (its commits `0705679` and `dad7795`, on its default branch at `df75f8c`) already
   qualifies that reason with this ruling. Its correspondence row L16 gives as `getCalendars`' cause "Neither engine's data
   carries that table, so both answer the empty list's fallback"; from this merge Broiler.JS's tables carry CLDR's calendar
   preferences and the date-time format reads them, while the library keeps `getCalendars` at `["gregory"]` by choice, as
   row L16 and part C hold it. That cause is owed a dated note in Broiler.VM, at its next re-pin. Neither touches this
   rule. *(This item was added 2026-11-09, at review, before this revision merged.)*

**Nothing else changed:** the populations, configurations, timeouts, classes C1, C3 and C5, part B's zone, parts C and D,
the base and after commits and every other line of the rule are the original's. Part C is not touched by this file: its
dates program's expected answers carry U+202F, which is why the alternative not taken would have failed it.

## Part A, restated with class L

```rule
evidence class: conformance parity (not a measurement)
population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser; test/intl402 is in it, and a regression on any other path fails this part too
configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as the base commit carries it and unchanged at the after commit, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified in every shard and in the re-run, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, UTC, shown by its host-zone record; both runs' host-zone records read alike, line for line: TZ unset, UTC +0000 at both instants, and one tzdata release; the base run is taken at the base commit and the after run at the after commit, on the same RID; each run's artifact test262-evidence is imported unchanged, with the digest of the zip as downloaded
comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; the script refuses two runs whose includeNegative differs; class L is not a label the script admits (it admits C1, C3 and C5, and reports a classification of a row that passed at the base as refused), so a row of class L is classified in the revision's file test262/revised-2026-11-09/classifications.json with the class C5 and a reason beginning "L (T12, the owner's ruling of 2026-11-09)", the script reports it as a regression with that classification refused, and the bundle's README reads such a row as class L
timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
admitted difference classes:
  C1 correction - a row failing at the base passes after, because the shared library follows the pinned text where this engine did not: each row JSD-0082's section "Every row, classified" puts in its column "Correction of Broiler.JS"; each one listed with its path, its variant and its row, and listed in the changelog (decision SJL-D-16)
  C3 implementation-defined - a row failing at the base passes after, because the shared library's choice where the specification leaves one to the implementation (best-fit matching, pattern selection, interval patterns, display-name fallbacks) replaces this engine's: each row in the column "Implementation-defined", listed with its path, its variant, its row and the vector that classifies it
  C5 data policy by decision - a row failing at the base passes after, because the tables the shared generator writes from CLDR 48.2.0 for this engine's locale list (JSD-0082 decision D5) replace host ICU data, the Broiler.Unicode.Cldr.LocaleData package, the CLDR 47.0.0 numbering-system table (decision D13) or a hand-written table: each row in the column "Data policy", listed with its path, its variant, its row and the decision
  L the one admitted loss, by the owner's ruling of 2026-11-09, "Follow CLDR" - test/intl402/DateTimeFormat/prototype/format/numbering-system.js, in each variant the run holds (original and terser), passing at the base and failing after because the String a format of en-US-u-nu-hanidec with hour, minute and second "2-digit" writes has U+202F NARROW NO-BREAK SPACE before the day period, CLDR 48.2.0's pattern (row T12, data policy C5; JSD-0082 decision D25), where the file's assertion "en-US-u-nu-hanidec: display all time units in 2-digit" asks for U+0020, and only when the probe retained as test262/revised-2026-11-09/class-l-probe.<part>.txt, run at the after commit in the shell the after run used (the binaries whose hashes the run records) and in the part's zone, prints for new Intl.DateTimeFormat("en-US-u-nu-hanidec", {hour: "2-digit", minute: "2-digit", second: "2-digit"}).format(new Date(2024, 0, 1, 2, 35, 6, 789)) exactly "〇二:三五:〇六 AM" with U+202F before AM, and for the same format with {second: "numeric"} alone a String that includes "六" (the file's line 57, which the failure masks), each with its code points; each variant listed with its path, its variant, row T12, this ruling, the assertion's message as the run records it and the probe's two answers; the file failing on any other assertion, either probe answer differing, the probe not retained, timing out or being skipped after is not in L, and no other path is
the resolved time zone is not a class: this engine already reports the identifier as given, which decision D15 keeps, so a verdict that moves on it is a regression
every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, whatever its class, but for the rows class L admits, and a row whose verdict moves between failed, timed out and skipped without a class
MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after but those class L admits, and every changed verdict is in C1, C3, C5 or L and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, because the runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

## Part B, restated with class L

```rule
evidence class: conformance parity (not a measurement)
population: part A's
configuration: part A's, with time_zone America/New_York in both dispatches, so that the workflow sets TZ to it for every shard and for the re-run; both runs' host-zone records read alike, line for line: TZ America/New_York, EST -0500 at 2021-01-15T12:00:00Z, EDT -0400 at 2021-07-15T12:00:00Z, and one tzdata release; the base run is taken at the base commit, and the after run at the after commit
comparison: part A's, with the revision's classification file test262/revised-2026-11-09/classifications.json and class L's reading as part A's block revised on 2026-11-09 states them
timeouts: part A's
admitted difference classes: part A's, over the same rows; the rows whose answer depends on the host's zone are read in this part's zone: row T27, a correction of this engine (the default zone's names), is in C1 and is listed per variant, and row T11, the host-zone provider, is an engine policy this engine keeps (decision SJL-D-19) and is in no class, so a verdict that moves on it is a regression; and class L, as part A's block revised on 2026-11-09 states it, read in this part's zone
every other difference is a regression, as in part A as revised on 2026-11-09
MET if and only if both runs are complete as part A says, every shard of each in the zone, the two runs' host-zone records read alike, the script finds them comparable, no row passing at the base fails, times out or is skipped after but those class L admits, and every changed verdict is in C1, C3, C5 or L and is listed.
NOT MET otherwise; and if the two runs' host-zone records differ, the part is not judged until the base is taken again at the same commit in the after run's zone record.
```

**How it is read.** As the rule's parts A and B are read, with one more table in the bundle: the class-L row of each part,
per variant, with its path, its variant, row T12, this ruling, the failing assertion's message as the run records it and the
probe's two answers with their code points. A part whose script report lists a regression other than a class-L row, or
whose probe is missing or answers otherwise, is NOT MET. *(Revised 2026-11-09, at review, before this revision merged: the
paragraph read "...this ruling and the failing assertion's message as the run records it. A part whose script report lists
a regression other than a class-L row is NOT MET.")*

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
