"""Tests of the reference datasets' copies and their harness (bundle sjl-8-001's rule, part C).

The copies are held to datasets.pin on every run; the harness's line split, divergences and comparison are
held to what Broiler.VM's IntlChecks.RetainedMatches does; its transport from the driver, its reports and its
base-to-after join are checked on made-up runs; and a negative control shows that one line of the expected
answer changed in memory is reported as a difference.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

CONFORMANCE_DIR = Path(__file__).resolve().parents[1]
if str(CONFORMANCE_DIR) not in sys.path:
    sys.path.insert(0, str(CONFORMANCE_DIR))

import run_reference_datasets as H  # noqa: E402

COPIES_SJL_8 = ("timezones.js", "timezones.icu-77.1.txt", "timezones.divergences.txt",
                "temporal.js", "temporal.polyfill-e8cc03fc.txt", "temporal.divergences.txt")
COPIES_SJL_10 = ("temporal-calendars.js", "temporal-calendars.polyfill-e8cc03fc.txt",
                 "temporal-calendars.divergences.txt")
COPIES = COPIES_SJL_8 + COPIES_SJL_10
BROILER_VM_9508BD5 = "9508bd5b9b6d0f6d009418dda351fb0e7d846745"
BROILER_VM_14072B0 = "14072b02027cde4c0fa83e5a0d2f1ac7e68b1f36"


def driver_escape(text: str) -> str:
    """What reference_dataset_driver.js's `ascii` does, over UTF-16 code units."""
    data = text.encode("utf-16-le", errors="surrogatepass")
    out = []
    for i in range(0, len(data), 2):
        unit = data[i] | (data[i + 1] << 8)
        out.append(chr(unit) if 0x20 <= unit <= 0x7E and unit != 0x5C else f"\\u{unit:04x}")
    return "".join(out)


def driver_stdout(program_file: str, program_text: str, completion: str) -> str:
    return (f"reference-dataset-driver 1 {program_file} {H.utf16_length(program_text)}\n"
            f"reference-dataset-completion string {H.utf16_length(completion)} {driver_escape(completion)}\n")


def read_copy(name: str) -> str:
    return H.read_all_text((H.DATASETS / name).read_bytes())


def vm_answer(program: H.Program) -> list[str]:
    """Broiler.VM's own answer, as its check holds it: the retained answer with the divergences applied."""
    retained = H.split_retained(read_copy(program.answer))
    replaced = H.read_divergences(read_copy(program.divergences))
    return H.expected_answer(retained, replaced, len(retained)).lines


