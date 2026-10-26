using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.ExpressionCompiler;
using Broiler.JavaScript.Runtime;
using BroilerJS;

namespace Broiler.JavaScript.Compiler.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class Phase3DiagnosticsCollection
{
    public const string Name = "Phase 3 diagnostics";
}

[Collection(Phase3DiagnosticsCollection.Name)]
public sealed class Phase3CompilerSpecializationTests
{
    [Fact(Timeout = 600000)]
    public void ScalarReplacement_UsesRawLocals_OnlyWhenBindingsAreUnobservable()
    {
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();

        var result = context.Eval("(function (a) { var x = a + 1; var y = x * 2; return y; })(20);");

        Assert.Equal(42, result.IntValue);
        Assert.True(CompilerSpecializationDiagnostics.Snapshot().ScalarLocals >= 2);

        CompilerSpecializationDiagnostics.Reset();
        var guarded = context.Eval("""
            var a = (function () { var x = 1; eval('x = 2'); return x; })();
            var c = (function () { var x = 4; with ({}) { x = x; } return x; })();
            var d = (function () { var x = 1; var ignored = eval('x = 2'); return x; })();
            [a, c, d].join('|');
            """);

        Assert.Equal("2|4|2", guarded.ToString());
        Assert.Equal(0, CompilerSpecializationDiagnostics.Snapshot().ScalarLocals);

        // A closure was the fourth case here, and item 3-7 moved it: a captured CLR local
        // becomes the `Box<double>` every closure over it reads through, so the box IS the
        // shared cell and capture is no longer a reason to refuse. It reaches the NUMERIC tier
        // only — the JSValue tier still declines it, because that tier has no cell at all and a
        // cell is what an eval-introduced `delete` and a TDZ need. The value is the assertion
        // that matters and it holds either way.
        CompilerSpecializationDiagnostics.Reset();
        var captured = context.Eval("(function () { var x = 3; return function () { return x; }; })()();");

        Assert.Equal("3", captured.ToString());
        var capturedCounts = CompilerSpecializationDiagnostics.Snapshot();
        Assert.Equal(CapturedNumericLocals.Enabled ? 1 : 0, capturedCounts.NumericLocals);
        Assert.Equal(capturedCounts.NumericLocals, capturedCounts.ScalarLocals);
    }

    [Fact(Timeout = 600000)]
    public void ScalarReplacement_DoesNotCrossNestedDirectEvalShadowBoundaries()
    {
        using var context = new JSContext();
        CompilerSpecializationDiagnostics.Reset();
        var result = context.Eval("""
            function t() {
              var x = 0;
              var innerX = (function () { x = (eval('var x = 2;'), 1); return x; })();
              return innerX + '|' + x;
            }
            t();
            """);

        Assert.Equal(0, CompilerSpecializationDiagnostics.Snapshot().ScalarLocals);
        Assert.Equal("2|1", result.ToString());
    }

    [Fact(Timeout = 600000)]
    public void DenseIntegerAndStringSwitches_UseBoundedDispatchTables()
    {
        using var context = new JSContext();
        ILSpecializationDiagnostics.Reset();

        var result = context.Eval("""
            function dense(v) {
              switch (v) {
                case 10: return 'ten';
                case 11: return 'eleven';
                case 12: return 'twelve';
                case 13: return 'thirteen';
                default: return 'miss';
              }
            }
            function text(v) {
              switch (v) {
                case 'aa': return 1;
                case 'bb': return 2;
                case 'cc': return 3;
                case 'dd': return 4;
                default: return 0;
              }
            }
            [dense(10), dense(13), dense(99), dense('10'), text('aa'), text('dd'), text('zz')].join('|');
            """);

        Assert.Equal("ten|thirteen|miss|miss|1|4|0", result.ToString());
        var snapshot = ILSpecializationDiagnostics.Snapshot();
        Assert.True(snapshot.DenseIntegerSwitches >= 1);
        Assert.True(snapshot.StringHashSwitches >= 1);
        Assert.InRange(snapshot.SwitchTableSlots, 8, 32);
    }

