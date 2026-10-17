"""Tests of the Unicode area's generator (corpus revision 18, milestone SJL-7.3).

The pin's refusals and the committed files' shape run everywhere. The tests that read the pinned Unicode data,
which the corpus does not archive, run where unicoderef.database() finds it (BROILER_UNICODE_PINS, or a
Broiler.Unicode checkout beside this repository's) and are skipped elsewhere; so is the check of the clause ids
against the pinned specification (BROILER_ECMA262_SPEC, or Broiler.VM's archive beside this repository). Run with
`python3 -I`, as any reading of downloaded data.
"""

from __future__ import annotations

import hashlib
import os
import re
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
REPOSITORY = CONFORMANCE_DIR.parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import generate_unicode_vectors as generator  # noqa: E402
import unicoderef as U  # noqa: E402

CORPUS = REPOSITORY / "conformance" / "vectors"
COMMITTED = CORPUS / "unicode"
FILES = ("casing", "identifiers", "normalize", "properties", "segmenter")
SPEC_VARIABLE = "BROILER_ECMA262_SPEC"
DEFAULT_SPEC = REPOSITORY.parent / "Broiler.VM" / "src" / "Broiler.VM.Profile.JavaScript" / "docs" / "specification" / \
    "ecma-262-es2026-spec.html"
HAVE_DATA = U.pins_directory().is_dir()
NEEDS_DATA = unittest.skipUnless(HAVE_DATA, f"no pinned Unicode data at {U.pins_directory()}; set "
                                            f"{U.PINS_DIRECTORY_VARIABLE}")


def read_records(path: Path) -> list[dict[str, str]]:
    """The records of a vector file, by the schema's section 1."""
    records, current = [], {}
    for line in path.read_text(encoding="utf-8").split("\n"):
        if line.startswith("#") or line == "schema: 1":
            continue
        if not line:
            if current:
                records.append(current)
                current = {}
            continue
        key, _, value = line.partition(": ")
        assert key not in current, (path.name, line)
        current[key] = value
    if current:
        records.append(current)
    return records


def committed() -> dict[str, dict[str, str]]:
    out = {}
    for name in FILES:
        for record in read_records(COMMITTED / f"{name}.vectors"):
            out[record["id"]] = record
    return out


def run_generator(out: Path, ucd: Path) -> subprocess.CompletedProcess:
    """The generator in a process of its own, isolated (-I), so its hash seed is a fresh random one each time."""
    return subprocess.run([sys.executable, "-I", str(CONFORMANCE_DIR / "generate_unicode_vectors.py"), "--ucd", str(ucd),
                           "--out", str(out)], capture_output=True, text=True, check=False)


