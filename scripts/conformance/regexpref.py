#!/usr/bin/env python3
# SPDX-FileCopyrightText: 2026 Broiler Platform contributors
# SPDX-License-Identifier: Apache-2.0
"""The regular-expression area's reference computation, `ref:regexpref@1` (corpus revision 31, milestone SJL-11.3).

    python3 -I scripts/conformance/regexpref.py exec <pattern> <flags> <subject> [--last-index N] [--cutoff N]
    python3 -I scripts/conformance/regexpref.py corpus <Broiler.Regex tests/Adversarial/corpus.tsv> [--rows K01,O01]

WHAT IT IS. ECMA-262 22.2 ("RegExp (Regular Expression) Objects") transliterated from the edition the corpus pins,
ES2026 at tc39/ecma262 0248456c758431e4bb8e5d26333ff1865123c9cd (conformance/vectors/sources/ecma262.pin), with Annex
B's B.1.2 ("Regular Expressions Patterns"). Decision D25 of Broiler.VM's JSD-0081 makes it the oracle of the area's
vectors and the arbiter of Broiler.Regex's reference arms (that repository's docs/evidence/sjl-11-001/reference-arms.md).
It reads the specification's text and the pinned Unicode Character Database, through unicoderef.py (`ref:unicoderef@1`,
UCD 17.0.0 by sources/unicode.pin), and nothing else: no engine is run or consulted, Python's `re` is never imported,
and no answer is taken from V8 or any other implementation.

What it transliterates, by clause id of the pinned text:
- the pattern grammar of 22.2.1 (sec-patterns) with every parameter: [UnicodeMode] (`u` and `v`), [UnicodeSetsMode]
  (`v`), [NamedCaptureGroups]; and B.1.2's grammar (sec-regular-expressions-patterns) for patterns without `u` and `v`,
  with B.1.2's ParsePattern (sec-parsepattern-annexb), which parses once without [NamedCaptureGroups] and again with
  it when the pattern holds a GroupName. With annex_b=False the main grammar alone is read (sec-parsepattern), which
  the generator uses to tell an outcome that holds only with Annex B from one that holds without it;
- every early error of 22.2.1.1 (sec-patterns-static-semantics-early-errors) and B.1.2.1's
  (sec-patterns-static-semantics-early-errors-annexb), with the static semantics they read: CountLeftCapturingParens*,
  MightBothParticipate, CapturingGroupNumber, IsCharacterClass, CharacterValue, MayContainStrings,
  GroupSpecifiersThatMatch, CapturingGroupName, RegExpIdentifierCodePoints;
- the pattern semantics of 22.2.2 (sec-pattern-semantics): CompilePattern, CompileSubpattern with RepeatMatcher,
  EmptyMatcher, MatchTwoAlternatives and MatchSequence (direction included, for lookbehind), CompileAssertion with
  IsWordChar, CompileQuantifier, CompileAtom with CharacterSetMatcher, BackreferenceMatcher, Canonicalize (both
  branches: simple case folding from CaseFolding.txt under `u` or `v`, toUppercase otherwise) and UpdateModifiers
  (`(?ims-ims:)`), CompileCharacterClass, CompileToCharSet with CharacterRange, WordCharacters, AllCharacters,
  MaybeSimpleCaseFolding, CharacterComplement, UnicodeMatchProperty and UnicodeMatchPropertyValue, and
  CompileClassSetString; B.1.2.5 to B.1.2.8's amendments, CharacterRangeOrUnion among them;
- RegExpInitialize (sec-regexpinitialize), RegExpBuiltinExec (sec-regexpbuiltinexec) with AdvanceStringIndex,
  GetStringIndex, GetMatchString and MakeMatchIndicesIndexPairArray, so a result has its index, captures, groups
  (duplicate names included, ES2025) and `d` indices; RegExp.prototype [ %Symbol.match% ] and [ %Symbol.split% ] and
  [ %Symbol.search% ] for an ordinary RegExp; the `flags` getter; RegExp.escape with EncodeForRegExpEscape.

HOW IT IS WRITTEN. The closures are the text's: a Matcher takes a MatchState and a MatcherContinuation and returns a
MatchState or ~failure~, and backtracking is by continuation, as CompileSubpattern writes it. Each Abstract Closure is
a Python generator function: where the text calls a closure and goes on with its result, the code writes
`r = yield m(x, c)`; where the text returns a closure's result, `return Tail(m(x, c))`. A trampoline (run()) makes the
calls, so a pattern or a subject of any size is limited by memory and never by Python's stack, and a tail call holds
no frame. Nothing is memoised and no step is skipped: the order of the search is the text's.

THE STEP COUNTER. One step is one invocation of a Matcher: an Abstract Closure of the kind 22.2.2.1 (sec-pattern-
notation) calls a Matcher, whatever produced it - CharacterSetMatcher, BackreferenceMatcher, EmptyMatcher,
MatchSequence, MatchTwoAlternatives, CompileAssertion's matchers, the matcher of Atom :: `(` GroupSpecifier?
Disjunction `)`, and the matcher of Term :: Atom Quantifier (each of whose iterations RepeatMatcher makes by
invoking the Atom's matcher). MatcherContinuations, RepeatMatcher itself (an abstract operation, not a closure), and
the Abstract Closure CompilePattern returns are not counted, and neither is parsing or compilation. The count of an
exec is summed over every call RegExpBuiltinExec makes of that closure (every start position) and over every
look-around's inner match. With a cut-off N, the (N + 1)th invocation is not made and the answer is "exhausted at N"
(Exhausted). This is the count Broiler.Regex's tests/Adversarial/README.md, section 3, names ("the number of
invocations of ECMA-262 22.2.2's Matcher closures, as regexpref will report it"), and the count whose hundredfold
margin decides a `status: exhausted` vector (JSD-0081 decision D25).

WHERE THE TEXT HAS NO ANSWER. A Match Record whose start passes its end (row M4's empty match from a lastIndex inside a
surrogate pair, which GetMatchString's assertion forbids) raises SpecAssertion: the reference gives no answer, and
JSD-0081's decision D41 classes the case. A CountLeftCapturingParensWithin of 2^32 - 1 or more is not reachable by
any input this reference is given and is not checked.

Strings are lists of UTF-16 code units (ints) at the edges; StringToCodePoints pairs a lead surrogate with a trail
one, and leaves any other surrogate a code point of its own. The module is deterministic: it iterates no set or dict
whose order depends on string hashing, reads no locale and no environment but unicoderef's pin directory.
"""

from __future__ import annotations

import argparse
import gc
import json
import sys
from bisect import bisect_right
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import unicoderef as U  # noqa: E402  (unicoderef reads the pinned UCD; it is not used for matching)

REFERENCE_VERSION = "regexpref@1"
EDITION = "es2026"
EDITION_REVISION = "0248456c758431e4bb8e5d26333ff1865123c9cd"
INFINITY = float("inf")
MAX_CODE_POINT = 0x10FFFF

FORWARD = "forward"
BACKWARD = "backward"


class _Failure:
    """The text's ~failure~."""

    __slots__ = ()

    def __repr__(self) -> str:
        return "failure"


FAILURE = _Failure()


class PatternSyntaxError(Exception):
    """A SyntaxError the text throws: a pattern the grammar does not derive, an early error, or a flag string
    RegExpInitialize refuses. `code` names which, `position` is the code point where it was found."""

    def __init__(self, code: str, position: int = -1, detail: str = "") -> None:
        super().__init__(f"{code} at {position}: {detail}" if detail else f"{code} at {position}")
        self.code = code
        self.position = position
        self.detail = detail


class Exhausted(Exception):
    """The step counter reached its cut-off: the (cutoff + 1)th Matcher invocation was not made."""

    def __init__(self, cutoff: int) -> None:
        super().__init__(f"exhausted at {cutoff}")
        self.cutoff = cutoff


class SpecAssertion(Exception):
    """An assertion of the text fails for this input: the text gives no answer."""


# ---- Strings (6.1.4, 7.1.4.1.1, 11.1.4) --------------------------------------------------------------------------


def is_lead(unit) -> bool:
    return unit is not None and 0xD800 <= unit <= 0xDBFF


def is_trail(unit) -> bool:
    return unit is not None and 0xDC00 <= unit <= 0xDFFF


def utf16(text: str) -> list[int]:
    """A Python str (lone surrogates allowed) as UTF-16 code units."""
    data = text.encode("utf-16-le", errors="surrogatepass")
    return [data[i] | (data[i + 1] << 8) for i in range(0, len(data), 2)]


def string_to_code_points(units) -> list[int]:
    """StringToCodePoints (sec-stringtocodepoints): CodePointAt over the string."""
    out, i, n = [], 0, len(units)
    while i < n:
        unit = units[i]
        if is_lead(unit) and i + 1 < n and is_trail(units[i + 1]):
            out.append(0x10000 + ((unit - 0xD800) << 10) + (units[i + 1] - 0xDC00))
            i += 2
        else:
            out.append(unit)
            i += 1
    return out


def code_point_units(cp: int) -> list[int]:
    """UTF16EncodeCodePoint."""
    if cp < 0x10000:
        return [cp]
    cp -= 0x10000
    return [0xD800 + (cp >> 10), 0xDC00 + (cp & 0x3FF)]


def code_points_to_units(cps) -> list[int]:
    """CodePointsToString."""
    out: list[int] = []
    for cp in cps:
        out.extend(code_point_units(cp))
    return out


def code_point_at(units, index: int) -> tuple[int, int]:
    """CodePointAt (sec-codepointat): the code point at a code unit index and its code unit count."""
    first = units[index]
    if not (is_lead(first) or is_trail(first)):
        return first, 1
    if is_trail(first) or index + 1 == len(units):
        return first, 1
    second = units[index + 1]
    if not is_trail(second):
        return first, 1
    return 0x10000 + ((first - 0xD800) << 10) + (second - 0xDC00), 2


def advance_string_index(units, index: int, unicode: bool) -> int:
    """AdvanceStringIndex (sec-advancestringindex)."""
    if not unicode:
        return index + 1
    if index + 1 >= len(units):
        return index + 1
    return index + code_point_at(units, index)[1]


def get_string_index(units, code_point_index: int) -> int:
    """GetStringIndex (sec-getstringindex)."""
    if not units:
        return 0
    length, code_units, code_points = len(units), 0, 0
    while code_units < length:
        if code_points == code_point_index:
            return code_units
        code_units += code_point_at(units, code_units)[1]
        code_points += 1
    return length


# ---- CharSets (22.2.2.1's CharSet), as range lists and, under v, sets of strings ----------------------------------


def _normalize(ranges) -> tuple[tuple[int, int], ...]:
    out: list[list[int]] = []
    for a, b in sorted(ranges):
        if a > b:
            continue
        if out and a <= out[-1][1] + 1:
            if b > out[-1][1]:
                out[-1][1] = b
        else:
            out.append([a, b])
    return tuple((a, b) for a, b in out)


