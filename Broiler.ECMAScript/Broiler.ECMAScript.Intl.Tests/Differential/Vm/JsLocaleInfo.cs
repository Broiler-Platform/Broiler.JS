// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsLocaleInfo.cs
//     blob 5cc13ac82844c7ce0a301ae21ed1a6d9f1cc444c: the whole file: JsLocaleInfo
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed.

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// The region and script data <c>Intl.Locale</c>'s information methods read (JSD-0046): each
/// region's week as CLDR states it, and each script's line direction.
/// </summary>
internal sealed class JsLocaleInfo
{
    /// <summary>Each region's first day, weekend start and end and minimal days, a field empty where the world's applies.</summary>
    internal Dictionary<string, (string FirstDay, string WeekendStart, string WeekendEnd, string MinDays)> Weeks { get; } = new(StringComparer.Ordinal);

    /// <summary>Whether each script CLDR states a direction for is written right to left.</summary>
    internal Dictionary<string, bool> RightToLeft { get; } = new(StringComparer.Ordinal);

    /// <summary>Each language's list patterns, by <c>language|type</c> with CLDR's type names (JSD-0048).</summary>
    internal Dictionary<string, (string Start, string Middle, string End, string Pair)> ListPatterns { get; } = new(StringComparer.Ordinal);

    /// <summary>Each language's relative time literals and patterns, by <c>language|field|key</c> (JSD-0049).</summary>
    internal Dictionary<string, string> RelativeTimes { get; } = new(StringComparer.Ordinal);

    /// <summary>Each language's display names, by <c>language|kind|code</c> (JSD-0052).</summary>
    internal Dictionary<string, string> DisplayNames { get; } = new(StringComparer.Ordinal);
}