class PinTests(unittest.TestCase):
    def setUp(self) -> None:
        self.pin = H.read_pin((H.DATASETS / H.PIN_NAME).read_text(encoding="utf-8"))

    def test_every_copy_equals_its_line(self) -> None:
        self.assertEqual([], H.verify_copies(H.DATASETS, self.pin))
        H.load_datasets(H.DATASETS)

    def test_the_pin_names_broiler_vm_9508bd5_and_the_six_files(self) -> None:
        self.assertEqual((BROILER_VM_9508BD5, BROILER_VM_14072B0), self.pin.source_commits)
        self.assertEqual(set(COPIES), set(self.pin.files))
        self.assertEqual(
            [("timezones", "timezones.js", "timezones.icu-77.1.txt", "timezones.divergences.txt"),
             ("temporal", "temporal.js", "temporal.polyfill-e8cc03fc.txt", "temporal.divergences.txt"),
             ("temporal-calendars", "temporal-calendars.js", "temporal-calendars.polyfill-e8cc03fc.txt",
              "temporal-calendars.divergences.txt")],
            [(p.name, p.program, p.answer, p.divergences) for p in self.pin.programs])
        self.assertEqual("src/tests/cldr/timezones/divergences.txt", self.pin.files["timezones.divergences.txt"].source_path)
        self.assertEqual("src/tests/temporal/divergences.txt", self.pin.files["temporal.divergences.txt"].source_path)
        for name in COPIES_SJL_8:
            self.assertEqual(BROILER_VM_9508BD5, self.pin.files[name].source_commit, name)

    def test_sjl_10_2_adds_temporal_calendars_from_broiler_vm_14072b0(self) -> None:
        for name in COPIES_SJL_10:
            self.assertEqual(BROILER_VM_14072B0, self.pin.files[name].source_commit, name)
        self.assertEqual("src/tests/temporal/calendars/temporal-calendars.js",
                         self.pin.files["temporal-calendars.js"].source_path)
        self.assertEqual("src/tests/temporal/calendars/divergences.txt",
                         self.pin.files["temporal-calendars.divergences.txt"].source_path)

    def test_a_file_line_takes_the_source_commit_above_it(self) -> None:
        text = ("source-commit a\nprogram p p.js p.txt p.div\nfile p.js 1 s b x/p.js\nfile p.txt 1 s b x/p.txt\n"
                "source-commit b\nfile p.div 1 s b x/divergences.txt\n")
        pin = H.read_pin(text)
        self.assertEqual(("a", "b"), pin.source_commits)
        self.assertEqual(["a", "a", "b"], [pin.files[n].source_commit for n in ("p.js", "p.txt", "p.div")])
        for bad in ("file p.js 1 s b x/p.js\nsource-commit a\nprogram p p.js p.js p.js\n",
                    "source-commit a\nfile p.js 1 s b x\nsource-commit a\nprogram p p.js p.js p.js\n",
                    "source-commit a\nfile p.js 1 s b x\nprogram p p.js p.js p.js\nprogram p p.js p.js p.js\n"):
            with self.assertRaises(H.HarnessError, msg=bad):
                H.read_pin(bad)

    def test_each_git_blob_id_is_the_copy_s_own(self) -> None:
        # The blob id the pin records is the one Broiler.VM's tree at 9508bd5 names, and it is a digest of
        # the bytes, so the copy is that blob.
        for pinned in self.pin.files.values():
            data = (H.DATASETS / pinned.name).read_bytes()
            blob = hashlib.sha1(b"blob %d\0" % len(data) + data).hexdigest()
            self.assertEqual(pinned.blob, blob, pinned.name)

    def test_the_directory_holds_the_copies_the_pin_and_the_readme_alone(self) -> None:
        self.assertEqual(set(COPIES) | {H.PIN_NAME, "README.md"}, {p.name for p in H.DATASETS.iterdir()})

    def test_a_copy_whose_digest_differs_is_refused(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            copy = Path(scratch) / "time"
            shutil.copytree(H.DATASETS, copy)
            path = copy / "temporal.polyfill-e8cc03fc.txt"
            data = bytearray(path.read_bytes())
            data[100] ^= 0x01
            path.write_bytes(bytes(data))
            problems = H.verify_copies(copy, self.pin)
            self.assertEqual(1, len(problems))
            self.assertIn("temporal.polyfill-e8cc03fc.txt: SHA-256", problems[0])
            with self.assertRaises(H.HarnessError):
                H.load_datasets(copy)
            with mock.patch("sys.stderr"):
                self.assertEqual(2, H.main(["verify", "--datasets", str(copy)]))

    def test_no_end_of_line_conversion_touches_the_copies(self) -> None:
        try:
            process = subprocess.run(["git", "-C", str(H.REPOSITORY), "check-attr", "text", "--",
                                      *(f"conformance/datasets/time/{name}" for name in COPIES)],
                                     capture_output=True, text=True, check=False)
        except OSError:
            self.skipTest("git is not available")
        if process.returncode != 0:
            self.skipTest("not a git work tree")
        for line in process.stdout.splitlines():
            self.assertTrue(line.endswith(": text: unset"), line)
        for name in COPIES:
            self.assertNotIn(b"\r", (H.DATASETS / name).read_bytes(), name)


class LineSemanticsTests(unittest.TestCase):
    """IntlChecks.RetainedMatches, case by case."""

    def test_a_key_is_the_text_before_the_arrow_or_else_before_resolved(self) -> None:
        self.assertEqual("Asia/Calcutta", H.line_key("Asia/Calcutta => Asia/Kolkata | GMT+05:30"))
        self.assertEqual("en-US", H.line_key("en-US resolved en-US-u-ca-gregory"))
        self.assertEqual("a resolved b", H.line_key("a resolved b => c"))
        self.assertEqual("no separator", H.line_key("no separator"))
        self.assertEqual("x", H.line_key("x => y => z"))
        self.assertEqual("x =>y", H.line_key("x =>y"))

    def test_read_all_lines_ends_a_line_at_crlf_cr_or_lf(self) -> None:
        self.assertEqual(["a", "b", "c", "d"], H.read_all_lines("a\r\nb\rc\nd"))
        self.assertEqual(["a"], H.read_all_lines("a\n"))
        self.assertEqual(["a", ""], H.read_all_lines("a\n\n"))
        self.assertEqual(["a", ""], H.read_all_lines("a\r\r\n"))
        self.assertEqual([""], H.read_all_lines("\n"))
        self.assertEqual([], H.read_all_lines(""))
        self.assertEqual(["a\x0bb\u2028c"], H.read_all_lines("a\x0bb\u2028c"))

    def test_divergences_skip_empty_and_hash_lines_and_a_later_line_wins(self) -> None:
        text = "# a comment => x\n\nA => 1\r\n #not a comment => 2\nB resolved b\nA => 3\n"
        self.assertEqual({"A": "A => 3", " #not a comment": " #not a comment => 2", "B": "B resolved b"},
                         H.read_divergences(text))

    def test_the_retained_answer_loses_every_trailing_lf_and_the_engine_s_none(self) -> None:
        self.assertEqual(["a", "b"], H.split_retained("a\nb\n\n\n"))
        self.assertEqual(["a", "", "b"], H.split_retained("a\n\nb\n"))
        self.assertEqual([""], H.split_retained(""))
        self.assertEqual(["a\r", "b"], H.split_retained("a\r\nb\n"))
        self.assertEqual(["a", "b", ""], H.split_answer("a\nb\n"))
        self.assertEqual(["a\r", "b"], H.split_answer("a\r\nb"))

    def test_a_divergence_replaces_the_line_its_key_names_and_counts_as_used(self) -> None:
        expected, results = H.compare_program("A => 1\nB => 2\nC => 3\n", "# why\nB => two\n",
                                              ["A => 1", "B => two", "C => 3"])
        self.assertEqual(["A => 1", "B => two", "C => 3"], expected.lines)
        self.assertEqual(["retained", "divergence", "retained"], expected.sources)
        self.assertEqual(1, expected.used)
        self.assertTrue(all(r.equal for r in results))
        _, results = H.compare_program("A => 1\nB => 2\n", "B => two\n", ["A => 1", "B => 2"])
        self.assertEqual([True, False], [r.equal for r in results])

    def test_a_line_past_either_end_is_no_line(self) -> None:
        _, results = H.compare_program("A => 1\nB => 2\n", "", ["A => 1"])
        self.assertEqual(("B => 2", H.NO_LINE, False), (results[1].expected, results[1].actual, results[1].equal))
        _, results = H.compare_program("A => 1\n", "", ["A => 1", "extra"])
        self.assertEqual((H.NO_LINE, "extra", False), (results[1].expected, results[1].actual, results[1].equal))
        # A trailing LF in the engine's value is one more line, as Broiler.VM's check reads it.
        _, results = H.compare_program("A => 1\n", "", H.split_answer("A => 1\n"))
        self.assertEqual([True, False], [r.equal for r in results])

    def test_a_divergence_can_even_name_no_line_as_the_check_s_loop_does(self) -> None:
        expected, results = H.compare_program("A\n", f"{H.NO_LINE}\n", ["A", H.NO_LINE])
        self.assertEqual(["A", H.NO_LINE], expected.lines)
        self.assertEqual(1, expected.used)
        self.assertTrue(all(r.equal for r in results))

    def test_the_comparison_is_ordinal(self) -> None:
        _, results = H.compare_program("e\u0301 => x\nA => 1\n", "", ["\u00e9 => x", "a => 1"])
        self.assertEqual([False, False], [r.equal for r in results])


class TheDatasetsTests(unittest.TestCase):
    """The copies as Broiler.VM's two checks read them."""

    def setUp(self) -> None:
        self.pin = H.load_datasets(H.DATASETS)
        self.programs = {p.name: p for p in self.pin.programs}

    def test_timezones_expects_610_lines_and_uses_each_of_its_51_divergences_once(self) -> None:
        program = self.programs["timezones"]
        retained = H.split_retained(read_copy(program.answer))
        replaced = H.read_divergences(read_copy(program.divergences))
        expected = H.expected_answer(retained, replaced, len(retained))
        self.assertEqual(610, len(retained))
        self.assertGreater(len(retained), H.VM_LEAST["timezones"])
        self.assertEqual(51, len(replaced))
        self.assertEqual(51, expected.used)
        self.assertEqual(51, expected.sources.count("divergence"))
        self.assertIn("Asia/Calcutta => Asia/Kolkata | GMT+05:53:28 GMT+05:21:10 GMT+05:30 GMT+05:30 GMT+05:30 GMT+05:30 GMT+05:30 GMT+05:30",
                      expected.lines)

    def test_temporal_expects_1629_lines_and_has_no_divergence(self) -> None:
        program = self.programs["temporal"]
        retained = H.split_retained(read_copy(program.answer))
        self.assertEqual(1629, len(retained))
        self.assertGreater(len(retained), H.VM_LEAST["temporal"])
        self.assertEqual({}, H.read_divergences(read_copy(program.divergences)))

    def test_broiler_vm_s_own_answer_equals_the_expected_answer_on_every_line(self) -> None:
        for program in self.pin.programs:
            answer = vm_answer(program)
            _, results = H.compare_program(read_copy(program.answer), read_copy(program.divergences), answer)
            self.assertTrue(all(r.equal for r in results), program.name)

    def test_negative_control_one_expected_line_changed_in_memory_is_a_difference(self) -> None:
        for program in self.pin.programs:
            answer = vm_answer(program)
            expected, results = H.compare_program(read_copy(program.answer), read_copy(program.divergences), answer)
            control = H.negative_control(expected, answer, results)
            self.assertEqual(1, control["line"], program.name)
            self.assertTrue(control["wasEqual"])
            self.assertTrue(control["shownAsDifference"])
            self.assertTrue(control["otherLinesUnchanged"])
            self.assertNotEqual(expected.lines[0], control["expectedChangedTo"])
            self.assertEqual(len(expected.lines[0]), len(control["expectedChangedTo"]))
            # And the same change made directly: exactly one line differs.
            changed = list(answer)
            changed[700 % len(changed)] += "!"
            _, results = H.compare_program(read_copy(program.answer), read_copy(program.divergences), changed)
            self.assertEqual([700 % len(changed) + 1], [r.number for r in results if not r.equal])

    def test_the_control_takes_the_first_equal_line(self) -> None:
        expected, results = H.compare_program("A => 1\nB => 2\nC => 3\n", "", ["A => 0", "B => 2", "C => 3"])
        control = H.negative_control(expected, ["A => 0", "B => 2", "C => 3"], results)
        self.assertEqual((2, "B => X", True, True), (control["line"], control["expectedChangedTo"],
                                                     control["shownAsDifference"], control["otherLinesUnchanged"]))


class TransportTests(unittest.TestCase):
    def test_every_utf16_code_unit_survives_the_driver_s_escaping(self) -> None:
        units = list(range(0x10000))
        text = H.units_to_text(units)
        self.assertEqual(text, H.decode_escaped(driver_escape(text)))
        self.assertEqual("\U0001F600 \ud800 \udc00x", H.decode_escaped("\\ud83d\\ude00 \\ud800 \\udc00x"))
        self.assertEqual("a\\b\nc", H.decode_escaped("a\\u005cb\\u000ac"))

    def test_a_malformed_or_unescaped_character_is_refused(self) -> None:
        for bad in ("\\x41", "\\u12", "\\u00E9", "\\", "caf\u00e9", "a\tb", "a\\nb"):
            with self.assertRaises(H.HarnessError, msg=bad):
                H.decode_escaped(bad)

    def test_utf16_length(self) -> None:
        self.assertEqual(4, H.utf16_length("a\U0001F600b"))
        self.assertEqual(1, H.utf16_length("\ud800"))

    def test_the_driver_s_lines_are_read_and_the_rest_is_kept(self) -> None:
        stdout = "warming up\r\n" + driver_stdout("p.js", "abc", "x\ny\u2212\n").replace("\n", "\r\n")
        outcome = H.parse_driver_output(stdout, "p.js", "abc", 0)
        self.assertTrue(outcome.completed)
        self.assertEqual("x\ny\u2212\n", outcome.completion)
        self.assertEqual("string", outcome.completion_type)
        self.assertEqual(["warming up"], outcome.extra_lines)

    def test_an_uncaught_error_is_not_a_completion(self) -> None:
        stdout = "reference-dataset-driver 1 p.js 3\nreference-dataset-uncaught RangeError: bad \\u2212\n"
        outcome = H.parse_driver_output(stdout, "p.js", "abc", 0)
        self.assertFalse(outcome.completed)
        self.assertEqual("RangeError: bad \u2212", outcome.uncaught)
        self.assertIsNone(outcome.completion)

    def test_each_inconsistency_is_a_problem(self) -> None:
        good = driver_stdout("p.js", "abc", "x")
        cases = {
            "no driver line": (good.split("\n", 1)[1], 0),
            "a program of another length": (driver_stdout("p.js", "abcd", "x"), 0),
            "another file": (driver_stdout("q.js", "abc", "x"), 0),
            "a wrong value length": (good.replace("completion string 1 ", "completion string 2 "), 0),
            "a non-zero exit": (good, 1),
            "two results": (good + "reference-dataset-uncaught E: m\n", 0),
            "no result": (good.split("\n", 1)[0] + "\n", 0),
        }
        for name, (stdout, code) in cases.items():
            outcome = H.parse_driver_output(stdout, "p.js", "abc", code)
            self.assertFalse(outcome.completed, name)
            self.assertTrue(outcome.problems, name)

    def test_the_driver_template_holds_its_marker_once_and_is_filled_with_a_json_string(self) -> None:
        template = H.DRIVER_TEMPLATE.read_text(encoding="utf-8")
        self.assertEqual(1, template.count(H.DRIVER_MARKER))
        self.assertIn("(0, eval)(text)", template)
        with tempfile.TemporaryDirectory() as scratch:
            path = Path(scratch) / "driver.js"
            H.write_driver(path, "timezones.js")
            written = path.read_text(encoding="utf-8")
            self.assertIn('var file = "timezones.js";', written)
            self.assertNotIn(H.DRIVER_MARKER, written)

    def test_the_child_has_tz_utc_and_no_locale_variable(self) -> None:
        environment = H.child_environment({"PATH": "/bin", "LANG": "de_DE.UTF-8", "LANGUAGE": "de", "LC_ALL": "C",
                                           "LC_CTYPE": "C.UTF-8", "TZ": "America/New_York", "DOTNET_NOLOGO": "1"})
        self.assertEqual({"PATH": "/bin", "TZ": "UTC", "DOTNET_NOLOGO": "1"}, environment)


class ReportTests(unittest.TestCase):
    def test_a_field_reads_back_exactly(self) -> None:
        for text in ("plain", "tab\there", "cr\rlf\n", "back\\slash", "literal \\u0041 and \\t", "lone \ud800 \udfff",
                     "\U0001F600 \u2212", ""):
            escaped = H.tsv_escape(text)
            self.assertNotIn("\t", escaped)
            self.assertNotIn("\n", escaped)
            escaped.encode("utf-8")
            self.assertEqual(text, H.tsv_unescape(escaped))
        with self.assertRaises(H.HarnessError):
            H.tsv_unescape("bad \\x")

    def test_a_report_reads_back_exactly(self) -> None:
        rows = [["1", "yes", "retained", "k", "a\tb", "a\tb"], ["2", "no", "divergence", "k2", "x", "\ud800"]]
        with tempfile.TemporaryDirectory() as scratch:
            path = Path(scratch) / "r.tsv"
            H.write_tsv(path, H.TSV_COLUMNS, rows)
            read = H.read_tsv(path)
        self.assertEqual(rows, [[row[c] for c in H.TSV_COLUMNS] for row in read])


def lines_rows(results: list[tuple[str, str]]) -> list[dict[str, str]]:
    """Report rows from (expected, actual) pairs."""
    return [{"line": str(i + 1), "equal": H.yes_no(e == a), "source": "retained", "key": H.line_key(e),
             "expected": e, "actual": a} for i, (e, a) in enumerate(results)]


class CompareTests(unittest.TestCase):
    def test_each_line_after_against_its_expected_answer_and_the_base(self) -> None:
        base = lines_rows([("A => 1", "A => 1"), ("B => 2", "B => x"), ("C => 3", "C => x"), ("D => 4", "D => x"),
                           ("E => 5", "E => 5")])
        after = lines_rows([("A => 1", "A => 1"), ("B => 2", "B => x"), ("C => 3", "C => y"), ("D => 4", "D => 4"),
                            ("E => 5", "E => y"), (H.NO_LINE, "F")])
        rows, counts = H.compare_rows(base, after)
        self.assertEqual(
            [["1", "yes", "yes", "yes"], ["2", "no", "yes", "no"], ["3", "no", "no", "no"], ["4", "yes", "no", "no"],
             ["5", "no", "no", "yes"], ["6", "no", "no", "no"]],
            [row[:4] for row in rows])
        self.assertEqual(H.NO_LINE, rows[5][6])
        self.assertEqual({"lines": 6, "afterEqual": 2, "afterDiffering": 4, "differingSameAsBase": 1,
                          "differingMovedSinceBase": 3, "baseDifferingAfterEqual": 1, "baseEqualAfterDiffering": 1,
                          "expectedDiffers": 0}, counts)

    def test_two_runs_of_different_copies_are_refused_and_alike_ones_join(self) -> None:
        with tempfile.TemporaryDirectory() as scratch:
            root = Path(scratch)
            for label, copies, actual in (("base", {"a": "1"}, "A => x"), ("after", {"a": "1"}, "A => 1")):
                run = root / label
                run.mkdir()
                summary = {"label": label, "shellCommit": label, "shellSha256": label, "datasetsSourceCommit": "c",
                           "copies": copies, "programs": {"p": {"completed": True}}}
                (run / "summary.json").write_text(json.dumps(summary), encoding="utf-8")
                H.write_tsv(run / "p.lines.tsv", H.TSV_COLUMNS,
                            [[r[c] for c in H.TSV_COLUMNS] for r in lines_rows([("A => 1", actual)])])
            with mock.patch("sys.stdout"):
                self.assertEqual(0, H.main(["compare", "--base", str(root / "base"), "--after", str(root / "after"),
                                            "--out", str(root / "out")]))
            result = json.loads((root / "out" / "compare.json").read_text(encoding="utf-8"))
            self.assertEqual(1, result["programs"]["p"]["baseDifferingAfterEqual"])
            self.assertEqual(1, len(H.read_tsv(root / "out" / "p.compare.tsv")))

            summary = json.loads((root / "after" / "summary.json").read_text(encoding="utf-8"))
            summary["copies"] = {"a": "2"}
            (root / "after" / "summary.json").write_text(json.dumps(summary), encoding="utf-8")
            with mock.patch("sys.stderr"):
                self.assertEqual(2, H.main(["compare", "--base", str(root / "base"), "--after", str(root / "after"),
                                            "--out", str(root / "out2")]))


class RunTests(unittest.TestCase):
    """The run command end to end, with a stand-in for the shell that answers as Broiler.VM does, but for
    one changed line of temporal.js."""

    def test_a_run_writes_every_line_and_its_counts(self) -> None:
        pin = H.load_datasets(H.DATASETS)
        answers = {p.program: "\n".join(vm_answer(p)) for p in pin.programs}
        answers["temporal.js"] = answers["temporal.js"].replace(" => 1/false", " => 1/true", 1)
        calls = []

        def fake_run(command, **kwargs):
            if command[0] == "dotnet" and "--script-host" in command:
                driver = Path(command[-1]).read_text(encoding="utf-8")
                program = json.loads(driver.split("var file = ", 1)[1].split(";", 1)[0])
                text = H.read_all_text((Path(kwargs["cwd"]) / program).read_bytes())
                calls.append((program, kwargs["env"].get("TZ"), "LANG" in kwargs["env"]))
                stdout = driver_stdout(program, text, answers[program]).encode()
                return subprocess.CompletedProcess(command, 0, stdout, b"")
            return subprocess.CompletedProcess(command, 1, "" if kwargs.get("text") else b"",
                                               "" if kwargs.get("text") else b"")

        with tempfile.TemporaryDirectory() as scratch:
            root = Path(scratch)
            shell = root / "bin" / "BroilerJS.dll"
            shell.parent.mkdir()
            shell.write_bytes(b"not a shell")
            with mock.patch.object(H.subprocess, "run", fake_run), mock.patch("sys.stdout"):
                code = H.main(["run", "--tree", str(root), "--shell", str(shell), "--out", str(root / "out"),
                               "--label", "test"])
            self.assertEqual(0, code)
            self.assertEqual([("timezones.js", "UTC", False), ("temporal.js", "UTC", False),
                              ("temporal-calendars.js", "UTC", False)], calls)
            summary = json.loads((root / "out" / "summary.json").read_text(encoding="utf-8"))
            timezones, temporal = summary["programs"]["timezones"], summary["programs"]["temporal"]
            self.assertEqual((610, 610, 0, 51), (timezones["comparedLines"], timezones["equal"],
                                                 timezones["differing"], timezones["divergencesUsed"]))
            self.assertEqual((1629, 1628, 1), (temporal["comparedLines"], temporal["equal"], temporal["differing"]))
            self.assertTrue(temporal["negativeControl"]["shownAsDifference"])
            self.assertEqual(2, temporal["negativeControl"]["line"])
            rows = H.read_tsv(root / "out" / "temporal.lines.tsv")
            self.assertEqual(1629, len(rows))
            self.assertEqual(("no", "duration P1Y2M3W4DT5H6M7.008009010S sign/blank => 1/true"),
                             (rows[0]["equal"], rows[0]["actual"]))
            self.assertEqual(answers["timezones.js"] + "\n",
                             (root / "out" / "timezones.answer.txt").read_text(encoding="utf-8"))
            identity = (root / "out" / "identity.txt").read_text(encoding="utf-8")
            self.assertIn("child environment: TZ=UTC; LANG, LANGUAGE and every LC_ variable removed", identity)
            self.assertIn("shell commit: unknown", identity)


if __name__ == "__main__":
    unittest.main()
