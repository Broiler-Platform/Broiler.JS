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
`--minifier terser --terser-module <package-directory>` adds a Terser-transformed
variant for each eligible script-host test; `--minifier-timeout-seconds` bounds each
transformation independently. The fixed `test262-safe-mangle-v2` profile applies
syntax minification and identifier mangling with compression disabled, and reserves
from mangling every identifier the test itself quotes (see below). The original
variant always runs.

A source Terser's own parser rejects is recorded as a not-applicable skip
(`skipKind: minifier-unsupported-syntax`), not a failure: nothing was minified, so there
is no minified variant whose result could be attributed to the engine. test262 exercises
corners a production minifier has no reason to accept — an escaped keyword used as an
IdentifierName (`break(){}`), `let` as a sloppy-mode identifier, a redeclared
`arguments`, an Annex B CallExpression assignment target, decorators, import attributes
with a trailing comma — and the engine passes all of them as written. Transformation
timeouts and internal minifier errors remain infrastructure failures.

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
already required to run Terser at all) before it is attributed to Broiler:

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

The check can only move a case OUT of the engine's column, never into it, and it never
runs on a passing case, a negative test (which passes BY failing, which an exit code
cannot distinguish), or when the reference engine is unusable — those keep the engine's
own verdict. `--no-reference-cross-check` turns it off, at the cost of reading minifier
artefacts as engine failures. The reference engine's own version rides on each
cross-checked case rather than on the run configuration, because nothing pins a runner's
Node the way the lockfile pins Terser's version.

Tests requiring `$262` host hooks remain host-harness exclusions. The `module` and `raw`
flags require separate host modes and are not validated by the ordinary script host.
Do not count excluded files as passes.

## CI and failure lifecycle

`.github/workflows/test262.yml` is the unified manual workflow. It can scope work through
`scripts/compliance/test262-assemblies.json`, path/glob subsets, and Test262 feature
metadata; shard the runnable selection; rerun saved failures first; retry an abnormal
shard once; execute original plus lockfile-pinned Terser variants by default; and
publish per-shard plus merged JSON/Markdown artifacts. It does not run automatically
after a merge or for a pull request.

Triage output is split into four focused issues: the most common normalized failure
groups, the biggest severity/impact groups, the size-ranked timeouts, and the
Terser-only failures. The last one lists only base paths whose original source passes
while the minified variant fails or times out, so a minification-specific defect is
never buried in a mixed-variant report. A source the minifier could not parse, and a
failure the reference engine attributed to the minifier, are not-applicable skips and do
not appear there; the issue body reports how many were attributed that way, so the volume
stays visible without crowding out the engine's own defects.
`terser_only_problems_limit` bounds its ranked case list, which leads with the smallest
minified body because that is the cheapest reduction.

The canonical merged JSON records the exact Broiler and test262 commits, workflow URL,
selection filters/scope, resource options, worker/shuffle settings, and runner
OS/architecture/.NET version, plus the selected minifier profile, pinned Terser
version, and transformation timeout. Cross-shard configuration drift and selections
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
the path-only manifest: an original-only pass cannot erase a Terser-only failure. If
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