    [Fact(Timeout = 600000)]
    public void SwitchSpecialization_PreservesDuplicateDefaultFallthroughAndGenericFallbacks()
    {
        using var context = new JSContext();
        ILSpecializationDiagnostics.Reset();

        var result = context.Eval("""
            function duplicate(v) {
              switch (v) {
                case 1: return 'first';
                case 1: return 'second';
                case 2: return 'two';
                case 3: return 'three';
              }
            }
            function fall(v) {
              var r = '';
              switch (v) {
                case 1: r += 'a';
                default: r += 'd';
                case 2: r += 'b'; break;
                case 3: r += 'c'; break;
                case 4: r += 'e';
              }
              return r;
            }
            function sparse(v) {
              switch (v) { case 1: return 1; case 1000: return 2; case 2000: return 3; default: return 0; }
            }
            [duplicate(1), fall(1), fall(9), sparse(1000), sparse('1000')].join('|');
            """);

        Assert.Equal("first|adb|db|2|0", result.ToString());
        var snapshot = ILSpecializationDiagnostics.Snapshot();
        Assert.True(snapshot.DenseIntegerSwitches >= 2);
        // The sparse three-way switch remains on the comparison slow path and
        // therefore contributes no oversized table.
        Assert.InRange(snapshot.SwitchTableSlots, 6, 16);
    }

    [Fact(Timeout = 600000)]
    public void PropertyCaches_PromoteBoundedly_AndKeepDictionaryAndProxySlowPaths()
    {
        using var context = new JSContext();
        using var recording = PropertyOptimizationDiagnostics.Enable();
        PropertyOptimizationDiagnostics.Reset();

        var result = context.Eval("""
            var traps = 0;
            function read(o) { return o.x; }
            var a = { x: 1 };
            var b = { a: 0, x: 2 };
            var c = { a: 0, b: 0, x: 3 };
            var d = { a: 0, b: 0, c: 0, x: 4 };
            var e = { a: 0, b: 0, c: 0, d: 0, x: 5 };
            var sum = 0;
            for (var i = 0; i < 8; i++) sum += read(a);
            sum += read(b) + read(c) + read(d) + read(e);
            delete a.x;
            a.x = 9;
            sum += read(a);
            var proxy = new Proxy({ x: 7 }, { get: function (target, key) { traps++; return target[key]; } });
            sum += read(proxy) + read(proxy);
            sum + '|' + traps;
            """);

        Assert.Equal("45|2", result.ToString());
        var snapshot = PropertyOptimizationDiagnostics.Snapshot();
        Assert.True(snapshot.ShapeTransitions > 0);
        Assert.True(snapshot.CacheHits > 0);
        Assert.True(snapshot.CacheMisses > 0);
        Assert.True(snapshot.PolymorphicPromotions > 0);
        Assert.True(snapshot.MegamorphicSites > 0);
        Assert.True(snapshot.DictionaryFallbacks > 0);
    }

    [Fact(Timeout = 600000)]
    public void PropertyCaches_RemainReadOnlyAcrossAllMemberWriteForms()
    {
        using var context = new JSContext();
        var result = context.Eval("""
            var o = {};
            o.x = 1;
            o[true] = 2;
            o[9007199254740990] = 3;
            [o.a, ...o.rest] = [4, 5, 6];
            for (o.key in { first: 1, second: 2 }) {}
            [o.x, o.true, o[9007199254740990], o.a, o.rest.join(','), o.key].join('|');
            """);

        Assert.Equal("1|2|3|4|5,6|second", result.ToString());
    }

    [Fact(Timeout = 600000)]
    public void PrototypeMutations_AdvanceTheVersion_AndInvalidateSafely()
    {
        using var context = new JSContext();
        using var recording = PropertyOptimizationDiagnostics.Enable();
        PropertyOptimizationDiagnostics.Reset();
        var before = PropertyOptimizationDiagnostics.Snapshot();

        var result = context.Eval("""
            var parent = { x: 1 };
            var child = Object.create(parent);
            function read() { return child.x; }
            var first = read();
            parent.x = 2;
            var second = read();
            Object.setPrototypeOf(child, { x: 3 });
            [first, second, read()].join('|');
            """);

        Assert.Equal("1|2|3", result.ToString());
        var after = PropertyOptimizationDiagnostics.Snapshot();
        Assert.True(after.PrototypeVersion > before.PrototypeVersion);
        Assert.True(after.PrototypeInvalidations > before.PrototypeInvalidations);
    }

