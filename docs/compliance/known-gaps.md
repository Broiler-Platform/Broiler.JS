# Known compliance gaps

This file tracks areas that must be validated before Broiler.JS can make strong standards-compliance claims.

## Tracking checklist

### Measurement and reporting

- [ ] Integrate `test262` with a pinned upstream commit and publish the pass/fail totals in `dashboard.md`.
- [ ] Integrate `engine262` smoke/cross-check coverage and publish the recorded command and totals in `dashboard.md`.
- [ ] Publish CI or release artifacts for each compliance run so the dashboard links to raw logs instead of local-only output.
- [ ] Produce the comparative engine matrix called for in `dashboard.md` against V8, SpiderMonkey, JavaScriptCore, Jint, and engine262.

### Parser and execution semantics needing follow-up

- [ ] Add coverage and implementation work for `for await (...)` loops, which are still rejected in `Broiler.JS/Broiler.JavaScript.Parser/FastParser.ForStatement.cs`.
- [ ] Validate non-strict/global semantics against the compliance suites; `Broiler.JS/OtherTests/JIntPerfTests/Program.cs` still documents the engine as strict-mode only by default.
- [ ] Re-run the failing `addition` and `strict-equals` public-suite files; local regression coverage now includes unresolved reads in `+` and `===` expressions across grouped forms plus left/right operand order variations, but the original test262 mismatches still need measured confirmation.
- [ ] Re-run the failing BigInt comparison public-suite files; local regression coverage now includes mixed BigInt/Number relational semantics plus `===` precedence-adjacent cases, but the original subset parser failures still need measured confirmation.
- [ ] Verify host-defined promise job queue and async interaction behavior against public suites, not only repository unit tests; local regression coverage now includes microtask ordering, nested promise resolution, async/await continuation order, and rejection timing.

### Built-in areas with implementation but incomplete standards evidence

- [ ] Bring `Array.isArray` into line with test262 for `Array.prototype`, proxies / revoked proxies, and constructor / descriptor metadata checks.
- [ ] Validate `Intl` behavior against ECMA-402-focused suites before making internationalization compliance claims.
- [ ] Run a matching public-suite subset for `Proxy` invariants and revocation edge cases; local regression coverage now covers revoked proxies plus `get`/`set`/`ownKeys` invariants around non-configurable properties.
- [ ] Add public-suite and regression coverage for typed arrays, `ArrayBuffer`, and `DataView`, including detach/transfer edge cases.
- [ ] Run a matching public-suite subset for `RegExp`; local regression coverage now includes `RegExp.escape` divergences, flag normalization, sticky `lastIndex` behavior, and unmatched optional captures, but broader Unicode / capture edge cases still need measured suite evidence.
- [ ] Run a matching public-suite subset for error subclassing and related built-in constructor semantics; local regression coverage now covers constructor names, prototype chains, `instanceof`, and message propagation.

## 2026-05-09 local evidence

- test262 subset against Chromium: 126 executed / 1 skipped; Broiler passed 75 and failed 51 while Chromium passed all 126 executed files.
- Largest failing executed areas were `addition`, `RegExp.escape`, `strict-equals`, and `Array.isArray`.
- The repo-local `JIntPerfTests` / Dromaeo-derived script set passed 11/11 on both Broiler and Chromium, so the immediate compliance gaps are concentrated in standards edge cases rather than the basic compatibility smoke scripts.

## Gap lifecycle

Each gap should move through: documented failing suite area, minimal repro test in the appropriate `*.Tests` project, implementation fix, public suite rerun, and dashboard update.