def _intersect(x, y) -> tuple[tuple[int, int], ...]:
    out, i, j = [], 0, 0
    while i < len(x) and j < len(y):
        a = max(x[i][0], y[j][0])
        b = min(x[i][1], y[j][1])
        if a <= b:
            out.append((a, b))
        if x[i][1] < y[j][1]:
            i += 1
        else:
            j += 1
    return tuple(out)


def _subtract(x, y) -> tuple[tuple[int, int], ...]:
    out = []
    j = 0
    for a, b in x:
        lo = a
        while j < len(y) and y[j][1] < lo:
            j += 1
        k = j
        while k < len(y) and y[k][0] <= b:
            if y[k][0] > lo:
                out.append((lo, y[k][0] - 1))
            lo = max(lo, y[k][1] + 1)
            k += 1
        if lo <= b:
            out.append((lo, b))
    return tuple(out)


class CharSet:
    """A CharSet: its single characters as sorted inclusive ranges, and (under v only) its CharSetElements of any
    other length as tuples of code points, the empty sequence included."""

    __slots__ = ("ranges", "strings", "_starts")

    def __init__(self, ranges=(), strings=()) -> None:
        self.ranges = _normalize(ranges)
        singles = [s[0] for s in strings if len(s) == 1]
        if singles:
            self.ranges = _normalize(list(self.ranges) + [(c, c) for c in singles])
        self.strings = frozenset(tuple(s) for s in strings if len(s) != 1)
        self._starts = [a for a, _ in self.ranges]

    @staticmethod
    def of(*cps: int) -> "CharSet":
        return CharSet([(c, c) for c in cps])

    def contains(self, cp: int) -> bool:
        i = bisect_right(self._starts, cp) - 1
        return i >= 0 and self.ranges[i][1] >= cp

    def union(self, other: "CharSet") -> "CharSet":
        return CharSet(self.ranges + other.ranges, self.strings | other.strings)

    def intersection(self, other: "CharSet") -> "CharSet":
        return CharSet(_intersect(self.ranges, other.ranges), self.strings & other.strings)

    def difference(self, other: "CharSet") -> "CharSet":
        return CharSet(_subtract(self.ranges, other.ranges), self.strings - other.strings)

    def single_count(self) -> int:
        return sum(b - a + 1 for a, b in self.ranges)

    def only_character(self):
        """The one character of a CharSet that holds exactly one, else None."""
        if self.strings or len(self.ranges) != 1 or self.ranges[0][0] != self.ranges[0][1]:
            return None
        return self.ranges[0][0]

    def sorted_strings(self) -> list[tuple[int, ...]]:
        return sorted(self.strings, key=lambda s: (-len(s), s))


# ---- The Unicode data, read once through unicoderef (sources/unicode.pin) -----------------------------------------


@lru_cache(maxsize=1)
def ucd() -> U.UCD:
    return U.database()


@lru_cache(maxsize=1)
def _lexical_sets() -> dict[str, CharSet]:
    sets = ucd().sets()
    return {
        "IdentifierStartChar": CharSet(sets["lexical:IdentifierStartChar"]),
        "IdentifierPartChar": CharSet(sets["lexical:IdentifierPartChar"]),
        "WhiteSpace": CharSet(sets["lexical:WhiteSpace"]),
        "LineTerminator": CharSet(sets["lexical:LineTerminator"]),
        "ID_Continue": CharSet(ucd().binary["ID_Continue"]),
    }


def lexical(name: str) -> CharSet:
    return _lexical_sets()[name]


@lru_cache(maxsize=1)
def _folding_sources() -> tuple[int, ...]:
    """The code points CaseFolding.txt maps (status C or S) to another code point, sorted."""
    return tuple(sorted(c for c, t in ucd().scf.items() if t != c))


def scf(cp: int) -> int:
    """scf (sec-maybesimplecasefolding): CaseFolding.txt's simple or common mapping, every other code point itself."""
    return ucd().fold(cp)


@lru_cache(maxsize=1)
def _canonicalize_bmp() -> tuple[int, ...]:
    """Canonicalize's second branch (rer.[[IgnoreCase]] true, neither u nor v) for every code unit."""
    data = ucd()
    return tuple(data.canonicalize_without_u(unit) for unit in range(0x10000))


@lru_cache(maxsize=1)
def _canonicalize_bmp_preimages() -> dict[int, tuple[int, ...]]:
    table = _canonicalize_bmp()
    pre: dict[int, list[int]] = {}
    for unit, value in enumerate(table):
        pre.setdefault(value, []).append(unit)
    return {k: tuple(v) for k, v in pre.items()}


def property_charset(key: str) -> CharSet:
    """The CharSet a resolved property key names: code points, and for a property of strings its strings too."""
    return _property_charset(key)


@lru_cache(maxsize=None)
def _property_charset(key: str) -> CharSet:
    data = ucd()
    ranges = data.sets()[key]
    strings = ()
    if key in data.string_members:
        strings = tuple(s for s in data.string_members[key] if len(s) != 1)
    return CharSet(ranges, strings)


# ---- RegExp Records and MatchStates (sec-regexp-records, sec-pattern-notation) ---------------------------------------


@dataclass(frozen=True)
class RegExpRecord:
    ignore_case: bool
    multiline: bool
    dot_all: bool
    unicode: bool
    unicode_sets: bool
    capturing_groups_count: int


def has_either_unicode_flag(rer: RegExpRecord) -> bool:
    """HasEitherUnicodeFlag (sec-runtime-semantics-haseitherunicodeflag-abstract-operation)."""
    return rer.unicode or rer.unicode_sets


class MatchState:
    """A MatchState { [[Input]], [[EndIndex]], [[Captures]] }; Captures is a tuple indexed 1 to n (index 0 unused),
    each a CaptureRange (start, end) or None for undefined."""

    __slots__ = ("input", "end", "captures")

    def __init__(self, input_, end: int, captures: tuple) -> None:
        self.input = input_
        self.end = end
        self.captures = captures


# ---- The trampoline and the step counter ----------------------------------------------------------------------------


class Tail:
    """`return Tail(call)`: the closure returns what the call returns, and the call takes its frame."""

    __slots__ = ("call",)

    def __init__(self, call) -> None:
        self.call = call


def run(call):
    """Runs an Abstract Closure call to its result. A closure is a generator: it yields each call it waits on and
    receives that call's result."""
    # The pending calls are many and acyclic; the cyclic collector would only rescan them, so it is suspended while a
    # match runs. It changes no answer and no count.
    collecting = gc.isenabled()
    gc.disable()
    try:
        stack = [call]
        value = None
        while True:
            try:
                request = stack[-1].send(value)
            except StopIteration as stop:
                stack.pop()
                value = stop.value
                if type(value) is Tail:
                    stack.append(value.call)
                    value = None
                    continue
                if not stack:
                    return value
                continue
            stack.append(request)
            value = None
    finally:
        if collecting:
            gc.enable()


class Meter:
    """The step counter: one step per Matcher invocation, with an optional cut-off."""

    __slots__ = ("count", "cutoff")

    def __init__(self, cutoff=None) -> None:
        self.count = 0
        self.cutoff = cutoff


class Context:
    """What every compiled Matcher of one RegExp shares: the meter of the exec running it."""

    __slots__ = ("meter",)

    def __init__(self) -> None:
        self.meter = Meter()


def matcher(ctx: Context, body):
    """A Matcher: invoking it is one step."""

    def invoke(x, c):
        meter = ctx.meter
        meter.count += 1
        if meter.cutoff is not None and meter.count > meter.cutoff:
            meter.count -= 1
            raise Exhausted(meter.cutoff)
        return body(x, c)

    return invoke


# ---- The parse tree ------------------------------------------------------------------------------------------------


class Node:
    __slots__ = ()


class Disjunction(Node):
    __slots__ = ("alternatives",)

    def __init__(self, alternatives) -> None:
        self.alternatives = alternatives


class Alternative(Node):
    __slots__ = ("terms",)

    def __init__(self, terms) -> None:
        self.terms = terms


class Assertion(Node):
    """`^`, `$`, `\\b` or `\\B`."""

    __slots__ = ("kind",)

    def __init__(self, kind: str) -> None:
        self.kind = kind


class LookAround(Node):
    __slots__ = ("ahead", "negative", "disjunction")

    def __init__(self, ahead: bool, negative: bool, disjunction) -> None:
        self.ahead = ahead
        self.negative = negative
        self.disjunction = disjunction


class Quantified(Node):
    """Term :: Atom Quantifier, or B.1.2's QuantifiableAssertion Quantifier."""

    __slots__ = ("atom", "min", "max", "greedy", "paren_index", "paren_count")

    def __init__(self, atom, minimum, maximum, greedy, paren_index, paren_count) -> None:
        self.atom = atom
        self.min = minimum
        self.max = maximum
        self.greedy = greedy
        self.paren_index = paren_index
        self.paren_count = paren_count


class Character(Node):
    """A one-character Atom: PatternCharacter, ExtendedPatternCharacter, an AtomEscape :: CharacterEscape, or B.1.2's
    `\\` [lookahead = c]."""

    __slots__ = ("value",)

    def __init__(self, value: int) -> None:
        self.value = value


class Dot(Node):
    __slots__ = ()


class ClassEscape(Node):
    """A CharacterClassEscape: `d D s S w W`, or `p{...}` / `P{...}` with its resolved property key."""

    __slots__ = ("kind", "key", "strings")

    def __init__(self, kind: str, key: str | None = None, strings: bool = False) -> None:
        self.kind = kind
        self.key = key
        self.strings = strings  # MayContainStrings of a \p{...}


class Group(Node):
    __slots__ = ("number", "name", "disjunction")

    def __init__(self, number: int, name, disjunction) -> None:
        self.number = number
        self.name = name
        self.disjunction = disjunction


class Modifiers(Node):
    __slots__ = ("add", "remove", "disjunction")

    def __init__(self, add: str, remove: str, disjunction) -> None:
        self.add = add
        self.remove = remove
        self.disjunction = disjunction


class Backreference(Node):
    """AtomEscape :: DecimalEscape (numbers, one element) or `k` GroupName (name, resolved after the parse)."""

    __slots__ = ("numbers", "name")

    def __init__(self, numbers=None, name=None) -> None:
        self.numbers = numbers
        self.name = name


class ClassRanges(Node):
    """A CharacterClass without v: its items, each a ClassAtom (Character or ClassEscape) or a (first, second)
    range."""

    __slots__ = ("items", "invert")

    def __init__(self, items, invert: bool) -> None:
        self.items = items
        self.invert = invert


class SetClass(Node):
    """A v-mode CharacterClass or NestedClass: `[`, `[^` and its ClassSetExpression (None for [empty])."""

    __slots__ = ("expression", "invert")

    def __init__(self, expression, invert: bool) -> None:
        self.expression = expression
        self.invert = invert


class SetUnion(Node):
    __slots__ = ("items",)

    def __init__(self, items) -> None:
        self.items = items


