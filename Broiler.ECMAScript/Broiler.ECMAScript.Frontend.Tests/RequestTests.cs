using System;
using System.Linq;
using Broiler.Unicode.Algorithms;
using Xunit;

namespace Broiler.ECMAScript.Syntax.Tests;

/// <summary>
/// Providers that throw when read: the request reads neither. <i>(Revised 2026-11-02, at stage SJL-13.2d: the validator admits
/// every literal, since the parse asks it about each from stage 4d, and the Frontend tests' EarlyErrorTests record its calls;
/// the summary read "Providers that throw when read: the request reads neither, and the parse asks the validator nothing at
/// stage 4b.")</i>
/// </summary>
internal sealed class NoTables : IUnicodeTables
{
    internal static NoTables Instance { get; } = new();

    public string UnicodeVersion => throw new InvalidOperationException("read");

    public ReadOnlySpan<byte> Table(UnicodeTable table) => throw new InvalidOperationException("read");
}

/// <inheritdoc cref="NoTables"/>
internal sealed class NoValidator : IRegularExpressionValidator
{
    internal static NoValidator Instance { get; } = new();

    public RegularExpressionVerdict Validate(string pattern, string flags) => RegularExpressionVerdict.Valid;
}

/// <summary>
/// The request (Broiler.VM's JSD-0080's request; JSD-0083, decisions D7, D8, D10, D14 to D17 and D32): its defaults for
/// each goal, the library's default request, and the preconditions it refuses when it is made.
/// </summary>
public sealed class RequestTests
{
    internal static ParseRequest Request(ParseGoal goal, string text = "1") =>
        new(new SourceText(text), goal, NoTables.Instance, NoValidator.Instance);

    [Fact]
    public void The_goals_are_the_contract_s_five_and_keep_their_numbers()
    {
        Assert.Equal(
            [("Script", 0), ("Module", 1), ("Eval", 2), ("FormalParameters", 3), ("FunctionBody", 4)],
            Enum.GetValues<ParseGoal>().Select(static g => (g.ToString(), (int)g)).ToArray());
        Assert.Equal(
            [("Normal", 0), ("Generator", 1), ("Async", 2), ("AsyncGenerator", 3)],
            Enum.GetValues<DynamicFunctionKind>().Select(static k => (k.ToString(), (int)k)).ToArray());
    }

    [Theory]
    [InlineData(ParseGoal.Script, false, true)]
    [InlineData(ParseGoal.Module, true, false)]
    [InlineData(ParseGoal.Eval, false, true)]
    [InlineData(ParseGoal.FormalParameters, false, true)]
    [InlineData(ParseGoal.FunctionBody, false, true)]
    public void The_default_request_takes_the_goal_s_switches(ParseGoal goal, bool topLevelAwait, bool htmlLikeComments)
    {
        var request = Request(goal);
        Assert.Equal(goal, request.Goal);
        Assert.Equal(new ParseSwitches { ForcedStrict = false, TopLevelAwait = topLevelAwait, AnnexB = true, HtmlLikeComments = htmlLikeComments }, request.Switches);
        Assert.Equal(ParseSwitches.For(goal), request.Switches);
        Assert.Equal(ProposalFlags.None, request.Proposals);
        Assert.Equal(LexerExtensions.None, request.Extensions);
        Assert.Same(ParseLimits.Default, request.Limits);
        Assert.Equal(DynamicFunctionKind.Normal, request.FunctionKind);
        Assert.Null(request.Meter);
        Assert.Same(NoTables.Instance, request.UnicodeTables);
        Assert.Same(NoValidator.Instance, request.RegularExpressions);
    }

    [Fact]
    public void Eval_code_inherits_the_top_level_s_context_by_default_and_no_other_goal_has_one()
    {
        Assert.Same(InheritedContext.TopLevel, Request(ParseGoal.Eval).Context);
        foreach (var goal in new[] { ParseGoal.Script, ParseGoal.Module, ParseGoal.FormalParameters, ParseGoal.FunctionBody })
            Assert.Null(Request(goal).Context);

        var top = InheritedContext.TopLevel;
        Assert.False(top.InFunction || top.InMethod || top.InDerivedConstructor || top.InClassFieldInitializer);
        Assert.Null(top.VisiblePrivateNames);
    }

