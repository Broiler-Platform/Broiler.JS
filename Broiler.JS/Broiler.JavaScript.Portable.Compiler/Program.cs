using System;
using System.IO;
using System.Text;
using Broiler.JavaScript.Portable;

if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: portable-compiler <source.js> <output.cs> <namespace> <type>");
    return 2;
}

var source = File.ReadAllText(args[0], Encoding.UTF8);
var program = new PortableCompiler().Compile(source);
var generated = PortableCompiler.EmitCSharp(program, args[2], args[3]);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
File.WriteAllText(args[1], generated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
return 0;
