# PROVENANCE (corpus revision 18, milestone SJL-7.3). A copy of Broiler.Unicode's
# scripts/reference-arms/tests/test_unicoderef_cases.py, git blob ff2ccf9f46ce9ed510122fbce9afc1451f4244a2,
# as committed at Broiler.Unicode efbdc1dbcb9a42fa3abe254f894a7f2241a69601: self-checks of the reference it
# tests, scripts/conformance/unicoderef.py (`ref:unicoderef@1`). One change, marked "CORPUS CHANGE" below: the
# module is skipped when the pinned Unicode data is not where unicoderef.database() looks (BROILER_UNICODE_PINS,
# or a Broiler.Unicode checkout beside this repository's), since the corpus archives none of it. Run with
# `python3 -I`, as any reading of downloaded data.
"""Hand-checked cases: each expected value below was worked out from the standard's text, not from the arbiter."""

from __future__ import annotations

import sys
import unittest
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


def units(text: str) -> list[int]:
    return U.code_units([ord(ch) for ch in text])


def text(units_: list[int]) -> str:
    return "".join(chr(c) for c in U.code_points(units_))


class FinalSigma(unittest.TestCase):
    def lower(self, s: str, language: str = "default") -> str:
        return text(ucd().to_lower(units(s), language))

    def test_final_and_medial(self) -> None:
        self.assertEqual("ας", self.lower("ΑΣ"))
        self.assertEqual("ασα", self.lower("ΑΣΑ"))
        self.assertEqual("σ", self.lower("Σ"))  # nothing cased before it
        self.assertEqual("ας.", self.lower("ΑΣ."))
        self.assertEqual("ας ας", self.lower("ΑΣ ΑΣ"))

    def test_cased_letters_of_other_lowercase(self) -> None:
        # Correspondence row C1: U+00AA and U+00BA are Cased (Other_Lowercase), so Σ after them is final.
        self.assertEqual("ªς", self.lower("ªΣ"))
        self.assertEqual("ºς", self.lower("ºΣ"))

    def test_case_ignorable_in_between_and_the_possessive_reading(self) -> None:
        # Row C2: ' . and U+00B7 are Case_Ignorable, so Α after them makes Σ medial; U+02B0 is both Cased and
        # Case_Ignorable, consumed as case-ignorable (section 3.13: "*" is possessive), so Σ is final.
        self.assertEqual("ασ'α", self.lower("ΑΣ'Α"))
        self.assertEqual("ασ.α", self.lower("ΑΣ.Α"))
        self.assertEqual("ασ·α", self.lower("ΑΣ·Α"))
        self.assertEqual("αςʰ", self.lower("ΑΣʰ"))
        # Section 3.13.1's examples: U+0345 alone before Σ does not satisfy Before, <Α, U+0345> does; U+0345
        # alone after Σ satisfies After, <U+0345, Α> does not.
        self.assertEqual("\u0345σ", self.lower("\u0345Σ"))
        self.assertEqual("α\u0345ς", self.lower("Α\u0345Σ"))
        self.assertEqual("ας\u0345", self.lower("ΑΣ\u0345"))
        self.assertEqual("ασ\u0345α", self.lower("ΑΣ\u0345Α"))

    def test_a_lone_surrogate_is_neither_cased_nor_case_ignorable(self) -> None:
        self.assertEqual([0xDC00, 0x3C3], ucd().to_lower([0xDC00, 0x3A3]))
        self.assertEqual([0x3B1, 0xD800, 0x3C3], ucd().to_lower([0x391, 0xD800, 0x3A3]))
        self.assertEqual([0x3B1, 0x3C2, 0xD800], ucd().to_lower([0x391, 0x3A3, 0xD800]))
        self.assertEqual([0xD800, 0x41], ucd().to_upper([0xD800, 0x61]))


class Turkic(unittest.TestCase):
    def test_dotted_and_dotless_i(self) -> None:
        self.assertEqual("i", text(ucd().to_lower(units("İ"), "turkic")))
        self.assertEqual("i\u0307", text(ucd().to_lower(units("İ"), "default")))
        self.assertEqual("ı", text(ucd().to_lower(units("I"), "turkic")))
        self.assertEqual("İ", text(ucd().to_upper(units("i"), "turkic")))
        self.assertEqual("I", text(ucd().to_upper(units("ı"), "turkic")))
        self.assertEqual("I", text(ucd().to_upper(units("i"), "default")))

    def test_after_i_and_before_dot(self) -> None:
        # I U+0307 lowers to i: I is before a dot (no ı), and the dot is after I (removed).
        self.assertEqual("i", text(ucd().to_lower(units("I\u0307"), "turkic")))
        # A class-220 mark between them does not block either condition.
        self.assertEqual("i\u0316", text(ucd().to_lower(units("I\u0316\u0307"), "turkic")))
        # A class-230 mark does.
        self.assertEqual("ı\u0301\u0307", text(ucd().to_lower(units("I\u0301\u0307"), "turkic")))

    def test_correspondence_row_c4(self) -> None:
        self.assertEqual([0x264], ucd().to_lower([0xA7CB], "turkic"))
        self.assertEqual([0xA7CF], ucd().to_lower([0xA7CE], "turkic"))