    [Fact]
    public void A_request_states_every_part_the_contract_names()
    {
        var context = new InheritedContext { InFunction = true, InMethod = true, InDerivedConstructor = true, VisiblePrivateNames = ["#x", "#y"] };
        var limits = new ParseLimits { NestingDepth = new DepthLimit(64), DiagnosticCap = 64, Reporting = ErrorReporting.AllErrors };
        ulong charged = 0;
        var request = new ParseRequest(new SourceText("eval code", "e.js", 10, 2, 5), ParseGoal.Eval, NoTables.Instance, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Eval) with { ForcedStrict = true },
            Context = context,
            Proposals = ProposalFlags.Decorators | ProposalFlags.AutoAccessors | ProposalFlags.ImportSourceCalls | ProposalFlags.ImportDeferCalls,
            Extensions = LexerExtensions.DecimalSuffix,
            Limits = limits,
            Meter = units => charged += units,
        };

        Assert.True(request.Switches.ForcedStrict);
        Assert.Same(context, request.Context);
        Assert.Equal(["#x", "#y"], request.Context!.VisiblePrivateNames!);
        Assert.Equal(64, request.Limits.NestingDepth.Maximum);
        Assert.Equal(ErrorReporting.AllErrors, request.Limits.Reporting);
        Assert.Equal(("eval code", "e.js", 10, 2, 5), (request.Source.Text, request.Source.Name, request.Source.BaseOffset, request.Source.BaseLine, request.Source.BaseColumn));
        Assert.NotNull(request.Meter);
    }

    [Fact]
    public void The_providers_and_the_source_are_required()
    {
        var source = new SourceText("1");
        Assert.Throws<ArgumentNullException>(() => new ParseRequest(null!, ParseGoal.Script, NoTables.Instance, NoValidator.Instance));
        Assert.Throws<ArgumentNullException>(() => new ParseRequest(source, ParseGoal.Script, null!, NoValidator.Instance));
        Assert.Throws<ArgumentNullException>(() => new ParseRequest(source, ParseGoal.Script, NoTables.Instance, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseRequest(source, (ParseGoal)5, NoTables.Instance, NoValidator.Instance));
    }

    [Theory]
    [InlineData(ParseGoal.Eval)]
    [InlineData(ParseGoal.FormalParameters)]
    [InlineData(ParseGoal.FunctionBody)]
    public void Top_level_await_is_refused_for_a_goal_but_Script_and_Module(ParseGoal goal)
    {
        Assert.Throws<ArgumentException>(() => new ParseRequest(new SourceText("1"), goal, NoTables.Instance, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(goal) with { TopLevelAwait = true },
        });
    }

    [Fact]
    public void Top_level_await_applies_to_a_script_when_the_request_sets_it()
    {
        var request = new ParseRequest(new SourceText("await 1"), ParseGoal.Script, NoTables.Instance, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Script) with { TopLevelAwait = true },
        };
        Assert.True(request.Switches.TopLevelAwait);

        var module = new ParseRequest(new SourceText("await 1"), ParseGoal.Module, NoTables.Instance, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Module) with { TopLevelAwait = false },
        };
        Assert.False(module.Switches.TopLevelAwait);
    }

    [Fact]
    public void A_module_has_no_HTML_like_comments_and_a_script_may_turn_them_off()
    {
        Assert.Throws<ArgumentException>(() => new ParseRequest(new SourceText("1"), ParseGoal.Module, NoTables.Instance, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Module) with { HtmlLikeComments = true },
        });
        var script = new ParseRequest(new SourceText("1"), ParseGoal.Script, NoTables.Instance, NoValidator.Instance)
        {
            Switches = ParseSwitches.For(ParseGoal.Script) with { HtmlLikeComments = false },
        };
        Assert.False(script.Switches.HtmlLikeComments);
    }

    [Fact]
    public void Only_eval_code_inherits_a_context_and_eval_code_always_has_one()
    {
        Assert.Throws<ArgumentException>(() => new ParseRequest(new SourceText("1"), ParseGoal.Script, NoTables.Instance, NoValidator.Instance)
        {
            Context = InheritedContext.TopLevel,
        });
        Assert.Throws<ArgumentNullException>(() => new ParseRequest(new SourceText("1"), ParseGoal.Eval, NoTables.Instance, NoValidator.Instance)
        {
            Context = null,
        });
    }

    [Fact]
    public void A_dynamic_function_s_kind_applies_to_its_two_goals_alone()
    {
        foreach (var goal in new[] { ParseGoal.FormalParameters, ParseGoal.FunctionBody })
        {
            var request = new ParseRequest(new SourceText("a"), goal, NoTables.Instance, NoValidator.Instance) { FunctionKind = DynamicFunctionKind.AsyncGenerator };
            Assert.Equal(DynamicFunctionKind.AsyncGenerator, request.FunctionKind);
        }

        Assert.Throws<ArgumentException>(() => new ParseRequest(new SourceText("a"), ParseGoal.Script, NoTables.Instance, NoValidator.Instance) { FunctionKind = DynamicFunctionKind.Async });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseRequest(new SourceText("a"), ParseGoal.FunctionBody, NoTables.Instance, NoValidator.Instance) { FunctionKind = (DynamicFunctionKind)4 });
    }

    [Fact]
    public void A_flag_the_library_does_not_define_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseRequest(new SourceText("1"), ParseGoal.Script, NoTables.Instance, NoValidator.Instance) { Proposals = (ProposalFlags)64 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new ParseRequest(new SourceText("1"), ParseGoal.Script, NoTables.Instance, NoValidator.Instance) { Extensions = (LexerExtensions)2 });
        Assert.Throws<ArgumentNullException>(() => new ParseRequest(new SourceText("1"), ParseGoal.Script, NoTables.Instance, NoValidator.Instance) { Limits = null! });
    }

    [Fact]
    public void The_proposal_flags_are_one_per_form_each_its_catalogue_row()
    {
        Assert.Equal(
            [("None", 0), ("Decorators", 1), ("AutoAccessors", 2), ("ImportSourceDeclarations", 4), ("ImportSourceCalls", 8), ("ImportDeferDeclarations", 16), ("ImportDeferCalls", 32)],
            Enum.GetValues<ProposalFlags>().Select(static f => (f.ToString(), (int)f)).ToArray());
        Assert.Equal([("None", 0), ("DecimalSuffix", 1)], Enum.GetValues<LexerExtensions>().Select(static f => (f.ToString(), (int)f)).ToArray());
    }

    [Fact]
    public void A_source_keeps_every_code_unit_and_refuses_a_bad_base()
    {
        var lone = new SourceText("\uFEFF\uD800x");
        Assert.Equal("\uFEFF\uD800x", lone.Text);
        Assert.Equal((string.Empty, 0, 1, 1), (lone.Name, lone.BaseOffset, lone.BaseLine, lone.BaseColumn));
        Assert.Throws<ArgumentNullException>(() => new SourceText(null!));
        Assert.Throws<ArgumentNullException>(() => new SourceText("x", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceText("x", baseOffset: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceText("x", baseLine: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceText("x", baseColumn: 0));
    }

    [Fact]
    public void Visible_private_names_are_copied_and_each_is_a_private_identifier()
    {
        string[] names = ["#a"];
        var context = new InheritedContext { VisiblePrivateNames = names };
        names[0] = "#b";
        Assert.Equal(["#a"], context.VisiblePrivateNames!);
        Assert.Empty(new InheritedContext { VisiblePrivateNames = [] }.VisiblePrivateNames!);
        Assert.Throws<ArgumentException>(() => new InheritedContext { VisiblePrivateNames = ["a"] });
        Assert.Throws<ArgumentException>(() => new InheritedContext { VisiblePrivateNames = ["#"] });
        Assert.Throws<ArgumentException>(() => new InheritedContext { VisiblePrivateNames = [null!] });
    }
}