class SetIntersection(Node):
    __slots__ = ("operands",)

    def __init__(self, operands) -> None:
        self.operands = operands


class SetSubtraction(Node):
    __slots__ = ("operands",)

    def __init__(self, operands) -> None:
        self.operands = operands


class SetCharacter(Node):
    """ClassSetCharacter as a ClassSetOperand."""

    __slots__ = ("value",)

    def __init__(self, value: int) -> None:
        self.value = value


class SetRange(Node):
    __slots__ = ("first", "second")

    def __init__(self, first: int, second: int) -> None:
        self.first = first
        self.second = second


class SetStrings(Node):
    """ClassStringDisjunction `\\q{...}`: its ClassStrings, each a tuple of code points."""

    __slots__ = ("strings",)

    def __init__(self, strings) -> None:
        self.strings = strings


# ---- The parser (22.2.1 and B.1.2) ----------------------------------------------------------------------------------

SYNTAX_CHARACTERS = frozenset(map(ord, "^$\\.*+?()[]{}|"))
EXTENDED_PATTERN_EXCLUDED = frozenset(map(ord, "^$\\.*+?()[|"))
CONTROL_ESCAPES = {ord("t"): 9, ord("n"): 10, ord("v"): 11, ord("f"): 12, ord("r"): 13}
CLASS_SET_SYNTAX_CHARACTERS = frozenset(map(ord, "()[]{}/-\\|"))
CLASS_SET_RESERVED_PUNCTUATORS = frozenset(map(ord, "&-!#%,:;<=>@`~"))
CLASS_SET_RESERVED_DOUBLE_PUNCTUATORS = frozenset(map(ord, "&!#$%*+,.:;<=>?@^`~"))
DECIMAL_DIGITS = frozenset(range(0x30, 0x3A))
OCTAL_DIGITS = frozenset(range(0x30, 0x38))
HEX_DIGITS = frozenset(map(ord, "0123456789abcdefABCDEF"))
ASCII_LETTERS = frozenset(list(range(0x41, 0x5B)) + list(range(0x61, 0x7B)))
PROPERTY_NAME_CHARACTERS = ASCII_LETTERS | {0x5F}
PROPERTY_VALUE_CHARACTERS = PROPERTY_NAME_CHARACTERS | DECIMAL_DIGITS
MODIFIER_LETTERS = frozenset(map(ord, "ims"))
LINE_TERMINATORS = frozenset((0x0A, 0x0D, 0x2028, 0x2029))


@dataclass
class GroupSpecifier:
    name: tuple[int, ...]       # CapturingGroupName as UTF-16 code units
    number: int                 # the capturing group's number, CountLeftCapturingParensBefore + 1
    ancestry: tuple             # (disjunction id, alternative index) of every Disjunction it is contained within
    position: int


