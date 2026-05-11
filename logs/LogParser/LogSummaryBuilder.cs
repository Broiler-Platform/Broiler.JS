using System.Text.Json;
using System.Text.RegularExpressions;

namespace LogParser;

/// <summary>
/// Parses shard logs and produces reusable group summaries.
/// </summary>
public static class LogSummaryBuilder
{
    private const string UnknownStatus = "unknown";
    private const string AllPathsBucket = "(all paths)";
    private const string UnhandledExceptionPrefix = "Unhandled exception. ";
    private const string UnknownContext = "(unknown context)";
    // Matches common .NET and JavaScript stack frames such as:
    //   at InitializeFactories in /repo/File.cs:line 17
    //   at MyClass.Method() in /repo/File.cs:line 17
    //   at Compile:/tmp/script.js:206,1
    // The "method" group captures the method/function token after "at " and trims any
    // trailing whitespace. When parentheses are present, the captured value stops before
    // the opening parenthesis so signatures and locations are not included in the context.
    private static readonly Regex StackFrameContextRegex = new(
        @"^\s*at\s+(?<method>.+?)(?:\s+in\s+|\(|:|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
        return SummarizeLogRun(
            Path.GetFullPath(path),
            ParseFile(path),
            isDirectorySummary: false,
            bucketDepth,
            notableEntryLimit);
    }

