# Compliance process

Broiler.JS compliance claims require repository tests plus a pinned public-suite run.
Every published result must identify the Broiler commit, suite revision, host mode,
selection, command, environment, totals, and raw artifact.

## Required evidence

| Evidence | Purpose |
| --- | --- |
| `dotnet test Broiler.JS.slnx` | Repository unit, architecture, integration, and regression tests |
| test262 at a commit SHA | ECMAScript and ECMA-402 conformance |
| `scripts/compliance/engine-scenarios.json` | Small Broiler/Node/engine262 semantic cross-check |
| `benchmarks/Broiler.JavaScript.Engine.Benchmarks` | Repository compatibility and legacy performance scenarios, over the `OtherTests/JIntPerfTests/Scripts` corpus |

test262 is the release conformance source. The other suites are useful cross-checks and
must not be added together into a synthetic “percent compliant” figure.

## Audit a suite revision

```powershell
python scripts/compliance/audit_test262.py `
  --suite-ref <sha> `
  --manifest-glob "scripts/compliance/test262-*.txt"
```

Pass `--suite-root <checkout>` to reuse a local checkout. The audit reports discovered
tests, runnable host modes, blocker counts, and manifest coverage.

## Run focused and full selections

Focused manifest:

```powershell
python scripts/compliance/run_test262.py `
  --suite-ref <sha> `
  --path-file scripts/compliance/test262-properties-proxy.txt
```

Full script-host-verifiable selection:

```powershell
python scripts/compliance/run_test262.py `
  --suite-ref <sha> `
  --all-script-host-verifiable `
  --shard-count 8 `
  --shard-index 0
```

Use `--shard-index -1` to run every shard locally. The runner supports async and
`noStrict` files, `onlyStrict`, semicolon-separated `--subset` path/glob filters,
Test262 metadata filters through `--features` and `--feature-match`, per-test timeout,
optional POSIX memory limits, `--max-workers`, `--shuffle-seed`,
`--prioritize-fragile`, and expected-error handling through `--include-negative`.
`--minifier` adds one minified variant for each eligible script-host test;
`--minifier-timeout-seconds` bounds each transformation independently, and the original
variant always runs. Two manglers are available, one per run:

| `--minifier` | Module flag | Profile | Transformation |
| --- | --- | --- | --- |
| `terser` | `--terser-module` | `test262-safe-mangle-v2` | Syntax minification and identifier mangling, compression disabled |
| `closure` | `--closure-module` | `test262-closure-advanced-v1` | Closure Compiler `ADVANCED_OPTIMIZATIONS` against the ES2026 standard |

Both reserve from renaming every identifier the test itself quotes (see below).

