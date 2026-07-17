#nullable enable
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;

namespace BroilerJS;

public readonly record struct AssemblyCodeCacheSnapshot(
    long Hits,
    long Misses,
    long QuarantinedEntries,
    long PersistedPeBytes,
    long PersistedPdbBytes);

/// <summary>
/// Persistent script cache. A versioned manifest is the commit marker, so readers
/// never observe a partially-written PE/PDB pair. Loaded entries live in collectible
/// contexts and corrupt or stale artifacts are quarantined rather than executed.
/// </summary>
public sealed class AssemblyCodeCache : ICodeCache, IDisposable
{
    private const int ManifestSchema = 3;
    private static readonly ConcurrentDictionary<string, object> EntryLocks = new(StringComparer.Ordinal);
    private static readonly Guid Sha256DocumentHashAlgorithm = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private static readonly Guid JavaScriptLanguage = new("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");

    private readonly DirectoryInfo cacheFolder;
    private readonly string engineVersion;
    private readonly List<AssemblyLoadContext> loadContexts = [];
    private long hits;
    private long misses;
    private long quarantinedEntries;
    private long persistedPeBytes;
    private long persistedPdbBytes;
    private bool disposed;

    public AssemblyCodeCache(string path = ".\\cache")
    {
        cacheFolder = new DirectoryInfo(path);
        engineVersion = GetType().Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
    }

    public AssemblyCodeCacheSnapshot Snapshot() => new(
        Interlocked.Read(ref hits),
        Interlocked.Read(ref misses),
        Interlocked.Read(ref quarantinedEntries),
        Interlocked.Read(ref persistedPeBytes),
        Interlocked.Read(ref persistedPdbBytes));

    public JSFunctionDelegate GetOrCreate(in JSCode code)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cacheFolder.Create();

