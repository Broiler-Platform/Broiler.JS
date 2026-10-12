# The vector format, schema 1

This document is the contract every reader, validator and runner of the shared conformance corpus
implements: the home's validator and runners in this repository, and Broiler.VM's engine-level runner
over its pinned archive. A difference between two runners is never a difference in how they read a
file or render a value, because both follow this document.

## 1. Files

- A vector file has the extension `.vectors`, is UTF-8 without a byte-order mark, and uses LF line
  endings.
- A line whose first character is `#` is a comment. Comments carry no meaning.
- The first line that is not a comment reads exactly `schema: 1`.
- Records follow, separated by one or more blank lines. A record is a run of non-blank lines, each
  `key: value`: the key is everything before the first `": "`, and the value is everything after it.
- A key appears at most once in a record. An unknown key is an error.

## 2. Fields

| Key | Required | Content |
|---|---|---|
| `id` | yes | A stable identifier, unique across the corpus and never reused: lowercase letters, digits, `-` and `.`, in segments separated by `/`, the first segment the area |
| `area` | yes | One of `numbers`, `numerics`, `text`, `date`, `unicode`, `time-zones`, `intl`, `temporal`, `regexp`, `syntax`, `semantics` |
| `op` | yes | The specification operation or built-in the vector pins, for example `Number::toString` or `StringToNumber` |
| `in` | yes | The typed inputs, separated by single spaces (section 3) |
| `out` | yes | The expected outcome (section 4) |
| `class` | yes | `normative`, `impl-defined` or `divergence` (section 5) |
| `per-engine.<engine>` | for `impl-defined` | That engine's expected outcome |
| `divergence.<engine>` | for `divergence`; allowed on `impl-defined` | Why that engine is known to differ, with a link to the plan that answers it |
| `prov` | yes | Where the expected outcome comes from, as provenance entries separated by single spaces (section 6) |
| `script` | yes | One line of JavaScript source, evaluated as a Script by an engine-level runner (section 7) |
| `note` | no | Free text for a reader; it decides nothing |
| `since` | yes | The corpus revision that added the vector, a positive integer no greater than the corpus's revision ([REVISIONS.md](REVISIONS.md)) |

`<engine>` is `broiler-js` or `broiler-vm`.

## 3. Typed values

| Notation | Value |
|---|---|
| `f64:0x` and 16 uppercase hexadecimal digits | A Number, by its IEEE 754 bit pattern. **Every NaN is written `f64:0x7FF8000000000000`.** A double is never written in decimal, so no vector depends on a decimal formatter, the one under test included |
| `str:"…"` | A String, as UTF-16 code units: a code unit from U+0020 to U+007E is written as itself, except `"` written `\"` and `\` written `\\`; every other code unit, lone surrogates included, is written `\u` and four uppercase hexadecimal digits. There is exactly one way to write each string |
| `bigint:` and a decimal integer, optionally `-` | A BigInt |
| `int:` and a decimal integer, optionally `-` | An integral Number given by its value (inputs only, for arguments such as a radix) |
| `bool:true`, `bool:false` | A Boolean |
| `undefined`, `null` | Those values |
| `symbol`, `object` | A Symbol or an object (outcomes only; no identity is pinned) |

## 4. Outcomes

An outcome is one of:

- `value: <typed value>`, the value a script's evaluation completes with;
- `error: <Name>`, a thrown error object whose constructor is the named standard error: `Error`,
  `EvalError`, `RangeError`, `ReferenceError`, `SyntaxError`, `TypeError`, `URIError` or
  `AggregateError`. An early error of the script is `error: SyntaxError`;
- `throw: <typed value>`, a thrown value that is not an error object;
- `status: <word>`, a library-level status (library runners only, for example `not-implemented`).

**`out` may list alternatives**, separated by ` | `, when the specification admits more than one
result and enumerates them: the least significant digit of Number::toString, which is "not
necessarily uniquely determined", and RoundMVResult's two options beyond 20 significant digits are
the cases in the number area. A result matches the vector when it equals any alternative. A note
names the recommended alternative where the specification recommends one.

## 5. Classes

- **`normative`**: the specification determines the outcome, up to the alternatives `out` lists.
  Every engine is compared with `out`.
- **`impl-defined`**: the specification leaves the outcome to the implementation (for example an
  "implementation-approximated" result). `out` holds the ideal outcome, and each
  `per-engine.<engine>` field holds what that engine's own plan commits it to. **No per-engine
  expectation is ever observed from the engine**: it comes from the engine's plan, and the vector's
  note or provenance says which. An engine with no `per-engine` field is compared with `out`.
- **`divergence`**: the specification determines the outcome in `out`, and an engine is known to
  differ. Each `divergence.<engine>` field names one such engine, with the reason and a link to the
  plan that answers it. A divergence is a classified difference, not an expected value: the engine is
  still compared with `out`.

**An `impl-defined` vector may carry a `divergence.<engine>` field too**, when that engine is known
not to meet the expectation its own `per-engine.<engine>` field holds: the plan commits the engine to
it, and the engine does not do it yet. The field needs that `per-engine` field beside it, and it
classifies the difference exactly as on a `divergence` vector; the vector stays `impl-defined`,
because the specification still leaves its outcome to the implementation. *(Added 2026-10-12, at
corpus revision 3, after Broiler.VM's first run found the case; until then the field was allowed on
`divergence` vectors only.)*

A vector an engine fails is never deleted or reclassified to make it pass. A classification change
is a dated entry in [REVISIONS.md](REVISIONS.md), in a new corpus revision.

## 6. Provenance

| Entry | Meaning |
|---|---|
| `spec:<edition>@<revision>#<clause-id>` | A clause of the pinned specification ([sources/ecma262.pin](sources/ecma262.pin)) |
| `test262:<path>` | A test of the pinned test262 revision that asserts the outcome |
| `ref:<generator>@<version>` | An independent computation in exact arithmetic, by a generator in `scripts/conformance/` |
| `ucd:<version>/<file>` | A file of the pinned Unicode Character Database ([sources/unicode.pin](sources/unicode.pin)) |
| `observed:<engine>` | An engine was seen to produce the outcome |

