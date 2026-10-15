# Evidence bundle SJL-6-001 (Broiler.JS) - the Date area

**Milestone:** SJL-6 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-6-date.md`: its home clauses and its
Broiler.JS clauses. **Rules:** [`decision-rule.md`](decision-rule.md) (test262 per path, part A in UTC and
part B in `America/New_York`) and [`reference-arms.md`](reference-arms.md) (the library-level comparison),
both committed at SJL-6.1 before any code they judge. **Area verdict:**
[`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", with Broiler.VM's JSD-0072 and its
correspondence from code, retained in Broiler.VM's bundle of the same name. **Owner:** the Broiler.JS
maintainer; one person holds every role (the programme's EX-30, recorded in Broiler.VM). **Reviewer:** none,
and nothing here has been read by anyone but its author.

**State:** collection in progress. Nothing here is a verdict until its section says so. **Taken:** both
test262 bases (section 1), the engine-level runs on the Date area, at corpus revisions 14 and 15
(section 2), and the reference-arm comparison, with this engine's runs on corpus revision 16, which holds
its class (b) kinds (section 3). The after runs and the rest of SJL-6.6 are added when they are taken.
Broiler.VM keeps its own bundle of the same name for its own half, judged by its own rule; neither bundle
reads the other's runs. *(2026-10-15, at SJL-6.3; the second and third sentences read "**Taken:** both
test262 bases (section 1) and the engine-level runs on the Date area, at corpus revisions 14 and 15
(section 2). The reference-arm comparison, the after runs and the rest of SJL-6.6 are added when they are
taken.")*

## 1. The test262 bases (parts A and B)

| | Part A, UTC | Part B, `America/New_York` |
|---|---|---|
| Commit | `5fef116`, the commit that carries the rule, before any code of SJL-6 landed | the same |
| Taken | 2026-10-15, 14:33Z to 16:00Z, by `.github/workflows/test262-evidence.yml` (run 38752508689), dispatched from `main` at `e8b801f` with `time_zone` empty | 2026-10-15, 14:33Z to 16:04Z, by the same workflow (run 38752514354), dispatched from `main` at `e8b801f` with `time_zone` `America/New_York` |
| Configuration | the rule's: eight shards over `--all-script-host-verifiable`, terser at its pinned version, the per-test timeout of 30 seconds, no memory limit, four workers, the suite at `ccaac100` | the same |
| Retained | [`test262/base/`](test262/base/): the workflow's artifact `test262-evidence`, every file unchanged, with [`imported.txt`](test262/base/imported.txt), which records its digest | [`test262/base-new-york/`](test262/base-new-york/), likewise, with [`imported.txt`](test262/base-new-york/imported.txt) |
| Zone | every shard in the expected zone: TZ unset, UTC +0000 at both instants, tzdata 2026c ([`host-zone.log`](test262/base/host-zone.log)) | every shard in the expected zone: TZ `America/New_York`, EST -0500 on 2021-01-15 and EDT -0400 on 2021-07-15, tzdata 2026c ([`host-zone.log`](test262/base-new-york/host-zone.log)) |
| Shell | one digest of the shell's binaries across the eight shards and the re-run ([`shell-hashes.log`](test262/base/shell-hashes.log)) | one digest, the same as part A's ([`shell-hashes.log`](test262/base-new-york/shell-hashes.log)) |
| Timed out | one path, `test/staging/sm/regress/regress-1507322-deep-weakmap.js`, in both variants; run again alone with one worker, it timed out again, and the re-run replaces its rows ([`rerun-timeouts.log`](test262/base/rerun-timeouts.log)) | the same path, with the same re-run ([`rerun-timeouts.log`](test262/base-new-york/rerun-timeouts.log)) |

The two parts' runs are never compared with each other: the rule says so. Each is compared with its own
after run, at the commit that lands SJL-6.5's cut-over, in the same zone.

## 2. Engine-level vectors: the first runs on the Date area (SJL-6.2)

Corpus revision 14 (`ff6cd24`) adds the Date area: 7,772 vectors in seven files under
`conformance/vectors/date/`, from `scripts/conformance/dateref.py`. None of them depends on the time zone,
so the engine-level runner, `Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release, was run on it twice,
from one build, once under `TZ=UTC`, as this repository's tests run, and once under
`TZ=America/New_York`, as part B runs:

| Run | Result | Files |
|---|---|---|
| Revision 14, UTC | 50,715 vectors: 50,646 match, 4 classified divergences, 65 unclassified differences | [`engine/run-revision-14-utc.log`](engine/run-revision-14-utc.log), [`engine/run-revision-14-utc.tsv.gz`](engine/run-revision-14-utc.tsv.gz) |
| Revision 14, New York | 50,715 vectors: 49,695 match, 4 classified divergences, 1,016 unclassified differences | [`engine/run-revision-14-new-york.log`](engine/run-revision-14-new-york.log), [`engine/run-revision-14-new-york.tsv.gz`](engine/run-revision-14-new-york.tsv.gz) |

The 4 are the number area's `strict-non-octal-decimal` fields, which stay with SJL-14. Every one of the 65
also differs in New York. Corpus revision 15 (`a3452d9`) classifies the 1,016, each under the row of
bundle `sjl-6-001`'s correspondence from code (Broiler.VM's `docs/evidence/sjl-6-001/correspondence.md`)
that it confirms; SJL-6.5 answers each one:

| Row | Vectors | Zones | What this engine does |
|---|---|---|---|
| L2 | 440 `date/offset`, 440 `date/to-string`, 10 `date/local-string`, `date/local/0035`, `date/date-time/0003` | New York | local time takes the zone's base offset where a date is made from local fields and read back by the local getters, and the offset in force at the instant where `getTimezoneOffset`, `toString` and some of `Date.parse`'s forms read it. Each of the 880 relation vectors that differs is an instant in daylight-saving time, and each one that matches is not; 8 of the `local-string` vectors are local dates in the years 1 to 999, before the zone's standard time, where its offset in the tzdata is local mean time |
| P1 | 8 `date/date-only`, 51 `date/utc` | New York | a date-only form for a year from 0001 to 9999 is read as local time |
| G2 | 22 `date/iso-week` | both | the ISO week of a date near the start or end of a negative year is counted with a truncating division |
| P9 | 16 `date/round-trip` | both | `Date.parse` reads none of the engine's own `toUTCString` renderings of a year below 1 or above 9999 |
| T3 | 14 `date/setter-order` | both | `setUTCMinutes`, `setUTCSeconds`, `setUTCMonth`, `setHours`, `setMinutes`, `setSeconds` and `setMonth` make the date invalid before converting their later arguments when the first is `undefined` or NaN |
| D1 | `date/utc-setter/0007`, `0010`, `0011`, `date/local-setter/0001`, `0002` | both | a setter whose valid result lies outside the years 0001 to 9999 answers NaN |
| D2 | `date/utc-setter/0008`, `0012`, `0021`, `date/local-setter/0004`, `0009` | both | a setter on a date stored outside those years answers NaN and leaves the date unchanged |
| D3 | `date/utc-setter/0019`, `date/round-trip/0021` | both | the instant 0001-01-01T00:00Z is the engine's invalid-date sentinel |
| T4 | `date/make-day/0863` | both | a month of 1e19 saturates as a 64-bit integer |

On revision 15, with the same build:

| Run | Result | Files |
|---|---|---|
| Revision 15, New York | 50,715 vectors: 49,695 match, 1,020 classified divergences, 0 unclassified differences | [`engine/run-revision-15-new-york.log`](engine/run-revision-15-new-york.log), [`engine/run-revision-15-new-york.tsv.gz`](engine/run-revision-15-new-york.tsv.gz) |
| Revision 15, UTC | 50,715 vectors: 50,646 match, 69 classified divergences, 0 unclassified differences; the 951 fields of rows L2 and P1 match there and are reported as no longer holding | [`engine/run-revision-15-utc.log`](engine/run-revision-15-utc.log), [`engine/run-revision-15-utc.tsv.gz`](engine/run-revision-15-utc.tsv.gz) |

**Every vector of revision 15 has a classified result in this engine, in both zones.** The 951 are
correct under UTC: their difference needs an offset other than zero. A field that no longer holds under
UTC is retired only when its correction is listed and the run in New York shows it gone. The negative
control ([`engine/negative-control-revision-15.log`](engine/negative-control-revision-15.log),
[`engine/negative-control-revision-15.tsv`](engine/negative-control-revision-15.tsv)) runs the 280 vectors
of `numbers/to-fixed.vectors` from a copy of the corpus in which `numbers/to-fixed/0001`'s expected value is
changed; that vector is the one verdict that moves, and it moves to a difference.

**What the runs say of the area verdict.** Every difference is a row the verdict classified, so its
falsifier does not fire, and no new record is needed. The vectors confirm rows T3, T4, D1 to D3, G2, P1, P9
and L2 for this engine, and refine two. Row T3 was worked from the code; the vectors show it for seven
setters, and `setUTCHours`, `setUTCFullYear` and `setFullYear` convert as the specification says. Row L2's
mechanism also shows before the zone's standard time, where the tzdata's offset is local mean time. Rows L3,
L4, L5, D4, D5, R3 and R4, and the fallback rows P4 to P7 beyond the numeric slash dates, have no vector:
their outcome depends on the zone, on the culture or on the host, or lies outside ECMA-262, and the
reference arms and part B's run carry them. The 22 numeric slash dates, `impl-defined`, match this engine's
expectation, its slash-date heuristic, which decision SJL-D-17 keeps.

## 3. The reference-arm comparison (SJL-6.3)

**MET**, with one judgement it states, under [`reference-arms.md`](reference-arms.md) as revised before any
run ([`reference-arms-revised-2026-10-15.md`](reference-arms-revised-2026-10-15.md)) and after the first
judged run was seen
([`reference-arms-revised-2026-10-15-after-first-run.md`](reference-arms-revised-2026-10-15-after-first-run.md),
weaker evidence on the two points it decides). The run, its first run, the three culture runs and the
negative controls are in [`reference-arms-run/`](reference-arms-run/README.md), whose README reads the MET
line clause by clause. In short: the Date area of `Broiler.ECMAScript.Algorithms` (`c69b70c`, with two
defects the first run showed fixed at `b75d12e9`) against arm J, this engine's code at `bc3597f` with its
eight other civil copies, and arm V, Broiler.VM's at `9c3d809`, over 13 population files; every difference
is a correction of an arm (class (a)) or a string outside the format the engines read differently (class
(b)), and none is unclassified. The corrections confirm rows G2, L3, L4, P8 and P9 of the correspondence for
this engine, and under the `fa-IR` and `ar-SA` cultures show a second way row P8's read-back fails, through
the culture's calendar.

**The class (b) kinds in vectors.** Corpus revision 16 (`6f29deb`) appends 12 `impl-defined` vectors,
`date/outside-format/0001` to `0012`, one or two for each of the comparison's 12 class (b) kinds. This
engine's expectation is its reading before its cut-over; Broiler.VM's is its dialect's reading as the library
ports it. The engine-level runner was run on revision 16 from one build of `6f29deb`, in both zones:

| Run | Result | Files |
|---|---|---|
| Revision 16, UTC | 50,727 vectors: 50,658 match, 69 classified divergences, 0 unclassified differences; the 951 fields of rows L2 and P1 are again reported as no longer holding | [`engine/run-revision-16-utc.log`](engine/run-revision-16-utc.log), [`engine/run-revision-16-utc.tsv.gz`](engine/run-revision-16-utc.tsv.gz) |
| Revision 16, New York | 50,727 vectors: 49,707 match, 1,020 classified divergences, 0 unclassified differences | [`engine/run-revision-16-new-york.log`](engine/run-revision-16-new-york.log), [`engine/run-revision-16-new-york.tsv.gz`](engine/run-revision-16-new-york.tsv.gz) |

The 12 new vectors match in both zones, the two read through the local fields included; every other verdict
is revision 15's. The runs were taken together, from 19:08Z to 19:31Z.

## What this bundle does not hold yet

- **The after runs** of parts A and B, at the commit that lands SJL-6.5's cut-over, and their comparisons.
- **The deletion list, Date's storage and the corrections list** (clauses 1, 2 and 5), at SJL-6.5 and
  SJL-6.6.

*(2026-10-15, at SJL-6.3: the list began with "**The reference-arm comparison**, which
[`reference-arms.md`](reference-arms.md) decides, when the area is in `Broiler.ECMAScript.Algorithms`
(SJL-6.3).")*
