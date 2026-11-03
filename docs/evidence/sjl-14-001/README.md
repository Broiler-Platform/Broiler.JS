# Bundle SJL-14-001: Broiler.JS on the shared syntax library

**Rules only. Nothing here is a verdict.**

**Written:** 2026-11-03, at milestone SJL-14 (work packages SJL-14.0 and SJL-14.1) of the shared JavaScript libraries
programme, Broiler.VM's `docs/javascript-shared-libraries/milestones/sjl-14-broiler-js-syntax.md`. This repository's
record is `docs/roadmap/SharedLibraries.md`, section "Broiler.JS on the shared syntax library (SJL-14)", with the
owner's decisions D40 to D56 and D64 (of 2026-11-03, but D41 and D44 to D47, of 2026-10-26 as Broiler.VM's JSD-0083
revised them; D45 revised again on 2026-11-03, finding S14-F1). SJL-14 files no verdict of its own: Broiler.VM's JSD-0083, SJL-13.1's area
verdict, covers this engine's front end too (decision D40). **Base:** this bundle's records were written from
Broiler.JS `33d3093b` and Broiler.VM `c5bde2b`, each repository's default branch on 2026-11-03.

## What it holds now

| File | What |
|---|---|
| [`decision-rule.md`](decision-rule.md) | The rule: eleven predeclared parts, A to K, filed before any change it judges (no bridge, no switch, no adapter package, no deletion); it holds no figure |
| [`corrections.md`](corrections.md) and [`corrections/`](corrections/k-rows.tsv) | The corrections K1 to K10, each with its rows and test262 paths; the classes E and T with their rows and sites; finding S14-F1; the probes' rows |
| [`dispositions.md`](dispositions.md), [`dispositions.tsv`](dispositions.tsv) and [`api/`](api/) | Every public type and member of `Ast` and `Parser` with its disposition (decision D50), from a PublicApiGenerator listing at `33d3093b`; the non-tree types' new home; SJL-14.5's deletion list member by member |
| [`inherited-context/`](inherited-context/README.md) | The inherited-context probes of decision D44, run on both engines with every answer recorded |
| [`inputs/`](inputs/README.md) | Part I's workloads (the real corpora, named by revision and SHA-256) and part J's seeds |
| [`tools/records/`](tools/records/write_records.py) | The script that writes `corrections/`'s generated tables and `dispositions.tsv`, with a `check` mode |
| [`tools/side-by-side/`](tools/side-by-side/README.md) | Part C's tool, with entry points P, B, K and K-on and sjl-12-001's canonical dump; `compare.py` (outcome pairs, part B's identity, class M's message table) and `spancheck.py` (class S's span check); landed with W1 (SJL-14.2) |

## What it will hold

Each named by the rule, retained when its run is taken, with the commit it ran at:
- `bridge/` - part B's identity runs at W1 and its first parent, and at each dark commit and its first parent, with
  their switch-on outputs (decision D52);
- `side-by-side/w1/` and `side-by-side/w2-base/` - part C's runs, every difference classified, with the generated
  message table of class M (decision D46), written by the side-by-side tool, `tools/side-by-side/`, which W1 adds;
- `base/` and `after/` - part A's hosted runs (UTC, negative tests), imported unchanged with their digests, and the
  comparison; parts D, E, F, G, H and K's local runs;
- `measurement/` - part I's runs, void on a lane that is not controlled (decision D48);
- `fuzz/` - part J's session;
- `manifest.json` (decision D56), written by the bundle's collector at the evidence step.

## The commits

- **The rule commit**: the last commit on this bundle's branch that changes `decision-rule.md`, and with it the merge
  that brings it into the default branch, named here when it exists. The rule is filed by that merge, before any
  bridge, switch or run, and is never rewritten after it. *(Noted 2026-11-03, at SJL-14.1, before merge: the branch's
  first commit carried the rule as first written; a later commit of the same branch corrects, in place, its class E
  bullet (finding S14-F1, worded plainly after review). Nothing was filed before the merge, so no rule was rewritten.
  The later commit also follows the one that adds `docs/roadmap/SharedLibraries.md`'s SJL-14 section, which the
  rule's header names.)*
- **W1, the bridge commit**, and **W2, the after commit**, with their first parents (W2's is the base commit), and any
  dark commit between them: named here when each exists, as the rule's section "The commits, named before they exist"
  defines them.

## What it does not claim

- **No figure.** No count, timing, ratio or score is written in this README; the bundle's runs hold their own. No
  test262 suite total is written here at any step (rule H5).
- **The parse thread's stack.** The rule writes it as "the 256 MiB thread" (its section on the corrections) and "a 256
  MB stack" (parts B and D), both after SJL-13's plan's section 4.4; both name the one stack bundle `sjl-12-001`'s tool
  uses, which that bundle records as "256 MB" and its tool sets as `256 * 1024 * 1024` bytes
  (`docs/evidence/sjl-12-001/tools/parse-corpus/Program.cs:23`).
- **Clause 6** is read as the milestone file states it when part I is read; what it becomes while no controlled lane
  exists is the owner's own choice (decision D48), not made here.
- **Exclusions:** none.

| Exclusion | What | Why |
|---|---|---|
| - | - | - |
