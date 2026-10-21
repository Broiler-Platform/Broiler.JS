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
/// <remarks>
/// Since milestone SJL-12.5 the tables and their adapter are <c>Broiler.JavaScript.Ast</c>'s, which the parser
/// and the runtime both reference (Broiler.VM's JSD-0080, decision D20), so it is <c>Ast</c> that calls the
/// owner's algorithms; the runtime no longer does.
/// </remarks>
public class UnicodeDependencyTests
{
    private static readonly string[] Retired = ["Broiler.Unicode.Properties", "UnicodeEmoji.StringProperties"];

    private static readonly string[] CarriesTables = ["Broiler.Unicode.Tables"];

    public static TheoryData<string> EngineAssemblies => new()
    {
        "Broiler.JavaScript.Ast",
        "Broiler.JavaScript.Parser",
        "Broiler.JavaScript.Runtime",
        "Broiler.JavaScript.BuiltIns",
    };

    private static Assembly Load(string name) => name switch
    {
        "Broiler.JavaScript.Ast" => typeof(Broiler.JavaScript.Ast.AstNode).Assembly,
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

    // The assembly that holds the tables calls the owner's algorithms over them: Ast's since milestone SJL-12.5,
    // where it was the runtime's.
    [Fact]
    public void The_assembly_holding_the_tables_calls_the_owner_s_algorithms()
        => Assert.Contains("Broiler.Unicode.Algorithms", References("Broiler.JavaScript.Ast"));

    // Segmentation is the built-ins' alone: Ast, the parser and the runtime do not carry it.
    [Fact]
    public void The_built_ins_alone_call_the_owner_s_segmenter()
    {
        Assert.Contains("Broiler.Unicode.Segmentation", References("Broiler.JavaScript.BuiltIns"));
        Assert.DoesNotContain("Broiler.Unicode.Segmentation", References("Broiler.JavaScript.Ast"));
        Assert.DoesNotContain("Broiler.Unicode.Segmentation", References("Broiler.JavaScript.Runtime"));
        Assert.DoesNotContain("Broiler.Unicode.Segmentation", References("Broiler.JavaScript.Parser"));
    }
}
