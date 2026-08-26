# Performance roadmap — status

The campaign's dated evidence snapshot, in full: the state of every phase and every item,
what phase 2 changed, the committed Octane run and what may be read out of it, the noise band,
the conformance gate, the patch handoff — and §4's state through 2026-08-07.

> The evidence half of [`Roadmap.md`](Roadmap.md). That document carries the detailed campaign
> catalogue and phase verdicts; [`Modernization.md`](Modernization.md) is the current
> cross-track sequencing authority. This record carries the measurements behind them.
>
> **Nothing here is *closed*.** [`Measurement.md`](Measurement.md) governs what may be
> claimed, and the answer is usually *not yet*. Per-phase evidence is in each
> `Phase-N.status.md`; this file carries what is global or cross-phase.
>
> **Snapshot rule.** Detailed campaign rows below preserve measurements taken through
> 2026-08-07. A check mark means the implementation/evidence item existed at that snapshot;
> it does not mean current-code reproduction or MOD-M1 acceptance. Current execution authority
> is [`Modernization.md`](Modernization.md), with per-item state in the phase status files.

---

## 0. Status


Everything the digest above compresses, phase by phase — the state of every item, what
phase 2 changed, the committed Octane run and what may be read out of it, the noise band,
the conformance gate and the patch handoff.


**Last updated 2026-08-22.** Snapshot of where the campaign stands; every claim is detailed in
the item's own section below, and nothing here is *closed* — see the acceptance protocol in §3.

