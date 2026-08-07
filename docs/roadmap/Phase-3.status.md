# Phase 3 — value representation — status

Everything phase 3 has measured: what was built, what it cost, what was refuted, and the
corrections each measurement forced on the plan.

> The evidence half of [`Phase-3.md`](Phase-3.md). **The plan document is the one to act
> from** — it carries each item's next action, size and exit gate, and links here for the
> argument. Nothing in this file is *closed*: [`Measurement.md`](Measurement.md) governs
> what may be claimed.

---

## Overview and targets, as the campaign recorded them

**Targets: Crypto (301×), zlib (340×), RayTrace (291×), EarleyBoyer (270×), Splay
(152×), NavierStokes (104×).** Blocker **B1** — the largest total win in the plan and
the largest change. Deliberately after phases 1 and 2 because those are contained and
this is not.

Owner assemblies: `Broiler.JavaScript.Storage`, `.Runtime`, `.Compiler`.

> **The order changed on a measurement.** 3-1 was written as the phase's opener because it looked
> like the most contained item covering the most benchmarks. Measured before starting (below), it
> is a 1:1 trade of write allocation for read allocation whose unambiguous half is live memory —
> and the same probe found a larger, strictly-cheaper target it was standing in front of: **an
> indexed access boxes its index**, ~32 B on every array read and write in the engine, with no
> read-side cost to removing it. That became **3-0**, and it goes first.

### 3-0 · Stop boxing the index of an indexed access — **landed, both halves**

**Measured, not proposed.** `a[0] = t` allocates **0.00 B/element** and `a[i] = t` allocates
**52.65**; the read side is **0.00** against **31.67**. The array, the element and the value are
identical across each pair — only the index expression differs — so ~32 B per access is the
index, and on the read path it is *the whole cost*. It is charged to every indexed access the
engine performs, on reference arrays as much as numeric ones, which is why it is worth more than
3-1 and costs less to take.

**Why it happens — confirmed at the source, and the code already said so.**
`FastFunctionScope` builds a numeric local's readable expression as `JSNumberBuilder.New(pe)`
over its raw `double` storage, under a comment that reads *"A numeric local's readable Expression
BOXES its storage, so every consumer that expects a JSValue keeps working"*. An index expression
was one of those consumers, so `a[i]` allocated a `JSNumber` purely to name a slot. The literal
form never did: `a[0]` lowers to a constant `uint` key, which is exactly why it measured at
0.00 B.

**What landed.** `JSValue.GetElementByNumber(double)` reads the element straight from the raw
double and `SetElementByNumber(double, JSValue)` writes it, emitted by `VisitMemberExpression`
and by a new `TryCreateNumericIndexStore` lowering for a computed key that resolves to a numeric
local. `--element-alloc`'s constant-index rows were already the floor both had to reach, and both
reach it:

| Site | Read before | Read after | Write before | Write after |
|---|--:|--:|--:|--:|
| `a[i] = i + 0.5` — numeric | 31.67 | **0.00** | 84.69 | **52.98** |
| `a[i] = t` — reference | 31.67 | **0.00** | 52.65 | **20.98** |
| `a[0] = t` — constant index (floor) | 0.00 | 0.00 | 0.00 | 0.00 |

**Indexed reads now allocate nothing at all**, and a write loses ~32 B. A write-once-read-once
numeric element goes **116.36 → 52.98 B (0.46x)** and a reference one **84.32 → 20.98 (0.25x)**.
What is left is exactly the two things 3-0 is not about: the value's own `JSNumber` (32 B, which
is 3-1's territory) and the amortized backing growth (~21 B). It applies to **reference arrays as
much as numeric ones**, which is the half a typed backing store could never have served.

**The guard is the item.** Only a non-negative integral double at most **2^32-2** names an array
index; everything else is an ordinary string-keyed property, so `a[1.5]` is the property `"1.5"`,
`a[-1]` is `"-1"`, and `a[4294967295]` is a string key — 2^32-1 being the one canonical numeric
string above the range. `-0` is deliberately admitted, because `ToString(-0)` is `"0"` and slot 0
is the right answer; NaN fails the lower comparison and every infinity fails the upper. **Each
rejection falls back to exactly the boxed path that ran before, so a guard that is too strict
costs an allocation and never a wrong answer** — which is what bounds the risk of the whole item.

**A guarded access is a CALL, and that is what shapes the item.** Three places need an index
*node* and therefore cannot have one: `CreateMemberAssignmentTarget` is assigned through,
`InternalUpdateExpression` switches on `right.NodeType` and takes a different branch for anything
that is not `BExpressionType.Index`, and a **compound** assignment reads and writes through a
single reference. So the fast path is offered to exactly two lowerings — the plain read and the
plain write — and `a[i] += v` keeps its boxed index; splitting it would evaluate the base twice
unless the whole form were rebuilt around a temp, which is more than this item. A test pins that
the base is still evaluated once.

*The first attempt hooked `CreateMemberExpression`, which is on the assignable path.* It compiles
cleanly either way — an expression tree only rejects the assignment later — so the callers had to
be read rather than the build trusted. Two of the three pass `computed: false` and so could not
have reached it, **but destructuring passes `property.Computed` straight through and would have.**

**The write path goes through `SetValue`, not the `uint` indexer, and that is deliberate.**
Measuring first turned up a pre-existing split in the error messages: `null[0] = 1` reports
*"Cannot get property 0 of null"* through `JSUndefined`'s `this[uint]` override, while
`null[i] = 1` reports *"Cannot set properties of null"* through the `JSValue` setter — because a
constant index has always lowered to a `uint` key and a variable one never did. Routing integer
indices to the `uint` indexer would have silently moved every variable index onto the other
message. Copying the `JSValue` setter's failure handling keeps both exactly where they were; a
test pins them, so reconciling that split later has to be a deliberate act rather than a side
effect.

**Verify.** 42 test cases in `NumericIndexKeyTests`, weighted to the keys the fast path must
*refuse*. Reads: the nine index values above, a hole still reaching the prototype chain, an index
accessor still running, string and typed-array receivers, a Proxy still seeing the key as a string
through its `get` trap, `'0'` and `'00'` staying distinct properties, and an optional-chain read
still taking the excluded path. Writes: each of the eight keys **read back through its string
form** rather than through the numeric local, so the two halves cannot agree on a shared bug; an
index setter running; a frozen array refusing silently in sloppy mode and throwing in strict; a
Proxy `set` trap seeing a string key; typed-array writes discarding an out-of-range index rather
than landing elsewhere; the null and undefined messages above; the assignment evaluating to its
right-hand side; and §13.15.2 ordering, proven with a right-hand side that reassigns the index
mid-assignment. Repository suite: **7 463 tests across 13 projects, 3 failures**, all three the
pre-existing win-x64 host-environment ones. **test262 over all four pinned manifests is unchanged
— 8 220 / 84 / 9, no test on a different side than before the item.**

**Size: M**, and it landed at that size.

### 3-1 · Unboxed backing stores for dense arrays — **re-measured; the storage half is re-opened as unmeasured, not refuted (§4.2a)**

> **The evidence that moved this item off storage came from a 7-of-15-suite corpus.** "Only 5.0% of
> NavierStokes' requests are a raw double crossing into a `JSValue`" is what retired the typed
> backing store, and it was never tested against **Gameboy** — a `Uint8Array` memory image with
> register arrays, never in any census, where the conversion is **51.0% of a 52.8 M-request
> workload** and which alone mints **more conversions (26.9 M) than the entire old corpus
> (24.6 M)**. The operator work this item did instead is real and measured and stands; what is
> withdrawn is the *refutation* of the storage half. See §4.2a. **This is not a claim the store is
> worth building** — that needs a wall-clock A/B nobody has run, and by `0086`'s rate lesson
> Gameboy's 1.96 GB over 23.8 s is a lower rate than NavierStokes' 0.49 GB over 2.0 s.
>
> **Since resolved on the axis that decides it** (`0113`): the read/write ratio the verdict rests on
> is now counted, and **Gameboy's is 1.03** — an allocation wash. *The suite that re-opened the item
> does not carry it.* The corpus is **3.34 reads per write**, NavierStokes 5.26 and Crypto 4.80, so
> a typed store is a net allocation loss of ~2.3 boxes per write. **The live-memory case stands and
> is now the whole of the item.**

#### The ratio the whole verdict rests on, counted — and it settles what §4.2a re-opened — `0113`

The item trades a write allocation for a read allocation, so its verdict is a ratio: *"a wash at a
1:1 read/write ratio, a win only when writes dominate, and a loss on read-heavy code"*. It then
asserts that its named targets *"read each element many times per write, which is the unfavourable
direction"*. **That is a claim about the corpus, and nothing counted it.**

Counted on the **dense path** — the population a typed store would serve, since a dictionary-kind
array is outside the item entirely — and split by whether the value is a **number**, because an
array of strings would make a corpus ratio describe arrays the item cannot help:

| Suite | numeric dense writes | numeric dense reads | **reads per write** |
|---|--:|--:|--:|
| NavierStokes — *the item's grid target* | 10 370 089 | 54 560 144 | **5.26** |
| Typescript | 338 606 | 1 723 870 | 5.09 |
| Crypto — *the item's digit-array target* | 8 670 639 | 41 589 626 | **4.80** |
| PdfJS | 16 442 985 | 39 097 665 | 2.38 |
| Box2D | 414 130 | 987 088 | 2.38 |
| **Gameboy** | 1 726 800 | 1 777 310 | **1.03** |
| **Splay** | 2 824 002 | 15 998 | **0.01** |
| **corpus** | **42 351 440** | **141 424 351** | **3.34** |

**The assertion was right and now has a number.** At 32 B boxed per read against 32 B saved per
write, a typed store is a **net allocation loss of about 2.3 boxes per write** over the corpus — and
worse on both suites the item names.

**And it settles what §4.2a re-opened.** That section withdrew the item's refutation because the
evidence retiring the typed store came from a corpus that never contained **Gameboy**, where the
raw-double-to-`JSValue` conversion is 51.0% of a 52.8 M-request workload. *Counted, Gameboy's dense
read/write ratio is 1.03* — **an allocation wash, not a win**. The suite that re-opened the item
does not carry it, and what survives is the **live-memory** argument the item already made and never
needed this measurement for.

**Splay is named rather than averaged away.** It is the one suite running the item's favourable
direction, and it runs it by two orders of magnitude: **2 824 002 writes against 15 998 reads**. A
corpus ratio hides that, which is why the table is per suite.

**Where.** `Broiler.JavaScript.Storage/ElementArray.cs` — `private IPropertyValue[] dense`.

P2-3 made each element one reference instead of a 32-byte descriptor, which was a real
win, but a dense array of a million doubles is still a million heap objects behind a
million interface references.

**Work.** A typed backing store (`double[]`, `int[]`) chosen on first store, with an
elements-kind tag on `ElementArray`, transitioning to `IPropertyValue[]` on the first
non-numeric write. Standard, well-understood machinery.

**Target.** Crypto's 28-bit digit arrays, NavierStokes' grids, and the
typed-array-shaped heaps in zlib, Mandreel and Gameboy. **The most contained item in
the phase and the one covering the most benchmarks** — which is why it goes first.

**Verify.** `test262-arrays` and `test262-binary-data`; `CompactElementStorageTests`,
`ElementDescriptorRoundTripTests`, `IndexedWriteAndLengthTests` for integrity levels,
foreign receivers, exotics and length-shrink. **Report allocation per element
alongside time.** **Size: L.**

#### Measured before starting — and it re-specifies the item

The item's premise is a claim about the **write** side, and it is true. What it does not say
is what the change costs on the **read** side, which is the half that decides it: the dense
store is `IPropertyValue[]` and every read hands back an `IPropertyValue`, so a raw `double[]`
cannot answer one without boxing a fresh `JSNumber`. 3-1 therefore *trades* a write allocation
for a read allocation, and the exchange rate had to be measured before anything was built.

`--element-alloc` (new; `ElementAllocationMetrics`), 100 000 elements, warmed then measured
after a forced gen2 collection, every row net of an inert no-array loop control:

| Site | Write B/element | Read B/element |
|---|--:|--:|
| loop control, no array access | 0.00 | 0.00 |
| `a[0] = t` — constant index, hoisted reference | **0.00** | **0.00** |
| `a[0] = i + 0.5` — constant index, fresh number | **32.00** | 0.00 |
| `a[i] = t` — variable index, hoisted reference | 52.65 | 31.67 |
| `a[i] = i + 0.5` — variable index, fresh number | **84.69** | **31.67** |
| `a[i] = i & 1023` — small integers | 84.32 | 31.67 |

**The 84.69 decomposes exactly, and only one third of it is what 3-1 removes:**

| Component | B/element | How the rows show it |
|---|--:|---|
| Boxing the **index** | ~32 | a constant index costs **0.00**; a variable one costs 32 more |
| Boxing the **value** | **32.00** | the constant-index-number row *is* this number, alone |
| Amortized backing growth | ~21 | 100 000 slots doubling from four ≈ 21 B/element |

**Three findings, and two of them change the plan.**

- **3-1's prize is 32 of 85 bytes on write, and it costs 32 bytes on every read.** Reads are
  free today — the value is already a heap object, so a read is a reference copy — and after a
  typed store each one boxes. On allocation the item is a **wash at a 1:1 read/write ratio, a
  win only when writes dominate, and a loss on read-heavy code.** Its named targets —
  NavierStokes' grids, Crypto's digit arrays — read each element many times per write, which
  is the unfavourable direction. **What survives unambiguously is live memory**: a resident
  `double[1e6]` is 8 MB against 8 MB of references plus 32 MB of `JSNumber`, so ~0.2x, and that
  is a real win for exactly those long-lived numeric heaps. **Re-specify 3-1 as a live-memory
  item whose throughput case is contingent on 3-4**, rather than as the phase's throughput
  opener.
- **The bigger contained win on array access is not the element store at all — it is that a
  variable index is boxed.** It costs ~32 B on every indexed read and every indexed write,
  whatever the array holds, and unlike a typed backing store removing it has **no read-side
  penalty**: it is pure removal, and it applies to reference arrays too. On the read path it is
  *the entire cost*. That deserves its own item ahead of 3-1.
- **The per-thread small-integer cache does not reach this path.** `a[i] = i & 1023` costs
  84.32 against 84.65 for large integers — a 0.33 B difference, i.e. none. So 3-1 buys the same
  32 B for small integers as for doubles, and P2-1's cache is not already collecting it here.
  Worth knowing before anyone sizes the integer case as already-solved.

*Neither the read-side cost nor the index boxing was visible from the item's text, and both
came out of one probe run. §3.5's rule about a premise not being a finding, applied to an item
that was right about its premise and wrong about its consequence.*

#### Re-measured for the promotion 3-8 gave it — and the two findings say it is a *precondition*, not an option

3-8 moved this item to the front of the phase on a census: **42.01% of the corpus's allocation is
number boxing** (corrected from 41.89% once the constructor was counted as well as the factory —
a builtin writing `new JSNumber(x)` directly turns out to be **0.3%** of all boxes, so the earlier
figure was a lower bound and barely one). That promotion sat against this item's own 2026 finding
that a typed backing store is *a wash*. Both are right, and reconciling them is what this item
needed before anything was built.

**The element chain decomposes exactly, and every term is a box the operators mint.** New
`provability` and `element` rows in `--local-alloc`, each running the identical arithmetic the
raw-double control runs at **0.00**:

| Site | B/iter | |
|---|--:|---|
| `local-read-only` — `s = s + v`, `v` a raw double | **0.00** | the floor |
| `element-read-only` — `s = s + a[0]` | 31.98 | one box: the add's result |
| `literal-static-operand` — `a[0] * 2` | 32.00 | one box |
| `literal-fresh-operand` — `a[0] * 1.5` | **64.00** | **two** — and the second one is the literal |
| `element-multiply-only` — `s = a[0] * 1.5` | 64.00 | |
| `element-read-constant-index` — `s = s + a[0] * 1.5` | 95.99 | = 64 + 32, exactly |
| `element-read-variable-index` — `a[i & 1023]` | 128.08 | + one box for `i & 1023` |
| `element-read-write-chain` — read, arithmetic, store back | 159.67 | five boxes, the NavierStokes kernel |

Every figure is an exact multiple of 32 and the composition checks out to the hundredth, which is
what says the model is right rather than approximately right. **The element store is not in any of
them.** The boxes are minted by the *operators*, and the element read is free today precisely
because the value it hands back is already a box. So 3-1's own verdict holds — a typed store alone
trades a write allocation for a read allocation — while 3-8's promotion also holds, for a reason
3-1 never stated: **the operators cannot stay unboxed while their operands come out of an array.**

**Two things fell out of that decomposition, and both were measured rather than argued.**

- **A numeric literal is re-boxed on every evaluation.** `VisitLiteral` has shared statics for
  NaN, 0, 1 and 2 and emits a factory call for everything else, so `a[0] * 1.5` allocates *two*
  boxes where `a[0] * 2` allocates one. Counted over the corpus through a separate factory entry,
  literals are **1 671 331 of 133 936 952 requests — 1.2%, and at most 2.0% of fresh boxes**. Real,
  exactly demonstrated, and too small to justify either a thread-shared constant (the small-integer
  cache is `[ThreadStatic]` for a stated reason) or a per-activation local per literal. **Recorded
  and not built**, with the number that says why.
- **The bitwise and shift operators had no native form, and the analysis had been proving them
  numeric all along.** `NumericLocalAnalysis.IsNumericBinary` lists `&`, `|`, `^`, `<<`, `>>` and
  `>>>`, so a local assigned `i & 1023` stays numeric — while `TryCreateNativeNumericValue` did
  not, so the value went out to a `JSValue` operator and came back. Measured:
  **`s = i + 1023` is 0.00 B/iter and `s = i & 1023` is 31.84**, with both operands raw doubles and
  the result stored straight into one. *The analysis proved something the emitter could not use.*

#### The bitwise half is built, and its corpus result is the finding

The exclusion had a real reason, and it is why the operators live in `JSNumericOperators` rather
than as `BExpression` nodes: a bitwise operand is not the double but `ToInt32`/`ToUint32` of it
(§7.1.5/§7.1.6) — truncated toward zero, reduced modulo 2^32, NaN and the infinities mapping to 0
— and that reduction is **not** a CLR cast, which is undefined on overflow rather than wrapping.
Routing all six through `JSValue.ToUint32`, the same helper `IntValue` uses, makes them identical
to the boxed operators by construction. On its shape it removes the box completely:

| Site | native | generic |
|---|--:|--:|
| `bitwise-on-numeric-locals` — `s = i & 1023` | **0.00** | 31.84 |
| `element-read-variable-index` | **96.25** | 128.08 |
| `element-read-write-chain` | **96.00** | 159.67 |

**On the corpus it removes nothing.** Six of the seven suites come back with the box count
identical to the digit — a difference of exactly zero — and the seventh is **Crypto**, a
BigInteger implementation built on `&`, `|` and `>>` that mints 42.4 M boxes, 55% of its own
allocation, where the two arms differ by 3 126 in the *wrong* direction. That is not a result
either: running the **same arm twice** gives 42 418 727 and 42 421 217, so Crypto's own
run-to-run variation is larger than the gap between the arms. (It generates RSA keys, so its work
is not fixed across runs — worth knowing before quoting any Crypto delta, and the reason the
census figures elsewhere in this item are quoted per suite rather than to the digit.) The reason is the whole point of this item: the native form is chosen when **both**
operands are native, and Crypto's digits live in `this.array[i]`. An element read is not a numeric
local, so the operator it feeds is never eligible, however good its native form is.

*That is item 3-5's finding — "the emission is fine; what is on the other side is not" — arriving
for the second time from a different direction, and it is the sharpest evidence this phase has
produced about its own ordering.* Six items have now built machinery that array-resident data
cannot reach: unboxed indices (3-0), unboxed locals in four categories (3-3), an unboxed
comparison (3-5), a captured raw cell (3-7), and now unboxed bitwise operators. Every one of them
is correct, every one is invisible on the corpus, and **every one of them is waiting on the same
thing.**

**Shipped on by default, with `BROILER_JS_NATIVE_BITWISE=0` to restore the generic operators**, on
3-5's terms: 15 test cases pinning ToInt32 wrapping, NaN and both infinities, shift-count masking,
`>>>`'s unsigned result, the coercions a non-numeric operand must still get and a getter that must
run exactly once — **every one asserted on both settings of the switch**.

#### What the operators are handed at run time — counted, and it moves the item off storage

Everything above says the boxes are minted by the **operators**, whose operands arrive boxed from
array elements and object fields. What nobody had counted is the half that decides whether any
fast path can be fed: **how often a generic operator's two operands are already Numbers.** A native
form guarded on that test reaches exactly those invocations and no others — and this item has paid
for skipping that count once already, with a bitwise emission that is correct on 15 semantics cases
and removes zero boxes.

`ArithmeticOperandDiagnostics` (new, off by default, one counter pair on each generic
arithmetic and bitwise operator) over the seven-suite driver:

| Suite | generic invocations | both operands Numbers | share | not both | boxes allocated |
|---|--:|--:|--:|--:|--:|
| Richards | 55 198 | 55 197 | 100.00% | 1 | 13 659 |
| DeltaBlue | 14 533 | 14 532 | 99.99% | 1 | 6 765 |
| RayTrace | 844 051 | 844 044 | 100.00% | 7 | 841 017 |
| Box2D | 15 916 294 | 15 916 279 | 100.00% | 15 | 11 434 706 |
| EarleyBoyer | 26 116 | 26 112 | 99.98% | 4 | 563 997 |
| Crypto | 39 867 896 | 39 866 794 | 100.00% | 1 102 | 42 410 739 |
| NavierStokes | 17 094 558 | 17 094 557 | 100.00% | 1 | 29 977 465 |
| **Total** | **73 818 646** | **73 817 515** | **100.00%** | **1 131** | **85 248 348** |

**Every generic arithmetic invocation on the corpus but 1 131 arrives with two Numbers**, and that
population is **86.6% of every box the corpus allocates**. Nine hundred and seventy-four of the
1 131 exceptions are one suite's. So the guard a speculating native form needs is not a
coin-toss — it is a branch that predicts perfectly, and the type test costs a compare.

**The number next to it is the one that re-specifies the item.** The compiler's own proof —
`isLeftNumber && isRightNumber`, the gate every phase-3 item so far has widened — reaches
**556 053 of 73 818 646 invocations, 0.75%**, and even that figure is generous: it counts the
`AddValue(double)` overload, and **`+` is the only operator that has one.** `-`, `*`, `/` and `%`
re-box a raw double they already hold in order to meet the `JSValue` operator. *Compile-time
provability reaches 0.75% of the arithmetic; run-time truth reaches 100.00% of it.* Six landed
items have widened the first number.

**So the shared half is not a storage change, and 3-1's own re-specification above needs one more
correction.** "Storage plus an unboxed element read" assumes the problem is where the value is
kept. It is not: the operator already receives two Numbers whatever they are stored in. What it
cannot do is **hand one back** — its consumer is a `JSValue` local, slot or element, so the result
is boxed at the root of every expression. The shared half is therefore a **run-time-guarded
specialization of an arithmetic expression tree**: evaluate each leaf once, test the leaves for
Number, compute the whole tree in raw doubles, and box only the root. The per-shape figures already
in this item say what that is worth — `s = s + a[0] * 1.5` is **96 B, three boxes**, of which two
are intermediates, and the read-modify-write chain is **159.67 B, five boxes**, of which four are.
A typed backing store then becomes what it always measured as — a live-memory item — rather than
the precondition.

**And it partly reverses item 3-8's "do not start as written", without contradicting it.** 3-8
priced a run-time numeric guard at the **local** and found the whole local tier worth 0.36% of the
corpus's boxing. That verdict stands, because it is about the local. The same speculation applied
at the **operator** reaches 86.6% of the boxes. The two measurements are of different things and
this document had only ever taken the first: *3-8 measured where the guard was proposed; this
counts where the boxes are minted.*

**One smaller finding, and it corrects a reading of this item's own table.** A numeric literal is
**already** a native double — `ToNativeExpression` returns one — so the second box in
`a[0] * 1.5` is not "the literal being re-boxed by `VisitLiteral`" as an operand of that
multiply. It is the compiler boxing a raw double it already holds, because `*` has no
`JSValue × double` overload. The literal-re-boxing finding stands on its own count (1.2% of
requests) and is a different site.

*The counter's first version read **zero** on all seven suites, against 85 M boxes. The enable had
been inserted next to the wrong one of two identical `NumberBoxingDiagnostics.Reset()` lines — one
in a call probe, one in the driver — so the driver never turned it on. §3.5's "check that the
thing you measured is the thing you built", from the third direction: not a stale binary, not a
binary being rewritten, but an instrument switched on in the wrong method. A counter reading zero
is a claim about the counter first.*

#### The shared half is built: a guarded numeric tree

The census says the operators are handed two Numbers essentially always and cannot hand one back.
So the build is not a storage change and not a widening of the compile-time proof — it is a
**run-time-guarded specialization of an arithmetic expression tree**: evaluate each leaf once into a
temporary, test the leaves for Number, compute the whole tree on raw doubles, and box only the
root. `NumericSpeculation` / `FastCompiler.NumericSpeculation.cs`, on by default, with
`BROILER_JS_NUMERIC_SPECULATION=0` restoring the unguarded emission.

**Evaluation order is the whole correctness argument, and it is what makes the rule narrower than
the census.** The ordinary emission evaluates a node's two operands and *then* coerces them, so in
a nested tree a coercion runs between two leaf evaluations — and a coercion is observable, because
`ToPrimitive` on an object runs `valueOf`. Hoisting every leaf ahead of the test would move later
leaves in front of that coercion. A tree is therefore eligible only when **every leaf evaluated
after the first internal node in postorder is one that can neither cause nor observe anything** — a
numeric literal or a proven-numeric local. Leaves *before* the first coercion are unrestricted, and
that is not a corner: JavaScript's precedence makes `s + a[0] * 1.5` parse right-leaning, so all
three of its leaves precede the multiply. `(a[0] * 2) + p.v` does not, and is refused.

**When the guard holds nothing is skipped**, which is what makes the two arms the same program:
every operator here applies ToNumeric (for `+`, ToPrimitive then ToNumeric) to both operands, and
on a Number each is the identity. The native forms are `TryCreateNativeNumericValue`'s — the same
ones the all-native path emits — so the arms are identical by construction rather than by
inspection, the argument this item's bitwise half already used for `ToUint32`.

