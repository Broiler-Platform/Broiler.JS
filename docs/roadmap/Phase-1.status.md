# Phase 1 — the front end — status

Everything phase 1 has measured: what was built, what it cost, what was refuted, and the
corrections each measurement forced on the plan.

> The evidence half of [`Phase-1.md`](Phase-1.md). **The plan document is the one to act
> from** — it carries each item's next action, size and exit gate, and links here for the
> argument. Nothing in this file is *closed*: [`Measurement.md`](Measurement.md) governs
> what may be claimed.

---

## Overview and targets, as the campaign recorded them

**Targets — and two of the three were wrong, measured.** This phase was aimed at
MandreelLatency (4646×), CodeLoad (371×) and Mandreel (300×), on the reading that they
measure the front end. Both Octane suites have now been *run* against a phase-1 build rather
than reasoned about:

- **MandreelLatency measures no compilation at all.** Octane compiles `mandreel.js` at script
  load and times only the benchmark's `run` function; `runMandreel` renders 20 frames over
  already-compiled code and `MandreelLatency` is the RMS of the pauses between them. Tripling
  the speed of compiling that file moves **neither** score: Mandreel 138.0 → 137.0 (0.993×),
  MandreelLatency 12.70 → 12.60 (0.992×), four samples an arm, ABBA on one build. **It is an
  execution-pause benchmark, so it belongs to B1/B7 and phase 3, not to B4 and this phase.**
- **CodeLoad is about a quarter compilation**, not the whole of it — see 1-1, which did move
  it, 1.099×.

What *did* move on Mandreel is real and outside every score: the suite's wall clock went
**358.2 → 350.0 s**, non-overlapping over four runs an arm. **So phase 1's value is page-load
time, exactly as the sentence below always said — and Octane is a poor instrument for it,
because Octane deliberately excludes load from what it times.** Blocker **B4**. This is the
phase the engine roadmap had excluded (§1.1), and it is the item with the clearest value
outside Octane: **this is page-load time.**

Owner assemblies: `Broiler.JavaScript.Parser`, `.Compiler`, `.BuiltIns` — **and
`.ExpressionCompiler`, which 1-4 adds and which is where the phase's cost turned out to
be.** The three-way split the phase was told to take (B4) now exists as `--compile-scaling`,
and on **2 000 synthetic top-level declarations** it reads **parse ≈ 0.5%, expression-tree
construction ≈ 11%, IL emission ≈ 89%**.

**That split is a fact about that shape, and 1-1 established it does not carry over.** On the
real corpora, removing every nested function body's IL generation removes only **17–36%** of
compile time, so tree construction is a far larger share of a real program than of a wall of
stubs — the synthetic shape has almost no tree to build. Both numbers are useful and neither
generalizes: use the synthetic one for machine-generated declaration walls (Mandreel) and the
corpus one for everything else.

**The phase splits in two, and the split is not the one the items are numbered by.**
Mandreel and CodeLoad were paired throughout as "the front end", and they fail for
unrelated reasons: Mandreel is **wide** (1 364 top-level declarations in one scope, which
was quadratic — 1-4, landed, 3.04×) and jQuery is **deep** (532 functions nested in one
IIFE, 96.5% of its compile in bodies that are never called — 1-1, open).

### 1-1 · Lazy function compilation — **the emission half landed; re-specified by measurement**

**Target.** CodeLoad and MandreelLatency are *designed* so this is the dominant term —
jQuery defines thousands of functions and calls almost none of them. A large multiple
on both is the expected outcome; if it is not, the measurement is wrong before the
change is. Mandreel's 313 s should fall substantially, and Typescript and PdfJS should
improve on load. **Steady-state execution does not change at all** — do not expect
Richards or DeltaBlue to move.

**Where.**

| File | Role |
|---|---|
| `Broiler.JavaScript.Parser/FastParser.Function.cs` | where a body is parsed today; needs a skip-with-errors mode |
| `Broiler.JavaScript.Compiler/Declarations/FastCompiler.CreateFunction.cs` | where the body is compiled eagerly |
| `Broiler.JavaScript.BuiltIns/Function/JSFunction.cs` | already carries `source` and already recompiles from it for tiering — the raw material for deferring is present |
| `Broiler.JavaScript.Engine` code cache | keyed on whole scripts; needs to key on function spans |

**Work.** Pre-parse a body far enough to find its extent and binding structure without
generating code; record source span + captured scope on the `JSFunction`; compile on
first invocation, memoized per function-span; force eager treatment for the cases
below.

**Risk — all four are spec-visible, and the first is the bulk of the work.**

- **Early errors must stay eager.** A syntax error inside a never-called function is
  still a `SyntaxError` at parse time. The pre-parser has to be a real parser *for
  error purposes* while skipping code generation. Most likely to regress test262.
- **Scope capture.** A deferred body must compile against the scope chain as it was at
  closure creation, not at first call.
- **Direct `eval`** inside a deferred body can introduce bindings into enclosing
  scopes. The pre-parser must detect it and opt that function out.
- **Generators and async bodies** suspend mid-frame; confirm deferral composes with
  `GeneratorRewriter` before assuming it does.

**Verify.** Full test262 over the four pinned manifests with **no new failure and no
new timeout** — the local suite is not sufficient for an early-error change. Plus
`ParserCompilerBenchmarks` before/after, and a CodeLoad number taken with the code
cache confirmed off (0-5).

**Size: XL.** The only item here that is a genuine sub-project.

**Its premise is now measured, and it holds — but not on the suite the item leads with.**
`--compile-profile`'s control compiles each corpus with every outermost function body
replaced by `{}`, which is the floor an ideal deferral converges to. The saving is *not*
that difference: a body that is never called must still be **parsed**, because a syntax
error inside it is still a `SyntaxError` at script-compile time (this item's first named
risk), and the stub source has no bodies to parse. Charging that pre-parse back gives
`ceiling = full − stub − (parseFull − parseStub)`. Taken **after 1-4**, so it is what is
left for 1-1 to win rather than what was there before the phase started:

| Corpus | Functions | Compile | Ceiling | Share |
|---|--:|--:|--:|--:|
| codeload-jquery | 532 | 767 ms | 741 ms | **96.5%** |
| box2d | 982 | 413 ms | 390 ms | 94.4% |
| typescript | 1 763 | 947 ms | 892 ms | 94.2% |
| pdfjs | 949 | 991 ms | 914 ms | 92.3% |
| mandreel | 1 476 | 7 815 ms | 6 646 ms | 85.1% |
| codeload-closure | 57 | 72 ms | 45 ms | 62.9% |

So the item is right that most front-end cost is function bodies — 92–96% on the large real
programs — and it is right that a *parse* is all a deferral has to keep. **Two corrections
to how it is stated.** First, the ceiling is a **lower** bound, not an upper one: the
control still emits an empty lambda per function, and a deferred function emits none, so
the floor is below the stub. Second, the item's headline pairing of CodeLoad with
MandreelLatency does not survive: Mandreel's cost was never mostly bodies (see **1-4**,
which took it 3.04× without deferring anything), while **jQuery's is 96.5% bodies and
essentially all of it is deferrable** — CodeLoad evaluates it and calls almost none of it.
1-1 is a CodeLoad item and a page-load item. It is not the Mandreel item it was written as.

**And its cost side is now known too**, which is what makes it startable: the deferred body
has to compile against the scope it was created in, and this engine has no closure object to
hang that on. Its only mechanism for "compile later against captured bindings" is direct
eval's `JSVariable[]` capture list, and identifiers compiled that way resolve through
`JSContextBuilder.ResolveIdentifier` at **run time** rather than binding to a cell at compile
time — fine for CodeLoad, whose payload is eval'd code either way, and a steady-state
regression for anything hot. Baking the cells in as constants is not available:
`ILGeneratorExtensions.EmitConstant` throws `NotSupportedException` for any reference type
that is not a `string`, `Type` or `MethodInfo`. **So the sub-project inside this item is a
capture mechanism, not a pre-parser** — the pre-parser is the part that already exists.

#### What landed: defer the *emission*, not the compilation

**The capture mechanism was not built, and the item's win was taken without it.** The four
risks above are all properties of the **front end**, and they are all settled before a
function reaches the emitter: the source is parsed, early errors have thrown,
`LambdaRewriter` has decided which variables are captured and boxed them, and
`GeneratorRewriter` has run. What is left at that point is generating machine code for a tree
that is already correct. **Deferring that is the same prize with none of the risk** — a
deferred site cannot observe anything, cannot fail differently on any input the eager path
accepts, and produces byte-identical IL, because it is the same call on the same tree, later.

**Where.** `RuntimeMethodBuilder.Relay` generated each nested lambda's `DynamicMethod` while
emitting its enclosing lambda. It now registers the site instead
(`MethodRepository.RegisterDeferred`), and `Create` — which runs once per *closure
instantiation* — hands back a thunk over that instance's boxes. The first invocation
generates the method, memoized on the shared site; a function defined once and instantiated a
million times generates once, and one instantiated once and never called generates never.
`LambdaRewriter.Rewrite` stays eager and must: it decides the boxes the creation site
references.

**Two things had to be got right, and both were found by measuring rather than by reading.**

