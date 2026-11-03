using System.Reflection;
using Broiler.JavaScript.Runtime;
using Xunit;

namespace Broiler.JavaScript.Architecture.Tests;

/// <summary>
/// The compiler's test-only switch of milestone SJL-14.2 (Broiler.VM's JSD-0083, decision D43) is off in the build
/// that ships: <c>JSCompilationOptions.BridgeByDefault</c>, true only in a build given
/// <c>-p:BroilerParseThroughBridge=true</c> (<c>Broiler.JS/Directory.Build.props</c>), is false here.
/// </summary>
/// <remarks>
/// A build with the switch on for every compile fails this test by design: that build is the switch-on test pass,
/// whose differences are recorded, not a product build. Window W2 removes the switch and this test.
/// </remarks>
public sealed class BridgeSwitchTests
{
    [Fact]
    public void The_compilers_test_only_switch_is_off_in_this_build()
    {
        var field = typeof(JSCompilationOptions).GetField("BridgeByDefault", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field);
        Assert.False((bool)field!.GetRawConstantValue()!,
            "Broiler.JavaScript.Runtime was built with BROILER_PARSE_THROUGH_BRIDGE: every compile parses through the bridge. Build without -p:BroilerParseThroughBridge=true.");
        Assert.False(new JSCompilationOptions().ParsesThroughBridgeForTest());
    }
}

internal static class JSCompilationOptionsProbe
{
    // The switch as the compiler reads it, through reflection: it is internal to Broiler.JavaScript.Runtime.
    internal static bool ParsesThroughBridgeForTest(this JSCompilationOptions options)
        => (bool)typeof(JSCompilationOptions).GetProperty("ParsesThroughBridge", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(options)!;
}
