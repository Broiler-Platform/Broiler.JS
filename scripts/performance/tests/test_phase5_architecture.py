from __future__ import annotations

from pathlib import Path
import unittest


REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "Broiler.JS"


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


class Phase5ArchitectureTests(unittest.TestCase):
    def test_tiering_is_opt_in_and_bounded_per_realm(self) -> None:
        tiering = text(SOURCE_ROOT / "Broiler.JavaScript.Runtime" / "FunctionTiering.cs")
        context = text(SOURCE_ROOT / "Broiler.JavaScript.Engine" / "JSContextOptions.cs")
        function = text(SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Function" / "JSFunction.cs")
        self.assertIn("public bool Enabled { get; init; }", tiering)
        self.assertIn("MaxRecompilations", tiering)
        self.assertIn("MaxRetainedCodeBytes", tiering)
        self.assertIn("TryReserve", tiering)
        self.assertIn("FunctionTieringOptions.Disabled", context)
        self.assertIn("Volatile.Write(ref f, replacement)", function)

    def test_numeric_pilot_guards_and_deoptimizes_to_baseline(self) -> None:
        tiering = text(SOURCE_ROOT / "Broiler.JavaScript.Runtime" / "FunctionTiering.cs")
        planner = text(SOURCE_ROOT / "Broiler.JavaScript.Compiler" / "NumericLoopPlanner.cs")
        compiler = text(
            SOURCE_ROOT
            / "Broiler.JavaScript.Compiler"
            / "Declarations"
            / "FastCompiler.CreateFunction.cs"
        )
        self.assertIn("if (!limitValue.IsNumber)", tiering)
        self.assertIn("return baseline(in arguments)", tiering)
        self.assertIn("var accumulator = accumulatorInitialValue", tiering)
        self.assertIn("TryCreate", planner)
        self.assertIn("HasOuterFunctionCaptures", compiler)
        self.assertIn("EnableTiering", compiler)

    def test_tagged_value_is_an_internal_scalar_only_prototype(self) -> None:
        tagged = text(SOURCE_ROOT / "Broiler.JavaScript.Runtime" / "TaggedValuePrototype.cs")
        jsvalue = text(SOURCE_ROOT / "Broiler.JavaScript.Runtime" / "JSValue.cs")
        self.assertIn("internal readonly struct TaggedValuePrototype", tagged)
        self.assertIn("private readonly ulong bits", tagged)
        self.assertIn("object references, strings, symbols, and BigInts", tagged)
        self.assertNotIn("TaggedValuePrototype", jsvalue)

    def test_portable_path_has_an_explicit_dynamic_code_free_boundary(self) -> None:
        interpreter = text(SOURCE_ROOT / "Broiler.JavaScript.Portable" / "PortableInterpreter.cs")
        sample = text(
            SOURCE_ROOT
            / "samples"
            / "Broiler.JavaScript.NativeAotSample"
            / "Broiler.JavaScript.NativeAotSample.csproj"
        )
        sample_program = text(
            SOURCE_ROOT / "samples" / "Broiler.JavaScript.NativeAotSample" / "Program.cs"
        )
        self.assertIn("no JS object model", interpreter)
        self.assertIn("<PublishAot>true</PublishAot>", sample)
        self.assertIn("Broiler.JavaScript.Portable.csproj", sample)
        self.assertNotIn("Broiler.JavaScript.Engine.csproj", sample)
        self.assertIn("RuntimeFeature.IsDynamicCodeSupported", sample_program)
        self.assertIn("!dynamicCodeSupported", sample_program)

    def test_phase5_evidence_covers_benchmarks_deopt_and_aot(self) -> None:
        collector = text(REPO_ROOT / "scripts" / "performance" / "collect_phase5.py")
        benchmark = text(
            SOURCE_ROOT
            / "benchmarks"
            / "Broiler.JavaScript.Engine.Benchmarks"
            / "Phase5AdvancedExecutionBenchmarks.cs"
        )
        tests = text(
            SOURCE_ROOT
            / "Broiler.JavaScript.Compiler.Tests"
            / "Phase5AdvancedExecutionTests.cs"
        )
        self.assertIn("matchesCheckedInProgram", collector)
        self.assertIn("dynamicCodeSupported", collector)
        self.assertIn("Phase5TieringBenchmarks", benchmark)
        self.assertIn("MixedInput", benchmark)
        self.assertIn("Deoptimizations", tests)
        self.assertIn("BudgetRejections", tests)


if __name__ == "__main__":
    unittest.main()
