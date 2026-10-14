# Bundle SJL-5-001's reference-arm rule: the shared text algorithms against both engines' pre-cut-over implementations

**Written:** 2026-10-14, at milestone SJL-5 (work package SJL-5.1) of the shared JavaScript libraries
programme, before the text area exists in `Broiler.ECMAScript.Algorithms`, before either arm, the area's
vectors or the generator this rule's population names exist. **Milestone judged:** SJL-5,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-5-text-algorithms.md`, home clause 1 (its
reference-arm half) and work package SJL-5.3. **Owner:** the Broiler.JS maintainer, as the home's
owner. **Reviewer:** none. Every role this rule names, the JavaScript profile owner whose engine
supplies one arm included, is held by one person (the programme records this as Broiler.VM's
exception EX-30), and this rule does not claim that anyone independent has read it.

**What this file is.** The predeclared decision of the library-level reference-arm comparison of the
text area, in template T4 of the programme's validation document,
`Broiler.VM/docs/javascript-shared-libraries/validation.md` section 4, with the procedure of its section
3, in the shape bundle `sjl-4-001`'s rule took. It declares the population: every class of input, and
the seeds of the random parts. **It holds no figure**: the counts and seeds below are parameters of a
population declared before any run, not results of one.

**The members each arm copies** are the ones the area verdict's correspondence names, by file and line:
Broiler.VM's `docs/evidence/sjl-5-001/correspondence.md`, read at Broiler.VM `1f253ff` and this
repository's `62c42e3`.

