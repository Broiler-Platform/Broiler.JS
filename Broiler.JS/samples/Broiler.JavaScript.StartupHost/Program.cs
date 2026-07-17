using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Broiler.JavaScript.BuiltIns;
using Broiler.JavaScript.Engine;
using Broiler.JavaScript.Runtime;
var process = Process.GetCurrentProcess();
var processToMainMilliseconds = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalMilliseconds;
var loaded = new HashSet<string>(
    AppDomain.CurrentDomain.GetAssemblies().Select(static assembly => assembly.GetName().Name ?? string.Empty),
    StringComparer.Ordinal);
AppDomain.CurrentDomain.AssemblyLoad += (_, e) =>
    loaded.Add(e.LoadedAssembly.GetName().Name ?? string.Empty);

var beforeAllocated = GC.GetTotalAllocatedBytes(precise: true);
var stopwatch = Stopwatch.StartNew();
var builder = JavaScriptBootstrap.CreateContextBuilder();

#if BROILER_FULL_HOST
const string profileName = "full";
var registry = JavaScriptBootstrap.Compose(
    DefaultBuiltInRegistry.Instance,
    new DeferredSampleFeatureRegistration());
var profile = new JavaScriptBootstrapProfile(
    "full-with-sample-satellite",
    BuiltInFeatures.All,
    BuiltInFeatures.Intl | BuiltInFeatures.Temporal | BuiltInFeatures.HostExtensions,
    isConformant: true);
builder.UseBuiltInRegistry(registry).UseProfile(profile);
#else
const string profileName = "minimal";
builder
    .UseBuiltInRegistry(DefaultBuiltInRegistry.Instance)
    .UseProfile(JavaScriptBootstrapProfile.Minimal);
#endif

using var context = builder.Build();
stopwatch.Stop();
var contextMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
var contextAllocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - beforeAllocated;

stopwatch.Restart();
var coreResult = context.Eval("[1,2,3].map(function(x){return x*2;}).join(',')").ToString();
stopwatch.Stop();
var firstScriptMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

string featureResult;
const string sampleFeatureAssemblyName = "Broiler.JavaScript.Feature.Sample";
var featureAssemblyLoadedBeforeUse = loaded.Contains(sampleFeatureAssemblyName);
stopwatch.Restart();
#if BROILER_FULL_HOST
featureResult = context.Eval("Intl.NumberFormat('en').format(42) + '|' + typeof Temporal.Instant + '|' + typeof sampleFeature").ToString();
#else
featureResult = context.Eval("typeof Intl + '|' + typeof Temporal").ToString();
#endif
stopwatch.Stop();
var featureAssemblyLoadedAfterUse = loaded.Contains(sampleFeatureAssemblyName);

process.Refresh();
var assemblyBytes = AppDomain.CurrentDomain.GetAssemblies()
    .Select(static assembly => assembly.Location)
    .Where(static location => !string.IsNullOrEmpty(location) && File.Exists(location))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .Sum(static location => new FileInfo(location).Length);

var diagnostics = StartupOptimizationDiagnostics.Snapshot;
using var output = new MemoryStream();
using (var writer = new Utf8JsonWriter(output))
{
    writer.WriteStartObject();
    writer.WriteString("schemaVersion", "1.0.0");
    writer.WriteString("profile", profileName);

    writer.WriteStartObject("environment");
    writer.WriteString("runtime", RuntimeInformation.FrameworkDescription);
    writer.WriteString("rid", RuntimeInformation.RuntimeIdentifier);
    writer.WriteString("architecture", RuntimeInformation.ProcessArchitecture.ToString());
    writer.WriteBoolean("serverGc", GCSettings.IsServerGC);
    writer.WriteEndObject();

    writer.WriteStartObject("measurements");
    writer.WriteNumber("processToMainMilliseconds", processToMainMilliseconds);
    writer.WriteNumber("contextMilliseconds", contextMilliseconds);
    writer.WriteNumber("contextAllocatedBytes", contextAllocatedBytes);
    writer.WriteNumber("firstScriptMilliseconds", firstScriptMilliseconds);
    writer.WriteNumber("firstFeatureUseMilliseconds", stopwatch.Elapsed.TotalMilliseconds);
    writer.WriteNumber("workingSetBytes", process.WorkingSet64);
    writer.WriteNumber("loadedAssemblyCount", loaded.Count);
    writer.WriteNumber("loadedAssemblyBytes", assemblyBytes);
    writer.WriteEndObject();

    writer.WriteStartObject("results");
    writer.WriteString("coreResult", coreResult);
    writer.WriteString("featureResult", featureResult);
    writer.WriteBoolean("featureAssemblyLoadedBeforeUse", featureAssemblyLoadedBeforeUse);
    writer.WriteBoolean("featureAssemblyLoadedAfterUse", featureAssemblyLoadedAfterUse);
    writer.WriteEndObject();

    writer.WriteStartObject("diagnostics");
    writer.WriteNumber(nameof(diagnostics.ContextsCreated), diagnostics.ContextsCreated);
    writer.WriteNumber(nameof(diagnostics.FullProfileContexts), diagnostics.FullProfileContexts);
    writer.WriteNumber(nameof(diagnostics.MinimalProfileContexts), diagnostics.MinimalProfileContexts);
    writer.WriteNumber(nameof(diagnostics.LazyCellsCreated), diagnostics.LazyCellsCreated);
    writer.WriteNumber(nameof(diagnostics.LazyCellsRealized), diagnostics.LazyCellsRealized);
    writer.WriteNumber(nameof(diagnostics.LazyCellsCanceled), diagnostics.LazyCellsCanceled);
    writer.WriteNumber(nameof(diagnostics.LazyCellFailures), diagnostics.LazyCellFailures);
    writer.WriteNumber(nameof(diagnostics.FeatureResolutions), diagnostics.FeatureResolutions);
    writer.WriteNumber(nameof(diagnostics.CompatibilityAssemblyProbes), diagnostics.CompatibilityAssemblyProbes);
    writer.WriteEndObject();
    writer.WriteEndObject();
}

Console.WriteLine(Encoding.UTF8.GetString(output.GetBuffer(), 0, checked((int)output.Length)));
