from __future__ import annotations

from pathlib import Path
import unittest


REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "Broiler.JS"


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


class Phase3ArchitectureTests(unittest.TestCase):
    def test_scalar_replacement_has_conservative_observability_guards(self) -> None:
        create_function = text(
            SOURCE_ROOT / "Broiler.JavaScript.Compiler" / "Declarations" / "FastCompiler.CreateFunction.cs"
        )
        scope = text(SOURCE_ROOT / "Broiler.JavaScript.Compiler" / "Scope" / "FastFunctionScope.cs")
        for guard in (
            "ParametersContainDirectEval",
            "BodyContainsDirectEval",
            "VisitFunctionExpression",
            "VisitWithStatement",
            "VisitDebuggerStatement",
        ):
            self.assertIn(guard, create_function)
        self.assertIn("VisitVariableDeclarator", create_function)
        self.assertIn("VisitObjectProperty", create_function)
        self.assertIn("VisitCase", create_function)
        self.assertIn("variableType == typeof(JSValue)", scope)
        self.assertIn("RecordScalarLocal", scope)

    def test_switch_tables_are_real_and_bounded(self) -> None:
        switch = text(
            SOURCE_ROOT
            / "Broiler.JavaScript.ExpressionCompiler"
            / "Generator"
            / "ILCodeGenerator.VisitSwitch.cs"
        )
        self.assertGreaterEqual(switch.count("OpCodes.Switch"), 2)
        self.assertIn("range64 > 256", switch)
        self.assertIn("range64 > firstCases.Count * 2L", switch)
        self.assertIn("tests.Count > 256", switch)
        self.assertIn("StringEqualsMethod", switch)

    def test_shapes_slots_and_pic_have_dictionary_and_megamorphic_fallbacks(self) -> None:
        shape = text(SOURCE_ROOT / "Broiler.JavaScript.Runtime" / "ObjectShape.cs")
        obj = text(SOURCE_ROOT / "Broiler.JavaScript.Runtime" / "JSObject.cs")
        self.assertIn("ConcurrentDictionary<uint, ObjectShape> transitions", shape)
        self.assertIn("private const int MaxEntries = 4", shape)
        self.assertIn("BecomeMegamorphic", shape)
        self.assertIn("var result = target[property]", shape)
        self.assertIn("ObjectShape.Dictionary", obj)
        self.assertIn("shapeSlots", obj)

    def test_prototype_mutations_are_versioned(self) -> None:
        obj = text(SOURCE_ROOT / "Broiler.JavaScript.Runtime" / "JSObject.cs")
        self.assertIn("prototypeMutationVersion", obj)
        self.assertIn("NotifyNamedPropertyMutation", obj)
        self.assertIn("NotifyPrototypeChainMutation", obj)
        self.assertIn("RecordPrototypeInvalidation", obj)

    def test_persistent_cache_has_atomic_manifest_integrity_pdb_and_collectible_load(self) -> None:
        cache = text(SOURCE_ROOT / "Broiler.JavaScript" / "AssemblyCodeCache.cs")
        self.assertIn("ManifestSchema = 3", cache)
        self.assertIn("IncrementalHash.CreateHash", cache)
        self.assertIn("WriteAtomically", cache)
        self.assertIn("PortablePdbBuilder", cache)
        self.assertIn("AssemblyLoadContext(isCollectible: true)", cache)
        self.assertIn("CreateDelegate<Func<JSFunctionDelegate>>", cache)
        self.assertIn("Quarantine", cache)
        self.assertNotIn("AppDomain.CurrentDomain.Load", cache)
        self.assertNotIn(".Invoke(null, null)", cache)


if __name__ == "__main__":
    unittest.main()
