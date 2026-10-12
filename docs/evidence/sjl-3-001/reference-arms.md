# Bundle SJL-3-001's reference-arm rule: the shared number conversions against both engines' pre-cut-over implementations

**Written:** 2026-10-12, at milestone SJL-0 (work package SJL-0.9) of the shared JavaScript libraries
programme, before `Broiler.ECMAScript.Algorithms`, its test project, either arm or the generator this
rule's population names exists. **Milestone judged:** SJL-3,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-3-number-conversion-pilot.md`, home clause 2
(its reference-arm half) and work package SJL-3.3. **Owner:** the Broiler.JS maintainer, as the home's
owner. **Reviewer:** none. Every role this rule names, the JavaScript profile owner whose engine
supplies one arm included, is held by one person (the programme records this as Broiler.VM's
exception EX-30), and this rule does not claim that anyone independent has read it.

**What this file is.** The predeclared decision of the library-level reference-arm comparison of the
number-conversion area, in template T4 of the programme's validation document,
`Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4, with the procedure of its
section 3. It declares the input population the area design asks for in
`Broiler.VM/docs/javascript-shared-libraries/areas/numbers.md` section 6: every class of input, and
the seeds of the random parts. **It holds no figure**: the counts and seeds below are parameters of a
population declared before any run, not results of one.

```rule
evidence class: conformance at library level (not a measurement)
arms:
  S  the shared implementation: Broiler.ECMAScript.Algorithms at the commit under test, called through its public API
  J  a test-only copy, in the home's test project, of Broiler.JS's implementation at bundle sjl-0-001's base commit 44a214dc: Parser/NumberCoercion; NumberParser's CoerceToNumber, TryCoerceToUInt32, TryGetArrayIndex, ParseInt, ParseFloat and their private algorithm members; JSNumber.ToECMAString and RoundTrippable; JSNumberPrototype's radix conversion with DecimalToBase and DecimalToArbitrarySystem, and its toFixed, toExponential and toPrecision digit generation with their kernels; JSBigInt.TryParseBigIntLiteral, and FastCompiler.CreateClass.BigIntLiteralToValue with ParseBigIntRadix; NumberParser.IsWhiteSpaceOrLineTerminator and JSString.trimCharacters
  V  a test-only copy, in the same project, of Broiler.VM's implementation at its bundle sjl-0-001's base commit 0087e8d7: JsNumberFormat whole; JsRealm.Number's statics from NumberFixedText to NumberIncrementFixed with their constants; JsParser.NumberText; JsParser.TryBigIntValue; the value loops of SliceTokenizer.ReadNumericLiteral and FinishNumeric
  each copy records the source path and git blob of every file it copies; where a member takes an engine type, the copy takes the CLR value that type carries and the change is recorded beside it; an arm whose copy changes a result is a defect of the arm, fixed in the arm, and the comparison is run again
