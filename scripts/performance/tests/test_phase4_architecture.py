from __future__ import annotations

from pathlib import Path
import unittest


REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "Broiler.JS"


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


class Phase4ArchitectureTests(unittest.TestCase):
    def test_generator_emits_immutable_feature_descriptors(self) -> None:
        generator = text(
            SOURCE_ROOT / "Broiler.JavaScript.JSClassGenerator" / "RegistrationGenerator.cs"
        )
        models = text(
            SOURCE_ROOT / "Broiler.JavaScript.JSClassGenerator" / "GeneratorModels.cs"
        )
        self.assertIn("GeneratedRegistrationDescriptors", generator)
        self.assertIn("BuiltInRegistrationDescriptor", generator)
        self.assertIn("BuiltInFeatures features = BuiltInFeatures.All", generator)
        self.assertIn("(features & BuiltInFeatures.", generator)
        self.assertIn("FeatureName", models)

    def test_lazy_cell_has_a_core_owned_boundary_and_property_storage_hooks(self) -> None:
        cell = text(SOURCE_ROOT / "Broiler.JavaScript.Runtime" / "LazyDataPropertyCell.cs")
        storage = text(
            SOURCE_ROOT / "Broiler.JavaScript.Runtime" / "JSObject.PropertyStorage.cs"
        )
        self.assertIn("IJSFeatureResolver resolver", cell)
        self.assertIn("BuiltInFeatureId feature", cell)
        self.assertIn("Recursive lazy initialization", cell)
        self.assertIn("Cancel()", cell)
        for forbidden in ("Func<", "System.Type", "Assembly", "IBuiltInFeatureSatellite"):
            self.assertNotIn(forbidden, cell)
        self.assertIn("FastAddLazyDataProperty", storage)
        self.assertIn("LazyDataPropertyCell", storage)
        self.assertIn("ResolvePropertyValue", storage)

    def test_profiles_keep_full_lazy_and_name_minimal_as_nonconformant(self) -> None:
        profiles = text(
            SOURCE_ROOT / "Broiler.JavaScript.Engine" / "JavaScriptBootstrapProfile.cs"
        )
        bootstrap = text(
            SOURCE_ROOT / "Broiler.JavaScript.Engine" / "JavaScriptBootstrap.cs"
        )
        context = text(SOURCE_ROOT / "Broiler.JavaScript.Engine" / "JSContext.cs")
        factories = text(
            SOURCE_ROOT / "Broiler.JavaScript.Engine" / "Core" / "JSValueCoreExtensions.cs"
        )
        self.assertIn("BuiltInFeatures.Intl | BuiltInFeatures.Temporal", profiles)
        self.assertIn('"minimal"', profiles)
        self.assertIn("isConformant: false", profiles)
        self.assertIn("CreateContextBuilder", bootstrap)
        self.assertIn("UseBuiltInRegistry", bootstrap)
        self.assertIn("Options.BuiltInRegistry == null", context)
        self.assertNotIn("EnsureBuiltInsAssemblyLoaded", factories)

    def test_satellite_and_minimal_package_have_explicit_dependencies(self) -> None:
        satellite = text(
            SOURCE_ROOT / "Broiler.JavaScript.Feature.Sample" / "SampleFeatureSatellite.cs"
        )
        satellite_project = text(
            SOURCE_ROOT / "Broiler.JavaScript.Feature.Sample" / "Broiler.JavaScript.Feature.Sample.csproj"
        )
        minimal_project = text(
            SOURCE_ROOT / "Broiler.JavaScript.Minimal" / "Broiler.JavaScript.Minimal.csproj"
        )
        host_project = text(
            SOURCE_ROOT / "samples" / "Broiler.JavaScript.StartupHost" / "Broiler.JavaScript.StartupHost.csproj"
        )
        deferred_host_feature = text(
            SOURCE_ROOT / "samples" / "Broiler.JavaScript.StartupHost" / "DeferredSampleFeatureRegistration.cs"
        )
        self.assertIn("IBuiltInFeatureSatellite", satellite)
        self.assertNotIn("ModuleInitializer", satellite)
        self.assertIn("Broiler.JavaScript.Runtime.csproj", satellite_project)
        self.assertNotIn("Broiler.JavaScript.Engine.csproj", satellite_project)
        for excluded in ("Broiler.JavaScript.Clr", "Broiler.JavaScript.Debugger", "Broiler.JavaScript.Modules"):
            self.assertNotIn(excluded, minimal_project)
        self.assertIn("BroilerHostProfile", host_project)
        self.assertIn("BROILER_FULL_HOST", host_project)
        self.assertIn("SampleSatelliteFactory.Create", deferred_host_feature)
        self.assertIn("MethodImplOptions.NoInlining", deferred_host_feature)

    def test_measurement_collector_covers_selective_r2r_and_trimmed_hosts(self) -> None:
        collector = text(REPO_ROOT / "scripts" / "performance" / "collect_phase4.py")
        benchmark = text(
            SOURCE_ROOT
            / "benchmarks"
            / "Broiler.JavaScript.Engine.Benchmarks"
            / "Phase4StartupBenchmarks.cs"
        )
        for variant in (
            "minimal-framework",
            "full-framework",
            "full-readytorun",
            "full-trimmed",
        ):
            self.assertIn(variant, collector)
        self.assertIn("PublishReadyToRun", collector)
        self.assertIn("PublishTrimmed", collector)
        self.assertIn("FullEager", benchmark)
        self.assertIn("Minimal", benchmark)


if __name__ == "__main__":
    unittest.main()
