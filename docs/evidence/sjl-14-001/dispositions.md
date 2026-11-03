# Bundle SJL-14-001's dispositions: every public type and member of `Ast` and `Parser`, the non-tree types' new homes, and SJL-14.5's deletion list member by member

**Written:** 2026-11-03, at milestone SJL-14 (work package SJL-14.1, task 1.1) of the shared JavaScript libraries
programme, with this bundle's [rule](decision-rule.md) and before any change it judges exists. **Decision:** D50 of
2026-11-03, the owner's under the direction of 2026-10-18 (`docs/roadmap/SharedLibraries.md`, section "Broiler.JS on
the shared syntax library (SJL-14)"), which applies SJL-D-12 (i) and SJL-D-25. **Read by:** the rule's parts F (every
type marked obsolete here carries the mark), H (`docs/public-api.md` at the after commit agrees with this file) and K
(the deletion search runs over the list below); Broiler.VM's JSD-0083, whose X6 gains this engine's half from this
file by a dated note. **Applied at:** the cut-over, W2 (SJL-14.4 and SJL-14.5); nothing is marked, moved or deleted
by this file.

## The listing

[`dispositions.tsv`](dispositions.tsv) gives **every public type and member** of `Broiler.JavaScript.Ast` and
`Broiler.JavaScript.Parser` at `33d3093b` (this branch's base, Broiler.JS's default branch on 2026-11-03), one line
each, with its disposition, its removal version and its home after the cut-over. It is written by
[`tools/records/write_records.py`](tools/records/write_records.py) from the listings in [`api/`](api/):
`Broiler.JavaScript.Ast.txt` and `Broiler.JavaScript.Parser.txt`, printed by PublicApiGenerator 11.4.6 (assembly
attributes left out) through the lister of bundle `sjl-0-001` (`docs/evidence/sjl-0-001/api/Program.cs`, unchanged),
built in Release against the two projects of a clean `git archive` of `33d3093b` in a scratch folder
([`api/api-lister.csproj`](api/api-lister.csproj), the two references written `<tree>`). Each listing is of the
`net10.0` build. The rule's part H compares the same lister's listings at the base and the after commit.

## The dispositions