operations: Number::toString at radix 10; Number::toString at every radix from 2 to 36; toFixed, toExponential and toPrecision digit generation, called with finite doubles and digit counts the built-ins have already validated; StringToNumber; parseInt at radix 0 (absent) and at every radix from 2 to 36; parseFloat; numeric literal values, BigInt literal values included; the array-index and canonical numeric string tests; StrWhiteSpaceChar membership. An arm is called only for the operations it implements, and the bundle names each operation an arm does not implement. Coercion, argument validation and error raising stay in the engines' adapters and are not compared.
population:
  D1  at both signs: zero, the smallest subnormal, the largest subnormal, the smallest normal, the largest finite double, infinity; and NaN
  D2  every power of two from 2^-1074 to 2^1023, with its next-lower and next-higher doubles
  D3  the double nearest every power of ten from 10^-323 to 10^308, with its next-lower and next-higher doubles
  D4  the doubles nearest 1e21 and 1e-7 (where Number::toString changes notation), with the eight doubles on each side of each
  D5  the 64 doubles on each side of 2^53 and of 2^64, and those two powers
  D6  a seeded random set: 2^20 doubles from the SplitMix64 generator with seed 0x534A4C332D303031, each draw read as an IEEE 754 bit pattern, a NaN pattern skipped and not counted
  T1  ties for toFixed: for every digit count f from 0 to 100, doubles a / 2^(f+1) with a odd and below 2^53, at most 64 per count drawn from SplitMix64 with seed 0x534A4C332D303032, each below 1e21; each is an exact tie at f digits after the point
  T2  ties for toExponential and toPrecision: for every precision p from 1 to 100 (toExponential's fraction digits p - 1), doubles whose exact decimal expansion has p + 1 significant digits ending in 5, at most 64 per precision, from the integers below 2^53 and the dyadic fractions with that expansion, drawn from SplitMix64 with seed 0x534A4C332D303033
  P1  for every finite double of D1 to D6, its shortest round-trip decimal string as the independent reference computes it, in plain and in exponent notation, each also signed with + and -
  P2  rounding boundaries: for every double of D2 and D3, and the first 2^14 finite doubles of D6, the exact decimal midpoint between it and its next-higher double, and that midpoint plus and minus one unit in its last written digit, each written to 17, 18, 19, 20, 25, 30, 40, 50, 100, 200, 400 and 800 significant digits, truncated; plus the full midpoint
  P3  radix strings around 2^53 and 2^64: every integer 2^e + k for e from 52 to 56 and from 63 to 66 and k from -64 to 64, and every halfway case 2^e + 2^(e-53) with its neighbours plus and minus one for e from 53 to 1023; each written in binary, octal and hexadecimal with StringToNumber's prefixes, and in every radix from 2 to 36 for parseInt
  P4  whitespace: every code point from U+0000 to U+FFFF, each lone surrogate included, alone, before "1", after "1", and on both sides of "1"
  P5  the grammar's edges: every prefix in both cases with no digit, with a separator and with a digit beyond its radix; Infinity signed and unsigned, and its case variants; the empty string; ".", ".5", "5.", "e5", "1e", "1e+", "1e-"; exponents of 400, 1000 and 100000 decimal digits' magnitude in both directions; leading zeros; separators anywhere, which StringToNumber refuses
  L1  literal values: every string of P1 to P3 that the NumericLiteral grammar admits, written as a literal; legacy octal and the 08 and 09 forms; separator forms; each integer of D5 and P3 as a BigInt literal in decimal, binary, octal and hexadecimal; 0n; 01n, which is refused; the BigInt width limit as the caller's parameter at 64, 1024 and 65536 bits
  K1  keys: the canonical numeric string of every double of D1 to D5; and "0", "1", "4294967294", "4294967295", "4294967296", "-0", "01", "1.0", "1e3", " 1", "+1", "-1", "Infinity", "-Infinity", "NaN"
  every operation runs over every class whose inputs it takes: the doubles D1 to D6 for Number::toString at radix 10; D1 to D5 and the first 2^14 doubles of D6 at every radix from 2 to 36; D1 to D5, D6 and T1 for toFixed at every digit count from 0 to 100; D1 to D5, D6 and T2 for toExponential and toPrecision at every count the built-ins admit; P1 to P5 for StringToNumber and parseFloat; P1, P3, P4 and P5 for parseInt; L1 for the literal values; K1 for the keys; P4 for StrWhiteSpaceChar
configuration: the home's test project, built in Release and run under JIT only, never in the Native AOT probe; the process's current culture set to the invariant culture, and an arm whose result depends on culture recorded as such; the population generated by the generator SJL-1.4 builds, at the commit the bundle names, its SplitMix64 implementation tested against the generator's published reference outputs
comparison: per operation, input and arm, S's result against the arm's result, exactly: a string by its UTF-16 code units, a double by its bit pattern with every NaN taken as one, a BigInt by its decimal text, and a refusal by its error kind
arbiter: which side of a difference is right is decided by the specification's algorithm computed by the independent reference computation of the vector generator in exact big-integer arithmetic, or, where the specification admits a range of results, by the clause that admits it; never by the agreement of the arms
admitted difference classes:
  (a) correction - an arm is wrong and S is right; listed with the engine it corrects (J, V or both), the input and the clause or the reference value that shows it
  (b) implementation-defined - the specification admits both results; listed, and moved into a vector at a named corpus revision
every other difference is class (c), a defect of S, which is fixed before either cut-over lands, after which the whole comparison is run again and both runs are retained
negative control: a copy of arm J with one result deliberately changed for one input of D1 is run once; the comparison reports that input as a difference and does not classify it; the copy is then removed
MET if and only if every operation ran over its whole population in S and in every arm that implements it, every difference is in class (a) or (b) and is listed, every class (b) difference is in a vector at a named corpus revision, no class (c) difference remains, and the negative control was reported unclassified.
NOT MET otherwise.
```

**How it is read.** The bundle retains the comparison's output: for each operation, the population
size each arm ran, and every difference with its input, the three results, the reference value and
its class or `unclassified`. The comparison classifies nothing itself; the classification is a file
written by the owner and retained beside the output, so every class can be checked against the
reference value line by line. A run that stopped before the end of any operation's population is void
and is not judged.

**Revisions.** A rule revised after any run it judges has been seen is a new dated file in this
directory, quoting this one whole and saying it was written afterwards; its decision is then weaker
evidence. SJL-3.2 confirms this file unchanged before any code of SJL-3.3 exists. The arms are
retired at SJL-16, or kept as a frozen regression population by decision.

## What it does not decide

- **Either engine's cut-over.** Engine-level vectors, test262 per path or per variant, and each
  engine's measurement are judged by that engine's own rule; this rule's population is not a test262
  population and meets no test262 clause.
- **Which engine is better.** A class (a) difference names the engine it corrects; the arms are
  compared with S and with the reference, never with each other.
- **Performance.** Nothing here is timed, and the arms' speed is not read.
- **The non-standard extensions** (decision SJL-D-17). The `m` literal suffix, `Number.prototype.clz`
  and the .NET format strings of `toLocaleString` stay in Broiler.JS and are not in S, so they are
  not operations here.
