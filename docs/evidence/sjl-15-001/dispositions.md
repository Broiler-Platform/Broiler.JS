# Bundle SJL-15-001 (Broiler.JS): SJL-15.5's deletion list, member by member, and the disposition of every public type it empties

**Written:** 2026-11-17, at SJL-15.1 (Broiler.JS's half), from the code at Broiler.JS
`6fda6ca44c041cea348dd5302c91ff90a0fe67bc`, before any change it describes exists. It is this engine's half of the
extraction gate's X6 for SJL-15 (JSD-0088's decision D-x2), which the Broiler.VM change completing JSD-0088's X2 and X6
copies by a dated note. The rule's part I searches the after commit for every implementation listed here. Paths are
relative to `Broiler.JS/`, and a line number is the line at `6fda6ca4`.

**What the list is.** SJL-15.5's work package names it: "the compiler's `FastFunctionScope`, `FreeNameScan`, the scope
half of `FastScope`; `SyntaxValidation`, `FastCompiler.EarlyErrors`, and the early-error checks in `Visit*` methods and
`DirectEvalSupport.Validate`", and clause 10, as revised at SJL-14.1 (decision D41), adds that "the compiler reads no
bridged tree". What each engine keeps is the milestone's table: this engine keeps its per-iteration `for` lowering,
numeric speculation, boxing and deferral, its expression-tree emission and its run-time evaluation support.

## The deletion list

| Member | Where (`6fda6ca4`) | What goes | What reads the model instead |
|---|---|---|---|
| `FastFunctionScope`'s scope analysis | `Broiler.JavaScript.Compiler/Scope/FastFunctionScope.cs:132` (the type), its binding table `variableScopeList` (`:306`) and the resolution over it: `GetVariable` (`:983`), `TryGetOwnVariable` (`:1005`), `TryResolveBinding` (`:1021`), `CreateVariable` (`:906`), `AddExternalVariable` (`:311`), `GetVisibleVariables` (`:670`), `GetWithFallbackVariables` (`:710`), `GetDirectEvalLexicalBindingNames` (`:741`), `GetImmediateVarEnvNames` (`:789`), `GetImmediateCatchParameterNames` (`:822`), `CreateEvalShadow` (`:1048`) and `EvalShadows`; the per-function facts `MentionsArguments` (`:355`), `CapturedByNestedFunctions` (`:325`), `CapturedByHoistedFunctions` (`:343`) and `HasNestedFunctions` (`:346`) where they are computed from the tree | The type, as a public type: obsolete (below); its scope and declaration facts are the model's | The model's scopes, declarations, resolution with its dynamic markers, and per-function facts (rows SC1, DE1, DE2, RS1, RS2, FF3 to FF5); the lowering keeps its own storage for each binding (closure boxes, numeric locals), its temporaries, labels and `this`, `super` and `new.target` expressions in an internal type of its own |
| `FastFunctionScope.VariableScope` | `Scope/FastFunctionScope.cs:134` | The nested public type, obsolete with its parent | The model's binding, beside the lowering's storage for it |
| `FreeNameScan` | `Declarations/FreeNameScan.cs:48`: `Free` (`:51`), `FreeInOrder` (`:73`), `Dynamic` (`:79`), `SelfNameReferenced` (`:92`), `ForProgram` (`:115`), `Of` (`:129`), and the private `Scope` (`:137`), `Walker` (`:143`) and `Hoister` (`:517`) | Its walk, whole; the public type is kept obsolete as a facade over the model's free names and captures, answering each member from them | The model's free names and captures (row FC1), which `DeferredCaptureLayout`, `DeferrableFunctions` and the compiler's capture predictions (`Declarations/FastCompiler.CreateFunction.cs:545`, `:712`, `:782`) read |
| The scope half of `FastScope` | `Broiler.JavaScript.Parser/FastScope.cs`, `FastScopeItem.cs` (`AddVariable` `:94`, `MarkVarDeclaredName` `:33`, `AddAnnexBName` `:51`, `GetAnnexBNames` `:66`, `GetVariables` `:237`), and the compiler's reads of the bridged tree's `HoistingScope` and `AnnexBFunctionNames` (`Statements/FastCompiler.VisitProgram.cs:283`, `Statements/FastCompiler.VisitBlock.cs:33`, `Declarations/FastCompiler.CreateFunction.cs:389`, `ForLoopPreparation.cs:1003`) | The parser's half leaves at SJL-14's W2 (decision D50: `FastScope` and `FastScopeItem` are facade-obsolete there); at SJL-15.5 the compiler stops reading the two side tables from the tree | The model's declarations and Annex B.3.3 alias decisions (rows DE1, DE2, AB1) |
| `SyntaxValidation` | `Broiler.JavaScript.Compiler/SyntaxValidation.cs:11`, internal, whole: `ValidateProgram` (`:21`), the module checks (`:75`, `:124`), `ContainsDirectEvalVarConflict` (`:454`), `VarDeclaredNamesCollector` (`:235`), `ControlFlowValidator` (`:491`), `StrictModeValidator` (`:702`) with `PrivateNameUsage` (`:1299`), and the token re-scan `ContainsLegacyOctalLiteral`, which decision D41 already deletes at SJL-14's cut-over | The type, whole; no public disposition, since it is internal | The model's early errors (rows EE1, LB1, PN1, MR1, DE3) |
| `FastCompiler.EarlyErrors` | `Broiler.JavaScript.Compiler/FastCompiler.EarlyErrors.cs`: `ValidateFunctionEarlyErrors` (`:13`), `HasSimpleParameterList` (`:34`), `ParameterNamesContain` (`:49`), `CollectParameterNames` (`:62`), `CollectBindingNames` (`:69`), `ContainsLegacyOctalLiteral` (`:99`), `TryGetLegacyOctalToken` (`:128`) and `LegacyOctalLiteralDetector` (`:106`), all private | The partial file, whole; no public disposition | The model's per-function facts (strictness, simplicity: rows FF1, FF2) and early errors |
| The early-error checks in `Visit*` | `Expressions/FastCompiler.VisitCallExpression.cs:397` (a super call outside a derived constructor), `Expressions/FastCompiler.VisitMemberExpression.cs:25` (a super property outside a method), `Expressions/FastCompiler.VisitIdentifier.cs:344` (`arguments` in a field initialiser), `Statements/FastCompiler.Module.cs:147` (an export of no declared binding), as the stage table's `js_site` names them, and every other `throw new FastParseException` of a `Visit*` method for a row the stage table assigns `semantics` | The checks, line by line; the methods stay, since they lower | The model's early errors (rows EE1, MR1) |
| `DirectEvalSupport.Validate` and its checks | `Broiler.JavaScript.Compiler/DirectEvalSupport.cs`: `Validate` (`:706`), `ValidateDirectEvalEarlyErrors` (`:248`), `DirectEvalEarlyErrorValidator` (`:265`), `IsRestrictedDeclaration` (`:827`), `ContainsRestrictedDeclarator` (`:844`), the `ContainsRestrictedBinding` overloads (`:856`, `:869`, `:881`), `IsRestrictedName` (`:893`), all private, and the re-scan `ContainsStrictReservedWordUsage` (`:771`), which decision D41 already deletes at SJL-14's cut-over | The private members; the public type stays (below) | The library's parse of eval code with the inherited context, and the model's early errors over it (rows FF6, PN1, EE1, DE3) |
| The bridged tree | `Broiler.JavaScript.Parser/Bridge/`, as the compiler reads it through the parser at SJL-14 (decision D41) | The compiler's every read of it | The shared tree; the bridge remains only as the implementation of the obsolete `Ast` and `Parser` adapter packages, until their removal version |

