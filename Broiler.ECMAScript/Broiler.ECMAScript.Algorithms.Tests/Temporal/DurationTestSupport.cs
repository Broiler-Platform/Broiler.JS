using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Broiler.ECMAScript.Numbers;
using Broiler.ECMAScript.Runners;
using Broiler.ECMAScript.Temporal;
using Xunit;
using static Broiler.ECMAScript.Algorithms.Tests.CalendarTestSupport;

namespace Broiler.ECMAScript.Algorithms.Tests;

/// <summary>
/// What the tests of the durations, rounding and differences sub-areas (milestone SJL-10.3, part C) share: the runner's
/// Temporal adapter over the data home's tables, so that a probe's line is computed through the engine-side steps an
/// adapter takes around the library, and the probes' <c>show</c> of a value.
/// </summary>
internal static class DurationTestSupport
{
    private static readonly Lazy<TemporalAdapter> Shared = new(() => new TemporalAdapter(Calendars, TimeTablesFixture.Database));

    /// <summary>The adapter, over one set of calendars and the zone database.</summary>
    internal static TemporalAdapter Adapter => Shared.Value;

    /// <summary>A property bag.</summary>
    internal static TemporalAdapter.Bag Bag(params (string Key, object? Value)[] entries) => new(entries);

    /// <summary>The probes' <c>show</c>: a Temporal object as its tag and string, a number as JavaScript writes it.</summary>
    internal static string Show(object? value) => value switch
    {
        null => "undefined",
        TemporalAdapter.JsNull => "null",
        BigInteger i => i.ToString(CultureInfo.InvariantCulture) + "n",
        string s => s,
        bool b => b ? "true" : "false",
        double d => d == 0 && double.IsNegative(d) ? "-0" : NumberText.ToString(d),
        int i => NumberText.ToString(i),
        TemporalAdapter.Duration or TemporalAdapter.PlainDate or TemporalAdapter.PlainDateTime or TemporalAdapter.PlainTime or
            TemporalAdapter.PlainYearMonth or TemporalAdapter.PlainMonthDay or TemporalAdapter.Instant or TemporalAdapter.ZonedDateTime =>
            TemporalAdapter.Tag(value) + " " + Adapter.Render(value),
        _ => throw new ArgumentException("no rendering for " + value.GetType().Name, nameof(value)),
    };

    /// <summary>The probes' <c>t</c>: the value shown, or the constructor of what it threw.</summary>
    internal static string Run(Func<object?> line)
    {
        try
        {
            return Show(line());
        }
        catch (TemporalAdapter.RangeError)
        {
            return "THROWS RangeError";
        }
        catch (TemporalAdapter.TypeError)
        {
            return "THROWS TypeError";
        }
    }

    /// <summary>
    /// Replays one of Broiler.VM's answer files: every line is computed by <paramref name="lines"/>, or named among the
    /// lines only an engine can answer; each computed line must answer as Broiler.VM did, a refusal compared by its
    /// constructor alone.
    /// </summary>
    internal static void Replay(string file, string sha256, IReadOnlyDictionary<string, Func<object?>> lines, IReadOnlyCollection<string>? engineOnly = null)
    {
        var answers = Correspondence(file, sha256);
        var mismatches = new List<string>();
        var computed = 0;
        foreach (var (label, answer) in answers)
        {
            if (engineOnly is not null && engineOnly.Contains(label))
                continue;

            Assert.True(lines.TryGetValue(label, out var line), file + ": no computation for the line " + label);
            computed++;
            var mine = Run(line!);
            if (WithoutMessage(mine) != WithoutMessage(answer))
                mismatches.Add(label + " => " + answer + "   library: " + mine);
        }

        Assert.True(mismatches.Count == 0, mismatches.Count + " of " + computed + " lines differ:\n" + string.Join('\n', mismatches));
        Assert.Equal(answers.Count - (engineOnly?.Count ?? 0), computed);
        Assert.Equal(lines.Count, computed);
    }
}
