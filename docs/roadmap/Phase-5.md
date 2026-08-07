# Phase 5 — RegExp

**Target: RegExp (110×), plus PdfJS and Typescript.** Blocker **B5**. Deliberately late: it
costs one score, measured against Octane's *lowest* reference baseline — but the same engine
is on PdfJS's and Typescript's critical path.

**Every item this phase named is closed.** Its gate overturned it twice, and what is left is
an item the phase never had.

> The plan half of [`Phase-5.status.md`](Phase-5.status.md), which carries the evidence
> behind every state below. Part of the
> [performance and benchmark roadmap](Roadmap.md).

---

## What this phase is for, and how its own gate rewrote it

**The gate was: profile the Octane regex corpus *before* any rewrite**, to separate
backtracking *strategy* from per-step *interpretive overhead*. It was satisfied, and it
overturned the phase — twice.

**First overturn: the phase was aimed at the wrong engine.** `Matching/Matcher.cs` — the
closure matcher the original blocker ranking named — **is not on the Octane path at all.**
Only semantic-gap patterns route to it; the default is .NET's engine, built *without*
`RegexOptions.Compiled`. B5's ranking had never been checked against the routing.

**Second overturn: nothing aimed at matching can move this suite.** The per-call envelope
measured for item 2 says why:

| | Share |
|---|---|
| The matcher's contribution to what `re.test` **costs** | **4.6–6.5%** |
| The matcher's contribution to what `re.exec` **allocates** | **8.7–9.4%** |
| Fixed cost every regex call pays before any matching | **~2.4 µs and 2 431 B** |

The fixed cost **does not change when the subject grows 18.8×**. So the .NET compiler — and,
by the same argument, a compiled `Broiler.Regex` — cannot move this suite.

**Owner:** `BuiltIns/RegExp/`, plus `Broiler.Regex` for the component half. The component's
own work is tracked in [`Broiler.Regex/docs/roadmap.md`](../../Broiler.Regex/docs/roadmap.md);
Broiler.JS owns the integration gate.

## Items

| # | Item | State |
|---|---|---|
| **1** | Profile the Octane regex corpus before any rewrite | ✅ **satisfied — and it re-ordered the phase twice** |
| **2** | Per-match subject copy on `replace` / `exec` | ✅ landed, both builtins |
| **3** | Single-match `replace` without a builder | ✅ landed |
| **4** | The global case's retained result list | ✅ landed |
| **5** | `RegexOptions.Compiled` decided **per pattern** | ✅ **built as a race, measured, shipped switchable with the default off** |
| ~~6~~ | ~~Compile `Broiler.Regex`~~ | ⛔ **struck** — item 1's envelope refutes it by the same argument that refuted the .NET compiler |
| **7** | **The per-call envelope** — the ~2.4 µs and 2 431 B every regex call pays | ❌ **unstarted, and it is where the phase's remaining time actually is** |

### Item 5 · The per-pattern `Compiled` race — ✅ shipped, default off

**The design was chosen to need no predicate.** A `JSRegExp` counts its own matches; at the
thousandth the engine builds the compiled form, times both arms ABBA-interleaved on the
subject in hand under a 4 ms budget, and adopts the compiled one only if it wins by 1.15×.
Verdicts are cached by (pattern, options) so sibling instances of a literal inherit rather
than race again.

**The arm the race picks is unobservable** — `RegexOptions.Compiled` changes code generation
and nothing else — and `RegexTieringTests` asserts that directly: **15 cases run on both
settings and required to agree**, covering captures and their order, named groups, `lastIndex`
across a global exec loop, sticky in both directions, a backreference, `replace` with a
function, `split`, the Annex B statics, a gap-feature pattern routed to `Broiler.Regex` that
must never race, and `RegExp.prototype.compile` replacing a matcher that has already gone hot.

**Off by default (`BROILER_JS_REGEX_TIERING=1`)**, because measuring it found no speed-up
worth a retained `DynamicMethod` per hot pattern: **1.010× on 3 of 6 interleaved pairs** of
Octane's RegExp suite — the only one of fifteen suites it reaches, since eleven build no
regex at all.

**And the decision it was blocked on for months does not reproduce.** The item had been left
unshipped because one of seven real Octane patterns — an ordinary `trim` — measured **4.3×
slower compiled**, stable across three repetitions, which would kill "compile after N uses".
Re-run unchanged, **all three losing rows change sign**, and the shape in question promotes at
**5.27×** on Octane's own subject.

### Item 7 · The per-call envelope — **the only work left here**

**What it is.** Every regex call pays **~2.4 µs and 2 431 B before any matching happens**,
independent of subject size. That is 93.5–95.4% of what `re.test` costs.

**What to do.** Attribute the 2.4 µs and the 2 431 B — argument coercion, `lastIndex`
handling, match-data materialization, the result object and its properties — then remove what
is removable. `--regex-call-envelope` is the instrument and it is already built; see
[`Measurement.md` Appendix A](Measurement.md#appendix-a--reproducing-the-measurements).

**Why this and nothing else.** It is the only regex work whose ceiling is larger than the
noise floor, and it is the same envelope PdfJS and Typescript pay.

## Exit gate

- The corpus is profiled **before** any rewrite — ✅ satisfied, and it is what struck items 6
  and re-ordered the rest.
- **The integration gate Broiler.JS owns**, from [`Component.md`](Component.md): route only
  features the native engine implements and tests; compare both backends during expansion;
  move `Exec`, `Split` and `Replace` to one match-data abstraction; and **retire the .NET
  translator only after the pinned RegExp corpus is clean**.
- Any tiering stays **off by default** until it earns its retained `DynamicMethod`.
- Everything closes under [`Measurement.md`](Measurement.md).

## Dependencies

Independent of phases 1–4 and can run in parallel with any of them. Item 7 is independent of
`Broiler.Regex`'s own roadmap — the envelope is Broiler.JS's, on the .NET path.
