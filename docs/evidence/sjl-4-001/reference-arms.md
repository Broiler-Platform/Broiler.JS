# Bundle SJL-4-001's reference-arm rule: the shared numerics against both engines' pre-cut-over implementations

**Written:** 2026-10-13, at milestone SJL-4 (work package SJL-4.1) of the shared JavaScript libraries
programme, before the numerics area exists in `Broiler.ECMAScript.Algorithms`, before either arm, the
area's vectors or the generator this rule's population names exist. **Milestone judged:** SJL-4,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-4-numerics.md`, home clause 2 (its
reference-arm half) and work package SJL-4.3. **Owner:** the Broiler.JS maintainer, as the home's
owner. **Reviewer:** none. Every role this rule names, the JavaScript profile owner whose engine
supplies one arm included, is held by one person (the programme records this as Broiler.VM's
exception EX-30), and this rule does not claim that anyone independent has read it.

**What this file is.** The predeclared decision of the library-level reference-arm comparison of the
numerics area, in template T4 of the programme's validation document,
`Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4, with the procedure of its section
3. It declares the population: every class of input, and the seeds of the random parts. **It holds no
figure**: the counts and seeds below are parameters of a population declared before any run, not
results of one. It is written in the shape bundle `sjl-3-001`'s rule took after its revision of
2026-10-12 (`../sjl-3-001/reference-arms-revised-2026-10-12.md`): what the bundle retains, and what it
regenerates, is stated here before any run, not afterwards.

**The members each arm copies** are the ones the area verdict's correspondence names, by file and line:
Broiler.VM's `docs/evidence/sjl-4-001/correspondence.md`, read at Broiler.VM `a04b2e5` and this
repository's `4899f9c`.