- **No stack handoff.** Wrapping generation in `CompilationStack.RunOnFreshStack` — matching
  what the eager path was inside — took the repository suite from 3.5 minutes to **over 20, at
  12% CPU**: blocked, not working. It is 1-2's own lesson about short sources, applied at the
  wrong granularity: a fixed ~180 µs handoff is nothing against one whole-script compilation
  and unaffordable once per function. It is also unnecessary, because `ILCodeGenerator` derives
  from `StackGuard` and segments onto a fresh stack only when a tree is actually deep enough.
- **The thunk's warm path is written in IL, not called.** A `DynamicMethod` does not inline
  what it calls, so a thunk that called `Instance.Resolve()` unconditionally cost **1.0247×**
  on a call-heavy script — a real regression on every deferred function that is ever invoked.
  Reading the field inline and branching to the slow path only when it is null takes that to
  **1.0009×**, with pairs straddling 1 in both directions.

**Result.** Compile only (the corpus is compiled and never invoked, which is CodeLoad's shape),
ABBA-interleaved on one build with `BROILER_JS_DEFER_IL` as the only difference, **one corpus
per process**, six pairs per arm:

| Corpus | Functions | Eager | Deferred | Time | Allocation |
|---|--:|--:|--:|--:|--:|
| **codeload-jquery** | 532 | 587 ms | **398 ms** | **0.661×** | **0.518×** |
| box2d | 982 | 914 ms | 581 ms | 0.636× | 0.553× |
| pdfjs | 949 | 1 607 ms | 1 107 ms | 0.689× | 0.549× |
| codeload-closure | 57 | 51 ms | 37 ms | 0.714× | 0.657× |
| mandreel | 1 476 | 7 115 ms | 5 923 ms | 0.832× | 0.544× |
| typescript | 1 763 | 1 657 ms | 1 805 ms | **1.034×** | 0.516× |

Steady state — a script that compiles *and* runs 6.3 M calls across three shapes — is
**1.0009×** by median paired ratio. So the change is free where it does not help.

**The benchmark this item names was run, and it passes** (§3.5: *a benchmark named as an
item's justification is a test that item has to pass*). Octane **CodeLoad**, ABBA-interleaved
on one build with `BROILER_JS_DEFER_IL` as the only difference, 24 samples per arm:

| Arm | n | Median | Range |
|---|--:|--:|--:|
| eager | 24 | 94.6 | 89.1–103.0 |
| **deferred** | 24 | **104.0** | 98.0–110.0 |

**1.099× (higher is better), with 93.1% pairwise dominance** — 536 of 576 deferred-vs-eager
sample pairs favour deferral. Worth noting how nearly this was mis-called: the first pair of
runs separated cleanly (94.3 vs 105) and the *reversed* pair did not (99.2 vs 99.4). At three
samples an arm, against a suite whose own noise band is 7.5%, either pair alone would have
been read as a verdict. It took 24.

**And it re-frames the item, because 1.099× is not what 1-1 predicts.** The item says "a large
multiple on both is the expected outcome; if it is not, the measurement is wrong before the
change is." The measurement is not wrong — the same payload's isolated *compile* moved 1.51×
(0.661× time). Solving for the share: if compilation were fraction *f* of CodeLoad's measured
time, `f × 0.661 + (1 − f) = 1/1.099`, giving **f ≈ 0.27**. So **compilation is about a
quarter of what CodeLoad measures**, not the dominant term this document has assumed since
§1.1 called it "the two that measure nothing but the front end". The rest is executing the
payload — jQuery's initialization runs a great deal of code, and the functions it calls have
their generation forced. That estimate is derived from two measured ratios rather than
observed directly, and it is the first thing 1-3 should check.

**Three honest qualifications, and the first corrects something this document said.**

1. **"Emission is 89%" is true of the synthetic shape and not of these corpora.** That figure
   comes from `--compile-scaling`'s top-level declarations, where a lambda is a stub and almost
   all the work is emitting it. On the real corpora, deferring every nested body removes
   **17–36%** of total compile time (jQuery 32%, Box2D 36%, PdfJS 31%, Closure 29%, Mandreel
   17%) — so on real source, expression-tree construction is a far larger share than 11%. The
   phase-level claim above has been qualified accordingly; *a split measured on one shape is a
   fact about that shape.*
2. **Typescript is consistently slower and it is not explained.** 1.034× on the final build,
   every one of six pairs above 1 (1.004–1.077), while its allocation halves like everything
   else's. It is not GC — allocation moves the same 0.52× as the corpora that gain. It is
   recorded rather than explained, because a plausible story with no measurement behind it is
   what §3.5 exists to stop.
3. **A deferred site retains its expression tree.** Sites are registered under a `GCHandle`
   that is never freed (pre-existing), so an un-generated site holds its tree for the life of
   the process where a generated one holds only its `DynamicMethod`. The tree is released the
   moment a site is forced. This is also what made the *first* measurement of this change
   wrong: with all six corpora in one process, the ones measured last paid for the ones before
   them and read 1.6× and 2.6× **slower**, with bimodal ratios — the tell that it was ordering
   and not the change.

**The three-engine comparison was run over both named suites.** Not `--only` — that makes the
harness skip the comparison by design ("its suite set is a subset, so folding it into the full
comparison would silently drop every suite it skipped") — but a **two-suite manifest**, which
makes it a full run over exactly CodeLoad and Mandreel and emits `comparison.md` honestly.
Chromium 3 reps, Broiler 3 reps, Jint 1:

| Benchmark | Chromium | Broiler | Jint | Broiler × slower | Broiler / Jint |
|---|--:|--:|--:|--:|--:|
| CodeLoad | 19 334 | 82.4 ⚠ | 1 997 | 234.6× | 0.041 |
| Mandreel | 28 359 | 104 | 58.5 | 272.7× | **1.778** |
| MandreelLatency | 35 865 | 9.8 ⚠ | 499 | **3 659.7×** | 0.020 |

⚠ = spread over 3 repetitions exceeded the 7.5% noise band (CodeLoad 15.9%, MandreelLatency
9.5%). All six suite runs completed `ok`; nothing to diagnose.

**Four things this table is not.** *(1)* Its geomean (44) and spread (15.6×) are over **three
scores**, not the suite's seventeen — they are not comparable to the committed run's 354 and
249×, and must never be quoted as if they were. *(2)* Broiler's absolute scores here are well
below the same build measured alone earlier the same day (CodeLoad 82.4 against 104, Mandreel
104 against 137): a three-engine session is a busier machine than a one-suite run, which is
§3.2's rule about comparing only within a run, showing up. *(3)* **Jint ran one repetition, and
`comparison.md`'s header said "Repetitions per suite: 3"** — the header took that number from a
single engine and presented it as global. **Found by running the thing, and fixed**: the summary
now records repetitions per engine, reports one number only while they agree, and otherwise names
each — the same comparison now reads *"Chromium 3, Broiler 3, Jint 1 — the engines differ, so each
column is that engine's own median, and Jint is a single run whose deltas cannot be distinguished
from noise"*. The ⚠ spread column follows Broiler's own count too, rather than whichever engine
came first, since it describes Broiler's samples. Eight checks added to
`tests/octane/harness-selftest.mjs`, five of which fail against the old harness. *(4)* Jint ran in a separate, later, quieter process after
the first attempt was killed, so its column is the weakest of the three.

**What it does show.** Broiler is **1.778× ahead of Jint on Mandreel** — the suite whose compile
1-4 tripled — and far behind it on the two that are not compile-bound: 0.041× on CodeLoad, whose
payload Jint evaluates far faster, and 0.020× on MandreelLatency. *A managed interpreter beating
this engine by 25× and 50× on those two is the sharper statement of where phase 1 and phase 3
have left to go, and it is a comparison no Chromium column makes visible.*

#### The remaining half, measured before it was built

**"On real source that is the larger half" was an inference, and it is now a reading.** It had
been derived — the ceiling table says 92–96% of compile is bodies, the deferral A/B says emission
is 17–36% of compile, and the remainder was read off as tree construction — which is the move
§3.5 has a rule about and item 3-6 paid for once already. `--compile-phases` takes the split
directly, on the real corpora rather than on `--compile-scaling`'s declaration walls, one corpus
per process, five repetitions (three for Mandreel), medians per column. Deferral on, i.e. the
engine as it ships:

| Corpus | parse | tree construction | emission | total | tree share | parse share |
|---|--:|--:|--:|--:|--:|--:|
| codeload-jquery | 14.7 ms | **79.9 ms** | 54.8 ms | 149.4 ms | **53.5%** | 9.8% |
| codeload-closure | 4.2 ms | 15.0 ms | 25.4 ms | 44.7 ms | 33.6% | 9.4% |
| box2d | 25.8 ms | **116.0 ms** | 99.2 ms | 240.9 ms | **48.2%** | 10.7% |
| pdfjs | 93.4 ms | **427.0 ms** | 173.0 ms | 693.3 ms | **61.6%** | 13.5% |
| typescript | 67.5 ms | **452.0 ms** | 188.3 ms | 707.7 ms | **63.9%** | 9.5% |
| mandreel | 650.5 ms | **3 037.2 ms** | 2 724.5 ms | 6 412.3 ms | **47.4%** | 10.1% |

**The claim holds and is sharper than it was stated.** Expression-tree construction is the single
largest phase on five of six corpora, parse and tree together are 43–75% of what the engine spends
compiling, and the parse — the part 1-1 may *never* defer, because a syntax error in a function
that is never called is still a `SyntaxError` — is only **9.4–13.5%**. So what is left of the item
is almost entirely tree construction, and the early-error risk that dominates the item's text
costs a tenth of it.

It also corrects the phase's own headline once more. `--compile-scaling`'s synthetic split reads
parse 0.5% / tree 11% / **emit 89%**; on real source, with the emission half landed, emission is
the *middle* term on five of six and the largest only on Closure, the smallest corpus here. The
qualification already recorded — "a split measured on one shape is a fact about that shape" — is
right, and the distance between the two shapes is larger than it was described.

**And the population is now counted, which the ceiling table never did.** `--defer-population`
reads item 1-1's own registration and forcing counters — both touched once per *site*, never per
call — after evaluating each corpus the way its harness does. Evaluating and stopping is not an
approximation of CodeLoad's shape, it *is* CodeLoad's shape:

| Corpus | sites registered | forced (invoked) | never forced | share |
|---|--:|--:|--:|--:|
| codeload-jquery | 415 | 68 | 347 | **83.6%** |
| codeload-closure | 51 | 5 | 46 | 90.2% |
| box2d | 978 | 101 | 877 | 89.7% |
| pdfjs | 804 | 105 | 699 | 86.9% |
| typescript | 1 574 | 235 | 1 339 | 85.1% |
| mandreel | 2 697 | 8 | 2 689 | **99.7%** |

**84–99.7% of a real script's functions are never invoked at load**, so the population the
remaining half would serve is nearly all of them. Between the two tables the item is well
founded: the work is over half the compile and almost none of it is needed.

**One correction the population count forces, and it is to this item's own ceiling table.**
`--compile-profile` stubs every *outermost* function body, and **jQuery has exactly one** — the
IIFE the library is written inside, 99.91% of the source by bytes. Its control is therefore an
empty file, and the **96.5%** in the table above is *everything except the parse*, not anything a
deferral can take: CodeLoad evaluates jQuery, which runs that IIFE. The sentence this document
has carried since — "532 functions nested in one IIFE, 96.5% of its compile in bodies that are
never called" — is wrong about the outermost body, which is called first and calls 67 more. The
right figure for jQuery is the population one, **83.6%**, and it is a different measurement rather
than a correction of the same one.


#### The remaining half, priced — and the price depends entirely on how the scan is written: **`0101`**

The item names its own sub-project: *"a free-name walk per deferred function, resolved against the
enclosing scopes and recorded as a name → index map"*, because a captured name's index in the
enclosing lambda's `Box[]` is decided by `LambdaRewriter` **from the tree** and a deferred body has
no tree. `--compile-phases` charged that walk at **5.4–9.9%** of tree construction and recorded the
figure as a **lower bound**, since it counted identifiers and resolved nothing. The bound has now
been checked by building the real thing.

**`FreeNameScan` is that walk.** It tracks scopes because the answer depends on them: a `var` is
function-scoped and a `let` block-scoped, so `{ var x; } x` is bound and `{ let x; } x` is free;
parameters, catch bindings, a named function expression's own name and hoisted declarations all
bind; a pattern's *target* binds while its *default* references. A direct `eval`, a `with` or a
`debugger` reaches bindings that are never mentioned, so a body containing one is reported
undeferrable rather than given a bigger set.

**The first implementation priced the item at up to half its own prize.**

| Corpus | body tree | naive scan | | one-pass scan | | prize left |
|---|--:|--:|--:|--:|--:|--:|
| codeload-jquery | 59.4 ms | 21.0 ms | 11.1% | **5.5 ms** | **9.3%** | 53.8 ms |
| codeload-closure | 7.5 ms | 0.9 ms | 5.8% | **0.9 ms** | **12.2%** | 6.6 ms |
| **box2d** | 105.2 ms | **70.9 ms** | **47.7%** | **11.4 ms** | **10.8%** | 93.9 ms |
| pdfjs | 339.9 ms | 60.9 ms | 9.4% | **22.6 ms** | **6.6%** | 317.4 ms |
| typescript | 356.2 ms | 55.4 ms | 13.7% | **25.9 ms** | **7.3%** | 330.2 ms |
| mandreel | 2 015.7 ms | 159.9 ms | 7.8% | **178.2 ms** | **8.8%** | 1 837.5 ms |

***Scanning each function independently is superlinear in nesting depth***, because walking a
function walks every function inside it, and every enclosing level walks it again. On Box2D — 978
functions, deeply nested — that is **47.7% of the prize**, against 5.8% on the flattest corpus. The
same walk restructured as **one bottom-up pass** is 6.6–12.2% everywhere, because free names
compose: a function's free set is its own references, plus its children's, minus what it binds. Box2D
goes **70.9 → 11.4 ms**.

**Mandreel is the one corpus where one pass is worse** — 7.8% → 8.8% — and it is worse for the
reason this document already recorded about it: *Mandreel is wide, not deep*. With almost no
nesting there is nothing to re-walk, so the per-function bookkeeping is pure overhead. It is the
control that says the improvement is about depth rather than about the rewrite being faster in
general.

**So the roadmap's lower bound was right about the number and silent about the thing that decides
it.** 5.4–9.9% is a good estimate *of a walk written the right way* and off by five-fold for the
obvious one. The item's charge-back is small; whether it is small is not a property of the item.

**What this delivers and what it does not.** The scan is built, public, and pinned by 23 fixtures —
every one a pair or a negative, because a scanner returning "every identifier" passes any test that
only checks a free name is present. Two defects were found by them rather than by inspection: a
non-computed member property (`o.a`) read as a reference to `a`, and object-literal **keys** did the
same, because `AstReduce` walks a literal's properties generically while routing a *pattern*'s
through `VisitObjectProperty`. Either would have boxed a binding for every property name in the
program. The one-pass form is checked against an independent naive oracle in the test project, and
building that oracle found two more bugs — *in the oracle* — which is the cross-check working.

**The deferral mechanism itself is not built.** What is now known is that its one named obstacle
costs **7–12% of the work it removes**, over a population that is **84–99.7% never invoked**, and
that the shape of the scan is worth a five-fold difference in that figure. **Size of what remains:
L**, unchanged — but no longer blocked on an unpriced precondition.

#### And how many sites need that obstacle solved at all: **`0102`**

**The obvious next move was to skip the mechanism for the sites that do not need it, and it does
not pay.** A function whose free names resolve to nothing an enclosing scope holds captures
nothing, is handed no `Box[]`, and could have its tree deferred with the machinery that already
exists — no capture mechanism, no name → index map, none of the L. Nobody had counted that
population, and the two scanners next to it cannot: `NestedFunctionScanner` collects identifiers a
nested function *mentions*, which cannot tell `function (q) { return q; }` from
`function () { return q; }`, and neither knows where a name *resolves*. Counted, one row per
function site the corpus compiles:

| Corpus | sites | capture-free | share | capturing | dynamic |
|---|--:|--:|--:|--:|--:|
| codeload-closure | 58 | 23 | **39.7%** | 35 | 0 |
| codeload-jquery | 534 | 82 | 15.4% | 452 | 0 |
| pdfjs | 949 | 145 | 15.3% | 803 | 1 |
| typescript | 1 763 | 240 | 13.6% | 1 518 | 5 |
| box2d | 982 | 129 | 13.1% | 853 | 0 |
| mandreel | 1 476 | 109 | **7.4%** | 1 366 | 1 |
| **total** | **5 762** | **728** | **12.6%** | **5 027** | **7** |

**So there is no cheap subset to take first.** 87.3% of sites need the capture mechanism, which
is to say the mechanism *is* the item — and the share is worst exactly where the prize is largest,
7.4% on Mandreel, whose compile is the biggest in the corpus and 99.7% of whose functions are
never invoked. The one refusal that no mechanism can lift, `Dynamic`, is **7 sites of 5 762,
0.1%** — the second time this item's stated risks have come back in an order the measurement
reverses, after the early-error pre-parse turned out to be a tenth of the compile it was said to
dominate.

**And the reading that looked like an opening is refused by the counter built to test it.**
Mandreel is 1 364 *top-level* function declarations, and its 7 605 bound free names are only
**165 function-owned** — the other 97.8% resolve to a program-level binding. A script's top-level
`var` and function declarations are properties of the global object per spec, so those names
looked like they should cost a deferral nothing, which would have made the largest compile in the
corpus deferrable for free. They do not: **`cellBacked` equals `bound` exactly on all six corpora,
15 118 of 15 118**, because this engine gives a program-level binding a CLR local in the program
lambda like every other binding, and a nested body referencing one needs a `Box[]` entry on
exactly the same terms. ***A spec-level fact about where a binding lives is not a fact about where
the compiler puts it*** — and the reason that is a finding rather than a guess is that the two
counts were kept apart on purpose instead of being assumed equal.

**The instrument was made to discriminate before it was pointed anywhere**, which is what makes an
exact equality mean something rather than being a claim about the counter — §3.5's rule, paid for
by items 3-8a and 3-9. Eighteen fixtures, the negatives load-bearing: the **same body text in two
enclosing scopes gives two different verdicts**, which nothing that scans the function alone can
do; a parameter or an inner `var` sharing the outer spelling captures nothing; and `cellBacked` is
shown to separate from `bound` *before* the equality is reported, through a named function
expression's own name — which binds with no CLR local and reads **1 bound / 0 cellBacked** against
an ordinary local's 1 / 1. Deliberately breaking the resolver fails **exactly the four
capture-detecting fixtures** and leaves the other eleven green. The denominator is cross-checked
rather than asserted: classified sites match `--compile-profile`'s own function count **exactly on
four corpora**, and by **+2 and +1** on the two that evaluate a CodeLoad epilogue — which contains
exactly 2 and 1 inline functions.

**One hazard was found by writing it and is worth more than the count.** The obvious way to ask
where a name resolves is `FastFunctionScope.GetVariable`, and it **sets
`RootScope.HasOuterFunctionCaptures` as a side effect of answering** — a conjunct of item 4-2a's
tiering gate. A probe built on it would have turned tiering *off* for every function it merely
asked about, which is an instrument changing the thing it measures, silently and only on the arm
where it is enabled. The probe uses a side-effect-free `TryResolveBinding` instead, and a fixture
runs the same program on both settings of the switch. *A read-only question asked through a
mutating API is not a read-only question.*

**What this does not change: the item's size or its ranking.** It is still **L**, still the front
of phase 1, and still worth 33.6–63.9% of compile over a population 84–99.7% never invoked. What
it removes is the option of doing a twelfth of it cheaply and calling the item started.

#### The capture layout, attempted — and two obstacles the item's statement does not name

**The item says its remaining half is blocked on one thing**: a captured name's index in the
enclosing lambda's `Box[]` is decided by `LambdaRewriter` *from the tree*, and a deferred body has
no tree, so the layout must be derivable from source alone. `0101` built the free-name walk that
would derive it. This builds the **checker** that says whether such a derivation is correct —
because the only property that matters about it is asymmetric: over-approximating costs a box per
creation site, and **under-approximating is a miscompile**, so a single missed capture disqualifies
the whole approach.

`DeferredCaptureLayout` records what the front end predicts from `FreeNameScan` plus scope
resolution, and compares it at relay against what the rewrite actually decided. Nine fixtures,
including one that **deliberately records an empty prediction against a non-empty truth and asserts
the miss is reported** — without it every green below is vacuous, which is `0096`'s rule applied to
a comparison rather than an emitter.

**Obstacle 1, found on the simplest fixture there is, and it is a distinction this document has been
conflating.** `ClosureRepository` holds **two** populations, and the item's phrase *"which variables
this lambda captures"* means one of them:

- bindings **handed in** from an enclosing scope (`Setup`, appended to `Inputs`, `index >= 0`) —
  what a deferred body needs a `Box[]` index for, and what a free-name walk answers;
- the lambda's **own locals that something nested captures** (`Convert`, `index == -1`) — which
  must live in a cell, and which a free-name walk of that lambda correctly does *not* name,
  because they are not free in it.

Compared against the whole repository, `function outer(){ var q = 1; var inner = function(){ return q; }; }`
reports **`outer` missing `q`** — a function "missing" its own local. The prediction was right and
the comparison was wrong. *So the deferral needs two derivations, not one*: the enclosing function
must learn which of its own locals to box from **its children's** free-name sets, and the deferred
body must learn its own free names' indices. `FreeNameScan.ForProgram` already computes bottom-up
and can serve both; nothing in the item's text says two are needed.

**Obstacle 2: every function captures a binding no source identifier names.** `ScriptInfo_1` —
script metadata the compiler threads into each function — is handed in to every one of them. A
free-name walk cannot predict it and must not be charged for it, so the layout has to carry a
reserved region for compiler-introduced captures alongside the source-derived one. Also in that
class: `this`, `arguments`, `new.target`.

**The instrument disagreed with itself, and fixing that is what produced the answer.** The first
corpus run reported 170 missed sites and, in the same run, Mandreel at **2 622 exact against 1 476
predicted sites** — more checks than predictions, which can only mean a site is relayed more than
once. Three corrections, in the order they had to be made:

1. **Repeats are recognised rather than counted.** A layout is a property of a *site*, so counting
   it once per relay makes every total a function of how many times the enclosing lambda happened
   to be emitted. **Mandreel relays 1 336 of its 1 358 sites twice**; jQuery and Typescript relay
   none twice. And the interesting half is counted rather than assumed — **repeat disagreements
   are 0 on every corpus**, so the repeat is pure duplication and the rewrite decides the same
   capture set on every relay.
2. **Undeferrable bodies are excluded, which was a defect in this checker.** A
   `FreeNameScan.Dynamic` body can reach bindings its text never names, so there is no set to be
   right about — but recording it as an *empty prediction* reports every one of its captures as a
   miss, which is the strongest signal the instrument has. That was the whole of the
   `predicted{}` population on Mandreel, PdfJS and Typescript. **7 sites across the corpus.**
3. **And then one real defect, in `0101`'s own code.** With the noise gone, every remaining miss
   was the same shape — `F/F`, `G/G`, `Dict/Dict`, `mandreelNextDecompress/…` — **a function
   referencing its own name**. `FreeNameScan.EnterFunction` bound the function's own name inside
   the function unconditionally, with a comment describing the *named function expression* case.
   A function **declaration**'s name is bound in the **enclosing** scope, so a self-reference is a
   free reference and a deferred body must be handed a box for it. **138 sites across five
   corpora reported as capturing nothing.** The fix is one condition, and it is a soundness fix
   rather than a precision one: built on as it stood, a deferred self-referential declaration
   would have resolved its own name to a box that was not there.

**With those three, the corpus reports zero.**

| Corpus | checked | excluded | repeats | disagreements | exact | over | **missed** |
|---|--:|--:|--:|--:|--:|--:|--:|
| codeload-closure | 49 | 0 | 0 | 0 | 48 | 1 | **0** |
| codeload-jquery | 413 | 0 | 0 | 0 | 272 | 141 | **0** |
| mandreel | 1 358 | 1 | 1 336 | 0 | 1 346 | 12 | **0** |
| pdfjs | 795 | 1 | 7 | 0 | 220 | 575 | **0** |
| typescript | 1 569 | 5 | 0 | 0 | 357 | 1 212 | **0** |
| box2d | 973 | 0 | 4 | 0 | 202 | 771 | **0** |
| **total** | **5 157** | **7** | **1 347** | **0** | **2 445** | **2 712** | **0** |

**So the precondition holds on the corpus: a layout derived from source alone never misses a
capture the rewrite makes.** It over-approximates on **2 712 of 5 157 sites** — safe, and the
price is one box per over-predicted name at each creation site plus that name's numeric tier,
which is the cost side the mechanism will have to be measured against.

**The one shape the corpus does not contain is now closed too, and it took two attempts.** A named
function *expression*'s own name is bound inside itself by the specification — `FreeNameScan` is
right that it is not free — and **this engine materialises that binding as a `JSVariable` parameter
in the enclosing scope which the body captures**, so the layout must carry it anyway.
`var f = function g(n) { … g … }` read `g/g predicted{}`.

**The first attempt looked the name up in the function's own scope and tested `Variable != null`,
which is exactly the field this binding leaves null on purpose** — its own comment says it *"is not
a local Variable of this scope (it is captured read-only), so it is exposed via
`EvalCaptureExpression` only"*. Reading `Variable ?? EvalCaptureExpression`, the same disjunction
`VariableScope.CaptureExpression` already makes, closes it. That is item `0097`'s rule for the third
time — ***ask what the compiler built, not what the analysis proved*** — and the first time it has
decided a **mechanism** rather than a measurement.

**Adding the self-name unconditionally cost 126 sites of precision**, because a named function
expression that never mentions its own name is handed no cell for it. So `FreeNameScan` now gives
the self-name **a scope of its own**, below the function scope, which is where the specification
puts it too: a parameter or body binding of the same spelling shadows it correctly, and a reference
that reaches it can be told apart from one that reaches a parameter. `SelfNameReferenced` is then
exact rather than "the function has a name".

| | missed | exact | over |
|---|--:|--:|--:|
| before the self-name was predicted | 0 *(shape absent from corpus)* | 2 445 | 2 712 |
| self-name added unconditionally | 0 | 2 319 | 2 838 |
| **self-name gated on a reference** | **0** | **2 445** | **2 712** |

**The gap closes at no precision cost** — the same 2 445 exact as before it was predicted at all.

#### The layout question, asked as the item states it — as an INDEX — `0112`

**`0104` predicted a set and checked membership; this document then recorded it as having settled
the layout, and later sections repeated that. It had not.** The item states its obstacle as *"a
captured name's **index** in the enclosing lambda's `Box[]`"*, and that index is `Inputs.Count` at
the moment `ClosureRepository.Setup` first sees the binding — **the order the closure rewrite's
descending walk meets it in the body**. The prediction was a `HashSet` built from
`FreeNameScan.Free`, itself a `HashSet`. *It has no order, so it could not have answered the
question even in principle.* Zero missed names means a deferred body would be handed the right
bindings; it says nothing about whether it would find each one **in the slot the creation site put
it**.

So the order is now recorded and compared — against `repository.Inputs`, the array the creation site
emits in index order, rather than against the `Closures` dictionary whose enumeration order is a
hash-table detail.

| Corpus | ordered | **exact** | **mismatched** | sets differ |
|---|--:|--:|--:|--:|
| codeload-closure | 72 | 34 | **0** | 38 |
| codeload-jquery | 497 | 143 | **0** | 354 |
| mandreel | 1 867 | 253 | **0** | 1 614 |
| pdfjs | 2 674 | 639 | **0** | 2 035 |
| typescript | 4 255 | 1 430 | **0** | 2 825 |
| box2d | 5 240 | 1 962 | **0** | 3 278 |
| **total** | **14 605** | **4 461** | **0** | **10 144** |

***Where the predicted set equals the handed-in set, the predicted order equals the slot order —
4 461 of 4 461, without exception.*** That is the reassuring half, and it is new.

**The other 10 144 carry a consequence `0104`'s framing did not.** They are the over-approximation
it counted at 2 712 sites, which it recorded as *"safe, and the cost side the mechanism must be
measured against"* — one box per creation site. **For membership that is right. For a layout it is
not merely a cost**: an extra predicted binding shifts every later slot, so the predicted numbering
and the tree-derived numbering are *different numberings*, not the same one with spare entries. That
is only safe if **the deferral drives the layout from the prediction rather than matching it** — the
enclosing function boxes exactly what was predicted, in predicted order — which is a design
constraint the item did not previously carry and which makes over-approximation cost boxes rather
than correctness.

**Two defects found writing it, both in this patch's own change.** Adding the order insertion
without braces made the following `else` bind to it — the dangling else — so `SelfNameReferenced`
stopped being set, and the named-function-expression fixture failed on the first run; *that fixture
is a paired assertion precisely so a one-sided regression cannot pass it*. And the parent's free
order was composed by iterating the child's **set**, which would have scrambled the property being
measured — fixed, and re-measured **identical**, so the hazard was latent rather than active.

**State: the layout is validated, the deferral is not built, and the item is still L.** What is now
known that was not: the derivation **never under-approximates**, on the corpus *or* on the one
shape the corpus lacks; it over-approximates on **53%** of sites, which is the cost the mechanism
must be measured against; `ClosureRepository`'s two populations must be told apart; and the
compiler-introduced captures (`ScriptInfo_*`, `this`, `arguments`, `new.target`) need a reserved
region beside the source-derived one. **Two soundness defects were fixed getting here**, both about
a function's own name, and both would have miscompiled a deferred body built on the layout as it
stood.

#### The mechanism: **the enclosing scope is kept alive, and re-entry is built** — `0105`

**The item's stated raw material does not serve it, and that is provable rather than arguable.** The
item says `JSFunction` *"already carries `source` and already recompiles from it for tiering — the
raw material for deferring is present"*. Read, `RecompileForTiering` wraps the text as `({source})`
and hands it to `CoreScript.Compile` **as a fresh top-level script** — no enclosing scope, by
construction, which is why 4-2a had to refuse the identity cases and repair strictness. The tiering
gate enforces the same thing from the other side: it admits a function only when
`!HasOuterFunctionCaptures && !HasNestedFunctions && withBoundaries.Count == 0`, not an arrow, not a
class, not direct-eval compilation. ***So the recompile path is sound precisely for the functions
with no enclosing context to reproduce, which is the complement of the population a deferral
serves*** — `0102` counted 87.3% of sites capturing something. The two mechanisms share a source
string and nothing else.

**What the deferral actually needs, itemised**: a body compiled at first call must be handed **14**
`CreateFunction` parameters, **9** reads of the enclosing `FastFunctionScope` and **5**
`FastCompiler` fields. The capture layout `0104` settled is the *solved* part; twenty-eight other
pieces of state are not.

**So the scope is kept rather than reproduced, and that turned out to be nearly free.**
`FastFunctionScope` **is not pooled** — its `Dispose` is `LinkedStackItem`'s, which only pops the
stack — so a scope object already outlives its frame, and holding a reference retains the whole
`Parent` chain, valid and unrecycled. Re-entry is `LinkedStack.Switch`, which the stack already has.
Only the compiler's own five fields are saved and restored, on the throwing path too, because a
deferred compile happens in the middle of somebody else's work and a moved scope stack would
corrupt a compilation that has nothing to do with it.

**Retention is inert and the check is the point.** With the switch on, the body is still compiled
eagerly *and* a context is retained; the question is whether compiling it a **second** time, after
the enclosing compilation has finished, reproduces the first tree. **That comparison is only
possible while both exist**, which is why it is made before anything is deferred — once the eager
tree stops being produced there is nothing to check the re-entered one against.

As first read, that was **4 811 of 5 723, 84.1%** — the number `0106` below re-takes and then
explains away, because two of the three things holding it down were the comparison and the check's
own second compilation rather than the re-entry.

Nine constructed shapes reproduce exactly — a three-level capture, a per-iteration `let` cell, a
shadowed name, a recursive declaration beside a named function expression, `'use strict'`
inheritance, and a fixture asserting the compiler state is left as it was found.

**Equality here is alpha-equivalence, and finding that out was the first result.** A second
compilation necessarily draws fresh names from the compiler's monotonic counters, so the raw text
differs everywhere: `Context3` against `Context4`, `#TempJSValue20` against `#TempJSValue31`,
`EnableTiering(…, 0, 0)` against `(…, 1, 1)`, and — the whole of the corpus's first pass —
`PropertyInlineCacheSite.Get(7, …)` against `Get(236, …)` at **every property access**. Those are
item 4-2b's process-wide site counter; **a genuine deferral compiles the body once, so they cannot
arise**. Canonicalising each family by *first appearance* rather than erasing it keeps a re-entry
that emitted them in a different **order** visible.

#### The residual, settled — and most of it was the comparison — `0106`

The paragraph this replaces said the residual **15.9%** looked like ordinal divergence on every
instance examined, that a printed diff *"can only ever answer 'equal up to a renaming somebody
thought of in advance'"*, and that settling it needed something else. It is settled, and **the
first thing the settling found was a defect in the checker rather than in the mechanism.**

**The gensym families shared one ordinal table, keyed on the bare number.** So `Context3` and
`#TempJSValue3` shared an entry, and any function where two families drew the same number on one
side and not the other **desynchronised every ordinal after it** — reporting a difference that was
an artifact of the comparison. The families do not share a counter; the table must not either.
**One table per family takes five of six corpora to 100.0%.**

**Then the residual is partitioned rather than described.** Beside the equality up to an
order-preserving renaming, the same three families are **erased** instead of mapped — which asks
whether the two trees agree in every token a counter did *not* produce.

| Corpus | re-entered | reproduced | **structural** | threw |
|---|--:|--:|--:|--:|
| codeload-closure | 58 | **100.0%** | **100.0%** | 0 |
| mandreel | 1 476 | **100.0%** | **100.0%** | 0 |
| codeload-jquery | 534 | 99.8% | **100.0%** | 0 |
| pdfjs | 910 | 99.7% | **100.0%** | 0 |
| box2d | 982 | 99.7% | **100.0%** | 0 |
| typescript | 1 763 | 73.7% | **100.0%** | 0 |
| **total** | **5 723** | **91.8%** | **100.0%** | **0** |

***No re-entered body differs from its eager tree in a node, an operator, a constant or a shape*** —
5 723 of 5 723, on every corpus, with none throwing. Erasure is deliberately the **weaker** of the
two questions and is reported *beside* the stronger rather than instead of it, because what it
hides is a permutation of counter values.

**And the 471 functions whose ordinals still differ classify exactly two ways, with nothing in
"other".** **460** are the site table's `-1` sentinel: the check compiles every body a *second*
time and a retained context recompiles its **whole subtree**, so Typescript drives item 4-2b's
process-wide counter from **24 759 to exactly its `MaxSites` of 65 536** and every allocation after
that is refused. **11** are the **eager** side re-using a site the re-entry allocated fresh — 4-2b's
tier-2 rule working as designed, since a recompile for tiering re-uses the tier-1 site so the
feedback it consumes stays addressable, and a re-entry has no tier-1 tree to re-use from.
***Both are properties of compiling the same body twice in one process, which a deferral by
construction does not do.*** The counts are per-process, because sharing the counter across six
corpora is what made the first reading 84.1% instead of 91.8%.

**Both equalities are pinned directly rather than trusted for never having failed** — §3.5's rule
from `0096`, and the rule `0104`'s layout checker was built under. One fixture drives a counter
renaming (both accept), a token no counter produced (both reject), and **a site re-used on one side
only**, which the strong equality reports and the weak one cannot: that is the exact gap between
the two numbers, asserted rather than described, and it is why the 471 are classified from the raw
sequences instead of left inside the weaker one.


**And nothing is deferred yet.** The eager path still compiles every body; the switch
(`BROILER_JS_DEFER_TREE`, default off) decides only whether a context is retained, and a fixture
asserts the same program returns the same answer on both settings. What is built is the half that
had no evidence — that a body *can* be compiled later from a kept scope — and it is built where it
can still be checked against the answer it has to match. **Size: still L**; what remains is
suppressing the eager build and threading the deferred site through `Relay`, which
`BLambdaExpression`'s readonly `Body` makes a change to the expression node rather than to the
compiler.

#### What landed for it: the closure rewrite is no longer walked once per level

**Measuring the phases found a repeat, in the phase 1-1 had already deferred.** With deferral on,
emission is 25–57% of compile and the closure rewrite is about half of *that* — which is odd for a
compile that generates IL for one lambda. It is not odd: `LambdaRewriter.Rewrite` descends through
nested lambdas (that is how `CheckForClosure` threads a capture up the whole chain), and
`RuntimeMethodBuilder.Relay` then called it **again** with the relayed lambda as its own root.
A lambda at depth *d* was walked *d+1* times, and jQuery's single IIFE means its whole tree was
walked twice by a compile that emitted almost nothing.

**Counted rather than argued, and then counted again for the claim that matters.** Two counters on
the relay, one per site: how many relayed sites needed a rewrite of their own, and how many had
already had one from the walk that emitted their parent. On jQuery, Box2D and Typescript the first
is **0 — 0 of 415, 0 of 978, 0 of 1 574.** But *"every relay is a repeat"* and *"the repeat does
nothing"* are two claims and only the first is what that counter measures — §3.5's rule about
indirect instruments, which this campaign has now paid for twice. A third counter answers the
second directly: with the switch **off**, so the repeat still runs, count the captures it creates
that the first walk had not. It is **0 on every corpus** — 0 against
415, 51, 978, 804 and 1 574 repeats respectively — which is what makes the skip a removal of
repeated work rather than of work.

`Relay` now skips a lambda a descending walk has already entered, marked on the lambda itself.
The mark is set only by a walk that rewrites nested lambdas, so `RewriteRootOnly` — the async
pre-rewrite, which stops at each nested lambda *by design* — leaves it clear, and anything built
after the walk (a generator or async body rewritten into a state machine) is rewritten at relay
exactly as before. That is why the skip is a fact about this tree rather than a guess about which
lambdas need rewriting. `BROILER_JS_RELAY_REWRITE_ONCE=0` restores the repeat.

**Measured, and one corpus of three does not separate.** `--compile-profile`'s whole-compile
number, ABBA-interleaved on one build with the switch as the only difference, one corpus per
process, six pairs an arm:

| Corpus | repeat (median) | skipped (median) | median pair ratio | pairs favouring the skip | control-arm spread |
|---|--:|--:|--:|--:|--:|
| codeload-jquery | 502.5 ms | 394.5 ms | **0.782×** | **6 of 6** | 13.0% |
| typescript | 1 940.6 ms | 1 711.3 ms | **0.867×** | **6 of 6** | 15.9% |
| box2d | 793.2 ms | 882.0 ms | 1.055× | 2 of 6 | **55.6%** |

Box2D is reported rather than dropped, and the last column is why it cannot be read either way:
its own *control* arm ranges 662–1 103 ms, a 55.6% spread against an effect near 10%. So the
whole-compile instrument answers on two corpora and is silent on the third.

**The phase that changed was then measured directly, and `--compile-phases` carries its own
control**: `parse` cannot be affected by this change, so a round whose parse column moves is a
round that measured the machine. Two rounds an arm; the first moved parse by 1.49× and 1.86× and
is discarded on that basis alone. In the round where the control holds to 6%, **Box2D's emission
phase goes 99.9 → 54.8 ms, 0.549×, and its whole compile 267.4 → 207.2 ms, 0.775×** — the same
ratio jQuery's whole compile shows on the other instrument. jQuery's own phase round has its
control drift 27%, so it is quoted only for direction: 59.1 → 28.4 ms.

*Two instruments, three corpora, one discarded round and one non-separating corpus is a weaker
result than a single clean table would be, and it is what the machine gave.* What is not
weak is the mechanism: the walk removed is provably a repeat on every site of every corpus, and
provably creates nothing.

**What is still open.** Tree construction itself is still eager, and the two tables above say what
it is worth: **43–75% of compile, over a population that is 84–99.7% never invoked.** *(The
charge-back has since been built and measured at 7–12% of that; see "The remaining half, priced"
above.)* Closing it
needs the capture mechanism, and **one sentence of this item's cost side needs correcting before
anyone starts.** "Baking the cells in as constants is not available — `EmitConstant` throws for
any reference type that is not a `string`, `Type` or `MethodInfo`" is true and is not the
obstacle: the engine already carries per-instance reference state into a generated method, through
the `Box[]` the creation site passes and the `Closures` the delegate is bound to, and a compiler
that knows a name's *index* in that array binds it with an array load rather than a name lookup.
So the capture mechanism is not missing, it is **unaddressable**: the index is decided by
`LambdaRewriter` from the tree, and a deferred body has no tree. What the remaining half has to
build is the eager step that makes the array addressable without one — a free-name walk per
deferred function, resolved against the enclosing scopes and recorded as a name → index map, which
`--compile-phases` charges back as `scanMs` and measures at **0.9–164 ms against 15–3 037 ms of
tree construction, 5.4–9.9% of it** — a lower bound, since a real scanner also has to resolve each
name and tell a free reference from a locally bound one. **Size of what remains: L**, and the sub-project inside it is
that map, not a pre-parser and not `EmitConstant`.

**Verify.** `DeferredCompilationTests` — ten fixtures covering the item's four named risks
against the implementation (a syntax error in a never-called function still throws at compile
time; per-instance loop captures; writes through a captured cell after first call; self- and
mutual recursion through a thunk; direct `eval` inside a deferred body; a deferred generator;
100 instances of one site keeping distinct state; concurrent first calls). Full repository
suite **7 640 tests, 0 failures**.

**Verify (the relay-rewrite half).** `RelayRewriteTests` — 19 cases, and the point of every one is
that a capture has to be threaded through levels that never mention it: a read three levels down,
a write back through two, per-instance loop cells two levels down, a generator and an `async` body
nested in a closure (the two rewrites that build lambdas *after* the descending walk), `this`
through an arrow, a named function expression's own name, a direct `eval` two levels down, and
five levels each reading and writing every binding above them. **Each is asserted on both settings
of `BROILER_JS_RELAY_REWRITE_ONCE`**, so they are a statement about closure semantics rather than a
description of the skip — and with the switch off they all still pass, which is what says the
second walk was not what they were relying on. Plus the counter invariant, as a test rather than a
corpus reading. Full repository suite **7 944 tests, 0 failures**, all thirteen projects.

**And the conformance gate was run, which the first version of this section said had been skipped.**
All five pinned manifests at `07adeb44` plus `0082` — the tree `0aa8a558` now is, an ancestor of
the pin — on linux-x64: **8 710 executed, 8 617 passed,
84 failed, 251 skipped, 9 timed out — every count identical to §3.4's recorded row, manifest by
manifest**, and the same *files* rather than the same totals (all 84 failures need `$262`; the 9
timeouts are lines 7–15 of `test262-failures.txt`). The reason to run it despite the change having
no semantic surface is that "no semantic surface" was the claim under test: what `0082` removes is
a walk that decides which bindings a nested function captures, and `strict-mode` and
`lexical-declarations` are where a lost capture would show.

**A second unreproduced failure is on the record, and it is not the one above.** One full-suite
run reported `CapturedNumericLocalTests.SuspendingNestedFunctionsCaptureThroughTheSameBox(captured:
False)` — item 3-7's fixture — returning `"2,12"` for `"2,2"`. That case runs an `async` body with
an `await 0` and asserts the continuation has *not* run when `Eval` returns, so what it observed is
a scheduling order, not a capture: `out` was correct on both sides and only the post-`await`
statement had run. It did not reproduce in **six further runs of that assembly, three on each
setting of the switch**, nor twelve times in isolation on both settings, nor in the final full-suite
run. Recorded rather than dismissed, on the same terms as the `ModuleExtensions` flake above: if it
recurs, it is a test that asserts a microtask has not been drained, and that is what to look at
first.

**One unreproduced failure is on the record.** A single run of the full suite reported
`Broiler.JavaScript.ModuleExtensions.Tests` 1 of 5 failed. It did not reproduce in **nine
subsequent full-suite runs** — four with `BROILER_JS_DEFER_IL=0` and five with it on, same
build — nor in isolation. That project's first test guards a *module-initializer ordering*
bug by its own comment ("before the BuiltIns `[ModuleInitializer]` that wires it had run"), so
it is order-dependent by construction and a plausible pre-existing flake. **Eight runs cannot
separate a 1-in-10 flake from a 1-in-10 regression**, so this is recorded as unresolved rather
than dismissed: if it recurs, start here.

---

### 1-2 · Stop recursive compilation from overflowing — **mitigation landed**

**The diagnosis this item carried was wrong about its own cause, and its acceptance
criterion passed before any work was done.** Both were settled by measurement at
`685026c0`, and the corrected item is below. What was wrong:

| This item said | Measured |
|---|---|
| The trigger is source **size** — `global_init`'s 152,948 lines | A **flat 200 000-statement** function compiles and runs (30 s). Size is not the trigger; **nesting depth** is: a `1+1+…` chain overflows between 10 000 (passes) and 20 000 (aborts) operators |
| The failure is `EnsureWithinStackBudget` — a catchable `RangeError` | On linux-x64 it is a **hard CLR stack overflow that aborts the process**. Not catchable, no `try` reaches it, and no JavaScript stack is involved at all |
| One site: the compiler's AST visitors | **Three passes**, in three assemblies. The *parser* is one of them — upstream of the compiler entirely |
| Verify: "a generated 200k-line single-function script compiles … without overflow" | **Already passed at `685026c0` before the change.** As written the criterion certified nothing |

The three passes, each reached in turn as the one above it was given more stack:

| Pass | Assembly | Overflows on |
|---|---|---|
| `FastParser` recursive descent (via `FastScanner.Commit`) | `.Parser` | a right-nested conditional, 20 000 levels |
| `AstReduce.VisitBinaryExpression` under `SyntaxValidation.StrictModeValidator` | `.Compiler` | a left-nested `+` chain — the abort trace shows ~19 400 levels of its three-frame cycle on the stack, ~845 B per level |
| `ILCodeGenerator.VisitBinary` / `VisitCall` | `.ExpressionCompiler` | the same chain, reached only once the front end survives it |

All three measured on the script host's 16 MiB thread. The ~845 B per level is what makes
the ceiling predictable: it scales with the stack, so the same shapes abort at ~1 200 levels
on a 1 MiB Windows main thread and ~9 700 on an 8 MiB Linux one.

**Target.** Any deeply nested source, which is a *correctness* result before it is a
performance one: a syntactically valid script takes the host process down. Machine-
generated code is where nesting gets deep without anyone intending it, which is how this
reached the roadmap through Mandreel — but see the repro note below, because Mandreel is
not the demonstration this item thought it was.

**Where.** `Broiler.JavaScript.ExpressionCompiler/CompilationStack.cs` (new);
`Broiler.JavaScript.Runtime/CoreScript.cs`, `.../DictionaryCodeCache.cs`,
`Broiler.JavaScript.Compiler/DirectEvalSupport.cs`,
`Broiler.JavaScript.ExpressionCompiler/Runtime/RuntimeAssembly.cs` for the boundary;
`Broiler.JavaScript.ExpressionCompiler/StackGuard.cs` for the real fix.

**Work.**

1. **Mitigation (S) — landed as `43bc4230`.** Compilation
   runs on a thread the engine sizes (`CompilationStack`, 64 MiB, settable via
   `SizeBytes` or `BROILER_JS_COMPILE_STACK_BYTES`; `0` compiles in place). The boundary
   sits at the point each `ICodeCache` starts a compilation, so parse, validation, tree
   construction and IL emission share one crossing, with catch-alls in `CoreScript` and
   `RuntimeAssembly` for a host that brings its own cache. Two details are what make it
   affordable:
   - **Workers are parked and reused.** A thread per compilation costs ~300 µs — the
     reservation is a kernel mapping, not bookkeeping — which measured **+27%** on a
     compile-only loop. Renting leaves two semaphore handoffs.
   - **Short sources stay put.** Nesting depth cannot exceed source length, so a source
     under 512 characters cannot exhaust even a 1 MiB stack and is compiled where it
     stands. This is a bound, not a heuristic, and it is what takes the remaining cost to
     nothing: the handoff is a fixed ~180 µs, which is unmeasurable against a large
     compile and unaffordable against `eval` of one expression.

   Cost, measured ABBA-interleaved at process granularity over five pairs of a
   5 000-compile loop, on **one build with the environment variable as the only
   difference** — comparing two builds cannot separate this from anything else that
   changed: **+1.5% by median of paired ratios, +3.2% comparing medians, and one pair of
   five negative.** Inside this container's noise band, and the loop is the worst case
   (pure compilation, no execution). All four nesting shapes that aborted now compile. With
   `BROILER_JS_COMPILE_STACK_BYTES=0` the fixtures below abort the test run, which is what
   makes them decisive rather than merely green.
2. **Real fix (M) — landed for the two visitor passes; the parser is still open.**
   `StackGuard` existed to segment the emitter's recursion and **could not fire**, for three
   independent reasons any one of which was fatal: it tested `address - start > MaxStackSize`
   on a stack that grows *downwards*, so the difference was negative and the branch
   unreachable; it truncated 64-bit stack addresses to `int`; and its threshold was **1 024
   bytes**, so with the sign corrected it would have hopped threads every few frames.

   The rules now live in `ExpressionCompiler/StackSegment.cs` and are shared, because they are
   exactly what a copy-per-pass got wrong: anchor high, measure `anchor - current` the way
   `CallFrames.EnsureWithinStackBudget` does, read it unsigned so an upward-growing stack
   degrades to *never segments* rather than *always*, and hold the threshold in megabytes
   (4 MiB, `BROILER_JS_VISITOR_SEGMENT_BYTES`, `0` to disable).

   **Two things had to be discovered rather than designed.** Segmentation goes through a new
   `CompilationStack.RunOnFreshStack`, not `Run`, because `Run` returns *inline* whenever a
   compilation boundary is already established on the thread — and a segmenter only ever fires
   from inside one, so routing it through `Run` would have segmented nothing. And the pass that
   actually overflows first is **not** the emitter: it is `AstReduce.VisitBinaryExpression`
   under `SyntaxValidation.StrictModeValidator`, whose base `AstMapVisitor<T>` never derived
   from `StackGuard` at all. Repairing `StackGuard` alone therefore did not move the repro;
   the same guard had to be put on `AstMapVisitor.Visit`. **`FastParser`'s recursive descent is
   the third pass, and it is now guarded too** — see below.

   **Verified by turning each mechanism off independently**, which is the only way to tell which
   one is doing the work. A 20 000-operator chain through the script host: mitigation on / guard
   on completes, **mitigation off / guard on completes** — the guard alone — mitigation on /
   guard off completes, and with **both off the process aborts**. That last row is what makes
   the other three mean anything. Cost, interleaved on one build with only the environment
   variable differing: **median paired ratio 1.0027**, i.e. nothing.

   > **That matrix is a linux-x64 result, and the second row does not hold on win-x64.** Re-run
   > there while guarding the parser, *this same 20 000-operator chain* completes with the
   > mitigation on and **aborts with it off**, guard or no guard. The reason was measured rather
   > than guessed: with the mitigation disabled the front end compiles in place on a stack that
   > tops out at **1 052 048 bytes** — about 1 MiB — while `StackSegment.SegmentAtBytes` is
   > **4 MiB**, so the threshold is larger than the whole stack and the guard cannot fire once.
   > `StackSegment`'s own remarks anticipate exactly this ("a walk cannot know how large the
   > stack it is standing on actually is, and a threshold above it would never be reached before
   > the CLR aborted the process"); what is new is that the condition is *reachable on a shipping
   > platform* with the mitigation off. It costs nothing in the default configuration, where the
   > worker is 64 MiB and the guard fires freely — 223 times on a 10 000-level parse. **The fix
   > is an adaptive threshold** (`RuntimeHelpers.TryEnsureSufficientExecutionStack` probes what
   > is actually left, rather than assuming), and it belongs to `StackSegment`, so it would move
   > all three passes at once. Not done here. *A one-platform matrix dates its rows to that
   > platform — the same lesson this item already learned from Mandreel, applied to its own
   > verification instead of to its repro.*

   *No unit test pins the guard-alone row.* Doing so means setting `CompilationStack.SizeBytes`
   to 0, which is a process-wide static that xUnit's parallel classes would race on, so it needs
   process isolation the fixtures do not have. The four-way matrix above is a manual result, and
   saying so is better than a test that appears to cover it and does not.
3. **The parser pass (S) — landed.** `FastParser`'s recursive descent was the last of the three
   and the one that overflows *first*, before the validator or the emitter ever see a tree.

   **The failing case was written first and watched to fail, in the configuration that ships.**
   A right-nested conditional through the script host, mitigation at its default 64 MiB:
   20 000 levels returns its answer, **25 000 aborts the process** — no exception, nothing to
   catch. So this was not a defect that needed a diagnostic switch flipped to see; it was
   reachable by a syntactically valid script on the default build.

   **Where.** `Broiler.JavaScript.Parser/FastParser.Expression.cs`. The abort trace's repeating
   cycle is seven frames — `Expression` → `SinglePrefixPostfixExpression` →
   `SingleMemberExpression` → `SingleExpression` → `BracketExpression` → `ExpressionList` →
   `Expression` — and `Expression` appears in it twice, so guarding that one entry covers every
   nested construct. `.Parser` already references `.ExpressionCompiler`, so it shares
   `StackSegment` rather than copying it, which is the whole point of that struct existing.

   **After: 25 000, 40 000 and 90 000 levels all complete.** With the guard alone disabled
   (`BROILER_JS_VISITOR_SEGMENT_BYTES=0`) 25 000 aborts again, which is what makes the pass
   attributable to the guard rather than to anything else in the build.

   **Cost: none measurable.** The guard sits on the parser's hottest entry, so it was measured
   rather than assumed — 3 000 *distinct* `new Function` compilations (distinct so the code cache
   cannot answer them), six interleaved pairs on one build with only the environment variable
   differing: **median of paired ratios 0.9993**, three pairs above 1 and three below.

   **Verify.** A 25 000-level fixture in `DeeplyNestedSourceTests` — the smallest depth that was
   fatal, and decisive without touching `CompilationStack.SizeBytes`. A syntax error at the
   deepest point of one still reports the same exception type and the right offset (1, 552 809),
   which is the path that crosses the worker handoff. Repository suite **7 561 tests across 13
   projects, 3 failures**, the pre-existing win-x64 host ones. **test262 unchanged across all four
   pinned manifests** — 8 220 passed, 84 failed, 9 timed out, identical manifest by manifest,
   which is the gate that matters most for a change to the parser. **Octane 14 of 15 `ok`**, the
   same set as before it, with Mandreel's failure record **byte-identical** to the previous run —
   so this does not fix Mandreel, and does not pretend to: that one aborts on the *JavaScript*
   stack budget during execution, not in parsing.

   > **The first version of this guard was wrong, and the `off/on` row is what exposed it.**
   > It inferred "this is the outermost call" from `!segment.IsAnchored`, which reads correctly
   > and is false: `StackSegment.Continue` *deliberately* clears the anchor so the continuation
   > measures against its fresh stack — so the first call on a segmented continuation calls
   > itself outermost and releases the anchor again the moment that one sub-expression finishes.
   > The accounting then restarts from whatever depth the next call happens to sit at, so the
   > guard fires on an interval it did not choose. Fixed with an explicit recursion counter,
   > which says what the anchor cannot: *this is the top of the recursion, not the top of the
   > current stack.* **Recorded as a structural defect, not as a measured regression** — it was
   > found while chasing the `off/on` failure above, which turned out to have a different cause,
   > and the two were never separated. The fix is cheap and obviously right, so it stayed; what
   > it is worth was not established.
   >
   > **`StackGuard<T,TIn>` makes the same inference** for the validator and emitter passes. Not
   > changed here — those two are verified working and this item is the parser — but it is the
   > first place to look if either is ever found segmenting less than it should.

**Verify.** `Broiler.JavaScript.Compiler.Tests/DeeplyNestedSourceTests.cs` — a nested `+`
chain, a nested conditional, a long flat statement list (kept, so the size case cannot be
re-diagnosed from length again), and a syntax error in deeply nested source reporting the
same type as one in shallow source. They assert values, not exceptions, because the
failure being pinned is the test host *aborting*. Repository suite at the patched tree:
**7 288 tests across 13 projects, 0 failures** — the three failures §4.1 attributes to the
host environment are win-x64 ones and do not reproduce on Linux.

> **The repro is win-x64 only, and that is a finding.** Phase 0's local pass lost Mandreel
> and MandreelLatency to `EnsureWithinStackBudget` on win-x64. Run here on linux-x64 at
> `685026c0` against upstream `chromium/octane`, **Mandreel completes either way**:
>
> | Mitigation | Status | Mandreel | MandreelLatency | Duration (budget 1 200 s) |
> |---|---|--:|--:|--:|
> | disabled (`…STACK_BYTES=0`) | `ok` | 138 | 12.6 | 365.5 s |
> | default (64 MiB) | `ok` | 123 | 13.3 | 355.5 s |
>
> So "this is the only thing between the suite and 17 of 17 scores" was a win-x64
> statement; on Linux the suite is not blocked on it, and the mitigation neither fixes nor
> breaks it there. **Read no score movement into those two rows** — one repetition each,
> the two metrics move in *opposite* directions, and §3.2 says a single run cannot tell a
> change from noise. This also does not separate regression from platform difference (the
> two runs differ in both commit and platform), but it does bound the question: whatever
> fires on win-x64 does not fire on linux-x64 at this pointer, so **0-6 will not reproduce
> it and CI cannot be the instrument that closes it.** The synthetic shapes above are the
> repro that exists on both, and they are what the fixtures pin.

### 1-3 · Reduce compile cost per byte — *only after 1-1*

**Do not start here.** If 1-1 lands, most source is never compiled at all and the
remaining throughput may not justify a pipeline change. Measure first with
`ParserCompilerBenchmarks`, splitting the cost three ways: parse, expression-tree
construction, IL emission. The measurement names the target; committing to one now
would be guessing. **Size: unknown by construction.**

There is now one datapoint to start from, taken while working 1-2: **~2.5 ms to compile
`return a + <i>;`** (B4). It is a whole-pipeline figure, so it does not yet split three
ways — but it does say the per-compile floor is milliseconds for a trivial body, which is
the part 1-1 cannot remove. A body that is never called costs nothing after 1-1; the one
that *is* called still pays this.

**The three-way split this item asks for now exists** — `--compile-scaling` times parse,
expression-tree construction and IL emission separately — and it was 1-4 that used it. On
2 000 top-level function declarations the three phases are **2.5 ms / 63 ms / 486 ms**: parse
noise, tree construction 11%, emission 89%.

**But that is the synthetic shape, and on real source the answer is the other way round.**
1-1's deferral removes *all* nested-body IL generation, and on the real corpora that is only
17–36% of compile time — so on a real program most of what is left, after parse, is
expression-tree construction. **1-3 is therefore a front-end item, not the emitter item the
synthetic split implied**, and the first thing it should do is take the same three-way split
on the corpora rather than on generated declarations.

### 1-4 · The closure rewrite was quadratic in a scope's binding count — **landed**

**Not an item either source roadmap had**, because neither could see it: it is invisible to
a probe (one-liners have one binding) and it does not look like a bottleneck in a score
(it looks like "Mandreel is big"). It was found by measuring 1-1's premise, and it is
larger than 1-1's premise.

**What it was.** `LambdaRewriter.Scope` held the variables in scope for one lambda in a
`List<BParameterExpression>` and performed exactly the two operations a list is worst at:
`Contains`, run by `CheckForClosure` for **every parameter reference in the tree**, and
`Remove`, run once per variable as each block scope ends. Both are linear scans, so the
cost of emitting a lambda grew as the **square of the number of bindings in it**. A
script's top level is one lambda, which makes the count of top-level declarations the term
that squares — and machine-generated JavaScript is nothing but top-level declarations.

**The measurement that found it.** `--compile-profile` compiles each corpus twice: as
written, and with every outermost function body replaced by `{}`. The second is the floor
1-1 converges to, so the difference sizes 1-1. Five of six corpora behaved as 1-1 predicts.
Mandreel did not: with **every function body removed**, its remaining 248 KB still took
**17.7 s** to compile, against ~5 ms for the same control on PdfJS, Typescript and Box2D.
Per byte that residue was ~70× more expensive than Box2D's entire source — a difference no
deferral can explain, because there was nothing left to defer.

`--compile-scaling` then varied one thing at a time. Emission on N top-level function
declarations:

| N | parse | tree | **emit** | ms per declaration |
|--:|--:|--:|--:|--:|
| 500 | 0.8 | 12.7 | **796.9** | 1.62 |
| 1 000 | 1.3 | 24.0 | **2 980.8** | 3.01 |
| 2 000 | 2.5 | 70.2 | **13 864.5** | 6.97 |

Just under 4× per doubling with parse and tree construction flat: quadratic, in emission,
in the declaration count. Name length was ruled out in the same run (200-character mangled
names cost the same as `f1`), and plain `var`s were quadratic too — so it is bindings, not
functions.

**The fix.** The scope became a reference-keyed multiset. Three details are load-bearing:

- **A multiset, not a set.** The list held *duplicates* and both operations depended on it:
  a variable registered by two nested block scopes is added twice, and the inner scope's
  exit has to leave the outer registration behind. A `HashSet` would have collapsed the
  pair and taken a still-live binding out of scope — a miscompile, not a lost optimization.
- **The list is kept for small scopes.** A dictionary is not free — hashing a reference is
  a runtime call — and most scopes are small. Dictionary-only bought Mandreel 3.6× and cost
  jQuery, one large IIFE full of small function scopes, ~20%. Promotion above 32 bindings
  takes both ends; the list is abandoned once the index exists, so nothing is maintained twice.
- **The comparison is spelled out.** `List.Contains` resolves through
  `EqualityComparer<T>.Default`, i.e. a virtual `Equals` per element, and
  `BParameterExpression` overrides neither `Equals` nor `GetHashCode`. An explicit
  `ReferenceEquals` loop is the same answer without the dispatch — and it makes the identity
  semantics the rewrite depends on unbreakable by a later `Equals` override.

**Result.** Synthetic, same probe, before and after — emission on 2 000 top-level function
declarations **13 864.5 → 485.7 ms (28.5×)**, and cost per declaration went from climbing
(1.62 → 3.01 → 6.97) to flat (0.38 → 0.25 → 0.28). The speedup factor doubles as N doubles,
which is what tells quadratic-made-linear from a constant-factor win.

Real corpora, **ABBA-interleaved at process granularity, six pairs per arm**:

| Corpus | HEAD | Changed | Ratio |
|---|--:|--:|--:|
| **mandreel** | 21 307 ms | **7 015 ms** | **0.329×** — every pair 0.29–0.40 |
| pdfjs | 1 005 ms | 878 ms | 0.874× |
| typescript | 1 028 ms | 920 ms | 0.894× |
| box2d | 396 ms | 367 ms | 0.927× |
| codeload-closure | 48.9 ms | 50.8 ms | 1.039× — ratios straddle 1 |
| codeload-jquery | 540 ms | 600 ms | 1.112× — ratios 0.669–1.437, no signal |

A second A/B on **one build**, with `BROILER_JS_REWRITER_INDEX_THRESHOLD` as the only
difference, isolates the promotion from the devirtualized comparison: the index alone is
**0.536× on Mandreel** and inside noise everywhere else. So both halves are real and the
larger one is the index.

**What it does not do.** CodeLoad is untouched, and that is the honest reading rather than a
disappointment: its two payloads are 57 and 532 functions *nested inside one IIFE*, so no
scope in them is wide. This item is about width, 1-1 is about depth of work per function,
and Mandreel needed the first while jQuery needs the second.

**Verify.** `DeclarationDenseSourceTests` — a scale-free ratio bound (4× the declarations
must cost under 8×; it was 17.4× before and 2.6× after, so the bound is near neither), plus
two semantic fixtures for the duplicate-registration case a set would have broken. Full
repository suite: **7 630 tests across 13 projects, 0 failures.**

**Size: S.** One class, and it was found by measuring a different item.

---
