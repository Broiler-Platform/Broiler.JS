"""Tests of regexpref.py (`ref:regexpref@1`, corpus revision 31, milestone SJL-11.3).

Every expected value here is derived by hand from the pinned edition's text (ECMA-262 ES2026 at 0248456c, 22.2 and
B.1.2): first the examples its notes give, with the answers the notes state, then cases worked through the
algorithms step by step, each naming its clause. None is taken from an engine. The tests that read the pinned Unicode
data look for it as unicoderef.py does (BROILER_UNICODE_PINS, or a Broiler.Unicode checkout beside this repository's)
and are skipped where it is absent. Run with `python3 -I`, as any reading of downloaded data.
"""

from __future__ import annotations

import os
import subprocess
import sys
import unittest
from pathlib import Path

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import regexpref as R  # noqa: E402
import unicoderef as U  # noqa: E402

NEEDS_DATA = unittest.skipUnless(U.pins_directory().is_dir(), f"no pinned Unicode data at {U.pins_directory()}")
UNDEFINED = None


def u(text: str) -> list[int]:
    out: list[int] = []
    for ch in text:
        out.extend(R.code_point_units(ord(ch)))
    return out


def exec_(pattern: str, flags: str, subject: str, last_index: int = 0, annex_b: bool = True):
    """(index, [elements as str or None], lastIndex after) or (None, None, lastIndex) for null."""
    r = R.regexp_create(u(pattern), u(flags), annex_b=annex_b)
    r.last_index = last_index
    s = u(subject)
    result = R.regexp_builtin_exec(r, s)
    if result is None:
        return None, None, r.last_index
    elements = [None if c is None else "".join(chr(x) for x in s[c[0]:c[1]]) for c in result.captures]
    return result.index, elements, r.last_index


def accepted(pattern: str, flags: str = "", annex_b: bool = True) -> bool:
    try:
        R.regexp_create(u(pattern), u(flags), annex_b=annex_b)
        return True
    except R.PatternSyntaxError:
        return False


def refusal(pattern: str, flags: str = "", annex_b: bool = True) -> str | None:
    try:
        R.regexp_create(u(pattern), u(flags), annex_b=annex_b)
        return None
    except R.PatternSyntaxError as error:
        return error.code


def steps(pattern: str, flags: str, subject: str, cutoff=None, last_index: int = 0) -> R.Counted:
    return R.counted_exec(u(pattern), u(flags), u(subject), last_index, cutoff)


