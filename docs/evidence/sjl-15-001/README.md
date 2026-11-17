# Bundle SJL-15-001: Broiler.JS on the shared semantic model

**Rules, records and probes only. Nothing here is a verdict, and no run of the rule has been taken.**

**Written:** 2026-11-17, at milestone SJL-15 (work packages SJL-15.0 and SJL-15.1, Broiler.JS's half) of the shared
JavaScript libraries programme, Broiler.VM's `docs/javascript-shared-libraries/milestones/sjl-15-static-semantics.md`.
This repository's records are `docs/roadmap/SharedLibraries.md`, sections "Static semantics (SJL-15.1)" in "Area
verdicts" and "Shared static semantics (SJL-15)", with the decisions that bind this repository by Broiler.VM's JSD-0088's
names and numbers and this repository's own decisions of 2026-11-17. Broiler.VM's half is its bundle `sjl-15-001` and
JSD-0088 (merged in Broiler-Platform/Broiler.VM#214 as `0ddc146f`, and the owner's answer to its decision D-census in
Broiler-Platform/Broiler.VM#215 as `626e4ed8`; observed). **Base:** this bundle was written from Broiler.JS `6fda6ca4`
(Broiler-Platform/Broiler.JS#1119) and Broiler.VM `5e90bfe4` (Broiler-Platform/Broiler.VM#216), each repository's
default branch on 2026-11-17.

## What it holds

| File | What |
|---|---|
| [`decision-rule.md`](decision-rule.md) | The rule: ten predeclared parts, A to J, filed before any change it judges (no model beyond the declarations walk, no model merge, no cut-over, no deletion); it holds no figure |
| [`inputs/closure-suites.txt`](inputs/closure-suites.txt) | Part D's population, the closure-sensitive suites, each test class with its file's digest at `6fda6ca4` and how it entered the list |
| [`dispositions.md`](dispositions.md) | SJL-15.5's deletion list member by member, and the disposition of every public type it empties (SJL-D-25), this engine's half of X6 |

## What it will hold

Each named by the rule, retained when its run is taken, with the commit it ran at: part A's hosted runs at the base and
after commits; part B's identity runs at each model commit after W2; parts C, D, E, H and I's local runs; parts F and G's
runs at the library commit; part J's runs, void on a lane that is not controlled (decision D-measure); and `manifest.json`
(decision D56), written by the bundle's collector at the evidence step.

## The commits

- **The rule commit**: the last commit on this bundle's branch that changes `decision-rule.md`, and with it the merge
  that brings it into the default branch, named here when it exists. The rule is filed by that merge, before any model
  merge, and is never rewritten after it.
- **The model commits, the library commit, the base commit and the after commit**: named here when each exists, as the
  rule's section "The commits, named before they exist" defines them.

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM), and
nothing here has been read by anyone but its author.
