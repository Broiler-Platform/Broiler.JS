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
CPUs), on 2026-11-02, with the frontend's code as stage SJL-13.2e commits it (after its review: section 3's last part):

- **Frames and bytes per level** (`--measure-frames`). For each recursive construct, a source nested past the request's
  nesting parameter is parsed on a thread of `ParseLimits.StackBytes`, and observed at the moment the library throws its
  nesting refusal, which is the deepest point of its recursion: the frames on the thread's stack (`StackTrace`) and the
  stack left below that point (a recursion of fixed frames, calibrated on two threads of known stacks, run until
  `RuntimeHelpers.TryEnsureSufficientExecutionStack` answers false). At parameters 64, 128, 256 and 512, the
  differences give the frames and the bytes per level of nesting, and the stack left at 512 against a baseline (a throw
  at the thread's start) gives the stack the parse used. The constructs are each production the parser recurses
  through, and the costliest of them again after the **precedence ladder** (`a || a && a | a ^ a & a == a < a << a + a
  * `, one operator of each of the grammar's ten binary precedences, before each level), and once with `#x in` in the
  ladder. For the tree depth, the same is observed at the tree-depth refusal of chains of each of the corpus's shapes,
  of 10,001 and of 100,000 nodes, and of left operands dividing the bound in three ways. The JIT is measured four ways,
  because its tiers give one method frames of different sizes: with every method at its first tier
  (`DOTNET_TC_CallCounting=0`); as it runs by default, **twenty times**, since which methods the JIT has replaced by
  their optimised tier when the parse runs differs from run to run; with every method promoted to its optimised tier,
  with dynamic profile-guided optimisation, after its first thirty calls (`DOTNET_TC_CallCountingDelayMs=0`, called
  "promoted" here), which is the tier the default reaches and its largest frames; and with every method optimised from
  the start, without profile-guided optimisation (`DOTNET_TieredCompilation=0`). Files:
  [`frames-jit-first-tier.tsv`](frames-jit-first-tier.tsv), [`frames-jit-tiered.tsv`](frames-jit-tiered.tsv) (the
  first default run) with [`frames-jit-tiered-runs.tsv`](frames-jit-tiered-runs.tsv) (all twenty),
  [`frames-jit-promoted.tsv`](frames-jit-promoted.tsv), [`frames-jit-optimised.tsv`](frames-jit-optimised.tsv) and
  [`frames-native-aot.tsv`](frames-native-aot.tsv).
- **The least stack, whole process** (`docs/evidence/sjl-13-001/tools/deep_nesting.py stack`). For **every one of the
  deep-nesting corpus's 54 sources** (G001 to G054: at, one past and far past each bound), each parsed alone in a fresh
  process (`--parse-one`, the JIT at its default tiers), the least thread stack at which the process still ends with the
  source's expected outcome, by bisection to 16 KiB between 64 KiB and the documented stack. Files:
  [`least-stack-jit.tsv`](least-stack-jit.tsv) and [`least-stack-native-aot.tsv`](least-stack-native-aot.tsv).

A level here is one unit of the nesting parameter the parser charges (each statement and each assignment expression,
each binding pattern, each prefix operator and `new`, each right operand of `**`, each class heritage): a construct that
charges two units per level of source shows half its source level's cost.

## 3. What one level costs

**Tree depth: nothing.** The parser builds a binary chain, a member or call chain and a decorator's member chain in a
loop (`ParseBinary`, `ParseChainLinks`, `ParseDecorators`), charging each node before it is built, and the declarations
walk keeps a stack of its own. At the tree-depth refusal the stack holds the same frames however long the chain is and
however a left operand and its chain share it (frames, then bytes used at the refusal):

| Shape at the refusal | JIT first tier | JIT promoted | JIT optimised | Native AOT |
|---|---|---|---|---|
| binary chain of 10,001 nodes, and of 100,000 | 20, 3,952 | 16, 5,776 | 18, 3,333 | 21, 2,736 |
| member chain of 10,001 nodes | 22, 6,688 | 17, 7,600 | 18, 4,545 | 22, 3,648 |
| a parenthesised chain of 1, 5,000 and 10,000 nodes beginning a chain of 10,000, 5,001 and 1 | 20, 3,952 | 15, 5,776 | 18, 3,333 | 21, 2,736 |
| a template's substitution holding a chain of 5,000, beginning one of 5,001 | 29, 7,296 | 19, 10,032 | 25, 5,757 | 28, 4,256 |
| a member chain of 5,000 beginning a binary chain of 5,001 | 20, 3,952 | 15, 5,776 | 18, 3,333 | 21, 2,736 |
| a parenthesised chain of 5,000 as the object of a member chain of 5,001 | 22, 6,688 | 17, 7,600 | 18, 4,545 | 22, 3,648 |

Every source of the deep-nesting corpus's tree rows (G037 to G054: the binary and member chains and the four
left-operand shapes, at the bound, one past it and far past it) ends as its row expects, parsed and walked, on a thread
of 64 KiB, the bisection's floor, under both runtimes.

**Nesting: from 1 to 10 frames and from 303 to 5,168 bytes per level** at the JIT's first tier, its optimised tier and
under Native AOT, by construct; the stack used at the library's supported maximum, 512 levels, is the last figure of
each cell (the full tables are in the `.tsv` files):

| Construct (one level of source) | JIT first tier: frames, bytes per level, used at 512 | JIT default, most of 20 runs: used at 512 | JIT promoted: used at 512 | JIT optimised | Native AOT |
|---|---|---|---|---|---|
| object literal's computed key `{[` ... `]: 1}` | 10, 5,168, **2,638,720** | **2,638,720** | **3,416,048** | 8, 4,688, **2,393,700** | 8, 3,295, **1,682,944** |
| object literal's property `{a: ` ... `}` | 9, 4,528, 2,312,528 | 2,312,528 | 2,934,512 | 7, 3,986, 2,035,857 | 7, 2,736, 1,397,488 |
| class method's return `class { m() { return ` | 8.5, 3,744, 1,912,464 | 1,912,464 | 3,049,120 | 7, 3,636, 1,856,784 | 7, 2,513, 1,283,184 |
| call's argument `f(` ... `)` | 8, 3,744, 1,912,464 | 1,912,464 | 2,958,832 | 6, 3,924, 2,003,436 | 6, 2,559, 1,307,504 |
| `import(` ... `)` | 7, 2,913, 1,488,384 | 1,488,384 | 2,886,176 | 6, 3,141, 1,604,688 | 6, 2,272, 1,160,672 |
| parentheses | 8, 3,376, 1,724,896 | 1,724,896 | 1,767,152 | 6, 2,424, 1,238,664 | 6, 1,792, 915,952 |
| templates and their substitutions | 9, 3,327, 1,700,272 | 1,700,272 | up to 2,208,256 | 7, 2,424, 1,238,664 | 7, 1,743, 891,328 |
| brackets, and spread elements | 8, 3,136, 1,602,384 | 1,602,384 | up to 2,437,168 | 6, 2,264, 1,157,460 | 6, 1,712, 875,216 |
| class heritage | 5, 2,528, 1,293,520 | 1,293,520 | 1,066,736 | 4, 1,786, 914,151 | 4, 1,440, 736,896 |
| binding patterns, array and object | 3, 1,760, 902,880 | 902,880 | up to 2,868,544 | 2, 2,424, 1,243,209 | 2, 1,984, 1,016,880 |
| function declarations | 4, 1,552, 795,872 | 795,872 | 1,934,656 | 4, 1,946, 997,173 | 4, 1,424, 729,904 |
| blocks | 3, 1,103, 566,352 | 566,352 | 943,616 | 2, 892, 458,136 | 2, 560, 287,584 |
| the rest (assignments, conditionals, arrows and async arrows, arrow bodies, prefix operators, `new`, `**`, `yield`, `if` and `while` statements) | 1 to 4, at most 1,592, at most 815,024 | at most 815,024 | at most 1,461,936 | 1 to 3.5, at most 1,419, at most 726,291 | 1 to 3.5, at most 1,032, at most 528,352 |
| any of the first eight after the precedence ladder | the same as without it | at most 2,638,720 | at most 3,416,352 | the same as without it | the same as without it |
| parentheses after the ladder with `#x in` | 10, 3,952, 2,018,256 | 2,181,504 | 2,992,272 | 8, 3,206, 1,638,321 | 8, 2,272, 1,161,280 |

The ladder costs nothing at the first tier, the optimised tier and under Native AOT: the precedence climb holds no
frame per precedence (below). `#x in` reads its right operand by one recursion of its own (`ParsePrivateIn` and a
`ParseBinary`), which cannot repeat without a level of nesting between, so it adds two frames to a level at most. In
the default runs, a frame of the climb replaced by its optimised tier while the parse runs leaves frames of both tiers
on the stack, so a construct after the ladder can use more than at either tier alone, up to 2,509,824 bytes in the
twenty runs, below the computed key's 2,638,720. Promoted, every method of the parse is at the optimised tier with
profile-guided optimisation, whose frames are the largest measured: the computed key needs 3,416,352 bytes at 512.

**The whole process**, each source parsed alone and judged by its exit (the bisection), needs at most **1,744 KiB under
the JIT and 912 KiB under Native AOT**, for the corpus's sources at, one past and far past the supported maximum
through parentheses and substitutions (G023, G024, G035 and G036 under the JIT; G022 to G024 and G034 to G036 under
Native AOT), and 64 KiB for every one of its trees. Below the
supported maximum, the sources at 64 levels need at most 272 KiB and 128 KiB. The declarations walk adds little to the
parser's recursion: blocks at 512 levels need 592 KiB accepted (parsed and walked) and 608 KiB refused one level past
(parsed to the refusal) under the JIT.

**What the first measurement missed, and the change it led to.** The derivation as first written at this stage measured
each construct alone and stated that each level is charged before the recursion. One recursion was not charged: the
precedence climb read each right operand of a higher precedence as `ParseBinary(precedence + 1)`, a ParseBinary and
ParseBinaryRest pair per precedence, grammar-bounded at ten pairs, between two levels of nesting. Measured with the
ladder before the computed key, at that code, a level cost 30 frames and 13,744 bytes at the JIT's first tier, 7,012,672
bytes at 512 (a margin of 2.4); 18 frames and 6,497 bytes under Native AOT, 3,315,120 bytes (5.1); and in the default
runs, where frames of both tiers mixed, up to 10,328,704 bytes in twenty runs (`import(` after the ladder; a margin of
1.6), against the 3,082,864 bytes (5.4) first stated. Every source still ended as expected on 16 MiB, but the margin was
below the three Broiler.VM's supported maximum nesting keeps. So the climb was reworked, as decision D14 says a path is
when its recursion will not do: `ParseBinary` reads the right operands of a higher precedence with an explicit stack of
the operators that wait (`pendingOperators`), and the right operand of `**`, which it still reads by recursion, is
charged as nesting as before. The tree, the token stream and the diagnostics are the same: the canonical dump of every
one of the parse corpus's 295,795 parses (`--digest-corpus`) has the same SHA-256 before and after the change, under
the JIT and from the Native AOT image. The rework keeps the operator handling out of the climb's frame, so a level
through it costs less than before: the computed key's 5,424 bytes at the first tier, 5,327 optimised and 3,616 under
Native AOT are 5,168, 4,688 and 3,295 now.

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
  maximum, 512, the costliest construct measured, the computed key, alone or after the precedence ladder, needs
  2,638,720 bytes at the JIT's first tier and in its default runs, 3,416,352 with every method promoted, 2,393,700
  optimised and 1,682,944 under Native AOT.
- **The margin**: 16 MiB over those, **about 6.4 at the JIT's first tier and in its default runs, 4.9 promoted, 7.0
  optimised and 10.0 under Native AOT**; over the corpus's sources, whole process, 9.4 under the JIT and 18.0 under
  Native AOT. JSD-0022 took about six for Broiler.VM's tree depth, and Broiler.VM's supported maximum nesting keeps
  three. The least, 4.9, is the JIT's optimised tier with profile-guided optimisation, which the default reaches for a
  method the process has called often: below JSD-0022's six, above Broiler.VM's three.
- **Broiler.VM's bound is admitted on the stack `CompilationStack` declares**: every tree at 10,000 and every source at
  512 levels of nesting through every construct measured, with the margins above. **One hot path was reworked to an
  explicit stack**, the precedence climb in `ParseBinary` (section 3's last part); the other two recursions the tree
  depth could have reached, the chain builders and the declarations walk, were loops and an explicit stack from stages
  4c and 4d.
- **The supported maximum nesting, `ParseLimits.MaximumSupportedNestingDepth`, 512**, is confirmed and not raised:
  twice it would leave the JIT promoted a margin under three.

## 5. What the tree-depth charge does not reach

Decision D14 charges a chain's nodes and a **left operand's spine**: a parenthesised chain, a template's substitution, a
member or call chain, and a parenthesised chain as a member chain's object, each beginning a chain, are charged
together, and the deep-nesting corpus holds all four. What the charge counts is the depth **along a chain and the spines
of its left operands**; it does not count the depth at which a **right operand's** subtree hangs. A chain is charged
from the tree depth its parse began at, and a chain's nodes are charged as they are built, so a right operand read
before its chain's first node is built is charged from the chain's start, and its own subtree, whatever its depth,
leaves the chain's count where it was. Four shapes follow, each probed on 2026-11-02 with `--parse-one` (configuration
V, nesting parameter 512), each with 9,999 nodes on each side:

- **A right operand of a chain's early nodes**, with no parentheses: `x = 1 + 2*2*...*2 + 3+...+3` is accepted. The `*`
  chain is the first `+` node's right operand, read before that node is built, so it is charged from 0 to 9,999 and
  released; the `+` chain then reaches 10,000. The `*` chain hangs under the deepest `+` node, so the tree is about
  20,000 deep. `x = a + (b+...+b) + c+...+c` is accepted in the same way. Since each level of parentheses costs the
  nesting bound one or two units, and each can begin such a right operand again, the depth **multiplies with nesting**:
  bounded by about the nesting bound's levels times the constant, not by the constant.
- **A chain that begins with an expression whose spine D14 does not name**: a prefix operator's operand, an array
  literal's element, a call's argument: `x = -(a+a+...+a) + b+...+b`, `x = [a+...+a].b...b` and `x = f(a+...+a) +
  b+...+b` are accepted, their trees about 20,000 deep.
- **A parenthesised sequence or conditional as the left operand**: `x = (0, a+...+a) + b+...+b` and
  `x = (c ? 0 : a+...+a) + b+...+b` are accepted. A SequenceExpression and a ConditionalExpression carry a spine of 0,
  so the chain inside them is not charged to the chain they begin.
- **The control**: `x = (a+...+a) + b+...+b`, D14's first shape, is refused with JSE9202 at 10,000.

The library is not reached by any of them, since a level of tree depth costs it no frame (section 3), and Broiler.VM's
base charges them the same way (its `JsParser.Continue` restores the depth it saved for each call and reads a right
operand through `ParseBinary`), so step 1 moves no answer for them. A consumer whose walks recurse per node and lean on
the constant, as JSD-0022's do, meets trees deeper than the constant there. Whether the charge should reach them is
decision D14's to revise, not this stage's: JSD-0083 names the four shapes, and a wider charge would move refusals no
record lists.

## 6. Measuring it again

A construct that grows a frame per level, or a runtime whose frames grow, moves these figures. To measure again, from
the repository root, after building the probe in Release:

    dotnet Broiler.ECMAScript/Broiler.ECMAScript.Probe/bin/Release/net10.0/Broiler.ECMAScript.Probe.dll --measure-frames
    DOTNET_TC_CallCounting=0 dotnet .../Broiler.ECMAScript.Probe.dll --measure-frames
    DOTNET_TC_CallCountingDelayMs=0 dotnet .../Broiler.ECMAScript.Probe.dll --measure-frames
    DOTNET_TieredCompilation=0 dotnet .../Broiler.ECMAScript.Probe.dll --measure-frames
    Broiler.ECMAScript/Broiler.ECMAScript.Probe/bin/Release/net10.0/linux-x64/publish/Broiler.ECMAScript.Probe --measure-frames
    python3 -I docs/evidence/sjl-13-001/tools/deep_nesting.py stack --probe "<the probe's command>"

The first was run twenty times. The least-stack files hold every row of the corpus, G001 to G054, under each runtime.
