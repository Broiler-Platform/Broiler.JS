from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import list_test262_assemblies as helper


SAMPLE_MANIFEST = {
    "assemblies": {
        "parser": {
            "project": "Broiler.JS/Broiler.JavaScript.Parser",
            "paths": ["test/language/literals", "test/language/keywords"],
        },
        "runtime": {
            "project": "Broiler.JS/Broiler.JavaScript.Runtime",
            "paths": ["test/built-ins/Promise"],
        },
    }
}


class ListTest262AssembliesTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tempdir = tempfile.TemporaryDirectory()
        self.addCleanup(self._tempdir.cleanup)
        self.root = Path(self._tempdir.name)
        self.manifest_path = self.root / "test262-assemblies.json"
        self.manifest_path.write_text(json.dumps(SAMPLE_MANIFEST), encoding="utf-8")

    def _load(self) -> dict:
        return helper._load_manifest(self.manifest_path)

    def test_selection_all_returns_sorted_unique_assembly_names(self) -> None:
        assemblies = self._load()
        self.assertEqual(helper._resolve_selection("all", assemblies), ["parser", "runtime"])

    def test_selection_single_returns_single_entry(self) -> None:
        assemblies = self._load()
        self.assertEqual(helper._resolve_selection("parser", assemblies), ["parser"])

    def test_selection_unknown_raises(self) -> None:
        assemblies = self._load()
        with self.assertRaises(SystemExit):
            helper._resolve_selection("bogus", assemblies)

    def test_paths_for_known_assembly(self) -> None:
        assemblies = self._load()
        self.assertEqual(
            helper._resolve_paths("parser", assemblies),
            ["test/language/literals", "test/language/keywords"],
        )

    def test_paths_for_unknown_assembly_raises(self) -> None:
        assemblies = self._load()
        with self.assertRaises(SystemExit):
            helper._resolve_paths("missing", assemblies)

    def test_empty_manifest_rejected(self) -> None:
        empty_path = self.root / "empty.json"
        empty_path.write_text("{}", encoding="utf-8")
        with self.assertRaises(SystemExit):
            helper._load_manifest(empty_path)

    def test_repository_manifest_is_well_formed(self) -> None:
        repo_manifest = (
            Path(__file__).resolve().parents[1] / "test262-assemblies.json"
        )
        assemblies = helper._load_manifest(repo_manifest)
        # Every Broiler.JS assembly entry must expose a non-empty path list.
        for name, entry in assemblies.items():
            with self.subTest(assembly=name):
                self.assertIsInstance(entry, dict)
                paths = entry.get("paths")
                self.assertIsInstance(paths, list)
                self.assertGreater(len(paths), 0)
                for path in paths:
                    self.assertTrue(
                        path.startswith("test/"),
                        msg=f"{name}: path {path!r} should be relative to the test262 root",
                    )
        # The orchestrating workflow expects parser/compiler/runtime/builtins
        # to always exist; verify the contract here so renames are caught.
        for required in ("parser", "compiler", "runtime", "builtins"):
            self.assertIn(required, assemblies)


if __name__ == "__main__":  # pragma: no cover
    unittest.main()