class Parser:
    """A recursive-descent reading of one goal symbol: Pattern[?UnicodeMode, ?UnicodeSetsMode, ?NamedCaptureGroups],
    the main grammar when unicode_mode is set or annex_b is off, B.1.2's otherwise."""

    def __init__(self, text: list[int], unicode_mode: bool, unicode_sets: bool, named_groups: bool, annex_b: bool,
                 total_groups) -> None:
        self.text = text
        self.pos = 0
        self.u = unicode_mode
        self.v = unicode_sets
        self.n = named_groups
        self.annex_b = annex_b and not unicode_mode
        self.total_groups = total_groups  # B.1.2's DecimalEscape condition reads the Pattern's count
        self.groups = 0
        self.specifiers: list[GroupSpecifier] = []
        self.named_references: list[tuple[tuple[int, ...], Backreference, int]] = []
        self.decimal_references: list[tuple[int, int]] = []
        self.ancestry: list[tuple[int, int]] = []
        self.disjunctions = 0
        self.has_group_name = False
        self.depth = 0
        self.max_depth = 0

    # -- reading

    def peek(self, k: int = 0):
        i = self.pos + k
        return self.text[i] if i < len(self.text) else None

    def at(self, s: str) -> bool:
        return all(self.peek(i) == ord(ch) for i, ch in enumerate(s))

    def fail(self, code: str, detail: str = "") -> PatternSyntaxError:
        return PatternSyntaxError(code, self.pos, detail)

    def expect(self, ch: str, what: str) -> None:
        if self.peek() != ord(ch):
            raise self.fail("grammar:" + what)
        self.pos += 1

    def enter(self) -> None:
        self.depth += 1
        self.max_depth = max(self.max_depth, self.depth)

    def leave(self) -> None:
        self.depth -= 1

    # -- Pattern, Disjunction, Alternative, Term

    def parse(self) -> Disjunction:
        self.enter()
        result = self.disjunction()
        self.leave()
        if self.pos != len(self.text):
            raise self.fail("grammar:unmatched-close" if self.peek() == ord(")") else "grammar:unexpected-character")
        return result

    def disjunction(self) -> Disjunction:
        identifier = self.disjunctions
        self.disjunctions += 1
        alternatives = []
        index = 0
        while True:
            self.ancestry.append((identifier, index))
            alternatives.append(self.alternative())
            self.ancestry.pop()
            if self.peek() != ord("|"):
                break
            self.pos += 1
            index += 1
        return Disjunction(alternatives)

    def alternative(self) -> Alternative:
        terms = []
        while True:
            c = self.peek()
            if c is None or c == ord("|") or c == ord(")"):
                return Alternative(terms)
            terms.append(self.term())

    def term(self):
        start_groups = self.groups
        if not self.annex_b:
            assertion = self.assertion(quantifiable_only=False)
            if assertion is not None:
                return assertion
            atom = self.atom()
        else:
            # B.1.2: QuantifiableAssertion Quantifier | Assertion | ExtendedAtom Quantifier | ExtendedAtom
            if self.at("(?=") or self.at("(?!"):
                atom = self.assertion(quantifiable_only=True)
            else:
                assertion = self.assertion(quantifiable_only=False)
                if assertion is not None:
                    return assertion
                atom = self.extended_atom()
        quantifier = self.quantifier()
        if quantifier is None:
            return atom
        minimum, maximum, greedy = quantifier
        return Quantified(atom, minimum, maximum, greedy, start_groups, self.groups - start_groups)

    def assertion(self, quantifiable_only: bool):
        c = self.peek()
        if not quantifiable_only:
            if c == ord("^") or c == ord("$"):
                self.pos += 1
                return Assertion(chr(c))
            if c == ord("\\") and self.peek(1) in (ord("b"), ord("B")):
                self.pos += 2
                return Assertion(chr(self.text[self.pos - 1]))
        for opener, ahead, negative in (("(?=", True, False), ("(?!", True, True), ("(?<=", False, False),
                                        ("(?<!", False, True)):
            if quantifiable_only and not ahead:
                continue
            if self.at(opener):
                self.pos += len(opener)
                self.enter()
                body = self.disjunction()
                self.leave()
                self.expect(")", "unterminated-group")
                return LookAround(ahead, negative, body)
        return None

    def quantifier(self):
        c = self.peek()
        if c in (ord("*"), ord("+"), ord("?")):
            self.pos += 1
            minimum, maximum = {ord("*"): (0, INFINITY), ord("+"): (1, INFINITY), ord("?"): (0, 1)}[c]
        elif c == ord("{"):
            braced = self.braced_quantifier(self.pos)
            if braced is None:
                return None
            minimum, maximum, end = braced
            start = self.pos
            self.pos = end
            if maximum != INFINITY and minimum > maximum:
                raise PatternSyntaxError("early:quantifier-range", start, "{n,m} with n > m")
        else:
            return None
        greedy = True
        if self.peek() == ord("?"):
            self.pos += 1
            greedy = False
        return minimum, maximum, greedy

    def braced_quantifier(self, at: int):
        """QuantifierPrefix's braced forms at `at`: (min, max, end) or None."""
        text = self.text

        def digits(i):
            j = i
            while j < len(text) and text[j] in DECIMAL_DIGITS:
                j += 1
            return (int("".join(map(chr, text[i:j]))), j) if j > i else (None, i)

        if at >= len(text) or text[at] != ord("{"):
            return None
        first, i = digits(at + 1)
        if first is None:
            return None
        if i < len(text) and text[i] == ord("}"):
            return first, first, i + 1
        if i >= len(text) or text[i] != ord(","):
            return None
        second, j = digits(i + 1)
        if j < len(text) and text[j] == ord("}"):
            return first, (INFINITY if second is None else second), j + 1
        return None

    # -- Atoms

    def atom(self):
        """Atom of the main grammar."""
        c = self.peek()
        if c is None:
            raise self.fail("grammar:unexpected-end")
        if c == ord("."):
            self.pos += 1
            return Dot()
        if c == ord("("):
            return self.group()
        if c == ord("["):
            return self.character_class()
        if c == ord("\\"):
            self.pos += 1
            escape = self.atom_escape()
            if escape is None:
                raise self.fail("grammar:invalid-escape")
            return escape
        if c in SYNTAX_CHARACTERS:
            if c in (ord("*"), ord("+"), ord("?")) or (c == ord("{")):
                raise self.fail("grammar:nothing-to-repeat")
            raise self.fail("grammar:lone-syntax-character")
        self.pos += 1
        return Character(c)

    def extended_atom(self):
        """B.1.2's ExtendedAtom."""
        c = self.peek()
        if c is None:
            raise self.fail("grammar:unexpected-end")
        if c == ord("."):
            self.pos += 1
            return Dot()
        if c == ord("\\"):
            start = self.pos
            self.pos += 1
            escape = self.atom_escape()
            if escape is not None:
                return escape
            if self.peek() == ord("c"):
                # ExtendedAtom :: `\` [lookahead = c]: the backslash alone.
                return Character(0x5C)
            self.pos = start
            raise self.fail("grammar:invalid-escape")
        if c == ord("["):
            return self.character_class()
        if c == ord("("):
            return self.group()
        if c == ord("{") and self.braced_quantifier(self.pos) is not None:
            raise self.fail("early:invalid-braced-quantifier", "B.1.2.1: ExtendedAtom :: InvalidBracedQuantifier")
        if c in EXTENDED_PATTERN_EXCLUDED:
            if c in (ord("*"), ord("+"), ord("?")):
                raise self.fail("grammar:nothing-to-repeat")
            raise self.fail("grammar:lone-syntax-character")
        self.pos += 1
        return Character(c)

    def group(self):
        start = self.pos
        self.pos += 1  # (
        if self.peek() != ord("?"):
            self.groups += 1
            number = self.groups
            self.enter()
            body = self.disjunction()
            self.leave()
            self.expect(")", "unterminated-group")
            return Group(number, None, body)
        self.pos += 1  # ?
        if self.peek() == ord("<"):
            self.groups += 1
            number = self.groups
            name = self.group_name()
            self.has_group_name = True
            self.specifiers.append(GroupSpecifier(tuple(name), number, tuple(self.ancestry), start))
            self.enter()
            body = self.disjunction()
            self.leave()
            self.expect(")", "unterminated-group")
            return Group(number, tuple(name), body)
        add, remove, dash = [], [], False
        while self.peek() is not None and self.peek() in MODIFIER_LETTERS:
            add.append(self.peek())
            self.pos += 1
        if self.peek() == ord("-"):
            dash = True
            self.pos += 1
            while self.peek() is not None and self.peek() in MODIFIER_LETTERS:
                remove.append(self.peek())
                self.pos += 1
        if self.peek() != ord(":"):
            raise self.fail("grammar:invalid-group")
        self.pos += 1
        if len(set(add)) != len(add):
            raise PatternSyntaxError("early:modifier-repeated", start, "the first RegularExpressionModifiers")
        if dash:
            if not add and not remove:
                raise PatternSyntaxError("early:modifiers-both-empty", start)
            if len(set(remove)) != len(remove):
                raise PatternSyntaxError("early:modifier-repeated", start, "the second RegularExpressionModifiers")
            if set(add) & set(remove):
                raise PatternSyntaxError("early:modifier-added-and-removed", start)
        self.enter()
        body = self.disjunction()
        self.leave()
        self.expect(")", "unterminated-group")
        return Modifiers("".join(map(chr, add)), "".join(map(chr, remove)), body)

    def group_name(self) -> list[int]:
        """GroupName :: `<` RegExpIdentifierName `>`, its CapturingGroupName as UTF-16 code units."""
        self.expect("<", "invalid-group-name")
        cps: list[int] = []
        while True:
            c = self.peek()
            if c == ord(">") and cps:
                self.pos += 1
                return code_points_to_units(cps)
            start = len(cps) == 0
            allowed = lexical("IdentifierStartChar" if start else "IdentifierPartChar")
            if c == ord("\\"):
                self.pos += 1
                if self.peek() != ord("u"):
                    raise self.fail("grammar:invalid-group-name", "only a \\u escape")
                self.pos += 1
                value = self.unicode_escape(plus_unicode_mode=True)
                if not allowed.contains(value):
                    raise self.fail("early:group-name-escape",
                                    "RegExpIdentifierStart" if start else "RegExpIdentifierPart")
                cps.append(value)
            elif not self.u and is_lead(c) and is_trail(self.peek(1)):
                value = 0x10000 + ((c - 0xD800) << 10) + (self.peek(1) - 0xDC00)
                self.pos += 2
                # UnicodeIDStart / UnicodeIDContinue: for a supplementary code point these are IdentifierStartChar
                # and IdentifierPartChar, whose additions ($, _, ZWNJ, ZWJ) are all in the BMP.
                if not allowed.contains(value):
                    raise self.fail("early:group-name-surrogate-pair",
                                    "UnicodeIDStart" if start else "UnicodeIDContinue")
                cps.append(value)
            elif c is not None and allowed.contains(c):
                self.pos += 1
                cps.append(c)
            else:
                raise self.fail("grammar:invalid-group-name")

    def atom_escape(self):
        """AtomEscape after its `\\`: a node, or None where the grammar derives none (the caller decides)."""
        c = self.peek()
        if c is None:
            raise self.fail("grammar:trailing-backslash")
        # DecimalEscape
        if c in DECIMAL_DIGITS and c != ord("0"):
            start = self.pos
            j = self.pos
            while j < len(self.text) and self.text[j] in DECIMAL_DIGITS:
                j += 1
            number = int("".join(map(chr, self.text[self.pos:j])))
            if not self.annex_b:
                self.pos = j
                self.decimal_references.append((number, start))
                return Backreference(numbers=(number,))
            if number <= self.total_groups:
                self.pos = j
                return Backreference(numbers=(number,))
            # B.1.2: not a DecimalEscape; a CharacterEscape below.
        # CharacterClassEscape
        escape = self.character_class_escape()
        if escape is not None:
            return escape
        # CharacterEscape
        value = self.character_escape()
        if value is not None:
            return Character(value)
        # `k` GroupName
        if c == ord("k") and self.n:
            self.pos += 1
            start = self.pos
            name = self.group_name()
            node = Backreference(name=tuple(name))
            self.named_references.append((tuple(name), node, start))
            return node
        if self.u or not self.annex_b:
            raise self.fail("grammar:invalid-escape")
        return None

    def character_class_escape(self):
        c = self.peek()
        if c is not None and c in map(ord, "dDsSwW"):
            self.pos += 1
            return ClassEscape(chr(c))
        if self.u and c in (ord("p"), ord("P")) and self.peek(1) == ord("{"):
            return self.property_escape()
        return None

    def property_escape(self) -> ClassEscape:
        negated = self.peek() == ord("P")
        start = self.pos
        self.pos += 2
        j = self.pos
        while j < len(self.text) and self.text[j] in PROPERTY_VALUE_CHARACTERS:
            j += 1
        first = self.text[self.pos:j]
        data = ucd()
        if j < len(self.text) and self.text[j] == ord("="):
            if not first or any(ch not in PROPERTY_NAME_CHARACTERS for ch in first):
                raise PatternSyntaxError("grammar:invalid-property", start)
            k = j + 1
            while k < len(self.text) and self.text[k] in PROPERTY_VALUE_CHARACTERS:
                k += 1
            second = self.text[j + 1:k]
            if not second or k >= len(self.text) or self.text[k] != ord("}"):
                raise PatternSyntaxError("grammar:invalid-property", start)
            self.pos = k + 1
            name, value = "".join(map(chr, first)), "".join(map(chr, second))
            if name not in data.ecma_nonbinary:
                raise PatternSyntaxError("early:property-name", start, name)
            key = data.resolve_pair(name, value)
            if key == U.REFUSED:
                raise PatternSyntaxError("early:property-value", start, f"{name}={value}")
            return ClassEscape("P" if negated else "p", key, False)
        if not first or j >= len(self.text) or self.text[j] != ord("}"):
            raise PatternSyntaxError("grammar:invalid-property", start)
        self.pos = j + 1
        name = "".join(map(chr, first))
        if name in data.ecma_strings:
            if not self.v:
                raise PatternSyntaxError("early:property-of-strings-without-v", start, name)
            if negated:
                raise PatternSyntaxError("early:negated-property-of-strings", start, name)
            return ClassEscape("p", name, True)
        key = data.resolve_lone(name, unicode_sets=self.v)
        if key == U.REFUSED:
            raise PatternSyntaxError("early:lone-property", start, name)
        return ClassEscape("P" if negated else "p", key, False)

    def character_escape(self):
        """CharacterEscape[?UnicodeMode, ?NamedCaptureGroups]: a character value, or None (nothing consumed)."""
        c = self.peek()
        if c is None:
            return None
        if c in CONTROL_ESCAPES:
            self.pos += 1
            return CONTROL_ESCAPES[c]
        if c == ord("c"):
            if self.peek(1) is not None and self.peek(1) in ASCII_LETTERS:
                self.pos += 2
                return self.text[self.pos - 1] % 32
            return None
        if c == ord("0") and (self.peek(1) is None or self.peek(1) not in DECIMAL_DIGITS):
            self.pos += 1
            return 0
        if c == ord("x"):
            if self.peek(1) in HEX_DIGITS and self.peek(2) in HEX_DIGITS:
                value = int(chr(self.peek(1)) + chr(self.peek(2)), 16)
                self.pos += 3
                return value
        elif c == ord("u"):
            if self.u:
                self.pos += 1
                return self.unicode_escape(plus_unicode_mode=True)
            if all(self.peek(i) is not None and self.peek(i) in HEX_DIGITS for i in (1, 2, 3, 4)):
                value = int("".join(chr(self.peek(i)) for i in (1, 2, 3, 4)), 16)
                self.pos += 5
                return value
        elif self.annex_b and c in OCTAL_DIGITS:
            return self.legacy_octal()
        # IdentityEscape
        if self.u:
            if c in SYNTAX_CHARACTERS or c == ord("/"):
                self.pos += 1
                return c
            return None
        if self.annex_b:
            if c == ord("c") or (self.n and c == ord("k")):
                return None
            self.pos += 1
            return c
        if lexical("ID_Continue").contains(c):
            return None
        self.pos += 1
        return c

    def legacy_octal(self) -> int:
        """B.1.2's LegacyOctalEscapeSequence (12.9.4)."""
        a = self.peek()
        b = self.peek(1)
        c = self.peek(2)
        oct_ = OCTAL_DIGITS
        if a == ord("0") and b in (ord("8"), ord("9")):
            self.pos += 1
            return 0
        if b is None or b not in oct_:
            self.pos += 1
            return a - 0x30
        if a <= ord("3") and c is not None and c in oct_:
            self.pos += 3
            return (a - 0x30) * 64 + (b - 0x30) * 8 + (c - 0x30)
        self.pos += 2
        return (a - 0x30) * 8 + (b - 0x30)

    def unicode_escape(self, plus_unicode_mode: bool) -> int:
        """RegExpUnicodeEscapeSequence[+UnicodeMode] after its `u`."""
        if self.peek() == ord("{"):
            j = self.pos + 1
            while j < len(self.text) and self.text[j] in HEX_DIGITS:
                j += 1
            if j == self.pos + 1 or j >= len(self.text) or self.text[j] != ord("}"):
                raise self.fail("grammar:invalid-unicode-escape")
            value = int("".join(map(chr, self.text[self.pos + 1:j])), 16)
            if value > MAX_CODE_POINT:
                raise self.fail("grammar:invalid-unicode-escape", "CodePoint above 0x10FFFF")
            self.pos = j + 1
            return value
        if not all(self.peek(i) is not None and self.peek(i) in HEX_DIGITS for i in range(4)):
            raise self.fail("grammar:invalid-unicode-escape")
        value = int("".join(chr(self.peek(i)) for i in range(4)), 16)
        self.pos += 4
        if 0xD800 <= value <= 0xDBFF and self.peek() == ord("\\") and self.peek(1) == ord("u") and \
                all(self.peek(i) is not None and self.peek(i) in HEX_DIGITS for i in (2, 3, 4, 5)):
            trail = int("".join(chr(self.peek(i)) for i in (2, 3, 4, 5)), 16)
            if 0xDC00 <= trail <= 0xDFFF:
                self.pos += 6
                return 0x10000 + ((value - 0xD800) << 10) + (trail - 0xDC00)
        return value

    # -- CharacterClass without v

    def character_class(self):
        if self.v:
            return self.set_class()
        self.pos += 1  # [
        invert = False
        if self.peek() == ord("^"):
            invert = True
            self.pos += 1
        items = []
        while True:
            c = self.peek()
            if c is None:
                raise self.fail("grammar:unterminated-class")
            if c == ord("]"):
                self.pos += 1
                return ClassRanges(items, invert)
            start = self.pos
            first = self.class_atom()
            if self.peek() == ord("-") and self.peek(1) is not None and self.peek(1) != ord("]"):
                self.pos += 1
                second = self.class_atom()
                first_class = isinstance(first, ClassEscape)
                second_class = isinstance(second, ClassEscape)
                if first_class or second_class:
                    if not self.annex_b:
                        raise PatternSyntaxError("early:class-escape-in-range", start)
                elif first.value > second.value:
                    raise PatternSyntaxError("early:class-range-out-of-order", start)
                items.append((first, second))
            else:
                items.append(first)

    def class_atom(self):
        c = self.peek()
        if c != ord("\\"):
            self.pos += 1
            return Character(c)
        self.pos += 1
        c = self.peek()
        if c is None:
            raise self.fail("grammar:unterminated-class")
        if c == ord("b"):
            self.pos += 1
            return Character(8)
        if self.u and c == ord("-"):
            self.pos += 1
            return Character(0x2D)
        if self.annex_b and c == ord("c") and self.peek(1) is not None and \
                (self.peek(1) in DECIMAL_DIGITS or self.peek(1) == ord("_")):
            self.pos += 2
            return Character(self.text[self.pos - 1] % 32)
        escape = self.character_class_escape()
        if escape is not None:
            return escape
        value = self.character_escape()
        if value is not None:
            return Character(value)
        if self.annex_b and c == ord("c"):
            return Character(0x5C)  # ClassAtomNoDash :: `\` [lookahead = c]
        raise self.fail("grammar:invalid-class-escape")

    # -- CharacterClass and NestedClass under v

    def set_class(self) -> SetClass:
        start = self.pos
        self.pos += 1  # [
        self.enter()
        invert = False
        if self.peek() == ord("^"):
            invert = True
            self.pos += 1
        if self.peek() == ord("]"):
            self.pos += 1
            self.leave()
            return SetClass(None, invert)
        expression = self.class_set_expression()
        self.expect("]", "unterminated-class")
        self.leave()
        if invert and may_contain_strings(expression):
            raise PatternSyntaxError("early:negated-class-with-strings", start)
        return SetClass(expression, invert)

    def class_set_expression(self):
        first, is_range = self.union_item()
        if not is_range and self.at("&&"):
            operands = [first]
            while self.at("&&"):
                self.pos += 2
                if self.peek() == ord("&"):
                    raise self.fail("grammar:class-set-operator")
                operands.append(self.class_set_operand())
            if self.peek() != ord("]"):
                raise self.fail("grammar:class-set-operator", "mixed operators")
            return SetIntersection(operands)
        if not is_range and self.at("--"):
            operands = [first]
            while self.at("--"):
                self.pos += 2
                operands.append(self.class_set_operand())
            if self.peek() != ord("]"):
                raise self.fail("grammar:class-set-operator", "mixed operators")
            return SetSubtraction(operands)
        items = [first]
        while self.peek() is not None and self.peek() != ord("]"):
            if self.at("&&") or self.at("--"):
                raise self.fail("grammar:class-set-operator", "an operator after a union")
            item, _ = self.union_item()
            items.append(item)
        return SetUnion(items)

    def union_item(self):
        """ClassSetRange or ClassSetOperand: (node, is_range)."""
        operand = self.class_set_operand(allow_range=True)
        return operand, isinstance(operand, SetRange)

    def class_set_operand(self, allow_range: bool = False):
        c = self.peek()
        if c == ord("["):
            return self.set_class()
        if c == ord("\\"):
            n = self.peek(1)
            if n == ord("q") and self.peek(2) == ord("{"):
                return self.class_string_disjunction()
            if n is not None and (n in map(ord, "dDsSwW") or (n in (ord("p"), ord("P")) and self.peek(2) == ord("{"))):
                self.pos += 1
                return self.character_class_escape()
        start = self.pos
        value = self.class_set_character()
        if allow_range and self.peek() == ord("-") and self.peek(1) != ord("-"):
            self.pos += 1
            second = self.class_set_character()
            if value > second:
                raise PatternSyntaxError("early:class-set-range-out-of-order", start)
            return SetRange(value, second)
        return SetCharacter(value)

    def class_set_character(self) -> int:
        c = self.peek()
        if c is None:
            raise self.fail("grammar:unterminated-class")
        if c == ord("\\"):
            n = self.peek(1)
            if n == ord("b"):
                self.pos += 2
                return 8
            if n is not None and n in CLASS_SET_RESERVED_PUNCTUATORS:
                self.pos += 2
                return n
            self.pos += 1
            value = self.character_escape()
            if value is None:
                raise self.fail("grammar:invalid-class-escape")
            return value
        if c in CLASS_SET_RESERVED_DOUBLE_PUNCTUATORS and self.peek(1) == c:
            raise self.fail("grammar:class-set-reserved-double-punctuator")
        if c in CLASS_SET_SYNTAX_CHARACTERS:
            raise self.fail("grammar:class-set-syntax-character")
        self.pos += 1
        return c

    def class_string_disjunction(self) -> SetStrings:
        self.pos += 3  # \q{
        strings, current = [], []
        while True:
            c = self.peek()
            if c is None:
                raise self.fail("grammar:unterminated-class-string")
            if c == ord("}"):
                self.pos += 1
                strings.append(tuple(current))
                return SetStrings(strings)
            if c == ord("|"):
                self.pos += 1
                strings.append(tuple(current))
                current = []
                continue
            current.append(self.class_set_character())


