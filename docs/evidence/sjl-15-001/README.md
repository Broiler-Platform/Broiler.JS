# Bundle SJL-15-001: Broiler.JS on the shared semantic model

**Rules, records, probes and the `semantics` vectors' runs only. Nothing here is a verdict, and no run of the rule has been taken.**

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
| [`decision-rule-revised-2026-11-17.md`](decision-rule-revised-2026-11-17.md) | The rule's first revision, written before any run it judges: a model commit's conditions on Broiler.VM's merges, what part J decides, the commit that carries the class tables and the column, and part D's input; it quotes the rule whole *(row added 2026-11-17, after review)* |
| [`inputs/closure-suites.txt`](inputs/closure-suites.txt) | Part D's population, the closure-sensitive suites, each test class with its file's digest at `6fda6ca4` and how it entered the list |
| [`dispositions.md`](dispositions.md) | SJL-15.5's deletion list member by member, and the disposition of every public type it empties (SJL-D-25), this engine's half of X6 |
| [`correspondence-broiler-js.md`](correspondence-broiler-js.md) | This engine's column of Broiler.VM's correspondence, read from code at `6fda6ca4`, each row with its class across both engines (decisions D-x2 and D-js-column), and its findings J1 to J7 |
| [`probes/`](probes/README.md) | The probes that settle the rows a reading cannot, run on both engines' default branches, every answer recorded beside the specification's |
| [`corrections.md`](corrections.md) and [`corrections/`](corrections/class-t.tsv) | The classes E and T and the corrections C1, each by row, before any run, written by [`tools/write_lists.py`](tools/write_lists.py) from the stage table and the probes |
| [`engine/`](engine/commands.txt) | The `semantics` vectors' runs: each engine's first run on corpus revision 42, with its negative control; both engines' runs on revision 43 as drafted, for the vectors it adds; and both engines' runs on revision 43, with their negative controls. Not runs of the rule |
| [`hashes.txt`](hashes.txt) | The SHA-256 of every other file of the bundle |

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

*(Noted 2026-11-17, after review, before any run of the rule.)* The rule commit was defined above as "the last commit on
this bundle's branch that changes `decision-rule.md`", which is `31ad8140`, a commit that carries none of
`corrections.md`, its tables, this engine's column or the probes, though the rule says they are "Committed in the same
change". They were added in `750630fb`, after the records and corpus revision 42, while Broiler.JS's first run on
revision 42 was being taken and before any result of any vector run had been seen. By the rule's revision of 2026-11-17
(matter 3), "the same change" is this branch's change, filed by its one merge, and **the rule commit is the last commit
on this branch that changes `decision-rule.md` or `decision-rule-revised-2026-11-17.md`**, with that merge: it carries
the rule, its revision, the class tables, the column and the probes together. The same revision says that a model commit
lands after Broiler-Platform/Broiler.VM#214 and after the Broiler.VM change of decision D-x2 have merged, each observed
and named here, rather than descending from them (matter 1).

*(Noted 2026-11-17, after review.)* Three files' prose was revised in place after the `semantics` vectors' first runs
(revision 42) had been seen: `corrections.md`, `correspondence-broiler-js.md` and `probes/README.md`, each in
`1f1f2bab` and again after review. Each holds a dated note quoting what was replaced. The class tables under
`corrections/` are as first committed in `750630fb` but for one row of `corrections.tsv`, C1's row of probe
`V046-EA0006`, moved from EA0006 to EA0007 after review, when the probe was found to test EA0007's step
(`corrections.md`'s note); no run of the rule had been taken. The `semantics` vectors do not yet hold the eval-code
inputs of probes V029, V030 and V046, which the probes show and revision 43 did not carry: they are next, by decision
D-js-revisions' note of 2026-11-17 in `docs/roadmap/SharedLibraries.md`.

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM), and
nothing here has been read by anyone but its author.

**What this repository's half leaves to Broiler.VM.** The column and the deletion list reach Broiler.VM's bundle
`sjl-15-001` and JSD-0088 by a dated note in a Broiler.VM change of its own (decision D-x2), which this change does not
make; until it merges, JSD-0088 records no row's class across both engines, and no SJL-15.2 code merges.