class Lithuanian(unittest.TestCase):
    def test_more_above_keeps_the_dot(self) -> None:
        self.assertEqual("i\u0307\u0300", text(ucd().to_lower(units("I\u0300"), "lithuanian")))
        self.assertEqual("j\u0307\u0301", text(ucd().to_lower(units("J\u0301"), "lithuanian")))
        self.assertEqual("į\u0307\u0303", text(ucd().to_lower(units("Į\u0303"), "lithuanian")))
        self.assertEqual("i", text(ucd().to_lower(units("I"), "lithuanian")))
        self.assertEqual("i\u0307\u0300", text(ucd().to_lower(units("Ì"), "lithuanian")))

    def test_more_above_is_blocked_by_class_0(self) -> None:
        # Row C6: U+0903 is of class 0 and intervenes.
        self.assertEqual([0x69, 0x903, 0x300], ucd().to_lower([0x49, 0x903, 0x300], "lithuanian"))
        self.assertEqual([0x69, 0x307, 0x316, 0x300], ucd().to_lower([0x49, 0x316, 0x300], "lithuanian"))

    def test_after_soft_dotted_removes_the_dot_in_upper_case(self) -> None:
        self.assertEqual("I", text(ucd().to_upper(units("i\u0307"), "lithuanian")))
        self.assertEqual("I\u0307", text(ucd().to_upper(units("i\u0307"), "default")))
        self.assertEqual("J\u0301", text(ucd().to_upper(units("j\u0307\u0301"), "lithuanian")))
        # Row C5: U+1DF1A is Soft_Dotted (PropList.txt).
        self.assertEqual(U.code_units([0x1DF1A]), ucd().to_upper(U.code_units([0x1DF1A, 0x307]), "lithuanian"))
        # A class-230 mark in between blocks it.
        self.assertEqual("I\u0301\u0307", text(ucd().to_upper(units("i\u0301\u0307"), "lithuanian")))


class Normalization(unittest.TestCase):
    def test_hangul(self) -> None:
        self.assertEqual([0x1111, 0x1171, 0x11B6], ucd().normalize([0xD4DB], "NFD"))
        self.assertEqual([0xD4DB], ucd().normalize([0x1111, 0x1171, 0x11B6], "NFC"))
        self.assertEqual([0xAC00, 0x11A7], ucd().normalize([0x1100, 0x1161, 0x11A7], "NFC"))  # U+11A7 is no T
        self.assertEqual([0xAC01], ucd().normalize([0xAC00, 0x11A8], "NFC"))

    def test_nfkc_of_u_fdfa(self) -> None:
        expected = [0x0635, 0x0644, 0x0649, 0x0020, 0x0627, 0x0644, 0x0644, 0x0647, 0x0020, 0x0639, 0x0644, 0x064A, 0x0647,
                    0x0020, 0x0648, 0x0633, 0x0644, 0x0645]
        self.assertEqual(expected, ucd().normalize([0xFDFA], "NFKC"))
        self.assertEqual(expected, ucd().normalize([0xFDFA], "NFKD"))
        self.assertEqual([0xFDFA], ucd().normalize([0xFDFA], "NFC"))

    def test_lone_surrogates_pass_through_as_starters(self) -> None:
        self.assertEqual([0xD800], ucd().normalize([0xD800], "NFC"))
        self.assertEqual([0x61, 0xDC00, 0x301], ucd().normalize([0x61, 0xDC00, 0x301], "NFD"))
        self.assertEqual([0x61, 0xDC00, 0x301], ucd().normalize([0x61, 0xDC00, 0x301], "NFC"))
        self.assertEqual([0xE1, 0xD800], ucd().normalize([0x61, 0x301, 0xD800], "NFC"))

    def test_exclusions_and_reordering(self) -> None:
        self.assertEqual([0x915, 0x93C], ucd().normalize([0x958], "NFC"))  # a composition exclusion
        self.assertEqual([0x71, 0x323, 0x307], ucd().normalize([0x71, 0x307, 0x323], "NFC"))
        self.assertEqual([0x1E69], ucd().normalize([0x1E9B, 0x323], "NFKC"))
        self.assertEqual([0x308, 0x301], ucd().normalize([0x344], "NFC"))
        self.assertEqual(U.code_units([0x1D157, 0x1D165]), ucd().normalize(U.code_units([0x1D15E]), "NFC"))


