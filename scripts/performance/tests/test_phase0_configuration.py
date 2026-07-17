from __future__ import annotations

import json
from pathlib import Path
import unittest


REPO_ROOT = Path(__file__).resolve().parents[3]
PERFORMANCE_ROOT = REPO_ROOT / "eng" / "performance"


class Phase0ConfigurationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.config = json.loads((PERFORMANCE_ROOT / "phase0.json").read_text(encoding="utf-8"))
        self.ownership = json.loads((PERFORMANCE_ROOT / "ownership.json").read_text(encoding="utf-8"))

    def test_configuration_and_schemas_are_valid_json_with_matching_versions(self) -> None:
        self.assertEqual("1.0.0", self.config["schemaVersion"])
        self.assertEqual("1.0.0", self.ownership["schemaVersion"])
        for schema_name in (
            "phase0-config.schema.json",
            "phase0-result.schema.json",
            "performance-ownership.schema.json",
        ):
            schema = json.loads((PERFORMANCE_ROOT / "schemas" / schema_name).read_text(encoding="utf-8"))
            self.assertEqual("https://json-schema.org/draft/2020-12/schema", schema["$schema"])

    def test_repeatable_profiles_have_two_runs_and_documented_noise_bands(self) -> None:
        profiles = self.config["benchmark"]["profiles"]
        for profile_name in ("smoke", "baseline"):
            with self.subTest(profile=profile_name):
                profile = profiles[profile_name]
                self.assertGreaterEqual(profile["repetitions"], 2)
                self.assertGreater(profile["lifecycleSamplesPerRepetition"], 0)
                self.assertGreater(profile["noiseBandPercent"], 0)
                self.assertTrue(profile["filters"])

    def test_eventpipe_covers_every_phase0_hot_area(self) -> None:
        self.assertEqual(
            {"context", "functions", "properties", "arrays", "parsing", "mapset"},
            set(self.config["eventPipe"]["scenarios"]),
        )

    def test_every_roadmap_priority_item_has_unique_ownership_and_existing_manifest(self) -> None:
        items = self.ownership["items"]
        self.assertEqual(21, len(items))
        self.assertEqual(len(items), len({item["id"] for item in items}))
        self.assertEqual({"P0", "P1", "P2", "P3"}, {item["priority"] for item in items})

        benchmark_sources = "\n".join(
            path.read_text(encoding="utf-8")
            for path in (REPO_ROOT / "Broiler.JS" / "benchmarks" / "Broiler.JavaScript.Engine.Benchmarks").glob("*.cs")
        )
        for item in items:
            with self.subTest(item=item["id"]):
                manifest = REPO_ROOT / item["manifest"]
                self.assertTrue(manifest.is_file(), item["manifest"])
                if not item["benchmark"].startswith("phase0-"):
                    self.assertIn(f"class {item['benchmark']}", benchmark_sources)

    def test_focused_manifests_contain_only_test262_paths_and_comments(self) -> None:
        manifests = {item["manifest"] for item in self.ownership["items"]}
        for relative_path in manifests:
            with self.subTest(manifest=relative_path):
                lines = (REPO_ROOT / relative_path).read_text(encoding="utf-8").splitlines()
                entries = [line.strip() for line in lines if line.strip() and not line.lstrip().startswith("#")]
                self.assertTrue(entries)
                self.assertTrue(all(entry.startswith("test/") for entry in entries))


if __name__ == "__main__":
    unittest.main()