```rule
evidence class: conformance at library level (not a measurement)
arms:
  S  the shared implementation: Broiler.ECMAScript.Algorithms at the commit under test, called through its public API
  J  a test-only copy, in the home's test project, of this engine's implementation at 62c42e3: IsWellFormedUtf16 and ToWellFormedUtf16; JSString.CodePointAt and AdvanceStringIndex; StringPad's builder under int.MaxValue, and the relative-index normalisations of String's Relative and of the typed arrays' ClampRelativeIndex; JSString.GetSubstitution, the initializer's GetSubstitution and JSRegExp.Replace(string, string)'s template walk, each driven by a fixed match result whose named groups are plain values; UriHelper's Encode, Decode, Escape and Unescape; JSUInt8Array's DecodeBase64 with its helpers, the hex decode of SetFromHex and FromHex, and the encoders of ToBase64 and ToHex; JSJsonParser's Parse over System.Text.Json with JSJSON.Parse's exception filter, JSJSON.QuoteString, and the gap's normalisation
  V  a test-only copy, in the same project, of Broiler.VM's implementation at 1f253ff: StringWellFormed and the toWellFormed loop; codePointAt's read and RegExpAdvanceIndex's rule; StringPadTo's builder under StringLengthCeiling, StringRelativeIndex and ArrayRelative's normalisation; StringExpandDollars, RegExpReplaceText's template walk and RegExpSubstitute, each driven by the same fixed match results; GlobalUriEncode, GlobalUriDecode, GlobalEscapeText and GlobalUnescapeText with their sets and helpers; Base64Decode with Base64DecodeFull, Base64DecodeFinal and Base64SkipSpace, Base64DecodeHex, Base64Encode and toHex's encoder; JsRealm.Json's reader over its lexical half and its building half as one parse, JsonQuote and JsonApplySpace's normalisation; and JsJsonModule's TryTranslate, as the validator
  each copy records the source path and git blob of every file it copies; where a member takes an engine type, the copy takes the CLR value that type carries, and the change is recorded beside it; arm V's charge delegate is kept, and every charge it makes is recorded; a member that throws an engine error is copied to throw a recorded exception carrying the error's constructor name instead; an arm whose copy changes a result or a charge is a defect of the arm, fixed in the arm, and the comparison is run again
operations: TrimString at each end and both; IsStringWellFormedUnicode; toWellFormed's replacement; CodePointAt; AdvanceStringIndex with and without full Unicode; StringPad at the start and at the end under a maximum length; the relative-index normalisation; GetSubstitution, as S's template parse followed by an evaluation of the parts written in the test project against the same fixed match result; Encode with each of the two unescaped sets; Decode with each of the two reserved sets; escape; unescape; FromBase64 in each alphabet and each last-chunk mode into a destination of each length below; FromHex into the same destinations; ToBase64 in each alphabet with and without padding; ToHex; JSON validation at a depth limit, and the value of every text a parse admits; QuoteJSONString; the gap from a number and from a string. An arm is called only for the operations it implements, and the bundle names each operation an arm does not implement. Coercion, option reading, receiver checks, object building beyond a value's structure, and error construction stay in the engines' adapters and are not compared, except that a refusal is compared by its kind
population:
  T1  strings: every one of the 65,536 code units as a one-unit string; every string of length 1 to 4 over the eleven units a, U+0020, U+00A0, U+0085, U+180E, U+FEFF, U+2028, U+D800, U+DBFF, U+DC00 and U+DFFF; every white-space character StrWhiteSpaceChar names, and U+0085, U+180E and U+200B, at either end and both of a word, singly and in runs of 1 to 3; 2^16 strings of length 0 to 64 drawn with seed 0x534A4C352D303031 from a mix of ASCII, white space, the rest of the BMP, astral pairs and lone surrogates, a draw being a SplitMix64 output read as stated in the generator's README; every index from -1 to the length + 1 of each string, for CodePointAt and AdvanceStringIndex
  P1  padding and indices: strings of lengths 0 to 5 and the strings of T1's random draws; targets from 0 to 70, and each maximum length ±1; maximum lengths 0, 1, 16, 2^24, 2^30 and 2^31 - 1; fillers "", " ", "ab", "\uD800", "\uDC00\uD800" and a ten-unit one; for the relative-index normalisation, relative values -Infinity, -(2^53), every integer from -(length + 2) to length + 2, 2^53 and +Infinity, over lengths 0, 1, 2, 7, 2^32 and 2^53 - 1
  S1  templates: every template of length 0 to 4 over the eleven characters $, &, `, ', 0, 1, 2, 9, <, > and a; 2^14 templates of length 5 to 40 drawn with seed 0x534A4C352D303032 over the same characters and the group names a, b and xyz; each against capture counts 0, 1, 2, 9, 10, 11 and 99, every capture defined, undefined or empty in turn, and against no named groups and against the groups a and b with a defined and with b undefined; each against a match at the start, in the middle and at the end of its subject
  U1  URI: every code point from U+0000 to U+10FFFF as a one-code-point string, lone surrogates included, for both encodes, escape and unescape's inverse; for both decodes, every %XX for XX from 00 to FF in both letter cases, every two-, three- and four-octet sequence whose lead octet is any of 80 to FF and whose continuation octets are drawn from 00, 7F, 80, 8F, 90, 9F, A0, BF, C0 and FF, each sequence whole and truncated after every octet, every reserved and unreserved ASCII character escaped and raw, and % followed by zero, one and two characters of which none, one or both are hexadecimal; for unescape, every %uXXXX in both letter cases and %u with fewer than four hexadecimal digits; 2^16 strings drawn with seed 0x534A4C352D303033 mixing escapes, raw text and malformed escapes
  B1  base64 and hex: every string of length 0 to 4 over the fourteen characters A, Z, a, z, 0, 9, +, /, -, _, =, U+0020, U+000A and @; 2^14 strings of length 5 to 120 drawn with seed 0x534A4C352D303034, most of them valid base64 of either alphabet with white space and padding mutated by one character; each decoded in each alphabet and each last-chunk mode into destinations of lengths 0, 1, 2, 3, 4 and unbounded; for hex, every string of length 0 to 4 over 0, 9, a, f, A, F, g, Z and U+0020, and 2^12 strings of length 5 to 64 drawn with seed 0x534A4C352D303035, into the same destinations, setFromHex("aaZZ")'s shape among them; for the encoders, every byte array of length 0 to 2 and 2^12 of length 3 to 64 drawn with seed 0x534A4C352D303036
  J1  JSON: every text of length 0 to 3 over the eighteen characters {, }, [, ], ",", :, ", \, 0, 1, -, ., e, t, n, u, a and U+0020; 2^14 documents drawn with seed 0x534A4C352D303037 from the JSON grammar at depths 1 to 600, each also mutated by one inserted, deleted or replaced character; the texts of every escape, of every code unit escaped in both letter cases and raw (a raw lone surrogate included), of 2^12 numbers drawn with seed 0x534A4C352D303038 in every exponent spelling, and of 1e400, -1e400, 1e-400 and -0; and arrays and objects nested exactly 63, 64, 65, 511, 512, 513, 1023, 1024 and 1025 deep; each validated at the depth limits 64, 512 and 1024; for QuoteJSONString, T1's strings; for the gap, the numbers -Infinity, -1, 0, 0.5, 1, 9, 10, 11, 2^53 and +Infinity and strings of every length from 0 to 12
  every operation runs over every class whose inputs it takes
