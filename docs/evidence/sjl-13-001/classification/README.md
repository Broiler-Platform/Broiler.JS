# The parse corpus, classified at the shared front end's syntax half (SJL-13.1, task 1.2)

**Written:** 2026-10-26, at milestone SJL-13 of the shared JavaScript libraries programme, work package SJL-13.1
(task 1.2 of the SJL-13 to SJL-16 plan, decision D26), on a branch from `8a95076c`, before any code of the shared
front end exists. **Read by:** bundle `sjl-13-001`'s rule in this repository, part A ("the parse corpus and test262's
syntax tests at library level, as classified"), which this classification fixes before the library can shape its
own expectations. **Built by:**
[`scripts/frontend/classify_parse_corpus.py`](../../../../scripts/frontend/classify_parse_corpus.py) from the stage
table [`Broiler.ECMAScript/docs/frontend/stages.tsv`](../../../../Broiler.ECMAScript/docs/frontend/stages.tsv) and the
inputs below. Nothing here is a run of the library, and nothing here is a verdict. It holds no figure.

## What is here

| File | What |
|---|---|
| `classification.tsv.gz` | One line per corpus input, variant and configuration, and per eval request: the expected outcome at the syntax half, the catalogue rows whose shared codes may come first, the reason and its basis. Its `#%` lines name every input by path and SHA-256 |
| `reasons.tsv` | Every reason code the table uses, with its meaning |
| `join-rules.tsv` | Every bucket of SJL-13.1's corpus join, the rows of the correspondence that hold it, and how an input of it is resolved |
| `compile-messages.tsv` | Broiler.JS's compile-stage messages, read as the catalogue's message identifiers (used only for a `D:stage-E` input that is no test262 parse-negative test) |

## The table

Gzip-compressed UTF-8, tab-separated, LF line endings, no quoting; `#%` lines first, then one column line, then the
rows in the manifest's order, each input's variants in the manifest's order and V before J, then its eval requests
E0 to E7. A cell is never empty; `-` means "none".

| Column | Values |
|---|---|
| `id` | the manifest's id |
| `variant` | `strict`, `sloppy`, `raw`, `module` or `json`, as the manifest gives them; or `E0` to `E7`, an eval request under that flag set of the manifest's `eval-flags` |
| `config` | `V` or `J` (below); an eval request is in `V` only |
| `expect` | `accepted`; `refused`; or `either`, an input the syntax half leaves to SJL-15's model, which part A admits accepted or refused ("semantics-only") |
| `first` | for `refused`, the catalogue rows (`;`-joined, by id) whose shared codes may come first: the shared code of each is the catalogue's `shared_code`; `-` otherwise |
| `reason` | a code of `reasons.tsv` |
| `basis` | `;`-joined: catalogue rows, rows of the correspondence (T, P, N, D, R), and baseline facts: `VB:<code>` (Broiler.VM's first registry code in B1), `B3:<set>` (Broiler.VM's answer in B3), `as:<reading>` (the reading an eval request takes its expectation from) |

## The two configurations (decision D26)

The expectations follow the request each configuration sets, as the rule's part A describes it, and these readings
of it:
- **V, Broiler.VM's request.** Every proposal flag off; no lexer extension; Annex B on for scripts; HTML-like comments
  by goal; the nesting parameter at Broiler.VM's default; top-level `await` on for the Module goal, as Broiler.VM's
  module options set it; the regular-expression provider answers as Broiler.VM's validator does (the binder's
  `CheckRegExp` at SJL-13.1's head).
- **J, Broiler.JS's request.** The proposal flags of the forms Broiler.JS parses today on: decorators, auto-accessors,
  and the import phases' call forms `import.source(…)` and `import.defer(…)`; the declaration forms off (one flag per
  form, the correspondence's F16); the `m` extension on (no corpus input holds an `m` literal); the nesting parameter at
  the library's supported maximum; top-level `await` on for the Module goal and off for the Script goal, as Broiler.JS's
  module and script options set it; the provider answers as Broiler.JS's validator does (`RegExpValidator` in its
  scanner).
- **Eval requests** (V only): every eval-family input, as eval code under each of the manifest's flag sets E0 to E7.