@NEEDS_DATA
class TheNotesOf22_2_2(unittest.TestCase):
    """The examples of 22.2.2's notes, with the answers the notes give."""

    def test_disjunction_tries_the_left_alternative_first(self) -> None:
        # 22.2.2.3, Disjunction's note: /a|ab/.exec("abc") returns "a" and not "ab".
        self.assertEqual((0, ["a"], 0), exec_("a|ab", "", "abc"))
        # ... and /((a)|(ab))((c)|(bc))/.exec("abc") returns ["abc", "a", "a", undefined, "bc", undefined, "bc"].
        self.assertEqual(["abc", "a", "a", None, "bc", None, "bc"], exec_("((a)|(ab))((c)|(bc))", "", "abc")[1])

    def test_greedy_and_lazy_quantifiers(self) -> None:
        # RepeatMatcher's notes: /a[a-z]{2,4}/ gives "abcde", /a[a-z]{2,4}?/ gives "abc".
        self.assertEqual(["abcde"], exec_("a[a-z]{2,4}", "", "abcdefghi")[1])
        self.assertEqual(["abc"], exec_("a[a-z]{2,4}?", "", "abcdefghi")[1])
        # /(aa|aabaac|ba|b|c)*/.exec("aabaac") returns ["aaba", "ba"].
        self.assertEqual(["aaba", "ba"], exec_("(aa|aabaac|ba|b|c)*", "", "aabaac")[1])

    def test_the_gcd_example(self) -> None:
        # "aaaaaaaaaa,aaaaaaaaaaaaaaa".replace(/^(a+)\1*,\1+$/, "$1") is "aaaaa": the capture is the gcd.
        self.assertEqual("aaaaa", exec_("^(a+)\\1*,\\1+$", "", "aaaaaaaaaa,aaaaaaaaaaaaaaa")[1][1])

    def test_each_iteration_clears_the_atom_s_captures(self) -> None:
        # /(z)((a+)?(b+)?(c))*/.exec("zaacbbbcac") returns ["zaacbbbcac", "z", "ac", "a", undefined, "c"].
        self.assertEqual(["zaacbbbcac", "z", "ac", "a", None, "c"], exec_("(z)((a+)?(b+)?(c))*", "", "zaacbbbcac")[1])

    def test_an_empty_iteration_past_the_minimum_ends_the_loop(self) -> None:
        # /(a*)b\1+/.exec("baaaac") returns ["b", ""]; /(a*)*/.exec("b") terminates.
        self.assertEqual((0, ["b", ""], 0), exec_("(a*)b\\1+", "", "baaaac"))
        self.assertEqual((0, ["", None], 0), exec_("(a*)*", "", "b"))

    def test_lookahead(self) -> None:
        # (?= Disjunction )'s note: /(?=(a+))/.exec("baaabac") matches the empty String after the first b, ["", "aaa"];
        # /(?=(a+))a*b\1/.exec("baaabac") returns ["aba", "a"], there being no backtracking into the lookahead.
        self.assertEqual((1, ["", "aaa"], 0), exec_("(?=(a+))", "", "baaabac"))
        self.assertEqual((3, ["aba", "a"], 0), exec_("(?=(a+))a*b\\1", "", "baaabac"))

    def test_negative_lookahead(self) -> None:
        # (?! Disjunction )'s note: ["baaabaac", "ba", undefined, "abaac"].
        self.assertEqual(["baaabaac", "ba", None, "abaac"], exec_("(.*?)a(?!(a+)b\\2c)\\2(.*)", "", "baaabaac")[1])

    def test_canonicalize_s_note_on_ohm(self) -> None:
        # Canonicalize's note: "\u2126" is matched by /[ω]/ui and /[\u03A9]/ui but not by /[ω]/i or /[\u03A9]/i.
        for pattern, flags, expected in (("[\u03c9]", "ui", True), ("[\\u03A9]", "ui", True), ("[\u03c9]", "i", False),
                                         ("[\\u03A9]", "i", False)):
            self.assertEqual(expected, exec_(pattern, flags, "\u2126")[0] is not None, (pattern, flags))

    def test_the_ends_of_a_range_keep_their_case(self) -> None:
        # CompileToCharSet's note: /[E-F]/i matches only E, F, e and f; /[E-f]/i matches every ASCII letter and
        # [ \ ] ^ _ `.
        matched = "".join(chr(c) for c in range(0x20, 0x7F) if exec_("^[E-F]$", "i", chr(c))[0] is not None)
        self.assertEqual("EFef", matched)
        matched = "".join(chr(c) for c in range(0x20, 0x7F) if exec_("^[E-f]$", "i", chr(c))[0] is not None)
        self.assertEqual("ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz", matched)

    def test_maybe_simple_case_folding_of_a_range(self) -> None:
        # ClassSetRange's note: under v and i, MaybeSimpleCaseFolding(rer, [Ā-č]) holds only the odd code points.
        rer = R.RegExpRecord(True, False, False, False, True, 0)
        folded = R.maybe_simple_case_folding(rer, R.CharSet([(0x100, 0x10D)]))
        self.assertEqual([c for c in range(0x100, 0x10E) if c % 2], [c for c in range(0x100, 0x10E) if folded.contains(c)])

    def test_a_bmp_pattern_is_its_code_units(self) -> None:
        # 22.2.2's note on U+1D11E: as a BMP pattern it is the two code units 0xD834 and 0xDD1E.
        self.assertEqual((0, ["\ud834\udd1e"], 0), exec_("\ud834\udd1e", "", "\ud834\udd1e"))
        self.assertEqual((0, ["\ud834"], 0), exec_("^[\ud834\udd1e]", "", "\ud834\udd1e"))
        self.assertEqual((0, ["\ud834\udd1e"], 0), exec_("^[\ud834\udd1e]", "u", "\ud834\udd1e"))

    def test_split_s_note(self) -> None:
        # %Symbol.split%'s note: /<(\/)?([^<>]+)>/ on "A<B>bold</B>and<CODE>coded</CODE>".
        r = R.regexp_create(u("<(\\/)?([^<>]+)>"), u(""))
        parts = R.regexp_split(r, u("A<B>bold</B>and<CODE>coded</CODE>"))
        self.assertEqual(["A", None, "B", "bold", "/", "B", "and", None, "CODE", "coded", "/", "CODE", ""],
                         [None if p is None else "".join(map(chr, p)) for p in parts])
        # An empty match splits into code units; the empty String splits into nothing when the pattern matches it.
        self.assertEqual([[0x61], [0x62]], R.regexp_split(R.regexp_create(u(""), u("")), u("ab")))
        self.assertEqual([], R.regexp_split(R.regexp_create(u("(?:)"), u("")), u("")))
        self.assertEqual([[]], R.regexp_split(R.regexp_create(u("a"), u("")), u("")))

    def test_the_property_name_notes(self) -> None:
        # UnicodeMatchProperty's note: Script_Extensions and scx are valid, script_extensions and Scx are not;
        # UnicodeMatchPropertyValue's: Xpeo and Old_Persian are valid values, xpeo and Old Persian are not.
        for name, valid in (("Script_Extensions", True), ("scx", True), ("script_extensions", False), ("Scx", False)):
            self.assertEqual(valid, accepted(f"\\p{{{name}=Latin}}", "u"), name)
        for value, valid in (("Xpeo", True), ("Old_Persian", True), ("xpeo", False), ("Old Persian", False)):
            self.assertEqual(valid, accepted(f"\\p{{Script_Extensions={value}}}", "u"), value)