```rule
evidence class: conformance at library level (not a measurement)
arms:
  S  the shared implementation: Broiler.ECMAScript.Algorithms at the commit under test, called through its public API
  J  a test-only copy, in the home's test project, of this engine's implementation at 4899f9c: the integer conversions (JSValue.IntValue, ToUint32(double), UIntValue; the typed arrays' and DataView's narrowing and clamping written as functions of a double; the ToIntegerOrInfinity, ToLength and ToIndex helpers the correspondence lists); Float16 through the platform's Half as this engine uses it; JSMath's Round, Pow, Hypot, Cbrt with CorrectlyRoundedCbrt, Expm1, Log1p and SumPrecise with its members, and JSValue.Power; TemporalRoundingOptions' modes, unsigned modes and increment roundings, JSTemporalDuration's RoundToIncrement, RatioToDouble and NearestDouble, JSTemporalZonedDateTime.Rounding's unsigned modes, and JSIntlNumberFormat's unsigned modes and RoundRatio; JSBigInt's operator bodies, Power, Shift, AsIntN, AsUintN, BigIntegerToRadixString, its radix-10 text, TryStringToBigInt with TryParseBigIntString and TryParsePrefixedDigits, ToNumber, NumberToBigInt, EqualsLiteral(double), and JSBigIntExtensions.CompareToNumber
  V  a test-only copy, in the same project, of Broiler.VM's implementation at a04b2e5: JsValue.ToInt32, ToUint32 and ToInteger; JsElements.Clamp, Low64 and Narrow; JsFloat16 whole; JsRealm.BinaryToIndex's range rule and JsRealm.ArrayToLength; JsRealm.Math's MathRoundHalfUp, MathPower, MathHypotenuse, the cbrt snap, MathExpMinusOne, MathLogOnePlus, and MathSumPrecise's kernel with MathDistil; JsTemporalCore's rounding members (Negate, UnsignedMode, ApplyUnsigned, RoundsToSecond, both RoundToIncrement, RoundToIncrementAsIfPositive and both ToDouble) and JsNumberFormatter.Unsigned with JsUnsignedRounding; JsBigInt whole, with its cost functions
  each copy records the source path and git blob of every file it copies; where a member takes an engine type, the copy takes the CLR value that type carries, and the change is recorded beside it; arm V's charge delegate is kept, and every charge it makes is recorded; an arm whose copy changes a result or a charge is a defect of the arm, fixed in the arm, and the comparison is run again
operations: ToInt32, ToUint32, ToInt16, ToUint16, ToInt8, ToUint8, ToUint8Clamp, ToIntegerOrInfinity, ToLength, ToIndex's range rule, the ToBigInt64 and ToBigUint64 bit patterns; Float16 encode under each NaN policy, decode, and round; Math.round, Number::exponentiate, hypot, cbrt, expm1, log1p and the precise sum; NegateRoundingMode, GetUnsignedRoundingMode, ApplyUnsignedRoundingMode over one step and over a window, RoundNumberToIncrement over integers and over rationals, RoundNumberToIncrementAsIfPositive, the rounding-mode names' parse, and a rational's nearest double; BigInt add, subtract, multiply, divide, remainder, exponentiate, negate, bitwise not, and, or, xor, left and right shift, asIntN, asUintN, text at every radix from 2 to 36, StringToBigInt, a BigInt's Number, an integral Number's BigInt, and comparison with a Number. An arm is called only for the operations it implements, and the bundle names each operation an arm does not implement. Coercion, argument validation, iteration and error raising stay in the engines' adapters and are not compared.
population:
  N1  doubles: at both signs, zero, the smallest subnormal, the largest subnormal, the smallest normal, the largest finite double, infinity, and NaN; every power of two from 2^-1074 to 2^1023 with its next-lower and next-higher doubles; for every k from 0 to 64, 2^k and 2^k plus and minus 0.5 and 1 where representable, at both signs; every n + 0.5 for n from -300 to 300; 254.5, 255.5, 256, 65504, 65519.99, 65520 and their neighbours; a seeded set of 2^20 doubles from the SplitMix64 generator with seed 0x534A4C342D303031, each draw read as an IEEE 754 bit pattern, a NaN pattern skipped and not counted
  N2  Float16: every one of the 65,536 bit patterns, for decode; for encode and round, every finite Float16 value as a double, its next-lower and next-higher doubles, every midpoint between adjacent Float16 values and the doubles one unit either side of it, and N1; NaN inputs: the double NaN patterns with every combination of sign, quiet bit and the highest, lowest and middle payload bits, and 2^12 drawn from SplitMix64 with seed 0x534A4C342D303032
  N3  Math: N1 for round, cbrt, expm1 and log1p; for expm1 and log1p also ±2^-k for every k from 1 to 1074 and 2^16 doubles drawn uniformly from (-50, 50) with seed 0x534A4C342D303033; for cbrt also every perfect cube n^3 for n from -2^17 to 2^17; for exponentiate, every pair from the special set {±0, ±1, ±0.5, ±2, ±3, ±Infinity, NaN, 1e-300, 1e300, ±(2^53 - 1), ±2^53 + 1, 0.1} and 2^18 pairs drawn from N1 with seed 0x534A4C342D303034; for hypot, every tuple of two and three from the special set, and 2^16 tuples of two to five doubles drawn from N1 with seed 0x534A4C342D303035, with tuples whose terms share an exponent and tuples whose exponents differ by more than 1000
  N4  precise sums: the test cases of the Math.sumPrecise proposal's reference; sequences whose exact sum is 0, ±0 and -0 at every length to 8; sequences mixing ±Infinity and NaN; sequences of the largest finite double that overflow a partial and cancel back; tiny terms beside a cancelling pair of large ones; 2^14 sequences of length 1 to 64 drawn with seed 0x534A4C342D303036 from doubles of every exponent
  R1  rounding: every mode, in each of the nine names and in an unknown name, for the parse, the negation and the unsigned mode at both signs; for the increment roundings, numerators drawn with seed 0x534A4C342D303037 across ±2^200 and the integers from -1000 to 1000, over denominators 1, 2, 3, 7, 10, 1000, 10^9, 2^k for k to 128 and 2^10 drawn, at every mode and at increments 1, 2, 5, 10, 20, 50, 100, 1000, 10^9 and 2^10 drawn, each with the exact ties and their neighbours constructed; windows r1 < x < r2 with x at, beside and halfway between the ends, at both signs, at every mode; for the nearest double, rationals with numerators and denominators of 1 to 2400 bits drawn with seed 0x534A4C342D303038, the exact midpoints between adjacent doubles and their neighbours, and the subnormal range and the overflow threshold in particular
  B1  BigInt values: 0, ±1, ±2^k and ±(2^k ± 1) for every k from 1 to 4100 and around Broiler.VM's width ceiling, 2^20 bits; 2^12 values drawn with seed 0x534A4C342D303039 at sizes from 1 to 70,000 bits; for the binary operations, pairs from these values drawn with seed 0x534A4C342D30303A, divisors and exponents of every sign included and zero included; shift counts 0, 1, 63, 64, 65, the value's size and its neighbours, 2^20 and 2^21; asIntN and asUintN widths 0, 1, 63, 64, 65, the value's size and its neighbours, 2^20, 2^20 + 1, 2^31, 2^32, 2^32 + 3 and 2^53 - 1; text at every radix from 2 to 36 of every value; StringToBigInt of every value's text at every radix it can be written in with the specification's prefixes, each with leading and trailing white space from every StrWhiteSpaceChar, a sign where the grammar admits one and where it does not, an empty string, a separator, a digit beyond its radix, and an n suffix; a BigInt's Number for every value, and for every integer 2^e + k for e from 52 to 1025 and k from -2 to 2 with the halfway cases between adjacent doubles; comparison of every value with every double of N1's first 2^14 draws and of its special doubles
  every operation runs over every class whose inputs it takes; S runs every BigInt operation twice, under Broiler.VM's width policy against arm V and under the unbounded policy against arm J
configuration: the home's test project, built in Release and run under JIT only, never in the Native AOT probe; the process's current culture set to the invariant culture, and arm J's text operations run a second time under a culture whose negative sign is U+2212, as the area record's correction requires; the population generated by the vector generator SJL-4.2 builds, at the commit the bundle names, its SplitMix64 implementation tested against the generator's published reference outputs
comparison: per operation, input and arm, S's result against the arm's result, exactly: an integer by its value, a double by its bit pattern with every NaN taken as one except in Float16 encode, where the bytes are compared under the NaN policy the arm's engine uses, a BigInt by its decimal text, a string by its UTF-16 code units, and a refusal or an over-width status by its kind; and, for every BigInt operation arm V implements, the total charge S makes through its meter against the total charge arm V makes, exactly
arbiter: which side of a difference is right is decided by the specification's algorithm computed by the independent reference computation of the vector generator, in exact rational and big-integer arithmetic; for an implementation-approximated Math function, by the exact value computed to at least forty significant digits and the clause that admits an approximation; never by the agreement of the arms. A charge has no arbiter but arm V: Broiler.VM's units are the specification of the charge (JSD-0067, decision 3)
admitted difference classes:
  (a) correction - an arm is wrong and S is right; listed with the engine it corrects (J, V or both), the input and the clause or the reference value that shows it. For expm1 and log1p, an arm's result for |x| below 2^-20 that is more than one unit in the last place from the exact value is wrong, because their clauses ask for a result accurate when x is close to 0
  (b) implementation-defined - the specification admits both results: two approximations of an implementation-approximated function, each within a stated bound of the exact value (S's bound is its documented one; an arm's is any finite result of the correct sign whose special cases are the clause's), and a NaN's Float16 encoding; listed, and moved into a vector at a named corpus revision
every other difference is class (c), a defect of S, which is fixed before either cut-over lands, after which the whole comparison is run again and both runs are retained; a charge of S that differs from arm V's is always class (c)
negative control: a copy of arm V with one charge deliberately changed for one input of B1, and a copy of arm J with one result deliberately changed for one input of N1, are each run once; the comparison reports each as a difference and does not classify it; the copies are then removed
MET if and only if every operation ran over its whole population in S and in every arm that implements it, every difference is in class (a) or (b) and is listed, every class (b) difference is in a vector at a named corpus revision, no class (c) difference remains, every BigInt charge of S equals arm V's, and both negative controls were reported unclassified.
NOT MET otherwise.
```

