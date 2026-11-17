# Bundle SJL-15-001 (Broiler.JS): the classes E and T and the corrections, by row, before any run

**Written:** 2026-11-17, at SJL-15.1 (Broiler.JS's half), with the rule, before any change it judges exists. The rule's
parts A, C, D and E classify by the rows here, and by no other. The three tables are written by
[`tools/write_lists.py`](tools/write_lists.py) from the stage table, `Broiler.ECMAScript/docs/frontend/stages.tsv`
(every row whose stage is `semantics`), and from this bundle's probes,
[`probes/answers.tsv`](probes/answers.tsv), and are never edited by hand; `write_lists.py check` fails if one differs
from what the script writes.

## Class E, the early-error stage (decision D45, applied to the semantic half)

[`corrections/class-e.tsv`](corrections/class-e.tsv): every `semantics` row whose Broiler.JS stage is a compiler site
(`SyntaxValidation`, `FastCompiler.EarlyErrors`, a `Visit*` method or `DirectEvalSupport`), with its sites at `6fda6ca4`
as the stage table gives them. No `semantics` row has this engine's parser as its site (the stage table's notes, at
SJL-13.1), so every refusal this engine makes of one is made in its compiler, after the parse, and the model makes it at
the parse. An input of such a row is accepted at entry point P at the base and refused there after; at entry point K it
is refused at both, and a change of its message is class M.

## Class T, timing (decision D64, its second half; decision D-js-timing)

[`corrections/class-t.tsv`](corrections/class-t.tsv): the class E rows whose Broiler.JS check runs as a function body is
compiled or when eval runs (`FastCompiler.EarlyErrors`, a `Visit*` method, `DirectEvalSupport`), and every row a timing
probe shows this engine refusing after any of the probe ran, or not at all, each with the probes that show it. The
probes put each row's refused construct at the top level of indirect-eval code, inside a function expression never
called, and inside one that is called ([`probes/`](probes/README.md)); the specification refuses all three before any of
the code runs. At `6fda6ca4` this engine refuses the `super` rows of an object literal's or a class's methods, of a
function's parameters and body that holds a super property, of a class with no heritage whose constructor holds a super
call, of a field initialiser that holds `arguments`, and of async functions and generators that hold a super property,
in none of the three forms: each is class T, its SyntaxError "raised later, or not at all" (decision D64's words).

## C1, the corrections

[`corrections/corrections.tsv`](corrections/corrections.tsv), by row:
- the `semantics` rows this engine checks nowhere (the stage table's `js_stage` `none`): EE0021, EE0054, EE0138 and
  EA0008;
- the rows a timing probe shows refused with another error than a SyntaxError, or not answered within the probe's bound:
  the super calls in a function's or a generator's or an async function's parameters or body, refused with a .NET
  `ArgumentNullException` the shell surfaces as an `Error`; a field initialiser holding a super call inside a function,
  which this engine compiles without end (each probe stopped at its bound, the first run's process having grown in
  memory until it was stopped); and a static block holding a super call, a run-time `TypeError`;
- the rows of the correspondence whose value probes this engine answers otherwise than the specification: FF4 (a
  closure in the parameters sees the body's `var`), FF5 (a direct eval in an arrow declares its `var` in the enclosing
  function), NE1 (an anonymous function in the indirect-eval code of the probes takes no name) and EA0006 (a `var` of eval
  code against a lexical declaration of the code that called it, accepted).

Each is listed in `Broiler.JS/CHANGELOG.md` at the cut-over (decision SJL-D-16 (a)). The `semantics` vectors' first runs
(corpus revision 43) classify each engine's differences by the same rows.

## What these tables are not

They are not a run of the rule: the probes were taken at the rule's base revision, `6fda6ca4`, from builds of each
engine's default branch, to list the rows before any run, as bundle `sjl-14-001`'s inherited-context probes listed
theirs. A row the probes did not reach and a run finds is a finding against the correspondence (the rule's C1), not a
class added afterwards.