**Two things the runner of part A must supply.** In V and J the regular-expression provider must answer as that
engine's validator answers, because the classification marks the validators' disagreements by configuration (the
correspondence's F29(c); rows R18 to R20): this home holds neither engine's validator behind the provider's interface
yet. And a `json` row is read under the Script goal, because the syntax half has no JSON goal (the correspondence's
P04), as SJL-12's tool read it.

## How each expectation is made

1. **A test262 test whose front matter says `negative: phase: parse`** takes the catalogue rows that explain it
   (`docs/evidence/sjl-12-001/catalogue/parse-negative.tsv`: the rows of each engine's first refusing site and the
   rows whose `test262` cell names it; EG0001 for the module variant only). If a row the stage table assigns `syntax`
   refuses in the configuration, the input is expected refused and those rows may come first. A proposal's row
   refuses only where its flag is off; a regular-expression row only where that configuration's validator refused the
   input (Broiler.VM's B1 for V, Broiler.JS's entry P for J). If no such row refuses and a row is `semantics`, the input
   is `either`. If the only rows are a validator's that the configuration admits, it is expected accepted, with the
   correspondence's R20 in its basis.
2. **Any other input both engines' SJL-12 after baselines treat alike** (Broiler.VM's B1 in the bytecode form,
   Broiler.JS's entries P and K) is expected as they treat it: accepted; or, where both parsers refuse it, refused
   with the `syntax` rows of the catalogue whose `vm_code` is Broiler.VM's first code. A test262 positive both refuse
   is the correspondence's P06b (a proposal's declaration form, off in both configurations) or P35 (a fixture that
   holds a deliberate error). A resolution-phase test both refuse at an import attribute the host cannot honour is
   EX0021's, an `engine` row: accepted.
