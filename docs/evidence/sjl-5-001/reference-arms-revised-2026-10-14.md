# Bundle SJL-5-001's reference-arm rule, revised 2026-10-14: the population's open readings, two bounds, and how the comparison writes and reads

**Written:** 2026-10-14, at milestone SJL-5.3, after the text area was written into
`Broiler.ECMAScript.Algorithms` and both arms were copied and the population generator, the comparison and
its classifier were written, and **before any run of the comparison**, void or judged: no result of S or of
any arm on the population has been seen. **Owner:** the Broiler.JS maintainer. **Reviewer:** none (the
programme's exception EX-30: every role is one person).

**What it revises, and what it does not.** The rule's block, [`reference-arms.md`](reference-arms.md)'s
```` ```rule ```` section, stays in this directory unedited, and
[`rules-in-force.log`](rules-in-force.log) shows it unchanged before any code of SJL-5.3. Its arms,
operations, arbiter, admitted classes, negative controls and MET line stand as written. This file does
three things:

1. it sets the readings the population leaves open, as the generator and the comparison implement them
   (section 1);
2. it bounds three parts of the population that cannot be run as written (section 2): pads to the
   largest maximum lengths in arm J, pads to them for every drawn string, and the relative index at every
   integer of a length of 2^32 or 2^53 - 1. It says which inputs each bound excludes and from which arm;
3. it says how results are written and compared, and how the output is read and retained (sections 3
   and 4).

**Why the evidence is weaker or not.** No run had been seen when this was written. The bounds rest on
arithmetic, not on a timing: the rule's words ask for strings of 2^30 and 2^31 - 1 code units, which .NET
cannot hold (its largest String is 2^30 - 33 code units), for pads of 2^24 code units built once for each
of 65,542 strings, and for 2^54 integers.

## 1. The population: the readings the rule leaves open

The generator is [`scripts/conformance/generate_text_population.py`](../../../scripts/conformance/generate_text_population.py).
It uses the vector generator's SplitMix64 (`generate_number_vectors.splitmix64`), which
`tests/test_reference_population.py` tests against the published reference outputs. It writes each class
as one or more gzip files, with a manifest of each file's line count and SHA-256, and states in its
docstring how a draw is read: a choice among n things is the draw modulo n, and a second value is the draw
shifted right by 16 bits, modulo its own range. Where the rule names a seed, the generator uses it.

| Class | The rule's words | Read as |
|---|---|---|
| T1 | "2^16 strings of length 0 to 64 drawn ... from a mix of ASCII, white space, the rest of the BMP, astral pairs and lone surrogates" | file T1R: a draw gives the length in code units, 0 to 64; each element is one draw, of five kinds in turn by its value modulo 5 (printable ASCII, a member of the white-space list below, a BMP unit outside ASCII and the surrogates, an astral pair, a lone surrogate); a pair that would pass the length is not taken and the next draw is |
| T1 | "every white-space character StrWhiteSpaceChar names, and U+0085, U+180E and U+200B, at either end and both of a word, singly and in runs of 1 to 3" | file T1W: each of them in runs of 1, 2 and 3 before, after, and before and after "ab" |
| T1 | the 65,536 one-unit strings and the strings of length 1 to 4 over the eleven units | files T1U and T1E, as written |
| T1 | "every index from -1 to the length + 1" | for CodePointAt, every one; for AdvanceStringIndex, from 0: no engine calls it below zero, since its index is `lastIndex` after `ToLength` |
| P1 | "strings of lengths 0 to 5" | `""`, `"a"`, `"ab"`, `"abc"`, `"abcd"` and `"abcde"` (file P1S); with T1R's drawn strings |
| P1 | "targets from 0 to 70, and each maximum length ±1" | every target from 0 to 70 for every string; each maximum length minus one, itself and plus one, above 70, for the six short strings (section 2) |
| P1 | "maximum lengths 0, 1, 16, 2^24, 2^30 and 2^31 - 1" | the policies S runs under (file P1M). **S is compared with an arm under that arm's policy**: arm V with S under 2^24, Broiler.VM's `StringLengthCeiling`; arm J with S under 2^31 - 1, `int.MaxValue`. Under 0, 1, 16 and 2^30, S is compared with S under 2^31 - 1, and must answer the same, or refuse exactly where the pad is longer than the maximum, the String shorter than the pad and the filler not empty. Those four comparisons are listed as variants of arm "S"; a difference there is a defect of S, class (c) |
| P1 | the fillers | `""`, `" "`, `"ab"`, `"\uD800"`, `"\uDC00\uD800"` and `"0123456789"` (file P1F), each at the start and at the end |
| P1 | the relative index's "every integer from -(length + 2) to length + 2" | for the lengths 0, 1, 2 and 7, every one; for 2^32 and 2^53 - 1, those within two of -length, of zero and of length (section 2); with -Infinity, -(2^53), 2^53 and +Infinity throughout (file P1X) |
| S1 | "2^14 templates of length 5 to 40 drawn ... over the same characters and the group names a, b and xyz" | file S1R: a draw gives the length; each draw adds one of fourteen tokens, the eleven characters and the three names, and the template is cut at its length |
| S1 | "every capture defined, undefined or empty in turn" | three configurations: every capture defined, the n-th as `"c<n>"`; every capture undefined; every capture empty. With no captures, one |
| S1 | "against the groups a and b with a defined and with b undefined" | `{ a: "ga", b: undefined }`, an absent name read as undefined |
| S1 | "a match at the start, in the middle and at the end of its subject" | the match `"ab"` in the subjects `"abzw"`, `"xyabzw"` and `"xyab"` |
| U1 | the decode inputs | file U1D: every `%XX` in both letter cases; every sequence of two, three and four octets whose lead is 80 to FF and whose continuations are drawn from the rule's ten, whole and cut after each octet; every `uriReserved`, `#` and `uriUnescaped` character raw and escaped in both cases; and `%` with zero, one or two characters of which none, one or both are hexadecimal |
| U1 | "unescape's inverse" over every code point | each code point's String written as `escape` writes it (`%XX` below 256, `%uXXXX` above, unit by unit), and unescaped; with file U1N's every `%uXXXX` in both cases and the short ones |
| U1 | "2^16 strings ... mixing escapes, raw text and malformed escapes" | file U1R: a draw gives the element count, 0 to 24; each element is a draw of eight kinds (raw ASCII, a raw BMP unit, an astral pair, a lone surrogate, a `%XX`, a UTF-8 sequence's escapes, a `%uXXXX`, a malformed escape) |
| B1 | "2^14 strings of length 5 to 120 drawn ..., most of them valid base64 of either alphabet with white space and padding mutated by one character" | file B1R: drawn bytes encoded in a drawn alphabet, padded or not by a draw, with zero to three drawn white-space characters put in, and then, three times in four, one of the fourteen characters inserted, one character deleted, or one replaced; a result outside 5 to 120 code units is drawn again |
| B1 | "unbounded" | a destination of `DecodedLengthBound(length)` bytes for S, which no decode fills; `int.MaxValue` for arm J's `DecodeBase64` and `fromHex`; `long.MaxValue` for arm V's, as each engine's `fromBase64` and `fromHex` pass |
| J1 | "2^14 documents drawn ... from the JSON grammar at depths 1 to 600, each also mutated" | file J1R: a draw gives the depth; the document is a chain of that many arrays and objects, each with drawn primitive siblings, and each document is followed by its mutation by one of the eighteen characters |
| J1 | "each validated at the depth limits 64, 512 and 1024" | S at 64 alone; S at 512 against arm V's `JSON.parse`; S at 1024 against arm V's module translation; and S with no limit against arm J, whose depth is System.Text.Json's and which no limit of S reproduces (correspondence row J1) |
| J1 | "for the gap, the numbers ... and strings of every length from 0 to 12" | file J1G: the ten numbers, and the first 0 to 12 code units of `"0123456789\tB"` |

**TrimString has no arm.** Neither arm's list copies a trim: both engines' trim already reads the
library's `StrWhiteSpaceChar` set, Broiler.JS's through `TrimStrWhiteSpace` itself. S runs over T1 at each
end and both, and its count is reported; nothing is compared.

## 2. Bounds

| Bound | Inputs excluded | From | Why |
|---|---|---|---|
| Pads to a maximum length ±1 only for the six short strings | the drawn strings at targets above 70 | S and every arm | a pad of 2^24 code units built for each of 65,542 strings, five fillers and two placements is some ten terabytes of writes |
| Arm J's pad to more than 2^24 + 1 code units | the targets 2^30 - 1 to 2^31 | arm J | its builder allocates up to `int.MaxValue` code units, past .NET's largest String; correspondence row P4 says what it does there, and the vectors show it. S is still run there, and refuses past .NET's largest String |
| The relative index at every integer of the large lengths | all but those within two of -length, 0 and length | S and every arm | 2^33 and 2^54 integers |
| Arm J's `Relative` and `ClampRelativeIndex`, and arm V's `StringRelativeIndex`, above `int.MaxValue` | the lengths 2^32 and 2^53 - 1 | those three members | they take an `int` length; arm V's `ArrayRelative` takes every length and runs there |

## 3. How the comparison writes and reads

The comparison is `Broiler.ECMAScript.Algorithms.Tests`, run as `compare-text`
(`ReferenceArms/Text/`). **A variant is S called as that arm's engine will call it**, after its cut-over:
where the engine's call site does more than call the member, S's side repeats those lines - `codePointAt`'s
one-unit charge before it reads, `padStart`'s answer before it converts the filler, and the evaluation of a
template's parts, which charges each expansion its length plus one as Broiler.VM's `RegExpAppend` does.

A result is written as text and compared exactly: a String by its code units with every character outside
`!` to `~` escaped; a number by its value; a byte array by its bytes; a decode as what it read, the bytes it
wrote and its error; a parsed JSON value by its structure, its strings by their code units and its numbers
by their bit patterns, and an object's members in the order either engine enumerates them (array-index
keys ascending, then the others in their order of creation); a refusal by its error's constructor. **A
variant of arm V carries its total charge after a bar**, and S under arm V's policy carries its own, so a
charge that differs is a difference. An arm's exception that is not an engine error is written by its
type.

The run writes `summary.json` and `differences.tsv.gz`, as bundle `sjl-4-001`'s did. **It classifies
nothing.** [`scripts/conformance/classify_text_arms.py`](../../../scripts/conformance/classify_text_arms.py),
written with this file and before the judged run, classifies each difference against `textref.py`, the
corpus's reference computation, and writes `classification.tsv.gz`, `classification.json` and `report.md`.
The negative controls are `compare-text --controls`: arm V's `GlobalUriEncode` with one more unit charged
for U1C's `"A"`, and arm J's `DecodeBase64` with the first byte changed for B1E's `"AAAA"` in the standard
alphabet, loose, unbounded. Each must be reported as exactly one difference, and the classifier must leave
it unclassified.

**The culture.** Every thread of the judged run is under the invariant culture. The rule's second run of
arm J's string operations is the whole comparison again with `--culture en-US`, whose comparison under ICU
treats U+000D U+000A as one element; its differences are compared with the first run's, and a difference
only one run has is reported.

## 4. Retention

As the rule says: `summary.json`, `classification.json`, `report.md`, the population's `manifest.json`, the
negative controls' runs and the commands of every step are retained in
[`reference-arms-run/`](reference-arms-run/); the two listings are not committed, and the bundle records
each one's SHA-256 and line count. A run with a line limit or an operation filter is void.
