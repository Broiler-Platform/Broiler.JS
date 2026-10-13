# Evidence bundle SJL-3-001 (Broiler.JS) - the number-conversion pilot

**Milestone:** SJL-3 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-3-number-conversion-pilot.md`: its home
clauses 1 to 3, its Broiler.JS clauses 4 to 7, and this repository's half of clause 14 - work
packages SJL-3.3, SJL-3.5, SJL-3.6 and SJL-3.7. **Rules:** [`decision-rule.md`](decision-rule.md)
(part A, test262 per path; part B, the measurement) and [`reference-arms.md`](reference-arms.md) (the
library-level comparison), both committed at SJL-0.9 before any code they judge, and confirmed
unchanged at SJL-3.2 ([`rules-in-force.log`](rules-in-force.log)). **Collected by**
[`collect.py`](collect.py), from a separate clean checkout of the after commit. **Owner:** the
Broiler.JS maintainer; one person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

**State:** collection in progress. Each run is added as it is taken, and the table of section 2 says
which have been; nothing here is a verdict until its section says so. Broiler.VM keeps its own bundle
of the same name for its own half, judged by its own rule; neither bundle reads the other's runs.

## 1. Identity

| Field | Value |
|---|---|
| Evidence bundle | SJL-3-001 (Broiler.JS's) |
| Base | Bundle [`sjl-0-001`](../sjl-0-001/README.md)'s base commit, `44a214dc`, and its retained runs |
| After | `ef67693`: the cut-over (`93df3fd`) and the two corrections the duplicate search of SJL-3.7 found (`b598028`, `ef67693`). Every run of this bundle is taken from a clean checkout of it, except the test262 run, taken from `b598028`, whose shell binaries are those of `ef67693` (section 5) |
| Rule commit | `1410079`, an ancestor of the after commit; the rule, the comparison script and the workloads are unchanged between the two (`identity.txt`, pending) |
| Shared package | `Broiler.ECMAScript.Algorithms` `0.1.0-preview.1`, the version Broiler.VM's committed feed holds, packed from `93df3fd`; its sources are unchanged at `ef67693` |
| Shared vectors | `conformance/vectors/numbers` at corpus revision 5 ([`REVISIONS.md`](../../../conformance/vectors/REVISIONS.md)) |
| RID | `linux-x64`, one machine, four logical processors |

## 2. The clauses, and what holds each

| Clause | What it asks | Where | State |
|---|---|---|---|
| 1 | The library under the conventions; SL1 to SL10, the API baseline and package validation pass; the Native AOT probe runs the vectors | section 3, `home/` | pending |
| 2 | Every number vector passes at library level; every reference-arm difference classified | section 3; [`reference-arms-run/`](reference-arms-run/README.md) | the reference arms: **MET**, at corpus revision 4; the library-level vectors pending |
| 3 | A preview package on Broiler.VM's committed feed | Broiler.VM's `eng/packages/` and its pin, which names `93df3fd` | **Done** at SJL-3.3 |
| 4 | `Parser`, `Runtime` and `BuiltIns` reference the library; every member of the deletion list absent; every disposition applied | section 8; [`docs/public-api.md`](../../public-api.md) | the deletion list: **every member absent**; the references and dispositions are in the cut-over commit |
| 5 | Test262 per path: no path passing at the base fails after; every improvement listed | section 5 | pending (the run is being taken) |
| 6 | Engine-level vectors pass; every correction in the changelog | section 4; [`Broiler.JS/CHANGELOG.md`](../../../Broiler.JS/CHANGELOG.md) | pending at `ef67693` |
| 7 | The pristine consumer and package checks; the measurement meets its rule | sections 6 and 7 | pending; the measurement is void on this machine (section 7) |
| 14 | Each adapter's review finds no algorithm; no intra-engine duplicate of the area survives | section 8 | **reviewed**: no algorithm in an adapter, and no duplicate after `ef67693` |

## 3. The home: the library at the after commit

Pending: the shared filter's tests (SL1 to SL10, the library-level vectors, the API baseline), the
corpus generator's own tests, the Native AOT probe published and run against the JIT, and the shared
set packed with package validation and restored by a pristine consumer, into `home/`. The
reference-arm comparison of clause 2 is [`reference-arms-run/`](reference-arms-run/README.md),
classified at corpus revision 4: MET.

## 4. Engine-level vectors

Pending at `ef67693`. Corpus revision 5 retired every `divergence.broiler-js` field the cut-over
corrected; [`engine/retired-divergences.tsv`](engine/retired-divergences.tsv) lists each retired
vector under its reason, and the changelog lists each correction.

## 5. Test262 per path (rule part A)

Pending. The after run is taken shard by shard in the base's configuration (its `drive.sh` is
retained when it is imported); a restart of the machine interrupted shard 1, which was run again from
its start with the same binaries, and the interrupted shard's log is retained beside the run, as the
base retained its own. The run's shell was built at `b598028`; `ef67693` changes no assembly the shell
loads, and the bundle will show the shell's binaries rebuilt at `ef67693` hashing the same.

## 6. The pristine consumer

Pending: the engine set packed and restored by a pristine consumer
(`scripts/packaging/test_pristine_consumer.py`), into `engine/`.

## 7. The measurement sub-bundle (rule part B)

Pending, and **void on this machine whatever its figures**: part B judges only a controlled lane, and
this container is not one ([`Measurement.md`](../../roadmap/Measurement.md), "Controlled lanes and
effective configuration"), so clause 7's measurement half stays open until a controlled lane runs it.
The runs are still taken, three arms rotated, for what the condition and the configuration checks
show.

## 8. The simplification check (SJL-3.7)

**The deletion check.** [`simplification/deletion-check.log`](simplification/deletion-check.log)
searches every member of the area's Broiler.JS deletion list (Broiler.VM
`docs/javascript-shared-libraries/areas/numbers.md` section 7) as a whole word in the engine's
product sources: **all 24 are absent**, `NumberParser` declares only its three public members, the
forwarders decision SJL-D-25 keeps each call the library (`TryCoerceToUInt32` below), the obsolete
`DecimalToBase`, `DecimalToArbitrarySystem` and `JSTrim` forward to it, and one `parseInt`/`parseFloat`
pair remains, `JSGlobal`'s, which `Number.parseInt` and `Number.parseFloat` are.

**The adapter review.** Each adapter was read at `ef67693` against the area's adapter definition
(coercion, errors, charging, object creation, and no algorithm):

| Adapter | What it holds | Algorithm |
|---|---|---|
| `JSNumberPrototype`: `toString`, `toFixed`, `toExponential`, `toPrecision` | `thisNumberValue`; `ToIntegerOrInfinity` of the argument in each method's order (toFixed's range check before the value, toExponential's and toPrecision's non-finite answer after the coercion and before the range check); the `RangeError`s; the `JSString` | none: the text is `NumberText`'s |
| `JSGlobal`: `parseInt`, `parseFloat` | `ToString` of the argument; `ToInt32` of the radix, once | none |
| `NumberParser`: `CoerceToNumber`, `TryGetArrayIndex` | one call each | none |
| `NumberParser.TryCoerceToUInt32` | this public API's own acceptance policy, unchanged from the base: a leading `0` before another digit refused, an all-white-space string refused, an integral value below 2^32 - 1 accepted | none: the value is `CoerceToNumber`'s, the white space `NumberParsing`'s; no digit is read into a value and nothing is rounded |
| `FastScanner.CommitNumberToken`, `FastCompiler.CreateClass`'s literal property names | the scan, the diagnostics, the token; a BigInt's value is the compiler's | none: `NumericLiterals.Evaluate` and `NumberText.ToString` |
| `JSNumber`, `JSString`, `JSValue`, `JSJSON`: a number's text in concatenation, keys and JSON | the string and the key cache; JSON's `null` for a non-finite value | none |
| `JSStringPrototype`: `trim`, `trimStart`, `trimEnd`; `JSBigInt`'s StringToBigInt white space | the `this` coercion; the original string when nothing is trimmed | none: `NumberParsing.TrimStrWhiteSpace` |
| `JSBigInt(string)` | the sign | none: `NumericLiterals.Evaluate` |
| `KeyStrings`, `JSTypedArray`: array indices and canonical numeric keys | the key's metadata cache | none: `NumericKeys` |

**The duplicate search.** [`simplification/duplicate-search.log`](simplification/duplicate-search.log)
lists every construct in the engine's product sources that converts between numbers and text, rounds
by powers of ten, or classifies white space with a table other than the library's: 66 hits at
`ef67693`. It found two duplicates, both corrected in this repository before the after commit and
listed in the changelog:

- **`==` between a Number and a String** compared .NET's text of the number with the string in three
  fast paths, so `NaN == "NaN"` was `true` (`b598028`).
- **`PortableCompiler.EmitCSharp`** wrote each number constant of generated C# in .NET's `"R"`
  format, which does not read back as the same double for `2**-25`, and wrote an infinite constant as
  `Infinity`, which C# does not compile (`ef67693`).

**None of the 66 hits left is a surviving duplicate of the area:**

| Class | Hits | Where | Reading |
|---|---|---|---|
| Names, paths, options and diagnostics | 28 | the compiler's deferred-body diagnostics and direct-eval names, the debugger, the engine's initialisers, the class generator, the expression utilities' input, modules (a `Guid`'s `"N"` format), the key-value store, the portable compiler's and program's arguments, `NullishAccess`'s message, the code cache, the shell's context and REPL, the package manager, `JSError`'s file name, `JSFunction`'s location | no ECMAScript value is converted |
| Other areas' code | 31 | BigInt (`JSBigInt`, 3: StringToBigInt's digits and `toString`'s radix table, SJL-4), Intl (`JSIntl`, 14, and the date-time format engine, 1: SJL-9), Temporal (7: SJL-10), RegExp (`JSRegExp`, 4: the source's escaping and named groups' `\u` escapes, SJL-11), and the lexer's white space (`FastScanner`, 2: SJL-13) | each is answered by its own milestone; areas/numbers.md section 1 does not name them |
| Kept by decision SJL-D-17 | 3 | `JSDecimal` | the `m` literal's `Decimal`, a non-standard extension |
| The compiler's own encoding | 1 | `JSArray.Template`'s `'d'` case | reads back the IEEE-754 bits the compiler wrote, not a formatted number |
| A trim with no caller in the engine | 3 | `StringSpan.Trim`, `TrimStart`, `TrimEnd` (`char.IsWhiteSpace`) | public members of an AST type, which no number conversion calls since the cut-over (`NumberParser` called `Trim` at the base); trimming beyond the shared predicate is SJL-5's |

**Two findings outside the area.** The lexer's white space (`FastScanner`) is .NET's
`char.IsWhiteSpace` and U+FEFF, so it admits U+0085 between tokens, which ECMAScript's `WhiteSpace`
does not (`eval("1\u0085+ 1")` is `2`, a SyntaxError in node); and `StringSpan`'s trims use the same set. Both are recorded for SJL-13 and SJL-5, not
changed here.

## 9. What this bundle does not claim

- **Nothing about Broiler.VM**, whose half is its own bundle, judged by its own rule.
- **A speed claim of any kind**: part B makes none, and on this machine it is void.
- **Review**: the reviewer field says none and means it.
