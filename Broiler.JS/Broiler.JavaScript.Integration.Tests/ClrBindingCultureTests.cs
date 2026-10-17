using System.Globalization;
using Broiler.JavaScript.Ast.Misc;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Extensions;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.Integration.Tests;

// The CLR binding's names do not read the host culture. Under tr-TR, the current culture's casing lowers `I` to
// dotless `ı`, so a .NET member named `ID` became the script name `ıd`, and a script object's key `id` no longer
// matched the property `ID` when it was unmarshalled.
public class ClrBindingCultureTests
{
    public sealed class Account
    {
        public int ID { get; set; }

        public string? Title { get; set; }
    }

    private static T UnderTurkish<T>(Func<T> body)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
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
        // The control: without it the tests below would pass on a host with no Turkish data.
        Assert.Equal("ıd", UnderTurkish(() => "ID".ToLower()));
    }

    [Fact]
    public void Camel_case_member_names_do_not_read_the_host_culture()
    {
        Assert.Equal("id", UnderTurkish(() => "ID".ToCamelCase()));
        Assert.Equal("iostream", UnderTurkish(() => "IOStream".ToCamelCase()));
        Assert.Equal("iostream", UnderTurkish(() => ClrMemberNamingConvention.CamelCase.Convert(new StringSpan("IOStream"))));
        Assert.Equal("id", UnderTurkish(() => new StringSpan("ID").ToCamelCase()));
        Assert.Equal("index", UnderTurkish(() => new StringSpan("INDEX").ToLower()));
        Assert.Equal("-is-index", UnderTurkish(() => new StringSpan("IsIndex").ToSnakeCase()));
    }

    [Fact]
    public void Unmarshalled_keys_match_properties_without_the_host_culture()
    {
        var account = UnderTurkish(() =>
        {
            using var ctx = new JSContext();
            return (Account)ctx.Eval("({ id: 7, title: 'Iğdır' })").ForceConvert(typeof(Account));
        });

        Assert.Equal(7, account.ID);
        Assert.Equal("Iğdır", account.Title);
    }
}