| Disposition | What it means at the cut-over | Removal | Applies to |
|---|---|---|---|
| `facade-obsolete` | The public name stays as a facade over the bridge, keeping every member, so that SJL-12's parse-corpus tool and the downstream consumers build and run unchanged; `[Obsolete]` with the programme's diagnostic identifier and the removal version; a refusal throws today's exception type, `FastParseException` | `1.0.0` | every public type and member of `Parser` but one (`FastScope` and `FastScopeItem` among them) |
| `kept-obsolete-no-version` | Kept, with the obsolete mark of SJL-12.5 and no removal version, until a consumer check is recorded (JSD-0080's decision D20; SJL-D-25 (c)) | none | `FastTokenStream.UndoMark()` |
| `obsolete` | The tree types and the members that build, walk or rewrite the tree: kept, `[Obsolete]` with the identifier and the removal version, built by the bridge from the shared tree | `1.0.0` | every public type of `Ast` but the non-tree types below |
| `moved-forwarded` | A non-tree type: moved to `Broiler.JavaScript.Expressions`, which every reader already references, with its namespace, and `Ast` keeps a `[TypeForwardedTo]` for it, so that a binary built against `Ast` still loads it; not marked obsolete (SJL-D-25 (a)) | none | `StringSpan` (with `StringSpan.CharEnumerator`), `StringSpanReader`, `ArraySpan<T>` (with its `Enumerator`), the property contracts `IPropertyAccessor` and `IPropertyValue`, `UnaryOperator`, `TokenTypes`, `CancellableDisposableAction`, `RegExpUnicodeValidator`, and `FastParseException` |

**One removal version, `1.0.0`**, for every SJL-D-12 (i) and SJL-D-25 (b) mark (decision D50). The marks carry one
diagnostic identifier, suppressed by one `NoWarn` in the repository root's `Directory.Build.props`, which every
project of the repository inherits, SJL-12's tool under `docs/evidence/` included, until SJL-15.5.

**The non-tree types' new home is `Broiler.JavaScript.Expressions`** (decision D50). The internal Unicode tables
(`JSUnicodeTables`, `JSUnicodeTableData`, `JSUnicode`, `JSUnicodePropertyNames`, in `Ast` since SJL-12.5) have no
public surface, so they appear in no listing; they move with the types that read them, `RegExpUnicodeValidator` and
`StringSpan`'s readers, with no forwarder (a forwarder is for a public type). `TokenTypesExtensions` stays in `Ast`,
obsolete with the tree: nothing outside the parser and the compiler calls it.

**Where this file departs from decision D50's list, and why.**
- **`FastParseException` is moved and forwarded, not marked obsolete.** D50's list of non-tree types does not name it,
  but its reason does: "obsoleting it whole would mark types Storage, Runtime and BuiltIns use". `Runtime` catches it
  (`Broiler.JavaScript.Runtime/CoreScript.cs:109`, `JSException.cs:265`), the facade throws it, and SJL-12's tool
  catches it, so it is a non-tree type. Read from the code at `33d3093b` by name (this bundle's
  `tools/records/write_records.py` lists the dispositions; the use was found by a search of every project's sources
  for each public type's name outside `Ast` and `Parser`).
- **Every other public type of `Ast` that a product project other than the compiler, the portable compiler and the
  module loader names** is on D50's list: `StringSpan` (`BuiltIns`, `Clr`, `Engine`, `ExpressionCompiler`,
  `Extensions`, `LinqExpressions`, `Modules`, `Network`, `Runtime`, `Storage`), the property contracts (`BuiltIns`,
  `Runtime`, `Storage`), `UnaryOperator` (`Runtime`), `TokenTypes` (`LinqExpressions`), `CancellableDisposableAction`
  and `RegExpUnicodeValidator` (`Runtime`, `BuiltIns`). `Runtime`'s mentions of `AstOptionalChain` and
  `AstExpressionExtensions` are in documentation comments only.

## SJL-14.5's deletion list, member by member (clause 2 as revised on 2026-11-03; the rule's part K)

Clause 2, as Broiler.VM's milestone file revises it on 2026-11-03 before any evidence (decision D50), reads "The
implementations on SJL-14.5's deletion list are absent; the public names SJL-D-12 (i)'s adapter keeps stay only as
obsolete facades over the bridge." These are the implementations, at `33d3093b`, under `Broiler.JS/` (lines are
that commit's). At the after commit each is absent, or is a facade member the table above keeps over the bridge, or a
relocation decision D51 allows; the deletion search of part K looks for each by name, declaration and reference.

| Item | Where | Members |
|---|---|---|
| `FastParser` and its partial files | `Broiler.JavaScript.Parser/` | `FastParser.cs` and `FastParser.ArrayExpression.cs`, `.AssignmentLeftPattern.cs`, `.Block.cs`, `.Class.cs`, `.Decorator.cs`, `.DoWhile.cs`, `.Export.cs`, `.Expression.cs`, `.ForStatement.cs`, `.Function.cs`, `.Identifier.cs`, `.IfStatement.cs`, `.Import.cs`, `.Literal.cs`, `.NextExpression.cs`, `.NonDeclarativeStatement.cs`, `.ObjectLiteral.cs`, `.Parameters.cs`, `.Program.cs`, `.PropertyName.cs`, `.SingleExpression.cs`, `.SingleMemberExpression.cs`, `.SinglePostfixPrefixExpression.cs`, `.Statement.cs`, `.Switch.cs`, `.Template.cs`, `.VariableDeclaration.cs`, `.WhileStatement.cs`, `.WithStatement.cs`: every member they declare; the public `FastParser` keeps its facade members (`ParseProgram`, `ParseModule`, the constructors, `Options`, `Location`, `PreviousToken`, `BeginUndo`, `Combine`, `ExpectStringLiteral`, `Export`, `NonDeclarativeStatement`, `variableScope`, `StreamLocation`) over the bridge |
| `FastScanner`'s scanning code | `Broiler.JavaScript.Parser/FastScanner.cs` | the cursor and its state (`position`, `line`, `column`, `templateParts`, `templateBraceDepth`, `templateBraceDepthStack`, `templateCookedInvalid`, `nextToken`, `lastToken`, `lastSignificantToken`, `parenIsStatementHead`, `lastCloseParenEndedStatementHead`, `SaveCursor` :265, `RestoreCursor` :268, `Peek` :295 and :314, `Next` :304, `ConsumeAndNext` :324, `Consume` :337, `CanConsume` :370 and :394, `CanConsumeNext` :382); the readers (`ReadToken` :407, `_ReadToken` :415, `ReadTemplateString` :1147, `ReadSymbol` :1216, `ReadCommentsOrRegExOrSymbol` :1222, `SkipMultilineComment` :1601, `SkipSingleLineComment` :1660, `ReadString` :1679, `ReadIdentifier` :1723, `ReadIdentifierEscape` :1788, `ReadNumber` :1846, `ScanEscaped` :776, `ScanUnicodeCodePointEscape` :1002, `HexDigit` :1040, `HexDigitValue` :1067); the classifiers (`IsWhiteSpace` :206, `IsIdentifierStart` :210, `IsIdentifierPart` :214, `OpensStatementHead` :220, `IsHtmlOpenCommentStart` :762, `IsHtmlCloseCommentStart` :768, `IsHashbangStart` :997); `Unterminated` :94, `UnexpectedCharacter` :114, `NoBigIntWidth` :1844, `State.CommitIdentifier` :2042; the public `FastScanner` keeps its facade members (`Text`, `YieldIsKeyword`, the constructors, `Location`, `Options`, `Token`, `ConsumeToken`, `Push`, `Unexpected`, `State` and its members) |
| `FastTokenStream`'s scanning code | `Broiler.JavaScript.Parser/FastTokenStream.cs` | the scanner it drives (`scanner` :16, `furthest` :68) and the internal `LineTerminator` :97, `Unexpected` :19, `UnexpectedStatement` :38; its public members stay as facade members over the bridge's token chain, `UndoMark` :434 as `kept-obsolete-no-version` |
| `FastKeywordMap`'s table | `Broiler.JavaScript.Parser/FastKeywordMap.cs` | the switch table of `IsKeyword` :14 (`:44` onward); no remnant: the `m` suffix arrives through the library's extension parameter (decision D17), so the remnant the milestone allows is not needed; `Instance` and `IsKeyword` stay as facade members |
| `CharExtensions`' scanner members | `Broiler.JavaScript.Parser/CharExtensions.cs` | `FromCodePoint` :9, `HexValue` :17, `IsDigitPart` :40, `IsLineTerminator` :90 (all internal; the public class has no public member and stays as an obsolete facade) |
| The parser's half of `FastScope` | `Broiler.JavaScript.Parser/FastScope.cs`, `FastScopeItem.cs` | `FastScope.Push` :12; `FastScopeItem`'s `Variables` :9, `annexBFunctionNames` :17, `varDeclaredNames` :27, `MarkVarDeclaredName` :33, `HasVarDeclaredName` :44, `AddAnnexBName` :51, `GetAnnexBNames` :66, `HasLexicalBinding` :82, `DeclaresVariable` :91, `AddVariable` :94 (with its refusals, the sites of EE0040, EE0062, EE0168, EE0169 and EG0004), `GetVariables` :237. Nothing outside the parser reads `FastScope`: the compiler reads `HoistingScope`, a tree fact the bridge synthesises from the library's side tables (decision D44). Both public types stay as obsolete facades |
| `RegExpValidator` | `Broiler.JavaScript.Parser/RegExpValidator.cs` | the whole class: `IsValid` :18 and its helpers to `NormalizeUnicodePropertyEscapes` :964; deleted if SJL-11.5 has landed, else relocated behind the request's regular-expression provider (decision D51) |
| `DecodeBracedUnicodeEscapes` | `FastScanner.cs:1048`, called at `:1502` | deleted, or relocated behind the provider if SJL-11.5 has not landed (decision D51; K6) |
| The parenthesis-balance patch | `FastScanner.cs:1463-1492` ("BROILER-PATCH: Validate parentheses balance in regex pattern") | the same as the line above |
| The division fallback | `FastScanner.cs`, `ReadCommentsOrRegExOrSymbol` :1222 (the speculative regular-expression scan that falls back to division, `:1230-1281`, `:1340-1356`) | deleted (K5; JSD-0081's row S9) |
| The two token re-scans and the direct-eval prefix (decision D41, as JSD-0083 revised it) | `Broiler.JavaScript.Compiler/SyntaxValidation.cs` and `DirectEvalSupport.cs` | `SyntaxValidation.ContainsLegacyOctalLiteral` :369 (called at :35); `DirectEvalSupport.ContainsStrictReservedWordUsage` :743 (called at :704); the `"use strict";\n` prefix at `DirectEvalSupport.cs:127-128` with the two places that skip the prefixed statement, `TryEvaluateInertProgram` :451 (its `injectedStrictDirective` parameter, called at :147) and `TryGetSingleStatement` :561 (called at :530). `FastCompiler.EarlyErrors`' own `ContainsLegacyOctalLiteral` (a function's strictness, over the tree) is not a re-scan and stays until SJL-15.5 |
| The test-only switch | `JSCompilationOptions`' internal member and the shell's hidden flag (decision D43) | added at W1, removed at W2 |
| What remains of `NumberCoercion` | none | deleted at SJL-3.5 (Broiler.VM's note 14-a); no declaration or reference remains at `33d3093b` but a comment in `Broiler.JavaScript.Integration.Tests/Issue751Tests.cs:13` |

**Not deleted at SJL-14:** `SyntaxValidation` but for its re-scan, `FastCompiler.EarlyErrors`, `FastFunctionScope`
and `FreeNameScan`, which SJL-15.5 deletes when the compiler moves to the shared tree (decision D41); and the `Ast`
and `Parser` packages themselves, which stay as obsolete adapter packages until `1.0.0`.
