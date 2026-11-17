# The correspondence probes (SJL-15.1, Broiler.JS's half)

**Written:** 2026-11-17, at milestone SJL-15 (work package SJL-15.1, Broiler.JS's half) of the shared JavaScript
libraries programme, with this bundle's rule, before any change it judges exists. **What they are for:** this engine's
column of the correspondence ([`../correspondence-broiler-js.md`](../correspondence-broiler-js.md)) is read from code,
and these probes settle the rows a reading cannot: when this engine raises each `semantics` row's SyntaxError, and what
each engine answers for the facts a completion value shows. They also list, before any run of the rule, the rows of its
classes T and C1 ([`../corrections.md`](../corrections.md)). Nothing here is a verdict, and nothing here is a run of the
shared library or of the rule.

## What was run

- **The probes**: [`probes.tsv`](probes.tsv) lists each with its row of the correspondence, its catalogue row, the
  specification's answer (ECMA-262 ES2026 at `0248456c`) and why; [`gen_probes.py`](gen_probes.py) writes it and
  [`probes.js`](probes.js), one `tt(id, source)` line per probe. Each probe's source is compiled by an indirect eval as
  global code, its first statement setting `__reached`, so that an answer says whether the source was refused before
  any of it ran (`THROWS (template)`), after some of it ran (`THROWS`), or completed (its value).
  - **T, timing**: for each `semantics` row a Script reaches whose Broiler.JS site is a compiler site, one refused
    construct at the top level, inside a function expression never called, and inside one that is called; and three
    constructs refused only at a Script's top level.
  - **V, values**: the facts of rows FF3, FF4, FF5, RS2, RS3, RS4, AB1, LB1, PN1 and NE1, and the eval-code rows EA0003
    to EA0007 and EA0009.
- **Both engines**, each built from a clean `git archive` of its default branch in a scratch folder ([`build.txt`](build.txt)):
  Broiler.JS `6fda6ca4` through its shell `BroilerJS --script-host`, one process per probe under a time bound;
  Broiler.VM `5e90bfe4` through its conformance root's `--test262` mode, as bundle `sjl-14-001`'s inherited-context
  probes ran ([`run_probes.sh`](run_probes.sh), cut from that bundle's). Each run's program, transcript and answers are
  under `runs/`; [`answers.tsv`](answers.tsv), written by [`merge_answers.py`](merge_answers.py), holds every probe's
  answer in each engine beside the specification's, and its column `matches` marks each (`=` the specification's
  answer, `x` another), Broiler.JS first.

## What they show

- **Broiler.JS** refuses before any code runs the `semantics` rows `SyntaxValidation` checks (the jumps, the duplicate
  `__proto__`, a private name no class declares) and a field initialiser's super call at the top level; it refuses neither when the construct is reached nor when it is not, in all three forms, the super rows of an
  object literal's and a class's methods, of a function's or a generator's or an async function's parameters and body
  that holds a super property, of a class with no heritage whose constructor holds a super call, the `arguments` of a
  field initialiser or of a static block, and `this` as a for-in target; it refuses the super calls of a function's,
  a generator's and an async function's parameters or body with an `Error` that is a .NET `ArgumentNullException`; it
  compiles a field initialiser holding a super call inside a function without end (`TIMEOUT`); and a static block's
  super call inside a function is a run-time `TypeError` when it runs. Of the values, it answers otherwise than the
  specification for a closure in the parameters, which sees the body's `var` (FF4), a direct eval in an arrow, whose
  `var` reaches the enclosing function (FF5), three anonymous functions in the probes' indirect-eval code, which take no
  name (NE1), and a `var` of eval code against the calling eval code's `let` (EA0006 and EA0007's path), which it
  accepts.
- **Broiler.VM** refuses every timing probe before any code runs, its message the profile's admission refusal; of the
  values, it answers otherwise than the specification for a block function named `arguments` (AB1: it keeps the
  arguments object, as test262's `annexB/language/function-code/block-decl-func-skip-arguments.js` at `ccaac100`
  asserts and the pinned edition's FunctionDeclarationInstantiation does not), a block function under a `let` of its
  name in an enclosing block (AB1: aliased), a block function in strict indirect-eval code (AB1: aliased), and
  `continue` to the outer label of a loop's label set (LB1: refused, JSD-0083's hand-off H20).
- **Not shown:** Broiler.VM's timing of eval code's undeclared private name (row PN1, finding F6 of its correspondence):
  its executor refuses the probe's eval code before any of it runs, so no completion value here separates the
  executor's check from a compile-time one.
