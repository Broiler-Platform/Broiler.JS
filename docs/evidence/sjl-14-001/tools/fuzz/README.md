# Part J's seeded mutation session

Bundle `sjl-14-001`'s part J ([`decision-rule.md`](../../decision-rule.md)): "a seeded mutation session, committed to the
bundle, compiling each mutant through CoreScript.Compile at the after commit in Release". Committed with window W2's
change on 2026-11-19, before any run of part J. It runs nothing on its own and judges nothing.

    dotnet run --project docs/evidence/sjl-14-001/tools/fuzz -c Release -- \
        --seeds docs/evidence/sjl-14-001/inputs/fuzz-seeds.txt --test262 <a test262 checkout at the corpus's revision> \
        --out <a new folder>

- **The seeds** are [`inputs/fuzz-seeds.txt`](../../inputs/fuzz-seeds.txt)'s lines, read as [`inputs/README.md`](../../inputs/README.md)
  says: a parse-corpus input by its id, checked against `conformance/parse/corpus.manifest`'s SHA-256, or a file of
  [`inputs/seeds/`](../../inputs/seeds/) by its path and SHA-256. The header's time bound per compile and each line's count
  of mutants apply. Before any seed runs, the session checks the manifest against the SHA-256 the seed list's
  `#% corpus` line pins, and every seed's text against its own; an input that differs stops the session with exit code
  2 before any process runs, as an input error and not a finding *(added 2026-11-19, in review of W2: the manifest's
  SHA-256 had been read and not checked, and a seed's mismatch was recorded as a lost process)*.
- **The mutants** come from a deterministic stream (xorshift64*) seeded by the session's seed (`--session-seed`, a
  hexadecimal value; the program's default when absent) and the seed's id, so a session is the same on every run of the
  same commit. The operators are in `Program.cs`'s header.
- **Each seed runs in a child process** of its own, so that a lost process is a finding rather than the end of the
  session; each mutant compiles on a thread of its own, joined within the bound.
- **The output**: `mutants.tsv` (seed, index, operator, the mutant's SHA-256, accepted, refused or finding, and the type
  and message), `findings.tsv`, each finding's text under `findings/`, and `processes.tsv`.
- **The control** (part J's): a scratch copy of the after commit in which the bridge throws on one node kind the seeds
  produce (for example `BridgeTreeBuilder`'s case for a template literal), built and run over the seeds; the session
  must report a finding. The copy is never committed, and the revert is checked. Unlike part K's `--control`, the
  control is not scripted here: its scratch edit and the check of its revert are the run's own steps, recorded with it.
