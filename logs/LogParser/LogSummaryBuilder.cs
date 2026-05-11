using System.Text.Json;

namespace LogParser;

/// <summary>
/// Parses shard logs and produces reusable group summaries.
/// </summary>
public static class LogSummaryBuilder
{
    private const string UnknownStatus = "unknown";
    private const string AllPathsBucket = "(all paths)";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static LogRun ParseFile(string path)
    {
        using var stream = File.OpenRead(path);
        var parsed = JsonSerializer.Deserialize<LogRun>(stream, JsonOptions)
            ?? throw new InvalidOperationException($"Log file '{path}' is empty or invalid.");

        return Normalize(parsed);
    }

    public static LogFileSummary ParseAndSummarize(string path, int bucketDepth = 4, int notableEntryLimit = 3)
    {
        var logRun = ParseFile(path);
        return new LogFileSummary
        {
            FilePath = Path.GetFullPath(path),
            LogRun = logRun,
            BucketDepth = bucketDepth,
            StatusGroups = SummarizeGroups(logRun.Results, entry => entry.Status, notableEntryLimit),
            PathGroups = SummarizeGroups(logRun.Results, entry => GetPathBucket(entry.Path, bucketDepth), notableEntryLimit)
        };
    }

    public static IReadOnlyList<LogGroupSummary> SummarizeGroups(
        IEnumerable<LogEntry> entries,
        Func<LogEntry, string> groupKeySelector,
        int notableEntryLimit = 3)
    {
        return entries
            .GroupBy(groupKeySelector, StringComparer.OrdinalIgnoreCase)
            .Select(group => SummarizeGroup(group, notableEntryLimit))
            .OrderByDescending(group => group.Count)
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string GetPathBucket(string path, int depth)
    {
        if (depth <= 0)
        {
            return AllPathsBucket;
        }

        var segments = path
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length <= depth
            ? path
            : string.Join('/', segments.Take(depth));
    }

    private static LogGroupSummary SummarizeGroup(IGrouping<string, LogEntry> group, int notableEntryLimit)
    {
        var entries = group.ToArray();
        var stdoutLengths = entries.Select(entry => entry.StdoutLength).ToArray();
        var stderrLengths = entries.Select(entry => entry.StderrLength).ToArray();
        var notableEntries = entries
            .OrderByDescending(entry => Math.Max(entry.StdoutLength, entry.StderrLength))
            .ThenBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, notableEntryLimit))
            .Select(entry => new NotableLogEntry
            {
                Path = entry.Path,
                Status = entry.Status,
                StdoutLength = entry.StdoutLength,
                StderrLength = entry.StderrLength
            })
            .ToArray();

        return new LogGroupSummary
        {
            Key = group.Key,
            Count = entries.Length,
            StatusCounts = entries
                .GroupBy(entry => entry.Status, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(statusGroup => statusGroup.Count())
                .ThenBy(statusGroup => statusGroup.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(statusGroup => statusGroup.Key, statusGroup => statusGroup.Count(), StringComparer.OrdinalIgnoreCase),
            MinStdoutLength = stdoutLengths.Min(),
            MaxStdoutLength = stdoutLengths.Max(),
            AverageStdoutLength = stdoutLengths.Average(),
            MinStderrLength = stderrLengths.Min(),
            MaxStderrLength = stderrLengths.Max(),
            AverageStderrLength = stderrLengths.Average(),
            NotableEntries = notableEntries
        };
    }

    private static LogRun Normalize(LogRun parsed)
    {
        return new LogRun
        {
            SuiteRef = parsed.SuiteRef ?? string.Empty,
            BroilerDll = parsed.BroilerDll ?? string.Empty,
            Executed = parsed.Executed,
            Passed = parsed.Passed,
            Failed = parsed.Failed,
            Skipped = parsed.Skipped,
            Results = parsed.Results?
                .Select(entry => new LogEntry
                {
                    Path = entry.Path ?? string.Empty,
                    Status = string.IsNullOrWhiteSpace(entry.Status) ? UnknownStatus : entry.Status,
                    Stdout = entry.Stdout,
                    Stderr = entry.Stderr
                })
                .ToArray()
                ?? []
        };
    }
}
