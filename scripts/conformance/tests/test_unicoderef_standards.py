# PROVENANCE (corpus revision 18, milestone SJL-7.3). A copy of Broiler.Unicode's
# scripts/reference-arms/tests/test_unicoderef_standards.py, git blob 7e2b78fa9dd12006ff98654592d2ff35e72eb61c,
# as committed at Broiler.Unicode efbdc1dbcb9a42fa3abe254f894a7f2241a69601: self-checks of the reference it
# tests, scripts/conformance/unicoderef.py (`ref:unicoderef@1`). One change, marked "CORPUS CHANGE" below: the
# module is skipped when the pinned Unicode data is not where unicoderef.database() looks (BROILER_UNICODE_PINS,
# or a Broiler.Unicode checkout beside this repository's), since the corpus archives none of it. Run with
# `python3 -I`, as any reading of downloaded data.
"""The arbiter against the standards' own test files, every line, and against the data files it derives from.

NormalizationTest.txt (every part, the header's invariants, and part 1's closing claim over every code point),
CaseFolding.txt (every C and S line, every other code point folding to itself, every orbit), and
GraphemeBreakTest.txt, WordBreakTest.txt and SentenceBreakTest.txt (every line): an arbiter that fails one of
them arbitrates nothing.
"""

from __future__ import annotations

import sys
import unittest
from collections import defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parents[1]
if str(HERE) not in sys.path:
    sys.path.insert(0, str(HERE))

import unicoderef as U  # noqa: E402

# CORPUS CHANGE: skip, rather than fail, where the pinned data is absent (the generator refuses to run there).
if not U.pins_directory().is_dir():
    raise unittest.SkipTest(f"no pinned Unicode data at {U.pins_directory()}; set {U.PINS_DIRECTORY_VARIABLE}")


def ucd() -> U.UCD:
    return U.database()


def break_cases(name: str):
    for number, line in enumerate(ucd().pins.read(name).split("\n"), 1):
        data = line.split("#", 1)[0].strip()
        if not data:
            continue
        cps, expected = [], []
        for token in data.split():
            if token == "÷":
                expected.append(len(cps))
            elif token != "×":
                cps.append(int(token, 16))
        yield number, cps, expected


class NormalizationTestFile(unittest.TestCase):
    def test_every_line_of_every_part(self) -> None:
        n = ucd().normalize
        failures, cases = [], 0
        for number, line in enumerate(ucd().pins.read("NormalizationTest.txt").split("\n"), 1):
            data = line.split("#", 1)[0].strip()
            if not data or line.startswith("@"):
                continue
            c1, c2, c3, c4, c5 = (U.code_units(U.code_points_of(f)) for f in data.split(";")[:5])
            checks = [c2 == n(c1, "NFC") == n(c2, "NFC") == n(c3, "NFC"), c4 == n(c4, "NFC") == n(c5, "NFC"),
                      c3 == n(c1, "NFD") == n(c2, "NFD") == n(c3, "NFD"), c5 == n(c4, "NFD") == n(c5, "NFD"),
                      all(c4 == n(c, "NFKC") for c in (c1, c2, c3, c4, c5)),
                      all(c5 == n(c, "NFKD") for c in (c1, c2, c3, c4, c5))]
            cases += 1
            if not all(checks):
                failures.append(number)
        self.assertEqual([], failures[:20])
        self.assertEqual(20034, cases)

    def test_part_1_closing_claim_over_every_code_point(self) -> None:
        listed = set()
        part = None
        for line in ucd().pins.read("NormalizationTest.txt").split("\n"):
            if line.startswith("@Part"):
                part = line.split()[0]
                continue
            data = line.split("#", 1)[0].strip()
            if part == "@Part1" and data:
                listed.add(U.code_points_of(data.split(";")[0])[0])
        changed = []
        for cp in range(U.CODE_POINTS):
            if cp in listed:
                continue
            units = U.code_units([cp])
            if any(ucd().normalize(units, form) != units for form in U.FORMS):
                changed.append(cp)
        self.assertEqual([], changed[:20])


