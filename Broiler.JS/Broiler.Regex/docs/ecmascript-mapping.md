# ECMA-262 §22.2 → Broiler.Regex mapping

How each piece of the ECMAScript regular-expression specification maps onto the
code in this project. Section numbers refer to ECMA-262 (2024+).

## §22.2.1 Patterns (grammar) → `Parsing/RegexParser.cs` + `Ast/`

| Grammar production | Parser method | AST node |
|--------------------|---------------|----------|
| `Disjunction` `A│B` | `ParseDisjunction` | `DisjunctionNode` |
| `Alternative` (concatenation) | `ParseAlternative` | `SequenceNode` / `EmptyNode` |
| `Term` | `ParseTerm` | — |
| `Assertion` `^ $ \b \B` | `TryParseAssertion` | `AnchorNode` |
| `Assertion` look-around | `TryParseAssertion` | `LookaroundNode` |
| `Quantifier` `* + ? {n,m}` | `TryApplyQuantifier` | `QuantifierNode` |
| `Atom` PatternCharacter | `ParseAtom` | `CharNode` |
| `Atom` `.` | `ParseAtom` | `AnyCharNode` |
| `Atom` `( … )` / `(?: … )` / `(?<name> … )` | `ParseGroup` | `GroupNode` |
| `Atom` `(?ims-ims: … )` | `ParseModifierGroup` | `ModifierGroupNode` |
| `CharacterClass` `[ … ]` | `ParseCharacterClass` | `CharClassNode` + `CharSet` |
| `AtomEscape` back-reference | `ParseAtomEscape` | `BackreferenceNode` |
| `CharacterClassEscape` `\d \w \s …` | `TryReadClassEscape` | `CharClassNode` / `CharSet` |
| `CharacterEscape` `\n \xHH \uHHHH \u{…} \cX` | `ReadCharacterEscape` | `CharNode` |

## §22.2.2 Pattern semantics (the matcher) → `Matching/Matcher.cs`

The spec defines a `Matcher` as a function of a **State** and a **Continuation**.
Broiler.Regex models these directly:

| Spec concept | Code |
|--------------|------|
| State (§22.2.2.1) | `MatchState` (end index + capture spans) |
| Continuation | `Continuation` delegate |
| Matcher | `CompiledMatcher` delegate |
| Direction (forward / backward) | `Matcher.Direction` |
| §22.2.2.3.1 RepeatMatcher | `CompileQuantifier` (incl. empty-match guard) |
| §22.2.2.3.4 capturing group | `CompileGroup` |
| §22.2.2.4 Assertions / look-around | `CompileAnchor`, `CompileLookaround` |
| §22.2.2.7 AtomEscape (character) | `CompileChar` |
| §22.2.2.9 BackreferenceMatcher | `CompileBackreference` |
| §22.2.2.9.4 Canonicalize | `Unicode/CaseFolding.cs` |
| §22.2.2.10 CharacterSetMatcher | `CompileCharClass` / `CharSet.Contains` |

### Why direction matters (look-behind)

§22.2.2.4 specifies that the body of a look-behind is evaluated with
`direction = −1`, and §22.2.2.3 composes the terms of an `Alternative` in
*reverse* under that direction. `CompileSequence` reverses its matcher list when
`dir == Backward`, and `ReadCodePoint` walks backward. Capturing groups still
store `[min, max]` (`CompileGroup`), so captures read left-to-right regardless of
match direction. This is the structural reason Broiler.Regex gets
look-behind + back-reference cases right where a reversed-capture engine cannot.

### Why the empty-match guard matters (nullable quantifiers)

§22.2.2.3.1 step 2.b returns failure when a `min = 0` repetition's body matched
without advancing (`y.endIndex == x.endIndex`). `CompileQuantifier`'s `d`
continuation implements exactly this check, so a nullable quantifier neither
loops forever nor terminates one iteration too early.

## §22.2.6 Flags → `RegexFlags.cs`

`d g i m s u v y` parse to `RegexFlags`; `i`/`m`/`s` are the only flags an inline
modifier group may toggle (`ModifierGroupNode`).

## Not yet mapped (see README "Known limitations")

- §22.2.1 `CharacterClassEscape :: p{…}` Unicode property escapes
  (`Unicode/UnicodeCharSets.ResolveProperty` — stub).
- §22.2.1 `v`-mode `ClassSetExpression` set operations and `\q{…}`
  (`CharSet.UsesSetOperations` — parsed, not evaluated).
- The complete §22.2.2.9.4 case-fold table (current coverage: ASCII + a
  documented subset of non-ASCII simple folds).
