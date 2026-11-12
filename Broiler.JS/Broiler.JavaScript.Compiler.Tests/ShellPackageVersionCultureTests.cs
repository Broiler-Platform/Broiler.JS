using System.Globalization;
using BroilerJS.Utils;

namespace Broiler.JavaScript.Compiler.Tests;

// The shell lowers a `#r "nuget: Name, Version"` reference's version with the invariant culture's casing under every host
// culture (milestone SJL-9's clause 8, on the owner's ruling of 2026-11-11, "Fix all + enforce"). Until that change it read
// `ToLower()`, the host's casing, so that under tr-TR `1.0.0-PREVIEW` became `1.0.0-prevıew`, with a dotless i.
public class ShellPackageVersionCultureTests
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

    [Fact]
    public void The_host_culture_would_lower_I_to_dotless_i()
    {
        // The control: without it the test below would pass on a host with no Turkish data.
        Assert.Equal("prevıew", UnderCulture("tr-TR", () => "PREVIEW".ToLower()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("de-DE")]
    [InlineData("sv-SE")]
    [InlineData("tr-TR")]
    public void A_package_version_is_lowered_with_the_invariant_culture(string culture)
    {
        var configuration = UnderCulture(culture, () => new Loader.ExtensionConfiguration("nuget: Some.Package, 1.0.0-PREVIEW.1"));
        Assert.Equal("Some.Package", configuration.Package);
        Assert.Equal("1.0.0-preview.1", configuration.Version);
    }
}
