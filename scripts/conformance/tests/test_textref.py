from __future__ import annotations

import base64
import json
import random
import sys
import tempfile
import unittest
import urllib.parse
from pathlib import Path

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
REPOSITORY = CONFORMANCE_DIR.parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import generate_text_vectors as generator
import numref as R
import textref as T


class Utf16Tests(unittest.TestCase):
    def test_code_point_at(self) -> None:
        self.assertEqual(T.CodePoint(0x1F600, 2, False), T.code_point_at("\uD83D\uDE00", 0))
        self.assertEqual(T.CodePoint(0xDE00, 1, True), T.code_point_at("\uD83D\uDE00", 1))
        self.assertEqual(T.CodePoint(0xD800, 1, True), T.code_point_at("\uD800", 0))
        self.assertEqual(T.CodePoint(0xD800, 1, True), T.code_point_at("\uD800a", 0))
        self.assertEqual(T.CodePoint(0x61, 1, False), T.code_point_at("a", 0))

    def test_well_formedness(self) -> None:
        self.assertTrue(T.is_string_well_formed_unicode("a\uD83D\uDE00b"))
        self.assertFalse(T.is_string_well_formed_unicode("\uDE00\uD83D"))
        self.assertEqual("\uFFFD\uFFFD", T.to_well_formed("\uDE00\uD83D"))
        self.assertEqual("a\uD83D\uDE00\uFFFD", T.to_well_formed("a\uD83D\uDE00\uDC00"))

    def test_trim_uses_the_pinned_white_space(self) -> None:
        self.assertEqual("a", T.trim_string("\uFEFF\u3000a\u2029\t", "start+end"))
        self.assertEqual("\u0085a\u180E", T.trim_string("\u0085a\u180E", "start+end"))
        self.assertEqual("a ", T.trim_string(" a ", "start"))
        self.assertEqual(" a", T.trim_string(" a ", "end"))

    def test_advance_string_index(self) -> None:
        self.assertEqual(2, T.advance_string_index("\uD83D\uDE00", 0, True))
        self.assertEqual(1, T.advance_string_index("\uD83D\uDE00", 0, False))
        self.assertEqual(2, T.advance_string_index("\uD83D\uDE00", 1, True))
        self.assertEqual(3, T.advance_string_index("ab", 2, True))


class PaddingTests(unittest.TestCase):
    def test_string_pad(self) -> None:
        self.assertEqual("abcabxyz", T.string_pad("xyz", 8, "abc", "start"))
        self.assertEqual("xyzabcab", T.string_pad("xyz", 8, "abc", "end"))
        self.assertEqual("xyz", T.string_pad("xyz", 8, "", "end"))
        self.assertEqual("xyz", T.string_pad("xyz", 2, "abc", "end"))

    def test_relative_index(self) -> None:
        self.assertEqual(0, T.relative_index(float("-inf"), 7))
        self.assertEqual(4, T.relative_index(-3, 7))
        self.assertEqual(0, T.relative_index(-30, 7))
        self.assertEqual(7, T.relative_index(30, 7))
        self.assertEqual(7, T.relative_index(float("inf"), 7))


class SubstitutionTests(unittest.TestCase):
    def test_the_clause_s_cases(self) -> None:
        subject, position, matched = "xbdy", 1, "b"
        def sub(template: str, captures: list[str | None], named: dict | None = None) -> str:
            return T.get_substitution(matched, subject, position, captures, named, template)
        self.assertEqual("$", sub("$$", []))
        self.assertEqual("x", sub("$`", []))
        self.assertEqual("dy", sub("$'", []))
        self.assertEqual("b", sub("$&", []))
        self.assertEqual("$0", sub("$0", ["b"]))
        self.assertEqual("$00", sub("$00", ["b"]))
        self.assertEqual("b0", sub("$10", ["b"]))
        self.assertEqual("b", sub("$01", ["b"]))
        self.assertEqual("$2", sub("$2", ["b"]))
        self.assertEqual("", sub("$2", ["b", None]))
        self.assertEqual("$<a>", sub("$<a>", ["b"]))
        self.assertEqual("b", sub("$<a>", ["b"], {"a": "b"}))
        self.assertEqual("", sub("$<z>", ["b"], {"a": "b"}))
        self.assertEqual("$<a", sub("$<a", ["b"], {"a": "b"}))


