# Changelog — Broiler.JS

Changes to the engine (the `Broiler.JavaScript.*` packages) a consumer can observe, newest first.
This file starts with the shared JavaScript libraries programme: under its decision SJL-D-16 (a), a
cut-over that moves an area to a shared library lists here, by name, every result that changes
because the shared implementation follows the specification where this engine did not, each with the
vector or test262 path that names it. Earlier changes are recorded in the history and in
[`docs/public-api.md`](../docs/public-api.md). The shared libraries have their own changelog,
[`Broiler.ECMAScript/CHANGELOG.md`](../Broiler.ECMAScript/CHANGELOG.md).

## Unreleased

### The for-loop desugaring moves from the parser to a preparation pass of the compiler (2026-10-21, milestone SJL-12.5)

The parser returns every `for`, `for-in` and `for-of` loop as written: the head's declaration, test and update,
and the body. Until this change `FastParser.ParseProgram` and `FastParser.ParseModule` rewrote a loop whose head
declares `let`, `const`, `using` or `await using` bindings into per-iteration carriers, set `HeadTdzNames` on a
lexical `for-in` or `for-of` loop, and wrapped a C-style `await using` loop in the block that disposes it. The
compiler now does all three in a preparation pass of its own, `ForLoopPreparation` (internal), which runs right
after it parses a program, in `FastCompiler` and in `DirectEvalSupport`, before any validation reads the tree, and
emits the trees the parser emitted. This is item 5 of milestone SJL-12.5 (Broiler.VM's JSD-0080, decisions D22 and
D23). Nothing a script can observe changes. One public member changes: `DirectEvalSupport.Execute`, the method the
compiled code calls for a direct eval, gains a parameter (below). No other public member is added, removed or
changed: the public API listings of `Ast`, `Parser`, `Compiler`, `Engine` and `Runtime`, written by
PublicApiGenerator 11.4.6 from `792ff56e` and from this change, differ in that one member alone.
[`docs/public-api.md`](../docs/public-api.md) records it.

- **A program that reads the parser's tree sees the loops as written.** `HeadTdzNames` is null on the parser's
  loops, and a lexical head is the declaration as written, with no temporaries. A host that compiles the parser's
  tree itself, rather than through `FastCompiler`, gets the loops unprepared.