class Segmentation(unittest.TestCase):
    def test_regional_indicator_pairs(self) -> None:
        flags = U.code_units([0x1F1E9, 0x1F1EA, 0x1F1EB, 0x1F1F7, 0x1F1E6])
        self.assertEqual([0, 4, 8, 10], ucd().grapheme_boundaries(flags))
        self.assertEqual([0, 4, 8, 10], ucd().word_boundaries(flags))
        # An Extend after the first indicator joins it (GB9), and the second indicator then starts a cluster:
        # GB12 and GB13 pair only adjacent indicators.
        self.assertEqual([0, 3, 5], ucd().grapheme_boundaries(U.code_units([0x1F1E9, 0x308, 0x1F1EA])))
        # For words, WB4 absorbs the Extend, so the indicators pair (WB15).
        self.assertEqual([0, 5], ucd().word_boundaries(U.code_units([0x1F1E9, 0x308, 0x1F1EA])))

    def test_a_lone_surrogate_is_its_own_code_point(self) -> None:
        self.assertEqual([0, 1, 2], ucd().grapheme_boundaries([0xD800, 0x61]))
        self.assertEqual([0, 1, 2], ucd().word_boundaries([0xD800, 0x61]))
        self.assertEqual([0, 2], ucd().sentence_boundaries([0xD800, 0x61]))
        self.assertEqual([0, 1, 3], ucd().grapheme_boundaries([0xDC00, 0xD83D, 0xDC4D]))

    def test_correspondence_rows_g1_to_g3(self) -> None:
        self.assertEqual([0, 5, 10, 17, 21, 26], ucd().sentence_boundaries(units("One. Two? Three! Mr. Smith")))
        self.assertEqual([0, 5], ucd().word_boundaries(units("can't")))
        self.assertEqual([0, 3, 4, 7, 8, 13], ucd().word_boundaries(units("a_b c.d 3,000")))
        self.assertEqual([0, 1, 2, 3, 4, 8], ucd().word_boundaries(units("日本語のテキスト")))
        self.assertEqual([0, 2, 6, 10], ucd().grapheme_boundaries(units("e\u0301👍🏽🇩🇪")))
        self.assertEqual([0, 2], ucd().grapheme_boundaries([0xD, 0xA]))

    def test_gb9c_and_gb11(self) -> None:
        # क\u094Dष: consonant, virama (Linker), consonant: one cluster.
        self.assertEqual([0, 3], ucd().grapheme_boundaries([0x915, 0x94D, 0x937]))
        # 👨\u200D👩: ExtPict ZWJ ExtPict: one cluster; a ZWJ alone before ExtPict after a letter is not.
        self.assertEqual([0, 5], ucd().grapheme_boundaries(U.code_units([0x1F468, 0x200D, 0x1F469])))
        self.assertEqual([0, 2, 4], ucd().grapheme_boundaries(U.code_units([0x61, 0x200D, 0x1F469])))


class StringProperties(unittest.TestCase):
    def test_emoji_zwj_membership(self) -> None:
        couple = U.code_units([0x1F468, 0x200D, 0x2764, 0xFE0F, 0x200D, 0x1F468])
        self.assertEqual(["RGI_Emoji_ZWJ_Sequence", "RGI_Emoji"], ucd().string_properties(couple))
        without = U.code_units([0x1F468, 0x200D, 0x2764, 0x200D, 0x1F468])
        self.assertEqual([], ucd().string_properties(without))

    def test_other_properties(self) -> None:
        self.assertEqual(["Basic_Emoji", "RGI_Emoji"], ucd().string_properties([0x231A]))
        self.assertEqual(["Basic_Emoji", "RGI_Emoji"], ucd().string_properties([0xA9, 0xFE0F]))
        self.assertEqual([], ucd().string_properties([0xA9]))
        self.assertEqual(["Emoji_Keycap_Sequence", "RGI_Emoji"], ucd().string_properties([0x23, 0xFE0F, 0x20E3]))
        self.assertEqual(["RGI_Emoji_Flag_Sequence", "RGI_Emoji"], ucd().string_properties(U.code_units([0x1F1E9, 0x1F1EA])))
        self.assertEqual(["RGI_Emoji_Modifier_Sequence", "RGI_Emoji"], ucd().string_properties(U.code_units([0x1F44D, 0x1F3FD])))
        self.assertEqual([], ucd().string_properties([0xD83D]))


