# The shared syntax library's limits: the derivation

[← The frontend's design](../design.md)

**Written:** 2026-11-02, at stage SJL-13.2e of milestone SJL-13, under Broiler.VM's JSD-0083, decision D14: "The
tree-depth constant is derived at SJL-13.2e as JSD-0022 derived this family's, with the stack it requires stated beside
it; the derivation must admit this family's bound on the stack `CompilationStack` declares; if it cannot, the library is
reworked (explicit stacks), not the bound lowered." **What this is:** a measurement and what follows from it, retained
with its figures (the `.tsv` files beside it). It is no evidence of a rule part: the home rule's part G
(`docs/evidence/sjl-13-001/decision-rule.md`) reads the derivation at the library commit, and runs the deep-nesting
corpus there.

## 1. How JSD-0022 derived Broiler.VM's bound, and what the library's version of it is

Broiler.VM's JSD-0022 bounds what the **tree** costs the walks that recurse over it once per node, and derives the
bound from a stack: "the measured failure point on the caller's stack, multiplied by the declared stack's ratio to it,
divided by a margin of about six" - a left spine of about five thousand nodes ended the process on a one-mebibyte main
thread, so about sixty thousand survive on the sixteen mebibytes `CompilationStack` declares, and ten thousand is the
bound. The same component bounds the **parser's own recursion** separately, by the nesting bound, whose supported
maximum (`SliceParseOptions.MaximumSupportedNestingDepth`, 512) it set at a third of a measured failure point.

The library's derivation asks the same two questions of the library's code: what does one level of tree depth cost the
library's stack, and what does one level of nesting cost it, measured as the frames and bytes each level adds, under the
JIT and under Native AOT; then what stack the library needs at its bounds, and what margin the declared stack leaves.

## 2. How it was measured

Two measurements, both with the probe of this repository (`Broiler.ECMAScript/Broiler.ECMAScript.Probe`, Release),
under the JIT (.NET 10.0.12) and as its Native AOT image (linux-x64), on one host (Linux 6.18.44, x86_64, 4 logical
CPUs), on 2026-11-02, with the frontend's code as stage SJL-13.2e commits it:

- **Frames and bytes per level** (`--measure-frames`). For each recursive construct, a source nested past the request's
  nesting parameter is parsed on a thread of `ParseLimits.StackBytes`, and observed at the moment the library throws its
  nesting refusal, which is the deepest point of its recursion: the frames on the thread's stack (`StackTrace`) and the
  stack left below that point (a recursion of fixed frames, calibrated on two threads of known stacks, run until
  `RuntimeHelpers.TryEnsureSufficientExecutionStack` answers false). At parameters 64, 128, 256 and 512, the
  differences give the frames and the bytes per level of nesting, and the stack left at 512 against a baseline (a throw
  at the thread's start) gives the stack the parse used. For the tree depth, the same is observed at the tree-depth
  refusal of chains of each of the corpus's shapes, of 10,001 and of 100,000 nodes, and of left operands dividing the
  bound in three ways. The JIT is measured three ways, because its tiers give one method frames of different sizes:
  with every method at its first tier (`DOTNET_TC_CallCounting=0`), as it runs by default (methods replaced by their
  optimised tier during the run), and with every method optimised from the start (`DOTNET_TieredCompilation=0`).
  Files: [`frames-jit-first-tier.tsv`](frames-jit-first-tier.tsv), [`frames-jit-tiered.tsv`](frames-jit-tiered.tsv),
  [`frames-jit-optimised.tsv`](frames-jit-optimised.tsv) and [`frames-native-aot.tsv`](frames-native-aot.tsv).
- **The least stack, whole process** (`docs/evidence/sjl-13-001/tools/deep_nesting.py stack`). For the deep-nesting
  corpus's sources at, one past and far past each bound, each parsed alone in a fresh process (`--parse-one`, the JIT at
  its default tiers), the least thread stack at which the process still ends with the source's expected outcome, by
  bisection to 16 KiB between 64 KiB and the documented stack. Files: [`least-stack-jit.tsv`](least-stack-jit.tsv) and
  [`least-stack-native-aot.tsv`](least-stack-native-aot.tsv).

A level here is one unit of the nesting parameter the parser charges (each statement and each assignment expression,
each binding pattern, each prefix operator and `new`, each right operand of `**`, each class heritage): a construct that
charges two units per level of source shows half its source level's cost.

## 3. What one level costs

**Tree depth: nothing.** The parser builds a binary chain and a member or call chain in a loop
(`ParseBinaryRest`, `ParseChainLinks`), charging each node before it is built, and the declarations walk keeps a stack
of its own. At the tree-depth refusal the stack holds the same frames however long the chain is and however a left
operand and its chain share it:

| Shape at the refusal | JIT first tier | JIT optimised | Native AOT |
|---|---|---|---|
| binary chain of 10,001 nodes, and of 100,000 | 19 frames, 5,168 bytes | 16 frames, 3,333 bytes | 19 frames, 2,432 bytes |
| member chain of 10,001 nodes | 22, 6,992 | 18, 5,151 | 22, 3,952 |
| a parenthesised chain of 1, 5,000 and 10,000 nodes beginning a chain of 10,000, 5,001 and 1 | 19, at most 5,168 | 16, 3,333 | 19, 2,432 |
| a template's substitution holding a chain of 5,000, beginning one of 5,001 | 28, 8,512 | 23, 6,363 | 26, 4,560 |
| a member chain of 5,000 beginning a binary chain of 5,001 | 19, 5,168 | 16, 3,333 | 19, 2,432 |
| a parenthesised chain of 5,000 as the object of a member chain of 5,001 | 22, 6,992 | 18, 5,151 | 22, 3,952 |

Every tree at the bound in the deep-nesting corpus (rows G037 to G052: the binary and member chains of 10,000 nodes and
the four left-operand shapes at 5,000 and 5,000) is parsed and walked, and every one far past it refused, on a thread
of 64 KiB, the bisection's floor, under both runtimes.

**Nesting: from 1 to 10 frames and from 288 to 6,032 bytes per level**, by construct and runtime; the stack used at the
library's supported maximum, 512 levels, is the last column of each (the full table is in the `.tsv` files):

| Construct (one level of source) | JIT first tier: frames, bytes per level, used at 512 | JIT tiered | JIT optimised | Native AOT |
|---|---|---|---|---|
| object literal's computed key `{[` ... `]: 1}` | 10, 5,424, 2,769,440 | 6, 6,032, **3,082,864** | 8, 5,327, 2,719,122 | 8, 3,616, **1,846,192** |
| object literal's property `{a: ` ... `}` | 9, 4,784, 2,442,944 | 9, 4,784, 2,442,944 | 7, 4,625, 2,361,279 | 7, 3,056, 1,560,432 |
| class method's return `class { m() { return ` | 8.5, 3,872, 1,977,824 | 7, 4,871, 2,491,280 | 7, 3,955, 2,019,495 | 7, 2,672, 1,364,656 |
| call's argument `f(` ... `)` | 8, 4,000, 2,043,184 | 8, 4,000, 2,043,184 | 6, 4,561, 2,328,555 | 6, 2,880, 1,470,752 |
| `import(` ... `)` | 7, 3,168, 1,618,800 | 4, 4,320, 2,209,776 | 6, 3,779, 1,930,110 | 6, 2,592, 1,323,920 |
| parentheses | 8, 3,632, 1,855,312 | 8, 3,632, 1,855,312 | 6, 3,062, 1,564,086 | 6, 2,112, 1,079,200 |
| templates and their substitutions | 9, 3,584, 1,830,992 | 9, 3,584, 1,830,992 | 7, 3,062, 1,564,086 | 7, 2,064, 1,054,576 |
| brackets, and spread elements | 8, 3,392, 1,733,104 | 8 and 4, 3,392 and 4,000, up to 2,046,832 | 6, 2,903, 1,482,882 | 6, 2,032, 1,038,464 |
| class heritage | 5, 2,528, 1,293,824 | 5, 2,960, 1,517,264 | 4, 1,786, 914,757 | 4, 1,440, 737,200 |
| binding patterns, array and object | 3, 1,760, 902,880 | 3, 1,760, 902,880 | 2, 2,424, 1,243,209 | 2, 1,984, 1,016,880 |
| function declarations | 4, 1,552, 795,872 | 4, 1,552, 795,872 | 4, 1,946, 997,173 | 4, 1,424, 729,904 |
| blocks | 3, 1,104, 566,352 | 3, 1,104, 566,352 | 2, 893, 458,136 | 2, 560, 287,584 |
| the rest (assignments, conditionals, arrows and async arrows, arrow bodies, prefix operators, `new`, `**`, `yield`, `if` and `while` statements) | 1 to 4, at most 1,592, at most 815,024 | 1 to 4, at most 3,008, at most 1,540,672 | 1 to 3.5, at most 1,419, at most 726,291 | 1 to 3.5, at most 1,032, at most 528,352 |

The tiered JIT's figures mix tiers: a method replaced during the run leaves frames of both sizes on the stack, so its
worst case (the computed key, 6,032 bytes a level) is above both the first tier's and the optimised tier's.

**The whole process**, each source parsed alone and judged by its exit (the bisection), needs at most 1,872 KiB under
the JIT and 1,072 KiB under Native AOT for the corpus's sources at, one past and far past the supported maximum (the
parentheses and the substitutions under the JIT, the parentheses and the substitutions under Native AOT), and 64 KiB for
its trees. The declarations walk adds little to the parser's recursion: blocks at 512 levels need 592 KiB accepted
(parsed and walked) and 608 KiB refused one level past (parsed to the refusal) under the JIT.