**A vector's outcome must have a source independent of both engines**: at least one entry that is not
`observed:`. The validator refuses a vector whose provenance is only engine-observed.

## 7. Engine-level evaluation and the rendering rule

An engine-level runner evaluates each vector in **a fresh realm**:

1. It evaluates [`prelude.js`](prelude.js) as a Script in the realm.
2. It evaluates the vector's `script` as a separate Script, sloppy unless the script's own directive
   prologue makes it strict.
3. It renders the outcome:
   - a normal completion with value V is `value: ` followed by V in the notation of section 3, with
     every NaN as `f64:0x7FF8000000000000`, -0 as `f64:0x8000000000000000`, any symbol as `symbol` and
     any object, functions and arrays included, as `object`;
   - a thrown error object, an object with an [[ErrorData]] internal slot, is `error: ` followed by
     the `name` of its constructor;
   - any other thrown value V is `throw: ` followed by V rendered as above;
   - a script the engine refuses to parse is `error: SyntaxError`;
   - an evaluation the engine does not complete, because a host exception escaped it or because it
     was still running at the runner's time limit, is `abort: ` and a description on one line. It is
     the engine's result: it matches no outcome, and a `divergence` field may classify it;
   - a fault of the runner itself (the prelude failed, a value has no rendering) is `fault: ` and a
     description on one line. It matches no outcome, and nothing classifies it.

A runner's time limit is far above the time any vector takes to complete, so that only an evaluation
that does not terminate reaches it; the runner states its limit.

The runner renders from the engine's own value representation, in the runner's code: a Number by
its bit pattern, a String by its code units. **It never asks the engine to format the value**, so a
vector's outcome does not pass through the conversion it may be testing.

A runner's report lists, for each vector: its identifier, its class, the expected outcome (the
engine's `per-engine` field for an `impl-defined` vector naming it, otherwise `out`), the rendered
outcome, and whether they match. Line endings in a rendered outcome are normalised to LF; nothing
else is normalised.

## 8. Comparison

For a running engine E, a vector's verdict is one of:

| Verdict | When |
|---|---|
| `match` | The rendered outcome equals an alternative of the expected outcome |
| `classified-divergence` | It differs, and the vector carries a `divergence.E` field (a `divergence` vector, or an `impl-defined` one, section 5) |
| `difference` | It differs, and nothing classifies the difference |

A `match` on a vector whose `divergence.E` field names E means the divergence no longer holds; the report says so,
and the next corpus revision may retire the field by a dated entry.
