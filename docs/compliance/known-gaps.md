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
- [ ] Add coverage and implementation work for async object accessors; `Broiler.JS/Broiler.JavaScript.Parser/FastParser.ObjectLiteral.cs` still documents async getter/setter support as missing.
- [ ] Validate non-strict/global semantics against the compliance suites; `Broiler.JS/OtherTests/JIntPerfTests/Program.cs` still documents the engine as strict-mode only by default.
- [ ] Align unresolved-reference semantics with test262 in `+` and `===` expressions; the 2026-05-09 subset run still returned values for cases such as `x + 1` and `x === 1` where Chromium threw `ReferenceError`.
- [ ] Resolve remaining parser failures in executed BigInt comparison tests, including subset failures that reported `Unexpected token StrictlyEqual` for valid `===` cases accepted by Chromium.
- [ ] Verify host-defined promise job queue and async interaction behavior against public suites, not only repository unit tests.

### Built-in areas with implementation but incomplete standards evidence

- [ ] Bring `Array.isArray` into line with test262 for `Array.prototype`, proxies / revoked proxies, and constructor / descriptor metadata checks.
- [ ] Validate `Intl` behavior against ECMA-402-focused suites before making internationalization compliance claims.
- [ ] Add public-suite and regression coverage for `Proxy` invariants and revocation edge cases.
- [ ] Add public-suite and regression coverage for typed arrays, `ArrayBuffer`, and `DataView`, including detach/transfer edge cases.
- [ ] Add public-suite and regression coverage for `RegExp` flags, captures, Unicode edge cases, and the current `RegExp.escape` escaping / descriptor divergences surfaced by the 2026-05-09 subset run.
- [ ] Add public-suite and regression coverage for error subclassing and related built-in constructor semantics.

## 2026-05-09 local evidence

- test262 subset against Chromium: 126 executed / 1 skipped; Broiler passed 75 and failed 51 while Chromium passed all 126 executed files.
- Largest failing executed areas were `addition`, `RegExp.escape`, `strict-equals`, and `Array.isArray`.
- The repo-local `JIntPerfTests` / Dromaeo-derived script set passed 11/11 on both Broiler and Chromium, so the immediate compliance gaps are concentrated in standards edge cases rather than the basic compatibility smoke scripts.

## Gap lifecycle

Each gap should move through: documented failing suite area, minimal repro test in the appropriate `*.Tests` project, implementation fix, public suite rerun, and dashboard update.