**Measured on the corpus**, one build, `BROILER_JS_NUMERIC_SPECULATION` the only difference:

| Suite | generic invocations off → on | | boxes allocated off → on | | trees |
|---|--:|--:|--:|--:|--:|
| Richards | 55 198 → 49 204 | 0.891 | 13 659 → 10 743 | **0.787** | 12 |
| DeltaBlue | 14 533 → 13 933 | 0.959 | 6 765 → 6 765 | 1.000 | 6 |
| RayTrace | 844 051 → 796 759 | 0.944 | 841 017 → 823 293 | 0.979 | 27 |
| Box2D | 15 916 294 → 13 870 770 | 0.871 | 11 434 706 → 10 663 471 | **0.933** | 424 |
| EarleyBoyer | 26 116 → 79 | **0.003** | 563 997 → 563 997 | 1.000 | 129 |
| Crypto | 39 872 917 → 23 249 552 | **0.583** | 42 412 174 → 33 356 341 | **0.786** | 191 |
| NavierStokes | 17 094 558 → 15 373 914 | 0.899 | 29 977 465 → 29 423 391 | 0.982 | 73 |
| **Total** | **73 823 667 → 53 354 211** | **0.723** | **85 249 783 → 74 848 001** | **0.878** | **862** |

**10 401 782 boxes removed — 12.2% of everything the corpus allocates, from 862 compiled sites.**
That is the first corpus-visible allocation result phase 3 has produced: 3-0, 3-3, 3-5, 3-7 and the
bitwise half of 3-1 moved **0.36% between them**, and this is thirty times that from one change.

**And it is well short of the 86.6% the census set as the ceiling, which the per-suite column
explains rather than excuses.** Crypto is the case the mechanism was built for — 0.583× of its
generic invocations, 0.786× of its boxes. **NavierStokes is not**: it loses 10.1% of its generic
invocations and **1.8%** of its boxes, so the great majority of its 30 M boxes are minted somewhere
that is not a binary arithmetic operator. EarleyBoyer is the sharpest version of the same thing —
**99.7% of its generic invocations removed and not one box** — because what it was doing there was
not allocating in the first place. *The census bounded what the operators could reach; it did not
say the boxes were all at the operators, and two suites now say plainly that they are not. That is
the next count, and it should be taken before anything else in this item is built.*

**One mistake, caught by measuring, and it is the item's own rule about populations arriving from
the other side.** The first eligibility condition required **two operators**, on the argument that a
single node mints one box either way — true of the *result*, and wrong, because it forgets the
*operand*: `a[0] * 2` costs two boxes today, the literal and the result, since only `+` has a
`JSValue × double` overload. Measured, that condition took the corpus from 10.4 M boxes removed to
**5.6 M — Crypto alone lost 4.7 M**. The condition that ships counts what the guard actually buys:
`(operators − 1) + native leaves ≥ 1`, i.e. one intermediate that never becomes a `JSValue`, or one
already-unboxed operand that no longer has to be boxed to meet a generic operator. *A savings rule
is a claim about the code and has to be measured like one; this one was reasoned and lost half the
prize.*

**And the wall clock, measured — with a control that comes free.** ABBA-interleaved at process
granularity, six pairs, one build, the switch the only difference, and the diagnostics counters
**off** for the timing pass (they had been enabled around the driver unconditionally; leaving them
would have charged the slower arm for 20.5 M interlocked increments it does not otherwise pay — a
bias pointing the same way as the result, which is the worst kind). The control is the corpus's
own: **DeltaBlue and EarleyBoyer remove exactly zero boxes** between the arms, so their time must
not move, and their spread is the noise floor.

| Suite | off (median) | on (median) | ratio | pairs favouring | |
|---|--:|--:|--:|--:|---|
| **Crypto** | 3 554 ms | 3 241 ms | **0.912×** | **6 of 6** | 0.857–0.961, entirely below 1 |
| Box2D | 6 652 ms | 6 596 ms | 0.991× | 5 of 6 | |
| NavierStokes | 1 929 ms | 1 890 ms | 0.982× | 4 of 6 | inside the noise |
| RayTrace | 2 327 ms | 2 260 ms | 0.972× | 3 of 6 | inside the noise |
| Richards | 689 ms | 690 ms | 0.995× | 3 of 6 | |
| DeltaBlue | 1 392 ms | 1 388 ms | 1.005× | 3 of 6 | **control** |
| EarleyBoyer | 3 805 ms | 3 808 ms | 1.006× | 2 of 6 | **control** |
| **Driver total** | **20 360 ms** | **19 916 ms** | **0.981×** | **6 of 6** | 0.946–0.994 |

**0.981× on the driver with six of six pairs, carried by Crypto at 0.912× with six of six**, and
the two control suites sit at 1.005× and 1.006× on 3-of-6 and 2-of-6 — they do not move, which is
what makes the rest readable. Their pair spread is ~11%, so **no per-suite effect under about 5%
can be called from this run**: Box2D, NavierStokes, RayTrace and Richards are all directionally
right and individually unproven. **No suite is slower.** The guard's losing side — a tree whose
operands turn out not to be Numbers pays a type test and then does what it did before — does not
show up anywhere, which is what the census predicted when it found 1 131 non-Number invocations in
73.8 M.

*The ratio between the two measurements is worth more than either.* **12.2% of the corpus's
allocation removed buys 1.9% of its execution time.** Allocation is not the dominant term in what
this engine spends, and that bounds the rest of phase 3 the way item 4-2b's 0.83% bounded phase 4:
the remaining boxes are worth having, and nobody should expect the next 12% of them to be worth
more than another ~2%.

**Verify.** `NumericSpeculationTests` — 33 cases, **each asserted on both settings of the switch**,
so each is a statement about JavaScript semantics rather than a description of the fast path. Values
(NaN, both infinities, −0 through `1/x`, `%` on infinity, ToInt32 wrapping and shift masking for all
six bitwise operators); types (a string leaf still concatenates under `+` and still coerces under
`*`, an object leaf still runs `valueOf`, a BigInt still throws on mixing, `null`/`undefined`/
booleans coerce as before); and **order** — a getter read exactly once and left to right, a `valueOf`
that mutates a later leaf, a getter that mutates a later leaf, and a throwing leaf that must stop
the next one being read. Plus two counter assertions, because every one of the 33 also passes when
the specialization never fires: one that the shape the item is written around *does* specialize, and
one that `(a[0] * 2 * 3) + p.v` is refused **by guard count** — one guarded leaf, not two — which
distinguishes "the root was refused" from "nothing was eligible", since the inner tree specializes
on its own. Full repository suite **7 963 tests, 0 failures**.

#### Where the remaining boxes are minted — every one of them, and it is not what this item assumed

The guarded tree left **74 835 575 boxes from 111 997 550 factory requests**, and the reading that
suggests itself is that they are **root** boxes: the value of a tree on its way into a `JSValue`
slot or element, which is exactly what a typed backing store — this item as originally written —
would remove. That is a hypothesis, and it is cheap to test: give the compiler's boxing conversion
its own factory entry (`JSNumber.CreateConversion`, counted apart from `Create` and `CreateLiteral`
exactly as the literal entry already is) and ask what share of a run's requests it is.

It is **18.4%**, and the first version of this section stopped there and called the hypothesis
falsified. That was half an answer: it left **40.5% of the corpus's requests attributed to nothing
at all**, which by §3.5 is a claim about the census, not about the engine. Two counters closed it.
`JSValue.BitwiseXor` turned out to be the one generic binary operator `0083` never hooked — a real
gap, though the corpus says a small one, below the run-to-run spread. The rest was **the unary
operators, which no census had ever looked at**: `-x` and `~x`, the `++`/`--` step, and the
`ToNumeric` that coerces the operand of `++`/`--`. That takes the unattributed share from **40.5%
to 1.0%**, measured in the arm that is *built* — the guarded tree on:

| Source | requests | share | what it is |
|---|--:|--:|---|
| Binary operators | 53 351 878 | 47.6% | what `0083` counted and `0084` consumes |
| **`++` and `--`** | **34 562 464** | **30.9%** | 17 281 232 steps and 17 281 232 `ToNumeric` coercions |
| Compiler conversion | 20 601 685 | 18.4% | a raw double crossing into a `JSValue` |
| Numeric literal | 1 671 314 | 1.5% | already native; re-boxed to meet an operator |
| Unary `-` and `~` | 702 031 | 0.6% | |
| **Unnamed** | **1 108 178** | **1.0%** | builtins reaching the factory directly |

**The root-box hypothesis is wrong, and most clearly wrong on the suite it was invented for.**

| Suite | requests | conversion | | binary | | `++`/`--` | | unnamed |
|---|--:|--:|--:|--:|--:|--:|--:|--:|
| **NavierStokes** | 36 669 153 | 1 827 793 | **5.0%** | 15 373 914 | 41.9% | **18 923 532** | **51.6%** | 1.0% |
| **Crypto** | 55 322 471 | 17 126 896 | **31.0%** | 23 247 219 | 42.0% | 14 417 806 | 26.1% | 0.3% |
| Box2D | 17 382 495 | 1 407 419 | 8.1% | 13 870 770 | 79.8% | 574 746 | 3.3% | 1.5% |
| EarleyBoyer | 756 617 | 114 468 | 15.1% | 79 | 0.0% | **608 502** | **80.4%** | 4.4% |
| RayTrace | 1 628 284 | 70 991 | 4.4% | 796 759 | 48.9% | 0 | 0.0% | 15.2% |
| Richards | 90 589 | 8 789 | 9.7% | 49 204 | 54.3% | 31 116 | 34.3% | 0.0% |
| DeltaBlue | 147 941 | 45 329 | 30.6% | 13 933 | 9.4% | 6 762 | 4.6% | 51.5% |

Only **5.0%** of NavierStokes' requests are the compiler carrying a raw double across into a
`JSValue`, so a typed backing store — which is what would remove those — cannot be why its boxes
survive. The suite where conversions *are* a large share is **Crypto at 31.0%**, and Crypto is the
one the guarded tree already served best. *The two suites are the opposite way round from the way
this item has assumed since it was written.* The conversion column is in fact the **ceiling on what
a typed store can remove without further operator work**, because where the tree already computes
natively the root is counted there: 5.0% on NavierStokes against 31.0% on Crypto.

**And the largest single source on the corpus's biggest boxer is `++`.** NavierStokes spends
**51.6%** of its boxing on increments and decrements, EarleyBoyer **80.4%**, the corpus **30.9%** —
more than the compiler conversion and the numeric literal together, and two thirds of what the
binary operators cost. **Exactly half of it is waste that is visible in four lines of source.**
`ToNumeric` ends `primitive.IsBigInt ? primitive : CreateNumber(primitive.DoubleValue)`, so an
operand that is *already* a `JSNumber` is copied into a second, equal `JSNumber` to be handed back
as the old value — and a JavaScript Number has no observable identity, so the copy can never be
detected. **17 281 232 requests, 15.4% of the corpus's boxing, for a value the engine is already
holding.** That is the next build, and it is the cheapest one this phase has surfaced: it is a
guard, not a mechanism.

*This is the third time in one item that a plausible mechanism has been checked and come back
wrong — the ceiling table, the "two operators" savings rule, and now the root-box hypothesis — and
the first time a residue was chased instead of rounded off. The 40.5% that "came from nowhere" was
not noise and not builtins; it was the operator every one of these suites runs most often, sitting
outside the census because the census was written around binary arithmetic. Each correction took
one counter and about ten minutes.*

#### The `ToNumeric` copy, removed — built straight off the census

`ToNumeric` coerces the operand of `++`/`--` and hands back the coerced old value, and it minted
unconditionally. So `n++` on a Number copied the Number into a second, equal `JSNumber`. **Reusing
it is sound because a JavaScript Number has no observable identity** — it compares by value, it
cannot carry a property, and `Object.is` on two Numbers is a value comparison — which is the same
argument the small-integer cache has rested on since P2-2, where unrelated call sites are already
handed the same instance. The guard is `primitive.IsNumber`, not `!primitive.IsBigInt`: a String,
a Boolean, `null` and `undefined` all reach this line and all still have to be coerced, which is
the whole reason `ToNumeric` exists (`"1"++` yields the Number 1, not the String).

Measured on the corpus, one build, `BROILER_JS_NUMERIC_UPDATE_REUSE` the only difference:

| Suite | requests | | boxes allocated | | of the removed requests, real |
|---|--:|--:|--:|--:|--:|
| **NavierStokes** | 36 669 153 → 27 207 387 | **0.742×** | 29 423 391 → 22 665 084 | **0.770×** | 6 758 307 of 9 461 766 — 71.4% |
| **EarleyBoyer** | 756 617 → 452 366 | **0.598×** | 563 997 → 282 000 | **0.500×** | 281 997 of 304 251 — 92.7% |
| Richards | 90 589 → 75 031 | 0.828× | 10 743 → 6 852 | 0.638× | 3 891 of 15 558 — 25.0% |
| Crypto | 55 327 970 → 48 114 381 | 0.870× | 33 357 396 → 33 352 279 | 1.000× | 5 117 of 7 213 589 — **0.1%** |
| Box2D | 17 382 495 → 17 095 127 | 0.983× | 10 663 471 → 10 661 949 | 1.000× | 1 522 of 287 368 — 0.5% |
| DeltaBlue | 147 941 → 144 560 | 0.977× | 6 765 → 6 765 | 1.000× | 0 of 3 381 — 0.0% |
| RayTrace | 1 628 284 → 1 628 284 | 1.000× | 823 293 → 823 293 | 1.000× | no updates at all |
| **Total** | **112 003 049 → 94 717 136** | **0.846×** | **74 849 056 → 67 798 222** | **0.906×** | 7 050 834 of 17 285 913 — 40.8% |

**17 285 913 requests removed, 15.4% — the census predicted 17 281 232, so the thing built is the
thing measured to 0.03%.** In allocations it is **7 050 834, 9.4%**, and *the gap between those two
numbers is the small-integer cache, which is the most useful thing in the table*: Crypto removes
7.2 M requests and **5 117 boxes**, because its updates are loop counters inside `[-128, 1024]`
where P2-2 was already answering them for free. NavierStokes' indices run past that bound, so
**71.4% of its removed requests were real allocations — 6.76 M boxes, 23.0% of everything it
allocates.** *A `++` on a small integer was already free; a `++` on anything larger was not, and
nothing before this said which suites were which.*

Set against the guarded tree's 10 401 782, this is **7 050 834 from a nine-line guard** — and it
lands on the suite the tree could not reach, NavierStokes, which the tree moved 1.8% and this moves
23.0%. **Together the two take the corpus from 85 255 034 boxes with neither switched on to
67 798 222 with both, 0.795×.** Five
coercions still mint on the reuse arm, which is the guard discriminating rather than a leak: those
operands are not Numbers.

**Wall clock, ABBA-interleaved at process granularity, six pairs, counters off for the timing pass
— and it is the sharpest reading phase 3 has produced, because of what it says about the suites
that did *not* move:**

| Suite | boxes removed | of its own | removed per second | median | pairs won |
|---|--:|--:|--:|--:|--:|
| **NavierStokes** | 6 758 307 | 23.0% | **4 240 469/s** | **0.906×** | **6 of 6** |
| EarleyBoyer | 281 997 | **50.0%** | 82 504/s | 1.002× | 3 of 6 |
| Richards | 3 891 | 36.2% | 5 842/s | 1.121× | 1 of 6 |
| Crypto | 5 117 | 0.0% | 1 767/s | 0.984× | 3 of 6 |
| Box2D | 1 522 | 0.0% | 255/s | 1.030× | 2 of 6 |
| RayTrace | 0 | 0.0% | 0 | 0.997× | 3 of 6 |
| DeltaBlue | 0 | 0.0% | 0 | 1.029× | 3 of 6 |
| **Driver total** | 7 050 834 | 9.4% | | **1.013×** | 2 of 6 |

**One suite moves: NavierStokes, 0.906× on six of six pairs, every pair between 0.862 and 0.928.**
The controls hold — RayTrace removes nothing and reads 0.997× — and **the driver total does not
move at all**, which the arithmetic predicts rather than contradicts: NavierStokes is 8.7% of the
driver, so 9.4% of it is **0.82% of the total**, under the total's own spread. Richards' 1.121× is
**not callable in either direction**: its own off-arm spread is 11.2% against a 12.1% effect, and
believing it would price 3 891 boxes at 18 µs each.

***The share of a suite's own boxes predicts nothing; the absolute rate predicts everything.***
EarleyBoyer **halves** its boxing — the largest proportional cut in the table — and reads 1.002×,
because 282 000 boxes over 3.4 s is 82 000/s. NavierStokes removes a smaller *share*, 23.0%, at
**fifty times the rate**, and is the only suite that moves. Every row in the table orders by rate
and none of them orders by percentage. That retires a habit this document has had since phase 3
opened — quoting a per-suite percentage of boxes as though it forecast time — and it sharpens
3-5's ceiling and 0084's *"12.2% of allocation buys 1.9% of time"* into something usable: **an
allocation item pays where the allocation rate is high in absolute terms, and nowhere else.**
NavierStokes mints 18.5 M boxes a second; EarleyBoyer mints 165 000. They are not the same kind of
problem and no single corpus figure can describe both.

`NumericUpdateReuseTests` — 9 fixtures, **each on both settings of the switch**, so every one is a
statement about JavaScript semantics rather than a description of the fast path: postfix and prefix
results, the non-Number operands that must still be coerced, NaN and the infinities, `-0` asserted
through both `1/x` and `Object.is` (it cannot survive the increment — `-0 + 1` is `1` — so the half
that matters is the old value), a `valueOf` that must run exactly once, a getter read once with the
setter seeing the increment, BigInt, the `Symbol` TypeError, and an element update. Plus **the
identity argument asserted rather than assumed** — `===`, `==`, `Object.is` and a property write
against a reused old value — and a counter invariant, because "the box count went down" would
otherwise be consistent with the coercion having stopped happening: `UnaryToNumeric +
UnaryToNumericReused` is equal on both arms and only the split moves.

**This build also broke one of `0085`'s own fixtures, which is the fixture working rather than a
cost.** `AnUpdateOnAPropertyCostsTwoBoxesNotOne` asserted the two-box cost, so it failed in all
three of the first suite runs the moment the reuse landed. It is now a Theory on both settings
asserting the invariant instead of the total — the **coercion count stays 1 either way**, and only
which side of the split it falls on moves. *A census fixture that survives the change it measured
would not have been measuring it.* Full repository suite **7 988 tests, 0 failures, on three
consecutive runs**.

**One caveat recorded rather than rounded off.** `Broiler.JavaScript.Integration.Tests` **stalled
once** on an earlier build of this stack — its host measured at *one jiffy of CPU over eight
seconds*, which is a hang and not slow progress. It did **not recur in six subsequent full-suite
runs**, three of them under `--blame-hang --blame-hang-timeout 300s` with no sequence file
produced, nor when that assembly was run alone, where it passed 4 571 in 47 s. It was not resource
exhaustion (27 GB disk and 13.7 GB RAM free, no stray processes). **Unexplained, not reproduced,
and not attributed to this change**; if it returns, `BROILER_JS_NUMERIC_UPDATE_REUSE=0` restores
the previous behaviour exactly and is the bisection. Separately,
`CapturedNumericLocalTests.SuspendingNestedFunctionsCaptureThroughTheSameBox` — the async-scheduling
intermittent 3-7 already records as unresolved — failed **once in those six runs**, which is the
first rate this document has for it.

#### Why the guarded tree reached a third of the census's ceiling — counted, not guessed

`0084` removed 12.2% of the corpus's boxes against a census ceiling of 86.6%, and its own section
said the per-suite column explained the gap: NavierStokes lost 10.1% of its generic invocations,
EarleyBoyer 99.7% and no boxes. What it did **not** say is *which of the six eligibility conditions
was doing the refusing* — the item had a numerator and no denominator, and by §3.5 that is a claim
about the instrument.

The waterfall it needed is the one item 3-6 already uses for hoisted names: attribute each
candidate to the **first** condition it fails, so the counts add up and each reads as *"widen this
and that many sites move"*. A candidate is a binary node whose operator has a native form —
counting anything else would put every `===` and `&&` in the denominator. One caveat the counter
has to be read with: a refused root **re-offers its children**, because `VisitBinaryExpression`
falls through to visiting the operands, so a refused chain contributes several rows.

| Suite | Specialized | AlreadyNative | NoSaving | **OrderUnsafe** | StringLeaf | With/eval |
|---|--:|--:|--:|--:|--:|--:|
| Richards | 12 | 1 | 22 | 11 | 1 | 0 |
| DeltaBlue | 6 | 1 | 13 | 9 | 4 | 0 |
| RayTrace | 27 | 1 | 126 | 77 | 1 | 0 |
| Box2D | 424 | 11 | 2 258 | 1 470 | 2 | 0 |
| EarleyBoyer | 129 | 16 | 111 | 36 | 2 | 2 |
| Crypto | 191 | 1 | 141 | 97 | 1 | 0 |
| NavierStokes | 73 | 9 | 47 | 62 | 1 | 0 |
| **Total** | **862** | **40** | **2 718** | **1 762** | **12** | **2** |

**862 of 5 396 candidate nodes specialize — 16.0%.** The two rules that turn down the rest are
`NoSavingToMake` (50.4%) and `OrderUnsafe` (32.7%), and *they are one finding rather than two*.
`+` is left-associative, so `a[0] + a[1] + a[2] + a[3]` parses left-leaning: the root is refused as
order-unsafe, its left child is refused as order-unsafe, and the bottom node — a single operator
over two unprovable leaves — is refused for having no saving to make. **A chain of *k* operators
produces *k−1* OrderUnsafe rows and one NoSaving row and specializes nothing.** The savings rule is
correct wherever it fires on a genuinely standalone `x op y`; most of the time it is firing on the
residue of a chain the order rule already declined.

**And the sub-census says the order rule is not refusing what this phase assumed it was.** Recorded
at the first blocking leaf, one-for-one against the OrderUnsafe row:

| Blocking leaf | count | share |
|---|--:|--:|
| A named property read, `o.x` | **1 028** | **58.3%** |
| An identifier that is not a proven-numeric local | 593 | 33.7% |
| Anything else | 83 | 4.7% |
| **A computed element read, `a[i]`** | **34** | **1.9%** |
| A call's return value | 24 | 1.4% |

*The element read is 1.9%.* Phase 3 has spent six items on the premise that array-resident data is
what its machinery cannot reach, and on this rule the blocked leaf is an **object field** — Box2D
alone contributes 984 of the 1 028. NavierStokes, the suite whose arrays the premise was written
about, is blocked 18 times by an element and 39 times by a plain name. **The order rule is not an
array problem and widening it is not a storage change**, which is the third time this item has had
a mechanism checked and come back pointing somewhere else.

#### The guard moves to where the coercion was — and that is the whole fix

The hoisting form is bounded by the fact that it *hoists*: every leaf is evaluated into a temporary
ahead of one combined test, so a leaf that moves in front of a coercion has to be one that can
neither cause nor observe anything. Nothing requires the leaves to move. Emitting each leaf at its
own postorder position and putting the test **where the coercion it stands in for would have run**
preserves the reference order exactly, and then the purity rule has nothing left to protect.

`NumericTreeOrdering` / `CreateOrderedNumericTree`, on by default, with
`BROILER_JS_NUMERIC_TREE_ORDER=0` restoring the hoisting form gate and all — two emitters rather
than one because the difference has to be attributable, and comparing against
`BROILER_JS_NUMERIC_SPECULATION=0` would charge this change for everything `0084` does.

**The soundness argument is `0084`'s, read from the other end.** The reference emission evaluates a
node's two operands and then coerces them, so the left operand's coercion runs *after* the right
operand is evaluated and *before* anything above the node is. This emits the leaves in that same
order and tests at that same point. When the test holds, the coercion it replaces was the identity
— ToNumeric of a Number is that Number — so nothing observable is skipped. When it fails, the same
generic operator runs at the same point over the values already in hand.

**What it costs is a two-armed node instead of a two-armed tree.** Each internal node carries a
`bool` saying its subtree stayed numeric, a raw `double` holding the value when it did and a
`JSValue` holding it when it did not, and one branch. So a failure part-way up no longer discards
the native work below it: the accumulated double is boxed once, at the node that failed, and the
rest of the tree proceeds generically — which the hoisting form cannot do, since its fallback is
the whole generic tree.

**Measured on the corpus**, one build, `BROILER_JS_NUMERIC_TREE_ORDER` the only difference — so the
`off` column is `0084`+`0086` as they ship and every removal below is this change alone:

| Suite | generic invocations off → on | | boxes allocated off → on | | trees |
|---|--:|--:|--:|--:|--:|
| Richards | 49 204 → 49 204 | 1.000 | 6 852 → 6 852 | **1.000** | 12 → 12 |
| DeltaBlue | 13 933 → 1 | 0.000 | 6 765 → 6 732 | 0.995 | 6 → 10 |
| RayTrace | 796 759 → 347 614 | 0.436 | 823 293 → 481 887 | **0.585** | 27 → 40 |
| Box2D | 13 870 770 → 4 152 413 | 0.299 | 10 661 949 → 5 225 033 | **0.490** | 424 → 1 090 |
| EarleyBoyer | 79 → 79 | 1.000 | 282 000 → 282 000 | **1.000** | 129 → 128 |
| Crypto | 23 249 298 → 338 328 | **0.015** | 33 349 915 → 13 412 191 | **0.402** | 191 → 208 |
| NavierStokes | 15 373 914 → 1 738 413 | 0.113 | 22 665 084 → 11 747 635 | **0.518** | 73 → 75 |
| **Total** | **53 353 957 → 6 626 052** | **0.124** | **67 795 858 → 31 162 330** | **0.460** | **862 → 1 563** |

**36 633 528 boxes removed — 54.0% of everything the corpus allocates — and 87.6% of the generic
arithmetic invocations that were left.** Set against the rest of the phase: `0084` removed 12.2%,
`0086` 9.4%, and 3-0, 3-3, 3-5, 3-7 and the bitwise half **0.36% between them**. Taken from the
baseline before any of the three, the corpus goes **85 255 034 → 31 162 330, 0.366×**.