def may_contain_strings(node) -> bool:
    """MayContainStrings (sec-static-semantics-maycontainstrings)."""
    if node is None:
        return False
    if isinstance(node, SetUnion):
        return any(may_contain_strings(item) for item in node.items)
    if isinstance(node, SetIntersection):
        return all(may_contain_strings(op) for op in node.operands)
    if isinstance(node, SetSubtraction):
        return may_contain_strings(node.operands[0])
    if isinstance(node, (SetCharacter, SetRange)):
        return False
    if isinstance(node, SetClass):
        return False if node.invert else may_contain_strings(node.expression)
    if isinstance(node, ClassEscape):
        return node.strings
    if isinstance(node, SetStrings):
        return any(len(s) != 1 for s in node.strings)
    raise TypeError(node)


@dataclass
class ParsedPattern:
    tree: Disjunction
    groups: int
    group_names: dict          # capture number -> CapturingGroupName (UTF-16 units tuple)
    has_group_name: bool
    depth: int                 # nesting depth as Broiler.VM counts it (JSD-0081 decision D7)
    unicode_mode: bool
    unicode_sets: bool


def _parse_goal(text, u, v, n, annex_b) -> ParsedPattern:
    total = None
    if annex_b and not u:
        # B.1.2's DecimalEscape reads CountLeftCapturingParensWithin(the Pattern). How a decimal escape is read
        # never moves a parenthesis, so a first parse that reads every one as a reference counts the groups.
        counting = Parser(text, u, v, n, annex_b, total_groups=INFINITY)
        counting.parse()
        total = counting.groups
    parser = Parser(text, u, v, n, annex_b, total_groups=total)
    tree = parser.parse()
    groups = parser.groups
    if total is not None and total != groups:
        raise AssertionError("the group count moved between the two parses")
    # Early errors of Pattern :: Disjunction (22.2.1.1)
    specifiers = parser.specifiers
    for i, x in enumerate(specifiers):
        for y in specifiers[i + 1:]:
            if x.name == y.name and might_both_participate(x, y):
                raise PatternSyntaxError("early:duplicate-group-name", y.position)
    for number, position in parser.decimal_references:
        if number > groups:
            raise PatternSyntaxError("early:decimal-escape-past-group-count", position)
    for name, node, position in parser.named_references:
        numbers = tuple(s.number for s in specifiers if s.name == name)
        if not numbers:
            raise PatternSyntaxError("early:undefined-group-name", position)
        node.numbers = numbers
    names = {s.number: s.name for s in specifiers}
    return ParsedPattern(tree, groups, names, parser.has_group_name, parser.max_depth, u, v)


def might_both_participate(x: GroupSpecifier, y: GroupSpecifier) -> bool:
    """MightBothParticipate (sec-mightbothparticipate): false when some Disjunction holds them in different
    alternatives."""
    alternatives = dict(x.ancestry)
    for disjunction, index in y.ancestry:
        if disjunction in alternatives and alternatives[disjunction] != index:
            return False
    return True


def _ensure_recursion(depth: int) -> None:
    """The parser and the compiler recurse once per nesting level, a few Python frames each; matching never does."""
    if sys.getrecursionlimit() < depth:
        sys.setrecursionlimit(depth)


def parse_pattern(pattern_units, u: bool, v: bool, annex_b: bool = True) -> ParsedPattern:
    """ParsePattern (sec-parsepattern; with annex_b, sec-parsepattern-annexb) of a pattern given as UTF-16 units,
    whose patternText RegExpInitialize forms: StringToCodePoints under u or v, each code unit otherwise."""
    if u and v:
        raise PatternSyntaxError("grammar:u-and-v", 0)
    _ensure_recursion(8 * len(pattern_units) + 10000)
    if u or v:
        return _parse_goal(string_to_code_points(pattern_units), True, v, True, annex_b)
    text = list(pattern_units)
    if not annex_b:
        return _parse_goal(text, False, False, True, False)
    first = _parse_goal(text, False, False, False, True)
    if first.has_group_name:
        return _parse_goal(text, False, False, True, True)
    return first


# ---- Character sets of the semantics (22.2.2.9) ---------------------------------------------------------------------


def canonicalize(rer: RegExpRecord, ch: int) -> int:
    """Canonicalize (sec-runtime-semantics-canonicalize-ch)."""
    if has_either_unicode_flag(rer) and rer.ignore_case:
        return scf(ch)
    if not rer.ignore_case:
        return ch
    if ch > 0xFFFF:
        raise SpecAssertion("Canonicalize without u or v: ch is not a code unit")
    return _canonicalize_bmp()[ch]


def canonical_preimage(rer: RegExpRecord, cc: int) -> tuple[int, ...]:
    """Every character a such that Canonicalize(rer, a) is cc."""
    if not rer.ignore_case:
        return (cc,)
    if has_either_unicode_flag(rer):
        return ucd().orbit(cc)
    return _canonicalize_bmp_preimages().get(cc, ())


def all_characters(rer: RegExpRecord) -> CharSet:
    """AllCharacters (sec-allcharacters)."""
    return _all_characters(rer.unicode_sets and rer.ignore_case, has_either_unicode_flag(rer))


@lru_cache(maxsize=None)
def _all_characters(folded: bool, unicode: bool) -> CharSet:
    if folded:
        return CharSet([(0, MAX_CODE_POINT)]).difference(CharSet([(c, c) for c in _folding_sources()]))
    if unicode:
        return CharSet([(0, MAX_CODE_POINT)])
    return CharSet([(0, 0xFFFF)])


def character_complement(rer: RegExpRecord, s: CharSet) -> CharSet:
    """CharacterComplement (sec-charactercomplement)."""
    return all_characters(rer).difference(s)


