using System.Reflection;
using Broiler.JavaScript.Parser;
using Broiler.JavaScript.Runtime;

namespace Broiler.JavaScript.BuiltIns.Tests;

/// <summary>
/// The Unicode area's references (milestone SJL-7.5, decision SJL-D-6 (c)): the engine's assemblies call the
/// owner's code packages, and no longer the loose <c>Broiler.Unicode.Properties</c> or the emoji package,
/// which still arrive in the package closure through Broiler.Regex until milestone SJL-11. A compile-time
/// reference nothing uses is not written into an assembly's metadata, so its referenced assemblies say what
/// its code calls.
/// </summary>
public class UnicodeDependencyTests
{
    private static readonly string[] Retired = ["Broiler.Unicode.Properties", "UnicodeEmoji.StringProperties"];

    private static readonly string[] CarriesTables = ["Broiler.Unicode.Tables"];

    public static TheoryData<string> EngineAssemblies => new()
    {
        "Broiler.JavaScript.Parser",
        "Broiler.JavaScript.Runtime",
        "Broiler.JavaScript.BuiltIns",
    };

    private static Assembly Load(string name) => name switch
    {
        "Broiler.JavaScript.Parser" => typeof(FastParser).Assembly,
        "Broiler.JavaScript.Runtime" => typeof(JSValue).Assembly,
        _ => typeof(RegExp.JSRegExp).Assembly,
    };

    private static HashSet<string> References(string name)
        => Load(name).GetReferencedAssemblies().Select(static reference => reference.Name!).ToHashSet(StringComparer.Ordinal);

    [Theory]
    [MemberData(nameof(EngineAssemblies))]
    public void No_engine_assembly_calls_the_retired_Unicode_packages(string name)
    {
        var references = References(name);
        foreach (var retired in Retired.Concat(CarriesTables))
            Assert.DoesNotContain(retired, references);
    }

    [Fact]
    public void The_runtime_calls_the_owner_s_algorithms()
        => Assert.Contains("Broiler.Unicode.Algorithms", References("Broiler.JavaScript.Runtime"));
}