- **The temporaries are named `<d>:<n>`** (decision D22): `d` is the direct-eval nesting depth the compilation
  inherits and `n` counts the temporaries of one pass, from 1. They were per-parse numbers from 2. A name with a
  colon is never an identifier, and the names depend on nothing but the tree and the depth. `d` is a compile-time
  depth: direct eval code is at one more than the depth the code that calls `eval` was compiled at, which the
  compiler passes as a constant of the call it emits, and every other compilation (a script, a module, an indirect
  eval, a `Function` constructor's body, even one compiled while a direct eval runs) is at 0. Along a chain of
  direct evals, then, each eval code's carriers are named apart from those of every compilation enclosing it,
  however late the calling code runs: a function compiled in eval code and called after that eval returned names
  its own direct evals' carriers one level deeper than its own. Two direct evals from the same code are both one
  level deeper than it and may name alike; neither encloses the other.
- **`DirectEvalSupport.Execute` takes the depth of the eval code**, `int directEvalDepth = 1`, a new parameter
  before its last, `tailCall`: the depth it validates and compiles the eval code at. The compiler passes its own
  depth + 1; the default, 1, is that of eval called from code compiled outside any direct eval. A call that
  passes its arguments by position up to `catchParameterNames` and `tailCall` by name compiles unchanged; code
  compiled against the old signature must be rebuilt.
- **A loop records whether its body holds a `continue`** (`ContainsContinue`, internal), as the parser counts them,
  and the pass reads it to decide whether a `let` loop copies its bindings back in a `finally`. `Ast` grants its
  internals to `Compiler` for it. It is internal rather than a public init-only property because the tree's
  public members are the frontend's contract, and which raw facts a shared tree publishes is SJL-14's to decide
  (JSD-0080, decision D23); the grant is wider than the one field, since `Compiler` could now reach every internal
  of `Ast`, the Unicode tables, their adapter and the regular-expression checks among them, though it reads only
  `ContainsContinue`. `JSContext` gains the internal `DirectEvalCodeDepth`, the depth `FastCompiler`
  names a program's temporaries by, which `DirectEvalSupport` sets around the compile of eval code alone
  (`CompileDirectEvalCodeAt`).
- **The rewrite's refusal moves with it.** A binding the rename cannot handle, which the parser refused with
  "Unknown token", is refused by the compile, at the same token with the same message, when nothing later in the
  source fails first; the parser alone now accepts it. The parser's rewrite refused it while the loop was parsed,
  before any later token was read, but the parser now parses the whole program before the pass runs, so a parse
  error later in the source, or an early error the parser raises after the loop, is now the one reported. No input
  of the parse corpus is refused with "Unknown token" at `792ff56e`.
- **The pass costs a walk.** It walks every program the compiler compiles once more, whether or not the program
  holds a lexical `for` head, and the code of an eval, direct or indirect, up to twice (for `DirectEvalSupport`'s
  validation and for its compile); the parser's rewrite took no walk of its own. The cost is not measured, and no
  speed is claimed.
- **The parser's architecture test** (`Architecture.Tests`) asserts that the parser has no violation: the three
  desugaring walkers, `AstIdentifierReplacer`, `IdentifierNameCollector` and `ClosureDetector`, leave the parser
  (`Ast` still declares them, and only the pass uses them), and a `for (let …)` loop parses to one for-statement
  whose initialiser is the declaration as written. The test's list of violations, empty now, goes.

### The parser no longer references the runtime; the Unicode tables and the regular-expression checks move to `Ast` (2026-10-21, milestone SJL-12.5)

The parser reached five types in `Runtime`: the Unicode adapter `JSUnicode`, over the tables the runtime embedded,
and the regular-expression literal's checks `RegExpUnicodeValidator`, `RegExpGroupNames` and
`JSUnicodePropertyNames`, and `CancellableDisposableAction`, which `FastTokenStream.UndoMark` returns. They move to
`Broiler.JavaScript.Ast`, which the runtime and the parser both reference, each with its namespace and its
visibility, and `Parser` no longer references `Runtime`. The parser's source and metadata still name the
namespaces `Broiler.JavaScript.Runtime` and `Broiler.JavaScript.Runtime.Unicode`: those names are the five moved
types', which keep their namespaces (a forwarder needs its type's full name), and the types they resolve to are
`Ast`'s. The milestone's clause that the parser references no `Runtime` type is read by assembly, as part B of
bundle sjl-12-001's rule reads it: the parser's metadata has no `AssemblyRef` to `Broiler.JavaScript.Runtime`.
This is item 4 of milestone SJL-12.5 (Broiler.VM's JSD-0080, decisions D20 and D29). Nothing a script can
observe changes: the tables are the same bytes, the checks the same code, and the literal and the `RegExp`
constructor still refuse the same patterns.
[`docs/public-api.md`](../docs/public-api.md) lists each member with its disposition.

- **The moved public types keep a forwarder** in `Runtime`: `RegExpUnicodeValidator` and
  `CancellableDisposableAction`, namespace `Broiler.JavaScript.Runtime`, now in `Ast`. An assembly built against
  the earlier `Runtime` binds to them unchanged where `Runtime` is deployed, since the forwarders are `Runtime`'s.
- **`FastTokenStream.UndoMark` is obsolete** (decision SJL-D-25 (b)). Its message names the replacement: read
  `Current` to mark the position and call `Reset(FastToken)` to return to it. It is kept, unchanged, until a
  recorded check of its consumers allows its removal (SJL-D-25 (c)); a call to it now warns (CS0618).
- **The `Parser` package no longer depends on `Runtime`. This is source-breaking for a transitive user**: a
  project that references `Broiler.JavaScript.Parser` but not `Broiler.JavaScript.Runtime`, and names a `Runtime`
  type such as `JSValue`, must now reference `Runtime` itself. The two forwarded types still compile through
  `Parser` alone. It breaks such a library at run time too, before anyone recompiles it: a library built against
  the earlier `Parser` package alone that names a `Runtime` type, the two forwarded types and `UndoMark`'s return
  type included, refers to `Broiler.JavaScript.Runtime` in its metadata, so it needs `Runtime` in the
  application's package graph, which `Parser` no longer brings; without it the library fails with a
  `FileNotFoundException` when it runs, and the forwarders cannot help, being in the missing assembly. The `Ast`
  package now depends on `Broiler.Unicode.Algorithms`, which `Runtime` and `Parser` reach through it, and the
  engine set's pristine consumer check (`scripts/packaging/test_pristine_consumer.py`) now expects that
  dependency in `Ast`'s package, where it expected it in `Runtime`'s, and `Ast` in `Runtime`'s.
- **The internal types moved with them**: the tables `JSUnicodeTableData` (`Unicode/JSUnicodeTableData.g.cs`),
  their `IUnicodeTables` provider `JSUnicodeTables`, and `JSUnicode`, `JSUnicodePropertyNames` and
  `RegExpGroupNames`. `Ast` grants its internals to `Parser` and `BuiltIns`, which read them, and `Runtime` no
  longer grants its internals to `Parser`.
- **`FastParserOptions.UnicodeTables` stays null by default**, and null is the engine's own tables, which `Ast`
  now holds; options keep their equality. No default throws.
- **The tests move with the tables**: `UnicodeTableRegenerationTests` and `JSUnicodeTests`, from
  `Runtime.Tests` to `Ast.Tests`. The regeneration writes the same tables; the generated file's header names the
  test project that regenerates it, so two of its comment lines changed. `Runtime`'s metadata no longer
  references `Broiler.Unicode.Algorithms`, and `Ast`'s does, so `UnicodeDependencyTests` (`BuiltIns.Tests`) now
  asserts the owner's algorithms of `Ast`, and holds `Ast`, as it holds the others, free of the retired Unicode
  packages and of the segmenter.
- **The parser's architecture test** (`Architecture.Tests`) no longer lists "references the assembly
  Broiler.JavaScript.Runtime" among the parser's violations; the parser's metadata has no `AssemblyRef` to it.

### The parser and the compiler take explicit options; corrections J1 and J2 (2026-10-20, milestone SJL-12.5)

The goal (script or module) and the permission to await at the top level reached the parser through two
ambient scopes of `Runtime`'s `CoreScript`, which `EvalWithTopLevelAwaitAsync` and the module loader entered
around a compile, and which flowed into everything the compile and the program then ran. They are options
now: an immutable `FastParserOptions`, which the scanner, the token stream and the parser are made with, and
`JSCompilationOptions.AllowTopLevelAwait`, which reaches the compiler through `IJSCompiler` and the code
cache's key. Every compile site passes its own, and the scopes are deleted. This is item 1 of milestone
SJL-12.5 (Broiler.VM's JSD-0080, decision D19); the two results that change are corrections J1 and J2 of
bundle `sjl-12-001`'s rule (decision D21), listed here under decision SJL-D-16 (a). These names are SJL-12's,
not the text area's correspondence rows of the same names.

- **J1: code compiled while `EvalWithTopLevelAwaitAsync` runs, but not as its own program, is compiled as the
  specification's goal without top-level `await`, as under `Eval`.** This covers direct and indirect `eval`,
  `ShadowRealm.prototype.evaluate`, the `Function` constructor and the debugger's `Runtime.evaluate`,
  `Runtime.compileScript` and `Runtime.callFunctionOn`. It reaches as well a CommonJS module the program
  loads, and a host's compile from a function the program calls, through the context's own `Eval` (as a
  shell's `$262.evalScript` does), `CoreScript.Compile` without options or `CoreScript.Evaluate`: each is
  compiled as under `Eval`. Under `EvalWithTopLevelAwaitAsync`, before and after:
  - `eval('await 1')`, `(0, eval)('await 1')`, `eval("eval('await 1')")` and
    `eval('for await (const v of []) {}')`, also after the program's own `await`: compiled with the
    permission, each answered a Promise (as the program's result, which the method awaits, the survey's
    probe at `24510b0e` saw `eval('await 1')` answer 1); now a `SyntaxError`, as under `Eval`;
  - `new ShadowRealm().evaluate('await 1')`: a `TypeError` ("ShadowRealm cannot wrap non-callable objects",
    the realm's code having compiled to a Promise); now the `SyntaxError` "ShadowRealm.prototype.evaluate:
    the provided source text could not be parsed", as under `Eval`;
  - `Function('}); await 1; ({')`, a body that closes the function: compiled, and answered a Promise; now a
    `SyntaxError`, as under `Eval`;
  - `await 1` compiled by a host function the program calls, with the context's `Eval` (which answered a
    Promise) or with `CoreScript.Compile` (which compiled it): now a `SyntaxError`, as under `Eval`;
  - the debugger's `Runtime.evaluate` and `Runtime.compileScript` of `await 1`, called while the program
    runs: compiled with the permission (`Runtime.evaluate` answered a Promise), and `Runtime.callFunctionOn`
    threw a `TypeError` calling the Promise its compile answered; now each reports a `SyntaxError`, as when
    no such program runs;
  - a CommonJS module that awaits at its top level, imported by the program: loaded; now the import is
    rejected with a `SyntaxError`, as under `ExecuteAsync`, which compiles its program as `Eval` does.

  A program's own top-level `await` is unchanged, and so is code it compiles that does not await.
  `TopLevelAwaitEvalCodeTests` (Integration.Tests), `DebuggerTests` (Debugger.Tests) and
  `TopLevelAwaitCommonJsTests` (Modules.Tests) hold these cases. Run over `48fee2d3`, nine of
  `TopLevelAwaitEvalCodeTests`' ten fail, the one that passes being the program keeping its permission; the
  debugger's test fails; and `TopLevelAwaitCommonJsTests` fails its refusal under
  `EvalWithTopLevelAwaitAsync` and passes its module that does not await.
- **J2: the code cache keys a compilation on its top-level-`await` permission**, so one context no longer
  answers a compilation from the other mode's entry. In a script `await (21)` calls a function named
  `await`, and with the permission it awaits 21: with `globalThis.await = x => x * 2`, the same text
  answered 42 against 21, or a Promise, by which of `Eval` and `EvalWithTopLevelAwaitAsync` compiled it first
  in the context (the critic's probe at `24510b0e`). Each now answers per call: 42 under `Eval`, 21 under
  `EvalWithTopLevelAwaitAsync`. `TopLevelAwaitCodeCacheTests` (Integration.Tests) holds the probe's cases;
  run over `48fee2d3`, its two cases in one context fail, and the case in fresh contexts passes.
- **The message "Unexpected await" is kept.** It is the refusal of a top-level `await` without the
  permission, which the shell's retry reads (`Broiler.JavaScript/Program.cs`) to run a script again with it.
- **New public members** (decisions D19 and D29; [`docs/public-api.md`](../docs/public-api.md) lists each with
  its disposition), all added beside the existing ones, none removed:
  - `Parser`: `FastParserGoal` (`Script`, `Module`, and `Eval`, `FormalParameters` and `FunctionBody`
    reserved and refused); `FastParserOptions`, a sealed record with `Script`, `Module`, `Goal`,
    `AllowTopLevelAwait` and `UnicodeTables` (a Broiler.Unicode `IUnicodeTables`, null for the engine's own
    tables); the constructors `FastScanner(FastPool, in StringSpan, FastKeywordMap, FastParserOptions)`,
    `FastTokenStream(in StringSpan, FastKeywordMap, FastParserOptions)`,
    `FastTokenStream(FastPool, in StringSpan, FastKeywordMap, FastParserOptions)` and
    `FastParser(FastTokenStream, FastParserOptions)`, which refuses a stream made with other options;
    `FastParser.ParseModule(in StringSpan, FastParserOptions)`; and `Options` on the scanner, the stream and
    the parser;
  - `Runtime`: `JSCompilationOptions.AllowTopLevelAwait` (`get; init;`), and the default interface method
    `IJSCompiler.Compile(in StringSpan, in JSCompilationOptions, string, IList<string>, ICodeCache)`;
  - `Engine`: `DefaultJSCompiler.Register` with a function of the options, and
    `DefaultJSCompiler.Compile(in StringSpan, in JSCompilationOptions, string, IList<string>, ICodeCache)`;
    with the new `Register`, a call `DefaultJSCompiler.Register(null)` or `Register(default)`, which could
    only throw, no longer compiles, since both overloads accept the literal;
  - `Compiler`: `FastCompiler(in StringSpan, in JSCompilationOptions, string, IList<string>, ICodeCache)`.
- **A host's own `IJSCompiler`: a behavioural change.** `CoreScript.Compile` now calls the new `Compile` with
  the options. Its default forwards a script without top-level `await` to the existing member and throws
  `NotSupportedException`, naming the compiler's type, for a module or a top-level `await`. A host compiler
  set as `CoreScript.Compiler` that implements only the existing member, and parsed with this engine's
  parser, compiled modules and top-level `await` through the ambient scopes before; it now implements the
  new member to compile them. A function registered with `DefaultJSCompiler.Register` without options is
  refused them in the same way.
- **The members without options compile a script without top-level `await`**: `FastScanner`'s and
  `FastTokenStream`'s constructors without options (and `FastParser` over such a stream), `FastCompiler`'s
  constructor without options, and `DefaultJSCompiler.Compile` without options. They read the ambient scopes
  before, which a public caller met inside a compile that `CoreScript` ran, such as a host compiler's, and,
  for the top-level-`await` permission, anywhere while a program `EvalWithTopLevelAwaitAsync` was given ran,
  its continuations included. `CoreScript.Compile` without options or `isModule`, and `CoreScript.Evaluate`,
  `EvaluateAsync` and `EvaluateWithTasks`, compile a script without top-level `await`, taking the current
  context's other options. `FastParser.ParseModule` without options parses a module with the permission, as
  before.
- **The debugger compiles under the options of the context it runs the code in.** `Runtime.evaluate` and
  `Runtime.callFunctionOn` take that context's options, and `Runtime.compileScript` those of the context the
  call names, or else the first; they took the options of whichever context was current on the calling
  thread, which differ in host inputs (`ScriptHostMode`, the feature flags, the backend) when it is another
  context or none.
- **`JSCode.Key`'s text changed**: the options in it now end with `AllowTopLevelAwait = False` or `True`. A
  module the module loader compiles carries `True`, since the loader passes the permission as an option;
  `CoreScript.Compile` called with `isModule: true` and no options carries `False`, and compiles the module
  without the permission, as it did before unless the loader's scope or `EvalWithTopLevelAwaitAsync`'s was
  open.

### The for-loop's temporaries are numbered per parse (2026-10-20, milestone SJL-12.5)

- **Two parses of one source give the same tree.** The parser names the temporaries of its
  `for (let …)` and `for (const …)` desugaring by number, and the counter was one static for the
  process: the names, and so the tree `FastParser` returns, depended on everything the process had
  parsed before, on any thread. The counter is now the parser's own. Each parse numbers its
  temporaries from 2, as the first parse in a process did.
  - A temporary's name is never an identifier, so no source text names one. Code a direct or
    indirect `eval` compiles is parsed on its own, so its temporaries are numbered from 2 as well and
    take the same names as the calling code's, which under the one counter they never did. The names
    meet but the bindings do not: an eval's temporary is its own, never the caller's of the same
    name. `ForLoopTemporaryEvalTests` holds the shapes in which the calling code reads its temporary
    again after an eval ran (a `for (const …;;)` loop with no test and no update, a `let` loop's
    update, in a function, a script, a generator, an async function and a `with` body); each gives
    what it gave under the one counter.
  - This is item 2 of milestone SJL-12.5 (Broiler.VM's JSD-0080). The parser's architecture test,
    `Broiler.JavaScript.Architecture.Tests`, holds the case: two parses of a source with every loop shape
    the parser rewrites give equal tree dumps.

### Each scanner has its own end-of-file token (2026-10-20, milestone SJL-12.5)

- **A parse no longer shares its end-of-file token with every other parse.** `FastScanner`'s end-of-file
  and empty placeholder tokens were static: one `FastToken` of each for the process. The scanner writes
  the end-of-file token's `Previous` link as it reads, and a statement that ends at the end of its input
  reads that link back, so while another thread parsed, a statement could end on the other parse's
  token: its `End` was a token of another source, and its `Code` was measured against it. Each scanner
  now has its own two tokens, with the same type and empty text as before, and `AstNode.Code` still
  answers a node that ends at the end-of-file token with its first token's span.
  - A parse that runs alone is unchanged.
  - This is item 3 of milestone SJL-12.5 (Broiler.VM's JSD-0080). The parser's architecture test,
    `Broiler.JavaScript.Architecture.Tests` (part B of bundle `sjl-12-001`'s rule), holds the case: four
    threads parse one source 20,000 times each, and no parse holds another parse's token.

### The Temporal core moves to the shared library (2026-10-19, milestone SJL-10.5)

The Temporal core is `Broiler.ECMAScript.Algorithms`' `Broiler.ECMAScript.Temporal`, which `BuiltIns` takes by
project reference: the shared stream's `0.1.0-preview.6`, and decision D12, below, which the stream's
`0.1.0-preview.7` carries, published to nuget.org on 2026-10-19. *(Revised 2026-10-19, when `0.1.0-preview.7` was
published; read "which the stream's unreleased entry records.")* All seven sub-areas of the area verdict (Broiler.VM's
JSD-0078) move:

- **records and limits**: SJL-8's `IsoDate`, `IsoTime` and `IsoDateTime` with no parallel type (decision D8), and
  every limit check (`TemporalLimits`);
- **balancing**: regulation under both overflows, ISO addition, balancing and the rounding of points
  (`IsoArithmetic`);
- **durations and rounding**: sign, the default largest unit, addition, `compare`, `round` and `total`, with and
  without `relativeTo`, through the pinned text's nudges and bubbling over exact rationals (`DurationMath`,
  `RelativeTo`);
- **differences**: `until` and `since` of every type (`TemporalDifferences`);
- **the grammar**: every Temporal string, by a linear parser of the proposal's grammar (`TemporalGrammar`);
- **calendars**: identifiers, the formula calendars, the arithmetic over the table calendars' shared years, month
  codes, eras and the reference dates of the proposal's Table 6 (`TemporalCalendars`, over the calendar tables this
  engine embeds);
- **serialization**: every string form and the rules of its options (`TemporalFormat`, `TemporalOptionRules`).

A zone's day lengths and the zoned addition reach SJL-8's rules through the library's zone provider, one
`TemporalTimeZone` per canonical identifier.

**Where they were**, each deleted (the commits `f42eac6b` to `6dbff805` name every member):
- whole files: `TemporalIsoString`, the lenient ISO string reader; `TemporalNonIso`, the non-ISO calendars' fields,
  conversions, reference-date search and arithmetic; `TemporalArithmeticCalendar`, the formula calendars;
  `TemporalTableCalendar`, the arithmetic over the shared years; `TemporalCalendarMath`, their dispatch;
  `JSTemporalCalendar.cs`, the Gregorian family's identifiers, years and era tables; `JSTemporalDuration.Arithmetic.cs`,
  `add` and `subtract` with their "not yet implemented" `Error`, the arithmetic of `round` and `total` and the
  plain-date relative machinery; `JSTemporalZonedDateTime.Rounding.cs`, the zoned relative machinery; and
  `JSTemporalStubs.cs`, which held only comments;
- in each type: the limit, regulation, addition and balancing copies, `CompareISODate` and `FloorDiv`; the eight
  regular-expression parsers and `CanonicalizeTimeZone`'s string fallback; the formatters, the `toString` bodies'
  precision arithmetic and the five `ReadCalendarName` copies; the five month-code copies; the difference copies and
  the settings' rules; `compare`'s arithmetic; and the six copies of `GetTemporalOverflowOption` and two of
  `GetTemporalDisambiguationOption`;
- `JSTemporalRoundingOptions`' arithmetic: the unit order, the maximum increments and their validation.

**The built-ins keep** their `JSTemporal*` objects and slots, whose public members do not change (decision
SJL-D-25; [`docs/public-api.md`](../docs/public-api.md)), the reads of options and fields in specification order,
coercion, `relativeTo` resolution, their errors (each library status a `RangeError` or `TypeError` with this engine's
own message), `Temporal.Now`'s clock and host zone (decision SJL-D-19), and Intl's formatting of Temporal objects,
which is milestone SJL-9's. `Intl.DateTimeFormat`'s projection of a date into a calendar with its own months and eras
is the library's `CalendarISOToDate` too, and no formatted answer changed in any calendar (`format`,
`formatToParts`, `formatRange` and the Temporal types' `toLocaleString`, compared before and after). The reads are
one adapter, `TemporalAdapter` (`Broiler.JavaScript.BuiltIns/Temporal/`), by concern: records, grammar,
serialization, calendars and durations.

**Corrections the corpus names.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/temporal`, revision 25), under its reason in `scripts/conformance/temporal-divergences.txt`,
with the row of bundle `sjl-10-001`'s correspondence from code (Broiler.VM's
`docs/evidence/sjl-10-001/correspondence.md`). They are corrections under decision SJL-D-16 (a). Each of the 473
reports that it no longer holds in the engine-level runs on revision 25 after the cut-over, in UTC and in
`America/New_York`. Corpus revision 26 retires them, and
[`docs/evidence/sjl-10-001/engine/retired-divergences-revision-26.tsv`](../docs/evidence/sjl-10-001/engine/retired-divergences-revision-26.tsv)
lists every retired vector under its reason.

- *Records and limits, 19 vectors:*
  - **Integer narrowing inside addition is a `RangeError`** (R7, 14 vectors of `temporal/balancing`, reason
    `js-r7`): `Temporal.PlainDateTime.from("2020-06-15T12:00").add({ years: 4294967295 })` is a `RangeError`, where
    the year sum wrapped as a 32-bit integer to `2019-06-15T12:00:00`, and so in `PlainYearMonth` and in Hebrew and
    Coptic dates; and a time added to a date no longer saturates, where 2020-01-01 plus 9 × 10^24 nanoseconds was
    2312-04-11.
  - **A year-month's subtraction at the lower limit** (B5, 5 vectors of `temporal/balancing`, `js-b5`):
    `Temporal.PlainYearMonth.from("-271821-05").subtract({ months: 1 })` is a `RangeError`, where it was
    `-271821-04`: the addition starts from the first of the month, and -271821-04-01 is not a date.
- *Durations and rounding, 64 vectors:*
  - **`Duration.prototype.add` and `subtract` with calendar units** (D3, 9 vectors of `temporal/durations`,
    `js-d3`): `Temporal.Duration.from({ years: 1 }).add({ months: 1 }, { relativeTo: "2020-01-01" })` is a
    `RangeError`, where it was a plain `Error`, "not yet implemented". Neither reads a second argument:
    `Temporal.Duration.from({ hours: 1 }).add({ hours: 1 }, 5)` is `PT2H`, where it was a `TypeError`, and a
    `relativeTo` getter on it is not run.
  - **`compare`'s range** (D4, 3 vectors of `temporal/durations`, `js-d4`):
    `Temporal.Duration.compare({ years: 1, seconds: 9e15 }, { years: 1 }, { relativeTo: new Temporal.PlainDate(2020, 1, 1) })`
    is 1, where it was a `RangeError`: relative to a plain date only the years, months and weeks are added.
  - **Rounding to months keeps the nudge's end** (D6, 7 vectors of `temporal/rounding`, `js-d6`):
    `Temporal.Duration.from("P1Y2M3W4DT5H6M7.008009010S").round({ smallestUnit: "month", roundingMode: "ceil", relativeTo: new Temporal.PlainDate(2020, 1, 31) })`
    is `P1Y3M`, where a difference re-derived through the clamped 2021-04-30 gave `P1Y2M30D`.
  - **The same past the first window** (D7, 23 vectors of `temporal/rounding`, `js-d7`):
    `Temporal.Duration.from({ months: 10, days: 29 }).round({ smallestUnit: "month", largestUnit: "year", roundingMode: "ceil", relativeTo: new Temporal.PlainDate(2019, 3, 31) })`
    is `P11M`, where it was `P10M29D`.
  - **The nudge after a shifted window keeps the window's start** (D8, 7 vectors of `temporal/rounding`, `js-d8`):
    `{ months: 10, days: 29, hours: 12 }` rounded to months with largest unit year, `trunc`, relative to
    `Temporal.ZonedDateTime.from("2019-03-31T00:00[UTC]")`, is `P11M`, where it was `P1Y` (decision D7, below).
  - **A `relativeTo` in any calendar** (D11, 15 vectors of `temporal/rounding`, `temporal/durations` and
    `temporal/grammar`, `js-d11`): `Temporal.Duration.from("P1M").round({ smallestUnit: "day", relativeTo: "2020-01-01[u-ca=hebrew]" })`
    is `P1M` and `Temporal.Duration.compare({ years: 1 }, { days: 365 }, { relativeTo: "2020-01-01[u-ca=hebrew]" })`
    is -1, where `round`, `total` and `compare` threw a plain `Error`, "not yet implemented"; and 45 days balanced
    with a largest unit of `month`, relative to a Coptic `ZonedDateTime`, is `P1M15D` in Coptic months, where it was
    `P1M14D` in ISO months.
- *Differences, 31 vectors:*
  - **Rounding options apply in every calendar** (F4, 20 vectors of `temporal/differences`, `js-f4`):
    `Temporal.PlainDate.from("2020-01-01[u-ca=hebrew]").until(Temporal.PlainDate.from("2020-07-20[u-ca=hebrew]"), { smallestUnit: "month", roundingMode: "halfExpand" })`
    is `P7M`, where the options were ignored and it was `P6M24D`; so for `PlainYearMonth` and `ZonedDateTime`, and
    a `PlainDateTime` rounds in its calendar's months, where it rounded in ISO months.
  - **A `ZonedDateTime` difference rounded to days measures the zone's days** (F7, 11 vectors of
    `temporal/differences`, `js-f7`): in `America/New_York`, 2026-03-07T12:00 until 23:45 the same day, to days
    with `halfExpand`, is `P1D` (the day to 2026-03-08T12:00 has 23 hours), where it was `PT0S`; and 2026-10-31T12:00
    until 2026-11-01T00:15 is `PT0S` (the day to 2026-11-01T12:00 has 25), where it was `P1D`. `round` and `hoursInDay` already measured the
    zone's days.
- *The grammar, 213 vectors of `temporal/grammar`, each a string this engine read otherwise than the grammar does.
  205 are now a `RangeError`: 164 of them it accepted, and in 41 (G9) a .NET `FormatException` escaped. The other 8
  (G17) now take the first of several `u-ca` annotations; 2 of these were a `RangeError` before:*
  - **a year-month's day must be a date** (G3, 7, `js-g3`): `Temporal.PlainYearMonth.from("2020-02-30")`, where it
    was `2020-02`;
  - **a time string's date must be a date** (G4, 6, `js-g4`): `Temporal.PlainTime.from("2021-13-45T12:00")`, where
    it was `12:00:00`;
  - **one separator style per time** (G6, 2, `js-g6`): `Temporal.Instant.from("2020-01-01T12:3045Z")`, where it was
    12:30:45;
  - **offsets in range in the plain types** (G8, 12, `js-g8`): `Temporal.PlainDate.from("2020-01-01T12:00+24:00")`,
    and `+23:60` and `+23:59:60`, where the offset was not checked;
  - **ASCII digits only** (G9, 45, `js-g9`): `Temporal.PlainDate.from("２０２０-01-01")` (U+FF12, U+FF10), where a .NET
    `FormatException` escaped the evaluation in 41 of the 45, and in the four plain types' offsets such digits were
    accepted and the offset ignored;
  - **no empty annotation** (G11, 13, `js-g11`): `Temporal.PlainDate.from("2020-01-01[]")`, and `[!]`;
  - **time zone annotations as the grammar has them** (G12, 48, `js-g12`): `[Mars Olympus]`, `[ UTC]`, `[UTC ]`,
    `[/a]`, `[a/]`, `[a//b]`, `[9a]` and `[+24:00]` after a date-time;
  - **the time zone annotation first** (G13, 15, `js-g13`): `Temporal.PlainDate.from("2020-01-01T12:00[u-ca=iso8601][UTC]")`;
  - **no time on a year-month** (G16, 4, `js-g16`): `Temporal.PlainYearMonth.from("2020-01T12:00")`, where it was
    `2020-01`;
  - **calendar strings** (G17, 32, `js-g17`): `new Temporal.PlainDate(2020, 1, 1).withCalendar("T10:00Z")`, a
    year-month or month-day with a calendar, an empty, unknown critical or misplaced annotation, or an offset out of
    range, where each read as a calendar; and of several `u-ca` annotations the first rules, where the last did;
  - **time zone strings** (G18, 16, `js-g18`): `Temporal.Instant.from("2020-01-01T00:00Z").toZonedDateTimeISO("T10:00Z")`,
    and `10:00Z` and `1214Z`, where each was `UTC`;
  - **`Z` is not an offset string** (G19, 4, `js-g19`):
    `Temporal.ZonedDateTime.from("2020-01-01T00:00[UTC]").with({ offset: "Z" }, { offset: "ignore" })`, and `z`, and
    in a property bag;
  - **`relativeTo` strings** (G20, 6, `js-g20`): `"2020-01-01[u-ca=iso8601][UTC]"` and an offset of `+25:00` as
    `round`'s or `total`'s `relativeTo`, where they answered `P1M` and 31;
  - **a time designator needs a time part** (G22, 3, `js-g22`): `Temporal.Duration.from("P1YT")`, `"P1DT"` and
    `"P1Dt"`, where they were `P1Y` and `P1D`.
- *Calendars, 146 vectors:*
  - **The aliases `gregorian` and `minguo` are refused** (C2, 12 vectors of `temporal/calendars` and
    `temporal/grammar`, `js-c2`; decision D5): `Temporal.PlainDate.from("2020-01-01[u-ca=gregorian]")`,
    `[u-ca=minguo]` and `new Temporal.PlainDate(2020, 1, 1, "GREGORIAN")` are `RangeError`s, where they read
    `gregory` and `roc`. Table 1's aliases are `ethiopic-amete-alem` and `islamicc`.
  - **An invalid month code is not constrained** (C5, 29 vectors of `temporal/calendars`, `js-c5`):
    `Temporal.PlainDate.from({ calendar: "coptic", year: 1739, monthCode: "M00", day: 1 }, { overflow: "constrain" })`
    is a `RangeError`, where it was the first month; `M13` where the calendar has no thirteenth month, `M14` and `M99`
    likewise, where they were the last, in `coptic`, the Ethiopian, `indian`, the Islamic and `persian` calendars.
  - **A month code's digits are ASCII** (C6, 23 vectors of `temporal/calendars`, `js-c6`):
    `Temporal.PlainDate.from({ calendar: "hebrew", year: 5784, monthCode: "M٠١", day: 1 })` (U+0660, U+0661)
    is a `RangeError`, where a .NET `FormatException` escaped the evaluation.
  - **Era codes are the pinned revision's** (C9, 24 vectors of `temporal/calendars`, `js-c9`; decision D4):
    `Temporal.PlainDate.from({ calendar: "coptic", era: "coptic", eraYear: 5, monthCode: "M01", day: 1 })` and the
    era `"AM"` are `RangeError`s, where both read `am` 5; so for the codes of earlier drafts and the aliases the era
    proposal removed (`gregory`, `gregory-inverse`, `hebrew`, `ethiopic`, `ethioaa`, `saka`, `persian`, `mundi`,
    `minguo`, `before-roc`), `incarnation`, and a code's ASCII case variants. Table 2's codes and aliases stand: the
    `indian` calendar's era is `shaka`.
  - **An ISO month-day's year past 2^31 keeps its leap year** (C12, 11 vectors of `temporal/calendars`, `js-c12`):
    `Temporal.PlainMonthDay.from({ year: 2147483652, month: 2, day: 29 }, { overflow: "reject" })` is `02-29`, where
    the year saturated to 2147483647 and it was a `RangeError` (and `02-28` under `constrain`).
  - **A `PlainMonthDay` lends its own calendar** (C13, 3 vectors of `temporal/calendars`, `js-c13`): a Hebrew
    month-day as a `calendar` value gives `hebrew`, where it gave `iso8601`.
  - **`toPlainYearMonth`'s reference day is the calendar month's first** (C15, 30 vectors of `temporal/calendars`,
    `js-c15`): `Temporal.PlainDate.from("2023-03-15").withCalendar("buddhist").toPlainYearMonth()` is
    `2023-03-01[u-ca=buddhist]`, and in `chinese` `2023-02-20`, where each kept 2023-03-15.
  - **`toPlainMonthDay` keeps a Gregorian-family calendar** (C16, 12 vectors of `temporal/calendars`, `js-c16`):
    in `gregory`, `buddhist`, `roc` and `japanese`, where the month-day and its `with` were `iso8601`.
  - **`PlainMonthDay.prototype.toPlainDate` reads a `buddhist` or `roc` year as the calendar's** (C17, 2 vectors of
    `temporal/calendars`, `js-c17`): Buddhist 2544's 10 February is 2001-02-10, where it was 2544-02-10, and ROC 90's
    likewise, where it was 0090-02-10.

**Corrections the reference datasets name.** Broiler.VM's `temporal.js` and `temporal-calendars.js`, run in this
engine against Broiler.VM's answers by bundle `sjl-10-001`'s rule, part C
([`conformance/datasets/time/`](../conformance/datasets/time/README.md)): at the base 7 and 103 of their lines
differed, and after the cut-over every line of both equals the expected answer. No line moved the other way.
- `temporal.js`, 7 lines, row D6: `P1Y2M3W4DT5H6M7.008009010S` rounded to months relative to 2020-01-31, in
  `ceil`, `expand`, `halfCeil`, `halfFloor`, `halfExpand`, `halfTrunc` and `halfEven`, is `P1Y3M`, where it was
  `P1Y2M30D`.
- `temporal-calendars.js`, 103 lines: `M13` in `indian`, `islamic-civil`, `islamic-tbla`, `islamic-umalqura` and
  `persian` is a `RangeError`, where it was constrained (C5, 5); `toPlainYearMonth`'s reference day (C15, 15);
  `toPlainMonthDay` in the Gregorian family (C16, 4); `toPlainDate`'s year in `buddhist` and `roc` (C17, 2); `until`
  rounded to months, which honours the rounding in eleven calendars (F4, 55); and `Duration` `round` and `total`
  relative to a date in those calendars, which answer where they threw a plain `Error` (D11, 22).

**Corrections the correspondence did not find.** Each follows the pinned text, and is the shared library's answer and
so Broiler.VM's; undoing one would need engine code around the library. They are kept under the owner's direction of
2026-10-18, as corrections under decision SJL-D-16 (a). The cut-over's parts, its review and these records found
them, and no vector, dataset line or test262 path names any.
[`docs/evidence/sjl-10-001/cutover/kept-changes.js`](../docs/evidence/sjl-10-001/cutover/kept-changes.js) probes
each one, with the base's answer (`1c411b68`) and this change's (`6dbff805`). On every line that changed, this
change's answer is Broiler.VM's and the polyfill's at `e8cc03fc`.
- *Property bags and options, read as Table 19 and the specification read them:*
  - **A `relativeTo` property bag is read in one pass, each field converted at its read** (part C). An unknown
    calendar is refused before any field is read: `{ calendar: "gregorian", year: 2020, month: 1, day: 1 }` as
    `round`'s `relativeTo` is a `RangeError` after `calendar` alone, where every field was read and the answer was
    `P1M`. `era` and `eraYear` are read only in a calendar with eras. The offset is parsed at its read:
    `{ year: 2020, month: 1, day: 1, offset: "garbage" }` with no `timeZone` is a `RangeError` in `round`, `total`
    and `compare`, where it was ignored (`P1M`, 31, 0), and `offset: "+25:00"` beside `timeZone: 5` is that
    `RangeError`, where the zone's `TypeError` came first (part A). The time zone is converted before `year`:
    `{ year: Infinity, month: 1, day: 1, timeZone: 5 }` is a `TypeError`, where it was a `RangeError`. `monthCode:
    "X1"` and `day: 0` beside `timeZone: 5` are `RangeError`s at their reads, where the zone's `TypeError` came first.
  - **A property bag in a calendar other than ISO 8601 and the Gregorian family is read in the specification's
    order** (part B): `Temporal.PlainDate.from({ calendar: "hebrew", year: 5780, month: 5, day: 1 }, options)` reads
    `calendar`, `day`, `month`, `year`, then the options' `overflow`, where it read `calendar`, then `overflow`, then
    `day`, `year`, `month` and `day` again. `ZonedDateTime.prototype.with` of a Hebrew receiver, given
    `{ day, hour, offset }` and options, reads the bag's `day`, `hour` and `offset`, then the options'
    `disambiguation`, `offset` and `overflow`, where it read the bag's `offset`, the options' `disambiguation` and
    `offset`, then `day`, `overflow`, `hour` and `day` twice more. The Gregorian family's `with` and
    `PlainMonthDay.prototype.toPlainDate` read `era` and `eraYear` before `year`, where they read `year` first.
  - **A missing field's `TypeError` follows the `overflow` option's read** (part B):
    `Temporal.PlainDate.from({ year: 2020, month: 1 }, { overflow: "bad" })` is the option's `RangeError`, where the
    missing day's `TypeError` came first.
  - **A field is converted at its read** (part B): `Temporal.PlainYearMonth.from("2020-01").toPlainDate({ day: 0 })`
    is a `RangeError`, where the day was clamped to 1; and `PlainMonthDay.from`'s ISO month 0 is a `RangeError`
    before `monthCode`, `year` and the options are read.
  - **An `era` is converted by ToString at its read** (the review's U4): an era object whose `toString` answers
    `String("ce")` is `"ce"` in `PlainMonthDay.from`, `PlainMonthDay.prototype.toPlainDate` and a `relativeTo` bag,
    where its text was taken as `"[object Object]"`, a `RangeError` in `gregory` and `japanese`, and in `coptic`'s
    `toPlainDate` and `relativeTo` bag, where such an era `"am"` with era year 1740 now gives
    `2023-09-12[u-ca=coptic]` and a `total` of 30 days; and
    `Temporal.PlainMonthDay.from({ calendar: "gregory", monthCode: "M02", day: 29 }).toPlainDate({ era: "ce", eraYear: 2021 })`
    is `2021-02-28`, where every era of `gregory` and `japanese` was a `RangeError` there.
  - **`PlainMonthDay.prototype.with` reads `era` and `eraYear` in a calendar with eras** (U6):
    `.with({ era: "ce", eraYear: 5 })` of a Gregorian month-day is `1972-02-01[u-ca=gregory]`, where it was the
    `TypeError` for a bag with no field. `PlainMonthDay.from` in the Gregorian family reads its options before it
    validates the era: `PlainMonthDay.from({ calendar: "gregory", era: "xx", eraYear: 1, monthCode: "M01", day: 1 }, 5)`
    is the options' `TypeError`, where it was the era's `RangeError`.
  - **`ZonedDateTime.prototype.toString` converts its `offset` option once** (part A): an option whose `toString`
    answers `"bad"` is converted once before the `RangeError`, where it was converted twice.
- *Calendars:*
  - **A month-day's `with` regulates against 1972** (part B): `new Temporal.PlainMonthDay(2, 1, "iso8601", 1900).with({ day: 30 })`
    is `02-29`, where it was `02-28`.
  - **`PlainMonthDay.from` in `buddhist` and `roc` reads a year as the calendar's** (part B, as C17 does):
    `Temporal.PlainMonthDay.from({ calendar: "roc", year: 109, month: 2, day: 29 }, { overflow: "reject" })` is
    `1972-02-29[u-ca=roc]`, where it was a `RangeError`, ISO 109 not being a leap year; Buddhist 2563 likewise.
  - **A non-ISO year-month whose month begins before -271821-04-19 is out of range** (part B, row B5's rule):
    `new Temporal.PlainDate(-271821, 4, 19, "coptic").toPlainYearMonth()` is a `RangeError`, where it was
    `-271821-04-19[u-ca=coptic]`; and so are `add`, `subtract`, `until` and `since` of the year-month the constructor
    gives with that reference day, where `add("P1M")` was `-271821-04-27[u-ca=coptic]` and `until` a later month
    `P2M`, as ISO 8601's already were.
  - **An era and a year that disagree are a `RangeError`** (part B, the review's U7):
    `Temporal.PlainDate.from({ calendar: "gregory", year: 2020, month: 1, day: 1 }).with({ year: 2021, era: "ce", eraYear: 2019 })`
    is a `RangeError`, where the year was taken; and in `PlainYearMonth.prototype.with` and
    `PlainMonthDay.prototype.with` of `hebrew`, `coptic`, the Ethiopian, the Islamic, `indian` and `persian`
    calendars an unknown era too: `PlainYearMonth.from({ calendar: "hebrew", year: 5780, monthCode: "M03" }).with({ year: 5781, era: "xx", eraYear: 1 })`
    is a `RangeError`, where it was `2020-11-17[u-ca=hebrew]`.
  - **A non-ISO month-day given a month needs a year** (U5):
    `Temporal.PlainMonthDay.from({ calendar: "gregory", monthCode: "M05", month: 5, day: 1 })` is a `TypeError`,
    where it was `1972-05-01[u-ca=gregory]`; and `with({ monthCode, month })` in every non-ISO calendar, where a
    Hebrew `M05`-01 `.with({ monthCode: "M06", month: 6 })` was `1972-02-16[u-ca=hebrew]`.
  - **`PlainYearMonth.prototype.with` constrains a leap month the year lacks** (U8): a `chinese` 2021 `M03`
    `.with({ monthCode: "M02L" })` is `M02`, `2021-03-13[u-ca=chinese]`, and a `hebrew` 5783 `M04` with `M05L` is
    `M06`, where both were `RangeError`s under `constrain`, as `from` and `PlainDate.prototype.with` already
    constrained. Under `reject` both are still `RangeError`s.
  - **A Temporal object read as a property bag lends its own calendar** (the review's code-1, a defect at the
    base): `Temporal.PlainDateTime.from(Temporal.ZonedDateTime.from("2020-01-15T10:00+00:00[UTC][u-ca=hebrew]"))` is
    `2020-01-15T10:00:00[u-ca=hebrew]`, where its getters were read as ISO fields and it was `5780-04-18T10:00:00`;
    `PlainYearMonth.from` and `PlainMonthDay.from` of a Hebrew `PlainDate` are `2019-12-29[u-ca=hebrew]` and
    `1972-12-23[u-ca=hebrew]`, where they were `5780-04` and `04-18`; a Hebrew `PlainDateTime` equals the Hebrew
    `ZonedDateTime` of its wall clock, where it did not; and `PlainDateTime.from` of a `ZonedDateTime` reads its
    slots and calls no getter.
  - **`ZonedDateTime` reads a `PlainDate` or `PlainDateTime` as a property bag** (the review's code-2, a defect at
    the base): `Temporal.ZonedDateTime.from(Temporal.PlainDate.from("2020-01-01"))` is the `TypeError` for the
    missing `timeZone`, at its read, where a .NET `ArgumentNullException` reached script as a plain `Error`; so for
    `ZonedDateTime.compare`, `until`, `since` and `equals` with one, and one that carries its own `timeZone` property
    converts (`2020-01-01T00:00:00+00:00[UTC]`).
- *Durations, differences and points:*
  - **A year-month difference is zero only between equal dates** (part C):
    `new Temporal.PlainYearMonth(-271821, 4, "iso8601", 19).until(new Temporal.PlainYearMonth(-271821, 4, "iso8601", 20))`
    is a `RangeError`, where it was `PT0S`: the month's first day, -271821-04-01, is not a date. At the upper limit,
    `+275760-09` with reference days 1 and 13 rounded to years, likewise.
  - **A nudge or bubble window past the date limits is a `RangeError`** (the review's U1 to U3), where this engine's
    own arithmetic never formed the window's end:
    `Temporal.Duration.from("P1W").round({ smallestUnit: "day", relativeTo: "+275760-09-01" })`, where it was `P1W`;
    `Temporal.Duration.from("PT100H").round({ smallestUnit: "week", roundingIncrement: 1e9, relativeTo: "2020-01-01" })`,
    where it was `PT0S`; `P1W.total({ unit: "week", relativeTo: "+275760-09-01" })`, where it was 1;
    `Temporal.PlainDateTime.from("-271821-04-19T00:00:00.000000001").until("+275760-09-13T00:00", { largestUnit: "year", smallestUnit: "hour", roundingMode: "ceil" })`,
    where it was `P547581Y4M25D`; and a `ZonedDateTime` difference to days within a day of either instant limit,
    `+275760-09-12T00:00[UTC]` until `+275760-09-13T00:00[UTC]`, where it was `P1D`.
  - **A rounded difference's days bubble into months** (U2), away from the limits too:
    `Temporal.PlainDateTime.from("2020-01-31T00:00:00.000000001").until("2020-02-29T00:00", { largestUnit: "month", smallestUnit: "hour", roundingMode: "ceil" })`
    is `P1M`, where it was `P29D`.
  - **A wall clock on -271821-04-19 with an offset is a `RangeError` under `reject` and `prefer`**
    (InterpretISODateTimeOffset's range check, step 7; the review's fixes and part C):
    `Temporal.ZonedDateTime.from({ year: -271821, month: 4, day: 19, hour: 12, offset: "-12:00", timeZone: "-12:00" })`
    is a `RangeError`, where it was `-271821-04-19T12:00:00-12:00[-12:00]`; so for `with({ minute: 1 })` and
    `round("hour")` of the earliest instant in `-12:00`, and for a `relativeTo` bag there, whose `total` in hours was
    24. Without an offset, each answers as before.
  - **The rounding of points rounds within the next larger unit** (found by these records): RoundTime rounds a
    minute within its hour and a second within its minute, where this engine rounded the nanoseconds since midnight.
    Only a tie under `halfEven` can differ:
    `Temporal.PlainTime.from("01:10").round({ smallestUnit: "minute", roundingIncrement: 20, roundingMode: "halfEven" })`
    is `01:00:00`, where it was `01:20:00`; so in `PlainDateTime` and `ZonedDateTime`, and 00:01:10 rounded to 20
    seconds is `00:01:00`, where it was `00:01:20`. Milestone SJL-4.5's "Not corrected here", below, named it for
    SJL-10.

  Part C's fifth, that `add` and `subtract` read no second argument, is row D3's, above.

**Code no input reaches, deleted** (rows G10, C3, C10, S8 and D12): the handling of U+2212 after a
match (G10); the `islamic` entry (C3) and the reference-date search's upper year (C10), the reference years now being
Table 6's, with no answer changed; the offset getter's dropped fraction of a second (S8); the zoned machinery's shift
of years and months and its fourth day correction (D12); and `RejectIncompatibleStyle`, `NotImplementedArithmetic`,
`ReadLargestUnit` and the unused `AddYearsMonths`. Its "inconsistent Instants" branch, which the correspondence also
read as unreached, is reached by a zone's skipped day (decision D12, below).

**Policy, by decision.** Each is the owner's, taken under the direction of 2026-10-18, "Please do all recommended
decisions and skip waiting for review." (Broiler.VM's JSD-0078).
- **The proposal revision is `tc39/proposal-temporal@e8cc03fc`** (decision D1), the Stage 4 state of 2026-07-27,
  which Broiler.VM follows; until now this engine pinned none. No answer moves to it but the corrections above.
- **Where the pinned text and its polyfill disagree, the text rules** (decision D7): row D8's nudge is `P11M`, where
  the polyfill answers `P1Y` (rows D7 and D8, above). The 45 vectors where the polyfill's own parser and era table
  read the texts otherwise take the texts' answer too, and this engine matches each.
- **The era codes of the pinned revision** (decision D4), row C9 above, **and the calendar aliases** (decision D5),
  row C2 above.
- **A week or day nudge window with no length is a `RangeError`** (decision D11). Pacific/Apia skipped 2011-12-30.
  With `a` 2011-12-31T16:15+14:00[Pacific/Apia] and `b` 2011-12-31T00:00+14:00[Pacific/Apia], the day window back
  from `a` ends on the skipped day and has no length. These moved to a `RangeError` (message "result is outside the
  representable range"):
  - `a.since(b)` and `a.until(b)` with `{ largestUnit: "day", smallestUnit: "day" }`, in each of the nine rounding
    modes: they were `PT0S` or one day by mode, `since` `PT0S` under `trunc` and `floor` and `P1D` in the other seven,
    and `until` `PT0S` under `trunc` and `ceil` and `-P1D` in the other seven;
  - `a.since(b)` with `{ largestUnit: "month", smallestUnit: "day" }`, in each mode, where it was `PT0S` under
    `trunc` and `floor` and `P1D` in the other seven;
  - `Temporal.Duration.from("-PT16H15M").round({ smallestUnit: "day", relativeTo: a })`, in each mode, and
    `.total({ unit: "day", relativeTo: a })`, which threw a plain `Error` carrying a .NET
    `ArgumentOutOfRangeException`.

  [`docs/evidence/sjl-10-001/cutover/d12-ext.js`](../docs/evidence/sjl-10-001/cutover/d12-ext.js) probes `trunc`,
  `floor`, `ceil`, `expand` and `halfExpand`, and
  [`d12-half-modes.js`](../docs/evidence/sjl-10-001/cutover/d12-half-modes.js) the other four. Nothing else in them
  moved to a `RangeError`: `b.until(a)` and `b.since(a)` in days, `a.since(b)` and rounding to weeks, `a.since(b)`
  in hours, anything relative to `b`, `compare`, `a.round("day")`, `hoursInDay` and `a.subtract("P1D")` answer as
  before. The lines of decision D12, below, were a `RangeError` before and change only its message.
- **NudgeToZonedTime's day span is a `RangeError` where its sign is not the duration's** (decision D12, the owner's
  of 2026-10-19, under the same direction): rounding to a time unit with a date largest unit back from `a`, as in
  `a.since(b, { largestUnit: "day", smallestUnit: "hour" })` in each of the nine modes, with a largest month, to 30
  minutes or to 2 nanoseconds, and `-PT16H15M` rounded so relative to `a`. This engine answered a `RangeError` there
  before the cut-over, so only its message moves, from "time zone returned inconsistent Instants" to "result is
  outside the representable range". `0.1.0-preview.6` of the shared stream, which Broiler.VM takes, answers durations
  from outside the text's domain there (`P1DT16H` and the like), and this engine's packages need the stream's
  `0.1.0-preview.7`, published to nuget.org on 2026-10-19, to answer as this engine does. *(Revised 2026-10-19, when
  `0.1.0-preview.7` was published; read "need the stream's next version to answer as this engine does.")*

**test262.** No path of the selection this change was checked against changed its verdict, in UTC or in
`America/New_York`: `built-ins/Temporal`, `intl402/Temporal`, `staging/Temporal`, `staging/Intl402/Temporal`,
`intl402/DateTimeFormat`, `intl402/DurationFormat` and `built-ins/Date` at `ccaac100`, negative tests included,
against the base at `1c411b68`. So no test262 path pins any correction above.

**Messages.** Each engine keeps its own messages (JSD-0078's decision, item 3), and many of this area's change with
the shared code: a non-object options argument reads "Temporal: options must be an object or undefined", where it
read "Temporal options must be …", and a `relativeTo` bag's missing field names `Temporal.Duration`, among others.
The correspondence's probes and the review's found no other change of an error's type.

**Not corrected here.** A string whose date is outside the limits is refused before the `overflow` option is read:
`Temporal.PlainDate.from("-271821-04-18", options)` and `Temporal.PlainYearMonth.from("-271821-03", options)` throw
the `RangeError` without reading `options.overflow`, where the specification reads it first, as Broiler.VM and the
polyfill do. The cut-over keeps each type's reads as they were.

Tests: `SharedTemporalDurationsTests` (D3, D4, D6, D7, D8, D11, F4, F7 and decision D11) and
`SharedTemporalAdapterTests` (code-1, code-2 and the offset's range check) in `Broiler.JavaScript.Integration.Tests`;
`Issue781Tests.IndianEraRefusesSakaSpelling` and `IndianEraAcceptsShaka`, which replace `IndianEraAcceptsSakaAlias`,
and `Issue777Tests.IndianFromEra`, which now expects `saka`'s `RangeError` (row C9); the library's
`DurationApiTests` hold decision D12.

### An offset's GMT name keeps its seconds (2026-10-18, milestone SJL-8.6)

`Intl.DateTimeFormat`'s `timeZoneName` `longOffset` and `shortOffset`, and the GMT name a zone with no CLDR name
is written with, keep an offset's seconds after its minutes, as CLDR's localized GMT format does in ICU and as
Broiler.VM's formatter writes it (correspondence row Z16). A local mean time is the offset that has them: in
`Africa/Abidjan` before 1912 the `longOffset` name is `GMT-00:16:08` and the `shortOffset` name `GMT-0:16:08`,
where they were `GMT-00:16` and `GMT-0:16`; in `America/New_York` before 1883 they are `GMT-04:56:02` and
`GMT-4:56:02`, where they were `GMT-04:56` and `GMT-4:56`. An offset of whole minutes is written as before.

**Brought into SJL-8.6 by the owner's direction of 2026-10-18.** The correspondence and the area verdict left
row Z16 to milestone SJL-9, zone display. Part C of bundle `sjl-8-001`'s rule admits a line whose difference is
only Z16 (D2) or only the resolved identifier (Z8, D1). At `5f96cd40`, 128 lines of `timezones.js` differed by
both, so part C was not met as written there. The owner chose to make the correction here rather than revise
the rule: with it, those 128 lines differ by Z8 alone.

Tests: `Issue786Tests.DateTimeFormat_RendersTimeZoneName` gains both offset styles for both zones' local mean
time, and an offset of whole minutes that is not whole hours.

### The time-zone and calendar area moves to the data home's tables (2026-10-17, milestone SJL-8.6)

`Temporal`, `Intl` and `Date`'s local time read one time zone dataset and one set of calendar tables: the
time data home's, [`Broiler.ECMAScript/data/time/`](../Broiler.ECMAScript/data/time/README.md), which
`Broiler.ECMAScript.Generation` writes from IANA tzdb 2026e, CLDR 48.2.0's `bcp47/timezone.json` and ICU4X's
`icu_calendar` 2.3.0 and `calendrical_calculations` 0.2.4. `Broiler.JavaScript.BuiltIns` embeds the generated
tables, linked from the home and never copied, and hands them to `Broiler.ECMAScript.Algorithms`' members under
`Broiler.ECMAScript.Time` (Broiler.VM's JSD-0076; decisions SJL-D-6 (c), SJL-D-20 and SJL-D-21):

- identifier lookup, primary identifiers and a region's zones (`TimeZoneDatabase`);
- offset zones (`OffsetTimeZones`);
- a zone's offset at an instant, the possible instants of a wall-clock time, their disambiguation, the start
  of a day and the next and previous transitions (`ZonedTime`);
- the years of `chinese`, `dangi`, `islamic-umalqura` and `persian` (`CalendarTables`, `CalendarYears`).

**Where they were**, each deleted: the embedded `tzdata.bin`, a DEFLATE pack of a build host's compiled
zoneinfo (tzdb 2025b), with its `EmbeddedResource` item and its generator `scripts/tzdata/generate_tzdata.py`;
`IanaTimeZone`, `IanaTimeZoneDatabase` and `TzifReader`; `JSTemporalZonedDateTime`'s offset, candidate,
disambiguation, start-of-day and transition helpers; `JSIntl`'s offset-zone normalisation and its region
table; the formatting engine's offset parser and zone fallback; `TemporalLunisolarCalendar` and
`TemporalSolarHijriCalendar`, over .NET's `ChineseLunisolarCalendar`, `KoreanLunisolarCalendar` and
`PersianCalendar`; the Umm al-Qura and .NET dispatch of `TemporalArithmeticCalendar` and
`TemporalCalendarMath`; and this engine's uses of the CLDR package's `CldrChineseCalendar` and `CldrUmmAlQura`.
**This is Broiler.JS's record of the `tzdata.bin` retirement** (milestone SJL-8's records).

The built-ins keep their argument reading and its order, the parsing of ISO strings and their annotations,
their errors, the formatter's resolved `timeZone`, zone display names, and the arithmetic over calendars with
the formula calendars (`coptic`, `ethiopic`, `ethioaa`, `islamic`, `islamic-civil`, `islamic-tbla`, `hebrew`,
`indian`), which are milestone SJL-10's.

**Corrections the corpus names.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/time-zones`, revision 22), under its reason in
`scripts/conformance/time-zone-divergences.txt`, with the row of bundle `sjl-8-001`'s correspondence from code
(Broiler.VM's `docs/evidence/sjl-8-001/correspondence.md`). They are corrections under decision SJL-D-16 (a).
Every one of the area's 1,161 fields, these and the policy changes' below, reports that it no longer holds in the
engine-level run on revision 22 after the cut-over. Corpus revision 23 retires the 1,161 fields, and
[`docs/evidence/sjl-8-001/engine/retired-divergences-revision-23.tsv`](../docs/evidence/sjl-8-001/engine/retired-divergences-revision-23.tsv)
lists every retired vector under its reason.

- **A name matches by its ASCII case only** (Z10, 13 vectors of `time-zones/fold` and `time-zones/iso-string`,
  reason `js-z10-lookup`): `America/Loſ_Angeles` (U+017F) is a `RangeError` in the `ZonedDateTime` constructor,
  `Intl.DateTimeFormat` and `toZonedDateTimeISO`, where .NET's invariant upper-casing read it as
  `America/Los_Angeles`. `eUrOpE/bErLiN` still matches.
- **An ISO date-time string is not the formatter's `timeZone`** (Z11, 4 vectors of `time-zones/iso-string`,
  `js-z11-formatter-iso`): `new Intl.DateTimeFormat('en', { timeZone: '2020-01-01T00:00[Europe/Berlin]' })`
  and `{ timeZone: '2020-01-01T00:00Z' }` are `RangeError`s, where they read `Europe/Berlin` and `UTC`.
  Temporal's `ToTemporalTimeZoneIdentifier` still reads a zone out of an ISO string.
- **`Intl.Locale.prototype.getTimeZones` answers the region's zones** (Z12, 10 vectors of `time-zones/region`,
  `js-z12-regions`): `zone.tab`'s zones in code-unit order, 29 for `US`, `Europe/Berlin,Europe/Busingen` for
  `DE`, `Europe/Kyiv,Europe/Simferopol` for `UA`, and none for `ZZ`, where a hand table of fourteen regions
  answered one zone each and `UTC` for every other region.
- **An offset with non-ASCII digits is a `RangeError`** (A13, 21 vectors of `time-zones/offset-digits`,
  `js-a13-digits`): `+٠٥:٣٠` (U+0660 to U+0669) as the `ZonedDateTime` constructor's zone or the formatter's
  `timeZone`, where .NET's `\d` matched it and `int.Parse` threw a `FormatException` that surfaced as an
  `Error`. The `offset` of a `ZonedDateTime` property bag, of `with` and of a `relativeTo` bag is read by the
  same grammar, ECMA-262's `UTCOffset`, so it too is a `RangeError` there, as is an hour above 23.

**Corrections with no vector**, under the same decision, held by `SharedTimeZoneTests`,
`SharedTimeZoneHostTests` and `SharedCalendarTableTests`:

- **The host's zone is validated** (A16): `Temporal.Now.timeZoneId()`, the default formatter's `timeZone` and
  `Date`'s local time read the host's IANA name resolved through the shared database to its primary
  identifier, and **`UTC` where the database does not hold it**. Under `TZ=posixrules`, `Temporal.Now`
  answered `posixrules` while `Intl.DateTimeFormat().resolvedOptions()` and `Temporal.Now.zonedDateTimeISO()`
  threw a `RangeError`; under `TZ=Factory` it answered `Factory`. Both now read `UTC`, and so does `Date`, which
  read the host's data for that name. A host name reads as its CLDR primary: `TZ=Europe/Bratislava` reads
  `Europe/Bratislava`, where it read tzdb's link target `Europe/Prague` (Z7). The specification's
  SystemTimeZoneIdentifier returns an offset or a primary identifier; this engine's choice of `UTC` for a
  zone it cannot name is recorded here.
- **`chinese` 2057** (K2): its ninth month begins on 2057-09-28, as the Hong Kong Observatory's table and
  ICU4X have it, where .NET's table began it a day later; `{ year: 2057, monthCode: 'M09', day: 1 }` is
  2057-09-28. It is the one year inside Table 1's published range in which the sources differed.
- **Far lunisolar dates are valid dates** (K7): `chinese` and `dangi` dates out to the limits are dates of 29-
  and 30-day months, `-271821-04-19` being `M03` 17, where the CLDR package's computation threw a .NET
  `IndexOutOfRangeException` at ±50000, −150000, −250000 and +100000 to +250000, an exception that ended the
  shell's process when nothing caught it, and gave months of 22 to 42 days elsewhere.
- **Each ISO date is one `persian` date** (K12): 0622-03-21 is 1/`M01`/1 and 0622-03-22 is 1/`M01`/2, and
  +010000-01-01 is 9378/`M10`/11, where 0622-03-21 and 0622-03-22 were both 1/`M01`/1 and the first three days
  of +010000 repeated the last three of 9999, at the seams between .NET's calendar and the 33-year rule.

**Code no input reached, deleted** (A6, A11): the zero offset an unknown zone name read, the formatter's
fallback to the host's zone for an unknown name, and start of day's fallback to local midnight when no
transition follows a skipped midnight. Every entry point validates a name before reading it, and no gap in
the data lacks a following transition; a day with no start is now a `RangeError`, never midnight.

**Data policy by decision.** The shared dataset replaces this engine's, under decisions SJL-D-20 and SJL-D-21;
the bundle's rule counts each as class C5.

- **The release is tzdb 2026e** (Z1, Z2, Z15, 309 vectors of `time-zones/release`, `js-z1-z2-release`), where
  `tzdata.bin` was 2025b, copied from a build host and not reproducible: Baja California 1953 and 1961 to 1975
  (2025c), Moldova (2026a), British Columbia's permanent −07:00 (2026b), Alberta's permanent −06:00 and
  Morocco's permanent +00:00 (2026c), the Northwest Territories, Colombia 1992 and Iran 1979 (2026d), and
  Manitoba and Ireland 1925 (2026e). America/Vancouver at 2027-01-15 is −07:00, where it was −08:00.
- **No backzone** (Z3, 463 vectors of `time-zones/backzone`, `js-z3-backzone`): the names that are Links in
  the default build read their target's history before 1970, Europe/Amsterdam in 1937 being +01:00 where it was
  +01:20, and `Arctic/Longyearbyen` reading Berlin's history, not Oslo's.
- **No 2037 cliff** (Z4, A5, A7, A10, 310 vectors of `time-zones/extrapolation-offset`, `-format`, `-wall`,
  `-day` and `-transition` and one of `time-zones/offset`, reasons `js-z4-extrapolation`,
  `js-a5-extrapolation-wall`, `js-a7-extrapolation-day` and `js-a10-extrapolation-transitions`): each zone's
  recurring rules continue it after its last listed transition, without limit, where the offset after 2037 was
  frozen, on standard time in the north and on daylight time in the south. America/New_York in July 2040 is
  −04:00, where it was −05:00, and America/Santiago −04:00, where it was −03:00; 2040-03-11T02:30 in New York is
  skipped, and `reject` refuses it; New York's next transition after 2037-12-01 is 2038-03-14, where there was
  none.
- **No `Factory`** (Z5, 10 vectors of `time-zones/factory` and 2 of `time-zones/supported`, `js-z5-factory` and
  `js-z5-z7-listed`): `Factory` is a `RangeError` in Temporal and the formatter, and
  `Intl.supportedValuesOf('timeZone')` no longer lists it.
- **CLDR's primary identifiers** (Z7, 19 vectors of `time-zones/primary` and `time-zones/supported`,
  `js-z7-primaries`): `Intl.supportedValuesOf('timeZone')` lists 445 names, where it listed 435 and `Factory`,
  and twelve names that CLDR keeps as keys and tzdb links away are their own primaries, so
  `Europe/Bratislava` no longer equals `Europe/Prague` (`America/Kralendijk`, `Lower_Princes`, `Marigot`,
  `St_Barthelemy`, `Arctic/Longyearbyen`, `Atlantic/Jan_Mayen`, `Europe/Bratislava`, `Busingen`,
  `Mariehamn`, `Podgorica`, `San_Marino`, `Vatican`). `Asia/Calcutta` still equals `Asia/Kolkata`.
- **The calendar years that change source** (K4, K5, K6, K11, K14; no vector): every year of the four
  calendars is ICU4X's, where it was the installed .NET runtime's or the CLDR package's. Table 1 of the Intl era
  and month code proposal leaves each of these years to an implementation-defined approximation.
  - `dangi` after 2050 is ICU4X's Korean year, reckoned at UTC+9, where it was the Chinese computation at
    Beijing's meridian; 25 of the 52 years differ, 2061 beginning on 2061-01-22 where it began on 01-21, and
    2099's leap month being `M03L` where it was `M02L` (K4).
  - `dangi` 918 to 1899 is ICU4X's approximation, where it was .NET's Korean table: 918 has 354 days, where
    it had 355 (K5).
  - `chinese` and `dangi` outside the tables, 1880 to 1899 and after 2102, are ICU4X's approximation, where they
    were the CLDR computation: `chinese` 2104 begins on 2104-01-27 with the leap month `M06L`, where it began on
    01-28 with `M05L` (K6).
  - `persian` outside Table 1's range follows the 33-year rule with `calendrical_calculations`' corrections,
    where it was .NET's astronomy: 1535 is a leap year, its Esfand 30 being 2157-03-20, and 1536 begins on
    2157-03-21, where it began a day earlier; 2,582 of the years 1 to 9378 change, 12 of them in 1178 to 3000
    (K11).
  - No calendar answer depends on the installed runtime's patch any more (K14).
  - A `chinese` or `dangi` `PlainMonthDay`'s reference date reads the years 1900 to 2035 only, as Table 6 of
    the proposal does, where it read 1898 to 2101; so ICU4X's years outside that span, such as 1898's `M01L`
    and `dangi` 2052's 30-day `M08L`, do not give a month-day a reference date Table 6 does not give it.

`islamic-umalqura` reads the same KACST data from the shared table that it read from the CLDR package's, and the
same civil arithmetic outside it, so no answer changes (K8, K9); `persian` keeps every year of 1178 to 1534
AP, Table 1's 1206 to 1498 among them (K10).

**The host's zone has one dataset** (A15, decision SJL-D-19). `Date`'s local-time provider reads the host's
zone from the shared database, as `Temporal` and the default formatter do, where it read the host's
`TimeZoneInfo`, whose release and rules could differ from `Temporal`'s for the same zone. Its offsets are the
database's to the second, so a local mean time keeps its seconds: in America/New_York,
`new Date(1850, 0, 1).getTimezoneOffset()` is 296.03333333333336 (−4:56:02), where it was 296. Its names, which
`toString` writes in parentheses, stay the host's display names, the standard or the daylight one as the
shared database's daylight flag reads it. The CLR views `JSDate.Value` and `JSDate.DateTime` keep the host's
offset ([`docs/public-api.md`](../docs/public-api.md)).

**Not this milestone's.** The formatter's resolved `timeZone` keeps the name it was given, as `canonical-tz`
asks, until the specification edition both engines pin settles it (Z8). *(Revised 2026-10-18, when row Z16 was
brought into SJL-8.6, entry above; the paragraph ended "; and a `longOffset` name still drops an offset's
seconds, which is zone display and milestone SJL-9's.")*

Tests: `SharedTimeZoneTests` (the embedded tables, held to the home's files and to the manifest's digests, and
every time-zone row above), `SharedTimeZoneHostTests` (A15, A16) and `SharedCalendarTableTests` (the calendar
rows) in `Broiler.JavaScript.BuiltIns.Tests`; `Issue783Tests` compares `persian` with .NET's calendar over
1178 to 1534 AP, where the two agree.

### A regular-expression literal's group names and classes are checked at parse time (2026-10-17)

The parser's early check of a regular-expression literal now makes the constructor's group-name checks. Both
call one implementation (`RegExpGroupNames`). The parser also gains two class checks that neither made
before. Each fix corrects test262 rows at `ccaac100` that failed both before and after milestone SJL-7's
cut-over, as section 6 of bundle `sjl-7-001` records.

- **A group name that is not a RegExpIdentifierName is an early SyntaxError.** Its code points are
  classified by `JSUnicode` (Broiler.Unicode's `EcmaScriptLexical`), as an identifier's are.
  - These no longer parse: `/(?<42a>a)/`, `/(?<a:>a)/u`, `/(?<❤>a)/`, `/(?<𐒤>a)/u` (ID_Continue but not
    ID_Start), `/(?<a\uD801>.)/u` and `/(?<a\>.)/`.
  - test262: `language/literals/regexp/named-groups/invalid-non-id-start-*`, `invalid-non-id-continue-*`,
    `invalid-numeric-*` and `invalid-punctuator-*` (20 paths).
- **A name declared twice in one alternative is an early SyntaxError**, as it already was in the
  constructor: `/(?<a>a)(?<a>a)/`. `/(?<a>a)|(?<a>b)/` still parses (`invalid-duplicate-groupspecifier*`,
  4 paths).
- **Where group names are parsed, every `\k` must be a complete `\k<GroupName>`.** That is the case under
  `u` or `v`, or when the pattern declares a group name.
  - `/(?<a>.)\k/`, `/\k(?<a>.)/` and `/(?<a>.)[\k]/` are now SyntaxErrors both as literals and through the
    constructor, which used to build them (`invalid-incomplete-groupname.js` and
    `invalid-incomplete-groupname-6.js`).
  - Otherwise `/\k/` is still Annex B's letter `k`.
- **Under `v`, a negated class that may contain strings is an early SyntaxError**, by ECMA-262's
  MayContainStrings.
  - `/[^\p{RGI_Emoji}]/v` and `/[^\q{ab}]/v` no longer parse.
  - `/[^\p{RGI_Emoji}&&a]/v`, an intersection with a single character, still does.
  - test262: `built-ins/RegExp/property-escapes/generated/strings/*-negative-CharacterClass.js` (7 paths).
- **Under `u`, a property escape at either end of a class range is a SyntaxError**, as `\d` or `\w`
  already was.
  - `/[--\p{Hex}]/u` and `/[\p{Hex}-￿]/u` are refused as literals and through the constructor, which
    used to build the second (`property-escapes/character-class-range-end.js` and
    `character-class-range-no-dash-start.js`).
  - The class is now read range by range. So `/[a\-\d]/u`, where an escaped `-` stands beside a class
    escape and forms no range, is now admitted; it used to be refused.

`RegExpLiteralEarlyErrorTests` (parser) and `RegExpConstructorEarlyErrorTests` (built-ins) hold the cases.

### The CLR binding's names do not read the host's culture (2026-10-17)

- **.NET member names and the keys matched to them are now lower-cased with invariant casing.** This
  covers a .NET member's script name and a script object's key matched to a .NET property.
  - **Before:** `char.ToLower` and `string.ToLower()` read the current culture. Under `tr-TR` or `az`, a
    member named `ID` became `ıd` (dotless i), and converting `({ id: 7 })` to a type with a property `ID`
    left that property unset.
  - **The sites:**
    - `ToCamelCase` in `Broiler.JavaScript.Runtime`'s `StringExtensions`, in `StringSpan` and in the class
      generator's `ListExtensions`;
    - `StringSpan.ToLower` and `StringSpanExtensions`' snake-case names;
    - the unmarshalling of a script object into a .NET object (`CoreInternalHelpers`, `MarshalExtensions`);
    - `KeyValueStore`'s header names.
  - `ClrType.getMethod` needed no change: reflection's `IgnoreCase` does not read the culture.
- The duplicate search of bundle `sjl-7-001` (section 9) found these sites. `ClrBindingCultureTests`
  holds the cases, run under `tr-TR`.

### The Unicode area moves to Broiler.Unicode's packages (2026-10-16, milestone SJL-7.5)

The Unicode area is Broiler.Unicode's: its code packages `Broiler.Unicode.Algorithms` and
`Broiler.Unicode.Segmentation` `0.1.0-preview.4`, over the Unicode 17.0.0 tables this engine embeds in
`Broiler.JavaScript.Runtime`, which the owner's generator writes from this repository's own pin,
[`data/unicode/pins/`](../data/unicode/pins/README.md), a byte-for-byte copy of the owner's (decision SJL-D-6
(c), as Broiler.VM's JSD-0074 applies it; a test regenerates the tables and compares them byte for byte):

- property lookup for `\p{…}` and `\P{…}`, exact as ECMA-262's tables write the names (`UnicodeProperties`);
- the lexical classes: identifiers, group names, the scanner's white space and `RegExp.escape`'s
  (`EcmaScriptLexical`);
- `toUpperCase`, `toLowerCase`, `toLocaleUpperCase` and `toLocaleLowerCase` (`UnicodeCaseMapping`);
- `normalize`, and the canonical decomposition `Intl.Collator` and `localeCompare` compare after
  (`UnicodeNormalization`);
- the properties of strings under the `v` flag (`UnicodeStringProperties`);
- `Intl.Segmenter`'s grapheme, word and sentence boundaries and its word status (`UnicodeSegmenter`).

**Where they were**, each deleted: the host's `string.Normalize` and `IsNormalized`; the special-casing
class of `Broiler.Unicode.Properties` over the runtime's data and the host's `CultureInfo`; `CharExtensions`'
identifier classes over the runtime's categories with hand lists and two Unicode 17 patch tables; the group
names' copy of them; `char.IsWhiteSpace` in the scanner and `RegExp.escape`; the loose `\p{…}` lookups of
`Broiler.Unicode.Properties` and of the .NET translator, and its loose name map of the emoji package; and
`Intl.Segmenter`'s grapheme enumeration over the runtime's `StringInfo`, its word heuristic and its
one-sentence answer. The built-ins keep their receiver and argument conversion, their errors, this engine's
locale canonicalisation and the mapping of a locale to the owner's language, the string ceiling, and
`Intl.Segmenter`'s objects and iterators.

**Policies decided here.**

- **A result longer than this engine's strings hold is a `RangeError`** ("Invalid string length"):
  `normalize`, the collator's decomposition and the four casing methods pass `JSString.MaximumLength` as the
  owner's maximum result length, checked on the whole result, where an over-long result was the runtime's
  exception.
- **The default locale of `toLocaleUpperCase` and `toLocaleLowerCase` is this engine's, `en-US`**, when no
  locale is requested, never the host's culture. Intl's other services still default to the host's culture,
  which milestone SJL-9 moves.
- **A property escape's name is checked twice by the same lookup**: by the parser for a regular-expression
  literal, an early error, and by the `RegExp` constructor before Broiler.Regex or the translator reads the
  pattern.

**Corrections the corpus names.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/unicode`, revision 19), under its reason in `scripts/conformance/unicode-divergences.txt`,
with the row of bundle `sjl-7-001`'s correspondence from code it confirmed (Broiler.VM's
`docs/evidence/sjl-7-001/correspondence.md`). Every one of the 3,678 reports that it no longer holds in the
engine-level run after the cut-over.

- **`\p{…}` names are matched exactly** (P1, 1,346 vectors of `unicode/property-refused` and
  `unicode/property-cells`, reason `js-p1-loose-names`): `/\p{letter}/u`, `/\p{Script_Extensions=greek}/u`,
  `/\p{GC=Lu}/u` and `/\p{lu}/u` are SyntaxErrors, where the lookup ignored case, underscores, spaces and
  hyphens. A literal's is an early error (test262's `built-ins/RegExp/property-escapes/` negative tests).
- **`Katakana_Or_Hiragana` is a Script value** (P2, 20 vectors of `unicode/property-accepted`,
  `unicode/property-cells` and `unicode/property-membership`, `js-p2-hrkt`): `/\p{sc=Hrkt}/u` is valid and
  matches nothing, where it was a SyntaxError.
- **A name no table holds never reaches .NET's `\p`** (P3; no vector): a lone name or a `Name=Value` that
  Broiler.Regex refused was left verbatim for the translator, whose .NET pattern might admit it; the lookup
  now refuses it before either engine reads the pattern.
- **General_Category is an operand of a `v`-mode set operation on the translator's path too** (P4; no
  vector): where Broiler.Regex declines a pattern, `[\p{gc=Lu}--\p{Script=Latin}]` resolves its operands,
  where the translator took only Script, Script_Extensions and binary properties and threw a SyntaxError.
- **The names of the properties of strings are exact** (S2, 1 vector of `unicode/property-cells`,
  `js-s2-string-properties`): `/\p{rgiemoji}/v` is a SyntaxError.
- **An identifier subtracts Pattern_Syntax** (I1, 2 vectors of `unicode/identifier-special`,
  `js-i1-pattern-syntax`): `var ⸯ` (U+2E2F, Lm and Pattern_Syntax), literal or escaped, is a SyntaxError.
- **A group name admits what an identifier admits** (I3, 6 vectors of `unicode/identifier-group-name`,
  `js-i3-group-names`): `/(?<ⸯx>.)/` is a SyntaxError and a group name with U+11B60, which Unicode 17 adds,
  is valid, where the group names' copy of the identifier tests had neither the subtraction nor Unicode 17's
  data.
- **U+0085 is not white space** (W1, 3 vectors of `unicode/white-space` and `unicode/white-space-cells`,
  `js-w1-u0085`): `1` U+0085 `+1` is a SyntaxError, and `RegExp.escape` leaves U+0085 unescaped, where both
  read .NET's `char.IsWhiteSpace`. The segmenter's word boundaries no longer read it either (G2).
- **U+00AA and U+00BA are Cased** (C1, 18 vectors of `unicode/casing-final-sigma` and `unicode/casing-cells`,
  `js-c1-other-lowercase`): `'ªΣ'.toLowerCase()` ends in ς.
- **The Turkic lower case reads Unicode 17's data** (C4, 4 vectors of `unicode/casing-cells`,
  `js-c4-turkic-table`): U+A7CB's `toLocaleLowerCase('tr')` is U+0264, where the runtime's older data showed.
- **Soft_Dotted is `PropList.txt`'s** (C5, 5 vectors of `unicode/casing-special` and `unicode/casing-cells`,
  `js-c5-soft-dotted`): U+1DF1A, U+1E04C, U+1E04D and U+1E068 are soft-dotted, so a following U+0307 is
  removed by `toLocaleUpperCase('lt')`.
- **A casing context admits no intervening character of class 0 or 230** (C6, 2 vectors of
  `unicode/casing-special` and `unicode/casing-cells`, `js-c6-context-marks`): `'I'` U+0903 U+0300
  `.toLocaleLowerCase('lt')` is `i` U+0903 U+0300, where a mark of class 0 was passed over.
- **A list of locales is read as a list** (C7, 2 vectors of `unicode/casing-locale`, `js-c7-locale-list`):
  `'i'.toLocaleUpperCase(['tr', 'en'])` is `İ`, where the joined list reached `CultureInfo` and threw a
  RangeError.
- **`null` is not a locale** (C8, 1 vector of `unicode/casing-locale`, `js-c8-locale-null`):
  `'i'.toLocaleUpperCase(null)` is a TypeError, where it read the host's culture.
- **A lone surrogate passes through normalization** (N1, 50 vectors of `unicode/normalize-lone-surrogate`,
  `unicode/normalize-cells` and `unicode/normalize-collator`, `js-n1-lone-surrogate`): `'\uD800'.normalize()`
  is the string itself, and `localeCompare` and `Intl.Collator.prototype.compare` compare such a string,
  where the host's normalizer threw an Error.
- **Sentence boundaries are UAX #29's** (G1, 138 vectors of `unicode/segmenter-sentence` and
  `unicode/segmenter-cells`, `js-g1-sentences`): `One. Two? Three! Mr. Smith` is five sentences, where every
  text was one.
- **Word boundaries are UAX #29's** (G2, 684 vectors of `unicode/segmenter-word` and
  `unicode/segmenter-cells`, `js-g2-words`), with no dictionary: `can't`, `a_b`, `c.d`, `3,000` and a Katakana
  run are one word each, and each ideograph is its own.

**Data policy by decision.** Pinned data at Unicode 17.0.0 replaces the host's, under decision SJL-D-6 (c);
JSD-0074 counts each of these as a correction under SJL-D-16 of class C5.

- **Normalization at Unicode 16 and 17** (N2, 582 vectors of `unicode/normalize-part1` to `part5` and
  `unicode/normalize-cells`, `js-n2-host-data`): `'\u{105C9}'.normalize('NFD')` decomposes, where the host's
  ICU 74.2 (Unicode 15.1) left it.
- **Normalization under invariant globalization** (N3; no vector): it normalizes, where the runtime's
  `Normalize` returned its input unchanged.
- **The default case mappings at Unicode 17** (C3, 50 vectors of `unicode/casing-lower` and
  `unicode/casing-upper`, `js-c3-beria-erfe`): Beria Erfe's case pairs map, which the hand patches lacked.
- **Cased and Case_Ignorable at Unicode 17** (C2; no vector): Final_Sigma beside the code points Unicode 16
  and 17 add is decided by `DerivedCoreProperties.txt`, where it read the runtime's categories.
- **Identifier classes** (I2; no vector of its own): the pinned ID_Start and ID_Continue, where the runtime's
  categories, hand lists and patches gave the same answers at Unicode 17.0 only through the patches.
- **Grapheme clusters** (G3, 17 vectors of `unicode/segmenter-grapheme`, `js-g3-graphemes`), where they were
  the runtime's `StringInfo`'s.

**Implementation-defined.** A word segment's `isWordLike` is the owner's word status, a letter, a number,
kana or an ideograph (G4, 747 vectors of `unicode/segmenter-word-like` and `unicode/segmenter-cells`, with
G2's boundaries, `js-g2-g4-word-status`), where it was "contains a letter or a digit". A native function's
name in its source text is checked by code point with the identifier classes, where it read
`char.IsLetter` and `char.IsLetterOrDigit`; every native name is ASCII, so nothing changes (I4).

**Not this milestone's.** Case folding under `/i` without `u` (`/[a-z]/i` matching U+212A, row F2) and the
translator's multiline line terminators (W5) are the .NET translator's, which milestone SJL-11 retires;
their code is untouched. `Broiler.Unicode.Properties` and `UnicodeEmoji.StringProperties` still arrive in
the package closure through `Broiler.Regex`'s dependencies until SJL-11, and no code of this engine calls
them. `StringSpan.Trim`, `TrimStart` and `TrimEnd` are obsolete forwarders to the shared white-space set
([`docs/public-api.md`](../docs/public-api.md)).

Tests: `SharedUnicodeTests` and `UnicodeDependencyTests` (BuiltIns), `UnicodeLexicalClassTests` (Parser),
`JSUnicodeTests` and `UnicodeTableRegenerationTests` (Runtime), `StringSpanTrimTests` (Ast).

### The `era` option is written beside a format with no year (2026-10-16)

- **`Intl.DateTimeFormat` writes the `era` option's era beside a format that shows no year**, where the
  pattern appended its era field only to a date with a year, so the option was dropped from `format`
  and `formatToParts` alike. It follows a date, `{ era: "short", month: "short", day: "numeric" }`
  formats as `"Oct 16 AD"` where it was `"Oct 16"`, and leads a time alone, joined as a date is:
  `{ era: "short", minute: "2-digit", second: "2-digit" }` is `"AD, 03:05"`, where it was `"03:05"`.
  These are CLDR's en layouts, and V8's (ICU 77.1, CLDR 47) for the same options.
- **A lone day, month or weekday beside the era is laid out as ICU's pattern generator lays it out**,
  V8's layouts for all 30 pairs of an era width and a day, month or weekday style. CLDR's en has no
  skeleton with an era and such a field, so ICU starts from the nearer, by its width distance, of the
  era alone and the field's own skeleton (`d`; `M` or `MMM`; `E`), the era on a tie, and appends the
  other with en's appendItems. From the era, a day or month is `"{0} ({2}: {1})"` and a weekday
  `"{0} {1}"`: `{ era: "short", day: "numeric" }` is `"AD (day: 16)"`, `{ era: "short", weekday: "short" }`
  `"AD Fri"` and `{ era: "long", day: "2-digit" }` `"Anno Domini (day: 16)"`. From the field, the era
  follows it, as after a date: `{ era: "long", day: "numeric" }` is `"16 Anno Domini"` and
  `{ era: "narrow", month: "long" }` `"October A"`. Beside a time the result joins as a date does
  (`"AD (day: 16), 12:03 PM"`). The field names are en's in every locale, as this engine's month,
  weekday and era names are. V8 joins a long month to a time with `" at "` (`"AD (month: October) at
  12:03 PM"`), as it does any long date, which this engine's patterns do not model.

An era-using calendar's era written without the option, such as `buddhist`'s `BE`, still needs a year,
as in CLDR; a Temporal type with no era, `PlainTime` or `PlainMonthDay`, still drops the option.
`DateTimeFormatEraWithoutYearTests` holds the cases.

### `formatToParts` writes a minute-and-second format through the pattern `format` writes (2026-10-16)

`Intl.DateTimeFormat.prototype.formatToParts` wrote a format with `minute` and `second` but no `hour` as
`mm:ss[.fff]` by hand, where `format` writes the format's resolved pattern. The hand-written branch is
gone, and such a format's parts join to what `format` writes. In the Latin-digit locales, for the formats
that had no other field, the parts are unchanged.

- **The digits follow the numbering system**, a locale's default one or a `-u-nu-` extension's, as
  `format`'s do: `new Intl.DateTimeFormat("ar-EG", { minute: "2-digit", second: "2-digit" })` writes the
  minute `"٠٣"`, where it was `"03"`, and with `fractionalSecondDigits` `arab` writes the fraction after
  its decimal separator U+066B, where it was `"."`.
- **The other fields the format asks for are written**: a date field, `weekday` or `timeZoneName` beside
  the minute and second was dropped from the parts, so
  `{ day: "numeric", minute: "2-digit", second: "2-digit" }` gave `minute`, `literal` and `second` alone.

`DateTimeFormatMinuteSecondPartsTests` holds the cases. The time-zone correction below holds on the
pattern's path, which reads the same zoned fields, and its tests still pass.

### `formatToParts` writes a minute and second in the format's time zone (2026-10-16)

- **`Intl.DateTimeFormat.prototype.formatToParts` writes a format with `minute` and `second` but no
  `hour` in its resolved `timeZone`**, from the zoned fields `format` writes, where it used the host's
  zone through `DateTimeOffset.ToLocalTime()`, bypassing this engine's provider of the host's zone
  (decision SJL-D-19) that `format` goes through. A zone whose offset is not a whole hour had the host's
  minute: `new Intl.DateTimeFormat("en", { minute: "2-digit", second: "2-digit", timeZone: "Asia/Kolkata" })`
  formats 12:00:05Z as parts that join to `"30:05"`, like `format`, where on a UTC host the minute was
  `"00"`. The fractional seconds come from the same fields.
- **That format writes a time value outside the years 0001 to 9999**: `formatToParts(8.64e15)` is
  `"00:00"` in UTC, where .NET's `ArgumentOutOfRangeException` reached the caller.

`DateTimeFormatMinuteSecondTimeZoneTests` holds the cases. Milestone SJL-6.6's duplicate search
(`simplification/duplicate-search.log` of bundle `sjl-6-001`) found it and named it for milestone SJL-9.

### The Date area moves to `Broiler.ECMAScript.Algorithms` (2026-10-16, milestone SJL-6.5)

The Date area is the shared library's, the stream's `0.1.0-preview.4` in this repository's
`Broiler.ECMAScript`:

- the time arithmetic of every getter and setter, the constructor, `Date.UTC`, `getYear` and `setYear`
  (`EcmaTime`);
- local time, `LocalTime` and `UTC` with the specification's disambiguation of skipped and repeated local
  times, over this engine's provider of the host's zone: `TimeZoneInfo.Local`, read at each instant with
  its daylight-saving time (decision SJL-D-19, `EcmaLocalTime`);
- `Date.parse`'s Date Time String Format, through the library's strict dialect, and Broiler.VM's dialect
  after this engine's own fallback (`EcmaDateParser`);
- `toString`, `toDateString`, `toTimeString`, `toUTCString` and `toISOString` (`EcmaDateFormatter`);
- the civil-day arithmetic of `Temporal.Instant`, `PlainDate`, `PlainDateTime`, `PlainMonthDay`,
  `PlainYearMonth` and `ZonedDateTime`, of the ISO string reader and the non-ISO calendars, and Intl's
  date fields (`IsoCalendarMath`, `EcmaTime`).

**A `Date`'s storage is its time value alone**, where it was a `DateTimeOffset` with a raw time value
beside it for the dates the type cannot hold; the CLR views of it are in
[`docs/public-api.md`](../docs/public-api.md). The built-ins keep their argument conversion and its
order, their errors, the fallback of `Date.parse` and the .NET format string of `toLocaleString` (decision
SJL-D-17). **Two of this engine's own readings are decided** (correspondence rows P4 to P6 and R5):

- **`Date.parse` keeps the format's leniencies its own grammars read**, now in every form of the format
  and every year, where the .NET format lists read them in the years 0001 to 9999 only and the expanded
  years' expression read only some: a fraction of none to nine digits, cut to milliseconds
  (`"2021-03-04T10:00:00.1Z"` is 1614852000100), and an offset without its colon
  (`"0001-01-01T00:00-0530"`). A lower-case `t` keeps the culture parse's reading, and a `'T'`-joined
  string the format does not hold is still not read leniently (test262 `staging/sm/Date/non-iso.js`).
- **A .NET format string formats a date from the year 1 to the year 9999 only**:
  `new Date(Date.UTC(-1, 0, 1)).toLocaleString(undefined, "yyyy-MM-dd")` is a `RangeError`, where it
  answered `"0001-01-01"`, the invalid-date sentinel's fields. Within those years it formats the instant
  at the offset in force at it, where it used the offset the date was stored with (L2).

**`Date.parse` reads what both its grammars refuse in Broiler.VM's dialect** (JSD-0072's order: the
format, this engine's fallback, then Broiler.VM's). Every string this engine read keeps its reading,
except where a correction below says otherwise; among the strings it now reads are its own renderings of
the years outside 0001 to 9999 (P9, below), a day past its month's length, carried into the next month
(`"2021-02-30"` is March 2, where it was NaN, P5), and the dialect's textual forms, a weekday that does not
match its date and an offset of 60 minutes among them (P7).

**Corrections the corpus names.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/date`, revision 16), with the row of bundle `sjl-6-001`'s correspondence it
confirmed (Broiler.VM's `docs/evidence/sjl-6-001/correspondence.md`). Corpus revision 17 retires the 1,016
fields, and [`docs/evidence/sjl-6-001/engine/retired-divergences-revision-17.tsv`](../docs/evidence/sjl-6-001/engine/retired-divergences-revision-17.tsv)
lists every retired vector under its reason.

- **One offset per instant, the one in force at it** (L2, 892 vectors of `date/offset`, `date/to-string`,
  `date/local-string`, `date/local` and `date/date-time`, in a zone with daylight-saving time): in
  New York, `new Date(2021, 6, 1)` is 04:00Z, midnight EDT, where storage and the components constructor
  took the zone's base offset, so it was 05:00Z with `getHours()` 0 and `toString()` 01:00 EDT; a local
  setter kept whichever offset the date was stored with.
- **A date-only form is UTC** (P1, 59 vectors of `date/date-only` and `date/utc`, in a zone whose offset is
  not zero): `Date.parse("2021-03-04")` is UTC midnight, where it was local midnight for the years 0001 to
  9999.
- **The ISO week of a date in a negative year** (G2, 22 vectors of `date/iso-week`):
  `Temporal.PlainDate.from("-000002-12-28").weekOfYear` is 53 of -2, where `WeeksInIsoYear` divided with
  truncation and answered week 1 of -1.
- **`Date.parse` reads back this engine's renderings of the years outside 0001 to 9999** (P9, 16 vectors of
  `date/round-trip`): `Date.parse(new Date(Date.UTC(-1, 0, 1)).toUTCString())` is that date, where the
  .NET formats could not hold the year and answered NaN.
- **A setter converts every argument before it writes the date** (T3, 14 vectors of `date/setter-order`):
  `setUTCMinutes`, `setUTCSeconds`, `setUTCMonth`, `setHours`, `setMinutes`, `setSeconds` and `setMonth`
  made the date invalid as soon as their first argument was `undefined` or NaN, so a later argument's
  `valueOf` read `getTime()` as NaN. `setYear` likewise reads the time value before it converts its
  argument, where it read it after, so a `valueOf` that moved the date set the year of the moved date.
- **A setter answers a valid date outside the years 0001 to 9999** (D1, 5 vectors of `date/utc-setter` and
  `date/local-setter`): `new Date(9999, 11, 31).setDate(32)` is January 1 of 10000, where the
  `DateTimeOffset` arithmetic threw and the date became invalid.
- **A setter sets a date outside those years** (D2, 5 vectors of the same files): `new Date(-1e15).setHours(1)`
  answers the new date, where it answered NaN and left the date unchanged.
- **The instant 0001-01-01T00:00:00.000Z is a date** (D3, 2 vectors of `date/utc-setter` and
  `date/round-trip`), -62135596800000, where it was the invalid-date sentinel.
- **A month at or past 2^53 is taken exactly** (T4, `date/make-day/0863`):
  `Date.UTC(-833333333333333333, 1e19, 1)` is May 1 of year 0, -62156764800000, where the month saturated
  as a 64-bit integer and the date fell in August.

**Corrections the corpus does not hold.** Their outcome depends on the zone, the culture or the host;
`SharedDateAlgorithmTests` holds each, the zone's in a collection that runs under `America/New_York`.

- **A skipped local time is read at the offset before its transition, a repeated one at its earlier
  instant, and a local time just after a transition at the offset after it** (L3): in New York,
  `new Date(2021, 2, 14, 2, 30)` is 07:30Z, 03:30 EDT, where `getHours()` answered 2;
  `new Date(2021, 10, 7, 1, 30)` is 05:30Z, where it was 06:30Z; and
  `Date.parse("2021-03-14T03:30:00.0000")` is 07:30Z, where it was 08:30Z.
- **A date outside `DateTimeOffset`'s range takes the offset the zone's rules give at its instant** (L4),
  where it took the offset in force at the moment of the call: in New York, `new Date(10000, 0, 1)` is
  05:00Z in any season, and a date before the zone's standard time takes its local mean time.
- **`getTimezoneOffset` reads a date outside the years 0001 to 9999** (D4), where it answered NaN.
- **A structured clone keeps a date outside those years** (D5): `structuredClone(new Date(-1e15))` is
  that date, where the clone copied only the `DateTimeOffset` storage and was invalid.
- **`toString`'s and `toTimeString`'s offset is written with U+002D** (R3), where `GMT-0500` was written
  with the culture's negative sign, U+2212 under `sv-SE`, and did not read back (P8).
- **`toUTCString` reads back under any culture** (P8): `Date.parse` reads its form before the culture's
  parse, which read the year in the culture's calendar under `fa-IR` and `ar-SA`.
- **A Gregorian-family `PlainMonthDay` with a year and a month past 12 is bound-checked in December**, as
  `constrain` regulates the month, where the civil copy the check called carried the month past the year:
  `Temporal.PlainMonthDay.from({ calendar: "gregory", year: 275759, month: 21, day: 13 })` is `12-13`, where
  it was a `RangeError`. The shared `EpochDays` takes a month from 1 to 12 only; `reject` refuses such a
  month either way.

**Messages and names.** The zone's name in parentheses is the host's daylight or standard name at the
instant (R4), where a date outside `DateTimeOffset`'s range took the name in force at the moment of the
call.

### Each locale's default numbering system is CLDR's (2026-10-15, milestone SJL-5.6)

A locale's default numbering system is CLDR's, release 47.0.0, where every locale defaulted to `latn`.
The 52 locales CLDR gives another default write its digits: `ar-EG`, `ar-SA` and the other Arabic regions
CLDR gives Arabic-Indic digits (`new Intl.NumberFormat("ar-EG").format(1234)` is `"١٬٢٣٤"`, where it was
`"1٬234"`), `fa`, `ps` and `ur-IN` Extended Arabic-Indic digits, `bn` Bengali, `mr` and `ne` Devanagari,
`my` Myanmar, and so on. `ar`, `en` and every other locale keep `latn`. NumberFormat, DateTimeFormat,
RelativeTimeFormat and DurationFormat resolve it, their `resolvedOptions().numberingSystem` reports it, and
`Intl.Locale.prototype.getNumberingSystems()` puts it first; a `-u-nu-` extension or a `numberingSystem`
option still chooses another. The table is generated by `scripts/intl/generate_default_numbering_systems.py`
from CLDR's `cldr-numbers-full` 47.0.0, which [`THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) names,
under the owner's data decision of 2026-10-15 recorded in
[`docs/roadmap/SharedLibraries.md`](../docs/roadmap/SharedLibraries.md); milestone SJL-9 moves it into the
shared locale data.

- **A Temporal value's digits follow the numbering system.** `Intl.DateTimeFormat.prototype.format` and
  `formatToParts`, and the Temporal types' `toLocaleString`, wrote a Temporal value's digits in ASCII
  whatever the numbering system, where a `Date`'s followed it (`formatRange` already did). test262:
  `intl402/DateTimeFormat/prototype/format/temporal-objects-no-time-clip-non-latin-numerals.js`, which
  passed before only because `String.prototype.includes` compared by culture, so that Latin `13` matched
  Arabic-Indic `١٣`: correction X2 below made the search ordinal, and the digits showed. Tests:
  `DefaultNumberingSystemTests`.

### Text algorithms move to `Broiler.ECMAScript.Algorithms` (2026-10-15, milestone SJL-5.5)

The text area is the shared library's, the stream's `0.1.0-preview.3` in this repository's
`Broiler.ECMAScript`:

- UTF-16: `isWellFormed`, `toWellFormed`, `codePointAt`, the String iterator's step, and
  `AdvanceStringIndex` wherever a RegExp method moves past an empty match (`[Symbol.split]`,
  `[Symbol.match]`, `[Symbol.replace]`, `matchAll`'s iterator and the engine's own enumerations);
- `padStart` and `padEnd`'s StringPad, and the relative-index normalisation of String `at`, `slice` and
  `substr`; Array `at`, `includes`, `indexOf`, `lastIndexOf`, `slice`, `toSpliced`, `with`, `copyWithin`,
  `fill` and `splice`; the typed arrays' `at`, `copyWithin`, `fill`, `includes`, `indexOf`, `lastIndexOf`,
  `slice` and `subarray`; and `ArrayBuffer`'s `slice` and `sliceToImmutable` and `SharedArrayBuffer`'s
  `slice`;
- the replacement templates of `replace`, `replaceAll` and `RegExp.prototype[Symbol.replace]`, which the
  library parses and this engine evaluates part by part;
- `encodeURI`, `encodeURIComponent`, `decodeURI`, `decodeURIComponent`, `escape` and `unescape`;
- `Uint8Array`'s base64 and hex codecs;
- `JSON.parse`'s and `JSON.rawJSON`'s reading, which no longer goes through System.Text.Json, and
  `JSON.stringify`'s quoting and gap.

The built-ins keep their coercions, their option reading, their errors and their drivers. **Two
policies are this engine's, and stated:** the longest String a pad or a repeat builds is
`JSString.MaximumLength`, 1,073,741,791 code units, the longest a .NET string can be, and past it the
call is a `RangeError`; and `JSON.parse` and `JSON.rawJSON` read a value at most 512 levels deep
(`JSJsonParser.MaximumDepth`), the document being level 0, and past it the parse is a `RangeError`. A
typed array's `with` keeps its normalisation inline: its relative step uses the length before the
value's conversion and its range test the length after it, which the shared one-length rule cannot
say. The API dispositions are in [`docs/public-api.md`](../docs/public-api.md).

**Corrections the corpus names.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/text`, revision 12), with its reason in `scripts/conformance/text-divergences.txt`
and the row of bundle `sjl-5-001`'s correspondence it confirmed. Corpus revision 13 retires the 42 fields,
and [`docs/evidence/sjl-5-001/engine/retired-divergences-revision-13.tsv`](../docs/evidence/sjl-5-001/engine/retired-divergences-revision-13.tsv)
lists every retired vector under its reason.

- **`setFromHex` stops at the destination** (B1, `js-set-from-hex-whole-input`,
  `text/set-from-hex/0001`, `0002`, `0011` to `0013` and `0021`): `new Uint8Array(1).setFromHex("aaZZ")`
  is `{ read: 2, written: 1 }`, where the whole text was parsed first and the `ZZ` the method never
  reads raised `SyntaxError`.
- **An options argument that is not an object is a `TypeError`** (B2, `js-options-not-object`,
  `text/options/0001`, `0002` and `0006`): `Uint8Array.fromBase64("", 1)` and `toBase64(null)` were
  read as no options.
- **A `null` `alphabet` or `lastChunkHandling` is a `TypeError`** (B3, `js-options-null-name`,
  `text/options/0003` and `0004`), where it was read as the default.
- **A detached or out-of-bounds array is a `TypeError` to the codecs** (B4, `js-detached-codec`,
  `text/options/0009` to `0012`): `toBase64` and `toHex` answered `""`, and `setFromBase64` and
  `setFromHex` answered `{ read: 0, written: 0 }`.
- **`JSON.parse` reads past 64 levels** (J1, `js-json-depth`, `text/parse-depth/0011` to `0022`):
  System.Text.Json's default depth, which nobody chose, refused a document nested 65 deep with
  `SyntaxError`; the depth is now the policy above.
- **A lone surrogate in a JSON string is that code unit** (J2 and J3, `js-json-escaped-surrogate` and
  `js-json-raw-surrogate`, `text/parse/2364`, `2365`, `2367` and `2368` and `text/parse-surrogate/0001`
  to `0004`): `JSON.parse('"\\ud800"')` raised `SyntaxError`, and a raw lone surrogate let the .NET
  `ArgumentException` of the text's transcoding to UTF-8 reach the script as a plain `Error`.
- **Only `JSON.rawJSON` makes a raw JSON object** (J6, `js-raw-json-brand`, `text/raw-json/0005` and
  `0006`): the brand is an internal slot of the object `JSON.rawJSON` returns, where any frozen object
  with an own string `rawJSON` passed for one, and `JSON.stringify(Object.freeze({ rawJSON: "}{" }))`
  wrote `}{` into its output unchecked.
- **The raw JSON object's `rawJSON` is enumerable** (J7, `js-raw-json-enumerable`, `text/raw-json/0003`):
  `Object.keys(JSON.rawJSON("1"))` is `["rawJSON"]`, where it was `[]`.
- **`%TypedArray%.prototype.lastIndexOf` reads `fromIndex` whenever a second argument is passed** (P5,
  `js-last-index-of-from-index`, `text/last-index-of/0002`), where a third argument made it ignored.
- **A named group's value is converted with ToString** (S2, `js-named-group-to-string`,
  `text/named-to-string/0001`): its `Symbol.toPrimitive` is called first, where the CLR's `ToString`
  called its `toString` directly.
- **`replace` with a RegExp whose `Symbol.replace` is `undefined` or `null` searches for its text** (S3,
  `js-absent-replace`, `text/absent-replace/0001` and `0002`): `"a/a/".replace(re, "x")` searches for
  `"/a/"`, where the pattern was matched as a regular expression by `JSRegExp.Replace`, whose templates
  never substituted `$<name>`.

**Corrections the corpus does not hold.** `SharedTextAlgorithmTests` holds each, beside control cases
that pass on both sides of the cut-over.

- **A pad or a repeat past the length policy is a `RangeError`** (P4): `"x".padEnd(2 ** 30 + 1)` and
  `"x".repeat(2 ** 30 + 1)` let .NET's `OutOfMemoryException` reach the script as a plain `Error`.
- **`split` with an empty separator honours its limit** (X1): `"abc".split("", 2)` is `["a", "b"]`,
  where it was `["a", "b", "c"]`.
- **`indexOf`, `includes`, `startsWith`, `endsWith` and the non-standard `contains` compare code units**
  (X2), where they used the current culture's comparison, which under ICU reads `"\r\n"` as one unit
  and ignores ignorable characters: `"a\r\nb".indexOf("\n")` is 2.

**Messages.** A JSON text `JSON.parse` refuses is a `SyntaxError` naming where the reader stopped, such
as "Unexpected token 'x' in JSON at position 3" or "Unexpected end of JSON input at position 1", where
the message was System.Text.Json's. A URI function's `URIError` and the codecs' `SyntaxError`s keep
their messages. `JSON.stringify`'s line terminator, the correspondence's J5, was corrected on
2026-10-14 (below).

### `JSON.stringify` breaks lines with LINE FEED on every platform (2026-10-14)

- **An indented `JSON.stringify` ends each line with LINE FEED (U+000A) alone**, as
  SerializeJSONObject and SerializeJSONArray specify: `JSON.stringify({ a: 1 }, null, 2)` is
  `"{\n  \"a\": 1\n}"` on every platform, where on Windows each line ended in CR LF, the platform's line
  terminator. Compact output, without a `space` argument, had no line breaks and is unchanged.
  `JsonStringifyLineTerminatorTests` holds the cases; test262's JSON tests run on Linux, where the two
  terminators agree, and the Windows Release job of #1025 found it.

### Numerics move to `Broiler.ECMAScript.Algorithms` (2026-10-13, milestone SJL-4.5)

The numerics area is the shared library's, version `0.1.0-preview.2` *(corrected 2026-10-14; read
"version `0.2.0`")*:

- the integer conversions (`ToInt32` to `ToUint8Clamp`, `ToIntegerOrInfinity`, `ToLength`, `ToIndex`'s
  range rule, `ToBigInt64` and `ToBigUint64`) and Float16;
- `Math.round`, `Math.pow` and `**` (`Number::exponentiate`, on every path the compiler has),
  `Math.hypot`, `cbrt`, `expm1`, `log1p` and `f16round`, and `Math.sumPrecise`'s sum;
- the exact rounding of Temporal and Intl: the rounding modes, `RoundNumberToIncrement` and the
  rounding of a ratio to the nearest Number;
- the BigInt kernel: every `BigInt::` operation, `BigInt.asIntN` and `asUintN`, a BigInt's Number and
  its comparison with a Number, its text at every radix, and `StringToBigInt`.

`Broiler.JavaScript.Expressions` references the shared library beside `Parser`, `Runtime`,
`BuiltIns` and `Storage`, for the compiled `**`. The built-ins keep their coercions, their errors, their
option reading and their value wrapping. The BigInt kernel runs under decision SJL-D-15 (a): no width
but the representation's (2^31 - 128 bits), no meter, and a result past it a `RangeError`. The API
dispositions are in [`docs/public-api.md`](../docs/public-api.md).

**Corrections the corpus names.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/numerics`, revision 8), with its reason in
`scripts/conformance/numerics-divergences.txt`. Corpus revision 9 retires the 54 fields, and
[`docs/evidence/sjl-4-001/engine/retired-divergences-revision-9.tsv`](../docs/evidence/sjl-4-001/engine/retired-divergences-revision-9.tsv)
lists every retired vector under its reason.

- **`1 ** NaN` is NaN, and so are 1 and -1 to an infinity, on every path** (`js-pow-one-nan`,
  `numerics/exponentiate/0128`, `exponentiate-numeric/0128` and `exponentiate-literal/0098` to
  `0100`). The generic `JSValue.Power` lacked the NaN exponent's case, and the literal and compiled
  paths called `Math.Pow`, which gives 1.
- **`Math.cbrt` is correctly rounded** (`js-cbrt-rounding`, 9 vectors, such as `numerics/cbrt/0012`):
  `Math.cbrt(Number.MIN_VALUE)` is 2^-358, an exact cube root, where it was the double below it.
- **`Math.hypot` is within one unit in the last place** (`js-hypot-pairwise`, 5 vectors, such as
  `numerics/hypot/0051`): `Math.hypot(2, 10, 11)` is 15, where it was 15.000000000000002, because the
  arguments were combined in pairs, rounding at each step.
- **`Math.expm1` and `Math.log1p` are within one unit in the last place** (`js-expm1-log1p`,
  `numerics/expm1/0025`, `0042`, `0044` and `0059`, and `numerics/log1p/0057`): `Math.expm1(-745)` is
  -1, where it was -1.00075, and `Math.expm1(709.78)` is finite, where it was Infinity.
- **A BigInt exponent of 2^31 or more is exact** (`js-bigint-power-exponent`,
  `numerics/exponentiate/0349` to `0352` and `numerics/policy/0022`): `1n ** 2n ** 31n` is `1n`,
  `0n ** 2n ** 31n` is `0n` and `(-1n) ** (2n ** 31n)` is `1n`, where each raised a .NET
  `OverflowException`; a result past the representation, `2n ** 2147483648n`, is a `RangeError`.
- **`BigInt.asUintN` of a negative value at a width of 2^32 or more is a `RangeError`**
  (`js-asuintn-wide`, 18 vectors, such as `numerics/asuintn/0211`): the width was cut to 32 bits, so
  `BigInt.asUintN(2 ** 32, -1n)` was `0n`, `BigInt.asUintN(2 ** 32 + 3, -1n)` was `7n`, and other widths
  raised a .NET exception.
- **A BigInt property key beyond 2^32 - 1 or below 0 is the BigInt's decimal text**
  (`js-bigint-key`, `numerics/property-key/0001`, `0002`, `0003`, `0005` and `0007`): `({})[2n ** 40n]`
  and `o[-1n] = 2` raised a .NET `OverflowException`, and `o["4294967295"] = 3; o[4294967295n]` was
  `undefined`.
- **`StringToBigInt` of a `0x`, `0o` or `0b` text is linear in its length** (`js-bigint-hex-parse`,
  `numerics/policy/0018` and `0019`): each digit multiplied the whole value, and a hexadecimal text of
  262,144 digits did not complete within the runner's limit.

**Corrections the corpus does not hold.** Each is a case of the tests named, run against this
repository's `01209c4`, before the cut-over, and after it.

- *The integer conversions* (`SharedIntegerConversionTests`):
  - **Atomics work on the element's bytes**: an operand is converted as the element type converts it,
    so `Atomics.add(new Int32Array([1]), 0, 2 ** 53)` leaves 1, where it left 0, and so do `or` and
    `compareExchange`'s expected value (`2 ** 63` matches 0). The operand was saturated to a 64-bit
    integer and summed through a double. `Atomics.wait` reads its value with `ToInt32`.
  - **`%TypedArray%.prototype.fill` reads its range with `ToIntegerOrInfinity`**:
    `new Uint8Array(3).fill(1, 0, Infinity)` fills every element, where it filled none, and
    `fill(1, 2 ** 32)` fills none, where it filled every one.
  - **A typed array's misaligned offset is a `RangeError` before its length is converted**
    (`InitializeTypedArrayFromArrayBuffer`): the length's `valueOf` ran, and threw, first.
  - **`%TypedArray%.from` passes its constructor `ToLength` of the source's length**: a length of 2^32
    reached the constructor as 2147483647.
  - **A `Date` setter's component is not wrapped modulo 2^32**: `setUTCSeconds(2 ** 32 + 1)` moves the
    date by 4294967297 seconds, where it moved it by 1, and `setUTCMinutes(0, 0, -(2 ** 32))` by
    -2^32 milliseconds, where it did not move it. Dates beyond what the engine's `DateTimeOffset` holds
    (the year 9999) remain invalid; that is the engine's limit, not the conversion's.
  - **`String.prototype.endsWith` takes an undefined position as the length**:
    `'abc'.endsWith('c', undefined)` is `true`, where it was `false`; and `endsWith`, `startsWith` and
    `includes` convert the search string before the position.
- *The Math kernels* (`SharedMathTests`):
  - **`Math.sumPrecise`'s `TypeError` for a value that is not a Number wins over whatever the
    iterator's `return()` throws**, where `return()`'s error escaped; and a count reaching 2^53 is the
    `RangeError` the proposal specifies.
- *The BigInt kernel* (`SharedBigIntTests`, each case at the top level and compiled in a function):
  - **A BigInt's text ignores the culture**: `String(-1n)`, concatenation, template literals,
    `toString()`, `join` and property keys write `"-1"`, where a culture whose negative sign is U+2212
    wrote `"−1"`.
  - **A result past the representation is a `RangeError`**: `1n << 2147483647n`, `1n << 2147483600n`,
    `(-1n) << 2147483646n`, `2n ** (2n ** 64n)`, `BigInt.asUintN(2 ** 31, -1n)` and
    `BigInt.asUintN(2 ** 53 - 1, -5n)` raised an `Error` wrapping a .NET exception, and
    `1n ** 0x10000000000000000n` a .NET `OverflowException`.
  - **`==` between a BigInt and a Number is exact on the compiled path too**:
    `2n ** 53n + 1n == 2 ** 53 + 1` and `2n ** 1024n == 1e400` are `false`, where the compiled
    comparison with a literal went through a double and gave `true`.
  - **`ToBigInt` and `BigInt(value)` convert with the number hint**, where they took the default one:
    `BigInt.asUintN(8, obj)` and `BigInt(obj)`, for an `obj` whose `Symbol.toPrimitive` answers `1n` for
    the number hint and `2n` otherwise, are `1n`, where they were `2n`.
  - **A BigInt element converts with the number hint as well**: a `BigInt64Array` or `BigUint64Array`
    element, `fill`, `DataView`'s `setBigInt64` and `setBigUint64`, and Atomics on a BigInt array convert
    with `ToBigInt`, so `new BigInt64Array([obj])`, for the `obj` above, holds `1n`, where it held `2n`.
    They refused a Number before, and still do.
  - **`new (1n)()` is a `TypeError`**, where it was `0n`.
- *The exact rounding* (`SharedRoundingTests`):
  - **`Intl.NumberFormat`'s scientific and engineering notations round with the rounding mode, and
    decide the exponent from the rounded digits**: with `maximumFractionDigits: 1` and
    `roundingMode: "floor"`, 9.99 in scientific notation is `9.9E0`, where it was `0.9E1`, and 999.99 in
    engineering notation is `999.9E0`, where it was `0.9E3`; with `maximumSignificantDigits: 2`, 9.99 is
    `1E1`, where it was `10E0`. They rounded half away from zero, whatever the mode.
  - **Its compact notation rounds with the rounding mode, and rounds the Number's exact value**, as the
    standard notation already did: 1990 with `"floor"` is `1.9K`, where it was `2K`; 1250 with
    `"halfEven"` is `1.2K`, where it was `1.3K`; -1910 with `"floor"` is `-2K`, and -1990 with `"trunc"`
    is `-1.9K`, where they were `-1.9K` and `-2K`; and 1.15, whose exact value is below 1.15, is `1.1`,
    where it was `1.2`.
  - **`Temporal.Duration.prototype.round` rounds a calendar unit within its increment's window**
    (`NudgeToCalendarUnit`): `Temporal.Duration.from("P1M").round({ smallestUnit: "months",
    largestUnit: "months", roundingIncrement: 2, roundingMode: "halfTrunc", relativeTo: "2023-01-01" })`
    is `P2M`, where it was `PT0S`.
  - **`Temporal.Duration.prototype.total` rounds once**: five years and 26368.123456789 seconds,
    relative to a `ZonedDateTime` in UTC, total 5.000836127709817 years, where a sum of doubles gave
    5.0008361277098174.
  - `Duration.prototype.round`'s `RangeError` for an unknown rounding mode reads "Temporal: invalid
    roundingMode", as the other types' do.

- *The copies the search for survivors found* (2026-10-14; `SharedNumericsSurvivorsTests`, run
  against `728e958`, the cut-over before the search, and after it). Built-ins outside the correspondence's list converted
  their arguments by hand; each now calls the library, and these results change:
  - **`Function.prototype.apply`, `Reflect.apply` and `Reflect.construct` build their list with
    `CreateListFromArrayLike`**: `LengthOfArrayLike`, then `Get` of each index. A length of 2^31 or
    more is a `RangeError`, where the call got no arguments, or a .NET `OverflowException` escaped for
    an array; an array-like whose `length` is inherited, a `Proxy`, and a `String` object give their
    elements, where the call got none (a `Proxy` of an array raised a .NET `OverflowException`); and a
    hole is read through the prototype, so with `Array.prototype[1] = "p"` the hole at 1 is `"p"`,
    where it was `undefined`.
  - **`String.raw` reads its `raw` object with `LengthOfArrayLike`**: `String.raw({ raw: "abc" }, 1, 2)`
    is `"a1b2c"`, where it was `""`, and a `raw` of length 2^32 is a `RangeError`.
  - **`RegExp.prototype[Symbol.match]` advances an empty match from `ToLength(lastIndex)`**: a
    `lastIndex` of -5 becomes 1, where it became -4, and one of 2^31 becomes 2147483649, where it
    became -2147483648; with the `u` flag, a negative `lastIndex` raised a .NET
    `IndexOutOfRangeException`.
  - **`String.prototype.padStart` and `padEnd` convert their length with `ToLength`**, and a result
    too long for the engine is a `RangeError`: `"a".padStart(2 ** 32 + 3, "-")` raised an `Error`
    wrapping a .NET `OutOfMemoryException`.
  - **`%TypedArray%.prototype.set` reads an array-like source's length with `ToLength`**:
    `new Uint8Array(2).set({ length: -5 }, 3)` is a `RangeError`, an offset past the end, where the
    negative length let it through.
  - **The Set methods' `GetSetRecord` truncates `size` before its sign test**: a set-like object of
    size -0.5 is of size 0, where it was a `RangeError`.
  - **`TimeClip` is `ToIntegerOrInfinity`**: `new Date(0).setTime(-0.5)` is +0, where it was -0.
  - **`MakeTime` and `MakeDay` apply `ToIntegerOrInfinity` to each component**: `setUTCHours(1.5)`
    sets one hour, where it set an hour and a half; `setUTCDate(1.5)` and `setDate(1.5)` keep the
    first day, where they added twelve hours; and `new Date(1000).setUTCMilliseconds(-0.5)` is 1000,
    where it was 999.
  - **`Intl.NumberFormat`'s scientific and engineering notations take the mantissa and exponent from
    the Number's exact value**, where they divided by a power of ten in doubles: with 20 fraction
    digits, `1e-30` is `1.00000000000000008334E-30`, where it was `1E-30`, `9.999999999999999e22` is
    `9.99999999999999916114E22`, where it was `0.99999999999999977796E23`, and `5e-324` is
    `4.94065645841246544177E-324`, where it was `0.5E-323`. The default precision's results are
    unchanged.
  - **A subnormal Number's exact value is right in every notation**: it was scaled by 2^-1075 rather
    than 2^-1074, so its digits were those of half the value (`5e-324` to three significant digits
    ended in 247, where it ends in 494).
  - **A String's Intl mathematical value trims the specification's white space**: with 20 fraction
    digits, `format("\uFEFF0.1")` is `0.1`, where U+FEFF stopped the decimal reading and the text was
    read as the Number 0.1 (`0.10000000000000000555`).
- *The typed arrays' copies of the same element type* (2026-10-14; `SharedIntegerConversionTests`, run
  against `b598028`, SJL-4's base, and after `bc2d663`):
  - **`slice`, `copyWithin` and the constructor given a typed array keep a `Float32Array`'s signalling
    NaN**: `0x7F800001` stays `0x7F800001`, where it was quieted to `0x7FC00001`, because each element
    went through a double. They move bytes, as the specification asks, and `set` already did. test262's
    `staging/sm/TypedArray/slice-bitwise-same.js`, which failed at the base, passes.

**Implementation-defined, and changed.** `Math.f16round(NaN)` and a NaN read from a `Float16Array` or
with `DataView.prototype.getFloat16` are the platform's quiet NaN, where they kept the binary16 value's
sign and payload in the double; no result but the bits written to another buffer shows it. A NaN
written to a binary16 element still keeps its sign and the high bits of its payload, as this plan
committed this engine to. A copy between typed arrays of the same element type moves the bytes, as the
specification asks, so it keeps each element's encoding: `slice`, `copyWithin`, `set`, and the
constructor given a typed array. *(Added 2026-10-14. The cut-over had made `slice`, `copyWithin` and
that constructor copy through the value, so a binary16 NaN came out as the platform's, its sign set
and its payload dropped; test262's `TypedArray/prototype/slice/bit-precision.js` found it in bundle
sjl-4-001's first after run. `SharedIntegerConversionTests` holds the cases.)*

**Not corrected here.** `Temporal.PlainTime`, `PlainDateTime` and `ZonedDateTime`'s `round` round the
nanoseconds since midnight rather than the unit the specification names (`01:10` rounded to 20 minutes
with `"halfEven"` is `01:20`, where the specification gives `01:00`); that is the Temporal consumer's
quantity, which item SJL-10 answers, as it answers Temporal's other quantities. `Intl.NumberFormat`'s
mathematical value is item SJL-9's. *(2026-10-19: corrected at milestone SJL-10.5, whose entry above lists it under
"Corrections the correspondence did not find". This paragraph is unchanged.)*

### Number conversion moves to `Broiler.ECMAScript.Algorithms` (2026-10-12, milestone SJL-3.5)

`Broiler.JavaScript.Parser`, `.Runtime`, `.BuiltIns` and `.Storage` reference the shared library,
and every number conversion is its: `Number::toString` at every radix, `toFixed`, `toExponential`
and `toPrecision`, `StringToNumber`, `parseInt` and `parseFloat`, the values of numeric literals,
array-index and canonical numeric keys, and the white-space set of `String.prototype.trim` and the
parsers. The built-ins keep their coercions, their RangeErrors and their string creation. The API
dispositions are in [`docs/public-api.md`](../docs/public-api.md).

**Corrections.** Each was a `divergence.broiler-js` field of the shared corpus
(`conformance/vectors/numbers`, revision 4), with its reason in
`scripts/conformance/number-divergences.txt`; corpus revision 5 retires the fields, and
[`docs/evidence/sjl-3-001/engine/retired-divergences.tsv`](../docs/evidence/sjl-3-001/engine/retired-divergences.tsv)
lists every retired vector under its reason. The argument handling the corpus does not hold is
covered by `Broiler.JavaScript.BuiltIns.Tests/SharedNumberConversionTests.cs`.

- **`Number.prototype.toString(radix)` at a radix other than 10 gives the shortest digits that read
  back as the value** (reason `radix-longer`, 233 vectors of `numbers/to-string-radix`, such as
  `0051`). The fraction was expanded digit by digit, up to 1,100 digits; the digit cap is gone with
  it.
- **Its digits denote the value** (`radix-inexact`, 124 vectors, such as `numbers/to-string-radix/0065`).
  The fraction was expanded in double arithmetic, whose rounding error entered the digits.
- **Its integer part is exact at 2^63 and beyond** (`radix-64-bit`, 238 vectors, such as
  `numbers/to-string-radix/0015`). It was converted through a 64-bit integer.
- **A radix whose `ToIntegerOrInfinity` is 0 is a RangeError: `0`, `-0`, `null` and `NaN`**
  (`radix-argument`, `numbers/to-string-radix-arguments/0002`, `0005`, `0006` and `0010`). The first
  three were read as radix 10, and `NaN` escaped as a host `ArgumentException`.
- **`toPrecision` coerces its argument before it looks at the value, and checks the truncated
  precision** (`precision-argument`, `numbers/to-precision-arguments/0003`): `(1.5).toPrecision(100.5)`
  is 100 digits instead of a RangeError, and `Infinity.toPrecision(x)` now calls `x`'s `valueOf`, as
  step 3 does before step 4.
- **`parseInt(null, radix)` and `parseInt(undefined, radix)` parse `"null"` and `"undefined"`**:
  `parseInt(null, 36)` is 1112745 instead of NaN (ECMA-262 §19.2.5 step 1;
  `SharedNumberConversionTests`). The radix is converted once, where it was converted twice.
- **The white-space set is the specification's**, generated from the pinned Unicode 17.0.0 data:
  `parseInt` no longer trims U+0085 NEXT LINE, which is not white space (`next-line-white-space`,
  `numbers/parse-int/0064` to `0070`); it trimmed with .NET's `string.Trim`. U+FEFF was, and is,
  trimmed by every parser and by `String.prototype.trim`, which read the set from a list of its own
  before. `BigInt(string)` and the comparisons of a BigInt with a string (StringToBigInt) trimmed
  .NET's set: they trim U+FEFF now, so `BigInt("﻿1")` is `1n` instead of a SyntaxError, and no
  longer trim U+0085 (`SharedNumberConversionTests`).
- **Hexadecimal, octal and binary integers beyond 2^53 are rounded to the nearest double, ties to
  even**, as literals and as strings (`non-decimal-rounding`, 62 vectors of `numbers/literal` and
  `numbers/string-to-number`, such as `numbers/literal/0213`). They were rounded twice.
- **`Number()` and `parseFloat` complete for a decimal significand of more than 309 digits**
  (`long-significand-hang`, 6 vectors, such as `numbers/parse-float/0777`), **and give its value, not
  NaN, below 1e-308** (`long-significand-nan`, 12 vectors, such as `numbers/parse-float/0918`).
- **Numeric literals the grammar refuses are SyntaxErrors**: `07.5` (`legacy-octal-fraction`,
  `numbers/literal/0091`), `1e` (`empty-exponent`, `0105` and `0106`), `01n` and `08n`
  (`bigint-leading-zero`, `0137` to `0140`) and `.5n` (`bigint-fraction`, `0145` and `0146`). The
  scanner's digit runs let them through and the value code read them; the scanner now takes each
  literal's value from the shared library, which refuses them.
- **Canonical numeric keys are `Number::toString`'s.** `"1E+21"` and `"1E-07"`, the .NET `"R"`
  format's spellings, were classified as canonical numeric strings, so a typed array ignored them as
  keys; they are ordinary property keys now (`SharedNumberConversionTests`).

- **A Number equals a String through ToNumber only.** Three fast paths of `==` compared .NET's text of
  the number with the string, so `NaN == "NaN"` and `"NaN" == NaN` were `true`; they are `false`
  (ECMA-262 §7.2.14; `SharedNumberConversionTests`). The search for surviving duplicates of the area
  (bundle sjl-3-001, milestone SJL-3.7) found them.
- **The portable compiler's generated C# holds every number constant exactly.**
  `PortableCompiler.EmitCSharp` wrote a constant in .NET's `"R"` format, which does not read back as
  the same double for every value: `2.9802322387695312e-8` (2\*\*-25) was written
  `2.980232238769531E-08`, the double below it, and an infinite one, such as the literal `1e400`, was
  written `Infinity`, which C# does not compile. Each is written as its IEEE-754 bits now
  (`PortableCompilerTests`). The same search found it.

**Not corrected here.** A `NonOctalDecimalIntegerLiteral` (`08`, `09.5`) in strict mode code is still
accepted (`strict-non-octal-decimal`, `numbers/literal/0082`, `0084`, `0088` and `0090`): strictness
is the compiler's, and item SJL-14 answers it.

**Kept, as decision SJL-D-17 has them:** the `m` (`Decimal`) literal suffix, `Number.prototype.clz`,
and `toLocaleString`'s .NET format strings.
