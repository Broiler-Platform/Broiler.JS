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

**Every probe is eval code**, compiled by an indirect eval (`tt`), so what they show of each engine is what it does in
eval code. The `semantics` vectors' first runs (corpus revision 42, classified at revision 43) run the same constructs
as Scripts and as direct-eval code, and where a Script's answer differs from the probe's the vectors say which.

- **Broiler.JS, in eval code**, refuses before any code runs the `semantics` rows `SyntaxValidation` checks (the jumps,
  the duplicate `__proto__`, a private name no class declares) and a field initialiser's super call at the eval code's
  top level. It refuses neither when the construct is reached nor when it is not, in all three forms, the super rows of
  an object literal's and a class's methods, of a function's, a generator's or an async function's parameters and body
  that hold a super property, and of a class with no heritage whose constructor holds a super call, and the `arguments`
  of a field initialiser: an indirect eval compiles its code as direct-eval code
  (`Broiler.JS/Broiler.JavaScript.Globals/JSGlobal.cs:145`, `PushDirectEvalCompilation`), and the compiler's `Visit*`
  checks of those rows skip direct-eval code (`Broiler.JS/Broiler.JavaScript.Compiler/Expressions/FastCompiler.VisitCallExpression.cs:397`,
  `FastCompiler.VisitMemberExpression.cs:25`, `FastCompiler.VisitIdentifier.cs:344`), leaving them to
  `DirectEvalSupport`'s validator, which checks the eval code's own top level and no method or function in it. It
  refuses the super calls of a function's, a generator's and an async function's parameters or body with an `Error`
  that is a .NET `ArgumentNullException`; it compiles a field initialiser holding a super call inside a function
  without end (`TIMEOUT`); a static block's super call is a run-time `TypeError` when it runs; and it refuses neither
  the `arguments` of a static block nor `this` as a for-in target, which it checks nowhere. **As Scripts**, the
  `semantics` vectors' first runs show it refusing every one of these rows before any code runs but the last two, whose
  vectors differ there as here. Of the values, it answers otherwise than the specification for a closure in the
  parameters, which sees the body's `var` (FF4), and a direct eval in an arrow, whose `var` reaches the enclosing
  function (FF5), as Scripts too; and, in indirect-eval code alone, for three anonymous functions, which take no name
  (NE1: a direct eval's code names them, as revision 43's vectors show), and a `var` of eval code against the calling
  eval code's `let` (EA0007's path), which it accepts.
- **Broiler.VM, in eval code**, refuses every timing probe before any code runs, its message the profile's admission
  refusal; of the values, it answers otherwise than the specification for a block function named `arguments` (AB1: it
  keeps the arguments object, as test262's `annexB/language/function-code/block-decl-func-skip-arguments.js` at
  `ccaac100` asserts and the pinned edition's FunctionDeclarationInstantiation does not), a block function under a `let`
  of its name in an enclosing block and a block function in strict eval code (AB1: aliased in both), and `continue` to
  the outer label of a loop's label set (LB1: refused, JSD-0083's hand-off H20). **As Scripts**, the vectors' first runs
  show the first and the last, and not the two other Annex B answers, which only its eval code gives.
- **Not shown:** Broiler.VM's timing of eval code's undeclared private name (row PN1, finding F6 of its correspondence):
  its executor refuses the probe's eval code before any of it runs, so no completion value here separates the
  executor's check from a compile-time one.

## Dated notes

*(Noted 2026-11-17, after review, before any run of the rule.)*
- **"What they show" was revised in `1f1f2bab`**, after the `semantics` vectors' first runs (corpus revision 42) had
  been seen, to say that every probe is eval code and what the vectors show as Scripts; no probe, run or answer changed.
  Its first item had begun "**Broiler.JS** refuses before any code runs the `semantics` rows `SyntaxValidation` checks
  (the jumps, the duplicate `__proto__`, a private name no class declares) and a field initialiser's super call at the
  top level", had counted "the `arguments` of a field initialiser or of a static block, and `this` as a for-in target"
  among the rows refused in none of the three forms, and had ended "and a `var` of eval code against the calling eval
  code's `let` (EA0006 and EA0007's path), which it accepts."; its second had begun "**Broiler.VM** refuses every timing
  probe before any code runs" and named "a block function in strict indirect-eval code (AB1: aliased)".
- **Probe `V046-EA0006`'s row is EA0007**, after review: its source, `let x; eval('var x;')`, runs as eval code, so its
  `let x` is that eval code's and the refusal it tests is EvalDeclarationInstantiation's step 3.d.i.2.a.i (row EA0007),
  not the global record's (EA0006). [`gen_probes.py`](gen_probes.py) labels its row EA0007 and keeps the id it ran
  under, which `runs/` answers; `probes.tsv` and `answers.tsv` were written again, and `probes.js`, the program that ran,
  is unchanged byte for byte.
