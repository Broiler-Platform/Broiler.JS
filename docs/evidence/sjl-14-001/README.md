# Bundle SJL-14-001: Broiler.JS on the shared syntax library

**Rules only. Nothing here is a verdict.**

**Written:** 2026-11-03, at milestone SJL-14 (work packages SJL-14.0 and SJL-14.1) of the shared JavaScript libraries
programme, Broiler.VM's `docs/javascript-shared-libraries/milestones/sjl-14-broiler-js-syntax.md`. This repository's
record is `docs/roadmap/SharedLibraries.md`, section "Broiler.JS on the shared syntax library (SJL-14)", with the
owner's decisions D40 to D56 of 2026-11-03. SJL-14 files no verdict of its own: Broiler.VM's JSD-0083, SJL-13.1's area
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

## What it will hold

Each named by the rule, retained when its run is taken, with the commit it ran at:
- `bridge/` - part B's identity runs at W1 and its first parent, and at each dark commit and its first parent, with
  their switch-on outputs (decision D52);
- `side-by-side/w1/` and `side-by-side/w2-base/` - part C's runs, every difference classified, with the generated
  message table of class M (decision D46); the side-by-side tool, `tools/side-by-side/`, lands with W1;
- `base/` and `after/` - part A's hosted runs (UTC, negative tests), imported unchanged with their digests, and the
  comparison; parts D, E, F, G, H and K's local runs;
- `measurement/` - part I's runs, void on a lane that is not controlled (decision D48);
- `fuzz/` - part J's session;
- `manifest.json` (decision D56), written by the bundle's collector at the evidence step.

## The commits

- **The rule commit**: the commit on this bundle's branch that carries `decision-rule.md`; its merge into the default
  branch is named here when it exists.
- **W1, the bridge commit**, and **W2, the after commit**, with their first parents (W2's is the base commit), and any
  dark commit between them: named here when each exists, as the rule's section "The commits, named before they exist"
  defines them.

## What it does not claim

- **No figure.** No count, timing, ratio or score is written in this README; the bundle's runs hold their own. No
  test262 suite total is written here at any step (rule H5).
- **Clause 6** is read as the milestone file states it when part I is read; what it becomes while no controlled lane
  exists is the owner's own choice (decision D48), not made here.
- **Exclusions:** none.

| Exclusion | What | Why |
|---|---|---|
| - | - | - |
