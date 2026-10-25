# The regular-expression area's measurement workloads (Broiler.JS)

These files are the population of part B of bundle `sjl-11-001`'s [rule](../../decision-rule.md), this engine's
candidate-versus-control measurement at milestone SJL-11 of the shared JavaScript libraries programme, which moves
this engine's regular expressions onto Broiler.Regex. The area verdict is
[`SharedLibraries.md`](../../../../roadmap/SharedLibraries.md), "Regular expressions (SJL-11.1)", with Broiler.VM's
JSD-0081, whose decision D19 says how part B's verdict is recorded: it is not a gate clause. **No figure produced
from these files is retained anywhere in this repository except in bundle `sjl-11-001`'s `measurement/`
sub-bundle.**

They were committed with the rule, before any code of SJL-11 existed (JSD-0081 decision D17). The class that runs
them is not here yet: `RegExpBenchmarks`, in `Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks/`, and the
benchmark `Program.cs`'s `--regexp-answers` are added by the rule's inputs commit, which reads these files verbatim.
Workload `Scan` runs `scan.js` and `ScanControl` runs `scan.control.js`, and so on for `Captures`, `Replace`,
`Split`, `Unicode` and `Backtrack`. **Each program's completion value is its answer**, which `--regexp-answers`
prints. No build was taken at SJL-11.1, so this engine first runs them at the inputs commit.

**The same text as Broiler.VM's.** The twelve programs are byte-for-byte copies of the workloads in Broiler.VM's
bundle of the same name, `docs/evidence/sjl-11-001/inputs/workloads/`, which its own rule's part E measures there.
Each engine measures them against itself only: no figure of one engine is compared with the other's (the
programme's validation principle 6). Their SHA-256 here:

| File | SHA-256 |
|---|---|
| `scan.js` | `02a619abf2fcf2f443136b05cb35d8cfd55d4a6dfd746e4912c7814a681287b0` |
| `scan.control.js` | `ef64a4b153c2c7df2dea77e42df2a730b4db0ca0771cfc454591c7403af32f0d` |
| `captures.js` | `e25e7178d7bcc5fb2d8757ea71ca82f14c782fdc6834c292249b206915530652` |
| `captures.control.js` | `19df64626ca1d819ee37f9d12205c34789361f692dbb7750a848e5413412c11b` |
| `replace.js` | `e831cd294655c95ec91e93ea2ea69a93535e5ab61dc05f82ea9bb49acd28275b` |
| `replace.control.js` | `1dd2f4d035ed75c940b0a74bff4af4e5ee994387c0ce129e937c50fa9b713b3f` |
| `split.js` | `62530412cfba8672de165b8be897fe0ef869142d5da591285e517b50223503da` |
| `split.control.js` | `8cf7d77169f7c709b08fd21eb42a93c54badf7e0b22033d182f1cf35594296a0` |
| `unicode.js` | `ec67a4f9353aaa4e0269b76d3173a6a6501bbc7ca3bed68244e4683cbdf6d3cf` |
| `unicode.control.js` | `a971807c729a121e24ce0945eee9165325b901c632059e6e9c6841367dcc5169` |
| `backtrack.js` | `f52f1ed6049205377d0909db043d4fa1c4570d6394f69d65c509d5a0e0593444` |
| `backtrack.control.js` | `ba1ef3cd30f74100871175a3e0929084dc9b75d0393db33105bcab4cdae4d033` |

| Workload | Control | What it prices |
|---|---|---|
| `scan.js` | `scan.control.js` | A global search for a literal across a long ASCII text, and a search for a literal that first occurs at the text's end |
| `captures.js` | `captures.control.js` | `exec` with named groups and the `d` flag: five named groups over each line of a log, and a global pattern with one named group run again from each line's start |
| `replace.js` | `replace.control.js` | `replace` and `replaceAll` with a global pattern: a template with numbered references, a template with named references, and a replacement function |
| `split.js` | `split.control.js` | `split` by separators with white space around them, by a capturing pattern, and with a limit |
| `unicode.js` | `unicode.control.js` | Property classes under `u` (General_Category, Script, a complement), a class difference, a class intersection and a property of strings under `v`, and case-insensitive literals and ranges under `iu`, `iv` and `i`, over Latin with diacritics, Greek, Cyrillic, Han, ASCII and emoji |
| `backtrack.js` | `backtrack.control.js` | Bounded backtracking: a nested quantifier anchored at both ends, a starred alternation of overlapping choices, a repeated word through a back-reference, quoted strings closed by a back-reference behind a negative look-ahead, and a palindrome through named back-references |

**The two arms run these patterns on different paths.** At the base arm this engine routes the `u` and `v`
patterns, and the few other shapes its routing names, to Broiler.Regex `0.1.0-preview.4`, and the rest to its
translator to .NET's `Regex`; at the after arm every pattern runs through Broiler.Regex's new backend. That move is
what part B prices.

**Each control is the same workload minus the thing measured**: the same strings, patterns, loops and accumulation,
with each regular-expression call replaced by a type test of its input. The difference between a workload and its
control is the cost of the calls; the rule retains it as attribution and decides nothing by it.

**Each answer counts the operations that completed, not their results.** Bundle `sjl-3-001`'s measurement of
Broiler.VM failed its condition because a correction moved a workload's answer, and bundle `sjl-7-001`'s part B
answered with counts for that reason; these do the same. A count is fixed by the loop bounds written in each file, so
no correction of this engine changes it, and a lane that throws or stops early still fails the condition.

**What the workloads stay away from**, so that both arms run every one of them to its count:
- **the rows of the correspondence on which this engine's answer differs from the specification**
  (Broiler.VM's `docs/evidence/sjl-11-001/correspondence.md`): every literal is valid, and no group name or reference
  is spelled with an escape (rows S2a, S2b, S7, S8, S10 and S11); no case-insensitive pattern or subject holds
  U+0130, U+0131, U+212A, U+1F80, U+1F88, U+A7CE or U+A7CF (M2, M2a, M2b and U3); no subject holds a line terminator
  for `^` or `$` (M3); no `lastIndex` is set inside a surrogate pair (M4); `String.prototype.search` is not called
  (M6); no back-reference runs under `u` or `v` (M7); and no `source` is read (T1);
- **this engine's resource defects through Broiler.Regex `0.1.0-preview.4`** on its `u` and `v` patterns (rows B1 to
  B3, B5, B8 and S3b): each of those patterns is a property class, a set operation or a short literal, run over a
  short subject, with no nested quantifier, no long run and no long sequence of terms;
- **the ceilings and the nesting limit** both engines pass from their cut-overs (JSD-0081 decisions D7 and D10): no
  greedy quantifier over a single character or a group runs over a long subject, and no pattern nests deeper than a
  few levels;
- **anything that is not this area's**: no `Intl`, no host capability and no output.

Every character outside ASCII is written as an escape, so each file is ASCII.