## 4. What follows

- **The tree-depth constant.** JSD-0022's formula needs a failure point, and the library has none in tree depth: no
  stack fails earlier for a deeper tree. So nothing in the library bounds the constant, and decision D14 sets it: it is
  Broiler.VM's bound, `SliceParseOptions.MaximumTreeDepth`, **10,000**, which a test of Broiler.VM holds equal to
  `ParseLimits.MaximumTreeDepth` when Broiler.VM references the library (its step 1; this repository references nothing
  of Broiler.VM). The provisional value stage 4c set is the derived value, and the constant's name stays the contract's.
  A walk of the tree that recursed once per level, as Broiler.VM's lowering does, is its consumer's to derive against
  the consumer's stack, as JSD-0022 did; the library's parse and walk need 64 KiB at the bound.
- **The stack the library documents, `ParseLimits.StackBytes`: 16 MiB, the stack `CompilationStack` declares**
  (Broiler.VM `013e385`, `CompilationStack.CompileStackBytes`). What it pays for is the nesting bound: at the supported
  maximum, 512, the costliest construct measured needs 3,082,864 bytes under the JIT and 1,846,192 under Native AOT.
- **The margin**: 16 MiB over those, **about 5.4 under the JIT and 9.1 under Native AOT**; over the corpus's sources,
  whole process, 8.75 and 15.3. JSD-0022 took about six for Broiler.VM's tree depth, and Broiler.VM's supported maximum
  nesting keeps three. The JIT's 5.4 is the tiered JIT's mixed frames at the costliest construct; its first tier alone
  gives 6.1.