@NEEDS_DATA
class TheStepCounter(unittest.TestCase):
    """One step is one Matcher invocation; each count is worked through the closures by hand."""

    def test_one_character(self) -> None:
        # Pattern :: Disjunction; Alternative :: Alternative Term with Alternative :: [empty]: MatchSequence(
        # EmptyMatcher, CharacterSetMatcher). /a/ on "a": MatchSequence, EmptyMatcher, the character: 3.
        counted = steps("a", "", "a")
        self.assertEqual((3, False), (counted.steps, counted.exhausted))

    def test_every_start_position_is_counted(self) -> None:
        # /a/ on "ba": 3 at index 0 (the character fails) and 3 at index 1.
        self.assertEqual(6, steps("a", "", "ba").steps)
        # /a/ on "bb": 3 at index 0, 3 at index 1, 3 at index 2 (the character fails at the end): 9.
        self.assertEqual(9, steps("a", "", "bb").steps)

    def test_alternatives(self) -> None:
        # /a|ab/ on "abc": MatchTwoAlternatives, then the first alternative's MatchSequence, EmptyMatcher and a: 4.
        self.assertEqual(4, steps("a|ab", "", "abc").steps)
        # /x|a/ on "a": MatchTwoAlternatives 1, x's three (fails), a's three: 7.
        self.assertEqual(7, steps("x|a", "", "a").steps)

    def test_repeat_matcher_invokes_the_atom_per_iteration(self) -> None:
        # /a*/ on "aa": MatchSequence 1, EmptyMatcher 2, the Term's matcher 3, then RepeatMatcher invokes the atom at
        # 0 (4), at 1 (5) and at 2 (6, it fails), and the continuation succeeds: 6.
        self.assertEqual(6, steps("a*", "", "aa").steps)
        # a{0} never invokes the atom: MatchSequence, EmptyMatcher, the Term's matcher: 3.
        self.assertEqual(3, steps("a{0}", "", "a").steps)

    def test_look_around_counts_its_inner_match(self) -> None:
        # /(?=a)/ on "a": MatchSequence 1, EmptyMatcher 2, the assertion 3, and inside it MatchSequence 4, EmptyMatcher
        # 5 and a 6.
        self.assertEqual(6, steps("(?=a)", "", "a").steps)

    def test_a_group_is_one_matcher(self) -> None:
        # /(a)/ on "a": MatchSequence, EmptyMatcher, the group, and inside it MatchSequence, EmptyMatcher, a: 6.
        self.assertEqual(6, steps("(a)", "", "a").steps)
        # A modifier group is no matcher of its own: /(?:a)/ and /(?i:a)/ count as a's sequence inside the outer one.
        self.assertEqual(5, steps("(?:a)", "", "a").steps)
        self.assertEqual(5, steps("(?i:a)", "", "a").steps)

    def test_the_cut_off(self) -> None:
        # /a/ on "ba" needs 6; with a cut-off of 5 the sixth invocation is not made: exhausted at 5.
        counted = steps("a", "", "ba", cutoff=5)
        self.assertEqual((5, True), (counted.steps, counted.exhausted))
        counted = steps("a", "", "ba", cutoff=6)
        self.assertEqual((6, False), (counted.steps, counted.exhausted))
        self.assertEqual(1, counted.result.index)

    def test_catastrophic_backtracking_reaches_any_cut_off(self) -> None:
        # (a+)+b on n a then c from index 0 visits every composition of every prefix: at least 2^n - 1 > 10^5 for n = 17.
        counted = steps("(a+)+b", "", "a" * 17 + "c", cutoff=100000)
        self.assertTrue(counted.exhausted)

    def test_a_long_greedy_run_needs_no_stack(self) -> None:
        # The trampoline holds the pending calls: 200,000 iterations complete, one atom invocation each.
        counted = steps("a*", "", "a" * 200000)
        self.assertFalse(counted.exhausted)
        self.assertEqual((0, (0, 200000)), (counted.result.index, counted.result.captures[0]))
        self.assertEqual(200000 + 4, counted.steps)


