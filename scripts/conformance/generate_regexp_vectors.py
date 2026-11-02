#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""Write the regular-expression area of the conformance corpus (corpus revision 31, milestone SJL-11.3).

    python3 -I scripts/conformance/generate_regexp_vectors.py [--out conformance/vectors/regexp]
    python3 -I scripts/conformance/generate_regexp_vectors.py --rows

THE AREA (decision D25 of Broiler.VM's JSD-0081, schema 1 unchanged). Every expected value is
scripts/conformance/regexpref.py's (`ref:regexpref@1`): ECMA-262 22.2 at the pinned edition (ES2026 at 0248456c,
sources/ecma262.pin) with Annex B's B.1.2, transliterated, over UCD 17.0.0 read through unicoderef.py
(sources/unicode.pin). No expected value is read from an engine, from V8 or from any other implementation. A vector
whose outcome the reference does not compute (an object-model step of 22.2.6 or of String.prototype.search, which the
reference does not model) cites the clauses that decide it alone, and says so in its note.

THE OPS. A vector's `op` and `in` decide its outcome, since they are all a library-level runner reads (corpus
revision 8): Broiler.Regex's runner (its bundle sjl-11-001's rule, part D) maps each op to the library and lists by
name each op of the object model it does not implement.
- `RegExpInitialize` - in: str:pattern str:flags. out: `value: bool:true` when RegExpInitialize (with ParsePattern)
  accepts, `error: SyntaxError` when it throws. script: `new RegExp(pattern, flags) instanceof RegExp`.
  `RegExpInitialize, nested pattern` - in: str:opener int:count str:middle str:closer str:flags, the pattern being
  the opener count times, the middle, and the closer count times.
- `IsValidRegularExpressionLiteral` - in: str:BodyText str:FlagText, both printable ASCII. out: `value: bool:true`
  when the literal is valid, `error: SyntaxError` when 13.2.7.1's early error refuses it. script: the literal inside a
  function that is never called, `(function () { return /body/flags; }), true`, so that an engine that refuses it only
  when it is evaluated completes with true.
- `RegExpBuiltinExec` - in: str:pattern str:flags str:subject int:lastIndex. out: the canonical serialisation of the
  result (below), computed by the script's `$x` from the engine's own result object.
- `RegExpBuiltinExec, repeated inputs` - in: str:pattern-piece int:count str:pattern-tail str:flags str:subject-piece
  int:count str:subject-tail int:lastIndex, the pattern and subject being each piece repeated count times and then
  the tail; out: the serialisation `$n` writes, which holds lengths instead of substrings.
