using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.BuiltIns.Tests;

// Regression tests for https://github.com/MaiRat/Broiler.JS/issues/818 — Problems 18 & 19
// (test/built-ins/Temporal/{Instant,ZonedDateTime}/prototype/{since,until}/
//  float64-representable-integer.js):
//   Test262Error: microseconds result should have FP precision loss
//   Expected SameValue(«-18446744073709548», «-18446744073709552») to be true.
//
// Temporal duration differences distribute an exact BigInteger nanosecond total into
// the duration's components and stored each as `(double)BigInteger`, which .NET rounds
// toward zero — so the microseconds component of a near-2^64 ns difference came out as
// -18446744073709548 (a multiple of 4 below the value) instead of the IEEE
// round-to-nearest -18446744073709552 (= ℝ(𝔽(-18446744073709551))) the spec prescribes.
// The Instant / PlainDateTime / ZonedDateTime balancers now use the existing ties-to-even
// NearestDouble helper, matching JSTemporalDuration's own balancer.
public class Issue818DurationFloat64Tests
{
    private static string Eval(string code)
    {
        using var ctx = new JSContext();
        return ctx.Eval(code).ToString();
    }

    [Fact]
    public void InstantSinceMicrosecondsIsRoundedToNearestDouble()
        => Assert.Equal("true", Eval(
            "var r = new Temporal.Instant(0n).since(new Temporal.Instant(18446744073709551616n), { largestUnit: 'microseconds' });" +
            "String(r.microseconds === -18446744073709552 && r.nanoseconds === -616)"));

    [Fact]
    public void InstantUntilMicrosecondsIsRoundedToNearestDouble()
        => Assert.Equal("true", Eval(
            "var r = new Temporal.Instant(0n).until(new Temporal.Instant(18446744073709551616n), { largestUnit: 'microseconds' });" +
            "String(r.microseconds === 18446744073709552 && r.nanoseconds === 616)"));

    [Fact]
    public void ZonedDateTimeSinceMicrosecondsIsRoundedToNearestDouble()
        => Assert.Equal("true", Eval(
            "var z1 = new Temporal.ZonedDateTime(0n, 'UTC');" +
            "var z2 = new Temporal.ZonedDateTime(18446744073709551616n, 'UTC');" +
            "var r = z1.since(z2, { largestUnit: 'microseconds' });" +
            "String(r.microseconds === -18446744073709552 && r.nanoseconds === -616)"));

    [Fact]
    public void ZonedDateTimeUntilMicrosecondsIsRoundedToNearestDouble()
        => Assert.Equal("true", Eval(
            "var z1 = new Temporal.ZonedDateTime(0n, 'UTC');" +
            "var z2 = new Temporal.ZonedDateTime(18446744073709551616n, 'UTC');" +
            "var r = z1.until(z2, { largestUnit: 'microseconds' });" +
            "String(r.microseconds === 18446744073709552 && r.nanoseconds === 616)"));

    // Adding 1 microsecond to a value whose internal representation is already the
    // rounded double must not change it (no more-precise internal representation).
    [Fact]
    public void SubsequentDurationOpsUseTheRoundedRepresentation()
        => Assert.Equal("0", Eval(
            "var r = new Temporal.Instant(0n).since(new Temporal.Instant(18446744073709551616n), { largestUnit: 'microseconds' });" +
            "String(Temporal.Duration.compare(r.add({ microseconds: 1 }), r))"));

    // Ordinary (small) differences are unaffected.
    [Fact]
    public void SmallDifferencesAreExact()
        => Assert.Equal("90061000000001", Eval(
            "var r = new Temporal.Instant(0n).until(new Temporal.Instant(90061000000001n), { largestUnit: 'nanoseconds' });" +
            "'' + r.nanoseconds"));
}