@NEEDS_DATA
class EarlyErrorsAndGrammar(unittest.TestCase):
    """22.2.1.1's early errors and B.1.2.1's, each with a pattern it refuses and one beside it that it admits."""

    def test_each_early_error(self) -> None:
        for pattern, flags, code in (
                ("(?<a>x)(?<a>y)", "", "early:duplicate-group-name"),
                ("(?<a>x)(?=(?<a>y))", "", "early:duplicate-group-name"),
                ("a{2,1}", "", "early:quantifier-range"),
                ("(?ii:a)", "", "early:modifier-repeated"), ("(?-ii:a)", "", "early:modifier-repeated"),
                ("(?-:a)", "", "early:modifiers-both-empty"), ("(?i-i:a)", "", "early:modifier-added-and-removed"),
                ("\\k<b>(?<a>x)", "u", "early:undefined-group-name"), ("\\2(a)", "u", "early:decimal-escape-past-group-count"),
                ("[\\d-a]", "u", "early:class-escape-in-range"), ("[b-a]", "", "early:class-range-out-of-order"),
                ("(?<\\u0030>x)", "", "early:group-name-escape"), ("(?<\\uD800>x)", "u", "early:group-name-escape"),
                ("(?<\ud83d\ude00>x)", "", "early:group-name-surrogate-pair"),
                ("\\p{Foo=Bar}", "u", "early:property-name"), ("\\p{sc=greek}", "u", "early:property-value"),
                ("\\p{letter}", "u", "early:lone-property"), ("\\p{RGI_Emoji}", "u", "early:property-of-strings-without-v"),
                ("\\P{RGI_Emoji}", "v", "early:negated-property-of-strings"),
                ("[^\\q{ab}]", "v", "early:negated-class-with-strings"),
                ("[^[\\p{RGI_Emoji}]]", "v", "early:negated-class-with-strings"),
                ("[^[\\p{RGI_Emoji}--\\p{RGI_Emoji}]]", "v", "early:negated-class-with-strings"),
                ("[z-a]", "v", "early:class-set-range-out-of-order"), ("{1}", "", "early:invalid-braced-quantifier"),
                ("a{1}{2}", "", "early:invalid-braced-quantifier")):
            self.assertEqual(code, refusal(pattern, flags), (pattern, flags))
        for pattern, flags in (("(?<a>x)|(?<a>y)", ""), ("(?:(?<a>x)|(?<a>y))*", ""), ("a{1,1}", ""), ("(?i-m:a)", ""),
                               ("\\k<a>(?<a>x)", "u"), ("\\1(a)", "u"), ("[\\d-a]", ""), ("[a-a]", ""),
                               ("(?<a\\u0030>x)", ""), ("(?<\ud835\udc00>x)", ""), ("\\p{sc=Greek}", "u"),
                               ("\\p{RGI_Emoji}", "v"), ("[^\\q{a}]", "v"), ("[^[\\p{RGI_Emoji}&&\\p{L}]]", "v"),
                               ("{", ""), ("a{1}b{2}", "")):
            self.assertIsNone(refusal(pattern, flags), (pattern, flags))

    def test_might_both_participate(self) -> None:
        # MightBothParticipate is false only when some Disjunction holds the two in different alternatives.
        self.assertTrue(accepted("(?<a>x)|(?:(?<a>y)|(?<a>z))"))
        self.assertFalse(accepted("((?<a>x)|(?<a>y))(?<a>z)"))
        self.assertFalse(accepted("(?<a>(?<a>x)|y)"))
        # z's b and w's b lie in one alternative of the outer Disjunction, and only z's in the inner one.
        self.assertFalse(accepted("(?<a>x)|((?<a>y)|(?<b>z))(?<b>w)"))
        self.assertTrue(accepted("(?<a>x)|((?<a>y)|(?<b>z))|(?<b>w)"))
        # MayContainStrings reads the grammar, not the set: a negated class of a subtraction from a property of
        # strings is refused although the subtraction is empty.

    def test_u_with_v_and_the_flags(self) -> None:
        for flags in ("uv", "gg", "x", "G", " g"):
            self.assertIsNotNone(refusal("a", flags), flags)
        for flags in ("", "dgimsuy", "dgimsvy", "ygmisd"):
            self.assertIsNone(refusal("a", flags), flags)

    def test_the_v_flag_s_reserved_syntax(self) -> None:
        for pattern in ("[(]", "[)]", "[{]", "[}]", "[/]", "[-]", "[|]", "[&&]", "[!!]", "[##]", "[$$]", "[%%]", "[**]",
                        "[++]", "[,,]", "[..]", "[::]", "[;;]", "[<<]", "[==]", "[>>]", "[??]", "[@@]", "[``]", "[~~]",
                        "[^^^]", "[_^^]", "[a&&&b]", "[a--b&&c]", "[a-\\d]", "\\q{a}", "[a|]", "[a!!b]", "[ab&&c]"):
            self.assertFalse(accepted(pattern, "v"), pattern)
        for pattern in ("[\\(]", "[a\\|]", "[a\\!\\!b]", "[a&&b]", "[a--b--c]", "[\\q{a|bc}]", "[[a-z]--[aeiou]]",
                        "[&]", "[a&b]", "[\\-]", "[]", "[^]"):
            self.assertTrue(accepted(pattern, "v"), pattern)

    def test_annex_b_against_the_main_grammar(self) -> None:
        # Each is B.1.2's alone: the main grammar refuses it without u (annex_b=False), B.1.2 admits it.
        for pattern in ("]", "{", "}", "a{", "\\8", "\\c", "[\\c_]", "(?=a)*", "\\p{L}", "[\\d-a]", "\\k", "\\01",
                        "\\_", "[\\B]", "\\u12", "\\x4"):
            self.assertTrue(accepted(pattern, ""), pattern)
            self.assertFalse(accepted(pattern, "", annex_b=False), pattern)
        # ... and with u, neither admits them.
        for pattern in ("]", "{", "\\8", "\\c", "(?=a)*", "[\\d-a]", "\\k", "\\01"):
            self.assertFalse(accepted(pattern, "u"), pattern)
        # A quantified look-behind is refused by both grammars.
        self.assertFalse(accepted("(?<=a)*", ""))
        self.assertFalse(accepted("(?<=a)*", "", annex_b=False))

    def test_annex_b_s_two_parses(self) -> None:
        # B.1.2's ParsePattern parses without [NamedCaptureGroups], and again with it when a GroupName occurs: \k is
        # the letter without a group name and a reference with one.
        self.assertEqual((0, ["k<a>"], 0), exec_("\\k<a>", "", "k<a>"))
        self.assertFalse(accepted("(?<b>x)\\k<a>"))
        self.assertFalse(accepted("[\\k<a>](?<a>x)"))
        self.assertTrue(accepted("[\\k<a>]"))

    def test_annex_b_s_decimal_escapes(self) -> None:
        # B.1.2's DecimalEscape only up to the group count, else LegacyOctalEscapeSequence or IdentityEscape.
        self.assertEqual((0, ["a\u0008", "a"], 0), exec_("(a)\\10", "", "a\u0008"))
        self.assertEqual((0, ["a\u00018", "a"], 0), exec_("(a)\\18", "", "a\u00018"))
        self.assertEqual((0, ["\u0002a", "a"], 0), exec_("\\2(a)", "", "\u0002a"))
        self.assertEqual((0, ["8"], 0), exec_("\\8", "", "8"))
        self.assertEqual((0, [" 0"], 0), exec_("\\400", "", " 0"))
        self.assertEqual((0, ["\u00ff"], 0), exec_("\\377", "", "\u00ff"))

    def test_lead_surrogate_escape_before_a_braced_escape(self) -> None:
        # Only `u HexLeadSurrogate \u HexTrailSurrogate` pairs; a braced escape never joins.
        self.assertEqual((0, ["\udc38"], 0), exec_("[\\uD83D\\u{DC38}]", "u", "\udc38"))
        self.assertEqual((None, None, 0), exec_("\\uD83D\\u{DC38}+", "u", "\ud83d\udc38\udc38"))
        self.assertEqual((0, ["\ud83d\udc38"], 0), exec_("[\\uD83D\\uDC38]", "u", "\ud83d\udc38"))

    def test_zero_before_a_non_ascii_digit(self) -> None:
        # CharacterEscape :: 0 [lookahead ∉ DecimalDigit], DecimalDigit ASCII only.
        self.assertEqual((0, ["\u0000\u0661"], 0), exec_("\\0\u0661", "u", "\u0000\u0661"))
        self.assertFalse(accepted("\\01", "u"))

    def test_nesting_depth_as_broiler_vm_counts_it(self) -> None:
        # The pattern's Disjunction is 1, each group and each v-mode class 1 more.
        self.assertEqual(512, R.regexp_create(u("(?:" * 511 + "a" + ")" * 511), u("")).parsed.depth)
        self.assertEqual(3, R.regexp_create(u("[[a]]"), u("v")).parsed.depth)
        self.assertEqual(1, R.regexp_create(u("[[a]]"), u("")).parsed.depth)
        # The specification has no bound.
        self.assertTrue(accepted("(?:" * 20000 + "a" + ")" * 20000))