class Lookups(unittest.TestCase):
    def test_exact_names(self) -> None:
        r = ucd()
        self.assertEqual("General_Category=Uppercase_Letter", r.resolve_lone("Lu"))
        self.assertEqual("General_Category=Cased_Letter", r.resolve_pair("gc", "LC"))
        self.assertEqual("General_Category=Decimal_Number", r.resolve_lone("digit"))
        self.assertEqual("White_Space", r.resolve_lone("space"))
        self.assertEqual("Script_Extensions=Greek", r.resolve_pair("scx", "Grek"))
        self.assertEqual("Script=Katakana_Or_Hiragana", r.resolve_pair("Script", "Hrkt"))
        self.assertEqual("Script=Coptic", r.resolve_pair("sc", "Qaac"))
        for refused in ("lu", "letter", "IsLatin", "WSpace", "Is_Lu", "L&", "Alphabetic ", "rgiemoji", "General_Category",
                        "Script", "Age", "Block", "Latin"):
            self.assertEqual(U.REFUSED, r.resolve_lone(refused, unicode_sets=True), refused)
        for prop, value in (("GC", "Lu"), ("gc", "lu"), ("Script_Extensions", "greek"), ("ASCII", "Y"), ("sc", "Lu"),
                            ("gc", "Latin"), ("Age", "1.1"), ("Alphabetic", "Yes"), ("RGI_Emoji", "Yes"), ("gc", "")):
            self.assertEqual(U.REFUSED, r.resolve_pair(prop, value), (prop, value))

    def test_properties_of_strings_only_under_v(self) -> None:
        self.assertEqual(U.REFUSED, ucd().resolve_lone("RGI_Emoji"))
        self.assertEqual("RGI_Emoji", ucd().resolve_lone("RGI_Emoji", unicode_sets=True))

    def test_canonicalize_without_u(self) -> None:
        r = ucd()
        self.assertEqual(0x212A, r.canonicalize_without_u(0x212A))  # row F2: K (Kelvin) stays
        self.assertEqual(0x4B, r.canonicalize_without_u(0x6B))
        self.assertEqual(0x17F, r.canonicalize_without_u(0x17F))   # long s: S is ASCII
        self.assertEqual(0xDF, r.canonicalize_without_u(0xDF))     # ß: SS is two units
        self.assertEqual(0x3A3, r.canonicalize_without_u(0x3C2))
        self.assertEqual(0xD800, r.canonicalize_without_u(0xD800))
        self.assertEqual(0x1E9E, r.canonicalize_without_u(0x1E9E))
        self.assertEqual(0x6B, r.fold(0x212A))


class Answers(unittest.TestCase):
    def test_the_comparison_s_notation(self) -> None:
        r = ucd()
        self.assertEqual("true", U.answer(r, "binary", "0041", "Alpha"))
        self.assertEqual("Lu", U.answer(r, "gc", "U+0041", "-"))
        self.assertEqual("Hira Kana", U.answer(r, "scx", "30FC", "-"))
        self.assertEqual("004B 006B 212A", U.answer(r, "orbit", "006B", "-"))
        self.assertEqual("refused", U.answer(r, "lookup", "lone/lu/", "u"))
        self.assertEqual("RGI_Emoji", U.answer(r, "lookup", "lone/RGI_Emoji/", "v"))
        self.assertEqual("0130", U.answer(r, "upper", "0069", "tr,en"))
        self.assertEqual("0053 0053", U.answer(r, "upper", "00DF", "default;max=2"))
        self.assertEqual("over", U.answer(r, "upper", "00DF", "default;max=1"))
        self.assertEqual("over", U.answer(r, "normalize", "repeat:00E9:8388608:0061", "NFD;max=16777216"))
        self.assertTrue(U.answer(r, "normalize", "repeat:00E9:8388608:0061", "NFC;max=16777216").startswith("len:8388609:"))
        self.assertEqual("0 2", U.answer(r, "grapheme", "000D 000A", "-"))
        self.assertEqual("true", U.answer(r, "string-property", "231A", "Basic_Emoji"))
        self.assertIsNone(U.answer(r, "charge", "0041", "upper"))


if __name__ == "__main__":
    unittest.main()