def maybe_simple_case_folding(rer: RegExpRecord, a: CharSet) -> CharSet:
    """MaybeSimpleCaseFolding (sec-maybesimplecasefolding)."""
    if not rer.unicode_sets or not rer.ignore_case:
        return a
    moved = [c for c in _folding_sources() if a.contains(c)]
    ranges = _subtract(a.ranges, _normalize((c, c) for c in moved))
    ranges = list(ranges) + [(scf(c), scf(c)) for c in moved]
    strings = [tuple(scf(cp) for cp in s) for s in a.strings]
    return CharSet(ranges, strings)


BASIC_WORD_CHARACTERS = CharSet([(0x30, 0x39), (0x41, 0x5A), (0x5F, 0x5F), (0x61, 0x7A)])


def word_characters(rer: RegExpRecord) -> CharSet:
    """WordCharacters (sec-wordcharacters)."""
    return _word_characters(rer.ignore_case, has_either_unicode_flag(rer))


@lru_cache(maxsize=None)
def _word_characters(ignore_case: bool, unicode: bool) -> CharSet:
    rer = RegExpRecord(ignore_case, False, False, unicode, False, 0)
    extra = []
    if ignore_case:
        candidates = _folding_sources() if unicode else range(0x10000)
        for c in candidates:
            if not BASIC_WORD_CHARACTERS.contains(c) and BASIC_WORD_CHARACTERS.contains(canonicalize(rer, c)):
                extra.append((c, c))
    return BASIC_WORD_CHARACTERS.union(CharSet(extra))


def class_escape_charset(rer: RegExpRecord, node: ClassEscape) -> CharSet:
    """CompileToCharSet of a CharacterClassEscape (sec-compiletocharset)."""
    kind = node.kind
    if kind == "d":
        return CharSet([(0x30, 0x39)])
    if kind == "D":
        return character_complement(rer, CharSet([(0x30, 0x39)]))
    if kind == "s":
        return lexical("WhiteSpace").union(lexical("LineTerminator"))
    if kind == "S":
        return character_complement(rer, lexical("WhiteSpace").union(lexical("LineTerminator")))
    if kind == "w":
        return maybe_simple_case_folding(rer, word_characters(rer))
    if kind == "W":
        return character_complement(rer, maybe_simple_case_folding(rer, word_characters(rer)))
    if kind == "p":
        return maybe_simple_case_folding(rer, property_charset(node.key))
    if kind == "P":
        return character_complement(rer, maybe_simple_case_folding(rer, property_charset(node.key)))
    raise ValueError(kind)


def class_ranges_charset(rer: RegExpRecord, node: ClassRanges) -> CharSet:
    """CompileToCharSet of ClassContents without v (sec-compiletocharset), with B.1.2's CharacterRangeOrUnion."""
    result = CharSet()

    def atom_set(atom):
        if isinstance(atom, ClassEscape):
            return class_escape_charset(rer, atom)
        return CharSet.of(atom.value)

    for item in node.items:
        if isinstance(item, tuple):
            a, b = atom_set(item[0]), atom_set(item[1])
            if not has_either_unicode_flag(rer) and (a.only_character() is None or b.only_character() is None):
                # CharacterRangeOrUnion (sec-runtime-semantics-characterrangeorunion-abstract-operation)
                result = result.union(a).union(b).union(CharSet.of(0x2D))
            else:
                result = result.union(CharSet([(a.only_character(), b.only_character())]))
        else:
            result = result.union(atom_set(item))
    return result


def set_charset(rer: RegExpRecord, node) -> CharSet:
    """CompileToCharSet of the v-mode productions."""
    if node is None:
        return CharSet()
    if isinstance(node, SetClass):
        inner = set_charset(rer, node.expression)
        return character_complement(rer, inner) if node.invert else inner
    if isinstance(node, SetUnion):
        result = CharSet()
        for item in node.items:
            result = result.union(set_charset(rer, item))
        return result
    if isinstance(node, SetIntersection):
        result = set_charset(rer, node.operands[0])
        for op in node.operands[1:]:
            result = result.intersection(set_charset(rer, op))
        return result
    if isinstance(node, SetSubtraction):
        result = set_charset(rer, node.operands[0])
        for op in node.operands[1:]:
            result = result.difference(set_charset(rer, op))
        return result
    if isinstance(node, SetRange):
        return maybe_simple_case_folding(rer, CharSet([(node.first, node.second)]))
    if isinstance(node, SetCharacter):
        return maybe_simple_case_folding(rer, CharSet.of(node.value))
    if isinstance(node, SetStrings):
        return maybe_simple_case_folding(rer, CharSet((), node.strings))
    if isinstance(node, ClassEscape):
        return class_escape_charset(rer, node)
    raise TypeError(node)


def dot_charset(rer: RegExpRecord) -> CharSet:
    a = all_characters(rer)
    if not rer.dot_all:
        a = a.difference(CharSet([(c, c) for c in sorted(LINE_TERMINATORS)]))
    return a


# ---- The Matchers (22.2.2.3 to 22.2.2.7) -----------------------------------------------------------------------------


def _continuation_identity(y):
    """The MatcherContinuation that returns its MatchState."""
    return y
    yield  # noqa: unreachable; makes the closure a generator


def empty_matcher(ctx: Context):
    """EmptyMatcher (sec-emptymatcher)."""

    def body(x, c):
        return Tail(c(x))
        yield

    return matcher(ctx, body)


def match_two_alternatives(ctx: Context, m1, m2):
    """MatchTwoAlternatives (sec-matchtwoalternatives)."""

    def body(x, c):
        r = yield m1(x, c)
        if r is not FAILURE:
            return r
        return Tail(m2(x, c))

    return matcher(ctx, body)


def match_sequence(ctx: Context, m1, m2, direction: str):
    """MatchSequence (sec-matchsequence)."""
    if direction == FORWARD:
        def body(x, c):
            def d(y):
                return Tail(m2(y, c))
                yield
            return Tail(m1(x, d))
            yield
    else:
        def body(x, c):
            def d(y):
                return Tail(m1(y, c))
                yield
            return Tail(m2(x, d))
            yield
    return matcher(ctx, body)


def repeat_matcher(m, minimum, maximum, greedy, x, c, paren_index, paren_count):
    """RepeatMatcher (sec-runtime-semantics-repeatmatcher-abstract-operation)."""
    if maximum == 0:
        return Tail(c(x))

    def d(y):
        if minimum == 0 and y.end == x.end:
            return FAILURE
        min2 = 0 if minimum == 0 else minimum - 1
        max2 = INFINITY if maximum == INFINITY else maximum - 1
        return Tail(repeat_matcher(m, min2, max2, greedy, y, c, paren_index, paren_count))
        yield

    cap = x.captures
    if paren_count:
        cap = list(cap)
        for k in range(paren_index + 1, paren_index + paren_count + 1):
            cap[k] = None
        cap = tuple(cap)
    xr = MatchState(x.input, x.end, cap)
    if minimum != 0:
        return Tail(m(xr, d))
    if not greedy:
        z = yield c(x)
        if z is not FAILURE:
            return z
        return Tail(m(xr, d))
    z = yield m(xr, d)
    if z is not FAILURE:
        return z
    return Tail(c(x))


def character_set_matcher(ctx: Context, rer: RegExpRecord, a: CharSet, invert: bool, direction: str):
    """CharacterSetMatcher (sec-runtime-semantics-charactersetmatcher-abstract-operation)."""
    if rer.unicode_sets:
        assert not invert and not a.strings
    found_cache: dict[int, bool] = {}
    forward = direction == FORWARD

    def found_for(ch: int) -> bool:
        cached = found_cache.get(ch)
        if cached is None:
            cc = canonicalize(rer, ch)
            if not rer.ignore_case:
                cached = a.contains(ch)
            else:
                cached = any(a.contains(member) and canonicalize(rer, member) == cc
                             for member in canonical_preimage(rer, cc))
            found_cache[ch] = cached
        return cached

    def body(x, c):
        inp = x.input
        e = x.end
        f = e + 1 if forward else e - 1
        if f < 0 or f > len(inp):
            return FAILURE
        found = found_for(inp[e if forward else f])
        if found == invert:
            return FAILURE
        return Tail(c(MatchState(inp, f, x.captures)))
        yield

    return matcher(ctx, body)


def backreference_matcher(ctx: Context, rer: RegExpRecord, ns, direction: str):
    """BackreferenceMatcher (sec-backreference-matcher)."""

    def body(x, c):
        inp = x.input
        cap = x.captures
        r = None
        for n in ns:
            if cap[n] is not None:
                assert r is None
                r = cap[n]
        if r is None:
            return Tail(c(x))
        e = x.end
        rs, re_ = r
        length = re_ - rs
        f = e + length if direction == FORWARD else e - length
        if f < 0 or f > len(inp):
            return FAILURE
        g = min(e, f)
        for i in range(length):
            if canonicalize(rer, inp[rs + i]) != canonicalize(rer, inp[g + i]):
                return FAILURE
        return Tail(c(MatchState(inp, f, cap)))
        yield

    return matcher(ctx, body)


def is_word_char(rer: RegExpRecord, inp, e: int) -> bool:
    """IsWordChar (sec-runtime-semantics-iswordchar-abstract-operation)."""
    if e == -1 or e == len(inp):
        return False
    return word_characters(rer).contains(inp[e])


def compile_assertion(ctx: Context, rer: RegExpRecord, node):
    """CompileAssertion (sec-compileassertion; B.1.2's sec-compileassertion-annexb for a quantified look-ahead)."""
    if isinstance(node, Assertion):
        kind = node.kind
        if kind == "^":
            def body(x, c):
                e = x.end
                if e == 0 or (rer.multiline and x.input[e - 1] in LINE_TERMINATORS):
                    return Tail(c(x))
                return FAILURE
                yield
        elif kind == "$":
            def body(x, c):
                e = x.end
                if e == len(x.input) or (rer.multiline and x.input[e] in LINE_TERMINATORS):
                    return Tail(c(x))
                return FAILURE
                yield
        else:
            boundary = kind == "b"

            def body(x, c):
                a = is_word_char(rer, x.input, x.end - 1)
                b = is_word_char(rer, x.input, x.end)
                if (a != b) == boundary:
                    return Tail(c(x))
                return FAILURE
                yield
        return matcher(ctx, body)
    m = compile_subpattern(ctx, rer, node.disjunction, FORWARD if node.ahead else BACKWARD)
    if not node.negative:
        def body(x, c):
            r = yield m(x, _continuation_identity)
            if r is FAILURE:
                return FAILURE
            return Tail(c(MatchState(x.input, x.end, r.captures)))
    else:
        def body(x, c):
            r = yield m(x, _continuation_identity)
            if r is not FAILURE:
                return FAILURE
            return Tail(c(x))
    return matcher(ctx, body)