**How it is read.** The comparison writes `summary.json`, for every operation and class the inputs S
and each arm ran and how many differed, with the operations each arm does not implement and why, and
`differences.tsv.gz`, every input on which an arm differs from S, with both results, both charges where
there are charges, and the reference value. **The comparison classifies nothing.** The classification
is the owner's, written as a script before the judged run and applied mechanically, as at SJL-3:
for each difference it computes every result the specification admits and gives the class and the
reason, into `classification.tsv.gz`, `classification.json` (the counts per operation, arm, class and
reason) and `report.md` (those counts with the first twenty differences of every group in full).
**"Listed"**, in the MET line, is read as present in `classification.tsv.gz` with its class.

**What the bundle retains.** `summary.json`, `classification.json`, `report.md`, the population's
`manifest.json`, the negative controls' runs and their arms' sources, and the commands of every step.
The two listings are **not committed**: the bundle records the SHA-256 of each one's uncompressed text
and its line count, and each is regenerated byte for byte from the commit the bundle names and the
population, itself regenerated by the generator and checked against the manifest's digests. A run that
stopped before the end of any operation's population is void and is not judged.

**Revisions.** A rule revised after any run it judges has been seen is a new dated file in this
directory, quoting this one whole and saying it was written afterwards; its decision is then weaker
evidence. Before SJL-4.3's code lands, a later bundle shows this file unchanged. The arms are retired
at SJL-16, or kept as a frozen regression population by decision.

## What it does not decide

- **Either engine's cut-over.** Engine-level vectors, test262 and Broiler.VM's fuel parity are judged
  by each engine's own rule; this population meets no test262 clause.
- **Which engine is better.** A class (a) difference names the engine it corrects; the arms are
  compared with S and with the reference, never with each other.
- **Performance.** Nothing here is timed.
- **The Intl mathematical value and the Temporal consumers**, which SJL-9 and SJL-10 judge. The
  rounding this rule compares is the area's: modes, increments and conversions over exact values.