class UriTests(unittest.TestCase):
    def test_encode_agrees_with_urllib_on_well_formed_text(self) -> None:
        rng = random.Random(5)
        for _ in range(2000):
            text = "".join(chr(rng.choice([rng.randrange(0x20, 0x7F), rng.randrange(0xA0, 0xD800),
                                           rng.randrange(0xE000, 0x10000), rng.randrange(0x10000, 0x110000)]))
                           for _ in range(rng.randrange(0, 6)))
            self.assertEqual(urllib.parse.quote(text, safe="-_.!~*'()"), T.encode(T.units(text), ""))

    def test_decode_refuses_what_strict_utf8_refuses(self) -> None:
        for escaped in ("%C0%80", "%E0%80%80", "%ED%A0%80", "%F4%90%80%80", "%F8%88%80%80%80", "%80", "%C3", "%C3%28"):
            self.assertEqual(T.Error("URIError"), T.decode(escaped, ""), escaped)
        self.assertEqual("\u20ac", T.decode("%E2%82%AC", ""))
        self.assertEqual("%3B", T.decode("%3B", T.PRESERVE_URI))
        self.assertEqual(";", T.decode("%3B", ""))

    def test_escape_and_unescape(self) -> None:
        self.assertEqual("a%20b%u0100@*_+-./", T.escape("a b\u0100@*_+-./"))
        self.assertEqual("\u00e9A%zz%u12", T.unescape("%u00e9%41%zz%u12"))
        self.assertEqual("%u00zz41", T.unescape("%u00zz41"))


class Base64Tests(unittest.TestCase):
    def test_strict_decoding_agrees_with_the_standard_library(self) -> None:
        rng = random.Random(6)
        for _ in range(2000):
            data = bytes(rng.randrange(256) for _ in range(rng.randrange(0, 20)))
            text = base64.b64encode(data).decode()
            self.assertEqual(T.DecodeResult(len(text), list(data)), T.from_base64(text, "base64", "strict"))
            url = base64.urlsafe_b64encode(data).decode().rstrip("=")
            self.assertEqual(list(data), T.from_base64(url, "base64url", "loose").bytes)
            self.assertEqual(text, T.to_base64(data, "base64", False))
            self.assertEqual(url, T.to_base64(data, "base64url", True))
            self.assertEqual(data.hex(), T.to_hex(data))
            self.assertEqual(list(data), T.from_hex(data.hex()).bytes)

    def test_the_proposal_s_cases(self) -> None:
        self.assertEqual(T.DecodeResult(2, [0xAA]), T.from_hex("aaZZ", 1))
        self.assertEqual(T.DecodeResult(2, [0xAA], "SyntaxError"), T.from_hex("aaZZ", 2))
        self.assertEqual(T.DecodeResult(0, [], "SyntaxError"), T.from_hex("aab"))
        self.assertEqual(T.DecodeResult(4, [104, 101, 108]), T.from_base64("aGVsbA", "base64", "stop-before-partial"))
        self.assertEqual("SyntaxError", T.from_base64("aGVsbA", "base64", "strict").error)
        self.assertEqual([104, 101, 108, 108], T.from_base64("aGVsbA", "base64", "loose").bytes)
        self.assertEqual("SyntaxError", T.from_base64("YR==", "base64", "strict").error)
        self.assertEqual([0x61], T.from_base64("YR==", "base64", "loose").bytes)
        self.assertEqual("SyntaxError", T.from_base64("+/8=", "base64url", "loose").error)
        # Stops where the next chunk would not fit, and reports what it read.
        self.assertEqual(T.DecodeResult(4, [104, 101, 108]), T.from_base64("aGVsbG8=", "base64", "loose", 3))
        self.assertEqual(T.DecodeResult(4, [104, 101, 108]), T.from_base64("aGVsbG8=", "base64", "loose", 4))


