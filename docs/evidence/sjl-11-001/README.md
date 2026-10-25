# Evidence bundle SJL-11-001 (Broiler.JS) - regular expressions

**Milestone:** SJL-11 of the shared JavaScript libraries programme,
`Broiler.VM/docs/javascript-shared-libraries/milestones/sjl-11-regexp.md`: its Broiler.JS clauses 4 to 6, SJL-11.6's
adversarial corpus in this engine, and this engine's measurement.

**Rule:** [`decision-rule.md`](decision-rule.md), committed at SJL-11.1 before any code of the area:
- part A is test262 per path in UTC, with negative-metadata tests (clauses 5 and 6);
- part B is this engine's measurement, over a benchmark class, `RegExpBenchmarks`, that the rule's inputs commit adds;
- part C is the adversarial corpus in this engine, the programs of [`inputs/adversarial/`](inputs/adversarial/);
- part D is the culture independence of the regular-expression vectors, under the invariant culture, `tr-TR` and
  `lt-LT`;
- part E is the deletion list, a search and a metadata check (clause 4).

Clause 7, every correction listed, is read from `Broiler.JS/CHANGELOG.md` against the parts' C1 rows and the rule's
list of this engine's corrections. The rule names it and does not decide it.

**Area verdict:** [`SharedLibraries.md`](../../roadmap/SharedLibraries.md), "Area verdicts", "Regular expressions
(SJL-11.1)", with Broiler.VM's JSD-0081,
`Broiler.VM/src/Broiler.VM.Profile.JavaScript/docs/decisions/0081-regular-expressions-move-to-broiler-regex-under-its-owner-conditions.md`,
and its correspondence from code, retained in Broiler.VM's bundle of the same name
(`Broiler.VM/docs/evidence/sjl-11-001/correspondence.md`, with every probe and each engine's answers).

**Inputs, committed with the rule:**
- [`inputs/adversarial/`](inputs/adversarial/): part C's programs, one per case, and `manifest.tsv`, written by
  Broiler.RegEx's `eng/adversarial-programs.py --engine broiler-js` from that repository's
  `tests/Adversarial/corpus.tsv`. The manifest's header names the corpus's SHA-256,
  `1a0f8c56bb970fa784fc6e573e9968e939d298d3e912320ed8c1a407fa997dd5`, and the Broiler.RegEx commit both were read at,
  `c2aa77167cb3ded40ef50b5c3eeae26f8f7c0b54`, on its branch for SJL-11.1, and each program's SHA-256. Two runs of the
  generator there wrote byte-identical files, and these are a byte-for-byte copy of them. Nothing in the folder is
  edited here. Broiler.VM's programs, in its bundle of the same name, come from the same corpus at the same commit.
- [`inputs/workloads/`](inputs/workloads/README.md): part B's six workloads and six controls, with their README.
  They are Broiler.VM's workloads, byte for byte, and the README gives each file's SHA-256.

**The pins.** test262 `ccaac100`. Broiler.Regex `0.1.0-preview.4`, which this engine restores today, until SJL-11.5
moves the pin to `0.2.0-preview.1` (JSD-0081 decision D12). From SJL-11.5 this engine passes Broiler.Regex no meter,
a frame ceiling of `1 << 20`, a trail ceiling of `1 << 21` and a nesting limit of 512 (decisions D7 and D10).

**Owner:** the Broiler.JS maintainer. One person holds every role (the programme's EX-30, recorded in Broiler.VM).
**Reviewer:** none, and nothing here has been read by anyone but its author.

**State, 2026-10-25, at SJL-11.1: rules only. Nothing here is a verdict, and nothing has been taken.**
- The inputs commit adds `RegExpBenchmarks` and the benchmark `Program.cs`'s `--regexp-answers`, which read the
  workloads verbatim (decision D17).
- Every base run is taken at the base commit, the first parent of the merge that lands SJL-11.5, while merges to this
  engine's and the shared folder's code are held (decision D16).
- The after runs follow that merge.

**The commits named so far:**
- **The rule commit** carries [`decision-rule.md`](decision-rule.md) and the inputs. It is named here, by all forty
  digits, when the change that carries it has merged.

No other commit is named yet.

**What the bundle will hold:**
- `base/`: part A's base run, imported unchanged with the digest of its zip, and parts C and D's base runs;
- `after/`: the after runs of every part, each comparison's report, and the table of every C1, C3 and C5 row;
- `engine/`: this engine's runs of the regular-expression vectors SJL-11.3 adds, and part D's three cultures;
- `controls/`: each control's patch and its output, and its revert;
- `measurement/`: part B's sub-bundle, with its own README and manifest.

**It holds no figure until a run is taken**, and then only in the run's own files and this README's reading of them.
A test262 suite total is never quoted: part A is decided per path, and totals decide nothing.

The owner's direction of 2026-10-18, "Please do all recommended decisions and skip waiting for review.", under which
JSD-0081 records its decisions, changes no rule: nothing is accepted without review, and SJL-11 may not meet its gate
before SJL-7 meets SJL-7's, and so not before SJL-3 meets SJL-3's.

Broiler.VM and Broiler.RegEx judge their own halves in their own bundles of the same name. This bundle reads none of
their runs.