def update_modifiers(rer: RegExpRecord, add: str, remove: str) -> RegExpRecord:
    """UpdateModifiers (sec-updatemodifiers)."""
    ignore_case, multiline, dot_all = rer.ignore_case, rer.multiline, rer.dot_all
    if "i" in remove:
        ignore_case = False
    elif "i" in add:
        ignore_case = True
    if "m" in remove:
        multiline = False
    elif "m" in add:
        multiline = True
    if "s" in remove:
        dot_all = False
    elif "s" in add:
        dot_all = True
    return RegExpRecord(ignore_case, multiline, dot_all, rer.unicode, rer.unicode_sets, rer.capturing_groups_count)


def _charset_atom(ctx: Context, rer: RegExpRecord, cs: CharSet, direction: str):
    """CompileAtom's steps for a CharSet under v that may hold strings (Atom :: CharacterClass and AtomEscape ::
    CharacterClassEscape, sec-compileatom)."""
    if not rer.unicode_sets or not cs.strings:
        return character_set_matcher(ctx, rer, cs, False, direction)
    lm = []
    for s in cs.sorted_strings():
        if len(s) <= 1:
            continue
        m2 = character_set_matcher(ctx, rer, CharSet.of(s[-1]), False, direction)
        for c1 in reversed(s[:-1]):
            m1 = character_set_matcher(ctx, rer, CharSet.of(c1), False, direction)
            m2 = match_sequence(ctx, m1, m2, direction)
        lm.append(m2)
    singles = CharSet(cs.ranges)
    lm.append(character_set_matcher(ctx, rer, singles, False, direction))
    if () in cs.strings:
        lm.append(empty_matcher(ctx))
    m2 = lm[-1]
    for m1 in reversed(lm[:-1]):
        m2 = match_two_alternatives(ctx, m1, m2)
    return m2


def compile_atom(ctx: Context, rer: RegExpRecord, node, direction: str):
    """CompileAtom (sec-compileatom; B.1.2's sec-compileatom-annexb)."""
    if isinstance(node, Character):
        return character_set_matcher(ctx, rer, CharSet.of(node.value), False, direction)
    if isinstance(node, Dot):
        return character_set_matcher(ctx, rer, dot_charset(rer), False, direction)
    if isinstance(node, ClassRanges):
        return character_set_matcher(ctx, rer, class_ranges_charset(rer, node), node.invert, direction)
    if isinstance(node, SetClass):
        cs = set_charset(rer, node)  # CompileCharacterClass: [^...] is CharacterComplement under v
        return _charset_atom(ctx, rer, cs, direction)
    if isinstance(node, ClassEscape):
        return _charset_atom(ctx, rer, class_escape_charset(rer, node), direction)
    if isinstance(node, Group):
        m = compile_subpattern(ctx, rer, node.disjunction, direction)
        paren_index = node.number - 1

        def body(x, c):
            def d(y):
                cap = list(y.captures)
                if direction == FORWARD:
                    assert x.end <= y.end
                    r = (x.end, y.end)
                else:
                    assert y.end <= x.end
                    r = (y.end, x.end)
                cap[paren_index + 1] = r
                return Tail(c(MatchState(x.input, y.end, tuple(cap))))
                yield
            return Tail(m(x, d))
            yield

        return matcher(ctx, body)
    if isinstance(node, Modifiers):
        return compile_subpattern(ctx, update_modifiers(rer, node.add, node.remove), node.disjunction, direction)
    if isinstance(node, Backreference):
        return backreference_matcher(ctx, rer, node.numbers, direction)
    if isinstance(node, LookAround):
        return compile_assertion(ctx, rer, node)
    raise TypeError(node)


def compile_subpattern(ctx: Context, rer: RegExpRecord, node, direction: str):
    """CompileSubpattern (sec-compilesubpattern; B.1.2's sec-compilesubpattern-annexb)."""
    if isinstance(node, Disjunction):
        # Disjunction :: Alternative `|` Disjunction, right-recursive.
        m = compile_subpattern(ctx, rer, node.alternatives[-1], direction)
        for alternative in reversed(node.alternatives[:-1]):
            m = match_two_alternatives(ctx, compile_subpattern(ctx, rer, alternative, direction), m)
        return m
    if isinstance(node, Alternative):
        # Alternative :: [empty] and Alternative :: Alternative Term, left-recursive.
        m = empty_matcher(ctx)
        for term in node.terms:
            m = match_sequence(ctx, m, compile_subpattern(ctx, rer, term, direction), direction)
        return m
    if isinstance(node, Assertion) or isinstance(node, LookAround):
        return compile_assertion(ctx, rer, node)
    if isinstance(node, Quantified):
        m = compile_atom(ctx, rer, node.atom, direction)
        minimum, maximum, greedy = node.min, node.max, node.greedy
        paren_index, paren_count = node.paren_index, node.paren_count
        assert minimum <= maximum

        def body(x, c):
            return Tail(repeat_matcher(m, minimum, maximum, greedy, x, c, paren_index, paren_count))
            yield

        return matcher(ctx, body)
    return compile_atom(ctx, rer, node, direction)


def compile_pattern(ctx: Context, rer: RegExpRecord, parsed: ParsedPattern):
    """CompilePattern (sec-compilepattern): an Abstract Closure of (Input, index)."""
    m = compile_subpattern(ctx, rer, parsed.tree, FORWARD)

    def closure(input_, index):
        assert 0 <= index <= len(input_)
        cap = (None,) * (rer.capturing_groups_count + 1)
        return run(m(MatchState(input_, index, cap), _continuation_identity))

    return closure


# ---- RegExp objects (22.2.3 to 22.2.7) ------------------------------------------------------------------------------

FLAG_CODE_UNITS = frozenset(map(ord, "dgimsuvy"))


class RegExp:
    """A RegExp object after RegExpInitialize (sec-regexpinitialize): its [[OriginalSource]], [[OriginalFlags]],
    [[RegExpRecord]] and [[RegExpMatcher]], and its lastIndex property."""

    def __init__(self, source, flags, rer: RegExpRecord, parsed: ParsedPattern, closure, ctx: Context) -> None:
        self.source = list(source)
        self.flags = list(flags)
        self.rer = rer
        self.parsed = parsed
        self.matcher = closure
        self.ctx = ctx
        self.last_index = 0

    def has_flag(self, letter: str) -> bool:
        return ord(letter) in self.flags


def check_flags(flags) -> None:
    """RegExpInitialize's flag check: a code unit other than d g i m s u v y, or one more than once."""
    if any(f not in FLAG_CODE_UNITS for f in flags):
        raise PatternSyntaxError("flags:unknown", -1)
    if len(set(flags)) != len(flags):
        raise PatternSyntaxError("flags:repeated", -1)


def regexp_parse(pattern, flags=(), annex_b: bool = True) -> ParsedPattern:
    """RegExpInitialize's steps up to ParsePattern: the flag check and the parse, with every early error. What
    RegExpInitialize does after them (CompilePattern) throws nothing, so this decides whether it throws."""
    flags = list(flags)
    check_flags(flags)
    return parse_pattern(list(pattern), ord("u") in flags, ord("v") in flags, annex_b=annex_b)


def regexp_create(pattern, flags=(), annex_b: bool = True) -> RegExp:
    """RegExpCreate / RegExpInitialize (sec-regexpcreate, sec-regexpinitialize) of a pattern and flags given as UTF-16
    code units. Raises PatternSyntaxError for the SyntaxError the constructor throws."""
    pattern = list(pattern)
    flags = list(flags)
    check_flags(flags)
    i, m, s, u, v = (ord(ch) in flags for ch in "imsuv")
    parsed = parse_pattern(pattern, u, v, annex_b=annex_b)
    rer = RegExpRecord(i, m, s, u, v, parsed.groups)
    ctx = Context()
    closure = compile_pattern(ctx, rer, parsed)
    return RegExp(pattern, flags, rer, parsed, closure, ctx)


def is_valid_regular_expression_literal(body, flags, annex_b: bool = True) -> bool:
    """IsValidRegularExpressionLiteral (sec-isvalidregularexpressionliteral), for BodyText and FlagText given as code
    units of ASCII source text, which they are in every vector this reference writes."""
    try:
        check_flags(list(flags))
    except PatternSyntaxError:
        return False
    u, v = ord("u") in flags, ord("v") in flags
    try:
        parse_pattern(list(body), u, v, annex_b=annex_b)
    except PatternSyntaxError:
        return False
    return True


@dataclass
class ExecResult:
    """What RegExpBuiltinExec returns, as the canonical serialisation reads it: None for null."""
    index: int
    captures: list            # element 0 the match, then 1 to n: (start, end) code unit ranges or None
    groups: list | None       # [(name units, (start, end) or None)] in the groups object's property order
    has_indices: bool
    indices_groups: list | None


def regexp_builtin_exec(r: RegExp, s, meter: Meter | None = None):
    """RegExpBuiltinExec (sec-regexpbuiltinexec). `s` is the subject's code units; r.last_index is the lastIndex
    property, read with ToLength and written as the text writes it. Returns an ExecResult or None."""
    meter = meter or Meter()
    r.ctx.meter = meter
    s = list(s)
    length = len(s)
    last_index = max(0, int(r.last_index))
    global_, sticky, has_indices = r.has_flag("g"), r.has_flag("y"), r.has_flag("d")
    if not global_ and not sticky:
        last_index = 0
    full_unicode = r.has_flag("u") or r.has_flag("v")
    if full_unicode:
        inp = string_to_code_points(s)
        unit_to_input = []
        for cp_index, cp in enumerate(inp):
            unit_to_input.extend([cp_index] * len(code_point_units(cp)))
        unit_to_input.append(len(inp))
    else:
        inp = s
        unit_to_input = None
    while True:
        if last_index > length:
            if global_ or sticky:
                r.last_index = 0
            return None
        input_index = unit_to_input[last_index] if full_unicode else last_index
        result = r.matcher(inp, input_index)
        if result is FAILURE:
            if sticky:
                r.last_index = 0
                return None
            last_index = advance_string_index(s, last_index, full_unicode)
            continue
        break
    e = result.end
    if full_unicode:
        e = get_string_index(s, e)
    if global_ or sticky:
        r.last_index = e
    n = len(result.captures) - 1
    assert n == r.rer.capturing_groups_count
    if not (last_index <= e <= length):
        raise SpecAssertion("GetMatchString: the Match Record's [[StartIndex]] passes its [[EndIndex]]")
    captures = [(last_index, e)]
    has_groups = r.parsed.has_group_name
    groups: list | None = [] if has_groups else None
    group_names: list = []
    matched_group_names: list = []
    for i in range(1, n + 1):
        capture = result.captures[i]
        if capture is None:
            value = None
        else:
            start, end = capture
            if full_unicode:
                start, end = get_string_index(s, start), get_string_index(s, end)
            value = (start, end)
        captures.append(value)
        name = r.parsed.group_names.get(i)
        if name is not None:
            if name in matched_group_names:
                assert value is None
                group_names.append(None)
            else:
                if value is not None:
                    matched_group_names.append(name)
                _set_property(groups, name, value)
                group_names.append(name)
        else:
            group_names.append(None)
    indices_groups = None
    if has_indices and has_groups:
        indices_groups = []
        for i in range(1, n + 1):
            name = group_names[i - 1]
            if name is not None:
                _set_property(indices_groups, name, captures[i])
    return ExecResult(last_index, captures, groups, has_indices, indices_groups)


