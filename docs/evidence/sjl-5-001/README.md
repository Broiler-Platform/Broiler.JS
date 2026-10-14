# Evidence bundle SJL-5-001 (Broiler.JS) - the text area

**Milestone:** SJL-5 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-5-text-algorithms.md`: its home clauses and
its Broiler.JS clauses. **Rules:** [`decision-rule.md`](decision-rule.md) (test262 per path) and
[`reference-arms.md`](reference-arms.md) (the library-level comparison), both committed at SJL-5.1 before
any code they judge, the latter revised before any run by
[`reference-arms-revised-2026-10-14.md`](reference-arms-revised-2026-10-14.md); their state before the
first code of the area is [`rules-in-force.log`](rules-in-force.log). **Area verdict:**
[`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", with Broiler.VM's JSD-0070.
**Owner:** the Broiler.JS maintainer; one person holds every role (the programme's EX-30, recorded in
Broiler.VM). **Reviewer:** none, and nothing here has been read by anyone but its author.

**State:** collection in progress. Nothing here is a verdict until its section says so. **Taken:** the
test262 base (section 1) and the first engine-level runs on the text area (section 2). The reference-arm
comparison, the after run and the rest of SJL-5.6 are added when they are taken. Broiler.VM keeps its own
bundle of the same name for its own half, judged by its own rule; neither bundle reads the other's runs.

## 1. The test262 base (part A)

| | |
|---|---|
| Commit | `3309b15`, the commit that carries the rule, before any code of SJL-5 landed |
| Taken | 2026-10-14, 16:49Z to 18:16Z, by `.github/workflows/test262-evidence.yml` (run 38601306943), in the rule's configuration: eight shards over `--all-script-host-verifiable`, terser at its pinned version, the per-test timeout of 30 seconds, no memory limit, four workers |
| Retained | [`test262/base/`](test262/base/): the workflow's artifact `test262-evidence`, every file unchanged, with [`imported.txt`](test262/base/imported.txt), which records its digest |
| Shell | one digest of the shell's binaries across the eight shards and the re-run ([`shell-hashes.log`](test262/base/shell-hashes.log)) |
| Timed out | one path, `test/staging/sm/regress/regress-1507322-deep-weakmap.js`, in both variants; run again alone with one worker, it timed out again, and the re-run replaces its rows ([`rerun-timeouts.log`](test262/base/rerun-timeouts.log)) |

The comparison with the after run is added when the after run is taken, at the commit that lands
SJL-5.5's cut-over.

## 2. Engine-level vectors: the first runs on the text area (SJL-5.2)

The engine-level runner, `Broiler.JS/Broiler.JavaScript.Vectors.Tests` in Release, read **no vector** of
corpus revision 10: its validator refused the four clause ids with `%` that the text area cites
([`engine/run-revision-10.log`](engine/run-revision-10.log)). Revision 11 widens the schema's clause ids
and changes no vector. On it, at `753d42e`
([`engine/run-revision-11.log`](engine/run-revision-11.log),
[`engine/run-revision-11.tsv.gz`](engine/run-revision-11.tsv.gz)): **42,943 vectors, 42,897 match, 4
classified divergences, 42 unclassified differences.** The 4 are the number area's
`strict-non-octal-decimal` fields, which stay with SJL-14. The negative control
([`engine/negative-control-revision-11.tsv`](engine/negative-control-revision-11.tsv)) runs the 280
vectors of `numbers/to-fixed.vectors` from a copy of the corpus in which `numbers/to-fixed/0001`'s
expected value is changed. That vector is the one verdict that moves, and it moves to a difference.

Corpus revision 12 classifies the 42, each under the row of bundle `sjl-5-001`'s correspondence from code
(Broiler.VM's `docs/evidence/sjl-5-001/correspondence.md`) that it confirms. SJL-5.5 answers each one:

| Row | Vectors | What this engine does |
|---|---|---|
| B1 | 6 `text/set-from-hex` | `setFromHex` parses its whole input before honouring the destination |
| B2 | `text/options/0001`, `0002`, `0006` | an options argument that is not an object is ignored |
| B3 | `text/options/0003`, `0004` | a `null` option is read as the default |
| B4 | `text/options/0009` to `0012` | a detached array encodes as empty or decodes nothing |
| J1 | `text/parse-depth/0011` to `0022` | nesting past 64 is refused |
| J2 | `text/parse/2364`, `2365`, `text/parse-surrogate/0001`, `0003` | an escaped lone surrogate is refused |
| J3 | `text/parse/2367`, `2368`, `text/parse-surrogate/0002`, `0004` | a raw lone surrogate lets a .NET exception through |
| J6 | `text/raw-json/0005`, `0006` | a look-alike object is taken for raw JSON |
| J7 | `text/raw-json/0003` | the brand's property is not enumerable |
| P5 | `text/last-index-of/0002` | `fromIndex` is ignored with a third argument |
| S2 | `text/named-to-string/0001` | a named group's value is converted without `Symbol.toPrimitive` |
| S3 | `text/absent-replace/0001`, `0002` | a RegExp whose `Symbol.replace` is absent is matched as a pattern |

**The vectors refute row J4**: `JSON.parse("1e400")` and `JSON.parse("-1e400")` answer the infinities,
as the specification gives them (`text/parse-number/0001`, `0002`). The plan's record of the corrections
says so in a dated correction. Rows P4, X1 and X2 have no vector, and row J5's three vectors match because
the run was on Linux. Every difference is one the area verdict classified, so its falsifier does not
fire. The run on revision 12 is added to this section when it is taken.
