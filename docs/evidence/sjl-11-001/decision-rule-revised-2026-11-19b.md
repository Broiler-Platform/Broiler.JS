# Bundle SJL-11-001's decision rule, revised a fourth time, after part C's runs were seen: part C admits case N05's change from past the wall-clock limit at the base to its declared outcome after, by name, and nothing wider

**Written:** 2026-11-19, at milestone SJL-11 (work package SJL-11.5, decision D35), after SJL-11.5 merged to the
default branch as Broiler-Platform/Broiler.JS#1126, the after commit `b2b226cd3158f0b7d990a3f4fecbe75b259b4fa1`, whose
first parent is the base commit `5429c4065e8b273e5c7965598b8a2f5490a8ad98`. It is written **after part C's runs were
seen**, and after part C was read on them: part C's base run, retained in [`base/part-c/`](base/part-c/), its after
run and its control, retained in [`after/part-c/`](after/part-c/) and [`controls/part-c/`](controls/part-c/), and the
bundle README's reading of them, "Part C: read NOT MET", on case N05 alone. The other runs this rule judges had been
seen too: parts A, C and D's base runs, parts C, D and E's after runs with the controls of parts C and E, part A's
after run, and each of those parts' readings in the bundle's README. No part B arm had been built and no part B lane
had run. **Owner:** the Broiler.JS maintainer. **Reviewer:** none; every role is one person (the programme's EX-30),
and this revision does not claim that anyone independent has read it.

*(Changed 2026-11-19, the day it was written, before this revision merged, with no run taken since it was written, so
that its own text holds no figure, as the rule says of itself ("It holds no figure. Where a count or an allowance
matters, it names the file that fixes it."): "Why it is written" read that N05's program "compiles a pattern of" a
number of "nested capturing groups", that "The manifest's header states the wall-clock limit" followed by the header's
line with its number, and N05's exit at the base and after by their numbers; it now names the kind of nesting, the
manifest's header and how each process ended, and the files that hold the numbers. The owner's answer, quoted
verbatim, is not changed. Nothing else changes.)*

*(Changed again 2026-11-19, the same day, before this revision merged, with no run taken since it was written, after
review: the note above read "before this revision merged and before any run", which was not true of this revision,
written after the runs named above had been seen, and now reads "before this revision merged, with no run taken since
it was written"; "Its configuration names a process's outcomes as" ended "or past the wall-clock limit", four, and its
MET line", and the count is removed, since the quote lists the outcomes; and "Its name" named only Broiler.VM's form
for a second revision of one day, and now also names this repository's. The decision and the two lines it changes are
not changed. Nothing else changes.)*

**Whose decision.** This revision is filed by the owner's answer of 2026-11-19, an answer of the owner's own, not one
taken under the owner's direction of 2026-10-18, quoted verbatim. Asked (2026-11-19): "SJL-11.5 part C (the
adversarial corpus) reads NOT MET on one case alone, N05. At the base, the old .NET-translated engine ran N05 past
the 300 s wall-clock limit and it was killed. After the cut-over, N05 throws its declared SyntaxError, which is the
outcome the corpus asks for. The rule has no class for that change: C1's base outcomes don't include "past the
wall-clock limit", and C3 needs the case to have completed at the base. The other nine changed cases are C3. What
should happen?" Chosen: "Admit N05 by revision" — "A dated revision admits exactly this change for N05: past the
wall-clock limit at the base, the declared outcome after. It is recorded as weaker evidence, because it was written
after the runs were seen. Part C is then read again on the same retained runs, with nothing rerun." Not taken: "Keep
part C NOT MET" — "The reading stands as taken. SJL-11's part C clause stays unmet for Broiler.JS until a later dated
choice." This file is that dated revision. It changes no rule of Broiler.VM's or Broiler.RegEx's.

**The rule revised** is `docs/evidence/sjl-11-001/decision-rule.md` as blob `b1f7091d966f5f22935cab333f67fa9271961768`,
the file the after commit holds. It is quoted whole at the end of this file, as the earlier revisions quote it (each
line prefixed with "> ", a blank line a bare ">"), and is not edited, as its section "Revisions" requires. **The
earlier revisions**, [`decision-rule-revised-2026-11-17.md`](decision-rule-revised-2026-11-17.md) (blob
`a04d3b0bf73b4e7fc77b807da58195b276583096`), [`decision-rule-revised-2026-11-18.md`](decision-rule-revised-2026-11-18.md)
(blob `62ad84b69ee4d00a96d4a5bfafa9205f7521f48d`) and [`decision-rule-revised-2026-11-19.md`](decision-rule-revised-2026-11-19.md)
(blob `69245b95884052262e3d63c7968100dae481b53b`), are not edited, and they and their readings stand; this file is
read beside all four. None of the earlier revisions changes part C. **Its name.** It is the bundle's second revision
of 2026-11-19, and the bundle has no earlier one of a single day; its name adds `b` to the date, as Broiler.VM's
bundle `sjl-13-001` names its second revision of 2026-11-09 `decision-rule-area-revised-2026-11-09b.md`. This
repository's own bundle [`sjl-13-001`](../sjl-13-001/) names its second revision of one day with a word instead,
`decision-rule-revised-2026-11-02-second.md`, beside `decision-rule-revised-2026-11-02.md`; this file keeps the `b`
form its work was given, and either name is the owner's to choose. The name decides nothing.

## Why it is written

Part C's comparison, admitted difference classes and MET line read, whole:

```rule
comparison: per case, the outcome after against the declared outcome, and against the outcome at the base
admitted difference classes:
  C1 correction - a case whose outcome at the base is a wrong answer, a no match where the specification matches, a shorter match, a flipped look-around, or an abnormal end, and whose outcome after is its declared one; each listed with the correspondence row
  C3 implementation limit - a case that completed at the base and after throws the RangeError or SyntaxError its declaration names, for a limit of decisions D7 and D10; listed
no other class
MET if and only if, after, every case gives its declared outcome, no process ends abnormally and none passes the wall-clock limit; every case whose outcome differs from the base's is in C1 or C3 and is listed; and the control is reported as stated.
```

Its configuration names a process's outcomes as "completed with its answer, a thrown error with its constructor, an
abnormal end, or past the wall-clock limit", and its MET line keeps the last two apart.

**Case N05** is the class N case of [`inputs/adversarial/manifest.tsv`](inputs/adversarial/manifest.tsv) whose program,
`N05.js` (SHA-256 `34d3fa80fa3a5825275f29651519051f7cd97eb6aaab562a5439d0f6afa6c61d`, as the manifest names it),
compiles a pattern of nested capturing groups, the deepest of that kind in the manifest, and whose declared outcome
is "threw SyntaxError". The manifest's header states the wall-clock limit.
- **At the base** its process passed the wall-clock limit and was killed: [`base/part-c/results.tsv`](base/part-c/results.tsv)
  (SHA-256 `710aa4f2296fcc8643adb6b0bb6125a9ed222ce149f81a4177cb73d2c6eb761c`) reads its outcome "past the wall-clock
  limit", with the process killed at the limit, and `base/part-c/cases/N05.out.txt` (`0cc162d1c9d44749493ed8fd76cba1fb1ac419bcb84bc5bf43c1effde877b701`)
  reads "timed-out: True", with no line of standard output.
- **After** it gives its declared outcome: [`after/part-c/results.tsv`](after/part-c/results.tsv)
  (`e87ca5b437a016c11c97dcabc01cc4ac1f9172ae0354c975e6b3b429237da466`) reads "threw SyntaxError", the process ending normally, "matches-declared
  yes", and `after/part-c/cases/N05.out.txt` (`209fda347ee7fee16bd551c4052d59f30365c1b78717c49b812c8ff269c4eddd`) reads
  "adversarial-outcome N05 threw SyntaxError".

Neither class admits that change. C1's outcomes at the base are "a wrong answer, a no match where the specification
matches, a shorter match, a flipped look-around, or an abnormal end", and "past the wall-clock limit" is none of them;
C3 asks for "a case that completed at the base". The rule admits "no other class", so "every case whose outcome
differs from the base's is in C1 or C3 and is listed" does not hold for N05, and the bundle's README read part C NOT
MET, on that case alone. Every other case whose outcome differs from the base's, M01, M02, N02, N04, N08, N10, N11, N14
and N16, is read there as C3.

## What changed

1. **Part C's admitted difference classes** read with one named exception, after C3 and before "no other class":
   "**the exception for case N05** - case N05 of the manifest alone, whose outcome at the base is past the wall-clock
   limit and whose outcome after is its declared one, threw SyntaxError; listed with this revision". It is not a
   class: it names one case, one outcome at the base and one outcome after, and admits nothing else.
2. **Part C's MET line**, in its second clause, "every case whose outcome differs from the base's is in C1 or C3 and is
   listed", now reads "every case whose outcome differs from the base's is in C1 or C3, or is case N05 under its
   exception, and is listed". Nothing else of the line changes: its first clause ("after, every case gives its declared
   outcome, no process ends abnormally and none passes the wall-clock limit") and its third ("the control is reported
   as stated") stand, and so does the NOT MET line.
3. **Nothing else of part C changes**: its evidence class, population, configuration (the wall-clock limit the
   manifest's header states among it), controls, comparison, the classes C1 and C3 as worded, "no other class" for
   every case but N05, and "How it is read".

## How it reads

- **Only N05, only that change.** The exception admits N05's difference only when its outcome at the base is past the
  wall-clock limit and its outcome after is "threw SyntaxError". Any other case past the wall-clock limit at the base,
  any other outcome of N05 at the base (completed, another thrown error, an abnormal end) or after, and any other
  difference, is read as the rule reads it: in C1 or C3, or no class.
- **Not C1 and not C3.** N05 is not read as C1, so no correspondence row is named for it, and not as C3, so it is not
  counted among the cases that completed at the base. The README lists it under this exception, beside the C3 cases.
- **The wall-clock limit after is not widened.** The MET line's first clause still holds every case to its declared
  outcome after, with no process past the limit, N05 among them; the exception reads N05's outcome at the base alone.
- **What part C's "How it is read" says of the N cases** ("The N cases past the limit complete at the base where this
  engine accepts their depth, ... after, they throw the `SyntaxError` of the nesting limit, under C3 or under C1 with
  row S3b") is not rewritten: it did not foresee a case that did not complete at the base, and this exception, not
  that paragraph, admits N05.
- **The runs are not taken again.** Part C is read again on the runs already retained, `base/part-c/`, `after/part-c/`
  and `controls/part-c/`, with nothing rerun, as the owner's answer says. The first reading, "read NOT MET", stays on
  record in the README beside the second.

## What it decides, and what it does not

- **It decides** that part C admits case N05's change from past the wall-clock limit at the base to its declared
  outcome after, "threw SyntaxError", by a named exception, and that part C's MET line reads with it.
- **It does not change** any other line of the rule or of the earlier revisions: the definitions of the rule commit,
  the inputs commit, the base commit and the after commit, and the voiding of the base runs if the after commit's first
  parent is not the commit they were taken at; parts A, B, D and E; and part C's every line but the two named above.
  It widens no tolerance: the manifest's wall-clock limit, part A's timeouts line and part B's noise floor stand.
- **It is weaker evidence** in the sense of the rule's section "Revisions": "A rule revised after any run it judges has
  been seen is a new dated file in this directory, quoting this one whole and saying it was written afterwards; its
  decision is then weaker evidence." It was written after part C's base and after runs, its control and its first
  reading had been seen, and after the runs named at the top of this file.
- **It takes no reading** of any part and declares nothing met; the bundle's README reads part C again under it.
- **The alternative not taken** is the owner's: "Keep part C NOT MET", quoted above.

## The rule it revises, quoted whole

> # Bundle SJL-11-001's decision rule: test262 parity per path with negative tests, Broiler.JS's measurement, the adversarial corpus in this engine, culture independence, and the deletion list
>
> **Written:** 2026-10-25, at milestone SJL-11 (work package SJL-11.1) of the shared JavaScript libraries programme,
> with the area verdict this repository's `docs/roadmap/SharedLibraries.md` records ("Regular expressions (SJL-11.1)")
> and Broiler.VM's JSD-0081,
> `Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0081-regular-expressions-move-to-broiler-regex-under-its-owner-conditions.md`.
> It was written before any change it judges exists: no adapter onto Broiler.Regex 0.2, no deletion and no benchmark
> class. Committed in the same change: part B's workloads as JavaScript text and part C's programs, under
> [`inputs/`](inputs/). **Milestone judged:** SJL-11,
> `Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-11-regexp.md`: its Broiler.JS clauses 4 ("The translator
> and the validators are gone"), 5 ("test262 `RegExp` per path meets the area rule") and 6 ("Duplicate named groups
> still work"), SJL-11.6's adversarial corpus in this engine, and this engine's measurement. **Owner:** the Broiler.JS
> maintainer. **Reviewer:** none. Every role this rule names is held by one person (the programme records this as
> Broiler.VM's exception EX-30), and this rule does not claim that anyone independent has read it.
>
> *Revised 2026-10-25, the day it was written, after review, before any run and before this change reached the default
> branch: part A's MET line admits a row passing at the base that fails after under either kind of class C3, where it
> read "outside C3 (L)", so that kind (E), row M4's empty match, admits a move in either direction, as its definition
> says. Nothing else changed.*
>
> *Dated note, 2026-11-01; the rule above is not rewritten.* Its opening paragraph reads "no adapter onto Broiler.Regex 0.2", the
> version that decision D12 of Broiler.VM's JSD-0081 named ("The SJL-11 surface ships as **`0.2.0-preview.1`**"). The owner
> wrote on 2026-11-01 "Please keep using version 0.1.0-x scheme!", which revises D12: the SJL-11 surface ships in the
> `0.1.0-preview.N` stream, as `0.1.0-preview.6`, the next after `0.1.0-preview.5`, which the owner published on
> 2026-10-29 from Broiler.Regex `24344b3`. "Broiler.Regex 0.2" is read as the Broiler.Regex version that carries the
> SJL-11 surface, `0.1.0-preview.6` or a later version of the same stream; nothing else of the rule changes.
>
> **What this file is.** Five predeclared decisions, one per part.
> - **The templates.** Part A is template T2 of the programme's validation document
>   (`Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4), part B T5. Part C is resource accounting
>   over a declared corpus (JSD-0081 decision D37), part D an identity of vector reports across host cultures
>   (decision D33), and part E a search and a metadata check (decision D34).
> - **Each part decides alone**, and no part reads another's verdict.
> - **Part B is the only measurement.** Its runs are retained in `sjl-11-001/measurement/`, because
>   [`Measurement.md`](../../roadmap/Measurement.md) asks every result bundle to declare one evidence class. It is not a
>   gate clause of SJL-11; JSD-0081 decision D19 says how its verdict is recorded.
> - **Other rules.** Broiler.VM and Broiler.RegEx commit their own rules in their own bundles `sjl-11-001`. Neither is
>   read here.
> - **It holds no figure.** Where a count or an allowance matters, it names the file that fixes it.
>
> **The correspondence this rule classifies by** is Broiler.VM's `docs/evidence/sjl-11-001/correspondence.md`, read
> from code at this repository's `1f91b8f1`, Broiler.VM's `aaa39fb` and Broiler.Regex `0.1.0-preview.4` (`c1422f4d`),
> which this engine restores for every `u` and `v` pattern, with every behavioural cell run. Its section "Revisions"
> reads this repository's `792ff56e`, where SJL-12.5 moved the validators to `Broiler.JavaScript.Ast` with comments and
> a forwarder as their only change. Its rows are named here by their letters and numbers: S (syntax and early errors),
> M (matching), B (budget and limits), F (flags), T (text helpers) and U (data). A row's class that rests on a reading of
> the specification's text stands until `regexpref` checks that reading; a reading it refutes is a finding against
> JSD-0081, and the row is reclassified there (decision D25).
>
> **This engine's corrections, named before any run** (JSD-0081 decision D23). Each is a row of the correspondence
> that names a correction of Broiler.JS, or of Broiler.Regex `0.1.0-preview.4` as this engine routes it. Each is listed
> by its row in `Broiler.JS/CHANGELOG.md` at SJL-11.5 (decision SJL-D-16 (a)). Where the correspondence names the
> test262 files a row explains, they are named here; each fails in this repository's bundle `sjl-10-001` after run
> (`test262/after/test262-results.json.gz`, at `00b65dd0`, whose validators, scanner, compiler literal and
> `BuiltIns/RegExp/` are byte-identical to `1f91b8f1`'s).
> - **The literal check**, closed by re-pointing the scanner's check to Broiler.Regex's validate entry (decision D22):
>   - **S2b**, a reference spelled with an escape, refused by the literal path where the constructor accepts it;
>   - **S7**, a quantified look-behind accepted without `u`:
>     `test/language/literals/regexp/invalid-optional-lookbehind.js`, `invalid-optional-negative-lookbehind.js`,
>     `invalid-range-lookbehind.js` and `invalid-range-negative-lookbehind.js`;
>   - **S8**, the `v` flag's reserved syntax accepted:
>     `test/built-ins/RegExp/prototype/unicodeSets/breaking-change-from-u-to-v-01.js` to
>     `breaking-change-from-u-to-v-28.js` but `-03.js`, whose pattern `[[]` this engine refuses today, the others'
>     patterns being `[(]` `[)]` `[{]` `[}]` `[/]` `[-]` `[|]` `[&&]` `[!!]` `[##]` `[$$]` `[%%]` `[**]` `[++]` `[,,]`
>     `[..]` `[::]` `[;;]` `[<<]` `[==]` `[>>]` `[??]` `[@@]` ``[``]`` `[~~]` `[^^^]` `[_^^]`; and `[a|]` and `[a!!b]`,
>     which no file names;
>   - **S11**, a literal's errors reported only when it is evaluated, not early: the invalid modifiers and the `v`-mode
>     classes the row lists, explaining `test/language/literals/regexp/early-err-modifiers-code-point-repeat-i-1.js`,
>     `early-err-modifiers-code-point-repeat-i-2.js`, `early-err-modifiers-other-code-point-uppercase-I.js`,
>     `early-err-modifiers-should-not-case-fold-i.js`, `early-err-modifiers-should-not-case-fold-m.js`,
>     `early-err-modifiers-should-not-case-fold-s.js`, and `early-err-arithmetic-modifiers-` followed by
>     `add-remove-i.js`, `add-remove-m.js`, `add-remove-multi-duplicate.js`, `add-remove-s.js`, `both-empty.js`,
>     `code-point-repeat-i-1.js`, `code-point-repeat-i-2.js`, `no-colon-1.js`, `no-colon-2.js`, `no-colon-3.js`,
>     `other-code-point-uppercase-I.js`, `reverse-add-remove-i.js`, `reverse-add-remove-m.js`,
>     `reverse-add-remove-multi-duplicate.js`, `reverse-add-remove-s.js`, `reverse-code-point-repeat-i-1.js`,
>     `reverse-code-point-repeat-i-2.js`, `reverse-other-code-point-uppercase-I.js`, `reverse-should-not-case-fold-i.js`,
>     `reverse-should-not-case-fold-m.js`, `reverse-should-not-case-fold-s.js`, `should-not-case-fold-i.js`,
>     `should-not-case-fold-m.js` and `should-not-case-fold-s.js`;
> - **S2a**, a name spelled once with an escape and once without, in one alternative, accepted (with the library);
> - **S10's class case**, `[\uD83D\u{DC38}]` under `u`, which joins the two escapes into one astral code point;
> - **case-insensitive matching through .NET**: M2 (bundle `sjl-7-001`'s F2), M2a under `tr-TR` and under `lt-LT`, and
>   M2b; and **U3**, by data decision (class C5 of part A, decision SJL-D-6 (c));
> - **M3**, .NET's line terminators (bundle `sjl-7-001`'s W5), with `$` without `m`;
> - **M4's matches of half a surrogate pair** from a `lastIndex` inside the pair. The step out of a pair that SJL-11.5
>   adds moves only the matcher's start, never the reported `"index"` or substring (decision D23);
> - **M6**, `String.prototype.search`'s direct call;
> - **T1**, a literal's `source` rewritten where its constructor twin's is not;
> - **with Broiler.Regex `0.1.0-preview.4`**: B1 (the shortened match and the no-match of a long greedy run), B2 (the
>   wrong index), B3 (the flipped look-around), B5 (the stack guard's fallback, to .NET and to no match), B8 (the process
>   abort of a flat sequence), S3b's process abort at the deepest nesting, and M7's look-behind back-reference inside a
>   pair.
>
> Not on it: `\0` before a non-ASCII digit, which is Broiler.Regex's alone and not observable here (row S4); B4,
> uncharged alternation, which this engine cannot show because it passes no meter; and an invalid literal read as
> division, which is SJL-14's (row S9). None of the rows above but S7, S8 and S11 names a test262 file.
>
> **Why negative tests are included.** The corrections include what a parser refuses or admits, and test262 tests them
> through negative-metadata tests: rows S7, S8 and S11 explain only negative tests. Bundles `sjl-7-001`, `sjl-8-001`,
> `sjl-10-001` and `sjl-12-001` took their runs with them.
>
> **The commits, named before they exist.**
> - **The rule commit** carries this file. It changes only `docs/`.
> - **The inputs commit** is the default-branch commit that adds `RegExpBenchmarks` and the benchmark `Program.cs`'s
>   `--regexp-answers`, changing only `Broiler.JS/benchmarks/` and `docs/`. It descends from the rule commit and from
>   SJL-12's corpus commit in this repository (JSD-0081 decision D17).
> - **The base commit** is the default-branch commit that the merge landing SJL-11.5 has as its first parent. It
>   descends from the inputs commit. Every base run is taken there, before the merge, while merges to
>   `Broiler.JS/Broiler.JavaScript.*/` and `Broiler.ECMAScript/` are held (decision D16).
> - **The after commit** is that merge. If its first parent is not the commit the base runs were taken at, the base
>   runs are void and are taken again before any after run is seen.
>
> **Why the base is not taken at the rule commit.** SJL-11.2 is long, and other milestones' product changes (SJL-12's,
> SJL-9's, SJL-17's) would land between a base at the rule commit and the after run, and be read as SJL-11's.
>
> ## Part A - test262 parity per path, in UTC, with negative tests (template T2)
>
> ```rule
> evidence class: conformance parity (not a measurement)
> population: the whole pinned suite - test262 at revision ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e, the revision .github/workflows/test262.yml defaults to and Broiler.VM's src/tests/conformance/pins/test262.pin pins - every path the runner's --all-script-host-verifiable selection admits with --include-negative, in both variants the canonical configuration runs, original and terser
> configuration: scripts/compliance/run_test262.py through the shell BroilerJS built in Release, by .github/workflows/test262-evidence.yml as this file's commit carries it, dispatched with the tested commit's forty-digit identifier, suite_ref at the revision above, max_workers 4, test_timeout_seconds 30, rerun_timeouts true, time_zone empty and include_negative true: eight shards merged by scripts/compliance/merge_test262_shards.py, --minifier terser at the version its lock file pins, no memory limit, negative-metadata tests included and their expected errors verified in every shard and in the re-run, a --self-check before each shard, every shard running the same shell, shown by the hashes of its binaries, and every shard in the runner's own zone, UTC, shown by its host-zone record; both runs' host-zone records read alike, line for line; the base run is taken at the base commit and the after run at the after commit, on the same RID; each run's artifact test262-evidence is imported unchanged, with the digest of the zip as downloaded
> comparison: per path and variant, joined on both, the verdict at the base against the verdict after, through scripts/compliance/compare_test262_paths.py as committed at SJL-0.9, with the classification file retained beside its report; the script refuses two runs whose includeNegative differs
> timeouts: a row that timed out in either run is the one the workflow ran again alone, with one worker, at that run's commit; the script takes the re-run through --base-rerun or --after-rerun, it replaces the row, and both the run and the re-run are retained
> duplicate named groups (clause 6): every row of a file whose front matter's features list regexp-duplicate-named-groups passes at both commits
> admitted difference classes:
>   C1 correction - a row failing at the base that passes after, because the shared engine follows the specification where this engine did not, under a row of this file's list of this engine's corrections (rows S2a, S2b, S7, S8, S10's class case, S11, M2, M2a, M2b, M3, M4's half-pair matches, M6, T1, and with Broiler.Regex 0.1.0-preview.4 B1, B2, B3, B5, B8, S3b's process abort and M7's look-behind), the test262 files that list names for rows S7, S8 and S11 among them; each listed with its path, its variant and the row, and listed in Broiler.JS/CHANGELOG.md (decision SJL-D-16 (a)); a C1 row that no row of the list explains is recorded in the bundle as a finding against JSD-0081 and is a regression
>   C3 implementation-defined, of two kinds: (L) a limit - a row passing at the base that after throws this engine's RangeError for Broiler.Regex's frame or trail ceiling, or its SyntaxError for the nesting limit (JSD-0081 decisions D7, D10 and D39); each listed with the limit it reached, shown by the row's pattern and subject run through Broiler.Regex at library level under the same policy, whose LimitExceeded status names that limit; the library run is retained; and (E) a row whose verdict moves only because this engine's answer to the empty match from a lastIndex inside a surrogate pair under u or v moves (row M4's empty match), which JSD-0081 decision D41 classes implementation-defined until the specification's text is corrected; listed with the row and the decision
>   C5 data policy by decision - a row whose verdict changes because Unicode 17.0.0's data through the Unicode owner's provider replaces the runtime's in a construct the translator delegated to .NET (a character class or case-insensitive matching without u), under SJL-D-6 (c); each listed with its row (row U3)
> every other difference is a regression, including a row that passes at the base and fails, times out or is skipped after, and a row whose verdict moves between failed, timed out and skipped without a class
> MET if and only if both runs are complete (every expected shard reported, no configuration failure, one suite revision, one shell, every shard in the zone, includeNegative true in both), the two runs' host-zone records read alike, the script finds them comparable, every duplicate-named-groups row passes at both commits, no row passing at the base fails, times out or is skipped after outside C3, of either kind, and every changed verdict is in C1, C3 or C5 and is listed.
> NOT MET otherwise; and if the two runs' host-zone records differ, because the runners' tzdata moved between them, the part is not judged until the base is taken again at the same commit in the after run's zone record.
> ```
>
> **How it is read.** As bundle `sjl-7-001`'s part A. The bundle retains each run, the comparison's report, the
> classification file, and a table naming each C1, C3 and C5 row with its reason. The script decides no class.
> - **The files this file's list names are expected to move**, each from failing at the base to passing after. One
>   that still fails after is no difference this part judges, and the bundle records it as a correction SJL-11.5 did not
>   make, which clause 7 then reads.
> - **A row of row S9**, an invalid literal read as division, is no C1 row: the division fallback stays, SJL-14's
>   (decision D22), and a change of such a row's verdict is a finding against JSD-0081 and a regression.
>
> ## Part B - Broiler.JS's measurement (template T5)
>
> ```rule
> evidence class: measurement; in Measurement.md's classes, an acceptance comparison when the lane is a controlled lane as its section "Controlled lanes and effective configuration" requires, and smoke otherwise; a smoke run is void for this part and is not judged
> population: the six workloads of RegExpBenchmarks (Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks/RegExpBenchmarks.cs) as the inputs commit adds it, each running the JavaScript text of docs/evidence/sjl-11-001/inputs/workloads/ verbatim, as committed with this rule - Scan (scan.js), Captures (captures.js), Replace (replace.js), Split (split.js), Unicode (unicode.js) and Backtrack (backtrack.js) - each with its control, ScanControl to BacktrackControl, running the *.control.js file beside it; the benchmark project built in Release under JIT with its own runtime settings; one machine and one RID, both named in the sub-bundle's manifest
> arms: B the base commit, with RegExpBenchmarks.cs and the benchmark Program.cs as the inputs commit has them; A the after commit, with the same two files; A-again the A arm measured a second time under another lane name, the A/A lane; each arm's benchmark and engine assemblies named by hash in the manifest, which is written and made read-only before any lane runs
> configuration: in every repetition, B, A and A-again each run once, in an order that rotates by repetition; one run is one launch of the benchmark executable with --filter on this class only, BROILER_BENCHMARK_PROFILE=baseline and a build timeout ample for the lane, which runs all six workloads and their controls, each in BenchmarkDotNet's own child process, under the baseline job BenchmarkConfiguration.cs fixes at the inputs commit; the process's culture invariant; seven repetitions per arm, every one retained
> condition: before and after every run, the arm's --regexp-answers prints every workload's answer, its program's completion value; each answer counts the operations that completed and equals the same workload's answer at arm B's first check; a run whose arm's answers differ fails the condition
> configuration check: the configuration object --regexp-answers prints (framework, RID, architecture, GC mode, tiering, PGO and ReadyToRun settings, vector acceleration, processor count, the invariant-globalization variable and the current culture), and in each run's full JSON report BenchmarkDotNet's host environment record with each workload's hardware-intrinsics record, are identical across arms, lanes and repetitions
> quantity: Q(workload, arm) = the median, over the arm's seven runs, of BenchmarkDotNet's reported median time per operation of that workload in that run
> noise floor: N(workload) = |Q(workload, A) - Q(workload, A-again)|
> comparison: D(workload) = Q(workload, A) - Q(workload, B)      (positive: the after arm slower), against N(workload); one engine's after arm against the same engine's base arm, never against another engine
> attribution, retained beside D and deciding nothing: E(workload, arm) = Q(workload, arm) - Q(control, arm)
> guardrails, reported for every workload and arm and deciding nothing: BenchmarkDotNet's allocated bytes per operation and its collection counts
> admitted difference classes: none
> MET if and only if all of:
>   1. the lane is a controlled lane, recorded as Measurement.md requires;
>   2. every run's condition passes, before and after, for every arm;
>   3. every configuration check is identical across arms, lanes and repetitions;
>   4. every run completed every workload, and BenchmarkDotNet reported no workload with issues;
>   5. for every workload, D <= N.
> NOT MET otherwise.
> readings:
>   - a workload whose |D| <= N is below resolution, and meets clause 5;
>   - a failed condition is a NOT MET, not a re-run; an interrupted collection may be run again from the start, with the stopped run retained and named as stopped;
>   - this part returns MET or NOT MET and makes no performance claim; JSD-0081 decision D19 records a NOT MET as the trade the area design names, "a slower but bounded matcher", and a void part leaves the question open for this engine.
> ```
>
> **Why a new class.** `RegexProfileMetrics` and `RegexCallEnvelopeMetrics` time .NET `Regex` and the routing
> directly, which the cut-over deletes, so they cannot run unchanged at both arms.
> `Broiler.JS/OtherTests/JIntPerfTests/Scripts/dromaeo-object-regexp.js` is not run either: part B's population is the
> six workloads above, whose answers this rule's condition can hold.
>
> **Why the answers count operations.** The cut-over corrects results this engine gets wrong today, so an answer
> built from the results would change for a correction, and the condition would refuse the very change the milestone
> exists to make. Each workload's answer counts the calls it completed, fixed by the loop bounds written in its file,
> and is still failed by a lane that throws or stops early. The workloads' README says which rows of the correspondence
> they stay away from.
>
> **The workloads are Broiler.VM's**, byte for byte, as their README records; each engine measures them against itself
> only, under its own rule, and no figure of one is compared with the other's.
>
> ## Part C - the adversarial corpus in this engine (resource accounting; SJL-11.6)
>
> ```rule
> evidence class: resource accounting (not a measurement)
> population: every case of docs/evidence/sjl-11-001/inputs/adversarial/manifest.tsv as committed with this rule, each a program generated by Broiler.RegEx's eng/adversarial-programs.py --engine broiler-js from that repository's tests/Adversarial/corpus.tsv, both named in the manifest's header by commit and SHA-256; the classes W, G, L, N, F, M, V and O, at the sizes the generator chose so that the specification's answer completes inside the manifest's wall-clock limit; the classes K and A are not in it, because this engine passes no meter (JSD-0081 decision D10) and their unbounded cases are judged at library level by Broiler.RegEx's rule; each case with its declared outcome after: completed with the specification's answer as the corpus declares it, a reading of the specification's text that regexpref checks (decision D25), a RangeError, or a SyntaxError
> configuration: the shell BroilerJS built in Release; each case one process with --script-host, its program read as this rule's commit stores it and checked against the manifest's SHA-256 before the run, with the wall-clock limit the manifest's header states, the process's culture invariant; at the base commit and at the after commit; for each process its exit, its outcome (completed with its answer, a thrown error with its constructor, an abnormal end, or past the wall-clock limit) and its output retained
> controls: a patch to a scratch copy of the after commit, never merged, in which the adapter answers no match where Broiler.Regex's match reports LimitExceeded naming frames or trail; every M case must be reported as differing from its declared outcome; retained; reverted and the revert checked
> comparison: per case, the outcome after against the declared outcome, and against the outcome at the base
> admitted difference classes:
>   C1 correction - a case whose outcome at the base is a wrong answer, a no match where the specification matches, a shorter match, a flipped look-around, or an abnormal end, and whose outcome after is its declared one; each listed with the correspondence row
>   C3 implementation limit - a case that completed at the base and after throws the RangeError or SyntaxError its declaration names, for a limit of decisions D7 and D10; listed
> no other class
> MET if and only if, after, every case gives its declared outcome, no process ends abnormally and none passes the wall-clock limit; every case whose outcome differs from the base's is in C1 or C3 and is listed; and the control is reported as stated.
> NOT MET otherwise.
> ```
>
> **How it is read.** This is validation section 7's control for SJL-11, "a pattern whose budget runs out, which must
> report exhaustion and not no match", for the classes this engine runs, and SJL-11.6's "adversarial resource corpus"
> in this engine, as decision D10 reads it.
> - **The M cases** are the only cases that reach a ceiling, through the adapter's mapping of `LimitExceeded` to a
>   catchable `RangeError`, so they are the cases the control can reach. At the base this engine has no ceiling, and an
>   M case that completes there and throws after is class C3.
> - **The N cases past the limit** complete at the base where this engine accepts their depth, and end the process
>   where Broiler.Regex `0.1.0-preview.4`'s parser recurses past the stack (row S3b); after, they throw the
>   `SyntaxError` of the nesting limit, under C3 or under C1 with row S3b.
> - **The G cases** write their run as the class `[a]`, which the corpus's third presumption runs as one instruction
>   (decision D37), so they complete inside the ceilings. A G case that throws after has broken that presumption and
>   differs from its declared outcome.
> - **A case declared to complete** is held to its declared answer, so a correction this engine needs but does not
>   make is a NOT MET here, whatever part A shows.
>
> ## Part D - culture independence of the regular-expression vectors
>
> ```rule
> evidence class: determinism across host cultures (not a measurement)
> population: every vector of conformance/vectors/regexp/ at the corpus revision bundle sjl-11-001 names (the revision that classified both engines' first runs)
> configuration: the engine-level vector runner (Broiler.JS/Broiler.JavaScript.Vectors.Tests) built in Release, run three times at the after commit - with the invariant culture, with tr-TR and with lt-LT as the current culture and current UI culture - and the same three runs at the base commit, retained and read, not judged
> comparison: the three after reports, byte for byte; and each vector's result after against its class's expectation for broiler-js
> admitted difference classes: none
> MET if and only if the three after reports are byte-identical and every vector's result after is the outcome its class expects (normative: out; impl-defined: per-engine.broiler-js, row M4's empty match among them by JSD-0081 decision D41; a divergence.broiler-js field that the cut-over answers reports that it no longer holds).
> NOT MET otherwise.
> ```
>
> **How it is read.** Case-insensitive matching through .NET `RegexOptions.IgnoreCase` without `CultureInvariant`
> (`Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs:698-701` at `1f91b8f1`) reads the current culture; the
> base runs show it (row M2a, under `tr-TR` and under `lt-LT`), and the after runs must not.
>
> *(Note of 2026-11-01, the owner's decision D-partd, taken as recommended under the owner's direction of 2026-10-18,
> "Please do all recommended decisions and skip waiting for review", and recorded in Broiler.VM's JSD-0081, "The owner's
> decisions of 2026-11-01". It was written before any run this part judges, and the rule above is not rewritten. Part
> D's population reads "every vector of conformance/vectors/regexp/ at the corpus revision bundle sjl-11-001 names". Its
> MET clause holds each vector to "the outcome its class expects". But the 18 vectors of `regexp/budget.vectors` whose
> `out` is `status: exhausted` are library-level (JSD-0081's decision D25). The corpus's SCHEMA.md section 4 has an
> engine-level runner skip them and give them no line, and this engine's runner does since `d8cb8406` (corpus revision
> 31 with SCHEMA.md's dated note; `CorpusRun`, which names each one it skips in its summary line). So an engine-level
> run can never give their `out`. By this decision **the status vectors are judged at library level only, by
> Broiler.Regex's part D**. **This part's
> population is the non-status vectors** of `regexp/` at that revision, the vectors this engine's run gives a line to.
> The three after reports are compared byte for byte over them, and each is held to its class's outcome as the MET
> clause says. The corpus revision this part names is 32, the one that classifies both engines' first runs. Two more
> readings, from the classification of revision 32 and the same decisions, are recorded for the reader of clause 7:
> - the list of this engine's corrections above reads with row S5's class case (decision D-s5: `\k` in a class beside a
>   named group, which this engine refuses and B.1.2 accepts) and row F3's `(?x:a)` (decision D-widen);
> - its rows M2, S7 and S8 read with the inputs decision D-widen adds.
>
> No test262 file at `ccaac100` tries any of these, so part A's list of C1 rows needs none of them.)*
>
> ## Part E - the deletion list (clause 4)
>
> ```rule
> evidence class: deletion (not a comparison of runs)
> population: the after commit's sources under Broiler.JS/ and its Release assemblies Broiler.JavaScript.Parser, Broiler.JavaScript.Ast, Broiler.JavaScript.BuiltIns and Broiler.JavaScript.Runtime
> checks:
>   E-1 search: git grep -w -n, at the after commit, over Broiler.JS/ but CHANGELOG.md, for each name of JSD-0081's Broiler.JS deletion list (its X6 sections and decisions D20 to D22) that a word search can find - the types RegExpValidator, RegexTiering and RegexTieringDiagnostics; the members ClampLargeQuantifiers, HexValue, GapScan, TryRouteToBroiler, FromNet, CreateRegex, ParseFlags, BuildFlagsString, TryAppendEscape, EscapePatternSource and DecodeBracedUnicodeEscapes; RegExpGroupNames' FirstError and FirstDuplicate and JSUnicodePropertyNames' FirstRefused, the regular-expression prechecks, wherever SJL-12.5 moved them - and the file Broiler.JS/Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.UnicodeSets.cs absent; no hit, except RegExpUnicodeValidator, the forwarder to Broiler.Regex's validate entry the list keeps under SJL-D-25 (a) (decision D21), and a hit the bundle lists with the declaration that shows it is another type's member of the same name
>   E-2 metadata: no TypeRef or MemberRef into System.Text.RegularExpressions from the Parser and Ast assemblies, from any type whose namespace is Broiler.JavaScript.BuiltIns.RegExp, from JSStringPrototype's search, match, matchAll, replace, replaceAll and split members, or from LegacyRegExpState; this holds the members of the list a word search cannot find - the translator's members between ClampLargeQuantifiers and HexValue that are not object-model code, Escape, the overflow fallback and the .NET value field
>   E-3 the scanner: FastScanner's regular-expression scan calls Broiler.Regex's validate entry, and DecodeBracedUnicodeEscapes and the parenthesis-balance patch are absent; the division fallback stays (decision D22)
>   E-4 the direct call: JSStringPrototype's Search member, read at the after commit, reaches a regular expression only through RegExpCreate and an Invoke of @@search, with no call of a JSRegExp member that matches (row M6, the last item of decision D20's list); the bundle cites the member's lines
> controls: one deleted member re-added to a scratch copy of the after commit, never merged; E-1 must report it; reverted and the revert checked
> MET if and only if E-1 to E-4 find nothing outside their exceptions and the control is reported.
> NOT MET otherwise.
> ```
>
> **How it is read.** Clause 4 says "The translator and the validators are gone". `RegExpUnicodeValidator` stays as a
> forwarder, its body a call of the validate entry, under decision SJL-D-25 (a), and the bundle names its body. The
> .NET `Regex` uses outside ECMAScript matching (`JSIntl.cs`, `DateParser.cs`, `FastCompiler.DeferredBody.cs`) are
> outside E-2's population; the bundle lists them as findings for their owners (decision D34).
>
> ## Revisions
>
> A rule revised after any run it judges has been seen is a new dated file in this directory, quoting this one whole
> and saying it was written afterwards; its decision is then weaker evidence. The workloads and the adversarial programs
> are not changed after they are committed; a change of Broiler.RegEx's corpus is such a revision, and the manifest then
> names another SHA-256.
>
> ## What this file does not decide
>
> - **The component clauses** (1 to 3), the library-level vectors and the reference arms: Broiler.RegEx's bundle.
> - **Broiler.VM's clauses.**
> - **Clause 7, every correction listed:** read from `Broiler.JS/CHANGELOG.md` against parts A, C and D's C1 rows, the
>   vectors' classification and this file's list of this engine's corrections.
> - **The public-API records:** `docs/public-api.md` and SJL-D-25's dispositions, with the consumer check's queries and
>   date (decision D21), read from the tree at SJL-11.5.
> - **.NET `Regex` outside ECMAScript matching** (`JSIntl.cs`, `DateParser.cs`, `FastCompiler.DeferredBody.cs`): listed
>   as findings for their owners; not SJL-11's.
> - **An invalid literal read as division:** SJL-14's, listed (JSD-0081 decision D22).
> - **The downstream suites** and **a speed claim of any kind.**
