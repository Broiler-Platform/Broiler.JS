using Broiler.Unicode.Algorithms;

namespace Broiler.JavaScript.Parser.Tests;

// The parser's explicit options (milestone SJL-12.5 item 1; Broiler.VM's JSD-0080, decision D19): an
// immutable value with the goal, the top-level-await permission and the Unicode provider.
public class FastParserOptionsTests
{
    [Fact]
    public void The_script_options_are_the_default_and_the_module_options_allow_top_level_await()
    {
        Assert.Equal(FastParserGoal.Script, FastParserOptions.Script.Goal);
        Assert.False(FastParserOptions.Script.AllowTopLevelAwait);
        Assert.Null(FastParserOptions.Script.UnicodeTables);
        Assert.Equal(FastParserOptions.Script, new FastParserOptions());

        Assert.Equal(FastParserGoal.Module, FastParserOptions.Module.Goal);
        Assert.True(FastParserOptions.Module.AllowTopLevelAwait);
        Assert.Null(FastParserOptions.Module.UnicodeTables);
    }

    [Fact]
    public void Options_are_equal_by_value()
    {
        var module = new FastParserOptions { Goal = FastParserGoal.Module, AllowTopLevelAwait = true };
        Assert.Equal(FastParserOptions.Module, module);
        Assert.Equal(FastParserOptions.Module.GetHashCode(), module.GetHashCode());
        Assert.NotSame(FastParserOptions.Module, module);

        Assert.NotEqual(FastParserOptions.Module, FastParserOptions.Module with { AllowTopLevelAwait = false });
        Assert.NotEqual(FastParserOptions.Script, FastParserOptions.Script with { AllowTopLevelAwait = true });
        Assert.Equal(FastParserOptions.Script, FastParserOptions.Module with { Goal = FastParserGoal.Script, AllowTopLevelAwait = false });
    }

    [Theory]
    [InlineData(FastParserGoal.Eval)]
    [InlineData(FastParserGoal.FormalParameters)]
    [InlineData(FastParserGoal.FunctionBody)]
    public void The_reserved_goals_are_refused(FastParserGoal goal)
    {
        var refusal = Assert.Throws<NotSupportedException>(() => new FastParserOptions { Goal = goal });
        Assert.Contains("reserved", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_value_that_is_no_goal_is_refused()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new FastParserOptions { Goal = (FastParserGoal)17 });

    [Fact]
    public void A_provider_the_lexical_classes_cannot_read_is_refused_when_the_options_are_made()
        => Assert.Throws<ArgumentException>(() => new FastParserOptions { UnicodeTables = new NoTables() });

    // A provider of another Unicode version that carries no table.
    private sealed class NoTables : IUnicodeTables
    {
        public string UnicodeVersion => "1.1.0";

        public ReadOnlySpan<byte> Table(UnicodeTable table) => [];
    }
}