The Closure profile is the aggressive one on purpose: `ADVANCED_OPTIMIZATIONS` renames
properties as well as variables, removes what it proves unreachable, inlines, and
collapses namespaces, which is the shape a real ADVANCED-compiled production bundle
ships. Two consequences follow, and both are expected rather than defects. First, far
more cases stop being the test they were — anything that reads a property name back as a
string, or asserts the `name` a binding gave a function, now measures the compiled
program — and the reference-engine cross-check below moves each of those to a
`minifier-changed-semantics` skip instead of an engine failure. Second, it is slow: the
compiler is a fresh process per test (roughly a second and a half each on CI hardware,
against Terser's persistent worker), so a Closure run costs substantially more wall-clock
than a Terser one at the same shard count.

One consequence of dead-code removal is worth reading reports with in mind: a minified
body is only evidence while it still contains the assertions. Calls into the harness are
calls into externs, which Closure must assume have side effects, so the assertions
themselves survive — but `minifiedSourceSizeBytes` and `minificationRatio` are recorded on
every case, and a case whose compiled body collapsed to almost nothing deserves a look
before it is read as a pass.

Closure has no `ECMASCRIPT_2026` language mode. Its flag parser accepts year names only
up to `ECMASCRIPT_2022`, which the compiler itself then refuses, and it calls the current
draft standard `ECMASCRIPT_NEXT` — "latest features supported". `ECMASCRIPT_NEXT` is
therefore what the profile passes for both `--language_in` and `--language_out`, and the
report records `ecmaScriptYear: 2026` alongside it so the standard that was asked for is
in the artifact.

The harness files a test includes — `assert.js`, `sta.js`, and everything in its
`includes:` metadata — are handed to Closure as externs. The runner concatenates the
harness around the compiled body rather than compiling them together, so within the
compilation unit `assert.sameValue` is a property of a free name that `ADVANCED` would
otherwise rename; declaring the pinned suite's own harness as externs protects exactly
that surface without a hand-maintained list that would rot against the suite.

**The host the compiled body runs on is declared the same way, and the engine is what
declares it.** ADVANCED renames every property name its externs do not mention, and the
externs Closure ships describe the standard surface its own release knows about — which
is not this standard. `Temporal.Duration` compiles to `Temporal.h`, `Iterator.prototype`'s
helpers to one-letter names, and the resulting program cannot reach the API the test is
about, so the engine correctly reports that `undefined` is not a constructor and a
conformance report reads it as the engine having lost Temporal. A production ADVANCED
build answers this with externs for its host; so does this profile. Before the first
compile of a shard, the runner has the engine under test walk its own globals — every own
property name reachable from `globalThis` through data properties and prototypes, with
accessors named but never invoked — and writes the result as an externs file that every
compilation gets (`minifierHostExternNames` records how many names that was). Asking the
engine rather than keeping a list is the same argument as the harness: a list is wrong the
day the engine implements one more builtin, and wrong in the direction that reports the
new builtin as a defect. A run whose engine cannot answer does not start, because a
Closure run without those externs measures a program that never reaches the engine.

What this does not cover is an option name the host reads off an object the *test* builds:
`{ smallestUnit: "second" }` is renamed to `{ a: "second" }` because nothing in the host's
own object graph is called `smallestUnit`. That program is broken in every engine — it is
the ADVANCED hazard that record-type externs exist to answer in production — so the
cross-check below is what settles those, whenever the reference engine implements the
feature the test needs. That proviso used to exclude Temporal, which is where most of
these tests are; the cross-check now asks the reference engine to switch Temporal on, and
what remains excluded is the feature *behind* the feature — V8's Temporal aborts on a
non-ISO calendar, so the `intl402` cases keep coming back with no opinion.

A source the minifier's own parser rejects is recorded as a not-applicable skip
(`skipKind: minifier-unsupported-syntax`), not a failure: nothing was minified, so there
is no minified variant whose result could be attributed to the engine. test262 exercises
corners a production minifier has no reason to accept — an escaped keyword used as an
IdentifierName (`break(){}`), `let` as a sloppy-mode identifier, a redeclared
`arguments`, an Annex B CallExpression assignment target, decorators, import attributes
with a trailing comma — and the engine passes all of them as written. Closure declines a
further set of its own (decorators, `using`, auto-accessors, private `#x in o`, the
RegExp `v` flag), each reported the same way.

A minifier that *crashes* on a source it accepted is the same fact with a different
diagnostic, and is its own kind (`skipKind: minifier-internal-error`). Closure walks
`for (x.y of [23])` — a MemberExpression as a for-of target, which the grammar allows and
every engine runs — into a `NullPointerException` inside `RemoveUnusedCode`, and reaches
an "AST should not contain Dynamic module import" assertion on the `import(specifier)` a
script may contain. Neither produced a minified body, so neither says anything about the
engine. What separates that from a broken harness is what the compiler named as the node
it died on: the crash has to name the test's own source and nothing else. A crash that
names an externs file, or names no source at all, is this runner being wrong about the
compilation rather than the compiler being wrong about the test, and it — along with every
transformation timeout — remains an infrastructure failure.

Syntax the minifier neither rejects nor preserves is the same skip. Terser 5 has no
auto-accessor: it reads `class C { accessor #x = 1; }` as a field named `accessor`
followed by a private field and prints `class C{accessor;#x=1}`, which parses and runs
and is not the test. The runner recognises the rewrite — the source declares an
auto-accessor and the output really did split it into a bare `accessor` element — so the
case is not applicable, and the day Terser learns the syntax the recognition stops firing
on its own.

Mangling can also change what a test measures, which is a limit of the variant rather
than a defect either side. A test whose subject IS binding identity — the separate
parameter scope in `scope-*-paramsbody-var-open`, the Annex B early-error condition in
`block-decl-func-skip-early-err-*`, a private name reached from a direct eval — is
measuring a relationship between two names that the mangler renames independently. The
minified program is then a different program, and every engine rejects it. Terser also
sometimes emits source that is not JavaScript at all: it prints `if (false) let \n {}` as
`if(false){let{}}`, unescapes `for (async of [7])` into the `for (async of …)` the
grammar forbids, and drops the parentheses from `import((1, 0, "./m.js"))`.

**Where the test names the binding, the profile keeps it instead.** Every `fn-name-*`
case asserts the name an anonymous function inherited from its binding —
`assert.sameValue(fn.name, 'fn')` — so mangling `fn` to `e` leaves the assertion
comparing against a name the program no longer contains. Any identifier-shaped quoted
spelling in a test's own source is therefore held back from mangling, and those cases
measure what they were written to measure while everything they do not name is still
mangled. Over-reserving costs one mangling opportunity and nothing else.

A test can also hard-code the source text of code it declares —
`assert.sameValue("function* g() { yield 1; }", g.toString())` in
`staging/sm/generators/runtime.js` — without naming `Function.prototype.toString` for the
path prefix or the pattern to key on. A quoted function or class definition that
reappears as code in the same file, with a `toString` standing next to it, is that test:
minification rewrites source text by definition, so there is no minified variant of it
either.

**The runner settles both automatically, by asking a second engine.** A minified case that
FAILS is re-run under the reference engine (`--reference-engine-bin`, Node by default —
already required to run either mangler at all) before it is attributed to Broiler:

| Reference engine | Attribution |
| --- | --- |
| parses the original, cannot parse the minified body | not applicable, `skipKind: minifier-invalid-output` |
| passes the original, fails the minified body | not applicable, `skipKind: minifier-changed-semantics` |
| passes both | engine failure, `referenceCrossCheck: engine-divergence` |
| cannot run the original either | engine failure, `referenceCrossCheck: inconclusive` |

Reading the minified body needs no support for what the test *does*, only for the grammar
it is written in, so the first row asks the parser and not the runtime: Node never
resolves the fixture `dynamic-import/assignment-expression/cover-parenthesized-expr.js`
imports, and still rejects the three-argument `ImportCall` Terser printed for
`import((1, 0, "./m.js"))`. The original is the control — an engine that cannot parse
THAT either is refusing the test's own syntax and has said nothing about the minifier.

The last row is the expensive one, because it is the row that keeps a case attributed to
Broiler, so the reference engine is given the test as test262 wrote it: a script, evaluated
in the global scope. Node run over a file does not do that — it wraps a CommonJS file in a
function body, where a top-level `var` is a local of the wrapper rather than a property of
the global object — and every test whose subject IS that distinction then fails under Node
no matter how correct Node is. The Annex B eval-code cases declare a function inside
`(0,eval)(…)` and read the binding back from the enclosing scope; `S15.3_A3_T2` asks a
`Function`-constructed body for `this.x`. Each of those originals fails inside the module
wrapper, and each therefore comes back "cannot run the original either".
`reference_host.cjs` compiles the program with `vm.runInThisContext` instead, which is the
scope the test was written for, and its `--check` is `vm.Script` — the script-mode parse
that `node --check`, which parses a module, is not.

**A strict test is assembled strict, on both sides.** An `onlyStrict` test's body does not
carry its own directive: the assembler is what puts `"use strict";` at byte zero, exactly
as it does for the program Broiler was handed. Telling the assembler the body already had
one ran the ORIGINAL of every `onlyStrict` test sloppy on this side of the comparison, so
the reference engine failed each test whose subject IS strict mode — `gNonStrict.caller`
throws only inside it — and each came back "cannot run the original either", which is the
row that keeps a case attributed to Broiler.

**A feature the reference engine hides is asked for rather than worked around.** The check
speaks only about a case whose original it can run, so a feature switched off is a blind
spot that answers "no opinion" and leaves that whole area attributed to Broiler — and for
a Closure run that area is most of the report, because the option name a Temporal test
writes into an object literal is precisely what ADVANCED renames. V8 has Temporal,
ShadowRealm, `Intl.DurationFormat`, and `Float16Array`; it keeps them behind flags. The
runner probes each flag against the binary with the same `--version` call that already had
to succeed, passes the ones it accepts, and records them on every cross-checked case
(`referenceEngineOptions`) next to the version. A flag the engine does not have costs an
opinion and nothing else, which is the safe direction: a missing opinion leaves the failure
reported against the engine rather than suppressed. What stays out of reach is a feature
the reference engine has but has not finished — V8's Temporal aborts on a non-ISO calendar,
so the `intl402` Temporal cases are still inconclusive.

The check can only move a case OUT of the engine's column, never into it, and it never
runs on a passing case, a negative test (which passes BY failing, which an exit code
cannot distinguish), or when the reference engine is unusable — those keep the engine's
own verdict. `--no-reference-cross-check` turns it off, at the cost of reading minifier
artefacts as engine failures. The reference engine's own version and flags ride on each
cross-checked case rather than on the run configuration, because nothing pins a runner's
Node the way the lockfiles pin the manglers' versions.

Tests requiring `$262` host hooks remain host-harness exclusions. The `module` and `raw`
flags require separate host modes and are not validated by the ordinary script host.
Do not count excluded files as passes.

## CI and failure lifecycle

`.github/workflows/test262.yml` is the unified manual workflow. It can scope work through
`scripts/compliance/test262-assemblies.json`, path/glob subsets, and Test262 feature
metadata; shard the runnable selection; rerun saved failures first; retry an abnormal
shard once; execute original plus lockfile-pinned Terser variants by default (or Closure
variants with `minifier: closure`); and publish per-shard plus merged JSON/Markdown
artifacts. It does not run automatically
after a merge or for a pull request.

Triage output is split into four focused issues: the most common normalized failure
groups, the biggest severity/impact groups, the size-ranked timeouts, and the
minifier-only failures (titled for whichever mangler ran). The last one lists only base
paths whose original source passes
while the minified variant fails or times out, so a minification-specific defect is
never buried in a mixed-variant report. A source the minifier could not parse, and a
failure the reference engine attributed to the minifier, are not-applicable skips and do
not appear there; the issue body reports how many were attributed that way, so the volume
stays visible without crowding out the engine's own defects.
`minifier_only_problems_limit` bounds its ranked case list, which leads with the smallest
minified body because that is the cheapest reduction.

The canonical merged JSON records the exact Broiler and test262 commits, workflow URL,
selection filters/scope, resource options, worker/shuffle settings, and runner
OS/architecture/.NET version, plus the selected minifier, its profile and exact option
set, its pinned version, and the transformation timeout. Cross-shard configuration drift and selections
that run no tests are configuration failures rather than green results. A terminal
verdict uses this authoritative merge, so a recovered retry can heal an initial job
failure while a missing full phase cannot pass accidentally.

`scripts/compliance/test262-failures.txt` is generated from tracked failures and
timeouts. CI refreshes only paths conclusively executed by the authoritative phase;
out-of-scope, skipped, cancelled, and incomplete-shard entries are preserved. A path
may be removed only after:

1. a minimal repository regression exists;
2. the focused public-suite reproduction passes;
3. the affected full shard passes; and
4. the dashboard is updated with the new evidence.

Manifest persistence consumes the canonical merged artifact, is serialized per branch,
and uses a compare-and-swap push. Only the canonical Terser-enabled profile may clear
the path-only manifest: an original-only pass cannot erase a Terser-only failure, and a
Closure run measures a different transformation entirely, so it never writes the
baseline. If
source files changed after the tested commit, the workflow leaves the newer branch
untouched instead of applying stale measurements.

Treat a newly failing previously-passing test as a regression unless a pinned suite
update intentionally changed the expectation.

## Cross-engine comparison

```powershell
python scripts/compliance/compare_engines.py `
  --manifest scripts/compliance/engine-scenarios.json `
  --engine262-bin <path-to-engine262>
```

Record engine versions and do not treat agreement between engines as a replacement for
the specification or test262.

## Reporting

Every result published in `dashboard.md` must include:

- Broiler commit and dirty state;
- suite name and exact revision;
- OS, architecture, .NET version, and relevant host options;
- selected host mode, paths/filters, shard count/index, worker count, and shuffle seed;
- discovered, selected-before-sharding, passed, failed, skipped, unsupported, and
  timed-out totals;
- blocker counts for `$262`, `module`, `raw`, or other exclusions, noting overlaps;
- the highest-impact failure buckets and follow-up issue/owner; and
- raw log or CI artifact location.

Large upstream suites stay outside the source tree or in CI caches. Do not vendor them
without an explicit license and update policy.
