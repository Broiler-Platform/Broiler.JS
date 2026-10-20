# The parse-corpus tool (bundle SJL-12-001, parts C and D)

**Written:** 2026-10-20, at SJL-12.4 (work package SJL-12.4, task 4.5), for
[`decision-rule.md`](../../decision-rule.md) parts C and D. **Revised:** 2026-10-20, before the corpus commit,
after verification: the dump is streamed into its hash and grows linearly with the tree, a dump that cannot be
completed is reported as such, `K` compiles on the tool's own thread, json rows take the rule's entry points, the
dump reads non-public fields, and the comparison requires the manifest. The rule names this folder as part of the corpus commit;
it is kept unchanged from the corpus commit to the after commit, and each commit's runs build it against that
commit's own projects. The comparison is [`scripts/compliance/compare_parse_corpus.py`](../../../../../scripts/compliance/compare_parse_corpus.py).

It holds no result. Its runs go to the bundle's `base/`, `after/` and `controls/`.

## What it runs

The tool reads the parse corpus's manifest (`conformance/parse/corpus.manifest`, format 1, which
`conformance/parse/README.md` defines) and every input it names: a test262 input from the suite at the manifest's
`pin-test262`, any other from the corpus directory. Before it parses anything it checks every input against the
manifest's SHA-256 and decodes it as strict UTF-8; a missing, changed or undecodable input refuses the whole run. It
refuses a manifest whose format, column line, row count, ids, goals or variants are not format 1's.

**Variants**, as format 1 says Broiler.JS's tool composes them: `strict` is the manifest's `strict-prologue` followed
by the text; `sloppy` and `raw` are the text unchanged under the script goal; `module` is the text unchanged under
the module goal; `json` is the text unchanged. **Source names** are the manifest's fixed names, `virtual-root` or
`stored-root` followed by the path.

**The json goal.** The rule's entry points are `FastParser.ParseModule` and the module flag for the module goal, and
`ParseProgram` and no module flag otherwise. A `json` row (the text of a module imported `with { type: "json" }`)
takes the second pair, as the rule's text gives it: `ParseProgram`, and `CoreScript.Compile` with `isModule: false`,
over its text unchanged, which is parsed as a script, not as JSON. So every row is answered with one of the rule's
three outcomes at both entry points, and none with an outcome part C does not name; the corpus's own README
(`conformance/parse/README.md`, "A `json` row under this repository's rule") reads a json row under part C the same
way. The report header's `json-goal` field says so.

**Part C (the default mode).** For each input and variant, in the manifest's order, on one thread:
- **P, the parser alone:** `new FastParser(new FastTokenStream(new StringSpan(text), (FastKeywordMap?)null))
  .ParseProgram()` for every goal but module, `FastParser.ParseModule(new StringSpan(text))` for the module goal. The
  keyword map is passed explicitly as `null`, the stream's default, so that the call binds to the same constructor if
  the after commit adds overloads with other optional parameters (JSD-0080 decision D19 adds options overloads to
  these types). `accepted`; `refused`, a `FastParseException`, with its type and message; or `crashed`, any other
  exception, with its type (and its message, which the comparison does not compare).
- **K, the whole compile:** `CoreScript.Compile(text, location: <source name>, args: null, codeCache: <pass-through>,
  compilationOptions: null, isModule: <module variant>)`, where the pass-through cache's `GetOrCreate` compiles the
  code it is given (`code.Compiler().CompileWithNestedLambdas()`) and stores nothing. `accepted`; `refused`, a
  `JSException` whose thrown value is a JavaScript `SyntaxError` (its `name`, read as the shell reads it), with the
  type and message; or `crashed`, any other exception, a `JSException` of another error included. A `JSException`'s
  type is written with the thrown value's name after a colon (`Broiler.JavaScript.Runtime.JSException:SyntaxError`).
- K compiles inside a `JSContext` made on the same thread, with the experimental features the shell runs test262
  with (`JavaScriptFeatureFlags.AllExperimentalEs2026`); a compile without a context cannot build the `SyntaxError` it
  refuses with. The source name is always given, because a compile without one is named from a process-wide counter.

**Part D (`--dump`).** For each input and variant: P, as above; for an accepted one, the SHA-256 of the canonical
dump of the tree the parser returns, written before the pass is called; then the compiler's for-loop pass over that
tree with a direct-eval depth of 0, and the SHA-256 of the canonical dump of the tree it returns, or, where it
throws, of the exception's type and message in its place. Run 1 is `--order manifest`, run 2 `--order reverse`, run 3
`--threads 2`, on which each input is parsed and prepared twice at once, once on each thread (a barrier starts both
threads on each input together; each thread has its own context).