class PinRefusalTests(unittest.TestCase):
    """The generator refuses a file whose length or SHA-256 is not the pin's, before a byte of it is used, and
    writes nothing; these need no real data."""

    def fake_archive(self, root: Path) -> Path:
        pins = U.Pins(U.PIN_FILE, root)
        for path, size, _ in pins.entries.values():
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b"\0" * size)  # the pinned length, another digest
        return root

    def test_a_file_of_the_right_length_and_another_digest_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            archive = self.fake_archive(Path(scratch) / "pins")
            out = Path(scratch) / "out"
            self.assertEqual(2, generator.main(["--ucd", str(archive), "--out", str(out)]))
            self.assertFalse(out.exists())

    def test_a_missing_directory_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            out = Path(scratch) / "out"
            self.assertEqual(2, generator.main(["--ucd", str(Path(scratch) / "absent"), "--out", str(out)]))
            self.assertFalse(out.exists())

    def test_the_pin_reader_refuses_a_malformed_pin(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            for text in ("version 17.0.0\nfile PropList.txt\npath ucd-17.0.0/PropList.txt\nbytes 1\n",
                         "version 17.0.0\nfile PropList.txt\npath ../PropList.txt\nbytes 1\nsha256 00\n",
                         "version 17.0.0\nfile PropList.txt\npath ucd-17.0.0/Other.txt\nbytes 1\nsha256 00\n",
                         "file PropList.txt\npath ucd-17.0.0/PropList.txt\nbytes 1\nsha256 00\nversion 17.0.0\n",
                         "version 17.0.0\nfile PropList.txt\npath ucd-17.0.0/PropList.txt\npath x/PropList.txt\n"):
                pin = Path(scratch) / "unicode.pin"
                pin.write_text(text, encoding="utf-8")
                with self.assertRaises(U.PinError, msg=text):
                    U.Pins(pin, Path(scratch))
            with self.assertRaises(U.PinError):
                U.Pins(U.PIN_FILE, None)

    def test_the_pin_keeps_revision_1_s_line_and_names_broiler_unicode_s_digests(self) -> None:
        text = U.PIN_FILE.read_text(encoding="utf-8")
        self.assertIn("version 17.0.0\nfile PropList.txt\npath ucd-17.0.0/PropList.txt\nbytes 145465\n"
                      "sha256 130dcddcaadaf071008bdfce1e7743e04fdfbc910886f017d9f9ac931d8c64dd\n", text)
        pins = U.Pins(U.PIN_FILE, REPOSITORY)  # resolving paths needs no file
        self.assertEqual(25, len(pins.entries))
        # The version the C# reader of the corpus takes is the first `version` line (VectorSources.Parse).
        self.assertEqual("version 17.0.0", next(line for line in text.splitlines() if line.startswith("version ")))

    @NEEDS_DATA
    def test_one_changed_byte_in_a_real_archive_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            archive = Path(scratch) / "pins"
            for path, _, _ in U.Pins(U.PIN_FILE, U.pins_directory()).entries.values():
                target = archive / path.relative_to(U.pins_directory().resolve())
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(path, target)
            victim = archive / "ucd-17.0.0" / "SpecialCasing.txt"
            data = bytearray(victim.read_bytes())
            data[-2] ^= 1
            victim.write_bytes(bytes(data))
            out = Path(scratch) / "out"
            self.assertEqual(2, generator.main(["--ucd", str(archive), "--out", str(out)]))
            self.assertFalse(out.exists())


@NEEDS_DATA
class GeneratorTests(unittest.TestCase):
    def test_two_runs_write_the_same_bytes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            first, second = Path(scratch) / "first", Path(scratch) / "second"
            for out in (first, second):
                result = run_generator(out, U.pins_directory())
                self.assertEqual(0, result.returncode, result.stderr)
            names = sorted(p.name for p in first.iterdir())
            self.assertEqual([f"{name}.vectors" for name in FILES], names)
            self.assertEqual(names, sorted(p.name for p in second.iterdir()))
            for name in names:
                self.assertEqual((first / name).read_bytes(), (second / name).read_bytes(), name)

    def test_the_committed_corpus_is_what_the_generator_writes(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            self.assertEqual(0, generator.main(["--ucd", str(U.pins_directory()), "--out", scratch]))
            produced = sorted(p.name for p in Path(scratch).iterdir())
            self.assertEqual(sorted(p.name for p in COMMITTED.glob("*.vectors")), produced)
            for name in produced:
                self.assertEqual((COMMITTED / name).read_bytes(), (Path(scratch) / name).read_bytes(), name)


class CommittedFileTests(unittest.TestCase):
    """The committed files' shape, read the schema's way; no data needed."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.vectors = committed()

    def test_the_generator_writes_the_revision_REVISIONS_md_ends_at(self) -> None:
        revisions = (CORPUS / "REVISIONS.md").read_text(encoding="utf-8")
        rows = [int(line.split("|")[1]) for line in revisions.splitlines() if line[:2] == "| " and line.split("|")[1].strip().isdigit()]
        self.assertEqual(list(range(1, len(rows) + 1)), rows)
        self.assertEqual(generator.CORPUS_REVISION, rows[-1])
        self.assertTrue(all(v["since"] == "18" for v in self.vectors.values()))

    def test_every_script_is_one_line_of_ascii_and_every_vector_is_classified_as_the_milestone_asks(self) -> None:
        for vector_id, vector in self.vectors.items():
            self.assertTrue(all(ord(c) < 0x80 for c in vector["script"]), vector_id)
            self.assertNotIn("observed:", vector["prov"], vector_id)
            self.assertIn("ref:unicoderef@1", vector["prov"].split(" "), vector_id)
            group = vector_id.split("/")[1]
            if group.startswith("segmenter-"):
                self.assertEqual("impl-defined", vector["class"], vector_id)
                self.assertEqual(vector["out"], vector["per-engine.broiler-js"], vector_id)
                self.assertEqual(vector["out"], vector["per-engine.broiler-vm"], vector_id)
            elif vector["class"] == "impl-defined":
                # Only a result past Broiler.VM's string ceiling, which its plan refuses.
                self.assertTrue(group.endswith("-ceiling"), vector_id)
                self.assertEqual("error: RangeError", vector["per-engine.broiler-vm"], vector_id)
                self.assertNotIn("per-engine.broiler-js", vector, vector_id)
            else:
                # Revision 19 classifies the engines' first runs: a normative vector one engine differs on is a
                # divergence, with that engine's field (unicode-divergences.txt).
                self.assertIn(vector["class"], ("normative", "divergence"), vector_id)
                self.assertEqual(vector["class"] == "divergence", any(k.startswith("divergence.") for k in vector), vector_id)
                self.assertTrue(any(e.startswith("spec:es2026@0248456c#") for e in vector["prov"].split(" ")), vector_id)

    def test_every_divergence_field_is_one_unicode_divergences_txt_lists(self) -> None:
        reasons, listed = {}, {}
        for line in generator.DIVERGENCES.read_text(encoding="utf-8").splitlines():
            if not line or line.startswith("#"):
                continue
            if line.startswith("reason "):
                key, _, text = line[len("reason "):].partition(": ")
                reasons[key] = text
                continue
            engine, key, vector_id = line.split(" ")
            listed[(engine, vector_id)] = reasons[key]
        fields = {(k[len("divergence."):], vector_id): v for vector_id, vector in self.vectors.items()
                  for k, v in vector.items() if k.startswith("divergence.")}
        self.assertEqual(listed, fields)
        # At revision 19 Broiler.VM's first run matched every vector.
        self.assertFalse(any(engine == "broiler-vm" for engine, _ in fields))

    def test_every_ucd_file_cited_is_one_the_pin_names(self) -> None:
        pinned = set(U.Pins(U.PIN_FILE, REPOSITORY).entries)
        for vector_id, vector in self.vectors.items():
            for entry in vector["prov"].split(" "):
                if entry.startswith("ucd:"):
                    self.assertTrue(entry.startswith("ucd:17.0.0/"), vector_id)
                    self.assertIn(entry[len("ucd:17.0.0/"):], pinned, vector_id)

    def test_every_clause_cited_is_in_the_pinned_specification(self) -> None:
        spec = Path(os.environ.get(SPEC_VARIABLE) or DEFAULT_SPEC)
        if not spec.is_file():
            self.skipTest(f"no pinned specification at {spec}; set {SPEC_VARIABLE}")
        data = spec.read_bytes()
        pin = (CORPUS / "sources" / "ecma262.pin").read_text(encoding="utf-8")
        self.assertIn(f"sha256 {hashlib.sha256(data).hexdigest()}\n", pin)
        ids = set(re.findall(r'id="([^"]+)"', data.decode("utf-8")))
        cited = {e.split("#", 1)[1] for v in self.vectors.values() for e in v["prov"].split(" ") if e.startswith("spec:")}
        self.assertTrue(cited)
        self.assertEqual(set(), cited - ids)

    def test_the_part_1_sample_is_the_documented_one(self) -> None:
        # Every 512th Hangul syllable and the last, and every tenth of the other lines: 22 + 1 and 592 lines.
        lines = {v["note"].split(",")[0] for v in self.vectors.values() if v["id"].startswith("unicode/normalize-part1/")}
        self.assertEqual(615, len(lines))


class HandCheckedVectorTests(unittest.TestCase):
    """Committed vectors whose outcome was worked out by hand from the standards' text, not from the reference."""

    @classmethod
    def setUpClass(cls) -> None:
        cls.by_script = {}
        for vector in committed().values():
            cls.by_script.setdefault(vector["script"], vector)

    def outcome(self, script: str) -> str:
        self.assertTrue(script in self.by_script, f"no committed vector has the script {script}")
        return self.by_script[script]["out"]

    def test_normalization(self) -> None:
        # NormalizationTest.txt's first line: U+1E0A is D with a dot above.
        self.assertEqual('value: str:"D\\u0307"', self.outcome('"\\u1E0A".normalize("NFD")'))
        self.assertEqual('value: str:"\\u00E9"', self.outcome('"e\\u0301".normalize("NFC")'))
        self.assertEqual('value: str:"fi"', self.outcome('"\\uFB01".normalize("NFKC")'))
        self.assertEqual('value: str:"\\uD800"', self.outcome('"\\uD800".normalize("NFD")'))
        self.assertEqual("error: RangeError", self.outcome('"a".normalize("nfc")'))
        # Hangul: U+D4DB is P + WI + LT, decomposed arithmetically (section 3.12).
        self.assertEqual('value: str:"\\u1111\\u1171\\u11B6"', self.outcome('"\\uD4DB".normalize("NFD")'))

    def test_casing(self) -> None:
        self.assertEqual('value: str:"SS"', self.outcome('"\\u00DF".toUpperCase()'))
        self.assertEqual('value: str:"i\\u0307"', self.outcome('"\\u0130".toLowerCase()'))
        self.assertEqual('value: str:"i"', self.outcome('"\\u0130".toLocaleLowerCase("tr")'))
        self.assertEqual('value: str:"\\u0131"', self.outcome('"I".toLocaleLowerCase("az")'))
        self.assertEqual('value: str:"\\u0130"', self.outcome('"i".toLocaleUpperCase("tr")'))
        self.assertEqual('value: str:"I"', self.outcome('"i\\u0307".toLocaleUpperCase("lt")'))
        self.assertEqual('value: str:"i\\u0307\\u0300"', self.outcome('"I\\u0300".toLocaleLowerCase("lt")'))
        # Row C1: U+00AA is Other_Lowercase, so Cased, and the sigma after it is final.
        self.assertEqual('value: str:"\\u00AA\\u03C2"', self.outcome('"\\u00AA\\u03A3".toLowerCase()'))
        self.assertEqual('value: str:"\\u03B1\\u03C3\\u03B1"', self.outcome('"\\u0391\\u03A3\\u0391".toLowerCase()'))
        self.assertEqual('value: str:"\\uD800A"', self.outcome('"\\uD800a".toUpperCase()'))
        self.assertEqual("error: TypeError", self.outcome('"i".toLocaleUpperCase(null)'))
        # Row F1: under u, U+212A KELVIN SIGN folds to k; without u, the upper case of U+00DF is two units, so it stays.
        self.assertEqual("value: bool:true", self.outcome('/[a-z]/iu.test("\\u212A")'))
        self.assertEqual("value: bool:false", self.outcome('/\\u00DF/i.test("\\u1E9E")'))
        self.assertEqual('value: str:"true,true,true,false"', self.outcome(
            'var r = /^\\u004B$/iu; [r.test("K"), r.test("k"), r.test("\\u212A"), r.test("\\u212B")].join()'))

    def test_properties(self) -> None:
        self.assertEqual("value: bool:true", self.outcome("/\\p{Script=Katakana_Or_Hiragana}/u instanceof RegExp"))
        self.assertEqual("error: SyntaxError", self.outcome("/\\p{lu}/u instanceof RegExp"))
        self.assertEqual("error: SyntaxError", self.outcome("/\\p{IsLatin}/u instanceof RegExp"))
        self.assertEqual("error: SyntaxError", self.outcome("/\\p{RGI_Emoji}/u instanceof RegExp"))
        self.assertEqual("value: bool:true", self.outcome("/\\p{RGI_Emoji}/v instanceof RegExp"))
        self.assertEqual("error: SyntaxError", self.outcome("/\\p{Age=1.1}/u instanceof RegExp"))
        self.assertEqual('value: str:"false,true,true,false"', self.outcome(
            'var r = /^\\p{General_Category=Uppercase_Letter}$/u; [r.test("@"), r.test("A"), r.test("Z"), r.test("[")].join()'))
        self.assertEqual('value: str:"true,true,false"', self.outcome(
            'var r = /^\\p{scx=Grek}$/u; [r.test("\\u0342"), r.test("\\u03B1"), r.test("A")].join()'))
        self.assertEqual("value: bool:true", self.outcome('/^\\p{Basic_Emoji}$/v.test("\\u231A")'))

    def test_identifiers_and_white_space(self) -> None:
        self.assertEqual("error: SyntaxError", self.outcome("var \\u2E2F; true"))
        self.assertEqual("error: SyntaxError", self.outcome("var \\u00B7; true"))
        self.assertEqual("value: bool:true", self.outcome("var a\\u00B7; true"))
        self.assertEqual("value: bool:true", self.outcome("var a\\u200C; true"))
        self.assertEqual("error: SyntaxError", self.outcome("/(?<\\u2E2Fx>.)/ instanceof RegExp"))
        self.assertEqual("error: SyntaxError", self.outcome('(0, eval)("1\\u0085+1")'))
        self.assertEqual("value: f64:0x4000000000000000", self.outcome('(0, eval)("1\\uFEFF+1")'))
        self.assertEqual('value: str:"\\\\x20\\u0085\\\\u3000"', self.outcome('RegExp.escape(" \\u0085\\u3000")'))
        self.assertEqual('value: str:"\\u0085x\\u0085"', self.outcome('"\\u0085x\\u0085".trim()'))

    def test_segmenter(self) -> None:
        segment = 'Array.from(new Intl.Segmenter("en", {{ granularity: "{}" }}).segment("{}"), function (s) {{ return s.index; }}).join()'
        self.assertEqual('value: str:"0"', self.outcome(segment.format("word", "can't")))
        self.assertEqual('value: str:"0,5,10,17,21"', self.outcome(segment.format("sentence", "One. Two? Three! Mr. Smith")))
        self.assertEqual('value: str:"0"', self.outcome(segment.format("grapheme", "\\u000D\\u000A")))
        word_like = ('Array.from(new Intl.Segmenter("en", { granularity: "word" }).segment("a_b c.d 3,000"), '
                     'function (s) { return s.isWordLike ? "Y" : "N"; }).join("")')
        self.assertEqual('value: str:"YNYNY"', self.outcome(word_like))


if __name__ == "__main__":
    unittest.main()
