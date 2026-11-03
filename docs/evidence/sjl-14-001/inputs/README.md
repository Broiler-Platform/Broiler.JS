# The inputs of bundle SJL-14-001's rule: part I's workloads and part J's seeds

**Written:** 2026-11-03, at milestone SJL-14 of the shared JavaScript libraries programme, work package SJL-14.1, with
the rule [`../decision-rule.md`](../decision-rule.md) and before any change it judges exists. Nothing here is a run,
and nothing here is a verdict. No figure produced from these files is retained anywhere in this repository except in
this bundle's `measurement/` and `fuzz/` sub-bundles, written at the evidence step (SJL-14.6).

| File | What | Read by |
|---|---|---|
| [`workloads/corpora.tsv`](workloads/corpora.tsv), [`workloads/README.md`](workloads/README.md) | The real corpora of `CompilePhaseMetrics`, each named by its Octane file and SHA-256 at a fixed Octane revision | Part I |
| [`fuzz-seeds.txt`](fuzz-seeds.txt) | The seeds: one line per seed, with its goal, its source, its count of mutants, and the time bound per compile in the header | Part J |
| [`seeds/`](seeds/) | The seeds written for this bundle, each reaching facts the bridge synthesises (decision D44) | Part J, through `fuzz-seeds.txt` |
| [`write_inputs.py`](write_inputs.py) | Writes `fuzz-seeds.txt`; `check` rebuilds it byte for byte | - |

## Part J's seeds

- **What a seed is.** Either an input of the parse corpus, `conformance/parse/corpus.manifest` (whose SHA-256 the
  header names), given by its id and read as bundle `sjl-13-001`'s home seeds read it (its README, "Part F's seeds"),
  or a file of `seeds/`, given by its path and SHA-256.
- **Which seeds.** Every configuration-J line of bundle `sjl-13-001`'s home seed list, whose rule chose each from the
  manifest alone (every input of Broiler.VM's origin, every module of Broiler.JS's, and the first non-negative input of
  each directory of test262's `test/language/` and `test/annexB/language/` two names below `language/`); and every file
  of `seeds/`, which reach what the bridge builds and the shared tree does not hold as Broiler.JS's tree does: Annex B
  block functions and the Annex B.3.4 block, loops with `let` heads and `continue`, tagged templates' raw and cooked
  strings, the `m` suffix, private names and static blocks, direct eval in each context decision D44 lists, the slash
  read as a regular expression or as division, and a module with top-level `await`.
- **How each is compiled.** Under the line's goal through `CoreScript.Compile` at the after commit in Release, by the
  seeded mutation session the after commit's change commits to the bundle; the mutation operators and the seed of
  their deterministic stream are the session's. The header's bound applies to each compile.
- **What a line is not.** An expectation. Part J reads only whether a mutant is accepted or refused with a
  SyntaxError; any other exception type, a lost process or a compile past the bound is a finding.

## Part I's workloads

See [`workloads/README.md`](workloads/README.md).