@NEEDS_DATA
class TheSemantics(unittest.TestCase):
    def test_duplicate_named_groups(self) -> None:
        # RegExpBuiltinExec: groups.a is the participating group's; \k reads it (BackreferenceMatcher's ns).
        r = R.regexp_create(u("(?<a>x)|(?<a>y)"), u("d"))
        result = R.regexp_builtin_exec(r, u("y"))
        self.assertEqual([((0x61,), (0, 1))], result.groups)
        self.assertEqual([((0x61,), (0, 1))], result.indices_groups)
        self.assertEqual((0, ["yy", None, "y"], 0), exec_("(?:(?<a>x)|(?<a>y))\\k<a>", "", "yy"))
        self.assertEqual((None, None, 0), exec_("(?:(?<a>x)|(?<a>y))\\k<a>", "", "yx"))
        # The groups object's properties are in the order of each name's first group.
        r = R.regexp_create(u("(?<b>.)(?<a>.)|(?<a>.)(?<b>.)"), u(""))
        result = R.regexp_builtin_exec(r, u("pq"))
        self.assertEqual([((0x62,), (0, 1)), ((0x61,), (1, 2))], result.groups)

    def test_look_behind_reads_right_to_left(self) -> None:
        # (?<= Disjunction ) compiles its Disjunction ~backward~: the greedy (\d+) right of (\d+) takes the most.
        self.assertEqual((4, ["", "1", "053"], 0), exec_("(?<=(\\d+)(\\d+))$", "", "1053"))
        self.assertEqual((2, ["b", "a"], 0), exec_("(?<=\\1(a))b", "", "aab"))
        self.assertEqual((None, None, 0), exec_("(?<!a)b", "", "ab"))

    def test_modifiers(self) -> None:
        # UpdateModifiers: (?i:a)b matches Ab and not AB; (?-i:a)b under i matches aB and not AB.
        self.assertEqual(0, exec_("(?i:a)b", "", "Ab")[0])
        self.assertIsNone(exec_("(?i:a)b", "", "AB")[0])
        self.assertEqual(0, exec_("(?-i:a)b", "i", "aB")[0])
        self.assertIsNone(exec_("(?-i:a)b", "i", "AB")[0])
        self.assertEqual(2, exec_("(?m:^b)", "", "a\nb")[0])
        self.assertEqual(0, exec_("(?s:.)", "", "\n")[0])

    def test_canonicalize_s_two_branches(self) -> None:
        # Without u, toUppercase: U+212A KELVIN SIGN is its own upper case, so [a-z]/i does not match it; ſ's is S,
        # but ſ is at or above 128 and S below, so it canonicalizes to itself. Under u, simple case folding: both fold
        # into the range.
        self.assertIsNone(exec_("[a-z]", "i", "\u212a")[0])
        self.assertEqual(0, exec_("[a-z]", "iu", "\u212a")[0])
        self.assertIsNone(exec_("[a-z]", "i", "\u017f")[0])
        self.assertEqual(0, exec_("[a-z]", "iu", "\u017f")[0])
        # The dotted capital I and the dotless i: no culture's mapping; neither canonicalizes to i or I.
        for pattern, flags, subject in (("i", "i", "\u0130"), ("I", "i", "\u0131"), ("i", "iu", "\u0130"),
                                        ("I", "iu", "\u0131"), ("i", "iv", "\u0130")):
            self.assertIsNone(exec_(pattern, flags, subject)[0], (pattern, flags, subject))
        # U+1F80's upper case is two code points, so without u it canonicalizes to itself, as U+1F88 does.
        self.assertIsNone(exec_("\u1f80", "i", "\u1f88")[0])
        # Under iu, \W does not match ſ: WordCharacters holds ſ and K, and no member of \W canonicalizes to s.
        self.assertIsNone(exec_("\\W", "iu", "\u017f")[0])
        self.assertIsNone(exec_("\\W", "iu", "S")[0])

    def test_line_terminators(self) -> None:
        # ^ and $ under m read LineTerminator (LF, CR, LS, PS); $ without m is the input's end only.
        self.assertEqual(2, exec_("^b", "m", "a\u2028b")[0])
        self.assertIsNone(exec_("^b", "m", "a\u0085b")[0])
        self.assertIsNone(exec_("a$", "", "a\n")[0])
        self.assertEqual(0, exec_("a$", "m", "a\n")[0])

    def test_back_references_under_u_compare_characters(self) -> None:
        # The input is code points: U+1D000 is not \uD834.
        self.assertIsNone(exec_("foo(.+)bar\\1", "u", "foo\ud834bar\ud834\udc00")[0])
        self.assertEqual(0, exec_("foo(.+)bar\\1", "", "foo\ud834bar\ud834\udc00")[0])
        self.assertIsNone(exec_("(?<=\\1(.))b", "u", "\ud834\udc00\udc00b")[0])

    def test_v_mode_strings_longest_first(self) -> None:
        self.assertEqual(["abc"], exec_("[\\q{abc|ab|a}]", "v", "abc")[1])
        self.assertEqual(["abc"], exec_("[\\q{a|ab|abc}]c?", "v", "abc")[1])
        self.assertEqual((0, [""], 0), exec_("[\\q{}]", "v", "x"))
        self.assertEqual(["\u212a\u212a"], exec_("[\\q{KK}]", "vi", "\u212a\u212a")[1])

    def test_last_index_inside_a_surrogate_pair(self) -> None:
        # RegExpBuiltinExec: the matcher starts at the character obtained from element lastIndex (the pair), the index
        # is lastIndex and the match ends where the matcher does.
        pair = "\ud834\udf06"
        self.assertEqual((1, ["\udf06"], 2), exec_(".", "gu", pair, 1))
        self.assertEqual((None, None, 0), exec_("\\udf06", "gu", pair, 1))
        self.assertEqual((1, ["\ud834\udf06"], 3), exec_(".", "gu", "a" + pair + "b", 1))
        self.assertEqual((2, ["\udf06"], 3), exec_(".", "gu", "a" + pair + "b", 2))
        self.assertEqual((1, ["\udf06"], 2), exec_(".", "g", pair, 1))
        # The empty match there: the Match Record would start at 1 and end at 0, which GetMatchString's assertion
        # forbids; the text has no answer (JSD-0081's decision D41).
        with self.assertRaises(R.SpecAssertion):
            exec_("(?:)", "gu", pair, 1)

    def test_sticky_and_global(self) -> None:
        self.assertEqual((None, None, 0), exec_("a", "y", "ba", 0))
        self.assertEqual((1, ["a"], 2), exec_("a", "y", "ba", 1))
        self.assertEqual((None, None, 0), exec_("b", "y", "abc", 0))
        self.assertEqual((2, ["a"], 3), exec_("a", "g", "aba", 1))
        self.assertEqual((None, None, 0), exec_("a", "g", "aba", 4))
        self.assertEqual((0, ["a"], 2), exec_("a", "", "aba", 2))
        r = R.regexp_create(u("(?:)"), u("gu"))
        self.assertEqual([[], [], []], R.regexp_match(r, u("a\ud834\udf06")))
        r = R.regexp_create(u("b"), u("y"))
        self.assertEqual(-1, R.regexp_search(r, u("aab")))

    def test_d_indices_under_u_are_code_unit_offsets(self) -> None:
        r = R.regexp_create(u("(.)(.)"), u("du"))
        result = R.regexp_builtin_exec(r, u("\ud83d\ude00x"))
        self.assertEqual([(0, 3), (0, 2), (2, 3)], result.captures)

    def test_property_escapes(self) -> None:
        self.assertEqual(0, exec_("\\p{Lu}", "u", "\ua7ce")[0])
        self.assertIsNone(exec_("\\p{Ll}", "u", "\ua7ce")[0])
        self.assertEqual(0, exec_("\\p{scx=Hira}", "u", "\u30fc")[0])
        self.assertIsNone(exec_("\\p{sc=Hira}", "u", "\u30fc")[0])
        self.assertEqual(["\ud83c\uddfa\ud83c\uddf3"], exec_("^\\p{RGI_Emoji_Flag_Sequence}$", "v",
                                                             "\ud83c\uddfa\ud83c\uddf3")[1])
        self.assertEqual(0, exec_("\\p{Lowercase_Letter}", "iv", "A")[0])
        self.assertIsNone(exec_("\\P{Ll}", "iv", "a")[0])