**The refusal waterfall is the check that it happened for the stated reason** rather than by some
other route: OrderUnsafe **1 762 → 0**, and NoSavingToMake **2 718 → 1 181** without that rule being
touched at all — which is the chain-residue prediction above coming out, since a root that
specializes no longer offers its bottom node as a separate candidate. Specialized goes 862 → 1 563.
Two rows grow because trees now reach conditions they used to be refused before: StringLeaf 12 →
123, and TooManyLeaves 0 → 8 (below).

**The leaf cap had to be re-measured, because the order rule had been hiding it.**
`MaximumSpeculativeLeaves` was 8 and *never fired on the corpus* — the order rule refused those
trees first. The ordered form accepts whole chains, and at 8 it turned 85 of them down, 80 of them
Box2D's. At 16 that is 8, and the corpus loses a further **664 338 boxes, 2.1%** (Box2D 0.954×,
NavierStokes 0.983×, Crypto 0.985×) while the *tree count falls* — Box2D 1 109 → 1 090 — because a
longer chain absorbs sub-trees that were separately specialized. *That is `0084`'s "two operators"
mistake avoided rather than repeated: a threshold is a claim about the code and this one moved the
answer by 2.1% the first time it was measured.*

**And the wall clock, ABBA-interleaved at process granularity, six pairs, counters off**, with the
corpus's own controls — Richards and EarleyBoyer remove **exactly zero** boxes between the arms, so
their time must not move:

| Suite | off (median) | on (median) | ratio | pairs won | boxes removed per second |
|---|--:|--:|--:|--:|--:|
| **NavierStokes** | 1 680 ms | 1 406 ms | **0.834×** | **6 of 6** | **6 500 000/s** |
| **Crypto** | 3 098 ms | 2 790 ms | **0.893×** | **6 of 6** | **6 437 000/s** |
| RayTrace | 2 284 ms | 2 224 ms | 0.959× | 5 of 6 | 149 000/s |
| Box2D | 6 315 ms | 6 358 ms | 1.003× | 3 of 6 | 861 000/s |
| DeltaBlue | 1 310 ms | 1 324 ms | 0.966× | 4 of 6 | 25/s |
| Richards | 704 ms | 732 ms | 1.002× | 3 of 6 | **0 — control** |
| EarleyBoyer | 3 713 ms | 3 793 ms | 0.999× | 3 of 6 | **0 — control** |
| **Driver total** | **19 080 ms** | **18 634 ms** | **0.969×** | **6 of 6** | 1 920 000/s |

**0.969× on the driver with six of six pairs**, carried by NavierStokes at 0.834× and Crypto at
0.893× — both six of six, both entirely below 1 (0.793–0.899 and 0.866–0.926). **The two zero-box
controls read 1.002× and 0.999×**, which is what makes the rest readable; their own pair spread is
~12% and ~14%, so no per-suite effect under about 5% is callable and RayTrace and DeltaBlue are
directionally right and individually unproven. **No suite is slower** by more than its control's
noise.

***And the standing lesson from `0086` predicts every row of that table, including the one that
looks wrong.*** Box2D removes **5 436 916 boxes, 51% of its own** — proportionally more than Crypto
— and reads 1.003×, because that is 861 000 boxes a second against NavierStokes' 6 500 000. The
two suites that move are exactly the two above ~6 M/s; nothing between 25/s and 861 000/s moves at
all. *The share of a suite's own allocation still forecasts nothing; the absolute rate still
forecasts everything*, and this is the second independent run to say so.

**The exchange rate is also worth stating, because it is the third reading of the same constant.**
`0084` bought 1.9% of execution time with 12.2% of the allocation; this buys **3.1% with 54.0%**.
Allocation is simply not the dominant term in what this engine spends — three measurements now
agree on that within a factor of about two, and it is the number anyone sizing the rest of phase 3
should start from rather than the box counts.

**Verify.** `NumericTreeOrderTests` — 11 fixtures, **every value case on both settings of
`BROILER_JS_NUMERIC_TREE_ORDER`**, so each is a statement about JavaScript rather than a
description of the fast path: the hoisting arm reaches these answers by refusing to specialize and
the ordered arm by specializing correctly, and a disagreement is the bug the file exists to catch.
Left-leaning chains of elements and of fields; **a valueOf that mutates a later leaf of a
three-node tree**; **a throwing coercion that must beat a later leaf that would also throw**, which
is the sharpest one in the file because both arms throw and only the *message* says whether the
order held; four getters logging that every leaf is read once and left to right; a failure
half-way up that must leave the rest generic with `valueOf` run exactly once; a String defeating
the guard mid-chain so `+` becomes concatenation from that node up; BigInt mixing from the middle
of a chain; NaN, the infinities and −0 carried through several nodes as raw doubles; ToInt32
wrapping at every node; and a thousand-iteration element kernel. Plus three counter assertions,
because all eleven also pass when nothing specializes: that the tree `NumericSpeculationTests` pins
as refused now takes **two** guarded leaves instead of one, that a four-element chain moves from
`OrderUnsafe` to `Specialized` **by refusal reason** rather than merely by count, and that the
order-blocker sub-census discriminates a property read from an element read and reports nothing at
all once the conjunct that consults it is gone.

**This change also broke one of `0084`'s own fixtures, and that is the fixture working.**
`ATreeWhoseOrderCannotBePreservedIsRefused` asserted the refusal this removes, so it failed the
moment the ordered emission landed — the same way `0085`'s `AnUpdateOnAPropertyCostsTwoBoxesNotOne`
failed when `0086` landed under it. It is now a Theory on both settings asserting the invariant
instead of the refusal: **the answer is 25 either way**, and only which form computes it moves
(one guarded leaf on the hoisting arm, two on the ordered one). *That is twice in three items that
an eligibility fixture has caught its own item's successor, which is the argument for asserting
counts and not only answers.*

Full repository suite **8 063 tests, 0 failures** across 14 assemblies.

#### The denominator this phase never had — collection is 1.8% of the driver, and it is not where an allocation item pays

Three items have now measured an allocation cut against wall clock and got roughly a sixth of the
share back — `0084` 12.2% → 1.9%, this 54.0% → 3.1% — and the document has recorded the ratio three
times without ever asking *what the collector was costing in the first place*. The whole of phase 3
is priced in boxes; nothing said what a box is worth.

`GC.GetTotalPauseDuration()` answers it exactly rather than by sampling — it is the runtime's own
accounting of how long execution was suspended — and it is four lines in the driver. Taken per
suite on both arms of this item, three runs each, medians:

| Suite | elapsed off → on | GC pause off → on | pause share | gen0 off → on |
|---|--:|--:|--:|--:|
| Richards | 677 → 725 | 5 → 5 | 0.8% | 1 → 1 |
| DeltaBlue | 1 311 → 1 320 | 25 → 27 | 2.0% | 1 → 1 |
| RayTrace | 2 322 → 2 345 | 55 → 55 | 2.4% | 48 → 43 |
| Box2D | 6 895 → 6 802 | 108 → 86 | 1.5% | 9 → 8 |
| EarleyBoyer | 3 955 → 3 894 | 78 → 92 | 2.2% | 5 → 5 |
| Crypto | 3 217 → 2 844 | 66 → 44 | 1.8% | 81 → 33 |
| **NavierStokes** | 1 732 → 1 411 | 67 → 39 | **3.3%** | 16 → 7 |
| **Total** | **20 109 → 19 341** | **404 → 350** | **2.0% → 1.8%** | |

**Collection is 1.8–2.0% of the driver.** And the decomposition is sharper than the level:
**768 ms of wall clock came off and 54 ms of it was collection — 7%.** *The other 93% is the
mutator's own allocation work* — the pointer bump, the zeroing, the write barriers and the cache
traffic of touching a gigabyte of fresh memory — which no GC counter reports and which is where an
allocation item actually pays.

The same run corroborates the box counters from outside them: **allocated bytes fall 4.00 GB →
2.92 GB**, against a prediction of ~1.0 GB from 36 633 528 boxes at 24–32 B each. Two independent
instruments, one counting objects at the factory and one counting bytes at the allocator, agree.

**So the ceiling on everything left in this phase can now be stated rather than guessed.** At the
measured rate — **711 ms per GB removed**, or **12–21 ns a box** depending on whether the six-pair
ABBA total or this run's is used — the **0.70 GB of number boxes still standing (24% of the 2.92 GB
that remains) is worth about 495 ms, 2.6% of the driver**, and a typed backing store reaches part of
that rather than all of it.

***This retires an assumption the phase has run on since it opened, and confirms a non-goal that
was until now asserted rather than measured.*** §Non-goals says GC work is out of scope because
"the allocation **rate** is a severe problem […] not […] the collector". That is now a measurement:
the collector costs 1.8% and the allocation costs about fourteen times what the collection of it
does. Aiming at the collector would have been aiming at a fourteenth of the problem.

#### And a sampling profiler cannot decompose this engine, which is a finding about item 4-5

Item 4-5 stopped at *"~85% of a call's fixed cost is still unattributable from outside the engine,
so the rest of 4-5 is blocked on a profiler rather than a design"*. A profiler was tried here —
`dotnet-trace` with `Microsoft-DotNETCore-SampleProfiler`, converted to speedscope and aggregated
by self time — and it does not lift the block. Two reasons, both worth recording so nobody spends
the afternoon again:

- **The JavaScript does not symbolicate.** Compiled JavaScript runs in `DynamicMethod`s, and the
  stack walker resolves almost none of them: **47.8% of the profiled run sits under
  `JSFunction.InvokeFunction` with a JavaScript frame below it on the stack that has no name**,
  against **2.4%** that reaches a named `dynamicClass.<function>-<file>` body. So the profile can
  say *"this is JavaScript executing"* and essentially nothing else about which JavaScript.
- **The largest single frame in the profile is the profiler.** `Thread.PollGCWorker` takes
  **28.0%** of self time — which is not collection, since the exact counter above says collection
  is 1.8% — it is threads rendezvousing at GC poll points so the sampler can walk their stacks.
  The profiled run takes **25.4 s against 19.3–20.1 s unprofiled**, and that ~29% inflation is the
  same 28%. *A profile whose biggest frame is its own suspension mechanism is measuring itself.*

**Neither is a reason to distrust the GC number**, which comes from a counter and not from the
sampler, and which the two arms' allocated-bytes agree with. It is a reason to stop treating "get a
profiler on it" as an available next step for phase 4: it needs one that can name a `DynamicMethod`,
and that is a different tool.

#### The census re-taken on the far side, and it hands the item back its original premise

`0085` gave the compiler's boxing conversion its own factory entry and used it to refute the
root-box hypothesis: **18.4% of the corpus's requests, and only 5.0% of NavierStokes'** — so a
typed backing store could not be why its boxes survived. That was right about the engine as it then
was. Re-taken on the arm that ships now, the same counters say something different, and the
difference is what this change did:

| Source | hoisting arm | | order-preserving arm | |
|---|--:|--:|--:|--:|
| **Compiler conversion** — a raw double crossing into a `JSValue` | 20 603 254 | 21.8% | **24 649 016** | **47.4%** |
| **`++` / `--` step** | 17 281 964 | 18.2% | 17 281 954 | **33.2%** |
| Binary operators | 53 353 957 | **56.3%** | 6 626 052 | 12.7% |
| Numeric literal | 1 671 332 | 1.8% | 1 671 332 | 3.2% |
| Unary `-` and `~` | 702 031 | 0.7% | 702 031 | 1.3% |
| Unnamed | 1 108 187 | 1.2% | 1 107 019 | 2.1% |
| **Total requests** | **94 720 730** | | **52 037 409** | |

**The conversion column did not grow much — 20.6 M to 24.6 M — the rest collapsed underneath it.**
It is now the largest single source of boxing on the corpus, and it grew *because* the guarded tree
works: a tree that computes natively boxes once, at the root, and a root box is a conversion. Per
suite the concentration is sharper still — Crypto **17.06 M** conversions against 13.41 M
allocations (the gap is the small-integer cache), NavierStokes 1.83 M → **4.04 M**, Box2D 3.21 M.

***So the root-box hypothesis was false when `0085` tested it and is true now, and this change is
what made it true.*** Everything a typed store cannot reach has been taken out from in front of it.

**And the second survivor points the same way.** The `++`/`--` step is untouched at 17.28 M and is
now **33.2%** — 9.46 M of it NavierStokes', 7.21 M Crypto's. `0086` removed the *coercion* half,
which was a copy; what is left is the **new value being stored back**, and `a[i]++` has to box it
because the element is a `JSValue` slot. That is a storage cost wearing an operator's name.

**Conversion plus update step is 80.6% of everything the corpus still boxes, and both are the same
sentence: a raw double crossing into a `JSValue` slot or element.**

#### Where the `++`/`--` step's operands live — counted, and not one of them is an element

The re-specification below named this as the count to take before a typed backing store is built,
on a stated disjunction: *if the operand is an element or a field the step shares that mechanism,
and if it is a local the analysis merely failed to type, it is a much smaller change aimed
somewhere else entirely.* It is the second, and not marginally.

The compiler already knows where the operand lives — it emits a different branch for each — so the
census is that knowledge carried into the step as a compile-time constant. The rows are recorded by
an overload while the total stays with `Increment` itself, so **the rows sum to `UnaryUpdate` by
construction** and a call site the emitter forgot shows as a shortfall rather than vanishing. Every
suite balances.

| Suite | step | Element | Property | LocalCell | **LocalSlot** | GlobalOrWith | Other |
|---|--:|--:|--:|--:|--:|--:|--:|
| Richards | 15 558 | 0 | 15 558 | 0 | 0 | 0 | 0 |
| DeltaBlue | 3 381 | 0 | 933 | 0 | 2 448 | 0 | 0 |
| RayTrace | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Box2D | 287 373 | 0 | 15 051 | 0 | 272 322 | 0 | 0 |
| EarleyBoyer | 304 251 | 0 | 0 | 93 | 19 149 | 285 009 | 0 |
| Crypto | 7 209 815 | 0 | 16 453 | 260 | **7 193 102** | 0 | 0 |
| NavierStokes | 9 461 766 | 0 | 0 | 6 | **9 461 760** | 0 | 0 |
| **Total** | **17 282 144** | **0** | 47 995 | 359 | **16 948 781** | 285 009 | **0** |

**Not one of the corpus's 17.28 M `++`/`--` steps is on an array element. 0.3% are on an object
field. 98.1% are on a local or parameter the numeric analysis did not prove numeric** — a
`LocalSlot`, meaning a name that resolved statically, so it never took the dynamic path, and stayed
a `JSValue` because nothing could type it.

***So the step is not a storage problem at all, and the disjunction resolves against the typed
store.*** Weighted by each suite's own request-to-allocation ratio, the step is **≈7.05 M real
boxes — 22.6% of the 31.16 M the corpus still allocates** — and **6.76 M of that is NavierStokes'
`LocalSlot` alone**, on the suite with the highest absolute boxing rate in the corpus, which is
where §3.5's rate lesson says an allocation item pays and nowhere else.

**Reading NavierStokes' source says exactly which locals, and it is one cascade.** The hot updates
are `++currentRow;` and `x[++currentRow]` in `lin_solve`, `advect` and `project` — never
`a[i]++`, which is why the Element column is a clean zero. `currentRow` is initialized
`var currentRow = j * rowSize`, and `rowSize` is a `FluidField`-scope `var` assigned inside
`reset()` from `width + 2`, where `width` is assigned inside `setResolution()` from a parameter.
So the analysis cannot type `rowSize`, therefore cannot type `currentRow`, therefore every
`++currentRow` boxes. Item 3-6's waterfall on the same run agrees to the name: NavierStokes has
**141 hoisted names and 24 numeric locals**, with the drops attributed to `OtherName` (17, the
outer-scope bindings) and `DroppedCandidate` (18, the cascade from them).

***One closure variable the analysis will not type costs 6.76 M boxes.***

**This re-opens item 3-8 on the same terms `0083` re-opened it once already.** 3-8 was told not to
start "as written" because it priced a run-time numeric guard at **the local** and measured the
whole static tier at 0.36% of the corpus's boxing. That verdict was about *the tier as built*.
Priced at the *update operator* — the same move that took a compile-time proof reaching 0.75% of
the arithmetic and replaced it with a run-time test reaching 100.00% — the population the tier
**misses** is 22.6% of what the corpus still allocates. *3-8 measured what the mechanism catches;
this measures what it lets through, and the two numbers differ by sixty-fold.*

**One adjacent gap, found by reading the emitter and worth naming rather than building.**
`++currentRow;` in statement position throws its value away, and the compiler has that concept —
`FastCompiler`'s `assignmentInStatementPosition` sets it so `n = 5;` on a numeric local stores an
unboxed double. It is set for **assignments only**; an update expression gets `discardResult` from
the `for`-update clause and from nowhere else, so a bare `++x;` statement boxes even when `x` is a
raw double. On this corpus that is worth **nothing measurable**, because the locals it would serve
are not numeric in the first place — which is the honest reason to record it next to the item that
would make it pay rather than to build it now.

#### Every conversion attributed to the site that mints it, over all fifteen suites — `0103`

**§4.2a re-opened this item on a count that could not name its own producer.** It found conversions
going **24.6 M → 69.3 M** once the census stopped silently running 7 of 15 suites, with Gameboy
alone at 26.9 M on a `Uint8Array` memory image — the shape 3-1 was written for — and withdrew the
item's refutation on that basis. `0113` then settled the *storage* question from the other side, by
counting the dense read/write ratio at 1.03 on Gameboy and 3.34 on the corpus. **Neither asked
where the conversions come from**, and nothing could: the counter sits in `JSNumber.CreateConversion`,
so it sees that a raw `double` crossed into a `JSValue` and not which of the compiler's emission
sites sent it. *A category is not a producer, and this document has now spent three sections
ranking a population by a counter that cannot distinguish its members.*

So each of the compiler's **21** `JSNumberBuilder.New` sites names itself, and the census reports
the split. The site is an ordinary constant argument on the one code path the engine ships — not a
factory entry per site, which would multiply into nine near-identical methods, and not an argument
gated behind the counter flag, which would leave the arm that is measured and the arm that ships
running different code.

**Counted with the counters on, over all fifteen suites** — the first census in this campaign to
have a row for every one of them:

| Site | conversions | of all conversions |
|---|--:|--:|
| **the guarded tree's ROOT box** | **42 847 270** | **61.79%** |
| a native binary operator's result | 9 415 048 | 13.58% |
| the `++`/`--` step | 8 288 977 | 11.95% |
| reading a scalar-replaced numeric local | 4 742 175 | 6.84% |
| a native unary `+`/`-` result | 2 980 358 | 4.30% |
| a numeric constant in an argument or element list | 950 895 | 1.37% |
| an assignment's value in expression position | 114 025 | 0.16% |
| **an operand falling back to the tree's generic arm** | **226** | **0.0003%** |
| *unclassified* | **0** | **0.00%** |

**Two readings, and the second is the one that re-specifies the item.**

**The guarded tree is not leaking.** The generic arm — the fallback an interior node takes when its
speculation fails — is **226 requests of 69.3 M**, zero on eleven of the fifteen suites. `0087`'s
order-preserving emission was argued to be correct and was never counted at run time; counted, its
guards essentially always hold. *There is no recoverable loss inside the mechanism.*

**And what is left is the box the design keeps on purpose.** 61.79% of the corpus's conversions are
the tree's root — one box per evaluation, minted because the root's *consumer* is a `JSValue` local,
slot or element. It is **92.5% of NavierStokes' conversions, 91.5% of Box2D's, 98.5% of Splay's and
84.4% of Crypto's**. That is not a storage problem and not a leak; it is the boundary the tree was
always going to stop at. **So the remaining question for phase 3 is not what the operators mint, and
not what the store holds — it is what the root box's CONSUMER is**, and whether a consumer could
take the raw double the tree already has in hand. That is the measurement this item hands forward,
and it is a compile-time attribution rather than another run-time counter.

**Gameboy, the suite §4.2a re-opened the item on, splits differently from every other suite and
against the item.** Its 26.9 M conversions are **47.3% root, 28.7% the `++`/`--` step (7 723 245) and
16.7% a binary operator** — the update step alone is larger than any other suite's entire conversion
count. §4.2a asked whether Gameboy's conversions are the typed store's population; they are not.
They are item 3-8's, which had priced the `++`/`--` step over a corpus that did not contain the one
suite where it dominates. *The suite that re-opened the storage half turns out to belong to the
locals half.*

> **Reproduction and honesty about the denominator.** 164 626 610 boxing requests and 69 338 974
> conversions over fifteen suites, against §4.2a's 164 127 581 and 69.3 M over twelve — the same
> instrument, 0.3% apart, with Mandreel, zlib and CodeLoad contributing 2, 1 and 600 conversions
> between them. Gameboy reproduced to the digit across two independent runs (26 938 581, matching
> §4.2a exactly); Crypto varied by 0.015% between runs, so *"deterministic"* is very nearly true of
> these counters rather than exactly true, and shares should not be quoted past three figures.

#### And the root's consumer, counted — the answer is the numeric local, not the store — `0105`

`0103` closed by naming the one measurement left: the root is boxed **because its consumer takes a
`JSValue`**, so the only thing that removes it is a consumer able to take the raw `double` the tree
already holds. That is knowable only to the compiler — the box is minted at the tree, and what
receives it is not visible from there — so the consumer travels **with the node being visited**.

**The attribution is restricted so that it cannot leak, which is the whole reason to trust it.** The
consumer is set only for a node that *is* an `AstBinaryExpression`, the one shape that reaches the
tree builder directly, so the field can never survive into a nested visit; the tree builder clears
it for its own construction, because a leaf may contain a tree; and a **compound** assignment does
not claim its right-hand side, because there the tree is an operand of the compound operator rather
than the value stored. *An attribution that leaks reads as a finding about the corpus when it is a
finding about the instrument*, and `a[0] = b * c + sink(d * 2 + i)` — an element store with a second
tree inside a call argument in its own right-hand side — is a test rather than a remark.

**Of 42 849 742 root boxes over all fifteen suites:**

| The root box is consumed by | boxes | of roots | of all conversions |
|---|--:|--:|--:|
| **a LOCAL or a declared binding** | **19 006 647** | **44.36%** | **27.41%** |
| an ELEMENT — `a[i] = …` | 7 673 079 | 17.91% | 11.07% |
| a named PROPERTY — `o.x = …` | 5 631 192 | 13.14% | 8.12% |
| a call ARGUMENT | 1 897 892 | 4.43% | 2.74% |
| a RETURN value | 169 872 | 0.40% | 0.24% |
| *unattributed* | *8 471 060* | *19.77%* | *12.22%* |

***The dominant consumer is a local, and that retires both of the storage items as the answer.*** A
proven-numeric local **already has a raw `double` home** — item 3-3 built it — so a root landing in
a local is not a root waiting for a new representation: it is one **the existing numeric tier failed
to type**. 44.36% of the tree's remaining boxes are being minted to cross into a destination that
did not need to be a `JSValue` at all.

**And it puts a ceiling on the typed backing store that is lower than anything §4.2a suggested.** The
element row — the entire population a typed store reaches — is **17.91% of roots, 11.07% of the
corpus's conversions, 7.67 M boxes**. `0113` already measured that store as an *allocation wash* at
the corpus's 3.34 read/write ratio, so this is a ceiling on something that is not free to begin with.
Item 3-2's shape slots are the property row, **13.14%**. *Neither storage item is where phase 3's
remaining boxes go.*

**The per-suite split is not uniform, and that is the useful half:**

| Suite | roots | dominant consumer | |
|---|--:|---|--:|
| Crypto | 14 396 573 | **local** | **81.8%** |
| Gameboy | 12 741 786 | local / element, near-even | 21.9% / 21.6% |
| Typescript | 4 825 206 | **property** | **74.0%** |
| NavierStokes | 3 734 334 | **element** | **59.2%** |
| PdfJS | 3 436 908 | local | 43.2% |
| Box2D | 2 934 766 | local | 45.1% |
| Splay | 518 880 | **argument** | **98.5%** |

Crypto's digit arrays send **81.8% of their roots to a local**, which is the suite this phase has
called an array workload throughout. NavierStokes is the one suite the typed store genuinely
addresses (59.2% element) and it is also the suite `0113` measured at a **5.26** read/write ratio —
the *worst* case for a typed store. *The suite that wants the mechanism is the suite that pays most
for it.*

> **The 19.77% residual is reported rather than folded away.** It is the assignment forms this pass
> does not wire — destructuring targets, shadowed bindings, and expression positions with no store
> at all. **Gameboy carries 43% of its roots there**, so its row is the least trustworthy in the
> table and its near-even local/element split should not be read as a finding. A default that
> silently absorbed these would have made every other row look more decisive than it is.

**What phase 3 has left, after this.** The element and property rows together are **31.05%** of the
roots and are the two items already measured as a wash and as Box2D-only. The local row is
**44.36%**, it belongs to the numeric-local tier, and that tier's own gap has been measured twice
from other directions — 3-8's `++`/`--` step at 98.1% `LocalSlot`, and 3-8a's dual representation
closed as a regression on the read/write ratio of the code it targeted. *Three independent counts
now point at the same mechanism, which is the strongest signal this phase has produced about its
own remainder.*

#### Which refusal costs the boxes — the tier is counted in the wrong currency — `0106`

`0105` put **44.36% of the corpus's root boxes into a local** and left two candidate explanations.
The cheap one is a **seam**: the destination is a local the tier had already *accepted*, and the box
is minted by the tree and unboxed by the very next instruction, because the assignment path asks for
the right-hand side as a `JSValue` whenever the **static** prover (`ToNativeExpression`) cannot type
it — even though the **whole-function** prover already did, which is the only reason the destination
is a numeric local at all. `AssignToVariable` then stores through `ToDoubleExpression`. Box, unbox,
two instructions apart.

***Measured, the seam is 36 boxes of 18.6 M.*** It is not the explanation — and finding that out
cost one counter and is what makes the rest worth instrumenting rather than guessing at.

**So every one of those locals is one the tier REFUSED, and item 3-6 already counted the refusals —
in the wrong currency.** It counted causes **per name**, which is the right shape for *"how many
bindings would a stronger analysis admit"* and the wrong one for *"what do the refusals cost"*: a
name refused in initialization code and a name refused inside a ten-million-iteration loop weigh the
same in it. The analysis now retains its per-name refusal and the boxing site names it, so the same
vocabulary is ranked by **execution** instead of by **declaration**.

