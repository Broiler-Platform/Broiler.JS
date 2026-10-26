# The inputs of bundle SJL-13-001's home rule: part F's fuzz seeds and part G's deep-nesting corpus

**Written:** 2026-10-26, at milestone SJL-13 of the shared JavaScript libraries programme, work package SJL-13.1,
with the rule [`../decision-rule.md`](../decision-rule.md) and before any code of the shared syntax library exists.
**Written by:** [`scripts/frontend/write_home_inputs.py`](../../../../scripts/frontend/write_home_inputs.py), from the
parse corpus's manifest and from the measure below; nothing here is edited by hand. From the repository root,
`python3 -I scripts/frontend/write_home_inputs.py check` rebuilds every file in memory and compares it with this
folder byte for byte, and `write` writes them. Two runs write the same bytes: no clock, locale, hash order or
file-system order reaches them. Nothing here is a run, and nothing here is a verdict.

| File | What | Read by |
|---|---|---|
| [`fuzz-seeds.txt`](fuzz-seeds.txt) | The seeds of the library's fuzz target: one line per seed and configuration, with its goal, its configuration, its count of mutants, and the time bound per parse in the header | Part F |
| [`deep/manifest.tsv`](deep/manifest.tsv) and the sources beside it | The deep-nesting corpus: one line per source and nesting parameter, with the source's SHA-256 and its expected outcome under that parameter | Part G |

## Part F's seeds

- **What a seed is.** An input of the parse corpus, `conformance/parse/corpus.manifest`, whose SHA-256 the file's
  header names, given by its id: its text is the corpus's (a test262 input read from test262 at
  `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e` and checked against the manifest's SHA-256, any other from
  `conformance/parse/inputs/`), without the manifest's strict prologue.
- **Which inputs.** Every input of Broiler.VM's origin; every module of Broiler.JS's origin; and, for each directory
  of test262's `test/language/` and `test/annexB/language/`, taken two names below `language/`, the first input in
  the manifest's order that is no negative test. The `selection` column names which rule chose the line. The rule
  reads the manifest alone, so the set does not depend on either engine's answers.
- **How it is parsed.** Under the line's goal (`script` or `module`; a `json` input under the Script goal, as part A
  reads a `json` variant) and the line's configuration, V or J, as the rule's section "The two configurations" sets
  them. Each line gives the count of mutants the target makes from that seed in that configuration; the mutation
  operators, and the seed of their deterministic stream, are the fuzz target's, which SJL-13.2's stage 4e writes
  (decision D22). The header's time bound applies to each parse of a mutant.
- **What a line is not.** An expectation. Part F reads only whether a parse returns a tree or diagnostics, and its
  findings are an exception escaping the library, a lost process, a parse past the time bound, or a diagnostic with
  no shared code of the catalogue.

## Part G's deep-nesting corpus

Each source is a Script, one line, ASCII, ending in a line feed, parsed alone in its own process under configuration
V's request with the nesting parameter its row names. Its row names its shape, its kind (`nesting` or
`tree-depth`), its levels, its charge under the measure below, and its expected outcome: `accepted` when the charge
is at most the bound, and `refused` when it is past it, with the diagnostic of its kind: the contract's nesting
diagnostic, EX0011's shared code (`limit.nesting.too-deep`), for a `nesting` row, and the tree-depth diagnostic,
EX0012's (`limit.tree.too-deep`), for a `tree-depth` row. For every shape
there are sources at the bound, one past it, and far past it.

**The bounds.** Two nesting parameters, Broiler.VM's default and the largest a Broiler.VM request may carry
(`SliceParseOptions.DefaultMaximumNestingDepth` and `MaximumSupportedNestingDepth` at Broiler.VM `4cf9ca4`); and the
tree-depth bound, the library's constant, which decision D14 makes equal to Broiler.VM's
`SliceParseOptions.MaximumTreeDepth`, a test holding the equality. The tree shapes name the default nesting
parameter, which none of them approaches.

**The measure** is Broiler.VM's wide parser's at `4cf9ca4`, the front end whose refusals Broiler.VM's rule for step 1
holds unchanged, with the one change decision D14 makes:
- **nesting** is charged one level for each statement and each assignment expression the parse enters
  (`JsParser.Enter`, at `ParseStatement` and `ParseAssignment`), and one for each binding pattern, recursing unary
  prefix and `new` (`JsParser.EnterNesting`). So `var x = ` and its initialiser take two levels; each bracket, each
  parenthesis and each template take one more, their element, their contents and their substitution being
  assignment expressions; and each block and each function declaration takes one, being a statement;
- **tree depth** is charged one level for each node a binary chain or a member or call chain adds (`JsParser.Deepen`).
  A parenthesis, a template and a substitution add none. **A left operand's spine counts toward the chain it begins**
  (decision D14, and row R14 of the correspondence in Broiler.VM's bundle `sjl-13-001`), where Broiler.VM's base
  charges a chain from the depth at the chain's own start. So each of the four left-operand shapes, a chain of `a`
  nodes as the left operand of a chain of `b` nodes, is charged `a + b`.

**What the measure was checked against.** Each of these was run at Broiler.VM's base and answers where the measure
puts the bound: Broiler.VM's `--checks` rows "template: nesting counts toward the nesting bound, and past it is
refused with 2103", "template: nesting around a template and inside its substitution adds up toward the nesting
bound", "the binder walks a tree at the tree-depth bound" and "a tree one level past the tree-depth bound is refused
before the binder" (bundle `sjl-12-001`'s `after/checks-after.log.gz` in Broiler.VM); the correspondence's probes RP06
(a member chain one past the tree-depth bound) and RP09 (parentheses nested far past the nesting bound, refused where
the measure puts the bound); and test262's `test/language/statements/function/S13.2.1_A1_T1.js`, whose refusal in
Broiler.VM's SJL-12 after baseline (corpus input `T050099`) falls at the expression of the first function the measure
puts past the default bound. The block, bracket and function-declaration shapes rest on the same two charge points
(`ParseBlock` and a function's body parse each statement through `ParseStatement`; an array literal's element is an
assignment expression); no run at the base has them at the bound. The left-operand shapes are charged as the library
charges them (decision D14), which no front end does today.

**How it is read.** A source at or below its bound that the library refuses, or one past its bound that it accepts or
refuses with a diagnostic other than its kind's, fails part G. If the failure is the corpus's and not the library's, only a new dated
rule file may correct it, quoting the rule whole (its section "Revisions").
