using System.Runtime.CompilerServices;
using Broiler.JavaScript.Portable;
using Broiler.JavaScript.NativeAotSample;

var result = PortableInterpreter.Execute(FibonacciProgram.Value, [35]);
var dynamicCodeSupported = RuntimeFeature.IsDynamicCodeSupported;
var dynamicCodeCompiled = RuntimeFeature.IsDynamicCodeCompiled;
Console.WriteLine(
    $"{{\"program\":\"{FibonacciProgram.Value.Name}\",\"result\":{result}," +
    $"\"dynamicCodeSupported\":{dynamicCodeSupported.ToString().ToLowerInvariant()}," +
    $"\"dynamicCodeCompiled\":{dynamicCodeCompiled.ToString().ToLowerInvariant()}}}");
return result == 9_227_465 && !dynamicCodeSupported && !dynamicCodeCompiled ? 0 : 1;
