// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsNumberFormatter.cs
//     blob db69b0dc5055e3e5c98648beea3d019ba427d78a: Unsigned(string, bool)
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; Unsigned sits in a static class JsNumberFormatter of its own instead of the sealed class JsNumberFormatter, whose other members are the engine's;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed;
//   - Unsigned made internal (private in the source);
//   - its JsUnsignedRounding is this arm's copy of the enum (JsDecimal.cs beside this file);
//   - the using directives (System, System.Collections.Generic, System.Globalization, System.Text) dropped: the copied member needs none.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>ECMA-402's GetUnsignedRoundingMode as Broiler.VM's <c>Intl.NumberFormat</c> has it, over the rounding mode's name.</summary>
internal static class JsNumberFormatter
{
    /// <summary>ECMA-402's GetUnsignedRoundingMode.</summary>
    internal static JsUnsignedRounding Unsigned(string mode, bool negative) => mode switch
    {
        "ceil" => negative ? JsUnsignedRounding.Zero : JsUnsignedRounding.Infinity,
        "floor" => negative ? JsUnsignedRounding.Infinity : JsUnsignedRounding.Zero,
        "expand" => JsUnsignedRounding.Infinity,
        "trunc" => JsUnsignedRounding.Zero,
        "halfCeil" => negative ? JsUnsignedRounding.HalfZero : JsUnsignedRounding.HalfInfinity,
        "halfFloor" => negative ? JsUnsignedRounding.HalfInfinity : JsUnsignedRounding.HalfZero,
        "halfTrunc" => JsUnsignedRounding.HalfZero,
        "halfEven" => JsUnsignedRounding.HalfEven,
        _ => JsUnsignedRounding.HalfInfinity,
    };
}
