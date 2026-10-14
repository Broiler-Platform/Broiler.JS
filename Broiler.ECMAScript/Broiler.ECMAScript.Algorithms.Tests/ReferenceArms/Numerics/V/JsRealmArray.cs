// REFERENCE ARM V: a test-only copy of Broiler.VM's implementation before the cut-over, for the
// reference-arm comparison of bundle sjl-4-001 (docs/evidence/sjl-4-001/reference-arms.md). It is
// never referenced outside this test project, and it is retired at SJL-16.
//
// Copied from Broiler.VM at a04b2e5 (SJL-3's merge, bundle sjl-4-001's base):
//   src/Broiler.VM.Profile.JavaScript/JsRealm.Array.cs
//     blob 7c1228f22a893d3b6a1afb9a9d0c79c3e9f3e679: the constant ArrayMaxSafeLength and ArrayToLength
//
// Changes from the source, none of which changes a result or a charge:
//   - the namespace; the two members sit in a static class JsRealmArray of their own instead of the partial class JsRealm, whose other members are the engine's;
//   - the generated assurance header and the per-member assurance annotations (Broiler-AI / Broiler-Human / Broiler-Falsified-If lines) removed;
//   - the SPDX lines above the assurance header removed;
//   - ArrayToLength made internal (private in the source);
//   - the parameters (JsEngine engine, JsValue value) replaced by the double `length` the source's first line computes as `engine.ToInteger(value)` (JsValue.ToInteger(engine.ToNumber(value))); that line is the engine's adapter and is not copied;
//   - the using directive (System.Collections.Generic) dropped: the copied lines need none.

namespace Broiler.ECMAScript.Algorithms.Tests.ReferenceArms.Numerics.V;

/// <summary>Broiler.VM's <c>ToLength</c>, over the integer <c>ToIntegerOrInfinity</c> answered.</summary>
internal static class JsRealmArray
{
    /// <summary>The largest length an array-like may have: 2^53-1.</summary>
    private const double ArrayMaxSafeLength = 9007199254740991.0;

    /// <summary>The specification's <c>ToLength</c>: an integer clamped to [0, 2^53-1].</summary>
    internal static double ArrayToLength(double length)
    {
        if (length <= 0)
        {
            return 0;
        }

        return length > ArrayMaxSafeLength ? ArrayMaxSafeLength : length;
    }
}
