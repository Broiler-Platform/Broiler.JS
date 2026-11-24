# The after commit's local runs: parts D, E, F, G, H, J and K (retained)

**Taken:** 2026-11-23, at the after commit `83a9bd82a5d258d3001468095ebf43f9efc3e9a7` (the merge of
Broiler-Platform/Broiler.JS#1129, which lands W2; its first parent is the base commit
`69a8a13bc446a1714d3c8be9be3cfd742aebee6e`), after W2's merge ended the hold, with the default branch's head read as
the after commit before every step. **Retained:** 2026-11-24, on branch `sjl-14-after`. These are the local after runs
[`../../decision-rule.md`](../../decision-rule.md) names: part D's tool at the after commit, with the reading aids the
rule's revision of 2026-11-23 reads by ([`../../decision-rule-revised-2026-11-23.md`](../../decision-rule-revised-2026-11-23.md));
part E's test project, collector and closure listing; part F's packing, feed check, pristine consumer, its control and
the metadata reader; part G's after candidate feed and both suites against it; part H's API lister; part J's session
and its control; part K's deletion search and its control. Part A's after run is the hosted one in
[`../part-a/`](../part-a/imported.txt). Nothing here reads a part; the bundle README's "W2's after commit, recorded on
2026-11-24" does.

## What was run

[`retained.txt`](retained.txt) names every step, its command, its outputs and its exit. In short: from a clean
`git archive` of the after commit on disk, outside every working tree, each step through the machine-wide lock and
idle gate ([`scripts/run.sh`](scripts/run.sh)), by [`scripts/adrive.sh`](scripts/adrive.sh) and
[`scripts/astep.sh`](scripts/astep.sh) under [`scripts/achain.sh`](scripts/achain.sh), every test, tool and engine run
under `env -i` with `TZ=UTC` and no `LANG`, `LC_*` or `BROILER_*` variable, builds in Release with no build server. The
base's harness is followed step for step where a step has a base counterpart; each departure from it is named in
`retained.txt`.

## What is retained

Every file of the run folder, unchanged, at the same path, but the files over 2 MB, which are retained by digest only
([`not-retained.sha256.txt`](not-retained.sha256.txt)), as [`../../base/local/`](../../base/local/README.md) retains the
base's; two files over 2 MB that the after comparisons read whole are kept whole here, as the base's records part 4
kept their base counterparts: part D's dump report [`partd/parse-corpus-d1.tsv.gz`](partd/parse-corpus-d1.tsv.gz) and
Broiler.HtmlBridge's TRX [`html/htmlbridge/trx/htmlbridge.trx`](html/htmlbridge/trx/htmlbridge.trx). The drivers'
scripts are in [`scripts/`](scripts/) as they ran and, for the three replaced while the chain ran, as they were started
(`*.before-fix`), with the patch that replaced them; the chain's log and the memory log are
[`chain.log`](chain.log) and [`memwatch.log`](memwatch.log). [`hashes.txt`](hashes.txt) gives the SHA-256 of every file
here, and a [`.gitattributes`](.gitattributes) keeps the bytes as committed.