## The disposition of every public type SJL-15.5 empties (SJL-D-25)

Under the owner's decision D-js-dispositions of 2026-11-17 (`docs/roadmap/SharedLibraries.md`, "Shared static semantics
(SJL-15)"), taken as recommended: **SJL-D-25 (b), obsolete with the removal version `1.0.0`**, with the diagnostic
identifier and the repository root's `NoWarn` that decision D50 gave `Ast` and `Parser` at SJL-14. *Not taken:* (c),
removal with a recorded consumer check, since no consumer check of these types is recorded.

| Public type | Package | Disposition | Removal | What the obsolete type does until then |
|---|---|---|---|---|
| `Broiler.JavaScript.Compiler.FastFunctionScope` | `Broiler.JavaScript.Compiler` | obsolete | `1.0.0` | Nothing new: the compiler stops using it, and its members answer as at the base for a caller that builds one; the lowering's state moves to an internal type |
| `Broiler.JavaScript.Compiler.FastFunctionScope.VariableScope` | `Broiler.JavaScript.Compiler` | obsolete | `1.0.0` | As its parent |
| `Broiler.JavaScript.Compiler.FreeNameScan` | `Broiler.JavaScript.Compiler` | obsolete | `1.0.0` | A facade over the model's free names and captures, each public member answering as today |
| `Broiler.JavaScript.Parser.FastScope`, `Broiler.JavaScript.Parser.FastScopeItem` | `Broiler.JavaScript.Parser` | facade-obsolete since SJL-14's W2 (decision D50, bundle `sjl-14-001`'s `dispositions.md`) | `1.0.0` | Unchanged by SJL-15.5 |

**The public types SJL-15.5 changes and does not empty**, which keep their public surface: `DirectEvalSupport`
(`Execute`, `:103`, and `ValidateIndirectEval`, `:698`, which `Broiler.JavaScript.Globals/JSGlobal.cs:132` calls, its
early errors then the library's), `LoopScope` (`Scope/LoopScope.cs:6`, the lowering's loop labels), and the two public
types `Scope/FastFunctionScope.cs` also declares, `SharedParserStringMap<T>` (`:33`) and `PrivateInstanceElement`
(`:24`), which hold no scope fact and stay where SJL-15.5 puts them. The compiler's internal and private types are
deleted without a disposition.

**Consumers.** At `6fda6ca4`, outside the compiler and its tests, `FreeNameScan` is named by
`Broiler.JavaScript.Expressions/DeferredCaptureLayout.cs` (in documentation comments) and by
`benchmarks/Broiler.JavaScript.Engine.Benchmarks/CompilePhaseMetrics.cs:360` (a call); `FastFunctionScope` by none.
The rule's part I lists every reference at the after commit.