@NEEDS_DATA
class TheHelpers(unittest.TestCase):
    def test_regexp_escape(self) -> None:
        # RegExp.escape: a leading ASCII letter or digit as \xHH; EncodeForRegExpEscape for the rest.
        def esc(text: str) -> str:
            return "".join(chr(c) for c in R.regexp_escape(u(text)))
        self.assertEqual("\\x61bc", esc("abc"))
        self.assertEqual("\\x31a", esc("1a"))
        self.assertEqual("_", esc("_"))
        self.assertEqual("\\.\\*\\/", esc(".*/"))
        self.assertEqual("\\x2c\\x2d\\x20", esc(",- "))
        self.assertEqual("\\u2028\\ufeff\\xa0", esc("\u2028\ufeff\u00a0"))
        self.assertEqual("\\t\\n\\v\\f\\r", esc("\t\n\u000b\f\r"))
        self.assertEqual("\\ud800\u0085", esc("\ud800\u0085"))
        self.assertEqual("\ud83d\ude00", esc("\ud83d\ude00"))

    def test_flags(self) -> None:
        self.assertEqual("dgimsvy", "".join(map(chr, R.regexp_flags(R.regexp_create(u("a"), u("yvsmigd"))))))

    def test_string_indices(self) -> None:
        s = u("a\ud83d\ude00b")
        self.assertEqual([0x61, 0x1F600, 0x62], R.string_to_code_points(s))
        self.assertEqual([0, 1, 3, 4, 4], [R.get_string_index(s, k) for k in range(5)])
        self.assertEqual([1, 3, 3, 4, 5], [R.advance_string_index(s, k, True) for k in range(5)])
        self.assertEqual(2, R.advance_string_index(s, 1, False))