| Phase | State |
|---|---|
| **0** — evidence | **Smoke infrastructure works; no RID is accepted.** The 2026-08-07 workflow produced 17/17 scores with three repetitions and a recorded spread, so it remains useful historical smoke/prioritization evidence. It does not establish candidate acceptance or a transferable “noise band.” Open work is 0-7/0-8 plus MOD-M1: immutable exact-row candidate/control comparison, all repetitions and resources, lane-specific A/A calibration, effective CPU/GC/JIT/PGO/R2R attestation, reproducible source/dependency/corpus identity, semantic-owner conformance, and durable raw evidence |
| **1** — compile-time | 1-2's mitigation ✅ (`43bc4230`); **1-2's real fix is now on all three recursing passes** — the validator and emitter (`StackGuard` had three defects and could not fire), and now `FastParser`, whose descent aborted the process at 25 000 nesting levels **in the default configuration** and now survives 90 000 at no measurable cost. 1-2's stated acceptance criterion **already passed before any work** — it measured size where the cause was nesting. **New: 1-4 ✅.** Measuring 1-1's premise found the phase's actual dominant cost, and it was not lazy compilation: the closure rewrite held a lambda's in-scope bindings in a `List` and asked it `Contains` per parameter reference, so **emission was quadratic in a scope's binding count** — 2 000 top-level declarations emitted in 13 865 ms against 2.5 ms of parse. A reference-keyed multiset (list-backed below 32 bindings) makes it linear: **28.5× on that shape, and 3.04× on Mandreel end-to-end**, ABBA-interleaved, six pairs. **1-1 is still open and its premise now has a number** — 92–96% of compile time is function bodies on the large real programs — but the measurement also **splits phase 1 in two and re-targets 1-1**: Mandreel was *wide*, not deep, and never was a 1-1 case, while jQuery at 96.5% deferrable is the whole of it. **1-1's emission half then landed without needing the capture mechanism at all**: every risk the item names is settled by the front end, so deferring *IL generation* to first invocation is the same prize with none of them — **jQuery 0.661×, Box2D 0.636×, PdfJS 0.689× on compile, allocation ~0.52× across the board, and 1.0009× steady state**. **Octane CodeLoad, the benchmark the item names, was run and passes: 94.6 → 104.0, 1.099×, 24 samples an arm, 93% pairwise dominance** — and it took 24 because the first three-sample pair and its reverse disagreed. That ratio also re-frames the item: compilation is only ~27% of what CodeLoad measures, not the whole of it. Two mistakes were caught by measuring: a stack handoff per deferred function took the suite from 3.5 to 20 minutes, and a thunk that *called* its resolve cost 1.0247% on call-heavy code until the warm path was written in IL. Typescript is 1.034× slower and unexplained. **The Mandreel suite was then run too, and it overturns the phase's headline target**: a 3.04× faster compile of `mandreel.js` moves Mandreel 0.993× and MandreelLatency 0.992× — Octane compiles that file at script load and times only the run function, so MandreelLatency measures execution pauses and belongs to phase 3. The saving is real and outside every score: suite wall clock **358.2 → 350.0 s**, non-overlapping. **What remains of 1-1 has now been measured before being built, and both halves of its premise hold.** The three-way split was only ever taken on synthetic declaration walls; taken on the real corpora it reads **parse 9.4–13.5%, expression-tree construction 33.6–63.9%, emission 25–57%** — tree construction is the single largest phase on five of six, parse and tree together are **43–75%** of compile, and the parse, the part an early-error rule forbids deferring, is a tenth of it. The population was never counted at all, and it is **84–99.7% of a script's functions never invoked once it has been evaluated** (jQuery 347 of 415, Mandreel 2 689 of 2 697). So the remaining half is over half the compile across a population that is almost entirely never needed. **It also corrects this item's own ceiling table**: `--compile-profile` stubs *outermost* bodies and jQuery has exactly one — the IIFE the library is written inside, 99.91% of its bytes — so the "96.5% of its compile in bodies that are never called" is everything except the parse, and that body is called first. **And measuring the phases found a repeat inside the half that already landed**: `LambdaRewriter.Rewrite` descends through nested lambdas, and `Relay` called it *again* per relayed site, so a lambda at depth *d* was walked *d+1* times and jQuery's whole tree was walked twice by a compile that emits almost nothing. Counted, the second walk finds nothing on any site — **0 of 415, 0 of 978, 0 of 1 574** — and a second counter says the repeat, left to run, creates **0 captures** the first walk had not. It is now skipped for any lambda a descending walk has already entered, with `RewriteRootOnly`'s pass deliberately marking nothing so async and generator bodies are unaffected. **All five pinned test262 manifests were run against it and every count is identical to §3.4's row, manifest by manifest** — 8 710 / 8 617 / 84 / 251 / 9, same files not just same totals. Whole compile **0.782× on jQuery and 0.867× on Typescript, six of six pairs each**; **Box2D does not separate** and its control arm's own spread is 55.6%, so the phase was measured directly instead — **its emission phase 0.549× and its whole compile 0.775×**, in the round where `--compile-phases`' parse control held. **1-1's remaining half now has a price rather than a lower bound** (`0101`): the free-name walk the item names as its own precondition is built and measured at **6.6–12.2%** of body-tree construction as one bottom-up pass — but at **up to 47.7%** written the obvious way, per-function and superlinear in nesting depth, which is what the recorded 5.4–9.9% *lower bound* was silent about. Mandreel, wide and not deep, is the control that goes the other way (7.8% → 8.8%). **And the population that could skip the mechanism entirely is now counted, which closes off the cheap way in** (`0102`): a site whose free names resolve to no enclosing binding needs no `Box[]` and is deferrable with what already exists, and that is **728 of 5 762 sites, 12.6%** — 39.7% on the flattest corpus and **7.4% on Mandreel**, worst exactly where the prize is largest. `Dynamic`, the direct-`eval` risk the item's text leads with, refuses **7 sites of 5 762, 0.1%**, the second time this item's stated risks have come back in an order the measurement reverses. **The reading that looked like an opening is refused by the counter built to test it**: Mandreel's 7 605 bound free names are only **165 function-owned**, because it is 1 364 top-level declarations and a top-level `var` is a global-object property per spec — but **`cellBacked` equals `bound` exactly on all six corpora, 15 118 of 15 118**, since this engine gives a program-level binding a CLR local in the program lambda like any other. *A spec-level fact about where a binding lives is not a fact about where the compiler puts it.* Writing the probe also found a hazard worth more than the count: the natural API for the question, `GetVariable`, **sets `RootScope.HasOuterFunctionCaptures` as a side effect**, a conjunct of item 4-2a's tiering gate — a probe built on it would have turned tiering *off* for every function it merely asked about. **The mechanism was then attempted and re-specified**: the item's stated raw material — *"`JSFunction` already carries `source` and already recompiles from it for tiering"* — **does not serve it**, because `RecompileForTiering` compiles `({source})` as a *fresh top-level script* with no enclosing scope, and the tiering gate admits a function only when there is no enclosing context to reproduce (`!HasOuterFunctionCaptures`, `!HasNestedFunctions`, no `with`, not an arrow, not a class). *That is the complement of the population a deferral serves.* The real obstacle, itemised: **14 `CreateFunction` parameters, 9 enclosing-scope reads and 5 pieces of `FastCompiler` instance state** must be reproduced at first call, each a silent miscompile if wrong. The recommendation is to **keep the enclosing scope alive rather than snapshot it**. **Not built**, because its gate is test262 over five manifests plus the item's four spec risks, and a half-verified version is a wrong answer in compiled code rather than a wrong number in an instrument. **The deferral mechanism is still not built and the item is still size L** — it is simply no longer blocked on an unpriced precondition, and no longer has a twelfth of itself available cheaply. **The layout itself was then attempted** (`0104`), and the checker built to validate it found **two obstacles the item's statement does not name**: `ClosureRepository` holds *two* populations — bindings **handed in** from an enclosing scope and the lambda's **own locals something nested captures** — so the deferral needs two derivations rather than one, and `outer` reads as "missing its own local" until they are told apart; and **every function is handed a `ScriptInfo_*` binding no source identifier names**, so the layout needs a reserved region for compiler-introduced captures. **The go/no-go is now answered: zero missed sites on 5 157 checked**, once three corrections were made — repeats recognised (Mandreel relays 1 336 of its 1 358 sites twice, with **0 disagreements**, so the repeat is pure duplication), undeferrable bodies excluded (7 sites, a defect in the checker: an empty prediction against a real capture set reports every capture as a miss), and **one real soundness defect in `0101`'s own code** — `FreeNameScan` bound a function DECLARATION's own name inside the function as well as in the enclosing scope, so **138 self-referential declarations across five corpora reported as capturing nothing**, and a deferred body built on that would have resolved its own name to a box that was not there. The derivation **over-approximates on 2 712 of 5 157 sites** — safe, and the cost side the mechanism must be measured against. **And the one shape the corpus does not contain is closed too**: a named function *expression*'s self-name, which the spec binds inside and this engine hands in as a cell anyway. The first attempt tested `Variable != null` — the field that binding leaves null on purpose, exposing itself through `EvalCaptureExpression` alone — which is `0097`'s rule a third time and the first time it has decided a **mechanism** rather than a measurement. Adding it unconditionally cost **126 sites** of precision, so `FreeNameScan` now gives the self-name **a scope of its own** and reports a reference to it exactly; **the gap closes at no precision cost, 2 445 exact either way**. *Two soundness defects fixed in total, both about a function's own name, both of which would have miscompiled a deferred body built on the layout as it stood*. **And `0104` settled MEMBERSHIP, not the layout — a distinction this document then lost for several sections, including in the patches that followed it** (`0112`). The item's obstacle is *an index*, that index is `Inputs.Count` at first encounter in the rewrite's descending walk, and the prediction was a `HashSet` derived from a `HashSet`: it has **no order**, so it could not answer the question even in principle. Asked properly — first-mention order recorded and compared against `repository.Inputs` — **14 605 sites, 4 461 exact, ZERO mismatched**, so *where the predicted set equals the handed-in set the order matches without exception*; the other **10 144 differ in SET**, which is the same over-approximation, and it carries a consequence the earlier framing did not: **an extra predicted binding shifts every later slot**, so the two numberings are different numberings rather than one with spare entries — safe only if the deferral **drives** the layout from the prediction instead of matching it. **The mechanism was then built for the half that had no evidence** (`0105`): a nested body **compiled a second time, after the enclosing compilation has finished, from the enclosing scope kept alive rather than snapshotted**. That is cheap because `FastFunctionScope` **is not pooled** — its `Dispose` only pops the stack, so a reference retains the whole `Parent` chain intact after the frame that built it returned — and `LinkedStack.Switch` already re-enters it; only the **five** `FastCompiler` fields not reachable from the scope are saved and restored, on the throwing path too. `CreateFunction` splits into a wrapper that decides retention and a `Core` both paths call, and the wrapper **refuses all fourteen context-carrying parameters** rather than trying to save them. **4 811 of 5 723 retained functions reproduce character-for-character, 84.1%** — Mandreel 100.0%, PdfJS 99.9%, Box2D 99.5%, jQuery 97.0%, Closure 96.6%, Typescript 49.6% — after canonicalising four families of compiler-generated numbering that are **counters over a compilation** and so differ by construction on a second pass (one of them item 4-2b's process-wide site counter, which *a genuine deferral cannot hit* because it compiles the body once). Each canonicalisation moved the total (45.5% → 48.9% → 83.2% → 84.1%) and **never once exposed a structural difference**, which is evidence about the residual **15.9% and not proof** — a printed-text diff answers only *“equal up to a renaming somebody thought of in advance”*, and settling it needs a structural walk that is not built. **Nothing is deferred yet, deliberately**: the eager path still compiles every body and the switch decides only whether a context is kept, because **the comparison is only possible while both trees exist** — once the eager one stops being produced there is nothing to check the deferred one against. **And the residual is now settled rather than described** (`0106`): erasing the counter-derived numbers instead of mapping them, **5 723 of 5 723 re-entered bodies agree with their eager tree in every token a counter did not produce, on every corpus, with none throwing** — no difference in a node, an operator, a constant or a shape. The 471 whose ordinals still differ classify **exactly two ways with nothing in “other”**: **460** are the site table's `-1`, because the check's second compilation drives item 4-2b's process-wide counter from 24 759 to **exactly its 65 536 cap** on Typescript, and **11** are the *eager* side re-using a site the re-entry allocated fresh — 4-2b's tier-2 rule working as designed. ***Both are properties of compiling the same body twice in one process, which a deferral by construction does not do.*** **The first finding was in the checker, not the mechanism**: the gensym families shared one ordinal table keyed on the bare number, so `Context3` and `#TempJSValue3` collided and desynchronised every ordinal after them — one table per family takes five of six corpora to 100.0% and the total from 84.1% to **91.8%**. Both equalities are pinned by a fixture that shows each reporting a difference, including **a site re-used on one side only**, which the strong one catches and the weak one cannot. **Still L**; what remains is suppressing the eager build and threading the deferred site through `Relay`, which `BLambdaExpression`'s **readonly `Body`** makes a change to the expression node rather than to the compiler |
| **2** — property access | **Every item landed or closed.** 2-0 ✅ 2-1 ✅ 2-2 ✅ 2-4 ✅ 2-7 ✅ 2-8 ✅ **2-9 ✅**; **2-3 and 2-5 closed on measurements**; 2-6 folded into 4-1. The phase's conformance gate is **satisfied**, and **its Octane exit criterion is now answered and splits: Richards is inside 200× at 183× (band 163–191) and DeltaBlue is not, at 576× (band 538–711)** — five repetitions per engine, same machine. **DeltaBlue is what phase 2 has left** (item **2-10**), and it is the suite 2-8 was written for. Its first pass found and fixed a real defect — `push` cost every array its shape permanently, **2 503 dictionary fallbacks → 0** — but that did **not** move DeltaBlue's read hit rate, which stays at **65.96% against Richards's 86.61%** and is the live lead. Decomposing those misses ruled out megamorphism (**0** megamorphic read sites) and, in passing, **found a live `class`-shaped instance of 2-0's defect**: `class C{}; new C()` published a global prototype invalidation **once per allocation** (2 002 for 2 000). **Fixed as 2-11** — the setter no longer invalidates when the chain did not actually change — and the effect on the real suites is far larger than the class case suggested, because the retirement was process-wide: **Richards's read hit rate 86.61% → 99.97%**, DeltaBlue's 65.96% → 69.45%, Box2D's 96.39% → 97.72%, with invalidations 37 → 10, 2 519 → 16 and 1 944 → 107. Then **2-12** found why the misses that remained could never heal: the cache's add path deduplicated on two keys while a hit checked six, so a stale entry was declined rather than refreshed and its site missed for the rest of the process — **77.7% of DeltaBlue's misses**. Refreshing in place takes **DeltaBlue's read hit rate to 93.16%** (65.96% before both fixes) and Box2D's to 98.83%. **DeltaBlue still fails the gate at 447× (399.5× on the committed run)**, but the cache is no longer the reason, and what remains is not property-cache-shaped. **2-13 then found where it is, using the column every committed run already carried and nobody had divided.** Asking Jint — a managed interpreter with no JIT, on the same runtime, in the same run — the same question separates *"DeltaBlue is hard for this engine"* from *"DeltaBlue is a suite V8 does unusually well on"*: DeltaBlue is **2.83× harder than Richards for Broiler and 2.56× harder for Jint**, so **only 1.10× of the gap is Broiler's own** — reproduced independently at 1.118× on the previous committed run. Closing the entire Broiler-specific residue takes DeltaBlue to **362×**, against a **200×** gate, so **the criterion is not reachable by removing a Broiler-specific deficiency at all**. Broiler is in fact *ahead* of Jint on DeltaBlue (0.77×) by nearly the margin it is ahead on Richards (0.69×), while the three suites where it is genuinely differentially behind are **MandreelLatency 54.3×, CodeLoad 37.8× and zlib 12.0×** — the front end and latency, which is where §1.1 always said the structural gap was. The obvious remaining explanation is falsified by a second control in the same table: **Crypto is 73.82% monomorphic against DeltaBlue's 77.10% and is Broiler's *best* suite against Jint at 0.46×**, so read polymorphism predicts the gap in neither direction. **2-10 closes as measured**, having produced three real defects and no explanation, and hands forward a question about the gate rather than a lead inside the suite. **And 2-7 and 2-9 have been re-taken over all fifteen suites** (§4.2a), which their own instrument could not previously complete — it aborted at the ninth on Mandreel and emitted nothing. **2-9 is corroborated**: 2 202 782 maps on the full corpus, the right side of its recorded 16.2 M → 2.5 M. **2-7 splits**: live memory still favours the shipped policy but by less than recorded (**0.644×** against **0.56×**), while its **allocated-bytes win changes sign** — geometric growth pays **33×** the node copying on suites it never saw, turning **0.82× into 1.044×**. The decision stands, since it was taken on live memory and live memory is still a third off; the allocated column should stop being quoted. **0-6's CI run has since confirmed the split independently — Richards 144.9×, DeltaBlue 460×** — so the phase's exit criterion is answered by two measurements on different machines that agree on which side of 200× each benchmark falls, rather than by one. Also outstanding: **2-9's ~20% compile-and-first-run cost still wants a follow-up — but not the one that was written.** Its losing-side hypothesis was measured against the control it never had (a *strict* function, which carries no Annex B deferred cells) and is **wrong**: every function materializes its trie **exactly once** whether strict or not, because the `prototype` install is withheld from shape-only storage by 2-8's DeltaBlue fix. "Stop materializing for a deferred cell" would have removed a materialization that already happened. The replacement candidate — split cache-visibility from shape-only storage — is specified and **not attempted**, since it is the code whose last regression broke DeltaBlue and it needs 0-6 |
| **3** — arithmetic | Started. **3-0 landed, both halves** — an indexed access boxed its index; a read now allocates **nothing at all** and a write loses ~32 B, on reference arrays as much as numeric ones. **3-1 measured before starting and re-specified**: it trades write allocation for read allocation 1:1, so its clean half is live memory. **3-3's parameter half landed** — and the measurement re-specified it: the gap was a per-call `JSVariable` **cell**, not a box, so a three-parameter call went **230.2 → 62.2 B**. **Probing that analysis before extending it found a wrong-answer bug shipped since P2-2** — two writes it could not see, one returning NaN and one aborting the process on valid JavaScript; fixed, at no measurable cost. **Its `let`/`const` half is now landed**, on the second attempt: the first was withdrawn on a miscompile, and re-built scoped to the *numeric* tier alone it reproduces the predicted number and not the defect — **`let` and `const` both 31.98 → 0.00 B/iter and 1 → 3 numeric locals, identical to the eligible `var` floor, with all twelve other `--local-alloc` rows byte-identical**, both arms from one tree. The recorded reproduction was re-run against it and is green, including under the switches that restore the pre-1-4 and pre-1-1 front end — so the withdrawn attempt's defect is **not explained**, only not reproduced; what the second attempt does differently is leave the JSValue tier closed to lexical names, since a TDZ and const-ness live in the cell that tier removes while the numeric gate proves both unobservable. **The block-scoped `var` then landed too, and 3-3 is complete**: the "definite-assignment analysis" it asked for is the function body's own dominance argument applied one level down — an unconditional block is *transparent* (entered whenever reached, exits only via `return`/`throw`), and any other block *confines* its declaration, which then needs every reference inside it. **`block-var` 31.98 → 0.00 B/iter and 1 → 3, one row moved and twelve byte-identical.** Two defects were caught on the way, neither shipped: a non-dominating declaration could mark a name readable and mask a read that would see `undefined`, and the fix for that over-corrected into rejecting a benign numeric re-declaration — caught by a pre-existing test written as "the guard against over-fixing". All four of the item's categories are now at the eligible floor except `parameter`, which cannot reach the numeric tier at all. 3-4 is a cost, not a task. **New: 3-5 ✅, and it measured the ceiling on this whole phase.** 4-5's probe found that the control loop every measurement here treats as a *floor* was itself paying a box per iteration — and the cause is not the parameter: `i` is a raw double, `n` is a `JSValue`, and `<` had a native form only when **both** sides were doubles, so the raw side was boxed to meet the generic operator. Unboxing the *other* side instead needs no entry guard and covers more (`i < a.length` is a property read, boxed for the same reason), and is sound because ToPrimitive of a Number is that Number. **33.77 → 10.03 ns and 32 → 0 B an iteration, 3.4× on its shape**; 33 semantics tests, every one of which also passes on the unmodified compiler. **On the Octane corpus it is invisible — 0.997× bytes, 0.995× time — and the reason is the number this phase never had: only 5.0% of scalar locals (203 of 4 029) reach the numeric tier at all.** The emission is not the problem (390 comparisons take the new form, 59% of those that could); what is on the other side is. That is the ceiling on 3-0, 3-3 and 3-5 alike, it is the same `CanScalarReplaceLocals` gate that bounds phase 4's tiering candidates, and widening it became **new item 3-6**. It also answers what 3-4 was told to wait for: the gap largely survives unboxed locals, because the unboxing reaches 5% of them. **3-6 has since done its count, and it retired its own design — and 3-5's explanation with it.** Of 2 695 hoisted names, `CanScalarReplaceLocals` — the gate 3-5 blamed — rejects **2, 0.1%**; the causes are *not proven numeric* (2 012, 74.7%) and *captured by a nested function* (478, 17.7%). Counted again inside the analysis, the first is not "most locals are not numbers" either: only **~170 names are never offered**, while the optimistic fixed point **offers 2 335 and drops 1 842 (78.9%)**, because something assigned to them comes from a parameter, a property read, an element or a call — none knowable statically. The two counts reconcile exactly, and the residue is **290 names the analysis proved numeric that the hoist site refused for being captured**. So the work splits: **3-7** gives a captured numeric local a raw-`double` cell (290 names, **203 → ~493, 2.4×**, entirely static), and **3-8** guards a local's numeric-ness at run time — which is **4-3b's in-method branch pointed at a representation**, and means *the largest single obstacle in phase 3 is shaped like phase 4*. Nothing was built for 3-6: its own text said to count first, and the count retired the design, for the fourth item running. **New: 3-7 ✅, and its premise was wrong in both directions.** The cell it asked for already existed — the expression compiler rewrites any CLR local a nested lambda references into a `Box<T>`, and **`Box<double>` *is* the shared cell**, so a captured numeric local costs *one* allocation where the `JSVariable` form costs two. The population, though, is **36× smaller than 3-6 said**: of its 478 captured names, **247 (51.7%) are named by a hoisted function declaration** and can never be widened, 223 more are not proven numeric, and the widening is worth **eight names, 224 → 232, 1.036×**. 3-6's 290 was **inferred rather than counted**, from *offered minus dropped* — and `Resolve` removes a third population between those two counters that had no counter at all, so the real reconciliation is **offered 2 295 = rejected 133 + dropped 1 916 + surviving 246**, and only **22** provably-numeric names are refused at the hoist site for any reason. Lifting the conjunct exposed **two wrong answers and one compile failure that had been hiding behind it**: a hoisted `function g(){ return s; }` can read `s` before `var s = 0` runs while sitting textually after it (`"0"` for `"undefined"`); a nested function's own parameter could mark the outer name initialized and mask a read that really sees `undefined` (`"0,5"` for `"undefined,5"`); and a function declaration stores a function object into the binding being typed, which no assignment-expression walk sees (`let f = 5; { function f(){} }` died on *"Assignment target Call is not supported"*). The first is fixed by a conjunct that is **not** behind the switch, because it is correctness. On its shape the result is exact — **63.97 → 0.01 B/iter, −112 B an activation, and shape ÷ control 7.19× → 1.0000×**, i.e. a captured numeric local now runs at the speed of the same loop with no closure at all — against an equally exact **losing side of +32 B and 1.111× when the value is read *through* the closure**. On the corpus it is **1.0001×**, invisible for the third item running, and the count says why: 2 439 names are not proven numeric and 247 are held by a hoisting rule. **Nothing left in phase 3 is a matter of loosening a conjunction** — and **3-8 then said the conjunctions were never where the prize was**. Two numbers, neither previously taken: **number boxing is 41.89% of everything the corpus allocates** (2.05 GB of 4.88 GB; 66.96% on NavierStokes, 55.16% on Crypto, 35.98% on Box2D, against 0.31% on DeltaBlue — a spread that buries the prize in any corpus average), and the **entire** numeric-local tier, measured for the first time against a build with it switched off, removes **311 187 boxes of 85.6 M — 0.36%, and 0.41% of total allocation**. So four "invisible on the corpus" readings were never evidence that the mechanism does not matter; they were evidence that eight more names do not. A box is minted by the **operator**, whose operands arrive boxed from array elements and object fields, so the local is one link carrying 0.36% of the traffic. Counting what defeats each proof says the same: of 1 916 drops, **894 (46.7%) are a property read and 570 (29.7%) a call's return — 76.4% values produced elsewhere** — against **47 (2.5%) parameters**, the category 3-3 deferred to phase 4 as the one that mattered. **3-8 as written should not be started; 3-1 and 3-2 move to the front of the phase.** Writing the classifier's tests also found the analysis offering a nested function's block-scoped `var`s to its *enclosing* function too, so each was dropped and counted once per level — no answer changes and every downstream figure is identical, but 3-7's `offered`/`rejected` pair is corrected from 2 521/359 to **2 295/133**. **New: 3-1 is started, and its first count re-specifies it off storage.** Nobody had measured what the generic arithmetic operators are *handed* — only what the compiler could prove about them. Counted: **73 817 515 of 73 818 646 invocations across the corpus arrive with both operands already Numbers, every one but 1 131**, and that population is **86.6% of all 85.2 M boxes**, while the compiler's `both are native` gate reaches **556 053, 0.75%** — and even that counts `+` alone, the only operator with a raw-double overload. *Compile-time provability reaches 0.75% of the arithmetic and run-time truth reaches 100.00% of it*, which is the sharpest statement this phase has of why six correct items are invisible. The consequence: the operator already gets two Numbers whatever they are stored in, so a typed backing store is not the precondition — what it cannot do is **hand one back**, because the consumer is a `JSValue`. The shared half is a **run-time-guarded specialization of an arithmetic tree**, boxing only the root, and the per-shape rows already say what it is worth (96 B and three boxes for `s = s + a[0] * 1.5`, of which two are intermediates). It also partly reverses 3-8's "do not start as written": 3-8 priced that guard at the **local** and was right that it is worth 0.36%; at the **operator** the same speculation reaches 86.6%. **And the shared half is now built and measured**: a guarded arithmetic tree — leaves evaluated once into temporaries, tested for Number, computed on raw doubles, boxed only at the root — removes **10 401 782 boxes of 85 249 783, 12.2% of everything the corpus allocates, from 862 compiled sites**, where 3-0, 3-3, 3-5, 3-7 and 3-1's bitwise half moved **0.36% between them**. Crypto 0.786× boxes and 0.583× generic invocations; Box2D 0.933×; Richards 0.787×. **Eligibility is bounded by evaluation order, not by the census**: a coercion runs between two leaf evaluations in a nested tree and is observable, so a leaf evaluated after the first internal node must be a literal or a proven-numeric local — which is why `s + a[0] * 1.5` qualifies and `(a[0] * 2) + p.v` is refused. **The gap to the 86.6% ceiling is itself the next finding**: NavierStokes loses 10.1% of its generic invocations and **1.8%** of its boxes, EarleyBoyer **99.7%** and **none**, so most of those two suites' boxes are minted somewhere that is not a binary arithmetic operator. **And the wall clock is measured too**, ABBA-interleaved, six pairs, with the corpus's own control: DeltaBlue and EarleyBoyer remove zero boxes between the arms and sit at **1.005× and 1.006×**, while the driver total is **0.981× on six of six pairs** and Crypto **0.912× on six of six**. No suite is slower. *12.2% of the corpus's allocation buys 1.9% of its execution time* — which bounds the rest of phase 3 the way 4-2b's 0.83% bounded phase 4. **And the gap to the ceiling has now been chased to its source, which is not where this item has been looking.** Giving the compiler's boxing conversion its own factory entry refutes the obvious reading — only **5.0%** of NavierStokes' requests are a raw double crossing into a `JSValue`, so a typed backing store cannot be why its boxes survive, while the conversion-heavy suite is **Crypto at 31.0%**, the one the guarded tree already served best. That first pass left **40.5% of the corpus's requests attributed to nothing**, and two counters took it to **1.0%**: `BitwiseXor` was the one generic binary operator the census never hooked, and the rest is **the unary operators, which no census had looked at**. **`++` and `--` are 30.9% of all boxing on the corpus, 51.6% on NavierStokes and 80.4% on EarleyBoyer** — more than the compiler conversion and the numeric literal together — and **exactly half of it is a `ToNumeric` copying a `JSNumber` into an equal `JSNumber`**, because it mints unconditionally to hand back the old value and a Number has no observable identity. **17 281 232 requests, 15.4% of the corpus's boxing, for a value the engine is already holding**. **That is now built, and it is nine lines.** Reuse is sound because a Number has no observable identity — the argument the small-integer cache has rested on since P2-2 — and the guard is `IsNumber`, not `!IsBigInt`, because a String or `null` still has to be coerced. Measured: **17 285 913 requests removed against a prediction of 17 281 232, the thing built matching the thing measured to 0.03%**, and **7 050 834 real allocations, 9.4%** — the gap between the two being the small-integer cache, which had already been answering Crypto's loop counters for free while NavierStokes' indices run past its bound. **NavierStokes loses 23.0% of its boxes and 0.906× of its time on six of six ABBA pairs**; with `0084` the corpus goes **85 255 034 → 67 798 222 boxes, 0.795×**. **And the run's sharpest reading is what did not move**: EarleyBoyer cut **50.0%** of its boxes — the largest proportional cut — for **1.002×**, because that is 82 000 boxes a second against NavierStokes' 4 240 000. *A share of a suite's own allocation forecasts nothing; the absolute rate forecasts everything*, which retires a habit this document has had since phase 3 opened. **And then the largest single result phase 3 has produced, from removing one eligibility rule rather than building a mechanism.** `0084` reached 12.2% against a census ceiling of 86.6% and never said which of its six conditions was refusing the rest; counted, **862 of 5 396 candidate arithmetic nodes specialize — 16.0%** — and the two rules that turn down the rest are one finding, not two: `+` is left-associative, so `a[0]+a[1]+a[2]+a[3]` refuses at the root as **order-unsafe** (1 762), refuses again at each left child, and its bottom node is then a single operator with **no saving to make** (2 718). *A chain of k operators produces k−1 order-unsafe rows and one no-saving row and specializes nothing.* **The sub-census then said the rule is not refusing what this phase assumed**: the blocking leaf is a property read **1 028** times and a computed element read **34** — 1.9% — so after six items written around array-resident data, the leaf that blocks the order rule is an object field, and Box2D alone contributes 984. **The fix is that nothing required the leaves to move.** Emitting each leaf at its own postorder position and putting the type test *where the coercion it stands in for would have run* preserves the reference order exactly, and the purity rule then has nothing left to protect — the same soundness argument `0084` makes, read from the other end. Each node carries a `bool`, a raw `double` and a `JSValue`, so a failure part-way up boxes the accumulated double once and lets the rest run generically, which the hoisting form cannot do. **Measured, one build, the switch the only difference: 53 353 957 → 6 626 052 generic invocations (0.124×) and 67 795 858 → 31 162 330 boxes — 36 633 528 removed, 54.0% of everything the corpus allocates**, against 12.2% for `0084`, 9.4% for `0086` and 0.36% for the five locals items combined; from the pre-`0084` baseline the corpus is **85 255 034 → 31 162 330, 0.366×**. OrderUnsafe goes **1 762 → 0** and NoSavingToMake **2 718 → 1 181** without that rule being touched, which is the chain-residue prediction coming out. The leaf cap had to be re-measured too — it was 8 and had *never fired*, because the order rule refused those trees first; at 16 it turns down 8 instead of 85 and the corpus loses a further **664 338 boxes, 2.1%**. **Wall clock, six ABBA pairs, counters off: driver 0.969× on six of six, NavierStokes 0.834× and Crypto 0.893× both on six of six**, against the two zero-box controls at **1.002× and 0.999×**. **And `0086`'s lesson predicts the row that looks wrong**: Box2D removes 51% of its own boxes and reads 1.003×, because that is 861 000 a second against NavierStokes' 6 500 000 — the two suites that move are exactly the two above ~6 M/s. *54.0% of the allocation buys 3.1% of the time*, which with `0084`'s 12.2% → 1.9% is the third reading of the same constant and the number to size the rest of the phase from **And the phase finally has a denominator.** Eight items priced in boxes, three of them measuring an allocation cut against wall clock and getting a sixth of the share back with no explanation — four lines of `GC.GetTotalPauseDuration()` say why. **Collection is 1.8–2.0% of the driver**, and of the 768 ms this item removed **54 ms was collection and 714 ms was the mutator** (pointer bump, zeroing, write barriers, cache traffic). *A box costs about fourteen times more to create than to collect here*, which turns §Non-goals' "the collector is not the problem" from an assertion into a measurement. Allocated bytes fall **4.00 → 2.92 GB**, corroborating the box counters from outside them. At **711 ms per GB** the **0.70 GB of number boxes still standing is worth ~495 ms, 2.6% of the driver** — so everything left in phase 3 is an XL bidding for under 2%, and the `++`/`--` step (33.2% of what remains, concentrated on the corpus's highest-rate boxer) should be counted before the typed store is built. **A sampling profiler was tried and does not help**: `dotnet-trace` inflates the driver ~29% and puts 28% of self time in `PollGCWorker`, its own rendezvous point, while compiled JavaScript lives in `DynamicMethod`s that do not symbolicate — 47.8% of the run lands on `InvokeFunction` and 2.4% on a named body. *The biggest frame in the profile is the profiler*, and 4-5's "blocked on a profiler" needs a different tool, not an afternoon **And the `++`/`--` count that re-specification asked for is taken, with a clean answer.** Of 17 282 144 steps: **Element 0, Property 0.3%, LocalCell 0.0%, LocalSlot 98.1%, Other 0** — *not one of the corpus's increments is on an array element*, so the step shares no mechanism with a typed store, and 98.1% are on a local or parameter the numeric analysis did not prove numeric. Weighted by each suite's request-to-allocation ratio that is **≈7.05 M real boxes, 22.6% of the 31.16 M the corpus still allocates**, and **6.76 M of it is NavierStokes' alone** — the corpus's highest-rate boxer, which is where §3.5's rate lesson says an allocation item pays. Reading the source names the cascade exactly: `++currentRow` in `lin_solve`, where `currentRow = j * rowSize` and `rowSize` is a `FluidField`-scope var written from a sibling closure, so the analysis cannot type it and 3-6's waterfall shows NavierStokes at **24 numeric locals of 141 hoisted names**. ***One closure variable the analysis will not type costs 6.76 M boxes.*** **This re-opens 3-8 on the terms `0083` already used once**: 3-8 priced a run-time guard at the *local* and measured the static tier at 0.36%, which was a measurement of what the mechanism catches; this measures what it **lets through**, and the two differ sixty-fold **Then scoped, by asking which RULE defeats the shape the traffic is in rather than which names were dropped.** Eight shapes, one per conjunct, with the update-target census as the oracle (a numeric local contributes no row, a slot contributes `LocalSlot`, a captured one `LocalCell`). Three suspects are innocent: a nested function **declaration** does not defeat the enclosing local, 3-7's hoisting rule produces a `LocalCell` — and NavierStokes has **9 461 760 `LocalSlot` steps against six `LocalCell`** — and passing the value in as an argument only trades `OtherName` for `Parameter`. **What is left is one conjunct: the analysis is per-function and will not type a name from outside it**, and the sharp fixture is that this holds *even when the enclosing name is already proven numeric* — a conclusion is not carried across a closure boundary. That splits the work: **3-9** (new, S–M, static) imports the enclosing scope's proven-numeric set, which is pure analysis reach with no soundness argument — but **does not reach NavierStokes**, whose root is held by 3-7's correctness rule, so its population must be counted before it is built; and **3-8a**, the run-time half, which is the only thing that reaches the cascade: where a local's *only* defeat is `OtherName`, one `IsNumber` test where the value enters decides the name for the whole function — 4-3b's in-method branch pointed at a representation, no longer general, and so no longer an XL. **Sized: 6.76 M of the 7.05 M real update boxes (96%), ≈0.16 GB, ≈115 ms — 0.6% of the driver.** *The best-founded item left in the phase, and still half a percent; phase 3 is not short of ideas, it is bounded by the exchange rate `0090` measured* **3-8a was then taken to the build and stopped, on its own instrument.** Two findings. **The mechanism is an XL after all**: narrowing *which names* it speculates on does not narrow *what has to change to hold one* — a speculative raw double is a double only while a flag holds, and every fast path (3-0's index, 3-5's comparison, the raw store, the native step, `ToNativeExpression`) keys off the single `NumericStorage` field, so each must become guard-aware or read a dead value, and a missed site is a **wrong answer**. *Size an item by the surface that changes, not by the population that uses it.* **And the population could not be measured**: the optimistic-minus-real instrument read **0 on all seven suites and on the shape it was built for**, so by §3.5 it is unusable — a counter never shown to read non-zero is a claim about the counter. One real defect fell out of it, and it is `0083`'s a second time: **the enable for a compile-time counter was placed among the run-time censuses, which switch on after the corpus has compiled.** Fixing that changed nothing, so the instrument was **reverted rather than shipped**. **What is kept is the A/B the item rests on**, reduced to one identifier: `var c = 2 * rowSize; c++` is a `LocalSlot` that boxes every step, `var c = 2 * 10; c++` is numeric and costs nothing — same nesting, same body, one name different, so the enclosing-scope read *is* the defeat **The count 3-8a was missing has now been taken, on the second attempt, and the discipline is the finding as much as the number**: the instrument was made to *discriminate on constructed shapes* before being pointed at the corpus — seven fixtures, of which the two that matter are negatives (a `Parameter`-defeated local and a never-offered `var a = []` must both stay out, or the count is a tally of every drop). **26 names across the corpus, 232 → 258 numeric locals, 1.11×** — and the distribution is the result: six suites gain one to three each while **NavierStokes gains fifteen and goes 24 → 39, 1.62×**, the largest single-suite widening this phase has produced, landing on exactly the suite that carries **9.46 M of the 16.95 M `LocalSlot` steps and 6.76 M of the 7.05 M real update boxes**. *The population and the traffic are concentrated in the same place, which is the condition every earlier phase-3 widening failed* — 3-7 moved 8 names at 1.036× and was worth 1.0001× because its eight were scattered where nothing hot lived. The prize is still `0090`'s ≈115 ms, **0.6% of the driver**: what changed is confidence, not size. **The count does not license the build** — the mechanism is still the XL above, since every fast path keys off `NumericStorage` and a speculative local is a double only while a flag holds; what it settles is that the work would have something to reach **The XL's storage half is then built, off by default, and it is a measured regression.** A speculative local is held as a raw `double`, a `bool` saying the double is live, and the ordinary `JSValue` slot, with `Expression` a conditional over the two — so **every existing read site is correct untouched** and a write through it is rejected loudly, the numeric tier's own safety argument reused. It deliberately does **not** get `NumericStorage`, the field five fast paths read as "this binding IS a double". Writes derive the flag from the slot branch-free; the `++`/`--` step branches on it and, while it holds, is a native double add that writes nothing back. **All three consumers that can take a raw double are now built too** — the guarded tree's leaf (`OrderedNode` already *is* a raw double, a flag and a fallback), the element read and the element write, over 3-0's `GetElementByNumber`/`SetElementByNumber`. **Each moved the number and none moved it enough: 1.021× storage alone, 1.017× with the tree leaf and the element read, 1.012× with the element write.** Only then was a counter added at the *read* — `JSNumber.CreateSpeculativeRead`, a fourth factory entry — and it closed the item in one line: **NavierStokes mints 393 705 boxes reading a speculative local and the whole item removes ≈5 300.** The 835 584 steps it genuinely takes off `Increment` mostly save no allocation at all, because they are `x[++i]` and the result is boxed to be an index either way. ***Closed as measured, not deferred***: the mechanism is correct and left in the tree behind a switch that defaults off, because what makes it lose is the read/write ratio of the code it targets — `currentRow` is read four ways and incremented once — which is a property of the workload, not of how many consumers the compiler grows. **Every premise the item was scoped on survived and the item still lost.** Building it also found a real defect the fixtures initially failed to catch — a tree leaf that offered a slot the raw half had left three increments stale, answering `"0!"` for `"3!"` — and the repair to the *test* discipline is §3.5's new rule: after writing a fixture for a new fast path, break the emitter deliberately and confirm it fails. **And the arm corrects the count's own reading**: the 15 names carry **835 584 of NavierStokes' 9.46 M steps, 8.8%**, not the whole of it — *the suite holding the names being the suite holding the traffic is not the claim that the names hold the traffic*. 1 191 + 4 571 + 2 103 tests green **on both settings**, with the four shape fixtures now Theories over the switch **3-9, the static half of the same split, is then closed by its own precondition count — and it cost one instrument and no mechanism.** Its population is **0 on all seven suites**, against 3-8a's 26 reported from the same call site in the same run, so the zero is the corpus rather than the harness. A second counter says *why*, because a single zero cannot separate "nested functions never read an enclosing numeric local" from "they read them constantly and never anywhere typable": the enclosing scope chain answers *"that name is already a raw double"* **0 times on the whole corpus**. The reads do not exist. That reconciles with item 3-7 exactly — 3-9 can only import from a name that is both proven numeric AND still a raw double despite being captured, which is 3-7's population of **eight names**, and not one of the eight is read from an assignment inside the function that captures it. **The instrument was made to discriminate first** — nine constructed fixtures, three reading non-zero, each re-checked by disabling the probe and confirming it fails, which is `0096`'s own new §3.5 rule applied to the thing that decides this item. It also settles a design question by flipping two fixtures: the probe must ask what the compiler **built** (`NumericStorage`) and not what the analysis **proved**, or a name 3-7 leaves in a cell for correctness reads as a win. *A good mechanism with nothing to point it at* — no guard, no fallback, 3-8a's failure mode structurally absent — declined because building it would buy an analysis pass and a scope-chain probe per compiled function for zero names. **And every number in this row has a denominator it never stated** (§4.2a): `SpecializingTierMetrics`, which produces all of them, ran **7 of Octane's 15 suites**. Widened, the corpus is **164.1 M boxing requests and 90.6 M boxes against 52.0 M and 31.4 M, 12.93 GB against 3.13 GB** — **65.4% of the boxes, 75.8% of the bytes and 80.3% of the execution are outside the suites this phase has been counting** — and **Gameboy alone allocates 41.3 M boxes, 1.32× the whole measured corpus**, having never been in a census. The seven reproduce their recorded 31.16 M, so it is the same instrument over more suites. **What survives is `0090`'s denominator, with one qualification** — collection is 1.80% of the widened corpus against 2.29% of the seven, so the GC non-goal stands; but measured counters-off the spread across suites is **0.7% to 10.3%**, and the top of it is **Splay**, the suite Octane includes to stress the collector and the one no census had run. The conclusion holds everywhere measured; the single exchange rate should stop being quoted. **What does not survive is the ranking**: "everything left in phase 3 is an XL bidding for under 2%" sized the remainder against a driver a fifth the size of the real one. **And attributing the widened corpus partly reverses 3-1's move off storage.** The census's own partition is stable in share — conversion 47.4% → **42.2%** — while the count goes **24.6 M → 69.3 M**, which is `0086`'s rate lesson arriving from the other direction: *a share that holds while its population triples is not a robust share, it is an unread one*. **64.4% of the corpus's conversions are outside the seven, and Gameboy alone mints 26.9 M — more than all seven together — at 51.0% of its own requests.** A conversion is the compiler boxing a raw `double` to cross into a `JSValue`, the one source a typed backing store removes without further operator work, and Gameboy is a `Uint8Array` memory image with register arrays: exactly the shape 3-1 was written for. **3-1's storage half should be re-opened as unmeasured rather than left recorded as refuted** — the evidence that retired it ("5.0% of NavierStokes' requests") came from a corpus that excluded the item's best case. It is not a claim the store is worth building: that needs a wall-clock A/B nobody has run, and Gameboy's 1.96 GB over 23.8 s is a lower *rate* than NavierStokes' 0.49 GB over 2.0 s  **And the conversion counter was then split by the site that mints it, which retires a suspicion and re-points the phase** (`0103`): over **all fifteen suites** — the first census here to reach every one — **61.79% of the corpus's 69.3 M conversions are the guarded tree's ROOT box**, and the generic fallback arm an interior node can take is **226 of 69.3 M**, zero on eleven suites. *The tree is not leaking; what is left is the one box per evaluation the design keeps*, 92.5% of NavierStokes' conversions and 98.5% of Splay's. So the remaining phase-3 question is neither the operators nor the store but **what the root box's CONSUMER is** — the measurement the item now hands forward. And **Gameboy, the suite §4.2a re-opened the storage half on, belongs to the locals half instead**: its 26.9 M conversions are 47.3% root and **28.7% the `++`/`--` step (7 723 245)**, larger than any other suite's entire conversion count, which is item 3-8's population priced over a corpus that never contained the suite where it dominates.  **And the root's consumer is now counted, which answers the question `0103` left and retires both storage items as the answer** (`0105`): of 42 849 742 root boxes, **44.36% are consumed by a LOCAL**, 17.91% by an element, 13.14% by a property, 4.43% by a call argument, 0.40% by a return, with 19.77% unattributed and reported as such. *A proven-numeric local already has a raw `double` home (item 3-3), so a root landing there is one the existing numeric tier failed to type* — not one waiting for a new representation. **The typed store's entire ceiling on root boxes is the element row, 11.07% of the corpus's conversions**, against `0113`'s finding that the store is an allocation wash at a 3.34 read/write ratio; and NavierStokes, the one suite it genuinely addresses (59.2% element), is also the suite with the worst ratio for it (5.26). Crypto sends **81.8%** of its roots to a local, Typescript 74.0% to a property, Splay 98.5% to a call argument. **Three independent counts — this, 3-8's 98.1% `LocalSlot` `++`/`--` step, and 3-8a's read/write closure — now point at the numeric-local tier as phase 3's remainder.**  **And the tier's refusals are now weighted by the boxes they cost, which is the currency item 3-6 did not use** (`0106`). First the cheap explanation was tested and refuted: the destinations are not locals the tier had already accepted — that seam is **36 boxes of 18.6 M** — so every one is a local the tier *refused*. Of 19 005 731 such boxes: **`DroppedCandidate` 38.41%**, **`ElementRead` 36.35%**, `Unknown` 12.78% (a gap in the instrument, not a cause), `PropertyRead` 4.87%, `Parameter` 4.08%. **The largest row has no independent cause to fix** — a cascade is refused only because another refused name reaches it, and NavierStokes at 96.8% cascade is this document's own `rowSize` finding arriving from a second direction. **The largest independent cause is `ElementRead`**, 58.1% of Crypto's: *the guarded tree already proves `a[i]` numeric at run time and the local's analysis refuses it statically, so the box is the cost of the two mechanisms not sharing a conclusion.* The obvious fix is item **3-8a**, built and closed as a regression, so what this hands forward is the read/write ratio for THIS population, counted before anything is built.  **The read/write ratio that would decide it was then attempted and is not obtainable this way**: wrapping a refused local's read in a counter took the population from 18 657 518 roots to 3 147 314 (0.169×, Gameboy zero), and the cause is not bias but a **crash** — `variable.Expression` is also the assignment target, so `x++` compiled to an assignment onto a method call and the IL backend refused it. *Reverted; nothing of it is in the pin.* The write side stands, the read side needs a hook that is not the node the assignment path writes through, and **item 3-1's remaining question is open with a named obstacle rather than a plan**.  **The FREE half of that read side is then counted from the one safe position** (`0107`, the guarded tree's leaf save — a value, never an assignment target, emitted after the tree commits; verified non-perturbing at **1.000 on every suite**, against the reverted attempt's 0.169×). Of **16 426 708 boxed writes there are 34 645 485 free leaf reads, 2.11 per write** — `Parameter` 12.89, `CallResult` 13.24, `ElementRead` 2.14, `DroppedCandidate` 0.72 — and within `ElementRead` alone Gameboy reads 31.06 per write against Box2D's 0.05. **It does not decide the item**: break-even is set by the BOXING reads, which still have no safe hook, and *item 3-8a lost with a free-read population that looked just as favourable* (393 705 boxes minted at the read against ≈5 300 removed). What is established is a bound — total reads ≥ 34.6 M — and a ranking.  **And the COST side is then counted at the consumers, which the read could not carry** (`0108`, verified non-perturbing at 1.000 first). Against 16 426 721 boxed writes the instrumented consumers see **5 768 745 boxing reads, 0.35** — and it splits: **`CallResult` 9.41, `NeverOffered` 3.72 and `PropertyRead` 1.83 are REFUTED at the bound**, while the two causes carrying **14.2 M of the 16.4 M** come in at **`ElementRead` 0.04 and `DroppedCandidate` 0.03**, so the un-instrumented consumers would have to supply 25× every read counted to break even. Crypto 0.02, Gameboy 0.04, PdfJS 0.00, **NavierStokes 0.00 — its refused locals are read only inside trees** — against Typescript 11.18 and EarleyBoyer 35.35. ***This is the first affirmative evidence phase 3 has produced for widening the numeric tier***, bounded above at 14.2 M boxes (8.4% of the corpus's requests), still a LOWER bound on cost, and still owed a wall-clock A/B before anything is built.  **The widening was then BUILT for those two causes and measured as a regression** (`0109`): boxing requests **1.061×** and allocations **1.039×**, Gameboy alone 1.123×. **The counters say the cause is not the ratio**: roots consumed by a refused local move 18 657 804 → **18 656 936** — *868 of 18.7 M writes removed* — while speculative-read boxes go 0 → **7 692 133**. The saving was never collected. **The assignment path tests `NumericStorage`, which a speculative local does not have**, so the tree still boxes its root and `AssignToSpeculativeVariable` unboxes it again: 3-8a has raw arms for the tree's leaf, the element read and the element write, and **none for the tree's ROOT**, which is the one site the saving lives at. *A ratio prices an outcome; it does not establish that a mechanism reaches it.* Off by default, kept as the arm a store-path change would be tested against.  **The missing raw arm was then built and the item is refuted on it** (`0110`). The arm works — roots into a refused local **18 657 804 → 16 225 570, 0.870×, 2 431 366 boxes removed** against `0109`'s 868 — and the corpus is **still 1.041× on requests and 1.039× on allocations**, because **the cost did not move: speculative-read boxes are 7 692 133 in both arms**. Saving 2.4 M, cost 7.7 M. Two structural findings: **`0108`'s consumer-side bound was 25× low because it counted the wrong sites** — the cost is a box at the local's own read expression, the site `0107` proved has no safe hook — and **the saving was never 14.2 M**, because a refusal census attributes a name to its FIRST cause and removing that cause admits it only if it was the ONLY blocker. Off by default and staying off.  **A fourth population then had its read cost counted FIRST, per `0110`'s method, and was refused on one measurement** (`0111`): parameters — the most tree-resident cause at 12.89 free reads per write — cost **417 582 speculative-read boxes for a NEGATIVE saving** (roots 18 657 804 → 18 962 176, because more speculative names make more trees eligible and each mints a root), corpus 1.003× / 1.006×. **NavierStokes mints exactly 393 705 — item 3-8a's recorded failure number to the digit — so on the suite that decides these items the fourth population is the third one wearing a different refusal.** `0108`'s refusals of `PropertyRead`, `CallResult` and `NeverOffered` are **void rather than confirmed** (`0110`), each now one flag and one run from an answer. *After four attempts the cost has been the same quantity every time; the dual representation should be treated as refuted as a general mechanism on this corpus.*  **The remaining populations were then tested and all refused** (`0112`): `PropertyRead` costs 3 828 813 speculative reads for 72 980 roots (**52.5×**, 1.030/1.045) and `CallResult` 913 011 for 431 131 (**2.12×**, 1.004/1.006). **Four of four populations cost more than they save.** *And `0108`'s bound was not merely low but INVERTED* — it ranked `CallResult` 9.41 against `PropertyRead` 1.83, and the truth is the reverse by twenty-five times, so a bound taken at the wrong sites cannot even be used to rank. **NavierStokes mints exactly 393 705 under `CallResult`, under `Parameter`, and under item 3-8a — three populations, one number**, the same handful of names reached three ways. `NeverOffered` is structurally not testable this way (its declaration is non-numeric, so no assumption admits it) and its 241 567-write ceiling is argued rather than measured. ***Item 3-1's dual-representation line is closed.*** |
| **4** — tiering | Started. **4-3a is landed and it found a real hazard**: restart is only sound if the body is not suspendable, and nothing said so — the property held by two unrelated accidents (the `EnableTiering` call sitting inside the ordinary-function `else` branch, and the tiering gate borrowing `CanScalarReplaceLocals`, which refuses generators for its own reasons). Defeating both, a legal `async function` whose body matches the planner's shape returns **`number` instead of a Promise** from its second call on — measured, not argued. One condition at the decision point fixes it, and 16 tests pin all three conditions. **4-3b is landed too**: `SpeculationBuilder.Guarded` compiles the specialized and generic forms into one method so a failed guard is a *branch*, with the subject evaluated exactly once (the hand-rolled spelling fails 12 of its 15 tests) and per-site poisoning after four misses. **It emits no JavaScript-level speculation, and that is structural rather than scope-trimming** — a guard needs a shape or a callee to speculate on and a tier-1 method knows neither, so the branch only has meaning inside a tier-2 recompile, which is 4-2. The mechanism lands before its consumer because it has to. **4-3's design is written** — and it re-specifies the item: this engine has no interpreter frame to reconstruct, so V8-style deopt has no counterpart here. Splits into 4-3a (state and enforce the restart contract the pilot already runs, S) and 4-3b (a generic fallback branch inside the specialized method, M–L), which gates 4-4 rather than all of phase 4. **4-1 has landed and it settles the phase's premise — over seven suites, and §4.2a has since re-taken it over twelve.** Per-site feedback now *retains* what the inline caches only observe — receiver shapes at reads, callee identities at calls — and over **seven** Octane suites, weighted by executed operations, **93.54% of 37.9 M property reads and 96.70% of 4.24 M calls happen at a site that only ever saw one shape or one callee**. **Widened to every suite that runs, the same instrument reads 80.11% of 307.9 M reads and 86.35% of 52.9 M calls** — 87.7% of the corpus's reads were outside the seven, and the two suites that move the number (Gameboy at **34.60%** monomorphic with 1 282 polymorphic sites, Splay at **10.15%**) had never been counted. 80% is still most of the work, so 4-2b is not retired; what is retired is the idea that 93.5% described the engine rather than seven suites chosen for being call-heavy. 4-2 and 4-4 are an XL each and both are worth their cost only in proportion to that number; nothing in the engine could report it until now, and it comes out high. **Megamorphism is essentially absent** — 18 sites in total, five of seven suites have none — corroborating 2-10's independent finding of zero megamorphic read sites; the fallback path 4-3b must still be correct, it just will not be hot. **DeltaBlue is the worst read case at 77.10%**, with 43 polymorphic read sites against Richards's 1, which is a lead on the suite still outside phase 2's gate. Collection is off by default, costs nothing on the call path *by construction*, and the item's third signal — numeric-vs-generic per site — is deliberately left uncollected rather than half-built. **4-2 has now landed too, and it splits the same way 4-3 did.** Measuring the branch it was told to replace found that it does **not** "recompile the same code the same way": a fresh top-level compilation builds a *second* function object and loses inherited strictness, so **DeltaBlue died on the shipping tier-2 hook** — `TypeError: Cannot get property call of undefined`, 0 of 1 benchmarks against 1 of 1 untiered, because its constructors read `X.superConstructor` off their own name and got the copy. Four of thirteen probes disagreed between tiered and untiered. **4-2a** states the recompile contract, refuses the identity cases and repairs strictness, at a cost of ~5% of promotions. **4-2b** then makes tier-2 re-emit tier-1's *own* site indices — which carries the warm caches across promotion and makes 4-1's feedback addressable — and emits a monomorphic read as a shape guard plus a direct slot load through 4-3b's in-method branch, whose **first JavaScript-level consumer** this is. **44.74% of the corpus's 37.9 M executed reads leave the inline-cache path** (counted exactly: cache misses are identical, so they were removed, not converted), carried by **1 130 sites**, with 156 guard misses and 30 poisoned — the monomorphism holds past the promotion point. **Each such read is 0.818× (46.83 → 37.12 ns, six pairs, 0.778–0.879), and the suite wall clock does not move: 0.9947 against a feedback-on control.** That is arithmetic, not failure — 16.9 M × 9.7 ns is 164 ms of a 19.7 s run, **0.83%**, under a ±2% floor. It also bounds the phase: the whole read path is ≤ ~9% of Octane's execution time here and the whole call path ≤ ~5.5%, so **the two paths phase 4 is built around are together at most ~15%**, which 4-4 should know before it starts. **The item's arithmetic half has now been priced, and it is refused by its own arithmetic** (`0107`, §4-2c): defined against what 3-1 already proves, the population is that item's `NoSavingToMake` refusal — **26.22% of candidate nodes**, refused by a condition that reasons about *allocation* and says nothing about time — and the census the item rests on turns out to be another seven-suite figure, **100.00% → 92.10% over twelve**, with the **spread the finding rather than the total** (Splay **0.46%**, Box2D 100.00%), which is the first evidence the item has ever had for its own per-site thesis. Priced three-armed and round-robin — after a first harness run in blocks reported spreads of **161–470%** and a `multiply-generic` **2.5× too slow**, an instrument producing the effect rather than finding it — it is **0.704× on multiply (11/12 rounds, 6.39 ns saved)**, 0.906× on add whose **miss is 18.567×** because the failure is a string concatenation rather than a coercion, and **best case 124 ms of a 104 620 ms driver: 0.119%**, net negative at the add rate. **7× smaller than 4-2b, which this document already called below the noise floor.** The relational lead it points at is **closed rather than left as a guess**, which needed a counter nobody had built — 3-1's speculation excludes the relational operators and 3-5 needs one side already unboxed, so a comparison over two boxed operands is reached by no fast path at all: **23 986 595 comparisons, 99.85% both-Numbers, worth 0.022%**. **So the whole generic binary-operator surface — 50.1 M invocations — is 497 ms, 0.475% of the corpus if removed entirely**, which beside 4-2b's read path ≤ ~9% and call path ≤ ~5.5% is the third side of the same box: *the operators are not where this engine's time goes.* **4-4's premise has now been measured too, and it re-specifies the item before any of it was built.** Counting at the call rather than through 4-1's compile-time gate, the corpus makes **6 194 758** invocations, and **37% of them are to native builtins** — an emitted call site with no body to inline, which any ceiling counting them inflates by more than a third. That correction came out of writing the counter's tests, which also found that a builtin runs a JavaScript callback on a *different and much shorter* entry (`InvokeCallback`, one `using` scope against five); callback invocations turn out to be **zero** on all seven suites, so an earlier guess that they explained the gap to 4-1's figure was wrong and is recorded rather than quietly deleted. Of the 3 902 620 calls with a JavaScript callee, **64.0% are made from a promoted function** — inlining's whole surface, and an upper bound. Against a hand-inlined control in one run set, **inlining saves 149 ns a call (0.37×)** — so the ceiling is **372 ms of a 19 694 ms driver, 1.89%**. **That ceiling was a seven-suite number too, and widened it is LARGER**: the same instrument reaches all fifteen since `0103`, the seven **reproduce to within 0.0002%** (6 194 744 invocations, 2 496 730 inlinable, 1.89%), and over **the twelve suites that run** it is **59 372 476 invocations, 40 523 273 with a JavaScript callee, and 4-4's ceiling is 2.43%** — with the fixed call prologue, 4-5's surface, at **8.06%** rather than 4.48%. **The two halves move in opposite directions and that is the finding**: the *population* share falls (*"from a promoted caller"* **64.0% → 42.1%**, because the suites nobody had counted hardly promote — **PdfJS 1.1% of 949 790 calls**, Splay 15.5%, five suites at 0%, **Typescript at 38.8% while being 77% of the corpus's JavaScript calls**) while the *time* share rises, since the seven are **10.4% of the corpus's calls against 18.8% of its time** — call-poor, the opposite of how they were chosen. Since a guard needs an observation and a tier-1 method has none, 4-2a's promotion gate is the ceiling on 4-4's ceiling. **The re-specification survives for a different reason than it was written for**: 4-5 beats 4-4 by **3.3×** (8.06% against 2.43%) rather than 2.4×, so the ranking is firmer — but **4-4's ceiling did not shrink into irrelevance, it grew to about three times 4-2b's landed 0.83%**, and the honest statement is *4-4 is not too small to matter, it is too small to beat 4-5*. **A first reading of this divided by all fifteen suites and reported 0.65%** — Mandreel spends 286 728 ms hitting the stack guard while making 1 488 of 59.7 M calls, so it is 72% of a fifteen-suite wall clock and nothing of what is counted; §4.2a's own convention already said the widened headlines are over twelve. Cross-checked against a **counters-off** driver at 110 620 ms against 104 620 ms (0.946×, per-suite 0.75×–1.11×), so the counters are inside run-to-run variance. Inlining is **expressible** here, unlike 4-3's deopt: labels and goto exist, a real function scope handles the callee's names, and 4-1 retains the callee — so the blocker is value, plus one semantics decision nothing can undo (an inlined callee has no frame, so it leaves `Error().stack`, and this engine has nothing to reconstruct one from). **New item 4-5 beats it**: a call costs **142 ns before it carries any argument**, plus 17.1 ns each, so ~90% of the overhead is fixed — and reducing that reaches all 6.19 M calls with no speculation, no guard and no fallback. The same probe also bounds the phase: reads are **9.16%** of execution time and the fixed call prologue **4.47%** (paid by every invocation, native callee included), so **the two paths phases 2 and 4 are built around are together under 14%** — **and re-taken over the twelve suites that run both halves RISE, to 14.01% and 8.06%, so the pair is 22.07%**, still a minority and a materially larger one — while the arithmetic-only control loop is 16.98 ns an iteration, which points at **3-4**, not at phase 4. **4-5's ablation has since run, and it falsifies most of its own premise**: five nested `using` scopes cost **0.011 ns**, EH 0.73 ns and dispatch 0.68 ns, so the prologue is not where a call's cost is. The one real cost is an **`AsyncLocal<bool>` read at 7.0 ns against a `[ThreadStatic]` at 0.31 ns** — read on every call, and documented in `JSEngine` as *"reads are cheap"*, **wrong by 24×**. Mirrored into a ThreadStatic with the AsyncLocal kept as the carrier (0.22% of the corpus; 9 tests, which also pass on the unmodified engine). **~85% of a call's fixed cost looked unattributable from outside the engine** — and it was the *replicas* that could not see it, not the engine (`0108`). 4-4 had already named the measurement that answers it and nobody had taken it: **`JSFunction.InvokeCallback` is the engine's own short path**, same `EnterRealm`, same delegate selection, same `this` coercion, one `using` scope instead of five and none of the executing-function or legacy-caller bookkeeping — a natural ablation that has been shipping the whole time. On the same callee with the same prebuilt arguments, **`InvokeFunction` is 114.60 ns and `InvokeCallback` 64.43 ns: a 50.18 ns difference, 0.562×, so 44% of a call entry is bookkeeping rather than ~10 ns of it**, and the gap is a lower bound (the short arm also pays a reflection delegate dispatch and a tail-call test). **That is 2 979 ms of a 104 620 ms driver — 2.85% of the corpus, against 4-4's whole ceiling of 2.43%** — with no speculation, no guard, no tier and no fallback path. **It is a budget rather than a saving**: the bookkeeping serves `f.caller`, strict mode across a call, realms and `with`, and some of it is required — but it localises the missing 85% to **eight named operations** between the two entries, with `EnterRealm` excluded because it is in *both*, which the replica pass could not have shown. **The ablation of those eight then closed the sum to within 0.5 ns** (`0109`), and it needed no engine change either: an **arrow** through the long entry costs **71.79 ns against an ordinary sloppy function's 116.19**, only **3.81 ns** more than the short entry, because `AddLegacyCallerAndArguments` is emitted for ordinary function declarations and expressions and not for arrows or methods. So **44.40 ns of the 48.21 — 92% — is the Annex B `caller`/`arguments` frame**, `PushLegacyFrame` copying the `Arguments` struct twice and popping it back **on every call to every ordinary non-strict function**; the executing-function save/set/restore is 2.14 ns, the `Current` cast and `Options.ScriptHostMode` read 2.14 ns, and the two `with`-scope pushes — the most suspicious-looking lines in the method — are **free**. **2-9 recorded that these cells cost something at function *creation*; they also cost 44 ns per *call*, and nothing had measured it.** Bounded to calls that can pay it, that is **≤1.65% of the corpus**, and *the largest single attributable cost in this engine's call path is a legacy web-reality feature no benchmark uses.* **And the strict control the ablation started from found something else, larger per call and rarer**: a strict callee entered from sloppy code costs **102.87 ns and 224 B more**, because `StrictModeScope` writes the strict-mode `AsyncLocal` on entry and again on exit — 4-5 fixed the *read* side and left the write side resting on *"the write only on a transition, so the common case is…"*, an argument about frequency nothing could check. **Counted, 2 813 191 of 59 372 513 calls cross — 4.74%, worth 0.266%** — and **nine of twelve suites never cross at all while PdfJS crosses on 52.65% of its calls**, so the claim is right about the corpus and false about one suite in it. **And that frame is now counted rather than bounded** (`0110`): **35 715 923 of 59 372 494 calls push one — 60.16% of all calls, 88.14% of the JavaScript-callee ones, 1.46% of the corpus** — with **Richards at 100.00%**, Typescript 75.44%, Box2D 69.87%, and **PdfJS 0.59% and Gameboy 0.02%** because *the two suites that escape it are the two that are strict*. **A program written in strict mode does not pay this at all.** **4-5 is no longer "make the prologue cheaper"; it is "make the legacy frame lazy" — and that fix has since been priced and refused** (`0111`): moving the frame to a thread-local stack is **0.730×, saving 6.19 ns — 0.20% of the corpus for an M–L**, because one 56-byte `Arguments` copy alone is 8.19 ns and *the cost is the copying, not where it lands*. **Removing the frame outright is 1.46%, seven times that**, and it is the one whose gate cannot be made sound — `caller` and `arguments` are reachable through a computed member access. **And this item's own control has since been fixed by the item it proposed**: 3-5 landed, so `for (var i = 0; i < n; i++)` reads **7.67 ns and 0 B** against the 33.77 ns and 32 B recorded here — **the box per iteration is gone** — which retires this item's closing inference that the control points at 3-4 rather than at phase 4: a call is now **~19× a loop iteration** where it was ~4×. **And the control every probe here has used turned out not to be a floor**: the same counted loop with a *literal* bound instead of a parameter one ran at **8.36 ns and 0 B an iteration** against **33.77 ns and 32 B** — same answer, **4.0× and a box per iteration**, because a parameter cannot reach the numeric tier (3-3's one acknowledged gap) so `i < n` boxes. `for (var i = 0; i < n; i++)` is the corpus's commonest shape; that became **item 3-5**, which **landed and worked — the probe now reads 7.67 ns and 0 B, and every figure quoted against that control needs to say which side of 3-5 it was taken on**  **4-5's floor then moved by the one lever `0111` pointed at** (`0104`): `PushLegacyFrame` returned a 72-byte frame by value and the caller assigned it, so an `out` parameter removes two copies with no semantic change — **`InvokeFunction` 117.32 → 115.50 ns on 9 of 12 interleaved ABBA pairs, 0.100% of the corpus**. Kept because it is strictly less work for identical semantics, *not* because 9 of 12 establishes 0.100%. **The prediction is the finding**: `0111` priced a struct copy at 8.19 ns and removing two bought 1.83, so the JIT had already elided most of the return-by-value traffic — *a struct copy in the source is not a struct copy in the code*. The frame itself is still **1.46%** and still gated on the soundness question nobody has answered. |
| **5** — regex | **Gate satisfied, and it overturns the phase.** `Matcher.cs` is not the default engine — `JSRegExp` routes only semantic-gap patterns to it, and Octane's corpus has no look-behind and no `u` flag, so it barely runs. The engine that does serve them is `System.Text.RegularExpressions` built **interpreted**; `RegexOptions.Compiled` is worth ~2× on six of seven real Octane patterns and a stable **4.3× against** on the seventh — a *trim* — so a use-count policy is ruled out. Largest regex cost measured was neither: `replace` with a global flag allocated **42 859 B per match**, because an Annex B legacy static copied the subject on every successful match — **fixed, 0.048x the bytes and 0.30x the time**. Decomposing what was left **per call** then found a single-match `replace` paying two full UTF-16 copies of the subject through a `StringBuilder`; concatenating three spans instead is **4.020 → 2.020 B per subject character, exactly the predicted halving**, and **the identical defect in `String.prototype.replace`'s string-`searchValue` builtin was found by reading the neighbouring code and fixed with it**. The global case's retained result list then landed too — **2 032.8 → 478.3 B per match**, dead linear on both sides, by streaming when the receiver's `exec` is the pristine intrinsic and the replacement is a `$`-free string. **Every follow-up this phase named is now closed, item 2 included.** The `Compiled` policy is **built** as a per-pattern **race** — at a pattern's thousandth match the engine times both forms on the subject in hand and keeps the winner, with no predicate anywhere — and measuring it produced two findings and no speed-up. **The 4.3× trim regression the "no policy" decision rested on does not reproduce**: re-run unchanged, all three losing rows change sign, and the shape in question promotes at **5.27×** on Octane's own subject, so a rule compiled in from the old table would now be wrong on every pattern it named. **And the race is worth 1.010× on 3 of 6 pairs** on the RegExp suite — the only one of fifteen it reaches, since eleven build no regex at all — because `--regex-call-envelope` says **the matcher is 4.6–6.5% of what `re.test` costs and 8.7–9.4% of what `re.exec` allocates**: a JS regex operation pays a fixed **~2.4 µs and ~2 431 B** that does not move when the subject grows 18.8×. **Shipped switchable, default off**, and the phase's real remaining target is that envelope, not the matcher |

