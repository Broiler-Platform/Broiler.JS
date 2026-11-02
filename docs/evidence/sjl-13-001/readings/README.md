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