    [Fact(Timeout = 600000)]
    public void PersistentCache_CommitsPeManifestAndPortablePdb_ThenHitsCold()
    {
        var folder = Path.Combine(Path.GetTempPath(), "broiler-phase3-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var cache = new AssemblyCodeCache(folder))
            using (var context = new JSContext { CodeCache = cache })
            {
                Assert.Equal(42, context.Eval("21 * 2", "phase3-cache.js").IntValue);
                var snapshot = cache.Snapshot();
                Assert.Equal(1, snapshot.Misses);
                Assert.True(snapshot.PersistedPeBytes > 0);
                Assert.True(snapshot.PersistedPdbBytes > 0);
            }

            var pdbPath = Assert.Single(Directory.GetFiles(folder, "*.pdb"));
            using (var stream = File.OpenRead(pdbPath))
            using (var provider = MetadataReaderProvider.FromPortablePdbStream(stream))
                Assert.True(provider.GetMetadataReader().Documents.Count > 0);

            using (var cache = new AssemblyCodeCache(folder))
            using (var context = new JSContext { CodeCache = cache })
            {
                Assert.Equal(42, context.Eval("21 * 2", "phase3-cache.js").IntValue);
                Assert.Equal(1, cache.Snapshot().Hits);
            }

            var pePath = Assert.Single(Directory.GetFiles(folder, "*.dll"));
            File.WriteAllBytes(pePath, [0, 1, 2, 3]);
            using (var cache = new AssemblyCodeCache(folder))
            using (var context = new JSContext { CodeCache = cache })
            {
                Assert.Equal(42, context.Eval("21 * 2", "phase3-cache.js").IntValue);
                Assert.True(cache.Snapshot().QuarantinedEntries > 0);
            }
            Assert.NotEmpty(Directory.GetFiles(folder, "*.bad-*"));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact(Timeout = 600000)]
    public void PersistentCache_DoesNotLoadAnEntryWrittenUnderAnotherEngine()
    {
        var folder = Path.Combine(Path.GetTempPath(), "broiler-phase3-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var cache = new AssemblyCodeCache(folder, "engine-a"))
            using (var context = new JSContext { CodeCache = cache })
                Assert.Equal(42, context.Eval("21 * 2", "phase3-engine.js").IntValue);
            var entryA = Path.GetFileNameWithoutExtension(Assert.Single(Directory.GetFiles(folder, "*.dll")));

            // Engine B keys the script apart from engine A, so it compiles the script rather
            // than open engine A's entry, and commits its own entry beside it.
            using (var cache = new AssemblyCodeCache(folder, "engine-b"))
            using (var context = new JSContext { CodeCache = cache })
            {
                Assert.Equal(42, context.Eval("21 * 2", "phase3-engine.js").IntValue);
                var snapshot = cache.Snapshot();
                Assert.Equal(0, snapshot.Hits);
                Assert.Equal(1, snapshot.Misses);
                Assert.Equal(0, snapshot.QuarantinedEntries);
            }
            var entryB = Path.GetFileNameWithoutExtension(Assert.Single(
                Directory.GetFiles(folder, "*.dll"),
                path => Path.GetFileNameWithoutExtension(path) != entryA));

            // Engine A's entry where engine B looks for the script: its manifest names engine A,
            // so engine B quarantines it rather than load it.
            foreach (var extension in new[] { ".manifest.json", ".dll", ".pdb" })
                File.Copy(Path.Combine(folder, entryA + extension), Path.Combine(folder, entryB + extension), overwrite: true);
            using (var cache = new AssemblyCodeCache(folder, "engine-b"))
            using (var context = new JSContext { CodeCache = cache })
            {
                Assert.Equal(42, context.Eval("21 * 2", "phase3-engine.js").IntValue);
                var snapshot = cache.Snapshot();
                Assert.Equal(0, snapshot.Hits);
                Assert.Equal(1, snapshot.QuarantinedEntries);
            }
            Assert.Equal(3, Directory.GetFiles(folder, entryB + ".*.bad-*").Length);

            using (var cache = new AssemblyCodeCache(folder, "engine-a"))
            using (var context = new JSContext { CodeCache = cache })
            {
                Assert.Equal(42, context.Eval("21 * 2", "phase3-engine.js").IntValue);
                Assert.Equal(1, cache.Snapshot().Hits);
            }
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact(Timeout = 600000)]
    public void PersistentCache_EngineCoversEveryEngineAssemblyAnEntryCalls()
    {
        var folder = Path.Combine(Path.GetTempPath(), "broiler-phase3-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var cache = new AssemblyCodeCache(folder))
            using (var context = new JSContext { CodeCache = cache })
            {
                Assert.Equal(42, context.Eval("var a = 41; eval('a + 1')", "phase3-engine.js").IntValue);
                Assert.Equal("1,2", context.Eval("function* g() { yield 1; yield 2; } [...g()].join(',')", "phase3-engine-generator.js").ToString());
            }

            var called = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pePath in Directory.GetFiles(folder, "*.dll"))
            {
                using var stream = File.OpenRead(pePath);
                using var pe = new PEReader(stream);
                var metadata = pe.GetMetadataReader();
                foreach (var handle in metadata.AssemblyReferences)
                    called.Add(metadata.GetString(metadata.GetAssemblyReference(handle).Name));
            }

            // The direct eval is a call of the compiler's DirectEvalSupport.Execute, which the
            // engine loads by name, so no reference from the host leads to the compiler.
            Assert.Contains("Broiler.JavaScript.Compiler", called);
            Assert.Contains("Broiler.JavaScript.Runtime", called);
            Assert.Contains("Broiler.JavaScript.LinqExpressions", called);
            var modules = AssemblyCodeCache.EngineModules;
            foreach (var name in called.Where(name => name.StartsWith("Broiler.", StringComparison.Ordinal)))
            {
                var loaded = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == name);
                Assert.True(modules.TryGetValue(name, out var moduleVersionId), name);
                Assert.Equal(loaded.ManifestModule.ModuleVersionId, moduleVersionId);
            }
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    // Each script's compiled code touches an internal engine member (`JSVariable.IsReadOnly`,
    // `JSObjectStatic.RequireObjectCoercible`, `JSValue.BooleanTrue`). The in-memory path reaches
    // them from dynamic methods that skip visibility checks; a persisted entry reaches them only
    // because it names the assemblies it references in an `IgnoresAccessChecksToAttribute`, and
    // without that each of these failed with a FieldAccessException or MethodAccessException.
    [Theory(Timeout = 600000)]
    [InlineData("class A { #x = 1; get x() { return this.#x; } static s() { return new A().x; } } A.s();", "1")]
    [InlineData("var o = { a: 1, ...{ b: 2 } }; var { a, ...r } = o; a + '|' + JSON.stringify(r);", "1|{\"b\":2}")]
    [InlineData("try { throw new Error('e'); } catch ({ message }) { message }", "e")]
    [InlineData("var s = ''; for (let i = 0; i < 3; i++) { s += (() => i)(); } s;", "012")]
    [InlineData("var log = ''; { using d = { [Symbol.dispose]() { log += 'disposed'; } }; } log;", "disposed")]
    public void PersistentCache_RunsCodeThatReachesEngineInternals_FromAColdEntry(string script, string expected)
    {
        var folder = Path.Combine(Path.GetTempPath(), "broiler-phase3-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var cache = new AssemblyCodeCache(folder))
            using (var context = new JSContext { CodeCache = cache })
            {
                Assert.Equal(expected, context.Eval(script, "phase3-cache-internals.js").ToString());
                Assert.Equal(1, cache.Snapshot().Misses);
            }

            using (var cache = new AssemblyCodeCache(folder))
            using (var context = new JSContext { CodeCache = cache })
            {
                Assert.Equal(expected, context.Eval(script, "phase3-cache-internals.js").ToString());
                var snapshot = cache.Snapshot();
                Assert.Equal(1, snapshot.Hits);
                Assert.Equal(0, snapshot.Misses);
            }
            Assert.Empty(Directory.GetFiles(folder, "*.bad-*"));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }
}