class CaseFoldingFile(unittest.TestCase):
    def test_every_c_and_s_line_and_every_other_code_point(self) -> None:
        listed = {}
        for rec in U.records(ucd().pins.read("CaseFolding.txt")):
            if rec[1] in ("C", "S"):
                listed[int(rec[0], 16)] = int(rec[2], 16)
        self.assertEqual(1512, len(listed))
        wrong = [cp for cp in range(U.CODE_POINTS) if ucd().fold(cp) != listed.get(cp, cp)]
        self.assertEqual([], wrong[:20])

    def test_every_orbit_is_the_set_with_the_same_folding(self) -> None:
        classes = defaultdict(set)
        for cp in range(U.CODE_POINTS):
            classes[ucd().fold(cp)].add(cp)
        wrong = [cp for cp in range(U.CODE_POINTS) if set(ucd().orbit(cp)) != classes[ucd().fold(cp)]]
        self.assertEqual([], wrong[:20])
        self.assertEqual((0x4B, 0x6B, 0x212A), ucd().orbit(0x212A))
        self.assertEqual((0x3A3, 0x3C2, 0x3C3), ucd().orbit(0x3C2))


class BreakTestFiles(unittest.TestCase):
    def check(self, name: str, function, count: int) -> None:
        failures, cases = [], 0
        for number, cps, expected in break_cases(name):
            cases += 1
            if function(cps) != expected:
                failures.append(number)
            # The same boundaries in UTF-16 code units through the public entry point.
            units = U.code_units(cps)
            offsets = [len(U.code_units(cps[:i])) for i in expected]
            if {"GraphemeBreakTest.txt": ucd().grapheme_boundaries, "WordBreakTest.txt": ucd().word_boundaries,
                    "SentenceBreakTest.txt": ucd().sentence_boundaries}[name](units) != offsets:
                failures.append(-number)
        self.assertEqual([], failures[:20])
        self.assertEqual(count, cases)

    def test_grapheme_break_test(self) -> None:
        self.check("GraphemeBreakTest.txt", ucd().grapheme_breaks, 766)

    def test_word_break_test(self) -> None:
        self.check("WordBreakTest.txt", ucd().word_breaks, 1944)

    def test_sentence_break_test(self) -> None:
        self.check("SentenceBreakTest.txt", ucd().sentence_breaks, 512)


