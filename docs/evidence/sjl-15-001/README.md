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
| [`engine/`](engine/commands.txt) | The `semantics` vectors' runs: each engine's first run on corpus revision 42, with its negative control; both engines' runs on revision 43 as drafted, for the vectors it adds; and both engines' runs on revision 43, with their negative controls. Not runs of the rule *(noted 2026-11-24, at corpus revision 44: the folder also holds both engines' runs on revision 44 as drafted, for the vectors it adds, and both engines' runs on revision 44, with their negative controls; the cell is left as written)* |
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

*(Noted 2026-11-18, before any run of the rule and before any SJL-15.2 code.)* The paragraph above, after its lead-in,
reads "The column and the deletion list reach Broiler.VM's bundle `sjl-15-001` and JSD-0088 by a dated note in a
Broiler.VM change of its own (decision D-x2), which this change does not make; until it merges, JSD-0088 records no
row's class across both engines, and no SJL-15.2 code merges." That change has merged:
**Broiler-Platform/Broiler.VM#217, merged as `450c785fd7773bf761c8bd857f9e4bc3dedd9869`** (first parent `5e90bfe4`) on
Broiler.VM's default branch, observed in Broiler.VM's history at `32b0fad0`. It fills the column "Broiler.JS" of
Broiler.VM's bundle `sjl-15-001`'s `correspondence.md` from this bundle's
[`correspondence-broiler-js.md`](correspondence-broiler-js.md), by the dated note of 2026-11-17 in Broiler.VM's
`correspondence.md`, and adds JSD-0088's dated note completing X2 and X6, which reads "No row's class is unknown" and
"Both engines' halves of X2 and X6 are then filed; this note meets no clause and moves no gate." This bundle's own
change had merged before it, in Broiler-Platform/Broiler.JS#1120 as `e4e45d38`.

**What that merge unblocks, by the rule's words.** The rule's revision of 2026-11-17, matter 1, says of a model commit
that "**it lands after** Broiler.VM's SJL-15.1 rule has merged (Broiler-Platform/Broiler.VM#214, `0ddc146f`,
observed) **and after** the Broiler.VM change that carries this engine's column and JSD-0088's dated note completing X2
and X6 has merged (decision D-x2), each merge observed and named, with its commit, in this bundle's README before any
run at the model commit is taken." The first merge is named in this README's opening paragraph; this note names the
second, with its commit. A merge of SJL-15.2 that lands from now on lands after both, and the paragraph above's "until
it merges" has passed.

**What it does not unblock.** It lets no SJL-15.2 code merge by itself. The eval-code inputs come first: this README's
note above reads "The `semantics` vectors do not yet hold the eval-code inputs of probes V029, V030 and V046, which the
probes show and revision 43 did not carry: they are next, by decision D-js-revisions' note of 2026-11-17", and that
note, in `docs/roadmap/SharedLibraries.md`, puts them in "a later corpus revision of the area, classified on both
engines' runs as revision 43's additions were, before any SJL-15.2 code merges". Broiler.VM's bundle `sjl-15-001`'s
README, as Broiler-Platform/Broiler.VM#217 left it, lists the same in "What waits, in order": "Broiler.JS's later
corpus revision with the eval-code inputs of probes V029, V030 and V046, classified on both engines' runs, before any
SJL-15.2 code". No such revision exists yet: `conformance/vectors/REVISIONS.md` ends at revision 43. SJL-11.5's hold
binds as well: the rule reads "None lands while SJL-11.5's bases are held (bundle `sjl-11-001`'s README, decision
D16)", and its revision leaves it: "The rest of the definition (W2, the dark windows of bundle `sjl-14-001`, SJL-11.5's
hold) is unchanged." No SJL-15.2 merge lands while those bases are held. This note names a merge and judges nothing;
the paragraph above is left as written.

*(Noted 2026-11-24, at corpus revision 44, before any SJL-15.2 code.)* The note above reads "No such revision exists
yet: `conformance/vectors/REVISIONS.md` ends at revision 43." This change adds it: corpus revision 44 (that file's row
44) adds the eval-code inputs of probes V029, V030 and V046 as `DirectEval` vectors, `since: 44`, and classifies both
engines' runs on them as revision 43's additions were, in `scripts/conformance/semantics-divergences.txt`: Broiler.JS's
at the default branch's engine `4f697339`, after SJL-14's W2, and Broiler.VM's at its origin/main `90e4cd24` from a
scratch pin. The runs and their commands are in [`engine/`](engine/commands.txt). Broiler.JS differs on V046's input
alone, accepting it (row DE3, a correction of Broiler.JS, C1's row EA0007); Broiler.VM differs on none of them, so the
vectors do not show the answer row AB1 of the correspondence records for Broiler.VM on probes V029 and V030, whose
sources ran as an indirect eval's code. The revision lands by this change's merge. It is no run of the rule, lets no
SJL-15.2 code merge by itself and judges nothing; the note above is left as written.