@NEEDS_DATA
class Determinism(unittest.TestCase):
    """No answer depends on string hashing or on the locale."""

    PROGRAM = (
        "import sys; sys.path.insert(0, sys.argv[1]); import regexpref as R\n"
        "for p, f, s in ((\"(?<b>.)(?<a>.)|(?<a>.)(?<b>.)\", \"d\", \"pq\"), (\"[\\\\q{ab|a|cd}]+\", \"vi\", \"ABcd\"),\n"
        "                (\"\\\\p{RGI_Emoji}\", \"v\", \"x\\U0001F468\\u200d\\U0001F4BB\"), (\"i\", \"i\", \"\\u0130\")):\n"
        "    c = R.counted_exec(R.utf16(p), R.utf16(f), R.utf16(s))\n"
        "    print(c.steps, c.result if c.result is None else (c.result.index, c.result.captures, c.result.groups))\n"
    )

    def run_with(self, environment: dict[str, str]) -> str:
        env = dict(os.environ)
        env.update(environment)
        return subprocess.run([sys.executable, "-I", "-c", self.PROGRAM, str(CONFORMANCE_DIR)], env=env,
                              capture_output=True, text=True, check=True).stdout

    def test_hash_seeds_and_locales_give_the_same_answers(self) -> None:
        first = self.run_with({"PYTHONHASHSEED": "1", "LC_ALL": "C.UTF-8"})
        self.assertEqual(first, self.run_with({"PYTHONHASHSEED": "4242", "LC_ALL": "tr_TR.UTF-8"}))
        self.assertEqual(first, self.run_with({"PYTHONHASHSEED": "7", "LC_ALL": "lt_LT.UTF-8"}))


class NoEngineNoRe(unittest.TestCase):
    def test_the_reference_imports_no_regular_expression_engine(self) -> None:
        source = (CONFORMANCE_DIR / "regexpref.py").read_text(encoding="utf-8")
        for line in source.split("\n"):
            stripped = line.strip()
            self.assertFalse(stripped.startswith("import re") or stripped.startswith("from re "), line)
        self.assertNotIn("subprocess", source)


if __name__ == "__main__":
    unittest.main()