class DataFiles(unittest.TestCase):
    def test_general_category_agrees_with_derived_general_category(self) -> None:
        derived = bytearray(U.CODE_POINTS)  # 0: Cn by @missing
        for rec in U.records(ucd().pins.read("DerivedGeneralCategory.txt")):
            a, b = U.code_range(rec[0])
            derived[a:b + 1] = bytes([ucd().gc_index[rec[1]]]) * (b - a + 1)
        self.assertEqual(bytes(derived), bytes(ucd().gc))

    def test_full_composition_exclusion_holds_singletons_and_non_starter_decompositions(self) -> None:
        for cp, d in ucd().canonical.items():
            if len(d) == 1 or ucd().ccc[cp] != 0 or ucd().ccc[d[0]] != 0:
                self.assertIn(cp, ucd().composition_exclusions, f"{cp:04X}")

    def test_id_start_and_id_continue_exclude_pattern_syntax(self) -> None:
        sets = ucd().sets()
        self.assertEqual(ucd().binary["ID_Start"], sets["lexical:ID_Start-Pattern_Syntax"])
        self.assertEqual(ucd().binary["ID_Continue"], sets["lexical:ID_Continue-Pattern_Syntax"])
        self.assertFalse(U.ranges_contain(sets["lexical:IdentifierStartChar"], 0x2E2F))

    def test_every_code_point_alone_maps_as_specialcasing_and_unicodedata_say(self) -> None:
        # Decision rule part A4: alone, the default full mapping is SpecialCasing.txt's unconditional line, or
        # UnicodeData.txt's simple mapping, or the code point itself.
        unconditional = {}
        for rec in U.records(ucd().pins.read("SpecialCasing.txt")):
            if len(rec) <= 4 or not rec[4]:
                unconditional[int(rec[0], 16)] = (U.code_points_of(rec[1]), U.code_points_of(rec[3]))
        simple_upper, simple_lower = {}, {}
        for line in ucd().pins.read("UnicodeData.txt").split("\n"):
            if line:
                f = line.split(";")
                if f[12]:
                    simple_upper[int(f[0], 16)] = int(f[12], 16)
                if f[13]:
                    simple_lower[int(f[0], 16)] = int(f[13], 16)
        wrong = []
        for cp in range(U.CODE_POINTS):
            lower, upper = unconditional.get(cp, ((simple_lower.get(cp, cp),), (simple_upper.get(cp, cp),)))
            if tuple(ucd().change_case([cp], True)) != upper or tuple(ucd().change_case([cp], False)) != lower:
                wrong.append(cp)
        self.assertEqual([], wrong[:20])

    def test_every_name_the_tables_admit_resolves_to_their_property(self) -> None:
        # Decision rule part A5.
        for alias, canonical in ucd().ecma_binary.items():
            self.assertEqual(canonical, ucd().resolve_lone(alias))
        for name in ucd().ecma_strings:
            self.assertEqual(U.REFUSED, ucd().resolve_lone(name))
            self.assertEqual(name, ucd().resolve_lone(name, unicode_sets=True))
        for prop in ("General_Category", "gc"):
            for rec in ucd().value_aliases["gc"]:
                for alias in rec:
                    self.assertEqual(f"General_Category={rec[1]}", ucd().resolve_pair(prop, alias))
                    self.assertEqual(f"General_Category={rec[1]}", ucd().resolve_lone(alias))
        for prop, canonical in (("Script", "Script"), ("sc", "Script"), ("Script_Extensions", "Script_Extensions"), ("scx", "Script_Extensions")):
            for rec in ucd().value_aliases["sc"]:
                for alias in rec:
                    self.assertEqual(f"{canonical}={rec[1]}", ucd().resolve_pair(prop, alias))
        self.assertEqual(53, len(set(ucd().ecma_binary.values())))

    def test_the_general_category_groups_are_their_members(self) -> None:
        sets = ucd().sets()
        letters = U.normalize_ranges(r for v in ("Uppercase_Letter", "Lowercase_Letter", "Titlecase_Letter", "Modifier_Letter", "Other_Letter")
                                     for r in sets[f"General_Category={v}"])
        self.assertEqual(letters, sets["General_Category=Letter"])
        self.assertEqual([(0, U.CODE_POINTS - 1)], U.normalize_ranges(r for v in ("Letter", "Mark", "Number", "Punctuation", "Symbol", "Separator", "Other")
                                                                        for r in sets[f"General_Category={v}"]))

    def test_script_extensions_default_to_script(self) -> None:
        self.assertEqual(("Grek",), ucd().script_extensions(0x3B1))
        self.assertIn("Grek", ucd().script_extensions(0x342))
        self.assertEqual(("Hira", "Kana"), ucd().script_extensions(0x30FC))
        self.assertEqual("Zzzz", ucd().script(0x378))
        sets = ucd().sets()
        self.assertTrue(U.ranges_contain(sets["Script_Extensions=Unknown"], 0x378))
        self.assertFalse(U.ranges_contain(sets["Script_Extensions=Common"], 0x30FC))
        self.assertTrue(U.ranges_contain(sets["Script=Common"], 0x30FC))
        self.assertEqual([], sets["Script=Katakana_Or_Hiragana"])


if __name__ == "__main__":
    unittest.main()