- `RegExpBuiltinExec, step budget` - in: str:pattern str:flags str:subject-piece int:count str:subject-tail
  int:lastIndex int:budget, the subject being the piece repeated count times and then the tail. A library-level
  vector: the library
  runs the exec with a step ceiling of `budget` (its own units) and no meter. out: `status: exhausted` when
  regexpref's count of the specification's Matcher invocations (regexpref.py's docstring defines one step) reaches at
  least 100 times the budget, the margin of decision D25 and of Broiler.Regex's tests/Adversarial/README.md section
  3; or the serialisation of the answer when the count is at most a hundredth of the budget. No budget vector is
  written between the two. Engine-level runners skip them (SCHEMA.md section 4, the note of corpus revision 31).
- `RegExp.prototype.test` - in: str:pattern str:flags str:subject. out: `value: bool:...`.
- `RegExp.prototype [ %Symbol.match% ]`, `[ %Symbol.split% ]`, `[ %Symbol.search% ]` - in: str:pattern str:flags
  str:subject. out: the serialisation `$a` writes of the array (or null), or the index.
- `get RegExp.prototype.flags` - in: str:flags. `RegExp.escape` - in: str:string.
- The object-model ops, which the library does not implement: `String.prototype.search` with a changed
  %Symbol.search%, `get RegExp.prototype.flags` over an ordinary object, and the invariants of EscapeRegExpPattern
  (`source`) that its text fixes; each is named in its file's header.
- `RegularExpressionLiteral, eval code` - in: str:code str:call (`direct` or `indirect`), the eval code holding a
  RegularExpressionLiteral (row S9b). out: `error: SyntaxError` when the literal's own grammar refuses the code. The
  syntax area's ops, ParseScript and PerformEval, are that area's alone.

THE SERIALISATION. One canonical script, CANONICAL_JS below, renders a result from the engine's own result object, and
SERIALISE writes the same text from regexpref's: `lastIndex=<n>` (the property after the call), then `null`, or
`index=<i>;length=<n+1>`, each element `k=<len>:<text>` (or `u` for undefined), `groups=` (`u`, or the count and each
`<name>=` in the object's property order), and `indices=` (`u`, or each `k=<start>-<end>`, then `igroups=`). Lengths
are in code units, so no element's text can be mistaken for a separator.

THE CLASSES. `normative` where the specification decides the outcome. Three kinds of vector are `impl-defined`, each
engine's `per-engine` expectation taken from its record, never from a run:
- a pattern without u and v whose outcome holds only with B.1.2 (the reference run with annex_b=False, the main
  grammar alone, answers otherwise): Annex B is required of a web-browser host and optional for any other, and both
  engines answer B.1.2's patterns (Broiler.VM's JSD-0081, requirement R5, "Annex B syntax only where the specification
  allows it", with bundle sjl-11-001's correspondence, rows S4 and S7, where both engines give B.1.2's answers), so
  each engine's expectation is B.1.2's outcome, which `out` also holds;
- a pattern nested past the depth decision D7 of JSD-0081 sets for both engines, 512 counted as Broiler.VM counts it
  (Broiler.Regex's tests/Adversarial/README.md section 2): the specification sets no bound and `out` holds its answer;
  each engine's expectation is the nesting limit's SyntaxError (D7; row S3, "engine policy that becomes a parameter");
- the empty match from a lastIndex inside a surrogate pair under u (row M4), which the text leaves ill-formed
  (regexpref raises SpecAssertion) and decision D41 classes implementation-defined: each engine's expectation is its
  answer as D41 records it, and `out` lists both (SCHEMA.md section 4's dated note on such a case).
No vector carries a `divergence` field at revision 31: each engine's first run on revision 31 is classified at
revision 32.

THE DIVERGENCES (corpus revision 32). scripts/conformance/regexp-divergences.txt, which this generator reads, holds
each engine's classified differences: a `reason <key>: <text>` line, which names the row of bundle sjl-11-001's
correspondence whose answer the engine gives, and `<engine> <key> <id>` lines, each writing the field
`divergence.<engine>: <text>` on that vector. A normative vector becomes `divergence`; an impl-defined one stays
impl-defined, the field saying the engine does not meet its own `per-engine` expectation yet (SCHEMA.md section 5).
No expected value is read from the file. *(Added 2026-11-01, at revision 32; until then the paragraph above was the
whole of this section's rule, and the generator read no other file.)*

THE ROWS. Each vector's note names the row of bundle sjl-11-001's correspondence (Broiler.VM's
docs/evidence/sjl-11-001/correspondence.md) it reaches, where it reaches one; `--rows` prints, for every row, the
vectors that reach it, and REACHED lists the rows with a behavioural difference that must each be reached (or say why
not, in NOT_REACHED).

The generator is deterministic and reads nothing but this file, regexpref.py, the pinned UCD (through unicoderef)
and regexp-divergences.txt: the same inputs write the same bytes.
"""

from __future__ import annotations

import argparse
import math
import struct
import sys
from dataclasses import dataclass, field, replace
from functools import lru_cache
from pathlib import Path
from typing import Sequence

SCRIPTS = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPTS))

import regexpref as R  # noqa: E402

REPOSITORY = SCRIPTS.parents[1]
CORPUS = REPOSITORY / "conformance" / "vectors"
DEFAULT_OUT = CORPUS / "regexp"
SINCE = 31             # the corpus revision that added the area
CORPUS_REVISION = 34   # the corpus revision this generator writes: REVISIONS.md's last row
DIVERGENCES = SCRIPTS / "regexp-divergences.txt"
GENERATOR = "generate_regexp_vectors.py"
REF = "ref:regexpref@1"
SPEC = "spec:es2026@0248456c#"
UCD = "ucd:17.0.0/"
SE = "error: SyntaxError"
TRUE = "value: bool:true"
BUDGET_MARGIN = 100

FILES = {
    "syntax": "acceptance and refusal through RegExpInitialize: the flags, the grammar of 22.2.1 under u, under v and "
              "without either (with B.1.2's), every early error, group names and their escapes, duplicate names, "
              "property names, the v flag's class syntax, modifiers and the nesting policy",
    "literals": "regular-expression literals whose pattern or flags 13.2.7.1 refuses early, read where the literal is "
                "never evaluated, beside literals it admits, and eval code whose literal its own grammar refuses",
    "matching": "RegExpBuiltinExec's results, with captures, groups and d indices: the notes and examples of 22.2.2, "
                "back-references, look-around in both directions, modifiers, duplicate named groups, the v flag's "
                "strings, escapes and line terminators, and long inputs",
    "case-insensitive": "the i flag without u (toUppercase), with u and with v (simple case folding): what "
                        "Canonicalize reads, which no host culture moves (Broiler.JS's bundle sjl-11-001, part D)",
    "properties": "property escapes and properties of strings over UCD 17.0.0, and the classes \\s \\d \\w",
    "last-index": "lastIndex, g and y: the start inside a surrogate pair, sticky attempts, and the global protocols' "
                  "steps (match, split, search)",
    "budget": "library-level vectors: an exec under a step budget, exhausted at a hundredfold margin of regexpref's "
              "count or completing within a hundredth of it (engine-level runners skip status vectors)",
    "text": "the text helpers and the flags: RegExp.escape, the flags getter, the invariants of source that "
            "EscapeRegExpPattern fixes, and String.prototype.search's protocol",
}

# ---- Records the impl-defined vectors cite --------------------------------------------------------------------------

ANNEX_B = ("Annex B is required of a web-browser host and optional for any other; both engines answer B.1.2's patterns "
           "(Broiler.VM's JSD-0081, requirement R5, Annex B syntax where the specification allows it, and bundle "
           "sjl-11-001's correspondence, rows S4 and S7, same in both), so each engine's expectation is B.1.2's outcome")
NESTING = ("the specification sets no nesting bound; JSD-0081's decision D7 has both engines pass Broiler.Regex a "
           "nesting limit of 512, counted as Broiler.VM counts it (the pattern's Disjunction 1, each group and each "
           "v-mode class 1 more), and refuse past it with a SyntaxError, so each engine's expectation is that "
           "SyntaxError (row S3, engine policy that becomes a parameter)")
D41 = ("the empty match from a lastIndex inside a surrogate pair under u: RegExpBuiltinExec's Match Record would start "
       "at 1 and end at 0, which GetMatchString's assertion forbids, so the text has no answer and regexpref gives "
       "none; JSD-0081's decision D41 classes the case implementation-defined until the text is corrected, and "
       "records each engine's answer, Broiler.VM index 0 and lastIndex 0, Broiler.JS index 1 and lastIndex 1, which "
       "are the per-engine expectations; out lists both. Revision 32: Broiler.JS's answer is index 0 and lastIndex 0, "
       "Broiler.VM's, since its u patterns went to Broiler.Regex 0.1.0-preview.5 (a58d31d1), as D41's dated note of "
       "2026-11-01 (owner's decision D-p5) records, so its per-engine expectation is that answer; out still lists "
       "both answers D41 has recorded")
NESTING_LIMIT = 512

# ---- Strings and typed values ---------------------------------------------------------------------------------------


class GeneratorError(RuntimeError):
    """The generator cannot write the area, and says why."""


def units(text: str) -> list[int]:
    """A Python str as UTF-16 code units: a character above U+FFFF is its surrogate pair, and a lone surrogate
    written into the str stays one unit."""
    out: list[int] = []
    for ch in text:
        out.extend(R.code_point_units(ord(ch)))
    return out


def ustr(us: Sequence[int]) -> str:
    """Code units as a str of one character per unit."""
    return "".join(chr(u) for u in us)


def quote(text: str) -> str:
    """A String in the schema's notation: printable ASCII but `"` and `\\`, and \\uXXXX for every other code unit."""
    out = ['"']
    for unit in units(text):
        if unit == 0x22:
            out.append('\\"')
        elif unit == 0x5C:
            out.append("\\\\")
        elif 0x20 <= unit <= 0x7E:
            out.append(chr(unit))
        else:
            out.append(f"\\u{unit:04X}")
    out.append('"')
    return "".join(out)


def js(text: str) -> str:
    """A JavaScript string literal of one line of printable ASCII."""
    out = ['"']
    for unit in units(text):
        if unit == 0x22:
            out.append('\\"')
        elif unit == 0x5C:
            out.append("\\\\")
        elif 0x20 <= unit <= 0x7E:
            out.append(chr(unit))
        else:
            out.append(f"\\u{unit:04x}")
    out.append('"')
    return "".join(out)


def s_(text: str) -> str:
    return "str:" + quote(text)


def i_(n: int) -> str:
    return f"int:{n}"


def f64(value: float) -> str:
    raw = struct.unpack(">Q", struct.pack(">d", float(value)))[0]
    if math.isnan(float(value)):
        raw = 0x7FF8000000000000
    return f"f64:0x{raw:016X}"


def value_str(text: str) -> str:
    return "value: " + s_(text)


def printable(text: str) -> bool:
    return all(0x20 <= u <= 0x7E for u in units(text))


# ---- The canonical serialisation ------------------------------------------------------------------------------------

CANONICAL_JS = (
    'function $s(v) { return v === undefined ? "u" : v.length + ":" + v; } '
    'function $p(v) { return v === undefined ? "u" : v[0] + "-" + v[1]; } '
    'function $g(o, f) { if (o === undefined) return "u"; var k = Object.keys(o), t = "" + k.length; '
    'for (var j = 0; j < k.length; j++) t += ";<" + k[j] + ">=" + f(o[k[j]]); return t; } '
    'function $x(r, m) { var t = "lastIndex=" + r.lastIndex; if (m === null) return t + ";null"; '
    't += ";index=" + m.index + ";length=" + m.length; for (var i = 0; i < m.length; i++) t += ";" + i + "=" + $s(m[i]); '
    't += ";groups=" + $g(m.groups, $s); if (m.indices === undefined) return t + ";indices=u"; '
    't += ";indices=" + m.indices.length; for (var j = 0; j < m.indices.length; j++) t += ";" + j + "=" + $p(m.indices[j]); '
    'return t + ";igroups=" + $g(m.indices.groups, $p); }'
)
LENGTHS_JS = (
    'function $n(r, m) { var t = "lastIndex=" + r.lastIndex; if (m === null) return t + ";null"; '
    't += ";index=" + m.index + ";length=" + m.length; '
    'for (var i = 0; i < m.length; i++) t += ";" + i + "=" + (m[i] === undefined ? "u" : m[i].length); return t; }'
)
ARRAY_JS = (
    'function $s(v) { return v === undefined ? "u" : v.length + ":" + v; } '
    'function $a(a) { if (a === null) return "null"; var t = "" + a.length; '
    'for (var i = 0; i < a.length; i++) t += ";" + $s(a[i]); return t; }'
)


def _s(subject: list[int], cap) -> str:
    if cap is None:
        return "u"
    a, b = cap
    return f"{b - a}:" + ustr(subject[a:b])


def _p(cap) -> str:
    return "u" if cap is None else f"{cap[0]}-{cap[1]}"


def _g(obj, f) -> str:
    if obj is None:
        return "u"
    text = str(len(obj))
    for name, value in obj:
        text += ";<" + ustr(name) + ">=" + f(value)
    return text


def serialise(last_index: int, result, subject: list[int]) -> str:
    """SERIALISE: what CANONICAL_JS's $x writes for regexpref's result."""
    text = f"lastIndex={last_index}"
    if result is None:
        return text + ";null"
    n = len(result.captures)
    text += f";index={result.index};length={n}"
    for k, cap in enumerate(result.captures):
        text += f";{k}=" + _s(subject, cap)
    text += ";groups=" + _g(result.groups, lambda cap: _s(subject, cap))
    if not result.has_indices:
        return text + ";indices=u"
    text += f";indices={n}"
    for k, cap in enumerate(result.captures):
        text += f";{k}=" + _p(cap)
    return text + ";igroups=" + _g(result.indices_groups, _p)


def serialise_lengths(last_index: int, result) -> str:
    """What LENGTHS_JS's $n writes."""
    text = f"lastIndex={last_index}"
    if result is None:
        return text + ";null"
    text += f";index={result.index};length={len(result.captures)}"
    for k, cap in enumerate(result.captures):
        text += f";{k}=" + ("u" if cap is None else str(cap[1] - cap[0]))
    return text


def serialise_array(items) -> str:
    """What ARRAY_JS's $a writes, for a list of code unit lists or None (undefined), or None (null)."""
    if items is None:
        return "null"
    text = str(len(items))
    for item in items:
        text += ";" + ("u" if item is None else f"{len(item)}:" + ustr(item))
    return text


# ---- The reference's answers ----------------------------------------------------------------------------------------


@lru_cache(maxsize=None)
def construct(pattern: str, flags: str, annex_b: bool = True):
    """RegExpInitialize: the RegExp, or the PatternSyntaxError it throws. A caller that runs it sets its lastIndex
    first."""
    try:
        return R.regexp_create(units(pattern), units(flags), annex_b=annex_b)
    except R.PatternSyntaxError as error:
        return error


@lru_cache(maxsize=None)
def parse(pattern: str, flags: str, annex_b: bool = True):
    """RegExpInitialize's flag check and ParsePattern: the parse, or the PatternSyntaxError RegExpInitialize throws."""
    try:
        return R.regexp_parse(units(pattern), units(flags), annex_b=annex_b)
    except R.PatternSyntaxError as error:
        return error


def outcome_construct(pattern: str, flags: str, annex_b: bool = True) -> str:
    return SE if isinstance(parse(pattern, flags, annex_b), R.PatternSyntaxError) else TRUE


def outcome_exec(pattern: str, flags: str, subject: str, last_index: int = 0, annex_b: bool = True) -> str:
    r = construct(pattern, flags, annex_b)
    if isinstance(r, R.PatternSyntaxError):
        return SE
    r.last_index = last_index
    s = units(subject)
    result = R.regexp_builtin_exec(r, s)
    return value_str(serialise(r.last_index, result, s))


def outcome_test(pattern: str, flags: str, subject: str, annex_b: bool = True) -> str:
    r = construct(pattern, flags, annex_b)
    if isinstance(r, R.PatternSyntaxError):
        return SE
    r.last_index = 0
    return "value: bool:true" if R.regexp_test(r, units(subject)) else "value: bool:false"


def outcome_literal(body: str, flags: str, annex_b: bool = True) -> str:
    return TRUE if R.is_valid_regular_expression_literal(units(body), units(flags), annex_b=annex_b) else SE


def pattern_features(pattern: str, flags: str) -> set[str]:
    """What a pattern's parse holds, for the clauses a vector cites."""
    parsed = parse(pattern, flags)
    if isinstance(parsed, R.PatternSyntaxError):
        return set()
    found: set[str] = set()
    if "i" in flags:
        found.add("ignore-case")
    if "d" in flags:
        found.add("indices")

    def walk(node) -> None:
        if node is None:
            return
        kind = type(node).__name__
        found.add(kind)
        if isinstance(node, R.ClassEscape) and node.kind in "pP":
            found.add("property")
            if node.strings:
                found.add("property-of-strings")
        if isinstance(node, R.Group) and node.name is not None:
            found.add("named")
        for attr in getattr(type(node), "__slots__", ()):
            value = getattr(node, attr, None)
            if isinstance(value, R.Node):
                walk(value)
            elif isinstance(value, (list, tuple)):
                for item in value:
                    if isinstance(item, R.Node):
                        walk(item)
                    elif isinstance(item, tuple):
                        for sub in item:
                            if isinstance(sub, R.Node):
                                walk(sub)

    walk(parsed.tree)
    return found


def exec_clauses(pattern: str, flags: str) -> list[str]:
    f = pattern_features(pattern, flags)
    out = ["sec-regexpbuiltinexec", "sec-compilepattern"]
    if "Disjunction" in f:
        out.append("sec-compilesubpattern")
    if "Quantified" in f:
        out.append("sec-runtime-semantics-repeatmatcher-abstract-operation")
    if "Assertion" in f:
        out.append("sec-compileassertion")
    if "LookAround" in f:
        out.append("sec-compileassertion")
    if "Backreference" in f:
        out.append("sec-backreference-matcher")
    if "Modifiers" in f:
        out.append("sec-updatemodifiers")
    if "ClassRanges" in f or "SetClass" in f:
        out.append("sec-compilecharacterclass")
        out.append("sec-compiletocharset")
    if "SetStrings" in f:
        out.append("sec-compileclasssetstring")
    if "property" in f:
        out.append("sec-runtime-semantics-unicodematchproperty-p")
    if "ignore-case" in f:
        out.append("sec-runtime-semantics-canonicalize-ch")
    if "indices" in f:
        out.append("sec-makematchindicesindexpairarray")
    if "u" in flags or "v" in flags:
        out.append("sec-getstringindex")
    return list(dict.fromkeys(out))


# ---- Cases ----------------------------------------------------------------------------------------------------------


@dataclass
class Case:
    file: str
    group: str
    op: str
    inputs: list[str]
    out: str
    script: str
    clauses: list[str]
    note: str
    rows: tuple[str, ...] = ()
    cls: str = "normative"
    per_engine: dict[str, str] = field(default_factory=dict)
    ucd: tuple[str, ...] = ()
    reference: bool = True


CASES: list[Case] = []


def add(case: Case) -> None:
    if case.file not in FILES:
        raise GeneratorError(f"no file {case.file}")
    if not printable(case.script) or "\n" in case.script:
        raise GeneratorError(f"{case.group}: a script is one line of printable ASCII: {case.script[:80]!r}")
    if not case.clauses:
        raise GeneratorError(f"{case.group}: no clause decides the vector")
    if not printable(case.note):
        raise GeneratorError(f"{case.group}: a note is one line of printable ASCII: {case.note[:80]!r}")
    CASES.append(case)


def note_of(rows: Sequence[str], text: str) -> str:
    head = ""
    if rows:
        head = ("correspondence row " if len(rows) == 1 else "correspondence rows ") + ", ".join(rows) + ": "
    return head + text


def _annex_b_class(case: Case, main_outcome: str, flags: str) -> None:
    """A pattern without u and v whose outcome holds only with B.1.2 is impl-defined, each engine's expectation the
    extension's outcome."""
    if "u" in flags or "v" in flags:
        return
    if main_outcome != case.out:
        case.cls = "impl-defined"
        case.per_engine = {"broiler-js": case.out, "broiler-vm": case.out}
        case.note += f"; the outcome holds with B.1.2 alone (without Annex B: {main_outcome}); " + ANNEX_B
        if "sec-regular-expressions-patterns" not in case.clauses:
            case.clauses.append("sec-regular-expressions-patterns")


def _nesting_class(case: Case, pattern: str, flags: str) -> None:
    parsed = parse(pattern, flags)
    if isinstance(parsed, R.PatternSyntaxError):
        return
    if parsed.depth > NESTING_LIMIT:
        case.cls = "impl-defined"
        case.per_engine = {"broiler-js": SE, "broiler-vm": SE}
        case.note += f"; nesting depth {parsed.depth}: " + NESTING


def syntax_clauses(pattern: str, flags: str, outcome: str) -> list[str]:
    out = ["sec-regexpinitialize", "sec-parsepattern"]
    r = parse(pattern, flags)
    if isinstance(r, R.PatternSyntaxError):
        if r.code.startswith("early:invalid-braced"):
            out.append("sec-patterns-static-semantics-early-errors-annexb")
        elif r.code.startswith("early:"):
            out.append("sec-patterns-static-semantics-early-errors")
        else:
            out.append("sec-patterns")
        if r.code.startswith("early:property") or r.code.startswith("early:lone-property") or \
                r.code.startswith("early:negated-property"):
            out.append("sec-runtime-semantics-unicodematchproperty-p")
            out.append("sec-runtime-semantics-unicodematchpropertyvalue-p-v")
        if r.code == "early:duplicate-group-name":
            out.append("sec-mightbothparticipate")
        if r.code == "early:undefined-group-name":
            out.append("sec-groupspecifiersthatmatch")
        if r.code in ("early:negated-class-with-strings", "early:negated-property-of-strings"):
            out.append("sec-static-semantics-maycontainstrings")
    else:
        out.append("sec-patterns")
        if "u" not in flags and "v" not in flags:
            out.append("sec-parsepattern-annexb")
    return out


def reason_of(pattern: str, flags: str) -> str:
    r = parse(pattern, flags)
    if isinstance(r, R.PatternSyntaxError):
        return f"refused ({r.code})"
    return "accepted"


def add_construct(file: str, group: str, pattern: str, flags: str, rows: Sequence[str] = (), text: str = "",
                  ucd: Sequence[str] = ()) -> None:
    outcome = outcome_construct(pattern, flags)
    main = outcome_construct(pattern, flags, annex_b=False)
    flags_shown = flags if printable(flags) else "(flags)"
    reason = reason_of(pattern, flags)
    case = Case(file, group, "RegExpInitialize", [s_(pattern), s_(flags)], outcome,
                f"new RegExp({js(pattern)}, {js(flags)}) instanceof RegExp",
                syntax_clauses(pattern, flags, outcome),
                note_of(rows, (text + "; " if text else "") + f"/{flags_shown}: {reason}"), tuple(rows), ucd=tuple(ucd))
    _annex_b_class(case, main, flags)
    _nesting_class(case, pattern, flags)
    add(case)


def add_nested(group: str, opener: str, count: int, middle: str, closer: str, flags: str, rows: Sequence[str],
               text: str) -> None:
    """RegExpInitialize of opener x count + middle + closer x count, the pattern given by its parts."""
    pattern = opener * count + middle + closer * count
    outcome = outcome_construct(pattern, flags)
    main = outcome_construct(pattern, flags, annex_b=False)
    case = Case("syntax", group, "RegExpInitialize, nested pattern",
                [s_(opener), i_(count), s_(middle), s_(closer), s_(flags)], outcome,
                f"new RegExp({js(opener)}.repeat({count}) + {js(middle)} + {js(closer)}.repeat({count}), {js(flags)}) "
                f"instanceof RegExp", syntax_clauses(pattern, flags, outcome),
                note_of(rows, text + f"; /{flags}: {reason_of(pattern, flags)}"), tuple(rows))
    _annex_b_class(case, main, flags)
    _nesting_class(case, pattern, flags)
    add(case)


def literal_safe(body: str) -> bool:
    if not body or not printable(body) or body[0] == "*" or "/" in body:
        return False
    trailing = len(body) - len(body.rstrip("\\"))
    return trailing % 2 == 0


def add_literal(group: str, body: str, flags: str, rows: Sequence[str] = (), text: str = "") -> None:
    if not literal_safe(body) or not printable(flags):
        raise GeneratorError(f"{group}: {body!r} is not a literal body this file writes")
    outcome = outcome_literal(body, flags)
    main = outcome_literal(body, flags, annex_b=False)
    clauses = ["sec-primary-expression-regular-expression-literals-static-semantics-early-errors",
               "sec-isvalidregularexpressionliteral", "sec-parsepattern"]
    r = parse(body, flags)
    if isinstance(r, R.PatternSyntaxError):
        clauses.append("sec-patterns-static-semantics-early-errors" if r.code.startswith("early:") else "sec-patterns")
    reason = reason_of(body, flags)
    case = Case("literals", group, "IsValidRegularExpressionLiteral", [s_(body), s_(flags)], outcome,
                f"(function () {{ return /{body}/{flags}; }}), true", clauses,
                note_of(rows, (text + "; " if text else "") + f"/{body}/{flags} never evaluated: {reason}"), tuple(rows))
    _annex_b_class(case, main, flags)
    _nesting_class(case, body, flags)
    add(case)


def add_exec(file: str, group: str, pattern: str, flags: str, subject: str, last_index: int = 0,
             rows: Sequence[str] = (), text: str = "", ucd: Sequence[str] = ()) -> None:
    outcome = outcome_exec(pattern, flags, subject, last_index)
    main = outcome_exec(pattern, flags, subject, last_index, annex_b=False)
    script = (CANONICAL_JS + f" var r = new RegExp({js(pattern)}, {js(flags)}); r.lastIndex = {last_index}; "
              f"$x(r, r.exec({js(subject)}))")
    case = Case(file, group, "RegExpBuiltinExec", [s_(pattern), s_(flags), s_(subject), i_(last_index)], outcome,
                script, exec_clauses(pattern, flags) if outcome != SE else syntax_clauses(pattern, flags, outcome),
                note_of(rows, text or "the exec's result"), tuple(rows), ucd=tuple(ucd))
    _annex_b_class(case, main, flags)
    _nesting_class(case, pattern, flags)
    add(case)


def add_test(file: str, group: str, pattern: str, flags: str, subject: str, rows: Sequence[str] = (),
             text: str = "", ucd: Sequence[str] = ()) -> None:
    outcome = outcome_test(pattern, flags, subject)
    main = outcome_test(pattern, flags, subject, annex_b=False)
    clauses = ["sec-regexp.prototype.test"] + exec_clauses(pattern, flags)
    case = Case(file, group, "RegExp.prototype.test", [s_(pattern), s_(flags), s_(subject)], outcome,
                f"new RegExp({js(pattern)}, {js(flags)}).test({js(subject)})", clauses,
                note_of(rows, text or "whether the pattern matches"), tuple(rows), ucd=tuple(ucd))
    _annex_b_class(case, main, flags)
    add(case)


def add_repeated(group: str, piece: str, count: int, tail: str, flags: str, s_piece: str, s_count: int,
                 s_tail: str, rows: Sequence[str], text: str, last_index: int = 0, file: str = "matching") -> None:
    pattern = units(piece) * count + units(tail)
    subject = units(s_piece) * s_count + units(s_tail)
    r = R.regexp_create(pattern, units(flags))
    r.last_index = last_index
    result = R.regexp_builtin_exec(r, subject)
    outcome = value_str(serialise_lengths(r.last_index, result))
    script = (LENGTHS_JS + f" var r = new RegExp({js(piece)}.repeat({count}) + {js(tail)}, {js(flags)}); "
              f"r.lastIndex = {last_index}; $n(r, r.exec({js(s_piece)}.repeat({s_count}) + {js(s_tail)}))")
    case = Case(file, group, "RegExpBuiltinExec, repeated inputs",
                [s_(piece), i_(count), s_(tail), s_(flags), s_(s_piece), i_(s_count), s_(s_tail), i_(last_index)],
                outcome, script, exec_clauses(piece * min(count, 2) + tail, flags), note_of(rows, text), tuple(rows))
    if r.parsed.depth > NESTING_LIMIT:
        raise GeneratorError(f"{group}: a repeated vector is not nested past the limit")
    add(case)


# ---- Files ----------------------------------------------------------------------------------------------------------


def build_syntax() -> None:
    f = "syntax"
    for flags in ("", "g", "dgimsy", "dgimsvy", "ygmisd", "gg", "x", "G", "uv", "vu", "yvsmigd", "dgimsuvy", " g",
                  "g ", "gi\u0000", "ii", "U"):
        add_construct(f, "flags", "a", flags, rows=("F1",), text="RegExpInitialize's flag check: d g i m s u v y, "
                                                                   "each once, and ParsePattern refuses u with v")
    # S1: duplicate named groups (ES2025).
    for p, flags in (("(?<a>x)|(?<a>y)", ""), ("(?<a>x)|(?<a>y)", "d"), ("(?<a>x)|(?<a>y)", "u"),
                     ("(?<a>x)|(?<a>y)", "v"), ("(?:(?<a>x)|(?<a>y))\\k<a>", ""), ("(?:(?<a>x)|(?<a>y))\\k<a>", "u"),
                     ("(?<a>x)(?<a>y)", ""), ("(?<a>x)(?<a>y)", "u"), ("(?<a>x)|(?:(?<a>y)|(?<a>z))", ""),
                     ("((?<a>x)|(?<a>y))(?<a>z)", ""), ("(?:(?<a>x)|(?<a>y))(?<a>z)", "u"),
                     ("(?<a>x)|(?<b>y)|(?<a>z)", ""), ("(?:(?<a>x)|(?<a>y))*", ""), ("(?<a>(?<a>x)|y)", ""),
                     ("(?<a>x)|((?<a>y)|(?<b>z))(?<b>w)", ""), ("(?<b>.)(?<a>.)|(?<a>.)(?<b>.)", ""),
                     ("(?=(?<a>x))|(?<a>y)", ""), ("(?<a>x)(?=(?<a>y))", "")):
        add_construct(f, "s1-duplicate-names", p, flags, rows=("S1",),
                      text="a name may repeat only across alternatives that cannot both participate (MightBothParticipate)")
    # S2, S2a: group names and their escapes.
    for p, flags in (("(?<a>x)", ""), ("(?<$>x)", ""), ("(?<_>x)", ""), ("(?<\u00e9>x)", ""), ("(?<a\u200cb>x)", ""),
                     ("(?<a\u200db>x)", "u"), ("(?<a b>x)", ""), ("(?<1>x)", ""), ("(?<a-b>x)", ""), ("(?<a.b>x)", ""),
                     ("(?<>x)", ""), ("(?<\\u0041>x)", ""), ("(?<\\u0041>x)", "u"), ("(?<\\u{41}>x)", ""),
                     ("(?<\\u{41}>x)", "u"), ("(?<\\uD835\\uDC00>x)", ""), ("(?<\\uD835\\uDC00>x)", "u"),
                     ("(?<\\u{1D400}>x)", ""), ("(?<\\u{1D400}>x)", "u"), ("(?<\ud835\udc00>x)", ""),
                     ("(?<\ud835\udc00>x)", "u"), ("(?<\\uD800>x)", ""), ("(?<\\uD800>x)", "u"), ("(?<\\u{D800}>x)", "u"),
                     ("(?<a\\u0020>x)", ""), ("(?<\\u0031>x)", ""), ("(?<a\\u0031>x)", ""), ("(?<\\x41>x)", ""),
                     ("(?<\ud835\udfda>x)", ""), ("(?<\ud835\udfda>x)", "u"), ("(?<a\ud835\udfda>x)", ""),
                     ("(?<\ud83e\udd8a>x)", ""), ("(?<\ud800>x)", ""), ("(?<\\u{110000}>x)", "u"),
                     ("(?<a\\u{200C}>x)", ""), ("(?<\\u200C>x)", ""), ("(?<\ua7ce>x)", ""), ("(?<\ua7ce>x)", "u")):
        add_construct(f, "s2-group-names", p, flags, rows=("S2",),
                      text="RegExpIdentifierName, its \\u escapes in every mode (sec-regexpidentifiercodepoint)",
                      ucd=("DerivedCoreProperties.txt",))
    for p, flags in (("(?<\\u0061>x)(?<a>y)", ""), ("(?<\\u0061>x)(?<a>y)", "u"), ("(?<\\u0061>x)|(?<a>y)", ""),
                     ("(?<\\u{61}>x)(?<a>y)", "v"), ("(?<a>x)(?<\\u0061>y)", "")):
        add_construct(f, "s2a-escaped-duplicates", p, flags, rows=("S2a",),
                      text="the duplicate test compares CapturingGroupName, the decoded name")
    for p, flags in (("(?<A>x)\\k<\\u0041>", ""), ("(?<A>x)\\k<\\u0041>", "u"),
                     ("(?<\\u{1D400}>x)\\k<\ud835\udc00>", ""), ("(?<\\u{1D400}>x)\\k<\ud835\udc00>", "u")):
        add_construct(f, "s2b-escaped-references", p, flags, rows=("S2b",),
                      text="a reference spelled with an escape names the group")
    # S3, S3b: nesting.
    for n in (510, 511, 512, 1000):
        for opener, closer, flags in (("(?:", ")", ""), ("(?:", ")", "u"), ("(", ")", ""), ("(?=", ")", ""),
                                      ("[", "]", "v")):
            add_nested("s3-nesting", opener, n, "a", closer, flags, ("S3",), f"{n} nested {opener}")
    for n, flags in ((20000, ""), (20000, "u"), (200000, "u")):
        add_nested("s3b-deep-nesting", "(?:", n, "a", ")", flags, ("S3b",), f"{n} nested (?:")
    # S4: escapes.
    for p, flags in (("\\0\u0661", "u"), ("\\0\u0661", ""), ("[\\0\u0661]", "u"), ("\\0\uff11", "u"), ("\\01", "u"),
                     ("\\01", ""), ("\\00", "u"), ("[\\00]", "u"), ("\\0", "u"), ("\\8", ""), ("\\8", "u"),
                     ("\\1(a)", ""), ("\\2(a)", ""), ("\\2(a)", "u"), ("\\x4", ""), ("\\x4", "u"), ("\\u12", ""),
                     ("\\u12", "u"), ("\\u{110000}", "u"), ("\\u{10FFFF}", "u"), ("\\u{3}", ""), ("\\u{3}", "u"),
                     ("\\c", ""), ("\\c", "u"), ("[\\c_]", ""), ("[\\c_]", "u"), ("[\\c1]", ""), ("\\cJ", "u"),
                     ("\\c1", ""), ("\\-", "u"), ("[\\-]", "u"), ("\\/", "u"), ("\\a", ""), ("\\a", "u"),
                     ("\\p", ""), ("\\p", "u"), ("\\e", ""), ("\\_", ""), ("\\$", "u"), ("[\\b]", "u"), ("\\B", "u"),
                     ("[\\B]", ""), ("[\\B]", "u"), ("[\\1]", ""), ("[\\1]", "u"), ("\\k", ""), ("\\k", "u"),
                     ("\\q", "v"), ("\\uD83D\\uDE00", "u"), ("\\u{0000000041}", "u"), ("\\u{}", "u")):
        add_construct(f, "s4-escapes", p, flags, rows=("S4",), text="AtomEscape, CharacterEscape and B.1.2's escapes")
    # S5: \k.
    for p, flags in (("\\k<b>(?<a>x)", "u"), ("\\k<b>(?<a>x)", ""), ("\\k<a>(?<a>x)", "u"), ("(?<a>x)\\k<a>", "u"),
                     ("\\k<a>", ""), ("\\k<a>", "u"), ("\\k", ""), ("(?<a>.)\\k", ""), ("(?<a>.)\\k<a", ""),
                     ("(?<a>.)\\k<>", ""), ("\\k<a>(?<b>x)", ""), ("[\\k<a>](?<a>x)", ""), ("[\\k<a>](?<a>x)", "u"),
                     ("[\\k](?<a>x)", ""), ("[\\k]", ""), ("(?<a>x)\\k<\\u0061>", "")):
        add_construct(f, "s5-named-references", p, flags, rows=("S5",),
                      text="AtomEscape :: k GroupName, GroupSpecifiersThatMatch, and \\k in a class, which no "
                           "production gives [NamedCaptureGroups] (B.1.2's ClassEscape reads ~NamedCaptureGroups, so "
                           "SourceCharacterIdentityEscape admits k; the main grammar's IdentityEscape refuses it)")
    # S6: property names.
    names = ["L", "Letter", "Lu", "Uppercase_Letter", "gc=Lu", "General_Category=Lu", "General_Category=Uppercase_Letter",
             "Script=Greek", "Script=Grek", "sc=Grek", "scx=Grek", "Script_Extensions=Greek",
             "Script=Katakana_Or_Hiragana", "sc=Hrkt", "scx=Hrkt", "ASCII", "Any", "Assigned", "ASCII_Hex_Digit",
             "AHex", "Lowercase", "Lower", "Emoji", "letter", "ascii", "Ascii", "A S C I I", "lu", "uppercaseletter",
             "uppercase-letter", "Uppercase-Letter", "general_category=Lu", "GC=Lu", "gc=lu", "Script=greek",
             "script=Greek", "Script = Greek", "ScriptExtensions=Greek", "scx:Greek", "Block=Basic_Latin",
             "InBasicLatin", "Is_Lowercase", "IsLowercase", "General_Category", "Script", "Lu=Lu", "Alphabetic=Yes",
             "ASCII=Y", "", "L&", "LC", "Cased_Letter", "Lowercase_Letter=Ll", "RGI_Emoji", "rgi_emoji",
             "Basic_Emoji", "Emoji_Keycap_Sequence", "sc=Zzzz", "sc=Unknown", "Script=Sidetic", "sc=Chisoi",
             "Script_Extensions=Unknown", "gc=Cn", "Cn", "Unassigned"]
    for name in names:
        for p, flags in (("\\p{" + name + "}", "u"), ("\\p{" + name + "}", "v"), ("[\\p{" + name + "}]", "u")):
            add_construct(f, "s6-property-names", p, flags, rows=("S6",),
                          text="names and values exactly as ECMA-262's tables and PropertyValueAliases.txt list them",
                          ucd=("PropertyAliases.txt", "PropertyValueAliases.txt"))
    for p, flags in (("\\P{RGI_Emoji}", "v"), ("[^\\p{RGI_Emoji}]", "v"), ("\\P{RGI_Emoji}", "u"),
                     ("\\p{RGI_Emoji}", "u"), ("[^[\\p{RGI_Emoji}]]", "v"), ("[^\\P{L}]", "v"),
                     ("[\\p{RGI_Emoji}--\\p{RGI_Emoji}]", "v"), ("[^[\\p{RGI_Emoji}--\\q{ab}]]", "v"),
                     ("[^[\\p{RGI_Emoji}&&\\p{L}]]", "v"), ("[^[\\p{L}&&\\p{RGI_Emoji}]]", "v")):
        add_construct(f, "s6-negated-strings", p, flags, rows=("S6",),
                      text="MayContainStrings refuses a negated class or \\P of a property of strings")
    # S7: B.1.2 and u mode.
    for p, flags in (("(?=a){2}", ""), ("(?=a){2}", "u"), ("(?<=a)*", ""), ("(?<=a)*", "u"), ("(?<=a)?", ""),
                     ("(?<!a){1}", ""), ("(?!a)+", ""), ("(?!a)+", "u"), ("{", ""), ("{", "u"), ("a{", ""), ("a{", "u"),
                     ("a{1", ""), ("a{1,", ""), ("x{2,1}", ""), ("x{2,1}", "u"), ("]", ""), ("]", "u"), ("}", ""),
                     ("}", "u"), ("{1}", ""), ("{1,}", ""), ("a{1}{2}", ""), ("^*", ""), ("$+", ""), ("\\b*", ""),
                     ("[\\d-a]", ""), ("[\\d-a]", "u"), ("[a-\\d]", ""), ("[a-\\d]", "u"), ("[--\\d]", ""),
                     ("[\\w-\\d]", ""), ("a**", ""), ("a{2147483648}", ""), ("a{9007199254740993}", ""), ("[b-a]", ""),
                     ("[b-a]", "u"), ("(?:)", ""), ("()", ""), ("[]", ""), ("[^]", ""), ("(", ""), (")", ""), ("a)", ""),
                     ("(?", ""), ("(?a)", ""), ("[", ""), ("\\", ""), ("\\", "u"), ("a|*", ""), ("|", ""),
                     ("(?<a", ""), ("(?<a>", ""), ("(?<a>x", ""), ("(?<=", ""), ("[a", ""), ("a{,5}", ""),
                     ("a{,5}", "u"), ("a{2,}", "u"), ("a{0}", "u"), ("a{1,1}?", "u")):
        add_construct(f, "s7-grammar-and-annex-b", p, flags, rows=("S7",),
                      text="the main grammar under u, B.1.2's without it: quantified assertions, braces, brackets, "
                           "class ranges with escapes")
    # Modifiers (F3, S11 through the constructor).
    for p, flags in (("(?i:a)b", ""), ("(?-i:a)b", "i"), ("(?ii:a)", ""), ("(?i-i:a)", ""), ("(?-:a)", ""),
                     ("(?i)a", ""), ("(?u:a)", ""), ("(?m-s:a)", ""), ("(?s:.)", ""), ("(?m:^b)", ""),
                     ("(?i:\\u212a)", "u"), ("(?imsi:a)", ""), ("(?I:a)", ""), ("(?M:a)", ""), ("(?S:a)", ""),
                     ("(?m-m:a)", ""), ("(?s-s:a)", ""), ("(?ims-m:a)", ""), ("(?m-ims:a)", ""), ("(?-ii:a)", ""),
                     ("(?-imsi:a)", ""), ("(?ii-:a)", ""), ("(?imsi-:a)", ""), ("(?I-:a)", ""), ("(?M-:a)", ""),
                     ("(?S-:a)", ""), ("(?-I:a)", ""), ("(?-M:a)", ""), ("(?-S:a)", ""), ("(?ms-i)", ""),
                     ("(?-s)", ""), ("(?i-)", ""), ("(?ims-:a)", ""), ("(?-ims:a)", "v"), ("(?i-ms:a)", "u"),
                     ("(?g:a)", ""), ("(?x:a)", "")):
        add_construct(f, "modifiers", p, flags, rows=("F3", "S11"),
                      text="Atom :: (? RegularExpressionModifiers : Disjunction ) and its early errors")
    # S8: the v flag's classes.
    for p, flags in (("[a&&&b]", "v"), ("[a--b&&c]", "v"), ("[(]", "v"), ("[a-\\d]", "v"), ("\\q{a}", "v"),
                     ("[\\q{a}]", "u"), ("[^\\q{ab}]", "v"), ("[^\\q{a}]", "v"), ("[\\q{abc|d}]", "v"), ("[\\q{}]", "v"),
                     ("[[a-z]--[aeiou]]", "v"), ("[\\w&&\\d]", "v"), ("[\\s--\\d]", "v"), ("[\\p{L}--\\p{Ll}]", "v"),
                     ("[a-z]", "v"), ("[|]", "v"), ("[a|]", "v"), ("[a\\|]", "v"), ("[!!]", "v"), ("[a!!b]", "v"),
                     ("[a\\!\\!b]", "v"), ("[a&&b--c]", "v"), ("[a&&[b]]", "v"), ("[a-z&&b]", "v"), ("[&&a]", "v"),
                     ("[a&&]", "v"), ("[a--]", "v"), ("[--a]", "v"), ("[a-]", "v"), ("[-a]", "v"), ("[\\-a]", "v"),
                     ("[a\\-z]", "v"), ("[z-a]", "v"), ("[\\q{a|bc}--\\q{bc}]", "v"), ("[a&b]", "v"), ("[&]", "v"),
                     ("[a&&b&&c]", "v"), ("[a--b--c]", "v"), ("[\\q{]", "v"), ("[\\q{a}", "v"), ("[\\q{(}]", "v"),
                     ("[\\q{\\(}]", "v"), ("[\\(]", "v"), ("[\\b]", "v"), ("[\\B]", "v"), ("[\\&]", "v"),
                     ("[\\%]", "v"), ("[\\~]", "v"), ("[\\.]", "v"), ("[\\q{a\\|b}]", "v"), ("[]", "v"), ("[^]", "v")):
        add_construct(f, "s8-unicode-sets", p, flags, rows=("S8",),
                      text="ClassSetExpression, ClassSetCharacter, the reserved double punctuators and \\q{}")
    for k, p in enumerate(("[(]", "[)]", "[[]", "[{]", "[}]", "[/]", "[-]", "[|]", "[&&]", "[!!]", "[##]", "[$$]",
                           "[%%]", "[**]", "[++]", "[,,]", "[..]", "[::]", "[;;]", "[<<]", "[==]", "[>>]", "[??]",
                           "[@@]", "[``]", "[~~]", "[^^^]", "[_^^]"), 1):
        add_construct(f, "s8-breaking-change", p, "v", rows=("S8",),
                      text=f"test262's built-ins/RegExp/prototype/unicodeSets/breaking-change-from-u-to-v-{k:02d}.js's "
                           f"pattern")
    # S10: the escapes the row tries.
    for p, flags in (("\\uD83D\\u{DC38}+", "u"), ("\\u{D83D}\\u{DC38}+", "u"), ("\\u{D83D}\\uDC38+", "u"),
                     ("\\uD83D\\uDC38+", "u"), ("\\uD83D\\u{3042}*", "u"), ("[\\uD83D\\u{3042}]*", "u"),
                     ("[\\uD83D\\u{DC38}]", "u"), ("\\uD83D\\u{DC38}", "v"), ("\\uD83D\\u{DC38}", "")):
        add_construct(f, "s10-lead-surrogate-escapes", p, flags, rows=("S10",),
                      text="a four-digit lead surrogate escape before a braced escape: only two four-digit escapes "
                           "pair")
    # Every early error, beside its nearest accepted pattern.
    early = [
        ("(?<a>x)(?<a>y)", "", "duplicate names in one alternative"), ("(?<a>x)|(?<a>y)", "", "across alternatives"),
        ("a{3,2}", "", "QuantifierPrefix {n,m}, n > m"), ("a{2,2}", "", "n = m"), ("a{3,2}", "u", "under u"),
        ("(?ii:a)", "", "a repeated modifier"), ("(?i-i:a)", "", "a modifier added and removed"),
        ("(?-:a)", "", "both modifiers empty"), ("(?i-s:a)", "", "valid modifiers"),
        ("\\k<x>(?<a>.)", "u", "GroupSpecifiersThatMatch empty"), ("\\2(a)", "u", "DecimalEscape past the count"),
        ("\\1(a)", "u", "DecimalEscape within the count"), ("(a)\\1", "", "a back-reference"),
        ("[\\w-a]", "u", "a class escape in a range"), ("[z-a]", "", "a range out of order"),
        ("[a-a]", "", "a one-character range"), ("[\\u{1F600}-\\u{1F601}]", "u", "an astral range"),
        ("[\\uDE00-\\uD83D]", "", "a range of code units out of order"),
        ("(?<\\u0030>x)", "", "an escape that is not IdentifierStartChar"),
        ("(?<a\\u002D>x)", "", "an escape that is not IdentifierPartChar"),
        ("(?<\ud83d\ude00>x)", "", "a surrogate pair that is not UnicodeIDStart"),
        ("(?<a\ud83d\ude00>x)", "", "a surrogate pair that is not UnicodeIDContinue"),
        ("\\p{Foo=Bar}", "u", "an unknown property name"), ("\\p{sc=Foo}", "u", "an unknown value"),
        ("\\p{Foo}", "u", "an unknown lone name"), ("\\p{Basic_Emoji}", "u", "a property of strings without v"),
        ("\\P{Basic_Emoji}", "v", "\\P of a property of strings"), ("[^\\q{abc}]", "v", "a negated class with a string"),
        ("[^[^\\q{abc}]]", "v", "a negated class in a negated class"), ("[\\q{z}-a]", "v", "\\q{} is no ClassSetCharacter"),
        ("[z-a]", "v", "a ClassSetRange out of order"), ("{2}", "", "B.1.2: an InvalidBracedQuantifier"),
        ("a|{2,}", "", "an InvalidBracedQuantifier after |"), ("({1,3})", "", "an InvalidBracedQuantifier in a group"),
    ]
    for p, flags, what in early:
        add_construct(f, "early-errors", p, flags, text=what)


def build_literals() -> None:
    g = "literals"
    for p in ("(?ii:a)", "(?i-i:a)", "(?-:a)", "(?i)a", "(?imsi:a)", "(?I:a)", "(?M:a)", "(?S:a)", "(?m-m:a)",
              "(?s-s:a)", "(?ims-m:a)", "(?m-ims:a)", "(?-ii:a)", "(?-imsi:a)", "(?ii-:a)", "(?imsi-:a)", "(?I-:a)",
              "(?M-:a)", "(?S-:a)", "(?-I:a)", "(?-M:a)", "(?-S:a)", "(?ms-i)", "(?-s)", "(?i-)", "(?u:a)",
              "(?i:a)", "(?-i:a)", "(?m-s:a)"):
        add_literal("s11-modifiers", p, "", rows=("S11", "F3"),
                    text="each invalid modifier is an early error of the pattern, reported before evaluation")
    for p in ("[a&&&b]", "[a--b&&c]", "[a-\\d]", "\\q{a}", "[a&&b]", "[a--b]", "[\\q{ab}]"):
        add_literal("s11-unicode-sets", p, "v", rows=("S11", "S8"),
                    text="a v-mode class the pattern grammar refuses, reported before evaluation")
    for p in ("(?<=a)*", "(?<=a)?", "(?<!a)*", "(?<!a){2}", "(?<=a){1,2}", "(?=a)*"):
        add_literal("s7-quantified-lookbehind", p, "", rows=("S7",),
                    text="QuantifiableAssertion is look-ahead only (B.1.2); test262's "
                         "language/literals/regexp/invalid-optional-lookbehind.js and its neighbours")
    for p in ("[(]", "[)]", "[[]", "[{]", "[}]", "[-]", "[|]", "[&&]", "[!!]", "[##]", "[$$]", "[%%]", "[**]", "[++]",
              "[,,]", "[..]", "[::]", "[;;]", "[<<]", "[==]", "[>>]", "[??]", "[@@]", "[``]", "[~~]", "[^^^]",
              "[_^^]", "[a|]", "[a!!b]"):
        add_literal("s8-breaking-change", p, "v", rows=("S8",),
                    text="the v flag's reserved syntax, a negative test of phase parse")
    for p, flags in (("(?<A>x)\\k<\\u0041>", ""), ("(?<A>x)\\k<\\u0041>", "u"), ("(?<a>x)\\k<\\u{61}>", "")):
        add_literal("s2b-escaped-reference", p, flags, rows=("S2b",),
                    text="the literal admits what the constructor admits")
    for p, flags in (("[\\k<a>](?<a>x)", ""), ("(?<a>x)(?<a>y)", ""), ("(?<\\u0061>x)(?<a>y)", ""),
                     ("\\k<b>(?<a>x)", ""), ("\\k<b>(?<a>x)", "u"), ("(?<a>x)|(?<a>y)", "")):
        add_literal("s1-s5-names", p, flags, rows=("S1", "S2a", "S5"), text="group names and references in a literal")
    for flags in ("gg", "uv", "x", "dgimsvy", "yvsmigd", "G", "ii", "dgimsy"):
        add_literal("f1-flags", "a", flags, rows=("F1",), text="a literal's FlagText")
    for n in (511, 512):
        add_literal("s3-nesting", "(?:" * n + "a" + ")" * n, "", rows=("S3",), text=f"{n} nested (?: in a literal")
    for p, flags in (("\\0\u0031", "u"), ("a{2,1}", ""), ("\\p{Letter}", "u"), ("\\p{letter}", "u"), ("\\8", "u"),
                     ("\\c", "u"), ("{", "u"), ("]", ""), ("\\u{110000}", "u")):
        if printable(p):
            add_literal("other-early-errors", p, flags, text="a literal refused or admitted as its pattern is")
    # Row S9b: a literal that ends in a backslash at the end of eval code (the front end's; outside SJL-11).
    # Its op is the area's own (the syntax area's ParseScript and PerformEval are that area's alone): the eval code and
    # whether the call is direct.
    for script, code, call, what in (
            ('(0, eval)("/a[bc\\\\")', "/a[bc\\", "indirect", "indirect"),
            ('eval("/a[bc\\\\")', "/a[bc\\", "direct", "direct"),
            ('(0, eval)("/\\\\\\n/")', "/\\\n/", "indirect", "a backslash before a line terminator, indirect")):
        add(Case("literals", "s9b-literal-grammar", "RegularExpressionLiteral, eval code", [s_(code), s_(call)], SE,
                 script,
                 ["sec-literals-regular-expression-literals", "sec-performeval"],
                 note_of(("S9b",), f"{what}: RegularExpressionLiteral's own grammar refuses the source, a SyntaxError; "
                                   "the expected value is read from the clauses, which regexpref does not model"),
                 ("S9b",), reference=False))


def build_matching() -> None:
    f = "matching"
    # The examples and notes of 22.2.2.
    notes = [
        ("a|ab", "", "abc", "sec-compilesubpattern"), ("((a)|(ab))((c)|(bc))", "", "abc", "sec-compilesubpattern"),
        ("a[a-z]{2,4}", "", "abcdefghi", "repeat"), ("a[a-z]{2,4}?", "", "abcdefghi", "repeat"),
        ("(aa|aabaac|ba|b|c)*", "", "aabaac", "repeat"), ("^(a+)\\1*,\\1+$", "", "aaaaaaaaaa,aaaaaaaaaaaaaaa", "gcd"),
        ("(z)((a+)?(b+)?(c))*", "", "zaacbbbcac", "repeat"), ("(a*)*", "", "b", "repeat"),
        ("(a*)b\\1+", "", "baaaac", "repeat"), ("(?=(a+))", "", "baaabac", "lookahead"),
        ("(?=(a+))a*b\\1", "", "baaabac", "lookahead"), ("(.*?)a(?!(a+)b\\2c)\\2(.*)", "", "baaabaac", "lookahead"),
    ]
    for p, flags, s, _ in notes:
        add_exec(f, "m1-notes", p, flags, s, rows=("M1",), text="an example of the notes of 22.2.2")
    for p, flags, s in (("((a)|b)+", "", "ab"), ("(a?b??)*", "", "ab"), ("(a*?)*?b", "", "aab"),
                        ("(?:a|ab)(?:c|bcd)(d*)", "", "abcd"), ("(a)|\\1b", "", "b"), ("\\1(a)", "", "aa"),
                        ("(?:(a)|b)\\1", "", "b"), ("(a\\1)", "", "aa"), ("(?<=(\\d+)(\\d+))$", "", "1053"),
                        ("(?<=\\1(a))b", "", "aab"), ("(?<=(?<a>\\w){3})f", "", "abcdef"), ("(?<!a)b", "", "ab"),
                        ("(?<!a)b", "", "cb"), ("(?<=^|,)\\w+", "g", "x,y"), ("(?=(a))a", "", "a"),
                        ("(?!(a))b", "", "b"), ("(a)?(?:b)?\\1c", "", "bc"), ("^.$", "", "\ud83d\ude00"),
                        ("^.$", "u", "\ud83d\ude00"), ("^..$", "", "\ud83d\ude00"), ("\\u{1F600}", "u", "\ud83d\ude00"),
                        ("[\ud83d\ude00]", "u", "\ud83d\ude00"), ("[\ud83d\ude00]", "", "\ud83d\ude00"),
                        ("\\uD83D", "u", "\ud83d\ude00"), ("\\uD83D", "", "\ud83d\ude00"),
                        ("\\uDE00", "u", "\ud83d\ude00"), ("\\uD83D\\uDE00", "u", "\ud83d\ude00"),
                        ("\\u{D83D}\\u{DE00}", "u", "\ud83d\ude00"), ("[^a]", "u", "\ud83d\ude00"),
                        ("\\S", "u", "\ud83d\ude00"), ("\\W", "u", "\ud83d\ude00"),
                        ("\ud83d\ude00{2}", "u", "\ud83d\ude00\ud83d\ude00"), ("\ud83d\ude00{2}", "", "\ud83d\ude00\ude00"),
                        ("(.)\\1", "u", "\ud83d\ude00\ud83d\ude00"), ("(.)\\1", "iu", "Aa"), ("\\b", "u", ""),
                        ("a{2,3}?", "", "aaa"), ("a{0}", "", "a"), ("(?:a|())*", "", "aa"), ("(a|)*", "", "aa"),
                        ("(?<a>a)|b", "d", "b"), ("(?<a>a)(?<b>b)?", "d", "a"), ("[\\d-z]+", "", "1-z"),
                        ("(?:)", "", ""), ("", "", "x"), ("a*?", "", "aaa"), ("(a+)+b", "", "aaab"),
                        ("(?:a?)*?b", "", "ab"), ("(a|b)*?c", "", "abc"), ("((a)|b)*", "", "ab"),
                        ("(a)(?:(b)|c)*", "", "abcb"), ("x*y+$", "", "xxyy"), ("[^]", "", "\n"), ("[]", "", "a")):
        add_exec(f, "m1-matching", p, flags, s, rows=("M1",), text="22.2.2's semantics")
    # S1: duplicate named groups in a match.
    for p, flags, s in (("(?<a>x)|(?<a>y)", "", "y"), ("(?<a>x)|(?<a>y)", "d", "y"), ("(?<a>x)|(?<a>y)", "u", "y"),
                        ("(?<a>x)|(?<a>y)", "v", "x"), ("(?<a>x)|(?<a>y)", "dv", "x"),
                        ("(?:(?<a>x)|(?<a>y))\\k<a>", "", "yy"), ("(?:(?<a>x)|(?<a>y))\\k<a>", "u", "xx"),
                        ("(?:(?<a>x)|(?<a>y))\\k<a>", "", "yx"), ("(?<a>x)|(?:(?<a>y)|(?<a>z))", "", "z"),
                        ("(?<a>x)|(?:(?<a>y)|(?<a>z))", "d", "z"), ("(?<b>.)(?<a>.)|(?<a>.)(?<b>.)", "", "pq"),
                        ("(?<b>.)(?<a>.)|(?<a>.)(?<b>.)", "d", "pq"), ("(?:(?<a>x)|(?<a>y))*", "d", "xy"),
                        ("(?:(?<a>x)|(?<a>y))*", "d", "yx"), ("(?<a>x)|(?<b>y)|(?<a>z)", "d", "z"),
                        ("(?:(?<a>x)|(?<a>y))\\k<a>(?<b>c)?", "d", "xxc"), ("(?<a>x)|(?<a>y)", "", "z"),
                        ("(?=(?<a>x))|(?<a>y)", "d", "y")):
        add_exec(f, "s1-duplicate-names", p, flags, s, rows=("S1",),
                 text="groups and indices.groups read the participating group; \\k reads it")
    for p, flags, s in (("(?<A>x)\\k<\\u0041>", "", "xx"), ("(?<\\u{1D400}>x)\\k<\ud835\udc00>", "", "xx"),
                        ("(?<\\u{1D400}>x)\\k<\ud835\udc00>", "u", "xx"), ("(?<\\u0041>x)", "d", "x"),
                        ("(?<\ud835\udc00>x)", "", "x"), ("(?<\ud835\udc00>x)", "u", "x"), ("(?<$>x)(?<_>y)", "", "xy")):
        add_exec(f, "s2-names", p, flags, s, rows=("S2", "S2b"), text="a name's code points are its property key")
    # S4: escapes.
    for p, flags, s in (("\\0\u0661", "u", "\u0000\u0661"), ("\\0\u0661", "", "\u0000\u0661"),
                        ("[\\0\u0661]", "u", "\u0661"), ("\\0\uff11", "u", "\u0000\uff11"), ("\\01", "", "\u0001"),
                        ("\\0", "u", "\u0000"), ("\\8", "", "8"), ("\\1(a)", "", "a"), ("\\2(a)", "", "\u0002a"),
                        ("\\x4", "", "x4"), ("\\u12", "", "u12"), ("\\u{3}", "", "uuu"), ("\\c", "", "\\c"),
                        ("[\\c_]", "", "\u001f"), ("[\\c]", "", "c"), ("[\\c]", "", "\\"), ("\\cJ", "u", "\n"),
                        ("[\\-]", "u", "-"), ("\\/", "u", "/"), ("(a)\\10", "", "a\u0008"), ("(a)\\18", "", "a\u00018"),
                        ("\\08", "", "\u00008"), ("\\012", "", "\n"), ("\\400", "", " 0"), ("\\377", "", "\u00ff"),
                        ("[\\012]", "", "\n"), ("[\\8]", "", "8"), ("\\k<a>", "", "k<a>"), ("\\k", "", "k"),
                        ("(?<a>x)\\k<a>", "", "xx"), ("[\\k](?<b>x)", "", "kx"), ("\\p{L}", "", "p{L}"),
                        ("\\a", "", "a"), ("[\\b]", "", "\u0008"), ("[\\b]", "u", "\u0008"),
                        ("\\uD83D\\uDE00", "", "\ud83d\ude00"), ("\\uD83D\\uDE00", "u", "\ud83d\ude00")):
        add_exec(f, "s4-escapes", p, flags, s, rows=("S4",), text="what each escape matches")
    # S5: the forward reference (a class with \k is in s4-escapes: the letter, under B.1.2).
    for p, flags, s in (("\\k<a>(?<a>x)", "u", "x"), ("(?<a>x)\\k<a>", "u", "xx"), ("\\k<a>(?<b>x)", "", "x")):
        add_exec(f, "s5-references", p, flags, s, rows=("S5",), text="a reference to a group not yet captured matches "
                                                                   "the empty string, or the reference is refused")
    # S10: lead surrogate escapes.
    for p, flags, s in (("\\uD83D\\u{DC38}+", "u", "\ud83d\udc38\udc38"), ("\\u{D83D}\\u{DC38}+", "u", "\ud83d\udc38\udc38"),
                        ("\\u{D83D}\\uDC38+", "u", "\ud83d\udc38\udc38"), ("\\uD83D\\uDC38+", "u", "\ud83d\udc38\udc38"),
                        ("\\uD83D\\u{3042}*", "u", "\ud83d"), ("\\uD83D\\u{3042}*", "u", "\ud83d\u3042\u3042"),
                        ("[\\uD83D\\u{3042}]*", "u", "\ud83d"), ("[\\uD83D\\u{3042}]*", "u", "\ud83d\u3042\u3042\ud83d"),
                        ("[\\uD83D\\u{DC38}]", "u", "\udc38"), ("[\\uD83D\\u{DC38}]", "u", "\ud83d"),
                        ("[\\uD83D\\u{DC38}]", "u", "\ud83d\udc38"), ("\\uD83D\\u{DC38}", "v", "\ud83d\udc38"),
                        ("\\uD83D\\u{DC38}", "", "\ud83du{DC38}"), ("\\uD83D\\u{DC38}", "", "\ud83d\udc38"),
                        ("[\\uD83D\\uDC38]", "u", "\ud83d\udc38")):
        add_exec(f, "s10-lead-surrogate-escapes", p, flags, s, rows=("S10",),
                 text="a braced escape never joins a pair, so each pattern holds a lone lead surrogate")
    # M3: line terminators.
    for s in ("a\nb", "a\rb", "a\u2028b", "a\u2029b", "a\r\nb", "a\u0085b"):
        for p, flags in (("^b", "m"), ("^b", "gm"), ("a$", "m"), ("a$", "gm"), ("^b", "mu"), ("a$", "mu"),
                         ("a.b", ""), ("a.b", "u"), ("a.b", "s"), ("(?m:^b)", ""), ("(?s:a.b)", ""), ("a.b", "v")):
            add_exec(f, "m3-line-terminators", p, flags, s, rows=("M3",),
                     text="LineTerminator is LF, CR, LS and PS: ^, $ under m, and . without s")
    for p, flags, s in (("a$", "", "a\n"), ("a$", "", "a\r"), ("a$", "u", "a\n"), ("a$", "g", "a\n"), ("^b", "", "a\nb"),
                        ("a$", "m", "a\n"), ("[^]$", "", "a\n"), ("$", "", "a\n"), ("^$", "m", "\n"), ("\\n$", "", "a\n"),
                        ("a(?=$)", "", "a\u2028")):
        add_exec(f, "m3-end-of-input", p, flags, s, rows=("M3",), text="$ without m is the input's end only")
    # M7: back-references across surrogate pairs.
    for p, flags, s in (("foo(.+)bar\\1", "u", "foo\ud834bar\ud834\udc00"), ("foo(.+)bar\\1", "u", "foo\ud834bar\ud834\ud834"),
                        ("foo(.+)bar\\1", "u", "foo\ud834bar\ud834A"), ("foo(.+)bar\\1", "u", "fooAbarA\udc00"),
                        ("foo(.+)bar\\1", "u", "foo\udc00bar\udc00\ud834"),
                        ("^(.+)\\1$", "u", "\udc00foobar\ud834\udc00foobar\ud834"),
                        ("foo(.+)bar\\1", "iu", "foo\ud834bar\ud834\udc00"),
                        ("foo(?<x>.+)bar\\k<x>", "u", "foo\ud834bar\ud834\udc00"),
                        ("(?<=\\1(.))b", "u", "\ud834\udc00\udc00b"), ("(?<=\\1(.))b", "", "\ud834\udc00\udc00b"),
                        ("foo(.+)bar\\1", "v", "foo\ud834bar\ud834\udc00"), ("foo(.+)bar\\1", "", "foo\ud834bar\ud834\udc00"),
                        ("(?<=(.)\\1)b", "u", "\ud834\udc00\ud834\udc00b"), ("(?<=\\1(.))b", "u", "\udc00\udc00b"),
                        ("(\ud834)\\1", "", "\ud834\ud834"), ("(\\ud834)\\1", "u", "\ud834\ud834\udc00")):
        add_exec(f, "m7-backreference-surrogates", p, flags, s, rows=("M7",),
                 text="under u the input is code points, and BackreferenceMatcher compares characters, forward and "
                      "backward")
    # Look-behind (direction).
    for p, flags, s in (("(?<=\\$)\\d+(\\.\\d*)?", "", "$10.53"), ("(?<!\\$)\\d+", "", "$10 20"), ("(?<=(a+))b", "", "aaab"),
                        ("(?<=(a+?))b", "", "aaab"), ("(?<=(a)(b))c", "", "abc"), ("(?<=a(?=b)b)c", "", "abc"),
                        ("(?<=\\b)a", "", " a"), ("(?<=^a)b", "m", "x\nab"), ("(?<=.)", "u", "\ud83d\ude00"),
                        ("(?<=\ud83d\ude00)x", "u", "\ud83d\ude00x"), ("(?<=\\ude00)x", "u", "\ud83d\ude00x"),
                        ("(?<=\\ude00)x", "", "\ud83d\ude00x"), ("(?<=(?<a>.))b", "d", "ab"), ("(?<=[a-c]{2})d", "", "bcd"),
                        ("(?<!(a)b)c", "", "abc"), ("(?<!(a)b)c", "", "xbc"), ("(?<=(\\w)+)$", "", "abc"),
                        ("(?<=(?:a|ab))c", "", "abc"), ("(?<=x|(y))z", "", "yz")):
        add_exec(f, "lookbehind", p, flags, s, rows=("M1",), text="CompileSubpattern ~backward~ inside (?<= and (?<!")
    # Modifiers.
    for p, flags, s in (("(?i:a)b", "", "Ab"), ("(?i:a)b", "", "AB"), ("(?-i:a)b", "i", "aB"), ("(?-i:a)b", "i", "AB"),
                        ("(?m-s:a)", "", "a"), ("(?s:.)", "", "\n"), ("(?m:^b)", "", "a\nb"), ("(?i:\\u212a)", "u", "k"),
                        ("(?i:\\u212a)", "", "k"), ("(?i:[a-z])", "", "Q"), ("(?i:\\w)", "u", "\u017f"),
                        ("(?i:\\b)", "u", "\u017f"), ("a(?i:b(?-i:c))", "", "aBc"), ("a(?i:b(?-i:c))", "", "aBC"),
                        ("(?s-i:.)", "i", "\n"), ("(?i:(a)\\1)", "", "aA"), ("(?i:a)\\1", "", "a"), ("(?m:$)", "", "\n"),
                        ("(?-m:^)b", "m", "a\nb"), ("(?ims:^A.)", "", "x\na\n"), ("(?<=(?i:a))b", "", "Ab")):
        add_exec(f, "modifiers", p, flags, s, rows=("F3",), text="UpdateModifiers' record compiles the group")
    # v mode.
    for p, flags, s in (("[\\q{abc|d}]", "v", "abc"), ("[\\q{}]", "v", ""), ("[\\q{}]", "v", "x"), ("[[a-z]--[aeiou]]", "v", "e"),
                        ("[[a-z]--[aeiou]]", "v", "b"), ("[\\w&&\\d]", "v", "a"), ("[\\w&&\\d]", "v", "5"),
                        ("[\\s--\\d]", "v", " "), ("[\\s&&\\S]", "v", " "), ("[\\p{L}--\\p{Ll}]", "v", "a"),
                        ("[\\p{L}--\\p{Ll}]", "v", "A"), ("^[\\p{RGI_Emoji}--\\q{\ud83d\ude00}]$", "v", "\ud83d\ude00"),
                        ("[a\\|]", "v", "|"), ("[a\\!\\!b]", "v", "!"), ("[\\q{KK}]", "vi", "kk"),
                        ("[\\q{KK}]", "vi", "\u212a\u212a"), ("[^[\\w]]", "vi", "\u017f"), ("[\\q{abc|ab|a}]c", "v", "abc"),
                        ("[\\q{abc|ab|a}]+", "v", "ababc"), ("[\\q{ab|cd}--\\q{cd}]", "v", "cd"),
                        ("(?<=[\\q{ab}])c", "v", "abc"), ("[\\q{ab}x]", "dv", "ab"), ("[^a]", "v", "\ud83d\ude00"),
                        ("[\\q{\ud83d\ude00\ud83d\ude00|a}]", "v", "\ud83d\ude00\ud83d\ude00"),
                        ("[[a-c]&&[b-d]]+", "v", "abcd"), ("[\\q{a}\\q{ab}]", "v", "ab"), ("[a-z&&[^aeiou]]", "v", "x")):
        add_exec(f, "v-mode", p, flags, s, rows=("S8",), text="the v flag's set operations and strings, longest first")
    # Long inputs: rows B1, B5, B6, B8 and S3b's breadth. Rows B1 and B6 are sized inside the frame and trail ceilings
    # decision D10 has both engines pass (frames 1 << 20, trail 1 << 21), at the largest sizes the correspondence
    # records completing in Broiler.VM: past them each engine stops by its policy (D10), which no outcome renders
    # (SCHEMA.md section 7), so a vector there could be neither normative nor impl-defined (PARTLY).
    add_repeated("b1-greedy-run", "a*", 1, "", "", "a", 1048000, "", ("B1",),
                 "a* over 1,048,000 characters: the whole input (inside the frame ceiling of decision D10, which "
                 "Broiler.VM reaches past 2^20 characters, one frame each; row B1's b1h)")
    add_repeated("b1-greedy-run", "[a]*", 1, "", "u", "a", 100000, "", ("B1",),
                 "a greedy class over 100,000 characters under u: the whole input, never a shortened match")
    add_repeated("b5-quantifier-sequence", "(?:ab)*", 50000, "c", "u", "ab", 10, "c", ("B5",),
                 "50,000 general quantifiers in sequence before c, under u: the specification's answer, no fallback")
    add_repeated("b5-quantifier-sequence", "(?:[\\s--\\d]a)*", 50000, "c", "v", " a", 10, "c", ("B5",),
                 "50,000 general quantifiers in sequence before c, under v")
    add_repeated("b5-quantifier-sequence", "(?:ab)*", 1, "", "u", "ab", 100000, "", ("B5",),
                 "a general quantifier over 100,000 iterations under u")
    # Row B5's u-mode fallback (probe b5g): past the stack guard Broiler.JS answers with the .NET translation, whose
    # answers for these two tails differ from the specification's; 10 in sequence stay with the library.
    for count in (10, 50000):
        add_repeated("b5-quantifier-sequence", "(?:ab)*", count, "(?:x)?(a?b??)*", "u", "ab", 1, "", ("B5",),
                     f"{count:,} general quantifiers in sequence before (?:x)?(a?b??)*, under u, on \"ab\": the "
                     f"specification's [\"ab\", undefined], never the .NET translation's [\"ab\", \"\"]")
        add_repeated("b5-quantifier-sequence", "(?:ab)*", count, "(?<=(\\d+)(\\d+))$", "u", "1053", 1, "", ("B5",),
                     f"{count:,} general quantifiers in sequence before (?<=(\\d+)(\\d+))$, under u, on \"1053\": "
                     f"the specification's captures, read right to left")
    add_repeated("b6-frames-and-trail", "(?:ab)*", 1, "", "", "ab", 1048000, "", ("B6",),
                 "(?:ab)* over 1,048,000 iterations: the whole input (inside the frame ceiling of decision D10, one "
                 "frame an iteration; row B6's b6f)")
    add_repeated("b6-frames-and-trail", "(?:(a)(b)(c))*", 1, "", "", "abc", 161000, "", ("B6",),
                 "(?:(a)(b)(c))* over 161,000 iterations: the whole input (inside the trail ceiling of decision D10, 13 "
                 "cells an iteration; row B6's b6d)")
    add_repeated("b6-frames-and-trail", "(?:(a)(b)(c))*", 1, "", "", "abc", 100000, "", ("B6",),
                 "(?:(a)(b)(c))* over 100,000 iterations: the whole input, well inside both ceilings")
    add_repeated("b8-flat-sequence", "(?:a)", 100000, "", "u", "a", 100000, "", ("B8",),
                 "a flat sequence of 100,000 terms under u: the whole input, with no recursion per term")
    add_repeated("b8-flat-sequence", "\\B", 100000, "", "", "", 0, "", ("B8",),
                 "100,000 \\B in sequence on the empty string")
    add_repeated("b8-flat-sequence", "(?:a|b)", 100000, "", "", "a", 100000, "", ("B8",),
                 "100,000 alternations in sequence")
    # B2, B3, B4 at sizes the specification's answer completes quickly in.
    for p, flags, s in (("(?:(a+)+b|a*c)", "", "a" * 16 + "c"), ("(?:(a+)+b|a*c)", "u", "a" * 16 + "c"),
                        ("^(?!(?:(a+)+b|a*c))", "", "a" * 16 + "c"), ("^(?!(?:(a+)+b|a*c))", "u", "a" * 16 + "c"),
                        ("^(?=(?:(a+)+b|a*c))", "u", "a" * 12 + "c"), ("$(?<=(?:b(a+)+|ca*))", "", "c" + "a" * 12),
                        ("$(?<!(?:b(a+)+|ca*))", "u", "c" + "a" * 12)):
        add_exec(f, "b2-b3-exhaustion-shapes", p, flags, s, rows=("B2", "B3"),
                 text="the shapes of rows B2 and B3 at a size the answer completes in; past a budget they are the "
                      "budget file's")
    for n in (12, 16):
        for flags in ("", "u"):
            add_exec(f, "b4-alternation", "(?:a|a)" * n + "b", flags, "a" * n, rows=("B4",),
                     text=f"(?:a|a) written out {n} times, then b, on {n} a")
            add_exec(f, "b4-alternation", "(?:" + "(?:a|a)" * n + "b|a+)", flags, "a" * n, rows=("B4",),
                     text=f"(?:(?:a|a) x{n} b|a+) on {n} a: the first alternative fails every way, the second matches")
    for flags in ("", "u"):
        add_exec(f, "m5-sticky-backtracking", "x|(a+)+b", flags + "y", "y" + "a" * 12, rows=("M5",),
                 text="a sticky attempt makes one attempt at lastIndex")
    # S3: the nested patterns' matches (impl-defined past the limit).
    for n in (511, 512):
        add_exec(f, "s3-nesting", "(" * n + "a" + ")" * n, "", "a", rows=("S3",),
                 text=f"{n} nested capturing groups: every group captures a")
        add_exec(f, "s3-nesting", "(?=" * n + "a" + ")" * n, "", "a", rows=("S3",), text=f"{n} nested (?=")
        add_exec(f, "s3-nesting", "[" * n + "a" + "]" * n, "v", "a", rows=("S3",), text=f"{n} nested v-mode classes")


def build_case_insensitive() -> None:
    f = "case-insensitive"
    folding = ("CaseFolding.txt",)
    upper = ("UnicodeData.txt", "SpecialCasing.txt")
    for p, flags, s in (("[a-z]", "i", "\u212a"), ("[a-z]", "iu", "\u212a"), ("[a-z]", "iv", "\u212a"),
                        ("\u212a", "i", "k"), ("\u212a", "iu", "k"), ("k", "i", "\u212a"), ("K", "iu", "\u212a"),
                        ("[a-z]", "i", "\u017f"), ("[a-z]", "iu", "\u017f"), ("\u017f", "i", "s"), ("\u017f", "iu", "S"),
                        ("s", "i", "\u017f"), ("\\w", "iu", "\u017f"), ("\\W", "iu", "\u017f"), ("\\w", "i", "\u017f"),
                        ("[\\w]", "iu", "\u212a"), ("\\b", "iu", "\u017f"), ("\\W", "iu", "S"), ("[^\\W]", "iu", "\u017f"),
                        ("\\w", "iv", "\u017f"), ("\\W", "iv", "\u017f"), ("[\\W]", "iv", "S")):
        add_test(f, "m2-kelvin-and-long-s", p, flags, s, rows=("M2",),
                 text="Canonicalize without u is toUppercase, which leaves U+212A and maps U+017F to S, kept only "
                      "within ASCII; under u and v it is simple case folding", ucd=upper if "u" not in flags and
                 "v" not in flags else folding)
    dotted = [("i", "i", "\u0130"), ("I", "i", "\u0131"), ("i", "i", "\u0131"), ("I", "i", "\u0130"), ("\u0130", "i", "i"),
              ("\u0130", "i", "I"), ("\u0131", "i", "i"), ("\u0131", "i", "I"), ("[a-z]", "i", "\u0130"),
              ("[a-z]", "i", "\u0131"), ("[\u0130]", "i", "i"), ("[\u0131]", "i", "I"), ("i", "iu", "\u0130"),
              ("I", "iu", "\u0131"), ("\u0130", "iu", "i"), ("\u0131", "iu", "I"), ("[a-z]", "iu", "\u0130"),
              ("[a-z]", "iu", "\u0131"), ("i", "iv", "\u0130"), ("[a-z]", "iv", "\u0131"), ("I", "iv", "\u0131"),
              ("(?i:i)", "", "\u0130"), ("(?i:[a-z])", "", "\u0131"), ("(?i:I)", "u", "\u0131"),
              ("(\u0131)\\1", "i", "\u0131I"), ("(i)\\1", "i", "i\u0130"), ("(I)\\1", "i", "I\u0131"), ("(i)\\1", "iu", "i\u0130"),
              ("i\u0307", "i", "\u0130"), ("\u00cc", "i", "\u00ec"), ("\u00cc", "i", "i\u0307\u0300"), ("\u0128", "i", "\u0129"),
              ("\u0130", "i", "i\u0307"), ("\u0130", "iu", "i\u0307"), ("[\u0130-\u0131]", "i", "I"),
              ("[\u0130-\u0131]", "iu", "i")]
    for p, flags, s in dotted:
        add_test(f, "m2a-dotted-and-dotless-i", p, flags, s, rows=("M2a",),
                 text="Canonicalize reads no culture: the Turkic and Lithuanian mappings are not its, so every case "
                      "holds in every host culture (Broiler.JS's part D)",
                 ucd=upper if "u" not in flags and "v" not in flags else folding)
    for p, flags, s in (("\u1f80", "i", "\u1f88"), ("\u1f88", "i", "\u1f80"), ("\u1f80", "iu", "\u1f88"),
                        ("[\u1f80]", "i", "\u1f88"), ("\u00df", "i", "\u1e9e"), ("\u00df", "iu", "\u1e9e"),
                        ("\u0390", "iu", "\u1fd3"), ("\u0390", "i", "\u1fd3"), ("\u1e9e", "i", "\u00df"),
                        ("\u01c5", "i", "\u01c4"), ("\u01c5", "i", "\u01c6"), ("\u01c5", "iu", "\u01c6")):
        add_test(f, "m2b-multi-code-point-uppercase", p, flags, s, rows=("M2b",),
                 text="an upper case of more than one code point canonicalizes a code unit to itself without u",
                 ucd=upper if "u" not in flags else folding)
    for p, flags, s in (("\u03c3", "i", "\u03c2"), ("\u03c3", "iu", "\u03c2"), ("\u03c2", "i", "\u03a3"),
                        ("\\u{10400}", "iu", "\ud801\udc28"), ("\ud801\udc00", "i", "\ud801\udc28"),
                        ("\u00b5", "i", "\u039c"), ("\u00b5", "iu", "\u03bc"), ("\u00b5", "i", "\u03bc"),
                        ("\u1c89", "i", "\u1c8a"), ("\u1c89", "iu", "\u1c8a"), ("\\p{Lu}", "iu", "a"),
                        ("\\P{Lu}", "iu", "A"), ("[^\\p{Lu}]", "iu", "A"), ("\\p{Lowercase_Letter}", "iv", "A"),
                        ("\\P{Ll}", "iv", "a"), ("[^\\P{Ll}]", "iv", "A"), ("(a)\\1", "i", "aA"),
                        ("\u2126", "i", "\u03c9"), ("[\u03c9]", "iu", "\u2126"), ("[\u03a9]", "iu", "\u2126"),
                        ("[\u03c9]", "i", "\u2126"), ("[\u03a9]", "i", "\u2126"), ("[\u0100-\u010d]", "iv", "\u0101"),
                        ("[\u0100-\u010d]", "iv", "\u0100"), ("\u0345", "i", "\u0399"), ("\u0345", "iu", "\u03b9"),
                        ("\u1e9b", "iu", "\u1e61"), ("\\u{1E922}", "iu", "\ud83a\udd00"), ("\u01f0", "i", "J\u030c"),
                        ("\ufb00", "iu", "ff"), ("[\\q{\u00df}]", "vi", "ss"), ("[\\q{ss}]", "vi", "\u00df")):
        add_test(f, "m2c-other-folding", p, flags, s, rows=("M2c",), text="Canonicalize's two branches",
                 ucd=upper if "u" not in flags and "v" not in flags else folding)
    for p, flags, s in (("\ua7ce", "i", "\ua7cf"), ("\ua7ce", "iu", "\ua7cf"), ("[\ua7ce]", "i", "\ua7cf"),
                        ("[\ua7ce]", "iu", "\ua7cf"), ("\ua7cf", "i", "\ua7ce"), ("\\u{10D50}", "iu", "\ud803\udd70"),
                        ("\\u{16EA0}", "iu", "\ud81b\udebb"), ("\u1c89", "i", "\u1c8a")):
        add_test(f, "u3-unicode-17-casing", p, flags, s, rows=("U3",),
                 text="UCD 17.0.0's casing of the code points new in it",
                 ucd=upper if "u" not in flags else folding)
    for p, flags, s in (("(?i:\u0131)", "", "I"), ("(?i:\u0130)", "u", "i"), ("[^i]", "i", "\u0130"),
                        ("[^I]", "i", "\u0131"), ("\\w", "i", "\u0130"), ("\\W", "i", "\u0131")):
        add_test(f, "m2a-dotted-and-dotless-i", p, flags, s, rows=("M2a",),
                 text="the same, negated and through \\w", ucd=upper if "u" not in flags else folding)
    for p, flags, s in (("(I)\\1", "i", "I\u0130"), ("(?<n>\u0130)\\k<n>", "i", "\u0130i"), ("(\u0131)\\1", "iu", "\u0131I")):
        add_exec(f, "m2a-back-references", p, flags, s, rows=("M2a",),
                 text="a back-reference canonicalizes both sides", ucd=upper if "u" not in flags else folding)


def build_properties() -> None:
    f = "properties"
    data = ("PropertyValueAliases.txt", "UnicodeData.txt", "Scripts.txt", "ScriptExtensions.txt", "PropList.txt",
            "DerivedCoreProperties.txt", "emoji-data.txt")
    for p, flags, s in (("\\s", "", "\ufeff"), ("\\s", "", "\u0085"), ("\\s", "", "\u180e"), ("\\s", "", "\u00a0"),
                        ("\\s", "", "\u3000"), ("\\s", "u", "\ufeff"), ("\\s", "u", "\u0085"), ("\\S", "", "\u0085"),
                        ("[\\s]", "", "\u2029"), ("\\s", "v", "\u1680"), ("\\s", "", "\u200b"), ("\\s", "", "\u202f"),
                        ("\\d", "u", "\u0661"), ("\\d", "", "\u0661"), ("\\w", "", "\u00e9"), ("\\w", "u", "\u0130"),
                        ("\\w", "i", "\u0130"), ("\\D", "u", "\ud83d\ude00"), ("\\D", "", "\ud83d")):
        add_test(f, "u1-character-class-escapes", p, flags, s, rows=("U1",),
                 text="\\s is WhiteSpace and LineTerminator at Unicode 17.0.0; \\d and \\w are ASCII",
                 ucd=("UnicodeData.txt",))
    for p, flags, s in (("\\p{L}", "u", "\ua7ce"), ("\\p{Lu}", "u", "\ua7ce"), ("\\p{L}", "u", "\ud803\udd70"),
                        ("\\p{Cn}", "u", "\ua7ce"), ("\\p{sc=Garay}", "u", "\ud803\udd70"),
                        ("\\p{Script=Sidetic}", "u", "\ud802\udd40"), ("\\p{sc=Sidt}", "u", "\ud802\udd40"),
                        ("\\p{ID_Start}", "u", "\ua7ce"), ("\\p{Emoji}", "u", "\u2615"),
                        ("\\p{Extended_Pictographic}", "u", "\ud83e\udee9"), ("\\p{Assigned}", "u", "\ua7ce"),
                        ("\\p{Any}", "u", "\udfff"), ("\\p{sc=Hrkt}", "u", "\u30a2"), ("\\p{scx=Hrkt}", "u", "\u30a2"),
                        ("\\p{Script=Hiragana}", "u", "\u3042"), ("\\p{scx=Hira}", "u", "\u30fc"),
                        ("\\p{sc=Zyyy}", "u", "\u30fc"), ("\\p{gc=Nd}", "u", "\u0661"), ("\\p{N}", "u", "\u2167"),
                        ("\\p{ASCII}", "u", "\u007f"), ("\\p{ASCII}", "u", "\u0080"), ("\\p{AHex}", "u", "F"),
                        ("\\p{AHex}", "u", "g"), ("\\p{Lowercase}", "u", "\u00aa"), ("\\p{Ll}", "u", "\u00aa"),
                        ("\\P{L}", "u", "1"), ("[\\p{L}--[a-z]]", "v", "a"), ("\\p{L}", "v", "\ud83d\ude00"),
                        ("\\p{Alphabetic}", "u", "\u0345"), ("\\p{White_Space}", "u", "\u0085"),
                        ("\\p{Script_Extensions=Latin}", "u", "\u0363"), ("\\p{sc=Latin}", "u", "\u0363"),
                        ("\\p{Cs}", "u", "\ud800"), ("\\p{Co}", "u", "\ue000"), ("\\p{Zs}", "u", "\u205f"),
                        ("\\p{RGI_Emoji}", "v", "\ud83d\ude00"), ("\\p{Emoji_Keycap_Sequence}", "v", "1"),
                        ("\\p{L}", "", "p{L}")):
        add_test(f, "u2-property-membership", p, flags, s, rows=("U2",),
                 text="membership at UCD 17.0.0 (\\p{...} under u and v; without u, the escape is an identity escape)",
                 ucd=data)
    seqs = ("emoji-sequences.txt", "emoji-zwj-sequences.txt")
    for p, flags, s in (("^\\p{RGI_Emoji}$", "v", "\ud83e\udee9"), ("^\\p{Basic_Emoji}$", "v", "\u231a"),
                        ("^\\p{RGI_Emoji_Flag_Sequence}$", "v", "\ud83c\uddfa\ud83c\uddf3"),
                        ("^\\p{Emoji_Keycap_Sequence}$", "v", "#\ufe0f\u20e3"),
                        ("^\\p{RGI_Emoji_ZWJ_Sequence}$", "v", "\ud83d\udc68\u200d\ud83d\udcbb"),
                        ("^\\p{RGI_Emoji}$", "v", "\ud83d\udc68\u200d\ud83d\udc69\u200d\ud83d\udc67\u200d\ud83d\udc66"),
                        ("^\\p{RGI_Emoji_Modifier_Sequence}$", "v", "\ud83d\udc4b\ud83c\udffd"),
                        ("^\\p{RGI_Emoji_Tag_Sequence}$", "v",
                         "\ud83c\udff4\udb40\udc67\udb40\udc62\udb40\udc65\udb40\udc6e\udb40\udc67\udb40\udc7f"),
                        ("^\\p{Basic_Emoji}$", "v", "\u231a\ufe0f"), ("^\\p{RGI_Emoji}$", "v", "a"),
                        ("^[\\p{RGI_Emoji_Flag_Sequence}--\\q{\ud83c\uddfa\ud83c\uddf3}]$", "v", "\ud83c\uddfa\ud83c\uddf3"),
                        ("^[\\p{RGI_Emoji_Flag_Sequence}&&\\q{\ud83c\uddfa\ud83c\uddf3|x}]$", "v", "\ud83c\uddfa\ud83c\uddf3")):
        add_test(f, "string-properties", p, flags, s, rows=("U2",),
                 text="a property of strings under v, its longest strings tried first", ucd=seqs)
    for p, flags, s in (("\\p{RGI_Emoji_Flag_Sequence}", "v", "x\ud83c\uddfa\ud83c\uddf3\ud83c\uddfa"),
                        ("\\p{RGI_Emoji}", "dv", "a\ud83d\udc68\u200d\ud83d\udcbbb"),
                        ("(?<=\\p{RGI_Emoji_Flag_Sequence})x", "v", "\ud83c\uddfa\ud83c\uddf3x")):
        add_exec(f, "string-properties", p, flags, s, rows=("U2",), text="where a string of a property matches",
                 ucd=seqs)


def build_last_index() -> None:
    f = "last-index"
    pair = "\ud834\udf06"
    for p, flags, s, li in (("." , "gu", pair, 1), (".", "gv", pair, 1), (".", "g", pair, 1), ("\\udf06", "gu", pair, 1),
                            ("\\udf06", "g", pair, 1), ("\\udf06", "u", pair, 0), (".", "yu", pair, 1),
                            ("\\udf06", "yu", pair, 1), (".", "gu", "a" + pair, 1), (".", "gu", "a" + pair, 2),
                            ("(?<=\\ud834).", "gu", pair, 1), (".", "gdu", pair, 1), ("(.)", "gdu", pair, 1),
                            ("\\udf06", "gv", pair, 1), ("[\\ud834\\udf06]", "gu", pair, 1), (".", "gu", pair, 2),
                            (".", "gu", pair, 3), ("$", "gu", pair, 1), ("(?:)", "gu", pair, 0), ("(?:)", "gu", pair, 2),
                            ("(?:)", "g", pair, 1), ("\\B", "gu", pair, 2)):
        add_exec(f, "m4-surrogate-pairs", p, flags, s, li, rows=("M4",),
                 text="the matcher starts at the character obtained from element lastIndex (the pair); index is "
                      "lastIndex and the substring starts there")
    # D41: the empty match from a lastIndex inside a pair.
    vm = "lastIndex=0;index=0;length=1;0=0:;groups=u;indices=u"
    jsa = "lastIndex=1;index=1;length=1;0=0:;groups=u;indices=u"
    try:
        R.counted_exec(units("(?:)"), units("gu"), units(pair), 1)
        raise GeneratorError("regexpref answers the empty match inside a pair; decision D41 no longer applies")
    except R.SpecAssertion:
        pass
    case = Case(f, "m4-empty-match-d41", "RegExpBuiltinExec", [s_("(?:)"), s_("gu"), s_(pair), i_(1)],
                value_str(vm) + " | " + value_str(jsa),
                CANONICAL_JS + ' var r = new RegExp("(?:)", "gu"); r.lastIndex = 1; $x(r, r.exec("\\ud834\\udf06"))',
                ["sec-regexpbuiltinexec", "sec-getmatchstring"], note_of(("M4",), D41), ("M4",), cls="impl-defined",
                per_engine={"broiler-js": value_str(vm), "broiler-vm": value_str(vm)}, reference=False)
    add(case)
    # M5: sticky.
    for p, flags, s, li in (("a", "y", "ba", 0), ("a", "y", "ba", 1), ("a", "uy", "ba", 0), ("b|ab", "y", "ab", 0),
                            ("^b", "y", "ab", 1), ("^b", "my", "a\nb", 2), ("(?<=a)b", "y", "ab", 1), ("\\bb", "y", "ab", 1),
                            ("a", "gy", "aab", 0), ("a", "gy", "aab", 2), ("a", "y", "a", 2), ("(?:)", "y", "a", 1),
                            ("a", "dy", "xa", 1), (".", "uy", "\ud834\udf06x", 2), ("b", "y", "abc", 1)):
        add_exec(f, "m5-sticky", p, flags, s, li, rows=("M5",), text="one attempt at lastIndex; a failure sets it to 0")
    for p, flags, s, li in (("a", "g", "aba", 0), ("a", "g", "aba", 1), ("a", "g", "aba", 3), ("a", "g", "aba", 4),
                            ("a", "", "aba", 2), ("(?:)", "g", "ab", 2), ("a|", "g", "ba", 0), ("(?:)", "g", "", 0),
                            ("a", "dg", "xa", 0)):
        add_exec(f, "global-steps", p, flags, s, li, rows=("M5",),
                 text="lastIndex under g: read, searched from, written; ignored without g and y")
    for p, flags, s in (("(?:)", "gu", pair), ("(?:)", "g", pair), ("(?:)", "gv", pair), ("a*?", "g", "aaa"),
                        ("\\d", "g", "a1b2"), ("a", "gy", "aab"), ("^", "gm", "a\nb\u2028c"), ("$", "gm", "a\nb\u2028c"),
                        ("(?:)", "gu", "a" + pair + "b"), (".", "gu", pair + "\udf06")):
        r = construct(p, flags)
        result = R.regexp_match(r, units(s))
        add(Case(f, "match-global", "RegExp.prototype [ %Symbol.match% ]", [s_(p), s_(flags), s_(s)],
                 value_str(serialise_array(result)),
                 ARRAY_JS + f" $a(new RegExp({js(p)}, {js(flags)})[Symbol.match]({js(s)}))",
                 ["sec-regexp.prototype-%symbol.match%", "sec-advancestringindex", "sec-regexpbuiltinexec"],
                 note_of(("M4", "M5"), "an empty match advances lastIndex by AdvanceStringIndex, a code point under u "
                                       "or v"), ("M4", "M5")))
    for p, flags, s in (("(?:)", "u", pair), ("(?:)", "", pair), ("(,)", "", "a,b"), ("(?<=b)", "", "abc"), (",", "y", "a,b"),
                        ("$", "m", "a\nb\u2028c"), ("<(\\/)?([^<>]+)>", "", "A<B>bold</B>and<CODE>coded</CODE>"),
                        ("(?:)", "", ""), ("a", "", ""), ("(?:)", "v", pair + "a"), ("x*", "", "axbxc"), ("(a)|b", "", "cbd")):
        r = construct(p, flags)
        result = R.regexp_split(r, units(s))
        add(Case(f, "split", "RegExp.prototype [ %Symbol.split% ]", [s_(p), s_(flags), s_(s)],
                 value_str(serialise_array(result)),
                 ARRAY_JS + f" $a(new RegExp({js(p)}, {js(flags)})[Symbol.split]({js(s)}))",
                 ["sec-regexp.prototype-%symbol.split%", "sec-advancestringindex", "sec-regexpbuiltinexec"],
                 note_of(("M4", "M5", "M1"), "the splitter adds y; an empty match does not split at the start, the end or "
                                             "the last split"), ("M4", "M5", "M1")))
    for p, flags, s in (("\\udf06", "u", pair), ("\\udf06", "", pair), ("b", "y", "aab"), ("b", "g", "aab"),
                        ("$", "", "ab"), ("x", "", "ab")):
        r = construct(p, flags)
        index = R.regexp_search(r, units(s))
        add(Case(f, "search", "RegExp.prototype [ %Symbol.search% ]", [s_(p), s_(flags), s_(s)],
                 "value: " + f64(index), f"new RegExp({js(p)}, {js(flags)})[Symbol.search]({js(s)})",
                 ["sec-regexp.prototype-%symbol.search%", "sec-regexpbuiltinexec"],
                 note_of(("M4", "M5"), "search saves and restores lastIndex and searches from 0, sticky or not"),
                 ("M4", "M5")))


def build_budget() -> None:
    f = "budget"
    shapes = [
        ("k-catastrophic", "(a+)+b", "", "b" + "a" * 18, ("B1", "B2"), "(a+)+b on b and 18 a"),
        ("k-catastrophic", "(a*)*b", "", "a" * 18 + "c", ("B2",), "(a*)*b on 18 a and c"),
        ("k-catastrophic", "(a|aa)+b", "", "b" + "a" * 24, ("B2",), "(a|aa)+b on b and 24 a"),
        ("k-catastrophic", "(?:a+)+b", "u", "b" + "a" * 18, ("B2",), "(?:a+)+b under u"),
        ("k-catastrophic", "^(\\w+\\s?)*$", "", "a" * 18 + "!", ("B2",), "^(\\w+\\s?)*$ on 18 a and !"),
        ("a-alternation", "(?:a|a)" * 14 + "b", "", "a" * 14, ("B4",), "(?:a|a) x14 then b, quantifier-free"),
        ("a-alternation", "(?:a|a)" * 14 + "b", "u", "a" * 14, ("B4",), "the same under u"),
        ("a-alternation", "(?:a|a)" * 14 + "b", "v", "a" * 14, ("B4",), "the same under v"),
        ("l-look-around", "^(?!(?:(a+)+b|a*c))", "", "a" * 16 + "c", ("B3",), "exhaustion inside a negative look-ahead"),
        ("l-look-around", "^(?!(?:(a+)+b|a*c))", "u", "a" * 16 + "c", ("B3",), "the same under u"),
        ("l-look-around", "^(?=(?:(a+)+b|a*c))", "u", "a" * 16 + "c", ("B3",), "a positive look-ahead"),
        ("l-look-around", "$(?<=(?:b(a+)+|ca*))", "", "c" + "a" * 16, ("B3",), "a look-behind, read right to left"),
        ("l-look-around", "$(?<!(?:b(a+)+|ca*))", "u", "c" + "a" * 16, ("B3",), "a negative look-behind under u"),
        ("w-one-alternative", "(?:(a+)+b|a*c)", "", "a" * 16 + "c", ("B2",), "exhaustion inside one alternative"),
        ("w-one-alternative", "(?:(a+)+b|a*c)", "u", "a" * 16 + "c", ("B2",), "the same under u"),
        ("g-greedy-run", "[a]*", "", ("a", 100000, ""), ("B1",), "a greedy class over 100,000 characters"),
        ("g-greedy-run", "^[a]*$", "u", ("a", 100000, ""), ("B1",), "anchored at both ends, under u"),
        ("g-greedy-run", "a*", "u", ("a", 100000, ""), ("B1",), "a greedy character under u"),
    ]
    for group, p, flags, s, rows, what in shapes:
        add_budget(f, group, p, flags, s, 1000, rows, what)
    completing = [
        ("o-ordinary", "(?:(a+)+b|a*c)", "", "a" * 8 + "c", ("B2",), "the shape of W at 8 a"),
        ("o-ordinary", "^(?!(?:(a+)+b|a*c))", "u", "a" * 8 + "c", ("B3",), "the shape of L at 8 a"),
        ("o-ordinary", "(a+)+b", "", "a" * 8 + "b", ("B2",), "(a+)+b that matches"),
        ("o-ordinary", "(?<a>x)|(?<a>y)", "", "y", ("S1",), "duplicate names"),
        ("o-ordinary", "[a]*", "", "a" * 200, ("B1",), "a greedy class within its budget"),
        ("o-ordinary", "(?:a|ab)*c", "", "ab" * 50 + "c", ("B6",), "a loop of alternatives"),
    ]
    for group, p, flags, s, rows, what in completing:
        add_budget(f, group, p, flags, s, 1000000, rows, what)


def add_budget(f: str, group: str, p: str, flags: str, s, budget: int, rows, what: str) -> None:
    piece, count, tail = s if isinstance(s, tuple) else (s, 1, "")
    pattern, subject = units(p), units(piece) * count + units(tail)
    exhausted = R.counted_exec(pattern, units(flags), subject, 0, cutoff=BUDGET_MARGIN * budget)
    if exhausted.exhausted:
        out = "status: exhausted"
        text = (f"{what}: regexpref's count reaches its cut-off of {BUDGET_MARGIN * budget} Matcher invocations, a "
                f"hundred times the budget of {budget}, so the library must report the step limit, never a match, a "
                f"shorter match, a later index or a flipped look-around")
    else:
        if exhausted.steps * BUDGET_MARGIN > budget:
            raise GeneratorError(f"{group}: {p!r} needs {exhausted.steps} steps, inside the margin of {budget}")
        r = R.regexp_create(pattern, units(flags))
        result = R.regexp_builtin_exec(r, subject)
        out = value_str(serialise(r.last_index, result, subject))
        text = (f"{what}: regexpref's count is {exhausted.steps}, at most a hundredth of the budget of {budget}, so "
                f"the library completes with the answer")
    script = (CANONICAL_JS + f" var r = new RegExp({js(p)}, {js(flags)}); r.lastIndex = 0; "
              f"$x(r, r.exec({js(piece)}.repeat({count}) + {js(tail)}))")
    add(Case(f, group, "RegExpBuiltinExec, step budget",
             [s_(p), s_(flags), s_(piece), i_(count), s_(tail), i_(0), i_(budget)], out, script,
             exec_clauses(p, flags), note_of(rows, text + "; a library-level vector (decision D25), which "
                                                         + ("engine-level runners skip" if exhausted.exhausted else
                                                            "engine-level runners evaluate too, as the exec without "
                                                            "a budget, since only a status vector is skipped "
                                                            "(revision 32)")), tuple(rows)))


def build_text() -> None:
    f = "text"
    for s in ("abc", "1a", "a1", "_", "a b", "\u00e9", "\u0085", "\u00a0", "\ufeff", "\u2028", "\u2029", "\u3000",
              "\u180e", "\u1680", "\u202f", "\ud800", "\udfff", "\ud83d\ude00", "x\ud83d", ",-=<>#&!%:;@~'`\"",
              "^$\\.*+?()[]{}|/", "\t\n\u000b\f\r", " ", "", "Z\u0000", "\u0080", "\u00ff", "\u0100", "9", "\u0661a",
              "a\u200d", "-1"):
        out = value_str(ustr(R.regexp_escape(units(s))))
        add(Case(f, "t5-escape", "RegExp.escape", [s_(s)], out, f"RegExp.escape({js(s)})",
                 ["sec-regexp.escape", "sec-encodeforregexpescape"],
                 note_of(("T5",), "EncodeForRegExpEscape over the WhiteSpace and LineTerminator of UCD 17.0.0"),
                 ("T5",), ucd=("UnicodeData.txt",)))
    for s, p_flags in (("a.b*c", ""), (",-", "u"), ("\ud83d\ude00", "v"), ("1+1", ""), ("\u2028x", "u"), ("[a]", "v")):
        r = construct(ustr(R.regexp_escape(units(s))), p_flags)
        out = "value: bool:true" if R.regexp_test(r, units(s)) else "value: bool:false"
        add(Case(f, "t5-escape-round-trip", "RegExp.escape, round trip", [s_(s), s_(p_flags)], out,
                 f"new RegExp(RegExp.escape({js(s)}), {js(p_flags)}).test({js(s)})",
                 ["sec-regexp.escape", "sec-regexpinitialize", "sec-regexp.prototype.test"],
                 note_of(("T5",), "the escaped text, as a pattern, matches the string"), ("T5",)))
    for flags in ("", "g", "yg", "dgimsy", "dgimsvy", "ygmisd", "v", "u", "sd"):
        r = construct("a", flags)
        out = value_str(ustr(R.regexp_flags(r)))
        add(Case(f, "f1-flags-getter", "get RegExp.prototype.flags", [s_(flags)], out,
                 f"new RegExp(\"a\", {js(flags)}).flags", ["sec-get-regexp.prototype.flags", "sec-regexphasflag"],
                 note_of(("F1",), "the flags in the order d g i m s u v y"), ("F1",)))
    plain = '{ hasIndices: 1, global: 1, ignoreCase: 0, multiline: "", dotAll: 1, unicode: 0, unicodeSets: 1, sticky: 1 }'
    add(Case(f, "f2-flags-getter-object", "get RegExp.prototype.flags, ordinary object", [s_(plain)],
             value_str("dgsvy"),
             f'Object.getOwnPropertyDescriptor(RegExp.prototype, "flags").get.call({plain})',
             ["sec-get-regexp.prototype.flags"],
             note_of(("F2",), "the getter reads each property with ToBoolean; read from the clause, which regexpref "
                              "does not model"), ("F2",), reference=False))
    order = ("var get = Object.getOwnPropertyDescriptor(RegExp.prototype, \"flags\").get, seen = [], o = {}; "
             "[\"hasIndices\", \"global\", \"ignoreCase\", \"multiline\", \"dotAll\", \"unicode\", \"unicodeSets\", "
             "\"sticky\"].forEach(function (k) { Object.defineProperty(o, k, { get: function () { seen.push(k); "
             "return false; } }); }); get.call(o); seen.join()")
    add(Case(f, "f2-flags-getter-object", "get RegExp.prototype.flags, read order", [s_("")],
             value_str("hasIndices,global,ignoreCase,multiline,dotAll,unicode,unicodeSets,sticky"), order,
             ["sec-get-regexp.prototype.flags"], note_of(("F2",), "the getter's reads in the clause's order"), ("F2",),
             reference=False))
    for script, out, what in (("RegExp.prototype.flags", value_str(""), "the prototype has no flags"),
                              ('String(new RegExp("a", "yg")).slice(-3)', value_str("/gy"),
                               "toString writes the flags getter's text after the last /"),
                              ("RegExp.prototype.global", "value: undefined", "RegExpHasFlag on the prototype"),
                              ("RegExp.prototype.source", value_str("(?:)"), "the prototype's source")):
        add(Case(f, "f2-prototype", "RegExp.prototype accessors", [s_(script)], out, script,
                 ["sec-get-regexp.prototype.flags", "sec-regexphasflag", "sec-get-regexp.prototype.source",
                  "sec-regexp.prototype.tostring"],
                 note_of(("F2", "T4"), what + "; read from the clauses"), ("F2", "T4"), reference=False))
    # T1: one P and F give one S.
    for p, flags in (("\\u{61}", "u"), ("\\u{1F600}", "u"), ("\\u{D83D}", "u"), ("[\\u{61}-\\u{7A}]", "v"), ("\\u{3}", ""),
                     ("\\\\u{61}", "u"), ("[/]", ""), ("a(b)c", "")):
        lit = p.replace("/", "\\/") if p != "[/]" else p
        if not printable(lit):
            continue
        script = f"/{lit}/{flags}.source === new RegExp({js(lit)}, {js(flags)}).source"
        add(Case(f, "t1-source-literal-and-constructor", "EscapeRegExpPattern, one P and F", [s_(lit), s_(flags)],
                 TRUE, script, ["sec-escaperegexppattern", "sec-regular-expression-literals-runtime-semantics-evaluation"],
                 note_of(("T1",), "a literal and its constructor twin give one pattern text P and flags F, and "
                                  "EscapeRegExpPattern gives one S for them; read from the clause"), ("T1",),
                 reference=False))
    # T2 and T3: what S must be, with no form fixed.
    for p, flags, s in (("[/]", "", "/"), ("/", "", "/"), ("\\/", "", "/"), ("a/b", "", "a/b"), ("\\\n", "", "\n"),
                        ("\\\u2028", "", "\u2028"), ("\n", "", "\n"), ("\r", "", "\r"), ("[/]\n/", "g", "/\n/"),
                        ("\u2029", "u", "\u2029"), ("", "", ""), ("\ud83d", "", "\ud83d"), ("\u0085", "", "\u0085")):
        r = construct(p, flags)
        matches = R.regexp_test(r, units(s))
        script = (f'var s = new RegExp({js(p)}, {js(flags)}).source; [s.indexOf("\\n"), s.indexOf("\\r"), '
                  f's.indexOf("\\u2028"), s.indexOf("\\u2029")].join() === "-1,-1,-1,-1" && '
                  f'(0, eval)("/" + s + "/{flags}").test({js(s)}) === {"true" if matches else "false"}')
        add(Case(f, "t2-t3-source-escapes", "EscapeRegExpPattern, a literal of S", [s_(p), s_(flags), s_(s)], TRUE,
                 script, ["sec-escaperegexppattern", "sec-literals-regular-expression-literals"],
                 note_of(("T2", "T3"), "S holds no line terminator, and /S/F parses as a literal that behaves as the "
                                       "pattern does (its test on the subject is regexpref's); the form of S is the "
                                       "implementation's"), ("T2", "T3")))
    # M6: String.prototype.search.
    for script, out, what in (
            ('delete RegExp.prototype[Symbol.search]; "abc".search(/b/)', "error: TypeError",
             "with RegExp.prototype[%Symbol.search%] deleted, search creates a RegExp from the argument and Invokes "
             "its %Symbol.search%, which is undefined"),
            ('delete RegExp.prototype[Symbol.search]; "abc".search(/b/y)', "error: TypeError", "the same, sticky"),
            ('delete RegExp.prototype[Symbol.search]; "abc".search("b")', "error: TypeError", "the same, a string"),
            ('var re = /b/; re[Symbol.search] = undefined; "abc".search(re)', "value: " + f64(-1),
             "an own %Symbol.search% of undefined: search makes RegExpCreate(re), whose pattern is ToString(re), a "
             "pattern matching /b/ itself, and \"abc\" has no /"),
            ('var re = /b/; re[Symbol.search] = null; "abc".search(re)', "value: " + f64(-1), "the same, null"),
            ('var re = /b/g; re.lastIndex = 2; "abcb".search(re) + "," + re.lastIndex', value_str("1,2"),
             "search restores lastIndex")):
        add(Case(f, "m6-string-search", "String.prototype.search", [s_(script)], out, script,
                 ["sec-string.prototype.search", "sec-regexp.prototype-%symbol.search%", "sec-regexpcreate"],
                 note_of(("M6",), what + "; read from the clauses (the match itself is regexpref's where one runs)"),
                 ("M6",), reference=False))


BUILDERS = (build_syntax, build_literals, build_matching, build_case_insensitive, build_properties, build_last_index,
            build_budget, build_text)

# The rows of the correspondence with a behavioural difference between an engine and the specification (its section
# "The rows by class": every row but those "same in both"), each of which a vector must reach.
REACHED = ("S1", "S2", "S2a", "S2b", "S3", "S3b", "S4", "S5", "S6", "S7", "S8", "S9b", "S10", "S11", "M2", "M2a", "M2b",
           "M3", "M4", "M6", "M7", "B1", "B2", "B3", "B4", "B5", "B6", "B8", "T1", "T2", "T3", "U3")
NOT_REACHED = {
    "S9": "an invalid literal read as division after an expression: the lexical goal (InputElementDiv against "
          "InputElementRegExp), which the syntax area pins (its regexp-delimiting vectors) and SJL-14 answers "
          "(JSD-0081 decision D22); no 22.2 operation decides it",
    "B7": "the compile charge: not observable through any outcome (the row's specification cell); the library's "
          "compile budget is Broiler.Regex's adversarial corpus's class V (its rule's part C, check A-2)",
}
# Rows the specification's answer for at Broiler.JS's own cell is out of the reference's reach: noted, reached
# otherwise.
PARTLY = {
    "B1": "Broiler.JS's cell (10,000,001 characters past the 10,000,000-step allowance of Broiler.Regex 0.1.0-preview.4) "
          "needs regexpref to hold ten million pending choice points; the vectors reach the row at 1,048,000 "
          "characters, inside the frame ceiling of decision D10 that Broiler.VM reaches past 2^20, and at library "
          "level under a budget, and Broiler.JS's part C programs (G01 to G03) hold the size; past the ceiling each "
          "engine stops by its policy (D10), Broiler.VM's stop an abort no outcome renders (SCHEMA.md section 7)",
    "B6": "Broiler.VM's cell (its stop past 2^20 frames or 2^21 trail cells) is engine policy that decision D10 keeps "
          "and sets for both engines, Broiler.JS's a RangeError: the vectors reach the row at the largest sizes the "
          "row records completing (b6f, b6d) and below them, and the sizes past the ceilings are left to the part C "
          "programs, since an abort matches no outcome (SCHEMA.md section 7) and the stop is no divergence a plan "
          "answers",
    "B2": "Broiler.JS's cell (24 a under u) needs about 10^8 Matcher invocations; the vectors reach the row's shape at "
          "16 a and at library level under a budget (W and K shapes); Broiler.JS's part C programs (W01, W02) hold the "
          "size",
    "B3": "Broiler.JS's cell (28 a under u) needs about 10^9 Matcher invocations; reached at 16 a and at library level "
          "(L shapes); Broiler.JS's part C programs (L01 to L05) hold the size",
}


def build() -> list[Case]:
    CASES.clear()
    for builder in BUILDERS:
        builder()
    return list(CASES)


# ---- Writing --------------------------------------------------------------------------------------------------------


def provenance(case: Case) -> str:
    entries = [SPEC + c for c in case.clauses]
    if case.reference:
        entries.append(REF)
    entries.extend(UCD + name for name in case.ucd)
    return " ".join(dict.fromkeys(entries))


def read_divergences(path: Path) -> dict[str, dict[str, str]]:
    """regexp-divergences.txt: `reason <key>: <text>` lines, then `<engine> <key> <id>` lines; by id, each engine's
    text."""
    if not path.exists():
        return {}
    reasons: dict[str, str] = {}
    out: dict[str, dict[str, str]] = {}
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line or line.startswith("#"):
            continue
        if line.startswith("reason "):
            key, sep, text = line[len("reason "):].partition(": ")
            if not sep or not text or key in reasons:
                raise GeneratorError(f"{path.name}:{number}: a reason is `reason <key>: <text>`, each key once")
            if not printable(text):
                raise GeneratorError(f"{path.name}:{number}: a reason is one line of printable ASCII")
            reasons[key] = text
            continue
        parts = line.split(" ")
        if len(parts) != 3 or parts[0] not in ("broiler-js", "broiler-vm") or parts[1] not in reasons:
            raise GeneratorError(f"{path.name}:{number}: `{line}` is not `<engine> <reason key> <id>`")
        engine, key, identifier = parts
        if engine in out.setdefault(identifier, {}):
            raise GeneratorError(f"{path.name}:{number}: {identifier} is classified twice for {engine}")
        out[identifier][engine] = reasons[key]
    return out


def render(identifier: str, case: Case, divergence: dict[str, str] | None = None) -> str:
    divergence = divergence or {}
    cls = case.cls
    for engine in divergence:
        if cls == "impl-defined" and engine not in case.per_engine:
            raise GeneratorError(f"{identifier}: an impl-defined vector classified for {engine}, whose case states "
                                 "no per-engine expectation for it (SCHEMA.md section 5)")
        if case.out.startswith("status: "):
            raise GeneratorError(f"{identifier}: a status vector is library-level, and no engine field classifies it")
    if divergence and cls == "normative":
        cls = "divergence"
    lines = [f"id: {identifier}", "area: regexp", f"op: {case.op}", "in: " + " ".join(case.inputs),
             f"out: {case.out}", f"class: {cls}"]
    for engine in ("broiler-js", "broiler-vm"):
        if engine in case.per_engine:
            lines.append(f"per-engine.{engine}: {case.per_engine[engine]}")
    for engine in ("broiler-js", "broiler-vm"):
        if engine in divergence:
            lines.append(f"divergence.{engine}: {divergence[engine]}")
    lines.append(f"prov: {provenance(case)}")
    lines.append(f"script: {case.script}")
    lines.append(f"note: {case.note}")
    lines.append(f"since: {SINCE}")
    return "\n".join(lines) + "\n"


# Revision 32: the rows a vector's note gains, because both engines' first runs on revision 31 showed a difference
# whose answer is that row's (the reason in regexp-divergences.txt names it), which the note did not name. The note
# names the row beside those it named; nothing else of the vector changes.
ROWS_REVISION_32 = {
    "regexp/syntax/early-errors/0002": ("S1",),
    "regexp/syntax/s2a-escaped-duplicates/0003": ("S1",),
    "regexp/matching/s4-escapes/0031": ("S5",),
    "regexp/matching/modifiers/0009": ("M2",),
    "regexp/case-insensitive/m2c-other-folding/0021": ("M2",),
    "regexp/case-insensitive/m2c-other-folding/0022": ("M2",),
    "regexp/last-index/match-global/0007": ("M3",),
    "regexp/last-index/match-global/0008": ("M3",),
    "regexp/last-index/split/0006": ("M3",),
}


def _revise_rows(identifier: str, case: Case) -> Case:
    added = ROWS_REVISION_32.get(identifier)
    if not added:
        return case
    head = note_of(case.rows, "")
    if case.rows and not case.note.startswith(head):
        raise GeneratorError(f"{identifier}: its note does not start with its rows")
    rows = tuple(dict.fromkeys(case.rows + added))
    text = case.note[len(head):] if case.rows else case.note
    return replace(case, rows=rows, note=note_of(rows, text))


def assign_ids(cases: Sequence[Case]) -> list[tuple[str, Case]]:
    counters: dict[tuple[str, str], int] = {}
    out = []
    for case in cases:
        key = (case.file, case.group)
        counters[key] = counters.get(key, 0) + 1
        identifier = f"regexp/{case.file}/{case.group}/{counters[key]:04d}"
        out.append((identifier, _revise_rows(identifier, case)))
    missing = sorted(set(ROWS_REVISION_32) - {identifier for identifier, _ in out})
    if missing:
        raise GeneratorError(f"ROWS_REVISION_32 names ids the area does not have: {missing}")
    return out


def file_text(name: str, vectors: Sequence[tuple[str, Case]], divergences: dict[str, dict[str, str]]) -> str:
    header = [
        f"# Generated by scripts/conformance/{GENERATOR}; do not edit by hand.",
        f"# The regular-expression area: {FILES[name]}.",
        "# Expected values: scripts/conformance/regexpref.py (ref:regexpref@1), ECMA-262 22.2 and B.1.2 at 0248456c",
        "# transliterated, over UCD 17.0.0 through unicoderef.py; a vector without ref:regexpref@1 is read from the",
        "# clauses it cites. No expected value is read from an engine.",
        "schema: 1",
    ]
    body = [render(identifier, case, divergences.get(identifier)) for identifier, case in vectors if case.file == name]
    return "\n".join(header) + "\n\n" + "\n".join(body)


def write_files(out: Path, divergences: dict[str, dict[str, str]] | None = None) -> dict[str, int]:
    vectors = assign_ids(build())
    divergences = read_divergences(DIVERGENCES) if divergences is None else divergences
    unknown = sorted(set(divergences) - {identifier for identifier, _ in vectors})
    if unknown:
        raise GeneratorError(f"{DIVERGENCES.name} classifies ids the area does not have: {unknown[:10]}")
    out.mkdir(parents=True, exist_ok=True)
    counts = {}
    for name in FILES:
        text = file_text(name, vectors, divergences)
        (out / f"{name}.vectors").write_bytes(text.encode("utf-8"))
        counts[name] = sum(1 for _, case in vectors if case.file == name)
    return counts


def rows_table(cases: Sequence[Case]) -> dict[str, list[str]]:
    table: dict[str, list[str]] = {}
    for identifier, case in assign_ids(cases):
        for row in case.rows:
            table.setdefault(row, []).append(identifier)
    return table


def check_rows(cases: Sequence[Case]) -> None:
    table = rows_table(cases)
    missing = [row for row in REACHED if row not in table]
    if missing:
        raise GeneratorError(f"rows with a behavioural difference no vector reaches: {missing}")


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT)
    parser.add_argument("--rows", action="store_true", help="print each correspondence row and the vectors reaching it")
    args = parser.parse_args(argv)
    if args.rows:
        cases = build()
        check_rows(cases)
        for row, ids in sorted(rows_table(cases).items()):
            print(f"{row}\t{len(ids)}\t{ids[0]} .. {ids[-1]}")
        for row, why in NOT_REACHED.items():
            print(f"{row}\tnot reached\t{why}")
        for row, why in PARTLY.items():
            print(f"{row}\tone cell reached otherwise\t{why}")
        return 0
    counts = write_files(args.out)
    check_rows(CASES)
    for name, count in counts.items():
        print(f"{name}.vectors\t{count}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
