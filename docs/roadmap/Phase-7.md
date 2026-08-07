# Phase 7 — VM 2.0: respectable JavaScript performance

Make the phase-6 interpreter fast enough to ship, **mostly by consuming machinery phases 2
and 3 already built** rather than by writing new machinery. The catalogue's VM 2.0 stage:
shapes, inline caches, string interning, array fast paths.

> The plan half of [`Phase-7.status.md`](Phase-7.status.md) — which records that **no
> measurement exists yet**, and carries the entry measurement this phase is blocked on.
> Part of the [performance and benchmark roadmap](Roadmap.md); the four VM phases are
> [track two](Roadmap.md#track-two--the-vm-tier-phases-69).

---

## Why this phase is cheaper than it looks

**The structures the catalogue's VM 2.0 stage asks for are already in this engine, and they
are not IL artifacts.** Shapes, inline caches, property maps, the small-number cache, dense
element storage and string ropes all live in `Broiler.JavaScript.Runtime` and
`.Storage` — assemblies the VM consumes unchanged. Phase 2 spent an entire campaign making
them *fire*; a VM that reimplements them would repeat that campaign's opening mistake.

| Catalogue row | Already built, by | The VM's work |
|---|---|---|
| Hidden classes / shapes | phase 2 (`Runtime/ObjectShape.cs`) | emit property opcodes that carry a cache slot |
| Monomorphic IC (get) | item 2-1 | consume it at `GetProperty` |
| Store IC | items 2-1, 2-4 | consume it at `SetProperty` |
| Method/property indexing | items 2-7, 2-9 | consume the slot index |
| Array fast path | item P2-3, item 3-0 | element opcodes over dense storage |
| String fast paths | item P2-4 (ropes) | nothing — it is in the builtins |
| Allocation avoidance | items P2-1, P2-2, 3-0 | nothing — it is in the runtime |

**So this phase is mostly plumbing, and its risk is the opposite of phase 6's**: not that
the VM diverges semantically, but that it quietly grows a *parallel* copy of a structure the
IL path already owns. **Consume, do not reimplement.**

**Owner assemblies:** `Broiler.JavaScript.Portable`, `.Portable.Compiler`, consuming
`.Runtime` and `.Storage`.

## Items

| # | Item | Size | State |
|---|---|---|---|
| **7-0** | **Profile the VM** — its own baseline, and its ratio to the IL path | M | ❌ **not started; blocks the rest of the phase** |
| **7-1** | Inline caches at the property opcodes | L | ❌ |
| **7-2** | Element opcodes over dense storage | M | ❌ |
| **7-3** | A constant pool with interned property names | S | ❌ |
| **7-4** | Arithmetic opcodes that box only at the root | L | ❌ |
| **7-5** | Stack machine or register machine — **decide on 7-0, not on preference** | L–XL | ❌ |
| **7-6** | Call and closure fast paths | M | ❌ |

### 7-0 · Profile the VM — **the entry measurement**

Nothing in this phase may be designed before it. It must report:

1. **The VM's own Octane profile** — all 15 suites, `--repetitions 3`, and a band, exactly
   as phase 0 requires of the IL path.
2. **The ratio to the IL path, per suite**, on the same machine at the same time. This is
   the number that says whether the VM is shippable at all, and it is the only context in
   which the two paths may be compared. Everywhere else, the VM's competitor is *not running*.
3. **Where the VM's time goes** — dispatch, operand decode, property operations, arithmetic,
   calls, allocation. Phase 8 is written against this histogram; so is 7-5.

**Report allocation beside time.** This campaign twice found the interesting half in the
column it was not looking at, and a fresh interpreter is exactly where that happens again.

### 7-1 · Inline caches at the property opcodes

Give `GetProperty` / `SetProperty` an inline-cache slot in the bytecode, keyed on
`ObjectShape`, resolving to the slot index item 2-9 made cheap.

**Carry phase 2's findings across rather than re-discovering them.** The engine already
learned, expensively, that:

- an ordinary property write must **not** destroy the object's shape (P1-1);
- the cache must cover **prototype** lookups or method calls never hit (P1-2);
- a store that *creates* its property needs its own entry, or it is **0 hits against
  600 000 misses** (2-1);
- `o.x++` and `o.x op= rhs` must go through both caches — they reached **neither** (2-4);
- statics on a constructor function are a hot path and were a **100% miss** (2-8).

**Every one of those is a test the VM arm must also pass.** They are already written.

### 7-2 · Element opcodes over dense storage

P2-3 made a dense element one reference instead of a 32-byte descriptor; 3-0 stopped boxing
the index. Emit element opcodes that reach both directly rather than through the generic
property path.

### 7-3 · A constant pool with interned property names

The catalogue rates string interning ⭐⭐⭐⭐⭐ at low–medium effort. In a VM it is nearly free
and structural: property names become constant-pool indices at compile time, so the
interpreter compares references rather than strings, and the IC key is an integer.

**S, and it should be built with 6-1's format rather than retrofitted.**

### 7-4 · Arithmetic opcodes that box only at the root

**The single most transferable result in this roadmap, and the VM gets it for free if it is
built in from the start.** Phase 3 measured that a box is minted by the **operator**:
applying the raw-`double` idea at the local removed 0.36% of the corpus's boxes; applying it
at the operator removed **54.0%**.

In a VM, "evaluate each leaf, test for Number, compute raw, box only the root" is not a
compiler transformation at all — it is **how the arithmetic opcodes are written**. Do it in
the opcode set; do not add it later.

**And carry the caveat:** 73 817 515 of 73 818 646 generic invocations arrive with both
operands already Numbers, but the compiler's static proof reaches only 0.75% of them. The
test belongs at run time, in the opcode, not in `PortableCompiler`.

### 7-5 · Stack machine or register machine

`PortableInterpreter` is a stack machine. The catalogue rates a register VM "high effect,
high effort".

**This is the phase's one genuinely open architectural question, and it is exactly the kind
this campaign has learned not to answer from a catalogue.** Decide it on 7-0's histogram:
if operand-stack traffic (`Duplicate`, `Pop`, redundant load/store pairs) is a measurable
share, the conversion is justified; if it is not, a register machine buys a rewrite and
nothing else.

**Precedent from this campaign:** item 3-8a was built complete, with every premise intact,
and lost on a property of the workload nobody had counted. **Count first.**

### 7-6 · Call and closure fast paths

A VM call should not pay the IL path's fixed prologue — item 4-5 measured that at **142 ns
before any argument**, of which ~85% is unattributable from outside the engine. **A VM frame
is attributable from inside it**, which makes this cheaper here than in phase 4.

Reuse the shadow stack: the activation record is a slot in `CallFrameStack` addressed by a
`FrameToken`, and an argument-less IL call allocates **0 B** because of it.

## Order

```
7-0 profile ← BLOCKS THE PHASE
  ├→ 7-3 interned constant pool (build with 6-1's format)
  ├→ 7-4 root-boxing arithmetic  (build with 6-1's opcode set)
  ├→ 7-1 property ICs → 7-2 element opcodes → 7-6 calls
  └→ 7-5 register machine — only if 7-0 says operand traffic is measurable
```

7-3 and 7-4 are marked "build with phase 6" deliberately: both are structural, both are
nearly free at design time, and both are expensive retrofits.

## Exit gate

1. **The dual-arm test262 gate still holds** — 6-8, unchanged. Every item here touches
   observable behaviour through a cache; phase 2's history says that is where wrong answers
   come from (2-8 shipped a regression that broke DeltaBlue).
2. **The VM's Octane run reports its own band**, per phase 0's rules, and a per-suite ratio
   to the IL path measured on the same machine at the same time.
3. **A target ratio, set by 7-0 and written down before the work starts.** "Faster than
   before" is not an exit criterion; phase 2's exit gate is the cautionary tale — it named
   200× and stayed split on four measurements.
4. **No structure duplicated from `.Runtime` or `.Storage`.** A reviewer should be able to
   check this by inspection.

## Dependencies

**Depends on phase 6 in full.** Consumes phase 2's shapes, caches and property maps and
phase 3's allocation work without modifying them — if the VM needs a change in `.Runtime`,
that change belongs to the owning phase and must keep the IL arm green.
