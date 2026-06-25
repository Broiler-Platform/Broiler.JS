# Broiler.Regex

A from-scratch, **ECMAScript-conformant** regular-expression engine for the
Broiler.JS runtime.

It exists to close the gap between the ECMAScript regular-expression grammar /
matching semantics (ECMA-262 §22.2) and what `System.Text.RegularExpressions`
can express. The existing engine
([`JSRegExp.cs`](../Broiler.JavaScript.BuiltIns/RegExp/JSRegExp.cs)) translates a
JS pattern into a .NET `Regex` through ~5000 lines of source-to-source patches.
That strategy has carried us a long way, but a class of failures is **structurally
impossible** to fix by pattern rewriting, because they are differences in the
*matching algorithm*, not the *surface syntax*.

This project is the long-term replacement for that translation layer: a direct
implementation of the spec's backtracking matcher (§22.2.2), so the hard cases
are correct *by construction* rather than by patch.

> **Status: skeleton + working core.** The parser and the backtracking matcher
> implement the common grammar and the gap cases below. Unicode property escapes
> (`\p{…}`), `v`-mode set operations, and full `Canonicalize` case-folding tables
> are stubbed with clear `TODO`s and documented limitations (see
> [Unicode/UnicodeCharSets.cs](Unicode/UnicodeCharSets.cs)). The engine is **not
> yet wired into `JSRegExp`** — see *Integration plan* below. Nothing here changes
> existing runtime behaviour.

---

## Why .NET `Regex` cannot be patched into conformance

These are the `RegExp` failures from
[issue #923](https://github.com/MaiRat/Broiler.JS/issues/923) and the reason each
is a *semantic* gap, not a syntax gap:

| # | test262 sample | .NET behaviour | Root cause |
|---|----------------|----------------|------------|
| 3 | `RegExp/lookBehind/mutual-recursive.js` | wrong / null capture | .NET matches lookbehind **right-to-left** and discards/duplicates captures differently from JS |
| 4 | `RegExp/lookBehind/back-references-to-captures.js` | `[c, abab]` vs `[c, ab]` | A back-reference *to a group captured inside a lookbehind* resolves against .NET's reversed capture, not the JS forward capture |
| 6 | `sm/RegExp/unicode-back-reference.js` | match vs `null` | Back-reference compares **code units**, not **code points**, and skips ECMAScript case-fold equivalence |
| 7 | `sm/RegExp/unicode-braced.js` | `🐸` vs `null` | `\u{1F438}` astral atom + quantifier must be **one code point**; .NET treats the surrogate pair as two units |
| 8 | `RegExp/nullable-quantifier.js` | `"a"` vs `"ab"` | ECMAScript `RepeatMatcher` **abandons** an empty-string iteration of a `min=0` quantifier; .NET keeps looping, producing a different (shorter) match |

The common thread: ECMAScript's matcher is a specific **continuation-passing
backtracker** with rules .NET's engine simply does not share — lookbehind that
matches backward *but preserves forward capture order*, an empty-match guard in
the repeat loop, and code-point-level atoms. You cannot rewrite a pattern to make
a different engine adopt these rules; you have to *be* the engine.

(Issue #923 Problems 1, 2, 5, 9 are `eval` / lexical-environment bugs, unrelated
to regex, and are out of scope for this component.)

---

## Architecture

```
pattern string ──▶ RegexParser ──▶ RegexNode AST ──▶ Matcher (CPS backtracker) ──▶ RegexMatch
                   (Parsing/)       (Ast/)            (Matching/)                   (Matching/)
```

* **`Parsing/RegexParser.cs`** — recursive-descent parser for the ECMAScript
  `Pattern` grammar (§22.2.1): disjunction, quantifiers, character classes,
  groups (capturing / non-capturing / named / inline-modifier), assertions
  (anchors, word boundaries, look-around), and escapes (including `\u{…}` and
  back-references). Produces a `RegexNode` tree.
* **`Ast/RegexNode.cs`** — the node hierarchy + `CharSet` (character-class model).
* **`Matching/Matcher.cs`** — compiles the AST to the spec's `Matcher`
  abstraction (`MatchState × Continuation → MatchState?`) and runs it. This is
  where the §22.2.2 algorithm lives, and where the gap cases are handled:
  * `Direction` (+1 / −1) threaded through compilation so a **lookbehind** body
    matches backward while its captures stay in source order (fixes #3, #4).
  * `RepeatMatcher`'s empty-iteration guard (fixes #8).
  * Code-point-aware atom and back-reference matching under `u`/`v`
    (fixes #6, #7).
* **`BroilerRegex.cs`** — the public façade (`Match` / `IsMatch`), mirroring the
  shape `JSRegExp` needs (`Success`, `Index`, `Length`, indexed + named groups).
* **`Unicode/UnicodeCharSets.cs`** — `\d \D \w \W \s \S` sets and the
  *stubbed* property-escape / case-fold hooks.

## What works today

- Literals, `.` (with/without `s`), character classes incl. ranges & negation
- Alternation, sequencing, the empty pattern
- Greedy & lazy quantifiers `* + ? {n} {n,} {n,m}` with the spec empty-match guard
- Capturing / non-capturing / **named** groups and named/numeric back-references
- Anchors `^ $` (with/without `m`), word boundaries `\b \B`
- Look-ahead and **look-behind** (positive & negative), backward-matching with
  preserved capture order
- `u`-mode code-point semantics: `\u{…}`, astral atoms as single units,
  code-point back-references
- Flags `g i m s u y d v` parsed; `i` case-folding (ASCII + simple-fold subset)

## Known limitations (stubbed / TODO)

- `\p{…}` / `\P{…}` Unicode property escapes — parsed, resolution throws
  `NotSupportedException` (needs the `Broiler.Unicode` property tables).
- `v`-mode set operations (`[a&&b]`, `[a--b]`, `\q{…}`) — parsed only.
- Full ECMAScript `Canonicalize` case-folding — current `i` folding covers ASCII
  plus a documented subset; the complete fold table is a TODO.
- Performance: the matcher is a clarity-first interpreter (per-step capture
  cloning, no DFA/JIT). Correctness first; optimisation later.

## Integration plan (not yet applied)

`JSRegExp` currently holds a `System.Text.RegularExpressions.Regex value` and
calls `value.Match(input, start)`. Adoption is incremental and low-risk:

1. Land this engine standalone with its own unit tests (this commit).
2. Add a feature flag in `JSRegExp.CreateRegex` that, for patterns using a
   gap feature (lookbehind + back-ref, nullable quantifier, astral back-ref),
   compiles a `BroilerRegex` instead of a .NET `Regex`.
3. Make `JSRegExp.Exec` / `Match` / `Split` / `Replace` consume a small common
   match-result interface implemented by both backends.
4. Grow `BroilerRegex` coverage (property escapes, `v`-mode) against test262
   until it can subsume the .NET backend entirely, then retire the translator.

See [`docs/ecmascript-mapping.md`](docs/ecmascript-mapping.md) for the
node-by-node mapping to ECMA-262 §22.2.2.
