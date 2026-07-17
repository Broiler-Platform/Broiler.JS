from __future__ import annotations

from pathlib import Path
import unittest


REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "Broiler.JS"


def text(path: Path) -> str:
    return path.read_text(encoding="utf-8")


class Phase2ArchitectureTests(unittest.TestCase):
    def test_map_and_set_use_typed_same_value_zero_index(self) -> None:
        map_source = text(SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Map" / "JSMap.cs")
        set_source = text(SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Set" / "JSSet.cs")
        comparer = text(
            SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Collections" / "SameValueZeroComparer.cs"
        )
        self.assertNotIn("ToUniqueID", map_source + set_source)
        self.assertNotIn("StringMap<", map_source + set_source)
        self.assertIn("SameValueZeroComparer.Instance", map_source)
        self.assertIn("SameValueZeroComparer.Instance", set_source)
        self.assertIn("RuntimeHelpers.GetHashCode", comparer)

    def test_weak_collections_are_direct_ephemeron_tables(self) -> None:
        weak_map = text(SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Map" / "JSWeakMap.cs")
        weak_set = text(SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Set" / "JSWeakSet.cs")
        sources = weak_map + weak_set
        self.assertIn("ConditionalWeakTable<JSValue, WeakMapValueBox>", weak_map)
        self.assertIn("ConditionalWeakTable<JSValue, object>", weak_set)
        self.assertNotIn("WeakReference", sources)
        self.assertNotIn("~WeakValue", sources)
        self.assertNotIn("lock (this)", sources)

    def test_key_metadata_reads_are_lock_free(self) -> None:
        key_strings = text(SOURCE_ROOT / "Broiler.JavaScript.Storage" / "KeyStrings.cs")
        metadata = text(SOURCE_ROOT / "Broiler.JavaScript.Storage" / "KeyMetadata.cs")
        self.assertIn("ConcurrentDictionary<string, KeyString>", key_strings)
        self.assertIn("Volatile.Read(ref entries)", key_strings)
        self.assertNotIn("ReaderWriterLockSlim", key_strings)
        self.assertIn("IsArrayIndex", metadata)
        self.assertIn("IsCanonicalNumericIndex", metadata)
        self.assertIn("StableOrdinalHash", metadata)

    def test_property_order_deletion_is_constant_time(self) -> None:
        sequence = text(SOURCE_ROOT / "Broiler.JavaScript.Storage" / "PropertySequence.cs")
        self.assertIn("public uint Previous;", sequence)
        remove_body = sequence[sequence.index("public bool RemoveAt"):sequence.index("public ref JSProperty GetValue")]
        self.assertNotIn("while (", remove_body)

    def test_element_kinds_and_guarded_bulk_paths_are_present(self) -> None:
        elements = text(SOURCE_ROOT / "Broiler.JavaScript.Storage" / "ElementArray.cs")
        array = text(SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Array" / "JSArray.cs")
        modification = text(
            SOURCE_ROOT / "Broiler.JavaScript.BuiltIns" / "Array" / "JSArrayPrototype.Modification.cs"
        )
        for kind in ("Packed", "Holey", "Dictionary"):
            self.assertIn(kind, elements)
        self.assertIn("IndexedPrototypeVersion", array)
        self.assertIn("TryCopyWithin", modification)
        self.assertIn("TryFill", modification)
        self.assertIn("TryReverse", modification)


if __name__ == "__main__":
    unittest.main()