class JsonTests(unittest.TestCase):
    def test_parse_agrees_with_the_standard_library_on_valid_text(self) -> None:
        rng = random.Random(7)
        def value(depth: int) -> object:
            kind = rng.randrange(6 if depth < 4 else 4)
            if kind == 0:
                return rng.choice([True, False, None])
            if kind == 1:
                return rng.choice([0, -1, 7, 2 ** 60, 0.5, -1.25e-7, 1e300])
            if kind in (2, 3):
                return "".join(rng.choice("ab\"\\\n\u00e9\u2028") for _ in range(rng.randrange(4)))
            if kind == 4:
                return [value(depth + 1) for _ in range(rng.randrange(3))]
            return {rng.choice("kxy"): value(depth + 1) for _ in range(rng.randrange(3))}
        for _ in range(1000):
            data = value(0)
            for text in (json.dumps(data), json.dumps(data, indent=2)):
                parsed = T.json_parse(text)
                self.assertEqual(json.dumps(data, separators=(",", ":"), ensure_ascii=False),
                                 _python_text(parsed), text)

    def test_the_grammar_s_refusals(self) -> None:
        for text in ("", "01", "1.", ".1", "+1", "[1,]", "{\"a\":1,}", "NaN", "Infinity", "'a'", "\"a\tb\"",
                     "\"\\x41\"", "\"\\u12\"", "\uFEFF[]", "\u00a0[]", "[1 2]", "1 2"):
            with self.assertRaises(T.JsonSyntaxError, msg=text):
                T.json_parse(text)

    def test_numbers_and_surrogates(self) -> None:
        self.assertEqual(R.SIGN_BIT, T.json_parse("-0").value)
        self.assertEqual(0x7FF0000000000000, T.json_parse("1e400").value)
        self.assertEqual("\uD800", T.json_parse("\"\\ud800\"").value)
        self.assertEqual("\uD800", T.json_parse("\"\uD800\"").value)

    def test_quote_and_serialize(self) -> None:
        self.assertEqual("\"\\u0001\\ud800\u2028\\\"\uD83D\uDE00\"", T.quote_json_string("\x01\uD800\u2028\"\uD83D\uDE00"))
        parsed = T.json_parse("{\"a\":[1,{}],\"a\":2,\"b\":[]}")
        self.assertEqual(["{\"a\":2,\"b\":[]}"], T.serialize(parsed, ""))
        self.assertEqual(["{\n  \"a\": 2,\n  \"b\": []\n}"], T.serialize(parsed, "  "))
        self.assertEqual(["1.2345678901234567e+29", "1.2345678901234568e+29"],
                         T.serialize(T.json_parse("123456789012345678901234567890"), ""))


def _python_text(value: T.JsonValue) -> str:
    """Compact text of a parsed value as Python's json writes it, for the cross-check only."""
    if value.kind in ("true", "false", "null"):
        return value.kind
    if value.kind == "string":
        return json.dumps(value.value, ensure_ascii=False)
    if value.kind == "number":
        number = struct_double(value.value)
        return json.dumps(int(number) if number.is_integer() and abs(number) < 2 ** 63 else number)
    if value.kind == "array":
        return "[" + ",".join(_python_text(item) for item in value.value) + "]"
    return "{" + ",".join(json.dumps(k, ensure_ascii=False) + ":" + _python_text(v) for k, v in value.value.items()) + "}"


def struct_double(bits: int) -> float:
    import struct
    return struct.unpack(">d", struct.pack(">Q", bits))[0]


class GeneratorTests(unittest.TestCase):
    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        committed = REPOSITORY / "conformance" / "vectors" / "text"
        with tempfile.TemporaryDirectory() as scratch:
            self.assertEqual(0, generator.main(["--out", scratch]))
            produced = sorted(p.name for p in Path(scratch).iterdir())
            self.assertEqual(sorted(p.name for p in committed.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((committed / name).read_bytes(), (Path(scratch) / name).read_bytes(), name)

    def test_the_generator_writes_the_revision_REVISIONS_md_ends_at(self) -> None:
        revisions = (REPOSITORY / "conformance" / "vectors" / "REVISIONS.md").read_text(encoding="utf-8")
        rows = [int(line.split("|")[1]) for line in revisions.splitlines() if line[:2] == "| " and line.split("|")[1].strip().isdigit()]
        self.assertEqual(generator.CORPUS_REVISION, rows[-1])


if __name__ == "__main__":
    unittest.main()
