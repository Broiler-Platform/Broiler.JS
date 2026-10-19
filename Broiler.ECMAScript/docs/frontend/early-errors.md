# The early-error catalogue

[← The shared frontend: design](design.md) · [Conventions](../conventions.md) ·
[This repository's plan](../../../docs/roadmap/SharedLibraries.md)

**Filed:** 2026-10-19, at milestone SJL-12.3 (task 3.1, the skeleton). **Status:** skeleton. Every row and its shared
code are here; the columns read from the engines are `unfilled`, and tasks 3.2 to 3.10 fill them.

**What it is.** [`early-errors.tsv`](early-errors.tsv) has one row per early-error rule of ECMA-262's ES2026 edition,
with four families of its own beside them (decision D30 of Broiler.VM's JSD-0080, its home decision D31). For each
row it will say where Broiler.VM's slice path and wide path check the rule today, where Broiler.JS checks it, the
shared code and the message identifier the shared front end (SJL-13) emits for it, and a test262 test. Milestone
SJL-12.3's done-when is that every early error has a row; the checks in both directions (task 3.11) are committed
with the filled catalogue in `docs/evidence/sjl-12-001/catalogue/`.

**What it is not.** The mapping from shared codes onto Broiler.VM's registry is Broiler.VM's,
`src/Broiler.VM.Profile.JavaScript/docs/diagnostics/frontend-mapping.tsv`, which names this file by commit and
SHA-256. Nothing here changes either engine.

**The tool.** [`scripts/frontend/ee_extract.py`](../../../scripts/frontend/ee_extract.py) writes every identifying
column (section 4) from the specification's source; its header names each input by URL, revision and SHA-256 and
says how to re-run it. At this file's first commit, `write` reproduces it byte for byte; after the fill, `check`
compares the identifying columns.

## 1. The edition, and the errata check

| Input | Revision | SHA-256 of `spec.html` | Read for |
|---|---|---|---|
| ES2026, the tag `es2026` | `tc39/ecma262@0248456c758431e4bb8e5d26333ff1865123c9cd` | `ce7bc30174061fd8d212270b81cf6511661180c1e174f6911d10ced0581527b0` (2,978,793 bytes) | Families EE, EA, EG |
| `main`, its head on 2026-10-19 | `tc39/ecma262@4feb0272b799ea795c5eadee9b482aea0c10ce45` | `1f7935840bf249dbbfb32cc4d083114cea48bab4c513b7d7bed00387a3ebe89e` (3,090,376 bytes) | EP0001-EP0008 only |
| `es2026-errata` (2026-07-28) | `tc39/ecma262@d89c03f2db8a597bc915b363a6518d0cc8acdbc0` | `032ecc740356b38f6846338d8c44536291a134a6ce60e651f935befb239cfc1a` (2,979,557 bytes) | The errata check |

- **The edition is the one both repositories already pin.** Broiler.VM archives the file as
  `src/Broiler.VM.Profile.JavaScript/docs/specification/ecma-262-es2026-spec.html` (its JSD-0019 and rule N14); this
  repository names it in [`conformance/vectors/sources/ecma262.pin`](../../../conformance/vectors/sources/ecma262.pin)
  and archives no copy. The catalogue follows the pin file: no specification text is committed here.
- **The errata change no rule.** `ee_extract.py diff` over the errata tag, run on 2026-10-19: 213 rules on each side,
  none added, removed or changed, none renumbered. The edition stays the pin, as Broiler.VM's JSD-0019 decided.
- **What the next edition will change**, from the same `diff` over `main` (not a pin; recorded so that a re-pin is
  a known difference): `main` changes seven rules of the edition, EE0018 (the `__proto__` rule's productions),
  EE0020 to EE0024 (the five template rules become three) and EE0121 (a variable renamed), and adds eight, the
  explicit-resource-management rules that are EP0001 to EP0008 here.
- **The extraction checks itself.** Each of the 204 occurrences of "Syntax Error" in the Early Errors clauses' own
  text (notes aside) lies inside a rule the tool extracted: 195 rules begin "It is a Syntax Error" or "It is an
  early Syntax Error", 8 say it later in the rule (EE0031 twice), 6 are "must cover" rules and 4 apply other rules.
  Every content key is unique.

## 2. Families

| Family | Rows | Codes | What a row is | Text from |
|---|---|---|---|---|
| **EE** | EE0001-EE0213 | by clause | One top-level item of a "Static Semantics: Early Errors" list of the edition, in document order: 41 clauses, 213 rules, 323 production-rule pairs. EE0209-EE0213 are Annex B's (B.1.2.1) | `rule` |
| **EA** | EA0001-EA0012 | 19, 20 | A step that throws a SyntaxError in eval or in a dynamic function: PerformEval 11.b and 11.e-h, EvalDeclarationInstantiation 3.a.i.1, 3.d.i.2.a.i.1 (with Annex B.3.4), 3.d.i.2.a.ii.1 and 7, CreateDynamicFunction 18, 20 and 24 | `step` |
| **EG** | EG0001-EG0008 | 81, 83 | One Annex B syntax feature: the grammar-only extensions (B.1.1 HTML-like comments, B.1.2 the pattern grammar, B.3.3 function declarations in `if` clauses, B.3.5 initialisers in `for`-`in` heads) and the relaxations Annex B lets a parser make (B.3.1 labelled function declarations, B.3.2 block-level functions, B.3.4 `var` in `catch` blocks, B.3.9 call expressions as assignment targets) | `prose` |
| **EP** | EP0001-EP0014 | 71, 72, 73 | A proposal's rule or construct that test262 `ccaac100` tests or either engine parses: explicit resource management (EP0001-EP0008, the eight rules `main` has merged), decorators (EP0009, EP0010), import phases (EP0011-EP0014: source-phase imports and `import defer`) | `rule`, `catalogue` |
| **EX** | EX0001-EX0024 | 91, 92, 93 | A refusal no early-error rule of the edition makes: source text no production matches (EX0001-EX0010), an engine's limits (EX0011-EX0016), an engine's policies (EX0017-EX0024) | `catalogue` |

- **EA covers decision D30's "eval and dynamic functions".** Steps that throw a SyntaxError elsewhere are not rows,
  and the reason is recorded here: GlobalDeclarationInstantiation (16.1.7) and a module's InitializeEnvironment
  (16.2.1.7.3.1) and InnerModuleLoading (16.2.1.6.1.1.1) run against a realm or a module graph, not a parse; ParseText,
  ParseScript and ParseModule (11.1.6, 16.1.5, 16.2.1.7.1) carry the grammar and the early errors that are already
  rows; the RegExp constructor, `JSON.parse`, `JSON.rawJSON` and the base64 and hex decoders are run-time; B.3.6 to
  B.3.8 are not syntax; Annex C restates strict-mode rules that are rows. If the fill or the completeness check
  (3.11) finds an engine that refuses one of these at compile time, it is added as a row (section 3).
- **EP rows are marked admitted or not per engine** (`vm_admits`, `js_admits`). The explicit-resource-management
  rows quote `main`. Broiler.VM's JSD-0034 pins the proposal itself at `38c13295`, whose Script-goal rule is worded
  by enclosing productions and which has no rule for `case` and `default` clauses; test262 `ccaac100`'s tests of
  `using` in those clauses follow the later wording, which `main` has. The fill records which wording each engine
  implements. Decorators and import phases have no text in `main`; their rows name the
  proposal's default branch read on 2026-10-19 (decorators `24882c7b`, source-phase imports `360899f1`, `import
  defer` `b9eb1429`), the production where the proposal's source has one, and the construct in the catalogue's own
  words. test262 `ccaac100` has parse-negative tests for explicit resource management (61), source-phase imports
  (128) and `import defer` (71), and positive syntax tests only for decorators.
- **EX rows exist so that every refusal site maps to a row** (task 3.11's direction from the engines). The
  specification reports source text that matches no production only as a failure to parse; how an engine divides
  those failures, and how it words them, is its own, so they are EX rows like the limits and the policies. There
  is one row per kind of refusal. EX0001 to EX0010 correspond one to one to Broiler.VM's grammar codes 2001 to
  2007, 2101, 2102 and 2401, so each of those codes has one engine-only row that can explain it where no rule of
  the edition reaches it (part E of Broiler.VM's rule).

## 3. Row identity

- **An id is permanent.** `EE`, `EA`, `EG`, `EP` or `EX` and four digits, never reused, never renumbered. Ids are
  assigned in the order of the tool's tables: EE in document order, EA and EG in document order, EP by proposal,
  EX by block.
- **A row's content key is its clause, its productions and `rule_sha10`**: the anchor without its revision prefix
  (the part after `#`; for a row with no specification text, the whole anchor), the `productions` cell, and the
  first ten hexadecimal digits of the SHA-256 of `rule_text` in UTF-8. Content keys are unique. The section number
  and the ordinal are not part of the key: they move when the document does.
- **`rule_text` is the specification's text, normalised**: white space collapsed; a cross-reference kept as
  `[xref#target]`; a superscript written `^` (EE0021 and EE0183 read `2^32`); grammar productions and inline
  algorithms kept as their source text; for a step, the step with each enclosing step, each prefixed by its rendered
  number (`11. Perform ... 11.e. If ...`).
- **A re-pin is a run of the tool over the new edition.** A row whose content key is unchanged keeps its id and its
  code; only its anchor's revision prefix and its section may change. A row whose key changes (its text, its
  productions or its clause) is withdrawn: it stays, with status `withdrawn` and its successor's id in `notes`, and
  the successor takes the family's next id. The successor keeps the withdrawn row's shared code when the change
  refuses the same source texts (main's EE0121 is an example), and takes its block's next code when it does not; a
  dated note here records which. When an edition absorbs a proposal, its EP rows keep their ids and codes: the
  family records where a row came from, not where the rule now lives.
- **Rows are added only in the tool.** A row the fill or the completeness check finds missing is proposed in the
  agent's scratch file with its anchor and its text; the catalogue's committer adds it to the tool's tables, where
  it takes its family's next id and its block's next code, and regenerates the identifying columns.

## 4. Columns

Tab-separated, one header line, LF line endings, no quoting; a cell never holds a tab or a line break. `-` means
"does not apply"; `unfilled` means "not yet read". Columns 1-11 identify the row and are written by the tool; columns
12-23 are the fill's.

| # | Column | Content |
|---|---|---|
| 1 | `id` | Section 3 |
| 2 | `family` | `EE`, `EA`, `EG`, `EP` or `EX` |
| 3 | `anchor` | `<source>@<revision>#<clause id>`: `es2026@0248456c#...`, `ecma262@4feb0272#...`, or a proposal repository at a commit; `-` for EX |
| 4 | `section` | The clause's number in that source (`13.15.1`, `B.3.4`); `-` without one |
| 5 | `productions` | The grammar the rule applies to, one right-hand side per production joined by ` ;; `; `-` without one |
| 6 | `ordinal` | EE and EP rules: the rule's place among the rules of the same productions (1, 2, ...); EA: the step number; EG: the paragraph of the clause (`p1`); `-` otherwise |
| 7 | `text_from` | `rule` (a list item of an Early Errors clause), `step` (an algorithm step), `prose` (a paragraph), `catalogue` (written here) |
| 8 | `rule_sha10` | Section 3 |
| 9 | `annexb` | The Annex B clauses the text refers to, `;`-separated; `annex-b` when the row's own clause is in Annex B; `-` |
| 10 | `rule_text` | Section 3 |
| 11 | `shared_code` | Section 5 |
| 12 | `message_id` | Section 6 |
| 13 | `vm_slice` | Where Broiler.VM's slice path checks the rule: `file:line` under `src/Broiler.VM.Profile.JavaScript.Compiler/`; `manifest` when the slice refuses the construct before the rule can apply (`SliceStaticSemantics.cs:607`, code 2104); or `none` with the search that found nothing |
| 14 | `vm_wide_stage` | `parser`, `lowering`, `parser+lowering` or `none` |
| 15 | `vm_wide_site` | `file:line` under the same folder, `; `-separated; `none` with the search |
| 16 | `vm_code` | The registry code Broiler.VM emits (2001-2406), `;`-separated if more than one; `none` if it accepts |
| 17 | `js_stage` | `scanner`, `parser`, `SyntaxValidation`, `FastCompiler.EarlyErrors`, `Visit*` or `DirectEvalSupport`, `+`-joined; `none` |
| 18 | `js_site` | `file:line` under `Broiler.JS/` (`Broiler.JavaScript.Parser/FastParser.Statement.cs:481`), `; `-separated; `none` with the search |
| 19 | `vm_admits` | EG and EP rows: `yes` or `no`, whether Broiler.VM admits the feature or the construct; `-` for the other families |
| 20 | `js_admits` | The same for Broiler.JS |
| 21 | `test262` | At least one path under `test/` at test262 `ccaac100`, `; `-separated; `none` with the search when test262 tests nothing for the row |
| 22 | `status` | `both`, `vm-only`, `js-only` or `neither` (which engines refuse what the row refuses); `delegates` for a row that refuses nothing itself (EE0070, EE0097, EE0111 and EE0147 apply UniqueFormalParameters' rules); `withdrawn` (section 3); `unfilled` until filled |
| 23 | `notes` | Free text: an Annex B relaxation's site, a second stage, a difference between the engines; `-` |

**The heads the sites are read at** (and cited by `file:line` at): Broiler.VM `d82b07a`, whose product sources are
`main` `046357b`; Broiler.JS `ec494f66`, whose product sources are `main` `1f91b8f1`. Every site is opened, not
guessed: a site that cannot be found is `none` with the search that was run.

## 5. Shared codes

**Form:** `JSE` and four digits, `JSEbbnn`. `bb` is the row's block, `nn` its place in the block, from `01`, in row
order. A code is permanent and names one row's refusal; it is never reused.

| Block | Codes now | Rows | Group |
|---|---|---|---|
| `12` clause 12, ECMAScript Language: Lexical Grammar | JSE1201-JSE1204 | EE0001-EE0004 | the clause of the anchor |
| `13` clause 13, Expressions | JSE1301-JSE1335 | EE0005-EE0039 | |
| `14` clause 14, Statements and Declarations | JSE1401-JSE1428 | EE0040-EE0067 | |
| `15` clause 15, Functions and Classes | JSE1501-JSE1592 | EE0068-EE0159 | |
| `16` clause 16, Scripts and Modules | JSE1601-JSE1623 | EE0160-EE0182 | |
| `19` clause 19, the global object's `eval` | JSE1901-JSE1909 | EA0001-EA0009 | |
| `20` clause 20, the Function constructor | JSE2001-JSE2003 | EA0010-EA0012 | |
| `22` clause 22, RegExp | JSE2201-JSE2226 | EE0183-EE0208 | |
| `81` Annex B.1, additional syntax | JSE8101-JSE8107 | EE0209-EE0213, EG0001, EG0002 | the Annex B clause |
| `83` Annex B.3, other additional features | JSE8301-JSE8306 | EG0003-EG0008 | |
| `71` explicit resource management | JSE7101-JSE7108 | EP0001-EP0008 | the proposal |
| `72` decorators | JSE7201-JSE7202 | EP0009, EP0010 | |
| `73` import phases | JSE7301-JSE7304 | EP0011-EP0014 | |
| `91` grammar failures | JSE9101-JSE9110 | EX0001-EX0010 | the kind of refusal |
| `92` limits | JSE9201-JSE9206 | EX0011-EX0016 | |
| `93` policies | JSE9301-JSE9308 | EX0017-EX0024 | |

- **Every row has its code now**, and the fill never makes one: a row added later takes its block's next code in
  the tool (section 3). The blocks 50 to 59 are kept free: a block that fills continues in the first of them, by a
  dated note here (clause 15 has seven codes left).
- **A code is not a registry code.** `JSE2201` is the first RegExp rule; Broiler.VM's `2201` is
  DuplicateLexicalDeclaration. A shared code is always written with its prefix, and Broiler.VM's mapping says which
  registry code each shared code becomes.
- **One code per row, one identifier per condition.** Rows stating one condition for different productions (the
  duplicate-lexical-names rule of a Block, a `switch`, a function body, a script, a module and a static block) have
  six codes and one message identifier. The code says which rule refused; the identifier says what to tell the
  author.

## 6. Message identifiers

**Form:** lower-case ASCII segments joined by `.`, two to four of them, each `[a-z][a-z0-9]*(-[a-z0-9]+)*`; for
example `scope.lexical.duplicate`. The first segment is one of these namespaces:

| Namespace | For |
|---|---|
| `identifier` | Identifier names and reserved words: escapes, reserved words, `yield` and `await` as names |
| `strict` | Restrictions that hold only in strict mode code: legacy octal literals and escapes, `with`, `delete` of a name, `eval` and `arguments` as bindings, strict reserved words |
| `scope` | Names declared twice, or declared in colliding ways |
| `declaration` | The forms of declarations: `const` without an initialiser, `let` as a lexically bound name |
| `assignment` | Assignment, update and destructuring targets |
| `cover` | A cover grammar that does not cover what it must |
| `expression` | Other expression rules: `CoverInitializedName`, a template in an optional chain, `delete` of a parenthesised name |
| `object` | Object literals: `__proto__`, methods |
| `template` | Template literals |
| `regexp` | Regular-expression literals, their flags and their patterns |
| `jump` | `break`, `continue` and labels |
| `statement` | Statements' own rules: labelled functions as bodies |
| `function` | Functions' parameters and directives |
| `super` | `super` outside where it may stand |
| `meta` | `new.target` and `import.meta` |
| `class` | Class bodies, elements, private names, initialisers |
| `module` | Imports, exports, the Module goal |
| `eval` | The eval steps (EA) not covered by the identifiers below |
| `dynamic-function` | CreateDynamicFunction's steps (EA) |
| `annexb` | Annex B features refused where they do not apply (EG) |
| `using`, `decorator`, `import-phase` | The proposals (EP) |
| `syntax`, `limit`, `engine` | EX's three blocks |

**Rules.**
1. **One identifier per condition.** Rows that state one condition share one identifier, whatever their productions,
   clause or family. The table below binds the conditions that recur across the fill's groups; a row it lists takes
   that identifier.
2. **An identifier names the condition**, never a production, a clause number, an engine, a stage or a registry code.
3. **A row that refuses nothing itself** (status `delegates`) takes the identifier of the rules it applies.
4. **An identifier is permanent.** It is never reused for another condition and never renamed once the catalogue
   merges; a condition worded differently later keeps its identifier.
5. **A new identifier is coined only inside the namespaces above**, by the fill agent, who writes it with its
   condition in its scratch file; the committer checks that no identifier in the table or already in the catalogue
   names the same condition before committing it. Arguments (the name, the label) are not part of an identifier;
   SJL-13 defines each identifier's arguments.

**Identifiers bound now.** The conditions that recur across the fill's groups, matched by the rule's wording, and a
few single-group conditions whose identifier the method was tried with; 159 rows in all with the `cover` rule
after the table:

| Identifier | Condition | Rows |
|---|---|---|
| `scope.lexical.duplicate` | The LexicallyDeclaredNames, or the BoundNames of a BindingList or ForDeclaration, contain a duplicate | EE0040, EE0043, EE0057, EE0062, EE0078, EE0133, EE0160, EE0168, EP0002, EP0005 |
| `scope.lexical.var-collision` | A lexically declared name is also var-declared in the same scope or loop body | EE0041, EE0051, EE0056, EE0063, EE0079, EE0134, EE0161, EE0169 |
| `scope.parameter.duplicate` | A parameter list binds a name twice where that is not allowed | EE0068, EE0069, EE0070, EE0090, EE0097, EE0111, EE0147 |
| `scope.parameter.lexical-collision` | A parameter name is also lexically declared in the body | EE0073, EE0086, EE0089, EE0092, EE0096, EE0100, EE0110, EE0114, EE0144, EE0149, EE0154, EE0158 |
| `scope.catch.duplicate` | A `catch` parameter binds a name twice | EE0065 |
| `scope.catch.lexical-collision` | A `catch` parameter name is lexically declared in the block | EE0066 |
| `scope.catch.var-collision` | A `catch` parameter name is var-declared in the block | EE0067 |
| `function.use-strict.non-simple-parameters` | A `"use strict"` directive in a function with a non-simple parameter list | EE0072, EE0085, EE0088, EE0091, EE0095, EE0099, EE0109, EE0113, EE0141, EE0145, EE0159 |
| `strict.binding.eval-or-arguments` | `eval` or `arguments` bound in strict mode code | EE0005, EE0071, EE0098, EE0112, EE0148 |
| `super.property.outside-method` | `super.x` or `super[x]` outside a method | EE0074, EE0075, EE0102, EE0103, EE0117, EE0118, EE0150, EE0151, EA0003 |
| `super.call.outside-derived-constructor` | `super(...)` outside a derived class's constructor | EE0015, EE0076, EE0077, EE0093, EE0104, EE0105, EE0106, EE0119, EE0120, EE0121, EE0124, EE0126, EE0131, EE0139, EE0142, EE0152, EE0153, EA0004 |
| `super.outside-method` | `super` in a script's or a module's own code | EE0162, EE0172 |
| `function.parameters.yield` | A `yield` expression in parameters | EE0083, EE0094, EE0101, EE0107, EE0115, EE0156 |
| `function.parameters.await` | An `await` expression in parameters | EE0084, EE0108, EE0116, EE0143, EE0146, EE0157 |
| `jump.label.duplicate` | A label nested inside a statement with the same label | EE0080, EE0135, EE0164, EE0174 |
| `jump.break.undefined-label` | `break` names a label that does not enclose it | EE0081, EE0136, EE0165, EE0175 |
| `jump.continue.undefined-label` | `continue` names a label that does not enclose an iteration | EE0082, EE0137, EE0166, EE0176 |
| `class.initializer.arguments` | `arguments` in a field initialiser or a static block | EE0130, EE0138, EA0005 |
| `class.private-name.undeclared` | A private name no enclosing class declares | EE0167, EE0177, EA0009 |
| `meta.new-target.outside-function` | `new.target` outside a non-arrow function | EE0163, EE0173, EA0002 |
| `statement.labelled-function` | A labelled function as the body of `if`, a loop or `with` | EE0045, EE0046, EE0047, EE0048, EE0049, EE0050, EE0052, EE0061 |
| `declaration.let.bound-name` | `let` as a lexically bound name | EE0042, EE0055, EP0001, EP0004 |
| `assignment.target.invalid` | The rule says AssignmentTargetType is ~invalid~ | EE0028, EE0029, EE0033, EE0034, EE0054 |
| `assignment.target.not-simple` | The rule says AssignmentTargetType is not ~simple~ | EE0035, EE0036, EE0039 |
| `cover.assignment-pattern` | An object or array literal target that does not cover an AssignmentPattern | EE0032, EE0038, EE0053 |
| `strict.legacy-octal-literal` | A legacy octal or non-octal decimal integer literal in strict mode code | EE0003 |
| `strict.legacy-octal-escape` | A legacy octal escape, `\8` or `\9` in strict mode code | EE0004 |
| `object.proto.duplicate` | Two `__proto__: value` entries in one object literal | EE0018 |
| `template.escape.invalid` | An escape a template literal does not define, in an untagged template | EE0020, EE0022, EE0023, EE0024 |
| `declaration.const.missing-initializer` | A `const` declaration without an initialiser | EE0044 |
| `jump.continue.no-iteration` | `continue` outside an iteration statement | EE0058 |
| `jump.break.no-target` | `break` outside an iteration statement or a `switch` | EE0059 |
| `strict.with` | `with` in strict mode code | EE0060 |
| `class.constructor.duplicate` | A class body with two constructors | EE0122 |
| `module.export.duplicate` | A module exporting one name twice | EE0170 |
| `module.import-attribute.duplicate-key` | An import attribute key given twice | EE0180 |

Every other "must cover" rule takes `cover.` and the covered production in kebab case: EE0025
`cover.parenthesized-expression`, EE0087 `cover.arrow-formal-parameters`, EE0155 `cover.async-arrow-head`, EP0003
`cover.await-using-declaration-head`.

## 7. The fill

Nine groups, one agent each, every row in exactly one group (tasks 3.2 to 3.10):

| Group | Rows | Count |
|---|---|---|
| 1 Lexical grammar and identifiers (12, 13.1) | EE0001-EE0014 | 14 |
| 2 Expressions (13.2-13.15) | EE0015-EE0039 | 25 |
| 3 Statements (14) | EE0040-EE0067 | 28 |
| 4 Functions (15.1-15.6, 15.8, 15.9) | EE0068-EE0120, EE0141-EE0159 | 72 |
| 5 Classes (15.7) | EE0121-EE0140 | 20 |
| 6 Scripts and modules (16) | EE0160-EE0182 | 23 |
| 7 RegExp (22.2.1.1, B.1.2.1) | EE0183-EE0213 | 31 |
| 8 Eval, dynamic functions and Annex B (EA, EG) | EA0001-EA0012, EG0001-EG0008 | 20 |
| 9 Proposals and engine-only refusals (EP, EX) | EP0001-EP0014, EX0001-EX0024 | 38 |

A fill agent writes its rows to its scratch file and never touches the catalogue; the committer merges them, and
`ee_extract.py check` then shows that no identifying column moved. Every site is read at the heads in section 4,
including any a survey of SJL-12 already named.

## 8. Re-running the tool

From the repository root, with the three inputs fetched as the tool's header shows:

```
python3 -I scripts/frontend/ee_extract.py check --edition es2026.html --later main.html \
    --catalogue Broiler.ECMAScript/docs/frontend/early-errors.tsv
python3 -I scripts/frontend/ee_extract.py diff --edition es2026.html --other errata.html --other-pin errata
```

`check` exits 1 when an identifying column differs; `diff` exits 1 when the other revision adds, removes or changes
a rule. On 2026-10-19 `check` found no difference and `diff` over the errata found no rule changed.
