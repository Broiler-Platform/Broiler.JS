# Bundle SJL-14-001's parse workloads (part I)

These are the population of part I of bundle `sjl-14-001`'s [rule](../../decision-rule.md), Broiler.JS's parse
measurement at milestone SJL-14 of the shared JavaScript libraries programme (template T5, clause 6). **No figure
produced from them is retained anywhere in this repository except in the bundle's `measurement/` sub-bundle.** Clause 6
is read as the milestone file states it when part I is read; while no controlled lane exists the part is void, neither
MET nor NOT MET, and what clause 6 becomes then is the owner's own choice (decision D48), which this bundle does not make.

**The workloads are the real corpora of `CompilePhaseMetrics`** (`Broiler.JS/benchmarks/Broiler.JavaScript.Engine.Benchmarks/`),
as `CompileProfileMetrics.LoadCorpora` takes them from an Octane checkout at `33d3093b`: CodeLoad's Closure and jQuery
payloads, Mandreel, pdf.js, the TypeScript compiler and Box2D. [`corpora.tsv`](corpora.tsv) names each by its file
and SHA-256 at Octane `570ad1ccfe86e3eecba0636c8f932ac08edec517` (`https://github.com/chromium/octane`, its default
branch on 2026-11-03). **No Octane file is copied into this repository**: a lane checks out that revision and checks
every SHA-256 before its first run, and a file that differs makes the lane's runs void. The two CodeLoad workloads are
the payloads `CompileProfileMetrics.ExtractCodeLoadPayloads` recovers from `code-load.js`.

**The quantity** is the time from source text to the tree the compiler reads, on the calling thread: at the base,
`FastParser.ParseProgram`; after, the hop to the parse thread, the frontend's parse, the bridge and the return
(decision D47). Each lane's condition, checked before and after, is that every workload parses to the outcome the base
gives. The harness, the repetitions and the warm-up are those `docs/roadmap/Measurement.md` fixes for the lane the
bundle names, one corpus per process (`--compile-phases`' rule).

**Why the real corpora and no synthetic workload.** The rule names them: large real programs, machine generated
(Mandreel) and hand written, which `CompilePhaseMetrics` already parses at the base. Whether a correction of K1 to K10
moves any of their outcomes is not known before the after commit exists; the lane's condition finds it, and the rule
reads a condition that fails on a controlled lane as NOT MET.