**Of 19 005 731 root boxes consumed by a refused local, over all fifteen suites:**

| Why the tier refused the destination | boxes | share |
|---|--:|--:|
| **`DroppedCandidate`** — a cascade: another refused name reaches it | **7 300 519** | **38.41%** |
| **`ElementRead`** — `a[i]` reaches it | **6 908 814** | **36.35%** |
| *`Unknown`* — *a gap in the instrument, not a cause* | *2 429 752* | *12.78%* |
| `PropertyRead` — `o.x` reaches it | 925 292 | 4.87% |
| `Parameter` — the caller picks the type | 775 865 | 4.08% |
| `CallResult` | 274 411 | 1.44% |
| everything else | 395 078 | 2.07% |

**The largest row has no independent cause to fix.** `DroppedCandidate` is a *cascade* — a name
refused only because another refused name appears in its assigned value — and the analysis's own
documentation already says such a name *"wants nothing at all — fixing its root fixes it for free"*.
So 38.41% of these boxes are downstream of the other rows rather than beside them. **NavierStokes is
96.8% cascade**, which is this document's own already-recorded finding arriving from a second
direction: *"one untypable closure variable (`rowSize`) cascades into every `++currentRow`"*. Gameboy
is 94.6% and Typescript 83.8%. *Three suites are one refusal each, wearing a large number.*

**And the largest independent cause is `ElementRead` at 36.35%** — 58.1% of Crypto's, the suite this
phase has called an array workload throughout. ***That is the same conclusion item 3-1's guarded tree
already reaches, at run time, and discards.*** The tree computes `a[i] * b` on raw doubles behind a
type test; the local's analysis refuses the destination because a *static* prover will not type
`a[i]`; so the tree boxes its root to store into a `JSValue` local. **The two mechanisms disagree
about the same expression, and the boxes are the cost of them not sharing a conclusion.**

> **`Unknown` at 12.78% is a gap in the instrument and is reported as one.** It is a destination
> whose name the analysis map does not carry — a store outside a body the analysis ran on, or a name
> resolved by a path that does not go through the function's own binding set. It is not a thirteenth
> cause, and it bounds how sharp the rest of the table can be.

**Where this leaves the tier, and why the next step is a count rather than a build.** The obvious
move is to let a local take the raw `double` the tree already has, guarded — which is **exactly item
3-8a**, built complete, measured, and closed as a regression at 1.012–1.021×. §3.5 records why:
*"a representation change is priced by the read/write ratio of the code it targets, counted before
the representation is built."* 3-8a's population was 26 names refused for a *different* conjunct;
this population is refused for `ElementRead` and is far larger. **So the measurement this hands
forward is that ratio, for these names** — and this phase has now twice been right to take it before
building, and once paid for not having.

#### The read side is not obtainable by wrapping the read — attempted, refused, reverted

`0106` closed by naming the measurement §3.5 requires before a representation change is built: the
**read/write ratio** for the locals refused for `ElementRead`. The write side is already counted —
one box per guarded-tree root stored into a refused local, **6 908 814** of them for that cause. The
read side is what a raw-`double` representation would begin paying, and it splits in two: a read
compiled as a guarded tree's **leaf** costs nothing (the tree already tests the operand and calls
`DoubleValue` on it), and **every other read** would mint a box the engine does not mint today.

**It was built, and it does not work.** The obvious hook is the local's read expression in
`VisitIdentifier`, wrapped in a counting call, gated at *compile* time so the shipping engine is
untouched — the pattern `SpeculativeNumericLocals.Counting` already uses. With it on, the population
it is supposed to measure fell **18 657 518 → 3 147 314 roots, 0.169×, with Gameboy's at exactly
zero**.

***The first reading of that was that the instrument perturbs the tree, and it is worse than
that.*** Exempting the tree's leaves and counting them from inside the tree — after every refusal is
decided, so eligibility cannot move — reproduced the same **0.169×** to three figures. The counters
were not biasing anything; the suites were **failing**:

```text
Crypto/Decrypt: Error: System.NotImplementedException:
  Assignment target Call (BCallExpression) is not supported
  at ILCodeGenerator.VisitAssign ... at montReduce-crypto.js:583
```

**A local's read expression and its assignment target are the same node.** `variable.Expression`
serves both, so wrapping it turns `x++` and `x op= v` into an assignment whose target is a method
call, which the IL backend rejects outright. The collapsed counts were aborted suites, and the
`0.169×` was the share of the corpus that happened to compile.

**So the ratio is not measured, and this is what stands instead.** The write side holds
(`0106`, unaffected — it is counted at the boxing factory, not in the emitted read). The read side
needs a hook that is *not* the expression the assignment path writes through: the tree's own leaf
save is one such position and yields the free half only; the boxing half has no equivalent, because
an ordinary local read is a bare CLR load with nothing to hang a counter on. **Item 3-1's remaining
question is therefore still open, and now has a named obstacle rather than a plan.** The work is
reverted; nothing of it is in the pin or in `patches/`.

> ***The reason to record a reverted instrument at all is that its first reading was wrong in the
> flattering direction.*** A 0.169× population looks like a subtle measurement bias one could argue
> about, correct for, or quote with a caveat; it was a crash. Two runs and a log line separate those,
> and only the second is a reason to stop. *An instrument that changes its own population by 83%
> should be assumed broken before it is assumed biased.*

#### The free half, counted from the one safe position — `0107`

The whole read side is unobtainable for the reason above. **The guarded tree's leaf save is the one
read position with neither problem**: the value is the right-hand side of an assignment into a fresh
temporary, so it is never an assignment target, and `BuildOrderedTree` runs only *after* every
refusal has been decided on the syntax, so a counter there can change neither what compiles nor
which trees specialize. A guarded leaf is also exactly the read a raw `double` would serve for
free — the tree already tests the operand for `IsNumber` and calls `DoubleValue` on it.

**Both safety claims are checked rather than argued, which is the lesson of the reverted attempt.**
Re-running the census with the counter on reproduces the roots-consumed-by-a-refused-local count —
**18 657 518 against 18 657 815, and 1.000 on every suite individually** — and the three suites that
fail under it (RegExp, Mandreel, zlib) fail identically with it off. *Last time the population moved
83% and the reason was a crash; this time it does not move at all.*

| Refusal | boxed writes | free leaf reads | free reads per write |
|---|--:|--:|--:|
| `CallResult` | 274 411 | 3 632 630 | **13.24** |
| `Parameter` | 775 877 | 10 002 325 | **12.89** |
| **`ElementRead`** | **6 908 985** | **14 799 912** | **2.14** |
| `PropertyRead` | 925 292 | 836 856 | 0.90 |
| `DroppedCandidate` | 7 300 576 | 5 249 912 | 0.72 |
| `NeverOffered` | 241 567 | 123 850 | 0.51 |
| **total** | **16 426 708** | **34 645 485** | **2.11** |

**The shape is a property of the workload, not of the cause.** Within `ElementRead` alone, Crypto
reads 1.98 times per write, **Gameboy 31.06** and **Box2D 0.05** — three orders of magnitude apart
under one heading, which is the same warning §4.2a gave about quoting a corpus share.

***This does not decide the item, and the most useful thing about it is why not.*** The break-even
condition for a raw-`double` representation is the **boxing** reads — the ones a `JSValue` consumer
forces — and those are precisely what has no safe hook. Free reads are neutral: they neither cost
nor save. **Item 3-8a is the standing warning against reading 2.11 as encouraging**: it lost at
1.012–1.021× with **393 705 boxes minted at the read against ≈5 300 removed**, and no count of the
reads it served for free would have predicted that. *A rich free-read population is what a favourable
workload looks like and also what 3-8a's looked like.*

**So what `0107` adds is a bound and a ranking, both weaker than a decision.** Total reads of these
locals are **at least 34.6 M**; the representation breaks even only if fewer than 16.4 M of the
remainder need a box. And the causes rank by how tree-resident their locals are — `Parameter` and
`CallResult` most favourable and both small, `ElementRead` middling at 2.14 and carrying the
population. *The item stays open, one measurement short, and the missing measurement is still the
one the compiler cannot be asked for safely.*

#### The cost side, counted at the CONSUMERS — and the ratio comes back in favour — `0108`

The read cannot carry a counter because it is also the assignment target. **A consumer's operand
can**, because it is a value and nothing else — an argument, a stored value, a returned value. And
`VisitConsumedBy` is already the single choke point for the five consumer categories `0105` plumbed,
so one hook covers an assignment's right-hand side into an element, a property or a local, a call
argument, and a return.

**Non-perturbation checked before anything was read from it**, which is now the standing order in
this item: the roots-consumed-by-a-refused-local count reproduces at **18 657 518 against
18 657 828, 1.000 on every suite**, and the three failing suites fail identically with it off.

***It is a LOWER BOUND on the cost and is quoted as one.*** Every `JSValue` consumer outside those
five — a generic operand, a member base, a condition, a comparison, a literal element — is missing.
**That direction is the useful one**: a bound *above* the saving refutes the representation outright,
while a bound below it does not confirm it.

| Refusal | saving (boxed writes) | cost ≥ (consumer reads) | cost / saving | |
|---|--:|--:|--:|---|
| `CallResult` | 274 412 | 2 581 505 | **9.41** | **refuted** |
| `NeverOffered` | 241 567 | 899 721 | **3.72** | **refuted** |
| `PropertyRead` | 925 292 | 1 689 299 | **1.83** | **refuted** |
| `Parameter` | 775 889 | 96 056 | 0.12 | open |
| **`ElementRead`** | **6 908 985** | **293 259** | **0.04** | **open** |
| **`DroppedCandidate`** | **7 300 576** | **208 905** | **0.03** | **open** |
| **total** | **16 426 721** | **5 768 745** | **0.35** | |

**Three causes are refused at the bound, and they are the small ones** — 1.44 M writes between them.
**The two carrying 14.2 M of the 16.4 M come in at 0.04 and 0.03**, which means the un-instrumented
consumers would have to supply **twenty-five times every read counted anywhere** to reach break-even.

**Per suite it is the same shape, and it lines up with where the boxes are:**

| Suite | writes | consumer reads | ratio |
|---|--:|--:|--:|
| Crypto | 9 547 789 | 194 988 | **0.02** |
| Gameboy | 2 778 674 | 98 880 | **0.04** |
| PdfJS | 1 420 349 | 4 416 | **0.00** |
| **NavierStokes** | 1 181 190 | **0** | **0.00** |
| Box2D | 1 100 346 | 254 317 | 0.23 |
| RayTrace | 35 028 | 146 623 | 4.19 |
| Typescript | 342 102 | 3 826 237 | 11.18 |
| EarleyBoyer | 21 243 | 751 019 | 35.35 |

***NavierStokes' refused locals are read only inside trees — zero instrumented boxing reads at
all***, which is the strongest case in the corpus and the suite whose refusals are 96.8% cascade,
i.e. the ones the analysis's own note says fixing a root fixes for free.

**This is the first affirmative evidence phase 3 has produced for widening the numeric tier**, and
every previous item in the phase that felt this good was wrong, so the qualifications are the
important part:

- **The cost is a lower bound, not the cost.** 0.04 becomes 1.00 if the un-instrumented consumers
  are 25× the instrumented ones. That is a lot, and it is not impossible.
- **Item 3-8a is the standing counter-example and it is not answered by this.** It lost with
  393 705 boxes minted at the read against ≈5 300 removed — a ratio of ~74:1 *against* — which no
  count taken before it was built had produced. What is different here is that a ratio *has* been
  taken and it is 0.03–0.04 on the population that matters; what is the same is that it is being
  read off an instrument rather than off a shipped change.
- **The three refuted causes should be excluded by construction if anything is built**, rather than
  discovered later: a widening that admits `CallResult` locals is buying 274 412 boxes for at least
  2 581 505.

**So the item is, for the first time, pointed at something specific and bounded**: widen the numeric
tier for names refused by `ElementRead` and by cascade from one, exclude `CallResult`,
`NeverOffered` and `PropertyRead`, and expect the saving to be bounded above by 14.2 M boxes —
**8.4% of the corpus's 164.6 M boxing requests**, and *worth building only if a wall-clock A/B says
so*, which §3.5 and `0086`'s rate lesson both insist on.

#### The widening built, and measured as a regression — the saving is not where the mechanism can reach it — `0109`

`0108` selected a population by measurement rather than by argument: `ElementRead` at a cost/saving
of **0.04** over 6 908 985 boxed writes, the cascade at **0.03** over 7 300 576, and `CallResult`,
`NeverOffered` and `PropertyRead` refuted at the bound and therefore excluded by construction. **It
is built.** An element read is not provably numeric, so the widened names go to item 3-8a's dual
representation and never to the sound tier; the cascade needs no rule of its own, because the pass
is a fixed point. One assume flag, one `IsNumeric` arm, one extra pass.

**Measured against the same build with the switch off, counters on, all fourteen suites:**

| | off | on | |
|---|--:|--:|--:|
| boxing requests | 124 693 165 | 132 273 724 | **1.061** |
| boxes allocated | 66 982 650 | 69 582 935 | **1.039** |
| *Gameboy's requests* | *52 835 472* | *59 321 464* | ***1.123*** |

***A regression, and not a small one.*** But the two counters that decide it say the cause is **not**
the read/write ratio `0108` measured:

| | off | on |
|---|--:|--:|
| roots consumed by a refused local — *the saving* | 18 657 804 | **18 656 936** |
| speculative-read boxes — *the cost* | 0 | **7 692 133** |

***868 of the 18.7 M writes the population was selected for are actually removed.*** The saving was
never collected; only the cost arrived.

**The mechanism cannot collect it, and this is the finding.** The assignment path tests
`NumericStorage` — which a *speculative* local does not have — so it still asks for the right-hand
side as a `JSValue`, the tree still boxes its root, and `AssignToSpeculativeVariable` unboxes it
again. **3-8a built raw arms for the tree's LEAF, the element read and the element write, and none
for the tree's ROOT** — which is the one site the entire saving lives at. Item 3-8a's own
re-specification lists its three consumers and that absence is not remarked on anywhere, because
until `0105` nothing had counted what the root's consumer was.

> ***`0108`'s 0.04 was a true measurement of the opportunity and a measurement of nothing about
> whether any available mechanism could take it.*** The ratio said *"if these locals held raw
> doubles, the reads would be nearly free"* — which is still true — and the tier's representation
> has no way to put a raw double into one at the site that mints the box. **A cost/benefit ratio
> prices an outcome; it does not establish that a mechanism reaches the outcome, and this phase has
> now spent two items discovering that separately.**

**Status.** The widening is **off by default and stays off**, kept as the arm a store-path change
would be tested against — the same disposition item 3-8a has, and for a sharper reason: 3-8a was
refuted by its population's read/write ratio, and this is refused by a missing consumer that is
nameable in one sentence. **The next step, if anyone takes it, is a raw arm for the tree's root
into a speculative local**: emit `raw = <native>, flag = true` on the guarded arm and
`slot = <generic>, flag = false` on the other, and box nothing. Whether *that* wins is then
`0108`'s ratio question again, and this time with the saving actually reachable.

#### The raw arm built — the saving is collectable, and the item is refuted anyway — `0110`

`0109` left one sentence of work: *"a raw arm for the tree's root into a speculative local — emit
`raw = <native>, flag = true` on the guarded arm and `slot = <generic>, flag = false` on the other,
and box nothing."* **Built**, at the assignment and at the declarator, in statement position only —
the line item 3-3's `NumericStoreResult` already draws, and for the same reason.

**It works.** Roots consumed by a refused local go **18 657 804 → 16 225 570, 0.870× — 2 431 366
boxes removed**, against `0109`'s 868. The missing consumer was the whole of that defect.

**And the item is refuted anyway.**

| against the widening-off arm | `0109` | `0110` |
|---|--:|--:|
| boxing requests | 1.061× | **1.041×** |
| boxes allocated | 1.039× | **1.039×** |
| roots into a refused local | 1.000× | **0.870×** |
| speculative-read boxes | 7 692 133 | **7 692 133** |

***The cost did not move at all.*** The saving is **2.4 M** and the cost is **7.7 M**, so completing
the mechanism converted a regression caused by collecting nothing into a regression caused by
collecting a third of what it pays for. **Off by default and staying off.**

**Two things this settles that no earlier count in this item could.**

**`0108`'s consumer-side bound was 25× too low, and structurally rather than by bad luck.** It counted
reads at five consumer positions and called the result a lower bound on the cost. The cost is a box
at **every** read of a speculative local that is not one of the three raw-capable consumers, minted
at the local's **own read expression** — precisely the site `0107` established has no safe hook. *A
lower bound taken at the wrong sites is not a loose bound on the right quantity; it is a bound on a
different quantity*, and nothing about its being a bound protects it from that.

**And the saving was never 14.2 M.** A refusal census attributes a name to its **first** cause, so
removing that cause admits the name only if it was the **only** blocker. `var t = a[0] * b + i` with
a parameter `b` is charged to `ElementRead` and is *still* refused once element reads are assumed
numeric, because `b` blocks it independently. That is why 6.9 M `ElementRead` writes yield 2.4 M
removable boxes. **`0106`'s table ranks refusals correctly and does not — and never claimed to —
measure what removing one would admit.**

> **What the three attempts share.** 3-8a priced a representation at the local and lost on reads;
> `0109` priced it on a measured ratio and collected nothing; `0110` completed the mechanism and
> still lost on the same reads. *Every time, the cost has been the boxes minted reading a
> dual-representation local, and every time it has been measured last.* If a fourth attempt is made,
> the read cost is the first thing to count and the only safe way found to count it is to build the
> representation and read `boxingSpeculativeReadRequests` — which is what this patch now makes cheap
> to do for any candidate population.

#### The read cost counted FIRST, for a fourth population — and it is the third one again — `0111`

`0110` closed with a method rather than a plan: *"the read cost is the first thing to count and the
only safe way found to count it is to build the representation and read
`boxingSpeculativeReadRequests`."* **This is that method run once**, on the parameter population,
with the counter read before anything else.

**Why parameters.** Item 3-3 records `parameter` as its one category that *"cannot reach the numeric
tier at all"*; item 3-8a deliberately excluded them (*"they want a guard at entry rather than at an
initializer"*); `0106` weighted the refusal at **775 877** boxed writes; and `0107` found their
locals the **most tree-resident of any cause, 12.89 free leaf reads per write**. *That last number is
exactly the kind that has flattered all three previous attempts*, which is the reason to count the
cost before believing it.

**Counted first:**

| | |
|---|--:|
| **speculative-read boxes — the cost** | **417 582** |
| roots consumed by a refused local | 18 657 804 → **18 962 176** |
| **the saving** | **−304 372** |
| corpus boxing requests | **1.003×** |
| corpus allocations | **1.006×** |

***The saving is negative.*** Admitting more speculative names makes more trees eligible —
`CountSpeculativeLeaves` is a term in the eligibility sum — and each new tree mints a root. So the
population pays 417 582 boxes to mint 304 372 more. **Refuted on one measurement, with no
build-then-diagnose cycle**, which is the entire point of the ordering.

***And the per-suite column carries the real finding.*** NavierStokes mints **exactly 393 705**
speculative-read boxes — *the number this document already records for item 3-8a's failure, to the
digit*. On the suite that decides these items, **the fourth population is the third one wearing a
different refusal**, and the counter says so before any wall clock was taken. Gameboy supplies the
negative saving almost entirely (−313 623 roots) and Crypto contributes nothing at all.

> **Three of `0108`'s conclusions are void rather than confirmed.** That patch refused
> `PropertyRead`, `CallResult` and `NeverOffered` on the consumer-side bound, and `0110` established
> that the bound was on a different quantity. **Those three are un-measured again, not eliminated** —
> and each is now one flag and one run away from an answer, which is what the method buys.

**What phase 3 has after four attempts at this mechanism.** 3-8a priced it at the local and lost on
reads; `0109` priced it on a ratio and collected nothing; `0110` completed the mechanism and lost on
the same reads; `0111` counted the reads first and refused before building anything. *The cost has
been the same quantity every time — boxes minted reading a dual-representation local — and the only
thing that has changed is how early it was known.* **The remaining populations are cheap to test and
none is promising**; the dual representation should be considered refuted as a general mechanism on
this corpus rather than as four unlucky populations.

#### The remaining populations, tested — and `0108`'s ranking was not merely low but inverted — `0112`

`0110` voided `0108`'s consumer-side refusals of `PropertyRead`, `CallResult` and `NeverOffered`.
Two of the three are expressible as assumptions and are measured here by `0110`'s method — build the
representation behind a flag, read `boxingSpeculativeReadRequests` **before anything else**.

| population | cost (spec. reads) | saving (roots) | requests | allocations |
|---|--:|--:|--:|--:|
| `Parameter` (`0111`) | 417 582 | **−304 372** | 1.003× | 1.006× |
| **`PropertyRead`** | **3 828 813** | 72 980 | 1.030× | **1.045×** |
| **`CallResult`** | **913 011** | 431 131 | 1.004× | 1.006× |

***All three refuted. Every population tried costs more than it saves — four of four.***

**And `0108`'s bound was not merely low; it was inverted.** It predicted `CallResult` **9.41** and
`PropertyRead` **1.83**, ranking `CallResult` as the worse of the two by five times. Measured,
`PropertyRead` is **52.5** and `CallResult` **2.12** — *the reverse, by twenty-five times*. `0110`
could say the bound landed on a different quantity; this says the quantity it landed on **does not
even preserve the ordering** of the one it stood in for. *A bound taken at the wrong sites is not a
conservative version of the right answer, and cannot be used to rank.*

**`NavierStokes` mints exactly 393 705 speculative-read boxes under `CallResult`** — the same figure
it mints under `Parameter`, and the same one this document records for item 3-8a. **Three
populations, one number.** On the suite that decides these items they are the same handful of names
reached by three different assumptions, which is why the mechanism keeps failing the same way.

> **`NeverOffered` is not testable by this method, and the reason is structural rather than effort.**
> The cause means the *declaration* is non-numeric — `var a = []`, `var s = ''` — so there is no
> assumption about a value source that admits it; the fixed point drops it on its own initializer.
> Holding it speculatively is not a widening of the analysis but a decision to represent *every*
> non-numeric local speculatively, a different proposition. Its ceiling is **241 567 boxed writes,
> 1.5%** of the 16.4 M, against costs of 0.4–3.8 M on every population measured. **Argued, not
> measured, and labelled as such.**

**Item 3-1's dual-representation line is closed.** Four populations, four refutations, one shared
failure mode, and the last three measured for the price of one run each. *What the item produced is
not a speed-up; it is a method for refusing one cheaply, and the sequence `0106`→`0107`→`0108`→`0110`
is worth reading backwards by anyone who proposes the next representation change in this engine.*

#### Re-specification

**3-1 returns to the storage change it was written as — and for the first time the objection that
took it off storage does not apply.**

- **The wash is gone, because the consumer now exists.** The item's original measurement said a
  typed store trades a write allocation for a read allocation, since the dense store is
  `IPropertyValue[]` and every read has to hand back an `IPropertyValue`. That was true of a
  compiler with nowhere to put a raw double. The guarded tree's leaf slot **is** that place: it
  saves each leaf and immediately reads `DoubleValue` off it, so an element read that could answer
  in a raw double would feed it directly and box nothing. The item is still **storage plus an
  unboxed element READ the numeric operators can consume** — joint `Broiler.JavaScript.Storage` and
  `Broiler.JavaScript.Compiler`, still an **XL** — but the second half now has a caller.
- **The ceiling is measured rather than assumed.** `boxingConversionRequests` is exactly what a
  typed store can remove without further operator work: **24 649 016 requests, 47.4%**, against the
  18.4% `0085` measured before the operators were cleared.
- **3-2 is the same argument for object fields and is now the larger half on two suites**, not one:
  Box2D 3.21 M conversions and Crypto 17.06 M. They remain one mechanism with two backends.
- **The `++`/`--` step is a third item's worth and it belongs with them**, since what it boxes is
  a value going into a slot or an element. 17.28 M requests, and it is NavierStokes' largest
  remaining source.
- **The bitwise emission is still waiting, and its wait is now shorter.** It cost one file and 15
  tests, it is correct today, and the day an element read yields a raw double it starts collecting
  on Crypto without another line being written.

**What the wall clock says about all of it should be read first, and it now has a mechanism behind
it rather than an observed ratio.** Collection is **1.8% of the driver**, and of the 768 ms this
item removed only **54 ms — 7% — was collection**; the rest is the mutator's own allocation work.
At the measured **711 ms per GB**, the **0.70 GB of number boxes still standing is worth about
495 ms, 2.6% of the driver**, and a typed backing store reaches a part of that rather than all of
it — the conversion column is 47.4% of *requests*, and requests are not allocations, because the
small-integer cache answers a large share of Crypto's for free.

**So the honest statement of what is left in phase 3 is: an XL for something under 2%.** That is
worth having and it is not worth doing ahead of anything with a better ratio. Two consequences for
sequencing:

- **The `++`/`--` step is the cheaper bid, and the count is now taken: it belongs to the numeric
  local, not to storage.** **Element 0, Property 0.3%, LocalSlot 98.1%** of 17.28 M steps — ≈7.05 M
  real boxes, **22.6% of everything the corpus still allocates**, 6.76 M of it NavierStokes' alone.
  So it shares no mechanism with a typed store and must be sequenced against it rather than after
  it. What it wants is **item 3-8's run-time guard, re-priced at the operator** — 3-8 measured the
  static tier's yield (0.36%) and concluded "do not start"; this measures what that tier lets
  through, and the two differ sixty-fold. *That is the same correction `0083` made when it moved
  the guard from the local to the arithmetic operator, arriving a second time by a different
  route.*
- **Nothing in phase 3 should be started on a box count again.** Every item from here is bidding
  against 2.6%, and the number to beat it with is a rate — ms per GB, or ns per box — not a share.

---

### 3-2 · Unboxed doubles in shape slots — **measured; its premise sentence is wrong, and the "one suite" was an artifact of the seven-suite corpus**

The object-field twin of 3-1: `shapeSlots` holds `JSValue` references, so
`vector.x = 1.5` allocates. This is what RayTrace and Box2D need, and it **composes
with 2-1** — a shape that knows a slot is a double can store it raw, so land 2-1 first
and this gets cheaper.

