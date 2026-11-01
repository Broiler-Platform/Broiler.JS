using System;
using System.Linq;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// The limits, the providers' shapes, the diagnostics' forms and the parse entry at stage SJL-13.2a (Broiler.VM's JSD-0083,
/// decisions D7, D11, D14, D15 and D32).
/// </summary>
public sealed class ContractTests
{
    [Fact]
    public void The_tree_depth_is_Broiler_VM_s_bound_and_the_nesting_admits_Broiler_VM_s_supported_maximum()
    {
        // Broiler.VM's SliceParseOptions.MaximumTreeDepth and MaximumSupportedNestingDepth at its SJL-13.1 head (decision D14).
        Assert.Equal(10_000, ParseLimits.MaximumTreeDepth);
        Assert.True(ParseLimits.MaximumSupportedNestingDepth >= 512);

        var limits = ParseLimits.Default;
        Assert.Equal(ParseLimits.MaximumSupportedNestingDepth, limits.NestingDepth.Maximum);
        Assert.Equal(ErrorReporting.FirstError, limits.Reporting);
        Assert.Equal(int.MaxValue, limits.DiagnosticCap);
    }

    [Fact]
    public void A_nesting_depth_outside_what_the_library_supports_and_a_cap_below_one_are_refused()
    {
        Assert.Equal(64, new ParseLimits { NestingDepth = new DepthLimit(64) }.NestingDepth.Maximum);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseLimits { NestingDepth = new DepthLimit(ParseLimits.MaximumSupportedNestingDepth + 1) });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseLimits { NestingDepth = default });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseLimits { DiagnosticCap = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseLimits { Reporting = (ErrorReporting)2 });
    }

    [Fact]
    public void The_regular_expression_verdict_carries_what_the_validate_entry_answers()
    {
        Assert.Equal(RegularExpressionStatus.Valid, RegularExpressionVerdict.Valid.Status);
        Assert.Null(RegularExpressionVerdict.Valid.MessageId);

        // Broiler.Regex's validate entry: code, message identifier, argument, position (in the flags for its three flag codes).
        var flags = RegularExpressionVerdict.Invalid("regexp.flags.duplicate", "g", 2, 1, inFlags: true);
        Assert.Equal((RegularExpressionStatus.Invalid, 2, "regexp.flags.duplicate", "g", 1, true), (flags.Status, flags.Code, flags.MessageId, flags.Argument, flags.Position, flags.InFlags));

        // Broiler.VM's matcher, which tells conditions apart by message text alone: EE0019's identifier, its message as the argument.
        var matcher = RegularExpressionVerdict.Invalid("regexp.literal.invalid", "Unterminated group", 0, 0, inFlags: false);
        Assert.Equal((RegularExpressionStatus.Invalid, 0, false), (matcher.Status, matcher.Code, matcher.InFlags));

        var deep = RegularExpressionVerdict.NestedTooDeep("limit.regexp.too-deep", null, 90, 512);
        Assert.Equal((RegularExpressionStatus.LimitExceeded, 512, false), (deep.Status, deep.Position, deep.InFlags));

        Assert.Throws<ArgumentException>(() => RegularExpressionVerdict.Invalid("", null, 0, 0, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => RegularExpressionVerdict.Invalid("regexp.literal.invalid", null, 0, -1, false));
    }

    [Fact]
    public void A_diagnostic_carries_a_shared_code_an_identifier_its_arguments_a_phase_and_a_span()
    {
        var arguments = new[] { "x" };
        var diagnostic = new SyntaxDiagnostic("JSE1201", "scope.lexical.duplicate", arguments, DiagnosticPhase.Semantics, new SourceSpan(4, 1, 1, 5), "a.js");
        arguments[0] = "y";
        Assert.Equal(("JSE1201", "scope.lexical.duplicate", DiagnosticPhase.Semantics, "a.js"), (diagnostic.Code, diagnostic.MessageId, diagnostic.Phase, diagnostic.SourceName));
        Assert.Equal(["x"], diagnostic.Arguments);
        Assert.Equal(new SourceSpan(4, 1, 1, 5), diagnostic.Span);
        Assert.Equal([("Tokenizer", 0), ("Parser", 1), ("Semantics", 2)], Enum.GetValues<DiagnosticPhase>().Select(static p => (p.ToString(), (int)p)).ToArray());
    }

    [Theory]
    [InlineData("JSE120")]
    [InlineData("JSE12011")]
    [InlineData("JSX1201")]
    [InlineData("JSE12a1")]
    public void A_code_not_of_the_shared_form_is_refused(string code)
    {
        Assert.Throws<ArgumentException>(() => new SyntaxDiagnostic(code, "scope.lexical.duplicate", [], DiagnosticPhase.Parser, new SourceSpan(0, 0, 1, 1), ""));
    }

    [Theory]
    [InlineData("scope.lexical.duplicate", true)]
    [InlineData("limit.regexp.too-deep", true)]
    [InlineData("regexp.flags.unicode-with-unicode-sets", true)]
    [InlineData("a.b.c.d", true)]
    [InlineData("scope", false)]
    [InlineData("a.b.c.d.e", false)]
    [InlineData("Scope.lexical", false)]
    [InlineData("scope..lexical", false)]
    [InlineData("scope.lexical-", false)]
    [InlineData("scope.-lexical", false)]
    [InlineData("scope.lex--ical", false)]
    [InlineData("scope.1exical", false)]
    [InlineData("scope.lexical.", false)]
    public void A_message_identifier_has_the_catalogue_s_form(string messageId, bool admitted)
    {
        var make = () => new SyntaxDiagnostic("JSE1201", messageId, [], DiagnosticPhase.Parser, new SourceSpan(0, 0, 1, 1), "");
        if (admitted)
            Assert.Equal(messageId, make().MessageId);
        else
            Assert.Throws<ArgumentException>(make);
    }

    [Fact]
    public void A_span_is_0_based_in_its_offset_and_1_based_in_its_line_and_column()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceSpan(-1, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceSpan(0, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceSpan(0, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceSpan(0, 0, 1, 0));
    }

    /// <summary>
    /// Stage 4a's answer: not implemented, with no diagnostic, for every goal; and the parse reads neither provider and
    /// charges no meter, because it reads none of the source (the providers here throw when read, the meter counts).
    /// </summary>
    [Theory]
    [InlineData(ParseGoal.Script)]
    [InlineData(ParseGoal.Module)]
    [InlineData(ParseGoal.Eval)]
    [InlineData(ParseGoal.FormalParameters)]
    [InlineData(ParseGoal.FunctionBody)]
    public void Every_source_is_answered_not_implemented_with_nothing_read_or_charged(ParseGoal goal)
    {
        ulong charged = 0;
        foreach (var text in new[] { string.Empty, "1", "let let = 1;", "/(/", "a\u2028b" })
        {
            var request = new ParseRequest(new SourceText(text), goal, NoTables.Instance, NoValidator.Instance) { Meter = units => charged += units };
            var result = SyntaxParser.Parse(request);
            Assert.Equal(ParseOutcome.NotImplemented, result.Outcome);
            Assert.Empty(result.Diagnostics);
        }

        Assert.Equal(0UL, charged);
        Assert.Throws<ArgumentNullException>(() => SyntaxParser.Parse(null!));
        Assert.Equal([("Accepted", 0), ("Refused", 1), ("NotImplemented", 2)], Enum.GetValues<ParseOutcome>().Select(static o => (o.ToString(), (int)o)).ToArray());
    }
}
