using System.Globalization;
using Broiler.JavaScript.BuiltIns.Number;
using Broiler.JavaScript.Engine;

namespace Broiler.JavaScript.Debugger.Tests;

// The debugger's description of a number is the invariant culture's text under every host culture (milestone SJL-9's
// clause 8, on the owner's ruling of 2026-11-11, "Fix all + enforce"). Until that change V8RemoteObject wrote
// `t.ToString()`, the host's culture, so that under de-DE the DevTools protocol described 1.5 as "1,5", and under sv-SE
// -2 as "−2".
public class DebuggerHostCultureTests
{
    private static T UnderCulture<T>(string culture, Func<T> body)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return body();
        }
        finally
        {
            CultureInfo.CurrentCulture = saved;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("de-DE")]
    [InlineData("sv-SE")]
    [InlineData("tr-TR")]
    public void A_number_is_described_with_the_invariant_culture(string culture)
    {
        using var ctx = new JSContext();
        Assert.Equal("1.5", UnderCulture(culture, () => new V8RemoteObject(new JSNumber(1.5)).Description));
        Assert.Equal("-2", UnderCulture(culture, () => new V8RemoteObject(new JSNumber(-2)).Description));
    }
}
