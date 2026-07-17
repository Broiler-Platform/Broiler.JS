from __future__ import annotations

from pathlib import Path
import unittest


REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "Broiler.JS"


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


class Phase1ArchitectureTests(unittest.TestCase):
    def test_runtime_host_mode_has_no_environment_lookup(self) -> None:
        runtime_projects = [
            SOURCE_ROOT / "Broiler.JavaScript.Runtime",
            SOURCE_ROOT / "Broiler.JavaScript.Engine",
            SOURCE_ROOT / "Broiler.JavaScript.BuiltIns",
            SOURCE_ROOT / "Broiler.JavaScript.ExpressionCompiler",
        ]
        sources = "\n".join(text(path) for root in runtime_projects for path in root.rglob("*.cs"))
        self.assertNotIn('GetEnvironmentVariable("BROILER_SCRIPT_HOST")', sources)

    def test_binary_scalar_paths_do_not_create_bitconverter_arrays(self) -> None:
        typed = SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Array" / "Typed"
        data_view = SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "DataView" / "DataView.cs"
        sources = "\n".join(text(path) for path in typed.glob("*.cs")) + text(data_view)
        self.assertNotIn("BitConverter.GetBytes", sources)
        self.assertIn("BinaryPrimitives", text(data_view))

    def test_runtime_package_dependencies_are_public_and_all_is_a_meta_package(self) -> None:
        built_ins = text(SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Broiler.JavaScript.BuiltIns.csproj")
        aggregate = text(SOURCE_ROOT / "Broiler.JavaScript.All" / "Broiler.JavaScript.All.csproj")
        self.assertNotIn("<PrivateAssets>all</PrivateAssets>", built_ins)
        self.assertNotIn("<PrivateAssets>compile</PrivateAssets>", built_ins)
        self.assertIn("<IncludeBuildOutput>false</IncludeBuildOutput>", aggregate)
        self.assertIn("<SuppressDependenciesWhenPacking>false</SuppressDependenciesWhenPacking>", aggregate)
        self.assertIn("NU5128", aggregate)
        self.assertIn(
            "<IsPackable>true</IsPackable>",
            text(SOURCE_ROOT / "Broiler.JavaScript.ModuleExtensions" / "Broiler.JavaScript.ModuleExtensions.csproj"),
        )
        self.assertIn(
            "<IsPackable>true</IsPackable>",
            text(REPO_ROOT / "Broiler.Unicode" / "src" / "UnicodeProperties" / "UnicodeProperties.csproj"),
        )

    def test_generator_is_incremental_and_persisted_output_is_opt_in(self) -> None:
        generator_project = text(
            SOURCE_ROOT / "Broiler.JavaScript.JSClassGenerator" / "Broiler.JavaScript.JSClassGenerator.csproj"
        )
        generator = text(SOURCE_ROOT / "Broiler.JavaScript.JSClassGenerator" / "JSClassGenerator.cs")
        build_props = text(REPO_ROOT / "Directory.Build.props")
        build_targets = text(REPO_ROOT / "Directory.Build.targets")
        self.assertNotIn("Workspaces", generator_project)
        self.assertGreaterEqual(generator.count("ForAttributeWithMetadataName"), 3)
        self.assertIn("RegistrationTypeModel", generator)
        self.assertIn("Generated\\**", build_props)
        self.assertIn("'$(BroilerPersistGeneratedFiles)' == 'true'", build_targets)
        self.assertIn(r"$(MSBuildProjectDirectory)\obj\generated\$(Configuration)\$(TargetFramework)", build_targets)

    def test_experimental_exports_are_gated_during_registration(self) -> None:
        class_generator = text(SOURCE_ROOT / "Broiler.JavaScript.JSClassGenerator" / "ClassGenerator.cs")
        registry = text(SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "DefaultBuiltInRegistry.cs")
        self.assertIn("JSValue.IsFeatureEnabled", class_generator)
        self.assertNotIn("ApplyExperimentalFeatureFlags", registry)


if __name__ == "__main__":
    unittest.main()