        var cacheKey = ComputeCacheKey(in code);
        var prefix = Path.Combine(cacheFolder.FullName, $"JS{cacheKey}");
        var paths = new EntryPaths(prefix);
        lock (EntryLocks.GetOrAdd(prefix, static _ => new object()))
        {
            using var fileLock = AcquireFileLock(paths.Lock);
            if (TryLoad(in code, paths, out var cached))
            {
                Interlocked.Increment(ref hits);
                return cached;
            }

            Interlocked.Increment(ref misses);
            Quarantine(paths);
            return Create(in code, cacheKey, paths);
        }
    }

    private bool TryLoad(in JSCode code, EntryPaths paths, out JSFunctionDelegate function)
    {
        function = null!;
        if (!File.Exists(paths.Manifest) || !File.Exists(paths.Pe) || !File.Exists(paths.Pdb))
            return false;

        try
        {
            var manifest = JsonSerializer.Deserialize<CacheManifest>(File.ReadAllBytes(paths.Manifest));
            if (manifest is null
                || manifest.Schema != ManifestSchema
                || manifest.EngineVersion != engineVersion
                || manifest.SourceHash != ComputeSourceHash(code.Code.Value)
                || manifest.Location != (code.Location ?? string.Empty)
                || manifest.Options != code.Options.ToString()
                || manifest.PeHash != ComputeFileHash(paths.Pe)
                || manifest.PdbHash != ComputeFileHash(paths.Pdb))
            {
                return false;
            }

            var context = new PersistentScriptLoadContext();
            try
            {
                using var pe = new FileStream(paths.Pe, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var pdb = new FileStream(paths.Pdb, FileMode.Open, FileAccess.Read, FileShare.Read);
                var assembly = context.LoadFromStream(pe, pdb);
                var method = assembly.GetType("JSScript", throwOnError: true)!
                    .GetMethod("Run", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new BadImageFormatException("Cached script entry point was not found.");
                var factory = method.CreateDelegate<Func<JSFunctionDelegate>>();
                function = factory();
                lock (loadContexts)
                    loadContexts.Add(context);
                return true;
            }
            catch
            {
                context.Unload();
                throw;
            }
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or BadImageFormatException
            or FileLoadException
            or TypeLoadException
            or MissingMethodException)
        {
            return false;
        }
    }

    private JSFunctionDelegate Create(in JSCode code, string cacheKey, EntryPaths paths)
    {
        var expression = code.Compiler();
        var assemblyName = $"JS{cacheKey}";
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(assemblyName);
        var type = module.DefineType("JSScript", TypeAttributes.Public);
        var methodBuilder = type.DefineMethod(
            "Run",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(JSFunctionDelegate),
            Type.EmptyTypes);
        var compiled = expression.CompileToStaticMethod(type, methodBuilder, true);
        var method = type.CreateType()!.GetMethod(compiled.Name, BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMethodException("Generated script entry point was not found.");
        var inMemoryFactory = method.CreateDelegate<Func<JSFunctionDelegate>>();

        var generator = new Lokad.ILPack.AssemblyGenerator();
        var pe = generator.GenerateAssemblyBytes(method.DeclaringType!.Assembly);
        var sourceHash = ComputeSourceHash(code.Code.Value);
        var pdb = GeneratePortablePdb(pe, code.Location, sourceHash);
        var manifest = new CacheManifest(
            ManifestSchema,
            engineVersion,
            sourceHash,
            code.Location ?? string.Empty,
            code.Options.ToString(),
            Convert.ToHexString(SHA256.HashData(pe)),
            Convert.ToHexString(SHA256.HashData(pdb)));

        try
        {
            WriteAtomically(paths.Pe, pe);
            WriteAtomically(paths.Pdb, pdb);
            WriteAtomically(paths.Manifest, JsonSerializer.SerializeToUtf8Bytes(manifest));
            Interlocked.Add(ref persistedPeBytes, pe.Length);
            Interlocked.Add(ref persistedPdbBytes, pdb.Length);

            // Load the persisted bytes once as an integrity check and to keep the
            // first-hit and cold-hit execution paths identical.
            if (TryLoad(in code, paths, out var persisted))
                return persisted;
        }
        catch (IOException)
        {
            // A concurrent process may have committed the same deterministic entry.
            if (TryLoad(in code, paths, out var winner))
                return winner;
        }

        Quarantine(paths);
        return inMemoryFactory();
    }

    private string ComputeCacheKey(in JSCode code)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendText(hash, $"broiler-persistent-cache-v{ManifestSchema}\n");
        AppendText(hash, engineVersion);
        AppendText(hash, "\n");
        AppendText(hash, code.Options.ToString());
        AppendText(hash, "\n");
        AppendText(hash, code.Location ?? string.Empty);
        AppendText(hash, "\n");
        if (code.Arguments is not null)
        {
            foreach (var argument in code.Arguments)
            {
                AppendText(hash, argument);
                AppendText(hash, "\0");
            }
        }
        AppendText(hash, "\n");
        AppendText(hash, code.Code.Value);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static void AppendText(IncrementalHash hash, string value)
    {
        if (value.Length == 0)
            return;

        var maximum = Encoding.UTF8.GetMaxByteCount(value.Length);
        var rented = ArrayPool<byte>.Shared.Rent(maximum);
        try
        {
            var count = Encoding.UTF8.GetBytes(value.AsSpan(), rented);
            hash.AppendData(rented.AsSpan(0, count));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static string ComputeSourceHash(string source)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        AppendText(hash, source);
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string ComputeFileHash(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static byte[] GeneratePortablePdb(byte[] peImage, string? sourcePath, string sourceHash)
    {
        using var peStream = new MemoryStream(peImage, writable: false);
        using var peReader = new PEReader(peStream);
        var reader = peReader.GetMetadataReader();
        var metadata = new MetadataBuilder();
        var document = metadata.AddDocument(
            metadata.GetOrAddDocumentName(string.IsNullOrWhiteSpace(sourcePath) ? "script.js" : sourcePath),
            metadata.GetOrAddGuid(Sha256DocumentHashAlgorithm),
            metadata.GetOrAddBlob(Convert.FromHexString(sourceHash)),
            metadata.GetOrAddGuid(JavaScriptLanguage));

        var methodCount = reader.GetTableRowCount(TableIndex.MethodDef);
        for (var i = 0; i < methodCount; i++)
            metadata.AddMethodDebugInformation(document, default);

        var rowCounts = ImmutableArray.CreateBuilder<int>(64);
        for (var i = 0; i < 64; i++)
        {
            try
            {
                rowCounts.Add(reader.GetTableRowCount((TableIndex)i));
            }
            catch (ArgumentOutOfRangeException)
            {
                rowCounts.Add(0);
            }
        }

        var builder = new PortablePdbBuilder(metadata, rowCounts.MoveToImmutable(), default);
        var blob = new BlobBuilder();
        builder.Serialize(blob);
        return blob.ToArray();
    }

    private static FileStream AcquireFileLock(string path)
    {
        IOException? lastError = null;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException exception)
            {
                lastError = exception;
                Thread.Sleep(20);
            }
        }
        throw new IOException($"Could not acquire persistent cache lock '{path}'.", lastError);
    }

    private static void WriteAtomically(string destination, byte[] bytes)
    {
        var temporary = $"{destination}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, destination, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private void Quarantine(EntryPaths paths)
    {
        var moved = false;
        foreach (var artifact in new[] { paths.Manifest, paths.Pe, paths.Pdb })
        {
            if (!File.Exists(artifact))
                continue;

            try
            {
                File.Move(artifact, $"{artifact}.bad-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");
                moved = true;
            }
            catch (IOException)
            {
            }
        }
        if (moved)
            Interlocked.Increment(ref quarantinedEntries);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        lock (loadContexts)
        {
            foreach (var context in loadContexts)
                context.Unload();
            loadContexts.Clear();
        }
    }

    private sealed class PersistentScriptLoadContext() : AssemblyLoadContext(isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }

    private sealed record CacheManifest(
        int Schema,
        string EngineVersion,
        string SourceHash,
        string Location,
        string Options,
        string PeHash,
        string PdbHash);

    private readonly record struct EntryPaths(string Prefix)
    {
        public string Manifest => Prefix + ".manifest.json";
        public string Pe => Prefix + ".dll";
        public string Pdb => Prefix + ".pdb";
        public string Lock => Prefix + ".lock";
    }
}
