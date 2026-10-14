// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsDecimal.cs
//     blob 554c0f645b05ac25c9b64d0de0f32c6d6a943104: the enum JsUnsignedRounding
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; only the enum JsUnsignedRounding is copied, which JsNumberFormatter.Unsigned answers; the file's other types (JsDecimalKind, JsDecimal) are the engine's;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed;
//   - the using directives (System, System.Globalization) dropped: the enum needs none.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>ECMA-402's unsigned rounding modes (table 22 of the pinned edition).</summary>
internal enum JsUnsignedRounding
{
    /// <summary>Toward zero.</summary>
    Zero,

    /// <summary>Away from zero.</summary>
    Infinity,

    /// <summary>To the nearer, ties toward zero.</summary>
    HalfZero,

    /// <summary>To the nearer, ties away from zero.</summary>
    HalfInfinity,

    /// <summary>To the nearer, ties to the even one.</summary>
    HalfEven,
}