**What phase 2 changed, measured.** Hit rates and byte counts are deterministic and exact; every
wall-clock figure is a median of interleaved process-granularity pairs against a control, per §3:

| Item | Result |
|---|---|
| 2-0 | `new` published a global prototype-mutation notice per allocation, retiring every prototype-keyed cache entry: **200 001 invalidations per 200 000 allocations → 3**. An inherited-method site inside an allocating loop went from a 50% hit rate to matching its hoisted control |
| 2-1 | A store that *creates* its property could never hit the store cache — **0 hits against 600 000 misses → 599 997 / 3**, and ~20% faster on a constructor loop |
| 2-2 | Named properties on a `JSArray` were a 100% miss: **0 → 199 999** |
| 2-4 | `o.x++` and `o.x op= rhs` reached **neither** cache — 0 hits *and* 0 misses. Both now take both, **0 → 199 999** on each side; the compound form went from costing 1.163x the spelled-out equivalent to 1.043x |
| 2-7 | The property map reserved 16 trie nodes for the first property of any object — **920 B unused**. 43.9% of 47 M real maps never outgrow one four-node group: **live map bytes 0.56x, allocated 0.82x**, and Typescript, the suite with the worst tail, gains most |
| 2-8 | Statics on a constructor function were a 100% miss — DeltaBlue's hot path — **0 → 199 999**, ~10% on a DeltaBlue-shaped loop. **This item also shipped a regression that broke DeltaBlue outright; the fix is folded into the same patch** |
| 2-9 | A shape-tracked property cost ~150 B of radix trie to store an 8-byte reference. The trie is no longer written at all while an object is shape-tracked — **a three-field object is 0.36x and an eight-field one 0.15x**, against **+8 B on every object** for the attribute array. Over an Octane run **six in seven property maps are never built**: 16.2 M → 2.5 M, live map bytes 0.15x. All 22 cache rows byte-identical. **Losing side, measured against a built control: ~20% on compile-and-first-run**, corroborated by Octane CodeLoad at 0.844 |