- **Broiler.VM's bound is admitted on the stack `CompilationStack` declares**: every tree at 10,000 and every source at
  512 levels of nesting through every construct measured, with the margins above. **No hot path needed reworking to an
  explicit stack**: the two recursions the tree depth could have reached, the chain builders and the declarations walk,
  were loops and an explicit stack from stages 4c and 4d.
- **The supported maximum nesting, `ParseLimits.MaximumSupportedNestingDepth`, 512**, is confirmed and not raised:
  twice it would leave the JIT a margin under three.

## 5. What the tree-depth charge does not reach

Decision D14 charges a chain's nodes and a **left operand's spine**: a parenthesised chain, a template's substitution, a
member or call chain, and a parenthesised chain as a member chain's object, each beginning a chain, are charged
together, and the deep-nesting corpus holds all four. A chain that begins with an expression whose spine D14 does not
name - a prefix operator's operand, an array literal's element, a call's argument - is charged from that expression's
own start: `x = -(a+a+...+a) + b+...+b`, `x = [a+...+a].b...b` and `x = f(a+...+a) + b+...+b`, with 9,999 nodes on each
side, are accepted, their trees about 20,000 deep, where the parenthesised control is refused (probed on 2026-11-02 with
`--parse-one`). The library is not reached by them, since a level of tree depth costs it nothing (section 3), and
Broiler.VM's base charges them the same way, so step 1 moves no answer for them. A consumer whose walks recurse per node
and lean on the constant, as JSD-0022's do, meets trees deeper than it there: each such operand, which costs the nesting
bound a level or two, can hold another chain of up to the constant. Whether the charge should reach them is decision D14's to revise, not this stage's: JSD-0083 names
the four shapes, and a wider charge would move refusals no record lists.

## 6. Measuring it again

A construct that grows a frame per level, or a runtime whose frames grow, moves these figures. To measure again, from
the repository root, after building the probe in Release:

    dotnet Broiler.ECMAScript/Broiler.ECMAScript.Probe/bin/Release/net10.0/Broiler.ECMAScript.Probe.dll --measure-frames
    DOTNET_TC_CallCounting=0 dotnet .../Broiler.ECMAScript.Probe.dll --measure-frames
    DOTNET_TieredCompilation=0 dotnet .../Broiler.ECMAScript.Probe.dll --measure-frames
    Broiler.ECMAScript/Broiler.ECMAScript.Probe/bin/Release/net10.0/linux-x64/publish/Broiler.ECMAScript.Probe --measure-frames
    python3 -I docs/evidence/sjl-13-001/tools/deep_nesting.py stack --probe "<the probe's command>" --rows <ids>

The rows measured here were G001, G013, G019 to G023, G025, G026, G028, G029, G031, G032, G034 to G037, G039, G040, G043,
G046, G049 and G052.