3. **An input the baselines treat differently** is in SJL-13.1's corpus join, and its bucket decides
   (`join-rules.tsv`). A test262 parse-negative test is still expected by rule 1, and its bucket's rows of the
   correspondence join its basis. Any other input is resolved as the bucket's row says: a correction by the
   specification's answer; a proposal by the configuration's flag; a validator's difference by the configuration's
   validator; a limit by the configuration's bound (part A admits a refusal past the bound as outcome L); an `engine`
   row, a module graph (P34) or a difference of SJL-12's entry K (P36, R04) as accepted; a `D:stage-E` input by the
   stage of the catalogue row its compile-stage message names (`compile-messages.tsv`, narrowed to the rows whose
   `vm_code` is Broiler.VM's code).
4. **A `json` row** is expected as Broiler.JS's entry P read it under the Script goal (P04).
5. **An eval request** takes Broiler.VM's B3 answer: accepted if B3 accepted it. If B3 refused it, it takes the
   expectation of the reading of the same text (a B1 variant of the same strictness first, then the others, then an
   earlier flag set) whose first diagnostic is the same; failing that, the stage Broiler.VM's code shows: a code only
   its tokenizer or parser emits, or a strict reading refused where the sloppy one is accepted or refused later in the
   text, is the parser's (refused); a code only its binder emits is `either`.

The script refuses to write anything if an input of the corpus, a variant, a configuration or an eval request has no
expectation, if a bucket of the join has no rule, or if a compile-stage message it must read is not in its table.

## The stage table

`Broiler.ECMAScript/docs/frontend/stages.tsv` assigns every row of the early-error catalogue to one stage of decision
D10 and records each engine's current stage and site, re-read at the heads of SJL-13.1's correspondence (Broiler.VM
`4cf9ca4`, Broiler.JS `d6795fc8`, whose sites are unchanged at `8a95076c`). The catalogue's own cells stay as filed
(`early-errors.md`, section 4; decision D28). Tab-separated, one column line, LF, no quoting:

| Column | Values |
|---|---|
| `id`, `shared_code` | the catalogue's |
| `stage` | `syntax` (the library at SJL-13), `request` (a switch of the request the library applies: EX0023, top-level `await`), `engine` (kept by its engine; the library never reports it) or `semantics` (SJL-15's model) |
| `basis` | why: `both-parsers`, `vm-parser`, `js-parser` (an engine's tokenizer or parser checks it at parse time today, D10's union); `grammar` (a rule that a production matches nothing valid, or the cover's own refinement, which no parser checks yet: EE0017, EE0037); `grammar-row` (an engine-only Grammar row); `limit` (the contract's two limits); `provider` (a regular-expression literal's validity, decided by the request's provider, D7); `goal` (refused by the rows its text breaks under one of the library's goals: eval code, the dynamic function's parameters and body, ShadowRealm's evaluation); `subsumed` (EE0179: every source it refuses EE0168 refuses, the catalogue's note); `request-switch`; `engine-policy`, `engine-limit` (the catalogue's other engine-only rows); `no-parser` (no parser checks it today); `run-time` (a step of an evaluation) |
| `switch` | the request parameter that governs the row: `proposal:<form>`, `regexp-provider`, `top-level-await`, `nesting-depth`, `goal`; `-` |
| `vm_stage`, `vm_site` | Broiler.VM's stages (`tokenizer`, `parser`, `admission`, `binder`, `lowering`, `executor`; `compile` for a row refused by the whole compile of an evaluated text) and its sites at `4cf9ca4`, under `src/Broiler.VM.Profile.JavaScript.Compiler/` unless a site begins `src/` |
| `js_stage`, `js_site` | Broiler.JS's stages, as the catalogue names them, and its sites at `8a95076c`, under `Broiler.JS/` |
| `js_compiler_only` | for a `syntax` row: `yes` if Broiler.JS checks it only in its compiler (SJL-14's class E, decision D45), `partly` if its parser checks part of it, `no` if its scanner or parser does, `not-checked` if Broiler.JS does not refuse it; `-` otherwise |
| `js_lazy` | for a `syntax` row Broiler.JS checks in its compiler: `yes` if only in `FastCompiler.EarlyErrors`, `Visit*` or `DirectEvalSupport`, which run as a function body is compiled (deferred bodies are compiled when first called) or when eval runs (SJL-14's class T, decision D64); `partly` if `SyntaxValidation`, which walks the whole program before it runs, checks part of it; `no` if only `SyntaxValidation`; `-` otherwise |
| `rows` | the rows of SJL-13.1's correspondence that hold its sites |
| `stale_cells` | the catalogue's cells that no longer hold at the heads beyond their line numbers (every site cell's lines have moved): `vm_wide_stage` where SJL-12.6 moved the lowering's check into the binder or the parser refuses a half, `status` (EE0082, EE0137, EE0166, EE0176: the correspondence's F30), `notes` (EX0023: F4), `js_site (path)` (the three files SJL-12.5 moved, `early-errors.md` section 4) |
| `notes` | the correspondence's finding or row behind a choice, a dead binder copy, and Broiler.JS's `case`-clause hole (D63) |

**The stages decided by hand** (the correspondence's F15, P31 and D-g): EE0017 is `syntax` by D10's first clause,
the grammar, though no parser checks it today, so step 1 deletes the binder's B15; EE0037 is `syntax` (P31, a
correction of both); EE0179 is `syntax` because EE0168, a `syntax` row, refuses every source it refuses, so step 1
deletes B27 whole; EE0018 and EE0054, which no parser checks, are `semantics`, and the binder keeps B16 and B21;
EE0021, which neither engine checks and no source reaches, is `semantics`. The proposal rows EP0009 to EP0014 are
`syntax`, each governed by its own flag. EX0029 is an `engine` row (the correspondence's F6).

## Rebuilding it

From the repository root, with Python 3.10 or later and PyYAML:

```
python3 -I scripts/frontend/classify_parse_corpus.py write --test262 <test262 archive or checkout> \
    --vm-baseline <Broiler.VM>/docs/evidence/sjl-12-001/after/parse-corpus-after-run1.tsv.gz \
    --correspondence <Broiler.VM>/docs/evidence/sjl-13-001/correspondence-runs/analysis
python3 -I scripts/frontend/classify_parse_corpus.py check  (the same three options)
```

`--test262` takes Broiler.VM's archive of the pin (`src/tests/conformance/pins/test262-ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e.tar.gz`)
or a checkout at `ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e`. Every input is checked against its SHA-256, as the
table's `#%` lines and the script's header name them, before anything is read from it; the stage table's digest is
recorded, not pinned. `check` rebuilds every file in memory and compares it with this folder byte for byte: two
runs are identical, because no clock, locale, hash order or file-system order reaches the output and the table is
compressed with a zero time stamp and no file name.