**Historical 0-6 smoke evidence.** The CI Octane run happened, was refreshed three times,
and the run committed **2026-08-07** — what `tests/octane/results/linux-x64/` held at the
snapshot — was the first to report its own spread. What it settles and what it does not:

- **Coverage: 17 of 17 scores, all 15 suites `ok`, for all three engines** — Broiler, Jint and
  a same-machine Chromium. Nothing errored, crashed or timed out (`diagnostics.md`: *"All 15
  suites completed. Nothing to diagnose."*). This establishes harness coverage for that
  smoke run, not acceptance of a candidate.
- **The configured smoke guard was recorded on that hosted runner.** Three
  repetitions per suite, medians reported, a spread beside every score: **16 of 17 are inside
  the declared 7.5%**, the median is **3.0%**, and the one exception is EarleyBoyer at 7.9%,
  flagged by the harness itself. This is a useful smoke observation. It is not an independent
  A/A calibration session, measurement-resolution result, or candidate decision threshold.
- **It also overturns the container band recorded below, and the correction runs the safe
  way.** Locally, five of thirteen scores were outside the band; on the runner **every one of
  those five is inside it** — Splay 15.9% → 5.1%, Crypto 12.5% → 3.0%, Richards 10.6% → 1.9%,
  SplayLatency 10.4% → 3.9%, DeltaBlue 9.1% → 5.3%. So the sentence this document has been
  carrying — *"a Richards change under ~10% is not measurable at three repetitions"* — is true
  of a shared container and false of that hosted runner, where the observed figure is nearer
  **2%**. The
  local table stays below, because the finding it actually supports is the one that survives:
  **stability is a property of a lane and workload and does not transfer**, which is why MOD-M1
  calibrates lane × workload × metric A/A envelopes independently.
- **A banded run and an unbanded one still cannot be differenced.** Between 2026-08-05 and
  2026-08-07 the geomean reads 498 → 372 and the ratio 149× → 158×, which looks like a
  regression and is nothing of the kind: **Chromium's own column moved 74 297 → 58 718 on the
  same runner and Jint's 820 → 577**, so what moved is the machine. The earlier run is
  `"Repetitions per suite: 1"`. **This smoke run is not retroactively one arm of an
  acceptance comparison.** The first accepted pair must be a separately controlled,
  identity-attested candidate/control session; a later hosted pair is continuity evidence,
  not a substitute for that session.
- **What is still owed is 0-7/0-8 plus MOD-M1's complete acceptance bundle**: exact rows and all
  repetitions/resources, independent A/A calibration, effective-setting attestation,
  immutable source/dependency/corpus identity, semantic-owner conformance, and durable raw
  evidence on every claimed RID.

#### The first configured spread on record — container smoke, not acceptance

Broiler-only, **3 repetitions**, twelve suites (the three slowest — Mandreel, zlib, Typescript —
left out for time), one process per repetition, in this container. Not a controlled accepted
lane and not a CI runner; what it was, was the first time anything in this campaign reported a
spread instead of promising one.

> **Kept as a lesson, not a decision rule.** The 2026-08-07 CI run measures the same thing on
> a different hosted lane and gets **16 of 17 inside 7.5%**, with all five of this table's
> offenders comfortably inside. **Do not quote either table's percentages as the campaign's
> candidate threshold or measurement resolution.** What this one is still good for is the
> point it makes against itself: two honest
> three-repetition measurements of the same engine, days apart, disagree about the band by up
> to **5.6×**, so a spread is a property of the lane and workload that produced it.

| Score | Median | Samples | Spread |
|---|--:|---|--:|
| Splay | 529 | 545, 461, 529 | **15.9%** |
| Crypto | 265 | 263, 265, 296 | **12.5%** |
| Richards | 246 | 249, 246, 223 | **10.6%** |
| SplayLatency | 1 199 | 1 163, 1 199, 1 288 | **10.4%** |
| DeltaBlue | 209 | 197, 209, 216 | **9.1%** |
| EarleyBoyer | 435 | 435, 445, 416 | 6.7% |
| Box2D | 657 | 657, 659, 623 | 5.5% |
| RegExp | 121 | 117, 123, 121 | 5.0% |
| Gameboy | 1 075 | 1 075, 1 043, 1 096 | 4.9% |
| NavierStokes | 517 | 535, 517, 516 | 3.7% |
| CodeLoad | 110 | 110, 112, 108 | 3.6% |
| RayTrace | 469 | 469, 460, 476 | 3.4% |
| PdfJS | 452 | 454, 452, 452 | 0.4% |

**Five of thirteen scores exceed the 7.5% band the harness declares here**, and the median is
5.5% against a best of 0.4%. On CI the same five read 5.1%, 3.0%, 1.9%, 3.9% and 5.3% and the
median over seventeen scores is 3.0%. *Both are measurements; neither is "the band".*

**Two of the five are Richards and DeltaBlue**, which is the pair phase 2's exit criterion rests
on. That does not disturb the verdict — 145× and 512× are nowhere near the 200× line, and four
measurements on two machines agree on which side each falls. **What this table cost, and it is
worth recording, is a wrong general claim drawn from a local one**: it concluded that *"a
Richards change under ~10% is not measurable at three repetitions"*, and the hosted runner's
1.9% observation says that local generalization was wrong. The conclusion §3.5 keeps is the
narrower one — lane × workload × metric stability must be independently A/A-calibrated where
the controlled arms run; neither a container nor this hosted smoke result supplies the candidate
threshold.

**What must not be read out of this table.** Its geomean (383) is over **13 scores, not 17**, and
it is Broiler alone in a container with no same-machine Chromium beside it — so it is not
comparable to the committed run's 498 and must never be quoted as if it were. Only the spread
column is the finding.

**The conformance gate is satisfied, and was re-run five times for items 3-3, 4-1, 4-3a and 4-3b.** All
four pinned manifests were run **2026-08-03 on linux-x64 at `9bf9639b` (the pin at the time)** — plus
`patches/0067`, and then plus each successive prefix through all five of `0067`–`0071` —
against the pinned suite ref `ccaac100`: **8 220 passed, 84 failed, 44 skipped, 9 timed out, and
every count is identical to §3.4's recorded run on all five, manifest by manifest.** The 84 are the same
`$262`-requiring files and the 9 the same integer-limit cases already tracked in
`test262-failures.txt`. So `properties-proxy` and `strict-mode`, which phase 2's exit gate names
because 2-1, 2-2, 2-4 and 2-8 all touch `OrdinarySetWithOwnDescriptor`, are **clean; 2-9, which
rewrites the storage underneath that path, adds no failure; and neither does 3-3's `let`/`const`
half.** A **fifth manifest** was added with that item — `test262-lexical-declarations`, because
none of the four covered `let` or `const` at all — and it is clean on both arms (§3.4).
**Re-run again 2026-08-04 at `61c8cc65` (the pin at the time) plus `patches/0078` for item 3-7, on both
settings of that patch's switch**: every count is identical, manifest by manifest. One run of
`properties-proxy` reported an extra failure whose captured stderr reads *"The JavaScript compiler
is not available"* — **a `dotnet build` rewriting the assembly under a running suite, which was
mine**, not an engine result; re-run with nothing else building it is clean (§3.4).

**Patch handoff status, corrected 2026-08-22.** The historical `0049`–`0115` campaign
handoff completed; the current aggregate tree has no `patches/` directory and no active
`Broiler.JS` patch ledger. The aggregate repository pins `Broiler.JS` at `7fb17553` at this
reading. Earlier pointer and patch numbers below are evidence provenance only and must not be
interpreted as pending work. Read current state with `git submodule status` and the submodule
log rather than copying a hash from prose.

**Two defects were recorded in the 2026-08-07 snapshot**: a refused write to a function's
`prototype` redirected `[[Construct]]`, and Octane's RegExp suite failed its own checksum.
Their status has not been reverified at the current pointer, so this is historical evidence,
not a current blocker assertion; any rescheduled work must reproduce them first.

---

## Track two — JavaScript seed exists; Broiler.VM profile phases are not accepted

The repository now contains more than the old file census recorded: the expression
model/emitter split has landed, `Broiler.JavaScript.Portable.Compiler` compiles the numeric
portable subset, and the aggregate product contains compile-ahead and an initial Worker
slice. Those facts are implementation evidence. They do **not** establish a general
JavaScript built-in for Broiler.VM, a runtime-compiler Native AOT closure, accepted VM
performance, shared-state ownership, or Worker lifecycle/resource completeness. No generic
Broiler.VM or WebAssembly-profile work is counted in this JavaScript evidence record.

| Phase | Current state | Next authority/gate |
|---|---|---|

The authoritative detail is in the four phase status files and
[`Modernization.md`](Modernization.md). Historical 2026-08-07 counts elsewhere in this file
remain reproducibility snapshots, not the current capability claim.
---

## 4. Where the engine stands

### 4.1 2026-08-07 implementation snapshot — phases A–F and 2 (none accepted)

Every item below was implemented and covered by the repository tests recorded at the
snapshot. **None is accepted**, and any current claim must reproduce the relevant semantic
and performance evidence under MOD-M1.

| Phase | Items | Result |
|---|---|---|
| **A** | P0-1, P0-3 | Prototype invalidation on every value allocation removed (800 013 → 3 per 200k loop); legacy `caller`/`arguments` made lazy via a deferred *data* property, preserving the Annex B descriptor shape. **2.0–2.9× on call paths, 6× less call allocation** |
| **B** | P0-2 | The ambient strict-mode scope stores a `bool` and writes **only on a transition**, so same-strictness call chains write nothing. `[ThreadStatic]` was rejected — it loses the value across async resumption |
| **C** | P1-1, P1-4 | Property writes stopped destroying the object's shape. A monomorphic read went **0 hits / 200 000 misses → 199 999 / 1**; constructor-built three-field objects **6 595 B → 1 480 B**, against 1 328 for the literal |
| **D** | P1-2, P1-3 | Prototype and class method calls now hit the cache (**0 → ~400k hits**); constant-key stores go through a store cache (2.1×, or 3.6× when the key is not one-character early-interned) |
| **E** | P2-1, P2-2 | Descriptor-free `push`; per-thread small-integer cache; unboxed `double` locals. Plus two array defects found by measuring: **repeated `pop` was quadratic (729×)** and array fill went **1 350 B → 145 B per element** |
| **F** | P2-3, P2-4, P3 | Dense element = one reference, not a 32-byte descriptor (`new Array(1000)` −73%); string concatenation no longer quadratic (**150×** on the accumulation loop); the per-call activation record became a slot in a context-owned array addressed by a struct token — an argument-less call allocates **nothing**, and call-heavy code runs **3–15% faster** (median ≈11%) |
| **2** | 2-0, 2-1, 2-2, 2-4, 2-7, 2-8 | Every remaining way a constant-key property access missed its cache, closed: allocation no longer retires prototype-keyed entries, a store that *creates* a property can hit, arrays and functions track named properties by shape, and `++`/`op=` take both caches. **Six sites went 0 → 199 999 hits.** Plus 2-7, which is memory rather than hit rate: the property map's 16-node floor charged **920 B of unused trie** to every object's first property — **live map bytes 0.56x**. Plus 2-9, which finishes what 2-7 started one layer down: a shape-tracked object no longer writes the radix trie at all, so **a three-field object is 0.36x and an eight-field one 0.15x** and **six in seven property maps over an Octane run are never built** (16.2 M → 2.5 M, live map bytes 0.15x). Delivered as `2df877a0`…`a6f101cc`, all in the pinned pointer, and 2-9 on top |

Headline before/after on the probes:

| Hot path | Before | After | Factor | Alloc before | Alloc after |
|---|---:|---:|---:|---:|---:|
| Plain function call (sloppy) | 945 ms | 327 ms | **2.9×** | 1 784 B | **264 B** |
| Closure call | 953 ms | 357 ms | **2.7×** | 1 816 B | **296 B** |
| Prototype method call | 861 ms | 370 ms | **2.3×** | 1 632 B | **264 B** |
| Built-in call (`Math.max`) | 443 ms | 217 ms | **2.0×** | 400 B | **176 B** |
| Empty `for` loop | 426 ms | 210 ms | **2.0×** | 96 B | 96 B |
| Own property read | 491 ms | 333 ms | **1.5×** | 128 B | 128 B |
| Integer arithmetic | 476 ms | 342 ms | **1.4×** | 128 B | 128 B |
| `s = s + x` × 20 000 | 1 604 ms / 3.20 GB | **10.7 ms / 4.4 MB** | 150× | 913 gen2 | **0 gen2** |
| `script:dromaeo-object-array` | 5 564 ms | **646 ms** | 8.6× | — | — |
| `script:stopwatch` (real script) | 976 ms | 669 ms | **1.5×** | 736 MB | **264 MB** |

Repository suite at `cdb2fd41`: **7 284 tests across 13 projects, 7 281 passing.** The
three failures were host-environment, not engine — `ReproTests.Repro` (a debugging
leftover writing to a hardcoded `D:\Broiler.JS\` path, asserting nothing) and two
`Issue838Tests` date cases that assume a UTC host. `ReproTests` and its `ReproT`
sibling have since been retired: neither asserted anything, and on Linux the `D:\`
string was treated as a relative filename, so `Repro` passed there and the count read
7 282 of 7 280 depending on the host. Its `super`-in-class-field-initializer probes now
assert, as `ClassFieldInitializerEvalSuperTests`. Expect two failures on a non-UTC host
and none of them from `Repro`. Baseline before attributing either to a change.
`Broiler.JS/BroilerJS.sln` has been deleted — it could not restore, referencing
`Broiler.Regex` paths that moved — so `Broiler.JS.slnx` at the repository root is the
solution to run.

Deleting that solution left `Broiler.JavaScript.Network` and
`Broiler.JavaScript.NodePollyfill` in no solution at all. They were deliberately not
added to `Broiler.JS.slnx`: neither compiles. Both still open `Broiler.JavaScript.Core`,
a namespace removed by the engine refactor, so every source file in them fails with
CS0234 — 23 errors and 3 errors respectively. They were only ever reachable through a
solution that itself could not restore, so nothing regressed when it went. Whoever
intends to revive the `fetch`/`Blob`/`AbortController` and Node polyfill surfaces should
repair the namespace first and register them then; deleting the sources is a separate
decision and is not made here.

### 4.2 The 2026-08-07 Octane profile

**Regenerated by the workflow on 2026-08-07 at the pointer pinned then** — `tests/octane/results/`,
Octane version 9, Chromium 149.0.7827.55 and Jint 4.15.3 on the same machine. This replaces the
2026-08-05 table, which replaced the 2026-08-03 one. Ordered by ratio, best first.

> **This is the first committed run that reports its own three-sample spread.** Three
> repetitions per suite, the median, and each observed range make it useful repeatability
> smoke and continuity evidence. They do not establish an A/A envelope, measurement
> resolution, or a claimable future delta. This table cannot be differenced against the run it
> replaces, and a later hosted pair remains a continuity trend rather than an attributable
> candidate/control comparison. Read it for historical magnitude and coverage only.

| Benchmark | Chromium | Broiler | × slower | spread | Jint | Dominant blocker |
|---|--:|--:|--:|--:|--:|---|
| SplayLatency | 76 352 | 2 412 | **32** | 3.9% | 3 357 | — (best axis; GC pauses are fine) |
| Typescript | 99 226 | 2 308 | 43 | 2.9% | 2 261 | mixed; overhead amortized by real work |
| Splay | 45 430 | 780 | 58 | 5.1% | 817 | B1 allocation rate |
| NavierStokes | 38 436 | 519 | 74 | **0.6%** | 257 | B1 boxed array elements |
| RegExp | 10 223 | 113 | 91 | 1.8% | 187 | B5 — **and B5 names the wrong component**; see phase 5 |
| Gameboy | 99 597 | 1 090 | 91 | 3.6% | 853 | B1 typed arrays, B3 exotic exclusion |
| PdfJS | 60 750 | 506 | 120 | **0.2%** | 778 | B1, B5, B4 |
| Crypto | 41 963 | 297 | 141 | 3.0% | 138 | B1 integer boxing |
| Box2D | 102 475 | 717 | 143 | 2.0% | 570 | B1 + B2 (no escape analysis, no inlining) |
| Richards | 37 710 | 260 | **145** | **1.9%** | 173 | B2 call cost, B3 shape transitions — **inside 200×** |
| CodeLoad | 28 215 | 144 | 196 | 3.5% | 4 074 | B4 eager compilation — **~27% of what it measures**, see 1-1 |
| zlib | 91 233 | 444 | 206 | 0.7% | 4 967 | B1 integer boxing |
| EarleyBoyer | 87 285 | 406 | 215 | **7.9% ⚠** | 367 | B1 allocation rate |
| RayTrace | 117 214 | 448 | 262 | 5.8% | 411 | B1 + B2 escape analysis |
| Mandreel | 46 988 | 173 | 272 | 2.3% | 86.9 | B1 heap traffic — **not B4 compile**, see 1-4 |
| DeltaBlue | 105 994 | 207 | **512** | 5.3% | 167 | **B2 polymorphic call cost — the one suite still outside 200×** |
| MandreelLatency | 67 368 | 15.4 | **4 375** | **0.0%** | 727 | ~~B4 compile latency~~ — **measured: not compilation.** Pauses between render frames over already-compiled code; a 3.04× faster compile of `mandreel.js` moves it 0.992×. Points at B1 allocation rate / B7 |
| **Overall (geomean)** | **58 718** | **372** | **158** | — | **577** | spread (worst ÷ best suite) **138×** |

The shape of that list *is* the finding, and it has not changed across three regenerations: the
extremes are front-end and call-path, not arithmetic. The losses are concentrated in two
subsystems rather than spread evenly — which is what makes them addressable in a defined order.

**What this run says, and one thing it conspicuously does not.**

- **The provisional 7.5% harness guard held for 16 of 17 scores on this hosted smoke run.**
  The one exception is EarleyBoyer at **7.9%**, which the harness marks itself.
  The median spread is **3.0%** and five suites are at or under 2% (PdfJS 0.2%, NavierStokes
  0.6%, zlib 0.7%, Richards 1.9%, Box2D 2.0%), with MandreelLatency reporting three identical
  samples. These ranges do not bound an MOD-M1 paired estimate or define the lane's resolution.
- **And it overturns the container band this document has been quoting.** The local
  three-repetition run recorded below put **five of thirteen** scores outside 7.5% — Splay
  15.9%, Crypto 12.5%, Richards 10.6%, SplayLatency 10.4%, DeltaBlue 9.1%. On the runner
  those same five read **5.1%, 3.0%, 1.9%, 3.9% and 5.3%**: every one of them is inside the
  guard, and Richards is **5.6× tighter**. The only valid inference is that three-sample spread
  changed materially with the host. Neither value is a practical effect threshold, paired
  uncertainty estimate, or acceptance envelope, and neither transfers to the other machine.
- **The Chromium column is the reason the no-deltas rule is not pedantry, and it fires for a
  third time.** Broiler's geomean reads 498 → 372 across the two most recent committed runs,
  which would be a 25% regression if anything about it were attributable to the engine.
  **Chromium's own geomean moved 74 297 → 58 718 on the same runner, and Jint's 820 → 577** —
  three engines falling 0.70–0.79× together is evidence of common-mode hosted-runner movement,
  not an attributable Broiler regression. Chromium and Jint are environmental canaries, not
  MOD-M1's same-build null control; different engines need not respond proportionally, so the
  ratio column is descriptive and cannot divide the host effect out or normalize a candidate
  delta.
- **Richards is inside 200× and DeltaBlue is not** — 145× against 512×, the same split the
  local five-repetition run found at 150× and 447×, the 2026-08-03 CI run at 144.9× and 460×,
  and the 2026-08-05 CI run at 141.3× and 399.5×. Four smoke observations agree on the side of
  the historical 200× prioritization line. The three-sample ranges do not bound a
  candidate/control delta, so this remains historical phase-ordering evidence rather than an
  MOD-M1-accepted performance claim.
- **Jint is the more informative column.** Against a managed interpreter on the same runtime
  Broiler is **0.644× overall** — behind, on a geometric mean of the 17 per-benchmark ratios.
  It is *ahead* on the call- and object-heavy suites this campaign has been working (Crypto
  2.15×, NavierStokes 2.02×, Mandreel 1.99×, Richards 1.50×, Gameboy 1.28×, Box2D 1.26×,
  DeltaBlue 1.24×, EarleyBoyer 1.11×, RayTrace 1.09×, Typescript 1.02×) and far behind on
  three: **MandreelLatency 0.021×, CodeLoad 0.035×, zlib 0.089×**. Those three are where the structural gap is, and it
  is a gap a reference that is not a JIT can show and Chromium's column cannot. **One of the
  three was mis-attributed here for three revisions and is corrected below**: this bullet used
  to read *"two of those three are the front end and the third is latency"*, which is true of
  CodeLoad and false of zlib — measured, zlib's `eval`-compile is **2.09 s against 35.06 s for
  one iteration of its own benchmark**, so the front end is under 6% of a single iteration and
  under 2% of the measured region. **zlib is an execution gap, not a compile gap** (§4.2a).
- **The worst score is still not a compilation problem.** MandreelLatency at 5 332× is the
  tail, and 1-4 and 1-1 between them made compiling `mandreel.js` 3.04× faster and moved it
  0.992×. It belongs to phase 3.

### 4.2a The corpus every phase-3 and phase-4 headline is computed over is **7 of 15 suites**

**Found by asking a different question and tripping over the answer.** §2-13 established that
Broiler's largest *private* deficiency is zlib — 12.0× behind Jint relative to Chromium, against
0.77× on DeltaBlue — so the obvious next step was to look at what the censuses say about zlib.
They say nothing. **zlib is in none of them**, and neither are Mandreel, Gameboy, PdfJS,
Typescript, CodeLoad, Splay or RegExp.

**`TypeFeedbackMetrics` and `SpecializingTierMetrics` both ran the same seven**: Richards,
DeltaBlue, RayTrace, Box2D, EarleyBoyer, Crypto, NavierStokes. That was a defensible corpus for the
question 4-1 asked. It stopped being one the moment its output was quoted as ***"the corpus"*** —
the phrase every phase-3 and phase-4 headline in this document uses — over a denominator that was
never stated.

**The third census in the same directory *lists* all fifteen and reaches nine**, which is worse
than either — see below. So the shortcut is not shared and not deliberate: two censuses were
written against seven suites, and the one written against fifteen could never finish them. *Nothing
here was a considered corpus decision; three instruments arrived at three different partial answers
and one phrase — "the corpus" — was used for all of them.*

#### Why it was seven, which nobody had written down: **Mandreel aborts the census host**

Not a choice about cost. Widening the suite list and running it produces, at the ninth suite:

```text
Stack overflow.
   at DynamicClass.global_init-mandreel.js:123784,0(...)
   at DynamicClass.mandreelAppInit-mandreel.js:1456,0(...)
```

**An uncatchable .NET stack overflow, and item 0-2 is exactly the thing that should have prevented
it.** 0-2 records that the shell *"runs script-host JS on a 16 MiB thread it sizes itself"* with a
4 MiB reserve, so deep recursion raises a catchable *"Maximum call stack size exceeded"*. That is a
property of `Broiler.JavaScript/Program.cs` — **the shell** — and `BenchmarkContext.Create` built a
plain `JSContext` on whatever stack the runtime handed `Main`. *Every benchmark host in this
campaign has been running without the reserve the shell has had since phase 0*, and the one suite
that needs it was therefore permanently unmeasurable.

**And the instrument made that as expensive as possible**: it serialized its JSON once, at the end,
so an abort in the ninth suite discarded the eight that had already run. A partial corpus that
cannot even produce a partial result is a corpus nobody widens twice.

**Both are fixed** — the census runs on the shell's thread with the shell's budget, and writes its
report after *every* suite — and Mandreel now fails the way the shell makes it fail, catchably,
instead of taking the process down.

#### What the widened count says, and it moves phase 4's premise

| Corpus | suites | property reads | monomorphic | calls | monomorphic |
|---|--:|--:|--:|--:|--:|
| as quoted throughout this document | 7 | 37 871 921 | **93.54%** | 4 239 252 | **96.70%** |
| every suite that runs | 12 | **307 869 165** | **80.11%** | **52 931 427** | **86.35%** |

**The old seven reproduce to the digit** — 93.54% of 37.9 M reads and 96.70% of 4.24 M calls, which
is §0's phase-4 row verbatim — so this is the same instrument over a bigger corpus and not a
different measurement. **87.7% of the corpus's property reads were outside the suites being
counted.**

**4-1's headline is the premise items 4-2 and 4-4 rest on, and it falls 93.54% → 80.11%.** The two
suites that move it were never in a census:

| Suite | reads | monomorphic | polymorphic sites |
|---|--:|--:|--:|
| **Typescript** | 207 393 777 | 89.52% | 748 |
| **Gameboy** | 54 539 137 | **34.60%** | **1 282** |
| **Splay** | 1 445 504 | **10.15%** | 43 |
| Box2D (in the seven) | 25 963 010 | 94.12% | 247 |

Typescript alone is **67% of the corpus's reads**. Gameboy at 34.60% monomorphic and Splay at
10.15% are not tails — they are the two most polymorphic suites in Octane, and the phase that
speculates on monomorphism had counted neither. *This does not retire 4-2b — 80% is still most of
the work — but "93.5% of reads are monomorphic" was a fact about seven suites chosen for being
call-heavy, and it has been carried as a fact about the engine.*

#### And the two suites Broiler is furthest behind on do almost no property reads or calls at all

| Suite | reads | calls | Jint ÷ Broiler (§2-13) |
|---|--:|--:|--:|
| zlib | **29** | **9** | **12.0** |
| Mandreel | **79** | **815** | 0.54 |
| MandreelLatency | — (same suite) | — | **54.3** |

Both are asm.js: a typed-array heap addressed by shifted integer indices, with arithmetic and
element access and essentially no named property access and no polymorphic dispatch. **So the two
paths phases 2 and 4 are entirely built around are structurally absent from the suite this engine
is worst at**, which is a sharper statement of §Non-goals' asm.js bullet than that bullet makes:
the reason not to special-case asm.js is well taken, and it is *not* a reason to believe the
general mechanisms reach it.

**zlib's own gap is execution, not the front end.** Measured directly — zlib evaluates a 185 KB
asm.js blob through `eval` inside its own timed function — the **eval-compile is 2 086 ms against
35 062 ms for one iteration** of the benchmark, so compilation is **5.9% of an iteration and under
2% of the measured region** (10 deterministic iterations). §4.2's Jint bullet used to file zlib
under "the front end" alongside CodeLoad; that is corrected above. What zlib is behind on is
running the code.

#### The same hole in phase 3's corpus, and it is bigger

`SpecializingTierMetrics` is where phase 3's headline numbers come from — the box counters, the
allocated bytes, the GC-pause denominator `0090` added — and **it ran the same seven suites**.
Widened the same way (shell stack, shell budget, checkpointed per suite), with the counters on so
every column below is a deterministic count:

| Corpus | suites | boxing requests | boxes allocated | allocated bytes |
|---|--:|--:|--:|--:|
| as quoted throughout phase 3 | 7 | 52 039 070 | 31 401 346 | 3.13 GB |
| every suite that runs | 12 | **164 127 581** | **90 641 738** | **12.93 GB** |

**65.4% of the corpus's boxes, 75.8% of its allocated bytes and 80.3% of its execution are outside
the suites phase 3 has been counting.** The seven's 31.4 M reproduces the 31.16 M this document
records after `0089`, so this is the same instrument over a bigger corpus.

**And one suite that was never in it out-allocates the entire measured corpus:**

| Suite | boxes allocated | allocated bytes | in the seven |
|---|--:|--:|---|
| **Gameboy** | **41 308 969** | 1.96 GB | **no** |
| Crypto | 13 416 207 | 0.92 GB | yes |
| NavierStokes | 11 747 641 | 0.49 GB | yes |
| PdfJS | 9 001 157 | **2.35 GB** | **no** |
| Typescript | 8 883 786 | **4.96 GB** | **no** |
| Box2D | 5 420 051 | 0.56 GB | yes |
| *(whole old seven)* | *31 401 346* | *3.13 GB* | — |

**Gameboy alone allocates 1.32× the boxes of the seven-suite corpus phase 3 ranked itself
against**, and Typescript allocates more bytes than all seven together. Every "N% of everything the
corpus allocates" in this phase — 41.89%, 12.2%, 54.0%, 9.4%, 0.36% — has that denominator.

**What survives the widening is worth as much as what does not.** `0090`'s denominator holds:
collection is **1.80% of elapsed on the widened corpus** against 2.29% on the seven, so *"a box
costs about fourteen times more to create than to collect"* was not an artifact of the corpus, and
§Non-goals' GC bullet stands. What does not survive is the **ranking** built on top of it: phase 3
sized its remaining work as *"an XL bidding for under 2%"* of a driver that turns out to be a fifth
of the real one, and the suites carrying the other four fifths were never asked what they allocate.

#### And it partly reverses item 3-1's move off storage

The same run carries item 3-1's boxing-source census, so the widened corpus can be attributed
without measuring anything else. The census's own partition is
`requests − literal − conversion = what the operators and builtins mint`:

| Corpus | requests | literal | **conversion** | operators + builtins |
|---|--:|--:|--:|--:|
| old seven | 52 039 070 | 3.2% | **47.4%** | 49.4% |
| every suite that runs | **164 127 581** | 5.1% | **42.2%** | 52.7% |

***The share barely moves and the population triples**, which is the trap `0086` already named
from the other direction*: a share looks robust — 47.4% against 42.2% — and would be read as
*"the corpus does not matter"*, while the absolute count goes **24.6 M → 69.3 M** and its dominant
producer changes identity completely.

| Suite | conversions | share of its requests | in the seven |
|---|--:|--:|---|
| **Gameboy** | **26 938 581** | **51.0%** | **no** |
| Crypto | 17 062 140 | 67.9% | yes |
| **Typescript** | 11 493 206 | 28.8% | **no** |
| **PdfJS** | 5 677 720 | 34.1% | **no** |
| NavierStokes | 4 039 054 | 25.6% | yes |
| Box2D | 3 206 383 | 34.9% | yes |
| *(whole old seven)* | *24 649 985* | — | — |

**64.4% of the corpus's conversions are outside the seven, and Gameboy alone mints more of them
than all seven together.** A conversion is the compiler boxing a raw `double` to cross into a
`JSValue` — the one boxing source a typed backing store removes without further operator work.

**That is item 3-1's re-specification meeting its own counter-example.** 3-1 was moved off storage
on the strength of this census: *"only 5.0% of NavierStokes' requests are a raw double crossing
into a JSValue, so a typed backing store cannot be why its boxes survive, while the
conversion-heavy suite is Crypto at 31.0% — the one the guarded tree already served best."* That
reasoning is sound about the seven and was never tested against **Gameboy**, where the conversion
is **51.0% of a 52.8 M-request workload**, and which is a Game Boy emulator: a `Uint8Array` memory
image and register arrays, which is *precisely* the shape 3-1 was originally written for.

**The claim this does and does not support.** It does not say the typed store is worth building —
that needs a wall-clock A/B nobody has run, and `0086`'s rate lesson says Gameboy's 1.96 GB over
23.8 s is a lower rate than NavierStokes' 0.49 GB over 2.0 s. It says the *evidence* that retired
the item was drawn from a corpus that excluded the item's best case, and **3-1's storage half should
be re-opened as unmeasured rather than left recorded as refuted**.

> **Shares here are not comparable to the ones this document records from the pre-`0084` census.**
> This run is post-`0084`, `0086` and `0089`, which between them removed most of the
> operator-minted boxes, so every remaining source's *share* is mechanically larger — NavierStokes
> reads 25.6% here against the 5.0% recorded then. What is comparable is the absolute counts and
> the ranking across suites within this one run, and those are what is quoted.

> **The elapsed column is not a timing measurement and is not used as one.** This run has the
> counters on, which the census's own documentation says distorts wall clock; elapsed is quoted
> only to say which suites carry the work, at order of magnitude. The box and byte counts are
> deterministic.

#### The third census listed fifteen suites and reached nine

**The correction above needs a second correction, and it goes the other way.**
`PropertyMapDistributionMetrics` — item 2-7's instrument, the one that has had all fifteen suites
since phase 2 — **aborts at the ninth on the same Mandreel stack overflow**, and because it too
serialized once at the end, an aborted run produces *nothing at all*. So it was not the well-behaved
control; it was the worst of the three. **Items 2-7 and 2-9's map figures are not reproducible from
a clean tree**: the only way to get output was `BROILER_MAP_DISTRIBUTION_SKIP`, and the recorded
numbers do not say what was skipped. *Listing a suite is not measuring it, and the difference is
invisible in the output.*

Fixed the same way. Run over all fifteen, it completes, and it re-takes two landed items:

- **2-9 is corroborated on the full corpus.** It recorded *"six in seven property maps are never
  built: 16.2 M → 2.5 M"*; the widened run counts **2 202 782 maps**, which is the right side of
  that ratio measured over twice the suites.
- **2-7's shipped policy is confirmed on live memory and its allocation win is not.** Simulating
  the candidates against the widened distribution:

| Policy | live bytes | vs round-up-16 | allocated | vs round-up-16 | nodes copied |
|---|--:|--:|--:|--:|--:|
| `round-up-16` (the pre-2-7 rule) | 2.044 GB | 1.000 | 2.075 GB | 1.000 | 424 472 |
| `round-up-8` | 2.043 GB | 0.9999 | 3.114 GB | 1.501 | 9 233 940 |
| `round-up-4` | 1.680 GB | 0.822 | 3.153 GB | 1.520 | 13 858 188 |
| **`min-4-then-double`** (what ships) | **1.316 GB** | **0.644** | 2.166 GB | **1.044** | 13 858 188 |

  2-7 recorded **0.56× live and 0.82× allocated**. **Both figures shrink on the wider corpus, and
  one of them changes sign.** Live memory still favours the shipped policy but by less — **0.644×
  against 0.56×** — while the allocated-bytes win becomes a small loss: geometric growth pays
  **33× the node copying** (13 858 188 against 424 472), and on suites 2-7 never saw that turns
  0.82× into **1.044×**. The item's decision stands, because it was taken on live memory and live
  memory is still a third off; what should stop being quoted is the allocated column, which is now
  a wash at best.

- **And `shareAtOneGroup` reads 0.02% against the 43.9% 2-7 was justified by** — not a
  contradiction, but §3.5's *"an item can be overtaken by the items around it"* a third time: 2-9
  subsequently stopped writing the trie at all for shape-tracked objects, and the maps that survive
  are the ones that needed a real descriptor, which are not the one-group population 2-7 was
  about. *A share recorded before the item after it landed is not a share.*

**Two defects in the instrument itself, reported rather than smoothed.** Its policy table labels
`round-up-16` *"current: VirtualMemory.Allocate as written"*, which has been false since 2-7
shipped — the code is geometric-from-`NodeBlock` and says so in its own remarks, so the simulation
now describes a baseline nobody runs. And one histogram bucket reads **−15**, with
`negativeBucketCounts: 15`: a count that can go negative is a defect in the counter, and it is
surfaced in the output instead of being clamped away.

**Both closed** (`0110`). The labels are corrected against the code: `round-up-16` is named as *the
pre-2-7 rule, kept as the baseline every other row is ratioed against*, and `min-4-then-double` —
which is what `VirtualMemory.Allocate` does today, geometric from `SAUint32Map.NodeBlock` — is
marked **CURRENT**, so the table no longer leaves the shipping policy unlabelled while calling a
retired one current. **And the negative bucket had a structural cause worth naming**: the histogram
counts each map once, in the bucket its life ended at, by *moving it out of the previous bucket* as
it grows — which assumes its arrival there was counted. A map already at that size when `Reset`
zeroed the table has a decrement with nothing to cancel. *A negative count is not a distribution,
and every share in the table is divided by a total that contains it.* The decrement now clamps at
zero and the clamps are counted separately (`resetStraddlingMaps`), which is the bargain the
negative was making, made correctly. **Re-run: 17 clamps, 0 negatives** — so the defect reproduces
and the fix is demonstrated rather than assumed.

#### Counters off: two things the widened corpus says that the seven could not

Re-run with the counters off and tiering disabled (`--specializing-tier <dir> none timing`), so
the wall clock is a legitimate one.

**First, and it is a ratio rather than a share, so the caveat below does not touch it: Splay
spends 10.3% of its time in collection, against 1.07% across the corpus** — and Splay was never
in a census.

| Suite | elapsed | GC pause | **GC share** | in the seven |
|---|--:|--:|--:|---|
| **Splay** | 2 011 ms | 206.5 ms | **10.3%** | **no** |
| Crypto | 2 700 ms | 118.4 ms | 4.4% | yes |
| DeltaBlue | 732 ms | 26.1 ms | 3.6% | yes |
| EarleyBoyer | 3 273 ms | 88.8 ms | 2.7% | yes |
| Typescript | 40 389 ms | 960.2 ms | 2.4% | **no** |
| Box2D | 6 440 ms | 124.7 ms | 1.9% | yes |
| Richards | 544 ms | 4.5 ms | 0.8% | yes |
| Mandreel | 288 674 ms | 2 035.7 ms | 0.7% | **no** |

`0090` measured collection at **1.8–2.0% of the driver** and concluded *"a box costs about fourteen
times more to create than to collect"*, which §Non-goals promotes from assertion to measurement.
**That average was taken without the one suite Octane includes to stress the collector.** Splay
builds and discards a large splay tree; `SplayLatency` exists to time the pauses. At 10.3% the
exchange rate there is nearer 9:1 than 14:1 — *still the right conclusion, and a fifth of the
margin*. The claim that survives is the one `0090` actually needs: allocation dominates collection
on every suite measured. What does not survive is quoting **one** number for it, when the spread
across suites is **0.7% to 10.3%**.

**Second: Mandreel completes with tiering off and fails the stack guard with tiering on.** Same
16 MiB stack, same 12 MiB budget, same source — `arm=None` runs it in 288.7 s, `arm=Feedback`
raises *"Maximum call stack size exceeded"* in `global_init`. So the tiering path costs enough
stack to matter on the deepest recursion in the corpus, which is a constraint on item 4-2 nobody
had measured because Mandreel had never reached a census. It is recorded rather than chased.

> **The elapsed *shares* in that table must not be read as "share of the engine's Octane work".**
> This census runs a fixed three iterations of each benchmark; Octane's scoring loop gives each
> benchmark roughly equal wall clock and normalises against a per-benchmark reference. So Mandreel
> at 78.8% of census elapsed means *one iteration of Mandreel is enormous*, not that Mandreel is
> four fifths of an Octane run. The **GC column above is a ratio internal to each suite** and is
> unaffected; the box and byte counts reported earlier share this three-iteration basis
> consistently, which is what makes *those* comparisons sound.

#### Honest limits of the widened run

Three suites load and report setup-only counts because their benchmark bodies fail in this host,
each for a reason already on record elsewhere in this document and none of them new:

- **zlib** — `ReferenceError: read is not defined`. The `read` shell builtin is defined in
  `Program.cs`, and the census host is not the shell. Same class of defect as the stack reserve.
- **Mandreel** — `RangeError: Maximum call stack size exceeded`. This is the fix *working*: what
  used to abort the process now throws. Getting Mandreel to complete in the census host is
  further work.
- **RegExp** — `Error: Wrong checksum`, the pre-existing defect §0 already records.

Together they contribute **267 472 of 308 136 637 reads, 0.09%**, so the headline is unaffected —
but the widened corpus is **12 suites, not 15**, and the table above says 12.

### 4.3 The blockers, ranked

The bridge between §4.1 (what the engine does) and the phases (what to do). Ordered by how
much of the gap each accounts for.

> **This ranking predates the corpus correction in §4.2a and has not been re-taken.** Every
> quantity it orders by was measured over the seven suites the censuses could reach, and §4.2a
> found that 87.7% of the corpus's property reads, 65.4% of its boxes and 75.8% of its allocated
> bytes lie outside them. Three specific entries below are now known to rest on a partial corpus:
> **B1**'s ranking as "the single largest multiplier" (the boxing census tripled and its dominant
> source changed identity), **B2**'s "Richards and DeltaBlue have the worst throughput ratios"
> (§2-13 attributes most of DeltaBlue's ratio to V8 rather than to this engine), and **B7**'s
> reading of the collector (Splay spends 10.3% of its time collecting and was in no census). *A
> re-ranking needs a per-suite time basis this document does not yet have* — the census's own
> elapsed column is on a fixed-iteration basis that Octane's scoring loop does not share — so the
> entries are left standing with their provenance marked rather than reordered on the wrong
> number.

**B1 · Every JavaScript value is a heap-allocated object.** `JSValue` is
`public abstract partial class JSValue` — a CLR reference type. No Smi tagging, no
NaN-boxing. Integer arithmetic allocated **128 B/iteration**; an *empty* `for` loop,
96. Two mitigations landed (small-integer cache, unboxed `double` locals) but the
second has a deliberately narrow gate. **Object fields, array elements, parameters,
return values, and anything crossing a call boundary are still boxed.** The single
largest multiplier, and it applies to all 17 scores. → **phase 3**

**B2 · One non-speculative compile tier.** source → `FastParser` → `FastCompiler` →
LINQ expression trees → IL via `Broiler.JavaScript.ExpressionCompiler` → RyuJIT. Real
machine code comes out, so this is not "an interpreter" — but it is compiled once,
generically, with no knowledge of the types that will flow through it. Every `+` is a
runtime helper implementing the full §13.15 algorithm. **No JS-into-JS inlining is the
sharpest sub-case**: Richards and DeltaBlue are built out of one-line methods, which
is why they have the worst throughput ratios. Now with a number on it — **a call costs
~250–300 ns, about thirteen times the entire loop body it replaces**, and none of that is
callee resolution (2-6). → **phase 4**

**B3 · Shapes and inline caches cover only a slice of the object model.** The
structures work well on the sites they cover; what they do not cover maps one-to-one
onto benchmarks. → **phase 2**

| Gap | Hits |
|---|---|
| Shape eligibility is `GetType() == typeof(JSObject)` — `JSArray`, `JSFunction`, every exotic excluded | **Fixed for arrays (2-2) and functions (2-8).** The four benchmarks named here were the wrong ones — they reach arrays by element and by `length`, neither of which a shape can hold. The idiom that pays is statics on a constructor function, and **DeltaBlue at 601× was the case** — 2-8 took it from 0 to 199 999 hits |
| No shape-transition cache — *creating* a property misses every time | Richards, DeltaBlue, RayTrace, Box2D |
| `o.x++`, `o.x += 1`, computed keys, `super`, optional chains, private names keep the old lowering | Richards, Gameboy, Box2D |
| Double storage in `TrackShapeDataProperty` | everything — but **measured at ~3% of a worst-case store loop and, after 2-7, 1.0-4.3% of an object's per-property bytes**; 2-3 is **closed** on that, and the 67-94% that *is* the trie became 2-9, **which has landed** |

A fifth gap belongs on that list and is **fixed**: every `new` published a global
prototype-mutation notice, so a prototype-keyed entry could not survive a loop that
allocated — which is every loop in Richards, DeltaBlue, RayTrace and Box2D. It was worth
half the hit rate at an inherited-method site. See 2-0; the structures were fine, nothing
was allowed to stay warm.

**B4 · Compile time and latency on large machine-generated code.** Compilation is
eager; expression trees are an expensive, non-incremental intermediate; and the front end
recurses over nested source. A measured floor for the first two: compiling
`return a + <i>;` through the `Function` constructor costs **~7.5 ms**, and that is three
compilations rather than one — §20.2.1.1.1 requires the parameter text and the body text to
be validated separately, so `JSFunction` compiles each alone before the assembled source
(the `Evaluate` that follows hits the cache). So **~2.5 ms to compile one trivial
expression.** That is the number 1-3 was told to go and measure, and it is large enough
that it will still be there after 1-1 stops compiling what is never called.

**This blocker named three causes and had a fourth, which was the biggest of them.** The
three-way split it asked for now exists (`--compile-scaling`) and reports **parse ≈ 0.5%,
expression-tree construction ≈ 11%, IL emission ≈ 89%** on function-dense source. Inside
that 89% sat an algorithmic defect rather than a cost: the closure rewrite's per-lambda
scope was a `List` scanned once per parameter reference, making emission **quadratic in a
scope's binding count**. "Machine-generated code is expensive to compile" was true and read
as a property of its size; it was a property of its *width*. Fixed in **1-4** — Mandreel's
whole front end 21 307 → 7 015 ms. What remains of B4 after it is genuinely the eager and
non-incremental part, which is 1-1 and 1-3.

The recursion is a separate, sharper problem: it aborts the process rather than costing
time, it lives in three passes across `.Parser`, `.Compiler` and `.ExpressionCompiler`,
and it follows source **nesting** rather than source **size** — a flat 200 000-statement
function is fine while ~19 400 nested operators is not. Mitigated at `685026c0` by giving
compilation a stack the engine sizes; see 1-2, which also records that the Mandreel
failure this blocker was written around does not reproduce on linux-x64. The blocker with
the clearest browser relevance: it is page load time. → **phase 1**

**B5 · The regex engine is a backtracking interpreter — *and it is not the engine Octane
runs*.** `Broiler.Regex`'s `Matching/Matcher.cs` has no compilation to native code; V8's
Irregexp JIT-compiles each pattern. RegExp is 110× off *against Octane's lowest reference
baseline*. **The second half of that sentence — "the same engine sits on PdfJS's and
Typescript's critical path" — is wrong, and phase 5's profile is what found it:** `JSRegExp`
keeps `System.Text.RegularExpressions` as the default engine and routes only semantic-gap
patterns to `Broiler.Regex`, and Octane's corpus contains no look-behind and no `u` flag, so
it never gets there. The engine that does serve those suites is built **interpreted** — no
`RegexOptions.Compiled` anywhere on the user-regex path. **This blocker names the wrong
component**; see phase 5 for what the measurement puts in its place. → **phase 5**

**B6 · Ambient state on hot paths — *the write half was the blocker, and it is gone*.**
`JSEngine` holds the current context and the strict-mode flag in `AsyncLocal<T>`. P0-2 removed
the redundant *writes*, which is where the cost was: a write allocated a fresh
`ExecutionContext`. `JSValue`'s set accessors do still **resolve** strictness through the
`AsyncLocal<bool>` on every uncached property write — and **that read measures at 0%**. Removing
all 13 resolutions moved a 30 M-write all-misses loop by nothing (median paired ratio 1.013).
So this is no longer a blocker on the write path; **2-5 is closed on that measurement.** What
remains under B6 is the *context* `AsyncLocal`, which nothing here has measured — it is read on
paths this blocker never quantified, and it should be measured before it is claimed. → **2-5
closed; the context read is unmeasured**

**B7 · GC — *not* a primary blocker.** Stated explicitly to keep it off the list.
SplayLatency at 45× is the *best* result in the suite and Splay's throughput at 152×
beats the median. The .NET collector is handling a workload it was never tuned for
well. The allocation **rate** is severe — that is B1, and it is a problem with what the
engine asks the collector to do, not with the collector.

**B8 · Correctness gates that cost whole suites.** Five of 15 suites scored nothing in
the committed results, each tracing to a small, *general* engine defect: `eval`
var-scoping (CodeLoad), `obj == null` running `ToPrimitive` (Crypto), `undefined + x`
string-concatenating (PdfJS), a dropped `for`-head comma expression (Typescript), a
missing `read` shell builtin (zlib). All five are fixed at `7ef80c03` and the pinned
pointer carries them.

Not one was exotic — four are core operator or scoping semantics any large real
program will hit. **That is the argument for keeping a retired benchmark in the loop:
Octane is 15 large real programs, and it found them.**

A structural point from the same change: the engine had no stack limit of its own,
only the CLR's probe, which fires when the stack is all but gone. .NET runs a catch
handler as a funclet *on top of* the frames it is handling, so the handler started
with no stack and its first call threw again — escaping the very `try` meant to catch
it. Octane's harness is literally `catch (e) { suite.NotifyError(e) }`, which is why
one overflowing benchmark took its entire suite down. The script host now sizes its
own thread (16 MiB) and opts into a reserve explicitly;
`JSContextOptions.MaxStackUsageBytes` correctly still defaults to 0 for embedders,
since a host that does not control its JavaScript thread's stack cannot pick a number.
