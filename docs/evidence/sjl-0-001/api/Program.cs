using System.Reflection;
using PublicApiGenerator;

// args: <output directory> <assembly name>...
var output = args[0];
foreach (var name in args.Skip(1))
{
    var path = Path.Combine(AppContext.BaseDirectory, name + ".dll");
    var text = Assembly.LoadFrom(path).GeneratePublicApi(new ApiGeneratorOptions { IncludeAssemblyAttributes = false });
    File.WriteAllText(Path.Combine(output, name + ".txt"), text.ReplaceLineEndings("\n") + "\n");
}
