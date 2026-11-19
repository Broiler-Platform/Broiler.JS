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
  of mutants apply.
- **The mutants** come from a deterministic stream (xorshift64*) seeded by the session's seed (`--session-seed`, a
  hexadecimal value; the program's default when absent) and the seed's id, so a session is the same on every run of the
  same commit. The operators are in `Program.cs`'s header.
- **Each seed runs in a child process** of its own, so that a lost process is a finding rather than the end of the
  session; each mutant compiles on a thread of its own, joined within the bound.
- **The output**: `mutants.tsv` (seed, index, operator, the mutant's SHA-256, accepted, refused or finding, and the type
  and message), `findings.tsv`, each finding's text under `findings/`, and `processes.tsv`.
- **The control** (part J's): a scratch copy of the after commit in which the bridge throws on one node kind the seeds
  produce (for example `BridgeTreeBuilder`'s case for a template literal), built and run over the seeds; the session
  must report a finding. The copy is never committed, and the revert is checked.