    public static LogFileSummary ParseAndSummarizeDirectory(string path, int bucketDepth = 4, int notableEntryLimit = 3)
    {
        var fullPath = Path.GetFullPath(path);
        var filePaths = Directory
            .GetFiles(fullPath, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (filePaths.Length == 0)
        {
            throw new InvalidOperationException($"Directory '{path}' does not contain any .json log files.");
        }

        return SummarizeLogRun(
            fullPath,
            CombineLogRuns(filePaths.Select(ParseFile)),
            isDirectorySummary: true,
            bucketDepth,
            notableEntryLimit);
    }

    private static LogFileSummary SummarizeLogRun(
        string path,
        LogRun logRun,
        bool isDirectorySummary,
        int bucketDepth,
        int notableEntryLimit)
    {
        return new LogFileSummary
        {
            FilePath = path,
            IsDirectorySummary = isDirectorySummary,
            LogRun = logRun,
            BucketDepth = bucketDepth,
            StatusGroups = SummarizeGroups(logRun.Results, entry => entry.Status, notableEntryLimit),
            PathGroups = SummarizeGroups(logRun.Results, entry => GetPathBucket(entry.Path, bucketDepth), notableEntryLimit),
            ExceptionSummary = SummarizeExceptions(logRun.Results, notableEntryLimit)
        };
    }

    private static LogRun CombineLogRuns(IEnumerable<LogRun> logRuns)
    {
        var runs = logRuns.ToArray();
        return new LogRun
        {
            SuiteRef = CombineMetadata(runs.Select(run => run.SuiteRef)),
            BroilerDll = CombineMetadata(runs.Select(run => run.BroilerDll)),
            Executed = runs.Sum(run => run.Executed),
            Passed = runs.Sum(run => run.Passed),
            Failed = runs.Sum(run => run.Failed),
            Skipped = runs.Sum(run => run.Skipped),
            Results = runs.SelectMany(run => run.Results).ToArray()
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

    public static ExceptionAnalysisSummary SummarizeExceptions(IEnumerable<LogEntry> entries, int exampleLimit = 3)
    {
        var allEntries = entries.ToArray();
        var exceptionEntries = allEntries
            .Where(entry => entry.Exception is not null)
            .Select(entry => (Entry: entry, Exception: entry.Exception!))
            .ToArray();

        var totalEntryCount = allEntries.Length;
        var totalExceptionCount = exceptionEntries.Length;

        return new ExceptionAnalysisSummary
        {
            TotalEntriesWithExceptions = totalExceptionCount,
            OccurrenceRate = totalEntryCount == 0 ? 0 : totalExceptionCount / (double)totalEntryCount,
            TypeGroups = exceptionEntries
                .GroupBy(item => item.Exception.Type, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ExceptionTypeSummary
                {
                    Type = group.Key,
                    Count = group.Count(),
                    OccurrenceRate = totalEntryCount == 0 ? 0 : group.Count() / (double)totalEntryCount,
                    DistinctMessageCount = group
                        .Select(item => item.Exception.Message)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count(),
                    Examples = SelectExceptionExamples(group, exampleLimit)
                })
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Type, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            ContextGroups = exceptionEntries
                .GroupBy(item => item.Exception.Type, StringComparer.OrdinalIgnoreCase)
                .SelectMany(typeGroup => typeGroup
                    .GroupBy(item => item.Exception.Context ?? UnknownContext, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new ExceptionContextSummary
                    {
                        Type = typeGroup.Key,
                        Context = group.Key,
                        Count = group.Count(),
                        OccurrenceRate = totalEntryCount == 0 ? 0 : group.Count() / (double)totalEntryCount,
                        DistinctMessageCount = group
                            .Select(item => item.Exception.Message)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Count(),
                        Examples = SelectExceptionExamples(group, exampleLimit)
                    }))
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Type, StringComparer.OrdinalIgnoreCase)
                .ThenBy(group => group.Context, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            MessageGroups = exceptionEntries
                .GroupBy(item => item.Exception.Message, StringComparer.OrdinalIgnoreCase)
                .Select(group => new ExceptionMessageSummary
                {
                    Message = group.Key,
                    Count = group.Count(),
                    OccurrenceRate = totalEntryCount == 0 ? 0 : group.Count() / (double)totalEntryCount,
                    Examples = SelectExceptionExamples(group, exampleLimit)
                })
                .OrderByDescending(group => group.Count)
                .ThenBy(group => group.Message, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            SuggestedPatterns = BuildSuggestedPatterns(exceptionEntries, totalExceptionCount)
        };
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
                    Stderr = entry.Stderr,
                    Exception = TryParseException(entry.Stderr) ?? TryParseException(entry.Stdout)
                })
                .ToArray()
                ?? []
        };
    }

    private static IReadOnlyList<ExceptionExample> SelectExceptionExamples(
        IEnumerable<(LogEntry Entry, ParsedException Exception)> entries,
        int exampleLimit)
    {
        return entries
            .OrderBy(item => item.Entry.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Exception.Message, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(0, exampleLimit))
                .Select(item => new ExceptionExample
                {
                    Path = item.Entry.Path ?? string.Empty,
                    Type = item.Exception.Type,
                    Message = item.Exception.Message,
                    Context = item.Exception.Context,
                    LogLine = item.Exception.LogLine
                })
            .ToArray();
    }

    private static IReadOnlyList<string> BuildSuggestedPatterns(
        IEnumerable<(LogEntry Entry, ParsedException Exception)> exceptionEntries,
        int totalExceptionCount)
    {
        if (totalExceptionCount == 0)
        {
            return [];
        }

        var repeatedMessages = exceptionEntries
            .GroupBy(item => item.Exception.Message, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(group =>
                $"Repeated message \"{group.Key}\" appears {group.Count()} times ({group.Count() / (double)totalExceptionCount:P1} of exception entries).")
            .ToArray();

        if (repeatedMessages.Length > 0)
        {
            return repeatedMessages;
        }

        var repeatedTypes = exceptionEntries
            .GroupBy(item => item.Exception.Type, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(group =>
                $"Exception type \"{group.Key}\" appears {group.Count()} times across {group.Select(item => item.Exception.Message).Distinct(StringComparer.OrdinalIgnoreCase).Count()} distinct messages.")
            .ToArray();

        if (repeatedTypes.Length > 0)
        {
            return repeatedTypes;
        }

        return exceptionEntries
            .GroupBy(item => item.Exception.Type, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(group =>
                $"Most frequent exception type so far is \"{group.Key}\" with {group.Count()} occurrence(s); compare its message and stack examples first.")
            .ToArray();
    }

    private static ParsedException? TryParseException(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var lineIndex = 0;
        foreach (var line in lines)
        {
            if (TryParseExceptionLine(line, out var exceptionType, out var exceptionMessage))
            {
                return new ParsedException
                {
                    Type = exceptionType,
                    Message = exceptionMessage,
                    Context = TryParseExceptionContext(lines, lineIndex + 1),
                    LogLine = line
                };
            }

            lineIndex++;
        }

        return null;
    }

    /// <summary>
    /// Extracts the method or function name from the first recognizable stack frame that follows a parsed
    /// exception line, tolerating intervening wrapper lines until a stack frame is found.
    /// </summary>
    /// <param name="lines">All non-empty log lines from the captured output.</param>
    /// <param name="startIndex">The index immediately after the line that contained the exception header.</param>
    /// <returns>The parsed method or function name, or <see langword="null"/> when no stack frame context is present.</returns>
    private static string? TryParseExceptionContext(IReadOnlyList<string> lines, int startIndex)
    {
        for (var i = startIndex; i < lines.Count; i++)
        {
            var match = StackFrameContextRegex.Match(lines[i]);
            if (match.Success)
            {
                var context = match.Groups["method"].Value.Trim();
                if (!string.IsNullOrWhiteSpace(context))
                {
                    return context;
                }
            }
        }

        return null;
    }

    private static bool TryParseExceptionLine(string line, out string exceptionType, out string exceptionMessage)
    {
        var candidate = line.Trim();
        if (candidate.StartsWith(UnhandledExceptionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[UnhandledExceptionPrefix.Length..].Trim();
        }

        var separatorIndex = candidate.IndexOf(": ", StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            exceptionType = string.Empty;
            exceptionMessage = string.Empty;
            return false;
        }

        var typeCandidate = candidate[..separatorIndex].Trim();
        if (!LooksLikeExceptionType(typeCandidate))
        {
            exceptionType = string.Empty;
            exceptionMessage = string.Empty;
            return false;
        }

        exceptionType = typeCandidate;
        exceptionMessage = candidate[(separatorIndex + 2)..].Trim();
        if (string.IsNullOrEmpty(exceptionMessage))
        {
            exceptionMessage = "(no message)";
        }

        return true;
    }

    private static bool LooksLikeExceptionType(string typeCandidate)
    {
        if (string.IsNullOrWhiteSpace(typeCandidate) || typeCandidate.Any(char.IsWhiteSpace))
        {
            return false;
        }

        return typeCandidate.EndsWith("Exception", StringComparison.Ordinal)
            || typeCandidate.EndsWith("Error", StringComparison.Ordinal)
            || typeCandidate.Contains('.', StringComparison.Ordinal);
    }

    private static string CombineMetadata(IEnumerable<string> values)
    {
        var distinctValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return distinctValues.Length switch
        {
            0 => string.Empty,
            1 => distinctValues[0],
            _ => string.Join(", ", distinctValues)
        };
    }
}