def _set_property(obj: list, name, value) -> None:
    """CreateDataPropertyOrThrow on an ordinary object held as an ordered list of (key, value): a new key is added
    last, an existing one keeps its place."""
    for k, (key, _) in enumerate(obj):
        if key == name:
            obj[k] = (name, value)
            return
    obj.append((name, value))


def regexp_test(r: RegExp, s, meter: Meter | None = None) -> bool:
    """RegExp.prototype.test (sec-regexp.prototype.test) for an ordinary RegExp: RegExpExec is RegExpBuiltinExec."""
    return regexp_builtin_exec(r, s, meter) is not None


def regexp_match(r: RegExp, s):
    """RegExp.prototype [ %Symbol.match% ] (sec-regexp.prototype-%symbol.match%) for an ordinary RegExp: the result
    of exec without g, else the list of matched substrings (as code unit lists) or None."""
    s = list(s)
    if not r.has_flag("g"):
        return regexp_builtin_exec(r, s)
    full_unicode = r.has_flag("u") or r.has_flag("v")
    r.last_index = 0
    out = []
    while True:
        result = regexp_builtin_exec(r, s)
        if result is None:
            return out if out else None
        start, end = result.captures[0]
        out.append(s[start:end])
        if start == end:
            r.last_index = advance_string_index(s, max(0, int(r.last_index)), full_unicode)


def regexp_split(r: RegExp, s, limit=None):
    """RegExp.prototype [ %Symbol.split% ] (sec-regexp.prototype-%symbol.split%) for an ordinary RegExp, whose
    species constructor is %RegExp%: the splitter is the pattern with y added. Returns a list whose elements are code
    unit lists or None (undefined)."""
    s = list(s)
    flags = regexp_flags(r)
    unicode_matching = ord("u") in flags or ord("v") in flags
    new_flags = flags if ord("y") in flags else flags + [ord("y")]
    splitter = regexp_create(r.source, new_flags)
    out: list = []
    lim = 2 ** 32 - 1 if limit is None else limit
    if lim == 0:
        return out
    size = len(s)
    if size == 0:
        z = regexp_builtin_exec(splitter, s)
        if z is not None:
            return out
        return [s]
    p = q = 0
    while q < size:
        splitter.last_index = q
        z = regexp_builtin_exec(splitter, s)
        if z is None:
            q = advance_string_index(s, q, unicode_matching)
            continue
        e = min(max(0, int(splitter.last_index)), size)
        if e == p:
            q = advance_string_index(s, q, unicode_matching)
            continue
        out.append(s[p:q])
        if len(out) == lim:
            return out
        p = e
        for capture in z.captures[1:]:
            out.append(None if capture is None else s[capture[0]:capture[1]])
            if len(out) == lim:
                return out
        q = p
    out.append(s[p:size])
    return out


def regexp_search(r: RegExp, s) -> int:
    """RegExp.prototype [ %Symbol.search% ] (sec-regexp.prototype-%symbol.search%) for an ordinary RegExp."""
    previous = r.last_index
    r.last_index = 0
    result = regexp_builtin_exec(r, s)
    r.last_index = previous
    return -1 if result is None else result.index


def regexp_flags(r: RegExp) -> list[int]:
    """get RegExp.prototype.flags (sec-get-regexp.prototype.flags) for an ordinary RegExp, whose accessors read
    [[OriginalFlags]] (RegExpHasFlag)."""
    return [ord(ch) for ch in "dgimsuvy" if r.has_flag(ch)]


def encode_for_regexp_escape(cp: int) -> list[int]:
    """EncodeForRegExpEscape (sec-encodeforregexpescape)."""
    if cp in SYNTAX_CHARACTERS or cp == 0x2F:
        return [0x5C] + code_point_units(cp)
    for letter, value in CONTROL_ESCAPES.items():
        if value == cp:
            return [0x5C, letter]
    other = set(map(ord, ",-=<>#&!%:;@~'`\""))
    if cp in other or lexical("WhiteSpace").contains(cp) or lexical("LineTerminator").contains(cp) or \
            0xD800 <= cp <= 0xDFFF:
        if cp <= 0xFF:
            return [0x5C, ord("x")] + [ord(ch) for ch in format(cp, "02x")]
        out: list[int] = []
        for cu in code_point_units(cp):
            out.extend([0x5C, ord("u")] + [ord(ch) for ch in format(cu, "04x")])
        return out
    return code_point_units(cp)


def regexp_escape(s) -> list[int]:
    """RegExp.escape (sec-regexp.escape) of a String's code units."""
    escaped: list[int] = []
    for cp in string_to_code_points(list(s)):
        if not escaped and (cp in DECIMAL_DIGITS or cp in ASCII_LETTERS):
            escaped = [0x5C, ord("x")] + [ord(ch) for ch in format(cp, "x")]
        else:
            escaped.extend(encode_for_regexp_escape(cp))
    return escaped


# ---- One exec with its count ----------------------------------------------------------------------------------------


@dataclass
class Counted:
    """An exec's answer with its step count: `result` an ExecResult or None, or exhausted (result unset)."""
    steps: int
    exhausted: bool
    result: ExecResult | None
    last_index: int


def counted_exec(pattern, flags, subject, last_index: int = 0, cutoff=None) -> Counted:
    """RegExpBuiltinExec of one pattern on one subject from one lastIndex, counted; under a cut-off, the answer is
    exhausted when the cut-off is reached."""
    r = regexp_create(pattern, flags)
    r.last_index = last_index
    meter = Meter(cutoff)
    try:
        result = regexp_builtin_exec(r, subject, meter)
    except Exhausted:
        return Counted(meter.count, True, None, r.last_index)
    return Counted(meter.count, False, result, r.last_index)


# ---- Broiler.Regex's adversarial corpus: the counts its section 3 asks for -----------------------------------------


def _corpus_expression(text: str) -> list[int]:
    """One corpus expression: JSON strings, each optionally `* n`, joined by ` + ` (tests/Adversarial/README.md,
    section 1)."""
    out: list[int] = []
    for part in _split_plus(text):
        part = part.strip()
        count = 1
        literal_end = _json_string_end(part)
        literal, rest = part[:literal_end], part[literal_end:].strip()
        if rest:
            if not rest.startswith("*"):
                raise ValueError(f"not a corpus expression: {text!r}")
            count = int(rest[1:].strip())
        out.extend(utf16(json.loads(literal)) * count)
    return out


def _json_string_end(part: str) -> int:
    assert part[0] == '"'
    i = 1
    while part[i] != '"':
        i += 2 if part[i] == "\\" else 1
    return i + 1


def _split_plus(text: str) -> list[str]:
    parts, current, in_string, i = [], [], False, 0
    while i < len(text):
        ch = text[i]
        if in_string:
            current.append(ch)
            if ch == "\\":
                current.append(text[i + 1])
                i += 1
            elif ch == '"':
                in_string = False
        elif ch == '"':
            in_string = True
            current.append(ch)
        elif text.startswith(" + ", i):
            parts.append("".join(current))
            current = []
            i += 3
            continue
        else:
            current.append(ch)
        i += 1
    parts.append("".join(current))
    return parts


def corpus_counts(corpus: Path, rows=None, factor: int = 100):
    """For each match@k or anchored@k row of the corpus, regexpref's count: a row declared limit:steps at a ceiling
    S is run with the cut-off factor x S and must be exhausted there; a row declared to match within a step ceiling S
    is run with the cut-off S / factor and must complete within it. Yields (id, declared, verdict, steps)."""
    lines = corpus.read_text(encoding="utf-8").split("\n")
    header = None
    for line in lines:
        if not line or line.startswith("#"):
            continue
        fields = line.split("\t")
        if header is None:
            header = fields
            continue
        row = dict(zip(header, fields))
        if rows and row["id"] not in rows:
            continue
        op = row["op"]
        if not (op.startswith("match@") or op.startswith("anchored@")):
            yield row["id"], row["status"], "not a match (no Matcher runs)", None
            continue
        policy = dict(item.split("=") for item in row["policy"].split())
        steps = policy["steps"]
        flags = "" if row["flags"] == "-" else row["flags"]
        # match@k searches from k (lastIndex k under g, which nothing else of the row reads); anchored@k is one
        # attempt at k (y).
        flags += "y" if op.startswith("anchored@") else "g"
        start = int(op.split("@")[1])
        pattern = _corpus_expression(row["pattern"])
        subject = _corpus_expression(row["subject"])
        status = row["status"]
        if steps == "none":
            yield row["id"], status, "no step ceiling (frames, trail or a meter decide the row)", None
            continue
        ceiling = int(steps)
        if status == "limit:steps":
            counted = counted_exec(pattern, utf16(flags), subject, start, cutoff=factor * ceiling)
            verdict = "exhausted at the cut-off" if counted.exhausted else "completed: margin NOT met"
        else:
            counted = counted_exec(pattern, utf16(flags), subject, start, cutoff=ceiling // factor)
            verdict = "completed within the cut-off" if not counted.exhausted else "exhausted: margin NOT met"
        yield row["id"], status, verdict, counted.steps


# ---- Command line ---------------------------------------------------------------------------------------------------


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    sub = parser.add_subparsers(dest="command", required=True)
    one = sub.add_parser("exec", help="RegExpBuiltinExec of a pattern on a subject, with its step count")
    one.add_argument("pattern")
    one.add_argument("flags")
    one.add_argument("subject")
    one.add_argument("--last-index", type=int, default=0)
    one.add_argument("--cutoff", type=int, default=None)
    corpus = sub.add_parser("corpus", help="the step counts of Broiler.Regex's adversarial corpus")
    corpus.add_argument("corpus", type=Path)
    corpus.add_argument("--rows", default="")
    corpus.add_argument("--factor", type=int, default=100)
    args = parser.parse_args(argv)
    if args.command == "exec":
        try:
            counted = counted_exec(utf16(args.pattern), utf16(args.flags), utf16(args.subject), args.last_index,
                                   args.cutoff)
        except PatternSyntaxError as error:
            print(f"SyntaxError {error.code}")
            return 0
        if counted.exhausted:
            print(f"exhausted at {counted.steps}")
        elif counted.result is None:
            print(f"null steps={counted.steps} lastIndex={counted.last_index}")
        else:
            res = counted.result
            print(f"index={res.index} captures={res.captures} groups={res.groups} steps={counted.steps} "
                  f"lastIndex={counted.last_index}")
        return 0
    rows = set(args.rows.split(",")) if args.rows else None
    for row_id, declared, verdict, steps in corpus_counts(args.corpus, rows, args.factor):
        print(f"{row_id}\t{declared}\t{verdict}\t{'-' if steps is None else steps}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
