using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace Broiler.JavaScript.Engine.Benchmarks;

internal static class AssemblyMetrics
{
    public static void Write(string path)
    {
        using var stream = File.OpenRead(path);
        using var peReader = new PEReader(stream);
        if (!peReader.HasMetadata)
            throw new InvalidDataException($"'{path}' is not a managed PE file.");

        var metadata = peReader.GetMetadataReader();
        long ilBytes = 0;
        var methodsWithBodies = 0;
        foreach (var methodHandle in metadata.MethodDefinitions)
        {
            var method = metadata.GetMethodDefinition(methodHandle);
            if (method.RelativeVirtualAddress == 0)
                continue;

            ilBytes += peReader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes().Length;
            methodsWithBodies++;
        }

        var result = new
        {
            path = Path.GetFullPath(path),
            fileBytes = stream.Length,
            typeDefinitions = metadata.TypeDefinitions.Count,
            methodDefinitions = metadata.MethodDefinitions.Count,
            methodsWithBodies,
            ilBytes,
            metadataBytes = peReader.PEHeaders.MetadataSize,
        };
        Console.WriteLine(JsonSerializer.Serialize(result));
    }
}
