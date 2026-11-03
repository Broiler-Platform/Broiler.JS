# Part A's readings under the rule's revision of 2026-11-02

The lists [`../decision-rule-revised-2026-11-02.md`](../decision-rule-revised-2026-11-02.md) reads, committed with it,
before any run it judges. Tab-separated, UTF-8, LF; `#` lines first, then one column line. None edits the
classification ([`../classification/`](../classification/README.md)), whose digest part A's completeness pins.

| File | What | Decision |
|---|---|---|
| `provider-readings.tsv` | Each input, variant and configuration whose literal the corpus runner's provider refuses, naming EE0019, though the classification names no provider row first: 756 parses. Written by [`../tools/provider_readings.py`](../tools/provider_readings.py) from the classification, the stage table, the catalogue and Broiler.JS's SJL-12 after baseline (entry P); its `check` mode rebuilds it byte for byte | (a) |
| `classification-readings.tsv` | Each input, variant and configuration whose first code the classification names wrongly, with its reading (H1 to H9: the row whose condition holds, admitted first; S1 and S2: `either`, semantics-only): 652 parses | (b) |
| `triage.tsv` | Every group of the run at `920f18f4`'s failures (`../corpus/failure-groups.tsv`'s key: reason, configuration, the classification's rows and the library's first row), split by what answers it - (i) a library site fixed (commit `9d4c7ef3`), (ii) a classification reading, (iii) the grammar pair EX0008/EX0009 (decision (e)), the provider reading (decision (a)) or a limit at its bound (decision (f)) - with its parses. 1,922 parses: 112, 652, 442, 710 and 6 | (a), (b), (e), (f) |

    python3 -I docs/evidence/sjl-13-001/tools/provider_readings.py check \
      --classification docs/evidence/sjl-13-001/classification/classification.tsv.gz \
      --stages Broiler.ECMAScript/docs/frontend/stages.tsv --catalogue Broiler.ECMAScript/docs/frontend/early-errors.tsv \
      --js-baseline docs/evidence/sjl-12-001/after/parse-corpus-c.tsv.gz \
      --out docs/evidence/sjl-13-001/readings/provider-readings.tsv

*(Added 2026-11-02, at SJL-13.2f, with the rule's second revision of that day,
[`../decision-rule-revised-2026-11-02-second.md`](../decision-rule-revised-2026-11-02-second.md), before any run it
judges.)* Two lists are added, each written by a committed script whose `check` mode rebuilds it byte for byte:

| File | What | Decision |
|---|---|---|
| `limit-readings.tsv` | Each input, variant and configuration the classification expects accepted whose source [`../tools/limit_readings.py`](../tools/limit_readings.py) measures past a limit's bound (bracket nesting past the configuration's nesting parameter; a run of one left-associative binary operator past the tree-depth constant), with its measure: 6 parses (J000243, J000799 nesting; J000451 tree; strict and sloppy, in J). Part A admits a refusal at a limit reported at its bound only for a listed line | (f'), replacing (f) |
| `grammar-readings.tsv` | Each input, variant and configuration the classification expects refused naming exactly one of EX0008 and EX0009 first, which the run the triage read (`../corpus/report.tsv.gz`, `920f18f4`) refused with the other's code first, reading G, with the row admitted: 442 parses. Written by [`../tools/grammar_readings.py`](../tools/grammar_readings.py) | (e'), replacing (e) |

    python3 -I docs/evidence/sjl-13-001/tools/limit_readings.py check --manifest conformance/parse/corpus.manifest \
      --corpus conformance/parse --test262 <test262 at ccaac100> \
      --classification docs/evidence/sjl-13-001/classification/classification.tsv.gz \
      --nesting V=64,J=512 --tree-depth 10000 --out docs/evidence/sjl-13-001/readings/limit-readings.tsv
    python3 -I docs/evidence/sjl-13-001/tools/grammar_readings.py check --report docs/evidence/sjl-13-001/corpus/report.tsv.gz \
      --classification docs/evidence/sjl-13-001/classification/classification.tsv.gz \
      --catalogue Broiler.ECMAScript/docs/frontend/early-errors.tsv \
      --out docs/evidence/sjl-13-001/readings/grammar-readings.tsv

*(Noted 2026-11-02, at SJL-13.2f, after review; `triage.tsv` is not edited.)* Three of `triage.tsv`'s outcome labels
are corrected here: "e: limit at the bound" is the first revision's decision (f), not (e), and is now read by (f'); "a:
provider reading P2" is decision (a)'s outcome, and "P2" is defined nowhere (read "a: provider reading"); "iii: grammar
pair EX0008/EX0009" is now read by (e'), the list `grammar-readings.tsv`. This README's first table names (e) and (f)
as the first revision has them.
