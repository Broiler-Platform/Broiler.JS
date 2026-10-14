# Bundle SJL-4-001's reference-arm rule, revised 2026-10-13: the population's open counts, two bounds on arm J, and how the comparison writes and reads

**Written:** 2026-10-13, at milestone SJL-4.3, **after** the numerics area landed in
`Broiler.ECMAScript.Algorithms` (`2b0a16b`), after both arms were copied and the population generator
and the comparison were written, and **before any run of the comparison**, void or judged: no result
of S or of any arm on the population has been seen. **Owner:** the Broiler.JS maintainer.
**Reviewer:** none (the programme's exception EX-30: every role is one person).

**What it revises, and what it does not.** The rule's block, [`reference-arms.md`](reference-arms.md)'s
```` ```rule ```` section, stays in this directory unedited. Its arms, operations, arbiter, admitted
classes, negative controls and MET line stand as written. This file does three things:

1. it sets the counts and readings the population leaves open, as the generator implements them
   (section 1);
2. it bounds two parts of the population that cannot be run in practicable time (section 2): arm J's
   quadratic BigInt text and parse, and powers whose result runs to hundreds of millions of bits. It
   says which inputs each bound excludes and from which arm. S and arm V run the whole population;
3. it says how results are written and compared, and how the output is read and retained
   (sections 3 and 4).

**Why the evidence is weaker or not.** No run had been seen when this was written. The two bounds of
section 2 rest on two timings taken outside the comparison, on single inputs, and on no result of the
comparison:

- `BigInteger.Pow` of a 1,678-bit base to the 262,160th power, a 439,642,321-bit result, took 404 s;
  one of the population's drawn pairs is a 1,678-bit base to the 524,288th power;
- arm J's radix conversion took 2.55 s for one 70,000-bit value at radix 2 (0.97 s at radix 3, 0.16 s
  at radix 36). It is copied from `JSBigInt.BigIntegerToRadixString`, which calls `BigInteger.DivRem`
  and `StringBuilder.Insert(0, …)` once per digit.

At those costs, the population as first written runs for many hours in arm J, and in S under the
unbounded policy.

## 1. The population: the counts and readings the rule leaves open

The generator is [`scripts/conformance/generate_numerics_population.py`](../../../scripts/conformance/generate_numerics_population.py).
It uses the vector generator's SplitMix64 (`generate_number_vectors.splitmix64`), which
`tests/test_reference_population.py` tests against the published reference outputs. It writes each
class as one gzip file, with a manifest of each file's line count and SHA-256. Where the rule names a
seed, the generator uses it. Where the rule says "drawn" without a count, or leaves a set to be read,
the generator takes the following:

| Class | The rule's words | Read as |
|---|---|---|
| N1 | "every power of two from 2^-1074 to 2^1023 with its next-lower and next-higher doubles" | positive powers, as the words say; the specials and the k-group are at both signs. The fixed part is de-duplicated, and then come the 2^20 drawn doubles, as drawn |
| N1 | "254.5, 255.5, 256, 65504, 65519.99, 65520 and their neighbours" | each with the next-lower and next-higher double |
| N2 | the NaN patterns' "highest, lowest and middle payload bits" | fraction bits 50, 0 and 25 (bit 51 is the quiet bit); every combination but the one with no fraction bit, which is an infinity |
| N2 | "every midpoint between adjacent Float16 values" | between adjacent finite values at both signs, and 65,520, the midpoint above the largest |
| N3 | the special set's "±2^53 + 1" | ±2^53 and ±(2^53 + 2), the doubles either side of 2^53 + 1, which is not a double |
| N3 | hypot's 2^16 drawn tuples, "with tuples whose terms share an exponent and tuples whose exponents differ by more than 1000" | in turn: terms drawn from N1; terms sharing one drawn exponent; terms alternating between two exponents 1,001 to 1,023 apart. Each tuple has 2 to 5 terms |
| N3 | "every tuple of two and three from the special set" | ordered, with repetition |
| N4 | "the test cases of the Math.sumPrecise proposal's reference" | tc39/proposal-math-sum at `341ddea`, `polyfill/test-polyfill.mjs`: every fixed case of its tests "basic ability to handle intermediate overflows", "special cases", "1, 2 element lists" and "some edge cases from the fuzzer". Its "fuzzing" test draws an unseeded set and has no fixed case. The cases are numbers, carried as data |
| N4 | the constructed sequences | at every length to 8: −0s alone; one +0 among −0s, first and last; and cancelling pairs of 1, 10^308, 5e-324, 0.1 and the largest double, padded with −0s. Every sequence of length 1 to 3 over {+∞, −∞, NaN, 1, −0}. The largest double k times, against k and k − 1 of its negation, interleaved, with a tiny term after, and with −∞ after, for k from 1 to 4. Every order of a large term, a tiny one and the large one's negation |
| N4 | "drawn ... from doubles of every exponent" | each element's exponent field uniform over the 2,047 finite fields, with a drawn sign and significand |
| R1 | numerators "drawn ... across ±2^200", and the "2^10 drawn" denominators and increments | 2^10 of each, from one stream of seed `0x534A4C342D303037`, in that order: numerators of 1 to 200 bits, signed; denominators of 1 to 128 bits; increments of 1 to 64 bits |
| R1 | "over denominators ..., at every mode and at increments ..." | every fixed denominator with every fixed increment, and the drawn denominators paired one to one with the drawn increments. Each pair takes every numerator: the integers from −1,000 to 1,000 and the drawn ones |
| R1 | "each with the exact ties and their neighbours constructed" | for each pair, the ties (k + 1/2) × increment, for k from −8 to 8 and for the k nearest each of the first 32 drawn numerators, over twice the denominator, each with the numerators one unit either side |
| R1 | RoundNumberToIncrement "over integers" | the lines whose denominator is 1. RoundNumberToIncrementAsIfPositive runs over them as well, since both arms' members take integers |
| R1 | the windows, "at both signs" | non-negative windows, because the specification applies ApplyUnsignedRoundingMode to magnitudes (NudgeToCalendarUnit passes abs(r1) and abs(r2)); "at both signs" is the sign passed to GetUnsignedRoundingMode. Width 1 is one step; widths 2, 3, 10, 1000, 10^9 and 2^64 are windows, starting at k times the width for k from 0 to 6 and for 16 drawn k (seed `0x534A4C342D303037` xor `0xFFFF`). The value sits at each end, one part in 2d inside each end, and at the midpoint and one part in 2d either side, for d = 3 and 2^64 |
| R1 | the nearest double's "rationals ... drawn" | 2^16 of them, numerators of 1 to 2,400 bits, signed, and denominators of 1 to 2,400 bits. The midpoints are those of every one of the first 64 subnormals and the binade edges, of four pairs in every seventh binade, and of the largest double and its predecessor. Each is written over 2^1075 times its reduced denominator, with the numerators one unit either side, at both signs |
| B1 | the values | 0 and ±1; ±2^k, ±(2^k + 1) and ±(2^k − 1) for k from 1 to 4,100; the same forms for k from 2^20 − 2 to 2^20 + 1; then 2^12 drawn, of 1 to 70,000 bits, signed |
| B1 | "pairs from these values drawn", and "zero included" | 2^14 pairs of value indices drawn with seed `0x534A4C342D30303A`. Each of the first 256 drawn left operands is also paired with 0n, 1n and −1n on either side, so that zero, unit and minus-unit operands are present whatever the draws give |
| B1 | shift counts | each listed count at both signs |
| B1 | StringToBigInt's texts | built by the comparison from each value, not written by the generator, since the texts of the widest values run to gigabytes. The forms are decimal, `0x`, `0o` and `0b`. Each form gives the text alone; the text with each of the 25 StrWhiteSpaceChar code points before and after it; `+` and `−` before the digits (a sign the decimal form admits and the prefixed forms do not); a separator after the first digit; a digit beyond the radix after the last; an `n` suffix; and, in the prefixed forms, the prefix in upper case. The value 0n also gives the empty text and a text of white space alone |
| B1 | "text at every radix", and "comparison of every value with every double" | built by the comparison: each value at radixes 2 to 36, and each value against each finite double of B1C. B1C holds N1's special doubles and its first 2^14 draws. NaN and the infinities are answered by each engine's adapter before its kernel is called, so no kernel is called with one |

## 2. Two bounds, and arm V's ceiling

- **Powers too wide to build.** Arm J, and S under the unbounded policy (against which arm J is
  compared), are not called for a power whose result the operands' widths prove wider than 2^24
  bits: |base| ≥ 2, the exponent at most 2^31 − 1, and exponent × (width − 1) > 2^24. Under
  Broiler.VM's policy, against arm V, every pair runs, and these refuse at once. An exponent of 2^31
  or more still runs in arm J, where `(int)exponent` throws, and in S, where it refuses.
- **Arm J's quadratic text.** Arm J's BigInt text at a radix other than 10, and its StringToBigInt of
  the prefixed forms, run on the values of at most 4,101 bits and on the first 256 drawn values. Its
  radix-10 text and its decimal texts are the platform's own conversions and run on every value. S and
  arm V run every value at every radix and every text.
- **Arm V's ceiling.** Broiler.VM cannot hold a BigInt wider than 2^20 bits: its `JsBigInt`
  constructor refuses one. So arm V is not called with an operand wider than that, and neither is S
  under Broiler.VM's policy against it. A text of a wider value is a text, and still runs.

These exclusions, together with the divisors of 0n and the negative exponents the engines refuse
before their kernels, are counted per variant as `notCalled` in `summary.json`. **"Ran over its whole
population"**, in the MET line, is read as: every input of this section's population was run in S and
in every variant not excluded above.

## 3. How results are written and compared

The comparison is `Broiler.ECMAScript.Algorithms.Tests/ReferenceArms/Numerics/`, run as
`compare-numerics`. Each variant of an arm is compared with **S called as that arm's engine will call
it**. A BigInt variant of arm V is compared with S under Broiler.VM's policy and meter; one of arm J
with S under the unbounded policy and no meter. A variant whose engine reads the member's result
through a further step of the specification is compared with S followed by the same step:
`BigInt.prototype.toString`'s radix check, a match position's clamp, or whether NudgeToCalendarUnit's
window ends at its upper end. Where an engine's call site computes the member's arguments, the variant
repeats those lines, and the code says so.

Results are compared as text, exactly:

- a double is compared by its bit pattern, every NaN as one. A Float16 encoding is compared by its
  bits, under the arm's engine's NaN policy;
- an integer is compared by its value. Arm J's helpers that map ±∞ to their type's extreme are read so;
- a BigInt is compared by its value **in hexadecimal**, where the rule says "by its decimal text". The
  comparison is as exact, and the text is linear in the value's width;
- a charge is written after a bar and compared with the result;
- a refusal is compared by the error each engine raises for it. S's over-width status, arm V's `null`
  and arm J's thrown RangeError are all `error:RangeError`. Any other exception is compared by its type.

## 4. How it is read and retained

`summary.json` holds, per operation and class: the lines read; the inputs they gave; and, per variant,
how many inputs it ran, how many differed from S, and how many it was not called for. It also holds
what each arm does not implement and why, and the population's manifest. `differences.tsv.gz` has one
line per input and variant that differ: the operation, the class and line, the parameter and any
derived input, the line as the population writes it, the variant, S's result and the variant's result.
A field longer than 4,096 characters is listed by its length and SHA-256. **The comparison classifies
nothing.**

The classification is the owner's. It is written as a script before the judged run and applied
mechanically: `scripts/conformance/classify_numerics_arms.py`. The arbiter is
`scripts/conformance/numericsref.py`, the corpus's exact reference computation. "Listed" and "in a
vector at a named corpus revision" are read as in bundle `sjl-3-001`'s revision: the difference is
present in `classification.tsv.gz` with its class, and the corpus holds a vector of the same operation
whose class is `impl-defined` or whose outcome admits more than one result. The classifier writes that
vector's identifier and revision beside the difference.

The bundle retains `summary.json`, `classification.json`, `report.md`, the population's
`manifest.json`, the negative controls' runs and the commands of every step. The two listings are
**not committed**: the bundle records the SHA-256 of each one's uncompressed text and its line count,
and each is regenerated from the commit the bundle names and the population, as the rule says.