**Where.** `Runtime/JSObject.cs`, `Runtime/ObjectShape.cs`. **Size: L.**

#### `vector.x = 1.5` does allocate, and not for the reason the item gives

The item's premise is one sentence, and it names a cause. Taken literally as a probe — the item's
own example, then the same store varying only where the value came from:

| Site | B/iter | |
|---|--:|---|
| `local-write-control` — the same arithmetic into a raw-double local | **0.00** | the floor |
| **`o.x = 2`** | **0.00** | **the slot store allocates nothing** |
| `o.x = 1.5` | 32.00 | one box — and it is the **literal**, not the slot |
| `o.x = v * 1.5`, `v` a raw double | 32.00 | one box — and *this* is the slot |
| `field-read-only` — `s = s + o.x` | 31.98 | |
| `field-read-write-chain` — read, arithmetic, store back | 96.00 | |

**`o.x = 2` allocates nothing at all**, because storing an already-boxed `JSValue` into a slot is a
reference copy. So `shapeSlots holds JSValue references, so vector.x = 1.5 allocates` is right that
the line allocates and wrong about why: what it pays for is the **literal**, which `VisitLiteral`
re-boxes on every evaluation (item 3-1 measured that at 1.2% of the corpus's boxing requests). The
slot's own cost appears only in the row the item does not write — `o.x = v * 1.5`, where the value
*is* a raw double at the point of the store and the slot cannot hold it. That is 32 bytes, the same
32 bytes this phase has now measured eleven times.

**And the field rows match the element rows to the hundredth**: `field-read-only` 31.98 against
`element-read-only` 31.98, `field-read-write-chain` 96.00 against `element-read-write-chain` 96.00.
*3-1 and 3-2 are not twins by analogy, they are the same numbers.* One mechanism — a value that
stays unboxed from its producer to its consumer — with two storage backends.

#### Which suite each item can actually reach, counted

The signal that settles it is the one **4-1 deliberately left uncollected** ("numeric-vs-generic
per site") and that 3-8 then named as the thing nobody could answer. It is one branch on the
inline cache's two hit returns: of the reads the cache answers, how many hand back a number.

| Suite | cache-answered reads | of them numeric | | fresh boxes | boxing share |
|---|--:|--:|--:|--:|--:|
| **Box2D** | 18 241 436 | **9 853 002** | **54.0%** | 11 629 732 | 36.60% |
| DeltaBlue | 470 291 | 64 274 | 13.7% | 13 794 | 0.64% |
| Crypto | 651 289 | 74 457 | 11.4% | **42 423 644** | **55.17%** |
| EarleyBoyer | 76 599 | 7 802 | 10.2% | 564 024 | 1.92% |
| RayTrace | 353 058 | 31 267 | 8.9% | 872 934 | 4.98% |
| Richards | 272 432 | 21 380 | 7.8% | 14 671 | 1.51% |
| **NavierStokes** | **388** | **0** | **0.0%** | **29 977 471** | **66.96%** |
| **Total** | **20 065 493** | **10 052 182** | **50.1%** | | |

**Half of every property read the cache answers hands back a number**, which is the number 3-2 was
missing — and the per-suite column is the one that decides the plan:

- **3-2 is a Box2D item.** Of the corpus's 10.05 M numeric reads, **98% are Box2D's**, against its
  11.6 M boxes — so an unboxed slot could serve most of what that suite mints. RayTrace, the other
  suite the item names, does 353 058 reads in total and is 4.98% boxing; it is not a target.
- **3-2 cannot touch 3-1's suites, at all.** NavierStokes mints **29 977 471** boxes and performs
  **388** property reads, **zero** of them numeric. Crypto mints 42 423 644 and reads 651 289.
  Together they are **85% of the corpus's boxes and essentially no property traffic**: their
  numbers live in `new Array` read by index. *No amount of work on shape slots reaches them.*

#### The table above is the seven suites, and widened it overturns the re-specification below

**The signal 3-2 was missing was collected on exactly the corpus §4.2a found the censuses were stuck
on**, and the total gives it away: 20 065 493 cache-answered reads, against **186 831 813** over the
twelve suites that run. *The seven are 10.7% of the corpus's cache-answered reads and 9.7% of its
numeric ones.* `SpecializingTierMetrics` has reached all fifteen since `0103`, so this is a re-read
rather than a new instrument — the fourth figure in this document to need one.

| Suite | cache-answered reads | of them numeric | | boxes allocated | in the seven |
|---|--:|--:|--:|--:|---|
| **Typescript** | **115 082 436** | **64 199 239** | **55.8%** | 8 797 514 | — |
| **Gameboy** | **47 152 809** | **27 437 672** | **58.2%** | **29 322 416** | — |
| Box2D | 18 242 021 | 9 853 002 | 54.0% | 5 225 033 | yes |
| PdfJS | 3 190 918 | 1 054 355 | 33.0% | 6 394 984 | — |
| Splay | 1 338 329 | 415 070 | 31.0% | 29 337 | — |
| Crypto | 651 171 | 74 382 | 11.4% | 13 409 653 | yes |
| NavierStokes | 388 | **0** | 0.0% | 11 747 635 | yes |
| **all twelve** | **186 831 813** | **103 158 443** | **55.2%** | **75 704 490** | |

**The premise strengthens and the plan inverts.** *"Half of every property read the cache answers
hands back a number"* goes from 50.1% to **55.2%**, so the item's founding observation is if
anything better than recorded. But **"3-2 is a Box2D item" is wrong**: Box2D is **9.6%** of the
corpus's numeric reads, not 98%. ***3-2 is a Typescript-and-Gameboy item*** — those two are
**64.2 M and 27.4 M numeric reads, 89% of the corpus's between them**, and neither had ever been
counted.

**The box split inverts with it.** *"3-1 carries 85% of the corpus's boxes (NavierStokes' 30.0 M plus
Crypto's 42.4 M)"* — over twelve suites those two are **25.2 M of 75.7 M, 33.2%**. **Gameboy alone is
29.3 M, 38.7%**, the largest single source in the corpus, and it is *not* one of 3-1's suites.

***And `0113` says which item Gameboy belongs to.*** Its dense element read/write ratio is **1.03**,
so a typed backing store there is an allocation wash — while **58.2% of its cache-answered property
reads hand back a number**. **For the corpus's biggest box source, 3-2 is the item and 3-1 is not.**
That is the opposite of the ordering below, and the two measurements were taken independently.

**What survives unchanged.** *"3-2 cannot touch 3-1's suites"* holds exactly as written and is
sharper for the widening: NavierStokes performs **388** property reads, **zero** numeric, against
11.7 M boxes; Crypto reads 651 171 against 13.4 M. Their numbers still live in `new Array` read by
index, and no work on shape slots reaches them. The two items are still one mechanism with two
backends — the identical per-iteration figures above are untouched by any of this.

#### Re-specification

> **Superseded in its ordering by the widened table above**, which was taken after it. The
> *mechanism* argument — one compiler half, two backends — stands; the *ranking* does not.

**3-1 first, then 3-2, and the split is now quantitative rather than a guess.**

- **3-1 carries 85% of the corpus's boxes** (NavierStokes' 30.0 M plus Crypto's 42.4 M) and is
  reachable by nothing else. **3-2 carries Box2D's 11.6 M**, where 54% of reads are already
  numeric. Both are worth doing; only one of them is most of the phase.
- **They are one mechanism.** The identical per-iteration figures say the compiler half — a value
  that stays unboxed from producer to consumer — is shared, and only the storage differs. Building
  either one without that half reproduces 3-1's measured wash, and building the half twice would be
  the waste this measurement exists to prevent.
- **The item's stated composition with 2-1 still holds and is now cheaper than written**: 4-2b's
  specialized read already resolves a monomorphic read to a **literal slot index** on 44.7% of the
  corpus's executed reads, so the site that would consume a raw slot largely exists.
- **`vector.x = 1.5` should be struck from the item's rationale.** It allocates for a reason that
  belongs to `VisitLiteral`, is worth 1.2% of requests, and pointed the item at the wrong half of
  its own mechanism for as long as it stood unmeasured.

### 3-3 · Widen the unboxed-locals eligibility gate — **complete: all three halves landed**

P2-2 item 3 covers a function-top-level `var` not named by any nested closure. Still
ineligible when this item was written: **function parameters**, `let`/`const` (needs TDZ
analysis), and `var` declared inside a block or loop body (needs definite-assignment
analysis).

The item then asserted an ordering: *"**Parameters are the valuable one** — every numeric
helper takes them, and every Octane benchmark is full of numeric helpers. Do parameters
first; treat the other two as separate items."* That is a claim about where the bytes are,
and it had never been measured. Measured now, it is **right about the target and wrong
about the tier**, which changes both what landed and what comes next.

**Where.** `Broiler.JavaScript.Compiler` — `Declarations/FastCompiler.CreateFunction.cs`
(the parameter-binding site and `TryPlanScalarReplacement`), `Scope/FastFunctionScope.cs`.

#### Measured before starting — `--local-alloc`, and it re-specifies the item

`LocalAllocationMetrics` (new; `--local-alloc`) reports bytes per iteration for every place
a number can live in a function, alongside the compiler's own count of how many bindings it
kept scalar. Two instruments on purpose: the counter is exact and settles *whether a shape is
eligible at all*, the bytes settle *what that eligibility is worth*. Every row is net of a
loop control carrying no value under test.

| Site | B/iteration | Numeric locals |
|---|--:|--:|
| `loop-control` | 0.00 | 2 |
| **`top-level-var`** — the only eligible category today | **0.00** | **3** |
| `parameter` | 31.98 | 1 |
| `let-binding` | 31.98 | 1 |
| `const-binding` | 31.98 | 1 |
| `block-var` | 31.98 | 1 |

**Three findings, and two of them change the plan.**

- **All four ineligible categories cost exactly the same.** A `let`, a `const` and a
  block-scoped `var` each cost what a parameter costs, to the byte. So "parameters are the
  valuable one" is not a statement about cost per site, and nothing in the item ever
  established it — the four were ranked by how they were written down, not by what they
  charge.
- **An ineligible binding does not merely fail to help; it de-optimizes the locals
  downstream of it.** The eligible row keeps **3** locals in raw doubles and every
  ineligible row keeps **1**. The accumulator `s` in `s = s + v * 2` stops being provably
  numeric the moment `v` is not, so one ineligible binding costs the specialization of
  everything that reads it. That is a multiplier the item did not have, and it is the
  strongest argument for finishing the gate.
- **The parameter gap is not a box. It is a cell** — and that is the finding the item's own
  title hid. Every parameter was created with `CreateVariable(name, null, …)`, whose default
  type is `JSVariable`, so **every parameter allocated a heap cell on every call**, while a
  `var` in the same function had been scalar-replaced since P2-2. It is not the numeric tier
  of the gate that parameters were missing, it is the *scalar* one.

| Helper called in a loop | B/call | What the pair isolates |
|---|--:|---|
| `h(a) { return a * 2 + 1; }` | 119.99 | binds and reads a parameter |
| `h(a) { return a; }` | 120.01 | **the same cost with no arithmetic at all** — so the cost is the binding |
| `h(a) { var b = 3.5; return b * 2 + 1; }` | 95.99 | a parameter nothing reads, which is elided outright |
| `h() { var b = 3.5; return b * 2 + 1; }` | 95.99 | no parameter — identical, which is what proves the row above |

**And the numeric tier cannot be widened to parameters at all.** A `var` can be proved to
hold only numbers by reading the function; a parameter's value is the caller's choice, and no
analysis of the callee can constrain it. Holding one in a raw `double` needs an entry guard
and a generic fallback — that is speculation, and speculation is **phase 4**. So the item as
written asked for the one thing in its list that this phase cannot deliver, and would have
delivered nothing had it been taken at its word.

#### Landed — a parameter no longer costs a cell

The gate now admits parameters at the scalar tier, on four conditions, and the
simple-parameter-list one is doing more work than it looks:

| Condition | Why |
|---|---|
| `CanScalarReplaceLocals` | the same hazards that stop a `var`: direct `eval`, `with`, `debugger`, a dynamic nested function, async, generator |
| `arguments` named **nowhere** in the function or anything nested in it | a sloppy simple-parameter-list function gets a **mapped** `arguments` object, and the mapping is built out of the parameters' cells. Refused on any mention, because `arguments` is materialized lazily on first reference — long after the parameters are created |
| a **simple** parameter list | rules out defaults, rest and destructuring, and with them every *expression* in the parameter list. Without it, a closure in a default (`function f(a, b = () => a)`) would capture a scalarized parameter, because the hazard detector scans the body and never the parameter list. **A bound, not a heuristic** — it is what lets this reuse the existing analysis instead of extending it |
| not named by any nested function | capturing a binding requires naming it — the same rule, and the same set, `VisitBlock` already applies to `var`s |

**Bytes per call**, from the item's own acceptance test, on a three-parameter helper called
20 000 times:

| | Before | After | Ratio |
|---|--:|--:|--:|
| three-parameter call | 230.2 B | **62.2 B** | **0.27** |
| of which parameter cells | 168.0 | **0** | — |

**56.0 bytes per parameter per call, and it is now nothing.** The `--local-alloc` rows agree
and identify it as per-*binding* rather than per-call: a one-parameter helper drops 56.00, a
three-parameter one 168.00, and **three rows that provably cannot move — the two
no-parameter controls and the numeric-var control — do not move by a byte.** So a one-line
helper's call allocation falls **47% at one parameter and 73% at three**, on every helper in
every program, whatever its parameters hold.

Note what is *not* claimed: the in-loop rows above are unchanged, because a cell is charged
once per call and those loops call once. This item removes an allocation per call, not per
use, and the 31.98 B/iteration those rows report is still owed to the numeric tier.

**Patch 0047's hazard is untouched, and deliberately so.** This item never puts a value in a
raw `double`, so the codegen path that produced **invalid IL** when an unboxed local reached
value position is not on it — `InvalidProgramException` is the signature for the *numeric*
half of this gate, which is the half that did not land. The `NaN <= x` precedent applies
there too, not here.

**Verify.** 57 test cases in `ScalarParameterTests`, weighted to what a cell exists *for*
rather than to hit rates, because a miss there is a miscompilation and shows up as a stale
read rather than a crash: a mapped `arguments` aliasing both directions and a strict one not;
a closure over a parameter seeing a later write and writing back through it; a direct `eval`
reading and assigning one; `with` shadowing one and stopping at the closing brace; and eleven
refusal cases asserting **zero** scalar bindings for the shapes that must keep cells
(defaults, rest, destructuring, generators, async, `debugger`, `var arguments`, an
`arguments` mention reaching in from a nested arrow). Plus duplicate parameter names, a `var`
redeclaring a parameter, a body function declaration overriding one, arity mismatches both
ways, every write form, `typeof`/`delete`, recursion, class and accessor parameters, and a
catch parameter.

**The counter assertions are the ones that pin the gate**, and they were checked against the
build without the item: `ScalarLocals` is **0** for every parameter before and 2 for a
two-parameter function after, and the allocation test fails at 230.2 B/call against its
100 B/call bound. *A criterion that passes before the change measures nothing (§3.5), so this
one was run before the change and watched to fail.* Repository suite: **7 525 tests across 13
projects, 3 failures**, all three the pre-existing win-x64 host-environment ones §4.1 names.

**test262 over all four pinned manifests is unchanged — 8 220 passed, 84 failed, 9 timed out,
identical counts manifest by manifest** (§3.4), which is the gate this item most needed:
`FunctionDeclarationInstantiation` and the Annex B `arguments` mapping are the spec surface it
edits.

**Octane was run too, because this item's justification names it.** 2-8 established that a
benchmark quoted as an item's reason is a test that item has to pass, and 3-3's reason is
"every Octane benchmark is full of numeric helpers" — a claim about calls with parameters,
which is exactly what this changes. **14 of 15 suites `ok`, DeltaBlue and Richards included**,
on win-x64 with results kept out of `tests/octane/results`.

**The fifteenth is Mandreel, and it is not this item — confirmed against a control rather than
assumed.** It fails in phase `Setup` with `RangeError: Maximum call stack size exceeded` at
`EnsureWithinStackBudget` (`CallFrames.cs:215`) from `mandreelAppInit` (`mandreel.js:1460`),
which is the win-x64 signature phase 0 recorded, item 1-2 diagnosed and 2-9 already controlled
for one pointer earlier. Re-run with **only the compiler reverted to `71dda1b7`**, same machine
and same harness, it fails **byte-identically** — the two `OCTANE_ERROR` records compare equal,
so it is the same guard, frame, phase and eleven-frame stack, not merely a similar-looking one.
*A failing suite is a claim; the control is what turns it into a verdict, and the pointer had
moved since the last one was taken.*

> **`RegExp` scored rather than failing its checksum**, which is a change from what 2-8
> recorded as a pre-existing defect. Not investigated here and not claimed as fixed — a single
> run on a different platform is not evidence either way — but it is worth someone confirming
> before that note is relied on again.

**Size: M**, and the half that landed came in at that size. What did not land is below.

> **One pre-existing defect found in passing, neither caused nor fixed here.** A parameter
> named `undefined` does not shadow the global: `(function (undefined) { return undefined; })(1)`
> answers `undefined`, and `typeof` on it answers `"undefined"` rather than `"number"`. It
> reproduces identically with this item reverted, so it is not a regression, and it is pinned
> by `KnownGap_AParameterNamedUndefinedDoesNotShadowTheGlobal` rather than left to be
> rediscovered — a fix flips a failing assertion instead of passing unnoticed.

#### A correctness fix the successor needed first — two writes the analysis could not see

**Found by probing `NumericLocalAnalysis` before extending it, and it is a wrong-answer bug in
shipped code** — present since `a746f82d` landed P2-2 item 3, on every platform, in ordinary
JavaScript. The analysis proves a `var` only ever holds a number and then the compiler keeps it
in a raw CLR `double`. Two ways of writing that binding were invisible to the proof:

| Invisible write | Why | What happened |
|---|---|---|
| A `var` **re-declared** below the function body's own statement list — inside a block, `if`, loop, `while`, `try`, `switch` | it names the same function-scoped binding, but only the *top-level* declarations were recorded as stores; the collector's `VisitVariableDeclarator` visited the initializer as a read and recorded nothing | `var s = 0; { var s = 'x'; } return s` → **NaN** |
| Any name bound through an **object destructuring pattern**, in a declaration *or* an assignment | `AstReduce` treats `ObjectProperty` as a leaf, and `NameCollector` — the walker behind every `RejectEveryNameIn` call — never overrode it | `({ a: s } = { a: 'x' })` → **NaN**; `var { a: s } = …` → **the process aborts** |

**The second failure mode is the serious one, and it is not a wrong answer — it is an unhandled
`System.NotImplementedException`** (*"Assignment target Call (BCallExpression) is not
supported"*) out of `ILCodeGenerator.VisitAssign`, which kills compilation of the whole script
and cannot be caught from JavaScript. That is precisely what the numeric local's own remarks
predict: its readable `Expression` is a **boxing read**, so writing through it is an assignment
to a method call. Three shapes reach it — `var { a: s } = o`, the same nested in a block, and
`for (var { a: s } of …)`. A fourth, `[...s] = ['a']`, threw a bogus `undefined is not a
function`.

**One root cause is shared and it is worth naming.** `ScalarReplacementHazardDetector` and
`NestedFunctionScanner` both carry a comment explaining that `AstReduce` leaves `ObjectProperty`,
`VariableDeclarator` and `Case` as leaves, and both override all three — *"Missing one here is
not a missed optimization but a miscompile."* `NameCollector` is the third walker in the same
family and had none of them. The comment was right, was written twice, and was not applied to
the class that needed it most: `RejectEveryNameIn` is the analysis's only *rejection* path, so a
name it cannot see is a name nothing else will reject either.

**The fix costs nothing measurable**, which is the expected result and worth stating because it
is checkable: all fourteen `--local-alloc` rows are byte-identical before and after and every
numeric-local count is unchanged, because the names now rejected are exactly the ones that were
being compiled wrongly. Ordinary code loses no specialization — `a[i] = v` and `o.x = i` are
asserted by count, not just by answer, since the over-broad version of the pattern rule would
have silently undone 3-0's unboxed index while still computing the right values.

**Verify.** 35 test cases in `NumericLocalWriteVisibilityTests`, written as ordinary JavaScript
answers because every one of them is a value the engine got wrong or refused to compile.
**18 of the 35 fail on the build without the fix**, four of those by aborting the test host —
which is what makes them a pin rather than a description. Repository suite: **7 560 tests across
13 projects, 3 failures**, the pre-existing win-x64 host ones.

*This is why the successor could not start first.* Extending the same analysis to `let`/`const`
without this would have widened a silent-NaN miscompilation to two more declaration forms.

#### What was left of 3-3, and it outranked what landed first

`let`/`const` and the block-scoped `var` were still ineligible, and the measurement moved them
**ahead** of where the item put them, for a reason the item could not have known:

- They cost the same per site as a parameter did — **31.98 B/iteration**, charged per
  *assignment* rather than once per call, so on a loop they dominate what a cell ever cost.
- They can reach the **numeric** tier, which parameters cannot: a `const v = 3.5` at function
  top level is exactly as provable as the `var` beside it, and the TDZ condition is already
  satisfied by the dominance argument `NumericLocalAnalysis` uses today — the declaration
  must be a direct statement of the function body with no textual reference before it.
- And they carry the multiplier: re-qualifying one binding re-qualifies every local
  downstream of it, which is the 1 → 3 in the table above.

So the successor item was **`let`/`const` at the numeric tier first**, then the block-scoped
`var` (which does need the definite-assignment analysis the item names). **Both have now
landed, and item 3-3 is complete**: all four categories the item named are at the eligible
floor except `parameter`, which cannot reach the numeric tier at all — the value arrives as a
`JSValue` and nothing proves it is a number, which is why its half landed at the *scalar* tier
instead.

| Site | Before 3-3 | After all three halves |
|---|--:|--:|
| `top-level-var` — the eligible floor | 0.00 B/iter, 3 | 0.00, 3 |
| `let-binding` | 31.98, 1 | **0.00, 3** |
| `const-binding` | 31.98, 1 | **0.00, 3** |
| `block-var` | 31.98, 1 | **0.00, 3** |
| `parameter` | 31.98, 1 | 31.98, 1 — *scalar* tier only; see the parameter section above |

#### `let`/`const` — **landed on the second attempt**

The first attempt is recorded below, because it was withdrawn on a miscompile and the
instruction it left behind ("find what else decides a lexical binding's storage") is the reason
the second attempt was scoped the way it was. **The second attempt reproduces the number and
not the defect.**

**What it does.** `NumericLocalAnalysis` offers a function-body-top-level `let` or `const` on
the same terms as a `var`; `VisitBlock`'s **numeric** gate admits a lexical name when the block
is the function's own body; and `VisitVariableDeclaration` tests `NumericStorage` *before* the
lexical branch rather than after it.

**What it deliberately does not do, and this is the whole difference from the first attempt.**
The **JSValue tier stays closed to lexical names.** The two tiers are not interchangeable:

| | JSValue tier (`useScalarLocal`) | Numeric tier (`useNumericLocal`) |
|---|---|---|
| admits a name because | it is an ordinary local nothing captures | the analysis **proved** it only ever holds a number |
| TDZ | nothing proves the dead zone unobservable | the dominance argument does — any name referenced before its declaration is rejected, so the throw is **unreachable**, not removed |
| const-ness | nothing proves no write happens | a const written anywhere is rejected outright, so there is no assignment whose `TypeError` could go missing |

A `let`'s dead zone and a `const`'s read-only-ness are both properties of the `JSVariable`
**cell** that either tier removes. Only the numeric tier's gate discharges them, so only it may
admit a lexical name.

*Whether the first attempt relaxed the shared condition instead is an **inference**, not
something checked — the branch was not kept, which is precisely why this section exists. It is
offered as the most likely reading of its recorded symptom ("none of those nested bindings is
one the gate admits") and should not be repeated as fact.*

**Measured, both arms from one tree, `--local-alloc`:**

| Site | Before | After |
|---|--:|--:|
| `let-binding` | 31.98 B/iter, 1 numeric local | **0.00 B/iter, 3** |
| `const-binding` | 31.98 B/iter, 1 numeric local | **0.00 B/iter, 3** |
| every other row (12 of them) | — | **byte-identical, numeric-local count unchanged** |

— identical to `top-level-var`, the eligible floor. The multiplier the section above predicts
is the second column: one binding re-qualified, and the accumulator and counter that read it
came with it.

**On the withdrawn attempt's defect: it did not reproduce, and it is not explained.** The
recorded reproduction was re-run against this implementation — two evaluations in one process,
fresh `JSContext` each, which is the only configuration that could ever see it — and all three
lines answer correctly. It was then re-run with `BROILER_JS_REWRITER_INDEX_THRESHOLD` set above
any real scope and with `BROILER_JS_DEFER_IL=0`, which between them restore the pre-1-4 and
pre-1-1 front end, since both landed *after* the withdrawal and `LambdaRewriter` was the one
plausible place a binding's storage is decided outside the gate. Green under all four
configurations. The three compiler files this item touches are **byte-identical between
`2ebc0c3c` (where the attempt was made) and the current pin**, so the tree did not fix it
either. Widening the JSValue tier as a deliberate experiment did not reproduce it.
**So the honest statement is that the second attempt avoids the defect rather than fixes it**,
and the reproduction below is kept as a pinned test rather than retired — it costs one test and
it is the only thing that would catch a recurrence.

#### The first attempt, **withdrawn** — kept because the next one started from it

It was built, it measured exactly as predicted, and it miscompiles. Recorded here rather than
left as a branch, because the next attempt should start from the evidence.

**What worked.** Offering a function-body-top-level `let`/`const` to `NumericLocalAnalysis` and
admitting a lexical name in the function body block only:

| Site | Before | After |
|---|--:|--:|
| `let-binding` | 31.98 B/iter, 1 numeric local | **0.00 B/iter, 3** |
| `const-binding` | 31.98 B/iter, 1 numeric local | **0.00 B/iter, 3** |

— identical to `top-level-var`, the eligible floor, with **every other `--local-alloc` row
unchanged**. The multiplier the section above predicts is visible in the second column: one
binding re-qualified, and the accumulator and counter that read it came with it. Semantics held
in single-compilation runs: the `const` reassignment `TypeError`, the `let` TDZ
`ReferenceError`, and the nested-shadowing dead zone all still fired, byte-identical to the
baseline.

**Two obligations it had to discharge, and both were fine.** The **TDZ** is discharged by the
dominance argument the analysis already makes — a name with any reference before its
declaration is rejected, so the throw is unreachable rather than removed. **Const-ness** needed
one addition: a write to a const is a `TypeError` raised by the binding's *cell*, so a const
written anywhere was rejected outright rather than specialized into a silent store.

**What is wrong.** After **any** earlier compilation in the same process — a different
`JSContext`, a different source — a `let` declared in a *nested block* reads back as an
uninitialized double:

```js
// First, in one JSContext:
(function () { let v = 3.5; v = v + 1; return v; })()      // → 4.5, correct

// Then, in a fresh JSContext in the same process:
(function () { let v = 1; { let v = 2; return v; } })()    // → 2.0000000074796844
(function () { { let v = 2; return v; } })()               // → 2.0000000074796844
(function () { let v = 1; { let w = 2; return w; } })()    // → 2.0000000074796844
```

**The tell is the third line: none of those nested bindings is one the gate admits.** A lexical
name is applied in the function body block only, so `{ let v = 2; }` must get a cell — and it
does not. **So a lexical binding's storage is decided somewhere other than that gate**, and
until that is found no amount of tightening the gate is a fix. Three hypotheses were eliminated:
it is not a specific predecessor (any one will do), not a compile count (64 preceding
compilations in a fresh context each are harmless), and not repetition of the same source. The
value's shape is a clue worth keeping — the high bits read as the right integer and the low
mantissa bits are garbage, which is a slot written narrower than it is read.

**One real bug was found and fixed on the way there**, which is why the attempt was worth
making even though it did not land: the lexical declaration path assigns through the binding's
value setter, and for a numeric local that setter is a **boxing read** — so the first build of
this threw `System.NotImplementedException: Assignment target Call (BCallExpression) is not
supported` out of `ILCodeGenerator.VisitAssign`. That is patch 0047's hazard family, exactly
where this item's own *Watch* note said to look, and the fix is to test `NumericStorage` before
the lexical branch rather than after it.

**What the next attempt did with this.** Three of the four instructions this section left were
followed and one was overtaken. Kept: the `NumericStorage`-before-lexical ordering in
`VisitVariableDeclaration`, the const-write rejection, and re-running the reproduction **as two
evaluations in one process** — which is exactly why the landed change has
`ALexicalBindingIsUnaffectedByAnEarlierCompilationInTheSameProcess` rather than a single-eval
test that would have been green either way. Overtaken: *"find what else decides a lexical
binding's storage"*. It was looked for at the two named places and not found, and the four
configurations above rule out the front end as well; what the second attempt changed instead
was **which tier** may admit a lexical name at all. See the landing section above for why that
distinction is the load-bearing one, and for the plain statement that the defect is avoided
rather than explained.

`const` did turn out to be the cheaper half, as this section predicted — it cannot be
reassigned, so its analysis reduces to checking the initializer plus rejecting any write — but
it was cheap enough that separating it from `let` bought nothing, and the two landed together.

**Verify.** `LexicalNumericLocalTests`, 58 cases in eight groups: that the gate admits `let` and
`const` *to the same numeric-local count as `var`* (without which every other case here passes
vacuously); arithmetic over the values doubles make awkward (NaN, both zeroes, the infinities,
`2**53`); the refusals — TDZ reads, every form of writing a `const`, a binding that later holds
a string/object/null/undefined/BigInt, and a captured one; the nested-block shapes, including
all three reproduction lines and a nested block's own dead zone; `for (let i …)` binding per
iteration, which a single raw double cannot represent; and the sharpest shadowing case, a
`for`-head `let` sharing its name with an eligible body-level one, where **both halves are
numeric so nothing about the type distinguishes them** and conflating them returns the loop's
final value instead of the outer binding's; and the two function kinds whose locals are not
ordinary CLR locals — a **generator**, where a lexical value has to survive a `yield` because
the body is rewritten into a state machine, and an **arrow**, whose concise form has no body
block at all, so the body-block test must fail to match rather than misfire. Repository suite:
**7 698 tests across 13 projects, 0 failures** on linux-x64, with the patch applied to a
clean checkout of the pin.

**And it exposed a gap in the conformance gate, which is closed here rather than noted.** No
pinned manifest covered `let`/`const` at all — `test262-language-basics` is twelve entries about
`throw`, commas and relational operators — so a change to how lexical bindings are *compiled*
had nothing in §3.4 that could fail. `scripts/compliance/test262-lexical-declarations.txt` adds
`language/statements/{let,const,variable}` and `language/block-scope`; it is **397 of 397
passing on both arms** (§3.4), so it reports nothing today and guards those paths from here on.

#### The block-scoped `var` — **landed, and it completes item 3-3**

> **In the pin.** Shipped as `patches/0068` while its push was blocked by a 403; since applied
> and pushed, and the pointer bumped — it is commit `f566b30d`, an ancestor of `61c8cc65`. The
> figures below were taken on a local build of `9bf9639b` plus `0067` with and without `0068`,
> both arms from the same tree, and they describe the pinned pointer directly now that both have
> landed.

This is the half the item said needs "definite-assignment analysis", and what it actually needs
is the **dominance argument the function body already gets, applied one level down**. The hazard
is exact: a `var` is hoisted to the function but its initializer sits inside a block, so between
function entry and that block the binding is observably `undefined` — and a raw double hoisted
to 0 answers `0` instead. That is a silent wrong answer, not a lost optimization.

**Two admissions, and they are different arguments.**

| | Transparent | Confined |
|---|---|---|
| shape | an unlabelled `{ … }` that is a direct statement of the function body, or of another transparent block | a `var` that is a direct statement of any other block |
| why the initializer has run | the block is entered whenever control reaches it, and the only ways out are `return`/`throw`, which leave the function — so it does not weaken the body's dominance at all | entering the block is itself the proof |
| extra condition | none; the name behaves exactly like a body-level `var` | **every reference must be inside that block**, and after the declaration |
| what it buys | the item's own probe shape — `{ var v = 3.5; }` then a loop that reads `v` | the case that matters in real code: a temporary declared and consumed inside a loop body |

**Measured, both arms from one tree, `--local-alloc`: `block-var` 31.98 → 0.00 B/iteration and
1 → 3 numeric locals**, identical to `top-level-var`, with **all twelve other rows byte-identical
and every other numeric-local count unchanged** — one row moved, which is the whole diff.

**Only a *direct* statement of the block qualifies, and that is load-bearing.** Keying on the
innermost *enclosing* block instead would admit `if (c) var t = 1; return t;` — whose enclosing
block is the function body, which does not dominate the declaration — and answer `0` where the
program sees `undefined`. A label is excluded for the same reason: `break` can leave a labelled
block before the declaration runs, and a labelled block is an `AstLabeledStatement` rather than
an `AstBlock`, so the transparency test does not match it. A `catch` is excluded because it is a
sibling of its `try`'s block, not inside it.

**One hazard was found by testing and would have shipped otherwise.** The first cut marked a
name readable at *whichever* declaration the walk reached first, which is how the existing
analysis has always worked — sound while every declaration dominates. It stops being sound once
a name can have both a dominating and a non-dominating declaration:

```js
if (c) { var t = 1; }      // non-dominating, but reached FIRST
var r = String(t);         // → "undefined", and a raw double answers "0"
{ var t = 2; }             // dominating, and what made the name a candidate
```

The fix is one line of principle — **a name becomes readable at its dominating declaration, not
at any other declaration of the same name** — and it is why the analysis now records which
initializer nodes are the dominating ones rather than only which names are.

**And the fix for that immediately over-corrected, which a pre-existing test caught.** Making
transparent blocks offer their declarations turned `var s = 0; { var s = 5; }` into a
"declared twice" rejection, failing
`NumericLocalWriteVisibilityTests.ANumericReDeclarationKeepsTheLocalSpecialized` — a test written
for exactly this, whose comment reads *"this is the guard against over-fixing"*. It was right:
two declarations that **both** dominate are not a hazard, because each dominates everything
after itself and the type proof still runs over both values. That rejection was over-conservative
even before this item — its stated reason ("the second may sit somewhere the first does not
dominate") never applied to its own call site — and it is now gone.

**Verify.** `BlockScopedVarNumericLocalTests`, 43 cases: the admissions with their numeric-local
counts asserted (a value-only assertion passes vacuously here, since the right answer and the
wrong storage often agree); and the refusals, each written as a value the program can observe —
`String(t)` answering `"undefined"` where a raw double answers `"0"`. The refusals are the
block that may not run, the declaration with no block of its own, the labelled block, the
`catch` reading its `try`'s declaration, the reference before the declaration, two declarations
in incomparable blocks, and a confined name reached from outside its block through `+=`, `++`
and `typeof` — the three forms that never reach an ordinary identifier read. Repository suite:
**7 741 tests across 13 projects, 0 failures** on linux-x64.

### 3-4 · A tagged value representation — *scope and cost, do not start*

The real fix, and a multi-quarter redesign of the engine's most fundamental type with
every built-in downstream of it. An `ownership.json` entry (`tagged-js-value`) already
exists from the earlier campaign.

**Write it up and cost it at the end of phase 3**, once 3-1 to 3-3 have shown how much
of the gap survives unboxed arrays, fields and locals. It is entirely possible the
answer is "less than expected", and that is worth knowing *before* committing to the
redesign rather than after. **Size: XL.**

### 3-5 · A numeric local compared against a JSValue — **landed, 3.4× on its shape; and it measured the ceiling on all of phase 3**

Item 4-5's probe produced this item by accident: the control loop every measurement in this
document has used as a *floor* was itself paying a box per iteration, and the same loop with a
literal bound ran at **8.36 ns and 0 B** against **33.77 ns and 32 B**.

#### The cause is not the parameter, and that changes the fix

3-3 recorded the gap as a property of parameters: *"All four of the item's categories are now at
the eligible floor except `parameter`, which cannot reach the numeric tier at all."* True, and it
is not what costs the box. `i` **is** a numeric local — a raw CLR double. `n` is a `JSValue`. The
compiler had a native form for `<` only when **both** operands were already doubles, so the mixed
case fell through to the generic operator and **boxed the raw `i`** to meet it.

So the fix is to unbox the *other* side rather than to make the parameter numeric: test the value
side, compare two doubles when it is a number, and take the ordinary operator when it is not. That
needs no entry guard and no second body — and it covers strictly more, because
`for (var i = 0; i < a.length; i++)` is a property read, not a parameter, and was boxed for exactly
the same reason.

**Sound because ToPrimitive of a Number is that Number.** Relational comparison runs ToPrimitive on
both operands first; when the value side is already a primitive number that step calls no
`valueOf`, no `toString`, and has no observable effect. So the guarded path is the same path with
the same answer, and everything else reaches the operator it reached before. **Only `<` and `>`**,
for the reason the neighbouring code already records: the backend emits an ORDERED compare for
`<=`/`>=`, which answers true on NaN where JavaScript answers false.

**Block-declared locals, not pooled temporaries, and that is a correctness point.** Both operands
are spilled — the value side is read twice (test and unbox) and the native side is read in both
arms — and the temporaries are needed *after* the operands have already been compiled. A pooled
temp could therefore be one a sub-expression released while being built, and the second spill would
clobber the first operand. `i < obj.m()` is enough to reach it. Declaring locals in the block cannot
collide with anything.

**Verify.** `MixedNumericComparisonTests`, 33 cases, all about semantics and none about speed:
every relation in both directions and with the native side on either side of the operator; NaN on
each side; ±0; ±Infinity; strings, `null`, `undefined`, booleans, arrays, objects, a `Number`
wrapper (not a primitive, so it must take the fallback), a BigInt, and a Symbol that must still
throw; `valueOf` called **exactly once** per comparison and only on the fallback; source-order
evaluation with each operand evaluated once; a throwing `valueOf`; a loop bound that is not a
number; and a bound whose type **changes mid-loop**, since the guard is per evaluation and not per
site. **All 33 pass on the unmodified compiler too** — they pin the existing semantics, they do not
describe the change. Repository suite: **7 872 tests across 13 projects, 0 failures**.

**On its shape it is large.** The counted loop with a parameter bound: **33.77 → 10.03 ns and
32 → 0 B per iteration, 3.4×.** Every probe shape in this document drops the same 32 B.

#### On the corpus it is invisible, and *why* is the finding

Paired Octane runs, four rounds, allocation exact: **0.997× bytes and 0.995× time.** 15.7 MB saved
of 4 487 MB — about 490 000 boxes avoided, against a corpus that performs 37.9 M property reads.

The compile-time counts say the sites exist. **390 relational comparisons take the new form, 59% of
those that could** — so the emission is not the problem. The problem is what is on the other side:

| Suite | Scalar locals | Numeric locals | Share |
|---|--:|--:|--:|
| Richards | 117 | 10 | 8.5% |
| DeltaBlue | 176 | 19 | 10.8% |
| RayTrace | 233 | 17 | 7.3% |
| Box2D | 1 774 | 66 | 3.7% |
| EarleyBoyer | 1 011 | 44 | 4.4% |
| Crypto | 521 | 24 | 4.6% |
| NavierStokes | 197 | 23 | 11.7% |
| **All seven** | **4 029** | **203** | **5.0%** |

> **Five per cent of scalar locals in the Octane corpus reach the numeric tier.**

That is the ceiling on **all** of phase 3's local work — 3-0, 3-3 and 3-5 alike — and nothing in
this document had measured it. Every one of those items is correct, tested, and demonstrably large
on the shape it targets; each one then meets the same gate.

> **Correction.** This section first named `CanScalarReplaceLocals` as the gate that costs the
> coverage — "no nested functions, no captured names, no `eval` and no `with`, and real code has
> those nearly everywhere". **Item 3-6 counted it and that is wrong: it rejects 2 names out of
> 2 695.** The real causes are below, in 3-6. The claim is left here struck rather than deleted
> because it is exactly the kind of plausible reading-of-the-code that this document keeps having
> to correct with a count.

#### Re-specification

- **New 3-6 (L): widen numeric-local eligibility.** At 5.0% coverage this is the **multiplier** on
  every local-representation item already landed, and it is worth more than any of them
  individually. The gate is a conjunction inherited from scalar replacement; the question is which
  conjunct actually costs the coverage, and that is a measurement — count the locals each conjunct
  rejects — not a design. **Do that count first**, on the evidence of the last three items that
  measuring a premise keeps changing what gets built.
- **3-4 stays "do not start", and now for a stated reason.** Its own instruction is to cost it
  *"once 3-1 to 3-3 have shown how much of the gap survives unboxed locals"*. The answer is that
  the gap largely survives, because the unboxing reaches 5% of locals — so the question 3-4 was
  told to wait for is answered, and it points at 3-6 rather than at the XL redesign.

### 3-6 · Which conjunct costs the coverage — **counted, and it is none of the ones the item named**

3-5 measured numeric-local coverage at 5.0% of scalar locals and blamed the scalar-replacement
gate. 3-6's whole instruction was to **count before designing**, because the last three items had
each been re-specified by their own premise. It was right to insist: the count says the item was
looking in the wrong place, and then says so a second time one level down.

#### The waterfall

Every hoisted name in the seven suites, attributed to the **first** conjunct of the numeric-local
gate it fails — a waterfall rather than overlapping tallies, so the numbers add up and each one
reads as "widen this and at most that many names become eligible":

| | Names | Share |
|---|--:|--:|
| **Accepted — became a raw `double`** | **203** | **7.5%** |
| Not proven numeric | 2 012 | 74.7% |
| Captured by a nested function | 478 | 17.7% |
| Function not scalar-replaceable | **2** | **0.1%** |
| Direct-eval root | 0 | — |
| Not in a function | 0 | — |
| Named `arguments` or `eval` | 0 | — |
| `let`/`const` outside the function body | 0 | — |
| **Total hoisted** | **2 695** | |

**`CanScalarReplaceLocals` rejects two names.** Async, generator, `eval`, `with`, `debugger` and
dynamic nested functions — the conjunction 3-5 named, and the same one that bounds phase 4's
tiering candidates — cost **0.1%** of the coverage. That claim is now corrected where it was made.

#### And "not proven numeric" is not what it sounds like either

The obvious reading of 74.7% is that most locals simply are not numbers, and that there is nothing
to fix because no analysis makes a string a double. Counted inside the analysis, that reading is
also wrong:

| | Names |
|---|--:|
| Offered as numeric candidates | **2 335** |
| **Dropped by the optimistic fixed point** | **1 842** |
| Surviving the analysis | 493 |
| Never offered at all | ~170 |

**Only ~170 names of 2 695 — 6.3% — are rejected because their declaration is not numeric.** The
analysis *offers* 2 335 and then drops **1 842 of them, 78.9%**, in the fixed point: a candidate is
dropped as soon as any assignment to it cannot be proved numeric under the current assumption.

The two counts also reconcile, which is what says neither is measuring the wrong thing: 1 842
dropped plus ~170 never offered is the 2 012 the waterfall attributes to *not proven numeric*, and
the 493 survivors minus the 203 accepted is **290 names that the analysis proved numeric and the
hoist site then refused** — all of them to the captured-by-a-nested-function conjunct.

> **Both of those numbers are wrong, and 3-7 found out how.** "493 survivors" is *offered minus
> dropped*, and `Resolve` removes a **third** population between those two counters — every name a
> rejection path named — which had no counter at all, so the subtraction silently counted it as
> zero. With the counter added, the same corpus reads **offered 2 295 = rejected 133 + dropped
> 1 916 + surviving 246** (as corrected by 3-8, which found the offer double-counted across
> nested functions): the survivors are 246, not 493, and the residue refused at the hoist site is
> **22 names, not 290**. The reconciliation this paragraph claims is real but circular —
> both figures were derived from the same two counters, so agreeing with each other told nobody
> that a third term was missing. See 3-7, and §3.5's *"a count you inferred is not a count"*.

#### What that leaves, and it is two different problems

- **The fixed point's 1 842 (68% of all hoisted names) is a *provability* wall, not a gate.** A
  candidate is dropped because something assigned to it comes from a parameter, a property read, an
  element or a call — values whose type is not knowable statically. That is precisely the wall 3-5
  hit from the other side, and no amount of widening a conjunction reaches it. **Making those
  numeric needs a runtime guard, which is a phase 4 mechanism applied to a phase 3 representation**
  — and 4-3b's in-method branch is exactly the facility for it. Worth stating plainly: **the
  largest single obstacle in phase 3 is shaped like phase 4.**
- **The 290 provably-numeric names refused for being captured is a bounded, purely static
  opportunity.** A closure captures through a cell, so a numeric local that any nested function
  mentions keeps its `JSVariable`. Giving those a raw-`double` cell instead would take numeric
  locals from **203 to ~493 — 2.4×** — with no speculation and no guard. That is the only part of
  3-6 that is a widening in the sense the item meant.

#### Re-specification

**3-6 as written is answered and closed**: the conjunction it proposed to widen costs 0.1%. What it
found splits into two successors, and the count is what says which is which:

- **3-7 (L): a raw-`double` cell for a captured numeric local.** 290 names, 2.4× numeric coverage,
  entirely static. The obvious first item, and the one to size next. **Built: it is 8 names, and
  "entirely static" was the part that was wrong** — half the captured names are held by a *hoisting*
  rule that no static widening can touch, and lifting the conjunct exposed two wrong answers. See
  3-7.
- **3-8 (XL, and it belongs to phase 4's machinery): guard a local's numeric-ness at run time.**
  The 1 842 dropped candidates are dropped for want of a *type*, not for want of a rule. This is
  4-3b's in-method branch pointed at a local's representation rather than at a property read, and
  it should not start before 3-7 says how much of the gap the static half closes.

**Nothing is built for this item**, deliberately. Its own text said to count first, and the count
retired the design it was going to justify — for the fourth item running, which is now less a run
of luck than a description of how this campaign works.

---

### 3-7 · A raw-`double` cell for a captured numeric local — **landed, and its own premise was wrong twice**

3-6 handed this item a number and a claim: **290 names, `203 → ~493`, 2.4×, "entirely static"**,
and called it "the obvious first item". Built and measured, the widening is worth **eight names —
`224 → 232`, 1.036×** — and getting there found **two wrong answers** that the item's "entirely
static" reading had no room for. Both halves of the premise failed, in opposite directions: the
mechanism is *cheaper* than the item thought (nothing had to be built for the cell at all) and the
population is **36× smaller**.

**Where.** `Broiler.JavaScript.Compiler` — `Statements/FastCompiler.VisitBlock.cs` (the gate),
`Declarations/FastCompiler.CreateFunction.cs` (the new hoisted-capture set),
`Declarations/NumericLocalAnalysis.cs` (both correctness fixes), `Scope/FastFunctionScope.cs`,
`CapturedNumericLocals.cs` (the A/B switch).

#### The cell already existed, which is the one part that was easier than written

The item is titled "give a captured numeric local a raw-`double` **cell**", and no cell had to be
written. The expression compiler already rewrites any CLR local a nested lambda references into a
`Box<T>` (`ClosureSeparator/Box.cs`, `LambdaRewriter.CheckForClosure`), and **`Box<double>` *is*
the shared cell a closure needs** — allocated once per activation, read and written through by
every closure over it. So a captured numeric local is *one* allocation where the `JSVariable` form
is two (the cell, plus the box the closure reads the cell through), and the change at the gate is
the removal of one conjunct.

That also answers a question the gate never stated: the JSValue tier refuses captured names too,
and **not** because sharing would break — `Box<JSValue>` would share just as well. It refuses them
because that tier has no cell at all, and a cell is what a TDZ, a `const`'s TypeError and a
`delete`d eval binding *are*. The numeric tier's gate proves each of those unreachable, which is
why the widening applies there and only there.

#### Two wrong answers, both found by running the widening rather than by reading it

The numeric tier's soundness rests on a **textual** argument: a name with any reference before its
declaration is refused, so the initializer has always run by the time anything reads the binding,
and a raw double hoisted to `0` is never observed where `undefined` belongs. Capture breaks the
link between text order and execution order, and 3-6's "entirely static" description is exactly
the reading that misses it.

- **A hoisted function declaration exists before the body runs.** Its body is textually *after*
  the declaration — so the analysis accepts it — and its function object exists at function entry,
  so it can run *before* the declaration:

  ```js
  function f() { var r = g(); var s = 0; function g() { return s; } return String(r); }
  ```

  `f()` is `"undefined"`. With only the gate widened it returned **`"0"`**. The fix is one more
  conjunct — a name mentioned by a function declaration at *body top level* keeps its cell — and
  it is deliberately **not** behind the switch, because it is correctness rather than policy. Only
  body-top-level declarations qualify: one inside a block, `if`, loop, `try` or `switch` has its
  *binding* hoisted (Annex B B.3.3.1) but not its *value*, so calling it early is a `TypeError` on
  `undefined` and never a read. A declaration textually *before* the numeric one is already
  refused by the analysis, so no position comparison is needed.

- **A declaration inside a nested function is a different binding.** `NumericLocalAnalysis`
  deliberately conflates names across nested functions — that is what makes a closure's
  `s = 'x'` drop an outer numeric `s` — but the conflation ran in the *initializing* direction
  too, so a nested function's own parameter opened `declared` for the outer name:

  ```js
  function f() { var r; { var g = function (t) { return t; }; r = String(t); var t = 5; } return r + ',' + t; }
  ```

  `"undefined,5"`. With only the gate widened, **`"0,5"`**. Fixed by suppressing `declared` at
  nested-function depth; writes are still recorded at every depth, which is the half that has to
  stay conflated.

A third defect was a *compile* failure rather than a wrong answer, and it is the same shape as the
first two: **a function declaration stores a function object into the very binding being typed**,
and a declaration is not an assignment expression, so the walk never saw the store.
`let f = 5; { function f() {} }` reaches that binding through Annex B's copy-out and died on
*"Assignment target Call (BCallExpression) is not supported"* — the write had been aimed at a
numeric local's *reading* expression, which boxes. It was covered by accident until now: a
declaration mentions its own name, so the name counted as captured and was refused for that.
**All three had been sitting behind the capture conjunct, and lifting it is what exposed them** —
which is the sharpest form of §3.5's rule about a conservative bug passing its own tests.

#### The count, and why 3-6's 290 was not there

Same waterfall as 3-6, over the same seven suites, with the capture row split by whether the
mention is hoisted. The **off** column reproduces the pinned pointer — `224` numeric locals,
`4 521` scalar, `2 920` hoisted names, and every conjunct identical except the capture row, which
the new counter splits (the pin reports its 478 undivided) — so the two correctness fixes above
cost **nothing** in coverage:

| Conjunct | off | on |
|---|--:|--:|
| **Accepted** | **224** | **232** |
| Not proven numeric | 2 216 | 2 439 |
| Captured by a **hoisted** function declaration | **247** | **247** |
| Captured by a nested function (other) | **231** | 0 |
| Function not scalar-replaceable | 2 | 2 |
| **Total hoisted** | **2 920** | **2 920** |

3-6's 478 captured names **split almost in half: 247 of them (51.7%) are named by a hoisted
function declaration** and can never be widened, because that conjunct is correctness. Of the 231
that remain, **223 are not proven numeric** and **8 become raw doubles**.

**3-6's 290 was inferred, not counted, and the inference had a missing term.** It read survivors as
*offered minus dropped* — 2 335 − 1 842 = 493 — and then 493 − 203 = 290. But `Resolve` removes a
third population between those two counters: every name a rejection path named (read before its
initializer, bound through a pattern, `delete`d, a written `const`, a for-in head) leaves in
`ExceptWith(rejected)`, which **had no counter at all**. Counted directly, on the same corpus:

```
offered 2 295  =  rejected 133  +  dropped by the fixed point 1 916  +  surviving 246
```

It reconciles exactly, and the survivor count is **246, not 493**. (Those `offered` and `rejected`
figures were first published as 2 521 and 359; item 3-8 found the analysis was offering a nested
function's block-scoped `var`s to its *enclosing* function as well as to its own, so both were
inflated. `dropped`, `surviving` and every figure this item rests on are unchanged — the
double-counted names were all rejected anyway.) Since 224 of those are already
accepted, **only 22 provably-numeric names are refused at the hoist site for any reason at all**,
and 14 of the 22 are the hoisted-capture ones. So the item's population was never 290; it was 22,
of which 8 are reachable. *An inferred count and a measured one are different kinds of number, and
3-6 said its two counts "reconcile exactly" — they did, to each other, while both omitted the same
term.*

#### What it is worth, and it has an exact losing side

`--local-alloc` gains a `capture` category — four spellings of `top-level-var` differing only in
how a closure names the value. Deterministic, exact, net of the loop control:

| Site | off | on | |
|---|--:|--:|---|
| `captured-var` | 63.97 | **0.01** | the value used by the enclosing function's own arithmetic |
| `captured-var-written-in-closure` | 127.93 | **0.02** | ...and written through the closure each iteration |
| `captured-var-read-in-closure` | 31.99 | **63.99** | **the losing side**: read *through* the closure each iteration |
| `captured-var-hoisted-fn` | 63.97 | 63.97 | what the correctness conjunct costs — the whole win, on that shape |
| `call-captured-var` (per **call**) | 3 135.99 | **3 023.99** | −112 B an activation: the `JSVariable` and the boxing of its arithmetic |

Every **per-iteration** delta is an exact multiple of 32 B, which is what says the model is right
rather than approximately right: two boxes an iteration removed, four when the closure writes, and
**one box added per read made through a closure** — a raw double has to box to hand a JSValue back
where a `JSVariable` returns the one it already holds. The per-activation row is the one that is
not, and for the expected reason: its −112 B is two of those boxes plus the `JSVariable` object
itself, which is not 32 bytes.

Timed the same way, one shape per process (ten rotations, four samples a rotation):

| | off | on | |
|---|--:|--:|---|
| the winning shape | 309.0 ms | **41.0 ms** | **0.1327×** |
| the same loop with no closure (control) | 43.0 ms | 41.0 ms | 0.9535× — same code both arms, the noise floor |
| **shape ÷ control** | **7.19×** | **1.0000×** | capture cost 7.2× on this shape and now costs nothing |
| the losing shape | 554.0 ms | 615.5 ms | **1.1110×** |
| its control (closure reads a literal) | 608.0 ms | 606.5 ms | 0.9975× |

**`shape ÷ control = 1.0000` is the result**: a captured numeric local now runs at exactly the
speed of the same loop with no closure at all, which is the floor this whole family aims at. The
losing side is real, bounded and priced — 11% and one box per closure read — and it bites only a
closure whose body hands the raw value straight back out; a closure that *computes* with it
resolves through the same `NumericStorage` and never boxes.

**The first measurement of this had to be thrown away**, and the tell was in the control: run in
one process, the shape and its control moved *together* (control 1.2857×), because the off arm
allocates 192 MB over the loop and its collections are charged to whichever function runs next.
That is §3.5's `--compile-profile` artifact one level down, and the rule is the same — **one shape
per process**.

#### On the corpus it is invisible, for the third item running

Seven Octane suites, driver run, allocation deterministic: **1.0001×**. Eight names of 2 920.
That is not a failure of the change, it is the same ceiling 3-5 and 3-6 measured from two other
directions, and the count now says where the rest of it is: **2 439 names are not proven numeric
and 247 are held by a hoisting rule no analysis can widen**. Nothing left in phase 3 is a matter
of loosening a conjunction.

The suites were **run, not merely compiled** — 2-8's lesson, which broke DeltaBlue by measuring a
loop that resembled it. All seven load and all nine benchmarks complete with no failures on the
pinned build, the off arm and the on arm alike.

**Shipped on by default, with `BROILER_JS_CAPTURED_NUMERIC_LOCALS=0` to restore the cell**, on the
same terms as `BROILER_JS_DEFER_IL`: the change has a losing side, so it has to be measurable
against a build that differs in nothing else, and every figure above is a pair from one tree.
`CapturedNumericLocalTests` is 24 cases — the three defects above, sharing across two closures,
one box per activation, a `var` a loop closes over, generators and `async` bodies (rewritten by a
different path), `try`/`catch`/`finally`, recursion, NaN / ±0 / ±Infinity, and a closure that
stores a string, an object, `undefined` or a destructured value — **each asserted on both settings
of the switch**, so they are a regression guard and not a description of the optimization. The
probe scripts they were written from answer **identically on a pristine build of the pinned
pointer**.

#### Re-specification

**3-7 is answered and closed.** What it leaves:

- **The 247 hoisted-capture names are closed, not deferred.** A raw double cannot represent
  `undefined`, and a hoisted declaration can observe the binding before its initializer runs. The
  only way to reach them is a representation that carries an *uninitialized* state — which is a
  tagged value, i.e. **3-4**, and 3-4 is a cost rather than a task.
- **3-8 is now the whole of what is left in phase 3**, and its size grew: the 2 439 names not
  proven numeric are 83.5% of every hoisted name, against the 8 this item moved. Its mechanism is
  4-3b's in-method branch pointed at a local's representation, and this item is the evidence that
  nothing static gets there: the last three attempts on phase 3's static coverage have moved the
  corpus by nothing — 3-5 at 0.997×, 3-6 which counted first and found its own design had nothing
  to widen, and 3-7 at 1.0001×.

---

### 3-8 · Guard a local's numeric-ness at run time — **3-8a is built complete, measured, and closed as a regression**

3-6 specified this from one sentence: *"the 1 842 dropped candidates are dropped for want of a
**type**, not for want of a rule"*, sized it XL, and named 4-3b's in-method branch as the
mechanism. 3-7 then made it "the whole of what is left in phase 3". Counted before building any of
it — the sixth item running to be re-specified by its own premise — the item is **well-founded
mechanically and aimed at almost none of the prize**, and the two measurements that say so are
ones nobody had taken.

**Where.** `Broiler.JavaScript.Compiler` — `Declarations/NumericLocalAnalysis.cs` (the drop-cause
classifier and one bookkeeping fix), `CompilerSpecializationDiagnostics.cs`,
`NumericLocalSpecialization.cs` (the whole-tier switch); `Broiler.JavaScript.BuiltIns` —
`Number/NumberBoxingDiagnostics.cs`, `Number/JSNumber.cs`.

#### The first number nobody had: how much of a real run is number boxing at all

Every phase 3 item so far was sized by a per-shape figure — **31.98 bytes an iteration**, reported
by 3-3 for four categories, by 3-5 for its comparison, by 3-7 for capture, and now by 3-8 for all
three of its own causes. The same number, over and over, and every item then moved the whole corpus
by nothing. The figure that would have explained that is what share of a real workload's allocation
is number boxing, and no counter existed for it. `NumberBoxingDiagnostics` (off by default) counts
every call to `JSNumber.Create`, split by whether the small-integer table answered it:

| Suite | allocated | boxing requests | cached | fresh boxes | **boxes as share of allocation** |
|---|--:|--:|--:|--:|--:|
| NavierStokes | 1 074 MB | 38 153 253 | 8 175 788 | 29 977 465 | **66.96%** |
| Crypto | 1 845 MB | 74 249 073 | 31 839 549 | 42 409 524 | **55.16%** |
| Box2D | 763 MB | 18 854 548 | 7 419 842 | 11 434 706 | **35.98%** |
| RayTrace | 420 MB | 1 658 272 | 817 255 | 841 017 | 4.80% |
| EarleyBoyer | 706 MB | 782 654 | 218 657 | 563 997 | 1.92% |
| Richards | 23 MB | 99 565 | 85 906 | 13 659 | 1.41% |
| DeltaBlue | 52 MB | 148 541 | 141 776 | 6 765 | 0.31% |
| **Total** | **4 884 MB** | **133 945 906** | **48 698 773** | **85 247 133** | **41.89%** |

**Number boxing is 41.89% of everything the corpus allocates** — 2.05 GB of 4.88 GB — and the
small-integer cache absorbs 36.4% of the requests before they allocate anything, which is P2-2's
table still earning its keep. So the prize phase 3 is aimed at is not small and never was; the
per-suite spread is what hid it, because a corpus average over four suites at 0.3–4.8% buries three
at 36–67%.

#### The second number nobody had: what the numeric-local tier is worth

Every phase 3 item was measured as a **delta** against the tier as it stood — 3-5 at 0.997×, 3-7 at
1.0001× — and four such readings look like evidence that the mechanism does not matter. They are
evidence that *eight more names* do not matter. Nobody had measured the tier itself, because there
was no way to turn it off. `BROILER_JS_NUMERIC_LOCALS=0` is that control, and it is the whole
cumulative product of P2-2 item 3, 3-0, 3-3, 3-5 and 3-7:

| | tier on | tier off | |
|---|--:|--:|---|
| fresh number boxes | 85 250 178 | 85 561 365 | **311 187 removed — 0.36%** |
| allocated bytes | 4 884.1 MB | 4 904.3 MB | **0.9959×, 0.41%** |
| numeric locals | 232 | 0 | |

Timed the same way — six rotations of the driver run per arm, interleaved — the wall clock does
**not separate at all**: 21 261 ms with the tier against 21 024 ms without it, a 1.0113 ratio
against a per-arm spread of 4–5% whose ranges *overlap end to end* (20 718–21 793 off,
20 980–21 823 on). That is not "the tier costs 1%", it is "six samples an arm cannot tell the two
apart", which is what §3.5 says to expect when the effect and the noise are the same size — and at
0.41% of allocation the effect is far smaller than the noise. **The honest reading is that the
whole mechanism is not measurable on this corpus's wall clock**, which is a much stronger statement
than any single item's 0.997× and is the one that should have been available before four of them
were built.

**The entire raw-double local tier removes 0.36% of the boxes the engine allocates**, and 3-8
proposes to widen that tier by roughly ten times. Even a perfect widening that scaled linearly in
names — which it will not, because the 232 already include the hottest loop counters — reaches a
few per cent of the 41.89%.

*The reason is structural and the drop-cause table below says it out loud.* A box is minted by the
**operator**, not by the local: `a[i] = b[i] * 2` boxes because the multiply's operands arrived
boxed from element reads, and it would box whether or not either end were held in a local. A local
is one link in that chain, and phase 3 has spent five items unboxing the link that carries 0.36% of
the traffic.

#### What defeats the proof, and it is mostly not the local

Every candidate the fixed point drops, attributed to the first leaf of the assigned expression the
analysis will not type — so `s = a.x * 2 + 1` is charged to the property read rather than to the
operator or the literal:

| | Names | Share |
|---|--:|--:|
| **A named property read** | **894** | **46.7%** |
| **A call's or `new`'s return** | **570** | **29.7%** |
| Another dropped candidate — a *cascade* | 132 | 6.9% |
| A computed element read | 101 | 5.3% |
| A literal that is not a number | 95 | 5.0% |
| Any other name (global, outer binding, catch) | 55 | 2.9% |
| **A parameter** | **47** | **2.5%** |
| An operator the analysis will not type | 22 | 1.1% |
| **Total dropped** | **1 916** | |

Three things follow, and none of them is in the item as written.

- **76.4% of the population is a value arriving from somewhere else** — a property read or a call
  return. The guard 3-8 proposes does belong at those points and would work there; what it does
  *not* do is what the item's title says, which is guard "a local's numeric-ness". The type is not
  unknown because the local is untyped; it is unknown because the **producing site** hands back a
  `JSValue`. Making those sites produce unboxed doubles is **3-1** (array backing stores) and
  **3-2** (shape slots) — and the boxing table says exactly the same thing, since the three suites
  where boxing dominates are the three that stream numbers through arrays and object fields.
- **A parameter is 2.5%.** 3-3 recorded the parameter gap as the one the numeric tier "cannot be
  widened to at all, because the caller picks the type; that is phase 4", and phase 4 is where it
  has sat since. It is 47 names of 1 916. *The category an item defers is not thereby the category
  that costs.*
- **93.1% of drops are roots, not cascades.** That is good news for any design — fixing a root
  frees its dependents — and it also means the 1 916 cannot be collapsed to a handful of causes by
  chasing chains.

#### The per-shape ceiling, which is the same number this phase always gets

`--local-alloc` gains a `provability` category: the three dominant causes, each with the value
hoisted out of the loop so the loop body is identical to `top-level-var`'s and the delta is the
cost of the local not being *provable* rather than the cost of the read.

| Site | net B/iter | numeric locals |
|---|--:|--:|
| `top-level-var` (provable) | **0.00** | 3 |
| `property-sourced-var` | 31.98 | 1 |
| `call-sourced-var` | 31.99 | 1 |
| `parameter-sourced-var` | 31.98 | 1 |

All three cost **31.98 bytes an iteration** — to the hundredth, the same figure 3-3 measured for
its four ineligible categories and 3-5 for its parameter-bound loop. Timed one shape per process,
ten rotations of four: the property-sourced loop is **280.5 ms against 41.0 ms**, **6.84×**, which
is the same order as 3-7's 7.19×. *Every route to "not provably numeric" costs exactly the same as
every other, and the shape-level prize has never been the question.*

#### A bookkeeping defect found while counting, and it corrects 3-7's published figures

Writing the classifier's tests turned up a drop being counted twice. `Collector` descends into
nested functions on purpose — that is what makes a closure's `s = 'x'` drop an outer numeric `s` —
but `VisitBlock` was also **offering the blocks it met there**, so a nested function's
block-scoped `var` became a candidate of every enclosing function as well as of its own, and was
dropped and counted once per level. Suppressed at nested-function depth. It changed **no answer**:
the enclosing function's hoisting scope never contains the name, so it never reached the hoist
site, and the corrected run leaves `dropped`, `surviving`, `numericLocals`, `hoistedNames` and
every drop cause **identical**. What it moves is the pair 3-7 published:
**`offered 2 521 → 2 295` and `rejected 359 → 133`**, corrected where 3-7 states them. *The
double-counted names were all names the enclosing analysis rejected anyway, which is why nothing
downstream moved — and why nobody would have found it except by writing a test that asserted an
exact count.*

#### Re-specification

**3-8 as written should not be started.** It is not wrong about its mechanism — a guard at the
value's source, branching into 4-3b's in-method fallback, is the right shape and would win the
31.98 B/iter its shapes cost. It is wrong about its target: the tier it widens carries **0.36%** of
the engine's number boxing, and the item's own population is **76.4% values produced elsewhere**.

- **3-1 and 3-2 move to the front of phase 3, and the boxing table is why.** 41.89% of the corpus's
  allocation is number boxes, and the three suites that carry it split cleanly between the two
  items — checked in their sources rather than assumed: **NavierStokes** (66.96%) and **Crypto**
  (55.16%) hold their numbers in `new Array` and read them by index, with no `.x`/`.y` field access
  anywhere, which is **3-1**; **Box2D** (35.98%) allocates no arrays at all and has 240 `.x`/`.y`
  accesses, which is **3-2**. Both items have been ranked behind the locals work since the phase
  opened, on no measurement at all.
- **What 3-8 keeps** is the 2.5% parameter case, which is small, and the observation that a guard
  at a *property read* is 4-2b's specialized read — already built, already knows the shape at the
  site, and already carries 44.7% of the corpus's executed reads. If unboxing is ever wired into a
  specialized read, the local half follows for free.
- **The instrument outlives the item.** `NumberBoxingDiagnostics` and
  `BROILER_JS_NUMERIC_LOCALS` are what turn "phase 3 is invisible on the corpus" from a repeated
  observation into a measured share, and any future item here should be sized against them before
  it is written.

#### Re-opened, on the terms `0083` already used once — the 0.36% was the wrong denominator

**"Do not start as written" stands; "aimed at almost none of the prize" does not.** Item 3-1's
update-target census counted where the `++`/`--` step's operand lives, and the answer re-prices
this item without contradicting a number in it: **Element 0, Property 0.3%, LocalCell 0.0%,
LocalSlot 98.1%** of 17 282 144 steps — **≈7.05 M real boxes, 22.6% of the 31.16 M the corpus
still allocates**, and 6.76 M of that on NavierStokes alone.

*The 0.36% and the 22.6% are measurements of different things.* `BROILER_JS_NUMERIC_LOCALS=0`
prices **what the tier catches** — every raw-double local the analysis can prove, which is a small
population precisely because the proof is hard. The update census prices **what the tier lets
through**: the names that would have been raw doubles had anything typed them. An item is worth its
second number, not its first, and this section reasoned from the first.

**It is the same correction `0083` made, arriving by a different route.** There, compile-time
provability reached 0.75% of the arithmetic while run-time truth reached 100.00%, which moved the
guard from the compiler's proof to the operator. Here the static tier reaches 5.0% of scalar locals
while the update operator hands 98.1% of its steps to a local that merely was not proved. *Twice
now, a mechanism priced by what the compiler can prove has been under-priced by two orders of
magnitude against what a run-time test can reach.*

**And the population has a named shape rather than being a long tail.** NavierStokes' 9.46 M steps
are `++currentRow` in three functions, where `currentRow = j * rowSize` and `rowSize` is a
`FluidField`-scope var written from a sibling closure — so **one untypable closure variable
cascades into 6.76 M boxes**, and 3-6's waterfall confirms it at the name (24 numeric locals of 141
hoisted). That suggests the guard does not have to be general to pay: a run-time numeric test on
the *initializer* of a local whose only defeat is an outer-scope name would reach this whole
population, which is a much smaller item than "guard every local".

**What still has to be answered before it is built** is the exchange rate, which is now known and
is not kind: `0090` puts collection at 1.8% of the driver and the measured cost of allocation at
**711 ms per GB**, so 7.05 M boxes at ~24 B is **≈0.17 GB, about 120 ms — 0.6% of the driver**.
That is worth having and it is not an XL's worth. *The re-opening is of the item's ranking, not of
its size: it should be re-scoped to the cascade it actually serves, and it should be argued in
milliseconds.*

#### 3-8a · Scoped to the cascade — one conjunct, one test, and the reason no static fix reaches it

The waterfall counts *which names* were dropped. Scoping needs the next thing down — **which rule
defeats the shape the traffic is actually in** — because the rules want different fixes and two of
them can never be widened at all. The update-target census is itself the oracle for that, and this
is the discrimination it was built for: a numeric local compiles `c++` to a native add and
contributes **no row**, a local that stayed a `JSValue` contributes `LocalSlot`, a captured one
contributes `LocalCell`. Eight shapes, one per conjunct (`NumericLocalDefeatTests`):

| Shape | Row | Defeated by |
|---|---|---|
| `var c = 10; c++` | *none — numeric* | — (control) |
| …with a nested function **declaration** present | *none — numeric* | **not** `CanScalarReplaceLocals` |
| a hoisted `function g(){ return c; }` names it | `LocalCell` | `CapturedByHoistedFunction` (3-7, correctness) |
| **`var c = 2 * rowSize`, `rowSize` one scope out** | **`LocalSlot`** | **`OtherName`** |
| …with `rowSize` written from a sibling closure | `LocalSlot` | `OtherName` |
| …with `rowSize` **already proven numeric** | `LocalSlot` | `OtherName` |
| the value passed in as a parameter instead | `LocalSlot` | `Parameter` (3-3's gap) |

**Three of these rule things out, and that is most of the work.** A nested function declaration is
innocent — `CanScalarReplaceLocals` tolerates it, and `FluidField` is built out of them. The
hoisting rule is innocent *of this traffic*: it produces a `LocalCell`, and NavierStokes reports
**9 461 760 `LocalSlot` steps against six `LocalCell`**, so the conjunct 3-7 proved is correctness
is not what is costing the boxes. And "just pass it in as an argument" trades `OtherName` for
`Parameter` and lands in the same row.

**What is left is one conjunct: the analysis is per-function and will not type a name from outside
it.** The sixth row is the sharp one — `rowSize` is *already proven numeric* by its own scope's
analysis, and the local one level down that reads it is still dropped as `OtherName`. **A
conclusion is not carried across a closure boundary.** That is pure analysis reach with no
soundness argument attached, and it splits the work in two:

- **3-9 (new, S–M, static, count first) — counted, and closed at a population of zero; see below.**
  Import the enclosing function's proven-numeric set into
  `IsNumeric`, so an identifier resolving to a numeric local one scope out is typed rather than
  classified `OtherName`. No run-time machinery, no guard, no fallback. **It does not reach
  NavierStokes** — the seventh fixture is why: there the readers of `rowSize` are hoisted
  *declarations*, so the root is held by 3-7's correctness conjunct and is untypable no matter how
  far the analysis reaches. So 3-9's population is names whose enclosing binding is captured only
  by function *expressions*, and **nobody has counted how many of those the corpus has**. That
  count is the item's own precondition, on the pattern that has now retired five designs here.
- **3-8a — the run-time half, and the only thing that reaches the cascade.** When a local's *only*
  defeat is `OtherName` or `DroppedCandidate` — every other conjunct already passes — one
  `IsNumber` test where the value enters decides the name for the whole function. That is 4-3b's
  in-method branch pointed at a representation, which is what 3-8 always said; what is new is that
  it no longer needs to be general. It does not need to guard a parameter (3-3's gap, a different
  entry point), a property read or a call result (a guard per *read*, not per name), which is the
  76.4% of drops 3-8 was originally sized around and the reason it was an XL.

**Sizing 3-8a honestly.** Its population is the names NavierStokes' `++currentRow` family lives in:
**6.76 M of the 7.05 M real update boxes, 96%** — the rest of the corpus's steps are either
answered by the small-integer cache already (Crypto: 7.19 M steps, 7 210 real boxes) or too few to
matter. At `0090`'s **711 ms per GB** that is **≈0.16 GB, ≈115 ms, 0.6% of the driver**, and the
*reads* of those same locals add nothing to it — `0084`'s guarded tree already computes on them
natively at the operator, and an index read hands back a box that already exists rather than
minting one.

#### Attempted, and stopped — the population narrowed but the mechanism did not

3-8a was taken to the build and **is not built**. Two things came out of the attempt, and the
second is the reason it stopped.

**The mechanism is an XL after all, and the scoping above was wrong to call it an M.** Narrowing
*which names* the item speculates on does not narrow *what has to change to hold one*. A local that
is a raw double today advertises itself through `VariableScope.NumericStorage`, and **every fast
path in the compiler keys off that one field** — item 3-0's `GetElementByNumber(double)` index,
item 3-5's mixed comparison, `AssignToVariable`'s raw store, the update emitter's native step, and
`ToNativeExpression`'s "this leaf is already a double". A speculative local is a double *only while
a flag holds*, so every one of those sites has to become guard-aware or read a dead double — and a
site that is missed produces a **wrong answer**, not a slow one. Holding the value in both
representations is what makes the reads correct, and then a read outside the guarded set costs a
box it does not cost today. *The population is small; the surface is the whole numeric tier.*

**And the population could not be measured, which is what actually stopped it.** The instrument was
built the honest way — take the same optimistic fixed point a second time with a name from an
enclosing scope assumed numeric, and subtract the real survivors, so the set comes out of the
existing analysis by difference rather than out of a new rule — and it read **0 on all seven
suites**. It also read **0 on the shape it was built for**. By §3.5's own rule that reading is
unusable: *a counter that has never been shown to read non-zero is a claim about the counter*, and
this one never discriminated. One real defect was found inside it on the way and is worth recording
because it is `0083`'s failure mode a second time — **the enable for a compile-time counter was
placed next to the run-time censuses, which run after the corpus has already been compiled**, so
the first reading was of a counter that was switched on too late. Fixing that changed nothing, and
the instrument was **reverted rather than shipped**: a zero nobody can vouch for is worse than no
number.

#### The count, on the second attempt — 26 names, and 15 of them are NavierStokes'

The instrument was rebuilt, and this time **made to discriminate before it was pointed at
anything** — which is the whole of what went wrong the first time. `AnalyzeSpeculative` still works
by difference against the real fixed point rather than by a new rule (run the same resolution a
second time with an identifier the function neither declares nor takes as a parameter assumed
numeric, and subtract the real survivors), and it now carries seven fixtures that make it *fail*
if it stops separating the populations:

| Shape | Drop cause | In the population? |
|---|---|---|
| `var c = 2 * gg` | `OtherName` | **yes** — 1 |
| `var r = gg; var c = 2 * r` | `OtherName` + `DroppedCandidate` | **yes** — 2, the cascade resolves |
| `var c = 2 * 10` | *(proven numeric)* | no |
| **`function f(n){ var c = 2 * n }`** | **`Parameter`** | **no** |
| `var c = 2 * o.x` | `PropertyRead` | no |
| `var a = []; var c = 2 * a` | *(never offered)* | no |

The last two rows are what make it a measurement rather than a tally. A **parameter** is one slot
away in the same enum and is *not* a name from outside the function — it is a value the caller
picks per call, so no test at an initializer decides it — and an instrument that could not separate
them would report 3-8a's population as everything item 3-3 already deferred. The final row is the
error that would have inflated rather than zeroed the figure: a local that was never *offered*
(`var a = []`) is not in `candidates` either, so an instrument asking only "is it a candidate?"
would classify it as coming from outside the function and assume it numeric. Telling *outside the
function* from *inside and unqualified* needs its own set of declared names.

**Counted on the corpus:**

| Suite | hoisted | numeric today | **+3-8a** | would be | | `OtherName` drops | `LocalSlot` steps |
|---|--:|--:|--:|--:|--:|--:|--:|
| Richards | 70 | 12 | 1 | 13 | 1.08× | 1 | 0 |
| DeltaBlue | 126 | 22 | 1 | 23 | 1.05× | 1 | 2 448 |
| RayTrace | 182 | 21 | 1 | 22 | 1.05× | 5 | 0 |
| Box2D | 1 446 | 80 | 3 | 83 | 1.04× | 8 | 272 322 |
| EarleyBoyer | 597 | 47 | 3 | 50 | 1.06× | 7 | 19 149 |
| Crypto | 358 | 26 | 2 | 28 | 1.08× | 16 | 7 191 452 |
| **NavierStokes** | 141 | 24 | **15** | **39** | **1.62×** | 17 | **9 461 760** |
| **Total** | **2 920** | **232** | **26** | **258** | **1.11×** | | 16 947 131 |

**26 names, and the distribution is the result rather than the total.** Six suites gain one to
three names each; **NavierStokes gains fifteen and its numeric-local count goes 24 → 39, 1.62×** —
by far the largest widening any item in this phase has produced on a single suite, and it lands on
exactly the suite the update-target census says carries **9.46 M of the 16.95 M `LocalSlot` steps
and 6.76 M of the 7.05 M real update boxes**. *The population and the traffic are concentrated in
the same place, which is the condition every other phase-3 widening failed.*

**Against the item it most resembles**: 3-7 widened the tier by **8 names, 224 → 232, 1.036×**, and
was worth 1.0001× on the corpus because its eight names were scattered where nothing hot lived.
This is 26 names at 1.11× with fifteen of them in the hottest boxing loop in the corpus. **The
prize is still bounded by `0090`'s exchange rate** — 6.76 M boxes is ≈0.16 GB, ≈115 ms, **0.6% of
the driver** — so what has changed is confidence, not size: the item now has a counted population
in the right place instead of an estimate.

**And the count does not license the build.** The mechanism is still the XL described above: every
fast path keys off `NumericStorage`, and a speculative local is a double only while a flag holds.
What the count settles is that if that work is ever done, there is something for it to reach.

#### Built, complete, measured — and it does not pay: **`0096`**

The whole mechanism is built: the dual representation, the writes, the `++`/`--` step, and all
three consumers that can take a raw `double`. It is **off by default**
(`BROILER_JS_SPECULATIVE_NUMERIC_LOCALS=1`), and it stays off, because the finished item is a
**1.2% regression** on the corpus's boxing and the counter that says why also says no fourth
consumer would change it.

**The storage half.** A speculative local is held as a raw `double`, a `bool` saying the double is
live, and the ordinary `JSValue` slot; `Expression` becomes a conditional over the two, so **every
existing read site is correct without being touched** and a write through it is an assignment to a
conditional, which the backend rejects loudly. That is the numeric tier's own safety argument
reused — the field it does *not* get is `NumericStorage`, because five fast paths read that on the
understanding that the binding **is** a double, and a speculative one is a double only sometimes.
Writes route through `AssignToVariable`, which lands the value in the slot, derives the flag from it
and mirrors the raw half — branch-free, and reading the flag and the double **off the slot** rather
than off the expression, so a value with a side effect cannot run twice. The `++`/`--` step branches
on the flag: while it holds, the increment is a native double add that **writes nothing back to the
slot**, which is the box the census priced.

**The three consumers.** The guarded arithmetic tree offers a speculative local **as a leaf** —
`OrderedNode` already *is* a raw double, a flag and a fallback, so the shape needed nothing invented
— snapshotted into three CLR locals at the leaf's own postorder position. The element **read**
(`x[currentRow]`) and the element **write** (`x[currentRow] = v`) each emit two arms over item 3-0's
`GetElementByNumber(double)` / `SetElementByNumber(double, JSValue)` and the ordinary indexer.

**Three things the build got wrong, and how each was caught.**

**A leaf that offered a stale slot.** `OrderedNode.IsLeaf` means *"the saved operand is the value
whichever way the test went"* — true of an ordinary guarded leaf, which saved the `JSValue` it was
handed. It is **false** of a speculative leaf, whose slot is deliberately stale exactly while the
flag is up, so a tree that fell to its generic arm read a value several increments old. `x++` three
times then `x + tail` answered `"0!"` instead of `"3!"` — no exception, no NaN, just an old number.
Fixed by building the leaf `IsLeaf: false` so `AsJSValue` re-materializes from the flag, which costs
a box on the arm that was going to box anyway.

**A leaf that was nearly unreachable.** Eligibility is
`CountOperators - 1 + CountNativeLeaves ≥ 1`, and a speculative local counts as neither — so
`c + p.v`, **the shape the whole population is made of**, was refused for having no saving to make
and the new leaf never ran. Counted as its own term (`CountSpeculativeLeaves`, self-gating: with the
switch off no variable carries a flag, so the control arm's rule is byte-for-byte unchanged).

**The first three fixtures proved nothing, and the file records why.** They passed against the
*broken* emitter. Two distinct causes, both worth keeping: the tree fixtures never built a tree at
all (the eligibility gate above), and the ordering fixture wrote `i = "2"` — **provably** non-numeric,
so it defeated the local's candidacy at *compile* time and the path under test was never emitted.
Each fixture was then re-checked by deliberately breaking the emitter and confirming it failed:
forcing the slot arm turns `60` into `30` and `11,22,33` into `33,2,3`.

*And the ordering fixture could not be repaired, which is the more interesting outcome.* `a[i]`
evaluates the receiver before it reads `i`, so a receiver that disturbed `i` would make the order
observable — but **to write `i` from inside a getter the getter must close over `i`, and a captured
binding is a `JSVariable` cell, which is not a candidate for either numeric tier.** The two
properties are mutually exclusive by construction. The fixture became a pair asserting exactly that,
and the receiver temp is justified by what it really buys — the compiled receiver emitted once,
behind one inline-cache site — rather than by an ordering rule that cannot be violated.

**Measured on the corpus, one build, the switch the only difference:**

| Suite | boxes off → on | | speculative locals | `LocalSlot` steps off → on |
|---|--:|--:|--:|--:|
| Richards / DeltaBlue / RayTrace / Box2D / EarleyBoyer | unchanged | 1.000 | 0 / 0 / 0 / 0 / 2 | unchanged |
| Crypto | 13 415 650 → 13 414 358 | 1.000 | 1 | 7 192 736 → 7 192 166 |
| **NavierStokes** | **11 747 641 → 12 136 012** | **1.033** | **14** | **9 461 760 → 8 626 176** |
| **Total** | **31 400 805 → 31 787 884** | **1.012** | **17** | |

Each consumer moved it, and none of them moved it enough: storage alone **1.021×**, plus the tree
leaf and the element read **1.017×**, plus the element write **1.012×**.

**Two things about that table are worth stating rather than smoothing.** The control arm was run
twice and **six of the seven suites are bit-identical**; only Crypto moves, by **5 668 boxes
(0.04%)**, which is the run-to-run variability `0084` recorded for it and which is larger than
anything this item does to that suite — so Crypto's `1.000×` row means *below its own noise*, not
*exactly zero*. And the control total here (**31 400 805**) does not match the one recorded against
`0095` (**31 162 965**), which is 635 from `0085`'s corpus baseline: since the four unaffected
suites are bit-exact across runs, that difference cannot be drift, and the earlier figure was a
**carried total rather than a re-sum of its own run**. Both arms in the table above come from one
build and one pair of runs, which is the property the ratio needs.

#### The counter that should have existed first, and what it settles

Three consumers were built by *guessing* where the remaining boxes were and checking afterwards
whether the total moved. `JSNumber.CreateSpeculativeRead` — a fourth factory entry beside
`CreateLiteral` and `CreateConversion`, on the same pattern — attributes a box **at the read**, and
answers directly:

| | NavierStokes |
|---|--:|
| boxes minted **reading** a speculative local | **393 705** |
| net change in boxes | **+388 371** |
| ⇒ boxes the whole item **removes** | **≈ 5 300** |

**The dual representation costs 394 000 boxes to save 5 300.** That is the item, and it is not a
matter of one more consumer: the 835 584 steps it genuinely takes off `Increment` mostly **do not
save an allocation**, because NavierStokes' steps are `x[++currentRow]` — the result is used as an
index, so the fast arm does a native add and then boxes the result anyway. Only a step whose value
is discarded (a `for` update clause) saves a box, and NavierStokes has almost none.

***The item is closed as measured, not deferred.*** The mechanism is correct, tested on both arms,
and left in the tree behind a switch that defaults off, because the thing that makes it lose is the
read/write ratio of the code it targets — `currentRow` is read four ways and incremented once — and
that ratio is a property of the workload, not of how many consumers the compiler grows.

**§3.5 gains the rule this cost most of an item to learn:** *a representation change is priced by
the ratio of reads to writes on the population, and that ratio has to be counted before the
representation is built.* The three consumers were each a reasonable guess and each of them was
worth less than the read it displaced; one counter at the read would have said so before any of
them existed. The same mistake, in the same shape, as the bitwise operators in item 3-1 — *count how
many of a fast path's operands can actually reach it* — except that this time the operands reached
it and the **other** side of the trade had not been counted.

**And the measurement corrects the count's own reading.** The population is 15 names in
NavierStokes, and the scoping above took the alignment between that and NavierStokes' 9.46 M steps
as read. Measured, **those 15 names carry 835 584 of the 9.46 M — 8.8%, not the whole of it.**
*The suite that holds the names and the suite that holds the traffic being the same suite is not
the same claim as the names holding the traffic*, and only running the arm distinguishes them.

**Wall clock was deliberately not measured, and that is a scoping call rather than an omission.**
Every landing item in this phase reports time beside allocation, because a box count is a proxy and
the exchange rate has to be checked. This item does not land: the switch defaults off, so the
shipping arm's time is unchanged *by construction*, and the only thing a driver run could price is
how much the losing arm costs in milliseconds — a number that changes no decision, since the
decision is already made by a counter that is exact rather than sampled. Six ABBA pairs on an arm
nobody will ship is an hour spent confirming the sign of something already counted. **If the item is
ever re-opened for a workload with a different read/write ratio, the timing run belongs to that
attempt, not to this one.**

**Gates.** 1 191 compiler tests, 4 571 integration, 2 103 built-ins, plus runtime, core, parser,
modules, storage and CLR — all green **on both settings of the switch**. 20 of the compiler tests
are `SpeculativeNumericReadPathTests`, every one asserted on both arms so a disagreement between
them *is* the bug, and `NumericLocalDefeatTests`' four shape fixtures are Theories over the switch —
the answer is unchanged and only the row moves, `LocalSlot` when off and nothing when on, which is
what says the speculation fires on exactly the shapes it was scoped from.

**The A/B the item was scoped from still holds, and that is the point.**
`NumericLocalDefeatTests` carries it reduced to one difference:

| Inner function | Result |
|---|---|
| `var c = 2 * rowSize; c++` — `rowSize` one scope out | `LocalSlot`, and every `c++` boxes |
| `var c = 2 * 10; c++` — literal | **numeric**, and `c++` costs nothing |

Same nesting, same body, same update; one identifier different. The enclosing-scope read really is
**the** defeat on this shape, and testing it at run time really does remove the row. *Every premise
the item was scoped on survived; the item still lost.* That is the shape of the finding — not a
mechanism that failed to work, but a correct mechanism whose cost was on the side nobody counted.

***So the item closes at a measured −1.2%, and phase 3's largest remaining candidate closes with
it.*** 3-8 was sized XL on the strength of 1 842 dropped candidates; scoped by measurement it became
26 names, then 15 names carrying 8.8% of their suite's steps, and finally a representation whose
reads cost seventy times what its writes save. *Phase 3's remaining work is not blocked on a missing
idea. It is bounded by the exchange rate `0090` measured, and this is what that bound looks like
when an item is followed all the way to a number instead of stopped at a plausible one.*

#### 3-9 · Counted, and closed by its own precondition — **`0097`**

3-9's specification made the count its precondition and predicted where the answer would come from:
*"it does not reach NavierStokes"*, because there the readers of `rowSize` are hoisted function
**declarations** and item 3-7 proved those must keep their `JSVariable` cell. So the population is
names whose enclosing binding is captured only by function *expressions* — and nobody had counted
how many of those the corpus has.

**It has none. Zero on all seven suites.**

| Suite | numeric locals | **3-9 population** | outer-numeric offers | 3-8a population |
|---|--:|--:|--:|--:|
| Richards / DeltaBlue / RayTrace | 12 / 22 / 21 | **0** | 0 | 1 / 1 / 1 |
| Box2D / EarleyBoyer / Crypto | 80 / 47 / 26 | **0** | 0 | 3 / 3 / 2 |
| NavierStokes | 24 | **0** | 0 | 15 |
| **Total** | **232** | **0** | **0** | **26** |

**A zero is exactly the reading this phase has learned not to trust, so it was earned before it was
taken.** Item 3-8a's first population instrument read zero on all seven suites and was nearly
published as a finding before anyone had shown it could read anything else; §3.5 gained the rule
that *a counter never shown to read non-zero is a claim about the counter.* This one was built the
other way round — **nine constructed fixtures first, and only then the corpus** — and three of them
read non-zero. Each was then re-checked by **disabling the probe and confirming it fails**, which is
the discipline `0096` added to §3.5 one item ago, applied to the instrument that decides this one.

**And the zero is not the harness.** 3-8a's 26 is reported from the same call site in
`CreateFunction`, two lines away, behind the same `CanScalarReplaceLocals` gate and the same
compile-time switch, in the same run that reports 3-9's zero. A harness that could not reach the
code would have zeroed both.

**Why the population is empty, counted rather than argued.** A single candidate count cannot tell
two very different worlds apart — nested functions never read an enclosing numeric local, or they
read them constantly and never anywhere typable — and the follow-up differs completely between them.
So a second counter records **how often the enclosing scope chain answers "that name is already a
raw `double`"** while 3-9's pass resolves a function. **It answers never: 0 offers on the whole
corpus.** The reads do not exist. There is nothing to import, rather than something that cannot be
used.

That reconciles exactly with the item this one sits behind. 3-9 can only import from a name that is
both *proven numeric* and *still a raw double despite being captured* — and that second condition is
precisely item 3-7's population, which measured **eight names in the entire corpus** (224 → 232).
Not one of those eight is read from an assignment inside the function that captures it, which is
what the offer counter says directly.

**The probe asks what the compiler BUILT, not what the analysis PROVED, and the difference is the
item's own prediction.** Pointed at the enclosing analysis's conclusion instead of at
`NumericStorage`, the hoisted-declaration fixture flips from 0 to 1 — a name 3-7 leaves in a cell
for correctness would be reported as a win — and the two-levels-out fixture flips from 1 to 0,
because a per-frame set is not what a lexical reference resolves through. Both flips were run.

***So 3-9 is closed without being built, and the mechanism is the cheapest thing in phase 3 to have
declined.*** Unlike 3-8a it needs no run-time test, no flag, no fallback representation, and its
failure mode is structurally absent — a name it typed would be an ordinary numeric local that every
fast path reads unchanged. **It is a good mechanism with nothing to point it at**, and building it
would buy an extra analysis pass and a scope-chain probe per compiled function in exchange for zero
names. *The count cost one instrument and no mechanism, which is the whole argument for taking it
first.*

**What would re-open it** is stated because the counter is left in the tree to answer it: 3-9's
population is bounded above by the number of captured numeric locals, so **widening item 3-7 is its
only supply**. If a future change moves 3-7's eight, re-run this counter before re-reading this
section — it is off by default (`BROILER_JS_OUTER_NUMERIC_COUNT=1`) and costs nothing while it is.

**Gates.** Full engine suite green — 1 200 compiler, 4 571 integration, 2 103 built-ins, plus
runtime, core, parser, modules, storage and CLR. The counter is off by default and changes no
emission on any setting, which is what makes this a measurement rather than a change.

**One pre-existing flake was found on the way. It is a real engine race, and it is now fixed —
see the section that follows.**
`CapturedNumericLocalTests.SuspendingNestedFunctionsCaptureThroughTheSameBox` fails
intermittently **under CPU load** — three of four runs while the test262 matrix was saturating the
container at load average ~14 on four cores, and not at all on an idle one. The failure is always
the same assertion and always the same way:

```js
var out = 'no'; var v = 1;
var f = async function () { v = v + 1; out = v; await 0; v = v + 10; };
f();
String(out) + ',' + v      // "2,2" required; "2,12" observed
```

`await 0` must queue a microtask, so `v = v + 10` cannot have run before the synchronous caller
returns. **`"2,12"` says the continuation resumed early**, which is a real scheduling violation
rather than a slow test — there is no timeout or drain in the fixture to be racing. **It reproduces
on the unmodified baseline**: the same four runs with this item's changes stashed fail three times,
so it is neither 3-9's nor 3-8a's. It is noted here because it is load-dependent and therefore
invisible on a quiet machine, which is how it survived every full-suite run in this phase until a
saturated container made it visible.


#### The async resumption race — found by the gates, and it was two threads running JavaScript at once: **`0098`**

The flake above is not a slow test. `await 0` queues a job, so the statements after `f()` belong to
the job already running and `v = v + 10` cannot have happened when they read `v`. **`"2,12"` says
the continuation ran anyway** — and it ran on a different thread.

**Both dispatch paths were wrong, for opposite reasons, and each covered the other's absence.**

| Ambient `SynchronizationContext` | What the engine did | Why it is wrong |
|---|---|---|
| none — every plain `Eval` | `ThreadPool.QueueUserWorkItem` | runs the job on a pool thread |
| present — every xUnit test | `SynchronizationContext.Current.Post` | xUnit's `AsyncTestSyncContext` dispatches through the pool too |

A job resumes a generator, and resuming runs user JavaScript, so **the engine let two threads
execute JavaScript in one context simultaneously.** ECMAScript's agent is single-threaded by
construction and this engine is written throughout on that assumption, so a wrong arithmetic answer
was the visible corner of an unsynchronized heap. *The reason it read as a rare flake rather than as
corruption is that the racing job only incremented a number.*

**The second row is the one that matters for how this was nearly missed.** The first fix addressed
only the thread-pool fallback, and the console harness agreed — **18 of 3 000 wrong before, 0 of
3 000 after.** Then the new fixtures failed anyway, because a test host is never the no-context
case: xUnit installs `AsyncTestSyncContext` on every test thread, so the suite had always been
taking the *other* branch. **The rate had also been measured on a loaded machine and re-measured on
a quiet one**, which on its own would have been enough to believe a fix that fixed the wrong half.
What settled it was a deterministic fixture, not a rate.

**The rule now lives in one place** (`JSContext.PostJob`), and the order of its cases is the whole
of it:

1. **We are on the engine's own pump** (`AsyncPump`, marked `IJSJobPump`) — post there.
2. **The pump this work belongs to**, captured when the promise or the `await` was created.
3. **This context's queue**, while it is executing JavaScript — the new case, and the fix.
4. **A host context**, with nothing executing.
5. **The thread pool**, with neither.

**Case 2 is not defensive and dropping it deadlocks `Execute`**, which is how the first attempt at
this rule failed: a promise created on the pump thread can be settled from a pool thread, where
`SynchronizationContext.Current` is null and case 1 cannot see the pump. Without case 2 the job took
the queue, the task `AsyncPump.Run` was blocking on never completed, and the pump spun forever
waiting for work that had gone somewhere else. `Issue814ForAwaitUsingTests.ForAwaitWithAwaitUsingHead`
hung for twelve minutes at load average 0.10 before `--blame-hang` named it. *Only a pump is
trusted, whether current or captured: an arbitrary captured context is no more the JavaScript thread
than an arbitrary current one.*

**The queue cannot strand a job, and that is a property of when it is taken rather than of a
fallback.** It accepts only while a JavaScript execution is in progress — exactly when there is
something for the job to race — so anything posted with nothing running keeps its old dispatch. That
is what lets a host `Task`-backed promise settle long after `EvalWithTopLevelAwaitAsync` returned.
The depth stays at one for the whole drain, so a job that queues another job takes the queue too, and
the return to zero is made under the same lock as the final dequeue, which is the only window in
which an enqueue could be lost. A nested `Eval` — a host callback evaluating more source while
JavaScript is on the stack — does not drain, or a job would run in the middle of another job and
reintroduce the interleaving on one thread.

**What is fixed and what is not.** The race is gone whenever JavaScript is executing when the job is
posted, which is every in-script `await` and every reaction queued by a running script. A job posted
while *nothing* is executing still takes the host context or the pool, and could in principle land
during a later execution. Closing that too means the embedding contract has to name a JavaScript
thread the engine can serialize against, which is a change to the API rather than to a dispatch
site. **That residual was then measured and closed — see below; "in principle" turned out to be 172
overlaps in 200 rounds.**

**Gates.** The eight new fixtures are written to **lose the race deterministically** — a spin after
the call, so a racing thread reliably wins — which is the difference between them and the test that
found this: that one asserts the same value and caught the bug **0.6% of the time**. Measured
against the unmodified pin they fail **5 of 8**; with the fix, 8 of 8. The whole engine suite is
green, and the two job-ordering test262 manifests — `test262-promise-jobs` and `test262-for-await`,
whose `ticks-with-*` cases assert exact microtask tick counts and are the sharpest check available
on this change — pass **5/5 and 2/2**. The five pinned manifests were re-run against it as well;
their counts are recorded in §3.4.

#### The embedding contract — the residual, measured at 86% and closed: **`0099`**

`0098` fixed where a job is *dispatched* and named what that could not reach: **a job posted while
nothing is executing** takes a host context or the thread pool, because the queue is deliberately
refused at depth zero — that refusal is what makes stranding a job impossible. Such a job could then
run JavaScript while a later `Eval` was running JavaScript.

**"In principle" was doing a lot of work in that sentence.** Reaching the case needs a JavaScript
entry point that is not `Eval`, and a host invoking a `JSValue` directly is exactly one — arm a
promise, settle it from a host thread with nothing running, and evaluate meanwhile. Measured:

| | peak threads in one context | overlaps / 200 rounds |
|---|--:|--:|
| `0098` — dispatch fixed, no lock | **2** | **172 (86%)** |
| `0099` — with the execution lock | **1** | **0** |

**The counter had to be built twice, and the first version is the more instructive one.** It counted
threads inside JavaScript *process-wide*, which is not the invariant: **two independent contexts
running in parallel is exactly what an embedder is supposed to be able to do**, so a process-wide
count reports legitimate concurrency as a violation and would fire on any full-suite run, where
xUnit evaluates several test classes at once. Detection is per context; only the aggregate is static,
because an overlap is a real violation whichever context produced it. *A concurrency counter measures
the wrong thing by default, and the default is plausible enough to ship.*

**The fix is one lock and one contract.**

A per-context `Monitor` is taken by every execution the engine owns — `Eval`, `Execute`,
`ExecuteAsync`, the queue drain, and **every job wherever it was dispatched** — so an evaluation and
a job are mutually exclusive even when the job runs on a pool thread. Re-entrancy is required rather
than convenient: a host callback that evaluates more source is the same agent going deeper and must
not deadlock against itself.

What the engine cannot see is a host reaching in by another route — invoking a `JSValue`, reading a
property whose getter is a JavaScript function, calling back from a thread of its own. Those are
ordinary calls on ordinary objects and **guarding each one would put a mutex on the engine's hottest
path**. So the rule is stated and given an API instead:

```csharp
using (context.EnterExecution())
    callback.InvokeFunction(new Arguments(JSUndefined.Value, value));
```

*Wrap host-initiated entry into JavaScript, unless it is already inside one.* A call made from within
an engine-owned execution needs nothing, and wrapping it anyway is harmless.

**What the lock costs is a bounded wait, and one pattern it cannot support: JavaScript blocking on a
host task whose completion has to re-enter the same context.** That was written here as one thing
the lock broke, and measuring it found the attribution wrong and the problem twice as large — see
the section after next. **Measured, it costs nothing on the suite**: the integration suite runs in 46–47 s
against a 43–57 s baseline, and the earlier reading of 3 m 4 s was a concurrently-running sweep
rather than the lock, which is worth recording because it was nearly believed. One allocation was
found and removed on the way — the public handle is a class, so the per-job path uses the struct
scopes directly rather than putting an allocation on every microtask.

**Gates.** Five new fixtures asserting the *property* rather than a value, which is the point: a
value assertion catches an overlap only when the overlap happens to change that value, and that is
how the original defect survived a phase of full-suite runs at a 0.6% hit rate. They cover the
residual shape, two host threads evaluating on one context, the contract API, re-entrancy, and that
jobs queued under a host scope drain when it is released. Whole engine suite green — **8 098 tests
across 13 projects, no deadlock** — with `test262-promise-jobs` and `test262-for-await` at 5/5 and
2/2 and the five pinned manifests recorded in §3.4.


#### The blocking host wait — one deadlock from each of the last two changes: **`0100`**

`0099` recorded that the lock cost "one pattern it cannot support". Measured, **the attribution was
wrong and there are two patterns, one contributed by each change**:

| Host function called from a script, waiting on a `Task` completed by… | `0098` queue | `0099` + lock |
|---|:--:|:--:|
| **a promise reaction on this context** | **hangs** | hangs |
| **host work that must enter this context** | completes | **hangs** |
| unrelated host work (control) | completes | completes |

The first is the *queue's*, not the lock's, and it arrived a change earlier than the note that
blamed the lock for it: a host frame called from a script is inside an execution, so a reaction it
waits for is **queued** and cannot run until that execution ends — which it never does. The second is
the lock's: the execution lock the frame holds keeps out the host work that would complete the task.
*Two mechanisms, two deadlocks, and writing one of them down as the other's cost is what the control
row exists to prevent.*

**`JSContext.WaitFor(task)` is the supported wait, and it answers one shape each.** It **drains** the
queue — which releases the first — and then **releases the context** while it blocks, retaking it
afterwards — which releases the second. The drain happens *before* the release, so a job queued by
the execution being suspended runs on the thread that queued it and in the order it was queued,
rather than being handed to whichever thread takes the context next.

```csharp
context["readFile"] = JSValue.CreateFunction((in Arguments a) =>
    (JSValue)context.WaitFor(File.ReadAllTextAsync(a.Get1().ToString())));
```

**The hazard it trades for is real, and is why the API is explicit rather than automatic.** While
the context is released, other JavaScript can run on it — queued jobs, another host thread — so
state the waiting frame read before the wait may differ after it. That is inherent in blocking a
single-threaded agent: *the alternative is not "safe", it is "hangs".* `task.Wait()` and `task.Result`
still deadlock, and are still the wrong thing to write; nothing detects them automatically, because
"this thread is blocked waiting for something that needs the context" is not a question a lock can
be asked.

**Two details that are easy to get wrong and are pinned by their own fixtures.** The depth released
is also the number of `Monitor` entries the thread holds, so a wait made two levels deep has to give
up both and take both back — getting that wrong leaks the lock and the *next* entry deadlocks, which
no value assertion would catch. And the fault is re-observed **after** the context is retaken and
through the awaiter rather than through `Wait`, so a host function sees the exception the task
actually carried instead of the `AggregateException` wrapping it, with the context held so it can
turn that into a JavaScript throw.

**Gates.** Seven fixtures, **each on a worker with a 15-second budget**, so a regression fails in
seconds instead of hanging the suite — which is not caution: the last deadlock met here took twelve
minutes and `--blame-hang` to identify. Against a plain blocking wait they fail **4 of 7**, three of
them reporting `deadlocked` explicitly; with `WaitFor`, 7 of 7. Whole engine suite green, and the
conformance manifests are recorded in §3.4.

---