configuration: the home's test project, built in Release and run under JIT only, never in the Native AOT probe; the process's current culture set to the invariant culture, and arm J's string operations run a second time under a culture whose comparison treats U+000D U+000A as one element; the population generated by the vector generator SJL-5.2 builds, at the commit the bundle names, its SplitMix64 implementation tested against the generator's published reference outputs
comparison: per operation, input and arm, S's result against the arm's result, exactly: a string by its UTF-16 code units, an index or a count by its value, a byte array by its bytes, a parsed JSON value by its structure, its strings by their code units and its numbers by their bit patterns, a refusal by its kind and, where both report one, its position; and, for every operation arm V implements with a charge, the total charge S makes through its meter against the total charge arm V makes, exactly
arbiter: which side of a difference is right is decided by the specification's algorithm, computed by the independent reference computation of the vector generator, written in Python from the specification's text and the proposals' for the base64 and hex codecs and JSON.rawJSON; never by the agreement of the arms. A charge has no arbiter but arm V: Broiler.VM's units are the specification of the charge (JSD-0070, decision 3)
admitted difference classes:
  (a) correction - an arm is wrong and S is right; listed with the engine it corrects (J, V or both), the input and the clause or the reference value that shows it, and, where the area verdict's correspondence names it, its row
  (b) implementation-defined - the specification admits both results: a refusal past an engine's own maximum length or depth, where S under that engine's policy refuses at the same point; listed, and moved into a vector at a named corpus revision
every other difference is class (c), a defect of S, which is fixed before either cut-over lands, after which the whole comparison is run again and both runs are retained; a charge of S that differs from arm V's is always class (c)
negative control: a copy of arm V with one charge deliberately changed for one input of U1, and a copy of arm J with one result deliberately changed for one input of B1, are each run once; the comparison reports each as a difference and does not classify it; the copies are then removed
MET if and only if every operation ran over its whole population in S and in every arm that implements it, every difference is in class (a) or (b) and is listed, every class (b) difference is in a vector at a named corpus revision, no class (c) difference remains, every charge of S equals arm V's, and both negative controls were reported unclassified.
NOT MET otherwise.
```

**How it is read.** As bundle `sjl-4-001`'s rule: the comparison writes `summary.json` and
`differences.tsv.gz`, and **classifies nothing**. The classification is the owner's, written as a script
before the judged run and applied mechanically, into `classification.tsv.gz`, `classification.json` and
`report.md`. **"Listed"**, in the MET line, is read as present in `classification.tsv.gz` with its class.

**What the bundle retains.** `summary.json`, `classification.json`, `report.md`, the population's
`manifest.json`, the negative controls' runs and their arms' sources, and the commands of every step.
The two listings are **not committed**: the bundle records the SHA-256 of each one's uncompressed text
and its line count, and each is regenerated byte for byte from the commit the bundle names and the
population, itself regenerated by the generator and checked against the manifest's digests. A run that
stopped before the end of any operation's population is void and is not judged.

**Revisions.** A rule revised after any run it judges has been seen is a new dated file in this
directory, quoting this one whole and saying it was written afterwards; its decision is then weaker
evidence. Before SJL-5.3's code lands, a later bundle shows this file unchanged. The arms are retired
at SJL-16, or kept as a frozen regression population by decision.

## What it does not decide

- **Either engine's cut-over.** Engine-level vectors, test262, Broiler.VM's fuel parity and its JSON
  modules' artifacts are judged by each engine's own rule; this population meets no test262 clause.
- **Which engine is better.** A class (a) difference names the engine it corrects; the arms are
  compared with S and with the reference, never with each other.
- **The drivers.** `split`, `replace`'s evaluation of named groups, string search, `JSON.parse`'s
  object building and reviver and `JSON.stringify` stay in the engines and are compared only through
  test262 and the engine-level vectors.
- **Performance.** Nothing here is timed.
