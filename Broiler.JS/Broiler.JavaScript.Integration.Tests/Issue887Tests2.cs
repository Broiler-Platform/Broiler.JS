using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Integration.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/887 — the next three
// tractable test262 failure clusters:
//
//   * Cluster D (issue Problems 25, 104): Temporal.ZonedDateTime.prototype.getTimeZoneTransition
//     must skip the no-op transition `zic` emits at the 32-bit boundary (INT32_MAX seconds,
//     2038-01-19T03:14:07Z) for zones with no genuine future transitions.
//   * Cluster E (issue Problem 16): keyed destructuring-assignment evaluation order — a
//     property-reference target's base/key reference is evaluated before the source value and
//     before the default; only ToPropertyKey is deferred to the assignment.
//   * Cluster F (issue Problems 40, 106): the UTC offset in a Temporal ZonedDateTime / Instant
//     *string* is rounded to the nearest minute (±HH:MM), while `offset` / `offsetNanoseconds`
//     keep full sub-minute precision.
public class Issue887Tests2
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code)?.ToString();
    }

    // ── Cluster D — getTimeZoneTransition skips no-op transitions ──────────────────

    [Fact]
    public void NoFutureTransitionReturnsNull()
        // Asia/Riyadh (+03, no DST) has no transition after its 1947 LMT→+03 change.
        => Assert.Equal("null", Eval(
            "String(Temporal.ZonedDateTime.from('2024-01-01T00:00[Asia/Riyadh]').getTimeZoneTransition('next'))"));

    [Fact]
    public void GenuinePreviousTransitionStillFound()
        => Assert.Equal("1947-03-13T23:53:08+03:00[Asia/Riyadh]", Eval(
            "Temporal.ZonedDateTime.from('2024-01-01T00:00[Asia/Riyadh]').getTimeZoneTransition('previous').toString()"));

    [Fact]
    public void DstZoneTransitionsStillWork()
    {
        Assert.Equal("2024-03-10T03:00:00-04:00[America/New_York]", Eval(
            "Temporal.ZonedDateTime.from('2024-01-01T00:00[America/New_York]').getTimeZoneTransition('next').toString()"));
        Assert.Equal("2024-03-10T03:00:00-04:00[America/New_York]", Eval(
            "Temporal.ZonedDateTime.from('2024-06-01T00:00[America/New_York]').getTimeZoneTransition('previous').toString()"));
    }

    // ── Cluster E — keyed destructuring-assignment evaluation order ────────────────

    [Fact]
    public void MemberTargetReferenceEvaluatedBeforeSourceValue()
    {
        // `{ [sk]: tg[tk] = dv } = s` inside `with` so identifier resolutions surface via the
        // Proxy `has` trap. The target base/key (b:tg, b:tk) come before the source GetV (get).
        const string code = @"
            var log = [];
            var sk = { toString: () => { log.push('sk'); return 'p'; } };
            var s  = { get p() { log.push('get'); return undefined; } };
            var tk = { toString: () => { log.push('tk'); return 'q'; } };
            var tg = { set q(v) { log.push('set'); } };
            var env = new Proxy({}, { has(t, k) { log.push('b:' + k); } });
            var dv = 0;
            with (env) { ({ [sk]: tg[tk] = dv } = s); }
            log.join(',')";
        Assert.Equal("b:s,b:sk,sk,b:tg,b:tk,get,b:dv,tk,set", Eval(code));
    }

    [Fact]
    public void MemberTargetWithoutDefaultEvaluationOrder()
    {
        const string code = @"
            var log = [];
            function source(){ log.push('source'); return { get p(){ log.push('get'); } }; }
            function target(){ log.push('target'); return { set q(v){ log.push('set'); } }; }
            function sk(){ log.push('sk'); return { toString(){ log.push('sk-ts'); return 'p'; } }; }
            function tk(){ log.push('tk'); return { toString(){ log.push('tk-ts'); return 'q'; } }; }
            ({ [sk()]: target()[tk()] } = source());
            log.join(',')";
        Assert.Equal("source,sk,sk-ts,target,tk,get,tk-ts,set", Eval(code));
    }

    [Fact]
    public void DestructuringAssignmentStillProducesCorrectValues()
    {
        Assert.Equal("5", Eval("var o={}; ({a:o.x}={a:5}); '' + o.x"));
        Assert.Equal("9", Eval("var o={}; ({a:o.x=9}={}); '' + o.x"));
        Assert.Equal("3", Eval("var o={}; ({a:o.x=9}={a:3}); '' + o.x"));
        Assert.Equal("7", Eval("var o={}; var k='z'; ({a:o[k]=7}={}); '' + o.z"));
    }

    // ── Cluster F — Temporal offset strings rounded to the minute ─────────────────

    [Fact]
    public void ZonedDateTimeToStringRoundsSubMinuteOffset()
        // Europe/Paris LMT is +00:09:21; the serialized offset rounds to +00:09.
        => Assert.Equal("1800-01-01T00:00:00+00:09[Europe/Paris]", Eval(
            "new Temporal.PlainDateTime(1800, 1, 1).toZonedDateTime('Europe/Paris').toString()"));

    [Fact]
    public void InstantToStringRoundsSubMinuteOffsetHalfExpand()
        // Africa/Monrovia is -00:44:30 at the epoch; -44.5 min rounds away from zero to -00:45.
        => Assert.Equal("1969-12-31T23:15:30-00:45", Eval(
            "new Temporal.Instant(0n).toString({ timeZone: 'Africa/Monrovia' })"));

    [Fact]
    public void OffsetPropertyAndNanosecondsKeepFullPrecision()
    {
        Assert.Equal("+00:09:21", Eval(
            "new Temporal.PlainDateTime(1800, 1, 1).toZonedDateTime('Europe/Paris').offset"));
        Assert.Equal(((9 * 60 + 21) * 1_000_000_000L).ToString(), Eval(
            "String(new Temporal.PlainDateTime(1800, 1, 1).toZonedDateTime('Europe/Paris').offsetNanoseconds)"));
    }

    [Fact]
    public void WholeMinuteOffsetsAreUnaffected()
    {
        Assert.Equal("1969-12-31T19:00:00-05:00", Eval(
            "new Temporal.Instant(0n).toString({ timeZone: 'America/New_York' })"));
        Assert.Equal("1970-01-01T01:00:00+01:00", Eval(
            "new Temporal.Instant(0n).toString({ timeZone: 'Europe/Berlin' })"));
    }
}
