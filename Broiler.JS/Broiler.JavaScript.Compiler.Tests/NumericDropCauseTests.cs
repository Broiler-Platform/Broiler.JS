using Broiler.JavaScript.Compiler;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Compiler.Tests;

// What defeated the numeric proof for each local the analysis's fixed point had to drop
// (docs/performance-roadmap.md item 3-8). Item 3-6 counted the drops and read them as one
// population — "dropped for want of a type" — and item 3-8 was specified from that sentence. The
// causes are what says where a runtime guard would have to sit, so the classifier has to put each
// shape in the right bucket rather than merely count.
//
// The classifier mirrors NumericLocalAnalysis.IsNumeric branch for branch, so these also pin that
// correspondence: a shape IsNumeric accepts must produce no cause at all, and a shape it rejects
// must produce exactly one.
[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class NumericDropCauseTests
{
    /// <summary>Compiles a function body and returns the drop causes it recorded.</summary>
    private static (string Result, long[] Causes, long Dropped) Compile(string body)
    {
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        var result = context.Eval("(function(){ " + body + " })()").ToString();
        var snapshot = CompilerSpecializationDiagnostics.Snapshot();
        return (result, snapshot.NumericDropCauses, snapshot.NumericCandidatesDropped);
    }

    private static long Cause(long[] causes, NumericDropCause cause) => causes[(int)cause];

    [Theory]
    // The three that account for 79% of the corpus's drops.
    [InlineData("var o = { x: 1 }; var v = o.x; var s = 0; for (var i = 0; i < 3; i++) s += v; return s;",
        NumericDropCause.PropertyRead)]
    [InlineData("function g() { return 1; } var v = g(); var s = 0; for (var i = 0; i < 3; i++) s += v; return s;",
        NumericDropCause.CallResult)]
    [InlineData("var a = [1]; var v = a[0]; var s = 0; for (var i = 0; i < 3; i++) s += v; return s;",
        NumericDropCause.ElementRead)]
    // `new` is a call for this purpose: the analysis types neither.
    [InlineData("function C() { this.n = 1; } var v = new C(); var s = 0; for (var i = 0; i < 3; i++) s += 1; return s;",
        NumericDropCause.CallResult)]
    // A literal that is simply not a number, and the object/array/function forms of the same.
    [InlineData("var v = 'x'; var s = 0; for (var i = 0; i < 3; i++) s += 1; return s;",
        NumericDropCause.NonNumericLiteral)]
    [InlineData("var v = {}; var s = 0; for (var i = 0; i < 3; i++) s += 1; return s;",
        NumericDropCause.NonNumericLiteral)]
    [InlineData("var v = []; var s = 0; for (var i = 0; i < 3; i++) s += 1; return s;",
        NumericDropCause.NonNumericLiteral)]
    // An operator the analysis will not type at all.
    [InlineData("var q = 1; var v = typeof q; var s = 0; for (var i = 0; i < 3; i++) s += 1; return s;",
        NumericDropCause.UnhandledOperator)]
    public void ADropIsAttributedToWhatDefeatedTheProof(string body, NumericDropCause expected)
    {
        var (_, causes, dropped) = Compile(body);

        Assert.True(dropped >= 1, "expected the fixed point to drop at least one candidate");
        Assert.True(Cause(causes, expected) >= 1,
            $"expected at least one {expected}, got [{string.Join(", ", causes)}]");
    }

    [Fact]
    public void AParameterIsDistinguishedFromAnyOtherName()
    {
        // The cause item 3-3 named as phase 4's job. It has to be told apart from a global,
        // because an entry guard reaches one and not the other.
        var (_, causes, _) = Compile("return (function (p) { var v = p; var s = 0; for (var i = 0; i < 3; i++) s += v; return s; })(1);");
        Assert.Equal(1, Cause(causes, NumericDropCause.Parameter));
        Assert.Equal(0, Cause(causes, NumericDropCause.OtherName));
    }

    [Fact]
    public void AGlobalIsNotAParameter()
    {
        var (_, causes, _) = Compile("var v = globalThis; var s = 0; for (var i = 0; i < 3; i++) s += 1; return s;");
        Assert.Equal(1, Cause(causes, NumericDropCause.OtherName));
        Assert.Equal(0, Cause(causes, NumericDropCause.Parameter));
    }

    [Fact]
    public void ACascadeIsNotCountedAsARootCause()
    {
        // `v` is dropped by the property read; `s` is then dropped only because `v` was. Fixing
        // the root fixes the dependent for free, so counting both as "needs a guard" would
        // double-count — which is the distinction the corpus figure rests on.
        //
        // The receiver arrives as a PARAMETER rather than an object literal on purpose: declaring
        // it here would make `o` a third candidate, dropped by its own initializer, and the point
        // of the test is the pair.
        var (_, causes, dropped) = Compile("return (function (o) { var v = o.x; var s = 0; for (var i = 0; i < 3; i++) s = s + v; return s; })({ x: 1 });");

        Assert.Equal(2, dropped);
        Assert.Equal(1, Cause(causes, NumericDropCause.PropertyRead));
        Assert.Equal(1, Cause(causes, NumericDropCause.DroppedCandidate));
    }

    [Fact]
    public void TheFirstLeafWins()
    {
        // `a.x * 2 + 1` is charged to the property read, not to the operator and not to the
        // literal: the cause is the first thing IsNumeric refuses, walked in evaluation order.
        // The receiver is a parameter so that it contributes no drop of its own.
        var (_, causes, dropped) = Compile("return (function (a) { var v = a.x * 2 + 1; var s = 0; for (var i = 0; i < 3; i++) s += 1; return s; })({ x: 1 });");

        Assert.Equal(1, dropped);
        Assert.Equal(1, Cause(causes, NumericDropCause.PropertyRead));
        Assert.Equal(0, Cause(causes, NumericDropCause.UnhandledOperator));
        Assert.Equal(0, Cause(causes, NumericDropCause.NonNumericLiteral));
    }

    [Fact]
    public void AProvablyNumericLocalRecordsNoCauseAtAll()
    {
        // The other half of the correspondence with IsNumeric: nothing it accepts may be charged.
        var (result, causes, dropped) = Compile("var v = 3.5; var s = 0; for (var i = 0; i < 3; i++) s = s + v * 2; return s;");

        Assert.Equal("21", result);
        Assert.Equal(0, dropped);
        Assert.All(causes, c => Assert.Equal(0, c));
    }

    [Fact]
    public void TheTierItselfCanBeTurnedOffForAControl()
    {
        // Every phase 3 item was measured as a delta against the tier as it stood; this switch is
        // what makes the tier itself measurable. The answer must not change, only the count.
        const string Body = "var s = 0; for (var i = 0; i < 10; i++) s += i; return s;";

        var previous = NumericLocalSpecialization.Enabled;
        try
        {
            NumericLocalSpecialization.Enabled = true;
            var on = Compile(Body);
            NumericLocalSpecialization.Enabled = false;
            var off = Compile(Body);

            Assert.Equal("45", on.Result);
            Assert.Equal("45", off.Result);
        }
        finally
        {
            NumericLocalSpecialization.Enabled = previous;
        }
    }
}