A dump is streamed into its SHA-256 as it is written, so no dump is held whole and its size has no limit but the
tree's; the tree is walked with an explicit stack, so its depth costs no thread stack. A dump that still cannot be
completed (an exception while it is written, say a property's enumeration that throws, or memory) is never hashed in
part: its hash column reads `dump-failed:<exception type>`, the comparison counts it as a failure in any run, and with
`--text` the partial text is followed by a line `!dump-failed <type> "<message>"`.

**The for-loop pass** is found by reflection when the tool runs, by the name and signature the rule fixes: in the
assembly `Broiler.JavaScript.Compiler`, the type `Broiler.JavaScript.Compiler.ForLoopPreparation` and its static
method `AstProgram Prepare(AstProgram program, int directEvalDepth)`, public or not. Where there is none - at the
corpus commit, which has no pass - the run dumps the parser's tree alone: every accepted row's pass column reads
`absent`, and the header's `for-loop-pass` field says why (`absent: Broiler.JavaScript.Compiler declares no type
Broiler.JavaScript.Compiler.ForLoopPreparation`). Where it is found the field reads `found: ` and the signature.

**Members named at compile time.** The tool is unchanged from the corpus commit to the after commit, so every
product type and member it names must exist, with the same signature, at both; SJL-12.5's items keep each of these:
- **Parser** (`Broiler.JavaScript.Parser`): the constructor `FastParser(FastTokenStream)`, `FastParser.ParseProgram()`,
  the static `FastParser.ParseModule(in StringSpan)`, the constructor `FastTokenStream(in StringSpan, FastKeywordMap =
  null)` and the type `FastKeywordMap`.
- **Ast** (`Broiler.JavaScript.Ast`, `Broiler.JavaScript.Ast.Misc`): the types `AstNode`, `AstProgram`,
  `FastParseException`; `AstNode.Type` (an enum), `AstNode.Start` and `AstNode.End` (each a `FastToken`);
  `FastToken.Type` and `FastToken.Span`; `TokenTypes.EOF`; the constructor `StringSpan(string)` and
  `StringSpan.Source`, `Value`, `Offset` and `Length`.
- **Runtime** (`Broiler.JavaScript.Runtime`): `CoreScript.Compile(in StringSpan code, string location, IList<string>
  args, ICodeCache codeCache, JSCompilationOptions? compilationOptions, bool isModule)`, called by parameter name;
  the interface `ICodeCache` and its one method `JSFunctionDelegate GetOrCreate(in JSCode code)`; the delegate type
  `JSFunctionDelegate`; the field `JSCode.Compiler` (a `JSCodeCompiler`, invoked); `JSException` and
  `JSException.Error`; `JSValue`'s indexer `this[KeyString]`, `JSValue.IsUndefined` and `ToString()`.
- **Storage** (`Broiler.JavaScript.Storage`): `KeyStrings.GetOrCreate(string)`, which returns the `KeyString` the
  indexer takes.
- **ExpressionCompiler** (`Broiler.JavaScript.ExpressionCompiler.Runtime`): the extension
  `RuntimeAssembly.CompileWithNestedLambdas<T>(this BExpression<T>)`. **Expressions** (namespace
  `Broiler.JavaScript.ExpressionCompiler`): the settable static `CompilationStack.SizeBytes`.
- **Engine** (`Broiler.JavaScript.Engine`): the constructor `JSContext(experimentalFeatures: ...)`, `JSContext` as
  `IDisposable`, and `JavaScriptFeatureFlags.AllExperimentalEs2026`.

The dump reads every other node member by reflection, and the pass by name, so neither needs to exist at the corpus
commit.

## Environment, threads and stack

The rule's configuration is one thread with a stack of 256 MB, no `LANG` and `TZ=UTC`. The tool refuses to run
unless `TZ` is `UTC` and `LANG`, every `LC_*` variable and every `BROILER_*` variable are unset (each `BROILER_*`
variable changes how the product parses or compiles, among them `BROILER_JS_COMPILE_STACK_BYTES`,
`BROILER_JS_VISITOR_SEGMENT_BYTES` and `BROILER_JS_DUMP_PROGRAMS`). Part C's work and each of part D's runs run on
threads of 256 MB that the tool starts: one, or two for run 3.

**K's compile runs on that thread too.** By default the product's `CompilationStack` hands a compile of a source
longer than 512 characters (`CompilationStack.InlineSourceLengthLimit`) to its own worker thread of 64 MB
(`CompilationStack.DefaultSizeBytes`). Before anything is compiled the tool sets `CompilationStack.SizeBytes` to 0,
which the product documents as the opt-out for a host that has sized its own thread: every compile then runs on the
calling thread, the tool's thread of 256 MB. The header's `compile-stack-bytes` field records the value. One
hand-off of the product's stays, because it is not a host's to set: a compiler visitor whose walk has used 4 MB of
stack (`StackSegment`) continues it on a fresh stack of `CompilationStack.DefaultSizeBytes`, and the tool's thread
waits for it. P and the for-loop pass are called by the tool directly, on its thread.

Each report records the environment, the compile stack, the runtime, the operating system, the processor count,
and each product assembly's informational version (which carries the commit) and module version id (which
identifies the build). The reports carry no time, so two runs from one build that the product answers alike write
byte-identical reports.

## Build and run

```
export PATH=/root/.dotnet:$PATH DOTNET_ROOT=/root/.dotnet
dotnet build -c Release -nologo --artifacts-path <scratch>/art docs/evidence/sjl-12-001/tools/parse-corpus/parse-corpus.csproj
T=<scratch>/art/bin/parse-corpus/release/parse-corpus.dll
M=conformance/parse/corpus.manifest
S=<test262 at ccaac100ff49d81e9ff47a75ff4c60e0bd3f262e: a checkout, or the Broiler.VM archive extracted>

env -u LANG TZ=UTC dotnet $T --manifest $M --test262 $S --out <run>/parse-corpus-c.tsv.gz
env -u LANG TZ=UTC dotnet $T --dump --order manifest --manifest $M --test262 $S --out <run>/parse-corpus-d1.tsv.gz
env -u LANG TZ=UTC dotnet $T --dump --order reverse  --manifest $M --test262 $S --out <run>/parse-corpus-d2.tsv.gz
env -u LANG TZ=UTC dotnet $T --dump --threads 2      --manifest $M --test262 $S --out <run>/parse-corpus-d3.tsv.gz
```

`--artifacts-path` keeps the build's `bin/` and `obj/` out of the tree. A path ending `.gz` is written gzipped.
`--text <file[.gz]>` (with `--dump`) also writes every dump's text, each after a line `=== <id> <variant> <slot>
<parser|prepared|threw>`, for reading a difference (each thread writes a dump to a scratch file beside it,
`<file>.slot-<slot>.part`, deleted when the run ends, and copies it in whole, so the two threads of run 3 never wait
for each other's dumps); `--only <id>[,<id>...]` runs those rows alone (the report says so, and the comparison
refuses it); `--trace` names each input on standard error before it is run, which locates an input that ends the
process. The exit status is 0 for a written report and 2 for a refused environment,
manifest or input; any other failure ends the process with an unhandled exception and writes no report.

The comparisons, the control and the controls' checks:

```
C=scripts/compliance/compare_parse_corpus.py
python3 $C control     --base base/parse-corpus-c.tsv.gz --out controls/parse-corpus-c-control.tsv.gz --output controls/parse-corpus-c-control.json
python3 $C acceptance  --base base/parse-corpus-c.tsv.gz --after after/parse-corpus-c.tsv.gz --manifest $M \
                       --classifications after/parse-corpus-classes.json --changelog Broiler.JS/CHANGELOG.md --output after/parse-corpus-c.json
python3 $C determinism --run1 after/parse-corpus-d1.tsv.gz --run2 after/parse-corpus-d2.tsv.gz --run3 after/parse-corpus-d3.tsv.gz \
                       --population after/parse-corpus-c.tsv.gz --manifest $M --require-pass --output after/parse-corpus-d.json
python3 $C determinism --run1 <K1 run 1> --run2 <K1 run 2> --run3 <K1 run 3> --population after/parse-corpus-c.tsv.gz --manifest $M --expect-differ 1:2
```

`acceptance` and `determinism` require `--manifest`: the runs' manifest digest must be that file's, and the runs must
report every input and variant it names (at both entry points, for part C), so the rule's completeness is always
checked; each prints the digest it checked. Every value in a report must be one the tool writes (an outcome of the
rule, a SHA-256 or `dump-failed:<type>`, a pass state), or the runs are not comparable.

The script decides no class: a difference of part C is admitted only when the classification file, written by a
person, names it as M, X or D with a reason and the row meets that class's conditions as the rule states them. For
M the changelog given with `--changelog` must contain the old and the new message; for X the classification cites,
as `"changelog"`, the changelog's text that names the correction, and the changelog must contain it (the rule lists
each resolved crash in the changelog as a correction). Its docstring sets out each subcommand.

**Tests.** The script's are `scripts/compliance/tests/test_compare_parse_corpus.py` (`python3 -m unittest discover
-s scripts/compliance/tests`). The tool's are beside this folder, in `../parse-corpus-tests`, which is not part of the
tool and is not held unchanged with it: the dump's format, a dump of trees as deep as the corpus's deepest input
(25 000 nested conditionals) and deeper (200 000 nested arrays, 300 000-link member and call chains) that is whole
and linear in size, a dump that cannot be completed reported as `dump-failed`, the non-public fields, and the entry
points a json row takes (`dotnet test -c Release --artifacts-path <scratch>/art
docs/evidence/sjl-12-001/tools/parse-corpus-tests/parse-corpus-tests.csproj`).

## The reports

UTF-8 (in practice ASCII), one record per line, LF. Header lines `#% <key>\t<value>...` come first, then the column
line, the rows, and the last line `#% end\t<rows>`; a report without it did not complete. The header names the mode,
the manifest's SHA-256 and row count, the selection, the order, the threads and their stack, the compile stack, the
environment, the runtime, how the json goal is answered, the product assemblies and, for a dump, the for-loop pass.

- Part C: `id`, `variant`, `entry` (`P` or `K`), `outcome` (`accepted`, `refused` or `crashed`), `type`,
  `message`; two rows per input and variant, in the manifest's order.
- Part D: `id`, `variant`, `slot` (`1`, or `A` and `B` on two threads), `parse` (P's outcome), `parser-sha256`,
  `pass` (`prepared`, `threw` or `absent`), `pass-sha256`; one row per input, variant and slot, in the manifest's
  order whatever the run's order. A hash column holds a SHA-256 in lower-case hex, `dump-failed:<exception type>`
  where the dump could not be completed, or `-` where there is no dump (P did not accept, or there is no pass).

A field's backslash, tab, line feed and carriage return are written `\\`, `\t`, `\n` and `\r`, and every other
character outside printable ASCII `\uXXXX` (a UTF-16 code unit, so a lone surrogate is kept); an empty or absent
value is `-`.

## The canonical dump

The dump is text whose SHA-256 (of its UTF-8 bytes, which are ASCII) is what a run reports; nothing in it depends on
the process (no identity, hash code, address, culture or clock), and names are written as they are, never
renumbered. Its size grows linearly with the tree's: no line is indented.

- One line per node: `<depth> <label>: <CLR type> <FastNodeType> @<start>..<end>` and then the node's inline members,
  ` <name>=<value>` each. `<depth>` is the line's depth in the dump as a decimal number, 0 for the root, whose label
  is `program`. `<start>` is the first token's offset and `<end>` the last token's offset plus its length, in UTF-16
  code units of the variant's text, or `eof` for the end-of-file token.
- **Members** are, of each type from `AstNode` down to the node's own and in that order, the type's instance fields
  and then its public readable properties without parameters, each in declaration (metadata) order. For a type of
  the product's own (an assembly named `Broiler.*`) the fields include the non-public ones, so that a flag or name
  held in an internal or private member is in the dump; for a type of the runtime's (a tuple, say) they are its
  public fields alone. A compiler-generated backing field is left out where its property is written, and is
  labelled with its property's name where it is not (a non-public auto-property). `AstNode.Type`, `Start` and `End`
  make the head, and `AstNode.Code` (the source text of the span) is left out. A member that hides an inherited one
  of the same name is labelled `<declaring type>.<name>`. A property whose getter throws is written
  `!throws <exception type>`.
- **Inline values:** `null`; `true` or `false`; an enum's name (flags joined by `|`); a number in the invariant
  culture (`R` for a double, `-0` for negative zero); a string or a `StringSpan` quoted, with the field escapes and
  `\"`; a token as `<TokenType>@<start>..<end>:"<text>"`, or `EOF`; a list of inline values as `[a,b]`; a value type
  of inline members as `<type>(<name>=<value>,...)`; any other class as its full type name in angle brackets. A
  type's name is its full name with no assembly name or version; a generic type's arguments follow in `<...>`.
- **Nested values** - a node, or a list or value type that holds one - follow on their own lines at the next depth:
  a node as above; a list as `<depth> <label>: list <count>` and its items labelled `[i]`; a value type as
  `<depth> <label>: <type>` and its inline members, then its nested ones. A node that is its own ancestor is written
  `cycle <CLR type>`; a node shared by two parents is written under each.
- Where the pass returns null, the dump in its place is `0 program: null`; where it throws,
  `throws <exception type> "<message>"`. A dump that cannot be completed has no hash (see Part D above).

At the corpus commit the parser's for-loop desugaring names its temporaries from a process-wide counter, so a loop's
dump depends on how many loops the process parsed before it: run 2 differs from run 1 there, which is what part D's
base shows. JSD-0080's decision D22 names the pass's temporaries `<d>:<n>`, deterministic per compilation; they are
dumped as they are.
