using System.Reflection.Metadata;
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
    [Fact]
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
            var b = (function () { var x = 3; return function () { return x; }; })()();
            var c = (function () { var x = 4; with ({}) { x = x; } return x; })();
            var d = (function () { var x = 1; var ignored = eval('x = 2'); return x; })();
            [a, b, c, d].join('|');
            """);

        Assert.Equal("2|3|4|2", guarded.ToString());
        Assert.Equal(0, CompilerSpecializationDiagnostics.Snapshot().ScalarLocals);
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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

    [Fact]
    public void PropertyCaches_PromoteBoundedly_AndKeepDictionaryAndProxySlowPaths()
    {
        using var context = new JSContext();
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

    [Fact]
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

    [Fact]
    public void PrototypeMutations_AdvanceTheVersion_AndInvalidateSafely()
    {
        using var context = new JSContext();
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

    [Fact]
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
}
