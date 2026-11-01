// DIFFERENTIAL CHECK, BROILER.VM'S ARM: a test-only copy of Broiler.VM's code at its SJL-9 rule commit, the
// reference the differential check of Broiler.VM's JSD-0082, decision D22, compares the library with. It is compiled
// into this test project and nowhere else, never ships, and runs under the JIT only; see Differential/Vm/README.md.
//
// Copied from Broiler.VM at 4cf9ca449fc96a1a5be810f6d0ca31c42b855c8f:
//   src/Broiler.VM.Profile.JavaScript/JsRealm.NumberFormat.cs
//     blob 7ae56e7fcc1a01aa976edb86b137b30bb30fd309: JsRealm's Intl.NumberFormat part, of which only the sanctioned units are kept
//
// Changes from the source, none of which changes a result:
//   - the namespace;
//   - the generated SPDX and assurance header and the per-member assurance annotations removed;
//   - removed: every member of JsRealm but `SanctionedUnits`, each the number formatter's of sub-area (c), and the type `JsNumberFormatObject`.

using System;
using System.Collections.Generic;

namespace Broiler.ECMAScript.Intl.Tests.Differential.Vm;

/// <summary>
/// <c>Intl.NumberFormat</c>, and <c>Number.prototype.toLocaleString</c> and
/// <c>BigInt.prototype.toLocaleString</c> through it (ECMA-402 s16; JSD-0044).
/// </summary>
/// <remarks>
/// <para>
/// <b>The constructor reads its options in ECMA-402's order</b> and keeps what it resolved; the
/// formatting is <see cref="JsNumberFormatter"/>'s. Called as a function on an object that inherits
/// from <c>Intl.NumberFormat.prototype</c>, it takes ECMA-402's normative optional legacy path: the
/// new formatter is stored on that object under the realm's <c>IntlLegacyConstructedSymbol</c>, and
/// the methods that unwrap read it from there.
/// </para>
/// </remarks>
internal sealed partial class JsRealm
{
    /// <summary>The sanctioned single units of ECMA-402 table 2.</summary>
    private static readonly string[] SanctionedUnits =
    [
        "acre", "bit", "byte", "celsius", "centimeter", "day", "degree", "fahrenheit", "fluid-ounce", "foot",
        "gallon", "gigabit", "gigabyte", "gram", "hectare", "hour", "inch", "kilobit", "kilobyte", "kilogram",
        "kilometer", "liter", "megabit", "megabyte", "meter", "microsecond", "mile", "mile-scandinavian",
        "milliliter", "millimeter", "millisecond", "minute", "month", "nanosecond", "ounce", "percent", "petabyte",
        "pound", "second", "stone", "terabit", "terabyte", "week", "yard", "year",
    ];
}
